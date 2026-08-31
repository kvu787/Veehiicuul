#pragma once

#include <DirectXMath.h>
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
    static constexpr float BackgroundAspectRatio = 32.0f / 9.0f;
    static constexpr DXGI_FORMAT SwapChainFormat = DXGI_FORMAT_R8G8B8A8_UNORM;
    static constexpr DXGI_FORMAT RenderTargetFormat = DXGI_FORMAT_R8G8B8A8_UNORM_SRGB;
    static constexpr DXGI_FORMAT DepthBufferFormat = DXGI_FORMAT_D32_FLOAT;

    struct PaintMaterialConstants
    {
        DirectX::XMFLOAT4 k1;
        DirectX::XMFLOAT4 k2;
        DirectX::XMFLOAT4 k3;
    };

    struct CarConstants
    {
        DirectX::XMFLOAT4X4 worldViewProjection;
        DirectX::XMFLOAT4X4 worldView;
        DirectX::XMFLOAT4 paintWarp;
        DirectX::XMFLOAT4 paintTone;
        std::array<PaintMaterialConstants, CarMaterialCount> paintMaterials;
        std::array<std::byte, 112> padding{};
    };

    static_assert(sizeof(CarConstants) == 512);

    struct PaintSettings
    {
        float brightness = 0.20136449f;
        float shift = 0.0f;
        float rotationDegrees = 0.0f;
        float darkPoint = 0.0f;
        float lightPoint = 1.0f;
        float facingCutoff = 0.01f;
        std::array<DirectX::XMFLOAT3, CarMaterialCount> baseColorsSrgb{};
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
    void WriteCarConstants(std::uint32_t frameIndex, DirectX::FXMMATRIX world);
    void DrawBackground();
    void DrawCar(std::uint32_t frameIndex);

    void WaitForFrame(std::uint32_t frameIndex);
    void WaitForGpu();
    void SignalFrame(std::uint32_t frameIndex);

    HWND m_window = nullptr;
    std::uint32_t m_width = 0;
    std::uint32_t m_height = 0;
    bool m_initialized = false;
    bool m_vsyncEnabled = true;
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
    DirectX::XMFLOAT4X4 m_projection{};

    GpuMesh m_carMesh;
    DirectX::XMFLOAT4 m_paintWarp{};
    DirectX::XMFLOAT4 m_paintTone{};
    std::array<PaintMaterialConstants, CarMaterialCount> m_paintMaterials{};

    Microsoft::WRL::ComPtr<ID3D12Resource> m_constantBuffer;
    std::byte* m_mappedConstants = nullptr;

    Microsoft::WRL::ComPtr<ID3D12Fence> m_fence;
    std::array<std::uint64_t, FrameCount> m_frameFenceValues{};
    std::uint64_t m_nextFenceValue = 1;
    HANDLE m_fenceEvent = nullptr;

    std::chrono::steady_clock::time_point m_animationStart{};
};
