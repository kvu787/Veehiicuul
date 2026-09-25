# Veehiicuul: evidence for the long-term platform decision

Inspected September 24, 2026, at Git commit `91c026054444f1204f0e3d5f9777e0be33d66231`. `git status --short` was empty both at the beginning and end of inspection; ignored historical logs were inspected separately. This is a read-only source and existing-artifact assessment. No game, build, export, or new benchmark was run. All new analysis files are in `MyAnalysis`. Source-repository instructions were read; their Windows-only implementation policies describe the current projects, while the present user request explicitly broadens the desired future target to Android. Version 4.7.2 below describes these project settings and recorded runtime logs, not the version of the separate Godot engine source checkout.

The central finding is that this repository demonstrates reusable racing-game algorithms, a Godot learning port, and serious investigation of a small native renderer. It does not contain three comparable completed racing games. It therefore supports conclusions about migration work and control over rendering, but cannot establish an engine performance winner.

## 1. What actually exists

| Component                    | Observed state                                                                                            | Decision relevance                                                                   |
| ---------------------------- | --------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------ |
| `Veehiicuul_Godot_CSharp`    | Forty Unity runtime files ported to Godot; intentional startup exception; required game scenes unfinished | Useful migration foundation, with material integration work remaining                |
| `Veehiicuul_Godot_GDScript`  | One one-byte placeholder file                                                                             | No GDScript implementation or performance evidence                                   |
| `Veehiicuul_DirectX12_Cpp`   | One one-byte placeholder file                                                                             | No separate C++ racing-game implementation                                           |
| `3dTestScene_CppDx12`        | Working-source native rendering experiment: animated car, sphere, flattened backdrop                      | Concrete evidence of rendering and frame-admission control, with a very small scope  |
| `InputLatency_Godot`         | Separate Godot C# input visualization and verification application                                        | Useful measurement scaffold; not the application in the older Godot latency captures |
| Blender assets and scripts   | GLB and JSON collision export, validators, imported track scene experiment                                | Asset-authoring work has begun; runtime scene assembly still needs completion        |

The Godot project calls itself a learning port, identifies the original ZoomTracks source snapshot, and explicitly excludes Unity scenes, meshes, materials, shaders, and editor tools from the completed port. Its claim is not just a stale README caveat: [Main.cs:33](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Main.cs:33) unconditionally throws before initialization. The existing [September 22 runtime log](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/MyLogOutput/2026-09-22_16-17-39/Godot.log:3) records that deliberate exception. Removing it would still encounter missing scene contracts described in the [README:111](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Readme.md:111) and enforced by the [scene loader:12](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Source/Utility/SceneLoadingUtility.cs:12).

There is also genuine newer asset work: [TestYo.tscn:3](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Testyo/TestYo.tscn:3) instantiates an imported Track009 GLB with environment resources. The [Blender exporter:169](C:/Users/k/Repository/Veehiicuul/Blender/Scripts/ExportToVeehiicuul.py:169) exports both GLB and collision JSON to that experiment folder. This is progress beyond a source-only port, but does not satisfy the runtime's `Scenes/Ui.tscn` and six named track scene contracts. Treat the imported experiment and playable-game readiness separately.

## 2. What the racing implementation implies

### The current game is custom planar motion

[CarState.cs:41](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Source/GameDataAndLogic/Car/CarState.cs:41) transforms gamepad acceleration through camera and vehicle coordinates, integrates velocity, applies braking and a speed limit, and points the car along its velocity. Position advances once per application update at line 90. [CollisionManager2.cs:34](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Source/GameDataAndLogic/CollisionManager2.cs:34) maps the vehicle into a two-dimensional footprint versus track-outline query. A collision triggers a reset in [Main.cs:117](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Main.cs:117).

This is a legitimate arcade driving model. It is not evidence for suspension, tire contact, a drivetrain, 3D vehicle-to-vehicle contact, airborne behavior, or a realistic vehicle solver. The Godot configuration explicitly selects **Dummy 2D and 3D physics** in [project.godot:28](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/project.godot:28). Engine physics rankings should therefore carry little weight for reproducing the current design. They matter if the intended future racing design needs those additional behaviors.

Godot's own documentation warns that `VehicleBody3D` has known issues and is not intended for realistic advanced vehicle physics; custom integration may be required. That is relevant to a future simulation-style racer, but it is not a blocker for the custom planar system already present here. [Official VehicleBody3D documentation](https://docs.godotengine.org/en/stable/classes/class_vehiclebody3d.html).

### Useful logic already crosses engine boundaries

The collision detector's core imports `System.*` types rather than Godot APIs and has an immutable grid/BVH broad phase, exact segment-contact logic, and a linear oracle. See [TrackCollisionDetector.cs:1](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Source/GameDataAndLogic/CollisionDetection/TrackCollisionDetector.cs:1) and [class design:65](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Source/GameDataAndLogic/CollisionDetection/TrackCollisionDetector.cs:65). Godot integration is concentrated in mesh-derived bounds, transforms, and the adapter class.

The source map documents all 40 runtime-file mappings in [SourceMap.md:3](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Documentation/SourceMap.md:3). The collision verification program compiles the production detector independently of the Godot application. Its tests cover indexed queries against the oracle, closing edges, sparse layouts, oversized-edge fallback, coordinate mapping, and steady-state allocation behavior. [Verification scope](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Verification/CollisionDetection/Readme.md:14). The README reports 12,160 queries, 1,984 Track001 edges, zero allocations over 100,000 queries, and 11,544 coordinate comparisons; these are existing recorded verification claims, not tests rerun in this assessment. [Recorded results](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Readme.md:136).

The strongest architectural lesson is to preserve plain C# domain data and algorithms behind thin engine adapters. That reduces future Godot/Unity switching costs without committing to maintaining both games. It does not eliminate scene, shader, UI, input, packaging, and platform-specific migration work.

### A single callback is a project choice

The port deliberately centralizes startup and update in `Main`, disables processing on loaded scene nodes, and has no application physics callback or worker loop. [Main.cs:88](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Main.cs:88), [passive-scene enforcement](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Source/Utility/SceneLoadingUtility.cs:30). These constraints are not Godot engine limitations.

`TimeManager`'s fixed-delta option substitutes `1 / refreshRate` once per rendered update; it does not implement an accumulator with independent fixed simulation ticks. [TimeManager.cs:20](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Source/GameDataAndLogic/TimeManager.cs:20). If enabled without matching actual rendering cadence, it changes simulated time per wall-clock second. The current default instead uses frame delta. A deliberate timing design, replay contract, and high-speed collision validation remain game work under every platform choice; choosing C++ does not supply them automatically.

The detector checks the current pose against track edges, rather than accepting a previous/current swept pose. Source inspection therefore identifies a continuous-collision question to test for the intended speed and timestep; it does not demonstrate an observed tunneling bug. See its [IsColliding entry point](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Source/GameDataAndLogic/CollisionDetection/TrackCollisionDetector.cs:171).

## 3. What the custom DX12 work proves

The C++ application deliberately renders an orthographic scene containing an animated car, a sphere, and one flattened background image. The objects and composition are documented at [README:31](C:/Users/k/Repository/Veehiicuul/3dTestScene_CppDx12/README.md:31). The car's transform comes from `CurrentAnimationState()`, not the Godot racing simulation. [Renderer.cpp:1165](C:/Users/k/Repository/Veehiicuul/3dTestScene_CppDx12/Source/Renderer.cpp:1165).

The project contains substantive engineering: explicit GPU frame contexts, swapchain lifecycle, descriptor/resources management, compiled HLSL, settings validation, vendor-neutral adapter checks, hardware/WARP GPU tests, and window lifecycle tests. [CMake test registrations:193](C:/Users/k/Repository/Veehiicuul/3dTestScene_CppDx12/CMakeLists.txt:193). That work is real and potentially reusable. It is also a concrete sample of the platform maintenance a custom engine takes on.

Its minimum-latency preset specifies one GPU frame in flight, DXGI maximum frame latency one, two backbuffers, presentation admission, tearing, and spin waits. [RenderPreparation.h:26](C:/Users/k/Repository/Veehiicuul/3dTestScene_CppDx12/Source/RenderPreparation.h:26). [Renderer.cpp:1274](C:/Users/k/Repository/Veehiicuul/3dTestScene_CppDx12/Source/Renderer.cpp:1274) waits for GPU resources and presentation admission, services Windows messages while waiting, and services them again immediately before rendering. [Application.cpp:58](C:/Users/k/Repository/Veehiicuul/3dTestScene_CppDx12/Source/Application.cpp:58) drains the pending message queue. This demonstrates control over admission and event ordering that is harder to expose uniformly through a general-purpose engine.

It does **not** yet demonstrate a portable engine with comparable asset import, gamepad/touch control, racing gameplay, UI, saves, audio, or Android lifecycle/packaging. No Vulkan backend was found in the inspected application source. Windows coupling is visible in its Win32 window/message APIs, DXGI swapchain, D3D12 linkage, WIC image path, and DirectXMath. [Application.cpp:16](C:/Users/k/Repository/Veehiicuul/3dTestScene_CppDx12/Source/Application.cpp:16), [CMakeLists.txt:181](C:/Users/k/Repository/Veehiicuul/3dTestScene_CppDx12/CMakeLists.txt:181).

The SimplePaint material math is better isolated: several C++ files use only the standard library, while its transform helper requires DirectXMath and the host owns shader compilation and GPU binding. [Module portability notes](C:/Users/k/Repository/Veehiicuul/3dTestScene_CppDx12/Source/SimplePaint/README.md:14). Reuse that separation if retaining the native work as a renderer laboratory or future library.

The local policy forbids vendor-specific latency APIs and any application-owned FPS limiter, and requires Windows 11 x64. [C++ policy](C:/Users/k/Repository/Veehiicuul/3dTestScene_CppDx12/README.md:55). Those policies are not inherent to C++, DX12, Godot, or the new platform decision. In particular, always-uncapped rendering and spin waits would need a fresh battery, heat, and frame-pacing assessment for Android; no mobile energy measurement exists here.

## 4. Latency and throughput: evidence, not engine rankings

### Existing 100 FPS captures

The September 17 report identifies Unity ZoomTracks, the native C++ demo, and a **separate Godot VsyncStutterTest 4.6.3 application**, each described as using an NVIDIA 100 FPS cap, VSync disabled, and G-SYNC disabled. It includes one Low Latency Mode Off run and one Ultra run per application. It explicitly notes differing workloads, input populations, capture durations, selected time windows, and incomplete per-run adapter provenance. [Capture identities and conditions](C:/Users/k/Repository/Veehiicuul/Documentation/InputLatency.md:34).

This assessment independently reread the four available Unity/Godot CSVs, using the prior report's selection windows, and reproduced the statistics below. The C++ September 17 CSV paths were not found under the current Veehiicuul tree; those two figures are attributed to the historical report rather than newly reproduced.

| Application and setting | Available input samples | Mean ms | Median ms | P95 ms | P99 ms | Evidence status                         |
| ----------------------- | ----------------------- | ------- | --------- | ------ | ------ | --------------------------------------- |
| Unity, Off              | 1,696                   | 9.583   | 9.613     | 10.154 | 11.328 | Independently recomputed from saved CSV |
| Unity, Ultra            | 2,653                   | 9.506   | 9.611     | 10.105 | 10.352 | Independently recomputed from saved CSV |
| C++ demo, Off           | 1,414                   | 9.442   | 9.378     | 10.869 | 13.386 | Existing September 17 report            |
| C++ demo, Ultra         | 2,224                   | 9.551   | 9.470     | 11.454 | 13.511 | Existing September 17 report            |
| Godot, Off              | 3,792                   | 9.508   | 9.438     | 10.202 | 12.694 | Independently recomputed from saved CSV |
| Godot, Ultra            | 3,086                   | 9.591   | 9.607     | 10.130 | 12.256 | Independently recomputed from saved CSV |

These are `MsAllInputToPhotonLatency` **software-associated input/display estimates**, not optical measurements of first visible gameplay response. They do not measure physical switch travel, the full USB path, panel response, or identify which image first reflects an input. Repeated mouse events can affect which event is associated with a frame. Gamepad latency cannot be inferred from these keyboard/mouse fields. The original report explains these boundaries at [InputLatency.md:70](C:/Users/k/Repository/Veehiicuul/Documentation/InputLatency.md:70), with links to PresentMon's implementation and metric definitions.

Independent-recheck artifacts:

- [RecheckExistingLatency.ps1](C:/Users/k/Repository/External/godot/MyAnalysis/RecheckExistingLatency.ps1): transparent read-only calculation of existing captures.
- [RecheckedExistingLatency.json](C:/Users/k/Repository/External/godot/MyAnalysis/RecheckedExistingLatency.json): full precision statistics, capture SHA-256 hashes, paths, durations, trim policy, and sample coverage.
- [Unity Off raw CSV](C:/Users/k/Repository/ZoomTracks/ZoomTracks/MyLogOutput/2026-09-17_01-33-02/PresentMon.csv) and [Unity Ultra raw CSV](C:/Users/k/Repository/ZoomTracks/ZoomTracks/MyLogOutput/2026-09-17_01-37-02/PresentMon.csv).
- [Godot Off raw CSV](C:/Users/k/Repository/VsyncStutterTest/MyLogOutput/2026-09-17_02-33-46/PresentMon.csv) and [Godot Ultra raw CSV](C:/Users/k/Repository/VsyncStutterTest/MyLogOutput/2026-09-17_02-38-05/PresentMon.csv).
- [Historical report statistics](C:/Users/k/Repository/Veehiicuul/Documentation/InputLatency.md:105), including the two C++ rows.

The defensible result is that no decisive typical-latency advantage appears among these lightweight applications under this particular externally capped setup. Differences of hundredths of a millisecond are not causal evidence from single, unmatched captures. The record does not show that all engines have identical responsiveness or that the native approach offers no possible latency benefit. Conversely, queue capacity alone does not prove extra occupied queue depth or a mandatory extra frame of delay.

The original report also records Godot startup outliers above 600 ms and 1,400 ms; trimming for a steady-state table must not erase them from startup evaluation. It separately records sparse click populations averaging roughly 14–17 ms. DOOM's lower figure has different G-SYNC/Reflex/workload conditions and is not an engine comparison. [Segmented and click results](C:/Users/k/Repository/Veehiicuul/Documentation/InputLatency.md:121).

### Existing uncapped C++ throughput result

The September 11 archive reports approximately **5,640 and 5,666 presents per second** over two five-minute periods on the small native scene, with a one-frame GPU limit and spin-based admission. Both periods came from the same continuously running game. The archive contains the raw split CSVs, analysis scripts, verification results, and a substantial context report. [Archived report](<C:/Users/k/Repository/Veehiicuul/3dTestScene_CppDx12/SavedLogOutput/2026-09-11 MinimumInputLatency/Report.md:16>).

This is evidence that the specialized renderer can have very low per-frame presentation cost on the recorded machine. The input-latency columns were unavailable, so 0.177 ms mean present spacing is not evidence of 0.177 ms input-to-visible-response latency. The workload has neither game parity nor visual/feature parity with a complete engine-driven race. The archive identifies an RTX 5070 Ti Laptop GPU; later Godot and Unity logs identify a different RTX 5090 Laptop GPU. Do not combine the two dates into one hardware-normalized comparison. [Archive limitations](<C:/Users/k/Repository/Veehiicuul/3dTestScene_CppDx12/SavedLogOutput/2026-09-11 MinimumInputLatency/Report.md:129>), [later hardware caveat](C:/Users/k/Repository/Veehiicuul/Documentation/InputLatency.md:50).

### Newer Godot scaffold

`InputLatency_Godot` explicitly refreshes OS events, samples input, and updates visible spheres/indicators in `_Process`; accumulated input is disabled. [InputLatency.cs:20](C:/Users/k/Repository/Veehiicuul/InputLatency_Godot/Source/InputLatency.cs:20), [frame sequence:50](C:/Users/k/Repository/Veehiicuul/InputLatency_Godot/Source/InputLatency.cs:50). Its event timestamps are application receipt times, not measured hardware latency. [Experiment README:23](C:/Users/k/Repository/Veehiicuul/InputLatency_Godot/Readme.md:23). This is a useful starting point for future equivalent visible-response tests, but it was not the application in the September 17 Godot recordings.

## 5. Android and other-platform migration

The Godot runtime's only export preset is Windows x86_64, with desktop texture formats enabled and mobile texture formats disabled. [export_presets.cfg:3](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/export_presets.cfg:3). It selects Windows D3D12, disables Vulkan/OpenGL fallback, and advertises Forward+. [project.godot:33](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/project.godot:33). This is a desktop configuration, not an Android qualification result. A future Android build needs appropriate export/runtime configuration, renderer testing, input/UI adaptation, and device lifecycle verification.

The project targets .NET 10 and references `System.Management`; the inactive diagnostics helper includes Win32/WMI logic. [csproj:1](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul_Godot_CSharp.csproj:1), [DebugInfo.cs:70](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/DebugInfo.cs:70). That diagnostic dependency should be isolated or excluded for mobile builds, with target-framework/runtime requirements checked against the chosen Godot release. The core planar arithmetic has much less platform coupling than these diagnostics and launchers.

Current official Godot documentation says C# supports desktop, Android, and iOS; Android and iOS remain experimental, iOS export requires macOS, and C# projects cannot currently export to web. This gives Godot C# an actual Android path, but makes early device export validation a sensible decision gate. [Official C# platform support](https://docs.godotengine.org/en/stable/tutorials/scripting/c_sharp/index.html#c-platform-support). The empty GDScript folder supplies no basis to estimate the cost or benefit of changing language.

For the custom C++ application, Android entails substantially more than exchanging one graphics API. It needs a platform/window/input/lifecycle layer, Android build/package integration, asset access, and a rendering backend. A Vulkan implementation would be additional engineering relative to this source snapshot. Choosing an established cross-platform support library or rendering abstraction could reduce that work, but that would be a different architecture from the inspected raw Win32/DX12 sample.

## 6. Implications for the overall choice

1. **Do not treat the Godot learning port as gameplay parity with ZoomTracks.** There is valuable reusable C# work, but a real asset/scene/editor-workflow conversion remains. This is a migration cost, not evidence that Godot cannot implement the game.
2. **Do not treat the native demo's FPS as a product-level win.** The result is impressive within its documented scope; the missing game/platform services and unmatched workload prevent the intended inference.
3. **Do not choose a physics engine based on a hypothetical driving model.** The present model already owns its motion and planar collision logic. If it remains the desired design, portability, iteration, input/presentation, asset workflow, and deployability dominate.
4. **The C# investment supports either Unity continuity or a Godot migration.** A standalone domain layer and explicit engine boundary protect more long-term value than rewriting algorithms merely to change engines.
5. **Custom C++ remains technically viable, with a larger ownership commitment.** Its control is demonstrated, while superior game latency, Android viability, and lower total development effort are not established by this repository.

The strongest counterargument to choosing Godot now is the combination of **unfinished racing integration and experimental C# mobile support**. Unity already hosts the source game that this port is recreating. A successful source translation reduces uncertainty, but it does not repay scene, material, tooling, and device qualification work. If finishing those tasks substantially slows the desired games or a real Android export fails, continuing with Unity has a stronger evidence-based case than restarting the racing game in native C++.

For a bounded platform qualification, implement the same one-track driving slice with identical content, custom motion/collision behavior, visible input-response marker, target resolution, and quality settings. Measure optimized standalone builds on the same Windows machine and at least one intended Android device. Keep steady-state frame-time tails, startup/transition stalls, visible response, build/debug iteration effort, and device heat/power separate. Run more than one trial per setting and record actual backend/driver/queue/presentation settings. That experiment resolves the material uncertainties identified here without assuming an extreme world scale or graphics target rules out any option.
