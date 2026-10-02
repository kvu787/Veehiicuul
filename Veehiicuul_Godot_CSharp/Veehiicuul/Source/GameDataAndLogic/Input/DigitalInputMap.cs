using Godot;
using System;

namespace Veehiicuul_Godot_CSharp;

public sealed class DigitalInputMap : IDisposable {
    public StringName JoyButtonA { get; } = new("JoyButtonA");
    public StringName JoyButtonB { get; } = new("JoyButtonB");
    public StringName JoyButtonX { get; } = new("JoyButtonX");
    public StringName JoyButtonY { get; } = new("JoyButtonY");
    public StringName JoyButtonDpadUp { get; } = new("JoyButtonDpadUp");
    public StringName JoyButtonDpadDown { get; } = new("JoyButtonDpadDown");
    public StringName JoyButtonDpadLeft { get; } = new("JoyButtonDpadLeft");
    public StringName JoyButtonDpadRight { get; } = new("JoyButtonDpadRight");
    public StringName JoyButtonLeftShoulder { get; } = new("JoyButtonLeftShoulder");
    public StringName JoyButtonRightShoulder { get; } = new("JoyButtonRightShoulder");
    public StringName JoyButtonLeftStick { get; } = new("JoyButtonLeftStick");
    public StringName JoyButtonRightStick { get; } = new("JoyButtonRightStick");
    public StringName JoyButtonBack { get; } = new("JoyButtonBack");
    public StringName JoyButtonStart { get; } = new("JoyButtonStart");
    public StringName KeyEscape { get; } = new("KeyEscape");
    public StringName MouseButtonMiddle { get; } = new("MouseButtonMiddle");

    private bool IsDisposed;

    public DigitalInputMap() {
        InputMap.AddAction(this.JoyButtonA);
        using (InputEventJoypadButton binding = new() { ButtonIndex = JoyButton.A, Device = -1 }) {
            InputMap.ActionAddEvent(this.JoyButtonA, binding);
        }
        InputMap.AddAction(this.JoyButtonB);
        using (InputEventJoypadButton binding = new() { ButtonIndex = JoyButton.B, Device = -1 }) {
            InputMap.ActionAddEvent(this.JoyButtonB, binding);
        }
        InputMap.AddAction(this.JoyButtonX);
        using (InputEventJoypadButton binding = new() { ButtonIndex = JoyButton.X, Device = -1 }) {
            InputMap.ActionAddEvent(this.JoyButtonX, binding);
        }
        InputMap.AddAction(this.JoyButtonY);
        using (InputEventJoypadButton binding = new() { ButtonIndex = JoyButton.Y, Device = -1 }) {
            InputMap.ActionAddEvent(this.JoyButtonY, binding);
        }
        InputMap.AddAction(this.JoyButtonDpadUp);
        using (InputEventJoypadButton binding = new() { ButtonIndex = JoyButton.DpadUp, Device = -1 }) {
            InputMap.ActionAddEvent(this.JoyButtonDpadUp, binding);
        }
        InputMap.AddAction(this.JoyButtonDpadDown);
        using (InputEventJoypadButton binding = new() { ButtonIndex = JoyButton.DpadDown, Device = -1 }) {
            InputMap.ActionAddEvent(this.JoyButtonDpadDown, binding);
        }
        InputMap.AddAction(this.JoyButtonDpadLeft);
        using (InputEventJoypadButton binding = new() { ButtonIndex = JoyButton.DpadLeft, Device = -1 }) {
            InputMap.ActionAddEvent(this.JoyButtonDpadLeft, binding);
        }
        InputMap.AddAction(this.JoyButtonDpadRight);
        using (InputEventJoypadButton binding = new() { ButtonIndex = JoyButton.DpadRight, Device = -1 }) {
            InputMap.ActionAddEvent(this.JoyButtonDpadRight, binding);
        }
        InputMap.AddAction(this.JoyButtonLeftShoulder);
        using (InputEventJoypadButton binding = new() { ButtonIndex = JoyButton.LeftShoulder, Device = -1 }) {
            InputMap.ActionAddEvent(this.JoyButtonLeftShoulder, binding);
        }
        InputMap.AddAction(this.JoyButtonRightShoulder);
        using (InputEventJoypadButton binding = new() { ButtonIndex = JoyButton.RightShoulder, Device = -1 }) {
            InputMap.ActionAddEvent(this.JoyButtonRightShoulder, binding);
        }
        InputMap.AddAction(this.JoyButtonLeftStick);
        using (InputEventJoypadButton binding = new() { ButtonIndex = JoyButton.LeftStick, Device = -1 }) {
            InputMap.ActionAddEvent(this.JoyButtonLeftStick, binding);
        }
        InputMap.AddAction(this.JoyButtonRightStick);
        using (InputEventJoypadButton binding = new() { ButtonIndex = JoyButton.RightStick, Device = -1 }) {
            InputMap.ActionAddEvent(this.JoyButtonRightStick, binding);
        }
        InputMap.AddAction(this.JoyButtonBack);
        using (InputEventJoypadButton binding = new() { ButtonIndex = JoyButton.Back, Device = -1 }) {
            InputMap.ActionAddEvent(this.JoyButtonBack, binding);
        }
        InputMap.AddAction(this.JoyButtonStart);
        using (InputEventJoypadButton binding = new() { ButtonIndex = JoyButton.Start, Device = -1 }) {
            InputMap.ActionAddEvent(this.JoyButtonStart, binding);
        }
        InputMap.AddAction(this.KeyEscape);
        using (InputEventKey binding = new() { Keycode = Key.Escape }) {
            InputMap.ActionAddEvent(this.KeyEscape, binding);
        }
        InputMap.AddAction(this.MouseButtonMiddle);
        using (InputEventMouseButton binding = new() { ButtonIndex = MouseButton.Middle }) {
            InputMap.ActionAddEvent(this.MouseButtonMiddle, binding);
        }
    }

    public void Dispose() {
        if (this.IsDisposed) {
            return;
        }

        InputMap.EraseAction(this.JoyButtonA);
        this.JoyButtonA.Dispose();
        InputMap.EraseAction(this.JoyButtonB);
        this.JoyButtonB.Dispose();
        InputMap.EraseAction(this.JoyButtonX);
        this.JoyButtonX.Dispose();
        InputMap.EraseAction(this.JoyButtonY);
        this.JoyButtonY.Dispose();
        InputMap.EraseAction(this.JoyButtonDpadUp);
        this.JoyButtonDpadUp.Dispose();
        InputMap.EraseAction(this.JoyButtonDpadDown);
        this.JoyButtonDpadDown.Dispose();
        InputMap.EraseAction(this.JoyButtonDpadLeft);
        this.JoyButtonDpadLeft.Dispose();
        InputMap.EraseAction(this.JoyButtonDpadRight);
        this.JoyButtonDpadRight.Dispose();
        InputMap.EraseAction(this.JoyButtonLeftShoulder);
        this.JoyButtonLeftShoulder.Dispose();
        InputMap.EraseAction(this.JoyButtonRightShoulder);
        this.JoyButtonRightShoulder.Dispose();
        InputMap.EraseAction(this.JoyButtonLeftStick);
        this.JoyButtonLeftStick.Dispose();
        InputMap.EraseAction(this.JoyButtonRightStick);
        this.JoyButtonRightStick.Dispose();
        InputMap.EraseAction(this.JoyButtonBack);
        this.JoyButtonBack.Dispose();
        InputMap.EraseAction(this.JoyButtonStart);
        this.JoyButtonStart.Dispose();
        InputMap.EraseAction(this.KeyEscape);
        this.KeyEscape.Dispose();
        InputMap.EraseAction(this.MouseButtonMiddle);
        this.MouseButtonMiddle.Dispose();
        this.IsDisposed = true;
    }
}
