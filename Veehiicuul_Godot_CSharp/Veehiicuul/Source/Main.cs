using Godot;
using System;

namespace Veehiicuul_Godot_CSharp;

public class Main {
    private static readonly string[] TrackNames = ["Ribeye"];
    private const int InitialTrackIndex = 0;

    private const double CarControlTimeoutSeconds = 0.35;
    private DateTime CarControlTimeoutStart = DateTime.MinValue;

    private InputManager InputManager = null!;
    private TrackSwitcher TrackSwitcher = null!;
    private CameraFollowManager CameraFollowManager = null!;
    private TrackObjects TrackObjects = null!;
    private CameraZoomManager CameraZoomManager = null!;
    private CameraPanManager CameraPanManager = null!;
    private CameraYawManager CameraYawManager = null!;
    private CarSwitcher CarSwitcher = null!;
    private CarStateManager CarStateManager = null!;
    private CollisionManager CollisionManager = null!;
    private bool IsReadyDone;
    private static Node MainNode = null!;

    public Main(Node mainNode) {
        ArgumentNullException.ThrowIfNull(mainNode);
        MainNode = mainNode;
    }

    public static void LogExceptionAndQuit(Exception exception) {
        GD.PrintErr(exception.ToString());
        Quit(1);
    }

    public static void Quit(int exitCode) {
        MainNode.SetProcess(false);
        MainNode.GetTree().Quit(exitCode);
    }

    public void Ready() {
        PrintInfoUtility.PrintDisplayInfo(MainNode.GetViewport());
        PrintInfoUtility.PrintGraphicsInfo();
        this.InputManager = new InputManager();
        this.TrackSwitcher = new TrackSwitcher(MainNode, this.InputManager, TrackNames, InitialTrackIndex);
        this.InitializeTrack();
        this.IsReadyDone = true;
    }

    private void InitializeTrack() {
        this.CameraFollowManager = new CameraFollowManager(this.InputManager, this.TrackSwitcher);
        this.TrackObjects = new TrackObjects(this.TrackSwitcher);
        this.CameraYawManager = new CameraYawManager(this.TrackObjects);
        this.CameraZoomManager = new CameraZoomManager(this.TrackObjects, this.InputManager, this.CameraFollowManager, this.TrackSwitcher);
        this.CarSwitcher = new CarSwitcher(this.InputManager, this.TrackSwitcher);
        this.CarStateManager = new CarStateManager(this.CarSwitcher, this.CameraYawManager, this.InputManager, this.TrackObjects);
        this.CameraPanManager = new CameraPanManager(this.TrackObjects, this.CameraFollowManager, this.CarStateManager);
        this.CollisionManager = new CollisionManager(this.TrackSwitcher.CurrentTrackName, this.CarSwitcher);
    }

    public void Process(double delta) {
        if (!this.IsReadyDone) {
            throw new InvalidOperationException("A frame ran before initialization completed.");
        }

        this.InputManager.UpdateInputs();

        if (this.InputManager.QuitGame) {
            Quit(0);
            return;
        }

        bool wasTrackSwitched = this.TrackSwitcher.ReadInputAndSwitchTracks();
        if (wasTrackSwitched) {
            this.InitializeTrack();
        } else {
            if (this.CarSwitcher.ReadInputAndSwitchCar()
                || this.InputManager.ResetCar
                || this.CollisionManager.IsCarColliding(this.CarStateManager.Position, this.CarStateManager.Rotation)) {
                this.CarControlTimeoutStart = DateTime.Now;
                this.CarStateManager.Reset_PositionRotationVelocity();
            }

            this.CameraFollowManager.ReadInputAndUpdateFollowSetting();
            this.CameraZoomManager.ReadInputAndZoom(delta);
            this.CameraZoomManager.ReadInputAndResetZoom();

            if (!this.InCarControlTimeout()) {
                this.CarStateManager.ReadInputAndUpdateState(delta);
            }

            this.CameraPanManager.Apply();
            this.CameraZoomManager.Apply();
            this.CarStateManager.Apply();

            this.CameraFollowManager.EndFrame();
        }

        if (wasTrackSwitched) {
            GarbageCollectionUtility.ForceGarbageCollection();
        }

        //--------------------------------------------------

        //this.InputManager.UpdateInputs();
        //this.CarControlTimeoutRemaining = Math.Max(0.0, this.CarControlTimeoutRemaining - delta);
        //if (this.InputManager.QuitGame) {
        //    this.Quit(0);
        //    return;
        //}
        //if (this.InputManager.ToggleFullscreen) {
        //    Window window = this.MainNode.GetWindow();
        //    window.Mode = window.Mode == Window.ModeEnum.Fullscreen
        //        ? Window.ModeEnum.Windowed : Window.ModeEnum.Fullscreen;
        //}
        //bool switchedTrack = this.TrackSwitcher.ReadInputAndSwitchTracks();
        //if (switchedTrack) {
        //    this.InitializeTrack();
        //} else {
        //    // As in ZoomTracks, show the collision frame, then reset and skip car input.
        //    bool resetCar = this.InputManager.ResetCar || this.CollisionManager.IsCarColliding();
        //    if (resetCar) {
        //        this.CarStateManager.Reset_PositionRotationVelocity();
        //        this.CarControlTimeoutRemaining = CarControlTimeoutSeconds;
        //    }
        //    this.CameraController.ReadInputAndChangeCameraSettings(delta);
        //    this.CameraPivotManager.ReadInputAndToggle();
        //    if (this.CarSwitcher.ReadInputAndSwitchCar()) {
        //        this.CarStateManager.Reset_PositionRotationVelocity();
        //        this.CarControlTimeoutRemaining = CarControlTimeoutSeconds;
        //    } else if (!resetCar && this.CarControlTimeoutRemaining <= 0.0) {
        //        this.CarStateManager.ReadInputAndUpdateState(delta);
        //    }
        //}
        //this.CarStateManager.ApplyStateToGameObject();
        //this.CameraController.Update();
        //this.CameraPivotManager.UpdateCameraPivot();
        //if (switchedTrack) {
        //    GarbageCollectionUtility.ForceGarbageCollection();
        //}
    }

    private bool InCarControlTimeout() {
        return (DateTime.Now - this.CarControlTimeoutStart).TotalSeconds <= CarControlTimeoutSeconds;
    }
}
