using Godot;
using System;

namespace Veehiicuul_Godot_CSharp;

/// <summary>The application's only startup entry point and frame callback.</summary>
public partial class Main : Node {
    private const double CarControlTimeoutSeconds = 0.35;
    private TimeManager TimeManager = null!;
    private InputManager InputManager = null!;
    private TrackSwitcher TrackSwitcher = null!;
    private CameraController CameraController = null!;
    private CarSwitcher CarSwitcher = null!;
    private CarState CarState = null!;
    private CollisionManager CollisionManager = null!;
    private CameraPivotManager CameraPivotManager = null!;
    private double CarControlTimeoutRemaining;
    private bool Initialized;

    private void LogExceptionAndQuit(Exception exception) {
        GD.PushError(exception.ToString());
        this.SetProcess(false);
        this.GetTree().Quit(1);
    }

    public override void _Ready() {
        try {
            this.InitializeGame();
        } catch (Exception exception) {
            this.LogExceptionAndQuit(exception);
        }
    }

    public override void _Process(double delta) {
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
        PrintInfoUtility.PrintDisplayInfo(this.GetViewport());
        PrintInfoUtility.PrintGraphicsInfo();
        this.TimeManager = CreateTimeManager();
        this.InputManager = new InputManager();
        string[] trackNames = ["Ribeye"];
        this.TrackSwitcher = new TrackSwitcher(this, this.InputManager, trackNames, 0);
        this.InitializeTrack();
        this.Initialized = true;
        GD.Print("Game initialization completed.");
    }

    private void InitializeTrack() {
        CameraFollowSettings followSettings = new(this.TrackSwitcher.CurrentTrackJson);
        TrackObjects trackObjects = new(this.TrackSwitcher.CurrentTrackScene);
        this.CameraController = new CameraController(this.TrackSwitcher.CurrentTrackScene, followSettings,
            this.TrackSwitcher.CurrentTrackJson, this.InputManager, this.TimeManager);
        this.CarSwitcher = new CarSwitcher(this.TrackSwitcher.CurrentTrackScene,
            this.TrackSwitcher.CurrentTrackJson, this.InputManager);
        this.CarState = new CarState(trackObjects.PlaceholderCarTransform, this.CarSwitcher,
            this.CameraController, this.InputManager, this.TimeManager);
        this.CarState.ApplyStateToGameObject();
        this.CameraPivotManager = new CameraPivotManager(this.TrackSwitcher.CurrentTrackScene, followSettings,
            this.CameraController, this.CarState, this.InputManager);
        this.CollisionManager = new CollisionManager(this.TrackSwitcher.CurrentTrackName, this.CarSwitcher);
    }

    // Ordinary synchronous method, called only by _Process; no second engine callback.
    private void UpdateGame(double delta) {
        this.TimeManager.Update(delta);
        this.InputManager.UpdateInputs();
        this.CarControlTimeoutRemaining = Math.Max(0.0, this.CarControlTimeoutRemaining - delta);
        if (this.InputManager.QuitGame) {
            this.GetTree().Quit();
            this.SetProcess(false);
            return;
        }
        if (this.InputManager.ToggleFullscreen) {
            Window window = this.GetWindow();
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
            this.CameraController.ReadInputAndChangeCameraSettings();
            this.CameraPivotManager.ReadInputAndToggle();
            if (this.CarSwitcher.ReadInputAndSwitchCar()) {
                this.CarState.Reset_PositionRotationVelocity();
                this.CarControlTimeoutRemaining = CarControlTimeoutSeconds;
            } else if (!resetCar && this.CarControlTimeoutRemaining <= 0.0) {
                this.CarState.ReadInputAndUpdateState();
            }
        }
        this.CarState.ApplyStateToGameObject();
        this.CameraController.Update();
        this.CameraPivotManager.UpdateCameraPivot();
        if (switchedTrack) {
            GarbageCollectionUtility.ForceGarbageCollection();
        }
    }

    private static TimeManager CreateTimeManager() {
        string[] arguments = OS.GetCmdlineUserArgs();
        int index = Array.IndexOf(arguments, "-refreshRate");
        if (index < 0) {
            return new TimeManager(null, true);
        }
        if (index + 1 >= arguments.Length) {
            throw new ArgumentException("No value found for -refreshRate.");
        }
        float refreshRate = ParseUtility.ParseFloat(arguments[index + 1]);
        if (!float.IsFinite(refreshRate)) {
            throw new ArgumentException("The refresh rate must be finite.");
        }
        return refreshRate > 0f ? new TimeManager(refreshRate, false) : new TimeManager(null, true);
    }
}
