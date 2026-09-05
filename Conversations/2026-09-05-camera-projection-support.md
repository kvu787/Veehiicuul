# Camera projection support

## User

does this app have logic to handle both multiple projection types, such as perspective and orthographic? 

## Assistant

I’ll check how the app builds and selects its camera projections.

## Assistant final response

No. The app currently supports **only a fixed orthographic projection**.

- [Renderer.cpp](C:/Users/k/Repository/CPlusPlus/Simple_DirectX12_3D_Game/src/Renderer.cpp:1244) always creates an orthographic matrix. There is no perspective mode or projection switch.
- [SimplePaint.hlsl](C:/Users/k/Repository/CPlusPlus/Simple_DirectX12_3D_Game/shaders/SimplePaint.hlsl:4) also assumes an orthographic camera when calculating the paint appearance.

Adding perspective support would require a projection choice and adjusting the paint shader’s view-direction calculations.

## User

are there any additional simplifications and/or optimizations that can be done if we encode a hard unchanging assumption that only orthographic projection will ever be supported?

## Assistant

I’ll check which parts already rely on orthographic projection and where making that assumption permanent could simplify the code or reduce rendering work.

## Assistant

The shader already takes advantage of the biggest simplification: a constant viewing direction. I found a few smaller candidates, including simpler vertex transforms and moving some paint calculations from pixels to vertices. The latter would only help when `Shift` is nonzero; the shipped settings already skip that work.

## Assistant final response

Yes, although **the largest orthographic-specific optimization is already implemented**: the paint shader uses a constant viewing direction, eliminating per-pixel view-direction and view-basis calculations.

There are three additional candidates:

| Change | What it buys you | Practical value |
|---|---|---|
| **Make the vertex shader output `w = 1` explicitly** | Replace the general 4×4 position transform with three position calculations and a constant fourth component. Orthographic projection plus the current affine object transforms guarantees this. | A small, concrete reduction in vertex calculations and potentially matrix storage. |
| **Declare normal interpolation as `noperspective`** | Explicitly tells the rasterizer that normals need no perspective correction. With constant `w`, this is mathematically equivalent to the current interpolation. | Clearly encodes the assumption; any performance improvement depends on the GPU and compiler. |
| **Move paint rotation into the vertex shader** | Rotate the normal’s X/Y components per vertex, then normalize the interpolated result per pixel. A constant rotation preserves lengths and commutes with interpolation, so the pixel shader can use the resulting X component directly. | Removes the `rotatedX` calculation from shifted pixels, at the cost of extra vertex work. Needs measurement and image comparison. |

The first and third follow from the current [vertex and pixel shader calculations](C:/Users/k/Repository/CPlusPlus/Simple_DirectX12_3D_Game/shaders/SimplePaint.hlsl:45). Microsoft documents the interpolation modifier in its [HLSL reference](https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-struct). All shipped materials currently have `Shift = 0`, so they already bypass the rotation calculation.

A few other cleanups are possible, but depend on **additional assumptions**:

- **A permanently fixed camera and stationary sphere:** cache the sphere’s transform constants and refresh them on resize instead of rebuilding them every frame. This opportunity exists in [Renderer::Render](C:/Users/k/Repository/CPlusPlus/Simple_DirectX12_3D_Game/src/Renderer.cpp:1341) independently of projection type.
- **Unchanging materials:** separate material constants from object transforms so the same six materials are not copied into both objects’ buffers every frame. Also independent of projection type.

Orthographic projection does **not** let us remove normal normalization—the sphere’s interpolated normals still need it—or depth testing, because surfaces still overlap at different depths. It also does not make nonlinear paint calculations safe to move wholesale to vertices.

I would start with explicit `w = 1` and `noperspective` as modest simplifications. I would benchmark the paint-rotation change before adopting it. **I would not expect a large speedup from the orthographic-only commitment alone.** These are code-review findings; I haven’t implemented or benchmarked them.

