# Ribeye Blender Scene Review

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
  <current_date>2026-10-01</current_date>
  <timezone>America/Los_Angeles</timezone>
  <filesystem><workspace_roots><root>C:\Users\k\Repository\Veehiicuul</root><root>C:\Users\k\.codex\visualizations\2026\10\01\01a0f75f-7255-7150-b982-cf588226c487</root></workspace_roots><permission_profile type="managed"><file_system type="restricted"><entry access="read"><special>:root</special></entry><entry access="write"><path>C:\Users\k\Repository\Veehiicuul</path></entry><entry access="write"><path>C:\Users\k\.codex\visualizations\2026\10\01\01a0f75f-7255-7150-b982-cf588226c487</path></entry><entry access="write"><special>:slash_tmp</special></entry><entry access="write"><special>:tmpdir</special></entry><entry access="read"><path>C:\Users\k\Repository\Veehiicuul\.git</path></entry><entry access="read"><path>C:\Users\k\.codex\visualizations\2026\10\01\01a0f75f-7255-7150-b982-cf588226c487\.git</path></entry><entry access="read"><path>C:\Users\k\Repository\Veehiicuul\.agents</path></entry><entry access="read"><path>C:\Users\k\.codex\visualizations\2026\10\01\01a0f75f-7255-7150-b982-cf588226c487\.agents</path></entry><entry access="read"><path>C:\Users\k\Repository\Veehiicuul\.codex</path></entry><entry access="read"><path>C:\Users\k\.codex\visualizations\2026\10\01\01a0f75f-7255-7150-b982-cf588226c487\.codex</path></entry><entry access="read"><path>C:\Users\k\Repository\Veehiicuul\.aws</path></entry><entry access="read"><path>C:\Users\k\.codex\visualizations\2026\10\01\01a0f75f-7255-7150-b982-cf588226c487\.aws</path></entry></file_system></permission_profile></filesystem>
</environment_context>

## User

<external_codex_apps_open_page>{"page_id":null}</external_codex_apps_open_page>

## User

do a thorough review/analysis of "C:\Users\k\Repository\Veehiicuul\Blender\Tracks\Ribeye.blend" and check for any issues

## Assistant commentary

I will inspect the Blender file, analyze its geometry and scene settings, and review renders for visible issues. I will keep the file unchanged.

## Assistant commentary

The repository has a track validator and an export script. I will check the scene against both and test the boundary intersections and clearances that TrackBuilder leaves to the author.

## Assistant commentary

The file loads successfully, and its embedded validator and export script match the repository copies. I found a difference between viewport and render subdivision settings; I will verify which geometry reaches the GLB export. I will also save a detailed local report using the write-page skill's review guidance.

## Assistant commentary

Both validators pass, and both TrackBuilder copies reproduce the saved track geometry exactly. A fresh collider export also matches the committed JSON. I am now checking the exported model for stale data and reviewing close-up renders of the start line and decoration.

## Assistant commentary

The geometry checks passed: I found no outline intersections, overlapping floor triangles, or overlapping barrier tops. The fresh GLB matches the committed model data. The confirmed subdivision difference affects four objects; I am checking whether Blender's zero-specular settings also survive Godot import.

## Assistant commentary

I found an export/import issue: the 24 lit materials have specular set to zero in Blender, but Godot imports them with its default specular value of 0.5. Ribeye's import settings omit the existing material callback. Its sunlight currently has specular disabled, so this is a latent rendering issue rather than evidence of a visible defect in the current scene.

## Assistant commentary

The review is complete. I found no blocking geometry or export failures. The report covers the latent specular-import issue and optional geometry cleanup: four objects export at a higher subdivision level than they render, and the checker line has 122 pairs of hidden internal faces. All six car footprints fit at the spawn position with about 4.68 units of clearance.

## Assistant

Reviewed with Blender 4.5.14 and checked material import in Godot 4.7.2. **No blocking geometry or export defects found.**

The findings are:

1. **Zero specular does not survive Godot import.** The 24 lit materials import with specular `0.5`, despite Blender specifying zero. Ribeye's import settings omit the existing material callback. Its sunlight currently disables specular, masking this issue.
2. **Four objects export denser geometry than Blender renders.** They use subdivision level 3 for export and level 2 for rendering. Aligning them at level 2 would remove **11,616 triangles**, about **21.4%** of the scene. This is optional optimization.
3. **The checker line contains 122 pairs of hidden internal faces.** These are redundant geometry; no visible artifact was found.

Validation passed for all **266 meshes**. All **230 barriers** are closed and consistently wound. Boundary intersections, floor overlaps, and barrier-top overlaps were absent. Both builders reproduce the saved geometry, the committed GLB matches a fresh export byte for byte, and all six car footprints have a clear spawn.

[Full report, measurements, and diagnostic images](C:/Users/k/.codex/visualizations/2026/10/01/01a0f75f-7255-7150-b982-cf588226c487/RibeyeReview/RibeyeReview.md).

The `.blend` file is unchanged. Full gameplay and frame-time testing were outside this review.
