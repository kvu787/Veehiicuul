using Godot;
using System;

namespace Veehiicuul_Godot_CSharp;

/// <summary>One snapshot per Main._Process; no input signals or node callbacks.</summary>
public sealed class InputManager {
    private readonly bool[] PreviousButtons = new bool[(int)JoyButton.Max];
    private readonly bool[] PressedButtons = new bool[(int)JoyButton.Max];
    private int PreviousGamepad = -1;
    private bool PreviousEscape;
    private DateTime LastLogTime = DateTime.MinValue;

    public bool HasGamepad { get; private set; }
    public float Brake { get; private set; }
    public Vector2 AccelerationInput { get; private set; }
    public Vector2 RightStick => this.AccelerationInput;
    public Vector2 LeftStick { get; private set; }
    public bool RightShoulderPressed { get; private set; }
    public bool ResetCameraZoom { get; private set; }
    public bool QuitGame { get; private set; }
    public bool PreviousTrack { get; private set; }
    public bool NextTrack { get; private set; }
    public bool PreviousCar { get; private set; }
    public bool NextCar { get; private set; }
    public bool ToggleBetweenFixedAndFollowCamera { get; private set; }
    public bool ToggleBetweenBorderlessAndExclusiveFullScreen { get; private set; }
    public bool NextMsaaMode { get; private set; }
    public bool NextTaaMode { get; private set; }
    public bool NextVsyncMode { get; private set; }
    public bool NextRenderScale { get; private set; }
    public bool ResetCar { get; private set; }
    public bool InsertStutterLogSpacer { get; private set; }

    public void UpdateInputs() {
        Godot.Collections.Array<int> gamepads = Input.GetConnectedJoypads();
        int gamepad = gamepads.Count == 0 ? -1 : gamepads[0];
        this.HasGamepad = gamepad >= 0;
        if (gamepad != this.PreviousGamepad) {
            Array.Clear(this.PreviousButtons);
            this.PreviousGamepad = gamepad;
        }

        for (int index = 0; index < this.PreviousButtons.Length; index++) {
            bool down = this.HasGamepad && Input.IsJoyButtonPressed(gamepad, (JoyButton)index);
            this.PressedButtons[index] = down && !this.PreviousButtons[index];
            this.PreviousButtons[index] = down;
        }

        bool escape = Input.IsPhysicalKeyPressed(Key.Escape);
        this.QuitGame = (escape && !this.PreviousEscape) || this.WasPressed(JoyButton.Start);
        this.PreviousEscape = escape;
        this.PreviousTrack = this.WasPressed(JoyButton.DpadDown);
        this.NextTrack = this.WasPressed(JoyButton.DpadUp);
        this.PreviousCar = this.WasPressed(JoyButton.DpadLeft);
        this.NextCar = this.WasPressed(JoyButton.DpadRight);
        this.ToggleBetweenFixedAndFollowCamera = this.WasPressed(JoyButton.Back);

        bool leftShoulder = this.PreviousButtons[(int)JoyButton.LeftShoulder];
        this.RightShoulderPressed = this.PreviousButtons[(int)JoyButton.RightShoulder];
        this.NextMsaaMode = leftShoulder && this.WasPressed(JoyButton.A);
        this.NextTaaMode = leftShoulder && this.WasPressed(JoyButton.B);
        this.NextVsyncMode = leftShoulder && this.WasPressed(JoyButton.X);
        this.NextRenderScale = leftShoulder && this.WasPressed(JoyButton.Y);
        this.ResetCar = !leftShoulder && this.WasPressed(JoyButton.X);
        this.ResetCameraZoom = this.WasPressed(JoyButton.Y);

        // These actions were deliberately unbound in ZoomTracks.
        this.ToggleBetweenBorderlessAndExclusiveFullScreen = false;
        this.InsertStutterLogSpacer = false;

        this.Brake = this.HasGamepad ? Input.GetJoyAxis(gamepad, JoyAxis.TriggerLeft) : 0f;
        // Godot stick Y is down-positive; the original driving/camera math expects up-positive.
        // GetJoyAxis returns raw axes: the driving code applies its own dead zones.
        this.AccelerationInput = this.HasGamepad
            ? new Vector2(Input.GetJoyAxis(gamepad, JoyAxis.RightX), -Input.GetJoyAxis(gamepad, JoyAxis.RightY))
            : Vector2.Zero;
        Vector2 rawLeftStick = this.HasGamepad
            ? new Vector2(Input.GetJoyAxis(gamepad, JoyAxis.LeftX), -Input.GetJoyAxis(gamepad, JoyAxis.LeftY))
            : Vector2.Zero;
        // Unity's camera reads the processed left stick. Preserve the source InputSystem
        // settings' radial deadzone before CameraController applies its axial zoom filter.
        this.LeftStick = ApplySourceStickDeadzone(rawLeftStick);
    }

    /// <summary>Optional diagnostic called explicitly after polling, never on its own timer.</summary>
    public void LogGamepadRightStick() {
        if (this.HasGamepad && DateTime.Now - this.LastLogTime > TimeSpan.FromSeconds(0.5)) {
            this.LastLogTime = DateTime.Now;
            Vector2 processed = ApplySourceStickDeadzone(this.RightStick);
            GD.Print($"Right stick processed: magnitude={processed.Length():R}, x={processed.X:R}, y={processed.Y:R}");
            GD.Print($"Right stick raw: magnitude={this.RightStick.Length():R}, x={this.RightStick.X:R}, y={this.RightStick.Y:R}");
        }
    }

    private bool WasPressed(JoyButton button) {
        return this.PressedButtons[(int)button];
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
