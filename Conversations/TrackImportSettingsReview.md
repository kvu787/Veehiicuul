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

## User

meshes/generate_lods=false is a requirement because exact geometry is a requirement
i won't use shadows at all in the entire game. does that change your recommendation on meshes/create_shadow_meshes?
explain in greater detail what this does: meshes/force_disable_compression

## Assistant

I’ll treat exact geometry as a requirement and keep LOD generation disabled. I’ll check how shadow meshes affect depth rendering without shadows, then explain precisely what the compression flag changes.

## Assistant

The source confirms that compression can move vertex positions even when the triangle count stays unchanged. Your exact-geometry requirement makes `force_disable_compression=true` the right choice. Shadow meshes can also serve depth passes without shadows, but this GLB’s double-sided materials still prevent that optimization.

## Assistant

Understood—**`meshes/generate_lods=false` is required**, and I withdraw the suggestion to benchmark enabling it.

My recommendation for **`meshes/create_shadow_meshes` remains `false` for this GLB**. Using no shadows removes its shadow-rendering benefit. There is one remaining consideration: Godot also uses these meshes in eligible **depth-only passes**, which help determine which surfaces are visible. That optimization can apply even in a game without shadows. [Renderer implementation](C:/Users/k/Repository/External/Godot_4-7-2/servers/rendering/renderer_rd/forward_clustered/render_forward_clustered.cpp:360).

However, this GLB’s materials are all double-sided, and the Forward+ path that uses shadow meshes requires backface culling. Consequently, there is no benefit from generating them for the current asset. For other assets with single-sided materials, “no shadows” alone would not rule out a depth-pass benefit. [Eligibility check](C:/Users/k/Repository/External/Godot_4-7-2/servers/rendering/renderer_rd/forward_clustered/scene_shader_forward_clustered.h:298).

**`meshes/force_disable_compression=true` is also the appropriate choice for your exact-geometry requirement.** It controls the lossy encoding of the imported mesh’s vertex attributes, including positions.

The setting’s double negative means:

- **`true`:** force the importer to disable `ARRAY_FLAG_COMPRESS_ATTRIBUTES`.
- **`false`:** allow that compression when the importer considers the surface eligible. Some surfaces are still excluded automatically.

This is explicit in the [glTF importer](C:/Users/k/Repository/External/Godot_4-7-2/modules/gltf/gltf_document.cpp:1786).

With compression **disabled**, Godot stores each vertex position as three 32-bit floating-point numbers: X, Y, and Z. Your GLB already supplies positions in that format. This avoids adding position quantization when packing the imported geometry into rendering buffers. [Position storage](C:/Users/k/Repository/External/Godot_4-7-2/servers/rendering/rendering_server.cpp:601).

With compression **enabled**, Godot calculates each surface’s bounding box and represents each coordinate using a 16-bit normalized integer. Conceptually, for X:

```text
normalized = (x - surface_min_x) / surface_width
stored     = truncate(clamp(normalized * 65535, 0, 65535))

rendered_x = surface_min_x + (stored / 65535) * surface_width
```

There are only **65,536 representable positions along each axis within that surface’s bounds**. Coordinates between those positions lose precision. The CPU packs this representation during mesh creation, and the vertex shader reconstructs positions when rendering. [Encoding](C:/Users/k/Repository/External/Godot_4-7-2/servers/rendering/rendering_server.cpp:516), [shader reconstruction](C:/Users/k/Repository/External/Godot_4-7-2/servers/rendering/renderer_rd/shaders/forward_clustered/scene_forward_clustered.glsl:729).

For your main track surface:

- Its X extent is approximately **145.1323 units**.
- The spacing between representable X coordinates becomes approximately **0.00221458 units**.
- If one unit represents one meter, that is approximately **2.21 mm**.
- The conversion truncates rather than rounding to the nearest value, so quantization error can approach one whole step, before accounting for floating-point arithmetic.

**Compression preserves triangle connectivity, but it can move the vertices defining those triangles.** An edge can therefore shift, a narrow feature can change shape, and adjoining surfaces with different bounding boxes can reconstruct a shared boundary differently. That is why disabling LODs alone would not satisfy your requirement.

The flag also changes other attributes:

- **Texture coordinates:** UV and UV2 use two 32-bit floats per set with compression disabled. With compression enabled, they use two 16-bit normalized integers, with additional scaling when needed. This can slightly change texture placement. [UV encoding](C:/Users/k/Repository/External/Godot_4-7-2/servers/rendering/rendering_server.cpp:726).
- **Normals and tangents:** these describe surface orientation for shading. Even with compression disabled, Godot stores them in compact, quantized encodings. Enabling compression packs their combined orientation more tightly using an axis and angle. Thus, `true` preserves full 32-bit position storage, but does **not** make every attribute lossless. [Normal/tangent storage](C:/Users/k/Repository/External/Godot_4-7-2/servers/rendering/rendering_server.cpp:611), [combined orientation encoding](C:/Users/k/Repository/External/Godot_4-7-2/servers/rendering/rendering_server.cpp:498).

The memory tradeoff for the attribute combinations in your GLB is:

| Vertex attributes                       | Disabled (`true`) | Compressed |
| --------------------------------------- | ----------------- | ---------- |
| Position + normal + tangent             | 20 bytes          | 12 bytes   |
| Position + normal + tangent + one UV set | 28 bytes          | 16 bytes   |

These are per-vertex buffer sizes, excluding triangle indices, materials, and other overhead. Compression reduces memory use and vertex-fetch bandwidth; its actual frame-time effect depends on the rendering workload. [Buffer layout](C:/Users/k/Repository/External/Godot_4-7-2/servers/rendering/rendering_server.cpp:1082).

For your requirement, keep **`force_disable_compression=true`**. It avoids this additional loss of position precision; ordinary floating-point rounding during transforms and rendering still applies.

## User

However, this GLB’s materials are all double-sided
^i dont think any of my models visually require double-sided rendering. so, how do i make them single-sided? also, how much a perf difference would there be if they are single-sided and i set meshes/create_shadow_meshes to true vs false?

## Assistant

I’ll check the Blender export code for the setting that makes these materials double-sided, then trace what single-sided materials would change in Godot’s depth pass.

## Assistant

Blender’s exporter uses each material’s “Backface Culling → Camera” setting to control sidedness in the GLB. I also counted the geometry: a shadow mesh could reduce this asset’s depth-pass vertex set from 73,128 entries to about 51,627 by sharing identical positions. That’s a 29% reduction in vertex entries, not a measured FPS gain.

## Assistant

**Make the materials single-sided in Blender, then re-export the GLBs.** This keeps the setting in the source assets.

In Blender 4.5’s Eevee material UI, enable **Material Properties → Settings → Surface → Backface Culling → Camera** for each material. The glTF exporter reads that exact property. With it enabled, the exporter omits `doubleSided`, whose default is single-sided. [Blender UI source](C:/Users/k/Program/blender-4.5.14-windows-x64/4.5/scripts/startup/bl_ui/properties_material.py:256), [exporter source](C:/Users/k/Program/blender-4.5.14-windows-x64/4.5/scripts/addons_core/io_scene_gltf2/blender/exp/material/materials.py:242).

To apply it to **all materials in the currently open .blend file**, run this in Blender’s Text Editor:

```python
import bpy

for material in bpy.data.materials:
    material.use_backface_culling = True
```

Save the .blend and run your export script again. If this becomes a requirement for every model, that loop can go immediately before the exporter call in [ExportToVeehiicuul.py](C:/Users/k/Repository/Veehiicuul/Blender/Scripts/ExportToVeehiicuul.py:173).

Godot’s resulting material should have **Cull Mode = Back**. Check that all intended visible surfaces still appear; a surface with reversed face winding may have been relying on double-sided rendering.

**Once the materials are single-sided, I would enable `meshes/create_shadow_meshes=true` for your current Forward+ setup**, while retaining:

```text
meshes/generate_lods=false
meshes/force_disable_compression=true
```

The generated mesh preserves the triangle geometry. It shares vertices that have **identical positions**, omits shading attributes, and remaps the same triangle indices to those shared positions. With compression disabled, this does not introduce position quantization or simplify the model. [Generation implementation](C:/Users/k/Repository/External/Godot_4-7-2/scene/resources/3d/importer_mesh.cpp:974).

As for performance, there are two separate changes:

- **Double-sided → single-sided:** backface culling avoids rasterizing back-facing triangles. This can save rasterization and fragment work, but does not halve the model’s vertex count or guarantee anything like twice the FPS.
- **Single-sided with shadow meshes `false` → `true`:** eligible depth-only passes can use the mesh with shared positions. The normal color pass still uses the original mesh. This does not reduce triangle count or draw calls. [Depth-pass selection](C:/Users/k/Repository/External/Godot_4-7-2/servers/rendering/renderer_rd/forward_clustered/render_forward_clustered.cpp:360), [shadow-mesh selection](C:/Users/k/Repository/External/Godot_4-7-2/servers/rendering/renderer_rd/forward_clustered/render_forward_clustered.cpp:4224).

I counted the positions directly in your GLB:

| Measurement                                      | Count   |
| ------------------------------------------------ | ------- |
| Source vertex entries across all surfaces        | 73,128  |
| Distinct positions, deduplicated within surfaces  | 51,627  |
| Potential reduction in depth-mesh vertex entries  | 29.4%   |
| Triangles, unchanged                             | 100,634 |

That **29.4% is a structural reduction, not a measured reduction in GPU time or an FPS improvement**. Actual vertex-shader work depends on visibility, indexing, and the GPU’s vertex cache. Generating these meshes would also add approximately **1.17 MiB of position/index buffers**, plus overhead, under your current no-LOD/no-compression settings.

The benefit also depends on the depth pass:

- Your current test scene has a simple direct-light setup, and Godot enables the depth prepass by default. Single-sided materials should make this optimization usable there.
- If you disable the depth prepass and use no shadows, these extra meshes provide no benefit.
- Features such as SSAO or SSR can change the prepass into one that also writes normals and roughness. That pass uses the original mesh, so this depth-only optimization does not apply.

[Default prepass setting](C:/Users/k/Repository/External/Godot_4-7-2/servers/rendering/rendering_server.cpp:3715), [pass selection](C:/Users/k/Repository/External/Godot_4-7-2/servers/rendering/renderer_rd/forward_clustered/render_forward_clustered.cpp:1925).

**I cannot give a reliable FPS percentage without an A/B measurement.** The improvement could be negligible if the depth pass is cheap or the game is CPU-limited. Compare `true` and `false` with the same single-sided materials and camera, measuring GPU frame time and “Render Depth Pre-Pass” time. The counts above establish an optimization opportunity; they do not establish its frame-time impact.
