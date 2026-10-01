using Godot;

namespace Veehiicuul_Godot_CSharp;

public sealed class InputManager {
    public float Brake { get; private set; }
    public Vector2 AccelerationInput { get; private set; }
    public bool ResetCameraZoom { get; private set; }
    public bool QuitGame { get; private set; }
    public bool PreviousTrack { get; private set; }
    public bool NextTrack { get; private set; }
    public bool PreviousCar { get; private set; }
    public bool NextCar { get; private set; }
    public bool ToggleBetweenFixedAndFollowCamera { get; private set; }
    public bool ResetCar { get; private set; }

    public float CameraZoom { get; private set; }

    private static float DeadzoneFilter(float input, float innerDeadzone, float outerDeadzone) {
        float sign = Mathf.Sign(input);
        input = Mathf.Abs(input);
        if (input > outerDeadzone) {
            input = 1f;
        } else if (input < innerDeadzone) {
            input = 0f;
        } else {
            input = (input - innerDeadzone) / (outerDeadzone - innerDeadzone);
        }
        return sign * input;
    }

    public void UpdateInputs() {
        this.PreviousCar = DigitalInputCounts.GetGamepadButtonDown(JoyButton.DpadLeft);
        this.NextCar = DigitalInputCounts.GetGamepadButtonDown(JoyButton.DpadRight);
        this.PreviousTrack = DigitalInputCounts.GetGamepadButtonDown(JoyButton.DpadDown);
        this.NextTrack = DigitalInputCounts.GetGamepadButtonDown(JoyButton.DpadUp);
        this.QuitGame = DigitalInputCounts.GetKeyDown(Key.Escape) || DigitalInputCounts.GetGamepadButtonDown(JoyButton.Start);
        this.ToggleBetweenFixedAndFollowCamera = DigitalInputCounts.GetGamepadButtonDown(JoyButton.Back);
        this.ResetCar = DigitalInputCounts.GetGamepadButtonDown(JoyButton.X);
        this.AccelerationInput = new Vector2(Input.GetJoyAxis(0, JoyAxis.RightX), -Input.GetJoyAxis(0, JoyAxis.RightY));
        this.Brake = Input.GetJoyAxis(0, JoyAxis.TriggerLeft);
        this.ResetCameraZoom = DigitalInputCounts.GetGamepadButtonDown(JoyButton.Y);
        if (Input.IsJoyButtonPressed(0, JoyButton.RightShoulder)) {
            const float innerDeadzone = 0.0078125f;
            const float outerDeadzone = 0.95f;
            this.CameraZoom = DeadzoneFilter(Input.GetJoyAxis(0, JoyAxis.LeftY), innerDeadzone, outerDeadzone);
        } else {
            this.CameraZoom = 0f;
        }
    }
}
