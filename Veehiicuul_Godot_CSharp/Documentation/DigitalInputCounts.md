# Digital input counts

The `DigitalInputCounts` autoload records digital down events delivered to its
`_Input` callback. Query it from `Main.Process` or code called by that method:

```csharp
DigitalInputCounts input = DigitalInputCounts.Instance;
int spacePresses = input.GetPhysicalKeyDownCount(Key.Space);
int letterPresses = input.GetKeyDownCount(Key.A);
int leftClicks = input.GetMouseButtonDownCount(MouseButton.Left);
int wheelUpEvents = input.GetMouseButtonDownCount(MouseButton.WheelUp);
int gamepadPresses = input.GetGamepadButtonDownCount(JoyButton.A);
int touchDowns = input.GetTouchDownCount(0);
```

- Counts start at zero. Multiple presses between game updates accumulate, even if
  each input was released before the update. Reading a count does not consume it.
- Keyboard auto-repeat and releases do not count. Logical and physical key codes
  have separate queries; a missing code in a synthetic event is not counted.
- Mouse wheel directions count events, not scroll distance. A double-click
  normally delivers two down events and therefore counts as two presses.
- Device IDs are ignored for every input type. Matching gamepad buttons share a
  count across all gamepads: pressing A once on each of two gamepads counts as two.
- Touch contacts are counted by contact index across all devices. Canceled touches,
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
If the main adapter stops processing while the collector continues receiving
input, counts accumulate until the adapter's next update.

Collection remains available across main-scene changes. It includes normal input,
events sent through `Input.ParseInputEvent`, and events pushed directly into the
root viewport with `GetTree().Root.PushInput(inputEvent)`, provided they reach
the autoload's `_Input` callback. Events delivered only to a different viewport
or native window do not reach this root-viewport autoload.

Godot calls `_Input` handlers in reverse tree order. An earlier handler can stop
an event from reaching this collector by calling `SetInputAsHandled()`. The
collector does not consume events itself. Regular GUI input handling happens
after `_Input`.

Collection obeys the node's process mode and `SetProcessInput`. With the default
inherited process mode, pausing the scene tree stops collection and the main
adapter's frame clearing until processing resumes.
