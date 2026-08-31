# Alt+Enter fullscreen-to-windowed sound

Date: 2026-08-30

Repository: `Simple_DirectX12_3D_Game`

## User

> when toggling fullscreen using f11 or "alt+enter", it seems like using alt+enter when going from fullscreen to windowed always triggers a windows "boop" sound. using f11 never does this, in either direction. explain

## Assistant

The sound comes from Win32's default processing of a second keyboard message, not from DirectX or the fullscreen resize itself.

The message loop calls `TranslateMessage` before dispatching every message. Pressing Alt+Enter produces a `WM_SYSKEYDOWN` for Enter, and `TranslateMessage` queues a corresponding `WM_SYSCHAR` (carriage return). The window procedure handles the `WM_SYSKEYDOWN`, toggles fullscreen, and returns zero, but it does not handle the already-queued `WM_SYSCHAR`. That later falls through to `DefWindowProcW`.

The direction matters because the window style has changed before `WM_SYSCHAR` is dispatched. On fullscreen-to-windowed, `ToggleFullscreen` has just restored `WS_OVERLAPPEDWINDOW`, including the system-menu/window-menu behavior. `DefWindowProcW` therefore interprets the unmatched Alt+Enter system character as an invalid menu/system mnemonic and plays Windows' default warning sound. On windowed-to-fullscreen, the code has just removed `WS_OVERLAPPEDWINDOW`, so that default menu path is inactive and no sound is heard.

F11 is different: it arrives as an ordinary `WM_KEYDOWN`, and because a function key is not character input, `TranslateMessage` does not generate a `WM_SYSCHAR`. The handled `WM_KEYDOWN` is therefore the end of the shortcut, with no later message available to trigger `DefWindowProcW`'s sound.

`DXGI_MWA_NO_ALT_ENTER` only disables DXGI's built-in Alt+Enter handling. It correctly leaves the program's custom borderless-fullscreen toggle in charge, but it does not suppress Win32's `WM_SYSCHAR` processing.

The direct fix would be to consume the matching `WM_SYSCHAR` for Alt+Enter (return zero), while continuing to pass unrelated system-key messages to `DefWindowProcW` so normal Alt shortcuts keep working.

No source code was changed as part of this explanation.
