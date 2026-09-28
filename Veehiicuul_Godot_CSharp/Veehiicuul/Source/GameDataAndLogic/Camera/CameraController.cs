using Godot;
using System;

namespace Veehiicuul_Godot_CSharp;

/// <summary>Controls the source camera rig through ordinary calls from Main.</summary>
public sealed class CameraController {
    private const float MinOrthographicCameraSize = 1f;
    private const float MaxOrthographicCameraSize = 281.25f;

    private const float CameraZoomSpeed = 50f;

    private TrackObjects TrackObjects { get; }
    private CameraFollowSettings CameraFollowSettings { get; }
    private InputManager InputManager { get; }

    private float DefaultFixedCameraSize { get; }
    private float DefaultFollowCameraSize { get; }
    public float CameraSize { get; set; }

    /// <summary>Clockwise yaw from Godot forward (-Z), in degrees.</summary>
    public float CameraYaw => -Mathf.RadToDeg(this.TrackObjects.Camera.GlobalRotation.Y);

    public CameraController(TrackObjects trackObjects, CameraFollowSettings cameraFollowSettings, TrackJson trackJson, InputManager inputManager) {
        ArgumentNullException.ThrowIfNull(trackObjects);
        ArgumentNullException.ThrowIfNull(cameraFollowSettings);
        ArgumentNullException.ThrowIfNull(trackJson);
        ArgumentNullException.ThrowIfNull(inputManager);

        this.TrackObjects = trackObjects;
        this.CameraFollowSettings = cameraFollowSettings;
        this.InputManager = inputManager;

        this.DefaultFixedCameraSize = this.TrackObjects.Camera.Size;
        this.DefaultFollowCameraSize = trackJson.FollowCameraSize;
        this.ResetZoom();
    }

    public void Update() {
        this.TrackObjects.Camera.Size = this.CameraSize;
    }

    public void ReadInputAndChangeCameraSettings(double delta) {
        if (this.InputManager.CameraZoom != 0f) {
            this.Zoom(this.InputManager.CameraZoom, delta);
        }

        if (this.InputManager.ResetCameraZoom) {
            this.ResetZoom();
        }
    }

    private void Zoom(float zoom, double delta) {
        this.CameraSize += (float)delta * CameraZoomSpeed * zoom;
        this.CameraSize = Mathf.Clamp(this.CameraSize, MinOrthographicCameraSize, MaxOrthographicCameraSize);
    }

    public void ResetZoom() {
        this.CameraSize = this.CameraFollowSettings.FollowsCarLocation ? this.DefaultFollowCameraSize : this.DefaultFixedCameraSize;
    }
}
