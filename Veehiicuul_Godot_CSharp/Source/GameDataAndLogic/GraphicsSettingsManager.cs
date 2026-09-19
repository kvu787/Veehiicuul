using Godot;
using System;

namespace Veehiicuul_Godot_CSharp;

public sealed class GraphicsSettingsManager {
    private readonly Viewport Viewport;
    private readonly InputManager InputManager;
    private bool MsaaEnabled = true;
    private bool TaaEnabled;
    private bool VsyncEnabled;
    private int RenderScaleIndex = 1;
    private static readonly float[] RenderScales = [0.125f, 1f, 2f];

    public static void ConfigureSessionGraphicsSettings(Viewport viewport) {
        Engine.MaxFps = 0;
        viewport.UseHdr2D = false;
        // Forward+ uses HDR internally for 3D. Unity's supportsHDR and maxQueuedFrames have
        // no equivalent per-session switches; see project.godot for presentation settings.
        // There is no URP asset to clone or restore: these settings belong to the live viewport.
        viewport.Scaling3DMode = Viewport.Scaling3DModeEnum.Bilinear;
    }

    public GraphicsSettingsManager(Viewport viewport, InputManager inputManager) {
        ArgumentNullException.ThrowIfNull(viewport);
        ArgumentNullException.ThrowIfNull(inputManager);
        this.Viewport = viewport;
        this.InputManager = inputManager;
        this.ApplyGraphicsSettings();
    }

    public void ReadInputAndUpdate() {
        bool changed = false;
        if (this.InputManager.NextMsaaMode) {
            this.MsaaEnabled = !this.MsaaEnabled;
            this.TaaEnabled = false;
            changed = true;
        } else if (this.InputManager.NextTaaMode) {
            this.MsaaEnabled = false;
            // Godot exposes one TAA implementation, without Unity's five quality tiers.
            this.TaaEnabled = !this.TaaEnabled;
            changed = true;
        }
        if (this.InputManager.NextVsyncMode) {
            this.VsyncEnabled = !this.VsyncEnabled;
            changed = true;
        }
        if (this.InputManager.NextRenderScale) {
            this.RenderScaleIndex = (this.RenderScaleIndex + 1) % RenderScales.Length;
            changed = true;
        }
        if (changed) {
            this.ApplyGraphicsSettings();
        }
    }

    private void ApplyGraphicsSettings() {
        if (this.MsaaEnabled && this.TaaEnabled) {
            throw new InvalidOperationException("Cannot enable MSAA and TAA simultaneously.");
        }
        this.Viewport.Msaa3D = this.MsaaEnabled ? Viewport.Msaa.Msaa8X : Viewport.Msaa.Disabled;
        this.Viewport.UseTaa = this.TaaEnabled;
        this.Viewport.Scaling3DScale = RenderScales[this.RenderScaleIndex];
        DisplayServer.WindowSetVsyncMode(this.VsyncEnabled ? DisplayServer.VSyncMode.Enabled : DisplayServer.VSyncMode.Disabled);
        GD.Print($"MSAA = {this.Viewport.Msaa3D}; TAA = {this.TaaEnabled}; VSync = {this.VsyncEnabled}; Render scale = {this.Viewport.Scaling3DScale}");
    }
}
