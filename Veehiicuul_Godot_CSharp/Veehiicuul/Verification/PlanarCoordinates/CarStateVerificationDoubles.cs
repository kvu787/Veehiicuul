using Godot;

namespace Veehiicuul_Godot_CSharp;

// Only the engine/input collaborators are doubles. The verification links the
// production motion managers and uses Godot's real vector/quaternion mathematics.
public sealed class InputManager {
    public float Brake { get; set; }
    public Vector2 AccelerationInput { get; set; }
}

public sealed class CarSwitcher {
    public VerificationCar CurrentCar { get; } = new();
}

public sealed class VerificationCar {
    public CarDynamic Dynamic { get; } = new() {
        AccelerationMap = new() { Forward = 4f, Reverse = 8f, Left = 2f, Right = 2f }
    };
    public VerificationNode Node { get; } = new();
}

public sealed class TrackObjects {
    public VerificationNode PlaceholderCarNode { get; } = new();
    public VerificationNode CameraPanAndYaw { get; } = new();
    public VerificationNode Camera { get; } = new();
}

public sealed class VerificationNode {
    public Vector3 Position { get; set; }
    public Vector3 GlobalPosition { get; set; }
    public Vector3 Rotation { get; set; }
    public Vector3 GlobalRotation { get; set; }
    public Quaternion AppliedRotation { get; private set; } = Quaternion.Identity;

    public void SetPositionAndRotation(Vector3 position, Quaternion rotation) {
        this.Position = position;
        this.AppliedRotation = rotation;
    }
}
