using Godot;
using System;
using System.IO;
using System.Text.Json;

namespace InputLatencyGodot;

public partial class InputLatency : Node3D
{
    private readonly InputState _state = new();
    private MeshInstance3D _sphere = null!;
    private readonly StandardMaterial3D _sphereMaterial = new() { Roughness = 0.65f };
    private InputDisplay _display = null!;
    private string _logDirectory = "";
    private bool _verify;
    private bool _closing;

    public override void _Ready()
    {
        _verify = Array.IndexOf(OS.GetCmdlineUserArgs(), "--self-test") >= 0;
        Input.UseAccumulatedInput = false;
        Input.MouseMode = Input.MouseModeEnum.Visible;
        GetTree().AutoAcceptQuit = false;
        GetWindow().CloseRequested += Close;
        Input.JoyConnectionChanged += OnGamepadConnectionChanged;
        foreach (int device in Input.GetConnectedJoypads()) _state.SetConnection(device, true);

        var camera = new Camera3D { Projection = Camera3D.ProjectionType.Orthogonal, Size = 7.2f, Position = new Vector3(0, 0, 10), Current = true };
        AddChild(camera);
        AddChild(new DirectionalLight3D { RotationDegrees = new Vector3(-25, -30, 0), LightEnergy = 1.2f, ShadowEnabled = false });
        _sphere = new MeshInstance3D
        {
            Mesh = new SphereMesh { Radius = 0.14f, Height = 0.28f, RadialSegments = 24, Rings = 12 },
            MaterialOverride = _sphereMaterial
        };
        AddChild(_sphere);
        var canvas = new CanvasLayer();
        AddChild(canvas);
        _display = new InputDisplay { State = _state, MouseFilter = Control.MouseFilterEnum.Ignore };
        canvas.AddChild(_display);
        UpdateGamepadDescription();
        WriteSessionInformation();
    }

    // This callback collects events only. The sole game update is _Process.
    public override void _Input(InputEvent input) => _state.Record(input);

    public override void _Process(double delta)
    {
        DisplayServer.ProcessEvents();
        if (_closing) return; // The explicit pump can deliver a window-close request.

        int device = _state.SelectedGamepad;
        _state.RightStick = device < 0 ? Vector2.Zero : new Vector2(
            Input.GetJoyAxis(device, JoyAxis.RightX), Input.GetJoyAxis(device, JoyAxis.RightY));
        // 100 pixels per world unit; positive stick Y points down, as in Godot.
        _sphere.Position = new Vector3(-3.85f + _state.RightStick.X * 1.5f, -0.1f - _state.RightStick.Y * 1.5f, 0);
        _display.Focused = GetWindow().HasFocus();
        _display.MousePosition = GetViewport().GetMousePosition();
        _display.SpacePressed = Input.IsPhysicalKeyPressed(Key.Space);
        _display.LeftMousePressed = Input.IsMouseButtonPressed(MouseButton.Left);
        _display.QueueRedraw();

        if (_verify)
        {
            _verify = false;
            FinishVerification();
        }
    }

    private void OnGamepadConnectionChanged(long device, bool connected)
    {
        _state.SetConnection((int)device, connected);
        _state.Gamepad.Add($"#{device}  {(connected ? "CONNECTED" : "DISCONNECTED")}");
        UpdateGamepadDescription();
        GD.Print($"Gamepad {device}: connected={connected}; selected={_state.SelectedGamepad}");
    }

    private void UpdateGamepadDescription()
    {
        int device = _state.SelectedGamepad;
        _sphereMaterial.AlbedoColor = new Color(device < 0 ? "596a7e" : "50dec4");
        _display.GamepadDescription = device < 0 ? "No gamepad connected" : $"#{device}  {Input.GetJoyName(device)}";
        _display.GamepadMapping = device < 0 ? "Connect a gamepad at any time." : Input.IsJoyKnown(device)
            ? "Mapped controller | direct axis values"
            : "Unmapped controller: axis layout may differ.";
    }

    private void WriteSessionInformation()
    {
        _logDirectory = System.Environment.GetEnvironmentVariable("INPUT_LATENCY_LOG_DIRECTORY") ?? "";
        if (_logDirectory.Length == 0)
        {
            string root = OS.HasFeature("editor") ? ProjectSettings.GlobalizePath("res://") : Path.GetFullPath(Path.Combine(Path.GetDirectoryName(OS.GetExecutablePath())!, ".."));
            _logDirectory = Path.Combine(root, "MyLogOutput", DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss"));
        }
        Directory.CreateDirectory(_logDirectory);
        var information = new
        {
            Started = DateTimeOffset.Now,
            ProcessId = OS.GetProcessId(),
            Executable = OS.GetExecutablePath(),
            Engine = Engine.GetVersionInfo()["string"].AsString(),
            DisplayServer = Godot.DisplayServer.GetName(),
            RenderingMethod = RenderingServer.GetCurrentRenderingMethod(),
            RenderingDriver = RenderingServer.GetCurrentRenderingDriverName(),
            WindowSize = DisplayServer.WindowGetSize().ToString(),
            VSync = DisplayServer.WindowGetVsyncMode().ToString(),
            MaximumFramesPerSecond = Engine.MaxFps,
            SwapchainImages = ProjectSettings.GetSetting("rendering/rendering_device/vsync/swapchain_image_count").AsInt32(),
            FrameQueueSize = ProjectSettings.GetSetting("rendering/rendering_device/vsync/frame_queue_size").AsInt32(),
            RenderingThreadModel = ProjectSettings.GetSetting("rendering/driver/threads/thread_model").AsInt32(),
            AccumulatedInput = Input.UseAccumulatedInput,
            SelectedGamepad = _state.SelectedGamepad,
            Verification = _verify
        };
        File.WriteAllText(Path.Combine(_logDirectory, "Session.json"), JsonSerializer.Serialize(information, new JsonSerializerOptions { WriteIndented = true }));
        GD.Print($"Input latency application ready. PID={OS.GetProcessId()}; logs={_logDirectory}");
    }

    private void Close() => Close(0);

    private async void FinishVerification()
    {
        try
        {
            int result = Verification.Run(_state);
            if (result == 0 && DisplayServer.GetName() != "headless")
            {
                // Capture our own rendered viewport, without desktop capture or input injection.
                for (int frame = 0; frame < 8; frame++) await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                using var image = GetViewport().GetTexture().GetImage();
                string path = Path.Combine(_logDirectory, "Verification.png");
                if (image.SavePng(path) != Error.Ok) throw new IOException("Could not save the rendered verification image.");
                GD.Print($"Rendered verification image: {path}");
            }
            Close(result);
        }
        catch (Exception exception)
        {
            GD.PushError(exception.ToString());
            Close(1);
        }
    }

    private void Close(int result)
    {
        _closing = true;
        GetTree().Quit(result);
    }

    public override void _ExitTree()
    {
        Input.JoyConnectionChanged -= OnGamepadConnectionChanged;
        GetWindow().CloseRequested -= Close;
    }
}
