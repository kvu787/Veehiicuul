using Godot;
using System;

namespace Veehiicuul_Godot_CSharp;

/// <summary>A snapshot of world pose and local scale.</summary>
public readonly struct TransformStruct {
    public Vector3 Position { get; }
    public Quaternion Rotation { get; }
    public Vector3 Scale { get; }

    public TransformStruct(Node3D node) {
        ArgumentNullException.ThrowIfNull(node);
        this.Position = node.GlobalPosition;
        this.Rotation = node.GlobalBasis.GetRotationQuaternion();
        this.Scale = node.Scale;
    }

    public void ApplyTo(Node3D node) {
        ArgumentNullException.ThrowIfNull(node);
        node.SetPositionAndRotation(this.Position, this.Rotation);
        node.Scale = this.Scale;
    }
}
