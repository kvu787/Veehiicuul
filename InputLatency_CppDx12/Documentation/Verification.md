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
