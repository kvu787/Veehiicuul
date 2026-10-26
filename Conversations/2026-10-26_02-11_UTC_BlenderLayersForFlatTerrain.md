# Blender layers for flat terrain

## User

does blender have something like layers? the reason i'm asking is that i'm making terrain that is fully 2d and flat on the xy plane, and i want to be able to put different things on different layers so that they render the way i want them to and don't z-fight. for example, i might have a big green square representing the grass on layer 1, and and then a gray path on layer 2, then some debris on the path on layer 3. i could achieve this by using the knife tool to cut the path out of the grass and cut the debris out of the path, but that is cumbersome.

## Assistant

Not in the way you mean. Blender 2.7x had 20 "layers", but since 2.8 they were replaced by **Collections**, which are purely organizational (grouping, hiding, render toggles). They don't control draw order. Mesh faces are always depth-tested, so two faces at the same Z will z-fight no matter which collection they're in.

The one thing in Blender with real ordered 2D layers is **Grease Pencil**, but it's strokes and fills, not meshes, so it's a poor fit if these meshes are going to Godot or your DX12 app.

### What I'd do: a tiny Z offset per layer

It's the standard trick, and it's still non-destructive:

1. Make one collection per layer (`Layer1_Grass`, `Layer2_Path`, `Layer3_Debris`) to keep things organized.
2. Give each object its layer height using **Object Properties > Transform > Delta Transform > Delta Location Z**: `0.00` for grass, `0.01` for path, `0.02` for debris, and so on.
   - Delta transforms stack on top of the normal location, so you can still move objects around on XY without touching the layer offset.
   - Alternative: add one Empty per layer at that Z and parent the layer's objects to it. Then you can shift a whole layer by moving one Empty.
3. Look at it from a top-down **orthographic** view (Numpad 7). From above, a 1 cm offset is invisible, and the path draws over the grass with no cutting.

### Depth precision (why the offset size matters)

- **Orthographic** views and cameras spread depth precision evenly, so even 0.001 is fine.
- **Perspective** views put most of their precision near the camera. With Blender's default viewport Clip Start of 0.01 m, a 1 mm offset can still flicker when you're zoomed out. If you see that, raise **Clip Start** (N panel > View, or the camera's Clip Start) to around 0.1 or more, or use a bigger offset. 0.01 per layer is a safe default for terrain at normal scale.

### If you actually need cut-out geometry

Say you don't want overlapping faces in the exported mesh. A **Boolean modifier** (Difference, with the path as the cutter) does what the knife does, but non-destructively. Booleans are unreliable with zero-thickness planes, so add a **Solidify** modifier to the cutter object. For pure rendering, though, the Z offset is simpler and more robust.

## User

The usual fix: a tiny Z offset per layer
i tried this a while back, but it didn't work out because i ended up having enough layers that the z separation became non-trivial enough to produce visuals that i didn't want.

is there a way to add this to blender? are there other programs that have this functionality?

## Assistant

Checking whether a Blender renderer trick I have in mind actually works before I suggest it.

The first pass was inconclusive (color management muddied the pixels, and creation order may be deciding). Tightening the test.

Result so far: with the default Blended settings, creation order wins and object origin is ignored, so the trick doesn't work as is. One more variant: turning on the material's Transparency Overlap option.

The experiment answered it: EEVEE doesn't let you control the order. Writing up the answer now.

## Assistant

### What I tested in Blender

My hope was that EEVEE's **Blended** materials would let you keep all geometry at Z = 0 and control order by raising each object's origin, since Blended objects are sorted per object. I tested this in Blender 4.5 with two coplanar planes, an orthographic top-down camera and Blended materials, with and without Transparency Overlap. The origin had no effect. The overlap is resolved by internal object order (in my test the last-created object always ended up on top), which isn't something you can control. So that trick doesn't work.

### Can it be added to Blender?

Not into the renderers themselves. Add-ons can't change how EEVEE, Cycles or Workbench depth-test or order objects, and materials have no depth-bias setting. What you can do:

| Approach                       | Live in viewport | Final render | Exports as meshes | Notes                                                                                                                                                       |
| ------------------------------ | ---------------- | ------------ | ----------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Grease Pencil, 2D depth order  | Yes              | Yes          | Poorly            | Object Data > Stroke Depth Order = 2D Layers. A real ordered layer stack, but strokes/fills, not meshes.                                                    |
| View layers + compositor       | No               | Yes          | Yes               | Render each layer in its own view layer, then stack them with Alpha Over nodes.                                                                             |
| Custom add-on viewport overlay | Yes              | No           | Yes               | Uses the `gpu` module to draw your layer meshes in order with depth testing off. Preview only, flat colors or simple textures. A moderate amount of Python. |

### Do it in the engine instead

These meshes are headed for Godot or your DX12 app, and whatever order Blender shows won't carry over anyway, so the engine has to handle it. The fix for your earlier problem is to offset **depth only**, in clip space, instead of moving geometry in world Z. The pixels stay exactly where they are, so you can have as many layers as you like with no visible separation.

Godot spatial shader (Godot 4 uses reversed Z, so larger Z is closer to the camera):

```glsl
shader_type spatial;
uniform int layer = 0;

void vertex() {
    POSITION = PROJECTION_MATRIX * MODELVIEW_MATRIX * vec4(VERTEX, 1.0);
    POSITION.z += float(layer) * 0.0001 * POSITION.w;
}
```

- **DX12:** draw the terrain layers in order with depth writes off, or set `DepthBias` per layer in the rasterizer state.
- **Godot 2D:** if the terrain is purely top-down, `Polygon2D` / `MeshInstance2D` with `z_index` gives you layers directly, with no depth involved at all.

### Other programs with real layers

- **Vector editors** (Inkscape, Affinity Designer, Illustrator): ordered layers. Export SVG, which Blender and Godot can both import.
- **Level editors** (Tiled, LDtk): ordered layers built for game maps, with Godot importers.
- **Godot's 2D editor:** node tree order plus `z_index`.
