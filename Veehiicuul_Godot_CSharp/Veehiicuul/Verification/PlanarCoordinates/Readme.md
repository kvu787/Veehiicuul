# Planar coordinate verification

Run from this folder:

```powershell
dotnet run --project PlanarCoordinatesVerification.csproj --configuration Release
```

This console check uses Godot's managed vector and quaternion types without
starting the engine. It checks the port against the original Unity yaw formula
after reflecting Z, verifies native Godot quaternion agreement, inverse rotations,
cardinal model headings, and exact preservation of Y=0. Model front is +Z and
model right is -X; camera-relative input still uses camera forward -Z and
camera right +X. All angles use radians.

It also compiles the production `CarStateManager` and `CameraYawManager` directly.
Small scene/input doubles supply deterministic inputs and capture applied poses;
all vector, quaternion, deadzone, acceleration, and braking calculations use the
production code and Godot's managed types. Regression checks cover coasting with
zero and deadzone input, velocity-aligned model headings, rotated spawn acceleration,
distinct acceleration strengths on all four model axes across multiple spawn
yaws, successive forward acceleration, the `CameraPanAndYaw` pivot's world yaw
independently of camera child rotations, braking without reversal, speed limiting
while coasting, reset, and spawn poses inherited from parent transforms. Native scene
initialization and controller polling require an application check; this console
program does not start the engine.
