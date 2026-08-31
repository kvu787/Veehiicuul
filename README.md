# Simple DirectX 12 3D Scene

A small native Win32/C++ DirectX 12 scene based on the composition in `Godot/VsyncStutterTest`:

- a fixed orthographic camera;
- a static, lit red cube;
- a flat-blue unlit sphere moving at constant speed between `x = -7` and `x = +7`;
- a lit green ground plane and directional light.

The camera, object transforms, material colors, light direction, sphere speed, and travel distance mirror the Godot reference. Because the camera is angled, the sphere's world-X movement appears as a top-left to bottom-right sweep on screen.

## Run

Double-click `Run.cmd` in File Explorer. This minimal wrapper starts `Run.ps1`, which discovers Visual Studio, configures a 64-bit Release build with its bundled CMake and Ninja, builds it, and launches the game. Subsequent launches rebuild only changed files.

Requirements:

- Windows 10 or newer;
- a DirectX 12 adapter and driver supporting Shader Model 6.0 (hardware rendering is preferred; WARP software rendering is used as a fallback when it meets that requirement);
- Visual Studio 2022 or newer with the **Desktop development with C++** workload, the **C++ CMake tools for Windows** component, and a Windows SDK.
- a current Windows SDK containing the DirectX Shader Compiler (`dxc.exe`).

The generated executable is `build\release\SimpleDirectX12Game.exe`.

## Controls

| Key | Action |
| --- | --- |
| `V` | Toggle VSync |
| `F11` or `Alt+Enter` | Toggle borderless fullscreen |
| `Esc` or `Alt+F4` | Quit |

The app starts windowed at 1280×720 with VSync on. To approximate the reference project's exclusive-fullscreen, VSync-off presentation, press `F11` and then `V`.

## Implementation

The renderer uses a two-buffer flip-discard swap chain, per-back-buffer command allocators and fence values, a depth buffer, a root constant-buffer view, and procedural cube/sphere/plane geometry. Shader source lives in `shaders/Scene.hlsl`. During the build, DXC compiles its `VSMain` and `PSMain` entry points as Shader Model 6.0 DXIL and generates byte arrays that are embedded directly into the executable. The application therefore performs no runtime shader compilation and does not need shader source or compiler DLLs beside the executable.

The renderer handles resizing, DPI changes, minimizing/restoring, and GPU/CPU frame synchronization without runtime shader/compiler files or other asset files.
