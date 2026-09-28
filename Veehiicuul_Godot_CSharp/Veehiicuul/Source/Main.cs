using Godot;
using System;
using System.Threading;

namespace Veehiicuul_Godot_CSharp;

public class Main(Node mainNode) {
    private static readonly string[] TrackNames = ["Ribeye"];
    private const int InitialTrackIndex = 0;
    private const double CarControlTimeoutSeconds = 0.35;

    private CameraFollowSettings CameraFollowSettings = null!;
    private TrackObjects TrackObjects = null!;
    private InputManager InputManager = null!;
    private TrackSwitcher TrackSwitcher = null!;
    private CameraController CameraController = null!;
    private CarSwitcher CarSwitcher = null!;
    private CarState CarState = null!;
    private CollisionManager CollisionManager = null!;
    private CameraPivotManager CameraPivotManager = null!;
    private double CarControlTimeoutRemaining;
    private bool Initialized;
    private readonly Node MainNode = mainNode;

    private void LogExceptionAndQuit(Exception exception) {
        GD.PrintErr(exception.ToString());
        this.Quit(1);
    }

    private void Quit(int exitCode) {
        this.MainNode.SetProcess(false);
        this.MainNode.GetTree().Quit(exitCode);
    }

    public void Ready() {
        try {
            this.InitializeGame();
        } catch (Exception exception) {
            this.LogExceptionAndQuit(exception);
        }
    }

    public void Process(double delta) {
        try {
            if (!this.Initialized) {
                throw new InvalidOperationException("A frame ran before initialization completed.");
            }
            this.UpdateGame(delta);
        } catch (Exception exception) {
            this.LogExceptionAndQuit(exception);
        }
    }

    private void InitializeGame() {
        PrintInfoUtility.PrintDisplayInfo(this.MainNode.GetViewport());
        PrintInfoUtility.PrintGraphicsInfo();
        this.InputManager = new InputManager();
        this.TrackSwitcher = new TrackSwitcher(this.MainNode, this.InputManager, TrackNames, InitialTrackIndex);
        this.InitializeTrack();
        this.Initialized = true;
    }

    private void InitializeTrack() {
        this.CameraFollowSettings = new CameraFollowSettings(this.TrackSwitcher.CurrentTrackJson);
        this.TrackObjects = new TrackObjects(this.TrackSwitcher.CurrentTrackScene, this.TrackSwitcher.CurrentTrackJson);
        GD.Print($"PlaceholderCar position: {this.TrackObjects.PlaceholderCarTransform.Position.X}, {this.TrackObjects.PlaceholderCarTransform.Position.Y}, {this.TrackObjects.PlaceholderCarTransform.Position.Z}");
        this.CameraController = new CameraController(this.TrackObjects, this.CameraFollowSettings, this.TrackSwitcher.CurrentTrackJson, this.InputManager);
        GD.Print($"Camera size: {this.CameraController.CameraSize}");
        this.CarSwitcher = new CarSwitcher(this.TrackSwitcher.CurrentTrackScene, this.TrackSwitcher.CurrentTrackJson, this.InputManager);
        this.CarState = new CarState(this.TrackObjects.PlaceholderCarTransform, this.CarSwitcher, this.CameraController, this.InputManager);
        this.CarState.ApplyStateToGameObject();

        //throw new NotImplementedException();
        //this.CameraPivotManager = new CameraPivotManager(this.TrackSwitcher.CurrentTrackScene, this.CameraFollowSettings, this.CameraController, this.CarState, this.InputManager);
        //this.CollisionManager = new CollisionManager(this.TrackSwitcher.CurrentTrackName, this.CarSwitcher);
    }

    public void UpdateGame(double delta) {
        Thread.Sleep(TimeSpan.FromSeconds(1));
        GD.Print(delta);
        return;

        this.InputManager.UpdateInputs();
        this.CarControlTimeoutRemaining = Math.Max(0.0, this.CarControlTimeoutRemaining - delta);
        if (this.InputManager.QuitGame) {
            this.Quit(0);
            return;
        }
        if (this.InputManager.ToggleFullscreen) {
            Window window = this.MainNode.GetWindow();
            window.Mode = window.Mode == Window.ModeEnum.Fullscreen
                ? Window.ModeEnum.Windowed : Window.ModeEnum.Fullscreen;
        }
        bool switchedTrack = this.TrackSwitcher.ReadInputAndSwitchTracks();
        if (switchedTrack) {
            this.InitializeTrack();
        } else {
            // As in ZoomTracks, show the collision frame, then reset and skip car input.
            bool resetCar = this.InputManager.ResetCar || this.CollisionManager.IsCarColliding();
            if (resetCar) {
                this.CarState.Reset_PositionRotationVelocity();
                this.CarControlTimeoutRemaining = CarControlTimeoutSeconds;
            }
            this.CameraController.ReadInputAndChangeCameraSettings(delta);
            this.CameraPivotManager.ReadInputAndToggle();
            if (this.CarSwitcher.ReadInputAndSwitchCar()) {
                this.CarState.Reset_PositionRotationVelocity();
                this.CarControlTimeoutRemaining = CarControlTimeoutSeconds;
            } else if (!resetCar && this.CarControlTimeoutRemaining <= 0.0) {
                this.CarState.ReadInputAndUpdateState(delta);
            }
        }
        this.CarState.ApplyStateToGameObject();
        this.CameraController.Update();
        this.CameraPivotManager.UpdateCameraPivot();
        if (switchedTrack) {
            GarbageCollectionUtility.ForceGarbageCollection();
        }
    }
}
