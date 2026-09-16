# Conversation: Godot input latency application

## User

read this conversation:
"C:\Users\k\Repository\Veehiicuul\Conversations\2026-09-15-GodotInputLatencyComparison.md"

Implement a minimal godot 4.7.2 c# 3d app in C:\Users\k\Repository\Veehiicuul\InputLatency_Godot that will serve as the godot app for input latency experiments.

this app must
- display the position of the gamepad's right stick
- display the stream of keyboard, mouse, and gamepad events
- not assume that any particular input device is currently connected
- handle connection/disconnection of input devices during game runtime
- if multiple of the same type of input device is connected, pick either one to display input and events from

## Assistant

I’ll read the latency discussion and repository instructions, then build and check the Godot input display app.

## Assistant

Godot combines physical keyboards and mice into one logical keyboard and mouse. For this experiment, should I keep that stock Godot behavior and select just one gamepad, or add Windows-specific input code to select one physical device of each type?

- Use stock Godot input (recommended for this comparison)
- Select one physical device of each type

## User

Use stock Godot input (recommended for this comparison)

## Assistant

I’ll keep stock Godot keyboard and mouse input, as you chose, and retain one gamepad until it disconnects. The app will use one per-frame update, refresh input just before reading it, and run with the D3D12 settings from the earlier discussion.

## Assistant

The app is implemented: it shows the right stick as a 3D marker and numbers, with separate keyboard, mouse, and gamepad event feeds. Each feed keeps seven recent events so mouse activity cannot grow the display history indefinitely. I’m checking the release build and device-selection behavior now.

## Assistant

The C# build passes. I fixed a solution-file requirement in Godot’s exporter and added a check so the launcher stops if an export error is logged. Next I’ll use the computer-use skill to check the rendered window and its keyboard and mouse responses.

## Assistant

The release export and all 13 input-state checks pass. A standalone launch also confirmed D3D12, a 1280×720 window, VSync off, and a connected gamepad.

Computer Use was not approved to access the app, so I couldn’t use it to test the window. I’m finishing a render check inside the app and documenting what still needs a physical-device test.

## Assistant

The final release passes all 15 checks, and I inspected its rendered D3D12 output. Both build-and-test modes work. I’m recording the usage instructions and conversation, then committing the app separately from the conversation log. Physical unplug/replug testing remains a manual check.

## Assistant

Implemented and committed. Double-click [Run.cmd](C:/Users/k/Repository/Veehiicuul/InputLatency_Godot/Run.cmd) to build and launch.

- 3D right-stick display and keyboard/mouse/gamepad event feeds.
- Automatic gamepad selection and disconnect/reconnect handling.
- Stock combined keyboard/mouse input, as agreed.
- D3D12 experiment settings and timestamped session logs.

Release build, 15 automated checks, and rendered-image inspection passed. Physical unplug/replug testing remains manual.

[Usage and experiment notes](C:/Users/k/Repository/Veehiicuul/InputLatency_Godot/Readme.md)
