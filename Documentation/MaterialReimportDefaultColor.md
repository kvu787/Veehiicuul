# Material color remaining blue after reimport

Investigated September 26, 2026 (UTC), using Godot
`4.7.2.stable.mono.official.ed1daf0bf` on Windows 11 x64.

The symptom is a Godot cached-resource reload bug. The imported scene on disk has
the correct white material; reloading it into an existing material object leaves
the previous blue albedo in memory. Restarting the editor creates a new material
with the correct default white albedo.

## Evidence from this project

The GLB's `CheckeredLine` node uses mesh `Plane.005`, with material indices 0 and 1:
`CheckeredLineWhite` and `CheckeredLineBlack`. The white material omits
`pbrMetallicRoughness.baseColorFactor`; Godot imports it as `(1, 1, 1, 1)`.
The black material explicitly specifies `(0, 0, 0, 1)`.

The visually white track barriers use a different material, `BarrierWhite`.
Its GLB factor is approximately `(0.7, 0.7, 0.7, 1)`, which Godot converts to
approximately `(0.854306, 0.854306, 0.854306, 1)`. That is not the default and
therefore reloads correctly. This explains the selective failure in the screenshot.

The import settings keep materials embedded (`materials/extract=0`) and have no
per-resource overrides (`_subresources={}`). `TestYo.tscn` instances the GLB
without a material override. The existing `DisableSpecularImport.gd` has its blue
albedo assignment commented out.

A separate C# diagnostic loaded a copy of the project's existing imported
`.scn`, recorded its 39 named materials, changed all their in-memory albedos to
`(0.15155911, 0.5057727, 0.8741714, 1)`, and reloaded the same file with
`ResourceLoader.CacheMode.Replace`, the mode used by the editor's scene reimport
refresh. It then compared the result against a cache-independent load using
`IgnoreDeep`.

| Material             | Fresh file read                     | Replace reload                              | Result  |
| -------------------- | ----------------------------------- | ------------------------------------------- | ------- |
| `CheckeredLineWhite` | `(1, 1, 1, 1)`                      | `(0.15155911, 0.5057727, 0.8741714, 1)`     | Stale   |
| `CheckeredLineBlack` | `(0, 0, 0, 1)`                      | `(0, 0, 0, 1)`                              | Correct |
| `BarrierWhite`       | `(0.854306, 0.854306, 0.854306, 1)` | `(0.854306, 0.854306, 0.854306, 1)`         | Correct |
| Other 36 materials   | Their original colors               | Their original colors                       | Correct |

Every material retained its object identity across the Replace load. Only
`CheckeredLineWhite` remained blue. `IgnoreDeep` returned the correct original
colors for all 39 materials. Calling `ResetState()` on the stale white material
also left it blue. Explicitly assigning white to that already-loaded material
restored it.

The test passed in the installed Godot binary, with exit code 0. It exercised the
resource reload mechanism headlessly, rather than automating the open editor's
Reimport button or inspecting its live objects. Startup also emitted a certificate
store error; all resource checks completed successfully. The project, its import
cache, and the user's script edit were left unchanged.

## Exact engine mechanism

The local engine source is the clean `4.7.2-stable` checkout at commit
`ed1daf0bf001b61586d9930840f2f1394092c079`.

1. The GLTF importer creates new materials. With no color factor present, white
   remains the constructor default. See [material creation](C:/Users/k/Repository/External/Godot_4-7-2/modules/gltf/gltf_document.cpp:2956)
   and [default albedo](C:/Users/k/Repository/External/Godot_4-7-2/scene/resources/material.cpp:3912).
2. The scene importer runs the post-import script, then saves the packed scene.
   See [post-import callback](C:/Users/k/Repository/External/Godot_4-7-2/editor/import/3d/resource_importer_scene.cpp:3435)
   and [scene save](C:/Users/k/Repository/External/Godot_4-7-2/editor/import/3d/resource_importer_scene.cpp:3478).
3. The binary saver skips properties equal to their defaults. Consequently the
   saved white material has no `albedo_color` property to apply during reload.
   See [default-value omission](C:/Users/k/Repository/External/Godot_4-7-2/core/io/resource_format_binary.cpp:2231).
4. The editor refreshes imported scene instances using `CACHE_MODE_REPLACE`.
   The binary loader reuses matching cached material objects and calls
   `reset_state()` before applying the properties present in the file. See
   [editor reload](C:/Users/k/Repository/External/Godot_4-7-2/editor/editor_node.cpp:7274)
   and [cached object reuse](C:/Users/k/Repository/External/Godot_4-7-2/core/io/resource_format_binary.cpp:699).
5. `Resource::reset_state()` invokes an optional virtual callback;
   `BaseMaterial3D`/`StandardMaterial3D` does not override it to reset albedo.
   The omitted color therefore leaves the existing blue value untouched. See
   [reset implementation](C:/Users/k/Repository/External/Godot_4-7-2/core/io/resource.cpp:230).

The public [ResetState documentation](https://docs.godotengine.org/en/stable/classes/class_resource.html#class-resource-method-reset-state)
also describes clearing non-exported state, rather than resetting all saved
properties to their defaults.

## Practical consequence

Commenting out the color assignment is correct. The imported file already has
the intended result. Reopening the project is a reliable workaround because it
discards the stale material objects; deleting `.godot` is unnecessary for this
case.

Assigning `Color.WHITE` in the import script would still produce a default-valued
property that the saver omits. It does not repair this reload mechanism.
Setting an almost-white color would avoid the omission, but would change the
asset's intended color and conceal the bug.

A durable engine fix must restore omitted properties to their defaults when
refreshing cached resources. A project-side editor workaround could instead
copy the correct values from a cache-independent load into the live materials
after reimport. Neither change was implemented for this investigation.

The same mechanism can affect other material properties when a previously
nondefault value returns to its default; this test specifically verified albedo.
