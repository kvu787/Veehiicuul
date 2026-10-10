# VSync latency measurement comparison

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

# PowerShell

Use modern PowerShell whose command should be `pwsh`, not legacy PowerShell.

## Git

When implementing stuff, avoid difficult-to-review "mega-commits".
When it makes sense, split large work into multiple commits to make it easier to review.
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

When creating an application, create a folder called `LogOutput` at the root of the application's folder in the git repo.
For each run of the application, a folder must be created in LogOutput and named with the current timestamp. This PowerShell code shows what the name of the folder should be:

```powershell
$logFolderPath = "$env:UserProfile\Repository\Godot\VsyncStutterTest\LogOutput\$(Get-Date -Format "yyyy-MM-dd_HH-mm-ss")"
New-Item -ItemType "Directory" -Path $logFolderPath
```

Any logs for that application session must be put in that log folder.
`LogOutput/` must be gitignored.

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

## User

<external_codex_apps_open_page>{"page_id":null}</external_codex_apps_open_page>

## User

with vsync on and running on a 120 hz monitor, the mean input latency is roughly one interval (8.33 ms) for "C:\Users\k\Repository\Veehiicuul\InputLatency_CppDx12" and roughly three intervals (25 ms) for "C:\Users\k\Repository\Veehiicuul\Veehiicuul_Godot_CSharp". explain why

## Assistant Commentary

I will compare how the two applications sample input, render, and present frames, then check the relevant engine behavior to explain the difference.

## Assistant Commentary

The C++ renderer explicitly limits presentation latency and samples fresh input after its waits. Godot uses a different frame pipeline. Both checked-in projects currently request VSync off, so I am treating your results as runs with VSync enabled externally or in a modified build.

## Assistant Question

How did you measure the two means, and how did you enable VSync (application setting or graphics driver override)? This will help distinguish actual input latency from PresentMon's other timing metrics.

## User

i used presentmon for veehiicuul and i checked the displayed mean for the dx12 input latency app. i enabled vsync in nvidia control panel.

## Assistant Commentary

That makes the comparison less certain: the C++ dashboard measures a specific GameInput reading through to its displayed frame, while PresentMon reports several metrics with different starting points. I will check the captured columns to see what the Godot number includes. The frame pipelines also differ in how much work they allow to queue.

## Assistant Question

Which PresentMon metric showed roughly 25 ms: DisplayLatency, MsAllInputToPhotonLatency, MsClickToPhotonLatency, or another column?

## User

i think it was display latency

## Assistant Commentary

If the Godot value was DisplayLatency, it starts at PresentMon's estimated CPU frame start, not at a gamepad reading. The C++ mean starts at the reading's timestamp. Godot also lacks the C++ renderer's explicit one-frame presentation limit, but these two means alone cannot tell us how much extra gamepad latency that adds.

## Assistant Final

**If the PresentMon value was DisplayLatency, you compared two different measurements. The numbers do not establish that Godot has three times the gamepad input latency.**

The starting points differ:

| Application / metric      | Measurement starts at                         | Measurement ends at           |
| ------------------------- | --------------------------------------------- | ----------------------------- |
| C++ dashboard mean        | Timestamp of the GameInput reading visualized | Matched Windows display event |
| PresentMon DisplayLatency | Estimated start of CPU work for the frame     | Windows display event         |

The C++ definition is documented in its [measurement description](C:/Users/k/Repository/Veehiicuul/InputLatency_CppDx12/ReadMe.md:71). PresentMon defines DisplayLatency as frame-start-to-display time; its input-to-display columns cover keyboard and mouse interactions, not gamepad readings. [PresentMon documentation](https://github.com/GameTechDev/PresentMon/blob/v2.6.0/README-ConsoleApplication.md#csv-columns)

**That difference matters especially because the C++ application samples input late.** Its sequence is:

1. Wait for the previous GPU work and for DXGI to admit another frame.
2. Poll the latest GameInput reading.
3. Draw the small dashboard and present it.

It creates a waitable swap chain, calls `SetMaximumFrameLatency(1)`, and explicitly waits before sampling. See [swap-chain configuration](C:/Users/k/Repository/Veehiicuul/InputLatency_CppDx12/Source/Renderer.cpp:156), [readiness waits](C:/Users/k/Repository/Veehiicuul/InputLatency_CppDx12/Source/Renderer.cpp:286), and [late input sampling](C:/Users/k/Repository/Veehiicuul/InputLatency_CppDx12/Source/Renderer.cpp:541).

PresentMon 2.6 estimates CPU frame start from the end of the preceding Present call. Consequently, its DisplayLatency can include the C++ application's readiness wait, even though the input eventually displayed is acquired afterward. **The C++ app could therefore report a larger PresentMon DisplayLatency than its own reading-to-display mean for the same frames.** See [PresentMon's calculation](C:/Users/k/Repository/External/PresentMon_2-6-0/IntelPresentMon/CommonUtilities/mc/MetricsCalculator.cpp:332).

There is also a real architectural reason Godot can accumulate more latency. The inspected Godot DirectX 12 backend uses ordinary [DXGI presentation](C:/Users/k/Repository/External/Godot_4-7-2/drivers/d3d12/rendering_device_driver_d3d12.cpp:2504), without the C++ application's waitable swap chain or explicit maximum presentation latency of one. Godot defaults to two GPU frame contexts. Your `swapchain_image_count=2` controls image buffers; it does **not** impose the same presentation limit. GPU completion and display completion are separate milestones. [Godot settings documentation](https://docs.godotengine.org/en/stable/classes/class_projectsettings.html#class-projectsettings-property-rendering-rendering-device-vsync-frame-queue-size)

With VSync, queued frames wait for refresh opportunities. At 120 Hz, each additional refresh interval costs 8.33 ms, so buffering can add substantial latency while maintaining a steady 120 FPS. Microsoft describes how ordinary Present blocking can leave frames waiting an extra interval, and why waiting before processing fresh input reduces that delay. [Microsoft's explanation](https://learn.microsoft.com/en-us/windows/uwp/gaming/reduce-latency-with-dxgi-1-3-swap-chains)

Forcing VSync in NVIDIA Control Panel synchronizes presentation, but it does not make the applications' queue limits or input-sampling timing identical.

**Your 25 ms value is likely a frame-pipeline latency measurement; the 8.33 ms value is a reading-to-display measurement.** More buffering in Godot is plausible, but attributing the difference to exactly two extra input-latency frames would require matching measurements. First capture both applications with PresentMon and compare the same DisplayLatency column. Measuring Godot's gamepad reading-to-display latency requires additional input/frame instrumentation.

