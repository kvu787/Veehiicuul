#include "Renderer.h"
#include "UVSphere.h"
#include "SimplePaint/Geometry.h"

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
    return {
        .Dimension = D3D12_RESOURCE_DIMENSION_BUFFER,
        .Alignment = 0,
        .Width = size,
        .Height = 1,
        .DepthOrArraySize = 1,
        .MipLevels = 1,
        .Format = DXGI_FORMAT_UNKNOWN,
        .SampleDesc = {.Count = 1, .Quality = 0},
        .Layout = D3D12_TEXTURE_LAYOUT_ROW_MAJOR,
        .Flags = D3D12_RESOURCE_FLAG_NONE,
    };
}

D3D12_RESOURCE_DESC TextureDescription(const std::uint32_t width, const std::uint32_t height)
{
    return {
        .Dimension = D3D12_RESOURCE_DIMENSION_TEXTURE2D,
        .Alignment = 0,
        .Width = width,
        .Height = height,
        .DepthOrArraySize = 1,
        .MipLevels = 1,
        .Format = DXGI_FORMAT_R8G8B8A8_TYPELESS,
        .SampleDesc = {.Count = 1, .Quality = 0},
        .Layout = D3D12_TEXTURE_LAYOUT_UNKNOWN,
        .Flags = D3D12_RESOURCE_FLAG_NONE,
    };
}

D3D12_HEAP_PROPERTIES HeapProperties(const D3D12_HEAP_TYPE type)
{
    return {
        .Type = type,
        .CPUPageProperty = D3D12_CPU_PAGE_PROPERTY_UNKNOWN,
        .MemoryPoolPreference = D3D12_MEMORY_POOL_UNKNOWN,
        .CreationNodeMask = 1,
        .VisibleNodeMask = 1,
    };
}

D3D12_RESOURCE_BARRIER TransitionBarrier(
    ID3D12Resource* resource,
    const D3D12_RESOURCE_STATES before,
    const D3D12_RESOURCE_STATES after)
{
    return {
        .Type = D3D12_RESOURCE_BARRIER_TYPE_TRANSITION,
        .Flags = D3D12_RESOURCE_BARRIER_FLAG_NONE,
        .Transition = {
            .pResource = resource,
            .Subresource = D3D12_RESOURCE_BARRIER_ALL_SUBRESOURCES,
            .StateBefore = before,
            .StateAfter = after,
        },
    };
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

    DecodedImage image{
        .width = width,
        .height = height,
    };
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

double ParseFloat(const std::string_view text, const std::size_t lineNumber)
{
    const std::string_view trimmed = Trim(text);
    double value = 0.0;
    const auto [end, error] = std::from_chars(
        trimmed.data(),
        trimmed.data() + trimmed.size(),
        value);
    if (error != std::errc{} || end != trimmed.data() + trimmed.size() || !std::isfinite(value))
    {
        throw std::runtime_error(std::format(
            "Invalid finite floating-point value on Settings.ini line {}.",
            lineNumber));
    }
    return value;
}

std::array<double, 3> ParseColor(const std::string_view text, const std::size_t lineNumber)
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
            "Expected an R, G, B triple on Settings.ini line {}.",
            lineNumber));
    }

    return {
        ParseFloat(text.substr(0, firstComma), lineNumber),
        ParseFloat(text.substr(firstComma + 1, secondComma - firstComma - 1), lineNumber),
        ParseFloat(text.substr(secondComma + 1), lineNumber),
    };
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

    D3D12_FEATURE_DATA_SHADER_MODEL shaderModelSupport{.HighestShaderModel = D3D_SHADER_MODEL_6_0};
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

    D3D12_COMMAND_QUEUE_DESC queueDescription{
        .Type = D3D12_COMMAND_LIST_TYPE_DIRECT,
        .Priority = D3D12_COMMAND_QUEUE_PRIORITY_NORMAL,
        .Flags = D3D12_COMMAND_QUEUE_FLAG_NONE,
        .NodeMask = 0,
    };
    Check(m_device->CreateCommandQueue(&queueDescription, IID_PPV_ARGS(&m_commandQueue)), "CreateCommandQueue");
    SetDebugName(m_commandQueue.Get(), L"Direct Command Queue");
}

void Renderer::CreateSwapChain()
{
    DXGI_SWAP_CHAIN_DESC1 description{
        .Width = m_width,
        .Height = m_height,
        .Format = SwapChainFormat,
        .Stereo = FALSE,
        .SampleDesc = {.Count = 1, .Quality = 0},
        .BufferUsage = DXGI_USAGE_RENDER_TARGET_OUTPUT,
        .BufferCount = FrameCount,
        .Scaling = DXGI_SCALING_STRETCH,
        .SwapEffect = DXGI_SWAP_EFFECT_FLIP_DISCARD,
        .AlphaMode = DXGI_ALPHA_MODE_IGNORE,
        .Flags = m_tearingSupported ? DXGI_SWAP_CHAIN_FLAG_ALLOW_TEARING : 0u,
    };

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
    D3D12_DESCRIPTOR_HEAP_DESC rtvDescription{
        .Type = D3D12_DESCRIPTOR_HEAP_TYPE_RTV,
        .NumDescriptors = FrameCount,
        .Flags = D3D12_DESCRIPTOR_HEAP_FLAG_NONE,
    };
    Check(m_device->CreateDescriptorHeap(&rtvDescription, IID_PPV_ARGS(&m_rtvHeap)), "Create RTV descriptor heap");
    m_rtvDescriptorSize = m_device->GetDescriptorHandleIncrementSize(D3D12_DESCRIPTOR_HEAP_TYPE_RTV);

    D3D12_DESCRIPTOR_HEAP_DESC dsvDescription{
        .Type = D3D12_DESCRIPTOR_HEAP_TYPE_DSV,
        .NumDescriptors = 1,
        .Flags = D3D12_DESCRIPTOR_HEAP_FLAG_NONE,
    };
    Check(m_device->CreateDescriptorHeap(&dsvDescription, IID_PPV_ARGS(&m_dsvHeap)), "Create DSV descriptor heap");

    D3D12_DESCRIPTOR_HEAP_DESC srvDescription{
        .Type = D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV,
        .NumDescriptors = 1,
        .Flags = D3D12_DESCRIPTOR_HEAP_FLAG_SHADER_VISIBLE,
    };
    Check(m_device->CreateDescriptorHeap(&srvDescription, IID_PPV_ARGS(&m_srvHeap)), "Create SRV descriptor heap");
}

void Renderer::CreatePipelines()
{
    D3D12_DESCRIPTOR_RANGE backgroundRange{
        .RangeType = D3D12_DESCRIPTOR_RANGE_TYPE_SRV,
        .NumDescriptors = 1,
        .BaseShaderRegister = 0,
        .RegisterSpace = 0,
        .OffsetInDescriptorsFromTableStart = D3D12_DESCRIPTOR_RANGE_OFFSET_APPEND,
    };

    const std::array<D3D12_ROOT_PARAMETER, 2> backgroundParameters = {{
        {
            .ParameterType = D3D12_ROOT_PARAMETER_TYPE_DESCRIPTOR_TABLE,
            .DescriptorTable = {
                .NumDescriptorRanges = 1,
                .pDescriptorRanges = &backgroundRange,
            },
            .ShaderVisibility = D3D12_SHADER_VISIBILITY_PIXEL,
        },
        {
            .ParameterType = D3D12_ROOT_PARAMETER_TYPE_32BIT_CONSTANTS,
            .Constants = {
                .ShaderRegister = 0,
                .RegisterSpace = 0,
                .Num32BitValues = 1,
            },
            .ShaderVisibility = D3D12_SHADER_VISIBILITY_VERTEX,
        },
    }};

    D3D12_STATIC_SAMPLER_DESC sampler{
        .Filter = D3D12_FILTER_MIN_MAG_MIP_LINEAR,
        .AddressU = D3D12_TEXTURE_ADDRESS_MODE_CLAMP,
        .AddressV = D3D12_TEXTURE_ADDRESS_MODE_CLAMP,
        .AddressW = D3D12_TEXTURE_ADDRESS_MODE_CLAMP,
        .MipLODBias = 0.0f,
        .MaxAnisotropy = 1,
        .ComparisonFunc = D3D12_COMPARISON_FUNC_ALWAYS,
        .BorderColor = D3D12_STATIC_BORDER_COLOR_OPAQUE_BLACK,
        .MinLOD = 0.0f,
        .MaxLOD = D3D12_FLOAT32_MAX,
        .ShaderRegister = 0,
        .RegisterSpace = 0,
        .ShaderVisibility = D3D12_SHADER_VISIBILITY_PIXEL,
    };

    D3D12_ROOT_SIGNATURE_DESC backgroundRootDescription{
        .NumParameters = static_cast<std::uint32_t>(backgroundParameters.size()),
        .pParameters = backgroundParameters.data(),
        .NumStaticSamplers = 1,
        .pStaticSamplers = &sampler,
        .Flags =
            D3D12_ROOT_SIGNATURE_FLAG_DENY_HULL_SHADER_ROOT_ACCESS |
            D3D12_ROOT_SIGNATURE_FLAG_DENY_DOMAIN_SHADER_ROOT_ACCESS |
            D3D12_ROOT_SIGNATURE_FLAG_DENY_GEOMETRY_SHADER_ROOT_ACCESS,
    };
    m_backgroundRootSignature = CreateRootSignature(
        m_device.Get(),
        backgroundRootDescription,
        "Create background root signature");

    const std::array<D3D12_ROOT_PARAMETER, 2> carParameters = {{
        {
            .ParameterType = D3D12_ROOT_PARAMETER_TYPE_CBV,
            .Descriptor = {.ShaderRegister = 0},
            .ShaderVisibility = D3D12_SHADER_VISIBILITY_VERTEX,
        },
        {
            .ParameterType = D3D12_ROOT_PARAMETER_TYPE_CBV,
            .Descriptor = {.ShaderRegister = 1},
            .ShaderVisibility = D3D12_SHADER_VISIBILITY_ALL,
        },
    }};

    D3D12_ROOT_SIGNATURE_DESC carRootDescription{
        .NumParameters = static_cast<std::uint32_t>(carParameters.size()),
        .pParameters = carParameters.data(),
        .NumStaticSamplers = 0,
        .pStaticSamplers = nullptr,
        .Flags =
            D3D12_ROOT_SIGNATURE_FLAG_ALLOW_INPUT_ASSEMBLER_INPUT_LAYOUT |
            D3D12_ROOT_SIGNATURE_FLAG_DENY_HULL_SHADER_ROOT_ACCESS |
            D3D12_ROOT_SIGNATURE_FLAG_DENY_DOMAIN_SHADER_ROOT_ACCESS |
            D3D12_ROOT_SIGNATURE_FLAG_DENY_GEOMETRY_SHADER_ROOT_ACCESS,
    };
    m_carRootSignature = CreateRootSignature(
        m_device.Get(),
        carRootDescription,
        "Create scene root signature");

    D3D12_RASTERIZER_DESC rasterizer{
        .FillMode = D3D12_FILL_MODE_SOLID,
        .CullMode = D3D12_CULL_MODE_NONE,
        .FrontCounterClockwise = FALSE,
        .DepthBias = D3D12_DEFAULT_DEPTH_BIAS,
        .DepthBiasClamp = D3D12_DEFAULT_DEPTH_BIAS_CLAMP,
        .SlopeScaledDepthBias = D3D12_DEFAULT_SLOPE_SCALED_DEPTH_BIAS,
        .DepthClipEnable = TRUE,
        .MultisampleEnable = FALSE,
        .AntialiasedLineEnable = FALSE,
        .ForcedSampleCount = 0,
        .ConservativeRaster = D3D12_CONSERVATIVE_RASTERIZATION_MODE_OFF,
    };

    D3D12_BLEND_DESC blend{
        .AlphaToCoverageEnable = FALSE,
        .IndependentBlendEnable = FALSE,
        .RenderTarget = {{
            .BlendEnable = FALSE,
            .LogicOpEnable = FALSE,
            .SrcBlend = D3D12_BLEND_ONE,
            .DestBlend = D3D12_BLEND_ZERO,
            .BlendOp = D3D12_BLEND_OP_ADD,
            .SrcBlendAlpha = D3D12_BLEND_ONE,
            .DestBlendAlpha = D3D12_BLEND_ZERO,
            .BlendOpAlpha = D3D12_BLEND_OP_ADD,
            .LogicOp = D3D12_LOGIC_OP_NOOP,
            .RenderTargetWriteMask = D3D12_COLOR_WRITE_ENABLE_ALL,
        }},
    };

    D3D12_DEPTH_STENCIL_DESC backgroundDepth{
        .DepthEnable = FALSE,
        .DepthWriteMask = D3D12_DEPTH_WRITE_MASK_ZERO,
        .DepthFunc = D3D12_COMPARISON_FUNC_ALWAYS,
        .StencilEnable = FALSE,
        .StencilReadMask = D3D12_DEFAULT_STENCIL_READ_MASK,
        .StencilWriteMask = D3D12_DEFAULT_STENCIL_WRITE_MASK,
    };

    D3D12_GRAPHICS_PIPELINE_STATE_DESC backgroundPipeline{
        .pRootSignature = m_backgroundRootSignature.Get(),
        .VS = {.pShaderBytecode = g_backgroundVertexShader, .BytecodeLength = sizeof(g_backgroundVertexShader)},
        .PS = {.pShaderBytecode = g_backgroundPixelShader, .BytecodeLength = sizeof(g_backgroundPixelShader)},
        .BlendState = blend,
        .SampleMask = UINT_MAX,
        .RasterizerState = rasterizer,
        .DepthStencilState = backgroundDepth,
        .InputLayout = {.pInputElementDescs = nullptr, .NumElements = 0},
        .IBStripCutValue = D3D12_INDEX_BUFFER_STRIP_CUT_VALUE_DISABLED,
        .PrimitiveTopologyType = D3D12_PRIMITIVE_TOPOLOGY_TYPE_TRIANGLE,
        .NumRenderTargets = 1,
        .RTVFormats = {RenderTargetFormat},
        .DSVFormat = DepthBufferFormat,
        .SampleDesc = {.Count = 1, .Quality = 0},
    };
    Check(
        m_device->CreateGraphicsPipelineState(&backgroundPipeline, IID_PPV_ARGS(&m_backgroundPipelineState)),
        "Create background pipeline state");

    const std::array carInputElements = {
        D3D12_INPUT_ELEMENT_DESC{
            .SemanticName = "POSITION",
            .SemanticIndex = 0,
            .Format = DXGI_FORMAT_R32G32B32_FLOAT,
            .InputSlot = 0,
            .AlignedByteOffset = 0,
            .InputSlotClass = D3D12_INPUT_CLASSIFICATION_PER_VERTEX_DATA,
            .InstanceDataStepRate = 0,
        },
        D3D12_INPUT_ELEMENT_DESC{
            .SemanticName = "NORMAL",
            .SemanticIndex = 0,
            .Format = DXGI_FORMAT_R32G32B32_FLOAT,
            .InputSlot = 0,
            .AlignedByteOffset = 12,
            .InputSlotClass = D3D12_INPUT_CLASSIFICATION_PER_VERTEX_DATA,
            .InstanceDataStepRate = 0,
        },
        D3D12_INPUT_ELEMENT_DESC{
            .SemanticName = "MATERIAL",
            .SemanticIndex = 0,
            .Format = DXGI_FORMAT_R32_UINT,
            .InputSlot = 0,
            .AlignedByteOffset = 24,
            .InputSlotClass = D3D12_INPUT_CLASSIFICATION_PER_VERTEX_DATA,
            .InstanceDataStepRate = 0,
        },
    };

    D3D12_DEPTH_STENCIL_DESC carDepth{
        .DepthEnable = TRUE,
        .DepthWriteMask = D3D12_DEPTH_WRITE_MASK_ALL,
        .DepthFunc = D3D12_COMPARISON_FUNC_LESS,
        .StencilEnable = FALSE,
        .StencilReadMask = D3D12_DEFAULT_STENCIL_READ_MASK,
        .StencilWriteMask = D3D12_DEFAULT_STENCIL_WRITE_MASK,
    };

    D3D12_GRAPHICS_PIPELINE_STATE_DESC carPipeline{
        .pRootSignature = m_carRootSignature.Get(),
        .VS = {.pShaderBytecode = g_simplePaintVertexShader, .BytecodeLength = sizeof(g_simplePaintVertexShader)},
        .PS = {.pShaderBytecode = g_simplePaintPixelShader, .BytecodeLength = sizeof(g_simplePaintPixelShader)},
        .BlendState = blend,
        .SampleMask = UINT_MAX,
        .RasterizerState = rasterizer,
        .DepthStencilState = carDepth,
        .InputLayout = {
            .pInputElementDescs = carInputElements.data(),
            .NumElements = static_cast<std::uint32_t>(carInputElements.size()),
        },
        .IBStripCutValue = D3D12_INDEX_BUFFER_STRIP_CUT_VALUE_DISABLED,
        .PrimitiveTopologyType = D3D12_PRIMITIVE_TOPOLOGY_TYPE_TRIANGLE,
        .NumRenderTargets = 1,
        .RTVFormats = {RenderTargetFormat},
        .DSVFormat = DepthBufferFormat,
        .SampleDesc = {.Count = 1, .Quality = 0},
    };
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
    D3D12_RENDER_TARGET_VIEW_DESC rtvDescription{
        .Format = RenderTargetFormat,
        .ViewDimension = D3D12_RTV_DIMENSION_TEXTURE2D,
        .Texture2D = {
            .MipSlice = 0,
            .PlaneSlice = 0,
        },
    };

    D3D12_CPU_DESCRIPTOR_HANDLE rtvHandle = m_rtvHeap->GetCPUDescriptorHandleForHeapStart();
    for (std::uint32_t frameIndex = 0; frameIndex < FrameCount; ++frameIndex)
    {
        Check(m_swapChain->GetBuffer(frameIndex, IID_PPV_ARGS(&m_renderTargets[frameIndex])), "Get swap-chain buffer");
        m_device->CreateRenderTargetView(m_renderTargets[frameIndex].Get(), &rtvDescription, rtvHandle);
        rtvHandle.ptr += m_rtvDescriptorSize;
    }

    D3D12_RESOURCE_DESC depthDescription{
        .Dimension = D3D12_RESOURCE_DIMENSION_TEXTURE2D,
        .Alignment = 0,
        .Width = m_width,
        .Height = m_height,
        .DepthOrArraySize = 1,
        .MipLevels = 1,
        .Format = DepthBufferFormat,
        .SampleDesc = {.Count = 1, .Quality = 0},
        .Layout = D3D12_TEXTURE_LAYOUT_UNKNOWN,
        .Flags = D3D12_RESOURCE_FLAG_ALLOW_DEPTH_STENCIL,
    };

    D3D12_CLEAR_VALUE clearValue{
        .Format = DepthBufferFormat,
        .DepthStencil = {.Depth = 1.0f, .Stencil = 0},
    };
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

    D3D12_DEPTH_STENCIL_VIEW_DESC dsvDescription{
        .Format = DepthBufferFormat,
        .ViewDimension = D3D12_DSV_DIMENSION_TEXTURE2D,
        .Flags = D3D12_DSV_FLAG_NONE,
        .Texture2D = {
            .MipSlice = 0,
        },
    };
    m_device->CreateDepthStencilView(
        m_depthBuffer.Get(),
        &dsvDescription,
        m_dsvHeap->GetCPUDescriptorHandleForHeapStart());

    m_viewport = {
        .TopLeftX = 0.0f,
        .TopLeftY = 0.0f,
        .Width = static_cast<float>(m_width),
        .Height = static_cast<float>(m_height),
        .MinDepth = 0.0f,
        .MaxDepth = 1.0f,
    };
    const float sceneWidth = std::min(
        static_cast<float>(m_width),
        static_cast<float>(m_height) * BackgroundAspectRatio);
    m_sceneViewport = {
        .TopLeftX = (static_cast<float>(m_width) - sceneWidth) * 0.5f,
        .TopLeftY = 0.0f,
        .Width = sceneWidth,
        .Height = static_cast<float>(m_height),
        .MinDepth = 0.0f,
        .MaxDepth = 1.0f,
    };
    m_scissorRect = {
        .left = 0,
        .top = 0,
        .right = static_cast<LONG>(m_width),
        .bottom = static_cast<LONG>(m_height),
    };
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
    SimplePaint::ValidateMesh<GeneratedCarMesh::Vertex>(vertices, indices, PaintMaterialCount);
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

    const D3D12_RANGE noCpuReads{.Begin = 0, .End = 0};
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

    D3D12_TEXTURE_COPY_LOCATION textureDestination{
        .pResource = m_backgroundTexture.Get(),
        .Type = D3D12_TEXTURE_COPY_TYPE_SUBRESOURCE_INDEX,
        .SubresourceIndex = 0,
    };
    D3D12_TEXTURE_COPY_LOCATION textureSource{
        .pResource = textureUpload.Get(),
        .Type = D3D12_TEXTURE_COPY_TYPE_PLACED_FOOTPRINT,
        .PlacedFootprint = textureFootprint,
    };
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

    m_sceneMesh.vertexView = {
        .BufferLocation = m_sceneMesh.vertexBuffer->GetGPUVirtualAddress(),
        .SizeInBytes = static_cast<std::uint32_t>(vertexBytes),
        .StrideInBytes = sizeof(GeneratedCarMesh::Vertex),
    };
    m_sceneMesh.indexView = {
        .BufferLocation = m_sceneMesh.indexBuffer->GetGPUVirtualAddress(),
        .SizeInBytes = static_cast<std::uint32_t>(indexBytes),
        .Format = DXGI_FORMAT_R32_UINT,
    };
    m_sceneMesh.indexCount = static_cast<std::uint32_t>(indices.size());

    D3D12_SHADER_RESOURCE_VIEW_DESC srvDescription{
        .Format = DXGI_FORMAT_R8G8B8A8_UNORM_SRGB,
        .ViewDimension = D3D12_SRV_DIMENSION_TEXTURE2D,
        .Shader4ComponentMapping = D3D12_DEFAULT_SHADER_4_COMPONENT_MAPPING,
        .Texture2D = {
            .MostDetailedMip = 0,
            .MipLevels = 1,
            .PlaneSlice = 0,
            .ResourceMinLODClamp = 0.0f,
        },
    };
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
    const std::uint64_t bufferSize = MaterialConstantOffset + MaterialConstantSize;
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

    const D3D12_RANGE noCpuReads{.Begin = 0, .End = 0};
    void* mappedData = nullptr;
    Check(m_constantBuffer->Map(0, &noCpuReads, &mappedData), "Map scene constant buffer");
    m_mappedConstants = static_cast<std::byte*>(mappedData);
    // Settings are immutable after startup; both objects and both frames share them.
    std::memcpy(m_mappedConstants + MaterialConstantOffset,
        m_paintMaterials.data(), sizeof(m_paintMaterials));
}

void Renderer::LoadPaintSettings()
{
    constexpr std::array<std::string_view, PaintMaterialCount> materialSections = {
        "SimplePaintShader_Axles",
        "SimplePaintShader_Body",
        "SimplePaintShader_Cabin",
        "SimplePaintShader_Headlights",
        "SimplePaintShader_Wheels",
        "SimplePaintShader_Sphere",
    };
    std::array<PaintSettings, PaintMaterialCount> materialSettings{};
    const std::array<std::array<double, 3>, PaintMaterialCount> defaultColors = {{
        {0.678429127, 0.678431321, 0.678431321},
        {SimplePaint::Margin, 0.436627067, SimplePaint::InteriorMaximum},
        {0.506386429, 0.756053146, SimplePaint::InteriorMaximum},
        {SimplePaint::InteriorMaximum, 0.815686771, SimplePaint::Margin},
        {0.345097446, 0.345097446, 0.345097446},
        {0.107, 0.223, 0.578},
    }};

    for (std::size_t index = 0; index < materialSettings.size(); ++index)
    {
        materialSettings[index] = {
            .baseColorSrgb = defaultColors[index],
            .brightness = index == CarMaterialCount ? 0.126 : 0.5,
            .lightPoint = index == CarMaterialCount ? 1.0 : 0.8,
        };
    }

    const std::filesystem::path settingsPath = ModuleDirectory() / L"assets" / L"Settings.ini";
    std::ifstream input(settingsPath);
    if (!input)
    {
        throw std::runtime_error(std::format(
            "Settings were not found at {}.",
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
            if (section != "Sphere" &&
                std::find(materialSections.begin(), materialSections.end(), section) == materialSections.end())
            {
                throw std::runtime_error(std::format(
                    "Unknown Settings.ini section [{}] on line {}.",
                    section,
                    lineNumber));
            }
            continue;
        }

        const std::size_t equals = content.find('=');
        if (equals == std::string_view::npos)
        {
            throw std::runtime_error(std::format(
                "Expected key = value on Settings.ini line {}.",
                lineNumber));
        }
        const std::string key(Trim(content.substr(0, equals)));
        const std::string_view value = Trim(content.substr(equals + 1));
        const std::string qualifiedKey = section + "." + key;

        if (qualifiedKey == "Sphere.UResolution" || qualifiedKey == "Sphere.VResolution")
        {
            std::uint32_t resolution = 0;
            const auto [end, error] = std::from_chars(value.data(), value.data() + value.size(), resolution);
            const bool isU = qualifiedKey == "Sphere.UResolution";
            const std::uint32_t minimum = isU ? 3u : 2u;
            if (error != std::errc{} || end != value.data() + value.size() ||
                resolution < minimum || resolution > 512)
            {
                throw std::runtime_error(std::format(
                    "{} must be an integer in [{}, 512] on Settings.ini line {}.",
                    qualifiedKey, minimum, lineNumber));
            }
            (isU ? m_sphereUResolution : m_sphereVResolution) = resolution;
        }
        else
        {
            const auto materialSection = std::find(materialSections.begin(), materialSections.end(), section);
            if (materialSection == materialSections.end())
            {
                throw std::runtime_error(std::format(
                    "Unknown Settings.ini key '{}' on line {}.", qualifiedKey, lineNumber));
            }
            PaintSettings& settings = materialSettings[
                static_cast<std::size_t>(materialSection - materialSections.begin())];
            if (key == "BaseColor")
            {
                settings.baseColorSrgb = ParseColor(value, lineNumber);
            }
            else if (key == "Brightness")
            {
                settings.brightness = ParseFloat(value, lineNumber);
            }
            else if (key == "Shift")
            {
                settings.shift = ParseFloat(value, lineNumber);
            }
            else if (key == "RotationDegrees")
            {
                settings.rotationDegrees = ParseFloat(value, lineNumber);
            }
            else if (key == "DarkPoint")
            {
                settings.darkPoint = ParseFloat(value, lineNumber);
            }
            else if (key == "LightPoint")
            {
                settings.lightPoint = ParseFloat(value, lineNumber);
            }
            else
            {
                throw std::runtime_error(std::format(
                    "Unknown Settings.ini key '{}' on line {}.", qualifiedKey, lineNumber));
            }
        }
    }

    for (std::size_t index = 0; index < m_paintMaterials.size(); ++index)
    {
        try
        {
            m_paintMaterials[index] = SimplePaint::Material::Compile(materialSettings[index]).Constants();
        }
        catch (const std::invalid_argument& error)
        {
            throw std::invalid_argument(std::format("[{}]: {}", materialSections[index], error.what()));
        }
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
    m_orthographicProjection = Orthographic::MakeProjection(
        5.0f * aspectRatio, 5.0f, 1.0f, 20.0f);
    DirectX::XMStoreFloat4x4(&m_view, view);

    // UpdateCamera runs at initialization or after Resize has waited for the GPU.
    // The stationary sphere needs new constants only when the viewport changes.
    constexpr float sphereRadius = 0.4f;
    const XMMATRIX sphereWorld =
        DirectX::XMMatrixScaling(sphereRadius, sphereRadius, sphereRadius) *
        DirectX::XMMatrixTranslation(1.5f, sphereRadius, -1.5f);
    for (std::uint32_t frame = 0; frame < FrameCount; ++frame)
    {
        WriteObjectConstants(frame, 1, sphereWorld);
    }
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

    return {
        .position = static_cast<float>(travelDistance - std::abs(phase - travelDistance * 2.0)),
        .rotation = static_cast<float>(std::fmod(elapsed * rotationSpeed, std::numbers::pi * 2.0)),
    };
}

void Renderer::WriteObjectConstants(
    const std::uint32_t frameIndex,
    const std::uint32_t objectIndex,
    DirectX::FXMMATRIX world)
{
    const XMMATRIX view = DirectX::XMLoadFloat4x4(&m_view);
    const Orthographic::ObjectTransforms constants =
        Orthographic::BuildObjectTransforms(world * view, m_orthographicProjection);
    std::memcpy(
        m_mappedConstants + (static_cast<std::size_t>(frameIndex) * ObjectsPerFrame + objectIndex) *
            ObjectConstantStride,
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
        static_cast<std::uint64_t>(frameIndex) * ObjectsPerFrame * ObjectConstantStride;
    m_commandList->SetGraphicsRootConstantBufferView(0, constantsAddress);
    m_commandList->SetGraphicsRootConstantBufferView(
        1, m_constantBuffer->GetGPUVirtualAddress() + MaterialConstantOffset);
    m_commandList->IASetPrimitiveTopology(D3D_PRIMITIVE_TOPOLOGY_TRIANGLELIST);
    m_commandList->IASetVertexBuffers(0, 1, &m_sceneMesh.vertexView);
    m_commandList->IASetIndexBuffer(&m_sceneMesh.indexView);
    m_commandList->DrawIndexedInstanced(m_carIndexCount, 1, 0, 0, 0);
    m_commandList->SetGraphicsRootConstantBufferView(0, constantsAddress + ObjectConstantStride);
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
