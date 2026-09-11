# Conversation: Minimum-latency PresentMon evidence report

## User

"C:\Users\k\Repository\CPlusPlus\Simple_DirectX12_3D_Game\SavedLogOutput\2026-09-11 MinimumInputLatency"

analyze the presentmon sessions in this folder
create a file called Report.md in that folder
put any code you use in that folder
note that these two sessions come from the same continuously running game session. the captures are take place roughly 80 minutes separate from each other.
include in your report the complete system specs and any other information or context that could be helpful in interpreting the perf results

the whole point of this is to demonstrate with evidence that this application can achieve extremely high fps (in the ~5,000 range), which explains the motivation to rewrite ZoomTracks using c++ and dx12. (i have tested other minimal 3d scenes in godot and unity and they significantly underperform this app.)

## Assistant

I’ll inspect both captures, the system details, and the game’s settings, then write `Report.md` with reproducible analysis code in the same folder. I’ll distinguish application frame rate from displayed frame rate so the evidence for roughly 5,000 FPS is clear.

## Assistant

I’m using the Spreadsheets skill for the CSV analysis. Each capture has 45 parts; I’ll check that they form a continuous timeline and look for the game’s original startup log to establish the capture settings.

## Assistant question

Were both captures taken at the same game resolution and window/fullscreen setting? Please give those settings, plus any hardware overclock or driver overrides you used. I’m collecting the system specs automatically, but the CSVs don’t record these capture conditions.

## Assistant

The earlier split check recorded about 1.7 million frames per capture, which suggests roughly 5,600 FPS over five minutes. I’m recalculating that from every row. The launcher log spans both captures, but it doesn’t record the render resolution or selected GPU, so I’ll label any later system inventory separately from capture-time evidence.

## User

yes both captures were taken at 2560*1440 fullscreen (as in the fullscreen implemented by this app). i was using armoury crate turbo mode

## Assistant

Both captures were at 2560×1440 in the app’s borderless fullscreen mode, with Armoury Crate Turbo mode enabled. I’ll include those confirmed conditions. The system inventory identifies a Core Ultra 9 275HX, RTX 5070 Ti Laptop GPU, and 32 GiB of DDR5 memory.

## Assistant

The full analysis supports the claim: the captures average **5,640 and 5,666 FPS**, and **all 600 complete one-second windows exceed 5,000 FPS**. The later capture is 0.46% faster, so there’s no observed throughput decline between the two samples. I’ll also include the rare stalls and the GPU-timing limitations caused by hardware-accelerated GPU scheduling.

## User

edit:
the whole point of this is to demonstrate with evidence that this application can achieve extremely high fps (in the ~5,000 range) even while using a minimum latency config, which explains the motivation to rewrite ZoomTracks using c++ and dx12. (i have tested other minimal 3d scenes in godot and unity and they significantly underperform this app.)

## Assistant

I’ll make that the report’s central claim: **the app sustains more than 5,000 FPS at 1440p while using `MinimizeInputLatency`**. The saved settings and preset code specify one GPU frame in flight, a presentation limit of one, two back buffers, and spin waits. I’ll distinguish that confirmed configuration from physical input latency, which these captures don’t measure.

## Assistant

An independent pass through all 3.39 million raw rows reproduced the FPS results and found no timestamp gaps or duplicated records across the part boundaries. Both logs report zero lost events, lost buffers, and overflowed presents. I’m finishing the report with those checks, the effective minimum-latency settings, and the system and workload context.

## Assistant

`Report.md` is written, including the minimum-latency result, system inventory, workload details, frame-pacing analysis, and reproducible scripts. I’m checking the final files and recording the conversation in the separate commit required by the repository instructions.

## Assistant

Created [Report.md](<C:/Users/k/Repository/CPlusPlus/Simple_DirectX12_3D_Game/SavedLogOutput/2026-09-11 MinimumInputLatency/Report.md>).

Both captures sustained **5,640–5,666 FPS at 2560×1440 with `MinimizeInputLatency`**. **All 600 complete one-second windows exceeded 5,000 FPS.**

The report includes system specs, configuration evidence, charts, limitations, and reproducible analysis code saved alongside it.

