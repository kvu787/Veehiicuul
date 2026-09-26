# Testyo material review — Godot 4.7.2 source

Prepared September 25, 2026. Target: Windows 11 x64, Direct3D 12, Forward+.

**Your material is a sensible opaque, matte, diffusely lit configuration.** It removes several features you do not want, but it is still a StandardMaterial3D using per-pixel Burley diffuse lighting, ambient lighting, and the engine's standard material infrastructure. It is not a minimal, texture-free shader merely because the `.tres` contains few lines.

The companion [complete property reference](C:/Users/k/Repository/Veehiicuul/Documentation/StandardMaterial3DProperties.md) covers every registered BaseMaterial3D property, every supported enum choice, Boolean state and numeric domain, including hidden properties and inherited resource controls. It gives behavior, performance and issues for each.

## Evidence and limits

I inspected the actual local source at `C:/Users/k/Repository/External/Godot_4-7-2`. Its clean checkout is commit `ed1daf0bf001b61586d9930840f2f1394092c079`, tag `4.7.2-stable`; `version.py` agrees. The application repository began at `0edff2d` (`Adjust material settings`). The review follows C++ shader generation, renderer pipeline setup, and the Forward+ GLSL lighting implementation, using the same checkout's XML documentation to cross-check intended behavior. When these disagree, implementation takes precedence.

Performance statements describe source-level work and likely tradeoffs. **No GPU timing, compiled D3D12 instruction analysis, application run, or visual reproduction was performed.** A generated sample does not establish its exact hardware cost; shader compilation, caching, scene overlap, resolution and GPU architecture matter. Nothing here establishes a numerical FPS or input-latency improvement. The material, scene and project were not changed.

The issue register identifies code defects/risks and relevant upstream reports. A report from another version/backend is corroborating evidence, not proof of a reproduced 4.7.2 D3D12 bug. “No issue identified” in the reference does not mean every possible property combination has been tested.

## Your saved material

The [material](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Testyo/Testyo.tres:1) contains these nondefault values:

| Property                     | Saved value                                      | Review                                                                                    |
| ---------------------------- | ------------------------------------------------ | ----------------------------------------------------------------------------------------- |
| `specular_mode`              | `2` — Disabled                                   | Removes the ordinary direct-light specular lobe at shader generation/compilation.         |
| `disable_fog`                | `true`                                           | Excludes material fog application; world fog generation is a separate cost.               |
| `disable_specular_occlusion` | `true`                                           | Removes indirect-specular occlusion math; it is not a switch disabling reflections.       |
| `albedo_color`               | `(0.15155911, 0.5057727, 0.8741714, 1)`          | Opaque blue base color, still affected by diffuse light, ambient fill and color pipeline. |
| `metallic_specular`          | `0`                                              | Suppresses base dielectric reflectance; a uniform value, not a shader feature removal.    |
| `disable_receive_shadows`    | `true`                                           | Excludes receiving shadow-map calculations; does not disable shadow casting.              |

The omitted settings retain defaults: `transparency = Disabled`, `blend_mode = Mix`, back-face culling, normal depth testing, opaque depth writes, `shading_mode = Per-Pixel`, `diffuse_mode = Burley`, `metallic = 0`, `roughness = 1`, and `disable_ambient_light = false`. All optional feature groups such as emission, normal mapping, clearcoat, AO, height, SSS, refraction and detail are disabled; texture slots are null; stencil, billboarding, fades and growth are disabled; `next_pass` is null. [Defaults](C:/Users/k/Repository/External/Godot_4-7-2/scene/resources/material.cpp:3908), [initializers](C:/Users/k/Repository/External/Godot_4-7-2/scene/resources/material.h:568).

This is a good combination for preserving shape from directional/ambient light while avoiding shiny highlights, fog and received shadows. Your specular settings complement each other: Disabled removes the direct lobe; scalar zero removes dielectric reflectance. Neither should be described as a universal switch eliminating every reflection feature for all possible metallic/clearcoat materials; see R12.

## Scene context and practical recommendations

1. **Keep the opaque path, culling, normal depth test and one material pass.** These generally matter more than adjusting a strength slider. There is no reason in this material's stated intent to enable transparency, disable depth testing, render both triangle orientations, or add another pass.
2. **Consider Lambert if you want simpler diffuse lighting.** Burley still evaluates extra view/roughness terms with specular disabled. Lambert reduces that direct-light math, but alters the shading. Retain Burley if you prefer its appearance. Changing roughness to 0 is not an equivalent optimization: roughness participates in Burley and has a specular edge case. [Diffuse implementation](C:/Users/k/Repository/External/Godot_4-7-2/servers/rendering/renderer_rd/shaders/scene_forward_lights_inc.glsl:223).
3. **Keep ambient lighting if you want the current fill.** Your [environment](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Resources/Environment.tres:4) supplies white ambient light at energy 0.2, with sky reflections disabled. Setting `disable_ambient_light = true` could remove more shader work but would visibly darken surfaces facing away from the directional light.
4. **The present scene already disables several corresponding world features.** The [DirectionalLight3D](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Testyo/TestYo.tscn:18) has `light_specular = 0`, no enabled shadow setting, zero indirect energy and zero volumetric-fog energy. The environment has no enabled fog, SSAO, SSIL, SSR or GI resources in this test scene. Your material switches still encode the intended behavior under other lighting, but do not assume large additional savings in this already simple scene.
5. **This resource is assigned to TestyoSphere, not automatically to the imported track.** The saved [scene](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Testyo/TestYo.tscn:9) references it as the sphere mesh material. The track is a separate imported scene. Claims about all track materials would require a separate check of their effective assignments.
6. **The sphere is unusually dense for testing a simple matte material.** It uses 256 radial segments and 128 rings. The [SphereMesh generator](C:/Users/k/Repository/External/Godot_4-7-2/scene/resources/3d/primitive_meshes.cpp:2003) produces 33,410 vertex entries and 66,048 indexed triangles for those settings, including cap degeneracies. This may be deliberate for a smooth visual reference. For performance experiments, compare at the resolution and geometry density you actually intend to ship. Per-Vertex moves work onto those vertices and is not automatically a win.
7. **Unshaded is only appropriate if you want no lighting-based shape cues.** It removes substantially more lighting evaluation, but does not preserve this material's current look. Per-Vertex also changes the lighting approximation, not just its frequency.
8. **A custom shader is an optional later measurement target, not a required rewrite.** StandardMaterial3D emits albedo, metallic and roughness samples even with null slots; the renderer substitutes fallback textures. A deliberately small constant-color Lambert shader could omit those samples and unused material parameters. The benefit must be measured against StandardMaterial3D on the actual scene/GPU. [Generated samples](C:/Users/k/Repository/External/Godot_4-7-2/scene/resources/material.cpp:1591), [fallback binding](C:/Users/k/Repository/External/Godot_4-7-2/servers/rendering/renderer_rd/storage_rd/material_storage.cpp:1056).

Your [project settings](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/project.godot:33) already select D3D12 and disable renderer fallbacks, vsync, the ordinary depth prepass and screen-space roughness limiting. Those are separate renderer decisions; material feature switches do not replace or prove the performance of those choices. Enabling features such as SSS can still introduce additional passes/buffers. For a meaningful comparison, hold camera, geometry, resolution and world effects constant; warm the shader pipelines and compare GPU frame time, not only FPS.

## Issue register

None of the feature-specific defects below appears to be actively triggered by your current material. They matter when choosing other property values. The always-generated fallback samples are current overhead, not a rendering correctness bug.

### R1 — Exactly zero roughness skips ordinary direct specular

**Source-confirmed implementation defect, explicitly marked FIXME.** Punctual-light specular evaluation is inside `if (roughness > 0)`, with a comment stating that roughness zero should not disable specular entirely. This affects the ordinary directional/omni/spot highlight path; the area-light path is different. Zero therefore does not simply give the sharpest direct highlight. Use a small positive value when such a highlight is desired. It does not affect your current Disabled specular setting. [Source](C:/Users/k/Repository/External/Godot_4-7-2/servers/rendering/renderer_rd/shaders/scene_forward_lights_inc.glsl:251).

### R2 — Ordinary UV2 scale/offset are gated by the detail UV selector

**Source-confirmed generator inconsistency.** The vertex shader transforms UV2 only when `detail_uv == DETAIL_UV_2` and UV2 is not triplanar. Emission/AO can separately read UV2 without that selector. Consequently, their nontriplanar UV2 scale/offset can be ignored until the Detail UV layer is set to UV2. The generator condition does not require Detail Enabled, so selecting UV2 programmatically can serve as a workaround without enabling the detail feature. Prefer a custom shader when independent, predictable coordinate control matters. [Transform condition](C:/Users/k/Repository/External/Godot_4-7-2/scene/resources/material.cpp:1226), [UV2 consumers](C:/Users/k/Repository/External/Godot_4-7-2/scene/resources/material.cpp:1486).

### R3 — UV2 triplanar can break a UV1 normal map

**Upstream-reported defect with the same mechanism present locally.** Enabling either triplanar layer replaces the shared tangent/binormal. An ordinary UV1 normal map then uses that replacement basis even if only UV2 AO/emission needed triplanar projection. The source explains why simply enabling UV2 triplanar can change existing UV1 lighting. Avoid this combination, or keep independent normal/projection bases in a custom shader. [Local shared-basis code](C:/Users/k/Repository/External/Godot_4-7-2/scene/resources/material.cpp:1355), [upstream report #120312](https://github.com/godotengine/godot/issues/120312). The report tested 4.6.2/4.6.3; I did not reproduce it on this runtime.

### R4 — Mixed/world triplanar normal handling is inconsistent

**Source-derived correctness risk; not visually reproduced.** UV1 world weights use `MODEL_NORMAL_MATRIX`; UV2 world weights use `mat3(MODEL_MATRIX) * NORMAL`. These are not equivalent under nonuniform scale. Furthermore, the shared tangent-basis world/local choice keys off UV1's world flag even when only UV2 is triplanar. Expect possible weight/orientation errors with mixed settings or nonuniform scaling; uniform scaling and aligned mapping avoid part of the problem. [Source](C:/Users/k/Repository/External/Godot_4-7-2/scene/resources/material.cpp:1355).

### R5 — Alpha Hash permits a zero scale that enters a division

**Source-derived numerical risk; not visually reproduced.** The Inspector permits 0 and the setter passes it through, while the hash function computes `1 / (hash_scale * derivative_size)`, then logarithms and hashes from it. Exactly zero is not a meaningful stable pattern scale. Use a positive scale, normally the default 1. [Property/setter](C:/Users/k/Repository/External/Godot_4-7-2/scene/resources/material.cpp:2895), [hash function](C:/Users/k/Repository/External/Godot_4-7-2/servers/rendering/renderer_rd/shaders/scene_forward_aa_inc.glsl:13).

### R6 — Height mapping subtracts the entire resulting UV from UV2 detail

**Source-confirmed suspicious coordinate operation; visual effect not reproduced.** After computing the displaced UV `ofs`, the generator assigns `base_uv = ofs` and, when detail uses UV2, executes `base_uv2 -= ofs`. This subtracts an absolute coordinate rather than just the parallax displacement. Even at height scale 0, UV2 detail can change by subtracting UV1. Treat height mapping plus UV2 detail as an unsafe combination without a visual check/custom coordinate implementation. [Source](C:/Users/k/Repository/External/Godot_4-7-2/scene/resources/material.cpp:1582).

### R7 — ObjectDither uses a four-component distance

**Source-confirmed distance discrepancy.** The expression is `length(VIEW_MATRIX * MODEL_MATRIX[3])`, with no `.xyz`. For ordinary affine transforms, the result's W is 1; the calculated distance is therefore `sqrt(x*x+y*y+z*z+1)`, not the three-dimensional origin distance. It reads 1 at the camera origin and about 1.414 at a true distance of 1. This primarily distorts short fade distances. PixelDither uses `length(VERTEX)` and does not have this particular discrepancy. [Source](C:/Users/k/Repository/External/Godot_4-7-2/scene/resources/material.cpp:1838).

### R8 — Stencil effect passes do not inherit FOV and other vertex settings

**Upstream-reported FOV defect, corroborated by local construction.** Outline/X-Ray creates a fresh StandardMaterial3D and copies selected effect settings; FOV override, z-clip scale, billboard and other arbitrary base vertex settings are not copied. The extra pass can detach from the original mesh if their projection/deformation differs. An explicit next-pass material with matched transforms, or a custom effect, provides control. [Pass construction](C:/Users/k/Repository/External/Godot_4-7-2/scene/resources/material.cpp:3130), [FOV report #111482](https://github.com/godotengine/godot/issues/111482). No reproduction on this GPU was performed.

### R9 — Editing an automatically generated stencil next pass is fragile

**Source-confirmed reset behavior with a related upstream report.** `_prepare_stencil_effect()` actively sets outline depth testing on and X-Ray depth testing off, along with color, growth and other fields. Recreating/preparing the owned pass can overwrite direct edits. Therefore an Inspector change to the internal next pass is not a reliable persisted customization workflow. Use Custom stencil/manual passes for custom behavior. [Local preparation](C:/Users/k/Repository/External/Godot_4-7-2/scene/resources/material.cpp:3152), [report #119805](https://github.com/godotengine/godot/issues/119805). That report used Compatibility; the shared material construction is still relevant, but it is not a D3D12 reproduction.

### R10 — Equal/reversed distance-fade endpoints are not robustly defined

**Source-derived numerical/portability risk.** Both alpha and dither paths directly call `smoothstep(min,max,distance)`. Equal endpoints make the usual interpolation division singular. Reversed endpoints are documented as fade-out by Godot, but the standard GLSL smoothstep contract does not guarantee that ordering. The local generated code contains no explicit reversal branch. Do not claim a definite observed D3D12 failure; this is an implementation-dependent boundary. Use distinct ordered endpoints for guaranteed fade-in, and explicitly invert a well-defined fade in a custom shader when robust fade-out behavior is required. [Fade expressions](C:/Users/k/Repository/External/Godot_4-7-2/scene/resources/material.cpp:1851), [GLSL smoothstep specification](https://registry.khronos.org/SPIR-V/specs/1.0/GLSL.std.450.html).

### R11 — Several local documentation descriptions do not match implementation

These are documentation/expectation issues, not automatically runtime defects:

- `clearcoat_texture` green is described as glossiness, but the generator multiplies `CLEARCOAT_ROUGHNESS` by green. Higher green means rougher, not glossier.
- Clearcoat's “secondary transparent pass” wording describes a lighting layer poorly; this material feature does not automatically create a second transparent draw.
- Heightmap layer limits are described using near/far camera distance. The generated count interpolates using the absolute tangent-space view/normal dot product, so the deciding input is angle.
- `disable_specular_occlusion` says it disables occlusion even if the global enable switch is false. The implementation is a local disable layered on the global enable; false globally already disables the feature.
- Skin mode hides the transmittance color/texture and ignores their RGB, but its shader still multiplies transmission by their resulting alpha. The broad claim that these inputs are ignored is incomplete.
- The property anisotropy range is -1–1, whereas its generated shader uniform hint says 0–1. Use the registered property's supported domain.

[Local XML](C:/Users/k/Repository/External/Godot_4-7-2/doc/classes/BaseMaterial3D.xml:1), [clearcoat equation](C:/Users/k/Repository/External/Godot_4-7-2/scene/resources/material.cpp:1904), [angle-based layers](C:/Users/k/Repository/External/Godot_4-7-2/scene/resources/material.cpp:1526), [global occlusion define](C:/Users/k/Repository/External/Godot_4-7-2/servers/rendering/renderer_rd/forward_clustered/render_forward_clustered.cpp:5122).

### R12 — “Specular zero disables all reflections” has qualifications

**Source-confirmed limitation of a broad documentation claim.** Base F0 is `mix(0.16 * specular * specular, albedo, metallic)`. At metallic=1 it is albedo regardless of the specular scalar. Clearcoat has its own reflectance, and Toon direct highlights use a separate expression. Thus neither specular=0 nor Disabled direct specular alone is a universal reflection kill switch for every property combination. Your metallic=0, clearcoat-off, specular-disabled combination avoids those cases. It still does not guarantee that every indirect sampling operation is optimized away. [F0](C:/Users/k/Repository/External/Godot_4-7-2/servers/rendering/renderer_rd/shaders/scene_forward_lights_inc.glsl:82), [coat and Toon](C:/Users/k/Repository/External/Godot_4-7-2/servers/rendering/renderer_rd/shaders/scene_forward_lights_inc.glsl:206), [indirect-light evaluation](C:/Users/k/Repository/External/Godot_4-7-2/servers/rendering/renderer_rd/shaders/forward_clustered/scene_forward_clustered.glsl:2236).

### R13 — Stencil priority boundary and update order

**Historical report plus locally inspected guard/remaining constraint.** The old maximum-priority stencil issue was closed upstream. Locally, selecting Outline/X-Ray at priority 127 explicitly fails because the generated next pass wants `priority + 1`. Do not list the old report as simply unfixed. However, the inherited `set_render_priority()` only updates the parent material; it does not update an already-created stencil next pass. Setting priority after enabling the effect can therefore leave the companion pass at its previous priority; arranging the desired parent priority before effect creation is safer. This ordering risk was not visually reproduced. [Stencil guard](C:/Users/k/Repository/External/Godot_4-7-2/scene/resources/material.cpp:3202), [priority setter](C:/Users/k/Repository/External/Godot_4-7-2/scene/resources/material.cpp:72), [historical report #108905](https://github.com/godotengine/godot/issues/108905).

### R14 — Alpha antialiasing has inconsistent shadow eligibility

**Source-confirmed disagreement between renderer decisions; runtime effect not reproduced.** `uses_depth_in_alpha_pass()` explicitly accepts alpha antialiasing and the surface cache grants it depth/shadow pass flags. However, `casts_shadows()` treats it as alpha and does not include the same exception unless depth-prepass alpha is also enabled. The scene culler calls that latter function when deciding whether an instance can cast shadows. A single-surface alpha-antialiased cutout can therefore be rejected earlier despite the later pass flags; multi-surface/next-pass cases can differ. Do not assume that switching a working Scissor/Hash material to edge antialiasing preserves its shadow behavior. [Pass helper](C:/Users/k/Repository/External/Godot_4-7-2/servers/rendering/renderer_rd/forward_clustered/scene_shader_forward_clustered.h:292), [shadow test](C:/Users/k/Repository/External/Godot_4-7-2/servers/rendering/renderer_rd/forward_clustered/scene_shader_forward_clustered.cpp:254), [scene culler](C:/Users/k/Repository/External/Godot_4-7-2/servers/rendering/renderer_scene_cull.cpp:4204).

## Completeness and validation

The reference was checked against the actual `ADD_PROPERTY`/`ADD_PROPERTYI` registrations: 131 BaseMaterial3D names, two Material names and four Resource names, plus Object's script and dynamic metadata family. StandardMaterial3D has no additional property-registration block. `orm_texture` is covered explicitly even though it is hidden for this class. API methods, constants, Inspector group labels, legacy Godot 3 property aliases, `.tres` serialization header fields and arbitrary user-script exports are not additional built-in material controls.

The checks verify coverage, source-link targets, enum/range transcription and document structure; they do not substitute for visual tests or benchmarks. The useful present-day decision is small: keep your current simple opaque material, consider Lambert if its appearance suits you, and measure before replacing StandardMaterial3D with a custom shader.
