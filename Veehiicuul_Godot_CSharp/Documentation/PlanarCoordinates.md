# Planar coordinates

All runtime angles are radians. Godot's positive Y rotation turns +Z toward +X.
`Vector3.Rotated(Vector3.Up, angle)`, `Get2DRotation`, `new Quaternion(Vector3.Up, angle)`,
and node `Rotation.Y` use this same convention. `Get2DRotation` requires a nonzero
horizontal vector; it rejects zero vectors and vectors with nonzero Y.

| Reference      | Forward | Right | Up  |
| -------------- | ------- | ----- | --- |
| Imported car   | +Z      | -X    | +Y  |
| Camera         | -Z      | +X    | +Y  |

The right stick supplies X to the right and Y upward. Driving converts this to
camera-local `(X, 0, -Y)`, rotates by `CameraPanAndYaw.Rotation.Y` into track space,
then rotates by negative car yaw into car space. Car-local +Z selects forward
acceleration, -Z reverse, -X right, and +X left. The resulting acceleration returns
to track space before velocity and position are integrated.

`CarStateManager.Position`, velocity, and yaw are relative to the track root.
The camera pivot and playable cars are children of that root. The placeholder is
under `Model`, whose local transform must be exactly identity; `TrackObjects`
checks this before using the placeholder. The placeholder's local pose can
therefore be copied directly to the playable car. Reset restores that pose and
clears velocity. Camera follow also copies the car's track-local position.

The collision detector uses a separate two-dimensional plane: Godot `(X, -Z)`,
matching the Blender X/Y coordinates stored in the exported outline JSON.
`CollisionManager` converts the vehicle's world position to this plane and
negates its native Godot yaw for the detector's clockwise rotation formula.
Model front (+Z) maps to minimum collision Y, so front shortening increases
`MinY`; rear shortening decreases `MaxY`. Collision pose angles are also radians.

Collision outlines are exported world coordinates. Moving the track hierarchy
does not transform the JSON data; its geometry must still line up with those
coordinates when using `CollisionManager`. The car/camera track-space calculations
do not change this separate collision-data requirement.

Run the [planar checks](../Veehiicuul/Verification/PlanarCoordinates/Readme.md) and
[collision checks](../Veehiicuul/Verification/CollisionDetection/Readme.md) after
changing these conventions.
