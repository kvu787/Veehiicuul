# Conversation

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

<environment_context>
  <cwd>C:\Users\k\Repository\Veehiicuul</cwd>
  <shell>powershell</shell>
  <current_date>2026-10-09</current_date>
  <timezone>America/Los_Angeles</timezone>
  <filesystem><workspace_roots><root>C:\Users\k\Repository\Veehiicuul</root><root>C:\Users\k\.codex\visualizations\2026\10\09\01a12196-7e4f-7533-8553-ad10f6432a8d</root></workspace_roots><permission_profile type="managed"><file_system type="restricted"><entry access="read"><special>:root</special></entry><entry access="write"><path>C:\Users\k\Repository\Veehiicuul</path></entry><entry access="write"><path>C:\Users\k\.codex\visualizations\2026\10\09\01a12196-7e4f-7533-8553-ad10f6432a8d</path></entry><entry access="write"><special>:slash_tmp</special></entry><entry access="write"><special>:tmpdir</special></entry><entry access="read"><path>C:\Users\k\Repository\Veehiicuul\.git</path></entry><entry access="read"><path>C:\Users\k\.codex\visualizations\2026\10\09\01a12196-7e4f-7533-8553-ad10f6432a8d\.git</path></entry><entry access="read"><path>C:\Users\k\Repository\Veehiicuul\.agents</path></entry><entry access="read"><path>C:\Users\k\.codex\visualizations\2026\10\09\01a12196-7e4f-7533-8553-ad10f6432a8d\.agents</path></entry><entry access="read"><path>C:\Users\k\Repository\Veehiicuul\.codex</path></entry><entry access="read"><path>C:\Users\k\.codex\visualizations\2026\10\09\01a12196-7e4f-7533-8553-ad10f6432a8d\.codex</path></entry><entry access="read"><path>C:\Users\k\Repository\Veehiicuul\.aws</path></entry><entry access="read"><path>C:\Users\k\.codex\visualizations\2026\10\09\01a12196-7e4f-7533-8553-ad10f6432a8d\.aws</path></entry></file_system></permission_profile></filesystem>
</environment_context>

## User

<external_codex_apps_open_page>{"page_id":null}</external_codex_apps_open_page>

## User

review the blender trackbuilder workflow. mainly, i want to lock in the track building workflow part of "C:\Users\k\Repository\Veehiicuul\Veehiicuul_Godot_CSharp" as "feature-freeze" so i can shift my development focus to another project while continuing to build tracks and play them with Veehiicuul_Godot_CSharp.

## Assistant commentary

I'll review the Blender-to-game workflow, check what could interrupt building and playing tracks, and define a concrete feature-freeze boundary.

## Assistant commentary

The workflow already has substantial geometry tests, but the new-track guide omits a required settings file and no longer matches the export script's collection checks. I'm checking the scripts embedded in the template and running the Blender tests to distinguish daily-use blockers from optional improvements.

## Assistant commentary

Ribeye exports successfully, and the embedded scripts match the repository code. The Blender suite has three failures caused by tests still expecting the old minimum dimension of 0.1; the implementation and template use 0.00390625. I'm also testing a new track through Godot export and checking whether a failed Blender export can leave mismatched model and collision files.

## Assistant commentary

I confirmed two reliability issues beyond the documentation: a failed GLB export overwrites the collision JSON while retaining the old model, and a supported NURBS curve can exceed the documented offset tolerance. The existing game builds cleanly. I'll capture a small freeze-readiness checklist and verify the template-to-game path in an isolated copy.

## Assistant commentary

I'm using the write-page skill's review guidance to save the findings and proposed freeze boundary in the repository documentation.

## Assistant commentary

The template-derived track now passes release export, startup, switching to Ribeye and back, and switching through all six cars. That supports freezing the current feature set. I'd make a bounded reliability pass first; the remaining fixes don't require redesigning the workflow. These were headless checks, so visual and controller playtesting remains part of final signoff.

## Assistant

**The feature set is ready to freeze, but I'd close four reliability gaps before calling the workflow settled:**

1. **Failed exports can mix generations.** The exporter overwrites collision JSON before exporting the GLB. A failure leaves new collisions paired with the old model. Reproduced.
2. **The authoring guide is incomplete.** It omits the required settings JSON, exact scene-root name, rebuilding after export, and several collection-toggle steps.
3. **NURBS offset alignment has a real defect.** A supported curve produced approximately 0.07758 units of error against the documented 0.001 tolerance, while reporting a successful build.
4. **Regression tests are stale.** Three parameter checks fail because they still expect the former minimum dimension.

The release build passed with zero warnings/errors. A template-derived track loaded successfully, switched to Ribeye and back, and cycled through all six cars. Visual/controller playtesting remains outstanding.

Freeze the implementation after those repairs; continue allowing track content, settings, catalog registration, and rebuilds. Caching, batching, automatic discovery, and other new features can stay deferred.

[Full review, proposed freeze boundary, and acceptance checklist](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Documentation/TrackBuildingFeatureFreezeReview.md). Application code and original Blender files remain unchanged.

- :codex-followup[Finish the freeze-readiness fixes]{prompt="Apply the four main findings in TrackBuildingFeatureFreezeReview.md, verify the Blender-to-Godot authoring loop, and record the track-building feature-freeze policy. Keep new features and optional performance work out of scope."}
