using System;

namespace Veehiicuul_Godot_CSharp;

public sealed class CameraFollowManager {
    public bool FollowsCarLocation { get; private set; }

    public bool FollowsCarLocationChanged { get; private set; }

    private readonly InputManager InputManager;

    public CameraFollowManager(InputManager inputManager, TrackSwitcher trackSwitcher) {
        ArgumentNullException.ThrowIfNull(inputManager);
        ArgumentNullException.ThrowIfNull(trackSwitcher);

        this.InputManager = inputManager;
        this.FollowsCarLocation = trackSwitcher.CurrentTrackJson.CameraFollowsCarLocation;
    }

    public void ReadInputAndUpdateFollowSetting() {
        if (this.InputManager.ToggleBetweenFixedAndFollowCamera) {
            this.FollowsCarLocation = !this.FollowsCarLocation;
            this.FollowsCarLocationChanged = true;
        }
    }

    public void EndFrame() {
        this.FollowsCarLocationChanged = false;
    }
}
