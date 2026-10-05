using Godot;
using System;
using System.Diagnostics;

namespace Veehiicuul_Godot_CSharp;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1001:Types that own disposable fields should be disposable", Justification = "TODO")]
public class Main {
    private const int InitialTrackIndex = 0;
    private static readonly string[] TrackNames = ["Ribeye"];

    private const double CarControlTimeoutSeconds = 0.35;
    private DateTime CarControlTimeoutStart = DateTime.MinValue;
    private bool IsReadyDone;
    private readonly Node MainNode;

    private DigitalInputMap DigitalInputMap = null!;
    private InputManager InputManager = null!;
    private TrackSwitcher TrackSwitcher = null!;
    private CameraFollowManager CameraFollowManager = null!;
    private TrackNodes TrackObjects = null!;
    private CameraZoomManager CameraZoomManager = null!;
    private CameraPanManager CameraPanManager = null!;
    private CameraYawManager CameraYawManager = null!;
    private CarSwitcher CarSwitcher = null!;
    private CarStateManager CarStateManager = null!;
    private CollisionManager CollisionManager = null!;

    public Main(Node mainNode) {
        ArgumentNullException.ThrowIfNull(mainNode);
        this.MainNode = mainNode;
    }

    public void LogExceptionAndQuit(Exception exception) {
        GD.PrintErr(exception.ToString());
        this.Quit(1);
    }

    public void Quit(int exitCode) {
        this.MainNode.SetProcess(false);
        this.MainNode.GetTree().Quit(exitCode);
    }

    public void Ready() {
        GD.Print($"ProcessId='{System.Environment.ProcessId}', QpcFrequency='{Stopwatch.Frequency}'");
        PrintInfoUtility.PrintDisplayInfo(this.MainNode.GetViewport());
        PrintInfoUtility.PrintGraphicsInfo();
        this.DigitalInputMap = new DigitalInputMap();
        this.InputManager = new InputManager(this.DigitalInputMap);
        this.TrackSwitcher = new TrackSwitcher(this.MainNode, this.InputManager, TrackNames, InitialTrackIndex);
        this.InitializeTrack();
        this.IsReadyDone = true;
    }

    private void InitializeTrack() {
        this.CameraFollowManager = new CameraFollowManager(this.InputManager, this.TrackSwitcher);
        this.TrackObjects = new TrackNodes(this.TrackSwitcher);
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

        if (Input.IsActionJustPressed(this.DigitalInputMap.JoyButtonLeftStick)) {
            long eventQpc = Stopwatch.GetTimestamp();
            ulong frameNumber = Engine.GetProcessFrames();
            GD.Print($"QPC='{eventQpc}', Engine.GetProcessFrames()='{frameNumber}', Event='Stutter observed by player. Account for player reaction time and latency.'");
        }

        if (this.InputManager.QuitGame) {
            this.Quit(0);
            return;
        }

        if (this.TrackSwitcher.ReadInputAndSwitchTracks()) {
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
                this.CarStateManager.ReadAccelerationAndBrakeInput_AndUpdateVelocity(delta);
                this.CarStateManager.UpdatePosition(delta);
            }

            this.CameraPanManager.ApplyInternalStateToCameraPosition();
            this.CameraZoomManager.ApplyInternalStateToCameraSize();
            this.CarStateManager.ApplyInternalStateToCarTransform();

            this.CameraFollowManager.ResetEvents();
        }

        //GC.Collect();
        //if (this.RunInitialForceGarbageCollection) {
        //    GarbageCollectionUtility.ForceGarbageCollection();
        //    this.RunInitialForceGarbageCollection = false;
        //} else if (wasTrackSwitched || wasCarReset) {
        //    GarbageCollectionUtility.ForceGarbageCollection();
        //}
    }

    private bool InCarControlTimeout() {
        return (DateTime.Now - this.CarControlTimeoutStart).TotalSeconds <= CarControlTimeoutSeconds;
    }
}
