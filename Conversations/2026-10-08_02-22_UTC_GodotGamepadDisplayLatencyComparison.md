# Godot gamepad display latency comparison

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

is there a way to measure input to display latency for gamepad input in "C:\Users\k\Repository\Veehiicuul\Veehiicuul_Godot_CSharp" that is comparable to how "C:\Users\k\Repository\Veehiicuul\InputLatency_CppDx12" measures this

## Assistant Commentary

I'll compare how the two applications handle input and rendering, then identify which latency measurements the Godot version can reproduce.

## Assistant Commentary

The C++ app measures a GameInput reading's timestamp through to a matched Windows display event. The Godot game currently polls Godot's input state and captures PresentMon data, but it doesn't link the input used by each frame to that frame's display event. That link is the main missing piece.

## Assistant Commentary

A matching measurement is feasible if the Godot frame uses the same timestamped GameInput reading being measured. For the current Godot input path, we can measure from `_Input` receipt, but that starts later and excludes input collection delay. The closest comparison would combine GameInput integration with instrumentation at Godot's DXGI `Present` call and reuse the existing display tracker.

## Assistant Final

Yes. **We can reproduce the C++ app's reading-to-display measurement in the Godot game, but it needs additional instrumentation.** The existing PresentMon capture is insufficient by itself.

The C++ app measures from a specific GameInput reading's timestamp to the Windows display event for the first displayed frame that visualizes that reading. It records exactly which reading each frame used and matches the frame to its DXGI presentation. See its [measurement description](C:/Users/k/Repository/Veehiicuul/InputLatency_CppDx12/ReadMe.md:71).

Your Godot game's [InputManager](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Source/GameDataAndLogic/Input/InputManager.cs:38) reads axis values and button states without retaining the original reading's timestamp or identity. Godot's public [InputEvent API](https://docs.godotengine.org/en/stable/classes/class_inputevent.html) exposes no acquisition timestamp. PresentMon's automatic input latency metrics cover keyboard and mouse interactions, so they don't supply the missing gamepad measurement. [PresentMon documentation](https://github.com/GameTechDev/PresentMon/blob/v2.6.0/README-ConsoleApplication.md)

There are two useful approaches:

- **Measure the existing Godot input path.** Timestamp gamepad events when `_Input` receives them, track which events contribute to each rendered frame, and correlate those frames with PresentMon display events. This gives **Godot-event-receipt-to-display latency**. It excludes input collection and delivery before `_Input`, so it cannot be compared directly with the C++ app's current starting point. Instrumenting Godot's existing SDL input backend would let us include more of that earlier delay.
- **Use the same starting point as the C++ app.** Integrate GameInput v3 into C# through a native helper, and make the measured reading actually drive the game's input or an immediate visual indicator. Preserve its timestamp and identity through rendering, then reuse the C++ display tracker and clock calibration. This gives the same measurement definition, but changes the game's gamepad acquisition path.

For either approach, the important rendering work is reliably linking an input-bearing frame to its actual DXGI `Present`. A small measurement patch around [Godot's Present call](C:/Users/k/Repository/External/Godot_4-7-2/drivers/d3d12/rendering_device_driver_d3d12.cpp:2506) would provide the same precise correlation information the C++ app records. Discarded frames and repeated readings need the same handling as the C++ implementation.

`RenderingServer.FramePostDraw` alone cannot serve as the display endpoint: it reports completion of the rendering server's viewport updates, not when Windows displays the frame. [Godot documentation](https://docs.godotengine.org/en/stable/classes/class_renderingserver.html#class-renderingserver-signal-frame-post-draw)

For comparing Godot's rendering latency against your custom renderer, I would use **GameInput plus an immediate input indicator rendered inside the running game**. For investigating the latency of your current Godot input pipeline, I would preserve that pipeline and instrument it. Both remain software measurements; physical button-to-photon latency requires external input and optical instrumentation.

