using Godot;
using System;

namespace Veehiicuul_Godot_CSharp;

/// <summary>The application's only startup entry point and frame callback.</summary>
public partial class Main_GodotAdapter : Node {
    private Main Main = null!;

    public override void _Ready() {
        try {
            this.Main = new Main(mainNode: this);
            this.Main.Ready();
        } catch (Exception exception) {
            Main.LogExceptionAndQuit(exception);
        }
    }

    public override void _Process(double delta) {
        try {
            this.Main.Process(delta);
            DigitalInputCounts.ClearFrameCounts();
        } catch (Exception exception) {
            Main.LogExceptionAndQuit(exception);
        }
    }
}
