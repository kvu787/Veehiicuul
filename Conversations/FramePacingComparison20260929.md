# Frame Pacing Comparison

## user

# AGENTS.md instructions for C:\Users\k\Repository\Veehiicuul

<INSTRUCTIONS>
# Base template

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
  <current_date>2026-09-29</current_date>
  <timezone>America/Los_Angeles</timezone>
  <filesystem><workspace_roots><root>C:\Users\k\Repository\Veehiicuul</root><root>C:\Users\k\.codex\visualizations\2026\09\29\01a0ef9b-f1ca-76e2-b61a-7750d9757b43</root></workspace_roots><permission_profile type="managed"><file_system type="restricted"><entry access="read"><special>:root</special></entry><entry access="write"><path>C:\Users\k\Repository\Veehiicuul</path></entry><entry access="write"><path>C:\Users\k\.codex\visualizations\2026\09\29\01a0ef9b-f1ca-76e2-b61a-7750d9757b43</path></entry><entry access="write"><special>:slash_tmp</special></entry><entry access="write"><special>:tmpdir</special></entry><entry access="read"><path>C:\Users\k\Repository\Veehiicuul\.git</path></entry><entry access="read"><path>C:\Users\k\.codex\visualizations\2026\09\29\01a0ef9b-f1ca-76e2-b61a-7750d9757b43\.git</path></entry><entry access="read"><path>C:\Users\k\Repository\Veehiicuul\.agents</path></entry><entry access="read"><path>C:\Users\k\.codex\visualizations\2026\09\29\01a0ef9b-f1ca-76e2-b61a-7750d9757b43\.agents</path></entry><entry access="read"><path>C:\Users\k\Repository\Veehiicuul\.codex</path></entry><entry access="read"><path>C:\Users\k\.codex\visualizations\2026\09\29\01a0ef9b-f1ca-76e2-b61a-7750d9757b43\.codex</path></entry></file_system></permission_profile></filesystem>
</environment_context>

## user

compare C:\Users\k\Repository\Veehiicuul\Veehiicuul_Godot_CSharp with C:\Users\k\Repository\VsyncStutterTest and check to see if there are any differences that could cause stutter/jitters or could cause non-trivial differences in fps, frame pacing, input latency, etc


## assistant

I’ll compare the rendering settings, frame loop, input handling, and build configuration, then identify differences that could affect smoothness or latency.


## assistant

The core presentation settings match: DirectX 12, Forward+, exclusive fullscreen, VSync off, and two swapchain images. I found differences in render resolution, input accumulation, and per-frame work; I’m checking which could explain ongoing jitter versus pauses during loading or switching.


## assistant

The recent logs confirm an important mismatch: VsyncStutterTest runs Godot 4.6.3, while Veehiicuul runs 4.7.2 .NET. Veehiicuul’s log also confirms native 2560×1440 rendering, so its viewport stretch setting is not reducing resolution in that session. I found a definite pause source when switching tracks, but no fixed-physics/render mismatch or one-frame camera-follow lag in the normal update order.


## assistant

Yes. **There are meaningful differences, but no obvious mismatch in the basic VSync/swapchain configuration.** I compared the current source, launch/build settings, scenes, and recent logs. This was a static review, not a new frame-time benchmark.

The main findings, in priority order:

1. **Different engine versions are a major comparison variable.** Recent logs show **VsyncStutterTest on Godot 4.6.3** and **Veehiicuul on Godot 4.7.2 .NET**, both using D3D12 Forward+ on the RTX 5090 Laptop GPU. Rendering, presentation, and runtime differences could affect FPS and pacing; the logs alone do not establish a regression. Matching engine versions is the first useful control. [Test log](C:/Users/k/Repository/VsyncStutterTest/MyLogOutput/2026-09-29_16-51-03/Godot.log), [Veehiicuul log](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/MyLogOutput/2026-09-29_16-57-03/Godot.log).

2. **The rendering workloads are substantially different.** VsyncStutterTest draws a sphere, ground plane, and box. Veehiicuul imports a track containing **132 meshes, 161 mesh primitives, and 28 materials**—asset totals, not simultaneous draw-call counts. More visible geometry, surfaces, and draw submissions can produce substantial FPS differences. Camera movement also changes the visible workload. Veehiicuul disables the depth prepass and screen-space roughness limiter; VsyncStutterTest leaves their defaults. Disabling the prepass can help simple scenes but hurt scenes with enough overdraw, so its effect needs measurement. [Veehiicuul rendering settings](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/project.godot:41).

3. **Track switching definitely introduces synchronous work and a possible substantial hitch.** Veehiicuul frees/reloads the scene, reconstructs its managers and collision index, then calls `GC.Collect()`, waits for finalizers, and collects again. Even with only one track, D-pad up/down reloads that same track. This explains switching pauses, but not continuous driving jitter. [Track switching](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Source/GameDataAndLogic/TrackSwitcher.cs:34), [forced collection](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Source/Utility/GarbageCollectionUtility.cs:7).

4. **Input accumulation differs, but an extra frame of latency is not established.** VsyncStutterTest explicitly disables it; Veehiicuul retains the default `true`. Godot documents a responsiveness/CPU tradeoff. However, the local 4.7.2 Windows source flushes buffered events before the main iteration, and Veehiicuul polls controller state at the beginning of its frame. Therefore, I would not claim this automatically adds one frame of controller latency. Also, the test sphere moves autonomously—it does not test input-to-motion latency at all. [Sphere code](C:/Users/k/Repository/VsyncStutterTest/Godot/Sphere.gd:17), [Godot input documentation](https://docs.godotengine.org/en/4.6/classes/class_input.html#class-input-property-use-accumulated-input).

5. **Veehiicuul’s motion is more sensitive to varying frame duration.** Both applications move in `_Process`, but the sphere uses constant velocity while Veehiicuul integrates acceleration, then velocity into position, using the variable frame delta. Uneven frame times therefore affect Veehiicuul’s trajectory as well as its visual sampling. Collision-triggered resets deliberately teleport the car. Camera follow uses the newly calculated car position in the same frame, so I found **no one-frame camera-follow lag**. [Movement integration](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Source/GameDataAndLogic/Car/CarStateManager.cs:49), [frame order](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Source/Main.cs:65).

6. **C# adds potential JIT/GC costs, but recurring GC stutter is unproven.** Veehiicuul has more managed/native calls and input-event processing. I found no obvious large allocation loop or active per-frame logging in its normal driving path. Its existing collision review reports allocation-free queries with very small steady-state costs, making collision-query computation a relatively weak suspect. Those historical measurements are not a fresh benchmark of the current application. [Existing measurements](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Documentation/CollisionReviewFromScratch20260929/Report.md:162).

The resolution settings differ too: Veehiicuul uses a fixed 2560×1440 viewport with viewport stretching, while VsyncStutterTest uses the default unstretched viewport. **Veehiicuul’s recent log confirms both window and viewport are 2560×1440**, so its 1280×720 window override is not lowering rendering resolution in that fullscreen session. On other display resolutions, this difference could materially affect performance. [Settings](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/project.godot:24), [stretch behavior](https://docs.godotengine.org/en/4.5/tutorials/rendering/multiple_resolutions.html).

Both configure **exclusive fullscreen, VSync off, two swapchain images, D3D12, and disabled graphics fallbacks**. Both launchers request **High process priority**, and neither configures an FPS cap. Veehiicuul uses dummy physics servers versus Jolt in the test, but neither drives its moving object through fixed physics ticks.

**For ongoing jitter, I would investigate engine-version differences and actual CPU/GPU frame times first. For switching hitches, the synchronous reload and forced GC are concrete causes.** Driver profiles, VRR, and presentation timing remain unverified; identical project settings do not establish identical displayed-frame pacing.

No application files were changed. The conversation was recorded and committed separately as required.
