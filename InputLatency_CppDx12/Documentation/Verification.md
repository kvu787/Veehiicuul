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
