using Godot;
using System;

namespace Veehiicuul_Godot_CSharp;

public static class Vector3Extensions {
    /// <summary>Returns native Godot yaw in radians from model front (+Z) for a nonzero horizontal vector.</summary>
    /// <exception cref="ArgumentException">The vector is zero or has a nonzero Y component.</exception>
    public static float Get2DRotation(this Vector3 vector) {
        RequireHorizontal(vector);
        if (vector == Vector3.Zero) {
            throw new ArgumentException("Rotation is undefined for Vector3.Zero", nameof(vector));
        }
        return Mathf.Atan2(vector.X, vector.Z);
    }

    private static void RequireHorizontal(Vector3 vector) {
        if (vector.Y != 0f) {
            throw new ArgumentException("Planar vectors must have Y equal to zero.", nameof(vector));
        }
    }
}
