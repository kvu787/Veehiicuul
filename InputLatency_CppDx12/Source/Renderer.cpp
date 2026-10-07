#include "Renderer.h"
#include "VertexShader.h"
#include "PixelShader.h"

#include <wincodec.h>

#include <algorithm>
#include <cmath>
#include <cstring>
#include <format>
#include <numbers>
#include <stdexcept>

using Microsoft::WRL::ComPtr;

namespace
{
constexpr DXGI_FORMAT BackBufferFormat = DXGI_FORMAT_R8G8B8A8_UNORM;
constexpr UINT AtlasWidth = 192, AtlasHeight = 144, GlyphWidth = 12, GlyphHeight = 24;
constexpr float StatisticsPanelHeight = 196, PanelGap = 12;
constexpr Color Gray(float brightness) { return {brightness, brightness, brightness}; }
constexpr Color Foreground = Gray(0.92f), Muted = Gray(0.64f), Accent = Gray(1.0f), SecondaryAccent = Gray(0.76f);

void Check(HRESULT result, const char* operation)
{
    if (FAILED(result)) throw std::runtime_error(std::format("{} failed: 0x{:08X}", operation, static_cast<unsigned long>(result)));
}

D3D12_RESOURCE_BARRIER Transition(ID3D12Resource* resource, D3D12_RESOURCE_STATES before, D3D12_RESOURCE_STATES after)
{
    D3D12_RESOURCE_BARRIER result{};
    result.Type = D3D12_RESOURCE_BARRIER_TYPE_TRANSITION;
    result.Transition = {resource, D3D12_RESOURCE_BARRIER_ALL_SUBRESOURCES, before, after};
    return result;
}

ComPtr<ID3D12Resource> CreateBuffer(ID3D12Device* device, std::size_t bytes)
{
    D3D12_HEAP_PROPERTIES heap{};
    heap.Type = D3D12_HEAP_TYPE_UPLOAD;
    D3D12_RESOURCE_DESC description{};
    description.Dimension = D3D12_RESOURCE_DIMENSION_BUFFER;
    description.Width = bytes;
    description.Height = 1;
    description.DepthOrArraySize = 1;
    description.MipLevels = 1;
    description.SampleDesc.Count = 1;
    description.Layout = D3D12_TEXTURE_LAYOUT_ROW_MAJOR;
    ComPtr<ID3D12Resource> result;
    Check(device->CreateCommittedResource(&heap, D3D12_HEAP_FLAG_NONE, &description,
        D3D12_RESOURCE_STATE_GENERIC_READ, nullptr, IID_PPV_ARGS(&result)), "Create upload buffer");
    return result;
}

std::vector<std::uint32_t> CreateFontPixels()
{
    // Rasterize an installed Windows font once at startup. All subsequent UI
    // text is ordinary batched DirectX 12 geometry, without GDI presentation.
    const HDC context = CreateCompatibleDC(nullptr);
    if (!context) throw std::runtime_error("Cannot create the font rasterizer.");
    BITMAPINFO information{};
    information.bmiHeader.biSize = sizeof(BITMAPINFOHEADER);
    information.bmiHeader.biWidth = AtlasWidth;
    information.bmiHeader.biHeight = -static_cast<LONG>(AtlasHeight);
    information.bmiHeader.biPlanes = 1;
    information.bmiHeader.biBitCount = 32;
    information.bmiHeader.biCompression = BI_RGB;
    void* pixels{};
    const HBITMAP bitmap = CreateDIBSection(context, &information, DIB_RGB_COLORS, &pixels, nullptr, 0);
    const HFONT font = CreateFontW(-18, 0, 0, 0, FW_NORMAL, FALSE, FALSE, FALSE, DEFAULT_CHARSET,
        OUT_DEFAULT_PRECIS, CLIP_DEFAULT_PRECIS, ANTIALIASED_QUALITY, FIXED_PITCH, L"Consolas");
    if (!bitmap || !font) {
        if (font) DeleteObject(font);
        if (bitmap) DeleteObject(bitmap);
        DeleteDC(context);
        throw std::runtime_error("Cannot create the dashboard font atlas.");
    }
    const auto oldBitmap = SelectObject(context, bitmap);
    const auto oldFont = SelectObject(context, font);
    std::memset(pixels, 0, AtlasWidth * AtlasHeight * 4);
    SetBkColor(context, RGB(0, 0, 0));
    SetTextColor(context, RGB(255, 255, 255));
    for (UINT index = 0; index < 95; ++index) {
        const wchar_t character = static_cast<wchar_t>(index + 32);
        TextOutW(context, static_cast<int>((index % 16) * GlyphWidth),
            static_cast<int>((index / 16) * GlyphHeight), &character, 1);
    }
    GdiFlush();
    std::vector<std::uint32_t> result(AtlasWidth * AtlasHeight);
    const auto* source = static_cast<const std::uint32_t*>(pixels);
    for (std::size_t index = 0; index < result.size(); ++index) {
        const auto pixel = source[index];
        const auto coverage = std::max({pixel & 255u, (pixel >> 8) & 255u, (pixel >> 16) & 255u});
        result[index] = 0x00ffffffu | (coverage << 24);
    }
    // Reserved solid texel in the otherwise empty space glyph.
    result[0] = 0xffffffffu;
    SelectObject(context, oldFont); SelectObject(context, oldBitmap);
    DeleteObject(font); DeleteObject(bitmap); DeleteDC(context);
    return result;
}

std::string Milliseconds(double microseconds) { return std::format("{:.3f}", microseconds / 1000.0); }
}

Renderer::~Renderer()
{
    try { if (queue && fence && fenceEvent) WaitForGpu(); } catch (...) {}
    if (vertexBuffer && mappedVertices) vertexBuffer->Unmap(0, nullptr);
    if (presentationEvent) CloseHandle(presentationEvent);
    if (fenceEvent) CloseHandle(fenceEvent);
}

void Renderer::Initialize(HWND window, std::uint32_t initialWidth, std::uint32_t initialHeight, bool softwareAdapter)
{
    width = initialWidth; height = initialHeight;
    UINT factoryFlags = 0;
#if defined(_DEBUG)
    ComPtr<ID3D12Debug> debug;
    if (SUCCEEDED(D3D12GetDebugInterface(IID_PPV_ARGS(&debug)))) {
        debug->EnableDebugLayer(); factoryFlags = DXGI_CREATE_FACTORY_DEBUG;
    }
#endif
    Check(CreateDXGIFactory2(factoryFlags, IID_PPV_ARGS(&factory)), "CreateDXGIFactory2");
    BOOL supportsTearing = FALSE;
    if (SUCCEEDED(factory->CheckFeatureSupport(DXGI_FEATURE_PRESENT_ALLOW_TEARING, &supportsTearing, sizeof(supportsTearing)))) tearing = supportsTearing != FALSE;
    ComPtr<IDXGIAdapter1> selected;
    const auto tryAdapter = [&](IDXGIAdapter1* adapter) {
        ComPtr<ID3D12Device> candidate;
        if (FAILED(D3D12CreateDevice(adapter, D3D_FEATURE_LEVEL_11_0, IID_PPV_ARGS(&candidate)))) return false;
        D3D12_FEATURE_DATA_SHADER_MODEL model{D3D_SHADER_MODEL_6_0};
        if (FAILED(candidate->CheckFeatureSupport(D3D12_FEATURE_SHADER_MODEL, &model, sizeof(model))) || model.HighestShaderModel < D3D_SHADER_MODEL_6_0) return false;
        selected = adapter; device = candidate;
        return true;
    };
    for (UINT index = 0; !softwareAdapter; ++index) {
        ComPtr<IDXGIAdapter1> adapter;
        const auto result = factory->EnumAdapterByGpuPreference(index, DXGI_GPU_PREFERENCE_HIGH_PERFORMANCE, IID_PPV_ARGS(&adapter));
        if (result == DXGI_ERROR_NOT_FOUND) break;
        Check(result, "Enumerate hardware adapter");
        DXGI_ADAPTER_DESC1 description{};
        Check(adapter->GetDesc1(&description), "Read adapter description");
        if (!(description.Flags & DXGI_ADAPTER_FLAG_SOFTWARE) && tryAdapter(adapter.Get())) break;
    }
    if (!device) {
        ComPtr<IDXGIAdapter1> adapter;
        Check(factory->EnumWarpAdapter(IID_PPV_ARGS(&adapter)), "Enumerate WARP adapter");
        if (!tryAdapter(adapter.Get())) throw std::runtime_error("No DirectX 12 adapter supports Shader Model 6.0.");
    }
    DXGI_ADAPTER_DESC1 adapterDescription{};
    Check(selected->GetDesc1(&adapterDescription), "Read selected adapter");
    for (wchar_t character : adapterDescription.Description) { if (!character) break; adapterName += character < 128 ? static_cast<char>(character) : '?'; }
    D3D12_COMMAND_QUEUE_DESC queueDescription{};
    queueDescription.Type = D3D12_COMMAND_LIST_TYPE_DIRECT;
    Check(device->CreateCommandQueue(&queueDescription, IID_PPV_ARGS(&queue)), "Create command queue");
    swapChainFlags = DXGI_SWAP_CHAIN_FLAG_FRAME_LATENCY_WAITABLE_OBJECT | (tearing ? DXGI_SWAP_CHAIN_FLAG_ALLOW_TEARING : 0);
    DXGI_SWAP_CHAIN_DESC1 description{};
    description.Width = width; description.Height = height;
    description.Format = BackBufferFormat; description.SampleDesc.Count = 1;
    description.BufferUsage = DXGI_USAGE_RENDER_TARGET_OUTPUT;
    description.BufferCount = 2; description.SwapEffect = DXGI_SWAP_EFFECT_FLIP_DISCARD;
    description.Scaling = DXGI_SCALING_STRETCH; description.AlphaMode = DXGI_ALPHA_MODE_IGNORE;
    description.Flags = swapChainFlags;
    ComPtr<IDXGISwapChain1> baseSwapChain;
    Check(factory->CreateSwapChainForHwnd(queue.Get(), window, &description, nullptr, nullptr, &baseSwapChain), "Create swap chain");
    Check(baseSwapChain.As(&swapChain), "Query swap chain v3");
    Check(swapChain->SetMaximumFrameLatency(1), "Set maximum presentation latency");
    presentationEvent = swapChain->GetFrameLatencyWaitableObject();
    if (!presentationEvent) throw std::runtime_error("The presentation waitable object is missing.");
    Check(factory->MakeWindowAssociation(window, DXGI_MWA_NO_ALT_ENTER), "Disable exclusive fullscreen");
    D3D12_DESCRIPTOR_HEAP_DESC targetDescription{};
    targetDescription.Type = D3D12_DESCRIPTOR_HEAP_TYPE_RTV; targetDescription.NumDescriptors = 2;
    Check(device->CreateDescriptorHeap(&targetDescription, IID_PPV_ARGS(&targetHeap)), "Create render target heap");
    descriptorSize = device->GetDescriptorHandleIncrementSize(D3D12_DESCRIPTOR_HEAP_TYPE_RTV);
    Check(device->CreateCommandAllocator(D3D12_COMMAND_LIST_TYPE_DIRECT, IID_PPV_ARGS(&allocator)), "Create command allocator");
    Check(device->CreateCommandList(0, D3D12_COMMAND_LIST_TYPE_DIRECT, allocator.Get(), nullptr, IID_PPV_ARGS(&commands)), "Create command list");
    Check(commands->Close(), "Close initial command list");
    Check(device->CreateFence(0, D3D12_FENCE_FLAG_NONE, IID_PPV_ARGS(&fence)), "Create GPU fence");
    fenceEvent = CreateEventW(nullptr, FALSE, FALSE, nullptr);
    if (!fenceEvent) throw std::runtime_error("Cannot create GPU completion event.");
    CreateTargets(); CreatePipelineAndAtlas();
    vertexBuffer = CreateBuffer(device.Get(), VertexCapacity * sizeof(DashboardVertex));
    D3D12_RANGE noReads{0, 0};
    Check(vertexBuffer->Map(0, &noReads, reinterpret_cast<void**>(&mappedVertices)), "Map vertex buffer");
    vertices.reserve(VertexCapacity);
}

void Renderer::CreateTargets()
{
    auto handle = targetHeap->GetCPUDescriptorHandleForHeapStart();
    for (UINT index = 0; index < 2; ++index) {
        Check(swapChain->GetBuffer(index, IID_PPV_ARGS(&targets[index])), "Get back buffer");
        device->CreateRenderTargetView(targets[index].Get(), nullptr, handle);
        handle.ptr += descriptorSize;
    }
}

void Renderer::CreatePipelineAndAtlas()
{
    D3D12_DESCRIPTOR_RANGE textureRange{D3D12_DESCRIPTOR_RANGE_TYPE_SRV, 1, 0, 0, D3D12_DESCRIPTOR_RANGE_OFFSET_APPEND};
    std::array<D3D12_ROOT_PARAMETER, 2> parameters{};
    parameters[0].ParameterType = D3D12_ROOT_PARAMETER_TYPE_32BIT_CONSTANTS;
    parameters[0].Constants = {0, 0, 2}; parameters[0].ShaderVisibility = D3D12_SHADER_VISIBILITY_VERTEX;
    parameters[1].ParameterType = D3D12_ROOT_PARAMETER_TYPE_DESCRIPTOR_TABLE;
    parameters[1].DescriptorTable = {1, &textureRange}; parameters[1].ShaderVisibility = D3D12_SHADER_VISIBILITY_PIXEL;
    D3D12_STATIC_SAMPLER_DESC sampler{};
    sampler.Filter = D3D12_FILTER_MIN_MAG_MIP_POINT;
    sampler.AddressU = sampler.AddressV = sampler.AddressW = D3D12_TEXTURE_ADDRESS_MODE_CLAMP;
    sampler.ComparisonFunc = D3D12_COMPARISON_FUNC_ALWAYS;
    sampler.MaxLOD = D3D12_FLOAT32_MAX; sampler.ShaderVisibility = D3D12_SHADER_VISIBILITY_PIXEL;
    D3D12_ROOT_SIGNATURE_DESC signature{};
    signature.NumParameters = static_cast<UINT>(parameters.size()); signature.pParameters = parameters.data();
    signature.NumStaticSamplers = 1; signature.pStaticSamplers = &sampler;
    signature.Flags = D3D12_ROOT_SIGNATURE_FLAG_ALLOW_INPUT_ASSEMBLER_INPUT_LAYOUT;
    ComPtr<ID3DBlob> serialized, errors;
    Check(D3D12SerializeRootSignature(&signature, D3D_ROOT_SIGNATURE_VERSION_1, &serialized, &errors), "Serialize root signature");
    Check(device->CreateRootSignature(0, serialized->GetBufferPointer(), serialized->GetBufferSize(), IID_PPV_ARGS(&rootSignature)), "Create root signature");
    const std::array<D3D12_INPUT_ELEMENT_DESC, 3> elements{{
        {"POSITION", 0, DXGI_FORMAT_R32G32_FLOAT, 0, 0, D3D12_INPUT_CLASSIFICATION_PER_VERTEX_DATA, 0},
        {"TEXCOORD", 0, DXGI_FORMAT_R32G32_FLOAT, 0, 8, D3D12_INPUT_CLASSIFICATION_PER_VERTEX_DATA, 0},
        {"COLOR", 0, DXGI_FORMAT_R32G32B32A32_FLOAT, 0, 16, D3D12_INPUT_CLASSIFICATION_PER_VERTEX_DATA, 0}}};
    D3D12_GRAPHICS_PIPELINE_STATE_DESC description{};
    description.pRootSignature = rootSignature.Get();
    description.VS = {VertexShader, sizeof(VertexShader)}; description.PS = {PixelShader, sizeof(PixelShader)};
    auto& blend = description.BlendState.RenderTarget[0];
    blend.BlendEnable = TRUE; blend.SrcBlend = D3D12_BLEND_SRC_ALPHA; blend.DestBlend = D3D12_BLEND_INV_SRC_ALPHA;
    blend.BlendOp = D3D12_BLEND_OP_ADD; blend.SrcBlendAlpha = D3D12_BLEND_ONE;
    blend.DestBlendAlpha = D3D12_BLEND_INV_SRC_ALPHA; blend.BlendOpAlpha = D3D12_BLEND_OP_ADD;
    blend.RenderTargetWriteMask = D3D12_COLOR_WRITE_ENABLE_ALL;
    description.SampleMask = UINT_MAX;
    description.RasterizerState.FillMode = D3D12_FILL_MODE_SOLID;
    description.RasterizerState.CullMode = D3D12_CULL_MODE_NONE;
    description.RasterizerState.DepthClipEnable = TRUE;
    description.DepthStencilState.DepthEnable = FALSE; description.DepthStencilState.StencilEnable = FALSE;
    description.InputLayout = {elements.data(), static_cast<UINT>(elements.size())};
    description.PrimitiveTopologyType = D3D12_PRIMITIVE_TOPOLOGY_TYPE_TRIANGLE;
    description.NumRenderTargets = 1; description.RTVFormats[0] = BackBufferFormat; description.SampleDesc.Count = 1;
    Check(device->CreateGraphicsPipelineState(&description, IID_PPV_ARGS(&pipeline)), "Create dashboard pipeline");

    D3D12_DESCRIPTOR_HEAP_DESC textureDescription{};
    textureDescription.Type = D3D12_DESCRIPTOR_HEAP_TYPE_CBV_SRV_UAV; textureDescription.NumDescriptors = 1;
    textureDescription.Flags = D3D12_DESCRIPTOR_HEAP_FLAG_SHADER_VISIBLE;
    Check(device->CreateDescriptorHeap(&textureDescription, IID_PPV_ARGS(&textureHeap)), "Create font texture heap");
    D3D12_RESOURCE_DESC atlasDescription{};
    atlasDescription.Dimension = D3D12_RESOURCE_DIMENSION_TEXTURE2D;
    atlasDescription.Width = AtlasWidth; atlasDescription.Height = AtlasHeight;
    atlasDescription.DepthOrArraySize = 1; atlasDescription.MipLevels = 1;
    atlasDescription.Format = BackBufferFormat; atlasDescription.SampleDesc.Count = 1;
    D3D12_HEAP_PROPERTIES heap{}; heap.Type = D3D12_HEAP_TYPE_DEFAULT;
    Check(device->CreateCommittedResource(&heap, D3D12_HEAP_FLAG_NONE, &atlasDescription, D3D12_RESOURCE_STATE_COPY_DEST,
        nullptr, IID_PPV_ARGS(&atlas)), "Create font texture");
    D3D12_PLACED_SUBRESOURCE_FOOTPRINT footprint{};
    UINT64 uploadSize{};
    device->GetCopyableFootprints(&atlasDescription, 0, 1, 0, &footprint, nullptr, nullptr, &uploadSize);
    auto upload = CreateBuffer(device.Get(), static_cast<std::size_t>(uploadSize));
    std::byte* uploadData{};
    D3D12_RANGE noReads{0, 0};
    Check(upload->Map(0, &noReads, reinterpret_cast<void**>(&uploadData)), "Map font upload");
    const auto pixels = CreateFontPixels();
    for (UINT row = 0; row < AtlasHeight; ++row) {
        std::memcpy(uploadData + footprint.Offset + row * footprint.Footprint.RowPitch, pixels.data() + row * AtlasWidth, AtlasWidth * 4);
    }
    upload->Unmap(0, nullptr);
    Check(allocator->Reset(), "Reset font allocator"); Check(commands->Reset(allocator.Get(), nullptr), "Reset font commands");
    D3D12_TEXTURE_COPY_LOCATION destination{}; destination.pResource = atlas.Get(); destination.Type = D3D12_TEXTURE_COPY_TYPE_SUBRESOURCE_INDEX;
    D3D12_TEXTURE_COPY_LOCATION source{}; source.pResource = upload.Get(); source.Type = D3D12_TEXTURE_COPY_TYPE_PLACED_FOOTPRINT; source.PlacedFootprint = footprint;
    commands->CopyTextureRegion(&destination, 0, 0, 0, &source, nullptr);
    auto barrier = Transition(atlas.Get(), D3D12_RESOURCE_STATE_COPY_DEST, D3D12_RESOURCE_STATE_PIXEL_SHADER_RESOURCE);
    commands->ResourceBarrier(1, &barrier);
    Check(commands->Close(), "Close font commands");
    ID3D12CommandList* lists[]{commands.Get()}; queue->ExecuteCommandLists(1, lists); WaitForGpu();
    D3D12_SHADER_RESOURCE_VIEW_DESC view{};
    view.Format = BackBufferFormat; view.ViewDimension = D3D12_SRV_DIMENSION_TEXTURE2D;
    view.Shader4ComponentMapping = D3D12_DEFAULT_SHADER_4_COMPONENT_MAPPING; view.Texture2D.MipLevels = 1;
    device->CreateShaderResourceView(atlas.Get(), &view, textureHeap->GetCPUDescriptorHandleForHeapStart());
}

void Renderer::WaitForGpu()
{
    lastFence = nextFence++;
    Check(queue->Signal(fence.Get(), lastFence), "Signal GPU completion");
    Check(fence->SetEventOnCompletion(lastFence, fenceEvent), "Set GPU completion event");
    if (WaitForSingleObject(fenceEvent, 10000) != WAIT_OBJECT_0) throw std::runtime_error("GPU completion wait failed or timed out.");
}

bool Renderer::PrepareFrame(const std::function<bool()>& serviceMessages)
{
    std::uint32_t polls = 0;
    for (;;) {
        if ((polls++ & 63u) == 0 && !serviceMessages()) return false;
        const auto completed = fence->GetCompletedValue();
        if (completed == UINT64_MAX) Check(device->GetDeviceRemovedReason(), "GPU removed during readiness wait");
        if (!presentationAdmitted) {
            const auto result = WaitForSingleObject(presentationEvent, 0);
            if (result == WAIT_OBJECT_0) presentationAdmitted = true;
            else if (result != WAIT_TIMEOUT) throw std::runtime_error("Presentation readiness poll failed.");
        }
        if (completed >= lastFence && presentationAdmitted) {
            if (!serviceMessages()) return false;
            backBuffer = swapChain->GetCurrentBackBufferIndex();
            return true;
        }
        YieldProcessor();
    }
}

void Renderer::Resize(std::uint32_t newWidth, std::uint32_t newHeight)
{
    if (!newWidth || !newHeight || (newWidth == width && newHeight == height)) return;
    WaitForGpu();
    for (auto& target : targets) target.Reset();
    Check(swapChain->ResizeBuffers(2, newWidth, newHeight, BackBufferFormat, swapChainFlags), "Resize swap chain");
    width = newWidth; height = newHeight; presentationAdmitted = false;
    CreateTargets();
}

void Renderer::Quad(float x, float y, float rectangleWidth, float rectangleHeight, float u0, float v0, float u1, float v1, Color color)
{
    if (vertices.size() + 6 > VertexCapacity) return;
    const DashboardVertex topLeft{x, y, u0, v0, color}, topRight{x + rectangleWidth, y, u1, v0, color};
    const DashboardVertex bottomLeft{x, y + rectangleHeight, u0, v1, color}, bottomRight{x + rectangleWidth, y + rectangleHeight, u1, v1, color};
    for (auto vertex : {topLeft, topRight, bottomLeft, bottomLeft, topRight, bottomRight}) vertices.push_back(vertex);
}

void Renderer::Rectangle(float x, float y, float rectangleWidth, float rectangleHeight, Color color)
{
    const float u = 0.5f / AtlasWidth, v = 0.5f / AtlasHeight;
    Quad(x, y, rectangleWidth, rectangleHeight, u, v, u, v, color);
}

void Renderer::Triangle(float firstX, float firstY, float secondX, float secondY, float thirdX, float thirdY, Color color)
{
    if (vertices.size() + 3 > VertexCapacity) return;
    const float u = 0.5f / AtlasWidth, v = 0.5f / AtlasHeight;
    vertices.push_back({firstX, firstY, u, v, color});
    vertices.push_back({secondX, secondY, u, v, color});
    vertices.push_back({thirdX, thirdY, u, v, color});
}

void Renderer::Circle(float x, float y, float radius, Color color)
{
    constexpr std::size_t SegmentCount = 32;
    // Computed once, shared by static guides and live stick markers.
    static const auto directions = [] {
        std::array<StickPosition, SegmentCount + 1> result{};
        for (std::size_t index = 0; index <= SegmentCount; ++index) {
            const float angle = static_cast<float>(index) * 2 * std::numbers::pi_v<float> / static_cast<float>(SegmentCount);
            result[index] = {std::cos(angle), std::sin(angle)};
        }
        return result;
    }();
    for (std::size_t index = 0; index < SegmentCount; ++index) {
        Triangle(x, y, x + radius * directions[index].x, y + radius * directions[index].y,
            x + radius * directions[index + 1].x, y + radius * directions[index + 1].y, color);
    }
}

float Renderer::ControllerPanel(const MonitorSnapshot& snapshot, float y)
{
    controllerPanelBeginning = y;
    const float offset = y - 142;
    std::array<ControllerAvailability, MaximumDevices> candidates{};
    const auto count = std::min(snapshot.devices.size(), candidates.size());
    for (std::size_t index = 0; index < count; ++index) candidates[index] = {snapshot.devices[index].connected, snapshot.devices[index].gamepad};
    selectedController = SelectController(std::span(candidates).first(count), selectedController);
    Rectangle(20, y, static_cast<float>(width) - 40, 334, Gray(0.10f));
    if (selectedController) {
        const auto& controller = snapshot.devices[*selectedController];
        Text(32, 152 + offset, std::format("SELECTED CONTROLLER #{} | {:04X}:{:04X}", *selectedController, controller.vendor, controller.product), Accent);
        Text(32, 178 + offset, "Name: " + controller.name, Foreground);
        Text(32, 204 + offset, "GameInput ID: " + controller.identifier, Muted);
    }
    else {
        Text(32, 152 + offset, "SELECTED CONTROLLER: none connected", Accent);
        Text(32, 178 + offset, "Connect a gamepad. The first connected gamepad will be selected automatically.", Foreground);
        Text(32, 204 + offset, "Name / GameInput ID: unavailable", Muted);
    }
    Text(32, 230 + offset, "Sticks -1..+1 (positive Y up) | Triggers 0..1 | Raw values, no added deadzone", Muted);
    const Color guide = Gray(0.30f), inside = Gray(0.15f);
    for (const float centerX : {150.0f, 390.0f}) {
        Rectangle(centerX - 58, 296 + offset, 116, 116, guide);
        Rectangle(centerX - 56, 298 + offset, 112, 112, inside);
        Circle(centerX, 354 + offset, 56, guide);
        Circle(centerX, 354 + offset, 54, inside);
        Rectangle(centerX - 56, 353 + offset, 112, 2, guide);
        Rectangle(centerX - 1, 298 + offset, 2, 112, guide);
    }
    Text(90, 268 + offset, "LEFT STICK", Foreground); Text(324, 268 + offset, "RIGHT STICK", Foreground);
    Text(96, 418 + offset, "X", Muted); Text(96, 442 + offset, "Y", Muted);
    Text(336, 418 + offset, "X", Muted); Text(336, 442 + offset, "Y", Muted);
    Text(584, 282 + offset, "LEFT TRIGGER", Foreground); Text(584, 378 + offset, "RIGHT TRIGGER", Foreground);
    const float barWidth = std::max(1.0f, static_cast<float>(width) - 624);
    Rectangle(584, 314 + offset, barWidth, 22, guide); Rectangle(584, 410 + offset, barWidth, 22, guide);
    return y + 334 + PanelGap;
}

void Renderer::DrawControllerState(VisualState& state)
{
    const float offset = controllerPanelBeginning - 142;
    const bool available = state.connected && state.controllerReadingAvailable;
    state.visualized = available;
    const auto& analog = state.controller;
    const auto value = [&](float x, float y, float number, bool signedValue) {
        const auto text = FormatAnalogValue(number, signedValue);
        Text(x, y, available ? text.View() : std::string_view("--"), available ? Accent : Muted);
    };
    value(120, 418 + offset, analog.leftStickX, true); value(120, 442 + offset, analog.leftStickY, true);
    value(360, 418 + offset, analog.rightStickX, true); value(360, 442 + offset, analog.rightStickY, true);
    value(776, 282 + offset, analog.leftTrigger, false); value(776, 378 + offset, analog.rightTrigger, false);
    if (!available) {
        Text(584, 442 + offset, "Waiting for gamepad state", Muted);
        return;
    }
    const auto left = MapStickPosition(analog.leftStickX, analog.leftStickY, 150, 354 + offset, 56);
    const auto right = MapStickPosition(analog.rightStickX, analog.rightStickY, 390, 354 + offset, 56);
    Circle(left.x, left.y, 7, Accent); Circle(right.x, right.y, 7, Accent);
    const float barWidth = std::max(1.0f, static_cast<float>(width) - 624);
    Rectangle(584, 314 + offset, barWidth * TriggerFill(analog.leftTrigger), 22, Accent);
    Rectangle(584, 410 + offset, barWidth * TriggerFill(analog.rightTrigger), 22, SecondaryAccent);
}

float Renderer::StatisticsPanel(float y, std::string_view title,
    const std::array<std::string_view, 5>& labels, const std::array<std::string, 5>& values,
    const std::array<bool, 5>& warnings)
{
    const float panelWidth = static_cast<float>(width) - 40;
    Rectangle(20, y, panelWidth, StatisticsPanelHeight, Gray(0.30f));
    Rectangle(21, y + 1, panelWidth - 2, StatisticsPanelHeight - 2, Gray(0.10f));
    Text(32, y + 12, title, Foreground);
    for (std::size_t index = 0; index < labels.size(); ++index) {
        Text(32, y + 44 + static_cast<float>(index) * 28,
            std::format("{} = {}", labels[index], values[index]), warnings[index] ? Accent : Foreground);
    }
    return y + StatisticsPanelHeight + PanelGap;
}

void Renderer::Text(float x, float y, std::string_view text, Color color)
{
    for (unsigned char character : text) {
        if (x + GlyphWidth > static_cast<float>(width) - 16) break;
        if (character < 32 || character > 126) character = '?';
        if (character != ' ') {
            const UINT index = character - 32;
            const float u = static_cast<float>((index % 16) * GlyphWidth) / AtlasWidth;
            const float v = static_cast<float>((index / 16) * GlyphHeight) / AtlasHeight;
            Quad(x, y, GlyphWidth, GlyphHeight, u, v, u + static_cast<float>(GlyphWidth) / AtlasWidth,
                v + static_cast<float>(GlyphHeight) / AtlasHeight, color);
        }
        x += GlyphWidth;
    }
}

void Renderer::BuildDashboard(const MonitorSnapshot& snapshot, std::size_t firstDevice, double framesPerSecond, bool foreground)
{
    vertices.clear();
    float y = 18;
    Text(24, y, "GAMEPAD LATENCY / GAMEINPUT / DIRECTX 12", Accent);
    y += 28;
    Text(24, y, "Latency: GameInput reading to Windows display event (ms).", Muted);
    y += 28;
    Text(24, y, std::format("VSync OFF | Uncapped | GPU 1 | Present 1 | Buffers 2 | Tearing {} | {:.0f} FPS",
        tearing ? "ON" : "unavailable", framesPerSecond), Foreground);
    y += 36;
    const auto& display = snapshot.displayTracking;
    const bool displayIncomplete = display.lostEvents || display.lostBuffers || display.droppedSubmissions
        || display.droppedCompletions || display.droppedMeasurements || display.decoderOverflows;
    const bool displayAvailable = display.active && !display.error;
    y = StatisticsPanel(y, "DISPLAY TRACKING",
        {"Shown frames", "Discarded frames", "Unresolved frames", "Clock errors", "Clock uncertainty"},
        {displayAvailable ? std::to_string(display.displayed) : "--", displayAvailable ? std::to_string(display.discarded) : "--",
            displayAvailable ? std::to_string(display.unresolved) : "--", std::to_string(display.invalidClocks),
            displayAvailable ? std::format("<= {:.1f} us", display.maximumClockUncertainty) : "--"},
        {false, false, false, display.invalidClocks != 0, false});
    const std::string_view displayStatus = !foreground
        ? "UNFOCUSED: measurement statistics pause. Activate this window to measure."
        : display.error || displayIncomplete ? std::string_view(display.status)
        : !display.active ? "Waiting for Windows display events." : "Tracking Windows display events.";
    Text(24, y, displayStatus, foreground && !display.error && !displayIncomplete ? Muted : Accent);
    y += 36;
    y = ControllerPanel(snapshot, y);
    Text(24, y, "Each gamepad: callback delay / late frame sample / DISPLAY event (milliseconds)", Muted);
    y += 28;
    deviceRowsBeginning = y;
    visibleDeviceCount = 0;
    // Leave room for diagnostics and shortcuts following the visible device rows.
    const float deviceRowsEnding = static_cast<float>(height) - StatisticsPanelHeight - 2 * PanelGap - 28;
    for (std::size_t index = firstDevice; index < snapshot.devices.size() && y + 124 <= deviceRowsEnding; ++index) {
        const auto& value = snapshot.devices[index];
        Rectangle(20, y, static_cast<float>(width) - 40, 124, Gray(0.10f));
        Text(32, y + 4, std::format("#{} {} | {} | {:04X}:{:04X} | {}", index, value.kind,
            value.connected ? "connected" : "disconnected", value.vendor, value.product, value.name), Foreground);
        const auto row = [&](const char* name, const StatisticSnapshot& statistic) {
            return statistic.count ? std::format("{} n={} mean={} p95={} p99={} max={}", name, statistic.count,
                Milliseconds(statistic.mean), Milliseconds(statistic.percentile95), Milliseconds(statistic.percentile99), Milliseconds(statistic.maximum))
                : std::format("{} waiting for new input", name);
        };
        Text(32, y + 30, row("Callback", value.callbackDelay), Muted);
        Text(32, y + 56, row("Sample  ", value.sampleDelay), Muted);
        Text(32, y + 82, value.displayDelay.count ? row("Display ", value.displayDelay)
            : snapshot.displayTracking.error ? "Display unavailable: display tracing requires permission." : "Display waiting for new input in a displayed frame", Accent);
        y += 136;
        ++visibleDeviceCount;
    }
    if (snapshot.devices.empty()) {
        Text(32, y + 16, "Waiting for GameInput gamepads. Connect a gamepad.", Foreground);
        y += 52;
    }
    y = StatisticsPanel(y, "INPUT DIAGNOSTICS",
        {"Callback drops", "Frame drops", "Invalid clocks", "Poll errors", "Device errors"},
        {std::to_string(snapshot.droppedCallbacks), std::to_string(snapshot.droppedFrames), std::to_string(snapshot.invalidTimestamps),
            std::to_string(snapshot.pollErrors), std::to_string(snapshot.deviceLimitEvents)},
        {snapshot.droppedCallbacks != 0, snapshot.droppedFrames != 0, snapshot.invalidTimestamps != 0,
            snapshot.pollErrors != 0, snapshot.deviceLimitEvents != 0});
    Text(24, y, snapshot.loggingFailed ? "LOGGING FAILED: inspect storage permissions/free space. Timing is incomplete."
        : "F11 fullscreen | PageUp/PageDown devices | Esc quit | CSV logs saved at exit", snapshot.loggingFailed ? Accent : Muted);
    dashboardVertexCount = vertices.size();
    dashboardDeviceCount = snapshot.devices.size();
}

PresentationResult Renderer::DrawAndPresent(InputMonitor& monitor, std::array<VisualState, MaximumDevices>& states, std::size_t firstDevice)
{
    Check(allocator->Reset(), "Reset frame allocator");
    Check(commands->Reset(allocator.Get(), pipeline.Get()), "Reset frame commands");
    auto handle = targetHeap->GetCPUDescriptorHandleForHeapStart(); handle.ptr += backBuffer * descriptorSize;
    auto barrier = Transition(targets[backBuffer].Get(), D3D12_RESOURCE_STATE_PRESENT, D3D12_RESOURCE_STATE_RENDER_TARGET);
    commands->ResourceBarrier(1, &barrier);
    commands->OMSetRenderTargets(1, &handle, FALSE, nullptr);
    const float background[]{0.04f, 0.04f, 0.04f, 1}; commands->ClearRenderTargetView(handle, background, 0, nullptr);
    const D3D12_VIEWPORT viewport{0, 0, static_cast<float>(width), static_cast<float>(height), 0, 1};
    const D3D12_RECT scissor{0, 0, static_cast<LONG>(width), static_cast<LONG>(height)};
    commands->RSSetViewports(1, &viewport); commands->RSSetScissorRects(1, &scissor);
    commands->SetGraphicsRootSignature(rootSignature.Get());
    const float screen[]{static_cast<float>(width), static_cast<float>(height)};
    commands->SetGraphicsRoot32BitConstants(0, 2, screen, 0);
    ID3D12DescriptorHeap* heaps[]{textureHeap.Get()}; commands->SetDescriptorHeaps(1, heaps);
    commands->SetGraphicsRootDescriptorTable(1, textureHeap->GetGPUDescriptorHandleForHeapStart());
    commands->IASetPrimitiveTopology(D3D_PRIMITIVE_TOPOLOGY_TRIANGLELIST);

    // Read fresh per-device input only after both GPU and DXGI waits, after
    // command setup, and after periodic dashboard formatting. The render path
    // never waits for the asynchronous reading callback or the logging thread.
    vertices.resize(dashboardVertexCount);
    for (auto& state : states) state.visualized = false;
    monitor.SampleLatest(states);
    const auto frameNow = monitor.Now();
    if (selectedController && *selectedController < monitor.DeviceCount()) DrawControllerState(states[*selectedController]);
    else {
        const float offset = controllerPanelBeginning - 142;
        for (const auto position : {StickPosition{120, 418 + offset}, StickPosition{120, 442 + offset}, StickPosition{360, 418 + offset},
            StickPosition{360, 442 + offset}, StickPosition{776, 282 + offset},
            StickPosition{776, 378 + offset}}) Text(position.x, position.y, "--", Muted);
    }
    float y = deviceRowsBeginning;
    const auto count = std::min(monitor.DeviceCount(), dashboardDeviceCount);
    for (std::size_t index = firstDevice; index < count && index - firstDevice < visibleDeviceCount; ++index) {
        auto& state = states[index];
        state.visualized = true;
        const auto now = frameNow;
        const bool recent = state.timestamp && now >= state.timestamp && now - state.timestamp < 80000;
        const Color activity = !state.connected ? Muted : state.active ? SecondaryAccent : recent ? Accent : Gray(0.24f);
        Rectangle(static_cast<float>(width) - 54, y + 34, 18, 64, activity);
        y += 136;
    }
    const auto vertexBytes = vertices.size() * sizeof(DashboardVertex);
    std::memcpy(mappedVertices, vertices.data(), vertexBytes);
    const D3D12_VERTEX_BUFFER_VIEW vertexView{vertexBuffer->GetGPUVirtualAddress(), static_cast<UINT>(vertexBytes), sizeof(DashboardVertex)};
    commands->IASetVertexBuffers(0, 1, &vertexView);
    commands->DrawInstanced(static_cast<UINT>(vertices.size()), 1, 0, 0);
    barrier = Transition(targets[backBuffer].Get(), D3D12_RESOURCE_STATE_RENDER_TARGET, D3D12_RESOURCE_STATE_PRESENT);
    commands->ResourceBarrier(1, &barrier);
    Check(commands->Close(), "Close frame commands");
    ID3D12CommandList* lists[]{commands.Get()}; queue->ExecuteCommandLists(1, lists);
    LARGE_INTEGER firstQpc{}, lastQpc{}, endQpc{};
    QueryPerformanceCounter(&firstQpc);
    const auto beginning = monitor.Now();
    QueryPerformanceCounter(&lastQpc);
    const auto result = swapChain->Present(0, tearing ? DXGI_PRESENT_ALLOW_TEARING : 0);
    const auto ending = monitor.Now();
    QueryPerformanceCounter(&endQpc);
    if (FAILED(result)) {
        if (result == DXGI_ERROR_DEVICE_REMOVED || result == DXGI_ERROR_DEVICE_RESET) Check(device->GetDeviceRemovedReason(), "GPU device removed");
        Check(result, "Present");
    }
    lastFence = nextFence++;
    Check(queue->Signal(fence.Get(), lastFence), "Signal frame completion");
    presentationAdmitted = false;
    return {beginning, ending, result == S_OK, static_cast<std::uint64_t>(firstQpc.QuadPart),
        static_cast<std::uint64_t>(lastQpc.QuadPart), static_cast<std::uint64_t>(endQpc.QuadPart),
        reinterpret_cast<std::uint64_t>(swapChain.Get()), GetCurrentThreadId()};
}

std::string Renderer::Description() const
{
    return std::format("Adapter={}\nVSync=Off\nFrameLimiter=None\nGpuFramesInFlight=1\nMaximumPresentLatency=1\nBackBufferCount=2\nWaitStrategy=Spin\nTearing={}\n", adapterName, tearing ? "Enabled" : "Unsupported");
}

void Renderer::VerifyDebugMessages() const
{
    ComPtr<ID3D12InfoQueue> information;
    if (FAILED(device.As(&information))) return;
    for (UINT64 index = 0; index < information->GetNumStoredMessagesAllowedByRetrievalFilter(); ++index) {
        SIZE_T bytes{};
        Check(information->GetMessage(index, nullptr, &bytes), "Read debug message size");
        std::vector<std::byte> storage(bytes);
        auto* message = reinterpret_cast<D3D12_MESSAGE*>(storage.data());
        Check(information->GetMessage(index, message, &bytes), "Read debug message");
        if (message->Severity == D3D12_MESSAGE_SEVERITY_ERROR || message->Severity == D3D12_MESSAGE_SEVERITY_CORRUPTION) {
            throw std::runtime_error(std::string("DirectX 12 validation: ") + message->pDescription);
        }
    }
}

void Renderer::SaveLastFrame(const std::filesystem::path& path)
{
    // Optional verification capture runs after measurement stops. It adds no
    // GPU readbacks or CPU waits to ordinary measurement sessions.
    WaitForGpu();
    const auto description = targets[backBuffer]->GetDesc();
    D3D12_PLACED_SUBRESOURCE_FOOTPRINT footprint{};
    UINT64 totalBytes{};
    device->GetCopyableFootprints(&description, 0, 1, 0, &footprint, nullptr, nullptr, &totalBytes);
    D3D12_HEAP_PROPERTIES heap{}; heap.Type = D3D12_HEAP_TYPE_READBACK;
    D3D12_RESOURCE_DESC buffer{};
    buffer.Dimension = D3D12_RESOURCE_DIMENSION_BUFFER;
    buffer.Width = totalBytes; buffer.Height = 1; buffer.DepthOrArraySize = 1;
    buffer.MipLevels = 1; buffer.SampleDesc.Count = 1; buffer.Layout = D3D12_TEXTURE_LAYOUT_ROW_MAJOR;
    ComPtr<ID3D12Resource> readback;
    Check(device->CreateCommittedResource(&heap, D3D12_HEAP_FLAG_NONE, &buffer,
        D3D12_RESOURCE_STATE_COPY_DEST, nullptr, IID_PPV_ARGS(&readback)), "Create verification readback");
    Check(allocator->Reset(), "Reset verification allocator");
    Check(commands->Reset(allocator.Get(), nullptr), "Reset verification commands");
    auto barrier = Transition(targets[backBuffer].Get(), D3D12_RESOURCE_STATE_PRESENT, D3D12_RESOURCE_STATE_COPY_SOURCE);
    commands->ResourceBarrier(1, &barrier);
    D3D12_TEXTURE_COPY_LOCATION destination{};
    destination.pResource = readback.Get(); destination.Type = D3D12_TEXTURE_COPY_TYPE_PLACED_FOOTPRINT;
    destination.PlacedFootprint = footprint;
    D3D12_TEXTURE_COPY_LOCATION source{};
    source.pResource = targets[backBuffer].Get(); source.Type = D3D12_TEXTURE_COPY_TYPE_SUBRESOURCE_INDEX;
    commands->CopyTextureRegion(&destination, 0, 0, 0, &source, nullptr);
    barrier = Transition(targets[backBuffer].Get(), D3D12_RESOURCE_STATE_COPY_SOURCE, D3D12_RESOURCE_STATE_PRESENT);
    commands->ResourceBarrier(1, &barrier);
    Check(commands->Close(), "Close verification commands");
    ID3D12CommandList* lists[]{commands.Get()}; queue->ExecuteCommandLists(1, lists); WaitForGpu();

    const auto initialized = CoInitializeEx(nullptr, COINIT_MULTITHREADED);
    if (FAILED(initialized) && initialized != RPC_E_CHANGED_MODE) Check(initialized, "Initialize PNG encoder COM");
    struct ComScope { bool initialized; ~ComScope() { if (initialized) CoUninitialize(); } } comScope{SUCCEEDED(initialized)};
    ComPtr<IWICImagingFactory> imaging;
    Check(CoCreateInstance(CLSID_WICImagingFactory, nullptr, CLSCTX_INPROC_SERVER, IID_PPV_ARGS(&imaging)), "Create PNG encoder factory");
    ComPtr<IWICStream> stream;
    Check(imaging->CreateStream(&stream), "Create PNG stream");
    Check(stream->InitializeFromFilename(path.c_str(), GENERIC_WRITE), "Create PNG file");
    ComPtr<IWICBitmapEncoder> encoder;
    Check(imaging->CreateEncoder(GUID_ContainerFormatPng, nullptr, &encoder), "Create PNG encoder");
    Check(encoder->Initialize(stream.Get(), WICBitmapEncoderNoCache), "Initialize PNG encoder");
    ComPtr<IWICBitmapFrameEncode> frame;
    Check(encoder->CreateNewFrame(&frame, nullptr), "Create PNG frame");
    Check(frame->Initialize(nullptr), "Initialize PNG frame");
    Check(frame->SetSize(width, height), "Set PNG dimensions");
    auto pixelFormat = GUID_WICPixelFormat32bppRGBA;
    Check(frame->SetPixelFormat(&pixelFormat), "Set PNG pixel format");
    const D3D12_RANGE range{static_cast<SIZE_T>(footprint.Offset), static_cast<SIZE_T>(totalBytes)};
    std::byte* pixels{};
    Check(readback->Map(0, &range, reinterpret_cast<void**>(&pixels)), "Map verification pixels");
    ComPtr<IWICBitmap> bitmap;
    const auto result = imaging->CreateBitmapFromMemory(width, height, GUID_WICPixelFormat32bppRGBA,
        footprint.Footprint.RowPitch, footprint.Footprint.RowPitch * height,
        reinterpret_cast<BYTE*>(pixels + footprint.Offset), &bitmap);
    const D3D12_RANGE noWrites{0, 0}; readback->Unmap(0, &noWrites);
    Check(result, "Create verification bitmap");
    ComPtr<IWICFormatConverter> converter;
    Check(imaging->CreateFormatConverter(&converter), "Create PNG pixel converter");
    Check(converter->Initialize(bitmap.Get(), pixelFormat, WICBitmapDitherTypeNone, nullptr, 0,
        WICBitmapPaletteTypeCustom), "Convert PNG pixel format");
    Check(frame->WriteSource(converter.Get(), nullptr), "Write PNG pixels");
    Check(frame->Commit(), "Commit PNG frame"); Check(encoder->Commit(), "Commit PNG encoder");
}
