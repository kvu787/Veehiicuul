# Track import settings review

## User

review these glb import settings: C:\Users\k\Repository\Veehiicuul\Veehiicuul_Godot_CSharp\Testyo\Track009_MiniComb4.glb.import

use the actual godot source code to inform your analysis: C:\Users\k\Repository\External\Godot_4-7-2

## Assistant

I’ll compare the import settings with the Godot 4.7.2 source and check the project context for settings that could affect this track.

## Assistant

The GLB contains static geometry with no images, skins, or animations. I’m checking the geometry options closely: Godot’s “shadow meshes” optimize depth and shadow rendering, and the compression setting controls vertex precision, so both have tradeoffs beyond import time.

## Assistant

Two source details change the recommendations: Forward+ won’t use generated shadow meshes with this GLB’s double-sided materials, and turning off “ensure tangents” won’t remove tangent storage when normals are present. I also confirmed that surface deduplication has no effect when importing as a PackedScene.

## Assistant

I’d keep most of this configuration. LOD generation is the main option worth benchmarking; disabling compression is defensible for preserving adjoining track geometry. I’m recording the review as required by the repository instructions.

## Assistant

**These settings are reasonable for a static track. I found no definite import correctness problem.** I checked the supplied Godot 4.7.2 source and the GLB itself: 81 meshes, 110 surfaces, 100,634 triangles, 39 materials, and no textures, skins, or animations.

The settings that deserve attention are:

1. **`meshes/generate_lods=false` — the main performance option worth testing.**
   This prevents Godot from generating reduced-triangle versions for distant rendering. With roughly 100,000 triangles, I would benchmark `true` and inspect the result from your gameplay camera. Keeping `false` is reasonable if consistent geometry at every distance matters more. The implementation creates additional index buffers referencing the original vertices; it preserves the base mesh. [LOD generation](C:/Users/k/Repository/External/Godot_4-7-2/scene/resources/3d/importer_mesh.cpp:757), [runtime selection](C:/Users/k/Repository/External/Godot_4-7-2/servers/rendering/renderer_rd/forward_clustered/render_forward_clustered.cpp:1105).

2. **`meshes/create_shadow_meshes=false` — keep this for the current materials.**
   Shadow meshes provide position-only geometry with coincident vertices merged, usable in eligible shadow and depth passes. This setting does **not** disable shadow casting. However, all 39 materials in this GLB have `doubleSided=true`, which Godot imports with culling disabled. Forward+ requires backface culling for its shared shadow-material path, where it selects the shadow mesh. Consequently, enabling this option alone would create extra mesh data that these materials cannot use. Reconsider it if you make suitable materials single-sided. [Mesh generation](C:/Users/k/Repository/External/Godot_4-7-2/scene/resources/3d/importer_mesh.cpp:955), [material import](C:/Users/k/Repository/External/Godot_4-7-2/modules/gltf/gltf_document.cpp:3184), [eligibility check](C:/Users/k/Repository/External/Godot_4-7-2/servers/rendering/renderer_rd/forward_clustered/scene_shader_forward_clustered.h:298), [mesh selection](C:/Users/k/Repository/External/Godot_4-7-2/servers/rendering/renderer_rd/forward_clustered/render_forward_clustered.cpp:4224).

3. **`meshes/force_disable_compression=true` — a defensible precision choice.**
   Compression quantizes positions to 16-bit coordinates within each surface’s bounding box. For the main track’s approximately 145-unit width, that means steps of approximately **0.00221 units** along X. Independently quantized adjoining surfaces can develop mismatches, so keeping compression disabled is reasonable for this asset. The cost is larger vertex buffers and more bandwidth. Also, “disable compression” does not make every attribute full precision: normals and tangents still use packed encoding. [Position quantization](C:/Users/k/Repository/External/Godot_4-7-2/servers/rendering/rendering_server.cpp:516), [buffer sizes](C:/Users/k/Repository/External/Godot_4-7-2/servers/rendering/rendering_server.cpp:1082), [normal/tangent packing](C:/Users/k/Repository/External/Godot_4-7-2/servers/rendering/rendering_server.cpp:611).

4. **`meshes/ensure_tangents=true` — fine to keep; disabling it has limited benefit here.**
   The GLB supplies normals but no tangents. Godot generates UV-based tangents where UVs exist and arbitrary tangents otherwise. Your current materials have no normal maps, so UV-based tangent generation is unnecessary for their appearance. Nevertheless, turning this off **does not eliminate tangent storage**: the rendering server requires tangents when normals exist and supplies missing ones. Disabling it can avoid some import work, but should not be treated as a guaranteed runtime memory optimization. [Importer](C:/Users/k/Repository/External/Godot_4-7-2/modules/gltf/gltf_document.cpp:1757), [required tangent format](C:/Users/k/Repository/External/Godot_4-7-2/servers/rendering/rendering_server.cpp:1213), [fallback generation](C:/Users/k/Repository/External/Godot_4-7-2/servers/rendering/rendering_server.cpp:680).

5. **`meshes/light_baking=0` — appropriate for the current direct-light setup.**
   This sets imported meshes to `GI_MODE_DISABLED`; ordinary direct lighting still works. Change it if you intend the track to participate in baked lighting or applicable GI systems. `lightmap_texel_size=0.2` currently has no effect because you are not generating lightmap UVs. [GI assignment](C:/Users/k/Repository/External/Godot_4-7-2/editor/import/3d/resource_importer_scene.cpp:2885), [lightmap option condition](C:/Users/k/Repository/External/Godot_4-7-2/editor/import/3d/resource_importer_scene.cpp:330).

The remaining settings look appropriate:

| Settings                                  | Assessment                                                                            |
| ----------------------------------------- | ------------------------------------------------------------------------------------- |
| Root type/name/script empty; scale `1.0`  | No custom root override or additional scaling. Keep.                                  |
| Import as skeleton bones `false`          | Appropriate for this static scene.                                                    |
| Both name-suffix options `true`           | Enable import hints such as `-col`; no relevant suffixes appear in this asset.        |
| Animation import `false`                  | Appropriate. The other animation options have no practical effect on this asset.      |
| Named skins `true`                        | Harmless; there are no skins.                                                         |
| Material extraction `0`                   | Keeps materials internal. Extract only if you want separately editable materials.     |
| Naming version `2`                        | Uses Godot 4.5-or-later naming. Keep.                                                 |
| Embedded images `1`; texture-map mode `1` | Extract images and remap texture maps to StandardMaterial3D; inactive without images. |

These meanings match the [scene importer defaults](C:/Users/k/Repository/External/Godot_4-7-2/editor/import/3d/resource_importer_scene.cpp:2639) and [glTF option definitions](C:/Users/k/Repository/External/Godot_4-7-2/modules/gltf/editor/editor_scene_importer_gltf.cpp:93).

One misleading entry: **`array_mesh/deduplicate_surfaces=true` does nothing for this PackedScene import.** It does not merge this track’s surfaces or reduce its draw calls. Likewise, `mesh_library/use_node_names_as_mesh_names` is irrelevant here. These belong to other import types and can remain as serialized defaults. [Import-type filtering](C:/Users/k/Repository/External/Godot_4-7-2/editor/import/3d/resource_importer_scene.cpp:291), [surface merging](C:/Users/k/Repository/External/Godot_4-7-2/editor/import/3d/resource_importer_scene.cpp:2232).

This was source and asset inspection; I did not change the settings, reimport the model, or measure rendering performance.
