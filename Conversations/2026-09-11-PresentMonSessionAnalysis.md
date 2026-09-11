# Conversation: PresentMon session analysis

## User

analyze this presentmon session:
"C:\Users\k\Repository\CPlusPlus\Simple_DirectX12_3D_Game\MyLogOutput\2026-09-11_13-33-39\PresentMon.csv"

## Assistant

I’ll inspect the capture and repository constraints, then analyze frame pacing, dropped frames, and any stalls.

## Assistant

I’m using the Spreadsheets skill to analyze the CSV. The opening frames show VSync off with tearing allowed and independent-flip presentation; I’ll check whether that remains consistent and verify the capture’s event-loss report.

## Assistant

The capture is clean: all reported loss counters are zero. It contains 174,759 frames over 30 seconds at about 5,825 presents per second. The longest presentation gap is 1.70 ms, with only four gaps above 1 ms. I’m checking the timing definitions before drawing conclusions about GPU load and latency.

## Assistant

**This is a clean capture with very high throughput and no large frame-pacing stalls.** The game recorded **174,759 presents over 30.003 seconds**, averaging **5,825 FPS**.

| Presentation metric                 | Result    |
| ----------------------------------- | --------- |
| Mean frame interval                 | 0.1717 ms |
| Median frame interval               | 0.1719 ms |
| 99th-percentile frame interval       | 0.2696 ms |
| 99.9th-percentile frame interval     | 0.3621 ms |
| Longest frame interval              | 1.6963 ms |
| 1% low FPS                          | 3,132     |
| 0.1% low FPS                        | 2,241     |

FPS is calculated from the mean interval between presents. The low-FPS figures use the mean of the slowest 1% and 0.1% of intervals.

**Frame pacing:** Only **25 intervals exceeded 0.5 ms**, and **four exceeded 1 ms**. None exceeded 2 ms. The worst gap occurred **14.631 seconds into the capture**. Complete one-second windows ranged from **5,593 to 6,064 presents**, showing modest variation without a sustained slowdown.

**Capture quality:** Every status entry in [PresentMon.log](C:/Users/k/Repository/CPlusPlus/Simple_DirectX12_3D_Game/MyLogOutput/2026-09-11_13-33-39/PresentMon.log) reports zero lost ETW events, lost buffers, and overflowed presents. The earlier overflow problem did not recur here. All rows have display timing, with no undisplayed presents indicated.

**Presentation behavior:** The entire session used one process and swap chain, VSync off, tearing allowed, and `Hardware: Independent Flip`. There were no presentation-mode transitions. At this setting, 5,825 FPS describes presentation throughput; it does **not** mean the monitor displayed 5,825 complete images per second.

**GPU and latency:** Average GPU busy time was **0.1448 ms**, with **0.0268 ms** of GPU wait within the measured frame. Average time inside `Present()` was **0.0488 ms**. The worst presentation gap coincided with **1.6384 ms of CPU-side elapsed time** and **1.4389 ms of GPU wait**, suggesting delayed CPU submission or synchronization rather than an unusually expensive GPU workload for that frame. This CSV cannot identify the underlying cause.

Present-to-display timing averaged **0.1241 ms** and reached **0.2012 ms at the 99th percentile**. This metric starts at the `Present()` call; it is not end-to-end input latency. Both input-to-photon columns are entirely unavailable. See [PresentMon’s metric definitions](https://github.com/GameTechDev/PresentMon/blob/main/README-ConsoleApplication.md).

The session provides **no evidence of multi-millisecond rendering hitches**. If motion looked uneven, this capture alone cannot establish the cause or measure full-screen visual smoothness under tearing.

- :codex-followup[Compare captures]{prompt="Compare this PresentMon session with another available session and quantify throughput, frame pacing, and latency differences."}
- :codex-followup[Investigate the largest gaps]{prompt="Inspect neighboring frames around the largest gaps in this PresentMon session and relate the timing patterns to the application's render loop."}
- :codex-followup[Plan a smoothness comparison]{prompt="Design a VSync-on versus VSync-off PresentMon comparison for this application, respecting the repository's prohibition on application-owned frame rate limiting."}

