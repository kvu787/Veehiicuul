using Godot;
using System;

namespace Veehiicuul_Godot_CSharp;

/// <summary>The UI scene stays alive across tracks; Main rebuilds this manager with each new camera.</summary>
public sealed class UiManager(Node uiRoot, CameraController cameraController, TimeManager timeManager) {
    private readonly Label ClockText = TrackObjects.RequireNode<Label>(uiRoot, "ClockText");
    private readonly Label CameraSizeText = TrackObjects.RequireNode<Label>(uiRoot, "CameraSizeText");
    private readonly Label DisplayModeText = TrackObjects.RequireNode<Label>(uiRoot, "DisplayModeText");
    private readonly Label FpsText = TrackObjects.RequireNode<Label>(uiRoot, "FpsText");
    private readonly CameraController CameraController = cameraController;
    private readonly TimeManager TimeManager = timeManager;

    public void UpdateUi() {
        this.ClockText.Text = $"{DateTime.Now:yyyy-MM-dd [tt] HH:mm:ss.fff}";
        this.CameraSizeText.Text = $"Camera size: {this.CameraController.OrthographicCameraSize:F2}";
        this.DisplayModeText.Text = DisplayServer.WindowGetMode() switch {
            DisplayServer.WindowMode.ExclusiveFullscreen => "Display mode: Exclusive",
            DisplayServer.WindowMode.Fullscreen => "Display mode: Borderless",
            DisplayServer.WindowMode.Windowed => "Display mode: Windowed",
            DisplayServer.WindowMode.Maximized => "Display mode: Windowed max",
            DisplayServer.WindowMode.Minimized => "Display mode: Minimized",
            _ => throw new InvalidOperationException($"Unrecognized window mode: {DisplayServer.WindowGetMode()}"),
        };
        this.FpsText.Text = this.TimeManager.FrameDeltaTime > 0
            ? $"FPS: {1.0 / this.TimeManager.FrameDeltaTime:F2}"
            : "FPS: --";
    }
}
