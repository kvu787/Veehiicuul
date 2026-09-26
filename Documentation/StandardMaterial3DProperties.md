# StandardMaterial3D property reference — Godot 4.7.2

Prepared September 25, 2026 for Windows 11 x64, Direct3D 12, Forward+. Read this with the [material review and issue register](C:/Users/k/Repository/Veehiicuul/Documentation/StandardMaterial3DReview.md).

## Coverage and interpretation

This covers all **131 properties registered by BaseMaterial3D**, including the hidden, inapplicable `orm_texture`, plus the two Material properties, four Resource properties, Object's `script`, and dynamic metadata. StandardMaterial3D itself adds no new registered properties. Hidden settings are included, not just the settings visible for your current material.

The source is `C:/Users/k/Repository/External/Godot_4-7-2`, clean at `ed1daf0bf001b61586d9930840f2f1394092c079`, tagged `4.7.2-stable`. The inventory and Inspector ranges come from [property registration](C:/Users/k/Repository/External/Godot_4-7-2/scene/resources/material.cpp:3558). Defaults were checked against the [constructor](C:/Users/k/Repository/External/Godot_4-7-2/scene/resources/material.cpp:3908) and [member initializers](C:/Users/k/Repository/External/Godot_4-7-2/scene/resources/material.h:535), rather than assuming an omitted value is zero.

“All values” means every supported enum choice and both Boolean states, with continuous numeric/color/vector domains described parametrically. It does not mean a list of every floating-point bit pattern, every possible texture, or every combination of settings. Numeric ranges below are **Inspector ranges**, unless explicitly described as setter clamps. Most setters store out-of-range numbers unchanged; those are not additional supported rendering modes. NaN, infinity, invalid enum integers, wrong resource types, and invalid textures have no useful guaranteed visual behavior. Do not depend on them. The `MAX` enum constants are sentinels, not selectable modes.

Each entry's **Issues** field reports a specific limitation or refers to the issue register. **None identified** means no additional property-specific defect found in the examined code; it does not certify bug-free behavior. Runtime reproduction and GPU timing were not performed. R1–R14 distinguish source defects, source-derived risks, documentation discrepancies, and upstream reports.

## Performance rules used throughout

- **Variant:** changes generated shader code or pipeline state. The first use of a new combination can incur compilation/pipeline work. Identical combinations share a generated shader. Repeatedly changing flags is different from changing a color uniform. See [shader cache](C:/Users/k/Repository/External/Godot_4-7-2/scene/resources/material.cpp:653) and [material key](C:/Users/k/Repository/External/Godot_4-7-2/scene/resources/material.h:359).
- **Uniform:** changes a shader input. Different numbers usually execute the same instructions. Zero strength is not equivalent to disabling a feature. Texture content, branch outcomes, screen coverage and subsequent renderer effects can still change cost.
- **Texture:** null binds a renderer fallback; an assigned Texture2D uses its image. Size, compression, mipmaps, filtering, cache behavior and dynamic updates determine memory/bandwidth cost. A SubViewport texture also has the separate cost of rendering that viewport; procedural or animated textures can have separate generation/upload costs. These apply to every texture entry. GPU reads listed here are source-level sample operations, not measured memory transactions.
- **Feature off:** the feature's generated section is omitted, even if its texture or strength is assigned. Retaining a reference can still retain texture memory. **Feature on:** its sample/math can remain even with a null texture or zero strength.
- **Triplanar:** an affected ordinary texture read becomes three reads and a blend. This is not a claim that total frame cost triples.
- **Transparent path:** often costs more because of sorting, overlap, blending, reduced depth rejection, and exclusions from effects that use opaque buffers. Merely setting alpha to 1 does not restore the opaque pipeline. Some other settings force this path even with `transparency = Disabled`.

## Transparency and depth

Implementation: [render modes](C:/Users/k/Repository/External/Godot_4-7-2/scene/resources/material.cpp:759), [alpha generation](C:/Users/k/Repository/External/Godot_4-7-2/scene/resources/material.cpp:1805), [alpha tests](C:/Users/k/Repository/External/Godot_4-7-2/servers/rendering/renderer_rd/shaders/forward_clustered/scene_forward_clustered.glsl:1380), [shadow eligibility](C:/Users/k/Repository/External/Godot_4-7-2/servers/rendering/renderer_rd/forward_clustered/scene_shader_forward_clustered.cpp:253).

### `transparency`

**Values/default:** `0 Disabled` (default), `1 Alpha`, `2 Alpha Scissor`, `3 Alpha Hash`, `4 Depth Pre-Pass`.

**Behavior and performance:** 0 normally ignores albedo alpha and uses the efficient opaque path. 1 blends smooth opacity, sorts transparent surfaces and ordinarily does not cast shadows. 2 discards pixels below the threshold, retaining hard opaque cutouts; it can cast cutout shadows and avoids ordinary alpha blending, but adds discard and edge aliasing. 3 uses a spatially hashed threshold for stippled partial coverage, can cast hashed shadows, and adds derivatives/hash work; it is generally more expensive than a simple scissor. 4 adds an opaque-part depth pass before transparent color drawing and can cast shadows from sufficiently opaque portions; this adds geometry/depth work and improves some self-overlap cases without solving all transparent sorting.

**Issues:** Sorting is primarily by object/surface, not an exact per-pixel solution for intersecting transparent geometry. Alpha antialiasing changes pipeline/shadow eligibility (see below). Your project disables the general depth prepass; do not assume the same depth-prepass benefits as a default project. Depth writes/tests and other features also affect eligibility. R5 for hash scale zero.

### `alpha_scissor_threshold`

**Values/default:** 0–1, default 0.5. **Behavior:** in Scissor mode, discard when alpha is strictly below this value. At 0 even zero-alpha fragments survive the plain scissor; at 1 only fully opaque values survive. Intermediate values set the cutoff. **Performance:** uniform comparison; varying the cutoff changes surviving coverage, not the shader variant. **Issues:** mipmapped alpha can shrink/disappear or grow at distance. With edge antialiasing this also contributes to the antialiasing threshold.

### `alpha_hash_scale`

**Values/default:** 0–2, default 1. **Behavior:** changes the spatial scale of the hash pattern, not overall opacity directly; larger positive scales make a coarser spatial hash grid and smaller positive scales a finer grid, with blending between scale levels. **Performance:** uniform; the hash/derivative work remains for every positive value. **Issues:** **avoid exactly 0**: the shader divides by this value (R5). Negative/out-of-range values are not protected by the setter.

### `alpha_antialiasing_mode`

**Values/default:** `0 Disabled` (default), `1 Alpha Edge Blend`, `2 Alpha Edge Clip`. **Behavior:** only generated for Scissor or Hash. 0 uses the normal cutout/hash result. 1 adds alpha-to-coverage plus alpha blending; 2 also forces alpha to one for the coverage result, producing a firmer cutout. **Performance:** 1/2 add derivative-based alpha edge computation and multisample coverage; require 3D MSAA for the intended smoothing and inherit its memory/raster cost. They override the normal blend mode. **Issues:** the Forward+ shadow-eligibility test and surface-pass flags disagree about alpha-antialiased cutouts (R14); do not assume identical shadow casting to plain scissor/hash. No MSAA is enabled in the saved project. Edge smoothing is not a substitute for correctly authored alpha/mipmaps.

### `alpha_antialiasing_edge`

**Values/default:** 0–1, default 0.3. **Behavior:** determines the derivative-smoothed alpha threshold when alpha antialiasing is active. For Scissor the actual threshold is `clamp(scissor + edge, 0, 1)`; for Hash this value is used directly. **Performance:** uniform; does not adjust MSAA sample count. **Issues:** a large value can erase thin features. Inactive when the antialiasing mode is disabled.

### `blend_mode`

**Values/default:** `0 Mix` (default), `1 Add`, `2 Subtract`, `3 Multiply`, `4 Premultiplied Alpha`. **Behavior:** with source color `S`, destination `D`, source alpha `a`, the RGB blend equations are: Mix `S*a + D*(1-a)`; Add `S*a + D`; Subtract `D-S*a`; Multiply `S*D`; Premultiplied `S+D*(1-a)`.

**Performance:** Mix can remain opaque when nothing needs alpha. Every other mode forces the transparent path; the arithmetic difference between blend operators is usually less important than overdraw. **Issues:** Multiply does not use alpha as a simple opacity slider in this equation. Premultiplied expects matching premultiplied output/content; inappropriate input produces fringes or excess brightness. For shaded premultiplication, the documented `PREMUL_ALPHA_FACTOR` control requires a custom shader; StandardMaterial3D does not emit that assignment automatically. Alpha antialiasing overrides this control. [Exact blend state](C:/Users/k/Repository/External/Godot_4-7-2/servers/rendering/renderer_rd/storage_rd/material_storage.cpp:651).

### `cull_mode`

**Values/default:** `0 Back` (default), `1 Front`, `2 Disabled`. **Behavior:** discard back-facing triangles, discard front-facing triangles, or render both orientations. **Performance:** Back/Front avoid rasterizing the culled orientation; Disabled can substantially increase fragment work for shells and transparent objects. It does not automatically double every mesh's cost. **Issues:** incorrect winding/negative transforms can expose the wrong faces; disabling culling is not a repair for broken topology. Double-sided lighting is not physical thickness or transmission.

### `depth_draw_mode`

**Values/default:** `0 Opaque Only` (default), `1 Always`, `2 Never`. **Behavior:** 0 writes depth in opaque rendering; 1 also allows depth writing for transparent rendering; 2 suppresses depth writes, not depth tests. **Performance:** depth writing can let subsequent geometry reject hidden fragments; Never also routes the material into the alpha queue in this Forward+ implementation and can increase overdraw. Always can occlude later transparency, changing the image. **Issues:** Refraction explicitly overrides this to Always in the generated shader. `no_depth_test` also affects whether depth writes are enabled in Forward+. Transparent sorting is not fixed merely by choosing Always.

### `no_depth_test`

**Values/default:** false (default), true. **Behavior:** false uses `depth_test`; true draws without the usual depth rejection and ignores `depth_test`. **Performance:** true routes the material into the alpha queue, can shade fragments otherwise rejected and, in this Forward+ pipeline setup, also prevents the normal depth-write setup. **Issues:** draw order determines visibility; this is appropriate for deliberate overlays, not an opaque-world optimization. Stencil-created next passes have separate settings (R9).

### `depth_test`

**Values/default:** `0 Default` (default), `1 Inverted`. **Behavior:** with reversed Z, 0 uses greater-or-equal depth; 1 uses less depth, admitting fragments behind the stored surface. **Performance:** both use hardware depth tests; Inverted also routes the material into the alpha queue, changing visibility/overdraw and ordering. **Issues:** ignored if depth testing is disabled. Inverted depth is useful for transparent/depth-write-disabled effects; ordinary opaque ordering or prepasses can make it ineffective or surprising. [Depth state](C:/Users/k/Repository/External/Godot_4-7-2/servers/rendering/renderer_rd/forward_clustered/scene_shader_forward_clustered.cpp:355).

## Shading

Implementation: [per-pixel light evaluation](C:/Users/k/Repository/External/Godot_4-7-2/servers/rendering/renderer_rd/shaders/scene_forward_lights_inc.glsl:177), [per-vertex evaluation](C:/Users/k/Repository/External/Godot_4-7-2/servers/rendering/renderer_rd/shaders/scene_forward_vertex_lights_inc.glsl:13), [ambient and reflections](C:/Users/k/Repository/External/Godot_4-7-2/servers/rendering/renderer_rd/shaders/forward_clustered/scene_forward_clustered.glsl:1687).

### `shading_mode`

**Values/default:** `0 Unshaded`, `1 Per-Pixel` (default), `2 Per-Vertex`. **Behavior:** 0 displays material color without the standard lighting evaluation; fog still applies unless separately disabled. 1 evaluates lighting across fragments and supports the full material lighting model. 2 computes simplified direct lighting at vertices and interpolates it. **Performance:** 0 removes most lighting work; 2 can save substantial work when vertices are few relative to shaded pixels; 1 generally costs more per pixel. Your very dense sphere means 2 is not automatically faster. **Issues:** vertex lighting is not the same BRDF evaluated less often: Burley/Toon diffuse fall back to Lambert, except Wrap; enabled specular uses normalized Blinn. Normal/bent-normal, anisotropy, clearcoat and backlight controls are hidden outside Per-Pixel. Hidden values are not necessarily reset. Low-poly meshes can show interpolation artifacts and miss small lights.

### `diffuse_mode`

**Values/default:** `0 Burley` (default), `1 Lambert`, `2 Lambert Wrap`, `3 Toon`. **Behavior/performance:** Burley adds view/roughness-dependent diffuse Fresnel terms; Lambert is the simpler clamped normal/light dot product; Wrap broadens lighting around the terminator using roughness and adds a division; Toon uses a smoothstep transition whose width depends on roughness. Lambert has the simplest ordinary direct-light math. **Issues:** Per-Vertex substitutes simpler models as above. Area lights use a separate LTC evaluation and do not reproduce every punctual-light distinction. Fully metallic surfaces lose ordinary diffuse contribution. Your roughness remains relevant with specular disabled because Burley uses it.

### `specular_mode`

**Values/default:** `0 SchlickGGX` (default), `1 Toon`, `2 Disabled` (your value). **Behavior/performance:** 0 evaluates the microfacet highlight (anisotropic when enabled); 1 produces a stylized highlight using a different expression and writes it into diffuse light; 2 compiles out the ordinary direct specular lobe. Disabled is the appropriate direct-highlight optimization. **Issues:** it does not remove environment/probe/GI/SSR reflection work. Clearcoat is a separate lobe and can remain. Toon is not scaled by `metallic_specular` in the same manner as GGX. At exactly zero roughness the punctual-light lobe is skipped (R1). Per-Vertex uses its own approximation.

### `disable_ambient_light`

**Values/default:** false (default), true. **Behavior:** false permits the environment/indirect-light path; true emits `ambient_light_disabled`, excluding the ambient/reflection blocks, including several GI/SSR contributions in Forward+. Direct lights remain. **Performance:** true can remove meaningful indirect-light evaluation; scene-wide effects may still be generated for other objects. **Issues:** it will remove your environment's 0.2 ambient fill and darken unlit-facing areas. This is a visual change, not a free replacement for your current setup.

### `disable_fog`

**Values/default:** false (default), true (your value). **Behavior:** false permits depth/height and volumetric fog; true excludes fog application to this material. **Performance:** true removes the material fog code, including volumetric lookup where applicable, but does not stop world volumetric-fog generation. **Issues:** a fog-free object can look disconnected from a foggy scene. Your saved environment has fog off, so do not expect a large current benefit.

### `disable_specular_occlusion`

**Values/default:** false (default), true (your value). **Behavior:** false permits the global specular-occlusion feature; true bypasses its attenuation of indirect reflections. With a bent normal, this can be a cone-intersection calculation; without one it is an ambient-luminance heuristic. **Performance:** true removes that section. **Issues:** it can brighten reflections/leak light when reflections are enabled. It does not disable reflections. With your nonmetallic, zero-specular, no-clearcoat material, it has no intended visible reflection to change. Local XML has a wording error about the global toggle (R11).

## Vertex color and albedo

Implementation: [vertex color conversion](C:/Users/k/Repository/External/Godot_4-7-2/scene/resources/material.cpp:1191), [albedo/MSDF evaluation](C:/Users/k/Repository/External/Godot_4-7-2/scene/resources/material.cpp:1591), [fallback textures](C:/Users/k/Repository/External/Godot_4-7-2/servers/rendering/renderer_rd/storage_rd/material_storage.cpp:1056).

### `vertex_color_use_as_albedo`

**Values/default:** false (default), true. **Behavior:** false ignores mesh vertex color for base color; true multiplies the albedo texture by interpolated vertex RGBA, then by `albedo_color`. Alpha matters only on an alpha-using path. **Performance:** true adds vertex-color input/interpolation and a multiply; it can support color variation without unique textures/materials. **Issues:** missing or unintended vertex colors can produce unexpected tints. It does not replace the albedo texture sample.

### `vertex_color_is_srgb`

**Values/default:** false (default), true. **Behavior:** false treats vertex RGB as linear; true converts sRGB to linear in the vertex shader for Forward+. **Performance:** true adds piecewise conversion/power work per vertex when used. **Issues:** choose according to how colors were authored; a wrong choice darkens or brightens them. With vertex color unused, there is no useful albedo effect and dead work may be optimized away.

### `albedo_color`

**Values/default:** any Color; ordinary RGBA components 0–1; default white `(1,1,1,1)`. **Behavior:** RGB multiplies texture/vertex base color. Black suppresses base color, white preserves it, intermediate colors tint it. Alpha multiplies opacity only when the shader uses alpha/refraction. Your value is `(0.15155911,0.5057727,0.8741714,1)`. **Performance:** uniform; changing hue costs essentially the same shader work. **Issues:** source-color inputs are converted appropriately for linear rendering; displayed final RGB also depends on lighting/exposure/tonemapping. Detail albedo is applied later and is not multiplied by this color. HDR/negative inputs are representable but not ordinary reflectance.

### `albedo_texture`

**Values/default:** null (default) or Texture2D. **Behavior:** null uses white; texture RGB multiplies base color and texture alpha supplies alpha where enabled. **Performance:** the source emits a sample even for null; an image adds its storage/bandwidth footprint. **Issues:** transparency is not enabled automatically by assigning an alpha image. Color-data import should match its use; use the MSDF toggle only for suitable distance-field data.

### `albedo_texture_force_srgb`

**Values/default:** false (default), true. **Behavior:** false relies on the ordinary source-color sampling path; true adds a manual sRGB-to-linear conversion, useful only for a texture whose encoding needs it. **Performance:** true adds per-fragment piecewise/power math. **Issues:** ordinary imported color textures usually already receive the proper conversion; enabling this unnecessarily can double-convert and darken them. Active MSDF decoding takes precedence over this branch.

### `albedo_texture_msdf`

**Values/default:** false (default), true. **Behavior:** false treats RGB as color; true interprets RGB as multichannel signed distances, derives coverage using the median, uses white as texture RGB, and optionally uses alpha for the outline. **Performance:** true adds conversion, median, derivatives and coverage math. **Issues:** needs a matching MSDF texture, correct pixel range, and an alpha-using rendering mode to expose coverage. UV1 triplanar disables MSDF with a warning. Null is not useful MSDF content.

### `orm_texture`

**Values/default:** null or Texture2D, default null. **Behavior:** registered on BaseMaterial3D but explicitly hidden/unused for StandardMaterial3D. On the separate ORMMaterial3D class, R/G/B mean AO/roughness/metallic. **Performance:** setting this on StandardMaterial3D does not consolidate its three material reads; it can merely retain an unused texture reference. **Issues:** do not use it as an optimization switch on this class. Changing to ORMMaterial3D is a separate material choice.

## Metallic and roughness

Implementation: [generated samples](C:/Users/k/Repository/External/Godot_4-7-2/scene/resources/material.cpp:1664), [F0](C:/Users/k/Repository/External/Godot_4-7-2/servers/rendering/renderer_rd/shaders/scene_forward_lights_inc.glsl:82), [channel setters](C:/Users/k/Repository/External/Godot_4-7-2/scene/resources/material.cpp:2922).

### `metallic`

**Values/default:** 0–1, default 0. **Behavior:** 0 is dielectric/nonmetal; 1 uses albedo as metallic reflection color and removes ordinary diffuse; intermediate values blend. It is multiplied by the chosen metallic texture channel. **Performance:** uniform; fully metallic values can skip diffuse evaluation, but increasing this is not a general optimization. **Issues:** a fully metallic material with direct specular disabled and no reflection source can become very dark. Specular zero is not a universal reflection-off switch for metals (R12).

### `metallic_specular`

**Values/default:** 0–1, default 0.5; your value 0. **Behavior:** dielectric normal-incidence reflectance is `0.16 * specular * specular`: 0 gives 0, 0.5 gives 0.04, 1 gives 0.16. This is mixed with albedo using metallic. **Performance:** uniform; zero does not select a cheaper generated shader or remove the environment-sampling code. Pairing it with Disabled specular removes the direct lobe separately. **Issues:** zero suppresses the base reflection for your metallic=0 case, but not arbitrary metallic/clearcoat/Toon combinations (R12). Values above 0.5 deliberately depart from the usual dielectric baseline.

### `metallic_texture`

**Values/default:** null (white) or Texture2D, default null. **Behavior:** sampled channel multiplies metallic; black gives nonmetal, white preserves the scalar, intermediate values blend. **Performance:** one generated sample even when null or metallic=0; triplanar makes three. **Issues:** setting a metallic map while the scalar stays 0 has no visual effect. Packed data must use appropriate linear data handling.

### `metallic_texture_channel`

**Values/default:** `0 Red` (default), `1 Green`, `2 Blue`, `3 Alpha`, `4 Gray`. **Behavior:** selects R/G/B/A or the arithmetic mean `(R+G+B)/3`, respectively. **Performance:** a uniform dot-product selector; all modes retain the sample. **Issues:** Gray is not perceptual luminance. Alpha on an opaque texture is generally 1. Packing several maps into one texture reduces asset memory/references but does not automatically merge the StandardMaterial3D sample expressions.

### `roughness`

**Values/default:** 0–1, default 1. **Behavior:** multiplied by the roughness texture; 0 is the sharpest intended reflection, 1 the broadest/roughest, intermediate values continuously adjust it. Also affects Burley/Wrap/Toon diffuse, rim width and refraction blur. **Performance:** uniform; it is not a lighting-quality setting. It can affect reflection mip selection and SSR eligibility/work, so total cost is scene-dependent. **Issues:** exact 0 suppresses the punctual direct specular lobe in this source (R1). Lowering it is not visually neutral even with `specular_mode = Disabled`.

### `roughness_texture`

**Values/default:** null (white) or Texture2D, default null. **Behavior:** selected channel multiplies roughness; black means 0, white preserves the scalar. **Performance:** one generated sample even when null; three with triplanar. **Issues:** black map regions hit the roughness-zero edge case. A roughness map is data, not color or glossiness; invert a gloss map if needed. Import-time roughness processing may also affect the result.

### `roughness_texture_channel`

**Values/default:** `0 Red` (default), `1 Green`, `2 Blue`, `3 Alpha`, `4 Gray`. **Behavior:** R/G/B/A or equal-weight RGB average. **Performance:** unlike the other channel selectors, this selects generated channel constants and roughness sampler hints, so it changes the shader key; steady sampling cost is similar. **Issues:** same data/alpha/packing caveats as metallic; changing the channel is not equivalent to reducing texture memory.

## Emission

Implementation: [emission generation](C:/Users/k/Repository/External/Godot_4-7-2/scene/resources/material.cpp:1760), [energy setters](C:/Users/k/Repository/External/Godot_4-7-2/scene/resources/material.cpp:2167).

### `emission_enabled`

**Values/default:** false (default), true. **Behavior:** false omits the emission section; true adds self-lit color to the surface. **Performance:** true adds a texture sample and arithmetic; glow/GI can add separate scene costs when enabled. **Issues:** emission is not automatically a light illuminating other objects. That requires a suitable GI/baking setup. It does not automatically enable glow.

### `emission`

**Values/default:** RGB Color, default black `(0,0,0,1)`; alpha unused. **Behavior:** with emission enabled, Add combines this with the emission texture; Multiply tints/multiplies that texture. Black contributes nothing in Add and zeros the result in Multiply; positive RGB produces the corresponding emitted color. **Performance:** uniform. **Issues:** black does not turn the feature off, and Add can still emit from a nonblack texture. Bright/HDR values affect exposure and glow if used.

### `emission_energy_multiplier`

**Values/default:** 0–16 with larger values allowed by the Inspector; default 1. **Behavior:** 0 zeros emission, 1 preserves it, intermediate/larger values scale it. With physical light units it also multiplies `emission_intensity`. **Performance:** uniform, not sample/feature removal at 0. **Issues:** high values can saturate the tonemapped image or create bloom; this is a content choice, not a measured quality/cost control.

### `emission_intensity`

**Values/default:** 0–100000 nits with larger values allowed; default 1000. **Behavior:** physical-unit emission luminance, multiplied by the energy multiplier. **Performance:** uniform; numeric magnitude does not add shader instructions. **Issues:** hidden when physical light units are disabled; the setter explicitly rejects setting it in that state. Your project does not enable physical units.

### `emission_operator`

**Values/default:** `0 Add` (default), `1 Multiply`. **Behavior:** Add gives `(color + texture) * energy`; Multiply gives `(color * texture) * energy`. **Performance:** variant selection with very similar steady arithmetic/sample cost. **Issues:** a null emission texture is black, so Multiply with no texture yields zero even with a bright emission color.

### `emission_on_uv2`

**Values/default:** false (default), true. **Behavior:** false samples emission with UV1; true selects UV2, or UV2 triplanar when enabled. **Performance:** switching ordinary UV sets is small; selecting triplanar adds reads. **Issues:** UV2 must exist for ordinary mapping. Nontriplanar UV2 scale/offset have an unexpected dependency on `detail_uv_layer` (R2).

### `emission_texture`

**Values/default:** null (black) or Texture2D, default null. **Behavior:** uses RGB in the emission equation; alpha unused. **Performance:** one read when emission is enabled, three if its selected UV layer is triplanar; no emission read when disabled. **Issues:** null plus Multiply is black. Correct color encoding and HDR format matter for bright emission data.

## Normal and bent normal maps

Implementation: [normal inputs](C:/Users/k/Repository/External/Godot_4-7-2/scene/resources/material.cpp:1736), [Forward+ reconstruction](C:/Users/k/Repository/External/Godot_4-7-2/servers/rendering/renderer_rd/shaders/forward_clustered/scene_forward_clustered.glsl:1435), [bent-normal specular occlusion](C:/Users/k/Repository/External/Godot_4-7-2/servers/rendering/renderer_rd/shaders/forward_clustered/scene_forward_clustered.glsl:2142).

### `normal_enabled`

**Values/default:** false (default), true. **Behavior:** false uses geometric/interpolated normals; true samples a tangent-space normal map and perturbs the lighting normal. **Performance:** true adds a read, tangent/binormal inputs and reconstruction/normalization. **Issues:** requires a valid tangent basis for ordinary UV mapping. It does not change silhouettes, geometry or collision. UV2 triplanar can unexpectedly disturb it (R3).

### `normal_scale`

**Values/default:** -16–16, default 1. **Behavior:** 0 gives the base normal, 1 the authored map, between 0 and 1 weakens it, above 1 exaggerates it, negative values reverse/extrapolate the perturbation. **Performance:** uniform; 0 still retains enabled mapping code. **Issues:** extreme/negative values can give unnatural or unstable-looking lighting; they do not represent actual geometry thickness.

### `normal_texture`

**Values/default:** null (flat-normal fallback) or Texture2D, default null. **Behavior:** reads X/Y from R/G and reconstructs positive Z; B/A are not normal-direction inputs. Expected convention is X+, Y+, Z+. **Performance:** one read while enabled, three for UV1 triplanar. **Issues:** normal data must not be treated as ordinary sRGB color; incorrect green-channel convention or tangents produces flipped/broken lighting. R3/R4 for triplanar interactions. Clearcoat intentionally uses geometric normals instead of this map.

### `bent_normal_enabled`

**Values/default:** false (default), true. **Behavior:** false uses the ordinary indirect-light direction; true supplies the average unoccluded ambient direction and enables more detailed specular-occlusion handling. **Performance:** true adds a map sample and direction reconstruction; when specular occlusion is enabled it selects a substantially more involved cone-intersection calculation. **Issues:** this is not a second surface normal map. Requires a correctly baked cosine-weighted bent normal and tangent basis. Disabling specular occlusion removes one benefit, though bent direction can still affect indirect lighting.

### `bent_normal_texture`

**Values/default:** null (flat-normal fallback) or Texture2D, default null. **Behavior:** tangent-space R/G direction, reconstructed positive Z; B/A ignored for direction. **Performance:** one enabled sample or three triplanar samples. **Issues:** same convention/tangent requirements as normal textures; use cosine-weighted baking. R3/R4 also affect the basis used for this map.

## Rim, clearcoat and anisotropy

Implementation: [feature samples](C:/Users/k/Repository/External/Godot_4-7-2/scene/resources/material.cpp:1873), [lighting terms](C:/Users/k/Repository/External/Godot_4-7-2/servers/rendering/renderer_rd/shaders/scene_forward_lights_inc.glsl:190).

### `rim_enabled`

**Values/default:** false (default), true. **Behavior:** true adds a view-angle-dependent lit rim, with its width influenced by roughness; false omits it. **Performance:** true adds a texture read and per-light rim/power math. **Issues:** not an outline generator; it does not enlarge geometry and is not visible as standard lighting in Unshaded mode. Roughness=1 makes its angular power zero, so it is no longer a narrow edge-only term.

### `rim`

**Values/default:** 0–1, default 1. **Behavior:** 0 gives zero rim, 1 full strength, intermediate values scale it, further multiplied by texture red. **Performance:** uniform; zero keeps the feature work. **Issues:** disable `rim_enabled` if unused; None identified beyond the shared rim limitations.

### `rim_tint`

**Values/default:** 0–1, default 0.5. **Behavior:** 0 uses white before multiplication by light color; 1 also tints by albedo; intermediate values interpolate. Texture green scales this value. **Performance:** uniform mix with similar cost at each value. **Issues:** it is not a tint Color and cannot independently pick an arbitrary rim color.

### `rim_texture`

**Values/default:** null (white) or Texture2D, default null. **Behavior:** R multiplies rim strength; G multiplies tint; B/A unused. **Performance:** one read when enabled, three with UV1 triplanar. **Issues:** the texture controls both strength and tint, not strength alone; author both channels accordingly.

### `clearcoat_enabled`

**Values/default:** false (default), true. **Behavior:** true adds a second reflective coat lobe and attenuates the base layer; false omits it. **Performance:** adds a texture read, direct-light coat BRDF, and potentially extra environment/probe sampling. This is evaluated inside the material lighting shader, **not automatically a second transparent draw pass**. **Issues:** uses geometric normals, not `normal_texture`; survives ordinary specular-disabled logic. Local XML's “pass” wording can be misleading (R11/R12).

### `clearcoat`

**Values/default:** 0–1, default 1. **Behavior:** coat coverage/strength multiplied by texture red; 0 removes the visible coat term, 1 full strength. **Performance:** uniform; zero does not remove coat code or samples. **Issues:** use the enable switch to remove cost. A coat can remain reflective when base metallic specular is zero.

### `clearcoat_roughness`

**Values/default:** 0–1, default 0.5. **Behavior:** multiplied by texture green, then used by a deliberately narrow coat roughness mapping; low values sharpen the coat, high values broaden it. The punctual coat calculation maps it into 0.001–0.1. **Performance:** uniform; the coat calculations remain. **Issues:** it is not the same effective roughness range as the base layer. Local XML incorrectly describes texture green as glossiness (R11).

### `clearcoat_texture`

**Values/default:** null (white) or Texture2D, default null. **Behavior:** R multiplies coat strength, G multiplies coat roughness; B/A unused. **Performance:** one enabled sample or three triplanar. **Issues:** green is roughness in this source, not inverted roughness/glossiness (R11). A black green channel creates the sharpest coat even with a high roughness scalar.

### `anisotropy_enabled`

**Values/default:** false (default), true. **Behavior:** true changes the GGX lobe to a direction-dependent shape; false uses isotropic GGX. **Performance:** true adds a flowmap read, tangent-direction work and a more complex BRDF; it is distinct from sampler anisotropic filtering. **Issues:** requires tangents; it is not useful as a disabled-specular optimization and is hidden outside Per-Pixel. Some lighting paths, including area-light approximations, differ from punctual anisotropic GGX.

### `anisotropy`

**Values/default:** -1–1, default 0. **Behavior:** 0 isotropic; positive/negative values change the relative tangent/binormal widths in opposite directions through `sqrt(1 - 0.9*anisotropy)`, after flowmap alpha multiplication. **Performance:** uniform; 0 keeps enabled feature calculations. **Issues:** the generated uniform hint says 0–1 while the actual property supports -1–1; the shader formula accepts the advertised negative range. Avoid values outside that range.

### `anisotropy_flowmap`

**Values/default:** null (engine directional fallback) or Texture2D, default null. **Behavior:** RG remaps from 0–1 into -1–1 to describe direction; A multiplies strength; B ignored. **Performance:** one enabled read or three triplanar reads plus direction math. **Issues:** an RG value encoding a zero direction is not a useful tangent direction; unsuitable maps/tangents can distort highlights. Alpha=0 hides anisotropy without removing its enabled path.

## Ambient occlusion

Implementation: [AO sampling](C:/Users/k/Repository/External/Godot_4-7-2/scene/resources/material.cpp:1928), [ambient/direct application](C:/Users/k/Repository/External/Godot_4-7-2/servers/rendering/renderer_rd/shaders/forward_clustered/scene_forward_clustered.glsl:2139).

### `ao_enabled`

**Values/default:** false (default), true. **Behavior:** true samples authored ambient occlusion, with white unoccluded and black occluded; false omits material AO. **Performance:** true adds a map read and light modulation. **Issues:** this controls texture AO, not the environment SSAO switch. It does not discover new geometry occlusion by itself; using both can over-darken surfaces.

### `ao_light_affect`

**Values/default:** 0–1, default 0. **Behavior:** 0 limits AO's direct-light attenuation to none; 1 fully applies AO to direct lighting; intermediate values use `mix(1, AO, value)`. Ambient AO still applies independently. **Performance:** uniform; no extra sampling as the value increases. **Issues:** full direct-light occlusion often looks too dark because baked crevice AO is not a dynamic shadow map.

### `ao_texture`

**Values/default:** null (white) or Texture2D, default null. **Behavior:** selected channel gives occlusion: 0 darkest, 1 unoccluded, intermediate partial. **Performance:** one enabled sample or three with the selected triplanar layer. **Issues:** use linear data and the correct UV set/channel. Null gives no useful AO but leaves the feature code enabled.

### `ao_on_uv2`

**Values/default:** false (default), true. **Behavior:** false uses UV1; true UV2, including UV2 triplanar when enabled. **Performance:** ordinary UV selection has little effect; triplanar adds samples. **Issues:** normal UV2 must exist; UV2 scale/offset dependence R2 and triplanar basis issue R3 apply.

### `ao_texture_channel`

**Values/default:** `0 Red` (default), `1 Green`, `2 Blue`, `3 Alpha`, `4 Gray`. **Behavior:** channel selection or equal-weight RGB mean. **Performance:** uniform selector; same sample count. **Issues:** not perceptual grayscale; an absent alpha channel ordinarily means full unoccluded AO. Packing does not guarantee merged texture reads.

## Height mapping

Implementation: [parallax code](C:/Users/k/Repository/External/Godot_4-7-2/scene/resources/material.cpp:1496), [layer setters](C:/Users/k/Repository/External/Godot_4-7-2/scene/resources/material.cpp:2830).

### `heightmap_enabled`

**Values/default:** false (default), true. **Behavior:** false leaves UVs unchanged; true offsets UVs based on height and view direction to suggest depth. **Performance:** simple mode adds a height read/math; deep mode adds a variable sample loop and more arithmetic. **Issues:** no real vertex displacement, silhouette, collision or accurate displaced shadows. UV1 triplanar suppresses it with a warning. Grazing views and excessive scale can warp textures. R6 for UV2 detail.

### `heightmap_scale`

**Values/default:** -16–16, default 5. **Behavior:** internally multiplied by 0.01 in the UV offset; 0 removes the visible offset, positive increases apparent depth, negative reverses it. **Performance:** uniform; zero does not remove the enabled reads or deep-parallax loop. **Issues:** apparent physical size depends on UV scale and geometry, so “5 cm” is an authoring convention, not a universal displacement guarantee. Negative scale is not identical to inverting the texture. R6 can affect UV2 detail even at 0.

### `heightmap_deep_parallax`

**Values/default:** false (default), true. **Behavior:** false uses one height lookup with limited offset; true ray-marches layers until crossing the sampled depth and interpolates the intersection. **Performance:** true can require many dependent samples per pixel, with divergent loop lengths. Usually one of the more expensive material options. **Issues:** layer count is angle-dependent, not a camera-distance LOD in this source (R11). It cannot fix silhouette/collision limitations.

### `heightmap_min_layers`

**Values/default:** integer 1–64, default 8. **Behavior:** layer-count endpoint used near a face-on tangent-space view; interpolated toward max at grazing angles. **Performance:** larger values generally increase potential loop steps/samples. **Issues:** setters do not enforce positivity or min≤max. Zero/negative values can cause invalid division or non-progressing loops; remain within the intended domain. Documentation's “far away” explanation is inaccurate here (R11).

### `heightmap_max_layers`

**Values/default:** integer 1–64, default 32. **Behavior:** layer-count endpoint used at grazing angles. **Performance:** larger values improve ray-march resolution at potentially high fragment cost; actual iterations also depend on texture depth. **Issues:** keep max≥min and both positive. It is not directly the “close-up” count despite local XML wording (R11).

### `heightmap_flip_tangent`

**Values/default:** false (default), true. **Behavior:** false retains the tangent sign; true reverses it for the view-to-heightmap basis. **Performance:** uniform sign input; essentially same work. **Issues:** use only to match tangent/map conventions; it does not fix an absent tangent basis or incorrect normal map convention globally.

### `heightmap_flip_binormal`

**Values/default:** false (default), true. **Behavior:** toggles the binormal sign in heightmap interpretation, on top of the generator's convention correction. **Performance:** uniform sign change. **Issues:** same convention caveat as flip tangent; only affects height interpretation.

### `heightmap_texture`

**Values/default:** null (black) or Texture2D, default null. **Behavior:** R is height; by default depth is `1-R`; flip texture uses R as depth. Other channels are unused. **Performance:** one sample in simple mapping, repeated dependent reads in deep mapping; no height read with the feature disabled or suppressed by triplanar. **Issues:** null black is not necessarily “no displacement”: it represents a constant depth in the default interpretation. Linear filtering is forced even when texture filtering is Nearest. Poorly normalized content gives poor depth resolution.

### `heightmap_flip_texture`

**Values/default:** false (default), true. **Behavior:** false interprets brighter texels as higher; true interprets them as deeper. **Performance:** variant; changes a subtraction around sampling, with essentially the same loop/sample workload. **Issues:** avoid double inversion if the source was already inverted on import. Not interchangeable with negative height scale.

## Subsurface scattering and transmittance

Implementation: [material inputs](C:/Users/k/Repository/External/Godot_4-7-2/scene/resources/material.cpp:1960), [transmittance model](C:/Users/k/Repository/External/Godot_4-7-2/servers/rendering/renderer_rd/shaders/scene_forward_lights_inc.glsl:31), [SSS renderer scheduling](C:/Users/k/Repository/External/Godot_4-7-2/servers/rendering/renderer_rd/forward_clustered/render_forward_clustered.cpp:1960).

### `subsurf_scatter_enabled`

**Values/default:** false (default), true. **Behavior:** true supplies scattering strength for Forward+'s screen-space subsurface diffusion; false omits it. **Performance:** can trigger separate lighting buffers and an SSS processing pass in addition to its map read. This is potentially much more than a small local material cost. **Issues:** disabled on transparent-background viewports; global SSS quality can disable the effect. Transparent rendering does not receive the same opaque SSS processing. A strength of 0 does not reliably avoid the renderer's feature/buffer setup.

### `subsurf_scatter_strength`

**Values/default:** 0–1, default 0. **Behavior:** texture-red-scaled scattering strength; 0 none, 1 full, intermediate partial. Global scattering scale also controls spread. **Performance:** uniform; enabling SSS with strength 0 retains the declared feature and can retain renderer costs. **Issues:** use the Boolean to disable an unused effect. Excessive scattering can blur fine surface lighting.

### `subsurf_scatter_skin_mode`

**Values/default:** false (default), true. **Behavior:** false generic scattering/transmittance; true selects a skin-oriented colored diffusion/transmission model. **Performance:** variant with different channel/lighting math, not a general cheaper setting. **Issues:** skin transmission ignores the generic transmittance color/texture for its color model; those properties are hidden. It is not suitable for every translucent substance.

### `subsurf_scatter_texture`

**Values/default:** null (white) or Texture2D, default null. **Behavior:** R multiplies scattering strength; other channels unused. **Performance:** one enabled read or three triplanar. **Issues:** map=0 hides local scattering without removing the feature or necessarily removing the full-screen processing cost.

### `subsurf_scatter_transmittance_enabled`

**Values/default:** false (default), true. **Behavior:** false omits transmission inputs; true enables light passing through the object using the SSS/transmittance lighting path. Intended to accompany `subsurf_scatter_enabled`; the Forward+ transmittance definition is guarded by both enabled features. **Performance:** adds a color-map sample, per-light attenuation math and potentially extra shadow-depth lookups for thickness. **Issues:** depends on usable lighting/shadow data for convincing thickness. Do not confuse it with transparent alpha or refraction. Disabled shadows change the available thickness information.

### `subsurf_scatter_transmittance_color`

**Values/default:** Color RGBA, default white `(1,1,1,1)`. **Behavior:** multiplied by its texture; RGB sets generic transmission color and alpha participates in transmission strength. Black/zero alpha suppresses the corresponding contribution; intermediate values tint/scale it. **Performance:** uniform. **Issues:** skin mode ignores RGB but still multiplies by the resulting alpha, despite hiding this property. Zero values do not disable the feature.

### `subsurf_scatter_transmittance_texture`

**Values/default:** null (white) or Texture2D, default null. **Behavior:** RGBA multiplies transmittance color before generic transmission evaluation. **Performance:** one enabled sample or three triplanar; the generator still emits this input section when the feature is enabled. **Issues:** skin mode hides this control and substitutes its own RGB model, but sampled alpha still scales skin transmission. Texture transparency does not make the geometry alpha-transparent.

### `subsurf_scatter_transmittance_depth`

**Values/default:** 0.001–8 with larger values allowed; default 0.1. **Behavior:** sets the thickness/attenuation distance scale; larger positive values allow light to persist through greater estimated thickness. **Performance:** uniform; not a ray-march sample count. **Issues:** zero/negative input from code is not clamped and can invalidate attenuation/division. Thickness is shadow-derived, not a volumetric simulation.

### `subsurf_scatter_transmittance_boost`

**Values/default:** 0–1, default 0. **Behavior:** increases the angular transmission/backlighting contribution; 0 is the baseline, not necessarily zero transmission; higher values boost it. **Performance:** uniform. **Issues:** high values can look nonphysical or washed out; it is not a substitute for correct depth/color.

## Back lighting and refraction

Implementation: [backlight input](C:/Users/k/Repository/External/Godot_4-7-2/scene/resources/material.cpp:1994), [refraction](C:/Users/k/Repository/External/Godot_4-7-2/scene/resources/material.cpp:1798).

### `backlight_enabled`

**Values/default:** false (default), true. **Behavior:** true adds an inexpensive thin-surface lighting approximation from the opposite side; false omits it. **Performance:** one extra material read and per-light math; generally simpler than full SSS/transmittance. **Issues:** does not measure thickness and does not make an object transparent. It needs lighting; hidden outside Per-Pixel.

### `backlight`

**Values/default:** RGB Color, default black; alpha unused. **Behavior:** added to backlight texture RGB; black contributes nothing, brighter colors increase/tint backlighting. **Performance:** uniform. **Issues:** black plus a nonblack texture still produces backlight. It is an additive color input, not a texture multiplier.

### `backlight_texture`

**Values/default:** null (black) or Texture2D, default null. **Behavior:** RGB adds to the backlight color; A unused. **Performance:** one read when enabled or three triplanar. **Issues:** this sampler lacks the `source_color` hint used by albedo/emission; do not assume identical color-encoding treatment. Null plus black color hides the effect but does not remove it.

### `refraction_enabled`

**Values/default:** false (default), true. **Behavior:** true distorts the already rendered screen using the surface/normal-map direction, albedo alpha and refraction strength; false omits this path. It forces alpha usage and `depth_draw_always`. **Performance:** true adds map, depth and screen-color samples and can require screen copies/mipmaps; transparent overdraw applies. **Issues:** only content available in the opaque screen/depth buffers can be refracted; hidden/offscreen and ordinary transparent layers are unavailable. Roughness drives screen-texture blur. Alpha=1 can hide refraction but still retain its costs. Detail normal is applied later than this calculation.

### `refraction_scale`

**Values/default:** -1–1, default 0.05. **Behavior:** 0 gives no screen-UV distortion, positive/negative distort in opposite directions; magnitude sets displacement. **Performance:** uniform; 0 does not remove screen/depth sampling or the transparent path. **Issues:** large displacement reveals screen-edge/depth discontinuity artifacts; it is not a physical index of refraction.

### `refraction_texture`

**Values/default:** null (white) or Texture2D, default null. **Behavior:** selected channel multiplies refraction scale; black suppresses distortion locally, white preserves it. **Performance:** one enabled read or three triplanar, plus the screen/depth reads. **Issues:** zero texture values do not remove refraction's rendering-path cost.

### `refraction_texture_channel`

**Values/default:** `0 Red` (default), `1 Green`, `2 Blue`, `3 Alpha`, `4 Gray`. **Behavior:** R/G/B/A or equal-weight RGB average. **Performance:** uniform selector; unchanged sample count. **Issues:** Gray is not luminance; choose the channel matching packed linear data. None identified beyond shared refraction limitations.

## Detail overlay

Implementation: [detail composition](C:/Users/k/Repository/External/Godot_4-7-2/scene/resources/material.cpp:2008).

### `detail_enabled`

**Values/default:** false (default), true. **Behavior:** true overlays secondary albedo/normal data controlled by detail alpha and mask red; false omits the whole section. **Performance:** true generates three reads (albedo, normal, mask), even for null slots; affected reads triple under triplanar. It adds normal-map processing too. **Issues:** not a separate draw pass. Null albedo falls back to opaque white, so enabling an otherwise empty Mix detail layer can overwrite base color with white.

### `detail_mask`

**Values/default:** null (white) or Texture2D, default null. **Behavior:** R=0 preserves base result, R=1 applies full detail result, intermediate blends. Always sampled with UV1/UV1 triplanar, even if detail albedo/normal use UV2. **Performance:** one enabled read, three under UV1 triplanar. **Issues:** UV2 detail selection does not move the mask to UV2. Mask zero does not skip the detail texture samples.

### `detail_blend_mode`

**Values/default:** `0 Mix` (default), `1 Add`, `2 Subtract`, `3 Multiply`. **Behavior:** constructs an overlay by replacing, adding, subtracting or multiplying detail RGB against base RGB, weighted by detail alpha, then mixes that result using mask red. **Performance:** similar sample count and cheap arithmetic for each; variant changes. **Issues:** these operate within the material and do not force transparent rendering. Premultiplied Alpha from the general BlendMode enum is not a supported detail choice: its code branch does not produce the required `detail` variable.

### `detail_uv_layer`

**Values/default:** `0 UV1` (default), `1 UV2`. **Behavior:** selects UV coordinates for detail albedo/normal; mask remains UV1. **Performance:** ordinary UV selection is small; selected triplanar means three reads per affected texture. **Issues:** selecting UV2 also controls whether the generator applies nontriplanar UV2 scale/offset at all, even for emission/AO (R2). Height mapping has a separate UV2-detail offset issue (R6).

### `detail_albedo`

**Values/default:** null (white, opaque) or Texture2D, default null. **Behavior:** RGB overlays base albedo; A weights both detail color blending and detail-normal blending. Not multiplied by `albedo_color`. **Performance:** one enabled read or three for its triplanar layer. **Issues:** a fully transparent detail texel also hides its detail-normal influence. Null is not a transparent/no-op detail texture.

### `detail_normal`

**Values/default:** null (flat-normal fallback) or Texture2D, default null. **Behavior:** tangent-space normal RGB encoding is blended with the base normal-map encoding using detail alpha and mask; reconstructed direction uses R/G. **Performance:** one enabled read or three with selected triplanar plus normal processing. **Issues:** this is an encoding-space mix, not a general physically exact combination of surface normals. Requires appropriate tangents/normal-map convention. UV2/triplanar normal basis limitations R3/R4 apply.

## UV1 and UV2

Implementation: [ordinary UV transforms](C:/Users/k/Repository/External/Godot_4-7-2/scene/resources/material.cpp:1220), [triplanar basis/coordinates](C:/Users/k/Repository/External/Godot_4-7-2/scene/resources/material.cpp:1355), [three-axis sampling](C:/Users/k/Repository/External/Godot_4-7-2/scene/resources/material.cpp:1466).

### `uv1_scale`

**Values/default:** any finite Vector3, default `(1,1,1)`. **Behavior:** ordinary UV uses XY; triplanar uses XYZ. 1 preserves scale, 0 collapses a coordinate, negative mirrors, increasing absolute magnitude increases repetitions. **Performance:** uniform multiply; does not change read count, but affects mip/cache behavior. **Issues:** ordinary Z is unused. UV repetition also depends on `texture_repeat`; extreme scale can alias if mipmaps/filtering are unsuitable.

### `uv1_offset`

**Values/default:** any finite Vector3, default `(0,0,0)`. **Behavior:** adds XY after scaling for ordinary UV, XYZ for triplanar; signed values slide texture coordinates. **Performance:** uniform addition; same read count. **Issues:** ordinary Z unused; wrap/clamp changes what happens beyond the texture boundary. None identified beyond the shared mapping limitations.

### `uv1_triplanar`

**Values/default:** false (default), true. **Behavior:** false uses mesh UV1; true blends three projections weighted by surface normal and generates a replacement tangent basis. **Performance:** three texture samples instead of one for each affected read, plus weights/varyings. **Issues:** blended projections can blur or reveal seams on directional patterns; normal maps are approximate. Disables height mapping and MSDF. R3/R4 cover shared-basis/world-space interactions.

### `uv1_triplanar_sharpness`

**Values/default:** real values clamped by the setter to 0–150; default 1. **Behavior:** weights are proportional to powered absolute normal components: 0 broadly equalizes axes; 1 uses ordinary absolute-component weighting; larger values favor the dominant axis. **Performance:** exponent/normalization work remains; high sharpness does not reduce the three texture reads. **Issues:** clamped because values outside the range can yield NaNs; even supported sharpness can create hard transitions on some meshes.

### `uv1_world_triplanar`

**Values/default:** false (default), true. **Behavior:** with UV1 triplanar, false anchors projections locally; true anchors them to world position and transformed normals. **Performance:** true adds world transforms, including basis handling; same read count. **Issues:** a moving object moves through a world-fixed pattern. Keep this false when triplanar is off; toggling it can still affect the shared basis when only UV2 triplanar is on (R3/R4).

### `uv2_scale`

**Values/default:** any finite Vector3, default `(1,1,1)`. **Behavior:** XY scale ordinary UV2; XYZ scale UV2 triplanar. Zero collapses, negative mirrors, absolute values above 1 repeat more. **Performance:** uniform coordinate math, with possible cache/mip changes. **Issues:** ordinary scale is only emitted when `detail_uv_layer = UV2`, even if AO/emission uses UV2 without detail (R2). Z is ignored outside triplanar.

### `uv2_offset`

**Values/default:** any finite Vector3, default `(0,0,0)`. **Behavior:** coordinate translation after scale; XY ordinary, XYZ triplanar. **Performance:** uniform addition. **Issues:** same nontriplanar gating defect R2. Height mapping plus UV2 detail has the additional coordinate subtraction R6.

### `uv2_triplanar`

**Values/default:** false (default), true. **Behavior:** false uses mesh UV2; true blends projections for features choosing UV2. **Performance:** triples each affected lookup and adds weights/varyings. **Issues:** enabling it also overwrites the shared tangent/binormal, potentially breaking the normal map that still uses UV1 (R3). This can happen even when UV2 is only for AO/emission.

### `uv2_triplanar_sharpness`

**Values/default:** clamped 0–150, default 1. **Behavior:** same weighting rule as UV1 sharpness, applied to UV2 projections. **Performance:** same three reads at all sharpness values. **Issues:** high values sharpen projection boundaries, not image resolution; world-normal weighting with nonuniform scale is a separate issue (R4).

### `uv2_world_triplanar`

**Values/default:** false (default), true. **Behavior:** false uses local positions/normals; true uses world position and a model-matrix normal expression for UV2 weights. **Performance:** extra transform work, same samples. **Issues:** local source uses `mat3(MODEL_MATRIX)` here instead of the normal-matrix handling used for UV1; nonuniform scale can bias weights. Shared tangent-basis selection also keys off UV1's world toggle (R4). World-fixed texture motion applies.

## Sampling

Implementation: [sampler hints](C:/Users/k/Repository/External/Godot_4-7-2/scene/resources/material.cpp:709).

### `texture_filter`

**Values/default:** `0 Nearest`, `1 Linear`, `2 Nearest Mipmap`, `3 Linear Mipmap` (default), `4 Nearest Mipmap Anisotropic`, `5 Linear Mipmap Anisotropic`.

**Behavior/performance:** 0 point samples without mip selection, preserving pixel edges but aliasing under minification. 1 bilinearly smooths without mipmaps and still aliases when minified. 2 retains nearest-style texels while using mip levels. 3 smooths with mipmaps and is a good ordinary default. 4/5 add anisotropic sampling to the corresponding mipmapped mode, improving oblique views with more texture filtering work according to the project anisotropy level and angle. Mipmaps consume storage but can substantially improve cache behavior, so “Nearest is always fastest overall” is not reliable.

**Issues:** mip modes need actual mip levels. Heightmaps force the linear counterpart of the selected mode. Screen/depth textures used internally by refraction/fade have their own fixed sampler hints. This is one shared sampler choice for the ordinary material maps, not independent UV1/UV2 samplers. Null fallback textures offer no meaningful visual filtering difference.

### `texture_repeat`

**Values/default:** true (default), false. **Behavior:** true wraps coordinates outside 0–1; false clamps to the edge. **Performance:** sampler/variant state, normally a small direct cost difference; access locality can change. **Issues:** repeating can expose nonseamless texture borders; clamping can stretch edge texels. It does not disable UV scaling or reduce generated texture sample count.

## Shadows

Implementation: [shadow flags](C:/Users/k/Repository/External/Godot_4-7-2/scene/resources/material.cpp:860), [shadow-to-opacity math](C:/Users/k/Repository/External/Godot_4-7-2/servers/rendering/renderer_rd/shaders/scene_forward_lights_inc.glsl:295).

### `disable_receive_shadows`

**Values/default:** false (default), true (your value). **Behavior:** false receives eligible light shadows; true compiles out shadow reception. **Performance:** true can remove shadow-coordinate/filtering/sample work. It does not stop this mesh being rendered into other objects' shadow maps. **Issues:** receiving and casting are separate; use GeometryInstance3D's casting setting or light shadow settings for casting cost. Your saved DirectionalLight3D does not enable shadows, so this is mostly a declaration of intent for the current scene.

### `shadow_to_opacity`

**Values/default:** false (default), true. **Behavior:** false shades normally; true modifies alpha using lighting attenuation to make shadowed parts opaque and lit parts transparent, for shadow-catcher-like effects. **Performance:** true enables alpha usage/transparent-path costs and retains relevant lighting/shadow work. **Issues:** not a general “shadow transparency” slider. No useful cast shadows, disabled reception, and multiple-light attenuation can make results surprising. The shader bypasses its ordinary alpha-scissor/hash block when this mode is active.

## Billboards and particle animation

Implementation: [billboard/atlas vertex code](C:/Users/k/Repository/External/Godot_4-7-2/scene/resources/material.cpp:1238).

### `billboard_mode`

**Values/default:** `0 Disabled` (default), `1 Enabled`, `2 Y-Billboard`, `3 Particle Billboard`. **Behavior:** 0 uses the mesh transform; 1 faces the main camera; 2 turns around a fixed world Y axis; 3 faces the rendering camera and also reads particle custom rotation/animation data. **Performance:** enabled modes add per-vertex matrix/basis math; particle mode adds atlas arithmetic. They do not reduce draw calls by themselves. **Issues:** ordinary modes 1/2 deliberately use the main camera even during shadow passes; mode 3 uses the current inverse view matrix. Y-billboarding has degenerate cross-product directions near vertical views. Stereo views and multi-camera shadow use require care; these are camera-facing approximations, not arbitrary 3D orientations.

### `billboard_keep_scale`

**Values/default:** false (default), true. **Behavior:** false replaces orientation without restoring original scale; true restores axis lengths from the model matrix after constructing the billboard. **Performance:** true adds axis-length and matrix math per vertex. **Issues:** only useful with an enabled billboard; axis lengths do not preserve every aspect of shear/negative scale. Without it, the sphere's large scale would not be preserved under billboarding.

### `particles_anim_h_frames`

**Values/default:** integer 1–128, default 1. **Behavior:** columns in the sprite atlas when Particle Billboard is active; 1 means one column, larger values subdivide U. **Performance:** uniform; count changes the UV calculation, not the number of texture samples or particles. **Issues:** zero is not rejected by the setter and causes invalid arithmetic; stay positive. Atlas padding/mips are needed to avoid frame bleeding.

### `particles_anim_v_frames`

**Values/default:** integer 1–128, default 1. **Behavior:** rows in the atlas; total frames are horizontal×vertical. **Performance:** uniform; image size and particle count matter more than this arithmetic. **Issues:** same positive-count and atlas-bleeding requirements. Inactive outside Particle Billboard.

### `particles_anim_loop`

**Values/default:** false (default), true. **Behavior:** false clamps the frame derived from `INSTANCE_CUSTOM.z`; true wraps it modulo total frames. **Performance:** selects clamp versus modulo in a uniform-controlled shader branch. **Issues:** this does not create animation time or loop the particle emitter; particle/custom data must advance the frame value. Positive frame counts required.

## Grow and transform

Implementation: [grow/fixed-size/projection code](C:/Users/k/Repository/External/Godot_4-7-2/scene/resources/material.cpp:1330), [Direct3D 12 point support](C:/Users/k/Repository/External/Godot_4-7-2/drivers/d3d12/rendering_device_driver_d3d12.cpp:5891), [point emulation](C:/Users/k/Repository/External/Godot_4-7-2/servers/rendering/renderer_rd/shaders/forward_clustered/scene_forward_clustered.glsl:707).

### `grow`

**Values/default:** false (default), true. **Behavior:** true moves each vertex along its normal by `grow_amount`; false omits displacement. **Performance:** a small per-vertex multiply/add; enlarged coverage can be much more important. **Issues:** does not add topology or alter collision. Hard/split normals can open cracks. CPU culling bounds are not automatically rebuilt from arbitrary shader growth; adjust mesh bounds/extra cull margin if needed.

### `grow_amount`

**Values/default:** -16–16, default 0, expressed as a distance in the material's local vertex space. **Behavior:** 0 unchanged, positive expands, negative contracts; object scale changes world thickness. **Performance:** uniform; large positive growth can increase raster coverage/overdraw. **Issues:** extreme contraction can invert/intersect geometry. With grow off it is inactive; with grow on and amount=0 the feature still exists.

### `fixed_size`

**Values/default:** false (default), true. **Behavior:** false normal projected size; true adjusts model-view scale to compensate perspective distance, with a separate orthographic path. It is useful for icons, not literal pixel-size locking. **Performance:** true adds per-vertex scale math; distant objects can retain large screen coverage. **Issues:** shader size and CPU culling bounds can disagree. Camera projection/FOV matters; source's orthographic handling is an approximation, not a normal world-scale mesh.

### `use_point_size`

**Values/default:** false (default), true. **Behavior:** true writes point size and samples base albedo with point coordinates; false follows ordinary geometry/UV handling. **Performance:** Direct3D 12 reports no native programmable point-size support here, so Forward+ emulates the points with triangles and expanded vertex handling. Larger points shade more pixels. **Issues:** intended for point rendering, not a harmless control on triangle meshes; Forward+ changes primitive handling when this is used. Do not enable on this sphere to optimize it.

### `point_size`

**Values/default:** 0.1–128 pixels, default 1. **Behavior:** sets point raster size when `use_point_size` is active. **Performance:** uniform, but covered area grows approximately with the square of size before clipping/overlap. **Issues:** does not resize normal triangles; hardware/emulation and subpixel coverage limit meaningful tiny sizes.

### `use_particle_trails`

**Values/default:** false (default), true. **Behavior:** true enables the specialized particle-trail path; false ordinary geometry. **Performance:** true enables extra trail/skinning processing appropriate to RibbonTrailMesh/TubeTrailMesh and particle data. **Issues:** enabling it on an ordinary mesh can break rendering. It does not create a particle system or trail geometry automatically.

### `use_z_clip_scale`

**Values/default:** false (default), true. **Behavior:** false normal projection depth; true applies `z_clip_scale` to pull rendered geometry toward the camera for first-person items. **Performance:** small added vertex/projection work. **Issues:** screen-space effects can see depth inconsistent with apparent placement; this does not move collision or the scene object's actual transform.

### `z_clip_scale`

**Values/default:** 0.01–1, default 1. **Behavior:** 1 leaves normal placement; progressively smaller positive values reduce camera-relative clipping depth. **Performance:** uniform; cost roughly constant, with changed visibility/overlap. **Issues:** low values can disrupt SSAO/SSR and other depth-based effects. It is not a global near-plane setting. The setter does not clamp arbitrary scripted input.

### `use_fov_override`

**Values/default:** false (default), true. **Behavior:** false uses the camera projection; true substitutes this material's vertical FOV during non-shadow rendering. **Performance:** true adds per-vertex projection setup/trigonometry. **Issues:** generated stencil next passes do not inherit it (R8). It can disagree with camera culling and depth-based effects. Shadows deliberately retain their own projection.

### `fov_override`

**Values/default:** 1–179 degrees, default 75. **Behavior:** lower angles magnify/narrow the view; higher angles widen/shrink the object. Uses a vertical/Keep Height interpretation. **Performance:** uniform; screen coverage is the significant value-dependent cost. **Issues:** not appropriate for this scene's orthographic camera; extreme/out-of-range values can make projection singular or unusable. R8 for outlines.

## Proximity and distance fades; MSDF parameters

Implementation: [fade shader](C:/Users/k/Repository/External/Godot_4-7-2/scene/resources/material.cpp:1817), [MSDF shader](C:/Users/k/Repository/External/Godot_4-7-2/scene/resources/material.cpp:1612).

### `proximity_fade_enabled`

**Values/default:** false (default), true. **Behavior:** true reduces alpha as the fragment approaches the surface represented in the scene depth texture; false omits it. **Performance:** true adds depth sampling/reconstruction and alpha/transparent-path costs. **Issues:** it detects screen-depth intersections, not general 3D distances to arbitrary objects; ordinary transparent objects are not reliably represented. No offscreen/collision proximity detection.

### `proximity_fade_distance`

**Values/default:** Inspector 0.01–4096 distance units, default 1; setter clamps to at least 0.01 with no corresponding maximum clamp. **Behavior:** smaller positive values make a tighter intersection fade; larger values spread it farther. **Performance:** uniform; wider fading does not remove the underlying draw. **Issues:** zero/negative scripted inputs become 0.01. Excessive distance can make most of an object translucent.

### `msdf_pixel_range`

**Values/default:** 1–100, default 4. **Behavior:** declares the distance range encoded around the MSDF shape, for conversion to screen-pixel coverage. **Performance:** uniform; same median/derivative work. **Issues:** must match the texture's generation settings; it is not a free sharpness/quality slider. Setter does not validate zero; invalid values can divide by zero. Inactive without effective MSDF mode.

### `msdf_outline_size`

**Values/default:** 0–250, default 0. **Behavior:** 0 uses ordinary distance-field coverage; positive values use the outline calculation, with effective width clamped to at most `pixel_range/2 - 1`. **Performance:** uniform-controlled branch with a little extra math; no extra texture fetch. **Issues:** increasing beyond the encoded distance range cannot create more usable outline. Very small pixel ranges make the positive-outline clamp interval problematic; use suitably generated distance data. No effect if MSDF is suppressed by triplanar.

### `distance_fade_mode`

**Values/default:** `0 Disabled` (default), `1 PixelAlpha`, `2 PixelDither`, `3 ObjectDither`. **Behavior/performance:** 0 omits fading. 1 computes per-fragment view-space distance and multiplies alpha, forcing transparency. 2 compares per-fragment distance-based fade against screen noise and discards pixels, avoiding ordinary alpha blending but adding noise/discard work. 3 uses object-origin distance for a uniform object fade amount, still applying per-pixel dither/discard. Dither modes can remain eligible for shadows, but still submit the mesh even when visually faded.

**Issues:** not CPU visibility culling or LOD. Dither can shimmer; alpha has sorting limitations. Shadow-pass distances can differ from main-view distances. ObjectDither measures a four-component vector in this version, biasing short distances (R7). Neither dither mode implies fewer triangles are submitted.

### `distance_fade_min_distance`

**Values/default:** 0–4096, default 0. **Behavior:** with min<max, below min is invisible and visibility rises toward max. With min>max, the documented intention reverses to a far-distance fade-out. **Performance:** uniform; no removal of draw submission. **Issues:** shader directly uses `smoothstep(min,max,distance)`. Equal endpoints are singular, and reversed endpoints are outside the portable GLSL definition despite the documented intended use (R10). Do not equate this with GeometryInstance3D visibility ranges.

### `distance_fade_max_distance`

**Values/default:** 0–4096, default 10. **Behavior:** with min<max, at/above max is fully visible; swapping endpoints requests fade-out. **Performance:** uniform; changing the span changes visible coverage, not submitted geometry. **Issues:** use distinct endpoints; R10 covers undefined/implementation-dependent smoothstep edge cases. The property names do not mean “near clip” and “far clip”.

## Stencil

Implementation: [generated effect pass](C:/Users/k/Repository/External/Godot_4-7-2/scene/resources/material.cpp:3130), [flag normalization](C:/Users/k/Repository/External/Godot_4-7-2/scene/resources/material.cpp:3222), [Forward+ stencil state](C:/Users/k/Repository/External/Godot_4-7-2/servers/rendering/renderer_rd/forward_clustered/scene_shader_forward_clustered.cpp:365).

### `stencil_mode`

**Values/default:** `0 Disabled` (default), `1 Outline`, `2 X-Ray`, `3 Custom`. **Behavior/performance:** 0 omits stencil state and removes an engine-owned effect pass. 1 writes a reference for the base material and adds an unshaded, alpha, grown next pass reading “not equal”; costs another geometry/color draw. 2 similarly adds an unshaded alpha next pass, with no growth and depth testing disabled, to expose occluded portions; also an extra draw/overdraw. 3 enables manual flags/compare/reference without automatically adding a pass.

**Issues:** engine-owned next passes do not inherit arbitrary base transforms/alpha masks, and can be regenerated (R8/R9). Effects sharing the same reference can interact/merge. Outline/X-Ray selection is rejected at render priority 127 (R13). This is geometry-based outlining; hard normal seams and CPU bounds still matter.

### `stencil_flags`

**Values/default:** bit flags, default 0. Valid stable states: `0 None`, `1 Read`, `2 Write`, `4 Write Depth Fail`, `6 Write + Write Depth Fail`. **Behavior:** Read tests the existing value; Write replaces it with the reference on passing fragments; Write Depth Fail replaces it when depth fails; 6 writes in either depth outcome. Values 3, 5 and 7 request incompatible read/write combinations and are normalized by the setter: when previously writing, read wins; when previously reading, the requested write bits win; from neither, read wins. **Performance:** hardware stencil test/write and related ordering/buffer work; no inherent second draw in Custom. **Issues:** this API does not support simultaneous read+write in one material. Read requires an alpha-queue material (for example alpha blending or disabled depth drawing/testing); an ordinary opaque reader emits an error. The actual result of setting a combination depends on previous flags. Modes 1/2 manage their own flags; prefer Custom for manual control. [Read restriction](C:/Users/k/Repository/External/Godot_4-7-2/servers/rendering/renderer_rd/forward_clustered/render_forward_clustered.cpp:4212).

### `stencil_compare`

**Values/default:** `0 Always` (default), `1 Less`, `2 Equal`, `3 Less Or Equal`, `4 Greater`, `5 Not Equal`, `6 Greater Or Equal`. **Behavior:** accepts all fragments or compares the reference against the stored stencil value with the named operator. **Performance:** hardware comparison; changed rejection/coverage matters more than operator complexity. **Issues:** relevant for Read; writing uses a zero compare mask in this implementation, so use the intended Always comparison for write-only operation. The internal renderer enum order differs from this material enum, but the generated named modes perform the translation.

### `stencil_reference`

**Values/default:** integer 0–255, default 1. **Behavior:** reference value to write/compare; 0 also matches the usual cleared stencil value. All 256 byte values are usable references, not only powers of two. **Performance:** literal in generated shader state, so changing it selects shader/pipeline state rather than merely a color uniform. **Issues:** Forward+ uses full-byte masks (255), not independent bit masks per material; references can overwrite each other. The property setter does not clamp arbitrary out-of-range integers.

### `stencil_color`

**Values/default:** RGBA Color, default opaque black. **Behavior:** sets the generated Outline/X-Ray pass albedo; alpha controls its transparent blending. **Performance:** uniform update to the extra pass; alpha=0 does not remove that pass. **Issues:** ineffective for Disabled/Custom without a generated effect. Lighting is unshaded, but the generated material has its own remaining settings.

### `stencil_outline_thickness`

**Values/default:** 0–1 with larger values allowed, default 0.01. **Behavior:** controls generated outline growth along normals; 0 no geometric expansion, positive values enlarge it. Object scaling affects world width. **Performance:** uniform; larger outlines can shade more pixels; zero retains the extra draw. **Issues:** not constant pixel thickness. Hard-normal cracks, culling bounds and FOV mismatch R8 apply. Intended for Outline; changing it forwards grow amount to an existing generated pass.

## Inherited Material, Resource and Object properties

These are not extra lighting models, but they are properties of a StandardMaterial3D resource and can affect sharing, draw count or behavior.

### `render_priority`

**Values/default:** integer -128–127, default 0; out-of-range setter calls are rejected. **Behavior:** controls material render ordering, especially transparent draws; larger values render later within the applicable ordering rules. **Performance:** can alter state locality and overdraw, not shader complexity. **Issues:** does not override depth tests or force opaque/transparent passes into arbitrary order; not a universal sorting fix. Outline/X-Ray needs room for a higher-priority next pass, and priority changes are not fully synchronized to that pass (R13). [Material setter](C:/Users/k/Repository/External/Godot_4-7-2/scene/resources/material.cpp:72).

### `next_pass`

**Values/default:** null (default) or another Material. **Behavior:** redraws the surface with additional material(s); chains are allowed and cycles are rejected. **Performance:** each pass adds draw/geometry/raster/fragment work; transparency can amplify it. **Issues:** does not guarantee an immediately adjacent draw regardless of render ordering. Stencil effects insert/remove an engine-owned pass in this chain; directly editing that generated pass is fragile (R9). Use a compatible 3D material. [Pass setter](C:/Users/k/Repository/External/Godot_4-7-2/scene/resources/material.cpp:45).

### `resource_local_to_scene`

**Values/default:** false (default), true. **Behavior:** false allows scene instances to share this material; true requests a scene-local duplicate when a PackedScene is instantiated. **Performance:** true adds resource/allocation overhead and can reduce material sharing, though equal feature combinations still share a shader. **Issues:** changing it after instances exist does not retroactively duplicate them. Shared material edits affect every user of the resource. [Resource properties](C:/Users/k/Repository/External/Godot_4-7-2/core/io/resource.cpp:774).

### `resource_name`

**Values/default:** any String, default empty. **Behavior:** human-readable resource label. **Performance:** negligible storage/editor overhead; no GPU shading effect for empty or nonempty names. **Issues:** not a file rename, resource identity or shader variable. None identified.

### `resource_path`

**Values/default:** String, default empty on a new unsaved resource; loaded resources acquire a path such as `res://Testyo/Testyo.tres`, or a subresource-qualified path. **Behavior:** participates in resource lookup/cache identity. **Performance:** affects loading/sharing rather than shader instructions. **Issues:** setting a path does not save or move the file; existing cache ownership can reject a conflicting path. Do not use it as a material optimization control.

### `resource_scene_unique_id`

**Values/default:** String, initially empty/unassigned; valid scene IDs use letters, digits and underscores. **Behavior:** identifies the embedded resource within scene serialization; invalid names are replaced with generated IDs and collisions can be resolved during saving. **Performance:** serialization/lookup overhead only. **Issues:** not the resource UID from the `.tres` header, and not a shading input. Usually engine-managed and hidden.

### `script`

**Values/default:** null (default) or a compatible Script resource; use C# for this repository. **Behavior:** attaches custom resource behavior and potentially extra exported properties. **Performance:** null has no custom script work; otherwise entirely dependent on that script, its allocations and callbacks. **Issues:** an arbitrary script can change every assumption about updates; this is not the generated spatial shader. StandardMaterial3D does not expose ShaderMaterial's `shader` property. [Object property list](C:/Users/k/Repository/External/Godot_4-7-2/core/object/object.cpp:517).

### `metadata/<name>`

**Values/default:** arbitrary named Variant metadata, absent by default; null removes an entry through the metadata API. **Behavior:** stores application/editor annotations, not standard lighting parameters. **Performance:** memory/serialization and any custom consumer's work; no automatic fragment cost. **Issues:** arbitrary exported script properties and metadata cannot have a finite built-in inventory. The engine uses `_stencil_owned` metadata internally; do not repurpose it. [Metadata properties](C:/Users/k/Repository/External/Godot_4-7-2/core/object/object.cpp:525).
