using Godot;

namespace Veehiicuul_Godot_CSharp;

// Only the engine/input collaborators are doubles. The verification links the
// production motion managers and uses Godot's real vector/quaternion mathematics.
public sealed class InputManager {
    public float Brake { get; set; }
    public Vector2 AccelerationInput { get; set; }
    public float CameraZoom { get; set; }
    public bool ResetCameraZoom { get; set; }
    public bool ToggleBetweenFixedAndFollowCamera { get; set; }
}

public sealed class TrackSwitcher {
    public VerificationTrackSettings CurrentTrackJson { get; } = new();
}

public sealed class VerificationTrackSettings {
    public bool CameraFollowsCarLocation { get; set; }
    public float FollowCameraSize { get; set; } = 60f;
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
    public Quaternion Quaternion => Basis.FromEuler(this.Rotation).GetRotationQuaternion();
    public float Size { get; set; } = 100f;
}
