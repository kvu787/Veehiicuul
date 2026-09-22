# Godot input latency experiment

Minimal Windows 11 x64 application using Godot **4.7.2 .NET**, C#, and two 3D spheres with an event display.

Double-click **Build.cmd** to build a standalone release export, then double-click **Run.cmd** to launch it. Run.cmd exits with an error if the build is missing or incomplete; rebuild after changing the project. Close the game using its window close button. The launcher prints the game process ID for PresentMon.

The solution is `InputLatencyGodot.slnx`, with `Debug`, `ExportDebug`, and `ExportRelease` configurations. Build.ps1 builds the `ExportRelease` configuration through this solution. Both .cmd files are simple wrappers for their corresponding PowerShell scripts.

The project disables shared C# compilation so Godot's Windows console wrapper can exit after exporting. Otherwise, the wrapper can wait for an idle compiler server and delay launching the application.

The build script uses the installed Godot .NET editor and matching .NET export templates under `%UserProfile%\Program\Godot_v4.7.2-stable_mono_win64`. The project targets the installed **.NET 10 SDK**. Godot packages come from that installation; the first release export may download .NET runtime packages from NuGet. The exported executable and its supporting files stay together in `Build`. Running an existing export does not require the editor, export templates, or SDK.

## Input display

The interface uses a 1780 x 720 logical canvas. The initial window follows Windows display scaling (2670 x 1080 physical client pixels at 150%), capped to 90% of the monitor's usable area and centered. Resizing or maximizing scales the text, stick circles, 3D markers, and mouse crosshair together. The layout stays centered with its original proportions; spare space is letterboxed. Canvas-items stretching keeps text and 3D rendering at the physical output resolution. This follows Godot's [multiple-resolution scaling](https://docs.godotengine.org/en/stable/tutorials/rendering/multiple_resolutions.html). An explicit engine `--resolution` argument overrides automatic startup sizing.

- The spheres and numbers show the selected gamepad's left and right sticks side-by-side, with the left stick on the left. Positive X is right; positive Y is down. There is no application deadzone, interpolation, or smoothing. Godot's controller mapping and device processing still apply.
- The application keeps the first selected gamepad while it remains connected. Other gamepads' axis/button events are ignored. When the selected pad disconnects, both stick positions reset and another connected pad is selected, or the display waits at zero. All controller connection changes appear in the gamepad feed.
- As agreed, stock Godot combines physical keyboards into one logical keyboard and mice into one logical mouse. There is no physical keyboard/mouse selection, connection enumeration, or per-device connection notification. Missing devices require no initialization; the feeds accept events whenever Windows/Godot supplies them, including after reconnection. No keyboard or mouse is required to start the application.
- Three independent feeds show key down/up/repeat, mouse motion/buttons/wheel, and selected gamepad axes/buttons. Each retains the latest seven events and counts all received events. Newest events appear at the bottom. Fast streams can replace entries before they are displayed; this is not a complete event recorder.
- Space and left mouse button have held-state indicators. A game-rendered crosshair follows the sampled mouse position. The ordinary Windows cursor remains visible and uncaptured. Click the window to focus keyboard/mouse input.

Event timestamps are application receipt times in seconds since engine startup, **not device timestamps or latency measurements**. Individual events are kept only in memory; no per-event console or disk logging runs during measurement.

## Frame sequence and experiment settings

The only application update is the main-thread `_Process` callback:

1. `DisplayServer.ProcessEvents()` refreshes Godot's available Windows input.
2. Read the selected controller axes, mouse position, and button/key state.
3. Update the 3D spheres and event display for rendering.

`_Input` only records events, and connection callbacks maintain controller selection. There is no `_PhysicsProcess`, timer-driven game update, or application polling thread. Disabling accumulated input preserves more Godot-delivered events; it does not bypass Windows event coalescing or poll physical hardware directly.

| Setting                    | Value                          |
| -------------------------- | ------------------------------ |
| Rendering                  | D3D12, Forward+                |
| Window                     | DPI-scaled, resizable          |
| VSync                      | Disabled                       |
| Application FPS limit      | None                           |
| Render thread mode         | Safe (1)                       |
| Swap-chain images          | 2                              |
| Frame queue size           | 2                              |
| Physics interpolation      | Disabled                       |
| Accumulated input          | Disabled                       |
| Graphics backend fallbacks | Disabled                       |
| Process priority           | Normal Windows launch priority |

Apply the intended **100 FPS NVIDIA driver cap** to `Build\InputLatencyGodot.exe` separately, matching the C++ app's driver settings. The launcher does not alter driver settings or start PresentMon. Capture the standalone game process, not the editor/import/export processes. Run keyboard, mouse-click, and mouse-motion experiments separately as described in the earlier conversation. PresentMon's automatic keyboard/mouse metrics must not be assumed to cover gamepads.

The live text display has a rendering/allocation cost. This app does not establish the hypothetical 500-microsecond workload or a measured input latency; confirm workload and valid PresentMon samples before comparing results.

## Logs and verification

Each build or launcher invocation creates `MyLogOutput\yyyy-MM-dd_HH-mm-ss`. Builds write `Build.log`, `Import.log`, and `Export.log`; runs write `Launcher.log`, the game log, and `Session.json`. The session records the engine version, executable, process ID, renderer, and relevant settings. `Build`, `.godot`, and `MyLogOutput` are gitignored. Use the launcher for experiment runs so startup diagnostics go into the session folder.

```powershell
.\Build.cmd
.\Run.cmd
.\Run.cmd -Test
.\Run.cmd -VisualTest
```

After building, `-Test` runs synthetic checks in the existing exported headless application: absent devices, arbitrary controller IDs, stable selection, filtering, disconnect/failover/reconnect, bounded event history, held key/button state, and Godot event dispatch into the scene. The success message reports the number of checks completed. `-VisualTest` runs the same checks using D3D12, verifies that both 3D markers project to their expected locations on the logical canvas, and saves its own rendered viewport as `Verification.png`, then exits. Neither option rebuilds. Verification injects synthetic Godot events; do not use those sessions for latency measurements. Headless metadata describes configured rendering settings, not an active D3D12 window.

On September 18, 2026, `Run.cmd -Test` and `Run.cmd -VisualTest` both completed without intervention after disabling shared compilation. Each passed 19 checks, and both release builds had zero warnings/errors. The D3D12 run used the expected rendering settings, and its 1280 x 720 image was inspected. No physical gamepad was connected during these runs. Physical unplug/replug and multiple-controller hardware behavior still need a manual check; automated checks exercise the selection logic without changing the user's devices.

For a manual check: launch with any devices absent, attach a controller, move each stick independently, attach a second controller, disconnect the selected controller, and reconnect it. Check that selection stays stable until disconnection, the remaining pad takes over, and the display returns to zero when none remain. Type, move/click/scroll, and reconnect keyboard/mouse devices to check the Windows/Godot event path.

API references: [Input](https://docs.godotengine.org/en/4.7/classes/class_input.html), [DisplayServer.ProcessEvents](https://docs.godotengine.org/en/4.7/classes/class_displayserver.html#class-displayserver-method-process-events), [InputEvent device IDs](https://docs.godotengine.org/en/4.7/classes/class_inputevent.html).
