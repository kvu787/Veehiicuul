# Input latency / C++ / DirectX 12

A standalone Windows 11 x64 application using Microsoft's GameInput v3 API to measure reading-to-display latency for connected gamepads. A bundled PresentMon v2.6.0 decoder tracks Windows display events asynchronously.

Double-click `Build.cmd`, then `Run.cmd`. The Release executable is `BuildOutput\Release\InputLatency.exe`. The application, source, dependencies, build output, and session logs all live within this folder. It does not reference or modify another application in the repository. This folder can be copied and used independently.

## Build and run

Development requires Visual Studio 2022 or newer with the Desktop development with C++ workload, C++ CMake tools, and a current Windows SDK containing `dxc.exe`. Use modern `pwsh`. Only Windows 11 x64 is supported.

```powershell
.\Build.ps1 -Test
.\Build.ps1 -Configuration Debug -Test
.\Run.ps1
.\Run.ps1 -DurationSeconds 30
.\Run.ps1 -Configuration Debug
```

The `.cmd` launchers pass their arguments directly to these PowerShell scripts. `Run.cmd` fails if its selected build does not exist. Build scripts discover the installed x64 compiler and CMake/Ninja tools. Release uses `/O2`, whole-program optimization, link-time code generation, and the static Microsoft C++ runtime.

The GameInput SDK and unmodified x64 runtime are included in `ThirdParty/GameInput`. Builds do not download anything or install GameInput system-wide. The build copies the runtime components and original licenses beside the executable. The application loads that bundled runtime explicitly. No Visual Studio installation or Visual C++ redistributable is required to run an already built Release package.

For distribution, copy `InputLatency.exe`, `GameInputRedist.dll`, `GameInputBridge.dll`, `GameInputRawInputProxy.exe`, `GameInputLicense.txt`, and `GameInputNotice.txt` together. Windows supplies the DirectX 12, DXGI, and font services. See [GameInput provenance](ThirdParty/GameInput/Provenance.md) for the package version, checksum, and license locations.

Include `PresentMonLicense.txt` in that package as well. The display decoder is linked into the executable; no PresentMon installation, service, or external executable is needed. See [decoder provenance](ThirdParty/PresentMon/Provenance.md).

Windows display-event tracing requires administrator or Performance Log Users permissions. `Run.cmd` requests Windows UAC elevation only when neither permission is present. Direct executable launches without tracing permission retain the analog dashboard and explicitly show that display latency is unavailable. The app never substitutes Present timing for a missing display event.

## Use

### Application requirements

- Use a content space of exactly 1200 px width * 1300 px height.
- Don't make the application contents responsive at all, except for the background color. This means that it should always render the content to the 1200*1300 content space and then fill the full window with the background color. If the window dimensions cut off content, that is fine.
- Allow window resizing.
- Use only one size of font. No upscaling/downscaling either.
- Use a simple layout flow in which things stack from left to right and top to bottom.
- Display tracking and input diagnostics use one textual item per line, such as `Shown frames = value` and `Callback drops = value`.

These requirements are also recorded in [AGENTS.md](AGENTS.md) for future application changes.

The application starts in borderless fullscreen on its startup monitor. Press F11 to switch to a resizable window or back to borderless fullscreen.

Activate the application window, then use gamepad buttons, sticks, and triggers. Each gamepad has its own statistics. Gamepad connection and disconnection are tracked throughout the session. Only the gamepad input kind is registered for device and reading callbacks and sampled per frame; mouse and keyboard inputs do not contribute measurements. Keyboard shortcuts below control the window.

| Control           | Action                                        |
| ----------------- | --------------------------------------------- |
| F11               | Toggle borderless fullscreen                  |
| PageUp / PageDown | Scroll the device list by one device          |
| Esc / Alt+F4      | Exit and finish writing the measurement files |

The persistent controller panel selects one connected gamepad automatically. It displays its name, full GameInput device identifier, device index, and vendor/product IDs. The first connected gamepad is chosen initially; it stays selected while connected. If it disconnects, another connected gamepad is chosen. Scrolling the latency device list does not change the selected controller.

Both analog sticks have position diagrams and signed X/Y values. Both triggers have fill bars and normalized numeric values. Stick values use -1..+1 with positive Y pointing up; trigger values use 0..1. Numeric values show four decimal places. The application adds no deadzone, smoothing, or filtering to the displayed values. If the controller disconnects or has no available gamepad reading, values are shown as `--` instead of retaining stale analog states.

Controller diagrams and numeric values use one gamepad reading every rendered frame, sampled after the GPU and presentation waits. Both sticks, both triggers, and buttons share that reading's original timestamp. Composite controllers are polled specifically for their gamepad stream. The controller panel stays visible independently of the latency rows. Displayed frames that visualize the selected gamepad contribute to its display statistics even when its latency row is outside the visible list.

Latency rows show callback, late sample, and reading-to-display statistics in milliseconds. Their right-hand indicators change immediately for new gamepad input and held gamepad buttons. Latency statistics text and controller identity/selection refresh four times per second. The content space is fixed at 1200x1300 pixels, anchored at the top left. Resizing changes neither content coordinates, panel widths, trigger bar widths, font size, nor the number of laid-out gamepad rows. Larger windows show extra background; smaller windows clip content. The initial windowed client size is 1200x1300, and there is no application-enforced minimum window size. Display statistics arrive asynchronously after Windows reports presentation, typically with an ETW buffer delay. That reporting delay is excluded from the measured latency.

Display tracking and input diagnostics have separate bordered panels containing vertical `Label = value` lists. All text uses the same fixed font size, with no scaling. The header, display tracking, controller panel, visible gamepad latency rows, input diagnostics, and shortcuts follow one top-to-bottom flow. Controls inside the controller panel are arranged from left to right. Diagnostics follow the visible gamepad data instead of being anchored to the bottom of the window. The display panel retains tracing-permission, lost-record, and unfocused messages; unavailable display counters show `--`.

The dashboard uses only grayscale colors, with equal red, green, and blue components. Text, controller markers, activity indicators, and warnings use brightness differences against dark gray backgrounds.

The foreground application records measurement statistics. Unfocused input, if delivered by GameInput, is marked in raw logs and excluded from callback statistics. Focus transitions establish fresh baselines. Minimized windows wait for window availability and do not render.

Composite or virtual devices may expose several input kinds. Only devices supporting gamepad input are listed, and only their gamepad stream is measured. The list preserves GameInput's device identities instead of assuming that every HID interface is a distinct physical peripheral. Up to 128 gamepad device identities are retained per session, including disconnected devices. Additional device/metadata errors are reported explicitly.

## What is measured

The beginning of each input measurement is `IGameInputReading::GetTimestamp()`. Callback, sample, and Present endpoints use `IGameInput::GetCurrentTimestamp()` directly. The display endpoint is the matched frame's Windows display-event QPC timestamp, converted into the GameInput clock.

Each frame brackets its pre-Present GameInput clock observation with two QPC calls. Conversion uses the midpoint of that bracket and `QueryPerformanceFrequency()`, without assuming equal clock epochs. Only the relative QPC duration is converted and rounded; addition to the integer GameInput anchor preserves single-microsecond precision even with large epochs. The reported uncertainty is half the bracket duration plus one microsecond for GameInput quantization. Brackets with uncertainty above 100 microseconds are rejected. Clock progression is checked approximately once per second; a discrepancy above 200 microseconds rejects measurements until validation recovers. This validates the observed clock relationship rather than asserting an undocumented API clock origin. Calibration uncertainty does not cover every possible Windows event or driver timing error.

| Metric                   | Endpoint / meaning                                            |
| ------------------------ | ------------------------------------------------------------- |
| Reading to callback      | Entry into GameInput's reading callback                       |
| State-change interval    | Time between consecutive callback readings for the device     |
| Reading to late sample   | Return from the render thread's fresh per-device reading poll |
| Reading to display       | First matched Windows display event for a visualized reading  |
| Reading to Present begin | Immediately before the frame's CPU call to DXGI `Present`     |
| Present call duration    | Time spent inside that CPU `Present` call                     |

These are **software timings**, not physical switch-to-photon measurements. They do not identify when a switch physically closed, a USB report was emitted, or a displayed pixel changed. Hardware debounce, firmware processing, USB/Bluetooth transport, display scanout, and panel response are not separately measurable through this application. Do not use these results to rank the complete hardware latency of different peripherals. External synchronized input and optical instrumentation is required for that measurement.

The render thread records the exact per-device reading used by each frame. The background decoder tracks the app's process, including DWM composition dependencies. Correlation requires the same swap-chain identity and render thread, with the DXGI Present-start event inside that frame's recorded Present-call QPC interval. Matching does not assume that the nth rendered frame is the nth displayed frame.

Reading-to-display statistics include each eligible reading once, when it first appears in a matched displayed frame and is visualized in the controller panel or a visible latency row. A discarded frame contributes no display latency. If its reading is reused by a later displayed frame, that later event can measure the reading. Unchanged snapshots retain their original timestamp; repeated displays are logged as state age but do not repeatedly count the same reading as new input latency. Input updates superseded before any rendered frame uses them have no reading-to-display measurement.

Present-begin statistics remain in the CSV logs and only include new readings for visualized devices with an accepted presentation. A successful `Present` does not prove the frame was displayed. Late-sample statistics cover all connected devices. Callback logs preserve all callback-delivered state changes.

Missing, lost, unmatched, and discarded presentation results are recorded separately. Unresolved frames expire after five seconds or at shutdown. There is no fallback to Present time. ETW loss, decoder overflow, and queue loss appear in diagnostics and the dashboard; measurements are rejected while ETW loss is known. The display event is a Windows presentation boundary, not an optical measurement of panel response or of the particular pixel's scan-out time. Tearing can display only part of a frame.

Shutdown allows 100 milliseconds for the final submitted image to reach presentation before disabling tracing. This wait does not affect rendering or input acquisition during measurement.

Initial cached readings are baselines, not latency observations. Repeated current readings are deduplicated by COM object identity, retaining the previous reading reference. Equal timestamps can belong to distinct readings and remain valid. Future timestamps are rejected instead of subtracting unsigned values. Percentiles use the nearest-rank method over the latest 8192 accepted observations for each metric; count, minimum, mean, and maximum cover the entire session.

State-change intervals are **not USB polling rates**. GameInput reports state changes; idle periods, button presses, and stick motion affect their distribution. Reading callbacks run on GameInput's worker and include callback scheduling delay. Rendering directly polls the cached stream instead of waiting for these callbacks.

## Low-latency configuration

The rendering policy follows the reference scene's `MinimizeInputLatency` configuration:

- DirectX 12 with vendor-neutral adapter selection; WARP is available for functional testing.
- Two flip-discard swap-chain buffers.
- One unfinished GPU frame in total, enforced before allocator/upload-buffer reuse.
- DXGI maximum presentation latency of one and its frame-latency waitable object.
- Spin polls with `YieldProcessor` while waiting for GPU completion and presentation admission; window messages are serviced during and after the waits.
- VSync off: `Present(0, DXGI_PRESENT_ALLOW_TEARING)` when DXGI supports tearing, otherwise `Present(0, 0)`.
- No frame-rate limiter, timer-based frame pacing, or timed delays on the render/input paths.
- Fresh input sampling after readiness waits and command setup, immediately before the small dashboard draw.
- Above-normal render-thread scheduling, without real-time scheduling or forced affinity.
- Preallocated, separate callback-to-logger and render-to-logger queues. Disk writes and percentile sorting run on a background thread.
- Frame-to-display correlation and ETW decoding use separate background workers. Rendering never waits for a display-event result. The extra clock observations are three QPC calls around each Present.

This policy aims to reduce software backlog. It cannot guarantee the lowest physical input latency on every machine. Spin polling intentionally uses CPU time while waiting. OS scheduling, CPU contention, graphics drivers, display configuration, and the Windows presentation path can influence results. Avoid unrelated load when comparing sessions.

## Session files

Every launch creates `LogOutput/yyyy-MM-dd_HH-mm-ss/`. Direct executable launches locate this application's folder relative to the executable, without hard-coded repository paths. Build invocations also write a timestamped `Build.log`. `LogOutput/` and `BuildOutput/` are ignored by Git.

| File                       | Contents                                                         |
| -------------------------- | ---------------------------------------------------------------- |
| Application.log            | Runtime, graphics policy, adapter, selected controller, errors   |
| Launcher.log               | PowerShell launcher transcript                                   |
| Devices.csv                | Device identities and connection/disconnection events            |
| Readings.csv               | Reading/callback timestamps and state-change intervals           |
| Presentations.csv          | New-reading sample and Present timestamps, visibility flag       |
| DisplayFrames.csv          | Frame, Present QPC, display QPC, mapped timestamp, result status |
| DisplayReadings.csv        | Reading/frame identity, display latency, clock uncertainty       |
| DisplayDiagnostics.txt     | Display counts, trace loss, correlation errors, clock errors     |
| Summary.csv                | Per-device statistics for all six metrics, written at exit       |
| MeasurementDiagnostics.txt | Dropped records, invalid clocks, device and polling errors       |
| Dashboard.png              | Optional GPU frame capture for verification                      |

Default session directories are reserved exclusively. When the current timestamp is occupied, startup waits for the next unused timestamp while preserving the required `yyyy-MM-dd_HH-mm-ss` naming format. Explicit `--log-directory` values may contain launcher/build files, but must not already contain `Application.log`. The application reserves that file atomically and rejects reuse without changing an existing session.

Raw measurement units are microseconds. Device indexes join the CSV files to `Devices.csv`. The logs record gamepad timings and device metadata. `Application.log` records `MeasuredInputKind=Gamepad`, and device/summary kind fields contain `Gamepad`. Both queues have 65536 records. Queue overflow drops measurement records, increments diagnostic counters, and never delays live rendering. Any nonzero drop count means the logs are incomplete. Logging failures appear in the dashboard and cause a nonzero application exit.

## Verification

`Build.ps1 -Test` runs six tests: arithmetic/statistics/queues, controller state, display clock conversion/correlation, session-log validation, concurrent session-directory isolation, and a short hidden DirectX 12 WARP/GameInput smoke test. Session validation accepts a known-good fixture, rejects ten corruptions, and checks that a rejected clock conversion cannot contribute latency. Session-directory tests launch two copies concurrently and verify that reusing an occupied directory leaves existing logs untouched. Debug builds enable the DirectX 12 debug layer when available and fail if it reports errors or corruption. The bundled event decoder uses its production behavior in both configurations to avoid upstream modal assertion dialogs on a background tracing thread.

`Tools/VerifyDisplayTracking.ps1` performs a short visible hardware-adapter test. Use gamepad buttons, sticks, and triggers during its eight-second session. It checks that all logged devices/statistics are gamepads, checks display correlation and timestamp subtraction, and reports when no fresh gamepad readings were displayed. Physical gamepad input remains a manual check when no such readings are collected.

`Tools/VerifyWindowLifecycle.ps1` runs a 35-second visible Debug session that checks borderless fullscreen startup and restoration of the 1200x1300 windowed client area, minimizes/restores, maximizes, toggles F11 fullscreen, and resizes to an 800x600 client area with clipped content. Click the application after restore if Windows blocks programmatic activation. It records action QPC timestamps, verifies that presentation pauses while minimized and resumes afterward, captures the clipped dashboard, and validates the session logs.

`Tools/ValidateSession.ps1` independently checks device identities, timestamp subtraction/order, frame joins, first-display deduplication, and every summary count/aggregate/rolling percentile against the CSV records. By default, any clock error fails validation. `-AllowRejectedClockFrames` permits explicitly recorded clock-rejection frames only when they have no accepted conversion or display-reading observation; other diagnostic errors still fail validation.

```powershell
.\Tools\VerifyWindowLifecycle.ps1
.\Tools\ValidateSession.ps1 -LogDirectory .\LogOutput\2026-10-07_03-19-44 -RequireGamepad -RequireDisplayMeasurements
```

See the [verification record](Documentation/Verification.md) for the completed checks and remaining manual checks.

```powershell
.\Run.ps1 -DurationSeconds 3 -SoftwareAdapter -Hidden
.\Run.ps1 -DurationSeconds 3 -Hidden -CaptureFrame
```

The hidden and software-adapter options are for functional tests. Use a visible, focused hardware-adapter session for measurements. `-CaptureFrame` saves the last rendered frame after measurements stop, without adding readbacks to the measurement loop. The smoke test captures its dashboard automatically.

## API references

- [Microsoft GameInput package](https://www.nuget.org/packages/Microsoft.GameInput/3.5.283).
- [GameInput readings and polling](https://learn.microsoft.com/en-us/xbox/gdk/docs/features/common/input/overviews/input-readings).
- [GameInput callbacks and serialized dispatch](https://learn.microsoft.com/en-us/gaming/gdk/docs/features/common/input/advanced/input-callbacks).
- [Reading timestamps](https://learn.microsoft.com/en-us/gaming/gdk/docs/reference/input/gameinput/interfaces/igameinputreading/methods/igameinputreading_gettimestamp).
- [PresentMon v2.6.0 decoder](https://github.com/GameTechDev/PresentMon/tree/v2.6.0/PresentData).
- [DXGI Present behavior](https://learn.microsoft.com/en-us/windows/win32/api/dxgi/nf-dxgi-idxgiswapchain-present).
- [DXGI maximum frame latency](https://learn.microsoft.com/en-us/windows/win32/api/dxgi1_3/nf-dxgi1_3-idxgiswapchain2-setmaximumframelatency).
