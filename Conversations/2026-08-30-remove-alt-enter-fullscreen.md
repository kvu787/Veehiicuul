# Remove Alt+Enter fullscreen toggle

## User

> remove this feature: "alt+enter" toggles borderless fullscreen

## Assistant

Removed the custom Alt+Enter borderless-fullscreen shortcut. F11 remains the
fullscreen toggle, and the window title and README now list only F11 for that
action.

The renderer still disables DXGI's built-in Alt+Enter handling. This ensures
that removing the custom shortcut does not expose DXGI's exclusive-fullscreen
behavior as a fallback.

The existing Release configuration compiled and linked successfully after the
change. The implementation was committed separately as `e94c188` (`Remove
Alt+Enter fullscreen shortcut`).
