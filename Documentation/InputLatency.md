# Input latency: evidence, implementation, and experiment design

Report date: September 17, 2026. Repository baseline: `9032419`.

This report consolidates the repository's latency discussions, current implementation, archived throughput experiments, and seven September 17 PresentMon captures. It distinguishes recorded values, user-reported settings, source-code behavior, and explanations that still need a controlled experiment.

## Findings

1. **With the NVIDIA driver limit set to 100 FPS, the lightweight non-Reflex applications report approximately 9.4–9.6 ms mean all-input latency in the selected comparison windows.** One target frame interval is 10 ms. This similarity holds across ZoomTracks, the Veehiicuul C++ demo, and the separate Godot VsyncStutterTest application. The measurements do not establish that the engines have identical physical responsiveness.
2. **Switching NVIDIA Low Latency Mode (LLM) from Off to Ultra produced no consistent reduction in typical latency.** The mean changes were −0.08 ms for ZoomTracks, +0.11 ms for C++, and +0.08 ms for Godot. Each pair contains only one run per setting, with different input patterns and sometimes different analysis windows. These are descriptive differences, not causal estimates.
3. **Click-only samples commonly average approximately 14–17 ms, rather than 10 ms.** Continuous-motion all-input measurements and isolated-click measurements describe different input populations. None of these captures is a complete record of latency for every hardware input report.
4. **DOOM reports a lower middle-segment mean of 7.53 ms and median of 7.36 ms.** That capture also has G-SYNC On and NVIDIA Reflex On, while the preceding September 17 apps had G-SYNC Off and no Reflex implementation. Its workload and presentation mode differ as well. It cannot isolate an engine, G-SYNC, or Reflex advantage.
5. **One-frame mean movement latency and two-frame upper-end latency are an empirical pattern here, not a theoretical minimum or a hard ceiling.** Input sampling, wait placement, rendering, buffering, and display timing determine the result. Startup outliers and some ordinary samples already exceed 20 ms.
6. **The archived roughly 5,600 FPS result is a separate, well-supported throughput result.** Its input-latency columns are entirely unavailable. It establishes small per-frame cost with limited configured queue capacity, not submillisecond physical input latency.
7. **The repository already implements useful vendor-neutral queue control and input-refresh ordering.** Its current constraints prohibit vendor-specific integrations, including Reflex, and prohibit an application-owned FPS limiter. Recommendations below respect those constraints.

## Scope and evidence strength

The review covered the repository inventory, all 63 Markdown conversation records present at review start, application source and configuration, build/launch paths, tests, rendering and numerical reports, capture tooling, and saved measurement evidence. Latency-relevant execution paths were traced directly. Generated meshes, Blender/image assets, shader numerical datasets, and the vendored JSON library were reviewed for their role in the workload, not treated as separate input-latency experiments or subjected to a new full correctness audit.

The seven recent CSVs were reread and their statistics recomputed. A separate standard-library CSV pass reproduced input sample counts, means, medians, P95, and P99. The 90 archived CSV parts were also reread to reproduce historical row counts and FPS and confirm that their input columns contain no measurements. No application code or driver settings were changed, and no new performance capture was made for this report.

Evidence has four distinct levels:

| Evidence                        | What it supports                                                                      | What it does not establish                                                                 |
| ------------------------------- | ------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------ |
| Raw PresentMon CSV              | Recorded process, frame timestamps, presentation modes, and available latency metrics | Physical pixels, actual gameplay response, undocumented driver waits, or all device events |
| User confirmation               | NVIDIA cap, LLM, G-SYNC and Reflex settings for the specified runs                    | Independent verification that every driver setting took effect                             |
| Source and current settings     | What the inspected revision implements and requests                                   | The exact binary/configuration of a historical run without a build/session record          |
| Earlier explanation or proposal | Experiment rationale and design intent                                                | A measured performance result or a current implementation unless verified                  |

The user explicitly accepted PresentMon's inferred input-to-frame association for the present experiment and deferred requiring visible input indicators in the C++ demo. The estimates remain useful within that agreed scope. An optical response experiment would answer a different, stronger question.

## Capture identities and conditions

All seven recent captures were described as using the **NVIDIA 100 FPS limiter**. All rows have `SyncInterval = 0`. That field records the application's presentation request; it does not independently prove that no driver override was active. Only the ZoomTracks description explicitly identified mostly mouse movement with a few clicks, although the discussion generally used that interaction style. The CSVs do not provide a movement-only classification.

| ID  | Application / capture folder                  | LLM   | G-SYNC | Reflex          | Rows  | Duration    |
| --- | --------------------------------------------- | ----- | ------ | --------------- | ----- | ----------- |
| ZO  | ZoomTracks, `2026-09-17_01-33-02`             | Off   | Off    | Not implemented | 2,065 | 21.017903 s |
| ZU  | ZoomTracks, `2026-09-17_01-37-02`             | Ultra | Off    | Not implemented | 3,163 | 31.849372 s |
| CO  | Veehiicuul C++, `2026-09-17_02-07-27`         | Off   | Off    | Not implemented | 6,345 | 63.465312 s |
| CU  | Veehiicuul C++, `2026-09-17_02-13-32`         | Ultra | Off    | Not implemented | 3,309 | 33.083899 s |
| GO  | Godot VsyncStutterTest, `2026-09-17_02-33-46` | Off   | Off    | Not implemented | 5,060 | 50.580563 s |
| GU  | Godot VsyncStutterTest, `2026-09-17_02-38-05` | Ultra | Off    | Not implemented | 4,245 | 42.430739 s |
| D   | DOOM The Dark Ages, `2026-09-17_10-42-33`     | Off   | On     | On              | 9,875 | 98.764560 s |

These setting labels include the user's later G-SYNC and Reflex clarifications. The repeated C++ request initially supplied the Off path while saying Ultra; the user then supplied the distinct `02-13-32` path. **CO remains Off and CU is Ultra.** The interrupted analysis of the repeated path is not an additional experiment.

The Godot capture logs identify **Godot 4.6.3, D3D12 Forward+, RTX 5090 Laptop GPU**. They are not captures of this repository's newer Godot 4.7.2 C# experiment. ZoomTracks logs also identify D3D12 and the RTX 5090 Laptop GPU, `maxQueuedFrames = 1`, `targetFrameRate = -1`, and `vSyncCount = 0`. Those logs do not prove the exact queue behavior of Unity's backend. The C++ and DOOM CSVs alone do not identify the rendering adapter. `PresentRuntime = DXGI` identifies the observed presentation runtime, not necessarily the complete rendering API used by a game.

The historical September 11 hardware inventory identifies an RTX 5070 Ti Laptop GPU. It must not be copied into the September 17 configuration. The user later stated that G-SYNC was Off for all captures before DOOM. That is retrospective user-reported configuration; the September 11 archive itself says capture-time VRR was not recorded and provides no independent per-run confirmation.

### Analysis windows

Elapsed time is `(TimeInMs - first TimeInMs) / 1000`. It starts at the first retained CSV row, not necessarily at process startup or capture-tool launch. Initial and middle ranges are half-open; the last range includes the final row. Exact calculations use CSV precision, not the rounded seconds below.

| ID  | Main comparison window, elapsed                   | Boundary in CSV TimeInMs, expressed in seconds |
| --- | ------------------------------------------------- | ---------------------------------------------- |
| ZO  | All 21.017903 s; only valid input rows contribute | No requested trim                              |
| ZU  | All 31.849372 s; only valid input rows contribute | No requested trim                              |
| CO  | Final 15 s: 48.465312–63.465312                   | Split at 62.939327                             |
| CU  | After first 10 s: 10–33.083899                    | Split at 23.509184                             |
| GO  | Middle: 5–45.580563                               | Splits at 6.966229 and 47.546792               |
| GU  | Middle: 5–37.430739                               | Splits at 5.660004 and 38.090743               |
| D   | Middle: 10–88.764560                              | Splits at 10.005677 and 88.770236              |

These preserve the user's requested windows. They are not matched-duration randomized trials. In particular, comparing CO's selected final 15 seconds with CU's post-startup period can reflect activity differences as well as any setting effect.

## What PresentMon measures

### Input metrics are frame-associated software estimates

`MsAllInputToPhotonLatency` and `MsClickToPhotonLatency` use a tracked Windows input timestamp and a system-derived display timestamp. They do not observe the physical switch, mouse sensor, USB transmission before the tracked event, panel scan position, or pixel response. They also do not inspect game state or verify which image first reacted. A message-consuming application can produce measurements without drawing an input response. See the [PresentMon 2.5.1 metric reference](https://github.com/GameTechDev/PresentMon/blob/v2.5.1/README-ConsoleApplication.md#csv-columns).

The inspected PresentMon input path records the latest device-read timestamp, updates process input state on message retrieval, and attaches pending input to a presentation. The implementation can replace the general input timestamp with a newer one. The documentation's description of the earliest contributing input should therefore not be read as a guarantee that every older keyboard or motion event remains represented. The source behavior is material to these tests. [Input tracking and frame association](https://github.com/GameTechDev/PresentMon/blob/v2.5.1/PresentData/PresentMonTraceConsumer.cpp).

Continuous mouse motion can repeatedly refresh that timestamp. A low all-input mean does not establish the same delay for every motion report or for an earlier keypress in the same frame. The click column follows a separate timestamp; its values can differ from the all-input value in that row. Counts are **metric samples**, not necessarily counts of physical user clicks. The source also carries input from an undisplayed frame to a later displayed frame in applicable cases. [Input metric calculation](https://github.com/GameTechDev/PresentMon/blob/v2.5.1/IntelPresentMon/CommonUtilities/mc/MetricsCalculatorInput.cpp).

Removing rows with click measurements is a useful sensitivity check, but does not create a provably pure mouse-movement dataset. Keyboard input, unclassified mouse activity, and differing input retrieval paths can remain. No gamepad latency conclusion follows from these automatic keyboard/mouse metrics.

### Distinguish the timing columns

| Quantity                           | Interpretation in this report                                                                                             |
| ---------------------------------- | ------------------------------------------------------------------------------------------------------------------------- |
| `MsAllInputToPhotonLatency`        | Available frame-associated all-input-to-display estimate                                                                  |
| `MsClickToPhotonLatency`           | Available click-specific input-to-display estimate                                                                        |
| `MsUntilDisplayed`                 | Delay from the recorded Present start to the attributed display event                                                     |
| All-input minus `MsUntilDisplayed` | Derived delay from the tracked input timestamp to Present, on the same row                                                |
| `MsBetweenPresents`                | Application presentation cadence; not input latency                                                                       |
| `MsBetweenDisplayChange`           | Display-event interval; not a direct measurement of panel refresh or response                                             |
| `MsGPUBusy`                        | Reported active GPU work attributed to the frame; not a percentage or a serial component to add to the others             |
| `MsCPUBusy` / `MsCPUWait`          | PresentMon's CPU-frame timing attribution; not a profiler separating application instructions, driver work, and all waits |

The derived pre-Present and post-Present means add back to the mean all-input estimate when computed on the same valid rows. That arithmetic does **not** establish that a particular game input caused all the work attributed to that row. GPU execution overlaps CPU and presentation work, and its attribution can differ from input attribution. Summing CPU busy, GPU busy, and display delay double-counts time.

Hardware-accelerated GPU scheduling can affect PresentMon's GPU timing accuracy. The historical inventory recorded it enabled; its state was not supplied for each recent capture. Treat GPU times as context rather than exact shader cost or sufficient proof of a bottleneck. [PresentMon limitations](https://github.com/GameTechDev/PresentMon/blob/v2.5.1/README.md#tracking-gpu-work-with-hardware-accelerated-gpu-scheduling-enabled).

`NA` is unavailable data, not zero latency. It can reflect absent associated input, an undisplayed frame, unsupported tracking, or another missing measurement. No click values in DOOM does not prove that no physical clicks occurred. Similarly, a missing display value is not by itself a complete diagnosis of why a frame was not tracked to display.

## Recomputed results

All latency values below are milliseconds. P95 means 95% of the available samples are at or below that value, using linear interpolation. Maxima are observed sample maxima, not guaranteed worst-case bounds.

### Selected comparison windows

| ID  | Input samples | Coverage | Mean  | Median | P95    | P99    | Maximum |
| --- | ------------- | -------- | ----- | ------ | ------ | ------ | ------- |
| ZO  | 1,696         | 82.1%    | 9.583 | 9.613  | 10.154 | 11.328 | 19.595  |
| ZU  | 2,653         | 83.9%    | 9.506 | 9.611  | 10.105 | 10.352 | 21.688  |
| CO  | 1,414         | 94.2%    | 9.442 | 9.378  | 10.869 | 13.386 | 19.649  |
| CU  | 2,224         | 96.3%    | 9.551 | 9.470  | 11.454 | 13.511 | 15.940  |
| GO  | 3,792         | 93.4%    | 9.508 | 9.438  | 10.202 | 12.694 | 19.393  |
| GU  | 3,086         | 95.2%    | 9.591 | 9.607  | 10.130 | 12.256 | 19.941  |
| D   | 6,554         | 83.2%    | 7.530 | 7.361  | 8.707  | 12.905 | 18.564  |

The seven means should not be pooled into an engine-independent average. Their windows, sampling coverage, workloads, and configurations differ. Available input samples cover approximately 82–96% of rows in these selected windows; equal frame counts would still not guarantee identical input populations.

The selected C++ and Godot windows are entirely `Hardware: Independent Flip`; DOOM's middle window is entirely `Hardware Composed: Independent Flip`. The full ZoomTracks files are predominantly independent flip, with a few startup rows in other modes. Presentation transitions are retained in the separate startup analysis.

### Startup, middle, and ending results

| ID  | Window             | Input samples | Mean   | Median | P95    | P99    | Maximum  |
| --- | ------------------ | ------------- | ------ | ------ | ------ | ------ | -------- |
| CO  | Full               | 5,162         | 9.649  | 9.469  | 12.292 | 13.214 | 30.379   |
| CO  | Before final 15 s  | 3,748         | 9.727  | 9.544  | 12.442 | 13.211 | 30.379   |
| CO  | Final 15 s         | 1,414         | 9.442  | 9.378  | 10.869 | 13.386 | 19.649   |
| CU  | Full               | 2,824         | 9.644  | 9.522  | 12.012 | 13.890 | 31.306   |
| CU  | Initial 10 s       | 600           | 9.991  | 9.687  | 13.278 | 17.274 | 31.306   |
| CU  | After initial 10 s | 2,224         | 9.551  | 9.470  | 11.454 | 13.511 | 15.940   |
| GO  | Full               | 4,386         | 9.905  | 9.451  | 10.445 | 13.236 | 1463.127 |
| GO  | Initial 5 s        | 310           | 14.698 | 9.696  | 12.397 | 19.388 | 1463.127 |
| GO  | Middle             | 3,792         | 9.508  | 9.438  | 10.202 | 12.694 | 19.393   |
| GO  | Final 5 s          | 284           | 9.977  | 9.708  | 12.752 | 18.311 | 19.721   |
| GU  | Full               | 3,724         | 9.762  | 9.608  | 10.184 | 12.496 | 619.400  |
| GU  | Initial 5 s        | 423           | 11.085 | 9.612  | 10.368 | 12.744 | 619.400  |
| GU  | Middle             | 3,086         | 9.591  | 9.607  | 10.130 | 12.256 | 19.941   |
| GU  | Final 5 s          | 215           | 9.610  | 9.605  | 10.321 | 13.278 | 17.413   |
| D   | Full               | 7,270         | 7.542  | 7.392  | 8.825  | 13.299 | 36.659   |
| D   | Initial 10 s       | 334           | 8.179  | 8.036  | 9.808  | 16.303 | 36.659   |
| D   | Middle             | 6,554         | 7.530  | 7.361  | 8.707  | 12.905 | 18.564   |
| D   | Final 10 s         | 382           | 7.180  | 7.496  | 9.615  | 12.818 | 17.773   |

ZoomTracks has no requested segmentation; its full-file valid-input statistics are in the preceding table. The C++ Ultra five-second-cutoff sensitivity check gives **9.55 ms mean, 9.47 ms median, 11.47 ms P95, and 13.50 ms P99**, nearly identical to excluding ten seconds. No input-latency outliers were silently removed from these tables.

### Click-specific results

| ID  | Window             | Click samples | Mean        | Median      | Minimum     | Maximum     |
| --- | ------------------ | ------------- | ----------- | ----------- | ----------- | ----------- |
| ZO  | Full               | 4             | 16.889      | 16.838      | 14.284      | 19.595      |
| ZU  | Full               | 8             | 14.297      | 14.687      | 10.794      | 18.230      |
| CO  | Full               | 32            | 15.675      | 15.525      | 9.953       | 21.661      |
| CO  | Before final 15 s  | 16            | 15.517      | 15.067      | 9.953       | 21.661      |
| CO  | Final 15 s         | 16            | 15.834      | 16.462      | 10.518      | 20.673      |
| CU  | Full               | 16            | 14.667      | 15.464      | 10.254      | 18.092      |
| CU  | Initial 10 s       | 10            | 14.436      | 15.072      | 10.254      | 17.329      |
| CU  | After initial 10 s | 6             | 15.054      | 15.621      | 11.849      | 18.092      |
| GO  | Full               | 35            | 14.657      | 14.417      | 10.253      | 19.646      |
| GO  | Initial 5 s        | 17            | 15.134      | 13.917      | 10.360      | 19.646      |
| GO  | Middle             | 15            | 14.040      | 14.417      | 10.253      | 19.393      |
| GO  | Final 5 s          | 3             | 15.040      | 14.697      | 13.930      | 16.493      |
| GU  | Full               | 14            | 14.509      | 15.088      | 10.459      | 18.964      |
| GU  | Initial 5 s        | 6             | 15.403      | 15.365      | 11.006      | 18.342      |
| GU  | Middle             | 6             | 14.420      | 13.897      | 10.459      | 18.964      |
| GU  | Final 5 s          | 2             | 12.095      | 12.095      | 11.099      | 13.091      |
| D   | Full               | 0             | Unavailable | Unavailable | Unavailable | Unavailable |
| D   | Initial 10 s       | 0             | Unavailable | Unavailable | Unavailable | Unavailable |
| D   | Middle             | 0             | Unavailable | Unavailable | Unavailable | Unavailable |
| D   | Final 10 s         | 0             | Unavailable | Unavailable | Unavailable | Unavailable |

Click counts are small, often single digits. Click P95/P99 would be misleadingly precise for the setting comparisons, so they are not used as headline statistics. DOOM has **zero valid click samples across the entire capture**, including the excluded edges.

Excluding click-bearing rows barely changes the selected all-input medians: **9.612/9.610 ms** for ZoomTracks Off/Ultra, **9.343/9.467 ms** for C++ Off/Ultra, and **9.437/9.607 ms** for Godot Off/Ultra. This confirms that a handful of click rows do not explain the roughly one-frame all-input pattern. It does not fix the input-association limitation.

### Where the recorded delay appears

The following means use exactly the input-bearing rows from each selected comparison window, including both kinds of input where present.

| ID  | Input to Present | Present to display | Total all-input | GPU busy (context) |
| --- | ---------------- | ------------------ | --------------- | ------------------ |
| ZO  | 1.292            | 8.291              | 9.583           | 0.654              |
| ZU  | 1.260            | 8.246              | 9.506           | 0.654              |
| CO  | 0.385            | 9.057              | 9.442           | 0.521              |
| CU  | 0.338            | 9.213              | 9.551           | 0.503              |
| GO  | 0.529            | 8.979              | 9.508           | 0.871              |
| GU  | 0.474            | 9.117              | 9.591           | 0.805              |
| D   | 0.284            | 7.246              | 7.530           | 9.364              |

For the small apps, roughly 8–9 ms of the recorded delay lies after Present while reported GPU busy time is below 1 ms. This is **consistent with** externally paced presentation and waiting dominating these runs. It does not identify the NVIDIA driver's exact sleep location or assign every millisecond to the limiter. DXGI admission, driver scheduling, scanout eligibility, and frame association are also relevant.

DOOM is different: reported GPU busy time is approximately **9.36 ms** on input-bearing rows, while its all-input mean is **7.53 ms**. Those values cannot be interpreted as a single serial path in which fresh input starts all of that GPU work. Pipeline overlap, attribution, and the tracked input's relation to the rendered result matter. Reflex is enabled by user report, but the file contains no instrumented Reflex breakdown or frame-type column; this CSV does not audit frame-generation settings or prove the visible-response latency.

### Frame pacing

| ID  | Display intervals | Mean   | P95    | P99    | Maximum |
| --- | ----------------- | ------ | ------ | ------ | ------- |
| ZO  | 1,997             | 10.000 | 10.150 | 10.330 | 10.972  |
| ZU  | 3,123             | 10.000 | 10.124 | 10.294 | 10.685  |
| CO  | 1,501             | 10.000 | 10.324 | 12.943 | 14.984  |
| CU  | 2,309             | 10.000 | 10.618 | 13.521 | 14.028  |
| GO  | 4,058             | 10.000 | 10.282 | 12.595 | 14.463  |
| GU  | 3,243             | 10.000 | 10.282 | 12.425 | 13.902  |
| D   | 7,874             | 10.003 | 11.068 | 11.593 | 14.857  |

For the two ZoomTracks pacing rows only, the window begins at the first valid input row and continues to the end, matching the earlier analysis and avoiding large startup gaps with no input measurements. The other pacing rows use their selected comparison windows. All rows with available display intervals contribute, not only input-bearing rows. A boundary row's interval can begin just before the window.

Approximately 10 ms average intervals confirm the observed 100 FPS cadence; they do not imply that every frame takes exactly 10 ms. The occasional shorter interval compensating a longer one also does not erase the older input in a delayed frame. The monitor's refresh rate must be recorded separately from the FPS cap.

### Off versus Ultra

| Application            | Mean change | Median change | P95 change | P99 change | Maximum change |
| ---------------------- | ----------- | ------------- | ---------- | ---------- | -------------- |
| ZoomTracks             | -0.077      | -0.002        | -0.049     | -0.976     | +2.093         |
| Veehiicuul C++         | +0.109      | +0.092        | +0.584     | +0.126     | -3.709         |
| Godot VsyncStutterTest | +0.083      | +0.169        | -0.072     | -0.438     | +0.548         |

Values are Ultra minus Off, using the selected windows. Negative means a lower recorded latency, not proof that Ultra caused an improvement.

There is no consistent median or mean benefit. The C++ maximum is lower with Ultra while its mean and percentiles are higher; Godot's P99 is lower while its mean and maximum are higher. Maximum comparisons are particularly sensitive to duration and rare events. Thousands of adjacent frame samples are not thousands of independent experimental runs. Repeated matched captures, preferably alternating setting order, are needed before claiming a small setting effect.

The older claim that driver Ultra Low Latency cannot apply to DX12 is obsolete: NVIDIA added DX12 support in driver 551.23 in January 2024. DX12 alone is not an explanation for these near-ties. [NVIDIA release announcement](https://www.nvidia.com/en-gb/geforce/news/geforce-rtx-4070-ti-super-rtx-video-hdr-game-ready-driver/).

### Outliers and transitions that should remain visible

- **ZoomTracks Off:** 106 ms and 289 ms startup display intervals precede the first input sample. They are frame-delivery observations, not measured input-latency spikes. Ultra's all-input maximum is 21.69 ms despite an approximately normal display interval at that row.
- **C++ Off:** the 30.38 ms input maximum occurs during composed flip with a 25.52 ms display interval. The selected last 15 seconds are entirely independent flip, but still have P99 input latency of 13.39 ms. Cleaner does not mean outlier-free.
- **C++ Ultra:** the 31.31 ms input spike at elapsed 2.50 s coincides with composed flip and a 23.23 ms display interval. Excluding five or ten seconds produces almost the same central result.
- **Godot Off:** the first row contains 1,463.13 ms input latency. Removing only that row changes the first-five-second mean from 14.70 to **10.01 ms**, with a 19.68 ms maximum. The segmented-results table retains it. It is startup-associated; its exact input history cannot be reconstructed from the CSV alone.
- **Godot Ultra:** the first row contains 619.40 ms. Without that row, the first-five-second mean is **9.64 ms** and maximum 17.23 ms. Again, the segmented-results table retains the sample.
- **Godot endings:** no input-latency samples appear in the final **1.63 s Off** or **2.11 s Ultra**, although frames continue. Ending statistics describe only the available input-bearing rows. Large late input values can occur without correspondingly large display intervals.
- **DOOM:** input tracking begins about four seconds after the first frame. The 36.66 ms early spike accompanies a presentation transition. Input samples stop **3.71 s before the end**; the later 31.22 and 43.76 ms display intervals therefore have no input-latency measurement attached. A lower final-segment mean does not establish better end-of-run responsiveness.

## Historical high-throughput evidence

The [September 11 report](<../Cpp/SavedLogOutput/2026-09-11%20MinimumInputLatency/Report.md>) describes two captures from the same continuously running C++ app, approximately 80 minutes apart, at user-confirmed 2560 × 1440 borderless fullscreen and Armoury Crate Turbo mode.

| Metric                                        | 13:59:13 capture     | 15:18:36 capture     |
| --------------------------------------------- | -------------------- | -------------------- |
| Rows                                          | 1,692,056            | 1,699,754            |
| Duration                                      | 300.008912 s         | 300.005088 s         |
| Presentation throughput                       | 5,640.02 FPS         | 5,665.75 FPS         |
| Mean present interval                         | 177.304 microseconds | 176.499 microseconds |
| Lowest complete one-second presentation count | 5,100                | 5,150                |
| Valid all-input / click latency values        | 0 / 0                | 0 / 0                |

This review reread all 45 CSV parts per capture and reproduced the row counts and FPS, found no non-increasing timestamps, and confirmed the entirely absent input metrics. The existing independent verification and archived logs additionally record all 600 complete one-second windows above 5,000 presents and zero reported ETW event loss, buffer loss, and present overflow. Those archive checks are documented in the [verification results](<../Cpp/SavedLogOutput/2026-09-11%20MinimumInputLatency/VerificationResults.json>) and [repository review](../Cpp/Documentation/Reports/RepositoryReview20260911/Review.md).

The small configured backlog was supported by saved settings and source: one unfinished GPU frame, DXGI presentation limit one, two buffers, spin waits, VSync off. The scene had a static background image and two live 3D draws. This is meaningful evidence of low prototype overhead. It is not a complete game benchmark, a monitor refreshing 5,600 times per second, or a physical input measurement. The approximately 74-minute gap between captured periods is unmeasured.

Older standalone throughput and overflow conversations supply useful history but are not pooled with this dataset. The unreliable early overflow capture showed recording gaps; the enlarged-buffer capture configuration addressed that problem. The preserved split captures, rather than a warning-bearing incomplete run, are the supported historical baseline.

## Current C++ input and rendering architecture

### Readiness before frame preparation

The current [application loop](../Cpp/Source/Application.cpp) and [renderer](../Cpp/Source/Renderer.cpp) follow this ordering:

```text
service Windows messages
apply pending resize / handle minimized state
wait for reusable GPU resources and, when enabled, DXGI admission
    continue servicing messages while waiting
service Windows messages again after readiness
compute current animation state and record rendering commands
submit commands -> Present -> signal completion fence
```

The application's update, message servicing, and submission run on one application-owned thread. GPU execution, drivers, and operating-system work are asynchronous; this does not make the whole system single-threaded. No separate high-rate application input polling thread is currently implemented.

`ServiceMessages()` now drains pending messages with `PeekMessageW(..., PM_REMOVE)` instead of stopping after 64 messages. The change was committed at 01:54:35 on September 17, before the named C++ captures. This chronology supports the intended experiment sequence but does not independently fingerprint the captured binaries. There is no measured before/after dataset isolating this change. The remaining `% 64` in `PrepareFrame()` controls how often spin polling services messages; it is **not** a 64-message drain limit.

The final drain allows input that arrived during readiness waits to be processed before preparing another frame. An unbounded drain can still delay rendering if event handling cannot keep up; minimizing handler work matters. The current ordinary Win32 path is not a buffered Raw Input implementation. Draining the Windows queue and draining buffered raw device reports are different operations.

The demo handles window controls such as VSync, fullscreen, and quit; its car animation follows elapsed time. It does not currently implement a mouse-following gameplay object or a complete gameplay input snapshot. Consequently, its PresentMon values are valid for the agreed process/input/presentation experiment but do not certify that moving the mouse changed the car image at that latency.

### Independent queue controls

The effective preset values come from [RenderPreparation.h](../Cpp/Source/RenderPreparation.h), not from inactive custom values in [Settings.json](../Cpp/Assets/Settings.json).

| Control                              | MinimizeInputLatency | Standard | MaximizeFps |
| ------------------------------------ | -------------------- | -------- | ----------- |
| Maximum GPU frames in flight         | 1                    | 2        | 3           |
| Maximum DXGI presentation latency    | 1                    | 2        | 2, inactive |
| Explicit presentation admission wait | Yes                  | Yes      | No          |
| Swap-chain image buffers             | 2                    | 3        | 4           |
| Tearing requested when VSync is off  | Yes                  | No       | Yes         |
| Readiness wait strategy              | Spin                 | Event    | Spin        |

`VSync` is independent of the preset. The shipped file selects `MinimizeInputLatency` and VSync false, even though its inactive Custom fields resemble Standard. Future run manifests must record resolved settings from the running process rather than copy those inactive fields as if they applied.

- **GPU frames in flight** limit unfinished frame work through per-frame resources and fences. One includes the frame executing on the GPU; it is not one waiting frame plus another executing frame.
- **Presentation latency** is a frame-count allowance on a waitable swap chain. It does not mean milliseconds or a guarantee that pixels have appeared. A GPU-completed frame can still await display.
- **Back-buffer count** provides image storage. Two images do not imply a fixed two-frame input delay, and the count need not equal the number of GPU frame contexts.
- **Spin versus Event** changes how readiness is waited for. It trades scheduling behavior against CPU/power cost; it does not remove the condition being waited on or guarantee a performance win.
- **WaitForPresentation false** removes the explicit DXGI admission gate. It does not remove safe resource-reuse waits or guarantee a nonblocking Present call.

Do not add queue capacities to calculate latency. Actual occupancy and timing matter. The complete setting semantics and valid ranges remain in [RenderPipeline.md](../Cpp/Documentation/RenderPipeline.md). Microsoft's [frame-latency API](https://learn.microsoft.com/en-us/windows/win32/api/dxgi1_3/nf-dxgi1_3-idxgiswapchain2-setmaximumframelatency) and [wait-before-render guidance](https://learn.microsoft.com/en-us/windows/uwp/gaming/reduce-latency-with-dxgi-1-3-swap-chains) describe the underlying mechanism.

The tests cover settings, GPU budgets, independent buffer counts, presentation permits, resizing, VSync changes, and cancellation while waiting. They test synchronization behavior, not which preset minimizes physical latency. Earlier recorded successful builds/tests were reviewed; no new application build or benchmark was necessary for this documentation-only change.

## Godot: distinguish the captured app from the experiment app

### Captured VsyncStutterTest

The two GO/GU logs report Godot **4.6.3**. The separately located project's currently inspected scene moves a sphere in `_process()` and disables accumulated input. Its project requests D3D12, VSync off, and two swap-chain images. It does not implement the newer C# experiment's explicit `DisplayServer.ProcessEvents()` call or its input display. Current external source is supporting context, not a frozen snapshot of the historical executables.

A comment in that external sphere script describes a theoretical two-interval upper bound. That comment is a hypothesis, not an enforced bound or a measurement. The model below explains why it must not be treated as a guarantee.

### Repository InputLatency_Godot

[InputLatency_Godot](../InputLatency_Godot/Readme.md) is a separate Godot **4.7.2 .NET / C#** experiment. None of the seven captures supplied in this conversation is a capture of its `InputLatencyGodot.exe`.

Its current [configuration](../InputLatency_Godot/project.godot) requests D3D12 Forward+, a fixed 1280 × 720 window, VSync disabled, no application cap, two swap-chain images, frame queue size two, Safe rendering thread mode, and physics interpolation disabled. The [main update](../InputLatency_Godot/Source/InputLatency.cs) calls `DisplayServer.ProcessEvents()`, reads current input, and updates its sphere and UI from `_Process`. `_Input` records events; it does not run a second gameplay update.

It displays one selected gamepad, keyboard/mouse event feeds, held-state indicators, and a game-rendered mouse marker. Stock Godot combines physical keyboards and mice into logical devices; gamepad selection stays stable until disconnect. Event receipt timestamps and synthetic verification are not latency measurements. Formatting and redrawing the feeds also adds real CPU/GPU work; the app must be measured rather than assumed to meet a 500-microsecond workload.

The inspected `Godot_4-7-2` checkout is commit `ed1daf0bf001b61586d9930840f2f1394092c079`. Its rendering-device code clamps both requested counts to at least two; the default frame queue size is two and default swap-chain image count is three. These controls remain active with VSync disabled. The D3D12 path separately chooses sync interval zero and tearing flags when supported. The inspected path does not use the C++ app's explicit frame-latency waitable-object gate or `SetMaximumFrameLatency` call.

Frame queue size two means rotating resource capacity with a fence wait before reuse. It is not a compulsory extra 10 ms at 100 FPS. The default Safe mode does not create a separate rendering thread; the separate-thread mode is a different option. The source-based findings and exact locations are retained in the [Godot comparison conversation](../Conversations/2026-09-15-GodotInputLatencyComparison.md) and [buffer-settings conversation](../Conversations/2026-09-17-GodotVsyncBufferSettings.md).

Matching the C++ preset exactly would require a Godot backend change: independent one-frame GPU admission, a DXGI presentation gate, correct placement before input/update, and lifecycle handling. That remains a proposal, not an implemented or benchmarked improvement. The near-equal 100 FPS results do not currently justify promising a millisecond gain from that work.

## VSync, G-SYNC, LLM, Reflex, and FPS limits

These controls are related but not interchangeable:

| Control                           | Changes                                                               | Does not prove                                                         |
| --------------------------------- | --------------------------------------------------------------------- | ---------------------------------------------------------------------- |
| Application VSync / sync interval | Requested synchronization of presentation with refresh                | No other waits when disabled                                           |
| AllowTearing                      | Permission for applicable presentation paths to expose partial frames | Visible tearing, disabled G-SYNC, or an input-latency bound            |
| G-SYNC / VRR                      | Display refresh scheduling within supported conditions                | The location of an application's input sample or NVIDIA limiter wait   |
| NVIDIA Max Frame Rate             | Driver-imposed production cadence                                     | A fixed input age or a universal one-frame minimum                     |
| NVIDIA LLM Ultra                  | Driver low-latency queue/scheduling policy where applicable           | A guaranteed improvement in a light, externally capped workload        |
| NVIDIA Reflex                     | Application-integrated coordination of input/simulation and rendering | A benefit quantified by comparing different games and display settings |

`Hardware: Independent Flip` and `Hardware Composed: Independent Flip` must not be collapsed into ordinary `Composed: Flip`. Independent flip can use hardware composition/multiplane overlay. The mode label alone is not a verdict on latency and does not certify G-SYNC state. [Microsoft flip-model guidance](https://learn.microsoft.com/en-us/windows/win32/direct3ddxgi/for-best-performance--use-dxgi-flip-model).

NVIDIA describes Reflex as coordinating CPU and GPU scheduling to reduce stale queued work and adjust when input/simulation starts. That provides a plausible mechanism for differences from a driver-only limit, but the DOOM capture does not isolate it. The relevant test is the **same DOOM workload with only Reflex toggled**, followed separately by a G-SYNC toggle. [NVIDIA Reflex explanation](https://www.nvidia.com/en-gb/geforce/news/reflex-low-latency-platform/).

The repository's [constraints](../README.md#constraints) prohibit adding Reflex, Anti-Lag 2, vendor-ID paths, or application-owned FPS limiting. Driver settings used externally for measurement do not change those implementation rules. Old conversations that suggested adding an NVAPI limiter or a timed application limiter predate the constraints and are not current implementation recommendations.

## Why one and two frame intervals appear

Let **T** be the target frame interval and **D** the delay from sampling an input to the endpoint of interest. Keep the endpoint explicit: completing rendering, reaching PresentMon's display event, and a physical pixel changing are different endpoints.

For isolated inputs arriving uniformly relative to one input sample per frame, with no extra stalls:

```text
wait for next input sample: 0 to approximately T
then sample-to-result delay: D

minimum approximately D
mean approximately T / 2 + D
upper end within a normal cycle approximately T + D
```

At 100 FPS, T is 10 ms. If D is approximately 10 ms, this predicts isolated-click latency around 10–20 ms, averaging 15 ms. That resembles the small click datasets. Continuous-motion all-input timestamps can instead be refreshed close to the next Present, giving a mean nearer one interval. The two statistics are compatible; neither is a measurement of every device event.

The earlier hypothetical assumed **0.5 ms total work**, fresh input sampled after the pacing wait, and no additional delay before a completed frame. Under those assumptions the numbers are approximately **0.5 ms minimum, 5.5 ms mean, and 10.5 ms upper end** to a completed frame. They are an illustrative model, not measured results or a promise about the NVIDIA limiter. More delay must be included if the endpoint is display or physical pixels.

Two 100 FPS implementations can have very different input age:

```text
wait -> sample fresh input -> update/render -> present
sample input -> update/render -> long pacing wait -> present
```

Waiting after input makes that input older. An external limiter may impose waiting beyond what the application explicitly controls; the present CSV cannot locate each driver wait. GPU queues, CPU scheduling, message handling, physics cadence, interpolation, and presentation transitions can add delays beyond two intervals. Neither VSync off nor two buffers supplies a hard upper bound.

The defensible conclusion is: **these non-Reflex 100 FPS experiments show roughly one-frame mean all-input latency and roughly two-frame upper-end interaction samples.** They do not establish the best possible NVIDIA-limited latency. All runs at one cap also cannot establish that latency scales linearly with the interval. Tests at additional caps are needed.

## Input freshness and future gameplay

The current minimum-latency ordering is worth preserving as gameplay is added: wait for the capacity actually needed, then refresh input as late as practical before updating the state to be rendered. Avoid arbitrary work or logging between that refresh and submission.

For a future complete input layer, retain both newest state and transitions. A press and release between two frame samples can leave the final held state unchanged; a state-only poll could miss the action. Accumulate relative mouse deltas, preserve button/key edges, handle focus/device changes, and avoid running full gameplay updates inside each input callback. Buffered Raw Input or an appropriate vendor-neutral input API can be evaluated separately; its tracking coverage must be revalidated with PresentMon. A dedicated polling thread is not automatically necessary or faster.

Godot's accumulated-input switch changes event batching, not device report rate or GPU queues. An explicit event pump refreshes events already available to the application; it cannot obtain a report that the device has not delivered. Gamepad data must be validated using suitable instrumentation rather than inferred from the keyboard/mouse columns.

Physics and rendering are another independent timing choice. A fixed simulation tick can support stable gameplay, but waiting for a slower physics tick or deliberately interpolating previous state can add visible delay. Immediate camera, cursor, and button feedback can follow a different path. No fixed extra physics delay should be added to these datasets without knowing the actual response path. Earlier fixed-tick architecture proposals were not implemented by the currently inspected C++ animation loop.

At 1,000–5,000 FPS, per-frame budgets are 1,000–200 microseconds. Allocations, driver calls, input dispatch, scheduling, and tracing can become material at that scale. More presents do not create more device reports or complete monitor refreshes. The specialized background-plus-vehicle design and optimized shaders reduce workload, but those savings must survive representative gameplay, UI, audio, and content before a full game performance claim is justified.

## Capture integrity and reproducibility

### Checks performed for this report

All seven recent files have one process and one swap chain, strictly increasing `TimeInMs`, and no disagreement larger than 0.00011 ms between consecutive timestamp differences and the corresponding `MsBetweenPresents`. Every row reports sync interval zero. Recomputed input statistics agree with the conversation's rounded values.

The counts of missing `MsUntilDisplayed` are **0, 0, 1, 1, 0, 0, and 10** for ZO, ZU, CO, CU, GO, GU, and D respectively. These are disclosed rather than imputed. The recent folders supplied no PresentMon status logs establishing zero ETW loss. Timestamp consistency supports internal coherence but cannot certify loss-free tracing or prove that all events were observed.

The historical 90-part data requires special care: only part 01 in each capture has a header. Reading every part as header-bearing would discard 44 rows per capture. The existing archive is capture-specific, and its report generator hard-codes some historical claims. It must not be reused to certify new inputs without replacing those assumptions and validating the actual data.

The current root [capture script](../Capture-PresentMon.ps1) names PresentMon 2.5.1, chooses a process name and unique timestamped output folder, but does not save complete experiment metadata or ETW status. The metric interpretation above uses the matching 2.5.1 documentation and source; a saved command line and executable hash would establish the version actually used for each run. At review time the script had a pre-existing local edit selecting DOOM. This report does not alter that edit. The [C++ README](../Cpp/README.md) still contains two alternative capture invocations that reuse the same output paths; running the entire block can overwrite the first recording. Use one invocation in a fresh directory. These capture-workflow issues were already identified in the [September 11 review](../Cpp/Documentation/Reports/RepositoryReview20260911/Review.md).

### Statistical method

1. Preserve file order and identify process/swap-chain populations before calculating anything.
2. Define windows from the first and last recorded `TimeInMs`, using full precision. Do not identify them by row number or assume exactly 100 rows per second.
3. Retain startup/ending segments separately and do not silently drop outliers. Keep the explicitly selected comparison segment visible.
4. Convert `NA` to missing values. Count available input and click samples separately; report available coverage against all frame rows.
5. Compute arithmetic sample means and linearly interpolated P50/P95/P99. A percentile is not an FPS low and the reciprocal of a mean is not the mean of reciprocals.
6. Compute pre-Present delay as all-input minus `MsUntilDisplayed` on the same valid rows. Compute GPU context on those same rows when comparing the breakdown.
7. For presentation throughput over a file use `(row count - 1) / (last timestamp - first timestamp in seconds)`. The first row's interval reaches outside the retained timeline; exclude it when deriving an interval-only throughput result.
8. Inspect presentation transitions and display gaps even when input values are missing. Do not equate a missing input statistic with a smooth or idle frame.

For a file with initial/final trim **S** seconds, the middle mask is exactly:

```python
elapsed = (data.TimeInMs - data.TimeInMs.iloc[0]) / 1000
duration = elapsed.iloc[-1]
middle = data[(elapsed >= S) & (elapsed < duration - S)]
latency = middle.MsAllInputToPhotonLatency.dropna()
summary = latency.agg(["count", "mean", "median", "min", "max"])
percentiles = latency.quantile([0.95, 0.99], interpolation="linear")
```

The report's numerical checks used pandas/NumPy plus an independent standard-library CSV/linear-percentile pass. Supporting scratch calculations were not added as an application or a replacement for the archive's existing tooling.

### Source files and fingerprints

The recent captures remain in their original locations outside this repository. This report records their identities; it does not archive their bytes. On another machine the paths must be supplied separately. Repository CSV and Blender files are configured for Git LFS, and historical raw data must be hydrated before recalculation.

**ZO:** `C:\Users\k\Repository\ZoomTracks\ZoomTracks\MyLogOutput\2026-09-17_01-33-02\PresentMon.csv`

529,600 bytes; `ZoomTracks.exe`; process ID `9776`; swap chain `0x18DFC121FB0`.

SHA-256: `1b2b4427fedbe93b38803f2c8e8a98a5597429c2a3c892e4c5fbaf3a54d65e5b`.

**ZU:** `C:\Users\k\Repository\ZoomTracks\ZoomTracks\MyLogOutput\2026-09-17_01-37-02\PresentMon.csv`

813,777 bytes; `ZoomTracks.exe`; process ID `27072`; swap chain `0x2AD6D3D6100`.

SHA-256: `2633acd56c4db6a3d92e2f4d45c9970155bb874c7271aadab5d53c91f723d464`.

**CO:** `C:\Users\k\Downloads\2026-09-17_02-07-27 Veehiicuul.exe logs\PresentMon.csv`

1,640,739 bytes; `Veehiicuul.exe`; process ID `23548`; swap chain `0x20BE22C9B30`.

SHA-256: `dfec50b2fcae3982fae61e266dee68b55afc058938d4624b67ed5d0533349f4a`.

**CU:** `C:\Users\k\Downloads\2026-09-17_02-13-32 Veehiicuul.exe logs\PresentMon.csv`

855,949 bytes; `Veehiicuul.exe`; process ID `30268`; swap chain `0x155A2D88550`.

SHA-256: `3a24d0c0d63d3e474fad36cb1f8b02a528f0511c3feb8b108e91876cef3376e9`.

**GO:** `C:\Users\k\Repository\VsyncStutterTest\MyLogOutput\2026-09-17_02-33-46\PresentMon.csv`

1,335,536 bytes; `VsyncStutterTest.exe`; process ID `27684`; swap chain `0x13A8741AF60`.

SHA-256: `c5c0ed4335ae6a4626d6165bb7636a2bbdbdde024f7147b3d3a5747f19912b2a`.

**GU:** `C:\Users\k\Repository\VsyncStutterTest\MyLogOutput\2026-09-17_02-38-05\PresentMon.csv`

1,119,444 bytes; `VsyncStutterTest.exe`; process ID `27860`; swap chain `0x204FB3D2F80`.

SHA-256: `c2ced0c0570f05ecc17ddcba5f7727138b1b2d6649ef934116cfcc30ccb7477e`.

**D:** `C:\Users\k\Downloads\2026-09-17_10-42-33 DOOMTheDarkAges.exe logs\PresentMon.csv`

2,673,184 bytes; `DOOMTheDarkAges.exe`; process ID `32864`; swap chain `0x27FD9A34FE0`.

SHA-256: `75dea38b810a13eebf142f1839685ef533c4323579ffc69520686a5ca311e452`.

## Experiments that would resolve the remaining questions

The highest-value next steps change one variable at a time and retain the same response path and workload.

| Question                                    | Controlled comparison                                                 | Required interpretation                                                                               |
| ------------------------------------------- | --------------------------------------------------------------------- | ----------------------------------------------------------------------------------------------------- |
| Does latency scale with cap interval?       | Same app at 60, 100, 144 FPS and uncapped, where sustainable          | Compare distributions and input coverage, not only FPS                                                |
| Does Ultra help this workload?              | Repeated Off/Ultra pairs at the same cap and scene, alternating order | Treat each capture as a trial; distinguish tiny effects from variation                                |
| What changes DOOM's result?                 | Reflex Off/On in the same game, then separately G-SYNC Off/On         | Keep cap, quality, display, camera path, and frame-generation state fixed and recorded                |
| Does the C++ queue policy matter?           | Custom settings changing one queue/admission control at a time        | Presets also change tearing and wait style, so a preset comparison is a combined change               |
| Do Godot's settings matter under load?      | Matched lightweight and GPU-heavier scenes with known queue settings  | Extra capacity may be unused in a light capped case                                                   |
| Is the inferred response frame correct?     | Same input-driven visual response and independent event/frame markers | Use optical measurement when the question is physical pixels                                          |
| Does draining input improve responsiveness? | Matched builds with documented input stress and handler cost          | Verify freshness and frame pacing together; do not assume the prior 64-message limit caused a backlog |

Before each experiment, record the executable hash and source revision, engine version, actual adapter, renderer, resolved settings, dimensions/window mode, monitor and refresh rate, VSync request and driver overrides, G-SYNC, Reflex, LLM, FPS limiter, power mode, HAGS, input device/report rate, focus state, and scene/quality settings. For generated-frame-capable games, explicitly record frame generation and the metric's frame-type coverage. Startup-only metadata avoids per-frame disk logging overhead.

Separate **stationary mouse clicks**, **keyboard presses with no mouse motion**, and **continuous motion**. Validate sample coverage first. Use the same predeclared warmup/ending policy and enough interaction time to populate the slow tail. Obtain several repeated captures rather than infer certainty from thousands of correlated frames in one run. Report count, mean, median, P95, P99, maximum, missing coverage, and relevant presentation transitions for each trial.

For high-rate runs, preserve PresentMon's command line/version and console/status log. Check lost-event, lost-buffer, and overflow counters. The archive's enlarged circular buffer and disabled console statistics are an established starting configuration, not proof of zero tracing overhead. Give every capture a unique output directory, verify exit status and duration, and never reuse an old verification result as evidence for new bytes.

No new vendor-specific integration, timed application limiter, or engine fork is required to perform these measurements. Proposed code changes remain subject to the current vendor-neutral and no-application-cap policies.

## Corrections and decisions carried forward

| Earlier idea or possible reading                                        | Current conclusion                                                                                  |
| ----------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------- |
| The C++ input queue stops after 64 messages                             | Superseded by the September 17 drain-to-empty change; there is no isolated latency benchmark for it |
| Godot's two frame slots add a mandatory frame of lag                    | Capacity is not occupancy; no fixed penalty follows from the count                                  |
| Settings under `vsync/` are irrelevant with VSync off                   | Both Godot counts remain active in the inspected D3D12 path                                         |
| Safe Godot rendering means a separate render thread                     | Safe is the default without the separate-render-thread option                                       |
| The two Godot captures validate the new C# experiment                   | They are VsyncStutterTest 4.6.3 captures, not InputLatencyGodot 4.7.2 captures                      |
| Ultra cannot work with DX12                                             | Obsolete since NVIDIA added support in 2024                                                         |
| One frame is the best possible mean and two frames a guaranteed maximum | An observed pattern for this capped workload and metric, not a limit                                |
| Continuous-motion mean also describes isolated clicks                   | Distinct sample populations and timestamp behavior                                                  |
| Present-to-display delay measures physical input latency                | It starts after earlier input/application work and ends at a system display timestamp               |
| More than 5,000 FPS proves submillisecond mouse-to-pixel latency        | The archived input fields are unavailable; throughput is a different result                         |
| DOOM is a controlled engine comparison                                  | G-SYNC, Reflex, workload, input path, and presentation mode differ                                  |
| Add Reflex or an in-app limiter to implement earlier suggestions        | Conflicts with current repository constraints; not recommended as an authorized change              |

## Review map and supporting material

The main implementation references are [Application.cpp](../Cpp/Source/Application.cpp), [Renderer.cpp](../Cpp/Source/Renderer.cpp), [preset definitions](../Cpp/Source/RenderPreparation.h), [settings guide](../Cpp/Documentation/Settings.md), [pipeline guide](../Cpp/Documentation/RenderPipeline.md), and the [Godot experiment guide](../InputLatency_Godot/Readme.md). CMake and launchers determine the build and logging path; geometry, SimplePaint, and precompiled shaders determine workload, rather than directly instrumenting input latency. `Godot/` remains a rewrite placeholder. `Blender/` supplies assets.

The [historical throughput report](<../Cpp/SavedLogOutput/2026-09-11%20MinimumInputLatency/Report.md>) and [confidence review](../Cpp/Documentation/Reports/RepositoryReview20260911/Review.md) preserve detailed capture-health evidence. Shader numerical reports and the [orthographic optimization report](../Cpp/Documentation/Reports/Rendering/OrthographicOptimization.md) explain rendering choices but are not input-latency experiments.

The complete repository conversation index reviewed for this report follows. Earlier source paths and proposals are historical; current source and later explicit user decisions take precedence. Conversations about layout, tools, JSON, shader numerics, and repository organization supply context, not additional latency samples.

- [2026-08-30-alt-enter-fullscreen-boop](../Conversations/2026-08-30-alt-enter-fullscreen-boop.md)
- [2026-08-30-borderless-vs-exclusive-fullscreen](../Conversations/2026-08-30-borderless-vs-exclusive-fullscreen.md)
- [2026-08-30-directx-12-scene](../Conversations/2026-08-30-directx-12-scene.md)
- [2026-08-30-directx-cpp-workload](../Conversations/2026-08-30-directx-cpp-workload.md)
- [2026-08-30-hlsl-file-layout](../Conversations/2026-08-30-hlsl-file-layout.md)
- [2026-08-30-latest-powershell-strict-mode](../Conversations/2026-08-30-latest-powershell-strict-mode.md)
- [2026-08-30-message-pump-high-rate-input](../Conversations/2026-08-30-message-pump-high-rate-input.md)
- [2026-08-30-ninja-build-tool](../Conversations/2026-08-30-ninja-build-tool.md)
- [2026-08-30-nvidia-frame-limiter-1000-to-5000](../Conversations/2026-08-30-nvidia-frame-limiter-1000-to-5000.md)
- [2026-08-30-powershell-run-wrapper](../Conversations/2026-08-30-powershell-run-wrapper.md)
- [2026-08-30-remove-alt-enter-fullscreen](../Conversations/2026-08-30-remove-alt-enter-fullscreen.md)
- [2026-08-30-ultra-high-frame-rate-targeting](../Conversations/2026-08-30-ultra-high-frame-rate-targeting.md)
- [2026-08-30-vsync-default](../Conversations/2026-08-30-vsync-default.md)
- [2026-08-30-zoomtracks-car-simplepaint](../Conversations/2026-08-30-zoomtracks-car-simplepaint.md)
- [2026-08-31-rust-directx12-toolchain](../Conversations/2026-08-31-rust-directx12-toolchain.md)
- [2026-08-31-rust-vs-cpp-ai-reviewability](../Conversations/2026-08-31-rust-vs-cpp-ai-reviewability.md)
- [2026-09-04-comprehensive-review-01a06f7c](../Conversations/2026-09-04-comprehensive-review-01a06f7c.md)
- [2026-09-04-moved-repository-cmake-error](../Conversations/2026-09-04-moved-repository-cmake-error.md)
- [2026-09-05-camera-projection-support](../Conversations/2026-09-05-camera-projection-support.md)
- [2026-09-05-cpp-designated-initializers](../Conversations/2026-09-05-cpp-designated-initializers.md)
- [2026-09-05-independent-paint-settings](../Conversations/2026-09-05-independent-paint-settings.md)
- [2026-09-05-runtime-uv-sphere](../Conversations/2026-09-05-runtime-uv-sphere.md)
- [2026-09-05-simplepaint-interface-and-mathematics](../Conversations/2026-09-05-simplepaint-interface-and-mathematics.md)
- [2026-09-05-simplepaint-rewrite](../Conversations/2026-09-05-simplepaint-rewrite.md)
- [2026-09-05-simplepaint-table-spacing](../Conversations/2026-09-05-simplepaint-table-spacing.md)
- [2026-09-06-centralize-shared-constraints](../Conversations/2026-09-06-centralize-shared-constraints.md)
- [2026-09-06-consolidate-readmes](../Conversations/2026-09-06-consolidate-readmes.md)
- [2026-09-06-cpp-ide-recommendations](../Conversations/2026-09-06-cpp-ide-recommendations.md)
- [2026-09-06-cpp-json-library](../Conversations/2026-09-06-cpp-json-library.md)
- [2026-09-06-max-queued-frames-equivalent](../Conversations/2026-09-06-max-queued-frames-equivalent.md)
- [2026-09-06-maximize-fps-pipeline](../Conversations/2026-09-06-maximize-fps-pipeline.md)
- [2026-09-06-visual-studio-shader-headers](../Conversations/2026-09-06-visual-studio-shader-headers.md)
- [2026-09-07-allow-tearing-vsync-off](../Conversations/2026-09-07-allow-tearing-vsync-off.md)
- [2026-09-07-dx12-study-resources](../Conversations/2026-09-07-dx12-study-resources.md)
- [2026-09-07-rewrite-pipeline-guide](../Conversations/2026-09-07-rewrite-pipeline-guide.md)
- [2026-09-08-application-settings-documentation-audit](../Conversations/2026-09-08-application-settings-documentation-audit.md)
- [2026-09-08-finish-render-pipeline-renaming](../Conversations/2026-09-08-finish-render-pipeline-renaming.md)
- [2026-09-08-json-settings-migration-proposal](../Conversations/2026-09-08-json-settings-migration-proposal.md)
- [2026-09-08-remove-json-serialization](../Conversations/2026-09-08-remove-json-serialization.md)
- [2026-09-08-rename-application-settings](../Conversations/2026-09-08-rename-application-settings.md)
- [2026-09-08-simplepaint-self-containment](../Conversations/2026-09-08-simplepaint-self-containment.md)
- [2026-09-08-veehiicuul-architecture-alternatives](../Conversations/2026-09-08-veehiicuul-architecture-alternatives.md)
- [2026-09-11-ApplyUpdatedAgentInstructions](../Conversations/2026-09-11-ApplyUpdatedAgentInstructions.md)
- [2026-09-11-MinimumInputLatencyReport](../Conversations/2026-09-11-MinimumInputLatencyReport.md)
- [2026-09-11-PresentMonOverflowAnalysis](../Conversations/2026-09-11-PresentMonOverflowAnalysis.md)
- [2026-09-11-PresentMonSessionAnalysis](../Conversations/2026-09-11-PresentMonSessionAnalysis.md)
- [2026-09-11-RemovePresentMon](../Conversations/2026-09-11-RemovePresentMon.md)
- [2026-09-11-RepositoryConfidenceReview](../Conversations/2026-09-11-RepositoryConfidenceReview.md)
- [2026-09-11-review-gitignore](../Conversations/2026-09-11-review-gitignore.md)
- [2026-09-11-VerifyPresentMonSplits](../Conversations/2026-09-11-VerifyPresentMonSplits.md)
- [2026-09-12-BuildFolderCapitalization](../Conversations/2026-09-12-BuildFolderCapitalization.md)
- [2026-09-12-CMakePresetsExplanation](../Conversations/2026-09-12-CMakePresetsExplanation.md)
- [2026-09-12-CppMoveReview](../Conversations/2026-09-12-CppMoveReview.md)
- [2026-09-12-RemoveRootLaunchers](../Conversations/2026-09-12-RemoveRootLaunchers.md)
- [2026-09-12-VeehiicuulRename](../Conversations/2026-09-12-VeehiicuulRename.md)
- [2026-09-15-CaptureGSyncState](../Conversations/2026-09-15-CaptureGSyncState.md)
- [2026-09-15-GodotInputLatencyComparison](../Conversations/2026-09-15-GodotInputLatencyComparison.md)
- [2026-09-16-GodotInputLatencyApplication](../Conversations/2026-09-16-GodotInputLatencyApplication.md)
- [2026-09-16-GodotSolutionXml](../Conversations/2026-09-16-GodotSolutionXml.md)
- [2026-09-16-ListLargeRepositoryFiles](../Conversations/2026-09-16-ListLargeRepositoryFiles.md)
- [2026-09-17-DrainInputMessageQueue](../Conversations/2026-09-17-DrainInputMessageQueue.md)
- [2026-09-17-GodotVsyncBufferSettings](../Conversations/2026-09-17-GodotVsyncBufferSettings.md)
- [2026-09-17-ZoomTracksInputLatency](../Conversations/2026-09-17-ZoomTracksInputLatency.md)
