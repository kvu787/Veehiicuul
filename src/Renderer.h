#pragma once

#include "OrthographicTransforms.h"
#include <Windows.h>
#include <d3d12.h>
#include <dxgi1_6.h>
#include <wrl/client.h>

#include <array>
#include <chrono>
#include <cstddef>
#include <cstdint>

class Renderer final
{
public:
    Renderer() = default;
    ~Renderer();

    Renderer(const Renderer&) = delete;
    Renderer& operator=(const Renderer&) = delete;

    void Initialize(HWND window, std::uint32_t width, std::uint32_t height);
    void Resize(std::uint32_t width, std::uint32_t height);
    void Render();

    void SetVsyncEnabled(bool enabled) noexcept { m_vsyncEnabled = enabled; }
    [[nodiscard]] bool IsVsyncEnabled() const noexcept { return m_vsyncEnabled; }
    [[nodiscard]] bool IsInitialized() const noexcept { return m_initialized; }

private:
    static constexpr std::uint32_t FrameCount = 2;
    static constexpr std::uint32_t CarMaterialCount = 5;
    static constexpr std::uint32_t PaintMaterialCount = CarMaterialCount + 1;
    static constexpr std::uint32_t ObjectsPerFrame = 2;
    static constexpr float BackgroundAspectRatio = 32.0f / 9.0f;
    static constexpr DXGI_FORMAT SwapChainFormat = DXGI_FORMAT_R8G8B8A8_UNORM;
    static constexpr DXGI_FORMAT RenderTargetFormat = DXGI_FORMAT_R8G8B8A8_UNORM_SRGB;
    static constexpr DXGI_FORMAT DepthBufferFormat = DXGI_FORMAT_D32_FLOAT;

    struct PaintMaterialConstants
    {
        DirectX::XMFLOAT4 paintWarp;
        DirectX::XMFLOAT4 paintTone;
        DirectX::XMFLOAT4 k1;
        DirectX::XMFLOAT4 k2;
        DirectX::XMFLOAT4 k3;
    };

    static constexpr std::uint32_t ObjectConstantStride =
        D3D12_CONSTANT_BUFFER_DATA_PLACEMENT_ALIGNMENT;
    static_assert(sizeof(Orthographic::ObjectTransforms) <= ObjectConstantStride);
    static_assert(sizeof(PaintMaterialConstants) == 80);
    static constexpr std::uint32_t MaterialConstantOffset =
        ObjectConstantStride * ObjectsPerFrame * FrameCount;
    static constexpr std::uint32_t MaterialConstantSize =
        (sizeof(PaintMaterialConstants) * PaintMaterialCount + ObjectConstantStride - 1) /
        ObjectConstantStride * ObjectConstantStride;

    struct PaintSettings
    {
        DirectX::XMFLOAT3 baseColorSrgb{};
        float brightness = 0.5f;
        float shift = 0.0f;
        float rotationDegrees = 0.0f;
        float darkPoint = 0.0f;
        float lightPoint = 0.8f;
    };

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
    void LoadPaintSettings();

    void UpdateCamera();
    [[nodiscard]] AnimationState CurrentAnimationState() const;
    void WriteObjectConstants(std::uint32_t frameIndex, std::uint32_t objectIndex, DirectX::FXMMATRIX world);
    void DrawBackground();
    void DrawObjects(std::uint32_t frameIndex);

    void WaitForFrame(std::uint32_t frameIndex);
    void WaitForGpu();
    void SignalFrame(std::uint32_t frameIndex);

    HWND m_window = nullptr;
    std::uint32_t m_width = 0;
    std::uint32_t m_height = 0;
    bool m_initialized = false;
    bool m_vsyncEnabled = false;
    bool m_tearingSupported = false;

    Microsoft::WRL::ComPtr<IDXGIFactory4> m_factory;
    Microsoft::WRL::ComPtr<ID3D12Device> m_device;
    Microsoft::WRL::ComPtr<ID3D12CommandQueue> m_commandQueue;
    Microsoft::WRL::ComPtr<IDXGISwapChain3> m_swapChain;
    Microsoft::WRL::ComPtr<ID3D12RootSignature> m_backgroundRootSignature;
    Microsoft::WRL::ComPtr<ID3D12RootSignature> m_carRootSignature;
    Microsoft::WRL::ComPtr<ID3D12PipelineState> m_backgroundPipelineState;
    Microsoft::WRL::ComPtr<ID3D12PipelineState> m_carPipelineState;

    std::array<Microsoft::WRL::ComPtr<ID3D12CommandAllocator>, FrameCount> m_commandAllocators;
    Microsoft::WRL::ComPtr<ID3D12GraphicsCommandList> m_commandList;

    Microsoft::WRL::ComPtr<ID3D12DescriptorHeap> m_rtvHeap;
    Microsoft::WRL::ComPtr<ID3D12DescriptorHeap> m_dsvHeap;
    Microsoft::WRL::ComPtr<ID3D12DescriptorHeap> m_srvHeap;
    std::uint32_t m_rtvDescriptorSize = 0;
    std::array<Microsoft::WRL::ComPtr<ID3D12Resource>, FrameCount> m_renderTargets;
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

    Microsoft::WRL::ComPtr<ID3D12Fence> m_fence;
    std::array<std::uint64_t, FrameCount> m_frameFenceValues{};
    std::uint64_t m_nextFenceValue = 1;
    HANDLE m_fenceEvent = nullptr;

    std::chrono::steady_clock::time_point m_animationStart{};
};
