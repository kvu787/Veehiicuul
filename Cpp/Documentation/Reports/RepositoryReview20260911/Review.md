# Repository review: PresentMon experiment confidence

Project rename on 2026-09-12: saved application labels now use Veehiicuul,
and saved checkout paths are relative to the repository root. Capture and
snapshot fingerprints were refreshed after renaming. The review date,
measurements, executable fingerprints, and conclusions refer to the original run.

Review date: 2026-09-11. Reviewed repository commit: 7a8b14d.
The latest production Source/Assets/CMakeLists.txt commit is
824a787e62f1e1678b48b3a27a5dbcd1459c4e18, which is also the production revision
identified in the archived report.

**The archived presentation-throughput result deserves high confidence within
its stated scope. The repository is suitable for further experiments, but its
current capture/report workflow needs safeguards before it can be trusted to
label and accept new experiments automatically.**

The two captures establish roughly 5,600 application presents per second on
this machine during those periods. They do not measure physical input latency,
rank the presets, or establish a controlled advantage over Godot or Unity.
The archived report already makes those limitations clear.

| Claim                                                     | Review conclusion                                                |
| --------------------------------------------------------- | ---------------------------------------------------------------- |
| Recorded FPS and present-interval statistics              | Reproduced; high confidence for these recorded periods           |
| Every complete fixed one-second window exceeded 5,000 FPS | Reproduced for all 600 windows                                   |
| Zero reported ETW loss and present-buffer overflow        | Confirmed in both original logs                                  |
| Preset was MinimizeInputLatency                           | Corroborated by settings, build, source, timing and user context |
| Resolution was 2560 x 1440 in borderless fullscreen       | User confirmed; actual render dimensions were not logged         |
| Lowest physical input latency                             | Not established; input-latency columns contain no measurements   |
| Exact GPU execution cost or bottleneck                    | Not established by these PresentMon GPU metrics                  |
| FPS during the approximately 74-minute unmeasured gap     | Not established                                                  |
| Relative performance of Standard, MaximizeFps or Custom   | Not measured in these two captures                               |
| Expected speedup of a complete ZoomTracks rewrite         | Not established without matched representative workloads         |

**Findings that matter before collecting a comparison series**

1. **[P1] The README capture block overwrites the first capture.**
   In [README.md](../../../README.md), lines 215-234, the first invocation
   records 300 seconds. The following winpty invocation records 30 seconds
   into the same PresentMon.csv and PresentMon.log, using the same directory.
   Executing the whole fenced block therefore replaces the desired long
   capture with the second capture. PresentMon 2.5.1 opens the CSV in write
   mode (local upstream PresentMon/CsvOutput.cpp, lines 1346 and 1389);
   Tee-Object also replaces its log without an append option.
   Use one chosen invocation per new directory, label alternatives explicitly,
   and verify exit status and measured duration. This problem does not explain
   away the archived results: both archived timelines are approximately
   300 seconds long.

2. **[P1 for reuse] Report generation can certify conditions that its inputs contradict.**
   [CreateReport.py](../../../SavedLogOutput/2026-09-11%20MinimumInputLatency/CreateReport.py),
   lines 31-55 and 354-362, hard-codes the 5,600-FPS narrative, 600 successful
   windows, preset, and zero loss/transition counters. Its only verification
   gate at lines 10-11 checks old Status strings; it does not bind them to
   current inputs. An in-memory reproduction supplied 100 FPS, zero windows
   over 5,000, EventsLost=123, OverflowedPresents=456, and a changed present
   mode. Generation still succeeded and printed zero losses and the original
   5,600-FPS conclusion. No original files were altered by the reproduction. The read-only
   [probe](ProbeReportGenerator.py) preserves the reproduction.
   The script explicitly describes itself as capture-specific, and its
   hard-coded statements match the original captures. Preserve it as the
   historical report generator or replace these statements with validated,
   data-derived claims before reusing it.

3. **[P2] Captures are not durably tied to the active configuration and workload.**
   [Main.cpp](../../../Source/Main.cpp), lines 40-46, logs only application
   start, successful settings loading, and exit. The resolved pipeline is
   sent to OutputDebugString in
   [Renderer.cpp](../../../Source/Renderer.cpp), line 324, and shown in the
   window title. Neither output is saved by the capture recipe. The selected
   adapter, dimensions, process ID, and runtime VSync/fullscreen transitions
   are also absent from the session log.
   Settings load once. Editing or rebuilding the staged JSON while an old
   process remains open does not change that process's settings.
   A later JSON copy can therefore describe a different configuration from
   the one actually measured. The archived attribution is stronger than that
   scenario: preserved timestamps precede launch, settings and executable
   fingerprints agree, the production revision is unchanged, and user context
   corroborates the run. Future experiments should save resolved settings
   from the running process at startup, plus PID, executable identity,
   adapter description/LUID and hardware/WARP status, dimensions and monitor.
   Log changes to VSync/fullscreen/dimensions with timestamps. These records
   need no per-frame file writes.

4. **[P2 for reuse] Analysis and context collection are not a general experiment harness.**
   [AnalyzePresentMon.py](../../../SavedLogOutput/2026-09-11%20MinimumInputLatency/AnalyzePresentMon.py),
   lines 15-16 and 35-47, selects two fixed directories and exactly 45 split
   parts per capture. A fresh unsplit PresentMon.csv from the README is not
   accepted as input. Lines 104-111 record loss counters but do not reject
   losses, missing status samples or abnormal log messages.
   [VerifyAnalysis.py](../../../SavedLogOutput/2026-09-11%20MinimumInputLatency/VerifyAnalysis.py)
   checks timestamps and identities, but never reads the capture log.
   [CollectSystemSpecs.ps1](../../../SavedLogOutput/2026-09-11%20MinimumInputLatency/CollectSystemSpecs.ps1),
   lines 43-68, refreshes the existing archive with current build/settings
   files and hard-coded 13:59:05 application logs. Its documented refresh
   behavior must not be used as capture-time provenance for another run.
   A reusable workflow should accept explicit input/output paths, validate the
   schema and run identity, preserve original inputs, and refuse a clean
   acceptance result when capture health or required provenance is missing.
   Analysis failures must stop report generation rather than leave a stale
   VerificationResults.json usable as a success signal.

P1 identifies an issue to address before relying on the affected workflow.
The reuse qualifications distinguish historical scripts from a proposed
general analysis tool. No production fixes were applied during this review.

**What was verified in the archived data**

Both independent streaming verification and the pandas/NumPy analysis were
executed against all original parts. The streaming checker was run in memory
with only its final output-file write removed. The analysis functions were
called without their file-writing main function. Every saved per-capture
result was compared with the recalculated value, including percentiles,
threshold counts, categories, log diagnostics, and input manifests.

| Measurement                         | Capture A: 13:59:13 | Capture B: 15:18:36 |
| ----------------------------------- | ------------------- | ------------------- |
| Raw data rows                       | 1,692,056           | 1,699,754           |
| First-to-last duration, seconds     | 300.0089115         | 300.0050880         |
| Presentation rate, FPS              | 5,640.0158          | 5,665.7472          |
| Complete one-second windows         | 300                 | 300                 |
| Minimum one-second presentation FPS | 5,100               | 5,150               |
| Maximum present interval, ms        | 7.4818              | 2.1414              |
| ETW status samples                  | 303                 | 302                 |
| EventsLost / BuffersLost / Overflow | 0 / 0 / 0           | 0 / 0 / 0           |

Combined: 3,391,810 rows, 3,391,808 measured intervals, 600.0139995 seconds,
and 5,652.881437 FPS. The second sample is 0.45623% faster.

The calculation uses (N-1) divided by the first-to-last elapsed duration.
It excludes the first interval, whose start lies outside the recorded
timeline, retains every internal stall, and does not average reciprocal
instantaneous FPS. Split boundaries are checked without dropping the first
row of headerless parts. The 600-window claim concerns fixed, nonoverlapping
windows; it is not a claim about every possible sliding one-second interval.

All 90 raw CSV SHA-256 fingerprints match the saved manifests. Both log hashes,
the five copied context files, the installed Release game executable and the
installed PresentMon executable matched their recorded fingerprints before
the build/test runs. The current production source revision matches the one
described in the report. These checks establish consistency with the saved
archive; fingerprints recorded after capture do not independently prove all
capture-time conditions.

The report's local links, aligned tables and Python syntax checks pass.
Its CPU/GPU/display table correctly converts milliseconds to microseconds.
No substantive numerical error was found in the archived report.

**What the renderer and tests support**

The source resolves all three presets and Custom consistently with
[RenderPipeline.md](../../../Documentation/RenderPipeline.md). The renderer
waits for both the selected frame-context fence and selected back-buffer
fence. Presentation admission is separate. Allocators and dynamic constants
are reused after the relevant GPU completion, and input/message processing
runs again after admission, before the animation state is sampled.
A single direct command queue orders the shared depth-buffer accesses.
Resize waits for GPU completion before replacing resources and refreshing
the stationary sphere's constants.

The source renders the same background, car and sphere through three draws
for every preset. All presets retain the same shaders, geometry and render
dimensions. I found no application FPS limiter, vendor-specific runtime path,
or preset-specific shortcut that would inflate the reported presentation
rate. GPU fences and DXGI admission remain synchronization constraints.

| Effective control     | MinimizeInputLatency | Standard | MaximizeFps |
| --------------------- | -------------------- | -------- | ----------- |
| GPU frames in flight  | 1                    | 2        | 3           |
| Presentation limit    | 1                    | 2        | Inactive    |
| Presentation wait     | Enabled              | Enabled  | Disabled    |
| Back buffers          | 2                    | 3        | 4           |
| Wait strategy         | Spin                 | Event    | Spin        |
| Tearing requested     | Yes                  | No       | Yes         |

VSync is independent of the presets. The six Custom controls take effect only
when Preset is Custom. MaxPresentLatency has no effect when
WaitForPresentation is false. Additional frame contexts can remain unused when
back-buffer availability or presentation becomes the tighter constraint.

Release and Debug each passed all 13 top-level CTest tests. This includes
hardware and WARP synchronization, independently sized rings, limits up to 16,
wait cancellation, VSync changes, resize/restore, settings and integer-token
validation, mesh/transform checks, shader comparisons, and the copied-module
standalone build. Each shader GPU run checked 30,208 production VS/PS cases
and reported that its debug layer was checked. The test GPU identities were
the RTX 5070 Ti Laptop GPU and Microsoft Basic Render Driver.

Saved evidence: [ReleaseTests.log](ReleaseTests.log),
[DebugTests.log](DebugTests.log), and [VerificationResults.json](VerificationResults.json).
The test runs used Run.ps1 -Test -Configuration Release and the corresponding
Debug command. Existing test logs were copied after completion.

These tests provide useful correctness evidence on this machine. They do not
benchmark the presets or measure photon latency. Application lifecycle smoke
tests cover Standard and MaximizeFps; MinimizeInputLatency has renderer-level
coverage but no separately registered full application lifecycle test.
There is no exhaustive test of every Custom combination or every Windows GPU.
The README's stated SM6.0/WARP fallback limitation remains.

The broader review inspected first-party C++/HLSL, the build/launcher and
asset-generation code, settings/documentation, current tests, and the requested
capture's analysis/provenance files. The vendored nlohmann/json header matches
its documented pinned checksum. Historical SimplePaint reports are explicitly
marked historical; their old parameter contracts are not current runtime
requirements. This review did not rederive every historical mathematical
report, regenerate the Blender assets, audit all third-party implementation
code, or collect a new performance or physical-latency experiment.

**How to interpret future measurements**

PresentMon can provide useful presentation throughput and pacing measurements
for MinimizeInputLatency, Standard, MaximizeFps and valid Custom settings.
Keep presents that are later discarded in the throughput population; report
displayed-frame behavior separately. A change in PresentMode is a real change
in the experiment's display path and should be reported or segmented.

CPUStartTime in this uninstrumented application is inferred from the previous
Present return. MsCPUBusy includes time between that return and the next
Present call, including the application's admission waits. MsCPUWait describes
time in Present in this path. Therefore changing Event versus Spin can change
CPU resource use without those columns directly measuring actual executing
CPU time. PresentMon's [metric definitions](https://github.com/GameTechDev/PresentMon/blob/v2.5.1/README-ConsoleApplication.md#csv-columns)
and the local v2.5.1 MetricsCalculatorCpuGpu.cpp/CalculateCPUStart implementation
were checked for this distinction.

GPU execution metrics have reduced accuracy with hardware-accelerated GPU
scheduling, as documented in the
[PresentMon 2.5.1 limitation](https://github.com/GameTechDev/PresentMon/blob/v2.5.1/README.md#tracking-gpu-work-with-hardware-accelerated-gpu-scheduling-enabled).
At roughly 0.18 ms per presented frame, do not use small differences in GPU
busy or GPU latency as precise shader-cost measurements or as an explanation
of a throughput difference. Use appropriate GPU timestamps or profiling for
that question and control HAGS consistently across comparisons.

Neither input-to-photon column contains any available value in either capture.
MsUntilDisplayed starts at Present and does not include earlier input handling,
simulation, or the physical display response. The 127-microsecond means are
not end-to-end input latency. Actual latency comparisons require a defined,
repeatable input-to-visible-response workload and suitable timing or external
measurement. Preset names express intended tradeoffs, not measured rankings.

Standard also changes tearing policy, so a direct preset comparison measures
the whole preset bundle. To identify the cause of a difference, start with
equivalent Custom values and vary one control while holding the other active
controls constant. Preserve VSync, actual client pixel dimensions, monitor,
refresh rate, HDR/VRR/driver settings, power mode and AC state, build, scene,
and capture method. Record the effective path rather than assuming identical
flags force identical presentation behavior.

**Recommended acceptance procedure**

1. Build once in Release for a comparison series and preserve executable,
   source revision/dirty state, assets and tool hashes. Relaunch for every
   settings change and record the active process configuration.
2. Run exactly one capture invocation into a new, unique directory. Preserve
   its exact command, PID, start/end times, exit status, CSV and complete log.
   Keep the original unsplit CSV; split only copies if needed.
3. Use the same declared warm-up and measured duration for each condition.
   Keep the game visible on the same display, and avoid resizing, toggling
   VSync, minimization, or test/build activity during a measured interval.
4. Check expected duration, finite and monotonic present timestamps, row
   schema, process/swap-chain identity, split continuity if applicable,
   presentation-mode transitions, and all available loss/overflow counters.
   Missing capture-health evidence is unavailable, not zero. A lossy trace
   should be recaptured or explicitly excluded from a clean comparison.
5. Report elapsed-time-weighted FPS, a defined interval distribution and tail
   metrics, and per-run results. State dropped/displayed populations and
   missing metric counts. Bind verification to current input fingerprints.
6. Repeat independent runs in a balanced or randomized configuration order,
   with several repetitions per setting. Judge improvements against
   between-run variation, not millions of correlated rows in one run.
   The existing two samples from one continuous session cannot establish a
   statistically repeatable 0.46% advantage or a ranking between presets.
7. Check capture overhead separately if small effects matter. Zero ETW loss
   establishes capture health, not zero observer cost. Keep vendor-neutral
   Windows/D3D12/DXGI mechanisms and the repository's no-FPS-limiter constraint.

The proposed safeguards can be added around the existing renderer. The saved
throughput evidence is already useful; the next engineering priority is making
each new capture self-identifying and each report depend on verified inputs.
