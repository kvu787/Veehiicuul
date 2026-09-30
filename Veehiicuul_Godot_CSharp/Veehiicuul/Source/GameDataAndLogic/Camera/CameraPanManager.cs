using Godot;
using System;

namespace Veehiicuul_Godot_CSharp;

public sealed class CameraPanManager {
    private readonly TrackObjects TrackObjects;
    private readonly CameraFollowManager CameraFollowManager;
    private readonly CarStateManager CarStateManager;

    private readonly Vector3 OriginalCameraPosition;

    public CameraPanManager(TrackObjects trackObjects, CameraFollowManager cameraFollowManager, CarStateManager carStateManager) {
        ArgumentNullException.ThrowIfNull(trackObjects);
        ArgumentNullException.ThrowIfNull(cameraFollowManager);
        ArgumentNullException.ThrowIfNull(carStateManager);

        this.TrackObjects = trackObjects;
        this.CameraFollowManager = cameraFollowManager;
        this.CarStateManager = carStateManager;

        this.OriginalCameraPosition = this.TrackObjects.CameraPanAndYaw.Position;

        this.ApplyInternalStateToCameraPosition();
    }

    public void ApplyInternalStateToCameraPosition() {
        this.TrackObjects.CameraPanAndYaw.Position = this.CameraFollowManager.FollowsCarLocation ? this.CarStateManager.Position : this.OriginalCameraPosition;
    }
}
