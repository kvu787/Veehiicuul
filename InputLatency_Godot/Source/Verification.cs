using Godot;
using System;

namespace InputLatencyGodot;

internal static class Verification {
    public static int Run(InputState liveState) {
        int checkCount = 0;
        try {
            InputState state = new();
            Check(state.SelectedGamepad == -1, "Startup without a controller");
            state.SetConnection(6, true);
            Check(state.SelectedGamepad == 6, "A nonzero controller ID is selected");
            state.SetConnection(2, true);
            Check(state.SelectedGamepad == 6, "Connecting another controller keeps the current selection");
            using InputEventJoypadButton other = new() { Device = 2, ButtonIndex = JoyButton.A, Pressed = true };
            using InputEventJoypadButton selected = new() { Device = 6, ButtonIndex = JoyButton.A, Pressed = true };
            state.Record(other);
            Check(state.Gamepad.Count == 0, "Other controller input is ignored");
            state.Record(selected);
            Check(state.Gamepad.Count == 1, "Selected controller input is shown");
            state.RightStick = Vector2.One;
            state.SetConnection(6, false);
            Check(state.SelectedGamepad == 2 && state.RightStick == Vector2.Zero, "Disconnect selects the remaining controller and clears axes");
            state.SetConnection(2, false);
            Check(state.SelectedGamepad == -1, "Removing the last controller is safe");
            state.SetConnection(6, true);
            Check(state.SelectedGamepad == 6, "Reconnect resumes selection");
            using InputEventJoypadMotion axis = new() { Device = 6, Axis = JoyAxis.RightX, AxisValue = -0.75f };
            state.Record(axis);
            Check(state.Gamepad.Count == 2, "Gamepad axis events are shown");

            using InputEventKey key = new() { Keycode = Key.Space, PhysicalKeycode = Key.Space, Pressed = true };
            state.Record(key);
            key.Pressed = false;
            state.Record(key);
            Check(state.Keyboard.Count == 2, "Both edges of a brief keypress are retained");
            using InputEventMouseMotion motion = new() { Position = new Vector2(30, 40), Relative = Vector2.One };
            for (int i = 0; i < 10000; i++) {
                state.Record(motion);
            }

            Check(state.Mouse.Count == 10000 && state.Mouse.Lines.Count == InputEventHistory.Capacity, "Motion history remains bounded");
            using InputEventMouseButton wheel = new() { ButtonIndex = MouseButton.WheelUp, Pressed = true, Factor = 1 };
            state.Record(wheel);
            Check(state.Mouse.Count == 10001, "Mouse wheel events are shown");
            // Exercise Godot's event dispatch into the actual scene, without OS input injection.
            ulong keyboardBefore = liveState.Keyboard.Count;
            key.Pressed = true;
            Input.ParseInputEvent(key);
            Check(Input.IsPhysicalKeyPressed(Key.Space), "Godot reports Space held after a press");
            key.Pressed = false;
            Input.ParseInputEvent(key);
            Check(!Input.IsPhysicalKeyPressed(Key.Space), "Godot clears Space held state after release");
            Check(liveState.Keyboard.Count == keyboardBefore + 2, "Godot delivers key press and release to the scene");
            ulong mouseBefore = liveState.Mouse.Count;
            Input.ParseInputEvent(motion);
            Input.ParseInputEvent(wheel);
            Check(liveState.Mouse.Count == mouseBefore + 2, "Godot delivers mouse motion and wheel events to the scene");
            mouseBefore = liveState.Mouse.Count;
            using InputEventMouseButton button = new() { ButtonIndex = MouseButton.Left, Pressed = true };
            Input.ParseInputEvent(button);
            Check(Input.IsMouseButtonPressed(MouseButton.Left), "Godot reports the left mouse button held after a press");
            button.Pressed = false;
            Input.ParseInputEvent(button);
            Check(!Input.IsMouseButtonPressed(MouseButton.Left), "Godot clears the left mouse button held state after release");
            Check(liveState.Mouse.Count == mouseBefore + 2, "Godot delivers mouse button press and release to the scene");
            GD.Print($"PASS: {checkCount} input verification checks. Synthetic events; physical hotplug and latency are not measured.");
            return 0;
        } catch (Exception exception) {
            GD.PushError(exception.ToString());
            return 1;
        }

        void Check(bool condition, string description) {
            if (!condition) {
                throw new InvalidOperationException(description);
            }

            checkCount++;
        }
    }
}
