using Godot;
using System;

namespace Veehiicuul_Godot_CSharp;

public static class TransformExtensions {
    /// <summary>Copies world position/rotation and local scale, as in the source.</summary>
    public static void SetFrom(this Node3D self, Node3D other) {
        ArgumentNullException.ThrowIfNull(self);
        ArgumentNullException.ThrowIfNull(other);
        self.SetPositionAndRotation(other.GlobalPosition, other.GlobalBasis.GetRotationQuaternion());
        self.Scale = other.Scale;
    }

    /// <summary>Sets a world pose while preserving the node's local scale.</summary>
    public static void SetPositionAndRotation(this Node3D self, Vector3 position, Quaternion rotation) {
        ArgumentNullException.ThrowIfNull(self);
        self.GlobalPosition = position;
        self.GlobalRotation = rotation.GetEuler();
    }
}
