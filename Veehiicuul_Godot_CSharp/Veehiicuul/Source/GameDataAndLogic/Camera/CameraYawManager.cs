using System;

namespace Veehiicuul_Godot_CSharp;

public sealed class CameraYawManager {
    public float Yaw => this.TrackObjects.CameraPanAndYaw.Rotation.Y;

    private readonly TrackNodes TrackObjects;

    public CameraYawManager(TrackNodes trackObjects) {
        ArgumentNullException.ThrowIfNull(trackObjects);
        this.TrackObjects = trackObjects;
    }
}
