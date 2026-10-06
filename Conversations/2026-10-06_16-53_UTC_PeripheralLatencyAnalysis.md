# Peripheral latency analysis conversation

## User

# AGENTS.md instructions for C:\Users\k\Repository\Veehiicuul

<INSTRUCTIONS>
# Base template

## Style

For single and double quotes, only use the ASCII forms: ', "
Never use these: “, ”, ‘, ’, etc.

## Development platform compatibility

Support Windows 11 x64 as the only development platform.

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

## PowerShell

All PowerShell scripts must use:

- Set-StrictMode -Version Latest
- $ErrorActionPreference = 'Stop'

## Godot

When creating a Godot application:

- Use Godot 4.7.2 .NET
- Use C#
- Don't use GDScript
- Halt if you don't find a portable/self-contained install of Godot 4.7.2 .NET at `%UserProfile%\Program\Godot_v4.7.2-stable_mono_win64`
- Halt if that install of Godot doesn't have export templates installed
- Build.cmd must do all building/exporting using release configuration with optimizations fully enabled and use Godot's export via the command-line to create an EXE
- Use DirectX 12
- Keep vsync off
- Keep max fps limiter off
- Set rendering_device/vsync/swapchain_image_count=2
- Set rendering_device/fallback_to_vulkan=false
- Set rendering_device/fallback_to_opengl3=false
- Use Forward+ renderer

The Godot csproj must include this:

```xml
<PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>

    <!-- Enables nullable reference annotations and warnings to catch potential null errors. -->
    <Nullable>enable</Nullable>

    <!-- Enforces the repository's configured code-style rules during builds. -->
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>

    <!--
        Required for IDE0005 to work.
        Enables the build-time IDE0005 check for unused using directives by generating XML documentation.
    -->
    <GenerateDocumentationFile>true</GenerateDocumentationFile>

    <!--
        Required in Godot .NET/C# projects.
        Prepares the C# library and its dependencies for dynamic loading by Godot.
    -->
    <EnableDynamicLoading>true</EnableDynamicLoading>

    <!--
        Required to properly export a Godot .NET/C# project using the command-line (instead of the Godot Editor export GUI).
        Prevents an idle compiler server from keeping Godot's Windows console wrapper waiting after export.
        See https://github.com/godotengine/godot/issues/110101 for more information.
    -->
    <UseSharedCompilation>false</UseSharedCompilation>
<PropertyGroup>
```

## Applications

### Running

If you create a runnable application, create files called `Build.cmd` and `Run.cmd` that respectively build and run the application when double-clicked from File Explorer.
These must be located at the root of the application's folder in the Git repo.
These must be simple wrappers for PowerShell scripts named `Build.ps1` and `Run.ps1` which contain the actual logic to minimize the amount of batch code written.
Run.cmd must exit if it doesn't discover a build of the application at the place that Build.cmd outputs to.
If the application doesn't need to be "built" for it to be run (such as a PowerShell script), then omit Build.cmd and Build.ps1.

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
Prefix these files with the PowerShell evaluation of `[DateTime]::UtcNow.ToString("yyyy-MM-dd_HH-mm") + "_UTC"`.
Prefix these commits with `[cnv]`.
If I attach images to prompts, save and record these in the conversation logs.
If the conversation begins with `dnr`, then do not record the conversation.

## Application compatibility

Do not attempt to maintain any sort of application compatibility between different commits of the repo. This creates unwanted complexity.

## Target platform compatibility

Support Windows 11 x64 as the only target platform.

</INSTRUCTIONS><environment_context>
  <cwd>C:\Users\k\Repository\Veehiicuul</cwd>
  <shell>powershell</shell>
  <current_date>2026-10-06</current_date>
  <timezone>America/Los_Angeles</timezone>
  <filesystem><workspace_roots><root>C:\Users\k\Repository\Veehiicuul</root><root>C:\Users\k\.codex\visualizations\2026\10\06\01a1121b-1d43-7622-88b2-50d130185934</root></workspace_roots><permission_profile type="managed"><file_system type="restricted"><entry access="read"><special>:root</special></entry><entry access="write"><path>C:\Users\k\Repository\Veehiicuul</path></entry><entry access="write"><path>C:\Users\k\.codex\visualizations\2026\10\06\01a1121b-1d43-7622-88b2-50d130185934</path></entry><entry access="write"><special>:slash_tmp</special></entry><entry access="write"><special>:tmpdir</special></entry><entry access="read"><path>C:\Users\k\Repository\Veehiicuul\.git</path></entry><entry access="read"><path>C:\Users\k\.codex\visualizations\2026\10\06\01a1121b-1d43-7622-88b2-50d130185934\.git</path></entry><entry access="read"><path>C:\Users\k\Repository\Veehiicuul\.agents</path></entry><entry access="read"><path>C:\Users\k\.codex\visualizations\2026\10\06\01a1121b-1d43-7622-88b2-50d130185934\.agents</path></entry><entry access="read"><path>C:\Users\k\Repository\Veehiicuul\.aws</path></entry><entry access="read"><path>C:\Users\k\.codex\visualizations\2026\10\06\01a1121b-1d43-7622-88b2-50d130185934\.aws</path></entry></file_system></permission_profile></filesystem>
</environment_context>

## User

<external_codex_apps_open_page>{"page_id":null}</external_codex_apps_open_page>

## User

Given this session capture: "C:\Users\k\Repository\Veehiicuul\Veehiicuul_Godot_CSharp\Veehiicuul\SavedLogOutput\2026-10-06_08-41-32"

Evaluate how much of a difference input and output peripherals impact the end-to-end latency and overall quality of the game experience.

For example, analyze these options:
- Acer Nitro XV273U F5 monitor (540 hz and 1000 hz modes)
- Asus PA278QGV monitor (120 hz)
- Standard 60 to 120 hz budget monitor
- ASUS ROG Raikiri II Pro PC controller
- Void GENESIS controller
- Razer Wolverine V3 Pro 8K PC controller
- GameSir G7 Pro 8K controller
- GameSir Tarantula 8K PC controller
- Standard xbox series controller
- Standard ps5 controller

analyze a variety of options from the lowest to the highest performance tiers and various levels in between.

also, consider locked fps levels for the game of 60, 120, 240, 480, and 960

## Assistant Commentary

I will inspect the session's timing data and look up the peripherals' specs and measured latency, then compare their impact at each requested frame-rate cap.

## Assistant Commentary

The game was rendering at about 1,248 FPS on a 120 Hz display. That makes the peripherals worth examining: the software was fast enough for controller delay and display behavior to account for much of the remaining latency.

The capture's roughly 1.3 ms input metric does not measure physical controller-to-pixel latency. I will keep the recorded timings separate from the estimates below.

## User

fyi, this capture was taken with nvcp low latency mode = ultra

## Assistant Commentary

I will include NVCP Low Latency Mode = Ultra in the baseline. I will also check its behavior with DirectX 12 before estimating frame-queue delay.

Your game reads the right stick for acceleration and the left trigger for braking, so analog-input latency and stick filtering matter more here than button-only results.

## Assistant Question

Which controller, connection mode (USB, dongle, or Bluetooth), and monitor were used for this capture? Firmware and controller polling settings would also help if you know them.

## Assistant Commentary

The hardware tests reinforce that polling rate alone is a poor predictor. Some 8 kHz controllers have sub-millisecond stick response; others take several milliseconds, depending on firmware and connection mode.

I also confirmed that NVIDIA added Ultra Low Latency support for DirectX 12 in driver 551.23. The capture is therefore an Ultra-enabled baseline, though it cannot show how much Ultra improved latency without a matching run with it disabled.

## User

<send_user_message_question_reply>
[{"questionItemId":"[\"request_user_input_async\",\"call_HI0Q9JYwbF70FsRhZNaT48vK\",0]","question":"Which controller, connection mode (USB, dongle, or Bluetooth), and monitor were used for this capture? Firmware and controller polling settings would also help if you know them.","answer":"i didn't use any controller. i used a wireless razer deathadder v4 pro mouse at 1000 hz polling rate and a wired kinesis advantage 360 smartset keyboard. i used an asus pa278qgv monitor at 120 hz connected displayport to thunderbolt 5."}]
</send_user_message_question_reply>

## Assistant Commentary

That clarifies the baseline: the capture contains input from a 1 kHz DeathAdder V4 Pro mouse and a wired Advantage360 keyboard, on the PA278QGV at 120 Hz.

Because the game's driving controls read controller axes, the CSV does not measure steering or braking response in this run. I will use it as evidence of rendering and presentation performance, and include your actual mouse, keyboard, and Thunderbolt display connection in the comparison.

## Assistant Commentary

The comparison shows a clear pattern: moving from 60 to 120 or 240 Hz offers much larger timing gains than the final step from 540 to 1,000 Hz. The Acer's 1,000 Hz mode also drops resolution from 1440p to 720p.

At the same frame rate, changing from a wired Xbox Series controller to a controller with roughly 0.5 ms measured stick response can save about 8 ms in those hardware tests. Raising an already fast game from 480 to 960 FPS saves only about 0.52 ms of average frame-sampling delay. I will keep those gains separate from panel response and the game's deadzones.

## User

also, refer to the presentmon source code at "C:\Users\k\Repository\External\PresentMon_2-6-0" to fully understand how it measures things

## Assistant Commentary

I will check the local PresentMon 2.6.0 source, focusing on input association, display timestamps, and scanout. That will help establish exactly which parts of physical end-to-end latency the CSV can support.

## Assistant Commentary

The PresentMon source confirms that the 1.31 ms figure ends at an ETW-derived display timestamp, before a pixel's visible response. Its input tracking starts when Windows reads a device report and can retain the newest retrieved input before a present. It therefore cannot establish the latency of the first physical action or a controller response.

I also found an NVIDIA timestamp adjustment that can produce repeated display times. Those repeats alone are not proof of visible stutter.

## Assistant Commentary

The source review also changes how to read the CPU/GPU columns: `MsCPUWait` is time inside Present, and `MsGPUTime` includes GPU idle gaps as well as active work. Adding all those columns would double-count parts of the timeline.

The comparison still favors a monitor upgrade over raising your mouse from 1 to 8 kHz. For controller play, measured analog response and usable deadzones matter more than the advertised polling rate.

## Assistant Final

**Peripherals can make a substantial difference in this game. Once rendering is already above 480 FPS, controller response and monitor behavior can matter more than another doubling of FPS.** The biggest improvements come from leaving the slowest tiers; the gains between the fastest products are much smaller.

For perspective, the comparisons below imply approximately:

- **8 ms less controller delay** when moving from a wired Xbox Series controller to the fastest tested stick implementations.
- **3.24 ms less average display timing delay** when moving from 120 to 540 Hz, before accounting for differences in pixel response and monitor processing.
- **0.52 ms less average game-input sampling delay** when increasing 480 to 960 FPS.
- **0.43 ms less average display timing delay** when increasing 540 to 1,000 Hz, with the Acer also dropping from 1440p to 720p.

The controller figures come from hardware tests; the frame-rate and refresh-rate figures are calculations under the assumptions explained below. They are not measured end-to-end results for your machine.

Your capture provides a useful software baseline. Excluding its first second, it averages **1,248 FPS**, with **0.799 ms median** and **1.101 ms 99th-percentile** present intervals. Median GPU busy time is **0.349 ms**. It uses DirectX 12, VSync disabled, and steady-state hardware independent flip with tearing allowed. Your clarification adds **NVCP Low Latency Mode = Ultra**, the **DeathAdder V4 Pro at 1 kHz**, the **Advantage360 SmartSet**, and the **PA278QGV at 120 Hz over DisplayPort through Thunderbolt 5**. [Session data](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/SavedLogOutput/2026-10-06_08-41-32/PresentMon.csv:1), [display configuration](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/SavedLogOutput/2026-10-06_08-41-32/Godot.log:5).

**The reported 1.31 ms mean input latency is not physical mouse-to-pixel latency.** I checked the local PresentMon source at tag `v2.6.0`. Its relevant measurements work as follows:

- **Input start:** Win32K's `InputDeviceRead_Stop` timestamp, followed by input-message retrieval and association with the process's next present. If several inputs arrive, the implementation can replace the pending timestamp with the **newest** one. It does not time physical actuation, wireless transport, or controller-axis consumption. [Input association](C:/Users/k/Repository/External/PresentMon_2-6-0/PresentData/PresentMonTraceConsumer.cpp:1781).
- **Input endpoint:** `MsAllInputToPhotonLatency` and `MsClickToPhotonLatency` subtract the associated input timestamp from `screenTime`. Missing associations produce `NA`; pending inputs from undisplayed frames can carry forward. Despite the names, there is no photodiode measurement or verification that the input changed a visible object. [Latency calculation](C:/Users/k/Repository/External/PresentMon_2-6-0/IntelPresentMon/CommonUtilities/mc/MetricsCalculatorInput.cpp:59).
- **Display endpoint:** `screenTime` comes from presentation/display ETW events. One tearing-capable path uses an MMIO-flip timestamp plus NVIDIA's driver-reported flip delay. That is not the moment a particular LCD pixel finishes changing. NVIDIA's processing also clamps proposed flip timestamps; repeated derived times alone cannot establish visible stutter. [Display tracking](C:/Users/k/Repository/External/PresentMon_2-6-0/PresentData/PresentMonTraceConsumer.cpp:1080), [NVIDIA adjustment](C:/Users/k/Repository/External/PresentMon_2-6-0/PresentData/NvidiaTraceConsumer.cpp:16).

Your mean `MsUntilDisplayed` is approximately **0.60 ms**, measuring Present start to that display endpoint. It omits earlier input/game work and physical panel response. With tearing allowed, multiple flips can occur within one 120 Hz scan; their sub-millisecond spacing does not mean the panel delivers 1,248 complete refreshes per second. [Metric definition](C:/Users/k/Repository/External/PresentMon_2-6-0/IntelPresentMon/CommonUtilities/mc/MetricsCalculatorDisplay.cpp:14).

Also, `MsCPUWait` measures time inside Present, `MsGPULatency` starts at a derived CPU-frame boundary, and `MsGPUTime` includes gaps between GPU start and completion. `MsCPUBusy` is elapsed time between that CPU boundary and Present, not a profiler's count of active CPU execution. These are overlapping timeline measurements, not independent delays to add together. [CPU/GPU calculations](C:/Users/k/Repository/External/PresentMon_2-6-0/IntelPresentMon/CommonUtilities/mc/MetricsCalculatorCpuGpu.cpp:14).

Because no controller was used, while the driving code reads controller axes, this run establishes rendering headroom rather than steering or braking latency. The game samples those axes and updates the car in its render-frame process, so higher actual frame rates can improve control freshness; there is no separate 60 Hz car-update ceiling in this code. [Driving inputs](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Source/GameDataAndLogic/Input/InputManager.cs:48), [frame processing](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Source/Main.cs:67).

For monitors, refresh rate determines how frequently the screen can update, while pixel response and processing determine how promptly and cleanly those updates become visible.

| Monitor tier / option            | Refresh interval | Experience in this game                                                         |
| -------------------------------- | ---------------- | ------------------------------------------------------------------------------- |
| Budget 60 Hz                     | 16.67 ms         | Lowest temporal detail; typically substantial blur and delayed visual feedback. |
| Budget 100-120 Hz                | 10.00-8.33 ms    | Large improvement over 60 Hz; pixel response and overdrive vary between models. |
| ASUS PA278QGV, 120 Hz            | 8.33 ms          | Sharp 1440p image and good color; a useful work/game compromise.                |
| Gaming 144-180 Hz                | 6.94-5.56 ms     | Modest timing upgrade from 120 Hz; a well-tuned panel can improve clarity more. |
| Gaming 240 Hz                    | 4.17 ms          | Strong middle tier: much finer motion than 120 Hz without extreme requirements. |
| Gaming 360 Hz                    | 2.78 ms          | Further improvement in tracking moving objects and visual feedback.             |
| 480 Hz OLED, e.g. ASUS PG27AQDP  | 2.08 ms          | Excellent motion clarity, fast pixel transitions, and strong contrast.          |
| Acer XV273U F5, 540 Hz at 1440p  | 1.85 ms          | Very high temporal resolution while preserving track and car detail.            |
| Acer XV273U F5, 1,000 Hz at 720p | 1.00 ms          | Highest refresh tier here, with a substantial spatial-resolution sacrifice.     |

ASUS specifies the PA278QGV as 1440p, 120 Hz, IPS, with **5 ms GtG** response. That 5 ms is a transition specification, not an input-lag measurement. A cheaper gaming monitor at the same 120 Hz could have similar or better responsiveness; ProArt's strengths do not automatically translate into lower gaming latency. An original review also found its default motion clarity comparatively weak. [ASUS specifications](https://www.asus.com/uk/displays-desktops/monitors/proart/proart-display-pa278qv-gen2-pa278qgv/techspec/), [PA278QGV testing](https://www.itpro.com/hardware/monitors/asus-proart-pa278qgv-review-color-accurate-well-connected-and-affordable).

The Acer's modes are **2560 x 1440 at 540 Hz** and **1280 x 720 at 1,000 Hz**: the latter has one quarter as many pixels. For your track edges, car outlines, and small details, that is a meaningful tradeoff. I would favor **540 Hz at 1440p for overall quality**, and regard 1,000 Hz as an option for prioritizing temporal detail. Acer's advertised minimum GtG response does not establish how all pixel transitions perform within a 1 ms refresh. [Acer specifications](https://news.acer.com/acers-new-predator-and-nitro-monitors-bring-gaming-experiences-to-life).

There is evidence that the Acer's fastest mode helps in practice: Tom's Hardware measured **14.4 ms at 540 Hz versus 11.1 ms at 1,000 Hz** in its latency test and praised its overdrive. Those are results from that test setup, not a monitor-only offset to add to your CSV. The measured 3.3 ms difference includes effects beyond the simple refresh-period calculation. A **480 Hz OLED** remains a serious alternative because pixel transitions and contrast can outweigh the Acer's small refresh advantage at 540 Hz. [Acer measurements](https://www.tomshardware.com/monitors/gaming-monitors/acer-nitro-xv273u-f5-27-inch-qhd-540-hz-gaming-monitor-review/2), [480 Hz OLED specification](https://rog.asus.com/us/monitors/27-to-31-5-inches/rog-swift-oled-pg27aqdp/).

For controllers, **stick latency is the more relevant comparison for your acceleration control**. These are selected Gamepadla hardware-test averages, measured to the host, before game sampling, rendering, and monitor delay.

| Controller / connection                  | Button average | Stick average | Tested firmware | Interpretation                                   |
| ---------------------------------------- | -------------- | ------------- | --------------- | ------------------------------------------------ |
| Xbox Series, USB                         | 6.50 ms        | 8.30 ms       | 5.23.6.0        | Comfortable baseline; relatively slow reporting. |
| Xbox Series, official wireless dongle    | 7.21 ms        | 8.88 ms       | 5.23.6.0        | Similar tier to USB in these tests.              |
| Xbox Series, Bluetooth                   | 12.23 ms       | 12.98 ms      | 5.23.6.0        | Slower and more variable.                        |
| Standard DualSense, stock USB            | 8.27-11.27 ms  | 7.41-8.19 ms  | 0630            | Stock USB is not its fastest tested mode.        |
| Standard DualSense, Bluetooth            | 5.96 ms        | 5.01 ms       | 0630            | Faster average here, but a longer latency tail.  |
| GameSir G7 SE, USB at 1 kHz              | 3.00 ms        | 3.23 ms       | 6.6.4           | Useful budget step up.                           |
| Razer Wolverine V3 Pro 8K PC, USB        | 1.87-2.04 ms   | 2.33-3.04 ms  | 1.1.0.1         | Fast; results differ between tests and APIs.     |
| Razer Wolverine V3 Pro 8K PC, dongle     | 1.87-1.99 ms   | 2.75-3.49 ms  | 1.1.0.1         | Low wireless delay in the selected tests.        |
| GameSir G7 Pro 8K, USB                   | 1.36 ms        | 2.46 ms       | 1.9.7           | Provisional user results; firmware matters.      |
| GameSir G7 Pro 8K, dongle                | 3.36 ms        | 10.03 ms      | 1.9.7           | High polling did not ensure fast stick response. |
| ASUS ROG Raikiri II Pro PC, USB          | 0.92 ms        | 0.89 ms       | 0.03.00.34      | Among the fastest tested implementations.        |
| ASUS ROG Raikiri II Pro PC, dongle       | 1.09 ms        | 1.70 ms       | 0.03.00.34      | Strong wireless result.                          |
| Void GENESIS, USB                        | 1.01 ms        | 0.50 ms       | 3.03            | Among the fastest tested stick implementations.  |
| Void GENESIS, dongle                     | 2.14 ms        | 1.70 ms       | 3.03            | Strong wireless result.                          |
| GameSir Tarantula 8K PC, USB, raw sticks | 1.04 ms        | 0.49 ms       | 2.2.9           | Among the fastest tested stick implementations.  |

Sources: [Xbox Series](https://gamepadla.com/xbox-core-controller.html), [DualSense](https://gamepadla.com/sony-dualsense.html), [G7 SE](https://gamepadla.com/gamesir-g7-se.html), [Wolverine 8K PC](https://gamepadla.com/razer-wolverine-v3-pro-8k-pc.html), [G7 Pro 8K](https://gamepadla.com/g7-pro-8k.html), [Raikiri II Pro PC](https://gamepadla.com/raikiri-ii-pro-pc.html), [GENESIS](https://gamepadla.com/void-genesis.html), [Tarantula 8K](https://gamepadla.com/gamesir-tarantula-8k.html).

**Treat these as tested configurations, not permanent rankings.** Some results are user submissions, the G7 Pro page lacks a verified reference, and the Raikiri measurements use Windows 10 rather than your Windows 11 platform. Input API also matters: the selected Raikiri tests use XInput over USB and DInput over dongle; Wolverine's USB stick range includes both APIs. Stick tests use a near-full-deflection threshold, so they do not establish the latency of every small correction or trigger movement. Differences such as 0.49 versus 0.50 ms are not a meaningful buying distinction.

The implications for the named controllers are:

- **Tarantula and GENESIS:** strongest wired stick-response candidates in these tests. Tarantula is wired and lacks vibration, which may reduce immersion if the game uses rumble. [GameSir FAQ](https://gamesir.com/pages/faq-tarantula-pro-8k-pc).
- **Raikiri II Pro PC:** a strong candidate when you want very low delay with wireless flexibility. Compare the exact PC Pro model; similarly named Raikiri models differ.
- **Wolverine V3 Pro 8K PC:** fast enough that fit, button access, and stick feel may matter more than its remaining roughly 2-3 ms disadvantage to the fastest wired results. The cited latency tests use older firmware.
- **G7 Pro 8K:** USB looks promising; the cited dongle result warrants testing your exact firmware rather than assuming that an 8 kHz label guarantees sub-millisecond analog response.
- **Xbox Series and DualSense:** perfectly usable, but leave several milliseconds available to recover. DualSense also illustrates why wireless cannot universally be assumed slower than stock USB. USB overclocking results should be considered separately from a standard controller.

For the requested FPS caps, a useful comparison model is:

`End-to-end delay ~= controller delay + 500/FPS + processing/render/queue delay + 500/Hz + monitor processing/pixel response`

The two half-period terms approximate average game-sampling and display-update delay with evenly distributed arrival phases, fixed refresh, immediate presentation, and no substantial frame queue. They are **comparison estimates**, not guaranteed optical latencies. VSync, VRR, limiter placement, and buffering can change them. Do not add another polling term to the measured controller values, which already include reporting delay.

The following table contains **only those two cadence terms**, in milliseconds. Controller, rendering, queue, and pixel delays still need to be added.

| Game FPS | 60 Hz | 120 Hz | 240 Hz | 480 Hz | 540 Hz | 1,000 Hz |
| -------- | ----- | ------ | ------ | ------ | ------ | -------- |
| 60       | 16.67 | 12.50  | 10.42  | 9.38   | 9.26   | 8.83     |
| 120      | 12.50 | 8.33   | 6.25   | 5.21   | 5.09   | 4.67     |
| 240      | 10.42 | 6.25   | 4.17   | 3.13   | 3.01   | 2.58     |
| 480      | 9.38  | 5.21   | 3.13   | 2.08   | 1.97   | 1.54     |
| 960      | 8.85  | 4.69   | 2.60   | 1.56   | 1.45   | 1.02     |

This explains several practical outcomes:

- **60 FPS:** the game samples driving input only every 16.67 ms. A fast controller helps, but a 1,000 Hz display cannot create 1,000 distinct motion states from 60 rendered frames.
- **120 FPS:** a substantial improvement; a 120 Hz display can refresh at the game's frame rate, though VSync-off presentation can still tear. Faster displays still help timing and potentially pixel clarity.
- **240 FPS:** a strong middle ground. On your PA278QGV, the extra FPS improves input freshness, while the screen's full refresh cadence remains 120 Hz.
- **480 FPS:** a strong premium target, especially with a 480-540 Hz display. Most of the frame-sampling improvement available up to 960 FPS has already been achieved.
- **960 FPS:** useful for pursuing minimum delay and feeding very fast displays. Its additional average sampling benefit over 480 FPS is approximately 0.52 ms.

For example, at **960 FPS on a 120 Hz monitor**, the cadence contribution is about **4.69 ms**. Adding the tested wired Xbox stick delay gives **12.99 ms**; adding Tarantula's gives **5.18 ms**, before the remaining software and display delays. Moving that fast controller to 540 Hz reduces the cadence contribution by another **3.24 ms**. Thus, changing both peripherals can remove about **11 ms in this model**, even while FPS stays unchanged.

There is also a quality distinction: **high FPS on a slow monitor improves responsiveness without providing the motion clarity of a fast monitor**. With tearing allowed, a refresh can contain portions of multiple game frames. Conversely, 1,000 Hz with a 480 FPS cap repeats motion states; it cannot deliver the temporal detail of 960 FPS. For a clean image, a suitable VRR configuration below the display ceiling is attractive, but it requires a separate timing comparison from this tearing-enabled capture.

Your existing mouse is already in a high-performance tier. Changing its reporting from **1 to 8 kHz** reduces the ideal average report-phase wait from **0.500 to 0.0625 ms**, a gain of **0.4375 ms**. That is considerably smaller than moving from your monitor to 240-540 Hz. Actual gains also depend on sensor and firmware behavior; higher polling does not make total latency eight times lower. Razer confirms the mouse's 8 kHz capability. [DeathAdder V4 Pro](https://www.razer.com/gaming-mice/razer-deathadder-v4-pro).

I found no reliable actuation-to-host benchmark for your exact **Advantage360 SmartSet**, so I would not assign it an invented millisecond figure. Wired connectivity alone does not establish scan/debounce latency. Its ergonomics may be valuable, but replacing it has little bearing on controller-based driving with the current mappings. [Kinesis model information](https://kinesis-ergo.com/shop/adv360/).

**The Thunderbolt connection is not itself evidence of a frame of added latency.** Thunderbolt 5 supports high-bandwidth DisplayPort video. What remains unknown is the adapter/dock path and whether scanout passes through an integrated GPU. The log identifies the rendering GPU, not that complete physical path. There is no evidence here that replacing the cable would outperform a monitor upgrade. [Intel Thunderbolt information](https://www.intel.com/content/www/us/en/architecture-and-technology/thunderbolt/overview.html).

For your game specifically, controller quality also includes **deadzones, response curves, filtering, centering, and usable analog resolution**. The current code applies **10% lateral and 5% longitudinal inner deadzones in car coordinates**. Crossing those thresholds with a slowly moving thumb can affect perceived response more than a sub-millisecond polling improvement. TMR sensing does not by itself guarantee low filtering delay or perfect centering. Since braking is analog, short digital trigger modes can also sacrifice gradual brake control. [Current deadzones](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Source/GameDataAndLogic/Car/CarStateManager.cs:40).

**My practical choices would be:**

- **Budget:** keep your PA278QGV, or choose a decent 120 Hz gaming monitor, and use a tested 1 kHz wired controller such as G7 SE, with a 240 FPS cap.
- **Balanced:** a good 240 Hz gaming display and a controller with measured low analog delay, with a 480 FPS cap.
- **Premium overall quality:** a 480 Hz OLED or the Acer at 540 Hz/1440p, with a comfortable fast controller and a 480 or 960 FPS cap.
- **Maximum temporal performance among these options:** Acer at 1,000 Hz/720p, 960 FPS, and a fast wired controller such as Tarantula or GENESIS, accepting the lower spatial detail.

For your current **PA278QGV + DeathAdder**, I would prioritize the **monitor** over replacing the mouse. For future controller play, I would prioritize a verified low-latency analog implementation and comfortable controls before paying specifically for an 8 kHz badge.

Finally, **960 FPS looks feasible on average, but this capture does not prove a reliable 960 FPS lock**: about **1.71%** of its steady-state present intervals exceed the 1.042 ms budget, versus approximately **0.004%** exceeding the 2.083 ms budget for 480 FPS. Those are uncapped observations, not predictions of capped pacing, and actual driving should be tested. Keep Ultra enabled as part of the comparison baseline; this single run cannot quantify its benefit or prove a zero-length queue. The decisive follow-up is physical controller-to-visible-response measurement during gameplay at each cap, with the monitor kept at its intended refresh mode.

Conversation recorded and committed in [PeripheralLatencyAnalysis.md](C:/Users/k/Repository/Veehiicuul/Conversations/2026-10-06_16-53_UTC_PeripheralLatencyAnalysis.md).
