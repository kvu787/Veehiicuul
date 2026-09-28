using Godot;
using System;

namespace Veehiicuul_Godot_CSharp;

public sealed class TrackObjects {
    public Node3D PlaceholderCarNode { get; }
    public Node3D CameraPanAndYaw { get; }
    public Node3D CameraYawOffset { get; }
    public Node3D CameraPanOffsetAndPitch { get; }
    public Camera3D Camera { get; }

    public TrackObjects(TrackSwitcher trackSwitcher) {
        this.PlaceholderCarNode = trackSwitcher.CurrentTrackScene.GetNode<MeshInstance3D>("Model/SlopeCarPlaceholder") as Node3D ?? throw new InvalidOperationException();
        ValidatePlaceholderCar(this.PlaceholderCarNode);
        this.PlaceholderCarNode.Visible = false;

        this.CameraPanAndYaw = trackSwitcher.CurrentTrackScene.GetNode<Node3D>("CameraPanAndYaw") ?? throw new InvalidOperationException();
        this.CameraYawOffset = trackSwitcher.CurrentTrackScene.GetNode<Node3D>("CameraPanAndYaw/CameraYawOffset") ?? throw new InvalidOperationException();
        this.CameraPanOffsetAndPitch = trackSwitcher.CurrentTrackScene.GetNode<Node3D>("CameraPanAndYaw/CameraYawOffset/CameraPanOffsetAndPitch") ?? throw new InvalidOperationException();
        this.Camera = trackSwitcher.CurrentTrackScene.GetNode<Node3D>("CameraPanAndYaw/CameraYawOffset/CameraPanOffsetAndPitch/Camera") as Camera3D ?? throw new InvalidOperationException();

        foreach (Car car in trackSwitcher.CurrentTrackJson.Cars) {
            Node3D carDecorativeNode = trackSwitcher.CurrentTrackScene.FindChild(car.GameObjectName, recursive: true, owned: false) as MeshInstance3D
                ?? throw new InvalidOperationException($"Car model MeshInstance3D name='{car.GameObjectName}' was not found in sceneName='{trackSwitcher.CurrentTrackScene.Name}'");
            Node3D carGameNode = carDecorativeNode.Duplicate(0) as MeshInstance3D
                ?? throw new InvalidOperationException();
            trackSwitcher.CurrentTrackScene.AddChild(carGameNode);
            carGameNode.Visible = false;
            car.Node = carGameNode;
        }
    }

    private static void ValidatePlaceholderCar(Node3D placeholderCar) {
        const float tolerance = 0.00001f;

        if (placeholderCar.Position.Y != 0f
            || Mathf.Abs(placeholderCar.RotationDegrees.X) >= tolerance
            || Mathf.Abs(placeholderCar.RotationDegrees.Z) >= tolerance
            || Mathf.Abs(placeholderCar.Scale.X - 1f) >= tolerance
            || Mathf.Abs(placeholderCar.Scale.Y - 1f) >= tolerance
            || Mathf.Abs(placeholderCar.Scale.Z - 1f) >= tolerance) {
            throw new InvalidOperationException("SlopeCarPlaceholder must be at ground height with only yaw rotation and unit local scale.");
        }
    }

    private void ValidateCameraParameters() {
        Require(this.CameraYawOffset.Position.IsEqualApprox(Vector3.Zero), "CameraYawOffset position must be zero.");
        Require(this.CameraYawOffset.Rotation.IsEqualApprox(Vector3.Zero), "CameraYawOffset rotation must be zero.");
        Require(this.CameraYawOffset.Scale.IsEqualApprox(Vector3.One), "CameraYawOffset scale must be one.");

        // Reflect Unity Z and point the native Godot camera along its local -Z.
        Require(this.CameraPanOffsetAndPitch.Position.IsEqualApprox(Vector3.Zero), "CameraPanOffsetAndPitch position must be zero.");
        Require(this.CameraPanOffsetAndPitch.RotationDegrees.IsEqualApprox(new Vector3(-45f, 0f, 0f)), "CameraPanOffsetAndPitch must have -45 degrees X pitch.");
        Require(this.CameraPanOffsetAndPitch.Scale.IsEqualApprox(Vector3.One), "CameraPanOffsetAndPitch scale must be one.");
        Require(Mathf.IsZeroApprox(this.Camera.Position.X) && Mathf.IsZeroApprox(this.Camera.Position.Y)
            && this.Camera.Position.Z > 0f, "Camera local position must be on the positive Z axis.");
        Require(this.Camera.Rotation.IsEqualApprox(Vector3.Zero), "Camera local rotation must be zero.");
        Require(this.Camera.Scale.IsEqualApprox(Vector3.One), "Camera scale must be one.");
        Require(this.Camera.Projection == Camera3D.ProjectionType.Orthogonal, "Camera projection must be orthogonal.");
        Require(this.Camera.KeepAspect == Camera3D.KeepAspectEnum.Height, "Camera must preserve vertical size with KeepAspect=Height.");
        //Require(IsValidSize(this.DefaultFixedCameraSize), "The fixed camera half-height is out of range.");
        //Require(IsValidSize(this.DefaultFollowCameraSize), "FollowCameraSize is out of range.");
        //Require(IsValidSize(this.OrthographicCameraSize), "The selected camera half-height is out of range.");
        Require(this.Camera.Near > 0f && this.Camera.Far > this.Camera.Near,
            "Camera clipping planes must have 0 < Near < Far.");
    }

    private static void Require(bool condition, string message) {
        if (!condition) {
            throw new InvalidOperationException(message);
        }
    }

    //private static bool IsValidSize(float size) {
    //    return MinOrthographicCameraSize <= size && size <= MaxOrthographicCameraSize;
    //}
}
