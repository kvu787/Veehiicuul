using System;

namespace Veehiicuul_Godot_CSharp;

public sealed class CameraFollowSettings {
    public CameraFollowSettings(TrackSwitcher trackSwitcher) {
        ArgumentNullException.ThrowIfNull(trackSwitcher);
        this.FollowsCarLocation = trackSwitcher.CurrentTrackJson.CameraFollowsCarLocation;
    }

    public bool FollowsCarLocation { get; set; }
}
