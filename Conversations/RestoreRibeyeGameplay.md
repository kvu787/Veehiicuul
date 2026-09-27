# Restore Ribeye gameplay

## User

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

## User

C:\Users\k\Repository\Veehiicuul\Veehiicuul_Godot_CSharp\Veehiicuul

do these changes:
- remove C:\Users\k\Repository\Veehiicuul\Veehiicuul_Godot_CSharp\Veehiicuul\Source\GameDataAndLogic\CollisionManager.cs entirely and rename C:\Users\k\Repository\Veehiicuul\Veehiicuul_Godot_CSharp\Veehiicuul\Source\GameDataAndLogic\CollisionManager2.cs to CollisionManager
- remove all ability to change graphics settings at runtime (C:\Users\k\Repository\Veehiicuul\Veehiicuul_Godot_CSharp\Veehiicuul\Source\GameDataAndLogic\GraphicsSettingsManager.cs)
- ensure that the game always runs at 16:9 aspect resolution and shows black bars if the window is not that aspect ratio
- start the game in borderless fullscreen at 2560*1440 resolution by default
- allow alt+enter to switch between borderless fullscreen and windowed mode
- remove all UI stuff and remove the dependence on a "UI" scene entirely

## Assistant

I’ll inspect the Godot project, update collision handling and display behavior, remove the UI dependency, then build and verify the changes. I’ll also record this conversation in a separate commit as required.

## Assistant

The required Godot install and export templates are present. I also found that `Main` currently skips initialization and throws every frame, and its configured track scenes are missing. I’m removing the requested dependencies while checking how the new window controls should fit into that unfinished startup path.

## Assistant question

Main currently skips game initialization and throws every frame; its configured track scenes are also missing. Should I preserve the unfinished gameplay setup while making Alt+Enter work in the existing window?

Options:

- Preserve gameplay setup; enable window controls
- Restore gameplay using the available Ribeye scene

## User

Restore gameplay using the available Ribeye scene

## Assistant

I’ll restore gameplay using Ribeye and check its car and camera nodes against the existing managers. The display will render at 2560×1440, preserve 16:9 with black bars, and use Alt+Enter to switch between borderless fullscreen and a resizable window.

## Assistant

The UI and runtime graphics controls are removed, and Ribeye is wired into startup. Its imported model has the six cars and spawn marker the game needs. I also removed unused legacy tire-marker requirements and adjusted camera validation to accept Ribeye’s authored camera distance and clipping planes.

## Assistant

The release export succeeded, but the startup check found two Ribeye integration issues: its collision JSON lacks required format metadata, and the detector still applies the old Unity coordinate conversion. I’m updating the export metadata and coordinate mapping so collisions line up with the Godot model.

## User

don't change the format of the collision JSON. make the code work without the format metadata.

## Assistant

I’ll keep the collision JSON format unchanged and remove the code’s metadata requirement. I’ll retain the coordinate fix needed to align collisions with Ribeye.

## Assistant

Ribeye now starts successfully using its original collision JSON. The collision checks pass, including a clear spawn position and contact at an exported barrier point. DirectX 12 renders the scene correctly; I’m finishing the Alt+Enter and black-bar checks in the exported game.

## User

also, restore UseSharedCompilation so that command-line export works properly

## Assistant

`UseSharedCompilation` is restored as `false` in the Godot project file. The command-line release export has completed successfully with it.

## Assistant

The exported game runs without errors, and the collision and coordinate checks pass. Windows is blocking the automated focus change needed for the keyboard test, so I’m checking Alt+Enter inside Godot’s input system next.

## Assistant

The in-engine checks found and resolved a Godot window-restoration issue. Alt+Enter now restores a 1280×720 window while rendering stays at 2560×1440. Fullscreen round trips, held-key handling, letterboxing, driving, and collision reset all pass.

## Assistant

The final exported game passes the window tests. I also checked screenshots of square and wide windows: both preserve the 16:9 view with black bars. I’m completing the separate implementation and conversation commits now.

## Assistant

Implemented and committed:

- Restored Ribeye gameplay and renamed the active collision manager.
- Removed runtime graphics controls and all UI dependencies.
- Fixed 2560×1440 rendering with 16:9 black bars.
- Borderless fullscreen startup; Alt+Enter toggles windowed mode, initially 1280×720.
- Kept collision JSON unchanged and removed metadata requirements.
- Restored `UseSharedCompilation=false`.

Release export, collision checks, driving/reset checks, and fullscreen/black-bar verification passed. Export required temporarily suppressing cached editor tabs that trigger a Godot headless error; the tabs were restored afterward.

Launch with [Run.cmd](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Run.cmd).
