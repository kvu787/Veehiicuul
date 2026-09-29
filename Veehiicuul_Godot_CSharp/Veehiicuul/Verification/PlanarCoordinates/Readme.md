# Planar coordinate verification

Run from this folder:

```powershell
dotnet run --project PlanarCoordinatesVerification.csproj --configuration Release
```

This console check uses Godot's managed vector and quaternion types without
starting the engine. It verifies native Godot quaternion agreement, inverse
rotations, heading/rotation round trips, cardinal model headings, rejection of
nonplanar inputs, and exact preservation of Y=0. Model front is +Z and model
right is -X; camera-relative input uses camera forward -Z and camera right +X.
All angles use native Godot radians: positive yaw turns +Z toward +X.

It also compiles the production `CarStateManager` and camera managers directly.
Small scene/input doubles supply deterministic inputs and capture applied poses;
all vector, quaternion, deadzone, acceleration, and braking calculations use the
production code and Godot's managed types. Regression checks cover coasting with
zero and deadzone input, velocity-aligned model headings, rotated spawn acceleration,
distinct acceleration strengths on all four model axes across multiple spawn
yaws, successive forward acceleration, the `CameraPanAndYaw` pivot's track-local
yaw independently of camera child rotations, braking without reversal, speed
limiting while coasting, and reset. Distinct local/world fixture values verify
that spawn, input, and applied poses stay in track space. `Model` must have an
identity local transform, as enforced by `TrackObjects` in the native application.

Camera checks cover starting in fixed/follow mode, toggling between their default
positions and sizes, zoom after the toggle frame, explicit zoom resets, and zoom
limits. The node doubles derive their quaternion from the assigned local rotation;
they do not require the removed world-pose setter. Native scene initialization,
the `Model` validation, and controller polling require an application check.
