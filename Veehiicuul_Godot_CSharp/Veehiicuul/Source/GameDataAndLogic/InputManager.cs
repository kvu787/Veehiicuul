using Godot;
using System;

namespace Veehiicuul_Godot_CSharp;

/// <summary>One snapshot per Main.Process; analog input uses gamepad 0 and digital down events include all devices.</summary>
public sealed class InputManager {
    private DateTime LastLogTime = DateTime.MinValue;

    /// <summary>Queries whether the gamepad used for analog input is connected; only needed by diagnostics.</summary>
    public static bool HasGamepad => Input.GetConnectedJoypads().Contains(0);
    public float Brake { get; private set; }
    public Vector2 AccelerationInput { get; private set; }
    public Vector2 RightStick => this.AccelerationInput;
    public bool ResetCameraZoom { get; private set; }
    public bool QuitGame { get; private set; }
    public bool PreviousTrack { get; private set; }
    public bool NextTrack { get; private set; }
    public bool PreviousCar { get; private set; }
    public bool NextCar { get; private set; }
    public bool ToggleBetweenFixedAndFollowCamera { get; private set; }
    public bool ToggleFullscreen { get; private set; }
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

        //Godot.Collections.Array<int> gamepads = Input.GetConnectedJoypads();
        //int gamepad = gamepads.Count == 0 ? -1 : gamepads[0];
        //this.HasGamepad = gamepad >= 0;
        //if (gamepad != this.PreviousGamepad) {
        //    Array.Clear(this.PreviousButtons);
        //    this.PreviousGamepad = gamepad;
        //}

        //for (int index = 0; index < this.PreviousButtons.Length; index++) {
        //    bool down = this.HasGamepad && Input.IsJoyButtonPressed(gamepad, (JoyButton)index);
        //    this.PressedButtons[index] = down && !this.PreviousButtons[index];
        //    this.PreviousButtons[index] = down;
        //}

        //bool escape = Input.IsPhysicalKeyPressed(Key.Escape);
        //this.QuitGame = (escape && !this.PreviousEscape) || this.WasPressed(JoyButton.Start);
        //this.PreviousEscape = escape;
        //bool fullscreenShortcut = Input.IsPhysicalKeyPressed(Key.F11);
        //this.ToggleFullscreen = fullscreenShortcut && !this.PreviousFullscreenShortcut;
        //this.PreviousFullscreenShortcut = fullscreenShortcut;
        //this.PreviousTrack = this.WasPressed(JoyButton.DpadDown);
        //this.NextTrack = this.WasPressed(JoyButton.DpadUp);
        //this.PreviousCar = this.WasPressed(JoyButton.DpadLeft);
        //this.NextCar = this.WasPressed(JoyButton.DpadRight);
        //this.ToggleBetweenFixedAndFollowCamera = this.WasPressed(JoyButton.Back);

        //this.RightShoulderPressed = this.PreviousButtons[(int)JoyButton.RightShoulder];
        //this.ResetCar = this.WasPressed(JoyButton.X);
        //this.ResetCameraZoom = this.WasPressed(JoyButton.Y);

        //this.Brake = this.HasGamepad ? Input.GetJoyAxis(gamepad, JoyAxis.TriggerLeft) : 0f;
        //// Godot stick Y is down-positive; the original driving/camera math expects up-positive.
        //// GetJoyAxis returns raw axes: the driving code applies its own dead zones.
        //this.AccelerationInput = this.HasGamepad
        //    ? new Vector2(Input.GetJoyAxis(gamepad, JoyAxis.RightX), -Input.GetJoyAxis(gamepad, JoyAxis.RightY))
        //    : Vector2.Zero;
        //Vector2 rawLeftStick = this.HasGamepad
        //    ? new Vector2(Input.GetJoyAxis(gamepad, JoyAxis.LeftX), -Input.GetJoyAxis(gamepad, JoyAxis.LeftY))
        //    : Vector2.Zero;
        //// Unity's camera reads the processed left stick. Preserve the source InputSystem
        //// settings' radial deadzone before CameraController applies its axial zoom filter.
        //this.LeftStick = ApplySourceStickDeadzone(rawLeftStick);
    }

    /// <summary>Optional diagnostic called explicitly after polling, never on its own timer.</summary>
    public void LogGamepadRightStick() {
        if (DateTime.Now - this.LastLogTime > TimeSpan.FromSeconds(0.5) && HasGamepad) {
            this.LastLogTime = DateTime.Now;
            Vector2 processed = ApplySourceStickDeadzone(this.RightStick);
            GD.Print($"Right stick processed: magnitude={processed.Length():R}, x={processed.X:R}, y={processed.Y:R}");
            GD.Print($"Right stick raw: magnitude={this.RightStick.Length():R}, x={this.RightStick.X:R}, y={this.RightStick.Y:R}");
        }
    }

    private static Vector2 ApplySourceStickDeadzone(Vector2 value) {
        const float innerDeadzone = 0.125f;
        const float outerDeadzone = 0.925f;
        float magnitude = value.Length();
        if (magnitude <= innerDeadzone) {
            return Vector2.Zero;
        }
        float processedMagnitude = Mathf.Min(1f, (magnitude - innerDeadzone) / (outerDeadzone - innerDeadzone));
        return value * (processedMagnitude / magnitude);
    }
}
