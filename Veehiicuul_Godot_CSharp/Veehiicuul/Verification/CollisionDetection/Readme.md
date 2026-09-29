# Collision verification and Ribeye benchmarks

Ribeye is the representative performance workload. This standalone .NET 10
program compiles the production detector directly. Track001 and synthetic
geometry remain correctness and fallback fixtures; their timings do not drive
the production index design. This folder is excluded from the Godot application
build and adds no application startup path or update callback.

Double-click `Build.cmd`, then `Run.cmd`. The launcher runs the original
correctness checks and three independent Ribeye benchmark processes. Each run
saves its transcript and raw JSON samples in `MyLogOutput/yyyy-MM-dd_HH-mm-ss`.
`Run.cmd` requires an existing build. These wrappers require Windows 11 x64 and
the .NET 10 SDK/runtime.

Optional PowerShell arguments, from this folder:

```powershell
.\Run.ps1 -VerificationOnly
.\Run.ps1 -Repetitions 1
.\Run.ps1 -DisableTieredCompilation -Repetitions 1
```

To run the extended correctness and scaling fixtures as well:

```powershell
.\bin\Release\net10.0\CollisionDetectionVerification.exe --performance --output=MyLogOutput\AllFixtures.json
```

## Contracts and correctness

The detector uses Blender world X/Y, matching glTF's conversion to Godot
X/negative Z. Model front is local Godot +Z, so front shortening applies to the
minimum local collision Y. Clockwise vehicle yaw is negative Godot Y rotation.
Contact includes exact endpoint and collinear contact. It is a discrete
perimeter test: containment without perimeter intersection is clear, and a
vehicle can cross an edge between sampled poses. These semantics are deliberate.

The manager uses the managed position and yaw applied by `CarStateManager`.
Mesh geometry and positive planar scale are fixed during a track session;
all cars' footprints and the shared track index are prepared at initialization.
Ancestor transforms are assumed to be identity. Changing geometry or scale
requires a new manager. The pose cache includes car selection, position, and
yaw; track reload creates a new manager.

`PerformanceAnalysis.cs` compares indexed queries with both a linear scan and
an independent BigInteger segment oracle. The oracle uses a separate integer
conversion and intersection implementation, while following the same rounded
binary32 rectangle-construction convention. Checks include every Ribeye edge,
closing edges, random poses, exact contacts, one-ULP gaps, subnormal and very
large coordinates, invalid input, and containment.

`GeometricValidation.cs` adds 2,358,720 comparisons on and one float step around
every default Ribeye grid boundary, seven yaw angles, 160 shifted-local-origin
oracle cases, and 32,019 independent orientation checks. The latter cover
arbitrary finite binary32 values and both sides of the Int128 fallback limit.
All timed Ribeye queries must allocate zero bytes after warmup, including exact
vertex contacts. Wider exponent spans still use BigInteger when necessary.

The original suite retains Track001 edge coverage, coordinate mapping,
arbitrary outline counts, sparse grids, and oversized-edge tree fallback.
Its short timing printouts are smoke checks, not the optimization measurements.

## Measurement method

Ribeye measurements use 15 calibrated batches per case, at least 150 ms of
query warmup, alternating indexed/linear measurement order, and checked contact
counts. The reported p95 describes **batch-average** costs, not individual-query
latency. Allocation measurement excludes setup and serialization. Construction
excludes JSON parsing and measures five warmup constructions followed by 15
samples; early construction can still show tiered-JIT effects.

Cell-size sweeps change the index scale independently of the physical footprint:
construction and query vehicle bounds remain identical. Mixed, clear, boundary,
exact-vertex, spawn, and oversized-query workloads are deterministic. These
corpora describe controlled geometry coverage, not a recorded gameplay trace.

## Native Godot validation

Run `RunNative.ps1` after importing the main project at least once. It requires
the configured portable Godot 4.7.2 .NET installation and export templates.
It copies project inputs and imported resources into the ignored repository
`Build/CollisionNativeAnalysis` directory. The source project and its compiled
assemblies are untouched. The copy receives `NativePerformanceAnalysis.cs.txt`
as its adapter and is built with optimized C# code before running headlessly.

Historical collision sources from commit `afcad73` are renamed and compiled into
that isolated measurement build only. The opt-in `CollisionBaselineDirectory`
build property is absent in normal builds and exports. Logs and historical
source snapshots are saved in this folder's timestamped `MyLogOutput` directory.

The native harness checks all six imported cars, 24,576 moving poses and cached
repeats, 800 track-boundary poses against the historical manager, rotation-only
changes, same-pose car changes with different bounds, nested hidden meshes,
custom AABBs, collision reset, input-driven car switching, explicit reset, and
track reload through `Main.Process`.

Native timings include a delegate call and 15 batches of 100,000 operations,
after 400 ms warmup. Both implementations are measured in the same process.
The moving apply-and-query cases include identical Godot position/yaw writes;
the baseline then reads native state while the optimized manager uses the
managed pose. Stationary cache hits are reported separately. These timings
exclude rendering, input processing, and the rest of the frame loop.

The main application's `Build.cmd` performs release compilation and export.
Run the exported application to check resource loading and DirectX presentation.
