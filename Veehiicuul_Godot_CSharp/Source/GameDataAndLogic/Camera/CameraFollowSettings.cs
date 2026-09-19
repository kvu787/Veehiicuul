using System;

namespace Veehiicuul_Godot_CSharp;

public sealed class CameraFollowSettings {
    public CameraFollowSettings(TrackJson trackJson) {
        ArgumentNullException.ThrowIfNull(trackJson);
        this.FollowsCarLocation = trackJson.CameraFollowsCarLocation;
    }

    public bool FollowsCarLocation { get; set; }
}
