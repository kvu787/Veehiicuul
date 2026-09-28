using System;

namespace Veehiicuul_Godot_CSharp;

public sealed class CameraYawManager {
    public float Yaw => this.TrackObjects.Camera.Rotation.Y;

    private readonly TrackObjects TrackObjects;

    public CameraYawManager(TrackObjects trackObjects) {
        ArgumentNullException.ThrowIfNull(trackObjects);
        this.TrackObjects = trackObjects;
    }
}
