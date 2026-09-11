# Simple DirectX 12 3D Car Scene

A native Win32/C++ DirectX 12 scene with an orthographic camera and SimplePaint shading.

Double-click [Run.cmd](Run.cmd) to build and launch the app.

## Documentation

- [Visual Studio guide](Documentation/VisualStudio.md): setup, code navigation, builds, debugging, tests, and troubleshooting.

- [SimplePaint usage](Source/SimplePaint/Usage.md): controls, numerical limits, and C++/DX12 integration.
- [Copyable SimplePaint module](Source/SimplePaint/README.md): self-contained C++/HLSL folder and integration instructions.
- [SimplePaint specification](Source/SimplePaint/Specification.md): mathematics, rationale, and verification.
- [Asset generation](Documentation/Assets.md): background and mesh regeneration instructions.
- [Historical reports](Documentation/Reports/README.md): earlier rendering and numerical analyses with supporting data.

Paths and command examples in these guides are relative to the repository root unless stated otherwise.

A small native Win32/C++ DirectX 12 scene moving toward the visual structure of
Zoom Tracks:

- a fixed orthographic camera with a 3/4 overhead view (orthographic-only by design);
- one 3D car, sourced from `Blender/Car.blend`, moving between `x = -7` and
  `x = +7` at 8 units per second while rotating at 90 degrees per second;
- a stationary UV sphere below and to the right of the cube, clear of the car;
- an optimized SimplePaint/K12 shader shared by the car and sphere; and
- one flattened 2D image containing the gray background, green ground, and
  static red cube.

The car and sphere are world-space 3D draws. The backdrop is a single textured
full-screen triangle and never uses the depth buffer. The car and sphere then
render in separate indexed draws with shared depth and separate transforms.
Both composite above the flattened environment.

## Constraints

This section is the single source of truth for constraints that apply to both
human developers and coding agents.

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

Hardware adapter selection checks both Direct3D 12 device creation and Shader
Model 6.0 support. If no hardware adapter qualifies, the renderer tries WARP.
The compiled shaders still require Shader Model 6.0 in the selected runtime,
including WARP. Support for Windows installations whose WARP runtime lacks
that capability remains an implementation gap.

### Frame rate policy

The app must not implement any frame rate limiting of its own. Do not add
an FPS cap, target-frame-rate or `FrameRateLimit` setting, or timer, sleep,
spin, or pacing logic intended to enforce a frame rate. This prohibition
includes optional limiters and background FPS caps.

GPU fences, resource-availability waits, DXGI presentation waits, and VSync
remain valid synchronization mechanisms. They must not be supplemented with
app-owned timing delays to impose an FPS target.

## Run

Double-click `Run.cmd` in File Explorer. This minimal wrapper starts `Run.ps1`,
which discovers Visual Studio, configures a 64-bit Release build with its
bundled CMake and Ninja, stages the runtime assets, builds, and launches the
game. Subsequent launches rebuild only changed files. If the repository or its
build folder has moved, the launcher automatically refreshes the saved CMake
configuration before building.

Build prerequisites:

- Visual Studio 2022 or newer with the **Desktop development with C++**
  workload, the **C++ CMake tools for Windows** component (CMake 3.24 or newer),
  and a Windows SDK;
  and
- a current Windows SDK containing the DirectX Shader Compiler (`dxc.exe`).

The generated executable is `MyBuildOutput\Release\SimpleDirectX12Game.exe`. CMake
places `SceneBackground.png` and `Settings.json` in its adjacent `Assets`
directory.

Each launcher invocation writes `Launcher.log` in
`MyLogOutput/yyyy-MM-dd_HH-mm-ss/`. Game runs also write startup, shutdown,
and errors to `Application.log` in that folder. Direct executable and Visual
Studio launches create a session folder under the repository's `MyLogOutput`
directory, whose location is set at build time. This directory is ignored by Git.

## Controls

| Key                | Action                       |
| ------------------ | ---------------------------- |
| `V`                | Toggle VSync                 |
| `F11`              | Toggle borderless fullscreen |
| `Esc` or `Alt+F4`  | Quit                         |

The app starts windowed at 1280x720 with VSync off. Press F11 to toggle borderless fullscreen.

With VSync off, presentation permits tearing only when the selected pipeline requests it and DXGI supports it.

## Render pipeline

Edit `Assets/Settings.json` and restart the app to select `RenderPipeline.Preset`:
`MinimizeInputLatency` (selected in the shipped JSON), `Standard`, `MaximizeFps`,
or `Custom`.
`RenderPipeline.VSync` is independent of every preset and defaults to `false` in the
shipped JSON. The `V` key changes VSync at runtime without changing the preset or
rewriting the JSON. The six custom render pipeline controls are ignored unless
`"Preset": "Custom"`.

See [Render pipeline configuration](Documentation/RenderPipeline.md) for presets, accepted
custom settings, wait behavior, and validation. The window title reports the
selected pipeline, queue limits, buffer count, wait strategy, and effective
tearing state.

MaximizeFps is an experimental throughput preset: three GPU frames in flight,
four image buffers, no explicit presentation admission wait, tearing permitted,
and spin waits for resource readiness. It preserves rendering quality and may
increase input latency and CPU/power use. Use the pipeline guide's Custom
example to compare queue sizes and wait strategies; higher FPS is not guaranteed.

## Adjust the paint and sphere

Edit `Assets/Settings.json`, then relaunch through `Run.cmd`. The six `SimplePaintShader_*`
objects provide independent paint controls for Axles, Body, Cabin, Headlights,
Wheels, and Sphere. See [Usage.md](Source/SimplePaint/Usage.md) for accepted numerical ranges, examples,
and instructions for embedding the shader in another C++/DX12 project.
[Specification.md](Source/SimplePaint/Specification.md) defines the mathematics, numerical
contract, cutoff decision, GPU layout, optimizations, and test results.

Every declared field is required in the settings file. JSON syntax and type
conversion are handled by pinned, vendored [nlohmann/json](ThirdParty/nlohmann_json/README.md);
unknown properties are ignored and the last duplicate property wins.
Application validation runs afterward using plain C++. Settings load once at
startup; invalid or missing files report an error. See [application settings](Documentation/Settings.md#run-and-edit-this-application)
for format rules and [Settings architecture](Documentation/Settings.md) for the code.

The rewritten SimplePaint validates materials in C++ before uploading them.
It preserves the K12 Schlick curve, facing lobe warp, and dark/light tone
remap, while using positive contributions to preserve highlight accuracy.
It removes the positive facing cutoff and all input clamps and denominator
floors. The base colors in the shipped settings now obey the explicit
numerical domain.

In the same JSON file, the `Sphere` object sets `"UResolution": 64` (longitude segments,
3 to 512) and `"VResolution": 32` (pole-to-pole latitude segments, 2 to 512).
Both require JSON integer tokens (`64` is valid; `64.0`, `64.`, and `6.4e1`
are rejected). The sphere is generated at startup with smooth radial
normals, radius 0.4, and a center at `(1.5, 0.4, -1.5)`.

Orthographic projection is a permanent renderer invariant. Object and view
transforms are affine; the vertex shader supplies clip W = 1 and passes
normals with `noperspective`. The C++ transform helper rejects perspective,
shear, and nonuniform scale. Mesh validation rejects invalid indices,
material assignments, and normals before upload.

Paint constants are uploaded once and shared by both objects and frame slots.
The stationary sphere's transforms refresh at initialization and after resize
while the GPU is idle. Only the car's 96-byte transform block changes each
frame; each object slot retains DirectX's 256-byte alignment.

Run `.\Run.ps1 -Test` for the Release CPU and production GPU tests, or add
`-Configuration Debug` for Debug. `-BuildOnly` builds without launching.
The GPU tests exercise both the preferred adapter and WARP.

## Assets and implementation

`Tools/GenerateAssets.py` uses Blender's own triangulation and evaluated corner
normals to turn `Blender/Car.blend` into the checked-in generated mesh header.
It also bakes the old static 3D scene into `Assets/SceneBackground.png`. The
background is 32:9 so normal windows can center-crop it while preserving the
camera's vertical scale; its center half is a native 2560x1440 image at 16:9.
Windows wider than 32:9 use matching side mattes and a centered 32:9 scene
viewport, so the live objects never drift relative to the baked scene.
See [Asset generation](Documentation/Assets.md) for the regeneration command. If Blender 4.5.12 or 5.2.0
is installed under `%UserProfile%\Program`, CMake also provides the explicit
`RegenerateAssets` target.

The renderer uses a configurable flip-discard swap chain, independently sized
frame contexts with command allocators and fence values, default-heap
mesh/texture resources, a persistent mapped per-frame constant buffer, and an
sRGB render-target view. The background
image is sampled as sRGB, while SimplePaint works in linear color; hardware
sRGB conversion keeps both paths correct without a per-pixel gamma function.

Application source lives under `Source`. The background shader is in
`Source/Shaders/Background.hlsl`; all reusable SimplePaint code, including
`SimplePaint.hlsl`, is in `Source/SimplePaint`. During the build, DXC compiles their vertex and
pixel entry points as optimized Shader Model 6.0 DXIL and emits byte arrays
embedded directly into the executable. No runtime shader compilation or shader
compiler DLL is required beside the executable.

The renderer handles resizing, DPI changes, minimizing/restoring, and GPU/CPU
frame synchronization. Only the two staged image/settings files are required
at runtime.

# [temp] PresentMon

```powershell
$gameProcesses = @(Get-Process -Name SimpleDirectX12Game -ErrorAction Stop)
if ($gameProcesses.Count -ne 1) { throw 'Run exactly one game instance.' }
$gameProcessId = $gameProcesses[0].Id
$captureTimestamp = Get-Date -Format 'yyyy-MM-dd_HH-mm-ss'
$captureDirectory = "$env:UserProfile\Repository\CPlusPlus\Simple_DirectX12_3D_Game\MyLogOutput\$captureTimestamp"
New-Item -ItemType Directory -Path $captureDirectory | Out-Null

& "$env:UserProfile\Program\PresentMon-2.5.1-x64.exe" `
    --process_id $gameProcessId `
    --session_name "SimpleDirectX12Game-$captureTimestamp" `
    --set_circular_buffer_size 65536 `
    --no_console_stats `
    --track_etw_status `
    --delay 5 --timed 300 --terminate_after_timed `
    --output_file "$captureDirectory\PresentMon.csv" `
    *> "$captureDirectory\PresentMon.log"

& "C:\Program Files\Git\usr\bin\winpty.exe" -Xallow-non-tty -Xplain `
    "$env:UserProfile\Program\PresentMon-2.5.1-x64.exe" `
    --process_id $gameProcessId `
    --session_name "SimpleDirectX12Game-$captureTimestamp" `
    --set_circular_buffer_size 65536 `
    --no_console_stats `
    --track_etw_status `
    --delay 5 --timed 30 --terminate_after_timed `
    --output_file "$captureDirectory\PresentMon.csv" `
    2>&1 | Tee-Object -FilePath "$captureDirectory\PresentMon.log"
```