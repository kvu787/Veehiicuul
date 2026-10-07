#include "Renderer.h"

#include <shellapi.h>
#include <chrono>
#include <filesystem>
#include <format>
#include <fstream>
#include <memory>
#include <stdexcept>

namespace
{
struct WindowState
{
    bool running{true}, minimized{}, foreground{}, borderless{};
    std::uint32_t pendingWidth{}, pendingHeight{};
    std::size_t firstDevice{};
    WINDOWPLACEMENT placement{sizeof(WINDOWPLACEMENT)};
};

void ToggleFullscreen(HWND window, WindowState& state)
{
    const auto visibility = GetWindowLongPtrW(window, GWL_STYLE) & WS_VISIBLE;
    if (!state.borderless) {
        MONITORINFO monitor{sizeof(MONITORINFO)};
        if (!GetWindowPlacement(window, &state.placement) || !GetMonitorInfoW(MonitorFromWindow(window, MONITOR_DEFAULTTONEAREST), &monitor)) return;
        if (!visibility) state.placement.showCmd = SW_SHOWNORMAL;
        SetWindowLongPtrW(window, GWL_STYLE, WS_POPUP | visibility);
        SetWindowPos(window, HWND_TOP, monitor.rcMonitor.left, monitor.rcMonitor.top,
            monitor.rcMonitor.right - monitor.rcMonitor.left, monitor.rcMonitor.bottom - monitor.rcMonitor.top,
            SWP_FRAMECHANGED | SWP_NOOWNERZORDER);
    }
    else {
        SetWindowLongPtrW(window, GWL_STYLE, WS_OVERLAPPEDWINDOW | visibility);
        SetWindowPlacement(window, &state.placement);
        SetWindowPos(window, nullptr, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED | SWP_NOOWNERZORDER);
    }
    state.borderless = !state.borderless;
}

LRESULT CALLBACK WindowProcedure(HWND window, UINT message, WPARAM first, LPARAM second)
{
    auto* state = reinterpret_cast<WindowState*>(GetWindowLongPtrW(window, GWLP_USERDATA));
    if (message == WM_NCCREATE) {
        state = static_cast<WindowState*>(reinterpret_cast<CREATESTRUCTW*>(second)->lpCreateParams);
        SetWindowLongPtrW(window, GWLP_USERDATA, reinterpret_cast<LONG_PTR>(state));
    }
    if (!state) return DefWindowProcW(window, message, first, second);
    switch (message) {
    case WM_GETMINMAXINFO: {
        auto* limits = reinterpret_cast<MINMAXINFO*>(second);
        RECT minimum{0, 0, 1280, 1300};
        AdjustWindowRectEx(&minimum, WS_OVERLAPPEDWINDOW, FALSE, 0);
        limits->ptMinTrackSize = {minimum.right - minimum.left, minimum.bottom - minimum.top};
        return 0;
    }
    case WM_CLOSE: state->running = false; return 0;
    case WM_DESTROY: state->running = false; PostQuitMessage(0); return 0;
    case WM_SIZE:
        state->minimized = first == SIZE_MINIMIZED;
        if (!state->minimized) { state->pendingWidth = LOWORD(second); state->pendingHeight = HIWORD(second); }
        return 0;
    case WM_ACTIVATEAPP: state->foreground = first != 0; return 0;
    case WM_KEYDOWN:
        if ((second & (1ll << 30)) != 0) return 0;
        if (first == VK_ESCAPE) state->running = false;
        if (first == VK_F11) ToggleFullscreen(window, *state);
        if (first == VK_NEXT) ++state->firstDevice;
        if (first == VK_PRIOR && state->firstDevice) --state->firstDevice;
        return 0;
    case WM_ERASEBKGND: return 1;
    default: return DefWindowProcW(window, message, first, second);
    }
}

std::filesystem::path ExecutableDirectory()
{
    std::wstring buffer(32768, L'\0');
    const DWORD count = GetModuleFileNameW(nullptr, buffer.data(), static_cast<DWORD>(buffer.size()));
    if (!count || count >= buffer.size()) throw std::runtime_error("Cannot locate the executable directory.");
    buffer.resize(count);
    return std::filesystem::path(buffer).parent_path();
}

struct Options { int duration{}; bool hidden{}, software{}, capture{}; std::filesystem::path logs; };
Options ParseOptions()
{
    int count{};
    auto* arguments = CommandLineToArgvW(GetCommandLineW(), &count);
    if (!arguments) throw std::runtime_error("Cannot read command-line options.");
    Options options;
    try {
        for (int index = 1; index < count; ++index) {
            const std::wstring_view argument = arguments[index];
            if (argument == L"--hidden") options.hidden = true;
            else if (argument == L"--software-adapter") options.software = true;
            else if (argument == L"--capture-frame") options.capture = true;
            else if (argument == L"--duration" && index + 1 < count) {
                std::size_t consumed{};
                const std::wstring text = arguments[++index];
                options.duration = std::stoi(text, &consumed);
                if (consumed != text.size() || options.duration < 0 || options.duration > 86400) throw std::runtime_error("Duration must be an integer from 0 to 86400 seconds.");
            }
            else if (argument == L"--log-directory" && index + 1 < count) options.logs = arguments[++index];
            else throw std::runtime_error("Unknown or incomplete command-line option.");
        }
    }
    catch (...) { LocalFree(arguments); throw; }
    LocalFree(arguments);
    return options;
}

void ReserveApplicationLog(const std::filesystem::path& directory)
{
    const auto path = directory / L"Application.log";
    const auto file = CreateFileW(path.c_str(), GENERIC_WRITE, FILE_SHARE_READ, nullptr, CREATE_NEW, FILE_ATTRIBUTE_NORMAL, nullptr);
    if (file == INVALID_HANDLE_VALUE) throw std::runtime_error(std::format(
        "Cannot reserve a new Application.log (Windows error {}). Use an unused session log directory.", GetLastError()));
    CloseHandle(file);
}

std::filesystem::path MakeSessionDirectory(const std::filesystem::path& executableDirectory, const Options& options)
{
    if (!options.logs.empty()) {
        std::filesystem::create_directories(options.logs);
        ReserveApplicationLog(options.logs);
        return options.logs;
    }
    // Keep the app relocatable: locate its own root from its executable, with
    // no compiled absolute path or dependency on the containing Git repo.
    auto applicationDirectory = executableDirectory;
    if (executableDirectory.parent_path().filename() == L"BuildOutput") applicationDirectory = executableDirectory.parent_path().parent_path();
    const auto logs = applicationDirectory / L"LogOutput";
    std::filesystem::create_directories(logs);
    for (;;) {
        SYSTEMTIME time{}; GetLocalTime(&time);
        const auto name = std::format(L"{:04}-{:02}-{:02}_{:02}-{:02}-{:02}", time.wYear, time.wMonth, time.wDay, time.wHour, time.wMinute, time.wSecond);
        const auto directory = logs / name;
        if (std::filesystem::create_directory(directory)) {
            ReserveApplicationLog(directory);
            return directory;
        }
        // Startup only: wait for an unused timestamp instead of mixing two
        // sessions' logs. This never runs on the rendering or input paths.
        Sleep(10);
    }
}

class WindowOwner final
{
public:
    HWND handle{};
    ~WindowOwner() { if (handle) DestroyWindow(handle); }
};
}

int WINAPI wWinMain(HINSTANCE instance, HINSTANCE, PWSTR, int)
{
    std::filesystem::path logDirectory;
    std::ofstream applicationLog;
    bool silent = false;
    try {
        const auto options = ParseOptions();
        silent = options.hidden;
        const auto executableDirectory = ExecutableDirectory();
        logDirectory = MakeSessionDirectory(executableDirectory, options);
        applicationLog.open(logDirectory / L"Application.log");
        applicationLog.exceptions(std::ios::failbit | std::ios::badbit);
        applicationLog << "InputLatency startup\nGameInputPackage=3.5.283\nGameInputRuntime=Bundled v3 (explicit local loading)\n"
            << "MeasuredInputKind=Gamepad\n"
            << "Measurement=GameInput reading timestamp to software milestones; no physical input-to-photon claim\n"
            << "Percentiles=Nearest rank over the last 8192 observations of each metric\n";
        if (!SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2) && GetLastError() != ERROR_ACCESS_DENIED) {
            applicationLog << "DPI awareness request failed: " << GetLastError() << '\n';
        }
        OSVERSIONINFOW version{sizeof(OSVERSIONINFOW)};
        using VersionFunction = LONG(WINAPI*)(OSVERSIONINFOW*);
        const auto versionFunction = reinterpret_cast<VersionFunction>(GetProcAddress(GetModuleHandleW(L"ntdll.dll"), "RtlGetVersion"));
        if (!versionFunction || versionFunction(&version) != 0 || version.dwBuildNumber < 22000) throw std::runtime_error("Windows 11 x64 is required.");

        WindowState windowState;
        WNDCLASSEXW windowClass{};
        windowClass.cbSize = sizeof(windowClass); windowClass.lpfnWndProc = WindowProcedure;
        windowClass.hInstance = instance; windowClass.hCursor = LoadCursorW(nullptr, IDC_ARROW);
        windowClass.lpszClassName = L"InputLatencyDirectX12";
        if (!RegisterClassExW(&windowClass)) throw std::runtime_error("Cannot register application window class.");
        RECT rectangle{0, 0, 1280, 1300}; AdjustWindowRectEx(&rectangle, WS_OVERLAPPEDWINDOW, FALSE, 0);
        WindowOwner window;
        window.handle = CreateWindowExW(0, windowClass.lpszClassName, L"Gamepad latency - GameInput / DirectX 12", WS_OVERLAPPEDWINDOW,
            CW_USEDEFAULT, CW_USEDEFAULT, rectangle.right - rectangle.left, rectangle.bottom - rectangle.top,
            nullptr, nullptr, instance, &windowState);
        if (!window.handle) throw std::runtime_error("Cannot create application window.");
        ToggleFullscreen(window.handle, windowState);
        if (!options.hidden) ShowWindow(window.handle, SW_SHOW);
        RECT client{}; GetClientRect(window.handle, &client);
        Renderer renderer;
        renderer.Initialize(window.handle, static_cast<std::uint32_t>(client.right), static_cast<std::uint32_t>(client.bottom), options.software);
        windowState.pendingWidth = windowState.pendingHeight = 0;
        applicationLog << renderer.Description();
        if (!SetThreadPriority(GetCurrentThread(), THREAD_PRIORITY_ABOVE_NORMAL)) applicationLog << "RenderThreadPriority=Normal (request failed)\n";
        else applicationLog << "RenderThreadPriority=AboveNormal\n";
        auto monitor = std::make_unique<InputMonitor>();
        monitor->Initialize(executableDirectory, logDirectory);
        const auto beginning = std::chrono::steady_clock::now();
        auto nextDashboard = beginning;
        auto rateBeginning = beginning;
        std::uint64_t frames{}, rateFrames{};
        double framesPerSecond{};
        std::size_t displayedFirstDevice{};
        MonitorSnapshot snapshot;
        std::array<VisualState, MaximumDevices> visualStates{};
        const std::function<bool()> serviceMessages = [&] {
            MSG message{};
            while (PeekMessageW(&message, nullptr, 0, 0, PM_REMOVE)) {
                if (message.message == WM_QUIT) windowState.running = false;
                TranslateMessage(&message); DispatchMessageW(&message);
            }
            monitor->SetForeground(windowState.foreground);
            if (options.duration && std::chrono::steady_clock::now() - beginning >= std::chrono::seconds(options.duration)) windowState.running = false;
            return windowState.running && !windowState.minimized && windowState.pendingWidth == 0;
        };
        while (windowState.running) {
            serviceMessages();
            if (!windowState.running) break;
            if (windowState.minimized) {
                // Wait for resource/window availability while minimized. This
                // is not a frame-rate cap; no frames can be shown in this state.
                MsgWaitForMultipleObjectsEx(0, nullptr, options.duration ? 100 : INFINITE, QS_ALLINPUT, MWMO_INPUTAVAILABLE);
                continue;
            }
            if (windowState.pendingWidth) {
                renderer.Resize(windowState.pendingWidth, windowState.pendingHeight);
                windowState.pendingWidth = windowState.pendingHeight = 0;
                nextDashboard = std::chrono::steady_clock::time_point{};
            }
            const auto now = std::chrono::steady_clock::now();
            if (now >= nextDashboard) {
                snapshot = monitor->Snapshot();
                if (windowState.firstDevice >= snapshot.devices.size()) windowState.firstDevice = snapshot.devices.empty() ? 0 : snapshot.devices.size() - 1;
                const auto seconds = std::chrono::duration<double>(now - rateBeginning).count();
                if (seconds > 0) framesPerSecond = static_cast<double>(rateFrames) / seconds;
                rateBeginning = now; rateFrames = 0;
                renderer.BuildDashboard(snapshot, windowState.firstDevice, framesPerSecond, windowState.foreground);
                displayedFirstDevice = windowState.firstDevice;
                nextDashboard = now + std::chrono::milliseconds(250);
            }
            if (!renderer.PrepareFrame(serviceMessages)) continue;
            const auto presentation = renderer.DrawAndPresent(*monitor, visualStates, displayedFirstDevice);
            monitor->RecordPresentation(visualStates, frames, presentation.beginning, presentation.ending, presentation.accepted,
                presentation.firstQpc, presentation.lastQpc, presentation.endQpc, presentation.swapChain, presentation.thread);
            ++frames; ++rateFrames;
        }
        monitor->Stop();
        if (options.capture && frames) renderer.SaveLastFrame(logDirectory / L"Dashboard.png");
        renderer.VerifyDebugMessages();
        const auto finalSnapshot = monitor->Snapshot();
        if (const auto selected = renderer.SelectedController()) {
            const auto& controller = monitor->Device(*selected);
            applicationLog << "SelectedController=" << *selected << "\nSelectedControllerName=" << controller.name
                << "\nSelectedControllerId=" << controller.identifier << '\n';
        }
        applicationLog << "FramesSubmitted=" << frames << "\nDevicesEnumerated=" << monitor->DeviceCount()
            << "\nDurationSeconds=" << std::chrono::duration<double>(std::chrono::steady_clock::now() - beginning).count()
            << "\nDisplayTrackingError=" << finalSnapshot.displayTracking.error << "\nDisplayedFrames=" << finalSnapshot.displayTracking.displayed
            << "\nLoggingFailed=" << finalSnapshot.loggingFailed << "\nInputLatency shutdown\n";
        applicationLog.flush();
        if (finalSnapshot.loggingFailed) throw std::runtime_error("Measurement logging failed. Check session storage permissions and free space.");
        return 0;
    }
    catch (const std::exception& error) {
        try { if (applicationLog.is_open()) { applicationLog.clear(); applicationLog << "ERROR: " << error.what() << '\n'; applicationLog.flush(); } } catch (...) {}
        if (!silent) {
            const auto message = std::string(error.what()) + "\n\nSee the session's Application.log in LogOutput.";
            MessageBoxA(nullptr, message.c_str(), "Input latency application error", MB_OK | MB_ICONERROR);
        }
        return 1;
    }
}
