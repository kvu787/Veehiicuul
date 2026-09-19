using Godot;

namespace Veehiicuul_Godot_CSharp;

public partial class Main : Node {
    // Called when the node enters the scene tree for the first time.
    public override void _Ready() {
        GD.Print("Meow");
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta) {
    }
}
