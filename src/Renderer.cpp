#include "Renderer.h"

#include "ScenePS.h"
#include "SceneVS.h"

#include <algorithm>
#include <array>
#include <cmath>
#include <cstring>
#include <format>
#include <stdexcept>
#include <string>

using DirectX::XMFLOAT3;
using DirectX::XMFLOAT4;
using DirectX::XMFLOAT4X4;
using DirectX::XMMATRIX;
using DirectX::XMVECTOR;
using Microsoft::WRL::ComPtr;

namespace
{
[[noreturn]] void ThrowFailure(const HRESULT result, const char* operation)
{
    throw std::runtime_error(std::format("{} failed with HRESULT 0x{:08X}", operation, static_cast<unsigned long>(result)));
}

void Check(const HRESULT result, const char* operation)
{
    if (FAILED(result))
    {
        ThrowFailure(result, operation);
    }
}

D3D12_RESOURCE_DESC BufferDescription(const std::uint64_t size)
{
    D3D12_RESOURCE_DESC description{};
    description.Dimension = D3D12_RESOURCE_DIMENSION_BUFFER;
    description.Alignment = 0;
    description.Width = size;
    description.Height = 1;
    description.DepthOrArraySize = 1;
    description.MipLevels = 1;
    description.Format = DXGI_FORMAT_UNKNOWN;
    description.SampleDesc = {1, 0};
    description.Layout = D3D12_TEXTURE_LAYOUT_ROW_MAJOR;
    description.Flags = D3D12_RESOURCE_FLAG_NONE;
    return description;
}

D3D12_HEAP_PROPERTIES HeapProperties(const D3D12_HEAP_TYPE type)
{
    D3D12_HEAP_PROPERTIES properties{};
    properties.Type = type;
    properties.CPUPageProperty = D3D12_CPU_PAGE_PROPERTY_UNKNOWN;
    properties.MemoryPoolPreference = D3D12_MEMORY_POOL_UNKNOWN;
    properties.CreationNodeMask = 1;
    properties.VisibleNodeMask = 1;
    return properties;
}

D3D12_RESOURCE_BARRIER TransitionBarrier(
    ID3D12Resource* resource,
    const D3D12_RESOURCE_STATES before,
    const D3D12_RESOURCE_STATES after)
{
    D3D12_RESOURCE_BARRIER barrier{};
    barrier.Type = D3D12_RESOURCE_BARRIER_TYPE_TRANSITION;
    barrier.Flags = D3D12_RESOURCE_BARRIER_FLAG_NONE;
    barrier.Transition.pResource = resource;
    barrier.Transition.Subresource = D3D12_RESOURCE_BARRIER_ALL_SUBRESOURCES;
    barrier.Transition.StateBefore = before;
    barrier.Transition.StateAfter = after;
    return barrier;
}

void SetDebugName(ID3D12Object* object, const wchar_t* name)
{
#if defined(_DEBUG)
    if (object != nullptr)
    {
        object->SetName(name);
    }
#else
    static_cast<void>(object);
    static_cast<void>(name);
#endif
}
}

Renderer::~Renderer()
{
    if (m_commandQueue && m_fence && m_fenceEvent != nullptr)
    {
        try
        {
            WaitForGpu();
        }
        catch (...)
        {
        }
    }

    if (m_constantBuffer && m_mappedConstants != nullptr)
    {
        m_constantBuffer->Unmap(0, nullptr);
        m_mappedConstants = nullptr;
    }

    if (m_fenceEvent != nullptr)
    {
        CloseHandle(m_fenceEvent);
        m_fenceEvent = nullptr;
    }
}

void Renderer::Initialize(HWND window, const std::uint32_t width, const std::uint32_t height)
{
    if (window == nullptr || width == 0 || height == 0)
    {
        throw std::invalid_argument("Renderer initialization requires a valid window and nonzero client size.");
    }

    m_window = window;
    m_width = width;
    m_height = height;

    CreateDevice();
    CreateSwapChain();
    CreateDescriptorHeaps();
    CreatePipeline();
    CreateCommandObjects();
    CreateWindowSizeResources();
    CreateMeshes();
    CreateConstantBuffer();
    UpdateCamera();

    m_animationStart = std::chrono::steady_clock::now();
    m_initialized = true;
}

void Renderer::CreateDevice()
{
    std::uint32_t factoryFlags = 0;
#if defined(_DEBUG)
    ComPtr<ID3D12Debug> debugController;
    if (SUCCEEDED(D3D12GetDebugInterface(IID_PPV_ARGS(&debugController))))
    {
        debugController->EnableDebugLayer();
        factoryFlags |= DXGI_CREATE_FACTORY_DEBUG;
    }
#endif

    Check(CreateDXGIFactory2(factoryFlags, IID_PPV_ARGS(&m_factory)), "CreateDXGIFactory2");

    ComPtr<IDXGIFactory5> factory5;
    BOOL allowTearing = FALSE;
    if (SUCCEEDED(m_factory.As(&factory5)) &&
        SUCCEEDED(factory5->CheckFeatureSupport(
            DXGI_FEATURE_PRESENT_ALLOW_TEARING,
            &allowTearing,
            sizeof(allowTearing))))
    {
        m_tearingSupported = allowTearing == TRUE;
    }

    ComPtr<IDXGIFactory6> factory6;
    const bool supportsGpuPreference = SUCCEEDED(m_factory.As(&factory6));
    ComPtr<IDXGIAdapter1> selectedAdapter;
    for (std::uint32_t adapterIndex = 0;; ++adapterIndex)
    {
        ComPtr<IDXGIAdapter1> candidate;
        const HRESULT enumerationResult = supportsGpuPreference
            ? factory6->EnumAdapterByGpuPreference(
                  adapterIndex,
                  DXGI_GPU_PREFERENCE_HIGH_PERFORMANCE,
                  IID_PPV_ARGS(&candidate))
            : m_factory->EnumAdapters1(adapterIndex, &candidate);
        if (enumerationResult == DXGI_ERROR_NOT_FOUND)
        {
            break;
        }
        Check(enumerationResult, "Enumerate graphics adapters");

        DXGI_ADAPTER_DESC1 description{};
        Check(candidate->GetDesc1(&description), "IDXGIAdapter1::GetDesc1");
        if ((description.Flags & DXGI_ADAPTER_FLAG_SOFTWARE) != 0)
        {
            continue;
        }

        if (SUCCEEDED(D3D12CreateDevice(candidate.Get(), D3D_FEATURE_LEVEL_11_0, __uuidof(ID3D12Device), nullptr)))
        {
            selectedAdapter = candidate;
            break;
        }
    }

    if (!selectedAdapter)
    {
        Check(m_factory->EnumWarpAdapter(IID_PPV_ARGS(&selectedAdapter)), "IDXGIFactory::EnumWarpAdapter");
    }

    Check(
        D3D12CreateDevice(selectedAdapter.Get(), D3D_FEATURE_LEVEL_11_0, IID_PPV_ARGS(&m_device)),
        "D3D12CreateDevice");
    SetDebugName(m_device.Get(), L"D3D12 Device");

    D3D12_FEATURE_DATA_SHADER_MODEL shaderModelSupport{D3D_SHADER_MODEL_6_0};
    if (FAILED(m_device->CheckFeatureSupport(
            D3D12_FEATURE_SHADER_MODEL,
            &shaderModelSupport,
            sizeof(shaderModelSupport))) ||
        shaderModelSupport.HighestShaderModel < D3D_SHADER_MODEL_6_0)
    {
        throw std::runtime_error("The selected DirectX 12 adapter does not support Shader Model 6.0.");
    }

#if defined(_DEBUG)
    ComPtr<ID3D12InfoQueue> infoQueue;
    if (SUCCEEDED(m_device.As(&infoQueue)))
    {
        infoQueue->SetBreakOnSeverity(D3D12_MESSAGE_SEVERITY_CORRUPTION, TRUE);
        infoQueue->SetBreakOnSeverity(D3D12_MESSAGE_SEVERITY_ERROR, TRUE);
    }
#endif

    D3D12_COMMAND_QUEUE_DESC queueDescription{};
    queueDescription.Type = D3D12_COMMAND_LIST_TYPE_DIRECT;
    queueDescription.Priority = D3D12_COMMAND_QUEUE_PRIORITY_NORMAL;
    queueDescription.Flags = D3D12_COMMAND_QUEUE_FLAG_NONE;
    queueDescription.NodeMask = 0;
    Check(m_device->CreateCommandQueue(&queueDescription, IID_PPV_ARGS(&m_commandQueue)), "CreateCommandQueue");
    SetDebugName(m_commandQueue.Get(), L"Direct Command Queue");
}

void Renderer::CreateSwapChain()
{
    DXGI_SWAP_CHAIN_DESC1 description{};
    description.Width = m_width;
    description.Height = m_height;
    description.Format = BackBufferFormat;
    description.Stereo = FALSE;
    description.SampleDesc = {1, 0};
    description.BufferUsage = DXGI_USAGE_RENDER_TARGET_OUTPUT;
    description.BufferCount = FrameCount;
    description.Scaling = DXGI_SCALING_STRETCH;
    description.SwapEffect = DXGI_SWAP_EFFECT_FLIP_DISCARD;
    description.AlphaMode = DXGI_ALPHA_MODE_IGNORE;
    description.Flags = m_tearingSupported ? DXGI_SWAP_CHAIN_FLAG_ALLOW_TEARING : 0;

    ComPtr<IDXGISwapChain1> swapChain;
    Check(
        m_factory->CreateSwapChainForHwnd(
            m_commandQueue.Get(),
            m_window,
            &description,
            nullptr,
            nullptr,
            &swapChain),
        "CreateSwapChainForHwnd");
    Check(swapChain.As(&m_swapChain), "Query IDXGISwapChain3");
    Check(m_factory->MakeWindowAssociation(m_window, DXGI_MWA_NO_ALT_ENTER), "MakeWindowAssociation");
}

void Renderer::CreateDescriptorHeaps()
{
    D3D12_DESCRIPTOR_HEAP_DESC rtvDescription{};
    rtvDescription.Type = D3D12_DESCRIPTOR_HEAP_TYPE_RTV;
    rtvDescription.NumDescriptors = FrameCount;
    rtvDescription.Flags = D3D12_DESCRIPTOR_HEAP_FLAG_NONE;
    Check(m_device->CreateDescriptorHeap(&rtvDescription, IID_PPV_ARGS(&m_rtvHeap)), "Create RTV descriptor heap");
    m_rtvDescriptorSize = m_device->GetDescriptorHandleIncrementSize(D3D12_DESCRIPTOR_HEAP_TYPE_RTV);

    D3D12_DESCRIPTOR_HEAP_DESC dsvDescription{};
    dsvDescription.Type = D3D12_DESCRIPTOR_HEAP_TYPE_DSV;
    dsvDescription.NumDescriptors = 1;
    dsvDescription.Flags = D3D12_DESCRIPTOR_HEAP_FLAG_NONE;
    Check(m_device->CreateDescriptorHeap(&dsvDescription, IID_PPV_ARGS(&m_dsvHeap)), "Create DSV descriptor heap");
}

void Renderer::CreatePipeline()
{
    D3D12_ROOT_PARAMETER rootParameter{};
    rootParameter.ParameterType = D3D12_ROOT_PARAMETER_TYPE_CBV;
    rootParameter.Descriptor.ShaderRegister = 0;
    rootParameter.Descriptor.RegisterSpace = 0;
    rootParameter.ShaderVisibility = D3D12_SHADER_VISIBILITY_ALL;

    D3D12_ROOT_SIGNATURE_DESC rootSignatureDescription{};
    rootSignatureDescription.NumParameters = 1;
    rootSignatureDescription.pParameters = &rootParameter;
    rootSignatureDescription.NumStaticSamplers = 0;
    rootSignatureDescription.pStaticSamplers = nullptr;
    rootSignatureDescription.Flags =
        D3D12_ROOT_SIGNATURE_FLAG_ALLOW_INPUT_ASSEMBLER_INPUT_LAYOUT |
        D3D12_ROOT_SIGNATURE_FLAG_DENY_HULL_SHADER_ROOT_ACCESS |
        D3D12_ROOT_SIGNATURE_FLAG_DENY_DOMAIN_SHADER_ROOT_ACCESS |
        D3D12_ROOT_SIGNATURE_FLAG_DENY_GEOMETRY_SHADER_ROOT_ACCESS;

    ComPtr<ID3DBlob> serializedRootSignature;
    ComPtr<ID3DBlob> errors;
    const HRESULT serializationResult = D3D12SerializeRootSignature(
        &rootSignatureDescription,
        D3D_ROOT_SIGNATURE_VERSION_1,
        &serializedRootSignature,
        &errors);
    if (FAILED(serializationResult))
    {
        const char* details = errors ? static_cast<const char*>(errors->GetBufferPointer()) : "No serializer details.";
        throw std::runtime_error(std::format("Root signature serialization failed: {}", details));
    }

    Check(
        m_device->CreateRootSignature(
            0,
            serializedRootSignature->GetBufferPointer(),
            serializedRootSignature->GetBufferSize(),
            IID_PPV_ARGS(&m_rootSignature)),
        "CreateRootSignature");

    const std::array inputElements = {
        D3D12_INPUT_ELEMENT_DESC{"POSITION", 0, DXGI_FORMAT_R32G32B32_FLOAT, 0, 0, D3D12_INPUT_CLASSIFICATION_PER_VERTEX_DATA, 0},
        D3D12_INPUT_ELEMENT_DESC{"NORMAL", 0, DXGI_FORMAT_R32G32B32_FLOAT, 0, 12, D3D12_INPUT_CLASSIFICATION_PER_VERTEX_DATA, 0},
    };

    D3D12_RASTERIZER_DESC rasterizer{};
    rasterizer.FillMode = D3D12_FILL_MODE_SOLID;
    rasterizer.CullMode = D3D12_CULL_MODE_NONE;
    rasterizer.FrontCounterClockwise = FALSE;
    rasterizer.DepthBias = D3D12_DEFAULT_DEPTH_BIAS;
    rasterizer.DepthBiasClamp = D3D12_DEFAULT_DEPTH_BIAS_CLAMP;
    rasterizer.SlopeScaledDepthBias = D3D12_DEFAULT_SLOPE_SCALED_DEPTH_BIAS;
    rasterizer.DepthClipEnable = TRUE;
    rasterizer.MultisampleEnable = FALSE;
    rasterizer.AntialiasedLineEnable = FALSE;
    rasterizer.ForcedSampleCount = 0;
    rasterizer.ConservativeRaster = D3D12_CONSERVATIVE_RASTERIZATION_MODE_OFF;

    D3D12_BLEND_DESC blend{};
    blend.AlphaToCoverageEnable = FALSE;
    blend.IndependentBlendEnable = FALSE;
    blend.RenderTarget[0].BlendEnable = FALSE;
    blend.RenderTarget[0].LogicOpEnable = FALSE;
    blend.RenderTarget[0].SrcBlend = D3D12_BLEND_ONE;
    blend.RenderTarget[0].DestBlend = D3D12_BLEND_ZERO;
    blend.RenderTarget[0].BlendOp = D3D12_BLEND_OP_ADD;
    blend.RenderTarget[0].SrcBlendAlpha = D3D12_BLEND_ONE;
    blend.RenderTarget[0].DestBlendAlpha = D3D12_BLEND_ZERO;
    blend.RenderTarget[0].BlendOpAlpha = D3D12_BLEND_OP_ADD;
    blend.RenderTarget[0].LogicOp = D3D12_LOGIC_OP_NOOP;
    blend.RenderTarget[0].RenderTargetWriteMask = D3D12_COLOR_WRITE_ENABLE_ALL;

    D3D12_DEPTH_STENCIL_DESC depthStencil{};
    depthStencil.DepthEnable = TRUE;
    depthStencil.DepthWriteMask = D3D12_DEPTH_WRITE_MASK_ALL;
    depthStencil.DepthFunc = D3D12_COMPARISON_FUNC_LESS;
    depthStencil.StencilEnable = FALSE;
    depthStencil.StencilReadMask = D3D12_DEFAULT_STENCIL_READ_MASK;
    depthStencil.StencilWriteMask = D3D12_DEFAULT_STENCIL_WRITE_MASK;

    D3D12_GRAPHICS_PIPELINE_STATE_DESC pipelineDescription{};
    pipelineDescription.pRootSignature = m_rootSignature.Get();
    pipelineDescription.VS = {g_sceneVertexShader, sizeof(g_sceneVertexShader)};
    pipelineDescription.PS = {g_scenePixelShader, sizeof(g_scenePixelShader)};
    pipelineDescription.BlendState = blend;
    pipelineDescription.SampleMask = UINT_MAX;
    pipelineDescription.RasterizerState = rasterizer;
    pipelineDescription.DepthStencilState = depthStencil;
    pipelineDescription.InputLayout = {inputElements.data(), static_cast<std::uint32_t>(inputElements.size())};
    pipelineDescription.IBStripCutValue = D3D12_INDEX_BUFFER_STRIP_CUT_VALUE_DISABLED;
    pipelineDescription.PrimitiveTopologyType = D3D12_PRIMITIVE_TOPOLOGY_TYPE_TRIANGLE;
    pipelineDescription.NumRenderTargets = 1;
    pipelineDescription.RTVFormats[0] = BackBufferFormat;
    pipelineDescription.DSVFormat = DepthBufferFormat;
    pipelineDescription.SampleDesc = {1, 0};
    Check(
        m_device->CreateGraphicsPipelineState(&pipelineDescription, IID_PPV_ARGS(&m_pipelineState)),
        "CreateGraphicsPipelineState");
}

void Renderer::CreateCommandObjects()
{
    for (std::uint32_t frameIndex = 0; frameIndex < FrameCount; ++frameIndex)
    {
        Check(
            m_device->CreateCommandAllocator(
                D3D12_COMMAND_LIST_TYPE_DIRECT,
                IID_PPV_ARGS(&m_commandAllocators[frameIndex])),
            "CreateCommandAllocator");
    }

    Check(
        m_device->CreateCommandList(
            0,
            D3D12_COMMAND_LIST_TYPE_DIRECT,
            m_commandAllocators[0].Get(),
            m_pipelineState.Get(),
            IID_PPV_ARGS(&m_commandList)),
        "CreateCommandList");
    Check(m_commandList->Close(), "Close initial command list");

    Check(m_device->CreateFence(0, D3D12_FENCE_FLAG_NONE, IID_PPV_ARGS(&m_fence)), "CreateFence");
    m_fenceEvent = CreateEventW(nullptr, FALSE, FALSE, nullptr);
    if (m_fenceEvent == nullptr)
    {
        throw std::runtime_error(std::format("CreateEventW failed with Win32 error {}", GetLastError()));
    }
}

void Renderer::CreateWindowSizeResources()
{
    D3D12_CPU_DESCRIPTOR_HANDLE rtvHandle = m_rtvHeap->GetCPUDescriptorHandleForHeapStart();
    for (std::uint32_t frameIndex = 0; frameIndex < FrameCount; ++frameIndex)
    {
        Check(m_swapChain->GetBuffer(frameIndex, IID_PPV_ARGS(&m_renderTargets[frameIndex])), "Get swap-chain buffer");
        m_device->CreateRenderTargetView(m_renderTargets[frameIndex].Get(), nullptr, rtvHandle);
        rtvHandle.ptr += m_rtvDescriptorSize;
    }

    D3D12_RESOURCE_DESC depthDescription{};
    depthDescription.Dimension = D3D12_RESOURCE_DIMENSION_TEXTURE2D;
    depthDescription.Alignment = 0;
    depthDescription.Width = m_width;
    depthDescription.Height = m_height;
    depthDescription.DepthOrArraySize = 1;
    depthDescription.MipLevels = 1;
    depthDescription.Format = DepthBufferFormat;
    depthDescription.SampleDesc = {1, 0};
    depthDescription.Layout = D3D12_TEXTURE_LAYOUT_UNKNOWN;
    depthDescription.Flags = D3D12_RESOURCE_FLAG_ALLOW_DEPTH_STENCIL;

    D3D12_CLEAR_VALUE clearValue{};
    clearValue.Format = DepthBufferFormat;
    clearValue.DepthStencil = {1.0f, 0};
    const D3D12_HEAP_PROPERTIES defaultHeap = HeapProperties(D3D12_HEAP_TYPE_DEFAULT);
    Check(
        m_device->CreateCommittedResource(
            &defaultHeap,
            D3D12_HEAP_FLAG_NONE,
            &depthDescription,
            D3D12_RESOURCE_STATE_DEPTH_WRITE,
            &clearValue,
            IID_PPV_ARGS(&m_depthBuffer)),
        "Create depth buffer");

    D3D12_DEPTH_STENCIL_VIEW_DESC dsvDescription{};
    dsvDescription.Format = DepthBufferFormat;
    dsvDescription.ViewDimension = D3D12_DSV_DIMENSION_TEXTURE2D;
    dsvDescription.Flags = D3D12_DSV_FLAG_NONE;
    dsvDescription.Texture2D.MipSlice = 0;
    m_device->CreateDepthStencilView(
        m_depthBuffer.Get(),
        &dsvDescription,
        m_dsvHeap->GetCPUDescriptorHandleForHeapStart());

    m_viewport = {
        0.0f,
        0.0f,
        static_cast<float>(m_width),
        static_cast<float>(m_height),
        0.0f,
        1.0f,
    };
    m_scissorRect = {0, 0, static_cast<LONG>(m_width), static_cast<LONG>(m_height)};
}

Renderer::GpuMesh Renderer::UploadMesh(const MeshData& mesh, const wchar_t* debugName) const
{
    if (mesh.vertices.empty() || mesh.indices.empty())
    {
        throw std::invalid_argument("Cannot upload an empty mesh.");
    }

    GpuMesh gpuMesh;
    const std::uint64_t vertexBytes = mesh.vertices.size() * sizeof(Vertex);
    const std::uint64_t indexBytes = mesh.indices.size() * sizeof(std::uint32_t);
    const D3D12_HEAP_PROPERTIES uploadHeap = HeapProperties(D3D12_HEAP_TYPE_UPLOAD);
    const D3D12_RESOURCE_DESC vertexDescription = BufferDescription(vertexBytes);
    const D3D12_RESOURCE_DESC indexDescription = BufferDescription(indexBytes);

    Check(
        m_device->CreateCommittedResource(
            &uploadHeap,
            D3D12_HEAP_FLAG_NONE,
            &vertexDescription,
            D3D12_RESOURCE_STATE_GENERIC_READ,
            nullptr,
            IID_PPV_ARGS(&gpuMesh.vertexBuffer)),
        "Create vertex buffer");
    Check(
        m_device->CreateCommittedResource(
            &uploadHeap,
            D3D12_HEAP_FLAG_NONE,
            &indexDescription,
            D3D12_RESOURCE_STATE_GENERIC_READ,
            nullptr,
            IID_PPV_ARGS(&gpuMesh.indexBuffer)),
        "Create index buffer");

    void* mappedData = nullptr;
    D3D12_RANGE noCpuReads{0, 0};
    Check(gpuMesh.vertexBuffer->Map(0, &noCpuReads, &mappedData), "Map vertex buffer");
    std::memcpy(mappedData, mesh.vertices.data(), static_cast<std::size_t>(vertexBytes));
    D3D12_RANGE writtenVertices{0, static_cast<SIZE_T>(vertexBytes)};
    gpuMesh.vertexBuffer->Unmap(0, &writtenVertices);

    Check(gpuMesh.indexBuffer->Map(0, &noCpuReads, &mappedData), "Map index buffer");
    std::memcpy(mappedData, mesh.indices.data(), static_cast<std::size_t>(indexBytes));
    D3D12_RANGE writtenIndices{0, static_cast<SIZE_T>(indexBytes)};
    gpuMesh.indexBuffer->Unmap(0, &writtenIndices);

    gpuMesh.vertexView.BufferLocation = gpuMesh.vertexBuffer->GetGPUVirtualAddress();
    gpuMesh.vertexView.SizeInBytes = static_cast<std::uint32_t>(vertexBytes);
    gpuMesh.vertexView.StrideInBytes = sizeof(Vertex);
    gpuMesh.indexView.BufferLocation = gpuMesh.indexBuffer->GetGPUVirtualAddress();
    gpuMesh.indexView.SizeInBytes = static_cast<std::uint32_t>(indexBytes);
    gpuMesh.indexView.Format = DXGI_FORMAT_R32_UINT;
    gpuMesh.indexCount = static_cast<std::uint32_t>(mesh.indices.size());

    SetDebugName(gpuMesh.vertexBuffer.Get(), debugName);
    return gpuMesh;
}

void Renderer::CreateMeshes()
{
    m_groundMesh = UploadMesh(CreateGroundPlane(10.0f, 5.0f), L"Ground mesh vertex buffer");
    m_cubeMesh = UploadMesh(CreateCube(1.0f), L"Cube mesh vertex buffer");
    m_sphereMesh = UploadMesh(CreateSphere(0.5f, 33, 64), L"Sphere mesh vertex buffer");
}

void Renderer::CreateConstantBuffer()
{
    const std::uint64_t bufferSize = sizeof(ObjectConstants) * FrameCount * SceneObjectCount;
    const D3D12_HEAP_PROPERTIES uploadHeap = HeapProperties(D3D12_HEAP_TYPE_UPLOAD);
    const D3D12_RESOURCE_DESC description = BufferDescription(bufferSize);
    Check(
        m_device->CreateCommittedResource(
            &uploadHeap,
            D3D12_HEAP_FLAG_NONE,
            &description,
            D3D12_RESOURCE_STATE_GENERIC_READ,
            nullptr,
            IID_PPV_ARGS(&m_constantBuffer)),
        "Create constant buffer");

    D3D12_RANGE noCpuReads{0, 0};
    void* mappedData = nullptr;
    Check(m_constantBuffer->Map(0, &noCpuReads, &mappedData), "Map constant buffer");
    m_mappedConstants = static_cast<std::byte*>(mappedData);
}

void Renderer::UpdateCamera()
{
    const XMVECTOR eye = DirectX::XMVectorSet(1.6889755f, 3.6863865f, 2.9253915f, 1.0f);
    const XMVECTOR target = DirectX::XMVectorZero();
    const XMVECTOR up = DirectX::XMVectorSet(-0.36863866f, 0.6755902f, -0.63850087f, 0.0f);
    const XMMATRIX view = DirectX::XMMatrixLookAtRH(eye, target, up);
    const float aspectRatio = static_cast<float>(m_width) / static_cast<float>(m_height);
    const XMMATRIX projection = DirectX::XMMatrixOrthographicRH(5.0f * aspectRatio, 5.0f, 1.0f, 20.0f);
    DirectX::XMStoreFloat4x4(&m_view, view);
    DirectX::XMStoreFloat4x4(&m_projection, projection);
}

float Renderer::SpherePosition() const
{
    constexpr double speed = 8.0;
    constexpr double travelDistance = 7.0;
    constexpr double fullCycleDistance = travelDistance * 4.0;

    const double elapsed = std::chrono::duration<double>(
        std::chrono::steady_clock::now() - m_animationStart).count();
    const double phase = std::fmod(elapsed * speed + travelDistance, fullCycleDistance);
    return static_cast<float>(travelDistance - std::abs(phase - travelDistance * 2.0));
}

void Renderer::WriteObjectConstants(
    const std::uint32_t frameIndex,
    const std::uint32_t objectIndex,
    DirectX::FXMMATRIX world,
    const XMFLOAT4& color,
    const bool unlit)
{
    const XMMATRIX view = DirectX::XMLoadFloat4x4(&m_view);
    const XMMATRIX projection = DirectX::XMLoadFloat4x4(&m_projection);

    ObjectConstants constants{};
    DirectX::XMStoreFloat4x4(&constants.worldViewProjection, world * view * projection);
    DirectX::XMStoreFloat4x4(&constants.world, world);
    constants.baseColor = color;
    constants.lighting = {0.5f, 0.70710677f, 0.5f, unlit ? 1.0f : 0.0f};

    const std::size_t slot = static_cast<std::size_t>(frameIndex) * SceneObjectCount + objectIndex;
    std::memcpy(m_mappedConstants + slot * sizeof(ObjectConstants), &constants, sizeof(constants));
}

void Renderer::DrawMesh(
    const GpuMesh& mesh,
    const std::uint32_t frameIndex,
    const std::uint32_t objectIndex)
{
    const std::uint64_t slot = static_cast<std::uint64_t>(frameIndex) * SceneObjectCount + objectIndex;
    const D3D12_GPU_VIRTUAL_ADDRESS constantsAddress =
        m_constantBuffer->GetGPUVirtualAddress() + slot * sizeof(ObjectConstants);

    m_commandList->SetGraphicsRootConstantBufferView(0, constantsAddress);
    m_commandList->IASetVertexBuffers(0, 1, &mesh.vertexView);
    m_commandList->IASetIndexBuffer(&mesh.indexView);
    m_commandList->DrawIndexedInstanced(mesh.indexCount, 1, 0, 0, 0);
}

void Renderer::Render()
{
    if (!m_initialized || m_width == 0 || m_height == 0)
    {
        return;
    }

    const std::uint32_t frameIndex = m_swapChain->GetCurrentBackBufferIndex();
    WaitForFrame(frameIndex);

    const XMMATRIX groundWorld = DirectX::XMMatrixIdentity();
    const XMMATRIX cubeWorld = DirectX::XMMatrixTranslation(0.0f, 0.5f, -1.5f);
    const XMMATRIX sphereWorld = DirectX::XMMatrixTranslation(SpherePosition(), 0.5f, 0.0f);

    WriteObjectConstants(frameIndex, 0, groundWorld, {0.08900002f, 0.89f, 0.48950002f, 1.0f}, false);
    WriteObjectConstants(frameIndex, 1, cubeWorld, {0.88f, 0.1672f, 0.17907982f, 1.0f}, false);
    WriteObjectConstants(frameIndex, 2, sphereWorld, {0.17000002f, 0.47433347f, 1.0f, 1.0f}, true);

    Check(m_commandAllocators[frameIndex]->Reset(), "Reset command allocator");
    Check(m_commandList->Reset(m_commandAllocators[frameIndex].Get(), m_pipelineState.Get()), "Reset command list");

    m_commandList->SetGraphicsRootSignature(m_rootSignature.Get());
    m_commandList->RSSetViewports(1, &m_viewport);
    m_commandList->RSSetScissorRects(1, &m_scissorRect);

    D3D12_RESOURCE_BARRIER toRenderTarget = TransitionBarrier(
        m_renderTargets[frameIndex].Get(),
        D3D12_RESOURCE_STATE_PRESENT,
        D3D12_RESOURCE_STATE_RENDER_TARGET);
    m_commandList->ResourceBarrier(1, &toRenderTarget);

    D3D12_CPU_DESCRIPTOR_HANDLE rtvHandle = m_rtvHeap->GetCPUDescriptorHandleForHeapStart();
    rtvHandle.ptr += static_cast<SIZE_T>(frameIndex) * m_rtvDescriptorSize;
    const D3D12_CPU_DESCRIPTOR_HANDLE dsvHandle = m_dsvHeap->GetCPUDescriptorHandleForHeapStart();
    m_commandList->OMSetRenderTargets(1, &rtvHandle, FALSE, &dsvHandle);

    constexpr std::array clearColor{0.3f, 0.3f, 0.3f, 1.0f};
    m_commandList->ClearRenderTargetView(rtvHandle, clearColor.data(), 0, nullptr);
    m_commandList->ClearDepthStencilView(dsvHandle, D3D12_CLEAR_FLAG_DEPTH, 1.0f, 0, 0, nullptr);

    m_commandList->IASetPrimitiveTopology(D3D_PRIMITIVE_TOPOLOGY_TRIANGLELIST);
    DrawMesh(m_groundMesh, frameIndex, 0);
    DrawMesh(m_cubeMesh, frameIndex, 1);
    DrawMesh(m_sphereMesh, frameIndex, 2);

    D3D12_RESOURCE_BARRIER toPresent = TransitionBarrier(
        m_renderTargets[frameIndex].Get(),
        D3D12_RESOURCE_STATE_RENDER_TARGET,
        D3D12_RESOURCE_STATE_PRESENT);
    m_commandList->ResourceBarrier(1, &toPresent);
    Check(m_commandList->Close(), "Close command list");

    ID3D12CommandList* commandLists[] = {m_commandList.Get()};
    m_commandQueue->ExecuteCommandLists(1, commandLists);

    const std::uint32_t presentFlags =
        !m_vsyncEnabled && m_tearingSupported ? DXGI_PRESENT_ALLOW_TEARING : 0;
    const HRESULT presentResult = m_swapChain->Present(m_vsyncEnabled ? 1 : 0, presentFlags);
    if (FAILED(presentResult))
    {
        if (presentResult == DXGI_ERROR_DEVICE_REMOVED || presentResult == DXGI_ERROR_DEVICE_RESET)
        {
            ThrowFailure(m_device->GetDeviceRemovedReason(), "D3D12 device");
        }
        ThrowFailure(presentResult, "Present");
    }

    SignalFrame(frameIndex);
}

void Renderer::Resize(const std::uint32_t width, const std::uint32_t height)
{
    if (!m_initialized || width == 0 || height == 0 || (width == m_width && height == m_height))
    {
        return;
    }

    WaitForGpu();
    for (ComPtr<ID3D12Resource>& renderTarget : m_renderTargets)
    {
        renderTarget.Reset();
    }
    m_depthBuffer.Reset();

    Check(
        m_swapChain->ResizeBuffers(
            FrameCount,
            width,
            height,
            BackBufferFormat,
            m_tearingSupported ? DXGI_SWAP_CHAIN_FLAG_ALLOW_TEARING : 0),
        "ResizeBuffers");
    m_width = width;
    m_height = height;
    CreateWindowSizeResources();
    UpdateCamera();
}

void Renderer::WaitForFrame(const std::uint32_t frameIndex)
{
    const std::uint64_t fenceValue = m_frameFenceValues[frameIndex];
    if (fenceValue == 0 || m_fence->GetCompletedValue() >= fenceValue)
    {
        return;
    }

    Check(m_fence->SetEventOnCompletion(fenceValue, m_fenceEvent), "SetEventOnCompletion");
    if (WaitForSingleObject(m_fenceEvent, INFINITE) != WAIT_OBJECT_0)
    {
        throw std::runtime_error(std::format("Fence wait failed with Win32 error {}", GetLastError()));
    }
}

void Renderer::SignalFrame(const std::uint32_t frameIndex)
{
    const std::uint64_t fenceValue = m_nextFenceValue++;
    Check(m_commandQueue->Signal(m_fence.Get(), fenceValue), "Signal frame fence");
    m_frameFenceValues[frameIndex] = fenceValue;
}

void Renderer::WaitForGpu()
{
    const std::uint64_t fenceValue = m_nextFenceValue++;
    Check(m_commandQueue->Signal(m_fence.Get(), fenceValue), "Signal GPU flush fence");
    if (m_fence->GetCompletedValue() < fenceValue)
    {
        Check(m_fence->SetEventOnCompletion(fenceValue, m_fenceEvent), "Set GPU flush event");
        if (WaitForSingleObject(m_fenceEvent, INFINITE) != WAIT_OBJECT_0)
        {
            throw std::runtime_error(std::format("GPU flush wait failed with Win32 error {}", GetLastError()));
        }
    }
}
