using Godot;
using System;
using System.Globalization;
using System.IO;
using System.Text.Json;

namespace InputLatencyGodot;

public partial class InputLatency : Node3D {
    private static readonly JsonSerializerOptions SessionSerializerOptions = new() { WriteIndented = true };
    private readonly InputState _state = new();
    private MeshInstance3D _leftSphere = null!;
    private MeshInstance3D _rightSphere = null!;
    private readonly StandardMaterial3D _sphereMaterial = new() { Roughness = 0.65f };
    private InputDisplay _display = null!;
    private string _logDirectory = "";
    private bool _verify;
    private bool _closing;

    public override void _Ready() {
        this._verify = Array.IndexOf(OS.GetCmdlineUserArgs(), "--self-test") >= 0;
        this.ConfigureWindow();
        Input.UseAccumulatedInput = false;
        Input.MouseMode = Input.MouseModeEnum.Visible;
        this.GetTree().AutoAcceptQuit = false;
        this.GetWindow().CloseRequested += this.Close;
        Input.JoyConnectionChanged += this.OnGamepadConnectionChanged;
        foreach (int device in Input.GetConnectedJoypads()) {
            this._state.SetConnection(device, true);
        }

        Camera3D camera = new() { Projection = Camera3D.ProjectionType.Orthogonal, Size = 7.2f, Position = new Vector3(0, 0, 10), Current = true };
        this.AddChild(camera);
        this.AddChild(new DirectionalLight3D { RotationDegrees = new Vector3(-25, -30, 0), LightEnergy = 1.2f, ShadowEnabled = false });
        this._leftSphere = this.CreateSphere();
        this._rightSphere = this.CreateSphere();
        CanvasLayer canvas = new();
        this.AddChild(canvas);
        this._display = new InputDisplay { State = this._state, MouseFilter = Control.MouseFilterEnum.Ignore };
        canvas.AddChild(this._display);
        this.UpdateGamepadDescription();
        this.WriteSessionInformation();
    }

    // This callback collects events only. The sole game update is _Process.
    public override void _Input(InputEvent @event) {
        this._state.Record(@event);
    }

    public override void _Process(double delta) {
        DisplayServer.ProcessEvents();
        if (this._closing) {
            return; // The explicit pump can deliver a window-close request.
        }

        int device = this._state.SelectedGamepad;
        this._state.LeftStick = device < 0 ? Vector2.Zero : new Vector2(
            Input.GetJoyAxis(device, JoyAxis.LeftX), Input.GetJoyAxis(device, JoyAxis.LeftY));
        this._state.RightStick = device < 0 ? Vector2.Zero : new Vector2(
            Input.GetJoyAxis(device, JoyAxis.RightX), Input.GetJoyAxis(device, JoyAxis.RightY));
        // 100 pixels per world unit; positive stick Y points down, as in Godot.
        this._leftSphere.Position = new Vector3(-6.35f + (this._state.LeftStick.X * 1.5f), -0.1f - (this._state.LeftStick.Y * 1.5f), 0);
        this._rightSphere.Position = new Vector3(-1.35f + (this._state.RightStick.X * 1.5f), -0.1f - (this._state.RightStick.Y * 1.5f), 0);
        this._display.Focused = this.GetWindow().HasFocus();
        this._display.MousePosition = this.GetViewport().GetMousePosition();
        this._display.SpacePressed = Input.IsPhysicalKeyPressed(Key.Space);
        this._display.LeftMousePressed = Input.IsMouseButtonPressed(MouseButton.Left);
        this._display.QueueRedraw();

        if (this._verify) {
            this._verify = false;
            this.FinishVerification();
        }
    }

    private MeshInstance3D CreateSphere() {
        MeshInstance3D sphere = new() {
            Mesh = new SphereMesh { Radius = 0.14f, Height = 0.28f, RadialSegments = 24, Rings = 12 },
            MaterialOverride = this._sphereMaterial
        };
        this.AddChild(sphere);
        return sphere;
    }

    private void ConfigureWindow() {
        Window window = this.GetWindow();
        if (DisplayServer.GetName() == "headless" || window.Mode != Window.ModeEnum.Windowed
            || Array.IndexOf(System.Environment.GetCommandLineArgs(), "--resolution") >= 0) {
            return;
        }

        // Windows reports effective monitor DPI: 144 DPI means 150% display scaling.
        // Canvas-items stretching scales the UI and renders 3D at the physical resolution.
        Rect2I workArea = DisplayServer.ScreenGetUsableRect(window.CurrentScreen);
        Vector2I designSize = window.ContentScaleSize;
        float scale = DisplayServer.ScreenGetDpi(window.CurrentScreen) / 96.0f;
        scale = Mathf.Min(scale, Mathf.Min(workArea.Size.X * 0.9f / designSize.X, workArea.Size.Y * 0.9f / designSize.Y));
        window.Size = new Vector2I(Mathf.RoundToInt(designSize.X * scale), Mathf.RoundToInt(designSize.Y * scale));
        window.Position = workArea.Position + ((workArea.Size - window.Size) / 2);
    }

    private void OnGamepadConnectionChanged(long device, bool connected) {
        this._state.SetConnection((int)device, connected);
        this._state.Gamepad.Add($"#{device}  {(connected ? "CONNECTED" : "DISCONNECTED")}");
        this.UpdateGamepadDescription();
        GD.Print($"Gamepad {device}: connected={connected}; selected={this._state.SelectedGamepad}");
    }

    private void UpdateGamepadDescription() {
        int device = this._state.SelectedGamepad;
        this._sphereMaterial.AlbedoColor = new Color(device < 0 ? "596a7e" : "50dec4");
        this._display.GamepadDescription = device < 0 ? "No gamepad connected" : $"#{device}  {Input.GetJoyName(device)}";
        this._display.GamepadMapping = device < 0 ? "Connect a gamepad at any time." : Input.IsJoyKnown(device)
            ? "Mapped controller | direct axis values"
            : "Unmapped controller: axis layout may differ.";
    }

    private void WriteSessionInformation() {
        this._logDirectory = System.Environment.GetEnvironmentVariable("INPUT_LATENCY_LOG_DIRECTORY") ?? "";
        if (this._logDirectory.Length == 0) {
            string root = OS.HasFeature("editor") ? ProjectSettings.GlobalizePath("res://") : Path.GetFullPath(Path.Combine(Path.GetDirectoryName(OS.GetExecutablePath())!, ".."));
            this._logDirectory = Path.Combine(root, "MyLogOutput", DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss", CultureInfo.InvariantCulture));
        }
        _ = Directory.CreateDirectory(this._logDirectory);
        var information = new {
            Started = DateTimeOffset.Now,
            ProcessId = OS.GetProcessId(),
            Executable = OS.GetExecutablePath(),
            Engine = Engine.GetVersionInfo()["string"].AsString(),
            DisplayServer = DisplayServer.GetName(),
            RenderingMethod = RenderingServer.GetCurrentRenderingMethod(),
            RenderingDriver = RenderingServer.GetCurrentRenderingDriverName(),
            WindowSize = DisplayServer.WindowGetSize().ToString(),
            ScreenDpi = DisplayServer.ScreenGetDpi(this.GetWindow().CurrentScreen),
            ContentScaleSize = this.GetWindow().ContentScaleSize.ToString(),
            ContentScaleMode = this.GetWindow().ContentScaleMode.ToString(),
            ContentScaleAspect = this.GetWindow().ContentScaleAspect.ToString(),
            VSync = DisplayServer.WindowGetVsyncMode().ToString(),
            MaximumFramesPerSecond = Engine.MaxFps,
            SwapchainImages = ProjectSettings.GetSetting("rendering/rendering_device/vsync/swapchain_image_count").AsInt32(),
            FrameQueueSize = ProjectSettings.GetSetting("rendering/rendering_device/vsync/frame_queue_size").AsInt32(),
            RenderingThreadModel = ProjectSettings.GetSetting("rendering/driver/threads/thread_model").AsInt32(),
            AccumulatedInput = Input.UseAccumulatedInput,
            this._state.SelectedGamepad,
            Verification = this._verify
        };
        File.WriteAllText(Path.Combine(this._logDirectory, "Session.json"), JsonSerializer.Serialize(information, SessionSerializerOptions));
        GD.Print($"Input latency application ready. PID={OS.GetProcessId()}; logs={this._logDirectory}");
    }

    private void Close() {
        this.Close(0);
    }

    private async void FinishVerification() {
        try {
            int result = Verification.Run(this._state);
            if (result == 0 && DisplayServer.GetName() != "headless") {
                // Capture our own rendered viewport, without desktop capture or input injection.
                for (int frame = 0; frame < 8; frame++) {
                    _ = await this.ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                }

                Verification.CheckDisplay(this._state, this._leftSphere, this._rightSphere);
                using Image image = this.GetViewport().GetTexture().GetImage();
                string path = Path.Combine(this._logDirectory, "Verification.png");
                if (image.SavePng(path) != Error.Ok) {
                    throw new IOException("Could not save the rendered verification image.");
                }

                GD.Print($"Rendered verification image: {path}");
            }
            this.Close(result);
        } catch (Exception exception) {
            GD.PushError(exception.ToString());
            this.Close(1);
        }
    }

    private void Close(int result) {
        this._closing = true;
        this.GetTree().Quit(result);
    }

    public override void _ExitTree() {
        Input.JoyConnectionChanged -= this.OnGamepadConnectionChanged;
        this.GetWindow().CloseRequested -= this.Close;
    }
}
