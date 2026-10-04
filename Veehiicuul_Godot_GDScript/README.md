# Veehiicuul with GDScript

This is a complete GDScript port of `Veehiicuul_Godot_CSharp`, for Windows 11 x64.
The application runs with standard Godot 4.7.2 and contains no C# scripts,
.NET projects, managed assemblies, or garbage collector calls.

The Godot project is in `Veehiicuul`. Open `Veehiicuul/project.godot` with the
standard editor. All gameplay scripts and the material import callback are
GDScript. The original C# application remains available separately.

## Build and run

Double-click `Build.cmd` to import assets, compile GDScript, and export an
optimized Windows x64 release to `Build/Veehiicuul_Godot_GDScript.exe` and its
adjacent `.pck`. It uses the command-line release exporter and matching release
templates. The scripts require this self-contained installation:

```text
%UserProfile%\Program\Godot_v4.7.2-stable_win64.exe\
    Godot_v4.7.2-stable_win64_console.exe
    _sc_
    editor_data\export_templates\4.7.2.stable\windows_release_x86_64.exe
```

The folder's `.exe` suffix is part of its existing name.

Double-click `Run.cmd` to run the export. It exits with an error if the EXE or
its `.pck` is missing. It launches with high process priority, as the original
launcher did. It does not rebuild the application.

Double-click `MyRun.cmd` to also record PresentMon 2.6.0 into the session folder.
This requires `%UserProfile%\Program\PresentMon-2.6.0-x64.exe` and Windows
administrator approval for its ETW capture. PresentMon terminates after the
target application exits. `MyRun_NoPresentMon.cmd` is an alias for `Run.cmd`.

Each build, launch, and verification session writes its logs under the root
`MyLogOutput/yyyy-MM-dd_HH-mm-ss` directory. Build outputs and session logs are
gitignored. Launcher logs, Godot output, console errors, clock calibration, and
any PresentMon CSV stay in that session directory.

Useful PowerShell commands from this folder:

```powershell
.\Build.ps1
.\Run.ps1
.\Run.ps1 -PresentMon
.\Run.ps1 -Windowed -QuitAfter 300
.\Run.ps1 -Headless -QuitAfter 30
.\Verify.ps1
.\Verify.ps1 -Render
```

`QuitAfter` counts engine iterations, not elapsed seconds. `Windowed` selects a
1280x720 window for checking the application. Normal runs use exclusive
fullscreen at the original 2560x1440 viewport resolution.

## Controller controls

Bindings and raw analog input use controller device 0, matching the original.

| Input                              | Action                            |
| ---------------------------------- | --------------------------------- |
| Right analog stick                 | Camera-relative acceleration      |
| Left trigger                       | Brake                             |
| D-pad left / right                 | Previous / next car               |
| D-pad down / up                     | Previous / next track             |
| X                                  | Reset car                         |
| Start                              | Toggle fixed / follow camera      |
| Right shoulder + left stick Y      | Camera zoom                       |
| Y                                  | Reset zoom                        |
| Left stick click                   | Log a player-observed stutter     |
| Back or keyboard Escape            | Quit                              |

The six car definitions, acceleration maps, asymmetric axial deadzones,
braking behavior, optional velocity limiter, yaw convention, initial yellow
car, camera sizes, and 350 ms control timeout after a car reset are preserved.
Track cycling reloads the track and its defaults even with a single track.
Opposing previous/next inputs cancel each other.

## Rendering and collision

The port keeps DirectX 12, Forward+, VSync disabled, no FPS limiter, two
swapchain images, and disabled Vulkan/OpenGL fallbacks. The editor import
callback preserves the original material settings, including disabled specular.

Collision uses Blender X/Y coordinates mapped to Godot X/negative Z. Vehicle
footprints are measured once from mesh geometry, including transformed children
and planar scale, and shortened by 0.165 at the front. Normal queries reuse the
last result for an unchanged car pose or query one cell of the precomputed
expanded grid. The Ribeye index has 800 edges, 23,904 cells, 6,499 occupied cells,
and 30,328 stored references, matching the C# application's index.

Perimeter contacts, including endpoints and collinear overlap, cause a reset.
Containment alone does not. As in the C# application, sampled collision allows
a sufficiently fast car to cross an outline between frames. Large or sparse
tracks use an AABB tree if the expanded grid exceeds its storage limits; unusual
footprints can use a general perimeter query. No arrays or scene objects are
constructed during a normal per-frame collision query.

This export has a different executable path from the C# version. For controlled
performance comparisons, check the corresponding NVIDIA application profile;
the port does not copy or modify driver profiles.

## Stutter timestamps

GDScript uses `Time.get_ticks_usec()` for elapsed timing and the reset timeout.
There is no equivalent application call to `.NET GC.Collect()`.

The stutter marker logs Godot monotonic microseconds and the engine frame count.
The launcher records a UTC/QPC calibration and passes its anchor to the
application. When launched through `Run.cmd` or `MyRun.cmd`, the marker also
includes `EstimatedQPC` for comparison with PresentMon. This is a UTC-based
estimate, not a direct raw QPC measurement; Windows clock resolution, calibration
timing, and clock corrections limit its accuracy. Editor launches without an
anchor log monotonic microseconds and Unix time instead.

## Verification

`Verify.cmd` runs deterministic behavior and application integration checks.
`Verify.cmd -Render` also saves fixed and follow camera PNGs into its session
directory using DirectX 12. Run `Build.cmd` before verification on a fresh clone
so imported resources and the global script class cache exist.

The suite covers 1,054 checks, including 960 indexed collision comparisons
against independent transforms and Godot's native segment intersection API
across all six cars. It also checks vehicle dynamics, braking, camera behavior,
car/track cycling, resets and their timeout, collision contacts, large-coordinate
fallback queries, and player stutter-marker input. Verification scripts are
excluded from the release export.

See `Documentation/HowToCreateANewTrack.md` for track preparation and import.
