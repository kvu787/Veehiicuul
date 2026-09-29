# Blender Car Forward Direction

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
<environment_context>
  <cwd>C:\Users\k\Repository\Veehiicuul</cwd>
  <shell>powershell</shell>
  <current_date>2026-09-29</current_date>
  <timezone>America/Los_Angeles</timezone>
  <filesystem><workspace_roots><root>C:\Users\k\Repository\Veehiicuul</root><root>C:\Users\k\.codex\visualizations\2026\09\29\01a0ecac-2558-7a63-b988-fa9a07cdb1c0</root></workspace_roots><permission_profile type="managed"><file_system type="restricted"><entry access="read"><special>:root</special></entry><entry access="write"><path>C:\Users\k\Repository\Veehiicuul</path></entry><entry access="write"><path>C:\Users\k\.codex\visualizations\2026\09\29\01a0ecac-2558-7a63-b988-fa9a07cdb1c0</path></entry><entry access="write"><special>:slash_tmp</special></entry><entry access="write"><special>:tmpdir</special></entry><entry access="read"><path>C:\Users\k\Repository\Veehiicuul\.git</path></entry><entry access="read"><path>C:\Users\k\.codex\visualizations\2026\09\29\01a0ecac-2558-7a63-b988-fa9a07cdb1c0\.git</path></entry><entry access="read"><path>C:\Users\k\Repository\Veehiicuul\.agents</path></entry><entry access="read"><path>C:\Users\k\.codex\visualizations\2026\09\29\01a0ecac-2558-7a63-b988-fa9a07cdb1c0\.agents</path></entry><entry access="read"><path>C:\Users\k\Repository\Veehiicuul\.codex</path></entry><entry access="read"><path>C:\Users\k\.codex\visualizations\2026\09\29\01a0ecac-2558-7a63-b988-fa9a07cdb1c0\.codex</path></entry></file_system></permission_profile></filesystem>
</environment_context>

## User

There is an issue with the veehiicuul blender export: the "-Y is forward" convention in blender doesn't seem to translate to "-Z is forward" in godot engine with the current export script. in godot engine, I can see that the local axis of the placeholder car has +Z pointing along the front of the car. how do i fix this?


## Assistant Commentary

I’ll check the Blender export script and how Godot imports the car to find where its forward direction is being flipped.

## Assistant Commentary

The exporter maps Blender `(X, Y, Z)` to Godot `(X, Z, -Y)`, so Blender’s `-Y` correctly becomes Godot’s `+Z`. Godot distinguishes model front (`+Z`) from the `-Z` forward direction your driving code uses. I’m checking the placeholder’s setup so the fix preserves its intended starting direction.

## Assistant Commentary

The placeholder and all six playable cars use the same local orientation. The fix needs to cover all seven: rotate each car’s mesh 180° around its local vertical axis, then compensate with the object’s rotation to preserve its placement. That will make local `-Z` point toward the nose after export.

## Assistant

**Keep `export_yup=True`. To match your existing driving code, the cars need to face local `+Y` in Blender, which exports as local `-Z` in Godot.**

Your [export script](C:/Users/k/Repository/Veehiicuul/Blender/Scripts/ExportToVeehiicuul.py:193) uses this conversion:

```text
Blender → Godot
+X      → +X
+Y      → -Z
+Z      → +Y
```

Consequently, Blender `-Y` becomes Godot `+Z`. This is expected: Godot distinguishes **model front (`+Z`)** from **camera/engine forward (`-Z`)**. [Godot’s documentation explains the distinction.](https://docs.godotengine.org/en/stable/tutorials/assets_pipeline/importing_3d_scenes/model_export_considerations.html#d-asset-direction-conventions)

For Veehiicuul, I recommend fixing the cars’ local mesh orientation because [your driving code](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Source/GameDataAndLogic/Car/CarStateManager.cs:66) already treats `-Z` as forward.

For each car in Blender:

1. In **Edit Mode**, select all vertices and rotate them **180° around the object’s local Z axis**, using the **object origin as the pivot**. The nose now points toward local `+Y`.
2. In **Object Mode**, rotate the object **180° around its local Z axis**, again around its origin. This restores its original appearance and direction on the track while keeping the corrected local mesh orientation.
3. **Do not apply that compensating object rotation.** Re-export and let Godot reimport.

Apply this to **`SlopeCarPlaceholder` and all six playable cars**, including the template used for future tracks. Fixing only the placeholder leaves the playable meshes facing backward relative to their movement.

If you want to **retain local `-Y` as front in Blender**, the export script can instead perform the same mesh rotation and compensating object rotation on temporary copies of those car objects. Rotating the entire exported scene won’t fix the car’s local forward axis, because the mesh and its axes rotate together.
