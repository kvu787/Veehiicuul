#include "Application.h"

#include <cwchar>
#include <stdexcept>

Application::~Application()
{
    if (m_window != nullptr && IsWindow(m_window))
    {
        SetWindowLongPtrW(m_window, GWLP_USERDATA, 0);
        DestroyWindow(m_window);
    }
    m_window = nullptr;
}

int Application::Run(HINSTANCE instance, const int showCommand)
{
    SetProcessDpiAwarenessContext(DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2);
    CreateMainWindow(instance, showCommand);

    RECT clientRectangle{};
    if (!GetClientRect(m_window, &clientRectangle))
    {
        throw std::runtime_error("GetClientRect failed while initializing the renderer.");
    }

    m_pendingWidth = static_cast<std::uint32_t>(clientRectangle.right - clientRectangle.left);
    m_pendingHeight = static_cast<std::uint32_t>(clientRectangle.bottom - clientRectangle.top);
    m_renderer.Initialize(m_window, m_pendingWidth, m_pendingHeight);
    m_resizePending = false;
    UpdateWindowTitle();

    ShowWindow(m_window, showCommand);
    UpdateWindow(m_window);

    MSG message{};
    while (message.message != WM_QUIT)
    {
        if (PeekMessageW(&message, nullptr, 0, 0, PM_REMOVE))
        {
            TranslateMessage(&message);
            DispatchMessageW(&message);
            ApplyPendingResize();
        }
        else if (!m_minimized && !m_inSizeMove)
        {
            m_renderer.Render();
        }
        else
        {
            WaitMessage();
        }
    }

    return static_cast<int>(message.wParam);
}

void Application::CreateMainWindow(HINSTANCE instance, const int showCommand)
{
    static_cast<void>(showCommand);

    WNDCLASSEXW windowClass{};
    windowClass.cbSize = sizeof(windowClass);
    windowClass.style = CS_HREDRAW | CS_VREDRAW;
    windowClass.lpfnWndProc = WindowProcedure;
    windowClass.hInstance = instance;
    windowClass.hCursor = LoadCursorW(nullptr, IDC_ARROW);
    windowClass.lpszClassName = WindowClassName;
    if (RegisterClassExW(&windowClass) == 0)
    {
        throw std::runtime_error("RegisterClassExW failed.");
    }

    constexpr DWORD windowStyle = WS_OVERLAPPEDWINDOW;
    RECT windowRectangle{0, 0, 1280, 720};
    const UINT dpi = GetDpiForSystem();
    if (!AdjustWindowRectExForDpi(&windowRectangle, windowStyle, FALSE, 0, dpi))
    {
        throw std::runtime_error("AdjustWindowRectExForDpi failed.");
    }

    const int windowWidth = windowRectangle.right - windowRectangle.left;
    const int windowHeight = windowRectangle.bottom - windowRectangle.top;
    m_window = CreateWindowExW(
        0,
        WindowClassName,
        L"Simple DirectX 12 Scene",
        windowStyle,
        CW_USEDEFAULT,
        CW_USEDEFAULT,
        windowWidth,
        windowHeight,
        nullptr,
        nullptr,
        instance,
        this);
    if (m_window == nullptr)
    {
        throw std::runtime_error("CreateWindowExW failed.");
    }
}

LRESULT CALLBACK Application::WindowProcedure(
    HWND window,
    const UINT message,
    const WPARAM wParam,
    const LPARAM lParam)
{
    Application* application = reinterpret_cast<Application*>(GetWindowLongPtrW(window, GWLP_USERDATA));
    if (message == WM_NCCREATE)
    {
        const auto* creation = reinterpret_cast<const CREATESTRUCTW*>(lParam);
        application = static_cast<Application*>(creation->lpCreateParams);
        SetWindowLongPtrW(window, GWLP_USERDATA, reinterpret_cast<LONG_PTR>(application));
    }

    if (application != nullptr)
    {
        return application->HandleMessage(window, message, wParam, lParam);
    }
    return DefWindowProcW(window, message, wParam, lParam);
}

LRESULT Application::HandleMessage(
    HWND window,
    const UINT message,
    const WPARAM wParam,
    const LPARAM lParam)
{
    switch (message)
    {
    case WM_CLOSE:
        DestroyWindow(window);
        return 0;

    case WM_DESTROY:
        PostQuitMessage(0);
        return 0;

    case WM_NCDESTROY:
        SetWindowLongPtrW(window, GWLP_USERDATA, 0);
        m_window = nullptr;
        return DefWindowProcW(window, message, wParam, lParam);

    case WM_ERASEBKGND:
        return 1;

    case WM_ENTERSIZEMOVE:
        m_inSizeMove = true;
        return 0;

    case WM_EXITSIZEMOVE:
        m_inSizeMove = false;
        return 0;

    case WM_SIZE:
    {
        m_minimized = wParam == SIZE_MINIMIZED;
        if (!m_minimized)
        {
            m_pendingWidth = static_cast<std::uint32_t>(LOWORD(lParam));
            m_pendingHeight = static_cast<std::uint32_t>(HIWORD(lParam));
            m_resizePending = true;
        }
        return 0;
    }

    case WM_DPICHANGED:
    {
        const auto* suggestedRectangle = reinterpret_cast<const RECT*>(lParam);
        SetWindowPos(
            window,
            nullptr,
            suggestedRectangle->left,
            suggestedRectangle->top,
            suggestedRectangle->right - suggestedRectangle->left,
            suggestedRectangle->bottom - suggestedRectangle->top,
            SWP_NOACTIVATE | SWP_NOZORDER);
        return 0;
    }

    case WM_SYSKEYDOWN:
        if (wParam == VK_RETURN &&
            (lParam & (1LL << 29)) != 0 &&
            (lParam & (1LL << 30)) == 0)
        {
            ToggleFullscreen();
            return 0;
        }
        break;

    case WM_SYSCHAR:
        if (wParam == L'\r' && (lParam & (1LL << 29)) != 0)
        {
            return 0;
        }
        break;

    case WM_KEYDOWN:
        if ((lParam & (1LL << 30)) != 0)
        {
            return 0;
        }

        if (wParam == VK_ESCAPE)
        {
            DestroyWindow(window);
            return 0;
        }
        if (wParam == VK_F11)
        {
            ToggleFullscreen();
            return 0;
        }
        if (wParam == 'V')
        {
            m_renderer.SetVsyncEnabled(!m_renderer.IsVsyncEnabled());
            UpdateWindowTitle();
            return 0;
        }
        break;

    default:
        break;
    }

    return DefWindowProcW(window, message, wParam, lParam);
}

void Application::ApplyPendingResize()
{
    if (m_resizePending && m_window != nullptr && IsWindow(m_window) &&
        !m_minimized && !m_inSizeMove &&
        m_pendingWidth > 0 && m_pendingHeight > 0 && m_renderer.IsInitialized())
    {
        m_renderer.Resize(m_pendingWidth, m_pendingHeight);
        m_resizePending = false;
    }
}

void Application::ToggleFullscreen()
{
    if (m_window == nullptr)
    {
        return;
    }

    if (!m_fullscreen)
    {
        m_windowedPlacement.length = sizeof(WINDOWPLACEMENT);
        GetWindowPlacement(m_window, &m_windowedPlacement);

        MONITORINFO monitorInfo{sizeof(MONITORINFO)};
        GetMonitorInfoW(MonitorFromWindow(m_window, MONITOR_DEFAULTTONEAREST), &monitorInfo);
        SetWindowLongPtrW(
            m_window,
            GWL_STYLE,
            GetWindowLongPtrW(m_window, GWL_STYLE) & ~static_cast<LONG_PTR>(WS_OVERLAPPEDWINDOW));
        SetWindowPos(
            m_window,
            HWND_TOP,
            monitorInfo.rcMonitor.left,
            monitorInfo.rcMonitor.top,
            monitorInfo.rcMonitor.right - monitorInfo.rcMonitor.left,
            monitorInfo.rcMonitor.bottom - monitorInfo.rcMonitor.top,
            SWP_FRAMECHANGED | SWP_NOOWNERZORDER);
        m_fullscreen = true;
    }
    else
    {
        SetWindowLongPtrW(
            m_window,
            GWL_STYLE,
            GetWindowLongPtrW(m_window, GWL_STYLE) | static_cast<LONG_PTR>(WS_OVERLAPPEDWINDOW));
        SetWindowPlacement(m_window, &m_windowedPlacement);
        SetWindowPos(
            m_window,
            nullptr,
            0,
            0,
            0,
            0,
            SWP_FRAMECHANGED | SWP_NOMOVE | SWP_NOOWNERZORDER | SWP_NOSIZE | SWP_NOZORDER);
        m_fullscreen = false;
    }

    UpdateWindowTitle();
}

void Application::UpdateWindowTitle() const
{
    const wchar_t* vsyncState = m_renderer.IsVsyncEnabled() ? L"On" : L"Off";
    const wchar_t* displayState = m_fullscreen ? L"Fullscreen" : L"Windowed";
    wchar_t title[256]{};
    swprintf_s(
        title,
        L"Simple DirectX 12 Scene | VSync: %s | %s | V: toggle VSync  F11/Alt+Enter: fullscreen  Esc: quit",
        vsyncState,
        displayState);
    SetWindowTextW(m_window, title);
}
