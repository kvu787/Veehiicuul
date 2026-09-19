using Godot;
using System;

namespace Veehiicuul_Godot_CSharp;

public sealed class CameraPivotManager {
    private CameraFollowSettings CameraFollowSettings { get; }
    private CameraController CameraController { get; }
    private CarState CarState { get; }
    private InputManager InputManager { get; }
    private Node3D CameraPanAndYaw { get; }
    private TransformStruct OriginalCameraPanAndYawTransform { get; }

    public CameraPivotManager(Node3D trackScene, CameraFollowSettings cameraFollowSettings, CameraController cameraController, CarState carState, InputManager inputManager) {
        ArgumentNullException.ThrowIfNull(trackScene);
        ArgumentNullException.ThrowIfNull(cameraFollowSettings);
        ArgumentNullException.ThrowIfNull(cameraController);
        ArgumentNullException.ThrowIfNull(carState);
        ArgumentNullException.ThrowIfNull(inputManager);
        this.CameraFollowSettings = cameraFollowSettings;
        this.CameraController = cameraController;
        this.CarState = carState;
        this.InputManager = inputManager;
        this.CameraPanAndYaw = trackScene.FindChild(nameof(this.CameraPanAndYaw), true, false) as Node3D
            ?? throw new InvalidOperationException("The track needs a CameraPanAndYaw Node3D.");
        this.Validate();
        this.OriginalCameraPanAndYawTransform = new TransformStruct(this.CameraPanAndYaw);
    }

    public void ReadInputAndToggle() {
        if (this.InputManager.ToggleBetweenFixedAndFollowCamera) {
            this.CameraFollowSettings.FollowsCarLocation = !this.CameraFollowSettings.FollowsCarLocation;
            this.CameraController.ResetZoom();
        }
    }

    public void UpdateCameraPivot() {
        this.CameraPanAndYaw.GlobalPosition = this.CameraFollowSettings.FollowsCarLocation
            ? this.CarState.Position : this.OriginalCameraPanAndYawTransform.Position;
    }

    private void Validate() {
        if (!Mathf.IsZeroApprox(this.CameraPanAndYaw.Position.Y)
            || !Mathf.IsZeroApprox(this.CameraPanAndYaw.Rotation.X)
            || !Mathf.IsZeroApprox(this.CameraPanAndYaw.Rotation.Z)
            || !this.CameraPanAndYaw.Scale.IsEqualApprox(Vector3.One)) {
            throw new InvalidOperationException("CameraPanAndYaw must have Y=0, no X/Z rotation, and unit local scale.");
        }
    }
}
