#include "Renderer.h"
#include "UVSphere.h"

#include "BackgroundPS.h"
#include "BackgroundVS.h"
#include "SimplePaintPS.h"
#include "SimplePaintVS.h"
#include "generated/CarMesh.generated.h"

#include <wincodec.h>

#include <algorithm>
#include <array>
#include <charconv>
#include <cmath>
#include <cctype>
#include <cstring>
#include <filesystem>
#include <format>
#include <fstream>
#include <limits>
#include <numbers>
#include <stdexcept>
#include <string>
#include <string_view>
#include <vector>

using DirectX::XMFLOAT3;
using DirectX::XMFLOAT4;
using DirectX::XMMATRIX;
using DirectX::XMVECTOR;
using Microsoft::WRL::ComPtr;

namespace
{
[[noreturn]] void ThrowFailure(const HRESULT result, const char* operation)
{
    throw std::runtime_error(std::format(
        "{} failed with HRESULT 0x{:08X}",
        operation,
        static_cast<unsigned long>(result)));
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

D3D12_RESOURCE_DESC TextureDescription(const std::uint32_t width, const std::uint32_t height)
{
    D3D12_RESOURCE_DESC description{};
    description.Dimension = D3D12_RESOURCE_DIMENSION_TEXTURE2D;
    description.Alignment = 0;
    description.Width = width;
    description.Height = height;
    description.DepthOrArraySize = 1;
    description.MipLevels = 1;
    description.Format = DXGI_FORMAT_R8G8B8A8_TYPELESS;
    description.SampleDesc = {1, 0};
    description.Layout = D3D12_TEXTURE_LAYOUT_UNKNOWN;
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

ComPtr<ID3D12RootSignature> CreateRootSignature(
    ID3D12Device* device,
    const D3D12_ROOT_SIGNATURE_DESC& description,
    const char* operation)
{
    ComPtr<ID3DBlob> serialized;
    ComPtr<ID3DBlob> errors;
    const HRESULT serializationResult = D3D12SerializeRootSignature(
        &description,
        D3D_ROOT_SIGNATURE_VERSION_1,
        &serialized,
        &errors);
    if (FAILED(serializationResult))
    {
        const std::string details = errors
            ? std::string(
                  static_cast<const char*>(errors->GetBufferPointer()),
                  errors->GetBufferSize())
            : "No serializer details.";
        throw std::runtime_error(std::format("{} serialization failed: {}", operation, details));
    }

    ComPtr<ID3D12RootSignature> rootSignature;
    Check(
        device->CreateRootSignature(
            0,
            serialized->GetBufferPointer(),
            serialized->GetBufferSize(),
            IID_PPV_ARGS(&rootSignature)),
        operation);
    return rootSignature;
}

std::filesystem::path ModuleDirectory()
{
    std::wstring modulePath(32768, L'\0');
    const DWORD length = GetModuleFileNameW(
        nullptr,
        modulePath.data(),
        static_cast<DWORD>(modulePath.size()));
    if (length == 0 || static_cast<std::size_t>(length) >= modulePath.size())
    {
        throw std::runtime_error(std::format(
            "GetModuleFileNameW failed with Win32 error {}.",
            GetLastError()));
    }
    modulePath.resize(length);
    return std::filesystem::path(modulePath).parent_path();
}

class ScopedComInitialization final
{
public:
    ScopedComInitialization()
    {
        const HRESULT result = CoInitializeEx(nullptr, COINIT_MULTITHREADED);
        if (result == RPC_E_CHANGED_MODE)
        {
            return;
        }
        Check(result, "CoInitializeEx");
        m_uninitialize = true;
    }

    ~ScopedComInitialization()
    {
        if (m_uninitialize)
        {
            CoUninitialize();
        }
    }

private:
    bool m_uninitialize = false;
};

struct DecodedImage
{
    std::uint32_t width = 0;
    std::uint32_t height = 0;
    std::vector<std::byte> pixels;
};

DecodedImage DecodeRgbaImage(const std::filesystem::path& path)
{
    ScopedComInitialization comInitialization;

    ComPtr<IWICImagingFactory> factory;
    Check(
        CoCreateInstance(
            CLSID_WICImagingFactory,
            nullptr,
            CLSCTX_INPROC_SERVER,
            IID_PPV_ARGS(&factory)),
        "Create WIC imaging factory");

    ComPtr<IWICBitmapDecoder> decoder;
    Check(
        factory->CreateDecoderFromFilename(
            path.c_str(),
            nullptr,
            GENERIC_READ,
            WICDecodeMetadataCacheOnLoad,
            &decoder),
        "Decode background image");

    ComPtr<IWICBitmapFrameDecode> frame;
    Check(decoder->GetFrame(0, &frame), "Get background image frame");

    ComPtr<IWICFormatConverter> converter;
    Check(factory->CreateFormatConverter(&converter), "Create WIC format converter");
    Check(
        converter->Initialize(
            frame.Get(),
            GUID_WICPixelFormat32bppRGBA,
            WICBitmapDitherTypeNone,
            nullptr,
            0.0,
            WICBitmapPaletteTypeCustom),
        "Convert background image to RGBA");

    UINT width = 0;
    UINT height = 0;
    Check(converter->GetSize(&width, &height), "Read background image size");
    if (width == 0 || height == 0 || width > std::numeric_limits<UINT>::max() / 4u)
    {
        throw std::runtime_error("The background image has invalid dimensions.");
    }

    const UINT stride = width * 4u;
    const std::uint64_t byteCount64 = static_cast<std::uint64_t>(stride) * height;
    if (byteCount64 > std::numeric_limits<UINT>::max())
    {
        throw std::runtime_error("The background image is too large for WIC.");
    }

    DecodedImage image;
    image.width = width;
    image.height = height;
    image.pixels.resize(static_cast<std::size_t>(byteCount64));
    Check(
        converter->CopyPixels(
            nullptr,
            stride,
            static_cast<UINT>(byteCount64),
            reinterpret_cast<BYTE*>(image.pixels.data())),
        "Copy decoded background pixels");
    return image;
}

std::string_view Trim(std::string_view value)
{
    while (!value.empty() && std::isspace(static_cast<unsigned char>(value.front())) != 0)
    {
        value.remove_prefix(1);
    }
    while (!value.empty() && std::isspace(static_cast<unsigned char>(value.back())) != 0)
    {
        value.remove_suffix(1);
    }
    return value;
}

float ParseFloat(const std::string_view text, const std::size_t lineNumber)
{
    const std::string_view trimmed = Trim(text);
    float value = 0.0f;
    const auto [end, error] = std::from_chars(
        trimmed.data(),
        trimmed.data() + trimmed.size(),
        value);
    if (error != std::errc{} || end != trimmed.data() + trimmed.size() || !std::isfinite(value))
    {
        throw std::runtime_error(std::format(
            "Invalid finite floating-point value on CarPaint.ini line {}.",
            lineNumber));
    }
    return value;
}

XMFLOAT3 ParseColor(const std::string_view text, const std::size_t lineNumber)
{
    const std::size_t firstComma = text.find(',');
    const std::size_t secondComma = firstComma == std::string_view::npos
        ? std::string_view::npos
        : text.find(',', firstComma + 1);
    if (firstComma == std::string_view::npos ||
        secondComma == std::string_view::npos ||
        text.find(',', secondComma + 1) != std::string_view::npos)
    {
        throw std::runtime_error(std::format(
            "Expected an R, G, B triple on CarPaint.ini line {}.",
            lineNumber));
    }

    return {
        ParseFloat(text.substr(0, firstComma), lineNumber),
        ParseFloat(text.substr(firstComma + 1, secondComma - firstComma - 1), lineNumber),
        ParseFloat(text.substr(secondComma + 1), lineNumber),
    };
}

void RequireUnitRange(const float value, const std::string_view name)
{
    if (value < 0.0f || value > 1.0f)
    {
        throw std::runtime_error(std::format("{} must be in the range [0, 1].", name));
    }
}

float SrgbToLinear(const float value)
{
    return value <= 0.04045f
        ? value / 12.92f
        : std::pow((value + 0.055f) / 1.055f, 2.4f);
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

    LoadPaintSettings();
    CreateDevice();
    CreateSwapChain();
    CreateDescriptorHeaps();
    CreatePipelines();
    CreateCommandObjects();
    CreateWindowSizeResources();
    CreateStaticResources();
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

        if (SUCCEEDED(D3D12CreateDevice(
                candidate.Get(),
                D3D_FEATURE_LEVEL_11_0,
                __uuidof(ID3D12Device),
                nullptr)))
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
    description.Format = SwapChainFormat;
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

    D3D12_DESCRIPTOR_HEAP_DESC srvDescription{};
    srvDescription.Type = D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV;
    srvDescription.NumDescriptors = 1;
    srvDescription.Flags = D3D12_DESCRIPTOR_HEAP_FLAG_SHADER_VISIBLE;
    Check(m_device->CreateDescriptorHeap(&srvDescription, IID_PPV_ARGS(&m_srvHeap)), "Create SRV descriptor heap");
}

void Renderer::CreatePipelines()
{
    D3D12_DESCRIPTOR_RANGE backgroundRange{};
    backgroundRange.RangeType = D3D12_DESCRIPTOR_RANGE_TYPE_SRV;
    backgroundRange.NumDescriptors = 1;
    backgroundRange.BaseShaderRegister = 0;
    backgroundRange.RegisterSpace = 0;
    backgroundRange.OffsetInDescriptorsFromTableStart = D3D12_DESCRIPTOR_RANGE_OFFSET_APPEND;

    std::array<D3D12_ROOT_PARAMETER, 2> backgroundParameters{};
    backgroundParameters[0].ParameterType = D3D12_ROOT_PARAMETER_TYPE_DESCRIPTOR_TABLE;
    backgroundParameters[0].DescriptorTable.NumDescriptorRanges = 1;
    backgroundParameters[0].DescriptorTable.pDescriptorRanges = &backgroundRange;
    backgroundParameters[0].ShaderVisibility = D3D12_SHADER_VISIBILITY_PIXEL;
    backgroundParameters[1].ParameterType = D3D12_ROOT_PARAMETER_TYPE_32BIT_CONSTANTS;
    backgroundParameters[1].Constants.ShaderRegister = 0;
    backgroundParameters[1].Constants.RegisterSpace = 0;
    backgroundParameters[1].Constants.Num32BitValues = 1;
    backgroundParameters[1].ShaderVisibility = D3D12_SHADER_VISIBILITY_VERTEX;

    D3D12_STATIC_SAMPLER_DESC sampler{};
    sampler.Filter = D3D12_FILTER_MIN_MAG_MIP_LINEAR;
    sampler.AddressU = D3D12_TEXTURE_ADDRESS_MODE_CLAMP;
    sampler.AddressV = D3D12_TEXTURE_ADDRESS_MODE_CLAMP;
    sampler.AddressW = D3D12_TEXTURE_ADDRESS_MODE_CLAMP;
    sampler.MipLODBias = 0.0f;
    sampler.MaxAnisotropy = 1;
    sampler.ComparisonFunc = D3D12_COMPARISON_FUNC_ALWAYS;
    sampler.BorderColor = D3D12_STATIC_BORDER_COLOR_OPAQUE_BLACK;
    sampler.MinLOD = 0.0f;
    sampler.MaxLOD = D3D12_FLOAT32_MAX;
    sampler.ShaderRegister = 0;
    sampler.RegisterSpace = 0;
    sampler.ShaderVisibility = D3D12_SHADER_VISIBILITY_PIXEL;

    D3D12_ROOT_SIGNATURE_DESC backgroundRootDescription{};
    backgroundRootDescription.NumParameters = static_cast<std::uint32_t>(backgroundParameters.size());
    backgroundRootDescription.pParameters = backgroundParameters.data();
    backgroundRootDescription.NumStaticSamplers = 1;
    backgroundRootDescription.pStaticSamplers = &sampler;
    backgroundRootDescription.Flags =
        D3D12_ROOT_SIGNATURE_FLAG_DENY_HULL_SHADER_ROOT_ACCESS |
        D3D12_ROOT_SIGNATURE_FLAG_DENY_DOMAIN_SHADER_ROOT_ACCESS |
        D3D12_ROOT_SIGNATURE_FLAG_DENY_GEOMETRY_SHADER_ROOT_ACCESS;
    m_backgroundRootSignature = CreateRootSignature(
        m_device.Get(),
        backgroundRootDescription,
        "Create background root signature");

    D3D12_ROOT_PARAMETER carParameter{};
    carParameter.ParameterType = D3D12_ROOT_PARAMETER_TYPE_CBV;
    carParameter.Descriptor.ShaderRegister = 0;
    carParameter.Descriptor.RegisterSpace = 0;
    carParameter.ShaderVisibility = D3D12_SHADER_VISIBILITY_ALL;

    D3D12_ROOT_SIGNATURE_DESC carRootDescription{};
    carRootDescription.NumParameters = 1;
    carRootDescription.pParameters = &carParameter;
    carRootDescription.NumStaticSamplers = 0;
    carRootDescription.pStaticSamplers = nullptr;
    carRootDescription.Flags =
        D3D12_ROOT_SIGNATURE_FLAG_ALLOW_INPUT_ASSEMBLER_INPUT_LAYOUT |
        D3D12_ROOT_SIGNATURE_FLAG_DENY_HULL_SHADER_ROOT_ACCESS |
        D3D12_ROOT_SIGNATURE_FLAG_DENY_DOMAIN_SHADER_ROOT_ACCESS |
        D3D12_ROOT_SIGNATURE_FLAG_DENY_GEOMETRY_SHADER_ROOT_ACCESS;
    m_carRootSignature = CreateRootSignature(
        m_device.Get(),
        carRootDescription,
        "Create scene root signature");

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

    D3D12_DEPTH_STENCIL_DESC backgroundDepth{};
    backgroundDepth.DepthEnable = FALSE;
    backgroundDepth.DepthWriteMask = D3D12_DEPTH_WRITE_MASK_ZERO;
    backgroundDepth.DepthFunc = D3D12_COMPARISON_FUNC_ALWAYS;
    backgroundDepth.StencilEnable = FALSE;
    backgroundDepth.StencilReadMask = D3D12_DEFAULT_STENCIL_READ_MASK;
    backgroundDepth.StencilWriteMask = D3D12_DEFAULT_STENCIL_WRITE_MASK;

    D3D12_GRAPHICS_PIPELINE_STATE_DESC backgroundPipeline{};
    backgroundPipeline.pRootSignature = m_backgroundRootSignature.Get();
    backgroundPipeline.VS = {g_backgroundVertexShader, sizeof(g_backgroundVertexShader)};
    backgroundPipeline.PS = {g_backgroundPixelShader, sizeof(g_backgroundPixelShader)};
    backgroundPipeline.BlendState = blend;
    backgroundPipeline.SampleMask = UINT_MAX;
    backgroundPipeline.RasterizerState = rasterizer;
    backgroundPipeline.DepthStencilState = backgroundDepth;
    backgroundPipeline.InputLayout = {nullptr, 0};
    backgroundPipeline.IBStripCutValue = D3D12_INDEX_BUFFER_STRIP_CUT_VALUE_DISABLED;
    backgroundPipeline.PrimitiveTopologyType = D3D12_PRIMITIVE_TOPOLOGY_TYPE_TRIANGLE;
    backgroundPipeline.NumRenderTargets = 1;
    backgroundPipeline.RTVFormats[0] = RenderTargetFormat;
    backgroundPipeline.DSVFormat = DepthBufferFormat;
    backgroundPipeline.SampleDesc = {1, 0};
    Check(
        m_device->CreateGraphicsPipelineState(&backgroundPipeline, IID_PPV_ARGS(&m_backgroundPipelineState)),
        "Create background pipeline state");

    const std::array carInputElements = {
        D3D12_INPUT_ELEMENT_DESC{
            "POSITION", 0, DXGI_FORMAT_R32G32B32_FLOAT, 0, 0,
            D3D12_INPUT_CLASSIFICATION_PER_VERTEX_DATA, 0},
        D3D12_INPUT_ELEMENT_DESC{
            "NORMAL", 0, DXGI_FORMAT_R32G32B32_FLOAT, 0, 12,
            D3D12_INPUT_CLASSIFICATION_PER_VERTEX_DATA, 0},
        D3D12_INPUT_ELEMENT_DESC{
            "MATERIAL", 0, DXGI_FORMAT_R32_UINT, 0, 24,
            D3D12_INPUT_CLASSIFICATION_PER_VERTEX_DATA, 0},
    };

    D3D12_DEPTH_STENCIL_DESC carDepth{};
    carDepth.DepthEnable = TRUE;
    carDepth.DepthWriteMask = D3D12_DEPTH_WRITE_MASK_ALL;
    carDepth.DepthFunc = D3D12_COMPARISON_FUNC_LESS;
    carDepth.StencilEnable = FALSE;
    carDepth.StencilReadMask = D3D12_DEFAULT_STENCIL_READ_MASK;
    carDepth.StencilWriteMask = D3D12_DEFAULT_STENCIL_WRITE_MASK;

    D3D12_GRAPHICS_PIPELINE_STATE_DESC carPipeline{};
    carPipeline.pRootSignature = m_carRootSignature.Get();
    carPipeline.VS = {g_simplePaintVertexShader, sizeof(g_simplePaintVertexShader)};
    carPipeline.PS = {g_simplePaintPixelShader, sizeof(g_simplePaintPixelShader)};
    carPipeline.BlendState = blend;
    carPipeline.SampleMask = UINT_MAX;
    carPipeline.RasterizerState = rasterizer;
    carPipeline.DepthStencilState = carDepth;
    carPipeline.InputLayout = {
        carInputElements.data(),
        static_cast<std::uint32_t>(carInputElements.size())};
    carPipeline.IBStripCutValue = D3D12_INDEX_BUFFER_STRIP_CUT_VALUE_DISABLED;
    carPipeline.PrimitiveTopologyType = D3D12_PRIMITIVE_TOPOLOGY_TYPE_TRIANGLE;
    carPipeline.NumRenderTargets = 1;
    carPipeline.RTVFormats[0] = RenderTargetFormat;
    carPipeline.DSVFormat = DepthBufferFormat;
    carPipeline.SampleDesc = {1, 0};
    Check(
        m_device->CreateGraphicsPipelineState(&carPipeline, IID_PPV_ARGS(&m_carPipelineState)),
        "Create scene pipeline state");
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
            m_carPipelineState.Get(),
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
    D3D12_RENDER_TARGET_VIEW_DESC rtvDescription{};
    rtvDescription.Format = RenderTargetFormat;
    rtvDescription.ViewDimension = D3D12_RTV_DIMENSION_TEXTURE2D;
    rtvDescription.Texture2D.MipSlice = 0;
    rtvDescription.Texture2D.PlaneSlice = 0;

    D3D12_CPU_DESCRIPTOR_HANDLE rtvHandle = m_rtvHeap->GetCPUDescriptorHandleForHeapStart();
    for (std::uint32_t frameIndex = 0; frameIndex < FrameCount; ++frameIndex)
    {
        Check(m_swapChain->GetBuffer(frameIndex, IID_PPV_ARGS(&m_renderTargets[frameIndex])), "Get swap-chain buffer");
        m_device->CreateRenderTargetView(m_renderTargets[frameIndex].Get(), &rtvDescription, rtvHandle);
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
    const float sceneWidth = std::min(
        static_cast<float>(m_width),
        static_cast<float>(m_height) * BackgroundAspectRatio);
    m_sceneViewport = {
        (static_cast<float>(m_width) - sceneWidth) * 0.5f,
        0.0f,
        sceneWidth,
        static_cast<float>(m_height),
        0.0f,
        1.0f,
    };
    m_scissorRect = {0, 0, static_cast<LONG>(m_width), static_cast<LONG>(m_height)};
}

void Renderer::CreateStaticResources()
{
    static_assert(sizeof(GeneratedCarMesh::Vertex) == 28);
    static_assert(GeneratedCarMesh::MaterialCount == CarMaterialCount);

    const std::filesystem::path backgroundPath =
        ModuleDirectory() / L"assets" / L"SceneBackground.png";
    const DecodedImage background = DecodeRgbaImage(backgroundPath);
    if (background.width != 5120 || background.height != 1440)
    {
        throw std::runtime_error(std::format(
            "SceneBackground.png must be 5120x1440; found {}x{}.",
            background.width,
            background.height));
    }

    const UVSphere::Mesh sphere = UVSphere::Generate(
        m_sphereUResolution, m_sphereVResolution, CarMaterialCount);
    std::vector<GeneratedCarMesh::Vertex> vertices(
        std::begin(GeneratedCarMesh::Vertices), std::end(GeneratedCarMesh::Vertices));
    std::vector<std::uint32_t> indices(
        std::begin(GeneratedCarMesh::Indices), std::end(GeneratedCarMesh::Indices));
    m_carIndexCount = static_cast<std::uint32_t>(indices.size());
    const auto sphereVertexOffset = static_cast<std::uint32_t>(vertices.size());
    vertices.insert(vertices.end(), sphere.vertices.begin(), sphere.vertices.end());
    indices.reserve(indices.size() + sphere.indices.size());
    for (const std::uint32_t index : sphere.indices)
    {
        indices.push_back(sphereVertexOffset + index);
    }
    const std::uint64_t vertexBytes = vertices.size() * sizeof(vertices[0]);
    const std::uint64_t indexBytes = indices.size() * sizeof(indices[0]);
    const D3D12_HEAP_PROPERTIES defaultHeap = HeapProperties(D3D12_HEAP_TYPE_DEFAULT);
    const D3D12_HEAP_PROPERTIES uploadHeap = HeapProperties(D3D12_HEAP_TYPE_UPLOAD);

    const D3D12_RESOURCE_DESC vertexDescription = BufferDescription(vertexBytes);
    const D3D12_RESOURCE_DESC indexDescription = BufferDescription(indexBytes);
    Check(
        m_device->CreateCommittedResource(
            &defaultHeap,
            D3D12_HEAP_FLAG_NONE,
            &vertexDescription,
            D3D12_RESOURCE_STATE_COPY_DEST,
            nullptr,
            IID_PPV_ARGS(&m_sceneMesh.vertexBuffer)),
        "Create scene vertex buffer");
    Check(
        m_device->CreateCommittedResource(
            &defaultHeap,
            D3D12_HEAP_FLAG_NONE,
            &indexDescription,
            D3D12_RESOURCE_STATE_COPY_DEST,
            nullptr,
            IID_PPV_ARGS(&m_sceneMesh.indexBuffer)),
        "Create scene index buffer");

    ComPtr<ID3D12Resource> vertexUpload;
    ComPtr<ID3D12Resource> indexUpload;
    Check(
        m_device->CreateCommittedResource(
            &uploadHeap,
            D3D12_HEAP_FLAG_NONE,
            &vertexDescription,
            D3D12_RESOURCE_STATE_GENERIC_READ,
            nullptr,
            IID_PPV_ARGS(&vertexUpload)),
        "Create scene vertex upload buffer");
    Check(
        m_device->CreateCommittedResource(
            &uploadHeap,
            D3D12_HEAP_FLAG_NONE,
            &indexDescription,
            D3D12_RESOURCE_STATE_GENERIC_READ,
            nullptr,
            IID_PPV_ARGS(&indexUpload)),
        "Create scene index upload buffer");

    D3D12_RANGE noCpuReads{0, 0};
    void* mappedData = nullptr;
    Check(vertexUpload->Map(0, &noCpuReads, &mappedData), "Map scene vertex upload buffer");
    std::memcpy(mappedData, vertices.data(), static_cast<std::size_t>(vertexBytes));
    vertexUpload->Unmap(0, nullptr);
    Check(indexUpload->Map(0, &noCpuReads, &mappedData), "Map scene index upload buffer");
    std::memcpy(mappedData, indices.data(), static_cast<std::size_t>(indexBytes));
    indexUpload->Unmap(0, nullptr);

    const D3D12_RESOURCE_DESC textureDescription =
        TextureDescription(background.width, background.height);
    Check(
        m_device->CreateCommittedResource(
            &defaultHeap,
            D3D12_HEAP_FLAG_NONE,
            &textureDescription,
            D3D12_RESOURCE_STATE_COPY_DEST,
            nullptr,
            IID_PPV_ARGS(&m_backgroundTexture)),
        "Create background texture");

    D3D12_PLACED_SUBRESOURCE_FOOTPRINT textureFootprint{};
    std::uint32_t rowCount = 0;
    std::uint64_t rowSize = 0;
    std::uint64_t textureUploadBytes = 0;
    m_device->GetCopyableFootprints(
        &textureDescription,
        0,
        1,
        0,
        &textureFootprint,
        &rowCount,
        &rowSize,
        &textureUploadBytes);
    const std::uint64_t expectedRowSize = static_cast<std::uint64_t>(background.width) * 4u;
    if (rowCount != background.height || rowSize != expectedRowSize)
    {
        throw std::runtime_error("Unexpected background texture copy footprint.");
    }

    const D3D12_RESOURCE_DESC textureUploadDescription = BufferDescription(textureUploadBytes);
    ComPtr<ID3D12Resource> textureUpload;
    Check(
        m_device->CreateCommittedResource(
            &uploadHeap,
            D3D12_HEAP_FLAG_NONE,
            &textureUploadDescription,
            D3D12_RESOURCE_STATE_GENERIC_READ,
            nullptr,
            IID_PPV_ARGS(&textureUpload)),
        "Create background texture upload buffer");

    Check(textureUpload->Map(0, &noCpuReads, &mappedData), "Map background texture upload buffer");
    auto* destination = static_cast<std::byte*>(mappedData) + textureFootprint.Offset;
    const std::size_t sourceRowBytes = static_cast<std::size_t>(background.width) * 4u;
    for (std::uint32_t row = 0; row < rowCount; ++row)
    {
        std::memcpy(
            destination + static_cast<std::size_t>(row) * textureFootprint.Footprint.RowPitch,
            background.pixels.data() + static_cast<std::size_t>(row) * sourceRowBytes,
            sourceRowBytes);
    }
    textureUpload->Unmap(0, nullptr);

    Check(m_commandAllocators[0]->Reset(), "Reset upload command allocator");
    Check(m_commandList->Reset(m_commandAllocators[0].Get(), nullptr), "Reset upload command list");
    m_commandList->CopyBufferRegion(m_sceneMesh.vertexBuffer.Get(), 0, vertexUpload.Get(), 0, vertexBytes);
    m_commandList->CopyBufferRegion(m_sceneMesh.indexBuffer.Get(), 0, indexUpload.Get(), 0, indexBytes);

    D3D12_TEXTURE_COPY_LOCATION textureDestination{};
    textureDestination.pResource = m_backgroundTexture.Get();
    textureDestination.Type = D3D12_TEXTURE_COPY_TYPE_SUBRESOURCE_INDEX;
    textureDestination.SubresourceIndex = 0;
    D3D12_TEXTURE_COPY_LOCATION textureSource{};
    textureSource.pResource = textureUpload.Get();
    textureSource.Type = D3D12_TEXTURE_COPY_TYPE_PLACED_FOOTPRINT;
    textureSource.PlacedFootprint = textureFootprint;
    m_commandList->CopyTextureRegion(&textureDestination, 0, 0, 0, &textureSource, nullptr);

    const std::array uploadBarriers = {
        TransitionBarrier(
            m_sceneMesh.vertexBuffer.Get(),
            D3D12_RESOURCE_STATE_COPY_DEST,
            D3D12_RESOURCE_STATE_VERTEX_AND_CONSTANT_BUFFER),
        TransitionBarrier(
            m_sceneMesh.indexBuffer.Get(),
            D3D12_RESOURCE_STATE_COPY_DEST,
            D3D12_RESOURCE_STATE_INDEX_BUFFER),
        TransitionBarrier(
            m_backgroundTexture.Get(),
            D3D12_RESOURCE_STATE_COPY_DEST,
            D3D12_RESOURCE_STATE_PIXEL_SHADER_RESOURCE),
    };
    m_commandList->ResourceBarrier(
        static_cast<std::uint32_t>(uploadBarriers.size()),
        uploadBarriers.data());
    Check(m_commandList->Close(), "Close upload command list");

    ID3D12CommandList* commandLists[] = {m_commandList.Get()};
    m_commandQueue->ExecuteCommandLists(1, commandLists);
    WaitForGpu();

    m_sceneMesh.vertexView.BufferLocation = m_sceneMesh.vertexBuffer->GetGPUVirtualAddress();
    m_sceneMesh.vertexView.SizeInBytes = static_cast<std::uint32_t>(vertexBytes);
    m_sceneMesh.vertexView.StrideInBytes = sizeof(GeneratedCarMesh::Vertex);
    m_sceneMesh.indexView.BufferLocation = m_sceneMesh.indexBuffer->GetGPUVirtualAddress();
    m_sceneMesh.indexView.SizeInBytes = static_cast<std::uint32_t>(indexBytes);
    m_sceneMesh.indexView.Format = DXGI_FORMAT_R32_UINT;
    m_sceneMesh.indexCount = static_cast<std::uint32_t>(indices.size());

    D3D12_SHADER_RESOURCE_VIEW_DESC srvDescription{};
    srvDescription.Format = DXGI_FORMAT_R8G8B8A8_UNORM_SRGB;
    srvDescription.ViewDimension = D3D12_SRV_DIMENSION_TEXTURE2D;
    srvDescription.Shader4ComponentMapping = D3D12_DEFAULT_SHADER_4_COMPONENT_MAPPING;
    srvDescription.Texture2D.MostDetailedMip = 0;
    srvDescription.Texture2D.MipLevels = 1;
    srvDescription.Texture2D.PlaneSlice = 0;
    srvDescription.Texture2D.ResourceMinLODClamp = 0.0f;
    m_device->CreateShaderResourceView(
        m_backgroundTexture.Get(),
        &srvDescription,
        m_srvHeap->GetCPUDescriptorHandleForHeapStart());

    SetDebugName(m_sceneMesh.vertexBuffer.Get(), L"Scene vertex buffer");
    SetDebugName(m_sceneMesh.indexBuffer.Get(), L"Scene index buffer");
    SetDebugName(m_backgroundTexture.Get(), L"Flattened scene background");
}

void Renderer::CreateConstantBuffer()
{
    const std::uint64_t bufferSize = sizeof(CarConstants) * ObjectsPerFrame * FrameCount;
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
        "Create scene constant buffer");

    D3D12_RANGE noCpuReads{0, 0};
    void* mappedData = nullptr;
    Check(m_constantBuffer->Map(0, &noCpuReads, &mappedData), "Map scene constant buffer");
    m_mappedConstants = static_cast<std::byte*>(mappedData);
}

void Renderer::LoadPaintSettings()
{
    PaintSettings settings;
    settings.baseColorsSrgb = {
        XMFLOAT3{0.678429127f, 0.678431321f, 0.678431321f},
        XMFLOAT3{0.0f, 0.436627067f, 1.0f},
        XMFLOAT3{0.506386429f, 0.756053146f, 1.0f},
        XMFLOAT3{1.0f, 0.815686771f, 0.0f},
        XMFLOAT3{0.345097446f, 0.345097446f, 0.345097446f},
        XMFLOAT3{0.345097446f, 0.345097446f, 0.345097446f},
    };

    const std::filesystem::path settingsPath = ModuleDirectory() / L"assets" / L"CarPaint.ini";
    std::ifstream input(settingsPath);
    if (!input)
    {
        throw std::runtime_error(std::format(
            "Car paint settings were not found at {}.",
            settingsPath.string()));
    }

    std::string section;
    std::string line;
    std::size_t lineNumber = 0;
    while (std::getline(input, line))
    {
        ++lineNumber;
        const std::size_t comment = line.find_first_of(";#");
        const std::string_view content = Trim(std::string_view(line).substr(0, comment));
        if (content.empty())
        {
            continue;
        }

        if (content.front() == '[' && content.back() == ']')
        {
            section = std::string(Trim(content.substr(1, content.size() - 2)));
            if (section != "SimplePaint" && section != "BaseColors" && section != "Sphere")
            {
                throw std::runtime_error(std::format(
                    "Unknown CarPaint.ini section [{}] on line {}.",
                    section,
                    lineNumber));
            }
            continue;
        }

        const std::size_t equals = content.find('=');
        if (equals == std::string_view::npos)
        {
            throw std::runtime_error(std::format(
                "Expected key = value on CarPaint.ini line {}.",
                lineNumber));
        }
        const std::string key(Trim(content.substr(0, equals)));
        const std::string_view value = Trim(content.substr(equals + 1));
        const std::string qualifiedKey = section + "." + key;

        if (qualifiedKey == "SimplePaint.Brightness")
        {
            settings.brightness = ParseFloat(value, lineNumber);
        }
        else if (qualifiedKey == "SimplePaint.Shift")
        {
            settings.shift = ParseFloat(value, lineNumber);
        }
        else if (qualifiedKey == "SimplePaint.RotationDegrees")
        {
            settings.rotationDegrees = ParseFloat(value, lineNumber);
        }
        else if (qualifiedKey == "SimplePaint.DarkPoint")
        {
            settings.darkPoint = ParseFloat(value, lineNumber);
        }
        else if (qualifiedKey == "SimplePaint.LightPoint")
        {
            settings.lightPoint = ParseFloat(value, lineNumber);
        }
        else if (qualifiedKey == "SimplePaint.FacingCutoff")
        {
            settings.facingCutoff = ParseFloat(value, lineNumber);
        }
        else if (qualifiedKey == "Sphere.UResolution" || qualifiedKey == "Sphere.VResolution")
        {
            std::uint32_t resolution = 0;
            const auto [end, error] = std::from_chars(value.data(), value.data() + value.size(), resolution);
            const bool isU = qualifiedKey == "Sphere.UResolution";
            const std::uint32_t minimum = isU ? 3u : 2u;
            if (error != std::errc{} || end != value.data() + value.size() ||
                resolution < minimum || resolution > 512)
            {
                throw std::runtime_error(std::format(
                    "{} must be an integer in [{}, 512] on CarPaint.ini line {}.",
                    qualifiedKey, minimum, lineNumber));
            }
            (isU ? m_sphereUResolution : m_sphereVResolution) = resolution;
        }
        else
        {
            constexpr std::array<std::string_view, PaintMaterialCount> colorKeys = {
                "BaseColors.Axles",
                "BaseColors.Body",
                "BaseColors.Cabin",
                "BaseColors.Headlights",
                "BaseColors.Wheels",
                "BaseColors.Sphere",
            };
            const auto colorKey = std::find(colorKeys.begin(), colorKeys.end(), qualifiedKey);
            if (colorKey == colorKeys.end())
            {
                throw std::runtime_error(std::format(
                    "Unknown CarPaint.ini key '{}' on line {}.",
                    qualifiedKey,
                    lineNumber));
            }
            settings.baseColorsSrgb[static_cast<std::size_t>(colorKey - colorKeys.begin())] =
                ParseColor(value, lineNumber);
        }
    }

    RequireUnitRange(settings.brightness, "Brightness");
    RequireUnitRange(settings.shift, "Shift");
    RequireUnitRange(settings.darkPoint, "DarkPoint");
    RequireUnitRange(settings.lightPoint, "LightPoint");
    RequireUnitRange(settings.facingCutoff, "FacingCutoff");
    for (const XMFLOAT3& color : settings.baseColorsSrgb)
    {
        RequireUnitRange(color.x, "Base color red channel");
        RequireUnitRange(color.y, "Base color green channel");
        RequireUnitRange(color.z, "Base color blue channel");
    }

    constexpr float epsilon = 1.0e-5f;
    const float safeBrightness = std::clamp(settings.brightness, epsilon, 1.0f - epsilon);
    const float safeShift = std::min(settings.shift, 1.0f - epsilon);
    const float rotationRadians = -DirectX::XMConvertToRadians(
        std::fmod(settings.rotationDegrees, 360.0f));
    m_paintWarp = {
        std::cos(rotationRadians),
        std::sin(rotationRadians),
        safeShift,
        std::sqrt(std::max(0.0f, 1.0f - safeShift * safeShift)),
    };
    m_paintTone = {
        settings.lightPoint - settings.darkPoint,
        settings.darkPoint,
        settings.facingCutoff,
        epsilon,
    };

    const float anchor = 1.0f - safeBrightness;
    for (std::size_t materialIndex = 0; materialIndex < m_paintMaterials.size(); ++materialIndex)
    {
        const XMFLOAT3 source = settings.baseColorsSrgb[materialIndex];
        const XMFLOAT3 baseColor{
            std::clamp(SrgbToLinear(source.x), epsilon, 1.0f - epsilon),
            std::clamp(SrgbToLinear(source.y), epsilon, 1.0f - epsilon),
            std::clamp(SrgbToLinear(source.z), epsilon, 1.0f - epsilon),
        };
        PaintMaterialConstants& material = m_paintMaterials[materialIndex];
        material.k1 = {
            baseColor.x * safeBrightness,
            baseColor.y * safeBrightness,
            baseColor.z * safeBrightness,
            0.0f,
        };
        material.k2 = {
            baseColor.x - anchor,
            baseColor.y - anchor,
            baseColor.z - anchor,
            0.0f,
        };
        material.k3 = {
            anchor * (1.0f - baseColor.x),
            anchor * (1.0f - baseColor.y),
            anchor * (1.0f - baseColor.z),
            0.0f,
        };
    }
}

void Renderer::UpdateCamera()
{
    const XMVECTOR eye = DirectX::XMVectorSet(1.6889755f, 3.6863865f, 2.9253915f, 1.0f);
    const XMVECTOR target = DirectX::XMVectorZero();
    const XMVECTOR up = DirectX::XMVectorSet(-0.36863866f, 0.6755902f, -0.63850087f, 0.0f);
    const XMMATRIX view = DirectX::XMMatrixLookAtRH(eye, target, up);
    const float aspectRatio = std::min(
        static_cast<float>(m_width) / static_cast<float>(m_height),
        BackgroundAspectRatio);
    const XMMATRIX projection = DirectX::XMMatrixOrthographicRH(5.0f * aspectRatio, 5.0f, 1.0f, 20.0f);
    DirectX::XMStoreFloat4x4(&m_view, view);
    DirectX::XMStoreFloat4x4(&m_projection, projection);
}

Renderer::AnimationState Renderer::CurrentAnimationState() const
{
    constexpr double movementSpeed = 8.0;
    constexpr double travelDistance = 7.0;
    constexpr double fullCycleDistance = travelDistance * 4.0;
    constexpr double rotationSpeed = std::numbers::pi / 2.0;

    const double elapsed = std::chrono::duration<double>(
        std::chrono::steady_clock::now() - m_animationStart).count();
    const double phase = std::fmod(elapsed * movementSpeed + travelDistance, fullCycleDistance);

    AnimationState state;
    state.position = static_cast<float>(travelDistance - std::abs(phase - travelDistance * 2.0));
    state.rotation = static_cast<float>(std::fmod(elapsed * rotationSpeed, std::numbers::pi * 2.0));
    return state;
}

void Renderer::WriteObjectConstants(
    const std::uint32_t frameIndex,
    const std::uint32_t objectIndex,
    DirectX::FXMMATRIX world)
{
    const XMMATRIX view = DirectX::XMLoadFloat4x4(&m_view);
    const XMMATRIX projection = DirectX::XMLoadFloat4x4(&m_projection);
    const XMMATRIX worldView = world * view;

    CarConstants constants{};
    DirectX::XMStoreFloat4x4(&constants.worldViewProjection, worldView * projection);
    DirectX::XMStoreFloat4x4(&constants.worldView, worldView);
    constants.paintWarp = m_paintWarp;
    constants.paintTone = m_paintTone;
    constants.paintMaterials = m_paintMaterials;
    std::memcpy(
        m_mappedConstants + (static_cast<std::size_t>(frameIndex) * ObjectsPerFrame + objectIndex) *
            sizeof(CarConstants),
        &constants,
        sizeof(constants));
}

void Renderer::DrawBackground()
{
    m_commandList->RSSetViewports(1, &m_viewport);
    m_commandList->SetPipelineState(m_backgroundPipelineState.Get());
    m_commandList->SetGraphicsRootSignature(m_backgroundRootSignature.Get());
    ID3D12DescriptorHeap* descriptorHeaps[] = {m_srvHeap.Get()};
    m_commandList->SetDescriptorHeaps(1, descriptorHeaps);
    m_commandList->SetGraphicsRootDescriptorTable(
        0,
        m_srvHeap->GetGPUDescriptorHandleForHeapStart());

    const float windowAspect = static_cast<float>(m_width) / static_cast<float>(m_height);
    const float horizontalUvScale = windowAspect / BackgroundAspectRatio;
    m_commandList->SetGraphicsRoot32BitConstants(1, 1, &horizontalUvScale, 0);
    m_commandList->IASetPrimitiveTopology(D3D_PRIMITIVE_TOPOLOGY_TRIANGLELIST);
    m_commandList->DrawInstanced(3, 1, 0, 0);
}

void Renderer::DrawObjects(const std::uint32_t frameIndex)
{
    m_commandList->RSSetViewports(1, &m_sceneViewport);
    m_commandList->SetPipelineState(m_carPipelineState.Get());
    m_commandList->SetGraphicsRootSignature(m_carRootSignature.Get());
    const D3D12_GPU_VIRTUAL_ADDRESS constantsAddress =
        m_constantBuffer->GetGPUVirtualAddress() +
        static_cast<std::uint64_t>(frameIndex) * ObjectsPerFrame * sizeof(CarConstants);
    m_commandList->SetGraphicsRootConstantBufferView(0, constantsAddress);
    m_commandList->IASetPrimitiveTopology(D3D_PRIMITIVE_TOPOLOGY_TRIANGLELIST);
    m_commandList->IASetVertexBuffers(0, 1, &m_sceneMesh.vertexView);
    m_commandList->IASetIndexBuffer(&m_sceneMesh.indexView);
    m_commandList->DrawIndexedInstanced(m_carIndexCount, 1, 0, 0, 0);
    m_commandList->SetGraphicsRootConstantBufferView(0, constantsAddress + sizeof(CarConstants));
    m_commandList->DrawIndexedInstanced(
        m_sceneMesh.indexCount - m_carIndexCount, 1, m_carIndexCount, 0, 0);
}

void Renderer::Render()
{
    if (!m_initialized || m_width == 0 || m_height == 0)
    {
        return;
    }

    const std::uint32_t frameIndex = m_swapChain->GetCurrentBackBufferIndex();
    WaitForFrame(frameIndex);

    const AnimationState animation = CurrentAnimationState();
    constexpr float carScale = 0.25f;
    const XMMATRIX carWorld =
        DirectX::XMMatrixScaling(carScale, carScale, carScale) *
        DirectX::XMMatrixRotationY(animation.rotation) *
        DirectX::XMMatrixTranslation(animation.position, 0.0f, 0.0f);
    WriteObjectConstants(frameIndex, 0, carWorld);
    // Grounded beside the baked cube at (0, 0.5, -1.5): screen-right and
    // below it, with clearance from the car's entire rotating sweep at Z=0.
    constexpr float sphereRadius = 0.4f;
    const XMMATRIX sphereWorld =
        DirectX::XMMatrixScaling(sphereRadius, sphereRadius, sphereRadius) *
        DirectX::XMMatrixTranslation(1.5f, sphereRadius, -1.5f);
    WriteObjectConstants(frameIndex, 1, sphereWorld);

    Check(m_commandAllocators[frameIndex]->Reset(), "Reset command allocator");
    Check(
        m_commandList->Reset(
            m_commandAllocators[frameIndex].Get(),
            m_backgroundPipelineState.Get()),
        "Reset command list");
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

    DrawBackground();
    m_commandList->ClearDepthStencilView(
        dsvHandle,
        D3D12_CLEAR_FLAG_DEPTH,
        1.0f,
        0,
        0,
        nullptr);
    DrawObjects(frameIndex);

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
            SwapChainFormat,
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
