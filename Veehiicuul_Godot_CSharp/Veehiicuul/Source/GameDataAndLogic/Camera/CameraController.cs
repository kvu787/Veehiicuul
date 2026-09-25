using Godot;
using System;

namespace Veehiicuul_Godot_CSharp;

/// <summary>Controls the source camera rig through ordinary calls from Main.</summary>
public sealed class CameraController {
    private const float CameraRotationSpeed = 100f;
    private const float CameraZoomSpeed = 50f;
    private const float MinOrthographicCameraSize = 1f;
    private const float MaxOrthographicCameraSize = 281.25f;

    private CameraFollowSettings CameraFollowSettings { get; }
    private InputManager InputManager { get; }
    private TimeManager TimeManager { get; }
    private Node3D CameraYawOffset { get; }
    private Node3D CameraPanOffsetAndPitch { get; }
    private Camera3D Camera { get; }
    private float RotationOffset { get; set; }
    private float DefaultFixedCameraSize { get; }
    private float DefaultFollowCameraSize { get; }

    /// <summary>Vertical half-height in world units, matching the authored track JSON.</summary>
    public float OrthographicCameraSize { get; set; }

    /// <summary>Clockwise yaw from Godot forward (-Z), in degrees.</summary>
    public float CameraYaw => -Mathf.RadToDeg(this.Camera.GlobalRotation.Y);

    public CameraController(Node3D trackScene, CameraFollowSettings cameraFollowSettings, TrackJson trackJson, InputManager inputManager, TimeManager timeManager) {
        ArgumentNullException.ThrowIfNull(trackScene);
        ArgumentNullException.ThrowIfNull(cameraFollowSettings);
        ArgumentNullException.ThrowIfNull(trackJson);
        ArgumentNullException.ThrowIfNull(inputManager);
        ArgumentNullException.ThrowIfNull(timeManager);
        this.CameraFollowSettings = cameraFollowSettings;
        this.InputManager = inputManager;
        this.TimeManager = timeManager;
        this.CameraYawOffset = trackScene.FindChild(nameof(this.CameraYawOffset), true, false) as Node3D
            ?? throw new InvalidOperationException("The track needs a CameraYawOffset Node3D.");
        this.CameraPanOffsetAndPitch = trackScene.FindChild(nameof(this.CameraPanOffsetAndPitch), true, false) as Node3D
            ?? throw new InvalidOperationException("The track needs a CameraPanOffsetAndPitch Node3D.");
        this.Camera = trackScene.FindChild(nameof(this.Camera), true, false) as Camera3D
            ?? throw new InvalidOperationException("The track needs a Camera Camera3D.");

        // Camera3D.Size is full height with KeepAspect=Height; Unity uses half-height.
        this.DefaultFixedCameraSize = this.Camera.Size * 0.5f;
        this.DefaultFollowCameraSize = trackJson.FollowCameraSize;
        this.ResetZoom();
        this.ValidateCameraParameters();
    }

    public void Update() {
        this.CameraYawOffset.Rotation = new Vector3(0f, -Mathf.DegToRad(this.RotationOffset), 0f);
        this.Camera.Size = 2f * this.OrthographicCameraSize;
    }

    public void ReadInputAndChangeCameraSettings() {
        if (this.InputManager.HasGamepad && this.InputManager.RightShoulderPressed) {
            const float innerDeadzone = 0.0078125f; // 2^-7, retained from ZoomTracks.
            const float outerDeadzone = 0.95f;
            float vertical = DeadzoneFilter(this.InputManager.LeftStick.Y, innerDeadzone, outerDeadzone);
            if (vertical > 0f) {
                this.Zoom(0f, vertical);
            } else if (vertical < 0f) {
                this.Zoom(Mathf.Abs(vertical), 0f);
            }
            if (this.InputManager.ResetCameraZoom) {
                this.ResetZoom();
            }
        }
    }

    private static float DeadzoneFilter(float input, float innerDeadzone, float outerDeadzone) {
        float sign = Mathf.Sign(input);
        input = Mathf.Abs(input);
        if (input > outerDeadzone) {
            input = 1f;
        } else if (input < innerDeadzone) {
            input = 0f;
        } else {
            input = (input - innerDeadzone) / (outerDeadzone - innerDeadzone);
        }
        return sign * input;
    }

    // Retained for the source's alternate camera-control experiments. The active
    // input scheme only zooms; it does not invoke rotation or pan controls.
    private void RotateOffset(float amount) {
        this.RotationOffset += this.TimeManager.DeltaTime * CameraRotationSpeed * amount;
    }

    private void ResetRotationOffset() {
        this.RotationOffset = 0f;
    }

    private void Zoom(float zoomOut, float zoomIn) {
        this.OrthographicCameraSize += this.TimeManager.DeltaTime * CameraZoomSpeed * (zoomOut - zoomIn);
        this.OrthographicCameraSize = Mathf.Clamp(this.OrthographicCameraSize, MinOrthographicCameraSize, MaxOrthographicCameraSize);
    }

    public void ResetZoom() {
        this.OrthographicCameraSize = this.CameraFollowSettings.FollowsCarLocation
            ? this.DefaultFollowCameraSize : this.DefaultFixedCameraSize;
    }

    private void ValidateCameraParameters() {
        Require(this.CameraYawOffset.Position.IsEqualApprox(Vector3.Zero), "CameraYawOffset position must be zero.");
        Require(this.CameraYawOffset.Rotation.IsEqualApprox(Vector3.Zero), "CameraYawOffset rotation must be zero.");
        Require(this.CameraYawOffset.Scale.IsEqualApprox(Vector3.One), "CameraYawOffset scale must be one.");

        // Reflect Unity Z and point the native Godot camera along its local -Z.
        Require(this.CameraPanOffsetAndPitch.Position.IsEqualApprox(Vector3.Zero), "CameraPanOffsetAndPitch position must be zero.");
        Require(this.CameraPanOffsetAndPitch.RotationDegrees.IsEqualApprox(new Vector3(-45f, 0f, 0f)), "CameraPanOffsetAndPitch must have -45 degrees X pitch.");
        Require(this.CameraPanOffsetAndPitch.Scale.IsEqualApprox(Vector3.One), "CameraPanOffsetAndPitch scale must be one.");
        Require(this.Camera.Position.IsEqualApprox(new Vector3(0f, 0f, 500f)), "Camera local position must be (0, 0, 500).");
        Require(this.Camera.Rotation.IsEqualApprox(Vector3.Zero), "Camera local rotation must be zero.");
        Require(this.Camera.Scale.IsEqualApprox(Vector3.One), "Camera scale must be one.");
        Require(this.Camera.Projection == Camera3D.ProjectionType.Orthogonal, "Camera projection must be orthogonal.");
        Require(this.Camera.KeepAspect == Camera3D.KeepAspectEnum.Height, "Camera must preserve vertical size with KeepAspect=Height.");
        Require(IsValidSize(this.DefaultFixedCameraSize), "The fixed camera half-height is out of range.");
        Require(IsValidSize(this.DefaultFollowCameraSize), "FollowCameraSize is out of range.");
        Require(IsValidSize(this.OrthographicCameraSize), "The selected camera half-height is out of range.");
        Require(Mathf.IsEqualApprox(this.Camera.Near, 1f), "Camera Near must be 1.");
        Require(Mathf.IsEqualApprox(this.Camera.Far, 1000f), "Camera Far must be 1000.");

        Godot.Environment environment = this.Camera.Environment ?? this.Camera.GetWorld3D().Environment
            ?? throw new InvalidOperationException("The camera requires an Environment with a solid #404040 background.");
        Require(environment.BackgroundMode == Godot.Environment.BGMode.Color, "Environment background must use a solid color.");
        Require(environment.BackgroundColor.ToHtml(false).Equals("404040", StringComparison.OrdinalIgnoreCase), "Environment background color must be #404040.");
    }

    private static bool IsValidSize(float size) {
        return MinOrthographicCameraSize <= size && size <= MaxOrthographicCameraSize;
    }

    private static void Require(bool condition, string message) {
        if (!condition) {
            throw new InvalidOperationException(message);
        }
    }
}
