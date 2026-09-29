using Godot;
using System;
using System.Collections.Generic;

namespace Veehiicuul_Godot_CSharp;

/// <summary>
/// Counts digital down events delivered to this node's _Input during the current game frame.
/// Query from the main thread; reads do not consume counts. Main_GodotAdapter clears them
/// after Main.Process returns, including early returns and exceptions.
/// </summary>
public partial class DigitalInputCounts : Node {
    private static DigitalInputCounts? CurrentInstance { get; set; }
    private readonly Dictionary<Key, int> KeyCounts = [];
    private readonly Dictionary<Key, int> PhysicalKeyCounts = [];
    private readonly Dictionary<MouseButton, int> MouseButtonCounts = [];
    private readonly Dictionary<(int Device, JoyButton Button), int> GamepadButtonCounts = [];
    private readonly Dictionary<(int Device, int Index), int> TouchCounts = [];

    /// <summary>The autoload instance, available before the main scene enters the tree.</summary>
    public static DigitalInputCounts Instance => CurrentInstance
        ?? throw new InvalidOperationException("The DigitalInputCounts autoload is not in the scene tree.");

    public override void _EnterTree() {
        CurrentInstance = this;
    }

    public override void _ExitTree() {
        this.ClearFrameCounts();
        CurrentInstance = null;
    }

    /// <summary>Down events for a layout-dependent key code, excluding keyboard auto-repeat.</summary>
    public int GetKeyDownCount(Key key) {
        return this.KeyCounts.GetValueOrDefault(key);
    }

    /// <summary>Down events for a physical key position, excluding keyboard auto-repeat.</summary>
    public int GetPhysicalKeyDownCount(Key key) {
        return this.PhysicalKeyCounts.GetValueOrDefault(key);
    }

    /// <summary>
    /// Down events for a mouse button, including wheel directions. Each wheel event counts
    /// once regardless of its scroll factor. Windows combines connected mice and keyboards.
    /// </summary>
    public int GetMouseButtonDownCount(MouseButton button) {
        return this.MouseButtonCounts.GetValueOrDefault(button);
    }

    /// <summary>Down events for a button on the specified Godot gamepad device ID.</summary>
    public int GetGamepadButtonDownCount(int device, JoyButton button) {
        return this.GamepadButtonCounts.GetValueOrDefault((device, button));
    }

    /// <summary>Touch-down events for a contact index on the specified device.</summary>
    public int GetTouchDownCount(int device, int index) {
        return this.TouchCounts.GetValueOrDefault((device, index));
    }

    public override void _Input(InputEvent @event) {
        switch (@event) {
        case InputEventKey { Pressed: true, Echo: false } key:
            // Synthetic events can supply only one of these codes; zero means unspecified.
            if (key.Keycode != Key.None) {
                Increment(this.KeyCounts, key.Keycode);
            }
            if (key.PhysicalKeycode != Key.None) {
                Increment(this.PhysicalKeyCounts, key.PhysicalKeycode);
            }
            break;
        case InputEventMouseButton { Pressed: true } mouseButton:
            Increment(this.MouseButtonCounts, mouseButton.ButtonIndex);
            break;
        case InputEventJoypadButton { Pressed: true } gamepadButton:
            Increment(this.GamepadButtonCounts, (gamepadButton.Device, gamepadButton.ButtonIndex));
            break;
        case InputEventScreenTouch { Pressed: true, Canceled: false } touch:
            Increment(this.TouchCounts, (touch.Device, touch.Index));
            break;
        default:
            break;
        }
    }

    private static void Increment<TCode>(Dictionary<TCode, int> counts, TCode code) where TCode : notnull {
        counts[code] = counts.GetValueOrDefault(code) + 1;
    }

    // The sole frame callback owns this boundary; do not clear during input dispatch or rendering.
    internal void ClearFrameCounts() {
        this.KeyCounts.Clear();
        this.PhysicalKeyCounts.Clear();
        this.MouseButtonCounts.Clear();
        this.GamepadButtonCounts.Clear();
        this.TouchCounts.Clear();
    }
}
