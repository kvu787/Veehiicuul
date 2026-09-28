using Godot;

namespace Veehiicuul_Godot_CSharp;

/// <summary>The application's only startup entry point and frame callback.</summary>
public partial class Main_GodotAdapter : Node {
    private Main Main = null!;

    public override void _Ready() {
        this.Main = new Main(mainNode: this);
        this.Main.Ready();
    }

    public override void _Process(double delta) {
        this.Main.Process(delta);
    }
}
