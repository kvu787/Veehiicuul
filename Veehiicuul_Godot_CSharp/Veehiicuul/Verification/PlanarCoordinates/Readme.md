# Planar coordinate verification

Run from this folder:

```powershell
dotnet run --project PlanarCoordinatesVerification.csproj --configuration Release
```

This console check uses Godot's managed vector and quaternion types without
starting the engine. It checks the port against the original Unity yaw formula
after reflecting Z, verifies native Godot quaternion agreement, inverse rotations,
cardinal headings, and exact preservation of Y=0. All angles use radians.

It also compiles the production `CarStateManager` and `CameraYawManager` directly.
Small scene/input doubles supply deterministic inputs and capture applied poses;
all vector, quaternion, deadzone, acceleration, and braking calculations use the
production code and Godot's managed types. Regression checks cover coasting with
zero and deadzone input, velocity-aligned headings, rotated spawn acceleration,
the `CameraPanAndYaw` pivot's world yaw independently of camera child rotations,
braking without reversal, speed limiting while
coasting, reset, and spawn poses inherited from parent transforms. Native scene
initialization and controller polling require an application check; this console
program does not start the engine.
