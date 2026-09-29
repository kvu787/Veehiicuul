using Godot;
using System;

namespace Veehiicuul_Godot_CSharp;

internal static class CameraStateVerification {
    public static void Run() {
        TrackObjects track = new();
        track.CameraPanAndYaw.Position = new Vector3(8f, 0f, -3f);
        track.PlaceholderCarNode.Position = new Vector3(3f, 0f, 5f);
        Vector3 fixedPosition = track.CameraPanAndYaw.Position;
        TrackSwitcher tracks = new();
        InputManager input = new();
        CameraFollowManager follow = new(input, tracks);
        CameraZoomManager zoom = new(track, input, follow, tracks);
        CarStateManager car = new(new CarSwitcher(), new CameraYawManager(track), input, track);
        CameraPanManager pan = new(track, follow, car);
        Require(track.Camera.Size == 100f && track.CameraPanAndYaw.Position == fixedPosition,
            "Fixed mode must use the authored camera pose and size.");

        input.CameraZoom = 1f;
        zoom.ReadInputAndZoom(0.2);
        zoom.Apply();
        Require(track.Camera.Size == 110f, "Zoom must integrate the frame delta.");

        input.ToggleBetweenFixedAndFollowCamera = true;
        follow.ReadInputAndUpdateFollowSetting();
        zoom.ReadInputAndResetZoom();
        zoom.Apply();
        pan.Apply();
        Require(follow.FollowsCarLocationChanged && follow.FollowsCarLocation
            && track.Camera.Size == 60f && track.CameraPanAndYaw.Position == car.Position,
            "Entering follow mode must restore its default size and follow the car in track space.");
        follow.EndFrame();

        input.ToggleBetweenFixedAndFollowCamera = false;
        follow.ReadInputAndUpdateFollowSetting();
        zoom.ReadInputAndZoom(0.1);
        zoom.ReadInputAndResetZoom();
        zoom.Apply();
        Require(!follow.FollowsCarLocationChanged && track.Camera.Size == 65f,
            "Follow-mode zoom must remain adjustable on subsequent frames.");

        input.ResetCameraZoom = true;
        zoom.ReadInputAndResetZoom();
        zoom.Apply();
        Require(track.Camera.Size == 60f, "Explicit reset must restore follow zoom.");
        input.ResetCameraZoom = false;
        input.ToggleBetweenFixedAndFollowCamera = true;
        follow.ReadInputAndUpdateFollowSetting();
        zoom.ReadInputAndResetZoom();
        zoom.Apply();
        pan.Apply();
        Require(!follow.FollowsCarLocation && track.Camera.Size == 100f
            && track.CameraPanAndYaw.Position == fixedPosition,
            "Returning to fixed mode must restore its original position and size.");
        follow.EndFrame();

        input.CameraZoom = -1f;
        zoom.ReadInputAndZoom(100.0);
        zoom.Apply();
        Require(track.Camera.Size == 1f, "Zoom must clamp at the minimum size.");
        input.CameraZoom = 1f;
        zoom.ReadInputAndZoom(100.0);
        zoom.Apply();
        Require(track.Camera.Size == 281.25f, "Zoom must clamp at the maximum size.");
        input.ResetCameraZoom = true;
        zoom.ReadInputAndResetZoom();
        zoom.Apply();
        Require(track.Camera.Size == 100f, "Explicit reset must restore fixed zoom.");

        tracks.CurrentTrackJson.CameraFollowsCarLocation = true;
        CameraFollowManager initialFollow = new(input, tracks);
        CameraZoomManager initialZoom = new(track, input, initialFollow, tracks);
        Require(initialFollow.FollowsCarLocation && !initialFollow.FollowsCarLocationChanged
            && initialZoom.CameraSize == 60f && track.Camera.Size == 60f,
            "A track configured to start in follow mode must apply follow zoom immediately.");
        Console.WriteLine("Passed camera follow, pan, zoom, reset, frame-boundary, and clamp checks.");
    }

    private static void Require(bool condition, string message) {
        if (!condition) {
            throw new InvalidOperationException(message);
        }
    }
}
