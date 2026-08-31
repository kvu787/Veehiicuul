# Borderless versus exclusive fullscreen wording

Date: 2026-08-30

Repository: `Simple_DirectX12_3D_Game`

## User

> explain what this means from the readme:
> To approximate an exclusive-fullscreen...
>
> The F11 fullscreen mode should mean modern borderless-window-fullscreen, unless there is some other more performant way of doing fullscreen that I'm not aware of.

## Assistant

The README wording is misleading. Pressing `F11` does not enter exclusive fullscreen; it toggles modern borderless fullscreen. The code removes the ordinary window frame and expands the window to cover the nearest monitor, but it never calls DXGI's `SetFullscreenState(TRUE)`.

The phrase “approximate an exclusive-fullscreen, VSync-off presentation” means only that borderless fullscreen plus VSync off resembles the visible and presentation behavior historically associated with exclusive fullscreen: the game fills the monitor, has no window chrome, presents without waiting for vertical refresh, and permits tearing when the system supports it.

This is already the normal modern implementation. The project uses a flip-discard swap chain and the DXGI tearing flags, so Windows can use its optimized flip/independent-flip presentation path when the window is eligible. On current Windows systems, true DXGI exclusive fullscreen is not generally a reliably faster replacement. It is primarily useful for cases such as taking explicit ownership of a display and changing its hardware display mode, and it brings worse Alt+Tab and desktop integration.

A clearer README sentence would be: “Press `F11` to toggle borderless fullscreen. With VSync off, presentation uses tearing when supported.”

No application or README source files were changed.
