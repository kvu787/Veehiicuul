# Conversation: PresentMon input latency capture comparisons

## User

<recommended_plugins>
Here is a list of plugins that are available but not installed.

- Airtable (airtable@openai-curated-remote)
- Alpaca (alpaca@openai-curated-remote)
- Apollo.io (apollo@openai-curated-remote)
- Spotify (app-68de829bf7648191acd70a907364c67c@openai-curated-remote)
- AllTrails (app-68f1afc5a6008191a701eaaab428816c@openai-curated-remote)
- Instacart (app-691eab1e001081919e57189f8b2f03bc@openai-curated-remote)
- Booking.com (app-69272cb413a081919685ec3c88d1744e@openai-curated-remote)
- Apple Music (app-6938a94a61d881918ef32cb999ff937c@openai-curated-remote)
- LONA Trading Assistant (app-694336b0c0948191a4ad234f9942885b@openai-curated-remote)
- SciSpace (app-69439d715a7c8191aed9e2f6649e105f@openai-curated-remote)
- Tarot (app-6943a2c078b0819188de39e4fe168d9b@openai-curated-remote)
- Todoist: To Do List & Calendar (app-6943b73823548191a9f9216c6790c453@openai-curated-remote)
- Consensus (app-6943e6f4a928819195962de16fb9ffe4@openai-curated-remote)
- Sider Scholar (app-6948b485f5bc8191adb4df13f369cec7@openai-curated-remote)
- True Sky (app-69490a4a06148191a0dd78606a3dbf1f@openai-curated-remote)
- Bigdata.com (app-69491eceef3c8191beb70788b7840429@openai-curated-remote)
- Gamma (app-698a098735908191989f5788d7ee317e@openai-curated-remote)
- Tredict (app-69aef5b699a0819184512d57743fc1cd@openai-curated-remote)
- Maersk (app-69b2b5a768d4819190d3a86c5f12e6d9@openai-curated-remote)
- Dropbox (app-69b31dc2110c8191b8b47dc98fe5a052@openai-curated-remote)
- Parqet (app-69b68652f0308191a27d7c7096cab4f6@openai-curated-remote)
- Interactive Brokers (IBKR) (app-69bc11db874881918718abaca20b68ce@openai-curated-remote)
- Financial Datasets (app-69cacd9394a88191ba6564e1bb0430fa@openai-curated-remote)
- Fathom (app-69d88b99c5c481918e8da9225737e1e9@openai-curated-remote)
- vidIQ (app-69dd11f3e50c8191b1ca48d03cf7e2ad@openai-curated-remote)
- TickTick:To-Do List & Calendar (app-69ddbaba3fb48191a825f22c21b0599d@openai-curated-remote)
- Plaud (app-69f3c30d68288191bbd428a394a78407@openai-curated-remote)
- Wolfram (app-69fe0bf66c8481919c513d799406436e@openai-curated-remote)
- Runway (app-6a05e3b201788191be12b590b43e6ce3@openai-curated-remote)
- Caliber (app-6a05e8f22d408191b13ba3897157f6df@openai-curated-remote)
- COROS (app-6a0694cbb2608191bbefb74ba810ab68@openai-curated-remote)
- TradingCursor (app-6a0d835ff1dc8191972eeabd14967446@openai-curated-remote)
- CoinMarketCap (app-6a172fe86f5481919f73cbc3bc3ad5bb@openai-curated-remote)
- Trello (app-6a20b18a639081918c1b438f8381b27e@openai-curated-remote)
- Longbridge (app-6a2baf2fad748191812393c3e00308ef@openai-curated-remote)
- freddy (app-6a322b52a82c8191b7fb653f9e9f7891@openai-curated-remote)
- Higgsfield (app-6a3293e129088191abf0875820e839da@openai-curated-remote)
- Stocktwits (app-6a427a19b1f481919c5db13838af00c2@openai-curated-remote)
- Webull (app-6a4b8801d1e8819182f624f012878a81@openai-curated-remote)
- CoinGecko (app-6a4f02d735388191959c8328877e0bbd@openai-curated-remote)
- Asana (asana@openai-curated-remote)
- Base44 (base44@openai-curated-remote)
- Binance (binance@openai-curated-remote)
- Box (box@openai-curated-remote)
- Canva (canva@openai-curated-remote)
- ClickUp (clickup@openai-curated-remote)
- Cloudflare (cloudflare@openai-curated-remote)
- Codex Security (codex-security@openai-curated-remote)
- Expedia (expedia@openai-curated-remote)
- Figma (figma@openai-curated-remote)
</recommended_plugins>
# AGENTS.md instructions for C:\Users\k\Repository\Veehiicuul

<INSTRUCTIONS>
# Base template

## Platform compatibility

Assume that everything is running on Windows 11 x64 and only targets Windows 11 x64.

## Folder and file naming

This only applies to things that we have the freedom to name as wanted.
Use CamelCase.
Use complete proper words. Don't use typical shortenings. Good: Source, Documentation. Bad: src, docs.

## External tools

You may use the tools in `%UserProfile%\Program`.
You may refer to local copies of source repos in `%UserProfile%\Repository\External`.

## Git

When implementing stuff, avoid difficult-to-review "mega-commits".
Split large work into multiple commits to make it easier to review.
Separate commits that record conversations from other commits.

## Markdown tables

Tables in Markdown must be padded and aligned in a way to make them easy to read in a plaintext editor, not only in a Markdown viewer.

## Mathematical notation in Markdown

Any mathematical notation in Markdown files (LaTeX, KaTeX, MathJax, etc) must display properly in VSCode's Markdown previewer, GitHub.com's Markdown displayer, and the markdown viewer in the Windows 11 ChatGPT app.

## Applications

### Running

If you create a runnable application, create a `Run.cmd` file that builds and launches the application when Run.cmd is double-clicked from File Explorer.
Run.cmd must be located at the root of the application's folder in the git repo.
Run.cmd must be a simple wrapper for a PowerShell script named `Run.ps1` that contains the actual logic to minimize the amount of batch code written.

### Logging

When creating an application, create a folder called `MyLogOutput` at the root of the application's folder in the git repo.
For each run of the application, a folder must be created in MyLogOutput and named with the current timestamp. This PowerShell code shows what the name of the folder should be:

```powershell
$logFolderPath = "$env:UserProfile\Repository\Godot\VsyncStutterTest\MyLogOutput\$(Get-Date -Format "yyyy-MM-dd_HH-mm-ss")"
New-Item -ItemType "Directory" -Path $logFolderPath
```

Any logs for that application session must be put in that log folder.
`MyLogOutput/` must be gitignored.

# Base template additions

## Conversations

Record verbatim and commit all conversations in a folder named `Conversations` located at the root of this Git repo.
Use one file per conversation.
Prefix these commits with `[cnv]`.
If I attach images to prompts, save and record these in the conversation logs.

## Application compatibility

Do not attempt to maintain any sort of application compatibility between different commits of the repo. This creates unwanted complexity.

</INSTRUCTIONS>
<environment_context>
  <cwd>C:\Users\k\Repository\Veehiicuul</cwd>
  <shell>powershell</shell>
  <current_date>2026-09-17</current_date>
  <timezone>America/Los_Angeles</timezone>
  <filesystem><workspace_roots><root>C:\Users\k\Repository\Veehiicuul</root><root>C:\Users\k\.codex\visualizations\2026\09\17\01a0ae80-4871-7ca2-bbe4-c3db692bbefa</root></workspace_roots><permission_profile type="managed"><file_system type="restricted"><entry access="read"><special>:root</special></entry><entry access="write"><path>C:\Users\k\Repository\Veehiicuul</path></entry><entry access="write"><path>C:\Users\k\.codex\visualizations\2026\09\17\01a0ae80-4871-7ca2-bbe4-c3db692bbefa</path></entry><entry access="write"><special>:slash_tmp</special></entry><entry access="write"><special>:tmpdir</special></entry><entry access="read"><path>C:\Users\k\Repository\Veehiicuul\.git</path></entry><entry access="read"><path>C:\Users\k\.codex\visualizations\2026\09\17\01a0ae80-4871-7ca2-bbe4-c3db692bbefa\.git</path></entry><entry access="read"><path>C:\Users\k\Repository\Veehiicuul\.agents</path></entry><entry access="read"><path>C:\Users\k\.codex\visualizations\2026\09\17\01a0ae80-4871-7ca2-bbe4-c3db692bbefa\.agents</path></entry><entry access="read"><path>C:\Users\k\Repository\Veehiicuul\.codex</path></entry><entry access="read"><path>C:\Users\k\.codex\visualizations\2026\09\17\01a0ae80-4871-7ca2-bbe4-c3db692bbefa\.codex</path></entry></file_system></permission_profile></filesystem>
</environment_context>

## User

analyze the input latency for this capture:
"C:\Users\k\Repository\ZoomTracks\ZoomTracks\MyLogOutput\2026-09-17_01-33-02\PresentMon.csv"

context: input mainly mouse movement with a couple mouse clicks.


## Assistant

I’ll inspect the capture’s timing fields and calculate the latency distribution, accounting for the fact that most input was mouse movement.


## Assistant

I’m using the Spreadsheets skill to analyze the CSV. The capture includes both all-input and click-only latency fields; I’ll check their coverage and meaning before interpreting the numbers.


## Assistant

The capture reports very consistent all-input latency: a 9.61 ms median, with 95% of samples at or below 10.15 ms. There are four click samples ranging from 14.28 to 19.60 ms. I’m checking how PresentMon associates input with frames, since that affects what these numbers say about mouse movement.


## Assistant

During the input portion, frames reached the display almost exactly every 10 ms, or 100 FPS. Most of the reported latency was after the Present call: 8.29 ms on average, versus 1.29 ms from the tracked input to Present. That points toward presentation pacing rather than heavy GPU rendering, although this capture cannot identify what enforces the 100 FPS cadence.


## Assistant

**PresentMon reports approximately 9.6 ms latency for your mostly mouse-movement input, with very little variation.** The four click samples are higher: **14.3–19.6 ms**.

Analyzed [PresentMon.csv](C:/Users/k/Repository/ZoomTracks/ZoomTracks/MyLogOutput/2026-09-17_01-33-02/PresentMon.csv): 2,065 frames spanning 21.02 seconds. Missing input values were excluded, not counted as zero.

| Reported latency | All input      | Click-only |
| ---------------- | -------------- | ---------- |
| Samples          | 1,696          | 4          |
| Mean             | 9.58 ms        | 16.89 ms   |
| Median           | 9.61 ms        | 16.84 ms   |
| 95th percentile  | 10.15 ms       | —          |
| 99th percentile  | 11.33 ms       | —          |
| Minimum          | 6.03 ms        | 14.28 ms   |
| Maximum          | 19.60 ms       | 19.60 ms   |

Given your input pattern, the all-input results mainly represent movement. Excluding the four rows with click measurements barely changes the median: **9.612 ms**. The individual click readings were **14.28, 19.60, 18.65, and 15.03 ms**; four samples are insufficient to characterize click latency reliably.

The timing reveals a few useful details:

- **Stable 100 FPS during the input period.** Display intervals averaged 10.000 ms; the 99th percentile was 10.33 ms and the longest was 10.97 ms. There are no large frame-delivery stalls in that period.
- **Most reported latency comes after Present.** On frames with input measurements, the mean splits into 1.29 ms from tracked input to Present, then 8.29 ms until display. GPU busy time averaged only 0.65 ms. This suggests presentation pacing or waiting dominates, rather than GPU rendering work.
- **Startup stalls are separate.** The capture contains 106 ms and 289 ms display gaps before the first input measurement. They do not appear in the input-latency statistics.

Although the capture records `SyncInterval=0` and almost exclusively independent flip with tearing allowed, its 100 FPS cadence does not establish whether a limiter, driver setting, display behavior, or application pacing caused it.

**Measurement limit:** these are software-tracked input-to-display estimates, not a physical mouse-to-visible-pixel measurement. PresentMon associates Windows input events with presented frames; the CSV cannot prove when the game visibly reacted or include all mouse hardware and panel delays. Continuous movement also should not be treated as equivalent to isolated clicks. [PresentMon metric definitions](https://github.com/GameTechDev/PresentMon/blob/main/README-ConsoleApplication.md).

## User

that capture was recorded with no vsync and an nvidia app 100 frame rate limit.

analyze this capture:
"C:\Users\k\Repository\ZoomTracks\ZoomTracks\MyLogOutput\2026-09-17_01-37-02\PresentMon.csv"

context: input mainly mouse movement with a few mouse clicks. also, nvidia low latency mode was switched from off to ultra in the nvidia app.


## Assistant

I’ll compare this capture with the previous one to see whether switching NVIDIA Low Latency Mode to Ultra changed input latency or frame pacing with the 100 FPS limit.


## Assistant

The median is essentially unchanged: 9.613 ms with Low Latency Mode Off versus 9.611 ms with Ultra. Ultra’s 99th percentile is lower, but its worst sample is higher. Both runs maintain the same 100 FPS cadence and roughly 8.3 ms from Present to display, so there is no clear reduction in typical latency.


## Assistant

**Switching Low Latency Mode from Off to Ultra did not meaningfully change typical mouse-movement latency in these captures.** Both report a median of approximately **9.61 ms** with your 100 FPS NVIDIA limiter and VSync disabled.

The [new capture](C:/Users/k/Repository/ZoomTracks/ZoomTracks/MyLogOutput/2026-09-17_01-37-02/PresentMon.csv) contains 3,163 frames over 31.85 seconds, including 2,653 valid all-input latency samples. Missing values are excluded.

| All-input latency | Off: previous | Ultra: new |
| ----------------- | ------------: | ---------: |
| Samples           |         1,696 |      2,653 |
| Mean              |       9.58 ms |    9.51 ms |
| Median            |       9.61 ms |    9.61 ms |
| 95th percentile   |      10.15 ms |   10.11 ms |
| 99th percentile   |      11.33 ms |   10.35 ms |
| Maximum           |      19.60 ms |   21.69 ms |

The mean decreased by just **0.077 ms**, and the median by **0.0023 ms**. The 99th percentile improved by **0.98 ms**, but one run per setting cannot establish that Ultra caused this difference. Excluding rows with click measurements still gives a **9.61 ms median in both runs**.

The eight click readings in the new capture were **11.10, 10.79, 16.63, 13.13, 15.13, 18.23, 14.96, and 14.41 ms**. Their mean decreased from **16.89 to 14.30 ms**, but eight versus four samples is too little evidence to claim a click-latency improvement, especially with clicks occurring at different points within each 10 ms frame interval.

The timing breakdown is also essentially unchanged:

| Mean on frames with input measurements | Off     | Ultra   |
| -------------------------------------- | ------: | ------: |
| Tracked input → Present                 | 1.29 ms | 1.26 ms |
| Present → display                      | 8.29 ms | 8.25 ms |
| GPU busy time                          | 0.65 ms | 0.65 ms |

**The 100 FPS pacing remains the dominant feature.** During the input period, Ultra delivered frames every 10.000 ms on average, with a 99th-percentile display interval of 10.29 ms and a maximum of 10.68 ms. The worst input-latency sample, **21.69 ms at CSV time 13.974 seconds**, occurred with a normal 10.05 ms display interval; it was not accompanied by a display stall.

These results are consistent with a lightly loaded GPU and limiter-related waiting leaving little render-queue latency for Ultra to remove. That is an interpretation, not proof of exactly where the driver waits. The application log confirms DirectX 12; NVIDIA added Ultra Low Latency support for DX12 in January 2024, so DX12 alone does not explain the lack of improvement. [NVIDIA’s announcement](https://www.nvidia.com/en-gb/geforce/news/geforce-rtx-4070-ti-super-rtx-video-hdr-game-ready-driver/).

To isolate the limiter’s contribution, a useful next comparison is **Ultra with the NVIDIA frame-rate limit disabled**, keeping the scene and other settings unchanged. These remain PresentMon software estimates, rather than measurements of physical mouse-to-pixel latency.

## User

analyze input latency for the veehiicuul cpp demo scene:
"C:\Users\k\Downloads\2026-09-17_02-07-27 Veehiicuul.exe logs\PresentMon.csv"

this is with nvidia fps limit of 100 and llm=off

in your analysis, separate the last 15 seconds which are pretty clean, while the earlier parts are less clean


## Assistant

I’ll analyze the final 15 seconds separately from the earlier portion, then compare the clean segment with the previous 100 FPS, Low Latency Mode Off capture.


## Assistant

The final 15 seconds have a 9.38 ms median and 9.44 ms average all-input latency. They are cleaner overall, though occasional spikes remain: the 95th percentile drops from 12.44 to 10.87 ms, while the 99th percentile stays around 13.3 ms. That entire final segment uses independent flip; the earlier portion includes presentation-mode changes.


## Assistant

**The final 15 seconds show about 9.4 ms of reported input latency.** They are cleaner than the earlier portion, but still contain occasional 13–20 ms samples.

I split [the capture](<C:/Users/k/Downloads/2026-09-17_02-07-27 Veehiicuul.exe logs/PresentMon.csv>) at **CSV time 62.939 seconds**. The clean segment runs through 77.939 seconds; the earlier segment spans approximately 48.47 seconds. Missing measurements are excluded.

| All-input latency | Earlier portion | Final 15 seconds |
| ----------------- | --------------: | ---------------: |
| Valid samples     |           3,748 |            1,414 |
| Mean              |         9.73 ms |          9.44 ms |
| Median            |         9.54 ms |          9.38 ms |
| 95th percentile   |        12.44 ms |         10.87 ms |
| 99th percentile   |        13.21 ms |         13.39 ms |
| Maximum           |        30.38 ms |         19.65 ms |

**The improvement is mainly fewer moderately slow samples and removal of the largest outlier.** The 99th percentile does not improve. In the final segment, 43 samples exceeded 12 ms and only one exceeded 16.67 ms.

Click latency is distinctly higher:

| Click-only latency | Earlier portion | Final 15 seconds |
| ------------------ | --------------: | ---------------: |
| Samples            |              16 |               16 |
| Mean               |        15.52 ms |         15.83 ms |
| Median             |        15.07 ms |         16.46 ms |
| Range              |   9.95–21.66 ms |   10.52–20.67 ms |

These small click samples show no improvement in the final segment. Excluding rows with click measurements gives a final-segment all-input median of **9.34 ms**, with a **13.16 ms 99th percentile**. This is a useful movement-dominated estimate, though the CSV does not explicitly identify movement-only samples.

The final segment’s timing explains where the delay appears:

- **Tracked input → Present:** 0.39 ms average.
- **Present → display:** 9.06 ms average on those same input-bearing frames.
- **GPU busy time:** 0.52 ms average on those frames.

Thus, most of the measured delay lies after Present, consistent with the 100 FPS limiter dominating rather than GPU workload. The CSV cannot isolate the driver’s exact waiting mechanism.

Frame delivery averages **10.000 ms, or 100 FPS**, throughout the final segment. Its display-interval 99th percentile is **12.94 ms**, with a maximum of **14.98 ms**: reasonably steady, but not perfectly uniform. All 1,501 final-segment frames use hardware independent flip. The earlier portion includes 80 composed-flip frames; its worst input sample, **30.38 ms**, occurs during composed flip alongside a **25.52 ms display interval**.

Compared with **ZoomTracks at 100 FPS and LLM Off**, this demo’s clean segment has a slightly lower median (**9.38 versus 9.61 ms**) but a higher 95th percentile (**10.87 versus 10.15 ms**) and 99th percentile (**13.39 versus 11.33 ms**). These runs therefore show **similar typical latency, with more variation in the C++ demo**, rather than a substantial latency advantage. Different scenes and input patterns prevent treating this as a controlled engine comparison.

As before, these are PresentMon software estimates, not physical mouse-to-visible-pixel measurements.

## User

analyze input latency for the veehiicuul cpp demo scene:
"C:\Users\k\Downloads\2026-09-17_02-07-27 Veehiicuul.exe logs\PresentMon.csv"
this is with nvidia fps limit of 100 and llm=ultra
the first 5 to 10 seconds of the capture may be less clean, so separate that portion




## Assistant

I’ll separate the first 10 seconds from the remainder, and also check a 5-second cutoff. This is the same file path as the previous Off analysis, so I’ll check whether its contents have changed before comparing the settings.


## Assistant

The file matches the previous analysis in size, frame count, timestamps, and latency results, so I’ll treat “LLM = Ultra” as a correction to its label. One detail matters for the split: the largest spike occurs 10.114 seconds after the first frame, just beyond a 10-second cutoff. I’ll retain it in the main result and show the effect of excluding the first 11 seconds.


## User

analyze input latency for the veehiicuul cpp demo scene:
"C:\Users\k\Downloads\2026-09-17_02-13-32 Veehiicuul.exe logs\PresentMon.csv"
this is with nvidia fps limit of 100 and llm=ultra
the first 5 to 10 seconds of the capture may be less clean, so separate that portion


## Assistant

I’ll use this new path for the Ultra capture, separate the first 10 seconds, and compare the remainder with the earlier Off capture. I’ll also check whether a 5-second cutoff changes the result.


## Assistant

After the first 10 seconds, Ultra reports a 9.47 ms median and 9.55 ms average. Using a 5-second cutoff gives almost identical results, so the conclusion is not sensitive to that choice. Compared with the earlier Off capture’s clean final 15 seconds, typical latency is slightly higher, while the worst observed sample is lower—there’s no clear overall latency reduction.


## Assistant

**With LLM Ultra and the NVIDIA 100 FPS limit, this capture reports about 9.5 ms of typical input latency after startup.** It does not show a clear overall improvement over the previous Off capture’s clean segment.

I analyzed [the new capture](<C:/Users/k/Downloads/2026-09-17_02-13-32 Veehiicuul.exe logs/PresentMon.csv>): **3,309 frames over 33.08 seconds**. The split is 10 seconds after the first recorded frame, at CSV time **23.509 seconds**. Missing input measurements are excluded.

| All-input latency | First 10 seconds | Remaining 23.08 seconds |
| ----------------- | ---------------: | ---------------------: |
| Valid samples     |              600 |                  2,224 |
| Mean              |          9.99 ms |                9.55 ms |
| Median            |          9.69 ms |                9.47 ms |
| 95th percentile   |         13.28 ms |               11.45 ms |
| 99th percentile   |         17.27 ms |               13.51 ms |
| Maximum           |         31.31 ms |               15.94 ms |

**A 5-second cutoff gives essentially the same steady-state result:** mean 9.55 ms, median 9.47 ms, 95th percentile 11.47 ms, and 99th percentile 13.50 ms. The less-clean behavior is concentrated in the first five seconds. The largest spike, **31.31 ms at elapsed time 2.50 seconds**, coincides with composed flip and a 23.23 ms display interval.

After the 10-second cutoff:

- All frames use **hardware independent flip**.
- Display intervals average **10.000 ms**, consistent with 100 FPS. Their 99th percentile is **13.52 ms**, and maximum **14.03 ms**, so some pacing variation remains.
- Average tracked-input-to-Present time is **0.34 ms**, followed by **9.21 ms from Present to display**. GPU busy time averages **0.50 ms** on input-bearing frames. Most reported latency remains after Present, consistent with limiter-related waiting rather than heavy GPU work.
- All-input latency exceeds 12 ms on **90 samples (4.0%)**; none exceed 16.67 ms.

There are **six click measurements** after the cutoff: **12.60, 16.54, 15.55, 15.69, 18.09, and 11.85 ms**. Their mean is **15.05 ms** and median **15.62 ms**. That is too few samples to establish a reliable click-latency difference.

Compared with the previously selected clean portion of the Off capture:

| All-input latency | Off: final 15 seconds | Ultra: after first 10 seconds |
| ----------------- | --------------------: | ---------------------------: |
| Mean              |               9.44 ms |                      9.55 ms |
| Median            |               9.38 ms |                      9.47 ms |
| 95th percentile   |              10.87 ms |                     11.45 ms |
| 99th percentile   |              13.39 ms |                     13.51 ms |
| Maximum           |              19.65 ms |                     15.94 ms |

**Typical latency is effectively similar.** Ultra has a lower observed maximum, but slightly higher median and percentile values. Different input patterns, segment lengths, and one run per setting prevent attributing these small differences to Ultra.

These are PresentMon software estimates, not physical mouse-to-visible-pixel measurements.

## User

analyze input latency this godot app:
"C:\Users\k\Repository\VsyncStutterTest\MyLogOutput\2026-09-17_02-33-46\PresentMon.csv"
this is with nvidia fps limit of 100 and llm=off
separate the initial 5 seconds and final 5 seconds


## Assistant

I’ll split the Godot capture into the first 5 seconds, the middle, and the final 5 seconds, then compare their input latency and frame pacing.


## Assistant

The middle 40.58 seconds report a 9.44 ms median, with 95% of samples at or below 10.20 ms. The first row contains a 1,463 ms input-latency value that heavily inflates the startup average; I’ll report it separately. The final five seconds have more latency spikes, even though frame delivery continues at roughly 100 FPS.


## Assistant

**The Godot app’s middle segment reports about 9.5 ms input latency: 9.44 ms median, with 95% of samples at or below 10.20 ms.** Startup contains one extreme measurement, and the final five seconds are less consistent.

I split [the capture](C:/Users/k/Repository/VsyncStutterTest/MyLogOutput/2026-09-17_02-33-46/PresentMon.csv) relative to its first recorded frame. It contains **5,060 frames over 50.58 seconds**:

| All-input latency | First 5 seconds | Middle: 5–45.58 s | Final 5 seconds |
| ----------------- | --------------: | ----------------: | --------------: |
| Valid samples     |             310 |             3,792 |             284 |
| Mean              |        14.70 ms |           9.51 ms |         9.98 ms |
| Median            |         9.70 ms |           9.44 ms |         9.71 ms |
| 95th percentile   |        12.40 ms |          10.20 ms |        12.75 ms |
| 99th percentile   |        19.39 ms |          12.69 ms |        18.31 ms |
| Maximum           |     1,463.13 ms |          19.39 ms |        19.72 ms |

Missing input values are excluded, not treated as zero. The boundaries in the CSV’s own time are **6.966 seconds** and **47.547 seconds**.

**Startup:** the very first row reports **1,463.13 ms** input latency during composed flip, alongside a 562 ms interval since the preceding Present. This is a startup-associated measurement, not representative of ongoing interaction. Excluding only that row reduces the first-five-second mean to **10.01 ms** and maximum to **19.68 ms**; its median remains **9.70 ms**.

**Middle:** all frames use hardware independent flip. Display intervals average **10.000 ms**, matching the NVIDIA 100 FPS limit; their 99th percentile is **12.60 ms** and maximum **14.46 ms**. Input latency exceeds 12 ms on **66 samples (1.7%)**, and 16.67 ms on **five samples (0.13%)**.

On middle-segment frames with input measurements, the average breakdown is:

- Tracked input → Present: **0.53 ms**.
- Present → display: **8.98 ms**.
- GPU busy time: **0.87 ms**.

Most measured delay therefore lies after Present, consistent with the limiter and presentation timing dominating rather than GPU rendering work.

**Ending:** the input-latency tail worsens, but display intervals still average 10.000 ms. The largest late input spikes occur with approximately normal 10 ms display intervals, so they do not correspond to large display stalls. No input-latency samples appear during the final **1.63 seconds**, although frames continue.

Click-only measurements are higher than the movement-dominated all-input values:

| Click latency | First 5 seconds | Middle   | Final 5 seconds |
| ------------- | --------------: | -------: | --------------: |
| Samples       |              17 |       15 |               3 |
| Mean          |        15.13 ms | 14.04 ms |        15.04 ms |
| Median        |        13.92 ms | 14.42 ms |        14.70 ms |

Compared with the **C++ demo’s clean LLM-Off segment**, Godot has essentially the same typical latency (**9.44 versus 9.38 ms median**), with somewhat lower 95th/99th percentiles (**10.20/12.69 versus 10.87/13.39 ms**). These are separate runs and scenes, so this suggests similar performance under the limiter, not a controlled engine ranking.

These remain PresentMon software estimates, not physical mouse-to-visible-pixel measurements.

## User

analyze input latency this godot app:
"C:\Users\k\Repository\VsyncStutterTest\MyLogOutput\2026-09-17_02-38-05\PresentMon.csv"
this is with nvidia fps limit of 100 and llm=ultra 
separate the initial 5 seconds and final 5 seconds




## Assistant

I’ll split this Ultra capture into the first 5 seconds, middle, and final 5 seconds, then compare the middle with the previous Godot LLM-Off capture.


## Assistant

Ultra’s middle segment has a slightly higher median than Off—9.61 versus 9.44 ms—but a slightly lower 99th percentile, 12.26 versus 12.69 ms. Both remain near 100 FPS, with about 9 ms from Present to display. This capture also has a large first-row startup measurement, which I’ll distinguish from normal interaction.


## Assistant

**Godot with LLM Ultra still reports roughly 9.6 ms typical input latency at the NVIDIA 100 FPS limit.** Compared with Off, the middle segment has a slightly higher median but a slightly better latency tail; there is no clear overall improvement.

The [capture](C:/Users/k/Repository/VsyncStutterTest/MyLogOutput/2026-09-17_02-38-05/PresentMon.csv) contains **4,245 frames over 42.43 seconds**. Splitting relative to the first recorded frame:

| All-input latency | First 5 seconds | Middle: 5–37.43 s | Final 5 seconds |
| ----------------- | --------------: | ----------------: | --------------: |
| Valid samples     |             423 |             3,086 |             215 |
| Mean              |        11.08 ms |           9.59 ms |         9.61 ms |
| Median            |         9.61 ms |           9.61 ms |         9.61 ms |
| 95th percentile   |        10.37 ms |          10.13 ms |        10.32 ms |
| 99th percentile   |        12.74 ms |          12.26 ms |        13.28 ms |
| Maximum           |       619.40 ms |          19.94 ms |        17.41 ms |

Missing input values are excluded. The split boundaries in CSV time are **5.660 seconds** and **38.091 seconds**.

**Startup:** the first row reports **619.40 ms** during composed flip. Excluding only that startup-associated sample brings the first-five-second mean to **9.64 ms** and maximum to **17.23 ms**. Its median remains 9.61 ms.

**Middle:** all 3,243 frames use hardware independent flip. Display intervals average **10.000 ms**, with a 99th percentile of **12.42 ms** and maximum **13.90 ms**. Of the input-latency samples, **36 (1.17%) exceed 12 ms**, and **two (0.065%) exceed 16.67 ms**.

The mean timing breakdown on middle-segment frames with input measurements is:

- Tracked input → Present: **0.47 ms**.
- Present → display: **9.12 ms**.
- GPU busy time: **0.80 ms**.

Most measured delay remains after Present, consistent with limiter/presentation waiting rather than heavy GPU rendering. The largest middle input spike, **19.94 ms**, occurs with a **10.33 ms display interval**, rather than a large display stall.

**Ending:** the median stays at 9.61 ms, with a modestly worse tail. No input-latency samples are recorded during the final **2.11 seconds**, although frames continue, so the ending statistics describe only the available input-bearing frames.

| Click-only latency | First 5 seconds | Middle   | Final 5 seconds |
| ------------------ | --------------: | -------: | --------------: |
| Samples            |               6 |        6 |               2 |
| Mean               |        15.40 ms | 14.42 ms |        12.09 ms |
| Median             |        15.36 ms | 13.90 ms |        12.09 ms |

These click sample counts are too small for a reliable setting comparison.

Comparing the middle segments of the two Godot captures:

| All-input latency | LLM Off  | LLM Ultra |
| ----------------- | -------: | --------: |
| Mean              |  9.51 ms |   9.59 ms |
| Median            |  9.44 ms |   9.61 ms |
| 95th percentile   | 10.20 ms |  10.13 ms |
| 99th percentile   | 12.69 ms |  12.26 ms |
| Maximum           | 19.39 ms |  19.94 ms |

**Ultra did not reduce typical latency in these runs.** The small percentile improvements could reflect ordinary run-to-run variation; repeated matched captures would be needed to establish an effect. These are PresentMon software estimates, not physical mouse-to-visible-pixel measurements.

## User

analyze input latency for this capture:
"C:\Users\k\Downloads\2026-09-17_10-42-33 DOOMTheDarkAges.exe logs\PresentMon.csv"

this is with nvidia fps limit of 100 and llm=off
separate the initial 10 seconds and final 10 seconds


## Assistant

I’ll split the DOOM capture into the first 10 seconds, the middle, and the final 10 seconds, then check input latency and frame pacing in each.


## User

also, gsync was on


## Assistant

I’ll include G-SYNC as enabled in the test conditions. The middle segment reports a 7.36 ms median and 7.53 ms average—lower than the earlier demo captures—but no click-only measurements are recorded. GPU busy time is much higher here, so the timing breakdown does not support the same “mostly limiter waiting” interpretation as the demos.


## User

gsync was off for all captures prior to "C:\Users\k\Downloads\2026-09-17_10-42-33 DOOMTheDarkAges.exe logs\PresentMon.csv"


## Assistant

Understood: all earlier captures had G-SYNC Off; this DOOM capture has it On. That makes G-SYNC another changed variable, so the lower DOOM latency cannot be attributed to the game alone.


## Assistant

**The middle of this DOOM capture reports 7.36 ms median input latency and 7.53 ms average**, with NVIDIA’s 100 FPS limit, LLM Off, and G-SYNC On. Most samples are tightly grouped, with occasional spikes into the teens.

All earlier captures had **G-SYNC Off**, so the cross-app comparison also changes the display configuration.

The [capture](<C:/Users/k/Downloads/2026-09-17_10-42-33 DOOMTheDarkAges.exe logs/PresentMon.csv>) contains **9,875 frames over 98.76 seconds**. These segments are measured from the first recorded frame:

| All-input latency | First 10 seconds | Middle: 10–88.76 s | Final 10 seconds |
| ----------------- | ---------------: | -----------------: | ---------------: |
| Valid samples     |              334 |              6,554 |              382 |
| Mean              |          8.18 ms |            7.53 ms |          7.18 ms |
| Median            |          8.04 ms |            7.36 ms |          7.50 ms |
| 95th percentile   |          9.81 ms |            8.71 ms |          9.62 ms |
| 99th percentile   |         16.30 ms |           12.90 ms |         12.82 ms |
| Maximum           |         36.66 ms |           18.56 ms |         17.77 ms |

Missing input measurements are excluded. The split boundaries in CSV time are **10.006 seconds** and **88.770 seconds**.

**First 10 seconds:** input measurements begin about four seconds into the capture. The largest latency spike, **36.66 ms at elapsed time 4.02 seconds**, coincides with a presentation-mode transition and a **25.11 ms display interval**. This portion contains 402 composed-flip frames.

**Middle:** all 7,874 frames use **Hardware Composed: Independent Flip**. Display intervals average **10.003 ms**, approximately **99.97 FPS**. Their 95th percentile is **11.07 ms**, 99th percentile **11.59 ms**, and maximum **14.86 ms**. Frame pacing is reasonably consistent, though not perfectly uniform with G-SYNC enabled.

Of the middle segment’s input samples:

- **144 (2.20%)** exceed 10 ms.
- **91 (1.39%)** exceed 12 ms.
- **13 (0.20%)** exceed 16.67 ms.
- None exceed 20 ms.

On those input-bearing frames, the averages are **0.28 ms from tracked input to Present**, then **7.25 ms from Present to display**. GPU busy time averages **9.36 ms**—substantially more GPU work than in your small demo apps. The after-Present interval therefore should not be interpreted as pure limiter waiting. GPU work overlaps other timings; these values are not three sequential stages to add together.

**Final 10 seconds:** the lower mean does not indicate consistently better responsiveness: the median is slightly higher and the distribution is wider. GPU workload also drops, so this is a different workload mix. Input measurements stop **3.71 seconds before the capture ends**. Later display gaps of **31.22 and 43.76 ms** have no associated input-latency measurement, so they do not appear in the input statistics.

**There are no click-to-photon samples anywhere in this CSV.** That does not establish that no clicks occurred; click latency cannot be assessed from this capture.

Compared with the Godot LLM-Off middle segment, DOOM’s reported median is about **2.08 ms lower** (**7.36 versus 9.44 ms**). That is a difference in the recorded metric, not proof of lower physical input latency: game behavior, input-to-frame association, presentation path, and display settings can differ. The capture also cannot isolate G-SYNC’s contribution without a matched G-SYNC-Off run.

## User

also, note that the "C:\Users\k\Downloads\2026-09-17_10-42-33 DOOMTheDarkAges.exe logs\PresentMon.csv" capture had NvidiaReflex=on while the other apps didn't implement nvidia reflex 


## Assistant

I’ll record Reflex On for the DOOM capture and no Reflex integration for the other apps. Reflex is another changed variable, so DOOM’s lower reported latency cannot be attributed to G-SYNC or the game alone.


## Assistant

Recorded the test conditions:

| Captures         | NVIDIA FPS limit | LLM          | G-SYNC | NVIDIA Reflex   |
| ---------------- | ---------------- | ------------ | ------ | --------------- |
| DOOM 10:42:33    | 100              | Off          | On     | On              |
| All earlier apps | 100              | Off or Ultra | Off    | Not implemented |

The measured results are unchanged. DOOM’s lower reported latency reflects a configuration with both **G-SYNC and Reflex enabled**; these captures cannot isolate either feature’s contribution or establish an engine-level advantage.

## User

okay,  when using an nvidia frame limiter with no vsync, it seems like the best mean input latency as measured by presentmon is 1 frame interval and the best worst-case input latency is 2 frame intervals?


## Assistant

I’ll check whether that pattern is a general limit or a consequence of how these apps and PresentMon handle input around the frame limiter.


## User

okay,  when using an nvidia frame limiter with no vsync, it seems like the best mean input latency as measured by presentmon is 1 frame interval and the best worst-case input latency is 2 frame intervals?


## Assistant

That is roughly the pattern in your non-Reflex captures, but movement and click measurements need different interpretations. I’m checking the distinction before treating one and two frame intervals as limits.


## Assistant

**Approximately for your movement-dominated captures, yes—but these measurements do not establish a best achievable latency or a universal bound.**

At 100 FPS, one frame interval is **10 ms**. Your non-Reflex apps showed:

| Measurement                         | Observed pattern              |
| ----------------------------------- | ----------------------------- |
| Mean all-input latency, mostly motion | About 9.4–9.6 ms, roughly 1 frame |
| Mean click latency                  | About 14–17 ms, roughly 1.5 frames |
| Largest normal-interaction samples  | Often around 20 ms, roughly 2 frames |

The distinction matters because continuous mouse movement supplies fresh events throughout each frame. A frame-associated all-input measurement is **not the average delay experienced by every individual mouse event**. PresentMon’s input association can update the tracked input timestamp as newer input arrives. [PresentMon input-tracking implementation](https://github.com/GameTechDev/PresentMon/blob/main/PresentData/PresentMonTraceConsumer.cpp).

A useful simplified model for an **isolated click**, assuming input is sampled once per frame and clicks arrive at random times, is:

- Wait until the next input sample: **0–1 frame**, averaging **half a frame**.
- Then add the delay from sampling that input to displaying its result, call it **D**.
- Mean latency: **half a frame + D**.
- Worst timing within that cycle: **one frame + D**, before extra stalls.

**If D is approximately one frame**, this predicts a click mean near **1.5 frames** and an upper end near **2 frames**—consistent with your click measurements.

But **D does not have to equal one frame**. Where the limiter waits relative to input sampling and rendering matters; NVIDIA describes how Reflex changes that scheduling. [NVIDIA’s explanation](https://www.nvidia.com/en-gb/geforce/news/reflex-low-latency-platform/). Stalls can also exceed two frames, as some of your captures already demonstrate.

So I would describe your finding as: **“These apps, with this NVIDIA limiter configuration, report roughly one-frame mean movement latency and roughly two-frame upper-end interaction latency.”** Establishing how this scales would require repeating the test at other caps, such as 60 and 144 FPS.

## User

okay, review this conversation, all other conversations, and the entire repo, and create a comprehensive report on input latency and commit it to C:\Users\k\Repository\Veehiicuul\Documentation\InputLatency.md



## Assistant

I’ll review the repository, its recorded conversations, and the available capture evidence, then write and commit a report that separates measured results from explanations and open questions.



## Assistant

The repository’s constraints matter to the conclusions: it prohibits both an application-owned FPS limiter and vendor-specific integrations such as Reflex. I also confirmed that the captured Godot app is a separate project from the newer `InputLatency_Godot` experiment, so the report will keep their settings and evidence distinct.



## Assistant

The review confirms two distinctions worth making explicit: the archived 5,600 FPS runs measured presentation throughput, with no valid input-latency samples; and PresentMon can associate input with a frame even when the demo does not visibly react to that input. I’m now recalculating all seven recent captures with consistent statistics and documenting the exact time windows.



## Assistant

The recalculated capture statistics agree with the earlier answers. The report will also correct several easy misreadings: two buffers do not imply two frames of delay, Ultra is supported in DX12, and the roughly one-frame average seen here is an observed pattern rather than a latency floor. I’m preserving the startup and ending results alongside the selected comparison windows.



## Assistant

The report draft now brings together the capture results, rendering code, and earlier discussions. I’m checking its tables and references before committing it, including which measurements support conclusions about input latency and which establish only frame throughput.



## Assistant

The numerical checks passed for all seven captures, and the report now includes the requested time segments, click samples, file fingerprints, and links to all 63 conversation records. The main finding remains: the non-Reflex runs cluster near 9.4–9.6 ms, but they do not establish a one-frame minimum or a two-frame ceiling.



## Assistant

The report is complete. It covers the measurements, C++ and Godot input paths, queue settings, measurement limits, and controlled follow-up experiments. The tables and local references passed validation; I’m committing the report and then recording this conversation in a separate commit, as the repository requires.



## Assistant

Created and committed [InputLatency.md](C:/Users/k/Repository/Veehiicuul/Documentation/InputLatency.md) as `36d845f`.

It covers all 63 recorded conversations, C++/Godot implementation, seven recent captures, historical runs, measurement limitations, and follow-up experiments. The conversation log was committed separately.
