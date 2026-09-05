# Simple DirectX 12 3D Car Scene

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

| Key | Action |
| --- | --- |
| `V` | Toggle VSync |
| `F11` | Toggle borderless fullscreen |
| `Esc` or `Alt+F4` | Quit |

The app starts windowed at 1280x720 with VSync off. Press F11 to toggle borderless fullscreen.

With VSync off, presentation uses tearing when supported.

## Adjust the paint and sphere

Edit `assets/Settings.ini`, then relaunch through `Run.cmd`. The settings are
loaded at startup. Each `[SimplePaintShader_Axles]`, `[SimplePaintShader_Body]`,
`[SimplePaintShader_Cabin]`, `[SimplePaintShader_Headlights]`,
`[SimplePaintShader_Wheels]`, and `[SimplePaintShader_Sphere]` section has its
own six K12 paint controls:

| Setting | Range | Effect |
| --- | ---: | --- |
| `Brightness` | 0 to 1 | Selects the facing angle where each base color appears |
| `Shift` | 0 to 1 | Moves the paint highlight sideways; 0 is symmetric |
| `RotationDegrees` | any degrees | Rotates the direction of a nonzero shift |
| `DarkPoint` | 0 to 1 | Tone used at zero facing |
| `LightPoint` | 0 to 1 | Tone used at maximum facing |
| `BaseColor` | sRGB triple, 0 to 1 | Base color for this material |

`[SimplePaintShader_GlobalParameters]` contains `FacingCutoff = 0.01`
(range 0 to 1), shared by all six materials. The shipped colors and paint
values preserve the existing appearance.
In the same INI file, `[Sphere]` sets `UResolution = 64` (longitude segments,
3 to 512) and `VResolution = 32` (pole-to-pole latitude segments, 2 to 512).
Both must be integers. The mesh is generated at startup with smooth radial
normals; restart through `Run.cmd` after editing the settings. The sphere has
radius 0.4 and a fixed center at `(1.5, 0.4, -1.5)` so it rests on the ground.

The [working input specification](docs/SimplePaintInputSpecification.md) defines proposed parameter limits for the numerical-stability review. These limits are not yet enforced by the implementation. See the [constrained analysis](Reports/ShaderNumerics/ConstrainedAnalysis.md) for the results.

The original
[K12 Godot shader](https://github.com/kvu787/SimplePaintShaders/blob/793126205e028f06f635f23e87a9bac856bf669a/Godot/ShaderTest/Shaders/K12.gdshader)
computes invariant material values in the vertex shader and carries them as 15
flat varyings. This port computes them once on the CPU. For the fixed
orthographic camera, it also removes K12's per-pixel view-basis construction
and replaces the slice/Schlick/remap sequence with an algebraically equivalent
one-square-root form. Input clamping, safe denominators, and saturated square
roots prevent the original pole and exact-black/white endpoint NaNs.

Orthographic projection is a permanent renderer invariant. Object and camera
transforms are affine: the vertex shader computes clip XYZ with three dot
products, supplies clip W = 1, and interpolates normals with noperspective.
OrthographicTransforms.h represents projection as three scales and a depth
offset; it builds packed transforms without a general projection-matrix multiply.
Normals still require per-pixel normalization, and depth testing remains enabled.

For nonzero paint shift, the vertex shader rotates both normal X/Y components
using that triangle's material. It passes the rotated normal to the pixel shader,
which normalizes it and uses X directly. This rotation preserves length and
commutes with interpolation because every triangle has one material. The
zero-shift path skips rotation, even when its configured angle is nonzero.

Paint constants are uploaded once and shared by both objects and frame slots.
The stationary sphere's transforms are populated at initialization and refreshed
after resize, while the GPU is idle. Only the car's 96-byte transform block is
updated each frame (each frame/object slot retains DirectX's 256-byte alignment).
Object transforms assume uniform scale, as the car and sphere already use.

## Assets and implementation

`tools/GenerateAssets.py` uses Blender's own triangulation and evaluated corner
normals to turn `Blender/Car.blend` into the checked-in generated mesh header.
It also bakes the old static 3D scene into `assets/SceneBackground.png`. The
background is 32:9 so normal windows can center-crop it while preserving the
camera's vertical scale; its center half is a native 2560x1440 image at 16:9.
Windows wider than 32:9 use matching side mattes and a centered 32:9 scene
viewport, so the live objects never drift relative to the baked scene.
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
