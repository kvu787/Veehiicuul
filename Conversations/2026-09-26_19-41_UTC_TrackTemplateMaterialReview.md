# TrackTemplate Material Review

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

</INSTRUCTIONS><environment_context>
  <cwd>C:\Users\k\Repository\Veehiicuul</cwd>
  <shell>powershell</shell>
  <current_date>2026-09-26</current_date>
  <timezone>America/Los_Angeles</timezone>
  <filesystem><workspace_roots><root>C:\Users\k\Repository\Veehiicuul</root><root>C:\Users\k\.codex\visualizations\2026\09\26\01a0df35-83f3-7103-9b88-4b3d79bb1e87</root></workspace_roots><permission_profile type="managed"><file_system type="restricted"><entry access="read"><special>:root</special></entry><entry access="write"><path>C:\Users\k\Repository\Veehiicuul</path></entry><entry access="write"><path>C:\Users\k\.codex\visualizations\2026\09\26\01a0df35-83f3-7103-9b88-4b3d79bb1e87</path></entry><entry access="write"><special>:slash_tmp</special></entry><entry access="write"><special>:tmpdir</special></entry><entry access="read"><path>C:\Users\k\Repository\Veehiicuul\.git</path></entry><entry access="read"><path>C:\Users\k\.codex\visualizations\2026\09\26\01a0df35-83f3-7103-9b88-4b3d79bb1e87\.git</path></entry><entry access="read"><path>C:\Users\k\Repository\Veehiicuul\.agents</path></entry><entry access="read"><path>C:\Users\k\.codex\visualizations\2026\09\26\01a0df35-83f3-7103-9b88-4b3d79bb1e87\.agents</path></entry><entry access="read"><path>C:\Users\k\Repository\Veehiicuul\.codex</path></entry><entry access="read"><path>C:\Users\k\.codex\visualizations\2026\09\26\01a0df35-83f3-7103-9b88-4b3d79bb1e87\.codex</path></entry></file_system></permission_profile></filesystem>
</environment_context>

## User

review "C:\Users\k\Repository\Veehiicuul\Blender\TrackTemplate.blend" for any "issues"
ensure that all materials are principled bsdf with default values except for color, Roughness=0.5, and 'Specular IOR Level'=0.0

## Assistant (commentary)

I’ll inspect the Blender file, check for scene and geometry issues, and update any materials that don’t match your requested Principled BSDF settings.

## Assistant (commentary)

The template has embedded build, validation, and export scripts, so I’m checking those alongside the meshes and materials. I’ll compare the shaders against fresh defaults from the installed Blender 4.5.14.

## Assistant (commentary)

All 31 materials already match your request: each has one Principled BSDF connected directly to Material Output, Roughness `0.5`, Specular IOR Level `0.0`, and default values for every other shader input except Base Color. No material edits are needed. I’m finishing the geometry and export checks.

## Assistant (commentary)

The geometry checks passed: all 61 meshes validate, and rebuilding the track reproduces the saved geometry. Fresh exports still expose two collision problems: the JSON lacks metadata required by the game, and the collision loader negates both coordinates, placing boundaries opposite an unrotated GLB import. The export paths now point inside the Godot project.

## Assistant (commentary)

The export preflight allows 13 color-block and example objects into the GLB. Excluding their collections reduces the export from 43,598 to 20,952 triangles. I also confirmed optional geometry cleanup: dense subdivision on VehicleRoad and BigCone, plus coincident internal faces in the checker line. I’m collecting the findings and evidence in the report.

## Assistant

**All 31 materials already meet your requirements:** Principled BSDF, Roughness **0.5**, Specular IOR Level **0.0**, and all other shader values at Blender 4.5.14 defaults except Base Color. The `.blend` file is unchanged.

The review found:

1. **Collision JSON fails the game’s loader:** required `FormatVersion` and `CoordinateSystem` fields are missing.
2. **Collision coordinates disagree with an unrotated GLB import:** the loader negates both coordinates, moving boundaries opposite the visible track.
3. **Export preflight permits 13 example objects.** Excluding their collections reduces the export from **43,598 to 20,952 triangles**.
4. **Background validation misidentifies the template** because template detection depends on the active editor being a Text Editor.

Optional cleanup: VehicleRoad exports **1,792 triangles** versus **448** at its render subdivision level; BigCone totals **22,528 triangles**; the checker line contains **42 coincident internal face pairs**.

All **61 meshes** passed validation, all **32 barriers** are closed, and rebuilding reproduces the saved track.

[Full review and evidence](C:/Users/k/.codex/visualizations/2026/09/26/01a0df35-83f3-7103-9b88-4b3d79bb1e87/TrackTemplateReview/TrackTemplateReview.md), including the separate Godot import handling needed for zero specular.


## User

Template validation depends on the current editor

^how do i throw an exception if the script isn't running from a text editor in a file with a name

## Assistant (commentary)

I’ll check the validator and Blender’s context API for the guard you need.

## Assistant

Put these checks at the beginning of `Main()`:

```python
def Main():
    area = bpy.context.area
    if area is None or area.type != "TEXT_EDITOR":
        raise RuntimeError("Run this script from Blender's Text Editor.")

    if not bpy.data.filepath:
        raise RuntimeError("Save the .blend file before running this script.")

    # Rest of your validation...
```

Then simplify the filename check:

```python
def IsTrackTemplateFile():
    return Path(bpy.data.filepath).name == "TrackTemplate.blend"
```

`bpy.data.filepath` checks whether the current **.blend file has a saved filename**. It does not require saving every subsequent edit.
