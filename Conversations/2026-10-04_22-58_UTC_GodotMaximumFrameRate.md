# Godot maximum frame rate conversation

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

</INSTRUCTIONS>
<environment_context>
  <cwd>C:\Users\k\Repository\Veehiicuul</cwd>
  <shell>powershell</shell>
  <current_date>2026-10-04</current_date>
  <timezone>America/Los_Angeles</timezone>
  <filesystem><workspace_roots><root>C:\Users\k\Repository\Veehiicuul</root><root>C:\Users\k\.codex\visualizations\2026\10\04\01a1091f-efe9-7813-9bfb-e98562dda38d</root></workspace_roots><permission_profile type="managed"><file_system type="restricted"><entry access="read"><special>:root</special></entry><entry access="write"><path>C:\Users\k\Repository\Veehiicuul</path></entry><entry access="write"><path>C:\Users\k\.codex\visualizations\2026\10\04\01a1091f-efe9-7813-9bfb-e98562dda38d</path></entry><entry access="write"><special>:slash_tmp</special></entry><entry access="write"><special>:tmpdir</special></entry><entry access="read"><path>C:\Users\k\Repository\Veehiicuul\.git</path></entry><entry access="read"><path>C:\Users\k\.codex\visualizations\2026\10\04\01a1091f-efe9-7813-9bfb-e98562dda38d\.git</path></entry><entry access="read"><path>C:\Users\k\Repository\Veehiicuul\.agents</path></entry><entry access="read"><path>C:\Users\k\.codex\visualizations\2026\10\04\01a1091f-efe9-7813-9bfb-e98562dda38d\.agents</path></entry><entry access="read"><path>C:\Users\k\Repository\Veehiicuul\.codex</path></entry><entry access="read"><path>C:\Users\k\.codex\visualizations\2026\10\04\01a1091f-efe9-7813-9bfb-e98562dda38d\.codex</path></entry><entry access="read"><path>C:\Users\k\Repository\Veehiicuul\.aws</path></entry><entry access="read"><path>C:\Users\k\.codex\visualizations\2026\10\04\01a1091f-efe9-7813-9bfb-e98562dda38d\.aws</path></entry></file_system></permission_profile></filesystem>
</environment_context>

## User

<external_codex_apps_open_page>{"page_id":null}</external_codex_apps_open_page>

## User

currently, for situations when I don't want to use vrr/gsync due to issues specific to vrr/gsync, the ideal setup for this game is this:
- nvcp low latency mode = ultra
- nvcp max frame rate = off, or on and set to the desired fps
- no vsync of any kind

interestingly, setting llm=ultra and mfr=off seems to impose an exact fps cap of 1250.

i'm curious about what it would be like to use godot's provided `application/run/max_fps`. benefits of this are that it can be adjusted at game runtime and it isn't nvidia-specific.

analyze the implementation of max_fps using the godot 4.7.2 source code at "C:\Users\k\Repository\External\Godot_4-7-2" and the game source code at "C:\Users\k\Repository\Veehiicuul\Veehiicuul_Godot_CSharp" and tell me pros, cons, and things i should know about using max_fps.


## Assistant

I'll trace Godot's frame limiter and compare it with this game's rendering and input loop, focusing on frame pacing, latency, and runtime control.

## Assistant

The Windows limiter is more precise than a simple sleep: it sleeps for the coarse portion, then busy-waits to a deadline. That matters at your frame rates, because a high cap may spend all its waiting time spinning on a CPU core. Your game also updates input, movement, and collision once per rendered frame, so the cap affects those update rates too.

## Assistant

The D3D12 code confirms that `max_fps` controls CPU frame timing; it does not change VSync, swapchain buffering, or the GPU queue policy. I also found a pacing tradeoff: the limiter can follow a late frame with a shorter frame to catch up, so a stable average FPS does not guarantee evenly spaced frames.

## Assistant

The main tradeoff is clear: Godot can provide the runtime-adjustable cap, but it does not reproduce all of LLM Ultra's behavior. Two details matter for testing: caps around 500-1250 FPS normally get no sleeping benefit from this limiter, and active accessibility support selects a different wait path that may not enforce the cap reliably. I'm checking the runtime controls and recording the analysis in the required conversation log.

## Assistant

**Godot's `max_fps` looks worth trying as a replacement for NVCP's Max Frame Rate, initially keeping LLM Ultra enabled.** Its placement is sensible for latency, and it gives you runtime control across GPU vendors. The biggest caveats are substantial CPU busy-waiting at high caps, imperfect frame pacing after a hitch, and the fact that your gameplay update rate follows the cap.

I inspected your local Godot 4.7.2 source at commit `ed1daf0b` and the current C# game source. The timing examples below are calculated from the implementation; I did not benchmark the game.

**How it works:** Godot reads `application/run/max_fps` into `Engine.MaxFps` during startup. On Windows, the normal game loop processes input events, runs game logic, draws/submits the frame, and then applies the limiter before the next iteration. This places the intentional wait before the next frame's input sampling. [Startup setting](C:/Users/k/Repository/External/Godot_4-7-2/main/main.cpp:2255), [Windows loop](C:/Users/k/Repository/External/Godot_4-7-2/platform/windows/os_windows.cpp:2349), [end-of-frame limiter call](C:/Users/k/Repository/External/Godot_4-7-2/main/main.cpp:5174).

The Windows limiter maintains a running deadline, advances it by `1,000,000 / max_fps` microseconds each frame, sleeps for a coarse portion of the remaining time, then busy-waits using `QueryPerformanceCounter` and `YieldProcessor()`. If the frame has already overrun its deadline, it skips the wait. [Limiter implementation](C:/Users/k/Repository/External/Godot_4-7-2/platform/windows/os_windows.cpp:2635).

The benefits are:

- **Runtime control without an NVIDIA dependency.** An options menu can change the cap immediately, including turning it off.
- **Good placement for input freshness.** The previous frame has already been submitted when the limiter waits. The next frame samples input after that wait.
- **Potentially precise pacing when the machine has headroom.** Busy-waiting handles the final portion that Windows' ordinary sleep cannot accurately target. The running deadline also avoids simply adding a full frame interval to the time already spent doing work.
- **Reduced rendering work and potentially lower GPU queueing latency.** A sustainable cap can keep the GPU from staying saturated. The latency benefit depends on the actual bottleneck and your existing LLM behavior; the source alone cannot establish an improvement over your current setup.

Several details matter for your game.

**1. At high FPS, this can behave almost entirely as a busy-wait limiter.**

Godot requests Windows' minimum supported timer period and normally uses a 1 ms sleep resolution. It reserves one resolution interval, then rounds the sleep portion down. Consequently, it needs approximately **2 ms remaining before the deadline** to issue even a 1 ms sleep. [Timer initialization](C:/Users/k/Repository/External/Godot_4-7-2/platform/windows/os_windows.cpp:295), [sleep calculation](C:/Users/k/Repository/External/Godot_4-7-2/platform/windows/os_windows.cpp:2651).

With that 1 ms resolution:

| Cap      | Target interval | Practical implication for the limiter                       |
| -------- | --------------- | ----------------------------------------------------------- |
| 240 FPS  | 4.166 ms        | Can sleep if enough time remains after the frame's work.     |
| 500 FPS  | 2.000 ms        | Normally no sleep once the frame has done any work.          |
| 1000 FPS | 1.000 ms        | Remaining limiter time is spent busy-waiting.                |
| 1250 FPS | 0.800 ms        | Remaining limiter time is spent busy-waiting.                |

For example, a 1250 FPS cap with 0.3 ms of CPU frame work could spend roughly another 0.5 ms spinning. `YieldProcessor()` is a processor spin-loop hint, not a Windows thread sleep.

Therefore, **a high Godot cap can reduce GPU work without producing a corresponding reduction in reported CPU utilization**. It can keep approximately one logical processor occupied between work and spinning, unless other waits intervene. Your launchers also use High process priority, which is relevant when evaluating CPU contention. [Launcher priority](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/MyRun.ps1:91).

**2. It targets a cadence, but does not enforce a minimum duration for every frame.**

The deadline is accumulated across frames. After an overrun, Godot may shorten subsequent intervals to catch up; it clamps accumulated lateness to roughly one target interval. In an idealized calculation at 240 FPS, a 6.000 ms interval can be followed by a 2.332 ms interval, then return to 4.166 ms. That preserves the schedule while producing a visibly uneven pair.

Also, the limiter controls when the next CPU iteration starts. Variation in the next frame's CPU work, GPU work, and presentation can still make actual displayed-frame timing uneven. Windows can schedule a sleeping thread later than requested, and a spinning thread can still be preempted. [Deadline correction](C:/Users/k/Repository/External/Godot_4-7-2/platform/windows/os_windows.cpp:2673), [Windows sleep semantics](https://learn.microsoft.com/en-us/windows/win32/api/synchapi/nf-synchapi-sleep).

The interval uses integer microseconds: 240 FPS becomes 4166 microseconds, corresponding to about 240.038 FPS under ideal conditions. 1250 FPS divides exactly into 800 microseconds. These are small quantization effects, but an apparently exact requested FPS is not a strict per-frame ceiling.

**3. In this game, the cap also controls input, movement, camera, and collision update frequency.**

Your `_Process` callback runs `Main.Process(delta)`. That reads input, tests collision, updates velocity and position, and applies the car/camera transforms. There is no independent fixed-rate vehicle simulation. Disabling accumulated input does not make those gameplay updates run during the limiter's wait. [Frame callback](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Source/Main_GodotAdapter.cs:10), [game loop](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Source/Main.cs:67).

This means:

- Lower caps increase the wait until the next gameplay input sample. For uniformly timed input arrivals, that component averages approximately half a frame: **0.4 ms at 1250 FPS, 1 ms at 500 FPS, and 2.08 ms at 240 FPS**. Those numbers exclude device, rendering, and display latency.
- Movement uses `delta`, so lowering FPS does not proportionally slow the game. However, velocity-then-position integration and changing acceleration directions produce somewhat different trajectories with different step sizes. [Movement integration](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Source/GameDataAndLogic/Car/CarStateManager.cs:81).
- Collision is checked at sampled poses before movement. Lower FPS increases distance traveled between checks and the chance of passing through a collider. Your collision code explicitly documents this accepted behavior. [Collision sampling](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Source/GameDataAndLogic/CollisionDetection/CollisionManager.cs:53).

These consequences also apply when an external limiter reduces the game's main-loop rate.

**4. It leaves presentation and queue management separate.**

Your project uses D3D12, VSync disabled, and two swapchain images. Godot's D3D12 backend presents with sync interval zero and allows tearing when supported. Changing `MaxFps` does not change that. Capping to the monitor's refresh rate, or a multiple of it, does not synchronize frames to scanout or guarantee tear-free, evenly paced motion. [Project configuration](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/project.godot:19), [D3D12 presentation settings](C:/Users/k/Repository/External/Godot_4-7-2/drivers/d3d12/rendering_device_driver_d3d12.cpp:2754).

Although `Engine.MaxFps` also forwards a hint to the rendering backend, D3D12 does not override that hook; it inherits a no-op. It does not dynamically change the frame queue or implement NVIDIA-style low-latency management. The CPU frame queue is a separate setting, defaulting to two in this version. [Backend hook](C:/Users/k/Repository/External/Godot_4-7-2/servers/rendering/rendering_device_driver.h:490), [frame queue](C:/Users/k/Repository/External/Godot_4-7-2/servers/rendering/rendering_device.cpp:8366).

Thus, replacing MFR with Godot's cap is a reasonable experiment; replacing the benefits you observe from LLM Ultra is a separate question. NVIDIA added Ultra Low Latency support for DX12 in driver 551.23, so DX12 alone is not grounds to dismiss your observation. [NVIDIA announcement](https://www.nvidia.com/en-au/geforce/news/geforce-rtx-4070-ti-super-rtx-video-hdr-game-ready-driver/).

**5. A few runtime details can change the result.**

Use the engine property on the main thread:

```csharp
Engine.MaxFps = 500;  // Apply a runtime cap.
Engine.MaxFps = 1250; // Values above 1000 are accepted.
Engine.MaxFps = 0;    // Remove Godot's cap.
```

The project setting is the startup default; changing it with `ProjectSettings.SetSetting(...)` alone does not update the running limiter. The inspector's 1000 FPS range is not a maximum enforced by `Engine.MaxFps`. The setter accepts positive integers and treats nonpositive values as uncapped. Save the user's preference separately if it should persist. [Engine setter](C:/Users/k/Repository/External/Godot_4-7-2/core/config/engine.cpp:87).

Other caveats:

- **Accessibility:** when `SceneTree.IsAccessibilityEnabled()` is true, Windows takes a different path using an event-interruptible, whole-millisecond wait. It bypasses the deadline/spin implementation. Above 1000 FPS, its calculated timeout rounds down to zero, so it cannot reliably enforce that high cap. The default accessibility mode activates when an assistive app is detected. [Alternate wait path](C:/Users/k/Repository/External/Godot_4-7-2/platform/windows/os_windows.cpp:2611), [activation condition](C:/Users/k/Repository/External/Godot_4-7-2/scene/main/scene_tree.cpp:213).
- **Low processor mode:** this uses a less precise sleep-only path and a separate minimum delay. It is disabled by default in your project.
- **Changing the cap:** the running deadline is retained, so allow a brief settling period when enabling or changing it.
- **Testing:** `--max-fps 500` can select the cap at launch without a code change. `--fixed-fps` is a different feature and bypasses this limiter.

For your first comparison, keep **LLM Ultra, VRR off, and all VSync off**, and compare NVCP MFR at a chosen rate with Godot at that same rate and MFR off. Choose a rate comfortably below the machine's sustained capacity and your observed 1250 FPS ceiling. Compare frame-time distributions, GPU busy time, CPU usage/power, and perceived responsiveness.

The limiter contains **no special 1250 FPS ceiling**, and your project has no configured Godot cap. Your existing startup log prints `Engine.MaxFps`, making that easy to verify. A value of zero alongside measured 1250 FPS rules out this Godot limiter as the source of that ceiling; the exact NVIDIA-side cause remains unproven by this source analysis. [Startup logging](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Source/Utility/PrintInfoUtility.cs:23).
