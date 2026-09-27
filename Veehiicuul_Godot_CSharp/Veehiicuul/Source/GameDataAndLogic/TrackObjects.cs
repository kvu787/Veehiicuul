using Godot;
using System;

namespace Veehiicuul_Godot_CSharp;

public sealed class TrackObjects {
    public Node3D PlaceholderCarTransform { get; }

    public TrackObjects(Node3D trackScene) {
        this.PlaceholderCarTransform = trackScene.FindChild("SlopeCarPlaceholder", true, false) as Node3D
            ?? throw new InvalidOperationException("The track needs a SlopeCarPlaceholder Node3D.");
        ValidatePlaceholderCar(this.PlaceholderCarTransform);
        this.PlaceholderCarTransform.Visible = false;
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
