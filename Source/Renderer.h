#pragma once

#include "SimplePaint/OrthographicTransforms.h"
#include "Settings.h"
#include "RenderPreparation.h"
#include <Windows.h>
#include <d3d12.h>
#include <dxgi1_6.h>
#include <wrl/client.h>

#include <array>
#include <chrono>
#include <cstddef>
#include <cstdint>
#include <functional>
#include <string>
#include <vector>

class Renderer final
{
public:
    Renderer() = default;
    ~Renderer();

    Renderer(const Renderer&) = delete;
    Renderer& operator=(const Renderer&) = delete;

    void Initialize(HWND window, std::uint32_t width, std::uint32_t height,
        const Settings& settings, bool useSoftwareAdapter = false);
    // Service messages without mutating renderer resources; return false to cancel the attempt.
    [[nodiscard]] bool PrepareFrame(const std::function<bool()>& serviceMessages);
    [[nodiscard]] std::wstring RenderPipelineDescription() const;
    [[nodiscard]] std::uint32_t PendingGpuFrames() const;
    void CheckDebugMessages() const;
    void Resize(std::uint32_t width, std::uint32_t height);
    void Render();

    void SetVsyncEnabled(bool enabled) noexcept { m_vsyncEnabled = enabled; }
    [[nodiscard]] bool IsVsyncEnabled() const noexcept { return m_vsyncEnabled; }
    [[nodiscard]] bool IsInitialized() const noexcept { return m_initialized; }

private:
    friend struct RendererTestAccess;
    static constexpr std::uint32_t CarMaterialCount = 5;
    static constexpr std::uint32_t PaintMaterialCount = CarMaterialCount + 1;
    static_assert(PaintMaterialCount == SIMPLE_PAINT_MATERIAL_COUNT);
    static constexpr std::uint32_t ObjectsPerFrame = 2;
    static constexpr float BackgroundAspectRatio = 32.0f / 9.0f;
    static constexpr DXGI_FORMAT SwapChainFormat = DXGI_FORMAT_R8G8B8A8_UNORM;
    static constexpr DXGI_FORMAT RenderTargetFormat = DXGI_FORMAT_R8G8B8A8_UNORM_SRGB;
    static constexpr DXGI_FORMAT DepthBufferFormat = DXGI_FORMAT_D32_FLOAT;

    using PaintMaterialConstants = SimplePaint::GpuMaterial;

    static constexpr std::uint32_t ObjectConstantStride =
        D3D12_CONSTANT_BUFFER_DATA_PLACEMENT_ALIGNMENT;
    static_assert(sizeof(Orthographic::ObjectTransforms) <= ObjectConstantStride);
    static_assert(sizeof(PaintMaterialConstants) == 80);
    static constexpr std::uint32_t MaterialConstantSize =
        (sizeof(PaintMaterialConstants) * PaintMaterialCount + ObjectConstantStride - 1) /
        ObjectConstantStride * ObjectConstantStride;

    struct GpuMesh
    {
        Microsoft::WRL::ComPtr<ID3D12Resource> vertexBuffer;
        Microsoft::WRL::ComPtr<ID3D12Resource> indexBuffer;
        D3D12_VERTEX_BUFFER_VIEW vertexView{};
        D3D12_INDEX_BUFFER_VIEW indexView{};
        std::uint32_t indexCount = 0;
    };

    struct AnimationState
    {
        float position = 0.0f;
        float rotation = 0.0f;
    };

    void CreateDevice();
    void CreateSwapChain();
    void CreateDescriptorHeaps();
    void CreatePipelines();
    void CreateCommandObjects();
    void CreateWindowSizeResources();
    void CreateStaticResources();
    void CreateConstantBuffer();

    void UpdateCamera();
    [[nodiscard]] AnimationState CurrentAnimationState() const;
    void WriteObjectConstants(std::uint32_t frameIndex, std::uint32_t objectIndex, DirectX::FXMMATRIX world);
    void DrawBackground();
    void DrawObjects(std::uint32_t frameIndex);

    void WaitForGpu();
    void SignalFrame();
    [[nodiscard]] std::uint64_t CompletedFence() const;

    HWND m_window = nullptr;
    std::uint32_t m_width = 0;
    std::uint32_t m_height = 0;
    bool m_initialized = false;
    bool m_vsyncEnabled = false;
    bool m_tearingSupported = false;
    bool m_useSoftwareAdapter = false;
    ResolvedRenderPipeline m_renderPipeline = StandardRenderPipeline;
    UINT m_swapChainFlags = 0;
    HANDLE m_presentationEvent = nullptr;
    bool m_presentationAdmitted = false;
    bool m_framePrepared = false;
    std::uint64_t m_frameSequence = 0;
    std::uint32_t m_frameIndex = 0;
    std::uint32_t m_backBufferIndex = 0;
    std::uint64_t m_waitFenceValue = 0;

    Microsoft::WRL::ComPtr<IDXGIFactory4> m_factory;
    Microsoft::WRL::ComPtr<ID3D12Device> m_device;
    Microsoft::WRL::ComPtr<ID3D12CommandQueue> m_commandQueue;
    Microsoft::WRL::ComPtr<IDXGISwapChain3> m_swapChain;
    Microsoft::WRL::ComPtr<ID3D12RootSignature> m_backgroundRootSignature;
    Microsoft::WRL::ComPtr<ID3D12RootSignature> m_carRootSignature;
    Microsoft::WRL::ComPtr<ID3D12PipelineState> m_backgroundPipelineState;
    Microsoft::WRL::ComPtr<ID3D12PipelineState> m_carPipelineState;

    struct FrameContext
    {
        Microsoft::WRL::ComPtr<ID3D12CommandAllocator> allocator;
        std::uint64_t fenceValue = 0;
    };
    std::vector<FrameContext> m_frames;
    Microsoft::WRL::ComPtr<ID3D12GraphicsCommandList> m_commandList;

    Microsoft::WRL::ComPtr<ID3D12DescriptorHeap> m_rtvHeap;
    Microsoft::WRL::ComPtr<ID3D12DescriptorHeap> m_dsvHeap;
    Microsoft::WRL::ComPtr<ID3D12DescriptorHeap> m_srvHeap;
    std::uint32_t m_rtvDescriptorSize = 0;
    std::vector<Microsoft::WRL::ComPtr<ID3D12Resource>> m_renderTargets;
    std::vector<std::uint64_t> m_backBufferFences;
    Microsoft::WRL::ComPtr<ID3D12Resource> m_depthBuffer;
    Microsoft::WRL::ComPtr<ID3D12Resource> m_backgroundTexture;

    D3D12_VIEWPORT m_viewport{};
    D3D12_VIEWPORT m_sceneViewport{};
    D3D12_RECT m_scissorRect{};
    DirectX::XMFLOAT4X4 m_view{};
    Orthographic::Projection m_orthographicProjection{};

    GpuMesh m_sceneMesh;
    std::uint32_t m_sphereUResolution = 64;
    std::uint32_t m_sphereVResolution = 32;
    std::uint32_t m_carIndexCount = 0;
    std::array<PaintMaterialConstants, PaintMaterialCount> m_paintMaterials{};

    Microsoft::WRL::ComPtr<ID3D12Resource> m_constantBuffer;
    std::byte* m_mappedConstants = nullptr;
    std::uint32_t m_materialConstantOffset = 0;

    Microsoft::WRL::ComPtr<ID3D12Fence> m_fence;
    std::uint64_t m_nextFenceValue = 1;
    HANDLE m_fenceEvent = nullptr;

    std::chrono::steady_clock::time_point m_animationStart{};
};
