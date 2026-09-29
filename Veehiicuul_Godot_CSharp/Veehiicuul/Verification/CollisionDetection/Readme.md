# Collision verification

This standalone .NET 10 program adapts ZoomTracks' existing production collision
test harness. It compiles the ported production detector directly and exercises
the committed Track001 collision data. It is excluded from the Godot application
build, so it does not add an application startup path or update callback.

From the Godot project root (`Veehiicuul_Godot_CSharp/Veehiicuul`):

```powershell
dotnet run --project Verification/CollisionDetection/CollisionDetectionVerification.csproj --configuration Release
```

The checks cover the expected 1,984 track edges, coordinate mapping, exact contact
semantics, arbitrary outline counts, sparse grids, oversized-edge BVH fallback,
every-edge neighborhoods, deterministic random queries versus the linear oracle,
and zero allocations during ordinary steady-state queries. Ribeye's unmodified
JSON also checks metadata-free loading, its clear spawn, and contact with an
exported outline. All pose rotations and verification angles use radians.
Comparative timings
are reported without machine-dependent performance thresholds.

The detector uses Blender world X/Y directly, matching glTF's conversion to Godot
X/negative Z. Model front is local Godot +Z, so front shortening applies to the
minimum local collision Y. Clockwise vehicle yaw is negative Godot Y rotation.
This program checks the collision algorithm and track data; the exported game's Ribeye scene
also needs a startup check to verify mesh import and vehicle integration.

## Extended analysis and benchmarks

Double-click `Build.cmd`, then `Run.cmd`. The launcher runs the existing checks
and three independent benchmark processes. Each run saves its transcript and raw
JSON samples in `MyLogOutput/yyyy-MM-dd_HH-mm-ss`. The executable must already
exist; `Run.cmd` does not build it. These wrappers require Windows 11 x64 and the
.NET 10 SDK/runtime.

Optional PowerShell arguments:

```powershell
.\Run.ps1 -VerificationOnly
.\Run.ps1 -Repetitions 1
.\Run.ps1 -DisableTieredCompilation -Repetitions 1
```

`PerformanceAnalysis.cs` compiles the unchanged production detector into this
verification assembly. It tests both committed tracks against the production
linear scan and an independent BigInteger segment oracle. The oracle has a
different integer conversion and intersection implementation, but intentionally
uses the same documented rounded binary32 rectangle-construction convention.
It covers every track edge, random poses, exact contacts, one-ULP gaps, subnormal
and very large coordinates, invalid input, and the grid integer-span fallback.

Timing uses 15 calibrated batches per case, at least 150 ms of query warmup,
alternating indexed/linear measurement order, and checked contact counts.
The reported p95 is a percentile of **batch-average** costs, not individual
query latency. Allocation measurement excludes setup and result serialization.
Index construction excludes JSON parsing and measures five warmup constructions
followed by 15 samples; early construction can still show tiered-JIT effects.
The synthetic tiled-track and long-edge workloads test scaling and should not
be interpreted as recorded gameplay. The largest tiled workload verifies the
linear result but omits its timed baseline. Cell-scale cases change only index
construction bounds; they keep query vehicle bounds identical.

`RunNative.ps1` performs the optional native Godot check. Close Godot first.
It saves the existing application adapter, temporarily installs
`NativePerformanceAnalysis.cs.txt`, builds optimized Debug code, and runs the
installed Godot 4.7.2 .NET headlessly. Its `finally` block restores the original
source bytes and rebuilds the normal Debug assembly. Logs and the backup are
saved in the verification folder's timestamped `MyLogOutput` folder. If the
PowerShell process is forcibly terminated, restore `OriginalAdapter.cs.txt`
from that run manually before building again.

The native check measures `CollisionManager.IsCarColliding`, the detector,
scaled bounds, and car switching; checks all six imported cars and 4,096 poses;
and probes track-root translation. It does not measure rendering, input-to-frame
latency, or the full frame loop. Car-switch timing includes the harness's
reflection and boxing overhead. Native timings include a delegate call.
