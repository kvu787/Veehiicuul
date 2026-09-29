# Ribeye collision optimization and validation

September 29, 2026. Baseline: `afcad73`. Implementation: `b5726dc` (detector)
and `cf412b5` (Godot integration). Windows 11 x64; Godot 4.7.2 .NET.

Ribeye drove the optimization decisions. The complete moving transform-write
and collision-query operation fell from **749.8 ns to 112.6 ns**, a **6.66×**
speedup across three native processes. Boundary-contact operations improved
**4.22×**. The standalone mixed Ribeye detector improved **9.29×**, and exact
vertex contacts now allocate **zero bytes**. All geometric and integration
checks passed, followed by a successful release export and DirectX 12 startup.

These are collision-operation improvements. They do not establish an FPS gain:
one moving operation saves about 0.64 microseconds on this machine. Track001 and
synthetic tracks were used for regression coverage, not performance tuning.

## What changed

[TrackCollisionDetector.ExpandedGrid.cs](../../Source/GameDataAndLogic/CollisionDetection/TrackCollisionDetector.ExpandedGrid.cs)
precomputes candidate edge lists for each cell. Each edge's bounds are expanded
by the largest vehicle corner radius, covering every vehicle heading. A query
looks up one cell using the vehicle origin. An empty cell returns immediately,
before computing trigonometry or rectangle corners. A populated cell uses the
existing exact segment tests on its candidate list. Ribeye no longer performs
sparse dictionary queries or a separate long-edge tree traversal.

The broad phase has outward rounding and a conservative binary32 rounding
margin. This margin changes candidate selection only; it adds no tolerance to
contact classification. Queries with shifted local origins use the rounded
rectangle's center. Larger query rectangles use an exact scan. Tracks exceeding
65,536 cells or 1,048,576 references retain the general center-grid/tree path.
No approximation replaces the exact fallback.

[TrackCollisionDetector.cs](../../Source/GameDataAndLogic/CollisionDetection/TrackCollisionDetector.cs)
uses `Math.SinCos` and rejects segments on the same side of an edge before
computing the remaining orientations. Ambiguous orientations first use an
exact Int128 determinant when coordinate exponent spans permit it. The bound
allows at most 61 magnitude bits per coordinate, so the signed determinant
fits comfortably in 128 bits. BigInteger remains available for the full finite
binary32 range. Ordinary Ribeye exact contacts avoid its allocations.

[CollisionManager.cs](../../Source/GameDataAndLogic/CollisionManager.cs)
receives the position and yaw already maintained by `CarStateManager`.
Per-query native position, Euler-angle, and basis reads are gone. Mesh
footprints and scaled bounds for all six cars are prepared once per track
session, and one detector covers their union. Switching cars selects a prepared
footprint without rebuilding geometry. An exact last-pose cache includes car
index, planar position, and yaw; track reload constructs a new manager.

[VehicleCollisionFootprint.cs](../../Source/GameDataAndLogic/CollisionDetection/VehicleCollisionFootprint.cs)
walks local mesh transforms directly, including hidden children and custom
AABBs. It avoids global-transform/inverse round trips and temporary traversal
collections. `Main.Process` performs the query at its existing point in the
frame, before movement integration. Collision resets and input behavior retain
their existing ordering.

## Native measurements

Both the baseline and optimized implementations ran in each isolated Godot
process using the actual imported Ribeye scene and car meshes. Historical
sources were taken directly from `afcad73`; renamed types exist only in the
measurement build. The primary operation performs identical Godot position
and rotation writes, then invokes the corresponding manager. It includes
transform application and any native reads caused by collision detection.

The mixed corpus has 4,096 deterministic positions in a 400-by-400 world region
and random yaw. The boundary corpus visits all 800 Ribeye outline vertices.
These are controlled workloads, not recorded gameplay. They supplement the
more concentrated track-bounds kernel corpus below.

Values are medians of three process medians. Ranges span those three medians.

| Native operation                     | Baseline median | Optimized median | Baseline range | Optimized range | Speedup |
| ------------------------------------ | --------------- | ---------------- | -------------- | --------------- | ------- |
| Moving transform writes plus query   | 749.8 ns        | 112.6 ns         | 747.4–841.7 ns | 110.2–113.2 ns  | 6.66×   |
| Boundary transform writes plus query | 763.0 ns        | 180.8 ns         | 761.4–807.9 ns | 177.2–197.9 ns  | 4.22×   |
| Repeated stationary spawn query      | 509.6 ns        | 1.5 ns           | 490.8–549.4 ns | 1.4–1.6 ns      | —       |
| Repeated stationary contact query    | 520.4 ns        | 1.3 ns           | 502.7–556.9 ns | 1.3–1.4 ns      | —       |

Optimized moving queries without transform writes took 29.6 ns
(29.4–29.8 ns). Transform writes alone took 76.0 ns (74.8–77.3 ns). Do not add or
subtract these separate microbenchmarks as if they were exact component costs.
Stationary results are the best case of an inlined cache-hit loop, not a claim
about moving vehicles. All measured optimized native operations allocated zero
managed bytes after warmup.

Each case warms for 400 ms and measures 15 batches of 100,000 operations. The
harness includes delegate invocation. Its p95 is a percentile of batch averages,
not individual-call tail latency. The optimized case precedes the baseline in
each process; no affinity, process-priority, or power-plan changes were applied.
Rendering, input processing, and the rest of the frame loop are excluded.

## Detector measurements

The before column is the captured pre-edit Ribeye run at `afcad73`. The after
column is the median of three final processes, with their complete range.
Each process uses 15 calibrated timing batches and at least 150 ms warmup.
Contact counts agree with the linear oracle throughout.

| Ribeye query                    | Before   | After    | After range     | Speedup |
| ------------------------------- | -------- | -------- | --------------- | ------- |
| Mixed inside track bounds       | 312.8 ns | 33.7 ns  | 32.6–35.4 ns    | 9.29×   |
| Clear inside track bounds       | 297.3 ns | 18.4 ns  | 16.9–21.9 ns    | 16.20×  |
| Contact near edge midpoints     | 252.5 ns | 97.5 ns  | 81.4–106.5 ns   | 2.59×   |
| Exact corner-to-vertex contact  | 290.7 ns | 65.7 ns  | 65.5–78.5 ns    | 4.43×   |
| Spawn                           | 142.6 ns | 11.0 ns  | 9.6–12.1 ns     | 12.99×  |
| Outside track bounds            | 43.6 ns  | 9.4 ns   | 9.0–9.7 ns      | 4.65×   |
| Huge rectangle containing track | 16.17 µs | 15.82 µs | 14.96–16.06 µs  | 1.02×   |

The mixed corpus contains 518 contacts in 4,096 poses. The clear subset has
3,578 poses. The contact corpus has 2,407 contacts sampled around every edge,
including closing edges. Exact vertex contact tests place a rectangle corner
at each of the 800 exported vertices. Their average allocation fell from
45.24 bytes per query to zero. The huge rectangle deliberately tests perimeter
semantics and the oversized-query fallback; it is not a car workload.

The earlier analysis's six-unit recommendation applied to the old center grid.
The expanded grid has a different tradeoff: finer cells reduce candidate work.
The following sweep holds the physical vehicle footprint and every query fixed.

| Expanded-grid cell size | Mixed median across three processes |
| ----------------------- | ----------------------------------- |
| 1.5, selected           | 34.1 ns                             |
| 3.0                     | 37.1 ns                             |
| 4.5                     | 41.3 ns                             |
| 6.0                     | 46.8 ns                             |
| 9.0                     | 53.0 ns                             |
| 12.0                    | 61.6 ns                             |

The separate sweep and main mixed measurements differ slightly with run order
and runtime warmup. These timings support the selected design; they are not a
proof that no other index or cell size could ever be faster.

## Construction and memory tradeoff

Actual Ribeye has 800 edges. Its new index has 207 by 179 cells, 8,274 occupied
cells, and 32,134 packed edge references. The retained array payload is about
450,560 bytes (440 KiB): 296,424 bytes for cell ranges, 128,536 for references,
and 25,600 for edges. This excludes object and array headers. Empty cells cost
a range entry but make rejection cheap.

Total measured allocation while constructing the standalone detector increased
from 111,440 to 612,080 bytes, including temporary arrays. The earlier sparse
index used less memory. Early-process construction medians were 0.633 ms before
and 0.547 ms after (0.500–0.599 ms). These timings exclude JSON parsing and mesh
extraction and retain tiered-JIT effects, so they do not establish faster scene
loading. Construction happens once per track session, rather than on car
selection. The bounded extra storage is the principal cost of faster queries.

## Validation completed

- Ribeye: 8,096 mixed and edge-neighborhood comparisons per run, plus
  2,358,720 grid-boundary comparisons at seven headings and one-float-step
  offsets on both axes. Every edge and closing edge is covered.
- 10,056 independent BigInteger rectangle/segment oracle comparisons in each
  Ribeye run, including 160 shifted-local-origin cases. The oracle follows the
  documented rounded binary32 corner convention using separate arithmetic.
- 32,019 independent orientation checks across arbitrary finite binary32 values,
  collinearity, and both sides of the Int128 exponent-span cutoff.
- Zero-allocation assertions for every timed Ribeye query, including exact
  vertex contacts, and every optimized native measurement.
- All six actual imported cars: 24,576 moving poses and 24,576 cached repeats
  per native run, with zero disagreements against the historical manager;
  another 800 native boundary poses also agree.
- 1,088 rotation-only and translation changes; switching between deliberately
  different-sized cars at the same pose; nested hidden meshes and custom AABBs.
- `Main.Process` collision reset, input-driven car switching, explicit reset,
  and track reload. Reload replaces the manager and its pose cache.
- Original collision suite, sparse and long-edge fallbacks, integer-span
  fallback, exact diagonal contact, subnormal inputs, invalid input, and larger
  Track001/tiled fixtures. The complete extended run made 11,877 independent
  geometric oracle comparisons. Non-Ribeye timings were not tuning inputs.
- Existing planar suite: 9,620 coordinate/quaternion comparisons, 10 car-state
  checks, and camera follow/pan/zoom/reset/frame-boundary/clamp checks.
- Production `Build.ps1`: optimized `ExportRelease`, warnings treated as errors,
  and Godot command-line Windows EXE export; zero warnings and errors.
- Exported EXE: 60-frame native startup with the optimized Ribeye index,
  DirectX 12, Forward+, VSync disabled, and maximum FPS zero. No runtime errors.
  Verification and historical baseline type names are absent from the exported
  application DLL.

The native headless benchmark processes logged a sandbox root-certificate-store
read error before completing all checks and exiting successfully. The production
release import, export, and native startup ran with the required filesystem
access and had no such error. Headless VSync reports do not describe the real
window; the exported DirectX run explicitly reported VSync disabled.

## Scope and reproduction

Vehicle mesh geometry and positive planar scale are immutable during a track
session. A new manager is required if they change. Ancestor transforms remain
subject to the repository's documented identity-transform assumption. Discrete
sampling, intentional tunneling, exact closed-segment contact, and clear results
for pure containment are preserved. The new manager uses the authoritative
managed yaw rather than a native Euler-angle round trip; comparison corpora
found no resulting collision disagreements. No universal bit-for-bit agreement
for every near-contact floating-point pose is claimed.

Hardware: Intel Core Ultra 9 275HX, 24 logical processors; NVIDIA GeForce RTX
5090 Laptop GPU. Windows 11 build 26200; .NET SDK 10.0.401/runtime 10.0.12;
Godot `4.7.2.stable.mono.official.ed1daf0bf`. Benchmarks ran sequentially with
default tiered compilation and the Godot editor closed. The native benchmark
uses optimized C# code in a headless Debug engine session; release export and
actual DirectX startup were validated separately.

See [the verification instructions](../../Verification/CollisionDetection/Readme.md)
for commands and test contracts. Saved evidence:

- [Pre-edit kernel samples](Measurements/KernelBefore.json).
- [Final kernel process 1](Measurements/KernelAfter1.json),
  [process 2](Measurements/KernelAfter2.json), and
  [process 3](Measurements/KernelAfter3.json).
- [Native process 1](Measurements/Native1.txt),
  [process 2](Measurements/Native2.txt), and
  [process 3](Measurements/Native3.txt).
- [Extended regression summary](Measurements/Regression.txt),
  [extended regression samples](Measurements/Regression.json), and
  [exported release startup](Measurements/ReleaseStartup.txt).

Native text files preserve the reported timing summaries and validation output;
the standalone JSON files retain every timed batch. Re-running on another
machine will change absolute costs. The historical baseline is test tooling
only and is excluded from normal application builds and exports.
