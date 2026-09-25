# Planar coordinate verification

Run from this folder:

```powershell
dotnet run --project PlanarCoordinatesVerification.csproj
```

This console check uses Godot's managed vector and quaternion types without
starting the engine. It checks the port against the original Unity yaw formula
after reflecting Z, verifies native Godot quaternion agreement, inverse rotations,
cardinal headings, and exact preservation of Y=0. It does not run the application
or bypass its deliberate startup exception.
