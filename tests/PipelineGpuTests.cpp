#include "Renderer.h"
#include <chrono>
#include <iostream>
#include <sstream>
#include <stdexcept>
#include <thread>

struct RendererTestAccess
{
    static Microsoft::WRL::ComPtr<ID3D12Fence> BlockQueue(Renderer& renderer)
    {
        Microsoft::WRL::ComPtr<ID3D12Fence> gate;
        if (FAILED(renderer.m_device->CreateFence(0, D3D12_FENCE_FLAG_NONE, IID_PPV_ARGS(&gate))) ||
            FAILED(renderer.m_commandQueue->Wait(gate.Get(), 1))) throw std::runtime_error("Cannot gate test GPU queue");
        return gate;
    }
    static void Flush(Renderer& renderer) { renderer.WaitForGpu(); }
    static bool HasPresentationWait(const Renderer& renderer) { return renderer.m_presentationEvent != nullptr; }
};
namespace
{
constexpr UINT CancelMessage = WM_APP + 1;
bool cancelled = false;
LRESULT CALLBACK WindowProc(HWND window, UINT message, WPARAM w, LPARAM l)
{
    if (message == CancelMessage) { cancelled = true; return 0; }
    return DefWindowProcW(window, message, w, l);
}
bool Pump()
{
    MSG message{};
    for (int i = 0; i < 64 && PeekMessageW(&message, nullptr, 0, 0, PM_REMOVE); ++i)
    {
        TranslateMessage(&message);
        DispatchMessageW(&message);
    }
    return !cancelled;
}
void Require(bool condition, const char* reason) { if (!condition) throw std::runtime_error(reason); }
struct Window
{
    HWND handle = nullptr;
    ~Window() { if (handle) DestroyWindow(handle); }
};
}
int main(int argc, char** argv)
{
    try
    {
        const bool warp = argc > 1 && std::string_view(argv[1]) == "--warp";
        Microsoft::WRL::ComPtr<ID3D12Debug> debug;
        Require(SUCCEEDED(D3D12GetDebugInterface(IID_PPV_ARGS(&debug))), "D3D12 debug layer required for pipeline tests");
        debug->EnableDebugLayer();
        WNDCLASSW wc{.lpfnWndProc = WindowProc, .hInstance = GetModuleHandleW(nullptr), .lpszClassName = L"PipelineTests"};
        RegisterClassW(&wc);
        Window window{.handle = CreateWindowW(wc.lpszClassName, L"Pipeline synchronization tests", WS_OVERLAPPEDWINDOW,
            30, 30, 360, 220, nullptr, nullptr, wc.hInstance, nullptr)};
        Require(window.handle != nullptr, "Cannot create test window");
        ShowWindow(window.handle, SW_SHOWNOACTIVATE);
        std::istringstream ini("[Rendering]\nVSync=false\n[Pipeline]\nMode=Standard\n");
        auto settings = ParseApplicationSettings(ini);
        const PipelineSettings configurations[] = {
            MinimumLatencyPipeline, StandardPipeline, MaximizeFpsPipeline,
            {.maxGpuFramesInFlight=3, .maxPresentLatency=1, .waitForPresentation=false, .backBufferCount=2, .allowTearing=true, .waitStrategy=WaitStrategy::Spin},
            {.maxGpuFramesInFlight=1, .maxPresentLatency=2, .waitForPresentation=true, .backBufferCount=3, .allowTearing=true, .waitStrategy=WaitStrategy::Event},
            {.maxGpuFramesInFlight=1, .maxPresentLatency=1, .waitForPresentation=false, .backBufferCount=2, .allowTearing=false, .waitStrategy=WaitStrategy::Event},
            {.maxGpuFramesInFlight=16, .maxPresentLatency=16, .waitForPresentation=true, .backBufferCount=16, .allowTearing=true, .waitStrategy=WaitStrategy::Event},
        };
        for (const auto& pipeline : configurations)
        {
            settings.pipeline = pipeline;
            settings.pipelineMode = pipeline == MinimumLatencyPipeline ? PipelineMode::MinimizeInputLatency :
                pipeline == StandardPipeline ? PipelineMode::Standard :
                pipeline == MaximizeFpsPipeline ? PipelineMode::MaximizeFps : PipelineMode::Custom;
            settings.vsync = pipeline.waitStrategy == WaitStrategy::Event;
            Renderer renderer;
            renderer.Initialize(window.handle, 320, 180, settings, warp);
            Require(renderer.IsVsyncEnabled() == settings.vsync, "INI VSync ignored during initialization");
            Require(RendererTestAccess::HasPresentationWait(renderer) == pipeline.waitForPresentation,
                "Swap-chain presentation wait does not match the selected pipeline");
            for (int frame = 0; frame < 24; ++frame)
            {
                if (frame == 6) renderer.SetVsyncEnabled(true);
                if (frame == 12) renderer.SetVsyncEnabled(false);
                Require(renderer.PrepareFrame(Pump), "Unexpected preparation cancellation");
                Require(renderer.PendingGpuFrames() < pipeline.maxGpuFramesInFlight, "GPU budget exceeded at admission");
                renderer.Render();
                Require(renderer.PendingGpuFrames() <= pipeline.maxGpuFramesInFlight, "GPU budget exceeded after submission");
                if (frame == 8)
                {
                    // Cancel after an admission permit may have been consumed, then resize and resume.
                    int callbacks = 0;
                    Require(!renderer.PrepareFrame([&] { return ++callbacks < 2; }), "Cancellation ignored");
                    renderer.Resize(300, 160);
                }
                if (frame == 16)
                {
                    ShowWindow(window.handle, SW_MINIMIZE);
                    Pump();
                    ShowWindow(window.handle, SW_SHOWNOACTIVATE);
                    Pump();
                    renderer.Resize(320, 180);
                }
            }
            RendererTestAccess::Flush(renderer);
            renderer.CheckDebugMessages();
            std::wcout << renderer.PipelineDescription() << L" passed\n";
        }
        for (const auto strategy : {WaitStrategy::Event, WaitStrategy::Spin})
        {
            settings.pipeline = MinimumLatencyPipeline;
            settings.pipeline.waitStrategy = strategy;
            Renderer renderer;
            renderer.Initialize(window.handle, 320, 180, settings, warp);
            Require(renderer.PrepareFrame(Pump), "Initial admission failed");
            auto gate = RendererTestAccess::BlockQueue(renderer);
            renderer.Render();
            // The delay drives a cancellation message while the GPU is blocked, not an FPS limit.
            std::jthread sender([&] {
                std::this_thread::sleep_for(std::chrono::milliseconds(20));
                PostMessageW(window.handle, CancelMessage, 0, 0);
                // Fail-safe releases GPU resources even if message-aware waiting regresses.
                std::this_thread::sleep_for(std::chrono::milliseconds(200));
                gate->Signal(1);
            });
            const auto start = std::chrono::steady_clock::now();
            const bool admitted = renderer.PrepareFrame(Pump);
            const auto elapsed = std::chrono::steady_clock::now() - start;
            gate->Signal(1);
            sender.join();
            Require(!admitted && elapsed < std::chrono::milliseconds(200), "Wait did not cancel promptly on a window message");
            cancelled = false;
            Require(renderer.PrepareFrame(Pump), "Could not resume cancelled frame");
            renderer.Render();
            RendererTestAccess::Flush(renderer);
            renderer.CheckDebugMessages();
        }
        std::cout << "All pipeline queue budgets, buffer counts, VSync changes, resize/restore, and wait cancellation checks passed.\n";
        return 0;
    }
    catch (const std::exception& e) { std::cerr << e.what() << '\n'; return 1; }
}
