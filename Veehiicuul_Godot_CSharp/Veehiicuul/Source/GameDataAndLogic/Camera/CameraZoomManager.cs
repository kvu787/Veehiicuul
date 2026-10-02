using Godot;
using System;

namespace Veehiicuul_Godot_CSharp;

public sealed class CameraZoomManager {
    private const float MinOrthographicCameraSize = 1f;
    private const float MaxOrthographicCameraSize = 281.25f;
    private const float CameraZoomSpeed = 50f;

    private readonly TrackNodes TrackObjects;
    private readonly InputManager InputManager;
    private readonly CameraFollowManager CameraFollowManager;

    private readonly float DefaultFixedCameraSize;
    private readonly float DefaultFollowCameraSize;

    private float PreviousCameraSize;
    public float CameraSize { get; private set; }

    public CameraZoomManager(TrackNodes trackObjects, InputManager inputManager, CameraFollowManager cameraFollowManager, TrackSwitcher trackSwitcher) {
        ArgumentNullException.ThrowIfNull(trackObjects);
        ArgumentNullException.ThrowIfNull(inputManager);
        ArgumentNullException.ThrowIfNull(cameraFollowManager);
        ArgumentNullException.ThrowIfNull(trackSwitcher);

        this.TrackObjects = trackObjects;
        this.InputManager = inputManager;
        this.CameraFollowManager = cameraFollowManager;

        this.DefaultFixedCameraSize = this.TrackObjects.Camera.Size;
        this.DefaultFollowCameraSize = trackSwitcher.CurrentTrackJson.FollowCameraSize;

        this.PreviousCameraSize = -1f;
        this.CameraSize = this.CameraFollowManager.FollowsCarLocation ? this.DefaultFollowCameraSize : this.DefaultFixedCameraSize;

        this.ApplyInternalStateToCameraSize();
    }

    public void ApplyInternalStateToCameraSize() {
        if (this.CameraSize != this.PreviousCameraSize) {
            this.TrackObjects.Camera.Size = this.CameraSize;
            this.PreviousCameraSize = this.CameraSize;
        }
    }

    public void ReadInputAndZoom(double delta) {
        if (this.InputManager.CameraZoom != 0f) {
            this.CameraSize += (float)delta * CameraZoomSpeed * this.InputManager.CameraZoom;
            this.CameraSize = Mathf.Clamp(this.CameraSize, MinOrthographicCameraSize, MaxOrthographicCameraSize);
        }
    }

    public void ReadInputAndResetZoom() {
        if (this.InputManager.ResetCameraZoom || this.CameraFollowManager.FollowsCarLocationChanged) {
            this.CameraSize = this.CameraFollowManager.FollowsCarLocation ? this.DefaultFollowCameraSize : this.DefaultFixedCameraSize;
        }
    }
}
