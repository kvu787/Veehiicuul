#pragma once

#include "Renderer.h"

#include <Windows.h>

#include <cstdint>

class Application final
{
public:
    ~Application();

    int Run(HINSTANCE instance, int showCommand);

private:
    static LRESULT CALLBACK WindowProcedure(HWND window, UINT message, WPARAM wParam, LPARAM lParam);
    LRESULT HandleMessage(HWND window, UINT message, WPARAM wParam, LPARAM lParam);

    void CreateMainWindow(HINSTANCE instance, int showCommand);
    void ApplyPendingResize();
    void ToggleFullscreen();
    void UpdateWindowTitle() const;

    static constexpr wchar_t WindowClassName[] = L"SimpleDirectX12GameWindow";

    HWND m_window = nullptr;
    Renderer m_renderer;
    bool m_minimized = false;
    bool m_inSizeMove = false;
    bool m_resizePending = false;
    bool m_fullscreen = false;
    std::uint32_t m_pendingWidth = 1280;
    std::uint32_t m_pendingHeight = 720;
    WINDOWPLACEMENT m_windowedPlacement{sizeof(WINDOWPLACEMENT)};
};
