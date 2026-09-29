# Digital input counts

The `DigitalInputCounts` autoload records digital down events from the main window
before scene or GUI input handlers can consume them. Query it from `Main.Process`
or code called by that method:

```csharp
DigitalInputCounts input = DigitalInputCounts.Instance;
int spacePresses = input.GetPhysicalKeyDownCount(Key.Space);
int letterPresses = input.GetKeyDownCount(Key.A);
int leftClicks = input.GetMouseButtonDownCount(MouseButton.Left);
int wheelUpEvents = input.GetMouseButtonDownCount(MouseButton.WheelUp);
int gamepadPresses = input.GetGamepadButtonDownCount(0, JoyButton.A);
int touchDowns = input.GetTouchDownCount(0, 0);
```

- Counts start at zero. Multiple presses between game updates accumulate, even if
  each input was released before the update. Reading a count does not consume it.
- Keyboard auto-repeat and releases do not count. Logical and physical key codes
  have separate queries; a missing code in a synthetic event is not counted.
- Mouse wheel directions count events, not scroll distance. A double-click
  normally delivers two down events and therefore counts as two presses.
- Gamepad buttons are separated by Godot device ID, which can be obtained from
  `Input.GetConnectedJoypads()`. Keyboards and mice use Windows' combined input.
- Touch contacts are separated by device ID and contact index. Canceled touches,
  mouse motion, touch drags, analog axes, and logical input actions do not count.
- Godot's touch/mouse emulation can generate an additional press of the other
  input type. Each delivered event counts under its own input code.
- All queries must run on the main thread. This is a game-frame counter, not a
  separate counter for each physics tick.

`Main_GodotAdapter._Process` clears all counts in a `finally` block immediately
after `Main.Process`, including when it returns early or throws. This keeps the
project's single frame callback and also works when rendering is disabled.
Read counts during the game update, before this boundary. Deferred work after
the update sees zero; input arriving after the boundary belongs to the next update.
If the main adapter stops processing, counts accumulate until its next update.

Collection uses the root window's `WindowInput` signal and remains active across
main-scene changes. `Input.ParseInputEvent` uses this route too. Events pushed
directly into another viewport with `Viewport.PushInput` are outside this window
input stream, as are input events from separate native windows.
