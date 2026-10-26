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
