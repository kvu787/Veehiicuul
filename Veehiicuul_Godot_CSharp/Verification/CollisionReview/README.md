# Collision detection verification

This Windows 11 x64 harness verifies the production collision detector, its Godot integration, and its CPU cost. It uses .NET 10 and the repository's portable Godot 4.7.2 .NET installation with matching export templates.

Double-click `Build.cmd`, then `Run.cmd`. The build exports an isolated copy of the real application with a C# verification scene. It also builds a managed console harness that links the production detector sources. Production source files are not edited. Rebuild after changing production code or assets so both harnesses test the same revision.

Each run writes results and logs under `MyLogOutput/yyyy-MM-dd_HH-mm-ss`. Generated exports, intermediate files, and session logs are ignored by Git. Runs use one process at a time, below-normal priority, and no CPU affinity override. The exported verification scene runs headless.

For correctness checks without benchmark loops:

```powershell
.\Run.cmd -SkipBenchmarks
```

For a fresh engine start using default .NET tiered compilation:

```powershell
.\Run.cmd -Stage Engine -UseDefaultTiering -SkipBenchmarks
```

Default benchmark runs disable tiered compilation in the child processes to measure optimized code without tier transitions. The launcher restores the caller's environment variables afterward.

The managed suite covers expanded, recentered, and oversized queries; dense and sparse center grids; linear outliers and their bounding volume hierarchy; broad scans; cell boundaries; invalid input; float extremes; and immutable input snapshots. It compares indexed queries with the production linear scan and with independent exact predicates implemented using integer arithmetic over binary32 coordinates. Geometry transformation follows the detector's documented float rounding contract.

The engine suite loads the real Ribeye scene, derives all six car footprints, checks yaw conversion against Godot transforms, verifies cached and moving queries, and exercises cache invalidation using deliberately different car footprints. Synthetic meshes reproduce the review's negative-scale and transform-inheritance findings. These findings are recorded as observations; the harness does not repair them. The known subvisual approximate predicate error is likewise a documented observation.

Performance results are medians and ranges of batch means, including loop overhead. They describe warm CPU caches, not GPU performance, frame-time percentiles, or worst-case individual queries. Construction measurements exclude JSON parsing and scene loading; retained-memory estimates use several rooted detectors to reduce measurement noise.

The dated review and saved results are in `../../Documentation/CollisionReview20261002`.
