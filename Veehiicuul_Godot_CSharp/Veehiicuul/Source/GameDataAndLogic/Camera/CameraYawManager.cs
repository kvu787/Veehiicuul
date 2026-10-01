using System;

namespace Veehiicuul_Godot_CSharp;

public sealed class CameraYawManager {
    /// <summary>Camera pivot yaw relative to the track, in native Godot radians, independent of its camera children.</summary>
    public float Yaw => this.TrackObjects.CameraPanAndYaw.Rotation.Y;

    private readonly TrackNodes TrackObjects;

    public CameraYawManager(TrackNodes trackObjects) {
        ArgumentNullException.ThrowIfNull(trackObjects);
        this.TrackObjects = trackObjects;
    }
}
