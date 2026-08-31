#pragma once

#include "Geometry.h"

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
    static constexpr std::uint32_t SceneObjectCount = 3;
    static constexpr DXGI_FORMAT BackBufferFormat = DXGI_FORMAT_R8G8B8A8_UNORM;
    static constexpr DXGI_FORMAT DepthBufferFormat = DXGI_FORMAT_D32_FLOAT;

    struct ObjectConstants
    {
        DirectX::XMFLOAT4X4 worldViewProjection;
        DirectX::XMFLOAT4X4 world;
        DirectX::XMFLOAT4 baseColor;
        DirectX::XMFLOAT4 lighting;
        std::array<std::byte, 96> padding{};
    };

    static_assert(sizeof(ObjectConstants) == 256);

    struct GpuMesh
    {
        Microsoft::WRL::ComPtr<ID3D12Resource> vertexBuffer;
        Microsoft::WRL::ComPtr<ID3D12Resource> indexBuffer;
        D3D12_VERTEX_BUFFER_VIEW vertexView{};
        D3D12_INDEX_BUFFER_VIEW indexView{};
        std::uint32_t indexCount = 0;
    };

    void CreateDevice();
    void CreateSwapChain();
    void CreateDescriptorHeaps();
    void CreatePipeline();
    void CreateCommandObjects();
    void CreateWindowSizeResources();
    void CreateMeshes();
    void CreateConstantBuffer();

    [[nodiscard]] GpuMesh UploadMesh(const MeshData& mesh, const wchar_t* debugName) const;
    void WriteObjectConstants(
        std::uint32_t frameIndex,
        std::uint32_t objectIndex,
        DirectX::FXMMATRIX world,
        const DirectX::XMFLOAT4& color,
        bool unlit);
    void DrawMesh(const GpuMesh& mesh, std::uint32_t frameIndex, std::uint32_t objectIndex);
    void UpdateCamera();
    [[nodiscard]] float SpherePosition() const;

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
    Microsoft::WRL::ComPtr<ID3D12RootSignature> m_rootSignature;
    Microsoft::WRL::ComPtr<ID3D12PipelineState> m_pipelineState;

    std::array<Microsoft::WRL::ComPtr<ID3D12CommandAllocator>, FrameCount> m_commandAllocators;
    Microsoft::WRL::ComPtr<ID3D12GraphicsCommandList> m_commandList;

    Microsoft::WRL::ComPtr<ID3D12DescriptorHeap> m_rtvHeap;
    Microsoft::WRL::ComPtr<ID3D12DescriptorHeap> m_dsvHeap;
    std::uint32_t m_rtvDescriptorSize = 0;
    std::array<Microsoft::WRL::ComPtr<ID3D12Resource>, FrameCount> m_renderTargets;
    Microsoft::WRL::ComPtr<ID3D12Resource> m_depthBuffer;

    D3D12_VIEWPORT m_viewport{};
    D3D12_RECT m_scissorRect{};
    DirectX::XMFLOAT4X4 m_view{};
    DirectX::XMFLOAT4X4 m_projection{};

    GpuMesh m_groundMesh;
    GpuMesh m_cubeMesh;
    GpuMesh m_sphereMesh;

    Microsoft::WRL::ComPtr<ID3D12Resource> m_constantBuffer;
    std::byte* m_mappedConstants = nullptr;

    Microsoft::WRL::ComPtr<ID3D12Fence> m_fence;
    std::array<std::uint64_t, FrameCount> m_frameFenceValues{};
    std::uint64_t m_nextFenceValue = 1;
    HANDLE m_fenceEvent = nullptr;

    std::chrono::steady_clock::time_point m_animationStart{};
};
