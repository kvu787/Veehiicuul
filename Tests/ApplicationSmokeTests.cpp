#include "SettingsTestData.h"
#include "Application.h"
#include <atomic>
#include <chrono>
#include <iostream>
#include <stdexcept>
#include <string>
#include <thread>

int main(int argc, char** argv)
{
    const bool maximizeFps = argc > 1 && std::string_view(argv[1]) == "--maximize-fps";
    const auto expectedRenderPipeline = maximizeFps ? L"MaximizeFps | GPU:3 Present:inactive Buffers:4 | Spin" :
        L"Standard | GPU:2 Present:2 Buffers:3 | Event";
    const DWORD uiThread = GetCurrentThreadId();
    std::atomic<bool> done = false;
    std::string failure;
    std::jthread driver([&] {
        HWND window = nullptr;
        auto wait = [&](const auto& predicate) {
            const auto deadline = std::chrono::steady_clock::now() + std::chrono::seconds(5);
            while (!done && std::chrono::steady_clock::now() < deadline)
            {
                if (predicate()) return true;
                // Test-driver synchronization only; the application has no FPS timer.
                std::this_thread::sleep_for(std::chrono::milliseconds(5));
            }
            return false;
        };
        auto titleContains = [&](const wchar_t* text) {
            wchar_t title[1024]{};
            GetWindowTextW(window, title, 1024);
            return std::wstring_view(title).find(text) != std::wstring_view::npos;
        };
        try
        {
            if (!wait([&] { EnumThreadWindows(uiThread, [](HWND candidate, LPARAM target) -> BOOL {
                    wchar_t name[128]{};
                    GetClassNameW(candidate, name, 128);
                    if (std::wstring_view(name) != L"SimpleDirectX12GameWindow") return TRUE;
                    *reinterpret_cast<HWND*>(target) = candidate;
                    return FALSE;
                }, reinterpret_cast<LPARAM>(&window));
                return window && titleContains(expectedRenderPipeline); }))
                throw std::runtime_error("Application did not initialize the configured render pipeline");
            if (!titleContains(L"VSync: Off")) throw std::runtime_error("Initial VSync was not applied from settings");
            PostMessageW(window, WM_KEYDOWN, 'V', 0);
            if (!wait([&] { return titleContains(L"VSync: On"); })) throw std::runtime_error("VSync hotkey failed");
            PostMessageW(window, WM_KEYDOWN, VK_F11, 0);
            if (!wait([&] { return titleContains(L"Fullscreen"); })) throw std::runtime_error("Fullscreen resize failed");
            ShowWindowAsync(window, SW_MINIMIZE);
            if (!wait([&] { return IsIconic(window) != FALSE; })) throw std::runtime_error("Minimize failed");
            ShowWindowAsync(window, SW_RESTORE);
            if (!wait([&] { return IsIconic(window) == FALSE; })) throw std::runtime_error("Restore failed");
            PostMessageW(window, WM_KEYDOWN, VK_F11, 0);
            if (!wait([&] { return titleContains(L"Windowed"); })) throw std::runtime_error("Windowed restore failed");
            PostMessageW(window, WM_KEYDOWN, 'V', 0);
            if (!wait([&] { return titleContains(L"VSync: Off") && titleContains(expectedRenderPipeline); }))
                throw std::runtime_error("VSync toggle changed the render pipeline preset");
            PostMessageW(window, WM_CLOSE, 0, 0);
        }
        catch (const std::exception& e)
        {
            failure = e.what();
            PostThreadMessageW(uiThread, WM_QUIT, 1, 0);
        }
    });
    try
    {
        Application application;
        auto settings = MakeTestSettings();
        settings.RenderPipeline.Preset = maximizeFps ? "MaximizeFps" : "Standard";
        const int result = application.Run(GetModuleHandleW(nullptr), SW_SHOWNOACTIVATE, settings);
        done = true;
        driver.join();
        if (!failure.empty()) throw std::runtime_error(failure);
        if (result != 0) throw std::runtime_error("Application returned failure");
        std::cout << "Application initialization, VSync hotkeys, fullscreen, minimize/restore, and close passed.\n";
        return 0;
    }
    catch (const std::exception& e)
    {
        done = true;
        driver.join();
        std::cerr << e.what() << '\n';
        return 1;
    }
}
