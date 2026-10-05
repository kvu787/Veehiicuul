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
  <current_date>2026-10-05</current_date>
  <timezone>America/Los_Angeles</timezone>
  <filesystem><workspace_roots><root>C:\Users\k\Repository\Veehiicuul</root><root>C:\Users\k\.codex\visualizations\2026\10\05\01a10e42-a568-7882-bdca-7727c0b8bd20</root></workspace_roots><permission_profile type="managed"><file_system type="restricted"><entry access="read"><special>:root</special></entry><entry access="write"><path>C:\Users\k\Repository\Veehiicuul</path></entry><entry access="write"><path>C:\Users\k\.codex\visualizations\2026\10\05\01a10e42-a568-7882-bdca-7727c0b8bd20</path></entry><entry access="write"><special>:slash_tmp</special></entry><entry access="write"><special>:tmpdir</special></entry><entry access="read"><path>C:\Users\k\Repository\Veehiicuul\.git</path></entry><entry access="read"><path>C:\Users\k\.codex\visualizations\2026\10\05\01a10e42-a568-7882-bdca-7727c0b8bd20\.git</path></entry><entry access="read"><path>C:\Users\k\Repository\Veehiicuul\.agents</path></entry><entry access="read"><path>C:\Users\k\.codex\visualizations\2026\10\05\01a10e42-a568-7882-bdca-7727c0b8bd20\.agents</path></entry><entry access="read"><path>C:\Users\k\Repository\Veehiicuul\.codex</path></entry><entry access="read"><path>C:\Users\k\.codex\visualizations\2026\10\05\01a10e42-a568-7882-bdca-7727c0b8bd20\.codex</path></entry><entry access="read"><path>C:\Users\k\Repository\Veehiicuul\.aws</path></entry><entry access="read"><path>C:\Users\k\.codex\visualizations\2026\10\05\01a10e42-a568-7882-bdca-7727c0b8bd20\.aws</path></entry></file_system></permission_profile></filesystem>
</environment_context>

## User

<external_codex_apps_open_page>{"page_id":null}</external_codex_apps_open_page>

## User

as seen in the staged change, i tried using presentmon's `--terminate_on_proc_exit` option and it still doesn't work, in that the presentmon console window remains open after the game process exits. investigate.

## Assistant Commentary

I'll inspect the staged change and the PresentMon launch path, then trace why the console stays open after the game exits.

## Assistant Commentary

The logs show two issues: a successful capture warns that limited process access can prevent `--terminate_on_proc_exit` from working, and the two latest launches failed because a "PresentMon" trace session was already running. I'll check the source to distinguish the shutdown failure from those failed launches.

## Assistant Commentary

I found a likely source bug: PresentMon queues process-exit events but handles them while processing target frames. After the game's last frame, other already-known processes are skipped before that step. I'll run a bounded capture with the actual game and a unique session name to test this.

## Assistant Commentary

The live tests reproduced the failure. PresentMon captured 8,434 game frames but stayed alive for ten seconds after normal game exit; it stopped only at the test's timed limit. In a second run, opening a new graphics window caused it to exit before that limit, which supports the queued-event bug. The earlier lingering capture also recorded two later game runs, explaining their "session already running" errors.

## Assistant Final

Confirmed: this is a **PresentMon 2.6.0 exit-handling bug**.

It queues the game's exit event, but handles that queue during frame processing. Already-known non-target processes are skipped before that step. Once the game stops producing frames, its exit event can remain queued indefinitely. [PresentMon source](https://github.com/GameTechDev/PresentMon/blob/v2.6.0/PresentMon/OutputThread.cpp).

Live tests reproduced this: PresentMon captured **8,434 game frames**, then remained alive after normal game exit. In a second test, opening an unrelated graphics window caused it to exit before the timed fallback, matching that source path.

Your latest two launches also failed because the earlier "PresentMon" trace session was still running. That earlier capture recorded both later games.

For reliable shutdown, use the explicit Ctrl+C flow already implemented by [MyRun.ps1](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/MyRun.ps1). Adding `--terminate_on_proc_exit` alone is insufficient.

[Detailed findings and test evidence](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/MyLogOutput/2026-10-05_15-53-33/InvestigationReport.txt). Your staged change is intact.

## User

is there a simple way to programmatically and properly shutdown the presentmon process a few seconds after `Wait-Process -Id $process.Id` returns? 

## Assistant Commentary

I'll check PresentMon's shutdown controls and the existing helper for a simple way to request a clean exit.

## Assistant Final

Yes. **PresentMon's `--terminate_existing_session` provides a clean shutdown:**

```powershell
Wait-Process -Id $process.Id
Start-Sleep -Seconds 5

& $PresentMonPath --terminate_existing_session
if ($LASTEXITCODE -ne 0) {
    throw "PresentMon shutdown failed with exit code $LASTEXITCODE."
}
```

This stops the default "PresentMon" ETW session. The original capture process then drains events, flushes its CSV, and exits normally. [Shutdown implementation](https://github.com/GameTechDev/PresentMon/blob/v2.6.0/PresentMon/ConsumerThread.cpp).

I tested this with your actual game: shutdown was requested **5.03 seconds** after game exit, and PresentMon exited successfully **0.14 seconds** later. It saved **8,837 frames**.

To wait explicitly for completion, retain the original PresentMon process using `-PassThru`, then call its `WaitForExit()` after the shutdown command.

If you give the capture a unique `--session_name`, supply that same name to the shutdown command. This prevents stopping another capture.

## User

implement the --terminate_existing_session approach with the WaitForExit() wait

## Assistant Commentary

I'll update the simple launcher to stop PresentMon's session five seconds after the game exits, then wait for PresentMon to finish flushing.

## Assistant Commentary

The updated launcher passed the live test: PresentMon exited successfully 5.11 seconds after the game and saved 9,105 frames. It uses a unique session name to target that capture. I'm checking the `NoPresentMon` path before committing.

## Assistant Commentary

The check found an existing issue: the inner script block was reading an empty `$args`, so `NoPresentMon` was ignored. I've moved that argument check to the script's outer scope and will verify both paths again.

## Assistant Final

Updated [MyRun_Simple.ps1](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/MyRun_Simple.ps1:50) to wait five seconds, stop its unique PresentMon session, then call `WaitForExit()`.

Also fixed the existing `NoPresentMon` argument handling.

Live Windows PowerShell 5.1 validation passed: PresentMon exited cleanly after **5.16 seconds**, saving **9,144 frames**. `NoPresentMon` ran without the shutdown delay.
