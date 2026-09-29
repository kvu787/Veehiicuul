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
