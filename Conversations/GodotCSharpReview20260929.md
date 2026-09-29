# Godot CSharp application review

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

review C:\Users\k\Repository\Veehiicuul\Veehiicuul_Godot_CSharp for any issues

## Assistant Commentary

I’ll review the application, build scripts, and configuration for concrete bugs and reliability issues, then report findings with file references.

## Assistant Commentary

The project’s Godot installation and export templates are present. I’m tracing startup, input, track switching, and collision handling, and I’ll run the existing verification programs alongside a release build.

## Assistant Commentary

The release build and both verification programs passed. The first build attempt hit sandbox restrictions; rerunning with the required access succeeded. I’m now checking scene startup and integration paths that the standalone tests don’t exercise.

## Assistant Question

Collision handling and the F11 fullscreen toggle are currently commented out. Should I treat those disabled features as intentional for this review?

## User

Yes, they are intentionally disabled

## Assistant Commentary

I’ll treat collision handling and F11 as intentionally disabled. No failures have surfaced in the active movement and camera checks. I’m checking car/track switching and logging through the native application before wrapping up.

## Assistant Commentary

Native checks passed for cycling all six cars, repeated track reloads, and camera follow. I confirmed that direct launches bypass the required session logging: no `MyLogOutput` folder is created, and output goes to Godot’s default user-data log. The disabled collision manager also still references the old collider-data path.

## Assistant Commentary

The exported game also passed the DirectX 12 startup check with Forward+, VSync disabled, and no FPS limit. I’m recording the review in the separate conversation commit required by `AGENTS.md`; application source remains unchanged.

## Assistant

Found one active issue and one latent issue in the intentionally disabled collision code:

::code-comment{title="[P2] Restore session logging for direct and IDE launches" body="Only Run.ps1 configures session logging; SessionLog.CreateDirectory has no callers. Launching the executable directly or using the Visual Studio profile creates no timestamped MyLogOutput folder and writes to Godot’s default user-data log instead. I reproduced this with a direct launch. Route these entry points through the required session-log setup so their diagnostics are retained per run." file="C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Source/Main.cs" start=40 end=43 priority=2}

::code-comment{title="[P3] Correct the dormant collision-data path" body="CollisionManager still loads res://ColliderData/{trackName}_ColliderData.json, but Ribeye’s file is now res://Tracks/Ribeye/Ribeye_ColliderData.json. The old path is absent from both the project and exported package. Re-enabling collision initialization will therefore throw FileNotFoundException. Update this path before reconnecting collision handling." file="C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Source/GameDataAndLogic/CollisionManager.cs" start=29 end=30 priority=3}

Validation passed: release build/export, both verification suites, native car/track switching and camera checks, and DirectX 12 startup.

Application source unchanged. Review recorded in a separate `[cnv]` commit.
