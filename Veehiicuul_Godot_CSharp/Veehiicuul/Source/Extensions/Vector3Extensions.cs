using Godot;
using System;

namespace Veehiicuul_Godot_CSharp;

public static class Vector3Extensions {
    /// <summary>
    /// Rotates a horizontal Godot vector clockwise viewed from above, in radians.
    /// This preserves the source game's positive yaw convention after reflecting Z.
    /// Godot's own positive Y-axis rotation has the opposite sign.
    /// </summary>
    public static Vector3 Rotate2D(this Vector3 vector, float rotationRadians) {
        RequireHorizontal(vector);
        float cosine = Mathf.Cos(rotationRadians);
        float sine = Mathf.Sin(rotationRadians);
        return new Vector3(
            vector.X * cosine - vector.Z * sine,
            0f,
            vector.X * sine + vector.Z * cosine);
    }

    /// <summary>Applies a native Godot quaternion containing only yaw.</summary>
    public static Vector3 Rotate2D(this Vector3 vector, Quaternion rotation) {
        if (!Mathf.IsZeroApprox(rotation.X) || !Mathf.IsZeroApprox(rotation.Z)) {
            throw new ArgumentException("A planar rotation must contain only Y-axis rotation.", nameof(rotation));
        }
        return vector.Rotate2D(-rotation.GetEuler().Y);
    }

    /// <summary>Returns clockwise model yaw in radians from model front (+Z); zero for a zero vector.</summary>
    public static float Get2DRotation(this Vector3 vector) {
        RequireHorizontal(vector);
        return vector == Vector3.Zero ? 0f : Mathf.Atan2(-vector.X, vector.Z);
    }

    /// <summary>Returns the native Godot yaw quaternion that points model front (+Z) along the vector.</summary>
    public static Quaternion Get2DRotationQuaternion(this Vector3 vector) {
        return new Quaternion(Vector3.Up, -vector.Get2DRotation());
    }

    private static void RequireHorizontal(Vector3 vector) {
        if (vector.Y != 0f) {
            throw new ArgumentException("Planar vectors must have Y equal to zero.", nameof(vector));
        }
    }
}
