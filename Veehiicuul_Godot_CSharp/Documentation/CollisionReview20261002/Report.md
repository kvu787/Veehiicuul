# Collision detection review

The current Ribeye collision detector passes the correctness checks for the project's existing cars and is inexpensive during steady play. I found two reproducible footprint problems with reflected roots and unusual node hierarchies. Neither occurs in the six current cars. I also measured a large performance change when a smaller cell size forces the detector out of its expanded-grid path. Keep the current approximate predicates and default cell size for the stated goal of performant collision that is visually good enough.

Reviewed on October 2, 2026, against production revision `7d8cd9c`. The latest collision implementation is `3f2781d`. Production code and assets were not changed. The review adds a reusable verification harness, this report, and saved measurements. See [SourceManifest.json](Results/SourceManifest.json) for hashes of the 33 production C# files checked against the isolated export.

## Correctness findings

### Reflected root scale is accepted but its reflection is discarded

Priority P2 for affected assets. [VehicleCollisionFootprint.cs:54](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Source/GameDataAndLogic/CollisionDetection/VehicleCollisionFootprint.cs:54) derives root scale from basis-vector lengths. These are positive even when the basis reflects an axis. The following guard therefore cannot enforce its stated positive-scale requirement.

An engine fixture has local X bounds `[2, 4]` and root scale `(-1, 1, 1)`. The visible mesh occupies X `[-4, -2]`; the collision footprint returns `[2, 4]`. For an asymmetric model, this moves the collider to the opposite side of the origin and can produce visible false contacts and missed contacts. Reject reflected root transforms during preparation, or incorporate their signed effect into the footprint and pose contract. The current positive-scale cars are unaffected.

### Child traversal does not follow Godot transform inheritance

Priority P2 for affected hierarchies. [VehicleCollisionFootprint.cs:89](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Source/GameDataAndLogic/CollisionDetection/VehicleCollisionFootprint.cs:89) always multiplies a Node3D child's local transform into the accumulated parent transform and carries that transform across ordinary Node children.

Both choices have counterexamples in Godot. A `TopLevel` mesh ignores its Node3D parent's transform. An ordinary Node between two Node3D objects also breaks their transform inheritance: Godot records a spatial parent only when the immediate parent is a Node3D. The engine fixtures produce X bounds `[22, 24]` while the actual meshes occupy `[19, 21]` in both cases. This follows the [Node3D parent assignment](C:/Users/k/Repository/External/Godot_4-7-2/scene/3d/node_3d.cpp:155) and [global transform calculation](C:/Users/k/Repository/External/Godot_4-7-2/scene/3d/node_3d.cpp:648) in the installed version's source.

Use Godot's actual transform relationships, or reject hierarchy modes that a static vehicle footprint cannot support. Independent children also need an explicit policy because they need not follow later vehicle movement. The current six car meshes have no children, so this finding does not affect Ribeye.

The exact reproduction values are saved in [EngineResults.json](Results/EngineResults.json). These observations are separate from the supported-case assertions, which all pass.

## Detector logic and integration

The detector's contract is vehicle perimeter versus closed track-outline segments. Edge touching and collinear overlap count as collisions. Full containment without a perimeter crossing does not. It does not classify drivable areas, provide contact normals, or sweep between poses.

The coordinate conversion is consistent: Blender X/Y becomes Godot X/negative Z; the manager negates Godot Z and yaw before transforming the rectangle. Native Godot transforms agreed with this conversion across positive and negative yaw for all six real cars within `0.00001` world units. Positive nonuniform root scale, hidden nested meshes, child rotation, custom bounds, and an empty-mesh rejection were also checked in the engine.

The expanded grid stores every edge whose expanded bounding box reaches a cell. Its radius covers every supported yaw, with rounding margin applied to broad-phase coverage. Empty cells can reject a query before trigonometry. Offset rectangles use the rounded rectangle center when supported; larger rectangles use the linear fallback.

When the expanded grid cannot be built, short edges go into a dense or sparse center grid. Query bounds expand by the maximum edge half extents. Larger edges use a linear scan for small counts or a balanced bounding volume hierarchy for larger counts. Large query ranges scan all edges. Coordinate spans that cannot be represented by integer cell indices move the affected edges into the outlier index. The tests exercised each of these routes and found no indexed answer that differed from the direct scan.

Construction copies collider coordinates into private edge storage, so later mutations of the input JSON objects do not change answers. It rejects null outlines, missing vertices, fewer than three vertices, repeated adjacent or closing vertices, and nonfinite coordinates. Invalid or default rectangle bounds are rejected; the default zero pose is valid. Finite inputs that overflow during corner transformation are also rejected.

The manager prepares every car's footprint once, shortens the front by `0.165` units, and caches the result by car index, planar position, and yaw. Moving queries perform no node-transform reads. A targeted test changed one car's footprint while keeping the pose unchanged and verified that switching the car invalidated the cached clear result. Track reinitialization constructs a new manager and index.

## Intentional limits and numerical behavior

Tunneling is explicitly accepted in [CollisionManager.cs:53](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Source/GameDataAndLogic/CollisionDetection/CollisionManager.cs:53). A fixture starts clear on one side of a thin wall and finishes clear on the other; its intermediate pose collides. I have not classified this as a defect. The current settings omit `VelocityLimiter` for every car, so there is no configured maximum speed from which to claim a safe minimum frame rate.

[Main.cs:85](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Source/Main.cs:85) checks the previous sampled pose before advancing and displaying the car. Consequently, a newly colliding pose can be displayed for one frame before the following update resets it. This is distinct from tunneling and is an integration behavior to keep in mind if visible contact response needs refinement.

The new approximate orientation calculation was reviewed against the user's earlier choice to prioritize performance and visually sufficient accuracy. In 114,000 rectangle-versus-track comparisons, including 60,000 near-boundary Ribeye queries, the independent exact oracle found no difference. An additional 10,000 segment cases with coordinates in `[-1000, 1000]` also agreed exactly.

The known constructed near miss with a gap of `2^(-60)`, approximately `8.67e-19` units, still reports a false contact. A separate stress distribution spanning float exponents from -140 to 124 produced 14 exact-predicate disagreements in 10,000 segment cases. These are deliberately extreme inputs, not measured driving failures. They demonstrate that the approximation is not mathematically exact. Near-zero determinant signs are a documented floating-point issue; see [Shewchuk's robust predicate research](https://www.cs.cmu.edu/~quake/robust.html).

Ribeye's largest collider-coordinate float spacing is approximately `0.00001526` units. The simplified predicate is consistent with the accepted accuracy goal on the tested geometry. Restoring exact arithmetic is not justified by these results. The harness keeps its exact arithmetic outside production and outside the timed query loops.

## Validation results

The production release build and Godot export completed with zero warnings and errors. The isolated verification export and managed harness also built cleanly. A 120-frame headless smoke test of the unmodified production export exited successfully without logged errors; its log is [ProductionSmoke.log](Results/ProductionSmoke.log).

| Validation                                     | Result                                              |
| ---------------------------------------------- | --------------------------------------------------- |
| Managed assertions                             | 144,060 passed                                      |
| Indexed versus production linear queries       | 124,000 comparisons, zero differences               |
| Independent exact rectangle oracle             | 114,000 comparisons, zero differences               |
| Real Ribeye queries within the exact checks    | 10,000 per car, 60,000 total, zero differences      |
| Expanded cell boundaries and adjacent floats   | 10,000 indexed versus linear checks passed          |
| Moderate-coordinate segment predicates         | 10,000 cases and reversal checks passed             |
| Exported Godot integration assertions          | 36,398 passed                                       |
| Reflected roots and transform boundaries       | Three reproductions of the two reported findings    |

The index cases include normal and recentered expanded queries, oversized rectangles, dense center grids with and without outliers, broad center scans, sparse center grids, linear outliers, outlier trees, coincident tree centers, integer grid overflow, subnormal geometry, and very large finite coordinates. Every generated case contains actual contacts as well as clear queries; this is not an all-clear test corpus. See [ValidationResults.json](Results/ValidationResults.json) for counts and index statistics.

An independent topology check of the actual asset found three outlines with 512, 224, and 64 vertices, totaling 800 edges. There are no nonadjacent self-intersections or intersections between outlines. Edge lengths range from `0.5519` to `4.1461` units. Bounds are approximately X `[-94.6073, 147.1670]`, Y `[-114.8798, 94.1410]`. See [AssetAnalysis.json](Results/AssetAnalysis.json).

## Query performance

Measurements used Windows 11 x64 build 26200, an Intel Core Ultra 9 275HX with 24 logical processors, .NET SDK 10.0.401, runtime 10.0.12, and Godot 4.7.2 .NET. Child processes ran sequentially at below-normal priority without affinity changes. The main benchmark disables tiered compilation, warms the code, then takes nine batch measurements per workload. The table shows the range of median batch means across six cars; timings include loop overhead.

| Workload    | Indexed ns    | Linear ns      | Speedup     |
| ----------- | ------------- | -------------- | ----------- |
| Uniform     | 31.5 - 43.9   | 956.7 - 1040.6 | 23.1 - 30.4 |
| Hits        | 121.7 - 144.9 | 570.8 - 656.0  | 4.4 - 4.7   |
| Near misses | 30.3 - 44.7   | 911.7 - 1020.7 | 22.6 - 30.1 |
| Far away    | 7.7 - 9.3     | 36.5 - 46.3    | 4.7 - 5.8   |

Inside the exported Godot verification application, an unchanged cached manager query measured `2.14 ns`; moving uniform manager queries measured `41.92 ns`. Both measured zero managed allocation bytes. The managed indexed benchmarks also measured zero bytes across 250,702,848 timed queries. This includes the fallback and larger synthetic cases; query counts are timing-loop repetitions, not that many independent randomized correctness cases.

The slowest Ribeye workload median is about `0.145 microseconds`, approximately `0.015%` of a 1 ms frame budget. Collision-query optimization is unlikely to produce a meaningful steady-play frame-rate gain at this scale. These are warm batch averages, not worst-case individual query latency.

## Construction and grid sizing

The default Ribeye index has 23,904 cells, 6,499 occupied cells, and 30,328 edge references. A warmed managed construction measured `0.175 ms`, allocated approximately 447 KB during construction, and retained approximately 339 KB afterward. Construction excludes scene loading, mesh footprint preparation, and JSON parsing.

| Cell scale | Cell size | Index    | Uniform ns | Retained KiB | Build ms |
| ---------- | --------- | -------- | ---------- | ------------ | -------- |
| 0.25       | 0.75      | Sparse   | 989.3      | 71.6         | 0.116    |
| 0.5        | 1.50      | Expanded | 42.8       | 330.6        | 0.162    |
| 1          | 3.00      | Expanded | 43.3       | 112.4        | 0.051    |
| 2          | 6.00      | Expanded | 50.9       | 53.5         | 0.027    |
| 4          | 12.00     | Expanded | 60.9       | 36.7         | 0.027    |

The smaller `0.25` scale exceeds the expanded grid's 65,536-cell limit and selects the sparse-center/outlier path. On the same uniform poses, it costs about 23 times the default scale. Some fallback queries exceed the covered-cell threshold and use a full scan, explaining why their aggregate timing approaches the direct scan. This is a confirmed performance discontinuity, not a correctness failure.

Doubling the default cell size reduced retained memory by about 66%; its uniform-query time was effectively equal in the final run. That is a possible memory tradeoff, but the sweep does not establish equivalent hit and near-miss costs for that setting. The current default is already fast across all measured Ribeye workloads, so changing it is unnecessary for the stated goal.

| Edges | Index    | Hit queries us | Linear us | Build ms | Retained MiB |
| ----- | -------- | -------------- | --------- | -------- | ------------ |
| 800   | Expanded | 0.266          | 0.681     | 0.080    | 0.135        |
| 8000  | Expanded | 1.841          | 5.690     | 0.654    | 1.178        |
| 80000 | Dense    | 15.861         | 61.936    | 5.283    | 2.759        |

The 80,000-edge circular outline exceeds the expanded reference budget and uses a dense center grid. The scaling cases show that fine tessellation increases candidate work even when the overall track bounds stay fixed. Expanded queries, broad scans, and heavily overlapping tree bounds can still have linear worst-case work. No general constant-time query guarantee follows from the spatial index.

One smaller storage issue is that [TrackCollisionDetector.cs:843](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Source/GameDataAndLogic/CollisionDetection/TrackCollisionDetector.cs:843) allocates `2 * edgeCount - 1` tree nodes even though leaves hold up to eight edges. That can retain several times the used node count in fallback trees. It has no effect on the current Ribeye expanded index; consider tighter capacity only if future tracks make outlier storage material.

## First query and measurement limits

Three fresh exported processes using default .NET settings measured first manager queries of `428`, `563`, and `442 microseconds`; the median is about `0.442 ms`. Complete application initialization measured about 73 to 74 ms in those runs, including scene and asset setup. These are fresh processes with existing OS file caches, not cold machine starts.

The optimized benchmark configuration with tiered compilation disabled measured a first query of `7.42 ms`. Do not use that figure as the normal application's first-frame cost: forcing full optimization changes initial JIT work. Warmed construction and steady-query timings likewise do not measure startup work.

The review did not measure windowed frame times, GPU cost, input latency, native heap allocations, cold CPU-cache queries, or visual play through every wall. Headless execution verifies native scene loading and transform behavior, but does not prove rendered contact appearance. Random and targeted coverage is evidence for the tested cases, not a proof for every finite float geometry. Prior large validation totals from earlier commits were not substituted for this revision's checks.

## Reproducing the review

Use [the verification harness](../../Verification/CollisionReview/README.md). Double-click its `Build.cmd` and `Run.cmd`, or run the commands in its README from Windows PowerShell. It exports an isolated application and writes each run's logs to a timestamped ignored folder. `Run.cmd -SkipBenchmarks` runs the required correctness checks; `Run.cmd -Stage Engine -UseDefaultTiering -SkipBenchmarks` checks a fresh engine process with normal runtime settings.

Saved results are in [Results](Results). Correctness and asset data come from the final harness run. The three default-runtime startup samples preceded the final addition of the explicit cache-switch assertion; their measured first query occurs before those later checks. Benchmark comparison uses the production linear scan from the same revision, not an older collision implementation.

For the current asset set, keep the existing detector and approximate predicates. Address the two footprint findings before accepting reflected vehicles or hierarchies with independent mesh children. If much larger tracks are introduced, remeasure the grid budget transition and consider choosing a larger cell size when the default cannot build an expanded grid.
