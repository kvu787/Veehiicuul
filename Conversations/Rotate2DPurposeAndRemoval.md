# Rotate2D Purpose And Removal

## User

what is the purpose of these methods? can they be removed?
```
c:\Users\k\Repository\Veehiicuul\Veehiicuul_Godot_CSharp\Veehiicuul\Source\Extensions\Vector3Extensions.cs:7:1-28:6
Lines and columns are 1-based. Columns are StartInclusive:EndExclusive. Columns count UTF-16 code units.
```
```
    /// <summary>
    /// Rotates a horizontal vector about Godot's positive Y axis, in radians.
    /// Positive yaw rotates model front (+Z) toward +X (model left).
    /// </summary>
    public static Vector3 Rotate2D(this Vector3 vector, float rotationRadians) {
        RequireHorizontal(vector);
        float cosine = Mathf.Cos(rotationRadians);
        float sine = Mathf.Sin(rotationRadians);
        return new Vector3(
            (vector.X * cosine) + (vector.Z * sine),
            0f,
            (-vector.X * sine) + (vector.Z * cosine));
    }

    /// <summary>Applies a native Godot quaternion containing only yaw.</summary>
    public static Vector3 Rotate2D(this Vector3 vector, Quaternion rotation) {
        if (!Mathf.IsZeroApprox(rotation.X) || !Mathf.IsZeroApprox(rotation.Z)) {
            throw new ArgumentException("A planar rotation must contain only Y-axis rotation.", nameof(rotation));
        }

        return vector.Rotate2D(rotation.GetEuler().Y);
    }
```

## Assistant Commentary

I’ll check how these overloads are used and whether Godot’s built-in rotation methods could replace them.

## Assistant

**Yes, both can be removed without changing current gameplay.** Neither has runtime callers anymore; their remaining callers are in the [planar verification program](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Verification/PlanarCoordinates/Program.cs:6).

Their purpose is to rotate vectors within the horizontal **XZ plane**:

- **`float` overload:** Rejects vectors with nonzero Y, rotates X/Z by the supplied angle in radians, and returns Y = 0.
- **`Quaternion` overload:** Rejects quaternions with appreciable X/Z components, extracts the yaw angle, and delegates to the first overload.

The car already uses `vector.Rotated(Vector3.Up, angle)` in [CarStateManager.cs](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Source/GameDataAndLogic/Car/CarStateManager.cs:56). Godot provides that rotation directly; the custom float overload mainly adds the horizontal-input restriction. [Godot implementation](https://github.com/godotengine/godot/blob/master/modules/mono/glue/GodotSharp/GodotSharp/Core/Vector3.cs)

Removing them also requires updating the verification program and its documentation references. **Keep `RequireHorizontal`**, because `Get2DRotation` still uses it, and `Get2DRotation` is used by gameplay.

I haven’t changed application code.
