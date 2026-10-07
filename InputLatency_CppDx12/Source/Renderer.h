#pragma once

#include "InputMonitor.h"

#include <d3d12.h>
#include <dxgi1_6.h>
#include <functional>
#include <string_view>

struct Color { float red{}, green{}, blue{}, alpha{1}; };
struct DashboardVertex { float x{}, y{}, u{}, v{}; Color color; };
struct PresentationResult
{
    std::uint64_t beginning{}, ending{};
    bool accepted{};
    std::uint64_t firstQpc{}, lastQpc{}, endQpc{}, swapChain{};
    std::uint32_t thread{};
};

class Renderer final
{
public:
    ~Renderer();
    void Initialize(HWND window, std::uint32_t width, std::uint32_t height, bool softwareAdapter);
    bool PrepareFrame(const std::function<bool()>& serviceMessages);
    void Resize(std::uint32_t width, std::uint32_t height);
    void BuildDashboard(const MonitorSnapshot& snapshot, std::size_t firstDevice, double framesPerSecond, bool foreground);
    PresentationResult DrawAndPresent(InputMonitor& monitor, std::array<VisualState, MaximumDevices>& states,
        std::size_t firstDevice);
    std::string Description() const;
    void VerifyDebugMessages() const;
    void SaveLastFrame(const std::filesystem::path& path);
    std::optional<std::size_t> SelectedController() const noexcept { return selectedController; }
private:
    void CreateTargets();
    void CreatePipelineAndAtlas();
    void WaitForGpu();
    void Rectangle(float x, float y, float width, float height, Color color);
    float StatisticsPanel(float y, std::string_view title,
        const std::array<std::string_view, 5>& labels, const std::array<std::string, 5>& values,
        const std::array<bool, 5>& warnings);
    void Circle(float x, float y, float radius, Color color);
    void Triangle(float firstX, float firstY, float secondX, float secondY, float thirdX, float thirdY, Color color);
    float ControllerPanel(const MonitorSnapshot& snapshot, float y);
    void DrawControllerState(VisualState& state);
    void Text(float x, float y, std::string_view text, Color color);
    void Quad(float x, float y, float width, float height, float u0, float v0, float u1, float v1, Color color);
    Microsoft::WRL::ComPtr<IDXGIFactory6> factory;
    Microsoft::WRL::ComPtr<ID3D12Device> device;
    Microsoft::WRL::ComPtr<ID3D12CommandQueue> queue;
    Microsoft::WRL::ComPtr<IDXGISwapChain3> swapChain;
    Microsoft::WRL::ComPtr<ID3D12DescriptorHeap> targetHeap, textureHeap;
    std::array<Microsoft::WRL::ComPtr<ID3D12Resource>, 2> targets;
    Microsoft::WRL::ComPtr<ID3D12CommandAllocator> allocator;
    Microsoft::WRL::ComPtr<ID3D12GraphicsCommandList> commands;
    Microsoft::WRL::ComPtr<ID3D12RootSignature> rootSignature;
    Microsoft::WRL::ComPtr<ID3D12PipelineState> pipeline;
    Microsoft::WRL::ComPtr<ID3D12Resource> atlas, vertexBuffer;
    Microsoft::WRL::ComPtr<ID3D12Fence> fence;
    HANDLE presentationEvent{}, fenceEvent{};
    std::uint64_t lastFence{}, nextFence{1};
    UINT descriptorSize{}, backBuffer{};
    std::uint32_t width{}, height{}, swapChainFlags{};
    bool tearing{}, presentationAdmitted{};
    std::string adapterName;
    DashboardVertex* mappedVertices{};
    std::vector<DashboardVertex> vertices;
    std::size_t dashboardVertexCount{};
    std::size_t dashboardDeviceCount{};
    std::optional<std::size_t> selectedController;
    float deviceRowsBeginning{}, controllerPanelBeginning{};
    std::size_t visibleDeviceCount{};
    static constexpr std::size_t VertexCapacity = 131072;
};
