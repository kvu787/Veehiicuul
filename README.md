# Veehiicuul

This repository contains the current C++ application and the planned Godot
rewrite of ZoomTracks.

- [Cpp](Cpp/README.md): the native Win32/C++ DirectX 12 application, including
  its source, build presets, tools, tests, documentation, and saved reports.
- [Godot](Godot): reserved for the rewrite; currently only a placeholder.
- [Blender](Blender): shared source assets, including `Car.blend`.
- [Conversations](Conversations): repository conversation records.

Double-click [Cpp/Run.cmd](Cpp/Run.cmd) to build and launch the current C++ application.
The launcher uses [Cpp/Run.ps1](Cpp/Run.ps1); builds and session
logs stay under `Cpp/MyBuildOutput` and `Cpp/MyLogOutput`.

From PowerShell at the repository root:

```powershell
.\Cpp\Run.ps1 -BuildOnly
.\Cpp\Run.ps1 -Test
.\Cpp\Run.ps1 -Test -Configuration Debug
```

For CMake commands or Visual Studio's Open Folder workflow, use the `Cpp`
directory, which contains `CMakeLists.txt` and `CMakePresets.json`. See the
[C++ guide](Cpp/Documentation/VisualStudio.md). Paths in the C++ documentation
are relative to `Cpp` unless stated otherwise.

Historical reports and conversation records retain their original wording and
saved paths. Application paths recorded before the move now live under `Cpp`.

## Constraints

This section is the single source of truth for constraints that apply to both
human developers and coding agents throughout the repository.

### Platform and GPU policy

The runtime platform preconditions are **Windows 11** and
**x86_64 (x64)**. There are **no GPU preconditions**: no particular GPU vendor,
model, generation, or hardware feature set may be required.

All repository code must use vendor-neutral interfaces and behavior. Do not
implement or integrate GPU-vendor-specific APIs, SDKs, extensions,
optimizations, workarounds, or vendor-ID-based paths. **NVIDIA Reflex and AMD
Anti-Lag 2 are prohibited**, including optional integrations. Rendering and
latency/queueing work must use vendor-neutral Windows, Direct3D 12, and DXGI
capability queries and fallbacks.

### Frame rate policy

The app must not implement any frame rate limiting of its own. Do not add
an FPS cap, target-frame-rate or `FrameRateLimit` setting, or timer, sleep,
spin, or pacing logic intended to enforce a frame rate. This prohibition
includes optional limiters and background FPS caps.

GPU fences, resource-availability waits, DXGI presentation waits, and VSync
remain valid synchronization mechanisms. They must not be supplemented with
app-owned timing delays to impose an FPS target.
