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
