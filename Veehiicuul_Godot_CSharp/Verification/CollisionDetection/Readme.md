# Collision verification

This standalone .NET 10 program adapts ZoomTracks' existing production collision
test harness. It compiles the ported production detector directly and exercises
the committed Track001 collision data. It is excluded from the Godot application
build, so it does not add an application startup path or update callback.

From the Godot application's folder:

```powershell
dotnet run --project Verification/CollisionDetection/CollisionDetectionVerification.csproj --configuration Release
```

The checks cover the expected 1,984 track edges, coordinate mapping, exact contact
semantics, arbitrary outline counts, sparse grids, oversized-edge BVH fallback,
every-edge neighborhoods, deterministic random queries versus the linear oracle,
and zero allocations during ordinary steady-state queries. Comparative timings
are reported without machine-dependent performance thresholds.

The detector retains the original two-dimensional collision plane. Its Y is
negative Godot Z, and clockwise vehicle yaw is negative Godot Y rotation. Godot
mesh import and vehicle scene integration require separate validation after the
assets are ported; this program checks the collision algorithm and track data.
