using Godot;
using System;
using System.Collections.Generic;

namespace Veehiicuul_Godot_CSharp;

/// <summary>
/// Counts digital down events delivered to this node's _Input during the current game frame.
/// Device IDs are ignored; matching input codes share a count across all devices.
/// Query from the main thread; reads do not consume counts. Main_GodotAdapter clears them
/// after Main.Process returns, including early returns. An exception exits the application.
/// </summary>
public partial class DigitalInputCounts : Node {
    private static DigitalInputCounts? CurrentInstance { get; set; }
    private readonly Dictionary<Key, int> KeyCounts = [];
    private readonly Dictionary<MouseButton, int> MouseButtonCounts = [];
    private readonly Dictionary<JoyButton, int> GamepadButtonCounts = [];
    private readonly Dictionary<int, int> TouchCounts = [];

    /// <summary>The autoload instance, available before the main scene enters the tree.</summary>
    private static DigitalInputCounts Instance => CurrentInstance
        ?? throw new InvalidOperationException("The DigitalInputCounts autoload is not in the scene tree.");

    public override void _EnterTree() {
        CurrentInstance = this;
    }

    public override void _ExitTree() {
        this.ClearCounts();
        CurrentInstance = null;
    }

    /// <summary>Down events for a layout-dependent key code, excluding keyboard auto-repeat.</summary>
    public static int GetKeyDownCount(Key key) {
        return Instance.KeyCounts.GetValueOrDefault(key);
    }

    public static bool GetKeyDown(Key key) {
        return GetKeyDownCount(key) > 0;
    }

    /// <summary>
    /// Down events for a mouse button, including wheel directions. Each wheel event counts
    /// once regardless of its scroll factor. Windows combines connected mice and keyboards.
    /// </summary>
    public static int GetMouseButtonDownCount(MouseButton button) {
        return Instance.MouseButtonCounts.GetValueOrDefault(button);
    }

    public static bool GetMouseButtonDown(MouseButton button) {
        return GetMouseButtonDownCount(button) > 0;
    }

    /// <summary>Down events for a button across all gamepads.</summary>
    public static int GetGamepadButtonDownCount(JoyButton button) {
        return Instance.GamepadButtonCounts.GetValueOrDefault(button);
    }

    public static bool GetGamepadButtonDown(JoyButton button) {
        return GetGamepadButtonDownCount(button) > 0;
    }

    /// <summary>Touch-down events for a contact index across all devices.</summary>
    public static int GetTouchDownCount(int index) {
        return Instance.TouchCounts.GetValueOrDefault(index);
    }

    public override void _ShortcutInput(InputEvent @event) {
        switch (@event) {
        case InputEventKey { Pressed: true, Echo: false } key:
            // Synthetic events can supply only one of these codes; zero means unspecified.
            if (key.Keycode != Key.None) {
                Increment(this.KeyCounts, key.Keycode);
            }
            break;
        case InputEventMouseButton { Pressed: true } mouseButton:
            Increment(this.MouseButtonCounts, mouseButton.ButtonIndex);
            break;
        case InputEventJoypadButton { Pressed: true } gamepadButton:
            Increment(this.GamepadButtonCounts, gamepadButton.ButtonIndex);
            break;
        default:
            break;
        }
    }

    private static void Increment<TCode>(Dictionary<TCode, int> counts, TCode code) where TCode : notnull {
        counts[code] = counts.GetValueOrDefault(code) + 1;
    }

    // The sole frame callback owns this boundary; do not clear during input dispatch or rendering.
    internal static void ClearFrameCounts() {
        Instance.ClearCounts();
    }

    private void ClearCounts() {
        this.KeyCounts.Clear();
        this.MouseButtonCounts.Clear();
        this.GamepadButtonCounts.Clear();
        this.TouchCounts.Clear();
    }
}
