# Orthographic renderer optimization verification

Date: 2026-09-05

Implementation commits: 34ce9a1 and b7e4578. Baseline: 08fcca7.

## Implemented changes

1. Paint rotation runs in the vertex shader for nonzero shift. Both normal X/Y
   components rotate before interpolation; the pixel shader normalizes the result
   and uses X directly. The zero-shift branch still skips rotation.
2. Normals use noperspective interpolation. The renderer permanently assumes
   orthographic projection and affine object/view transforms.
3. Packed object transforms store three clip-position columns and three
   normal-transform columns (96 bytes total, previously 128 bytes of matrices).
   The vertex shader calculates clip XYZ and writes W = 1 explicitly.
4. CPU projection uses three scale factors and a depth offset. It no longer
   constructs a general projection matrix or multiplies each object by one.
5. The 480 bytes of material constants are uploaded once and shared by all
   objects/frame slots. Object constants and material constants use separate
   bindings; object constants are visible only to the vertex shader.
6. The stationary sphere's constants are initialized in both frame slots and
   refreshed after resize while the GPU is idle. Only the moving car's 96-byte
   transform block is uploaded during ordinary frames, down from 1,536 bytes.
   Buffer addresses still meet the 256-byte alignment requirement. The constant
   buffer's requested resource size decreases from 3,072 to 1,536 bytes; this
   does not imply an equivalent reduction in physical allocation granularity.

Depth testing and per-pixel normal normalization remain necessary and enabled.
Object scaling retains the existing uniform-scale assumption. No perspective
option or compatibility path was added.

## Build and regression checks

- Release build passed, including both optimized Shader Model 6.0 paint shaders.
- CTest passed both UVSphereGeometry and OrthographicTransforms.
- OrthographicTransforms compares 4,000 packed transforms against DirectXMath,
  checks near/far depth endpoints, and checks that every car triangle uses one
  material. The existing sphere test checks its material assignment.
- A hidden-window DirectX probe exercised initialization, ordinary rendering,
  resize, fixed-pose drawing, GPU readback, and shutdown.
- A separate cache check verified both sphere frame slots, unchanged sphere and
  material data across ordinary renders, and sphere refresh after resize.
- DirectX debug validation reported no new warnings or errors. The probe permits
  the baseline's existing CREATERESOURCE_STATE_IGNORED buffer-creation notice.
- Final compiled shader binaries match the binaries used for GPU validation.
  DXIL inspection confirms clip W is the literal 1, normals use noperspective,
  constant blocks are 96 and 480 bytes, and the pixel rotation FMad is gone.
- Run.cmd remains the build-and-launch entry point.

## Image comparisons

The comparison probe compiled each renderer snapshot with its own generated
shader headers. It rendered the same car pose and sphere through the actual
background/object draw functions. Images were copied directly from the GPU
render target and compared as 8-bit RGB; no desktop screenshot timing was involved.

| Paint preset | Coverage | Comparisons | Result |
| --- | --- | ---: | --- |
| Shipped settings | Zero shift, original angles | 4 | Pixel-identical |
| Zero shift, changed angles | Different nonzero angles in all six materials | 4 | Pixel-identical |
| Shifted paint | Shifts 0.3–0.95, varied positive/negative angles | 4 | Pixel-identical |
| Edge values | Shifts around epsilon and near/at 1; wrapped angles | 4 | Pixel-identical |

Each preset was compared at 1280×720, 720×1280, 2560×720, and 3000×700.
These cover normal, portrait, 32:9, and wider-than-background layouts, with
two fixed poses. All 16 comparisons had zero changed pixels. The shifted scene
was also visually inspected, and a separate check confirmed that switching
from shipped to shifted settings changes the image.

These are sampled image results, not a guarantee of bitwise identity for every
possible input. Reordering floating-point operations can change rounding.

The temporary comparison tool initially selected a noncurrent swap-chain buffer
for one pose. This was corrected to use GetCurrentBackBufferIndex before the
reported validation runs. No product correction was needed for that probe issue.

## GPU timing

Hardware: NVIDIA GeForce RTX 5070 Ti Laptop GPU.
Resolution: 2560×1440. Shipped car/sphere geometry and fixed pose.
Debug layer disabled for timing.

Each measurement used GPU timestamps around 256 repetitions of depth clear plus
the two actual object draws. The background was outside the timed interval.
Four warmup batches preceded 15 recorded batches. Three independent process runs
rotated the order of baseline, rotation/interpolation-only, and final renderer.

| Renderer | Shipped settings: run medians (microseconds) | Shifted paint: run medians (microseconds) |
| --- | --- | --- |
| Baseline | 8.043, 8.058, 8.036 | 8.713, 9.165, 8.045 |
| Rotation/interpolation only | 8.029, 8.026, 8.038 | 8.055, 8.050, 8.054 |
| Final renderer | 8.035, 8.039, 8.037 | 8.052, 8.059, 8.052 |

Default-settings times are effectively unchanged. Two shifted baseline runs
were slower, but the third was as fast as the optimized variants. This is not
sufficient evidence for a reliable speedup claim. These measurements also do
not include CPU constant updates, presentation, or whole-frame time.

The concrete reductions are less pixel arithmetic, no general per-object
projection multiplication, and fewer bytes uploaded per frame. A larger scene
or one dominated by shifted-paint pixel work may benefit differently.

Temporary probe sources, snapshots, raw images, PNGs, and timing samples remain
in the ignored build/orthographic-validation directory for local inspection.

