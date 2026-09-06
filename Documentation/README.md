# Simple DirectX 12 3D Car Scene

## Documentation

- [SimplePaint usage](Usage.md): controls, numerical limits, and C++/DX12 integration.
- [SimplePaint specification](Specification.md): mathematics, rationale, and verification.
- [Asset generation](Assets.md): background and mesh regeneration instructions.
- [Historical reports](Reports/README.md): earlier rendering and numerical analyses with supporting data.

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

## Run

Double-click `Run.cmd` in File Explorer. This minimal wrapper starts `Run.ps1`,
which discovers Visual Studio, configures a 64-bit Release build with its
bundled CMake and Ninja, stages the runtime assets, builds, and launches the
game. Subsequent launches rebuild only changed files. If the repository or its
build folder has moved, the launcher automatically refreshes the saved CMake
configuration before building.

Requirements:

- Windows 10 or newer;
- a DirectX 12 adapter and driver supporting Shader Model 6.0 (hardware
  rendering is preferred; WARP software rendering is used as a fallback when
  it meets that requirement);
- Visual Studio 2022 or newer with the **Desktop development with C++**
  workload, the **C++ CMake tools for Windows** component (CMake 3.24 or newer),
  and a Windows SDK;
  and
- a current Windows SDK containing the DirectX Shader Compiler (`dxc.exe`).

The generated executable is `build\release\SimpleDirectX12Game.exe`. CMake
places `SceneBackground.png` and `Settings.ini` in its adjacent `assets`
directory.

## Controls

| Key               | Action                       |
| ----------------- | ---------------------------- |
| `V`               | Toggle VSync                 |
| `F11`             | Toggle borderless fullscreen |
| `Esc` or `Alt+F4` | Quit                         |

The app starts windowed at 1280x720 with VSync off. Press F11 to toggle borderless fullscreen.

With VSync off, presentation uses tearing when supported.

## Adjust the paint and sphere

Edit `assets/Settings.ini`, then relaunch through `Run.cmd`. Each of the Axles,
Body, Cabin, Headlights, Wheels, and Sphere sections has independent paint
controls. See [Usage.md](Usage.md) for accepted numerical ranges, examples,
and instructions for embedding the shader in another C++/DX12 project.
[Specification.md](Specification.md) defines the mathematics, numerical
contract, cutoff decision, GPU layout, optimizations, and test results.

The rewritten SimplePaint validates materials in C++ before uploading them.
It preserves the K12 Schlick curve, facing lobe warp, and dark/light tone
remap, while using positive contributions to preserve highlight accuracy.
It removes the positive facing cutoff and all input clamps and denominator
floors. The base colors in the shipped settings now obey the explicit
numerical domain.

In the same INI file, `[Sphere]` sets `UResolution = 64` (longitude segments,
3 to 512) and `VResolution = 32` (pole-to-pole latitude segments, 2 to 512).
Both must be integers. The sphere is generated at startup with smooth radial
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

`tools/GenerateAssets.py` uses Blender's own triangulation and evaluated corner
normals to turn `Blender/Car.blend` into the checked-in generated mesh header.
It also bakes the old static 3D scene into `assets/SceneBackground.png`. The
background is 32:9 so normal windows can center-crop it while preserving the
camera's vertical scale; its center half is a native 2560x1440 image at 16:9.
Windows wider than 32:9 use matching side mattes and a centered 32:9 scene
viewport, so the live objects never drift relative to the baked scene.
See [Asset generation](Assets.md) for the regeneration command. If Blender 4.5.12 or 5.2.0
is installed under `%UserProfile%\Program`, CMake also provides the explicit
`RegenerateAssets` target.

The renderer uses a two-buffer flip-discard swap chain, per-back-buffer command
allocators and fence values, default-heap mesh/texture resources, a persistent
mapped per-frame constant buffer, and an sRGB render-target view. The background
image is sampled as sRGB, while SimplePaint works in linear color; hardware
sRGB conversion keeps both paths correct without a per-pixel gamma function.

Shader sources live in `shaders/Background.hlsl` and
`shaders/SimplePaint.hlsl`. During the build, DXC compiles their vertex and
pixel entry points as optimized Shader Model 6.0 DXIL and emits byte arrays
embedded directly into the executable. No runtime shader compilation or shader
compiler DLL is required beside the executable.

The renderer handles resizing, DPI changes, minimizing/restoring, and GPU/CPU
frame synchronization. Only the two staged image/settings files are required
at runtime.
