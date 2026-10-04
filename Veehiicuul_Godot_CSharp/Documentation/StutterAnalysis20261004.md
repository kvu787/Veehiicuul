# Stutter analysis of the MyDefaultWindowsDesktop export

Audit date: 2026-10-04. Repository revision before this audit: `2e11e1b0ee658c4df6595029aa2a0c048851cbc1`.

**There are identifiable ways for this application to hitch or show discontinuous motion. The available evidence does not establish a single cause for rare hitches during uninterrupted driving.** The strongest application findings are synchronous track reloads, deliberate reset pauses, and Godot's remaining process-delta adjustment. Native rendering, input processing, and first-use compilation also contain paths that can stall the main thread.

The game was not run. Godot was not started, built, or used to re-export the project. No application code, settings, assets, launchers, or exported files were changed. The checks consisted of source inspection, reading binary metadata and packaged data, reanalyzing existing logs, and running two isolated algorithm checks without the game or Godot runtime.

The scope is `Veehiicuul_Godot_CSharp`, specifically the `MyDefaultWindowsDesktop` preset and the existing `Veehiicuul/MyBuildOutput` export. Engine implementation references are to the requested local Godot 4.7.2 source, revision `ed1daf0bf001b61586d9930840f2f1394092c079`. That source checkout was clean. A [companion evidence file](StutterAnalysisEvidence20261004.json) preserves hashes, packaged settings, source-checksum verification, model counts, and experiment results.

## Findings at a glance

"Confirmed" below means the behavior exists in the audited code or isolated algorithm. It does not mean it caused a particular recorded hitch. Investigation order considers both likely impact and how directly the behavior can be verified.

| Order | Finding                                                                    | Evidence and scope                                                                                                                                         |
| ----- | -------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------- |
| 1     | Switching tracks reloads the same sole track synchronously                 | Confirmed main-thread load, destruction, instantiation, JSON parsing, car duplication, and collision-index construction. Triggered by D-pad up/down.       |
| 2     | Collision, car selection, and reset intentionally stop movement for 350 ms | Confirmed visible pause and position reset; frames can continue normally.                                                                                  |
| 3     | `physics_jitter_fix=0.5` still changes `_Process` delta                    | Confirmed in the exact isolated engine timer algorithm even with delta smoothing disabled and Dummy physics. Can extend motion irregularity after a hitch. |
| 4     | Main-thread rendering can wait on D3D12 fences and presentation            | Confirmed blocking paths with VSync disabled and two buffers. Whether they cause the observed rare stalls is unproven.                                     |
| 5     | First-use managed JIT, shader work, and pipeline creation remain possible  | Confirmed opportunities; most relevant at startup, reload, and first use of a render state or code path. No timing attribution to the historical stalls.   |
| 6     | SDL controller processing reaches locks and device work on the main thread | Confirmed possible blocking path despite the joystick-thread hint. Needs a call-stack trace to establish relevance.                                        |
| 7     | Unchanged car and camera transforms are resent every frame                 | Confirmed avoidable native work; a baseline efficiency issue, not evidence of a 10-20 ms stall by itself.                                                  |
| 8     | Marker logging and capture helpers add work                                | Confirmed conditional work. Marker logging occurs after the player notices a stutter; capture overhead has not been isolated.                              |

The current collision query, explicit garbage collection, an accidental debug export, and continuous asset loading are not supported as explanations for ordinary steady driving. Details and limits follow.

## Verify what was actually exported

The analysis did not assume that the current source or the other Windows preset described this build.

* The exported EXE is x64. Its complete `.text` section matches the installed Godot 4.7.2 .NET **release** template and does not match the debug template. The EXE has modified Windows resources, so comparing only its whole-file hash to a template would have been misleading.
* The managed assembly says `ExportRelease`. Its `DebuggableAttribute` flags are `2`, without `DisableOptimizations`. The project also explicitly enables optimization for `ExportRelease`.
* The DLL's CodeView identity matches the exported portable PDB. All 35 PDB documents available on disk matched their recorded source checksums. Eight virtual source-generator documents were unavailable; none of the available documents mismatched. All 33 C# files under `Source` were inspected. The two additional available PDB documents are generated build files.
* The exported `Main.Process` IL was inspected as data. It contains no call to `GC.Collect` or the forced-collection helper. This verifies the commented-out state in the source against the DLL.
* The separate PCK is 845,824 bytes, format version 4, marked Godot 4.7.2, with 55 entries. Every stored entry checksum validated. Its `project.binary` was decoded to verify the actual packaged settings. The relevant JSON and imported model payloads match the local files. C# source stubs and export remapping files differ as expected during export.
* The shipped runtime is self-contained .NET 10.0.12. The game assembly has no ReadyToRun native header; optimized IL still needs JIT compilation.

| Artifact                                    | SHA-256                                                            |
| ------------------------------------------- | ------------------------------------------------------------------ |
| `MyBuildOutput/Veehiicuul_Godot_CSharp.exe` | `dcb991b28fccd8156b11c4a59c84e2638759f5e177f8d4e571d77cae801c10ee` |
| `MyBuildOutput/Veehiicuul_Godot_CSharp.pck` | `e7c489a607d6481c3de4a8b968cc94addff0f400cf126b3a6290eb4d2798f567` |
| Exported `Veehiicuul_Godot_CSharp.dll`      | `d089c5432a72b0ddacbe765fed03816d94dead0e33caffaa80bcf88c65467ded` |

The EXE and PCK were last written at approximately **2026-10-04 21:31:32 UTC / 14:31:32 PDT**. The historical session discussed below predates this export. Matching present source to this DLL does not identify which DLL was used in that earlier session.

### Effective configuration

Values explicitly present in the PCK were checked against the source configuration. Omitted values below were checked against this Godot version's defaults; command-line, runtime, driver, or environment overrides in a future launch could still affect behavior.

| Setting                          | Effective value                                   | Relevance                                                                              |
| -------------------------------- | ------------------------------------------------- | -------------------------------------------------------------------------------------- |
| Renderer and driver              | Forward+, D3D12                                   | Analysis uses the D3D12 implementation, not Vulkan behavior.                           |
| VSync / maximum FPS              | Disabled / 0, unlimited                           | No intentional normal-frame limiter; synchronization waits still exist.                |
| Swapchain images                 | 2                                                 | Verified packaged setting.                                                             |
| Rendering-device frame queue     | Default 2                                         | A separate setting from swapchain image count.                                         |
| Render thread model              | Default 1, Safe                                   | Does not select the separate render-thread mode.                                       |
| Window / viewport                | ExclusiveFullscreen setting; 2560 x 1440 viewport | 1280 x 720 overrides are window-size overrides, not proof of a 720p fullscreen render. |
| Vulkan / OpenGL fallbacks        | Both false                                        | Preserved; no recommendation to change renderer.                                       |
| Delta smoothing                  | False                                             | Does not disable the separate timer adjustment below.                                  |
| Physics backends                 | Dummy for both 2D and 3D                          | No real rigid-body simulation, but engine timer bookkeeping continues.                 |
| Physics timer / jitter fix       | Default 60 Hz / 0.5                               | Relevant to the process delta despite movement occurring in `_Process`.                |
| Maximum physics steps per frame  | Default 8                                         | Very long stalls have an additional catch-up limit.                                    |
| Physics interpolation            | Default false                                     | No evidence that car transforms are being interpolated at a separate 60 Hz cadence.    |
| Low-processor mode / frame delay | False / 0                                         | No intentional sleep during ordinary drawable frames.                                  |
| Shader baking                    | False                                             | This export carries no baked shader cache.                                             |
| MSAA / screen-space AA / TAA     | Disabled defaults                                 | Aliasing can affect perceived motion quality; not a frame-time stall.                  |
| Depth prepass                    | False                                             | Can change rendering cost and overdraw; does not itself imply a periodic hitch.        |

Default definitions and frame-loop behavior: [main.cpp:2252](C:/Users/k/Repository/External/Godot_4-7-2/main/main.cpp:2252), [main.cpp:2776](C:/Users/k/Repository/External/Godot_4-7-2/main/main.cpp:2776), [rendering_device.cpp:8368](C:/Users/k/Repository/External/Godot_4-7-2/servers/rendering/rendering_device.cpp:8368).

### Preset and launcher traps

`Build.ps1` explicitly exports **`Windows Desktop` to `Build`**, not the preset and directory requested here. `Run.ps1` uses that other build. The `MyRun` launchers use `MyBuildOutput`. A future comparison using `Build.cmd` followed by `MyRun` could silently measure an older build. This is a reproducibility issue rather than a runtime stutter cause. See [Build.ps1:47](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Build.ps1:47).

For `MyDefaultWindowsDesktop`, `application/export_d3d12=0` means **Auto**, not disabled. The exporter checks the D3D12 driver and copies Agility SDK files when those template files exist. The inspected build/template do not contain `D3D12Core.dll`; Godot can use the system D3D12 runtime. This is independent of the disabled Vulkan/OpenGL fallbacks. No evidence ties that SDK choice to a hitch. See [Windows export_plugin.cpp:273](C:/Users/k/Repository/External/Godot_4-7-2/platform/windows/export/export_plugin.cpp:273) and [rendering_context_driver_d3d12.cpp:91](C:/Users/k/Repository/External/Godot_4-7-2/drivers/d3d12/rendering_context_driver_d3d12.cpp:91).

Including PDBs does not turn an optimized release into a debug build. The empty export exclusion filter includes a small editor-import script, but there is no runtime attachment that executes it. The PCK contains neither a recursively included build directory nor accumulated log data. These are not demonstrated stutter causes.

## 1. Synchronous track reload is the strongest direct hitch trigger

`TrackNames` contains only `Ribeye`. D-pad previous/next track wraps the index back to that same entry. `ReadInputAndSwitchTracks` does not check whether the destination index equals the current index before it:

1. Reads and deserializes track settings.
2. Calls `CurrentTrackScene.Free()` synchronously.
3. Loads a `PackedScene` with `ResourceLoader.CacheMode.Ignore`.
4. Instantiates and adds the replacement scene.
5. Returns to `Main.Process`, which reconstructs the managers, duplicates the six playable cars, computes their collision footprints, reads collider JSON, and constructs the collision index.

All of this happens inside a frame. Native resource allocation, render-resource creation, JSON reflection/JIT on first use, managed allocation, scene notifications, and later deferred GPU cleanup can contribute. `Ignore` bypasses the requested resource's normal reuse; it is not `IgnoreDeep`, so it would be incorrect to say every dependent mesh and material is necessarily reloaded from disk each time.

References: [TrackSwitcher.cs:36](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Source/GameDataAndLogic/Track/TrackSwitcher.cs:36), [SceneUtility.cs:11](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Source/Utility/SceneUtility.cs:11), [Main.cs:85](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Source/Main.cs:85), [TrackNodes.cs:29](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Source/GameDataAndLogic/Track/TrackNodes.cs:29).

The isolated collision-index constructor alone took 6.4054 ms on its first call, including first-use JIT, and allocated 447,064 managed bytes after JSON had already been parsed. This is not an in-game timing and does not measure the complete reload. It does show why treating index construction as per-frame-safe work would be wrong.

**Recommended change:** make same-index track selection a no-op unless an explicit reload is desired. For actual transitions, prepare reusable data before the transition and move suitable resource loading out of the critical frame, while keeping Godot scene-tree operations on their required thread. Replacing `Free()` with `QueueFree()` alone would postpone destruction without eliminating the load and construction work.

This trigger does not explain a hitch when the player has not selected a track and the track has not reinitialized.

## 2. Several visible interruptions are application behavior

On collision, car selection, or X/reset, the application restores position and rotation, zeros velocity, and suppresses acceleration and position updates for **0.35 seconds**. The following camera can jump with the reset car. This is a confirmed pause in motion while rendering remains active. At 120 Hz it spans about 42 display refreshes.

Collision is checked against the pose at the beginning of the frame, before the next movement update. Contact reached by that update is handled on a later frame. Collision sampling is intentionally discrete, not swept. At sufficiently high speed or after a long delta, the vehicle can cross a boundary between samples. The settings omit a positive `VelocityLimiter`, so this code does not impose a speed ceiling.

The timeout uses `DateTime.Now`, not a monotonic elapsed-time source. A system-clock adjustment can shorten or extend it. This is a conditional correctness issue, not evidence that Windows clock adjustment caused any observed stall. Use `Stopwatch` timestamps for elapsed timeout duration if that behavior is revised.

References: [Main.cs:90](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Source/Main.cs:90), [Main.cs:123](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Source/Main.cs:123), [CarStateManager.cs:140](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Source/GameDataAndLogic/Car/CarStateManager.cs:140), [CollisionManager.cs:53](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Source/GameDataAndLogic/CollisionDetection/CollisionManager.cs:53).

Other smaller motion discontinuities are possible:

* Follow/fixed camera switching and zoom reset apply their new values immediately. They do not ease the camera transition.
* Car yaw is recalculated for any nonzero velocity. `MinVelocityForRotation` exists in track data but is unused. Near zero speed, a small change in velocity direction can produce a noticeable rotation change without a slow frame.
* Velocity and position use variable-step integration. An unusually large or transformed delta changes the distance and acceleration applied in that frame. This is relevant even when render performance returns to normal immediately afterward.

References: [CameraZoomManager.cs:54](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Source/GameDataAndLogic/Camera/CameraZoomManager.cs:54), [CarStateManager.cs:131](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Source/GameDataAndLogic/Car/CarStateManager.cs:131).

These distinctions matter when a player reports a stutter but PresentMon shows no long interval. Presentation timestamps alone cannot show whether the world advanced smoothly.

## 3. Disabling delta smoothing leaves another delta adjustment active

`application/run/delta_smoothing=false` bypasses `DeltaSmoother`. However, `MainTimerSync::advance()` still calls `advance_checked()`. That function applies clamps related to physics timing and `physics_jitter_fix`, whose effective value here is the default **0.5**. Dummy physics backends do not bypass this timer code.

The game uses the resulting delta for both velocity and position. Consequently, engine presentation intervals and game motion increments need not correspond exactly, even though movement is exclusively in `_Process`.

References: [main_timer_sync.cpp:252](C:/Users/k/Repository/External/Godot_4-7-2/main/main_timer_sync.cpp:252), [main_timer_sync.cpp:432](C:/Users/k/Repository/External/Godot_4-7-2/main/main_timer_sync.cpp:432), [main_timer_sync.cpp:542](C:/Users/k/Repository/External/Godot_4-7-2/main/main_timer_sync.cpp:542), [main.cpp:4938](C:/Users/k/Repository/External/Godot_4-7-2/main/main.cpp:4938).

### Isolated reproduction

The exact `MainFrameTime` and `MainTimerSync` code was extracted from the specified engine checkout and compiled with MSVC optimization. The only substitutions were a minimal Engine object supplying the chosen jitter setting and a delta smoother that returns its input, matching this project's disabled smoothing. No engine or game was linked or started. Each scenario had 50,000 frames, discarding 1,000 warmup frames, a 60 Hz physics timer, and no fixed-FPS override.

| Synthetic input intervals            | Altered deltas with jitter fix 0.5 | Altered deltas with jitter fix 0 |
| ------------------------------------ | ---------------------------------- | -------------------------------- |
| Constant 0.800 ms                    | 0                                  | 0                                |
| Alternating 0.700 / 0.900 ms         | 0                                  | 0                                |
| 0.800 ms with one 19.382 ms interval | 18                                 | 0                                |
| Alternating 0.750 / 2.100 ms         | 48,976 of 49,000                   | 0                                |
| Alternating 7.000 / 9.600 ms         | 49,000 of 49,000                   | 0                                |

In the isolated single-hitch scenario, a real input interval of **19.382 ms** became a process delta of **11.048667 ms**, followed by multiple deltas around **1.388889 ms** despite the input intervals having returned to **0.800 ms**. One following delta was approximately **0.100 ms**. The algorithm redistributes the simulation-time advancement rather than simply forwarding each interval.

The 19.382 ms input was chosen to resemble the historical presentation-gap magnitude. **It is not a reconstruction of that game's main-loop timestamps**, which were not logged. The test proves the mechanism, not its exact contribution in the historical session. The steady high-FPS scenarios also show that this setting does not automatically produce constant jitter at all high frame rates.

**Recommended experiment:** compare `physics/common/physics_jitter_fix=0.0` with the present value while recording raw monotonic interval, supplied process delta, and actual pose change. This is particularly relevant because this application does not use real physics simulation or physics interpolation. It would address motion timing, not remove a driver wait or a scene-loading stall. No setting was changed during this audit.

Separately, `main.cpp` reduces process advancement when physics catch-up exceeds the configured eight steps. A stall longer than roughly eight 60 Hz steps can therefore encounter another delta limit. Setting jitter fix to zero does not promise literal wall-clock delta under every possible engine condition. See [main.cpp:4955](C:/Users/k/Repository/External/Godot_4-7-2/main/main.cpp:4955).

## 4. D3D12 synchronization remains a stall path with VSync off

The engine does not use a separate rendering thread with the effective thread-model setting. Rendering submission, main-loop work, and associated blocking calls can therefore delay the next application update.

At frame reuse, `RenderingDevice::_begin_frame()` invokes `_stall_for_frame()`. If that frame has a signaled fence outstanding, it calls the D3D12 driver's fence wait. The driver calls `SetEventOnCompletion` and **`WaitForSingleObjectEx(..., INFINITE, ...)`**. This is a real wait path; VSync disabled does not remove the requirement to wait before reusing resources still needed by the GPU. A completed fence may make the wait return immediately, so the existence of the call does not establish a long wait.

References: [rendering_device.cpp:8048](C:/Users/k/Repository/External/Godot_4-7-2/servers/rendering/rendering_device.cpp:8048), [rendering_device.cpp:8217](C:/Users/k/Repository/External/Godot_4-7-2/servers/rendering/rendering_device.cpp:8217), [rendering_device_driver_d3d12.cpp:2398](C:/Users/k/Repository/External/Godot_4-7-2/drivers/d3d12/rendering_device_driver_d3d12.cpp:2398).

The disabled-VSync branch selects sync interval zero and allows tearing when supported, but `Present()` is still a driver/OS call. The Microsoft API documentation also describes synchronization and thread interactions; zero sync interval does not mean every presentation call is guaranteed to return immediately. See [D3D12 presentation:2506](C:/Users/k/Repository/External/Godot_4-7-2/drivers/d3d12/rendering_device_driver_d3d12.cpp:2506) and [Microsoft's Present documentation](https://learn.microsoft.com/en-us/windows/win32/api/dxgi/nf-dxgi-idxgiswapchain-present).

Other conditional synchronization exists for transfer completion, exhausted upload staging space, resource retirement, and swapchain resize. The application does not continuously upload new meshes/textures or perform GPU readbacks, so these are chiefly startup, reload, resize, or driver-pressure candidates here. Source existence alone is not a reason to blame them for every rare steady-state hitch.

Godot's `ExclusiveFullscreen` window-mode label also does not prove traditional DXGI exclusive mode. This D3D12 backend creates a flip-discard swapchain for the HWND with no fullscreen descriptor and does not establish DXGI fullscreen via `SetFullscreenState`. The actual historical PresentMon mode, **Hardware: Independent Flip**, is more useful evidence than the label. See [swapchain creation:2805](C:/Users/k/Repository/External/Godot_4-7-2/drivers/d3d12/rendering_device_driver_d3d12.cpp:2805).

Running uncapped gives the system less idle time and makes occasional scheduling delays visible against very short ordinary frames. CPU descheduling, driver work, interrupts/DPCs, power-state changes, and GPU scheduling remain possible, but no supplied trace contains the stacks and scheduling detail needed to choose among them. VSync, the FPS limit, buffer count, renderer, and fallbacks were preserved.

## 5. Shader and pipeline first use is reduced, not eliminated

The selected export disables shader baking and its PCK contains no shader-cache payload. The current user-data directory does contain shader-cache files, but their presence now does not prove that all needed variants were cached during an earlier run.

Godot 4.7.2 Forward+ has asynchronous specialization and an ubershader fallback. Therefore, the simplistic claim that every new material must block to compile a specialized pipeline is wrong. Nevertheless, the fallback or another required pipeline can still need completion. `get_pipeline` can wait; mesh preparation also contains explicit pipeline waits. Shader variants can need cache loading or compilation, and D3D12 PSO creation reaches `CreateGraphicsPipelineState`.

References: [render_forward_clustered.cpp:498](C:/Users/k/Repository/External/Godot_4-7-2/servers/rendering/renderer_rd/forward_clustered/render_forward_clustered.cpp:498), [pipeline_hash_map_rd.h:155](C:/Users/k/Repository/External/Godot_4-7-2/servers/rendering/renderer_rd/pipeline_hash_map_rd.h:155), [render_forward_clustered.cpp:5033](C:/Users/k/Repository/External/Godot_4-7-2/servers/rendering/renderer_rd/forward_clustered/render_forward_clustered.cpp:5033), [shader_rd.cpp:723](C:/Users/k/Repository/External/Godot_4-7-2/servers/rendering/renderer_rd/shader_rd.cpp:723), [D3D12 PSO creation:5358](C:/Users/k/Repository/External/Godot_4-7-2/drivers/d3d12/rendering_device_driver_d3d12.cpp:5358).

This scene is largely static, with no continual material generation. Startup and track reconstruction are stronger triggers than prolonged unchanged driving. Car selection does not load a new scene or rebuild the collision index: all six cars already exist. Also, the original decorative car meshes remain in the track, so a car's material may already have been encountered before selection.

There is an important backend-specific exclusion: **the D3D12 driver's pipeline-cache create/serialize operations are unimplemented in this source**. Periodic persistence of a Vulkan-style Godot pipeline cache is not a supported explanation for this D3D12 build. That does not eliminate Godot's separate shader cache or the graphics driver's internal cache. See [rendering_device_driver_d3d12.cpp:4216](C:/Users/k/Repository/External/Godot_4-7-2/drivers/d3d12/rendering_device_driver_d3d12.cpp:4216).

A useful later measurement is the change in Godot's pipeline-compilation counters alongside a hitch. The `DRAW`, `MESH`, `SURFACE`, and `SPECIALIZATION` counters distinguish situations better than merely observing that a shader cache directory exists. See [performance.cpp:277](C:/Users/k/Repository/External/Godot_4-7-2/main/performance.cpp:277). Shader baking or explicit preparation should be evaluated using those measurements, not assumed to eliminate all stalls.

## 6. .NET allocation and compilation

The ordinary movement/input/collision path has no obvious growing collection, LINQ enumeration, recurring JSON work, or per-frame allocation of reference-type game objects. Vectors, poses, and collision temporaries are value types. Input action `StringName` objects are cached. Native Godot calls can still do work outside the managed allocation measurements.

The explicit `GC.Collect` block and initial/reload forced collection block are commented out and absent from the exported `Main.Process` call sequence. Merely keeping the helper method or an unused `RunInitialForceGarbageCollection` field in the assembly does not invoke it. Old `.uid` files named `StutterLogger` or `TimeManager` do not provide executable classes.

**Automatic GC is still enabled.** The runtime configuration does not disable it or request a no-GC region. .NET's ordinary defaults include workstation GC and background GC; background collection does not imply that every collection phase is pause-free. Startup and track reloads allocate JSON objects, scene wrappers, arrays, and collision-grid storage. Some grid arrays are large enough for the large-object heap. Future collections remain possible even though the historical session below recorded none. See [Microsoft's GC configuration documentation](https://learn.microsoft.com/en-us/dotnet/core/runtime-config/garbage-collector).

Optimized IL is also not ahead-of-time machine code. First calls into game, framework, serialization, and binding methods can require JIT work. Default tiered compilation can subsequently replace initial code with optimized code. These are conditional candidates, especially during early frames or first input use; they are not evidence that tiering caused a specific hitch. The exported runtime config contains no tiering overrides. See [Microsoft's compilation settings](https://learn.microsoft.com/en-us/dotnet/core/runtime-config/compilation).

ReadyToRun can reduce some first-use JIT work but does not eliminate all JIT and can change size/startup tradeoffs. Consider it only as a controlled follow-up if JIT events correlate with the problem. Disabling GC, inserting forced GC into the frame loop, or switching to unverified NativeAOT is not justified by this audit. See [Microsoft's ReadyToRun documentation](https://learn.microsoft.com/en-us/dotnet/core/deploying/ready-to-run).

The cached input names are not disposed by `Main`, but this is a small fixed set created once, not an increasing allocation stream in normal play. Native object/resource lifetime around repeated scene replacement deserves more attention than that bounded lifetime issue. Godot editor/plugin unload code containing forced collection is not the normal exported-game frame path.

## 7. Collision detection is a weak steady-state suspect

The current detector uses a precomputed expanded spatial grid. With the audited Ribeye geometry and vehicle envelope, construction produced the same grid statistics as the existing game log:

| Quantity                                         | Result                   |
| ------------------------------------------------ | ------------------------ |
| Outline / edge count                             | 3 / 800                  |
| Grid cells / occupied cells                      | 23,904 / 6,499           |
| Stored edge references                           | 30,328                   |
| Cell size                                        | Approximately 1.50000036 |
| Maximum candidate edges in any cell              | 20                       |
| 99th percentile candidate count across all cells | 12                       |
| Median candidate count across all cells          | 0                        |

The per-query path does not scan all 800 edges. It selects the current cell's candidate list, performs bounded rectangle/segment work, and returns early where possible. Repeating the exact same car/pose is also cached by `CollisionManager`. Mesh traversal and AABB extraction occur when the manager is constructed, not on every collision query. The old exact-decimal/`BigInteger` utilities are not used in this hot path.

For an isolated check, the unchanged pure collision source was compiled with optimization in a separate PowerShell process, without Godot bindings or the game assembly. It queried every grid-cell center with 16 deterministic orientations, warmed that sequence three times, and measured eight passes: **3,059,712 measured queries and zero managed bytes allocated on the calling thread**. The representative spawn pose did not collide.

The average was about 0.064 microseconds per query on that microbenchmark, but most grid cells are empty. This is neither a worst-case latency bound nor a game-frame prediction. The test ran on .NET 10.0.11, while the export ships 10.0.12; it also excludes scheduling interference, Godot/native overhead, and arbitrary future collider data. The structural bound and absence of query allocations are stronger evidence than the average timing.

References: [CollisionManager.cs:23](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Source/GameDataAndLogic/CollisionDetection/CollisionManager.cs:23), [CollisionManager.cs:55](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Source/GameDataAndLogic/CollisionDetection/CollisionManager.cs:55), [TrackCollisionDetector.ExpandedGrid.cs](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Source/GameDataAndLogic/CollisionDetection/TrackCollisionDetector.ExpandedGrid.cs).

**Conclusion for collision:** construction is material to startup/reloads; routine queries are not a credible leading explanation for rare multi-millisecond hitches on this track without contrary measurements. Collision-triggered resets can still explain visible interruptions.

## 8. Native input can stall outside the C# update

Windows event processing occurs before `Main::iteration`. The SDL controller event pump is part of this path. Setting SDL's joystick-thread hint does not move every operation off the main thread: `SDL_PollEvent` can pump maintenance, which calls `SDL_UpdateJoysticks`; that function locks the joystick state, updates devices/backends, and performs detection.

Mutex contention, a slow device/backend operation, or hotplug work can therefore delay a frame before `Main.Process` is entered. This is especially relevant when diagnosing a controller-driven application, but the source does not prove that the user's controller or its polling rate caused a stall. A duration measured only inside the C# callback would miss this delay.

References: [os_windows.cpp:2352](C:/Users/k/Repository/External/Godot_4-7-2/platform/windows/os_windows.cpp:2352), [display_server_windows.cpp:4424](C:/Users/k/Repository/External/Godot_4-7-2/platform/windows/display_server_windows.cpp:4424), [joypad_sdl.cpp:125](C:/Users/k/Repository/External/Godot_4-7-2/drivers/sdl/joypad_sdl.cpp:125), [SDL_events.c:1386](C:/Users/k/Repository/External/Godot_4-7-2/thirdparty/sdl/events/SDL_events.c:1386), [SDL_joystick.c:2501](C:/Users/k/Repository/External/Godot_4-7-2/thirdparty/sdl/joystick/SDL_joystick.c:2501).

`Input.UseAccumulatedInput=false` is set intentionally. It avoids the normal accumulation behavior but can increase work under dense input. The inspected Windows source already includes special handling for high-rate raw mouse input and limits ordinary mouse-move processing; attributing this build to an old unbounded mouse-message-pump problem would ignore the actual version. There is no evidence here that changing input accumulation would fix the reported symptoms.

## 9. Rendering workload and avoidable transform work

The GLB contains 109 nodes, 108 meshes, 137 primitives, 49,849 vertices, 52,730 triangles, and 28 materials. It contains no textures, images, or animations. These are asset totals, not measured draw calls or the exact visible scene count: importing can share resources, the placeholder is hidden, playable cars are duplicated, and culling changes what is rendered.

There is no game audio, particle system, animation playback, or custom compositor workload evident in the inspected scene. The environment has no enabled SSAO, SSIL, SSR, SDFGI, glow, fog, or volumetric fog. The camera attributes do not enable depth of field or automatic exposure. The directional light does not enable shadows. The import script disables specular/received shadows and related material features; its work occurs at import, not every game frame.

This makes large effects or texture-streaming spikes poor explanations. It does not make rendering free. Forward+ still performs its ordinary scene/render work at the 1440p viewport size. There are many separate mesh surfaces, no generated mesh LODs, and no occlusion-culling setup. Camera movement or zoom can expose more objects and change overdraw. Disabled antialiasing can make edges shimmer during motion. None of these static observations quantifies a long-frame cause.

The game also writes the current car's position and rotation every frame and writes the camera parent position every frame, including when it is fixed or nothing changed. Godot's `Node3D::set_position` and `set_rotation` do not simply return on equal input: they dirty transform state and propagate relevant notifications. The camera hierarchy can consequently trigger additional update work.

References: [CarStateManager.cs:146](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Source/GameDataAndLogic/Car/CarStateManager.cs:146), [CameraPanManager.cs:27](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Source/GameDataAndLogic/Camera/CameraPanManager.cs:27), [node_3d.cpp:714](C:/Users/k/Repository/External/Godot_4-7-2/scene/3d/node_3d.cpp:714), [node_3d.cpp:796](C:/Users/k/Repository/External/Godot_4-7-2/scene/3d/node_3d.cpp:796), [camera_3d.cpp:159](C:/Users/k/Repository/External/Godot_4-7-2/scene/3d/camera_3d.cpp:159).

The camera-size setter already uses a previous-value guard. Applying the same principle to unchanged transforms would reduce needless work, especially at the roughly 1,250 presents/s seen in the historical capture. It should not be presented as a proven fix for rare 10-20 ms gaps.

## 10. Logging, launch priority, and observation overhead

The left-stick marker builds a message and calls `GD.Print` in `Main.Process`. Formatting allocates and the native logging path performs synchronous output work. Release stdout flushing is disabled by default, and file logging does not flush every normal message by default, but buffered output can still encounter a slow sink.

The marker is explicitly sent after the player has noticed a stutter. Its output cannot cause a gap that already happened before the marker timestamp. It could perturb the marker frame or a subsequent frame. Initialization/reload logs are additional conditional output; there is no continuous per-frame game log in the current source. See [Main.cs:73](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Source/Main.cs:73), [logger.cpp:185](C:/Users/k/Repository/External/Godot_4-7-2/core/io/logger.cpp:185), [main.cpp:1084](C:/Users/k/Repository/External/Godot_4-7-2/main/main.cpp:1084).

The `MyRun` workflow can run PresentMon, kernel/runtime ETW collection, PowerShell helpers, and output-capture code alongside the game. Their event processing, disk writes, and wakeups can perturb scheduling. The historical CSV is large because every present is captured at a high rate, not because the game logs every frame. No measurement here isolates capture overhead.

`MyRun_NoPresentMon` still enables GC capture unless it is separately disabled. `MyRun_Simple` omits those captures. The wrappers set the game to High process priority, including the simple launcher. High priority does not prevent driver waits or guarantee better frame pacing and may change competition with other processes. Any later comparison should record the launcher and priority explicitly.

The helper polling sleeps are in separate launcher/capture processes. A 100/250 ms polling interval is not an instruction for the game to pause every 100/250 ms. GC event capture records activity; it does not force collection. None of these launchers was executed during this audit.

## What the existing capture establishes

The preexisting session `MyLogOutput/2026-10-04_05-23-23` contains PresentMon data, Godot logs, GC ETW data, an existing decoded GC XML file, and capture status. The raw CSV and existing decoded runtime events were checked independently of earlier written conclusions.

**This session predates the inspected export by roughly nine hours.** The capture metadata does not bind it to the current EXE/PCK/DLL hashes. It is evidence about that historical run, not a benchmark of the current export.

Its startup log reports Godot 4.7.2, D3D12 Forward+, an NVIDIA GeForce RTX 5090 Laptop GPU, 2560 x 1440, approximately 119.998 Hz, VSync disabled, maximum FPS zero, and render-thread model 1. The collision-index statistics match the isolated reconstruction.

There are 1,193,121 captured game presents. After excluding the first 10 seconds, 1,180,913 remain. Their median interval is **0.7992 ms**, the 99th percentile is approximately **1.005688 ms**, and all are reported as **Hardware: Independent Flip**. The recorded interval agrees with the raw QPC difference. Only two of these steady-period intervals exceed 3 ms:

| Time in session | Present interval | Elapsed before current Present | Current Present call | GPU busy  | GPU wait   |
| --------------- | ---------------- | ------------------------------ | -------------------- | --------- | ---------- |
| 24.411597 s     | 19.3824 ms       | 18.8349 ms                     | 0.2108 ms            | 0.3814 ms | 18.5685 ms |
| 71.690851 s     | 11.6798 ms       | 11.1149 ms                     | 0.2442 ms            | 0.4852 ms | 10.8968 ms |

Most of each interval occurred before the current `Present` call. The previous `Present` call accounted for 0.5475 ms and 0.5649 ms respectively. These rows do not support the interpretation that the GPU spent 19/11 ms actively shading the frame, or that the current `Present` call itself consumed that interval.

However, PresentMon's `MsCPUBusy` is derived from elapsed timestamps, not CPU samples proving the thread was continuously executing game code. It can include a fence wait, driver work, input processing, descheduling, or other pre-present work. Likewise, GPU wait is not GPU execution time. The calculation is visible in the local [PresentMon MetricsCalculatorCpuGpu.cpp:14](C:/Users/k/Repository/External/PresentMon_2-6-0/IntelPresentMon/CommonUtilities/mc/MetricsCalculatorCpuGpu.cpp:14).

All reported PresentMon event-loss, buffer-loss, and overflow counts were zero. The existing GC XML contains 8,390 events overall; for game PID 21696 it contains one runtime-information event and six segment-creation events, with **no collection-start/end or suspend/restart events**. The existing GC summary reports no lost events. There is therefore no recorded GC pause explaining those two gaps. This does not establish that GC can never occur in this application, nor substitute for a CPU scheduling trace.

The first two player markers followed those gaps by approximately 1.04 and 1.94 seconds, consistent with an observation/reaction delay but not proof of causation. The later two markers had no presentation gap over approximately 1.4 ms in their preceding ten-second windows in the existing analysis. That makes motion/camera/input timing particularly important to measure; it does not invalidate the player's observation.

A monitor at approximately 120 Hz does not display 1,250 complete, independent frames each second. With VSync off, presentation timing, scanout/tearing, and movement increments are separate quantities. A present-interval histogram alone cannot certify smooth visible motion.

## Recommended order of follow-up

These are proposed changes and measurements, not actions already taken. No game run is needed to review the first four items.

1. **Prevent unintended same-track reloads.** Add the no-op guard before JSON loading or scene destruction. Keep an explicit reload action only if desired.
2. **Make reset behavior observable.** Keep or revise the 350 ms design deliberately; record the reset reason and use a monotonic timeout. Separate this from actual frame stalls.
3. **Evaluate the remaining timer correction.** A narrowly scoped `physics_jitter_fix=0` comparison is justified by the isolated reproduction. Record raw elapsed time, received delta, velocity, and displacement so that improvement can be evaluated rather than inferred from FPS.
4. **Skip unchanged native transform setters.** Cache the last applied car pose and camera-parent position, taking car/track replacement into account. This reduces baseline work without claiming to solve a rare stall.
5. **Bind every future capture to the exact build and launcher.** Record EXE/PCK/DLL hashes, preset, engine version, runtime, command line, process priority, driver version, display mode/refresh, and enabled capture tools. Resolve the `Build` versus `MyBuildOutput` mismatch before comparing changes.
6. **Add low-overhead timing evidence before broad optimization.** Use a preallocated in-memory ring buffer for whole-callback and input/collision/movement/transform phase timestamps, process delta, pose change, reset reason, and car/track selection. Avoid formatting, disk output, or `GD.Print` per frame; flush outside active measurement. Sample allocation/collection and pipeline-compilation counters at an appropriate low frequency.
7. **Capture the missing native timeline in a separately authorized run.** CPU sampling plus context-switch/ready-thread events and stacks can distinguish running from waiting. Include disk/hard faults, DPC/ISR, and GPU scheduling when practical; include JIT/loader events if investigating first-use compilation. Time only inside C# is insufficient for waits in the preceding input pump or following render submission.
8. **Use controlled comparisons.** Capture helpers enabled versus disabled, then one targeted application/timer change at a time. Preserve DirectX 12, Forward+, VSync off, no FPS limiter, two swapchain images, and disabled fallbacks. Observe pipeline counters before investing in shader-baking/preparation changes.

The current evidence supports reducing known conditional hitches and investigating motion timing. It does not support declaring garbage collection, collision-query cost, a debug build, Vulkan pipeline-cache persistence, or a particular hardware component to be the root cause of the two historical gaps.

## Coverage and limits

The audit covered every available C# source file; editor import code; scene, environment, camera, material/model and collider data; project and export settings; build/run/capture scripts; exported native/managed metadata; PCK contents and imported-resource provenance; and relevant Godot Windows, SDL, timer, C# bridge, scene-transform, Forward+, shader, RenderingDevice, and D3D12 code paths. Existing telemetry was read without starting any new capture.

The collision test executed only copied pure collision logic against data. The timer test executed only extracted timer logic with minimal stubs. Neither started the game, created a window, connected to its process, or loaded its assemblies for execution. Their source programs and full intermediate results remain under `C:/Users/k/.codex/visualizations/2026/10/04/01a108d7-1475-7133-84d6-ab0b6b438daa`; their hashes and concise results are in the companion evidence file.

Static inspection cannot measure the current export's actual frame-time distribution, prove all native/driver calls are bounded, identify an unrecorded scheduling interruption, or guarantee absence of stutter. The historical data narrows the problem, while the identified source paths make the next investigation specific and reviewable.
