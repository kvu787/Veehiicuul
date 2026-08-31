# DirectX 12 scene implementation

Date: 2026-08-30

Repository: `Simple_DirectX12_3D_Game`

## User

> implement a simple 3d scene that is similar to "C:\Users\k\Repository\Godot\VsyncStutterTest" (fixed camera, static red cube, blue sphere moving back and forth across the screen) using directx 12 and c++.

## Assistant

Implemented a native Win32/C++20 DirectX 12 application after inspecting the referenced Godot project. The scene mirrors the reference's orthographic camera, procedural green plane, lit red cube, unlit blue sphere, material colors, directional light, and constant-speed sphere sweep between `x = -7` and `x = +7`.

The implementation includes:

- DirectX 12 device and high-performance adapter selection with WARP fallback;
- a two-buffer flip-discard swap chain, depth buffer, root signature, pipeline state, embedded HLSL, and per-frame fence synchronization;
- procedural plane, cube, and sphere geometry;
- resize, minimize/restore, DPI, VSync, tearing, and borderless-fullscreen handling;
- a root-relative `Run.cmd` that discovers Visual Studio's C++/CMake tools, builds Release, and launches the executable;
- build and usage documentation in `README.md`.

Validation completed with warning-free Debug and Release builds. Both the Debug executable and the `Run.cmd` Release executable launched successfully and remained responsive during runtime checks. No images were attached to this conversation.
