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


## User

Move paint rotation into the vertex shaderRotate the normal’s X/Y components per vertex, then normalize the interpolated result per pixel. A constant rotation preserves lengths and commutes with interpolation, so the pixel shader can use the resulting X component directly.Removes the rotatedX calculation from shifted pixels, at the cost of extra vertex work. Needs measurement and image comparison.

this seems like an obvious win because pixel shader executions typically outnumber vertex shader executions. or is my intuition/mental model wrong?

## Assistant

I’ll check the exact tradeoff in this shader, especially how the zero-shift path affects your intuition.

## Assistant final response

**Your intuition is broadly correct.** When an object covers many pixels per processed vertex, moving equivalent work into the vertex shader is a standard optimization. Microsoft explicitly recommends considering this in its [HLSL optimization guidance](https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-optimize).

For this particular change, I would expect it to be a good candidate **when shifted paint covers a substantial screen area**. My earlier wording was cautious about the measurable benefit, not the underlying reasoning.

The qualifications are:

- **Count pixels that actually execute the calculation.** Here, rotation runs only for pixels that pass the facing cutoff and take the nonzero-shift path. All shipped materials have `Shift = 0`, so they currently skip it. An unconditional move would add vertex work without removing any executed pixel work in that configuration.
- **The work is slightly different.** The current pixel shader calculates only rotated X. The proposed vertex shader must rotate both X and Y to preserve the normal’s length before pixel normalization. It also needs material rotation constants that it currently does not read. These are small costs, but relevant when triangles cover few pixels.
- **Count this shader’s pixels, not the entire window.** The background uses a different shader. A small, densely tessellated sphere can have relatively little pixel work per vertex.
- **Less arithmetic does not necessarily shorten the frame.** If CPU submission, memory traffic, or another rendering operation determines frame time, removing a few pixel instructions might have no measurable effect.

There is also a favorable detail: **this change needs no additional interpolated values**. We can replace the existing three-component normal with the rotated normal, keeping normalization in the pixel shader.

So, with nonzero shift and sufficiently large objects, **I would expect this to reduce shader work**. Measurement establishes how much that helps this app. Image comparison checks floating-point differences from changing the order of operations; the rotation/interpolation equivalence itself is mathematically sound when each triangle uses one material.


## User

this is enough to support implementing that optimization.

do that, and implement any simplifications or optimizations that can be done given that this app now assumes a hard unchanging assumption that only orthographic projection will ever be supported.

provide a summary of each change you implement.

## Assistant

I’ll move paint rotation into the vertex shader, make the orthographic-only assumption explicit in the transforms and interpolation, and check the resulting images and performance. I’ll keep the changes in focused commits and summarize each one.

## Assistant

I’ll preserve the zero-shift shortcut, use compact transforms that always produce `w = 1`, and mark normal interpolation as non-perspective. I’ll also stop uploading unchanged paint settings every frame and cache the stationary sphere’s transforms. Those last two reduce repeated CPU work under the app’s existing fixed-scene setup.

## Assistant

The updated renderer builds successfully. Object transform data is now 96 bytes instead of 128, and routine per-frame uploads drop from two 768-byte blocks to one 96-byte update for the moving car. I’m now comparing fixed-pose renders with the original, including different paint angles, shift values, and window sizes.

## Assistant

The compact transforms pass 4,000 comparisons against DirectXMath, and the first image comparison is pixel-identical. I found a back-buffer selection error in the temporary comparison tool and am correcting it before continuing the wider image checks.

## Assistant

The 16 initial image comparisons are pixel-identical, including nonzero shifts and angles, portrait windows, and ultrawide layouts. DirectX validation also passed after correcting the comparison tool. GPU timings are close enough that I can’t claim a reliable frame-rate gain; the reduced per-frame data uploads are confirmed.

## Assistant

The cache checks passed: both frame slots hold the sphere’s transforms, ordinary rendering leaves them and the paint settings unchanged, and resizing refreshes the sphere correctly. The implementation is committed in two focused changes. I’m recording the verification results and the per-change summary now.

## Assistant final response

Implemented and committed these changes:

- **Vertex paint rotation:** rotates normals per vertex for nonzero shift; zero-shift materials still skip rotation.
- **Orthographic interpolation:** normals explicitly use `noperspective`.
- **Compact vertex transforms:** compute three position components and always output `w = 1`; transform data shrinks from 128 to 96 bytes.
- **Simpler CPU projection:** uses scale factors and a depth offset instead of a general projection-matrix multiplication.
- **Shared material constants:** upload paint settings once instead of copying them for each object every frame.
- **Cached sphere transforms:** update only at initialization and resize.

Routine per-frame constant uploads drop from **1,536 to 96 bytes**.

The release build, regression tests, cache checks, and DirectX validation passed. All **16 sampled image comparisons were pixel-identical**. GPU timings did not establish a reliable speedup in this small scene.

[Full verification report](C:/Users/k/Repository/CPlusPlus/Simple_DirectX12_3D_Game/Reports/Rendering/OrthographicOptimization.md)

