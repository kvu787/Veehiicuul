# Minimal Godot Visual Setup Review

## User

on the godot side for "C:\Users\k\Repository\Veehiicuul\Veehiicuul_Godot_CSharp\Testyo\TestYo.tscn", i am trying to configure the most minimal visual setup possible. check to see if i missed anything.

## Assistant Commentary

I’ll check the scene, its scripts, and the project’s rendering settings for anything that adds unnecessary visual work.

## Assistant Commentary

The environment effects are already off. I found one setting outside the scene that matters: VSync is still enabled by the project default when you run `TestYo.tscn` directly. I’m also checking the imported materials, since they can retain lighting and two-sided rendering costs even with a minimal environment.

## Assistant Commentary

Two rendering defaults are still active: the screen-space roughness limiter and the depth prepass. Your current materials are already single-sided and opaque, but they still use per-pixel lighting and specular shading. The scene also references an empty compositor and default camera attributes; those can be cleared for simplicity, though they aren’t enabling extra effects.

## Assistant

**You’re close for a minimal lit scene.** I checked [TestYo.tscn](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Testyo/TestYo.tscn), its resources, the GLB, and project settings against your local Godot 4.7.2 source.

These are the remaining items:

1. **Screen-space roughness limiter is still enabled.** It defaults to `true`, independently of MSAA/TAA. For a minimal setup, disable `rendering/anti_aliasing/screen_space_roughness_limiter/enabled`. It reduces shimmering in specular highlights at a small rendering cost. [Documentation](https://docs.godotengine.org/en/stable/classes/class_projectsettings.html#class-projectsettings-property-rendering-anti-aliasing-screen-space-roughness-limiter-enabled)

2. **Depth prepass is still enabled.** `rendering/driver/depth_prepass/enable` defaults to `true`, so opaque geometry gets an additional depth pass. Try disabling it for this simple, elevated-view scene. This reduces passes, but **benchmark it**: the prepass can save work when surfaces overlap and shading is expensive. [Documentation](https://docs.godotengine.org/en/stable/classes/class_projectsettings.html#class-projectsettings-property-rendering-driver-depth-prepass-enable)

3. **The materials still use per-pixel lighting and specular shading.** None of the GLB’s 39 materials is unlit. For the absolute minimum, use **Shading → Shading Mode → Unshaded**, then remove the directional light; this changes the appearance to flat colors. If you want to keep lighting, consider **Shading → Specular Mode → Disabled**. Your GLB’s `KHR_materials_specular` settings—including zero specular—aren’t handled by this Godot importer. Disabling environment reflections does not disable the light’s specular highlights. [Material documentation](https://docs.godotengine.org/en/stable/classes/class_basematerial3d.html#enum-basematerial3d-shadingmode), [importer source](C:/Users/k/Repository/External/Godot_4-7-2/modules/gltf/gltf_document.cpp:2946)

4. **The camera attributes and compositor references are unnecessary.** In [WorldEnvironment](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Testyo/TestYo.tscn:18), you can clear **Camera Attributes** and **Compositor**. Their resources contain only defaults: depth of field and auto exposure are off, and the compositor has no effects. This simplifies the setup; don’t expect a meaningful FPS improvement.

5. **VSync is still enabled when running this scene directly.** [project.godot](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/project.godot:18) doesn’t override the enabled default. Set **Display → Window → Vsync → Vsync Mode → Disabled**, which stores `window/vsync/vsync_mode=0` under `[display]`. The two-image swapchain setting does not disable VSync. The FPS limiter is already unlimited by default. [Godot startup source](C:/Users/k/Repository/External/Godot_4-7-2/main/main.cpp:2839)

**Already configured appropriately:** shadows off; light baking disabled; ambient light and environment reflections disabled; no sky; linear tonemapping; SSAO, SSIL, SSR, SDFGI, glow, fog, and volumetric fog off. MSAA, TAA, screen-space AA, and debanding are also off for the standalone scene. The current GLB uses opaque, single-sided materials without textures.

One future trap: [GraphicsSettingsManager.cs](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Source/GameDataAndLogic/GraphicsSettingsManager.cs:9) defaults to **8× MSAA** when initialized. That does not affect running `TestYo.tscn` directly.

This was a static review; I haven’t changed the scene or settings or benchmarked the alternatives.

## User

Depth prepass is still enabled. rendering/driver/depth_prepass/enable defaults to true, so opaque geometry gets an additional depth pass. Try disabling it for this simple, elevated-view scene. This reduces passes, but benchmark it: the prepass can save work when surfaces overlap and shading is expensive. Documentation

explain this in greater detail

## Assistant Commentary

I’ll explain what the extra pass does, why it can help or hurt, and how that applies to your track scene.

## Assistant Commentary

Turning it off still keeps normal depth testing. The prepass establishes the nearest surfaces before material shading begins, which can avoid shading surfaces that a later draw would cover. Whether that saves time depends on overlap, material cost, and the cost of drawing the geometry again.

## Assistant

The tradeoff is **extra geometry work versus less wasted pixel shading**. A depth prepass adds work upfront so that the GPU can identify hidden surfaces before doing their more expensive material and lighting calculations.

First, a **depth buffer** stores how close the nearest surface found so far is at each screen pixel. The GPU uses it to decide whether a new surface belongs in front of or behind what it has already drawn. Both modes use this buffer: **disabling the prepass keeps ordinary depth testing and correct front-to-back visibility for your opaque track.**

**Without a depth prepass**, the GPU builds the depth buffer while drawing the colored scene:

1. Transform a mesh’s vertices into screen space and determine which pixels its triangles cover.
2. Compare those surfaces against the depths already recorded.
3. For surfaces that pass, calculate their material color and lighting, and update the color and depth buffers.

For ordinary opaque materials, the GPU can often reject hidden surfaces before running their pixel shader. The limitation is that it only knows about surfaces processed **so far**. If a nearer object is drawn later, some earlier shading work may be overwritten.

**With a depth prepass**, rendering is split into two stages:

1. **Depth pass:** Draw the eligible opaque geometry using a simpler rendering path that establishes depth without calculating the final lit colors.
2. **Color pass:** Draw that geometry again, using the completed depth buffer to reject hidden surfaces before expensive shading.

In your Godot source, the ordinary opaque color pass switches to an **equal-depth test** when the prepass is enabled and stops writing depth again. It shades surfaces matching the depths established by the prepass. [Godot implementation](C:/Users/k/Repository/External/Godot_4-7-2/servers/rendering/renderer_rd/forward_clustered/scene_shader_forward_clustered.cpp:451)

Consider **one screen pixel** where three opaque surfaces overlap: ground, a raised road, and a car roof. Only the roof should appear. In a simplified example:

| Rendering approach                          | Full material shading at that pixel                         | Additional depth-only pass |
| ------------------------------------------- | ----------------------------------------------------------- | -------------------------- |
| Prepass off; ground → road → roof draw order | Ground, then road, then roof; earlier colors get overwritten | No                         |
| Prepass off; roof → road → ground draw order | Roof; the farther surfaces can fail the early depth test     | No                         |
| Prepass on                                  | Roof; the prepass has already established the nearest depth  | Yes                        |

The first row illustrates **overdraw**: multiple surfaces cover the same screen area, potentially causing repeated shading. The second shows why having overlap does not automatically make a prepass worthwhile—ordinary depth testing can already eliminate much of the wasted work.

The prepass itself still costs time:

- Additional geometry processing: fetching vertices, transforming them, and rasterizing triangles.
- Additional draw commands and rendering state work.
- Depth-buffer traffic and synchronization between rendering stages.

It is cheaper than another full color pass, but it is not free. It pays off when **the hidden shading work avoided costs more than the extra depth pass**. Expensive materials and substantial overlap favor it; cheap materials and little overlap favor disabling it. Godot’s documentation specifically suggests testing it disabled for views such as top-down games with limited overdraw. [Documentation](https://docs.godotengine.org/en/stable/classes/class_projectsettings.html#class-projectsettings-property-rendering-driver-depth-prepass-enable)

**For your track**, the materials have no textures, there is one directional light without shadows, and the camera looks down from above. Those make disabling the prepass worth testing. Your asset also contains approximately **100,000 triangles**, so processing geometry for an extra pass has a cost. However, the road, terrain, barriers, and vehicles can still overlap from the camera’s viewpoint. I have not measured that overlap or GPU time, so this is a test candidate rather than a demonstrated improvement.

If you simplify the materials further to **Unshaded**, the work the prepass might save becomes cheaper still, which generally strengthens the case for trying it off.

To compare, change this entry under `[rendering]` in [project.godot](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/project.godot:32):

```ini
driver/depth_prepass/enable=false
```

Run `TestYo.tscn` with the same camera, resolution, and other settings in both cases. Disable VSync and any FPS cap, let shader compilation settle, and compare **total GPU frame time** over repeated runs. The prepass disappearing from the profiler alone proves nothing—the color pass may become more expensive. If CPU work limits FPS, GPU time can improve without an obvious FPS increase.

For your current opaque scene, the intended visible result is the same. The choice is about how much work the GPU does to produce it.
