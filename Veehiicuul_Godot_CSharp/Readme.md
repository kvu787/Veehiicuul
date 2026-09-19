# ZoomTracks C# learning port

This project ports all 40 runtime C# files from `ZoomTracks/Assets/Scripts` to
native Godot 4.7.2 .NET APIs. It builds, exports, and deliberately stops at startup.
Unity scenes, meshes, materials, shaders, and editor tools are outside this code
port. Removing the startup exception does **not** produce a playable game: the
required Godot scenes still need to be authored.

Start reading [Main.cs](Main.cs), then follow its calls into `Source`. The source
snapshot was ZoomTracks commit `b58ac18a28ee3f72712cdaf79d6d4ac9752e29d5`.
The 13 original StreamingAssets JSON files are included unchanged in `TrackData`.

## Execution and intentional failure

`Veehiicuul_Godot_CSharp.Main._Ready` is the only application startup entry point.
Its first statement inside the exception boundary throws `InvalidOperationException`.
No game initialization, diagnostic report, asset loading, or frame update runs.
Only exception reporting and immediate process termination follow the throw.
The narrowly scoped `CS0162` suppression keeps the deliberately unreachable
initialization code compiled and available to study.

`Main._Process(double delta)` is the only application update callback. All game
systems are ordinary synchronous C# objects called from these two methods. There
are no autoloads, application-created threads, async continuations, coroutines,
timers, input callbacks, or physics callbacks in the port. Godot and .NET still
have their own internal threads; this constraint concerns application code.
The pre-existing `DebugInfo` utility remains available but is never called by
startup or frame processing.

Godot normally catches and logs exceptions at its C# callback boundary. Both
callbacks therefore have explicit fatal exception boundaries. If the intentional
throw is later removed, `_Ready` installs the
[`FirstChanceException` handler](https://learn.microsoft.com/en-us/dotnet/api/system.appdomain.firstchanceexception?view=net-10.0)
before game initialization. This is deliberately strict: even a managed exception
that a library intended to catch ends the process. It does not turn native Godot
error messages into C# exceptions.

`QuitOnException` writes the exception to stderr and `Fatal.log`, then kills the
current process. No later gameplay or cleanup callbacks execute. This avoids the
CLR shutdown assertion observed when `Environment.Exit` was called from Godot's
native-to-managed callback. Windows reports exit code `-1` for this termination.
Reporting failures also terminate, with `Environment.FailFast` as the fallback
if killing the process fails. The Godot editor itself is not the game process.

## Build and run

On Windows x64, install .NET 10 and the Godot 4.7.2 .NET editor with its matching
export templates. `Run.ps1` uses the editor under
`%UserProfile%/Program/Godot_v4.7.2-stable_mono_win64`.

Double-click **Run.cmd** to build and export the Windows application, launch it,
and observe its intentional immediate exit. Each launcher session writes its
build, import, export, engine, and exception logs under
`MyLogOutput/yyyy-MM-dd_HH-mm-ss`. Generated logs and `Build` are ignored by Git.

```powershell
# Build/export without launching.
.\Run.cmd -BuildOnly

# Build/export and assert the intended startup failure. Success returns 0.
.\Run.cmd -VerifyStartupFailure

# Compile only, with warnings treated as errors.
dotnet build Veehiicuul_Godot_CSharp.slnx --configuration Debug -warnaserror
```

The runtime port uses portable Godot/.NET APIs. The launcher/export preset and
the actual runtime verification target Windows x64. Linux/macOS exports have not
been tested. The existing Windows-only system report is not invoked.

## Unity to Godot reading guide

| Unity concept                  | Godot code in this project                                      |
| ------------------------------ | -------------------------------------------------------------- |
| `MonoBehaviour.Awake/Start`     | Explicit initialization under `Main._Ready`                     |
| Awaitable frame loop           | One call to `Main._Process(delta)` per rendered frame           |
| `GameObject` and `Transform`    | `Node3D`, `GlobalPosition`, `GlobalBasis`, and `Transform3D`       |
| `Instantiate` and active state | `Node.Duplicate`, `AddChild`, and `Node3D.Visible`                |
| Additive scene loading         | Synchronous `PackedScene` loading, instantiation, and parenting  |
| Unity input system             | One snapshot using `Godot.Input` key/button/axis polling         |
| `Time.deltaTime`               | The `delta` parameter passed into `TimeManager.Update`          |
| TMP text                       | `Label.Text`                                                   |
| URP graphics settings          | Live `Viewport`, `DisplayServer`, and `Engine` settings          |
| `JsonUtility`/StreamingAssets   | `System.Text.Json`, Godot `FileAccess`, and `res://TrackData`      |
| Unity exception log hook       | Explicit callback boundaries plus .NET first-chance reporting  |

Unity vectors `(x, y, z)` map to Godot `(x, y, -z)`. Vehicle forward is Godot `-Z`.
The driving code retains clockwise yaw in degrees; native quaternion yaw uses
the negative angle in radians. The collision algorithm still uses its original
two-dimensional plane, with plane Y equal to negative Godot Z. Unity orthographic
camera size is a half-height; Godot camera size is twice that value with
`KeepAspect = Height`.

Graphics differences are explicit: Godot exposes TAA on/off rather than Unity's
five quality tiers, and there is no URP asset to clone/restore. Forward+ 3D HDR and
Unity's `maxQueuedFrames` do not have matching per-session switches. The project
retains its two-image swapchain and Dummy physics backends. The inactive original
`CollisionManager` still returns false; `CollisionManager2` is the active detector.

Scene loading now blocks in the calling method. The old per-frame "Busy" loop
is replaced by elapsed-time loading messages. Godot reference counting and explicit
node destruction replace Unity's unused-assets operation. The car control timeout
uses elapsed frame delta instead of wall-clock time. Stutter logs always belong to
the session folder, so the old arbitrary `-stutterLogFilePath` override is removed.
The `-refreshRate` user argument is retained for future fixed-timestep experiments.

## Scene contracts for later exploration

The code expects `Scenes/Ui.tscn` with direct `Label` children `ClockText`,
`CameraSizeText`, `DisplayModeText`, and `FpsText`.

Tracks are `Tracks/Basic.tscn` and `Tracks/Track001.tscn` through
`Tracks/Track005.tscn`, each with a `Node3D` root. Each track needs:

- A direct `SlopeCarPlaceholder` child at Y=0 with yaw-only rotation, unit scale,
  and children named `CarFL`, `CarFR`, `CarRL`, and `CarRR`.
- Decorative `Node3D` car models named by each JSON `GameObjectName`, with
  descendant `MeshInstance3D` geometry for footprint bounds.
- The hierarchy `CameraPanAndYaw / CameraYawOffset / CameraPanOffsetAndPitch / Camera`.
  `Camera` is a `Camera3D`. All rig scales are one. The yaw offset has zero local
  position and rotation; the pitch node has zero position and rotation degrees
  `(-45, 0, 0)`. The camera has local position `(0, 0, 500)`, zero rotation,
  orthogonal projection, height-preserving aspect, near=1 and far=1000.
- A camera/world `Environment` with solid background color `#404040`.

Keep these scenes passive, without scripts or signals that execute gameplay.
The loader rejects attached scripts and disables processing on loaded nodes.
The code deliberately throws clear errors for absent resources or invalid nodes;
it does not silently substitute placeholder gameplay.

## Verification

All three app configurations (`Debug`, `ExportDebug`, `ExportRelease`) compile
with zero warnings/errors. Windows release export and startup failure were tested
both headless and with Direct3D 12. `Run.cmd -VerifyStartupFailure` checks the exit
status and exception origin; the startup throw is never disabled by a test flag.

These separate console programs are excluded from the Godot app and its resource
scanner, so their entry points are not application callbacks:

```powershell
dotnet run --project Verification/CollisionDetection/CollisionDetectionVerification.csproj --configuration Release
dotnet run --project Verification/PlanarCoordinates/PlanarCoordinatesVerification.csproj --configuration Release
dotnet run --project Verification/ExceptionHandling/ExceptionHandlingVerification.csproj --configuration Release
```

Collision verification checks 12,160 queries against the linear oracle, all 1,984
Track001 edges, all three closing edges, and zero allocations over 100,000 queries.
Coordinate verification checks 11,544 comparisons plus cardinal headings. The
exception verifier launches separate child processes to check caught exceptions,
direct termination, and fatal-log write failures. Full gameplay, imported vehicle
geometry, visuals, and input hardware behavior remain untested because the scenes
are intentionally outside this port.

The complete source inventory is in [Documentation/SourceMap.md](Documentation/SourceMap.md).
