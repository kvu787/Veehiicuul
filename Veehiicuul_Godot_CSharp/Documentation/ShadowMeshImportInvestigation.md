# Shadow Mesh Import Investigation

Investigated on 2026-09-24 using Godot `4.7.2.stable.mono.official.ed1daf0bf`, DirectX 12, Forward+, and an NVIDIA GeForce RTX 5090 Laptop GPU.

## Finding

Changing `meshes/create_shadow_meshes` from `true` to `false` and replacing the already loaded imported scene exposes a Godot resource-cache bug. Cached `ArrayMesh` objects retain old shadow-mesh references because `ArrayMesh::reset_state()` does not clear them. A fresh load of the disabled import is correct.

The exact four errors in the supplied screenshot were reproduced using a copy of `Testyo/Track009_MiniComb4.glb`. In the reproduction, `SlopeCarBlue` has five surfaces but its stale shadow-mesh reference has one. Forward+ requests shadow surfaces 1, 2, 3, and 4 and reports all four accesses as out of bounds at `mesh_storage.h:433`.

This establishes an engine reload defect that explains the screenshot. It does not indicate that the GLB needs to be re-exported. The original asset, import settings, scene, and installed engine were not modified during this investigation.

## Reproduction and verification

The source GLB has SHA-256 `A7E50717C164931F4F0FC5420F9BCEE8891FF35167E3078B45CF838792F7975E`. Its generator is `Khronos glTF Blender I/O v4.5.51`. It contains 81 meshes; seven have five material primitives and one has two.

A temporary C# diagnostic project was created at `../../.godot/ShadowMeshInvestigation/`, outside the application project and excluded from Git. It uses the same rendering settings and asset import options. Separate imports generated binary scene snapshots with shadow meshes enabled and disabled. To exercise the reload path, the diagnostic loads the enabled snapshot, replaces the file with the disabled snapshot, and loads the same resource path using `ResourceLoader.CacheMode.Replace`. It then instantiates and renders the reloaded scene. No GDScript was used.

| Scenario                                        | Mesh instances | Shadow references | Surface-count mismatches | Matching renderer errors |
| ----------------------------------------------- | -------------- | ----------------- | ------------------------ | ------------------------ |
| Initial load with shadow meshes enabled         | 81             | 81                | 0                        | 0                        |
| Replace cached enabled scene with disabled one  | 81             | 28                | 2                        | 4                        |
| Fresh process loading disabled scene            | 81             | 0                 | 0                        | 0                        |
| Clear shadow references before replacing scene | 81             | 0                 | 0                        | 0                        |

The second mismatch is `Barrier_Outer_0001`: one regular surface with a stale five-surface shadow mesh. It does not request an out-of-range index, but demonstrates that the stale references extend beyond the mesh producing the error messages.

The cold-load and pre-cleared-reference controls were both checked with DirectX 12 / Forward+. The diagnostic light had `ShadowEnabled = false`, and the depth prepass was disabled as in this project. Therefore these errors can occur during renderer surface-cache preparation even when no light shadows are enabled.

Local diagnostic logs, retained under the ignored diagnostic project:

- `MyLogOutput/2026-09-24_23-26-44/ReloadDirectX.log`: exact four-error reproduction.
- `MyLogOutput/2026-09-24_23-27-07/cold.log`: fresh disabled load.
- `MyLogOutput/2026-09-24_23-27-11/clear.log`: explicit reference clearing before reload.

The sandbox also produced unrelated user-directory/shader-cache and certificate-store errors in all three rendering runs. The counts above refer specifically to the reported mesh-surface bounds errors. The experiment exercises the engine reload path directly; it does not automate the user's open editor or discard its unsaved scene changes.

## Source trace

The local engine source is at `C:/Users/k/Repository/External/Godot_4-7-2`, tag `4.7.2-stable`, commit `ed1daf0bf001b61586d9930840f2f1394092c079`, matching the installed executable.

1. `editor/import/3d/resource_importer_scene.cpp:2842` only creates a shadow mesh when the import option is enabled.
2. `core/io/resource_format_binary.cpp:699` reuses cached subresources for `CACHE_MODE_REPLACE` and calls `reset_state()` before applying the serialized properties.
3. `scene/resources/mesh.cpp:1745` implements `ArrayMesh::reset_state()`. It clears surfaces, blend shapes, and bounding boxes, but leaves `shadow_mesh` intact. This permits old shadow references to survive replacement with resources whose serialized shadow property is omitted.
4. `servers/rendering/renderer_rd/forward_clustered/render_forward_clustered.cpp:4228` obtains the shadow mesh and requests the regular mesh's surface index from it. It assumes corresponding surface counts.
5. `servers/rendering/renderer_rd/storage_rd/mesh_storage.h:433` rejects the resulting out-of-range indices. This is the location shown in the screenshot.

An engine-level fix should clear the shadow mesh through `set_shadow_mesh(Ref<ArrayMesh>())` during `ArrayMesh::reset_state()`, keeping the resource and rendering-server state consistent. The pre-clearing control supports that correction, but no patched engine was built or installed, and upstream fix status was not established.

## Workaround

Save any scene edits, set `meshes/create_shadow_meshes=false`, reimport, and then fully close and reopen the project in Godot. A fresh process loading the disabled import has zero shadow references and produces none of these four errors. The experiment did not require deleting the application's `.godot` directory or changing the GLB.

At investigation time, the application's saved `.glb.import` had `meshes/create_shadow_meshes=true`; it was left unchanged.

This import option controls generation of an optimized mesh for shadow rendering. It does not itself turn off light shadows; those are controlled by the lights' `ShadowEnabled` properties.

## Proposed 4.7.2 source fix

Clear both the rendering-server shadow link and the resource reference at the start of `ArrayMesh::reset_state()` in `scene/resources/mesh.cpp`:

```diff
 void ArrayMesh::reset_state() {
+	if (mesh.is_valid()) {
+		RS::get_singleton()->mesh_set_shadow_mesh(mesh, RID());
+	}
+	shadow_mesh.unref();
+
 	clear_surfaces();
 	clear_blend_shapes();
```

The validity check handles empty meshes, whose rendering-server handles are allocated lazily. Calling the existing `set_shadow_mesh(Ref<ArrayMesh>())` unconditionally would lack that protection. Removing only the resource reference could leave the rendering-server link active if something else still owns the old shadow mesh.

This is a proposed C++ patch, not a compiled engine change. The earlier diagnostic verified that explicitly clearing the shadow references through the setter on already initialized meshes removes the reported errors. A patch regression test should cover cached scene replacement from enabled to disabled shadow meshes, resetting an empty mesh, and subsequent replacement with shadow meshes enabled again.

## Godot 4.8 status

Checked upstream on 2026-09-24. The [official archive](https://godotengine.org/download/archive/) identifies **4.8-dev6**, published September 15, as the latest 4.8 release. Its [release notes](https://godotengine.org/article/dev-snapshot-godot-4-8-dev-6/) identify source commit `8898c2b3db32adf6f92c694ffb6dac19af672e5f`.

The fix is absent from both:

- [4.8-dev6 source](https://github.com/godotengine/godot/blob/8898c2b3db32adf6f92c694ffb6dac19af672e5f/scene/resources/mesh.cpp#L1755).
- [Current master source](https://github.com/godotengine/godot/blob/30caae98b79ec7e75e5f893290a51b4048eaa141/scene/resources/mesh.cpp#L1755), verified against the live GitHub API; HEAD is `30caae98b79ec7e75e5f893290a51b4048eaa141`, dated September 24, and `version.py` identifies it as 4.8 development.

Both still reset surfaces, blend shapes, and bounding boxes without clearing the shadow mesh. The current master binary resource loader still calls this reset during cache replacement, and Forward+ still looks up the regular surface index in the shadow mesh. This is a source inspection of 4.8; no 4.8 executable was built or run.
