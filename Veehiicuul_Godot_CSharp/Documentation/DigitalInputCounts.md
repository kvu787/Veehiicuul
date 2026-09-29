# Digital input counts

The `DigitalInputCounts` autoload records digital down events delivered to its
`_Input` callback. Query its static methods from `Main.Process` or code called by that method:

```csharp
int spacePresses = DigitalInputCounts.GetKeyDownCount(Key.Space);
int letterPresses = DigitalInputCounts.GetKeyDownCount(Key.A);
int leftClicks = DigitalInputCounts.GetMouseButtonDownCount(MouseButton.Left);
int wheelUpEvents = DigitalInputCounts.GetMouseButtonDownCount(MouseButton.WheelUp);
int gamepadPresses = DigitalInputCounts.GetGamepadButtonDownCount(JoyButton.A);
int touchDowns = DigitalInputCounts.GetTouchDownCount(0);
bool quitPressed = DigitalInputCounts.GetKeyDown(Key.Escape);
bool clicked = DigitalInputCounts.GetMouseButtonDown(MouseButton.Left);
bool resetPressed = DigitalInputCounts.GetGamepadButtonDown(JoyButton.X);
```

- The autoload instance is private. Static queries access it internally and throw
  if the autoload is not in the scene tree.
- Counts start at zero. Multiple presses between game updates accumulate, even if
  each input was released before the update. Reading a count does not consume it.
- Keyboard auto-repeat and releases do not count. Keyboard queries use logical
  key codes; events with no logical key code are not counted.
- The boolean helpers return whether the corresponding count is greater than zero.
  They do not consume events or count repeated presses as separate game updates.
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

`Main_GodotAdapter._Process` clears all counts immediately after `Main.Process`
returns successfully, including early returns. Exceptions are logged and quit the
application. This keeps the
project's single frame callback and also works when rendering is disabled.
Read counts during the game update, before this boundary. Deferred work after
the update sees zero; input arriving after the boundary belongs to the next update.
If the main adapter stops processing while the collector continues receiving
input, counts accumulate until the adapter's next update.

`InputManager` uses these digital queries for quit, car reset, camera follow, and
zoom reset. Analog axes and held zoom-modifier input are polled from gamepad 0;
button-down counts include every gamepad. With no gamepad 0 connected, its analog
axes read as zero and the car continues coasting until another input changes it.

The autoload uses `Scenes/DigitalInputCounts.tscn`, a `Node` with the C# script
attached. The explicit node type lets the editor load it during a fresh asset
import before any C# assembly exists. The release export attaches the compiled
script to that same node; no separate Debug build is needed for importing.

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
