# Godot memory optimization source analysis

## User

# AGENTS.md instructions for C:\Users\k\Repository\Veehiicuul

<INSTRUCTIONS>
# Base template

## Style

For single and double quotes, only use the ASCII forms: ', "
Never use these: “, ”, ‘, ’, etc.

## Development platform compatibility

Support Windows 11 x64 as the only development platform.

## Folder and file naming

This only applies to things that we have the freedom to name as wanted.
Use CamelCase.
Use complete proper words. Don't use typical shortenings. Good: Source, Documentation. Bad: src, docs.

## External tools

You may use the tools in `%UserProfile%\Program`.
You may refer to local copies of source repos in `%UserProfile%\Repository\External`.

## Git

When implementing stuff, avoid difficult-to-review "mega-commits".
Split large work into multiple commits to make it easier to review.
Separate commits that record conversations from other commits.

## Markdown tables

Tables in Markdown must be padded and aligned in a way to make them easy to read in a plaintext editor, not only in a Markdown viewer.

## Mathematical notation in Markdown

Any mathematical notation in Markdown files (LaTeX, KaTeX, MathJax, etc) must display properly in VSCode's Markdown previewer, GitHub.com's Markdown displayer, and the markdown viewer in the Windows 11 ChatGPT app.

## PowerShell

All PowerShell scripts must use:

- Set-StrictMode -Version Latest
- $ErrorActionPreference = 'Stop'

## Godot

When creating a Godot application:

- Use Godot 4.7.2 .NET
- Use C#
- Don't use GDScript
- Halt if you don't find a portable/self-contained install of Godot 4.7.2 .NET at `%UserProfile%\Program\Godot_v4.7.2-stable_mono_win64`
- Halt if that install of Godot doesn't have export templates installed
- Build.cmd must do all building/exporting using release configuration with optimizations fully enabled and use Godot's export via the command-line to create an EXE
- Use DirectX 12
- Keep vsync off
- Keep max fps limiter off
- Set rendering_device/vsync/swapchain_image_count=2
- Set rendering_device/fallback_to_vulkan=false
- Set rendering_device/fallback_to_opengl3=false
- Use Forward+ renderer

The Godot csproj must include this:

```xml
<PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>

    <!-- Enables nullable reference annotations and warnings to catch potential null errors. -->
    <Nullable>enable</Nullable>

    <!-- Enforces the repository's configured code-style rules during builds. -->
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>

    <!--
        Required for IDE0005 to work.
        Enables the build-time IDE0005 check for unused using directives by generating XML documentation.
    -->
    <GenerateDocumentationFile>true</GenerateDocumentationFile>

    <!--
        Required in Godot .NET/C# projects.
        Prepares the C# library and its dependencies for dynamic loading by Godot.
    -->
    <EnableDynamicLoading>true</EnableDynamicLoading>

    <!--
        Required to properly export a Godot .NET/C# project using the command-line (instead of the Godot Editor export GUI).
        Prevents an idle compiler server from keeping Godot's Windows console wrapper waiting after export.
        See https://github.com/godotengine/godot/issues/110101 for more information.
    -->
    <UseSharedCompilation>false</UseSharedCompilation>
<PropertyGroup>
```

## Applications

### Running

If you create a runnable application, create files called `Build.cmd` and `Run.cmd` that respectively build and run the application when double-clicked from File Explorer.
These must be located at the root of the application's folder in the Git repo.
These must be simple wrappers for PowerShell scripts named `Build.ps1` and `Run.ps1` which contain the actual logic to minimize the amount of batch code written.
Run.cmd must exit if it doesn't discover a build of the application at the place that Build.cmd outputs to.
If the application doesn't need to be "built" for it to be run (such as a PowerShell script), then omit Build.cmd and Build.ps1.

### Logging

When creating an application, create a folder called `MyLogOutput` at the root of the application's folder in the git repo.
For each run of the application, a folder must be created in MyLogOutput and named with the current timestamp. This PowerShell code shows what the name of the folder should be:

```powershell
$logFolderPath = "$env:UserProfile\Repository\Godot\VsyncStutterTest\MyLogOutput\$(Get-Date -Format "yyyy-MM-dd_HH-mm-ss")"
New-Item -ItemType "Directory" -Path $logFolderPath
```

Any logs for that application session must be put in that log folder.
`MyLogOutput/` must be gitignored.

# Base template additions

## Conversations

Record verbatim and commit all conversations in a folder named `Conversations` located at the root of this Git repo.
Use one file per conversation.
Prefix these commits with `[cnv]`.
If I attach images to prompts, save and record these in the conversation logs.
If the conversation begins with `dnr`, then do not record the conversation.

## Application compatibility

Do not attempt to maintain any sort of application compatibility between different commits of the repo. This creates unwanted complexity.

## Target platform compatibility

Support Windows 11 x64 as the only target platform.

</INSTRUCTIONS>
<environment_context>
  <cwd>C:\Users\k\Repository\Veehiicuul</cwd>
  <shell>powershell</shell>
  <current_date>2026-10-01</current_date>
  <timezone>America/Los_Angeles</timezone>
  <filesystem><workspace_roots><root>C:\Users\k\Repository\Veehiicuul</root><root>C:\Users\k\.codex\visualizations\2026\10\01\01a0f7fe-9e99-7f41-a782-d2f3e8b56cf9</root></workspace_roots><permission_profile type="managed"><file_system type="restricted"><entry access="read"><special>:root</special></entry><entry access="write"><path>C:\Users\k\Repository\Veehiicuul</path></entry><entry access="write"><path>C:\Users\k\.codex\visualizations\2026\10\01\01a0f7fe-9e99-7f41-a782-d2f3e8b56cf9</path></entry><entry access="write"><special>:slash_tmp</special></entry><entry access="write"><special>:tmpdir</special></entry><entry access="read"><path>C:\Users\k\Repository\Veehiicuul\.git</path></entry><entry access="read"><path>C:\Users\k\.codex\visualizations\2026\10\01\01a0f7fe-9e99-7f41-a782-d2f3e8b56cf9\.git</path></entry><entry access="read"><path>C:\Users\k\Repository\Veehiicuul\.agents</path></entry><entry access="read"><path>C:\Users\k\.codex\visualizations\2026\10\01\01a0f7fe-9e99-7f41-a782-d2f3e8b56cf9\.agents</path></entry><entry access="read"><path>C:\Users\k\Repository\Veehiicuul\.codex</path></entry><entry access="read"><path>C:\Users\k\.codex\visualizations\2026\10\01\01a0f7fe-9e99-7f41-a782-d2f3e8b56cf9\.codex</path></entry><entry access="read"><path>C:\Users\k\Repository\Veehiicuul\.aws</path></entry><entry access="read"><path>C:\Users\k\.codex\visualizations\2026\10\01\01a0f7fe-9e99-7f41-a782-d2f3e8b56cf9\.aws</path></entry></file_system></permission_profile></filesystem>
</environment_context>

## User

<external_codex_apps_open_page>{"page_id":null}</external_codex_apps_open_page>

## User

perform a comprehensive analysis of "C:\Users\k\Repository\Veehiicuul\Veehiicuul_Godot_CSharp" to see if there are any good opportunities for memory optimization such as reducing per-frame allocations.
use the godot engine source code at "C:\Users\k\Repository\External\Godot_4-7-2".
this is a source code only level analysis. don't build or run the engine, editor, or game, etc


## Assistant (commentary)

I'll review the C# code and trace allocation-sensitive Godot APIs in the engine source. I'll keep this entirely at the source level and won't build or run anything.

## Assistant (commentary)

The first major finding is an unconditional `GC.Collect()` at the end of every game frame. Most movement and camera calculations use value types, so I'm checking hidden allocations in input dispatch, collision edge cases, and track loading before ranking the recommendations.

## Assistant (commentary)

The normal frame path appears to have very little managed allocation after initialization. Input event wrappers still allocate when events cross into C#, but Godot's shortcut-input filter already keeps mouse motion and joystick-axis events out of this callback. I'm also checking retained mesh memory and track-switch allocations, where there may be larger savings.

## Assistant (commentary)

Two more useful opportunities are confirmed: cycling tracks reloads the entire track even though only Ribeye is configured, and the import settings generate extra shadow meshes despite shadows and the regular depth prepass being disabled. The collision code already uses allocation-free `Int128` arithmetic for ordinary exact checks; its allocating `BigInteger` fallback is limited to more extreme coordinate ranges.

## Assistant (commentary)

The asset inspection found identical geometry in all six car color variants and the hidden placeholder, so sharing one mesh with material overrides could reduce retained mesh memory. I'm finishing the recommendations with separate priorities for frame overhead, loading allocations, and retained memory, and recording the conversation as required by the repository instructions.

## Assistant (final)

Yes. **The biggest immediate opportunity is removing the forced garbage collection on every frame.** The normal movement, camera, and collision calculations already appear largely free of managed heap allocations after initialization. Additional opportunities concern track reloads and retained mesh resources.

I reviewed all 36 C# files, the scene/resource/import settings, and the relevant native code, C# bindings, and binding generators in your Godot 4.7.2 checkout. Nothing was built or run. The estimates below come from source and asset data; they are not measured allocation rates or performance results.

1. **Highest priority: remove per-frame forced GC.**

   [Main.cs:104](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Source/Main.cs:104) unconditionally calls `GC.Collect()` at the end of each completed frame, including track-switch frames.

   Parameterless `GC.Collect()` requests collection across all generations. Repeating that every frame introduces collection overhead and possible pauses even when the update itself produces little garbage. It does not prevent allocations.

   Let .NET schedule normal collections. The stronger `ForceGarbageCollection()` helper also waits for finalizers and collects twice; keep that out of gameplay too. Removing forced collections can allow temporary garbage and finalizable native wrappers to remain longer before reclamation, so deterministic ownership remains useful at loading boundaries.

2. **Good opportunity for allocation bursts: avoid rebuilding the same track unnecessarily.**

   [TrackSwitcher.cs:40](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Source/GameDataAndLogic/TrackSwitcher.cs:40) computes the next index but never checks whether it equals the current index. With `TrackNames = ["Ribeye"]`, either track-cycle button reloads Ribeye.

   That rereads settings, frees and instantiates a scene, duplicates six gameplay car nodes, creates the managers again, rereads collider JSON, and rebuilds the collision index.

   If cycling is intended to change tracks, skip the reload when the index stays unchanged. If this deliberately acts as a complete reset, consider resetting the existing session or reusing immutable collision geometry/index data. Cache that data separately from `Car.Node` references, which belong to a particular scene instance.

   Avoid treating a cache-mode change as a complete solution. Your `CacheMode.Ignore` still uses `Reuse` for external dependencies in [Godot's loader](C:/Users/k/Repository/External/Godot_4-7-2/scene/resources/resource_format_text.cpp:1446). Resource caching also does not eliminate scene instantiation or your managed index construction.

3. **Good opportunity for retained native/GPU memory: stop generating unused shadow meshes.**

   [Ribeye_Model.glb.import](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Tracks/Ribeye/Ribeye_Model.glb.import) enables `meshes/create_shadow_meshes=true`.

   Godot creates separate position/index geometry for these meshes and attaches an additional `ArrayMesh` to the main mesh. This is actual retained geometry, not merely an import-time helper: see [ImporterMesh::get_mesh](C:/Users/k/Repository/External/Godot_4-7-2/scene/resources/3d/importer_mesh.cpp:902) and [GPU buffer creation](C:/Users/k/Repository/External/Godot_4-7-2/servers/rendering/renderer_rd/storage_rd/mesh_storage.cpp:392).

   The current scene has no enabled light shadows, and the project disables the regular depth prepass. Both matter because Godot also uses shadow meshes for ordinary depth passes, as shown in [the Forward+ renderer](C:/Users/k/Repository/External/Godot_4-7-2/servers/rendering/renderer_rd/forward_clustered/render_forward_clustered.cpp:360).

   Disabling shadow-mesh generation is therefore a strong candidate for this visual configuration. It would require reimporting later to realize the saving. Keep generated LODs as a separate decision: they cost index-buffer memory but can reduce rendering work.

4. **Good opportunity for retained geometry: share the car mesh across color variants.**

   Source-asset inspection found that all six colored cars and `SlopeCarPlaceholder` have identical position, normal, UV, and index data: five surfaces, 1,962 vertices, and 5,652 indices each. They use seven distinct glTF mesh entries, with differing material assignments.

   Use one shared mesh resource with per-instance surface material overrides for the colors. That can remove six redundant geometry copies, including their associated generated LOD/shadow geometry.

   The existing `array_mesh/deduplicate_surfaces=true` option does not accomplish this for the current scene import. Its relevant path is [importing a scene as one merged mesh](C:/Users/k/Repository/External/Godot_4-7-2/editor/import/3d/resource_importer_scene.cpp:2201).

   There is also a smaller opportunity to replace the permanently hidden placeholder mesh with a transform marker. That requires updating [TrackNodes.cs:19](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Source/GameDataAndLogic/TrackNodes.cs:19), which currently requires a `MeshInstance3D`.

   Conversely, your six gameplay `Duplicate(0)` calls already share their source mesh resources. Those duplicates add nodes and rendering instances, rather than six more complete geometry copies. Reducing them to one active gameplay node would save some additional memory, but is less valuable than sharing the authored meshes.

5. **Useful per-frame optimization, primarily for CPU work: write transforms and camera size only when changed.**

   Every normal frame writes car position, car rotation, camera-pivot position, and camera size, even when the car is stationary and the camera is fixed.

   These setters do not all skip unchanged values. [Node3D::set_position](C:/Users/k/Repository/External/Godot_4-7-2/scene/3d/node_3d.cpp:714) and `set_rotation` propagate transform changes. [Camera3D::set_size](C:/Users/k/Repository/External/Godot_4-7-2/scene/3d/camera_3d.cpp:743) forces a projection update, bypassing the equality guard in `set_orthogonal`.

   Remember the last applied values or maintain dirty flags. Force an initial application after track creation, reset, or selecting another car node.

   **This is not a demonstrated managed-allocation saving.** The binding generator passes these value types through pointers and uses stack-allocated argument storage. The benefit is avoiding unnecessary interop, transform propagation, and renderer bookkeeping.

6. **Conditional allocation issue: the gamepad diagnostic can allocate every frame while disconnected.**

   [InputManager.HasGamepad](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Source/GameDataAndLogic/InputManager.cs:11) calls `GetConnectedJoypads()` on every access. Godot constructs a fresh native array, then wraps it in managed typed/untyped arrays with disposal tracking.

   There is currently no active caller in the main frame path, so this is not an existing steady-state allocation problem.

   However, if `LogGamepadRightStick()` is called every frame, its throttle stops working while gamepad 0 is disconnected: `LastLogTime` updates only after `HasGamepad` succeeds. The array query then runs every frame, rather than twice per second.

   Cache connectivity from `JoyConnectionChanged`, with one initial query, or advance the diagnostic timestamp before checking connectivity. In this checkout, `Array<T>` itself is not `IDisposable`; an independently owned query result's underlying array is accessible through the explicit untyped-array cast in [Array.cs:1205](C:/Users/k/Repository/External/Godot_4-7-2/modules/mono/glue/GodotSharp/GodotSharp/Core/Array.cs:1205).

7. **Secondary loading optimization: deserialize JSON from UTF-8 bytes.**

   [JsonUtility.cs:20](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Source/Utility/JsonUtility.cs:20) reads an entire file through `GetAsText()`, then deserializes the managed string.

   Godot's implementation first reads bytes and constructs a native string; marshaling then constructs the C# string. Ribeye's collider JSON is 100,524 bytes, so its managed text alone is approximately 196 KiB, before the deserialized data and collision-build arrays.

   Reading through `FileAccess.GetBuffer()` and passing the resulting UTF-8 bytes to `JsonSerializer.Deserialize<T>(ReadOnlySpan<byte>, Options)` would remove the text-conversion stages. It still allocates a byte array and the object graph, but reduces temporary memory. Keep Godot file access so exported `res://` paths continue working.

   This affects initialization and reloads, not ordinary frames. The shared `JsonSerializerOptions` already avoids repeated options creation.

8. **Lower priority: trim collision-construction memory without weakening the query algorithm.**

   The collision implementation already makes good memory choices: contiguous arrays of structs, one-time footprint preparation, allocation-free query traversal, and a cached result for an unchanged car pose.

   Static calculation from Ribeye's source bounds suggests approximately 23,904 expanded-grid cells and 30,328 edge references. Together with 800 edges, that is about **330 KiB of retained array payload**, plus roughly 106 KiB of temporary grid-build arrays. These estimates exclude object headers and imported-mesh differences.

   The repeated references buy a single-cell lookup and fast rejection. I would keep that tradeoff. Using 16-bit references for this 800-edge track would save only about 59 KiB.

   The fallback builders have some reasonable loading-only refinements:
   - Reuse the dense builder's counts array as its write cursors, as the expanded builder already does.
   - Replace the sparse builder's per-cell temporary lists with counts/offsets or grouped assignments.
   - Allocate BVH nodes for the actual split tree. [OutlierIndex](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Source/GameDataAndLogic/CollisionDetection/TrackCollisionDetector.cs:845) reserves `2 * EdgeCount - 1` nodes despite allowing eight edges per leaf.

   These paths matter mainly for other track shapes or rejected expanded grids.

   The exact segment predicate also already uses [allocation-free Int128 arithmetic](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Source/GameDataAndLogic/CollisionDetection/TrackCollisionDetector.cs:1247) for bounded coordinates. Its `BigInteger` fallback can allocate for wider exponent ranges, but ordinary near-collinear contacts do not automatically require it. Replacing that fallback would need another exact arithmetic implementation; removing it or substituting an epsilon would weaken correctness.

Input handling has an important limit: **the engine can allocate input events even when your frame calculations allocate nothing.** Godot creates native joystick-motion events when axis values change. Events delivered to `_ShortcutInput` also require managed object wrappers. Your current callback already benefits from [Godot's native shortcut filter](C:/Users/k/Repository/External/Godot_4-7-2/scene/main/viewport.cpp:3583), which excludes mouse motion and joystick-axis events before C# dispatch. Replacing it with a general `_Input` callback could increase managed allocations. Replacing event counts with state polling could lose presses between frames.

Other suspected allocation sites are already fine: `Vector2`, `Vector3`, `Quaternion`, collision structs, nullable yaw, and `Math.SinCos` tuples are value types; dictionary clearing retains capacity; node references and footprints are cached. Pre-sizing the input dictionaries could avoid occasional capacity growth, but would not remove recurring per-frame garbage. System reporting, float-to-string experiments, and stick logging are dormant diagnostics. Your string-based `GD.Print` calls use the string overload, so they do not introduce a `params object[]` allocation.

I found no definite track-node leak in the inspected ownership paths: freeing the track recursively deletes its gameplay children, and the owned `PackedScene` and `FileAccess` wrappers already use `using`. The project also has no imported images or animations in Ribeye, so texture or animation optimizations offer little here.

No application or engine files were changed. The conversation is recorded and committed in [GodotMemoryOptimizationAnalysis20261001.md](C:/Users/k/Repository/Veehiicuul/Conversations/GodotMemoryOptimizationAnalysis20261001.md).

## User

<external_codex_apps_open_page>{"page_id":null}</external_codex_apps_open_page>

## User

perform a comprehensive analysis of "C:\Users\k\Repository\Veehiicuul\Veehiicuul_Godot_CSharp" and focus on per-frame memory allocations during active driving time which doesn't include switching tracks or switching car.
use the godot engine source code at "C:\Users\k\Repository\External\Godot_4-7-2".
this is a source code only level analysis. don't build or run the engine, editor, or game, etc


## Assistant (commentary)

I'll trace the active-driving frame path and the Godot code it reaches, excluding track and car switching. I'll distinguish managed allocations from native engine allocations and keep the analysis entirely at the source level.

## Assistant (commentary)

The current driving loop no longer forces garbage collection every frame, and its movement and camera calculations use value types. The remaining allocation risks are concentrated in input event delivery, an exceptional collision fallback, and native engine work. I'm checking those paths against the supplied Godot source to separate recurring allocations from buffers that simply grow during warmup.


## Assistant (commentary)

I found a definite native allocation in Godot's input dispatch: it shares the node list, then requests a writable pointer, which immediately copies the buffer. The renderer also creates small temporary arrays each frame. These are separate from managed GC pressure, so removing C# allocations alone would not eliminate all driving-time allocation churn.


## Assistant (commentary)

The managed input callback already excludes joystick motion, so analog steering does not create C# event wrappers. Digital button and key events still do. Godot's native action polling looks like a viable way to remove those wrappers, with care around the current cross-device counting behavior. The collision code's allocating fallback is conditional; its normal and Int128 paths reuse existing storage.


## Assistant (commentary)

The review is complete. The strongest recurring opportunities are native buffer copies and temporary input/rendering storage. The C# driving calculations already avoid routine heap allocations; their remaining opportunities concern digital input delivery, first-use dictionary growth, and the conditional collision fallback. I'm checking the source references and recording this analysis as required by AGENTS.md.


## Assistant (final)

The current code is already close to allocation-free on the managed heap during ordinary analog driving. **The clearest recurring allocation opportunities are in Godot's native input and rendering paths.** Managed allocations remain on digital input events and one conditional collision path.

I reviewed the current application at `816357f` against the supplied Godot 4.7.2 source at `ed1daf0`. This includes steering, braking, collisions, resets, camera follow and zoom, and engine input/rendering work. Track and car switching are excluded. Nothing was built or run; allocation frequency and performance impact are not measured.

The earlier per-frame GC finding has been addressed: [Main.Process](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Source/Main.cs:102) now forces collection only after a track or car switch.

1. **Unnecessary native buffer copies: concrete, small engine optimization.**

   Godot has two reachable places that share a reference-counted vector and immediately request a writable pointer:

   - The [rendering frustum constructor](C:/Users/k/Repository/External/Godot_4-7-2/servers/rendering/renderer_scene_cull.h:182) assigns `planes = p_planes`, then calls `planes.ptrw()`.
   - [Input group dispatch](C:/Users/k/Repository/External/Godot_4-7-2/scene/main/scene_tree.cpp:1448) assigns `nodes_copy = g.nodes`, then calls `nodes_copy.ptrw()`.

   [CowData::ptrw](C:/Users/k/Repository/External/Godot_4-7-2/core/templates/cowdata.h:164) invokes copy-on-write. Because both buffers are shared at that point, it allocates and copies immediately, even though these callers only read the elements.

   The frustum copy occurs during scene rendering; the input copy occurs per nonempty input-group dispatch, including delivery to `DigitalInputCounts`. Using `planes.ptr()` and `Node *const *gr_nodes = nodes_copy.ptr()` would remove these unnecessary copies while preserving the snapshots. These are engine changes, with native heap benefits.

2. **Other recurring renderer allocations: reuse small CPU buffers.**

   The rendering path creates several fresh vectors during driving:

   - [Projection::get_projection_planes](C:/Users/k/Repository/External/Godot_4-7-2/core/math/projection.cpp:469) allocates a six-plane vector. Both [scene culling](C:/Users/k/Repository/External/Godot_4-7-2/servers/rendering/renderer_scene_cull.cpp:3352) and the enabled [light culler](C:/Users/k/Repository/External/Godot_4-7-2/servers/rendering/rendering_light_culler.cpp:632) request one. The frustum also constructs a new plane-sign vector.
   - Scene culling creates a fresh `directional_lights` vector and appends the track's visible Sunlight.
   - [Forward+ render-pass setup](C:/Users/k/Repository/External/Godot_4-7-2/servers/rendering/renderer_rd/forward_clustered/render_forward_clustered.cpp:3483) constructs a 16-entry lightmap/shadowmask RID vector, even when all entries are default textures. Setup runs for the opaque and transparent passes, including when the transparent render list is empty.
   - The [opaque pass](C:/Users/k/Repository/External/Godot_4-7-2/servers/rendering/renderer_rd/forward_clustered/render_forward_clustered.cpp:2206) constructs a temporary clear-color vector when clearing the framebuffer.

   Fixed storage for the six-plane frustum and its signs, retained light-list storage, and a cached default lightmap RID array could remove this recurring native allocation work. [Uniform-set cache hits](C:/Users/k/Repository/External/Godot_4-7-2/servers/rendering/renderer_rd/uniform_set_cache_rd.h:185) already reuse GPU uniform sets; the temporary CPU arrays are still constructed before that lookup.

   These buffers are small. Their existence is clear from the source; whether removing them noticeably improves frame times remains unknown.

3. **Changing controller axes allocate native events and queue nodes.**

   The Windows SDL path passes axis changes to `Input::joy_axis`. Identical values [return early](C:/Users/k/Repository/External/Godot_4-7-2/core/input/input.cpp:1695), but a changed value reaches [_axis_event](C:/Users/k/Repository/External/Godot_4-7-2/core/input/input.cpp:1836), which instantiates an `InputEventJoypadMotion`.

   With the current default accumulated-input behavior, [parse_input_event](C:/Users/k/Repository/External/Godot_4-7-2/core/input/input.cpp:1549) also appends it to a linked list. Joypad motion does not implement accumulation, so these events each get a newly allocated [list element](C:/Users/k/Repository/External/Godot_4-7-2/core/templates/list.h:295), freed during flushing.

   Consequently, allocation follows delivered axis changes, potentially several per frame. Your C# deadzones run afterward and cannot reduce this native event creation.

   There are two possible approaches:

   - A project-side option is `Input.UseAccumulatedInput = false`. With agile flushing left at its current default of false, this source dispatches immediately and avoids buffered-list nodes. It still creates the input events, changes dispatch timing, and disables mouse-motion accumulation, so it needs a deliberate behavior tradeoff.
   - An engine-side retained queue or ring buffer could reduce queue allocations while retaining buffering behavior. Coalescing analog changes before constructing events would require additional care around action threshold crossings and event consumers.

   There is another conditional Windows allocation: [process_raw_input](C:/Users/k/Repository/External/Godot_4-7-2/platform/windows/display_server_windows.cpp:4392) uses `new BYTE[dw_size]` and `delete[]` for every pending raw mouse/keyboard batch. A retained, suitably aligned buffer could avoid repeated allocation. This is not a constant cost of controller-only driving.

4. **Digital input delivery creates managed wrappers; native action polling can avoid them.**

   [DigitalInputCounts._ShortcutInput](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Source/DigitalInputCounts.cs:62) receives keyboard and joypad-button events. Their conversion creates managed event objects, temporary class-name representations, and disposal-tracking objects through [ScriptManagerBridge](C:/Users/k/Repository/External/Godot_4-7-2/modules/mono/glue/GodotSharp/GodotSharp/Core/Bridge/ScriptManagerBridge.cs:93) and [DisposablesTracker](C:/Users/k/Repository/External/Godot_4-7-2/modules/mono/glue/GodotSharp/GodotSharp/Core/DisposablesTracker.cs:68).

   Filtering releases, keyboard repeat, or irrelevant buttons inside the C# callback happens after that conversion.

   Your callback already benefits from [Godot's shortcut filter](C:/Users/k/Repository/External/Godot_4-7-2/scene/main/viewport.cpp:3583): joystick motion and mouse motion never reach it. Therefore, this managed pressure is tied to digital events; holding an analog stick does not create managed event wrappers.

   A useful application change would be native InputMap actions queried through `Input.IsActionJustPressed`, with action `StringName` objects cached once, and the managed event callback removed or disabled. In this source, [the default nonlegacy behavior](C:/Users/k/Repository/External/Godot_4-7-2/core/input/input.cpp:435) preserves a press followed by release within the same frame.

   Action transitions differ from your raw down-event counts, especially when one controller holds a button while another presses it. That behavior needs to be preserved or intentionally changed. Plain held-state polling would also lose short presses between frames.

5. **Collision detection has a conditional managed allocation fallback.**

   The grid lookup, rectangle transformation, indexed loops, BVH traversal, and ordinary predicate calculations reuse arrays and value types. The unchanged-pose cache also avoids repeating stationary queries.

   However, [ExactOrientationSign](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Source/GameDataAndLogic/CollisionDetection/TrackCollisionDetector.cs:1228) falls back from `Int128` to `BigInteger` when the coordinate exponent spread exceeds its bounded conversion limit. That path allocates backing arrays for wide arithmetic.

   The stored Ribeye coordinates span only 12 raw dyadic exponent steps, within the existing 37-step limit. Transformed vehicle corners must also be considered, so that observation does not prove the allocating fallback is unreachable.

   This is a lower-priority opportunity unless strict allocation-free collision queries are required. Separate X/Y exponent normalization and significand-width checks could expand the existing `Int128` path. A fixed-size exact arithmetic implementation could eliminate the fallback entirely, but requires careful correctness work.

6. **Repeated transform and camera writes offer CPU savings, without a demonstrated steady heap saving.**

   [CarStateManager](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Source/GameDataAndLogic/Car/CarStateManager.cs:146), [CameraPanManager](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Source/GameDataAndLogic/Camera/CameraPanManager.cs:27), and [CameraZoomManager](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Source/GameDataAndLogic/Camera/CameraZoomManager.cs:38) write their values every frame, including unchanged yaw, fixed-camera position, and zoom.

   Godot's position/rotation setters propagate transform dirtiness without equality guards, and `Camera3D.set_size` forces a projection update. Caching the last applied values would avoid redundant interop calls and engine work.

   The main-thread transform notification list is intrusive, and the renderer checks unchanged instance transforms. I would classify this as a CPU optimization rather than claim that every setter allocates.

The remaining ordinary frame code looks sound for managed memory: `Vector2`, `Vector3`, nullable yaw, collision structs, `Math.SinCos` tuples, and timeout arithmetic use value types. [Frame dispatch](C:/Users/k/Repository/External/Godot_4-7-2/modules/mono/glue/GodotSharp/GodotSharp/Core/Bridge/CSharpInstanceBridge.cs:24) uses a stack-only argument wrapper, and generated native calls use `stackalloc`. Node references and collision footprints are cached; the driving path contains no resource loading, LINQ materialization, or routine string formatting.

The input dictionaries retain capacity when cleared. Pre-sizing them once could remove first-use or capacity-growth allocations during driving, but would not remove recurring digital-event wrapper allocations. Likewise, [render-graph scratch storage](C:/Users/k/Repository/External/Godot_4-7-2/servers/rendering/rendering_device_graph.cpp:1757) and the [DX12 command-list array](C:/Users/k/Repository/External/Godot_4-7-2/drivers/d3d12/rendering_device_driver_d3d12.cpp:2479) retain storage; their `clear`/`resize` operations should not be counted as fresh heap allocations every frame.

No application or engine files were changed. This analysis is recorded and committed in [the conversation log](C:/Users/k/Repository/Veehiicuul/Conversations/GodotMemoryOptimizationAnalysis20261001.md).

## User

elaborate on this:
Digital input delivery creates managed wrappers; native action polling can avoid them.
[DigitalInputCounts._ShortcutInput (line 62)](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Source/DigitalInputCounts.cs:62) receives keyboard and joypad-button events. Their conversion creates managed event objects, temporary class-name representations, and disposal-tracking objects through [ScriptManagerBridge (line 93)](C:/Users/k/Repository/External/Godot_4-7-2/modules/mono/glue/GodotSharp/GodotSharp/Core/Bridge/ScriptManagerBridge.cs:93) and [DisposablesTracker (line 68)](C:/Users/k/Repository/External/Godot_4-7-2/modules/mono/glue/GodotSharp/GodotSharp/Core/DisposablesTracker.cs:68).
Filtering releases, keyboard repeat, or irrelevant buttons inside the C# callback happens after that conversion.
Your callback already benefits from [Godot's shortcut filter (line 3583)](C:/Users/k/Repository/External/Godot_4-7-2/scene/main/viewport.cpp:3583): joystick motion and mouse motion never reach it. Therefore, this managed pressure is tied to digital events; holding an analog stick does not create managed event wrappers.
A useful application change would be native InputMap actions queried through Input.IsActionJustPressed, with action StringName objects cached once, and the managed event callback removed or disabled. In this source, [the default nonlegacy behavior (line 435)](C:/Users/k/Repository/External/Godot_4-7-2/core/input/input.cpp:435) preserves a press followed by release within the same frame.
Action transitions differ from your raw down-event counts, especially when one controller holds a button while another presses it. That behavior needs to be preserved or intentionally changed. Plain held-state polling would also lose short presses between frames.


## Assistant (commentary)

I'll trace an input event through the native and C# paths, then explain what action polling would save and how its behavior differs from your current counters.


## Assistant (commentary)

The key distinction is event rate versus frame rate: your analog polling already avoids C# event objects. The savings apply to keyboard and button events, and require removing or disabling the managed shortcut callback. I also confirmed that your current consumers use only "pressed this frame" booleans, although overlapping presses from different controllers would still behave differently.


## Assistant (final)

**The opportunity is to keep digital input events on the native side and let C# query their results.** The allocation savings scale with keyboard/button event traffic, so the benefit during mostly analog driving is likely modest.

Godot first creates a native input event. To pass that event to your C# `_ShortcutInput` method, it needs a managed object representing it. For a newly wrapped event, [ScriptManagerBridge](C:/Users/k/Repository/External/Godot_4-7-2/modules/mono/glue/GodotSharp/GodotSharp/Core/Bridge/ScriptManagerBridge.cs:93) creates:

- An `InputEventKey` or `InputEventJoypadButton` C# wrapper around the native event.
- A temporary managed `StringName` representing the event's native class name.
- A managed string containing that class name, used to select the wrapper constructor.
- Weak-reference objects and tracking registrations for the event wrapper and temporary `StringName`, through [DisposablesTracker](C:/Users/k/Repository/External/Godot_4-7-2/modules/mono/glue/GodotSharp/GodotSharp/Core/DisposablesTracker.cs:68).

This happens when the native event first needs a managed representation. [An existing managed wrapper is reused](C:/Users/k/Repository/External/Godot_4-7-2/modules/mono/glue/GodotSharp/GodotSharp/Core/NativeInterop/InteropUtils.cs:35), so multiple C# listeners receiving the same event do not necessarily create separate wrappers.

By the time [your switch statement](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Source/DigitalInputCounts.cs:62) checks `Pressed`, `Echo`, or the button code, these allocations have already happened. Returning immediately for releases or keyboard repeat cannot avoid them. A normal press followed by release can therefore cause this wrapper work twice, even though your counters use only the press.

Your counter dictionaries reuse their storage when cleared. Replacing them with arrays would leave the event-conversion allocations in place.

With InputMap actions, Godot instead updates the action's state while processing the native event. C# can subsequently ask whether the action became pressed during the current frame. [The native query](C:/Users/k/Repository/External/Godot_4-7-2/core/input/input.cpp:419) looks up stored state, compares frame numbers, and returns a boolean; it does not return an event object.

For example, after defining a `ResetCar` action mapped to the X button, the relevant parts of `InputManager` could become:

```csharp
private static readonly StringName ResetCarAction = new("ResetCar");

public void UpdateInputs() {
    this.ResetCar = Input.IsActionJustPressed(ResetCarAction);
    // Other existing input queries...
}
```

The action and its bindings should be configured once, through project configuration or startup code. To obtain the allocation savings, migrate the other digital queries too, then remove or disable `_ShortcutInput` and remove the manual `ClearFrameCounts` dependency. Keeping the callback enabled would continue creating event wrappers, even if its body ignored every event. Other managed input listeners receiving those events could also keep that allocation path active.

**Caching the action name matters.** Writing `Input.IsActionJustPressed("ResetCar")` every frame implicitly converts the string to a `StringName`. [That conversion constructs a new wrapper](C:/Users/k/Repository/External/Godot_4-7-2/modules/mono/glue/GodotSharp/GodotSharp/Core/StringName.cs:81), with disposal tracking. Although the string literal itself is reused, the conversion would introduce recurring allocations. The cached field avoids that. Without caching, querying several actions every frame could create more managed garbage than the occasional digital events you currently receive.

The behavior comparison, assuming the action has the same bindings and device scope, is:

| Situation                                      | Current counter-based query | Native `IsActionJustPressed`  |
| ---------------------------------------------- | --------------------------- | ----------------------------- |
| First press during this frame                  | True                        | True                          |
| Held since a previous frame, no new press      | False                       | False                         |
| Press and release before the next frame update | True                        | True with the default setting |
| Two complete presses within one frame          | True; underlying count is 2 | True; no count is provided    |
| Controller B presses while A still holds       | True                        | False                         |

The short-press case works because Godot records the frame of the press. With the default `legacy_just_pressed_behavior = false`, [the query does not require the action to remain held](C:/Users/k/Repository/External/Godot_4-7-2/core/input/input.cpp:435). Merely sampling held state and comparing it with the previous frame would miss a press and release occurring between samples. `IsActionJustPressed` also does not consume the result: repeated queries in that frame can return true.

The overlapping-controller case differs because Godot [combines held state across devices and bindings](C:/Users/k/Repository/External/Godot_4-7-2/core/input/input.cpp:1846), then [records a press only when the combined state changes from released to pressed](C:/Users/k/Repository/External/Godot_4-7-2/core/input/input.cpp:1030). If controller A already holds X, controller B pressing X leaves that combined state unchanged. Your counter records B's new down event independently.

All current [InputManager consumers](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Source/GameDataAndLogic/InputManager.cs:33) use booleans, so losing the exact numeric count would not affect their current logic. To match their existing all-controller scope, action bindings would need the [all-devices setting, `Device = -1`](C:/Users/k/Repository/External/Godot_4-7-2/core/input/input_map.h:47). Preserving independent overlapping-controller presses would additionally require device-specific actions whose results are combined, or another event-counting design.

For your active-driving workload, this is an event-dependent cleanup opportunity. [The native shortcut filter](C:/Users/k/Repository/External/Godot_4-7-2/scene/main/viewport.cpp:3583) already excludes joystick-axis and mouse-motion events. Frames containing only analog input therefore avoid these particular managed allocations already. Action polling would remove the additional managed work caused by digital events; native event creation, queuing, and action processing would continue.

No application or engine code was changed, built, or run. The explanation is recorded in [the conversation log](C:/Users/k/Repository/Veehiicuul/Conversations/GodotMemoryOptimizationAnalysis20261001.md).
