# CreateMainWindow step-by-step

## User

<<<LOCATION_START
Lines and columns are 1-based. Columns are StartInclusive:EndExclusive. Columns count UTF-16 code units.
c:\Users\k\Repository\Veehiicuul\Cpp\Source\Application.cpp:77:1-121:2
>>>LOCATION_END

explain CreateMainWindow step-by-step

## Assistant commentary

I’ll read `CreateMainWindow` and the surrounding code, then explain each step in order.

## Assistant

[`CreateMainWindow`](C:/Users/k/Repository/Veehiicuul/Cpp/Source/Application.cpp:77) creates the application's Windows window and stores its handle in `m_window`. It leaves showing the window to `Run`, after the renderer is initialized.

1. **Accept the startup information.**
   `instance` identifies the running executable module. `showCommand` describes how the window should initially appear, but this function does not use it. `static_cast<void>(showCommand);` explicitly marks it as intentionally unused; it has no runtime effect.

2. **Describe the window class.**
   `WNDCLASSEXW windowClass{ ... };` defines a Windows window *type*, which is separate from a C++ class:
   - `cbSize` tells Windows the structure's size in bytes.
   - `CS_HREDRAW | CS_VREDRAW` requests repainting the entire client area when the width or height changes. `|` combines the flags.
   - `lpfnWndProc = WindowProcedure` identifies the callback Windows uses to deliver messages, such as resizing or closing.
   - `hInstance` associates the class with this executable module.
   - `LoadCursorW(nullptr, IDC_ARROW)` selects the standard system arrow cursor.
   - `lpszClassName` names this window type `VeehiicuulWindow`.
   Fields omitted from the initializer are zero-initialized. The `W` suffix on Windows APIs denotes their wide-character versions.

3. **Register that class with Windows.**
   `RegisterClassExW(&windowClass)` makes the description available for creating windows. A return value of zero means registration failed, so the function throws an exception and stops.

4. **Choose the appearance and desired drawing area.**
   `WS_OVERLAPPEDWINDOW` selects a normal desktop window with a title bar, resizable border, system menu, and minimize/maximize buttons.
   `RECT{0, 0, 1280, 720}` describes the desired **1280 × 720 client area**: the interior where the application draws. It does not yet include the title bar or borders, and these coordinates do not set the window's screen position.

5. **Calculate the outer size, accounting for display scaling.**
   `GetDpiForSystem()` obtains the system DPI. `AdjustWindowRectExForDpi` expands the rectangle to include the frame needed around that client area. Its arguments specify the chosen style, no menu (`FALSE`), no extended styles (`0`), and that DPI. Failure throws an exception.
   The client size remains the requested 1280 × 720 pixels; this call accounts for the surrounding frame at the supplied DPI.

6. **Extract the resulting dimensions.**
   `right - left` gives the outer width, and `bottom - top` gives the outer height. Subtraction matters because the adjustment can make `left` and `top` negative.

7. **Create the actual window.**
   `CreateWindowExW(...)` receives, in order:
   - `0`: no extended window styles.
   - `WindowClassName`: the registered window type.
   - `L"Veehiicuul"`: the initial title, expressed as a wide-character string.
   - `windowStyle`: the appearance selected above.
   - Two `CW_USEDEFAULT` values: let Windows choose the initial placement.
   - `windowWidth`, `windowHeight`: the calculated outer dimensions.
   - Two `nullptr` values: no parent/owner window and no menu.
   - `instance`: the executable module.
   - `this`: a pointer to the current `Application` object.

   That final pointer connects the Windows window to your C++ object. During creation, Windows calls `WindowProcedure` with `WM_NCCREATE`. The callback extracts this pointer and stores it in the window's user-data slot, allowing messages to reach this object's `HandleMessage` function.

8. **Store the handle and check for failure.**
   The returned `HWND` goes into `m_window`. It is Windows' identifier for the newly created window. If it is `nullptr`, creation failed and the function throws.

After this returns, `Run` queries the actual client size, initializes the renderer, updates the title, and calls `ShowWindow(m_window, showCommand)` followed by `UpdateWindow(m_window)`.

## User

why isn't there just a single api call for creating a window instead of an additional "registration" step?

## Assistant commentary

I’ll explain what registration contributes and how it applies to this app.

## Assistant

Win32 separates **defining a kind of window** from **creating an instance of it**.

`RegisterClassExW` defines the shared behavior and defaults: the message callback, cursor, icons, and class styles. `CreateWindowExW` then creates an individual window with its own title, position, size, and other settings.

This lets you register a type once and create many windows from it without repeating the shared configuration. Windows also provides predefined classes such as `BUTTON` and `EDIT`, so you can create those controls directly with `CreateWindowExW` without registering your own class first.

For this app, which creates one main window, the separation does add boilerplate with little immediate benefit. **There is no fundamental reason a higher-level API couldn't combine the steps.** A wrapper could register the class internally and then create the window, exposing a single call to your code. Here, `CreateMainWindow` already serves as that wrapper.

One subtlety: registration doesn't create a window or reserve screen space. It just gives Windows a named definition to look up when you request a window.

## User

can you create a window without doing an initial registration step?

## Assistant commentary

I’ll check the Windows API documentation for the exact requirement.

## Assistant

Yes—**if you use a window class Windows already provides**, such as `STATIC`. You do not need to call `RegisterClassExW` yourself. [`CreateWindowEx` accepts predefined system classes](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-createwindoww).

For example, this creates a visible top-level window using the predefined `STATIC` class:

```cpp
HWND window = CreateWindowExW(
    0,
    L"STATIC",
    L"Example window",
    WS_OVERLAPPEDWINDOW | WS_VISIBLE,
    CW_USEDEFAULT, CW_USEDEFAULT,
    800, 600,
    nullptr, nullptr,
    instance,
    nullptr);
```

You still need error handling and a message loop to keep it responsive. Also, it uses the built-in class’s message handler, so it will not automatically call your `Application::WindowProcedure`.

**For your current `VeehiicuulWindow` class, registration is required.** Removing registration while keeping that class name would make creation fail, unless it had already been registered elsewhere. Every Win32 window belongs to a window class; you can skip registering one yourself by using an existing class.
