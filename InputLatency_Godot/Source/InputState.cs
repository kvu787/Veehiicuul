using Godot;
using System.Collections.Generic;

namespace InputLatencyGodot;

// Stores only the visible tail. High-rate motion cannot grow memory without bound.
public sealed class EventStream
{
    public const int Capacity = 7;
    public Queue<string> Lines { get; } = new(Capacity);
    public ulong Count { get; private set; }

    public void Add(string description)
    {
        Count++;
        if (Lines.Count == Capacity) Lines.Dequeue();
        Lines.Enqueue($"{Time.GetTicksMsec() / 1000.0,8:F3}  {description}");
    }
}

public sealed class InputState
{
    private readonly SortedSet<int> _gamepads = new();
    public int SelectedGamepad { get; private set; } = -1;
    public EventStream Keyboard { get; } = new();
    public EventStream Mouse { get; } = new();
    public EventStream Gamepad { get; } = new();
    public Vector2 RightStick { get; set; }

    public void SetConnection(int device, bool connected)
    {
        if (connected) _gamepads.Add(device);
        else _gamepads.Remove(device);
        if (_gamepads.Contains(SelectedGamepad)) return;
        SelectedGamepad = _gamepads.Count == 0 ? -1 : _gamepads.Min;
        RightStick = Vector2.Zero;
    }

    public void Record(InputEvent input)
    {
        switch (input)
        {
            case InputEventKey key:
                Keyboard.Add($"{(key.Pressed ? (key.Echo ? "REPEAT" : "DOWN") : "UP")}  {OS.GetKeycodeString(key.Keycode)}  physical={OS.GetKeycodeString(key.PhysicalKeycode)}");
                break;
            case InputEventMouseMotion motion:
                Mouse.Add($"MOVE  ({motion.Position.X:F0}, {motion.Position.Y:F0})  delta=({motion.Relative.X:F0}, {motion.Relative.Y:F0})");
                break;
            case InputEventMouseButton button:
                Mouse.Add($"{(button.Pressed ? "DOWN" : "UP")}  {button.ButtonIndex}  factor={button.Factor:F2}  ({button.Position.X:F0}, {button.Position.Y:F0})");
                break;
            case InputEventJoypadMotion motion when motion.Device == SelectedGamepad:
                Gamepad.Add($"#{motion.Device}  AXIS  {motion.Axis} = {motion.AxisValue:+0.0000;-0.0000;0.0000}");
                break;
            case InputEventJoypadButton button when button.Device == SelectedGamepad:
                Gamepad.Add($"#{button.Device}  {(button.Pressed ? "DOWN" : "UP")}  {button.ButtonIndex}");
                break;
        }
    }
}
