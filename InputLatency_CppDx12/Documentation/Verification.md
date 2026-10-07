# Verification

Verified on Windows 11 x64 on October 6, 2026, using MSVC 19.51.36260.0 and the included GameInput 3.5.283.0 runtime.

| Check                                 | Result                                                           |
| ------------------------------------- | ---------------------------------------------------------------- |
| Optimized Release build               | Passed with `/O2`, `/GL`, `/LTCG`, static CRT, and `/W4 /WX`     |
| Debug build                           | Passed with `/W4 /WX`                                            |
| Release measurement tests             | Passed                                                           |
| Debug measurement tests               | Passed                                                           |
| Release WARP/GameInput smoke test     | Passed; hidden three-second session                              |
| Debug WARP/GameInput smoke test       | Passed; hidden three-second session with debug-message checks    |
| GPU dashboard capture                 | PNG inspected; readable layout without clipping                  |
| Release launcher/hardware-adapter run | Passed; NVIDIA GeForce RTX 5090 Laptop GPU                       |
| Isolated runtime package              | Passed from an unrelated working directory                       |
| GameInput device enumeration          | Eleven device identities, including an Xbox 360 controller       |
| Measurement diagnostics               | Zero dropped records, invalid clocks, device errors, poll errors |
| PowerShell script parsing             | Build, Run, and ImportGameInput passed                           |
| Executable import inspection          | Windows system DLLs only; no dynamic Visual C++ runtime import   |

Measurement tests cover future-clock rejection, equal timestamps, microsecond subtraction, cumulative statistics, rolling-window nearest-rank percentiles, queue capacity/overflow, and ordering of one million observations between concurrent producer and consumer threads.

The isolated package contained only the executable, three bundled GameInput runtime files, and two original license/notice files. It rendered 22773 frames in approximately three seconds, enumerated the devices, saved its PNG and CSV files into its own `LogOutput` folder, and exited successfully. This frame count is functional test evidence, not a latency benchmark or display frame-rate claim.

The automated sessions were hidden and unfocused. They did not collect foreground input observations or measure physical input-to-photon latency. Manually exercise mice, keyboard keys, gamepad buttons/sticks/triggers, focus transitions, hotplug, device scrolling, and fullscreen in a visible focused session before relying on a particular device's measurement results.

Build/test artifacts and captures remain in ignored `BuildOutput` and `LogOutput` folders within this application. No reference-application files were changed.

## Selected controller analog panel

The controller panel follow-up passed all three tests in both Release and Debug: measurement tests, controller-state tests, and the hidden three-second WARP/GameInput application smoke test. Debug rendering reported no errors or corruption.

The Release dashboard capture from `LogOutput/2026-10-06_18-05-11/Dashboard.png` was inspected. It displayed the connected Xbox Controller's name, complete GameInput identifier, both stick diagrams with signed X/Y values, and both trigger bars with normalized values. The actual cached controller reading was neutral. The panel and identifier were readable without clipping, and the session reported zero dropped records, invalid timestamps, metadata errors, or unexpected polling errors.

Controller-state tests cover multiple eligible devices, keeping a connected selection, selecting another gamepad after disconnection, clearing unavailable selections, stick direction mapping, preserving small drift, trigger range mapping, and signed numeric precision. Automated checks did not exercise physical stick/trigger motion or physically disconnect multiple gamepads; those remain visible-session checks.

## Grayscale palette

The grayscale update passed the optimized Release build and all three existing tests. The dashboard capture at `LogOutput/2026-10-06_18-12-05/Dashboard.png` was visually inspected for readability. All 1,100,800 pixels were also checked numerically: zero pixels had unequal red, green, and blue values. Backgrounds, text, stick markers, trigger bars, device indicators, and warning colors all use grayscale. The modified Build and Run PowerShell scripts passed parsing checks.

## Reading-to-display measurement

The application now links a self-contained PresentMon v2.6.0 display decoder and correlates its own frame submissions with Windows display events. Unit tests cover different QPC/GameInput epochs, frequency conversion, calibration uncertainty, preempted clock samples, overflow, frame/thread/swap-chain matching, and invalid reading/sample/Present/display ordering.

Both Release and Debug passed all four tests: MeasurementTests, ControllerStateTests, DisplayTimingTests, and ApplicationSmoke. The application's Debug configuration retains DX12 debug validation. An upstream decoder CRT assertion initially blocked Debug shutdown with a modal dialog; using the upstream production decoder behavior in both configurations resolved that timeout. Decoder warnings still have explicit counters.

The visible hardware-adapter run in `LogOutput/2026-10-06_23-23-41` submitted 14956 frames and matched 14928 displayed frames plus 28 discarded frames. It had zero unresolved frames, unmatched events, ETW event/buffer loss, queue drops, invalid clocks, decoder warnings, or decoder overflows. Maximum accepted clock-bracket uncertainty was 3.45 microseconds. The dashboard capture was visually inspected for readability and grayscale appearance.

The WARP smoke run in `LogOutput/2026-10-06_23-20-01` also exercised composition tracking: 98 displayed frames, 1334 discarded frames, and 18 unresolved frames. Unresolved results were recorded as missing display events and contributed no display latency. Its maximum accepted clock-bracket uncertainty was 1.1 microseconds, with zero trace loss, queue drops, or invalid clocks.

`Tools/VerifyDisplayTracking.ps1` ran in `LogOutput/2026-10-06_23-29-37`, matching 295 displayed frames with zero trace loss, queue drops, or clock errors. This machine's GameInput path did not expose the synthetic mouse input, so it collected no fresh input-to-display observations. No controller was connected during these new runs. Physical mouse, keyboard, and gamepad motion remain manual checks; these runs establish frame/display correlation and clock conversion, not a physical latency benchmark.

The final Release/Debug suites also cover the strengthened input timeline checks and the shutdown-only 100-millisecond trace drain. Build, Run, and VerifyDisplayTracking PowerShell scripts passed parser checks. The render thread never waits for display-event delivery; the original uncapped, tearing-enabled, single-GPU-frame policy remains active.

The final hardware verification in `LogOutput/2026-10-06_23-36-59` matched 276 displayed and 9734 discarded frames out of 10010 submissions, with zero unresolved frames, unmatched events, trace loss, queue drops, clock errors, decoder warnings, or decoder overflows. Its maximum accepted clock-bracket uncertainty was 1.1 microseconds. Fresh synthetic input remained unavailable through this GameInput path.

## Gamepad-only measurement

Verified on October 7, 2026. Device and reading callback registration and per-frame polling now request only `GameInputKindGamepad`. Device enumeration rejects metadata without gamepad support, and reading callbacks reject readings without the gamepad input kind. Mouse/keyboard state extraction and the mouse-position visual have been removed. Keyboard window-control shortcuts remain available.

The optimized Release and Debug builds each passed all four existing tests. The updated `VerifyDisplayTracking.ps1` passed its PowerShell parser check and hardware-adapter run in `LogOutput/2026-10-07_02-57-49`. It checks that device and summary rows contain only the `Gamepad` kind and verifies display timestamp subtraction without generating synthetic mouse input.

That hardware session matched 297 displayed frames and 9585 discarded frames, with 121 unresolved frames and 121 unmatched events. Trace loss, queue drops, clock errors, decoder warnings, decoder overflows, and input measurement diagnostics were all zero. Maximum accepted clock-bracket uncertainty was 1.15 microseconds. Its dashboard capture was inspected: gamepad-only labels and the no-controller state were readable without clipping.

No gamepad was connected during these runs; no devices or input observations were logged. Physical gamepad button/stick/trigger motion and connection/disconnection remain manual checks. The runs verify the builds, empty-device behavior, and display tracking, without establishing a physical gamepad latency result.

## Connected-controller validation

Completed on October 7, 2026 with the connected Xbox 360 Controller for Windows (GameInput vendor/product 3537:10C6). The final Release and Debug builds each passed all six CTest tests, including the new session-validation and concurrent-session regression tests.

The user exercised both sticks, both triggers, buttons, D-pad, shoulders, and stick clicks, unplugged/reconnected the controller, and toggled F11. The user confirmed that the controls/activity indicator responded correctly and that values became unavailable while unplugged. Captures show full trigger values of 1.0000, released values of 0.0000, positive and negative stick directions, neutral sticks, and small unfiltered drift. All captures were visually inspected; all pixels in the two physical-run captures and final minimum-size capture were numerically grayscale.

The unplug interval was 4.689541 seconds. Connection records retained the same device index and 64-character GameInput identity after reconnect. No fresh presentation samples occurred inside the disconnected interval, allowing five milliseconds at its edges for status delivery. Measurement resumed with fresh readings.

The independent validator checked 301396 first-display readings across the following sessions, including all six metrics' counts, last/minimum/mean/maximum values and rolling nearest-rank percentiles. It verified exact timestamp subtraction, timeline order, device/frame joins, deduplication, and exclusion of discarded/unresolved/rejected frames. These sessions preceded the large-epoch precision fix described below; they establish controller behavior and internal CSV consistency, while their converted timestamps can contain the original one-microsecond rounding error.

| Session                  | LogOutput folder      | First displays | Clock rejection |
| ------------------------ | --------------------- | -------------- | --------------- |
| Release physical input   | 2026-10-07_03-19-44   |          34946 |               0 |
| Debug unplug/reconnect   | 2026-10-07_03-22-42   |         104143 |               0 |
| Debug window lifecycle   | 2026-10-07_03-28-47   |          55706 |               0 |
| Final display helper     | 2026-10-07_03-35-55   |           6425 |               0 |
| Final minimum-size run   | 2026-10-07_03-36-04   |         100176 |               1 |

All five sessions had zero input-record drops, ETW event/buffer loss, submission/completion/measurement queue drops, decoder warnings/overflows, invalid input timestamps, and unexpected polling errors. The Release physical run had 86 unmatched events and 86 unresolved frames; the other four runs had zero of each. Unresolved and discarded frames contributed no latency. The main Release/Debug physical runs matched 35293/130066 displayed frames respectively, with no rejected clocks.

The final minimum-size stress run rejected one clock conversion (frame 15335). It recorded no converted timestamp and no display-reading row for that frame. Strict validation flagged the clock counter; explicit rejected-clock validation verified that the counter was fully explained by that excluded frame and that all remaining observations/summaries were correct. The source does not retain the failed clock bracket, so the rejection's precise cause cannot be identified from these logs. Maximum accepted bracket uncertainty across these five sessions was 96.5 microseconds, below the 100-microsecond acceptance threshold.

Window action timestamps establish that presentation stopped while minimized and resumed after restore. Maximize, borderless fullscreen, return to windowed mode, focus recovery, and resize to exactly 1024x720 completed. The final captured neutral/drift controller state and complete device ID were readable at minimum size. DX12 Debug validation reported no application errors or corruption.

Validation found and fixed three application issues: header text clipped at the minimum client size, same-second concurrent launches could mix session logs, and large GameInput epochs lost a microsecond during floating-point clock conversion. Headers are shorter. Default session directories are now created exclusively; startup waits for another timestamp when needed. Application.log is reserved atomically, so explicit reuse fails without overwriting existing records. Regression tests check concurrent launches, the timestamp naming format, normal shutdown, occupied-directory rejection, and unchanged hashes of the protected logs.

The precision regression used the actual observed GameInput epoch range and failed on the original implementation. The fix converts/rounds only the QPC delta and adds it to the integer GameInput anchor. Tests now cover the observed large epoch, an exact one-microsecond delta, the largest representable timestamp, and overflow. Both final configurations passed all six tests after this correction.

The display helper's hidden-launch setting was also corrected: it now launches visibly, checks for a real window, reports Windows foreground restrictions, and uses graceful shutdown on failure. The window helper waits briefly for the user to reactivate after restore. Early helper failures were corrected and rerun; the two traces left by interrupted test processes were stopped after confirming those processes had exited. The final trace inventory contained no InputLatencyDisplay sessions.

The isolated Release package in LogOutput/2026-10-07_03-45-48/PortablePackage passed when launched from the system temporary directory. Only the executable, bundled GameInput components, and licenses were copied. It enumerated the connected gamepad, matched 272 displayed frames, and passed strict log validation with zero trace loss, clock errors, queue drops, and logging failures. It had 601 unresolved frames, which were excluded, and no input observations because it was hidden/unfocused. The initial isolated-package run exposed the session-collision bug; that mixed-log test output in LogOutput/2026-10-07_03-40-57 is invalid and was excluded from the results above. The final precision-corrected package also ran successfully from the system temporary directory in LogOutput/2026-10-07_03-54-31, enumerated the controller, and passed strict log validation. That hidden run produced no matched display events or gamepad observations, so it verifies packaging and exclusion of unresolved frames rather than live display latency.

These checks validate software reading-to-display timing and controller handling. They do not measure physical switch-to-photon latency. Selection among multiple simultaneously connected gamepads was covered by the selection tests, but only one physical controller was available for these live runs.

The precision-corrected visible run in LogOutput/2026-10-07_03-53-05 remained unfocused and produced no fresh input observations or matched display events. That empty run was excluded from positive input/display validation. The final focused run below completes the previously pending live check.

## Final focused validation

Completed on October 7, 2026 in `LogOutput/2026-10-07_11-38-16` using the final Release build and connected Xbox 360 Controller for Windows. The visible run lasted 60.1436 seconds. A native foreground-window check confirmed that the application was focused during the run. The user was instructed to exercise both sticks, triggers, and buttons; the earlier physical-control and unplug/reconnect checks already passed.

Strict `ValidateSession.ps1 -RequireGamepad -RequireDisplayMeasurements` validation passed: one gamepad, 445318 callback readings, 68308 presentation readings, 68714 display-reading rows, and 68298 distinct first-display readings. All six summaries matched independently reconstructed observations, including timestamp subtraction, ordering, frame/device joins, first-display deduplication, cumulative statistics, and rolling nearest-rank percentiles.

All 72053 submitted frames were resolved: 69034 displayed and 3019 discarded. Discarded frames contributed no latency. There were zero rejected clocks, unmatched events, ETW event/buffer losses, queue/record drops, decoder warnings/overflows, invalid input timestamps, metadata errors, polling errors, or logging failures. Maximum accepted clock-bracket uncertainty was 92.35 microseconds. The final reading-to-display cumulative mean was 0.886343 milliseconds; the last 8192 observations had p95 1.677 milliseconds and p99 1.795 milliseconds. These remain software reading-to-display measurements, rather than physical switch-to-photon measurements.

The saved dashboard was visually inspected: gamepad-only labels, complete controller identity, both stick panels, trigger bars, and measurement rows were readable. The application exited normally, with no remaining application process or `InputLatencyDisplay` ETW session. The final live check is complete.

## Display tracking and input diagnostics panels

Verified on October 7, 2026. Optimized Release and Debug builds passed all six existing CTest tests. Dashboard captures at 2560x1440 (`LogOutput/2026-10-07_12-33-28/Dashboard.png`) and 1200x680 (`LogOutput/2026-10-07_12-35-23/Dashboard.png`) were visually inspected. The expanded panels have separate labeled columns, enlarged values, dividers, and padding. The minimum-size layout preserves both bordered summaries, the complete connected-controller identifier, both stick diagrams and trigger bars, one full latency row, and the shortcut footer without overlap or clipping.

The minimum-size capture used a hidden Release WARP session resized to a measured 1200x680 client area. These sessions did not have Windows display-tracing permission; the unavailable-display message remained visible and expanded display counters showed `--`. The captures validate layout and rendering, without collecting new foreground gamepad latency measurements.

## Fixed font and textual layout flow

The revised application requirements assume at least 1280x1400 pixels, one unscaled font size, and layout flowing from left to right and top to bottom. Display tracking and input diagnostics now show five vertical `Label = value` lines each. Font scaling, column dividers, compact layouts, and bottom anchoring were removed. Diagnostics and shortcuts follow the visible gamepad rows; device-row activity uses the same visible row count as the dashboard.

The optimized Release and Debug builds passed all six existing CTest tests. The hidden Release WARP capture at `LogOutput/2026-10-07_13-24-56/Dashboard.png` was resized to a measured 1280x1400 client area and visually inspected. Both text lists, complete controller identity, analog controls, gamepad row, diagnostics, and shortcuts fit without overlap. Display tracing lacked permission in this session, so the display counters correctly showed `--`; this was a layout check, not a new foreground latency measurement.

## Fixed 1200x1300 content space

The dashboard now uses fixed 1200x1300 content coordinates for text, panel widths, trigger bars, activity indicators, and gamepad row capacity. Window resizing only changes the back buffer, full-window background, and clipping. The original pixel coordinates and unscaled font remain the same in every window size. The application-enforced minimum window size was removed; the windowed client initially uses 1200x1300 pixels.

Release and Debug passed all six existing CTest tests. Hidden Release WARP sessions captured measured client areas of 1200x1300 (`LogOutput/2026-10-07_13-36-00/Dashboard.png`), 1600x1400 (`LogOutput/2026-10-07_13-36-07/Dashboard.png`), and 800x600 (`LogOutput/2026-10-07_13-36-13/Dashboard.png`). Visual inspection confirmed identical content positions and dimensions, extra background in the larger window, and clipped content in the smaller window. The lifecycle verification script's windowed-size and clipped-resize expectations were updated and its PowerShell syntax was checked. These hidden sessions lacked tracing permission and did not collect new foreground latency measurements.

## Fixed non-resizable window mode

The window modes are now borderless fullscreen and a non-resizable window with an exact 1200x1300 client area. Windowed style omits the resize border and maximize button, resize/maximize system commands are blocked, and window-position size changes are constrained to the fixed client size. Non-client dimensions account for the window DPI. Hidden verification remains hidden when switching out of fullscreen.

Release and Debug passed all six existing CTest tests. The hidden Release WARP session in `LogOutput/2026-10-07_13-41-44` verified fullscreen startup, F11 restoration of the fixed client size and style, rejection of sizing/maximize commands, rejection of a direct 800x600 resize attempt, and repeated F11 transitions. The application exited successfully. The lifecycle verification script was updated to assert resize/maximize rejection and passed its PowerShell syntax check. These checks exercised window behavior without collecting new foreground latency measurements.
