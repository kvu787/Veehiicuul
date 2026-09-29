# Collision detection analysis and performance measurements

Date: September 29, 2026. Production revision: `c59af9791fdbbad4028ee4fd4799ccec86c26d80`.

The current collision pipeline is inexpensive for the shipped tracks. The full
Godot manager took about **0.55 microseconds per call** at both spawn and contact.
No geometric mismatches were found in the tested identity-root configuration.
One conditional integration defect was reproduced: moving the track root leaves
collision geometry behind. The largest demonstrated optimization opportunity is
Ribeye's grid sizing: six-unit cells reduced its mixed-query kernel cost from
319 ns to 120 ns, with identical collision results.

Production collision code was not changed. The committed additions are a
repeatable verification/benchmark harness, this report, and raw measurements.

## Findings

### 1. Conditional correctness defect: transformed track roots

[CollisionManager.cs, lines 39–45](../../Source/GameDataAndLogic/CollisionManager.cs)
reads the car's global position and global yaw, while the detector stores the
untransformed Blender coordinates from the track JSON. Moving the track scene
root moves its render geometry and cars, but does not move the collision edges.

Native reproduction: place a car across the first Ribeye outline vertex, then
translate the track root by `(1000, 0, 1000)` without changing the car's local
pose. `IsCarColliding()` changes from `true` to `false`, although the car still
intersects the same rendered barrier. The harness prints:

```text
ROOT_TRANSLATION: identical track-relative contact before=True, after=False.
```

This does **not** affect Ribeye at its current identity root transform. It matters
when a track is translated, rotated, scaled, or parented under a transformed
node. `TrackObjects` currently checks the Model child's local identity transform,
not the track root's global transform. Either enforce an identity global track
transform, or consistently express queries and footprints in track coordinates.
Treat this as a conditional correctness fix, ahead of micro-optimizations if
transformed tracks are intended to work.

### 2. Ribeye's vehicle-width grid scale is measurably suboptimal

At [TrackCollisionDetector.cs, line 107](../../Source/GameDataAndLogic/CollisionDetection/TrackCollisionDetector.cs),
cell size is the shorter representative vehicle extent. This gives three-unit
cells for the actual cars. Ribeye then uses a sparse dictionary and a second
index for 181 long edges. Six-unit cells put all 800 edges in a small dense grid.

| Ribeye cell size | Grid     | Storage | Long edges | Mixed median | Relative to default |
| ---------------- | -------- | ------- | ---------- | ------------ | ------------------- |
| 0.75             | 380×337  | Sparse  | 719        | 893.0 ns     | 2.80× slower        |
| 3.00, current    | 101×88   | Sparse  | 181        | 318.9 ns     | Baseline            |
| 6.00             | 51×44    | Dense   | 0          | 120.3 ns     | 2.65× faster        |
| 12.00            | 26×22    | Dense   | 0          | 124.4 ns     | 2.56× faster        |

These experiments change only the representative bounds passed to index
construction. **Every query still uses the same vehicle bounds and poses.**
Changing actual car collision bounds is not the proposed optimization.

The six-unit result was consistent across three processes (119.3–120.9 ns) and
with tiered compilation disabled (134.5 ns versus 338.3 ns for the default grid).
Track001 does not benefit consistently: its three-, six-, and twelve-unit
results were 173.8, 182.1, and 199.7 ns. A track-dependent cell-size policy or
explicit independently tunable index scale is preferable to multiplying every
track's cell size. The experiment combines changed cell size, dense storage,
and removal of the long-edge tree; it does not isolate their individual effects.

The absolute saving is about 0.20 microseconds per mixed Ribeye kernel query.
There is no evidence that this is currently a frame-rate bottleneck.

### 3. Exact contacts can allocate managed memory

The ordinary mixed/contact workloads and measured native manager calls allocated
zero bytes. Exact arithmetic at
[TrackCollisionDetector.cs, lines 1171–1199](../../Source/GameDataAndLogic/CollisionDetection/TrackCollisionDetector.cs)
uses `BigInteger`, which is not universally allocation-free.

| Workload                                     | Average managed allocation per query |
| -------------------------------------------- | ------------------------------------ |
| Ribeye, corner placed at each track vertex   | 45.24 bytes                          |
| Track001, corner placed at each track vertex | 39.37 bytes                          |
| Synthetic diagonal exact contact             | 64.00 bytes                          |
| Ordinary mixed, clear, and contact corpora   | 0 bytes                              |

The synthetic case uses triangle `(10,10), (20,20), (20,10)` and an axis-aligned
rectangle from `(15,15)` to `(17,17)`. This is an ordinary-sized exact contact,
not an extreme-coordinate-only case. Simple horizontal collinearity and the
tested subnormal fixture allocated zero; exact fallback does not always allocate.

This is a qualified allocation finding, not a correctness failure. The current
verification correctly promises zero allocation only for ordinary queries.
Avoid broadening that promise. If persistent contacts or many vehicles become
important, retain this workload when evaluating an allocation-free exact
predicate. Replacing exact arithmetic with an epsilon would change semantics.

### 4. The long-edge tree overallocates node storage

[TrackCollisionDetector.cs, line 790](../../Source/GameDataAndLogic/CollisionDetection/TrackCollisionDetector.cs)
allocates `2 * EdgeCount - 1` nodes, enough for one edge per leaf, but leaves hold
up to eight edges. The unused portion remains referenced by the immutable index.

| Fixture                  | Long edges | Allocated nodes | Used nodes | Unused node payload |
| ------------------------ | ---------- | --------------- | ---------- | ------------------- |
| Ribeye                   | 181        | 361             | 63         | 9,536 bytes         |
| 16,384 long edges        | 16,384     | 32,767          | 4,095      | 917,504 bytes       |
| 256 tiled Ribeye copies  | 46,336     | 92,671          | 16,383     | 2,441,216 bytes     |

Payload is calculated from the current 32-byte node fields; array headers are
excluded. This is minor for the shipped track, but avoidable retained memory
at larger scales. Size from the actual leaf partition or compact after building
if memory becomes significant. Sparse-grid construction also allocates temporary
per-cell lists; construction allocations below include those temporaries.

### 5. Existing timing output is too short for stable performance conclusions

The existing comparative benchmark warms 4,096 queries and times only 16,384
queries, always measuring indexed queries first. That can mix tiered compilation
and startup effects into very short measurements. Its mixed Track001 indexed
result ranged from 683.6 to 1,095.4 ns in two default-runtime invocations during
this investigation. Those short runs are useful smoke checks, not a reliable
optimization baseline.

The added mode records 15 calibrated batches, alternates measurement order,
checks contact counts, records allocation separately, and retains every sample.
It also adds an independent exact predicate oracle: the existing linear oracle
shares the production rectangle transformation and segment predicate, so its
agreement alone cannot validate those components.

## Measured query costs

Hardware: Intel Core Ultra 9 275HX, 24 logical processors. Windows 11 x64,
build 26200; .NET SDK 10.0.401, runtime 10.0.12. `Stopwatch` frequency: 10 MHz.
The runtime's reported OS string is `Microsoft Windows 10.0.26200`, which is the
Windows 11 kernel version. No affinity, power-plan, or process-priority changes
were applied. Performance processes ran sequentially.

The table shows the median of three process medians with default tiered
compilation. Each process uses 15 timed batches per workload. Ranges are the
smallest and largest process medians. One ns is one billionth of a second.

| Workload                         | Indexed median | Process-median range | Linear median | Speedup |
| -------------------------------- | -------------- | -------------------- | ------------- | ------- |
| Ribeye mixed                     | 318.9 ns       | 316.7–319.4 ns       | 962.1 ns      | 3.02×   |
| Ribeye clear within track AABB   | 298.4 ns       | 293.4–312.7 ns       | 1,008.7 ns    | 3.38×   |
| Ribeye ordinary contact          | 248.6 ns       | 244.8–251.6 ns       | 529.0 ns      | 2.13×   |
| Ribeye spawn                     | 144.2 ns       | 140.3–172.3 ns       | 960.1 ns      | 6.66×   |
| Ribeye outside track AABB        | 44.6 ns        | 42.8–45.1 ns         | 23.3 ns       | 0.52×   |
| Ribeye exact vertex contact      | 298.7 ns       | 291.0–299.2 ns       | 681.3 ns      | 2.28×   |
| Track001 mixed                   | 173.8 ns       | 169.0–181.5 ns       | 2,159.6 ns    | 12.43×  |
| Track001 clear within track AABB | 120.2 ns       | 118.8–123.5 ns       | 2,457.1 ns    | 20.44×  |
| Track001 ordinary contact        | 341.1 ns       | 323.2–373.2 ns       | 1,349.7 ns    | 3.96×   |
| Track001 exact vertex contact    | 277.7 ns       | 275.5–281.3 ns       | 1,332.4 ns    | 4.80×   |

The mixed corpus has 4,096 poses per track, sampled uniformly inside each track's
bounding box with random yaw. Ribeye has 518 contacts; Track001 has 968.
The ordinary-contact corpus is filtered from five positions around every edge
midpoint, including closing edges: 2,407 Ribeye and 5,985 Track001 contacts.
These are controlled workloads, not a measured distribution of gameplay poses.

Outside-AABB rejection and tiny synthetic tracks are faster in the simple linear
method; both costs are only tens of nanoseconds there. Indexed lookup pays off
when it avoids scanning substantial geometry. Do not interpret the index as
uniformly faster for every possible input.

## Native Godot integration

Godot `4.7.2.stable.mono.official.ed1daf0bf`; actual imported meshes and production
manager; optimized Debug C# build; headless engine. All six cars have effectively
the same raw footprint, approximately `(-1.5, -3.1575215)` to `(1.5, 3.0)` in
collision coordinates. The manager shortens the minimum Y bound by 0.165.

| Native workload             | Median   | Batch p95 | Managed bytes/call |
| --------------------------- | -------- | --------- | ------------------ |
| Complete manager, spawn     | 551.2 ns | 599.4 ns  | 0                  |
| Detector alone, spawn       | 149.4 ns | 157.4 ns  | 0                  |
| Read scaled footprint       | 165.4 ns | 174.7 ns  | 0                  |
| Complete manager, contact   | 555.4 ns | 596.9 ns  | 0                  |
| Detector alone, contact     | 133.9 ns | 142.0 ns  | 0                  |
| Switch active car and query | 2.14 µs  | 3.04 µs   | 280                |

The native check uses 15 batches of 100,000 calls (1,000 for switching), after
400 ms of warmup. Timings include a delegate call. Switching additionally
includes reflection and boxing in the harness; its allocation/time is an upper
bound for the production refresh path, not a clean measurement of that path.
It exercises same-sized cars and does not rebuild the track index every switch.

Reading native transforms, deriving scaled bounds, and other manager work account
for most of the measured difference from the kernel. A complete stationary
collision query consumes roughly 0.013% of a 240 Hz frame budget on this machine.
This is not a whole-frame or rendering benchmark.

The native run logged a root-certificate-store read error during sandboxed engine
startup. It still loaded the scene, completed every collision check,
printed its PASS marker, and exited successfully. Headless rendering reports are
not evidence about actual DirectX frame presentation. The temporary adapter was
restored byte-for-byte and the normal Debug assembly rebuilt successfully.

## Construction, memory, and scaling

| Fixture                   | Edges   | Build median across processes | Allocated bytes/build | Clear query median   |
| ------------------------- | ------- | ----------------------------- | --------------------- | -------------------- |
| Ribeye, early process     | 800     | 0.596 ms                      | 111,440               | 298.4 ns mixed-clear |
| Ribeye, later warm build  | 800     | 0.108 ms                      | 111,440               | 143.6 ns spawn       |
| Track001                  | 1,984   | 0.118 ms                      | 141,104               | 120.2 ns             |
| 16 tiled Ribeye copies    | 12,800  | 1.161 ms                      | 1,884,384             | 176.1 ns spawn       |
| 256 tiled Ribeye copies   | 204,800 | 39.908 ms                     | 31,795,472            | 1,234.8 ns spawn     |
| 256 long-edge squares     | 1,024   | 0.097 ms                      | 115,280               | 104.2 ns             |
| 4,096 long-edge squares   | 16,384  | 2.414 ms                      | 1,835,696 typical     | 122.6 ns             |

Construction excludes JSON deserialization and mesh extraction. Five construction
warmups do not eliminate all tiered-JIT effects: the later one-copy tiled Ribeye
case has the same geometry and allocation count as the early Ribeye case but
builds much faster. With tiering disabled, those medians were 0.120 and 0.093 ms.
These are repeated-build timings, not first-load timings. Allocated bytes include
temporary objects and are not retained heap size or process working set.

The largest tiled fixture spans 25.95 million logical cells but stores only
92,507 occupied cells, demonstrating the sparse representation's value. Its
query costs remain small, but approximately 40 ms of construction would be
noticeable if moved into a frame loop. Current production construction occurs at
track initialization or sufficiently different car selection.

Broad query fallback is intentionally linear. A rectangle enclosing the entire
track without touching its perimeter costs 15.8 µs on Ribeye and 39.2 µs on
Track001. These are stress cases, not normal car sizes. The 16,384-long-edge
fixture's contact query is 136.3 ns versus 19.8 µs for a linear scan; its tree
successfully avoids almost all irrelevant edges.

## Correctness and design review

The pipeline transforms mesh bounds once per active-car change, applies current
positive planar scale, maps Godot X/Z to collision X/negative Z, and uses negative
Godot Y rotation as clockwise collision yaw. The front offset is applied to the
correct end of the local Y interval. The index snapshots input geometry into
private edge arrays, so later JSON mutation does not modify an existing index.

Each ordinary edge is stored once by its bounding-box center. Expanding the query
by maximum half-edge extents makes the center lookup conservative; outward
binary64 rounding is applied at expansion boundaries. Dense and sparse layouts
share the final segment test. Long edges use a small linear scan or a median-split
tree, and excessive grid-coordinate spans fall back to the tree rather than
overflowing integer cell coordinates. Large query cell ranges switch to a full
scan. Narrow-phase orientation uses a binary64 filter and exact dyadic-integer
fallback for ambiguous or subnormal inputs.

Validation completed:

- Existing collision verification: 12,160 indexed/linear comparisons, explicit
  contact/containment checks, and 100,000 ordinary queries allocating zero bytes.
- Added real-track comparisons: 8,096 Ribeye and 14,016 Track001 poses, covering
  every edge and every closing edge.
- Independent exact oracle: 8,615 comparisons, including samples from both real
  tracks; 7,000 randomized triangle cases at scales from approximately 2^-140 to
  2^120; exact contacts, adjacent representable coordinates, degenerate rounded
  corners, subnormals, and forced integer-grid fallback.
- Eleven invalid-input checks, including missing outlines, null elements,
  insufficient vertices, zero-length edges, nonfinite coordinates, invalid bounds,
  and transformed-coordinate overflow.
- All benchmark inputs also matched the linear result; timed batches checked
  expected contact totals.
- Native integration: all six cars spawn clear; 4,096 randomized manager/kernel
  pose comparisons passed. Track-root translation reproduced finding 1.
- Existing planar suite: 9,620 coordinate/quaternion comparisons, ten car-state
  checks, and camera-state checks passed.

The independent oracle does not use the production broad phase or segment
predicate. It deliberately uses the same rounded binary32 corner convention;
it is not proof of accuracy relative to an ideal unrounded rectangle or every
possible binary32 input. The native/kernel comparisons share the detector and
check integration consistency, not an independent full geometry implementation.

Intentional behavior and current assumptions:

- Collision means vehicle-perimeter versus track-outline contact. Complete
  containment in either direction is not a collision; existing tests require it.
- Only sampled poses are tested. Between-frame tunneling is explicitly accepted
  in `CollisionManager`; it is not reported as a defect here.
- Main checks the previous applied pose before integrating the next movement and
  resets on detected contact. There is no continuous collision detection or
  physical collision response in this subsystem.
- Footprints are cached render-mesh AABBs, including hidden descendants. Animated
  deformations, later mesh edits, pitch/roll, shear, and mirrored scale are not
  validated support targets. Current car state uses pure yaw and positive scale.
- Very large world coordinates can round distinct rectangle corners together.
  Exact segment arithmetic preserves the supplied rounded coordinates, not the
  precision lost while constructing them. Shipped tracks are nowhere near this
  limit.
- The detector has no shared mutation during queries, so independent simultaneous
  reads appear safe from source inspection; concurrency was not stress-tested.
  The Godot manager accesses scene nodes and should remain on the scene thread.

## Reproduction and measurement limits

Use [Build.cmd](../../Verification/CollisionDetection/Build.cmd), then
[Run.cmd](../../Verification/CollisionDetection/Run.cmd). The default performs
three sequential benchmark processes. See the
[verification readme](../../Verification/CollisionDetection/Readme.md) for the
single-process, tiering-disabled, and native options. The native option temporarily
replaces the existing adapter and restores it in `finally`; close Godot first.

Raw samples are preserved in [Measurements](Measurements). The three
`PerformanceDefault` files are the reported baseline; `PerformanceNoTiering.json`
is a sensitivity check. The added harness source contains deterministic seeds,
fixture definitions, calibration, and the independent oracle. Session transcripts
remain in the gitignored timestamped `MyLogOutput` folders.

No profiler, hardware performance counters, allocation stack trace, core pinning,
thermal control, or live gameplay trace was collected. Results include loop and
call overhead and are specific to this Windows machine/runtime. Reported p95
values are percentiles of batch averages (the maximum of 15 batches with this
estimator), **not** per-call tail latency. Benchmarks use at least 150 ms of
warmup and batches calibrated to roughly 15 ms or more; JIT promotion and desktop
scheduling can still affect some samples. The separate tiering-disabled run
supports the main grid-sizing conclusion but is not the default runtime result.

Suggested order: decide whether to enforce or support transformed track roots;
preserve the stronger validation and repeatable measurements; then consider
track-dependent grid sizing. Allocation-free exact arithmetic and tighter tree
storage are lower-priority improvements for the current one-car, 800-edge game.
