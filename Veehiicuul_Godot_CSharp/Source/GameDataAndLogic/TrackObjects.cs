using Godot;
using System;
using System.Linq;

namespace Veehiicuul_Godot_CSharp;

public sealed class TrackObjects {
    public Node3D PlaceholderCarTransform { get; }
    public Node3D[] TireGroundContactPoints { get; }
    public Node3D[] Obstacles { get; }

    public TrackObjects(Node3D trackScene) {
        this.PlaceholderCarTransform = RequireNode<Node3D>(trackScene, "SlopeCarPlaceholder");
        ValidatePlaceholderCar(this.PlaceholderCarTransform);
        this.PlaceholderCarTransform.Visible = false;
        this.TireGroundContactPoints = [
            RequireNode<Node3D>(this.PlaceholderCarTransform, "CarFL"),
            RequireNode<Node3D>(this.PlaceholderCarTransform, "CarFR"),
            RequireNode<Node3D>(this.PlaceholderCarTransform, "CarRL"),
            RequireNode<Node3D>(this.PlaceholderCarTransform, "CarRR"),
        ];
        // Optional legacy collision group; the active detector reads collider JSON instead.
        Node3D? obstacleGroup = trackScene.GetNodeOrNull<Node3D>("ObstacleGroup");
        this.Obstacles = obstacleGroup is null ? [] : obstacleGroup.GetChildren().OfType<Node3D>().ToArray();
    }

    public static T RequireNode<T>(Node parent, string relativePath) where T : Node {
        return parent.GetNodeOrNull<T>(relativePath)
            ?? throw new InvalidOperationException($"Scene '{parent.Name}' requires a {typeof(T).Name} at '{relativePath}'. Scene assets have not been ported automatically.");
    }

    private static void ValidatePlaceholderCar(Node3D placeholderCar) {
        // Importing and decomposing rotations introduces rounding. Preserve the source tolerance,
        // checking all three scale axes (the source accidentally checked X three times).
        const float scaleTolerance = 0.00001f;
        float angleTolerance = Mathf.DegToRad(0.00001f);
        Vector3 rotation = placeholderCar.GlobalBasis.GetEuler();
        Vector3 scale = placeholderCar.Scale;
        if (placeholderCar.GlobalPosition.Y != 0f
            || Mathf.Abs(Mathf.AngleDifference(rotation.X, 0f)) >= angleTolerance
            || Mathf.Abs(Mathf.AngleDifference(rotation.Z, 0f)) >= angleTolerance
            || Mathf.Abs(scale.X - 1f) >= scaleTolerance
            || Mathf.Abs(scale.Y - 1f) >= scaleTolerance
            || Mathf.Abs(scale.Z - 1f) >= scaleTolerance) {
            throw new InvalidOperationException("SlopeCarPlaceholder must be at ground height with only yaw rotation and unit local scale.");
        }
    }
}
