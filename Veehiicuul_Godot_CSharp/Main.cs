using Godot;
using System;

namespace Veehiicuul_Godot_CSharp;

/// <summary>The application's only startup entry point and frame callback.</summary>
public partial class Main : Node {
    private const string StartupStopMessage = "Intentional startup stop: the ZoomTracks learning port is not initialized.";
    private const double CarControlTimeoutSeconds = 0.35;
    private StutterLogger StutterLogger = null!;
    private TimeManager TimeManager = null!;
    private InputManager InputManager = null!;
    private TrackSwitcher TrackSwitcher = null!;
    private CameraController CameraController = null!;
    private GraphicsSettingsManager GraphicsSettingsManager = null!;
    private CarSwitcher CarSwitcher = null!;
    private CarState CarState = null!;
    private CollisionManager2 CollisionManager = null!;
    private CameraPivotManager CameraPivotManager = null!;
    private UiManager UiManager = null!;
    private Node UiRoot = null!;
    private double CarControlTimeoutRemaining;
    private bool Initialized;

    public override void _Ready() {
        try {
            throw new InvalidOperationException(StartupStopMessage);

            // Deliberately unreachable for this learning port. Remove the throw above
            // when you want to step through initialization (scenes/assets are still needed).
#pragma warning disable CS0162 // The requested startup stop deliberately precedes all initialization.
            this.InitializeGame();
#pragma warning restore CS0162
        } catch (Exception exception) {
            GD.PushError(exception.ToString());
            this.SetProcess(false);
            this.GetTree().Quit(1);
        }
    }

    public override void _Process(double delta) {
        try {
            if (!this.Initialized) {
                throw new InvalidOperationException("A frame ran before initialization completed.");
            }
            this.UpdateGame(delta);
        } catch (Exception exception) {
            GD.PushError(exception.ToString());
            this.SetProcess(false);
            this.GetTree().Quit(1);
        }
    }

    private void InitializeGame() {
        GraphicsSettingsManager.ConfigureSessionGraphicsSettings(this.GetViewport());
        PrintInfoUtility.PrintDisplayInfo(this.GetViewport());
        PrintInfoUtility.PrintGraphicsInfo();
        this.TimeManager = CreateTimeManager();
        this.StutterLogger = new StutterLogger(SessionLog.CreateDirectory(), this.TimeManager);
        this.InputManager = new InputManager();
        this.UiRoot = SceneLoadingUtility.LoadAndAttach<Node>(this, "res://Scenes/Ui.tscn");
        string[] trackNames = ["Basic", "Track001", "Track002", "Track003", "Track004", "Track005"];
        this.TrackSwitcher = new TrackSwitcher(this, this.InputManager, trackNames, 5);
        this.InitializeTrack();
        this.Initialized = true;
        GD.Print("Game initialization completed.");
    }

    private void InitializeTrack() {
        CameraFollowSettings followSettings = new(this.TrackSwitcher.CurrentTrackJson);
        TrackObjects trackObjects = new(this.TrackSwitcher.CurrentTrackScene);
        this.CameraController = new CameraController(this.TrackSwitcher.CurrentTrackScene, followSettings,
            this.TrackSwitcher.CurrentTrackJson, this.InputManager, this.TimeManager);
        this.GraphicsSettingsManager = new GraphicsSettingsManager(this.GetViewport(), this.InputManager);
        this.CarSwitcher = new CarSwitcher(this.TrackSwitcher.CurrentTrackScene,
            this.TrackSwitcher.CurrentTrackJson, this.InputManager);
        this.CarState = new CarState(trackObjects.PlaceholderCarTransform, this.CarSwitcher,
            this.CameraController, this.InputManager, this.TimeManager);
        this.CarState.ApplyStateToGameObject();
        this.CameraPivotManager = new CameraPivotManager(this.TrackSwitcher.CurrentTrackScene, followSettings,
            this.CameraController, this.CarState, this.InputManager);
        this.CollisionManager = new CollisionManager2(this.TrackSwitcher.CurrentTrackName, this.CarSwitcher);
        this.UiManager = new UiManager(this.UiRoot, this.CameraController, this.TimeManager);
    }

    // Ordinary synchronous method, called only by _Process; no second engine callback.
    private void UpdateGame(double delta) {
        this.StutterLogger.Update();
        this.TimeManager.Update(delta);
        this.InputManager.UpdateInputs();
        this.CarControlTimeoutRemaining = Math.Max(0.0, this.CarControlTimeoutRemaining - delta);
        if (this.InputManager.QuitGame) {
            this.StutterLogger.Dispose();
            this.GetTree().Quit();
            this.SetProcess(false);
            return;
        }
        if (this.InputManager.InsertStutterLogSpacer) {
            this.StutterLogger.InsertSpacer();
        }
        if (this.InputManager.ToggleBetweenBorderlessAndExclusiveFullScreen) {
            DisplayServer.WindowMode mode = DisplayServer.WindowGetMode();
            DisplayServer.WindowSetMode(mode switch {
                DisplayServer.WindowMode.Fullscreen => DisplayServer.WindowMode.ExclusiveFullscreen,
                DisplayServer.WindowMode.ExclusiveFullscreen => DisplayServer.WindowMode.Fullscreen,
                DisplayServer.WindowMode.Windowed or DisplayServer.WindowMode.Minimized or
                    DisplayServer.WindowMode.Maximized => throw new InvalidOperationException($"Cannot toggle fullscreen from {mode}."),
                _ => throw new InvalidOperationException($"Cannot toggle fullscreen from {mode}.")
            });
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
            this.GraphicsSettingsManager.ReadInputAndUpdate();
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
        this.UiManager.UpdateUi();
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
