# Simple DirectX 12 3D Car Scene

A small native Win32/C++ DirectX 12 scene moving toward the visual structure of
Zoom Tracks:

- a fixed orthographic camera with a 3/4 overhead view;
- one 3D car, sourced from `Blender/Car.blend`, moving between `x = -7` and
  `x = +7` at 8 units per second while rotating at 90 degrees per second;
- an optimized SimplePaint/K12 shader on the car; and
- one flattened 2D image containing the gray background, green ground, and
  static red cube.

The car is the only world-space 3D draw. The backdrop is a single textured
full-screen triangle and never uses the depth buffer. The car then renders in
one indexed draw with depth used only for its own surface occlusion. As in the
Zoom Tracks design, the car always composites above the flattened environment.

## Run

Double-click `Run.cmd` in File Explorer. This minimal wrapper starts `Run.ps1`,
which discovers Visual Studio, configures a 64-bit Release build with its
bundled CMake and Ninja, stages the runtime assets, builds, and launches the
game. Subsequent launches rebuild only changed files.

Requirements:

- Windows 10 or newer;
- a DirectX 12 adapter and driver supporting Shader Model 6.0 (hardware
  rendering is preferred; WARP software rendering is used as a fallback when
  it meets that requirement);
- Visual Studio 2022 or newer with the **Desktop development with C++**
  workload, the **C++ CMake tools for Windows** component, and a Windows SDK;
  and
- a current Windows SDK containing the DirectX Shader Compiler (`dxc.exe`).

The generated executable is `build\release\SimpleDirectX12Game.exe`. CMake
places `SceneBackground.png` and `CarPaint.ini` in its adjacent `assets`
directory.

## Controls

| Key | Action |
| --- | --- |
| `V` | Toggle VSync |
| `F11` | Toggle borderless fullscreen |
| `Esc` or `Alt+F4` | Quit |

The app starts windowed at 1280x720 with VSync off. Press F11 to toggle borderless fullscreen.

With VSync off, presentation uses tearing when supported.

## Adjust the car paint

Edit `assets/CarPaint.ini`, then relaunch through `Run.cmd`. The settings are
plain sRGB values and mirror the user-facing K12 controls:

| Setting | Range | Effect |
| --- | ---: | --- |
| `Brightness` | 0 to 1 | Selects the facing angle where each base color appears |
| `Shift` | 0 to 1 | Moves the paint highlight sideways; 0 is symmetric |
| `RotationDegrees` | any degrees | Rotates the direction of a nonzero shift |
| `DarkPoint` | 0 to 1 | Tone used at zero facing |
| `LightPoint` | 0 to 1 | Tone used at maximum facing |
| `FacingCutoff` | 0 to 1 | Front/back cutoff; K12 defaults to 0.01 |
| material colors | RGB, 0 to 1 | Base colors for axles, body, cabin, headlights, and wheels |

The original
[K12 Godot shader](https://github.com/kvu787/SimplePaintShaders/blob/793126205e028f06f635f23e87a9bac856bf669a/Godot/ShaderTest/Shaders/K12.gdshader)
computes invariant material values in the vertex shader and carries them as 15
flat varyings. This port computes them once on the CPU. For the fixed
orthographic camera, it also removes K12's per-pixel view-basis construction
and replaces the slice/Schlick/remap sequence with an algebraically equivalent
one-square-root form. Input clamping, safe denominators, and saturated square
roots prevent the original pole and exact-black/white endpoint NaNs.

## Assets and implementation

`tools/GenerateAssets.py` uses Blender's own triangulation and evaluated corner
normals to turn `Blender/Car.blend` into the checked-in generated mesh header.
It also bakes the old static 3D scene into `assets/SceneBackground.png`. The
background is 32:9 so normal windows can center-crop it while preserving the
camera's vertical scale; its center half is a native 2560x1440 image at 16:9.
Windows wider than 32:9 use matching side mattes and a centered 32:9 car
viewport, so the live car never drifts relative to the baked scene.
See `assets/README.md` for the regeneration command. If Blender 4.5.12 or 5.2.0
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
