using System;

namespace Veehiicuul_Godot_CSharp;

public sealed class CameraYawManager {
    /// <summary>Camera pivot world yaw in clockwise radians, independent of its camera children.</summary>
    public float Yaw => this.TrackObjects.CameraPanAndYaw.Rotation.Y;

    private readonly TrackObjects TrackObjects;

    public CameraYawManager(TrackObjects trackObjects) {
        ArgumentNullException.ThrowIfNull(trackObjects);
        this.TrackObjects = trackObjects;
    }
}
