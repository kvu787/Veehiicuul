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

## User

if godot treats model front as +Z, then shouldn't the driving code treat forward as +Z?

## Assistant Commentary

I’ll check how the driving code uses “forward” so I can distinguish the car’s local direction from camera and world directions.

## Assistant

**Yes—for your existing models, the driving code should treat car-local `+Z` as forward.** That matches the Blender → glTF → Godot asset convention directly.

Godot supports both conventions:

- **Camera forward:** local `-Z`.
- **Model forward:** local `+Z`, with **model right being local `-X`**.

Its documentation explicitly recommends the `MODEL_*` direction constants when working in model space. [Godot Vector3 documentation](https://docs.godotengine.org/en/stable/classes/class_vector3.html#class-vector3-constant-model-front)

My earlier recommendation preserved the existing driving code by changing the assets. **For your pipeline, I would instead correct the car code and keep the Blender models and export conversion as they are.** Using `-Z` for gameplay objects is a choice, not an engine requirement.

That correction needs to cover forward/reverse acceleration, car-relative left/right, heading calculated from velocity, and which end of the collision footprint is considered the front. Your camera-relative input can continue using `-Z` as camera forward; it must then be transformed into the car’s coordinate frame.

So this is a mismatch between the driving code’s assumptions and the imported models’ convention. The exporter is behaving correctly.

## User

also, am i modeling in blender correctly? currently, i model so that a character/vehicle's local axis is such that the model is looking toward the -Y axis

## Assistant Commentary

I’ll verify the Blender convention against the glTF/Godot documentation.

## Assistant

**Yes. Having the character or vehicle face local `-Y` in Blender is correct for the standard Blender → glTF → Godot workflow.** Godot’s documentation explicitly identifies Blender `-Y` as the asset’s front. [Source](https://docs.godotengine.org/en/stable/tutorials/assets_pipeline/importing_3d_scenes/model_export_considerations.html#d-asset-direction-conventions)

With your current export settings:

| Model direction             | Blender local axis | Godot local axis |
| --------------------------- | ------------------ | ---------------- |
| Front                       | `-Y`               | `+Z`             |
| Up                          | `+Z`               | `+Y`             |
| Right, from its perspective  | `-X`               | `-X`             |

The **local** part matters: you can rotate the vehicle in Object Mode to face any direction on the track while its nose still points along its own local `-Y`.

Keep modeling that way. My initial suggestion to change the meshes to face `+Y` was an alternative convention to accommodate the existing code; it wasn’t a correction your models needed. The driving code should be brought into agreement with your models.

## User

what is "camera forward" and "model forward" in blender

## Assistant Commentary

I’ll check Blender’s camera axes so I can compare them precisely with the model convention we’ve been discussing.

## Assistant

In Blender, they describe **which direction something is looking, measured using its own local axes**:

|                           | Camera object                  | Character or vehicle model        |
| ------------------------- | ------------------------------ | --------------------------------- |
| Meaning of forward        | Direction the lens looks       | Direction the face or nose points |
| Local forward axis        | `-Z`                           | Conventionally `-Y`               |
| Local up axis             | `+Y`                           | Conventionally `+Z`               |

**Camera forward is built into Blender’s camera behavior:** the camera looks along its local `-Z`. Rotating the camera changes where that axis points in the world. With zero rotation, a Blender camera looks straight down along world `-Z`. [Blender documentation](https://docs.blender.org/manual/en/4.5/modeling/modifiers/modify/uv_project.html#options)

**Model forward is an authoring convention:** Blender doesn’t automatically know which end of a mesh is its nose. You build the model with its nose pointing toward local `-Y` and its top toward local `+Z`, matching the convention used by your export pipeline. [Godot’s Blender asset guidance](https://docs.godotengine.org/en/stable/tutorials/assets_pipeline/importing_3d_scenes/model_export_considerations.html#d-asset-direction-conventions)

For example, an unrotated character faces **world `-Y`**. A camera positioned in front of that character looks back toward **world `+Y`**. You rotate the camera so that its **local `-Z`** points toward world `+Y`.

The axes belong to different objects, so they don’t need to have matching names to point toward each other.

## User

okay, do this fix: for your existing models, the driving code should treat car-local +Z as forward

## Assistant Commentary

I’ll update the car’s forward, right, and heading calculations to match the imported models, check the collision front/rear handling, and run the existing verification suites.

## Assistant Commentary

I found three assumptions to change: acceleration treats `-Z` as forward, heading calculations point `-Z` along velocity, and collision shortening treats `-Z` as the nose. I’ll update those for model forward `+Z` and model right `-X`, while keeping camera-relative input and track coordinates unchanged.

## Assistant Commentary

The driving checks pass, including distinct acceleration strengths on all four model axes, rotated spawns, and successive forward acceleration. The applied rotation now points the car’s `+Z` nose along its velocity. I’m checking the collision suite and release build next.

## Assistant Commentary

The C# release build passes with no warnings or errors. The export step is blocked because the project has no `export_presets.cfg`. I also found that gameplay collisions are currently disabled; I’ve corrected their front/rear handling and will verify it directly without enabling collisions.

## Assistant Commentary

The native Godot check passed with all six cars loaded from the existing Ribeye GLB. Their `+Z` noses follow forward motion, and contact checks at three rotations confirm collision shortening now applies to the nose. I’m reviewing the diff and committing the fix and conversation log separately.

## Assistant

Implemented in commit `f9a7bff`.

Cars now use **local +Z as forward and -X as right**. Velocity-based heading points the nose along movement, and collision shortening applies to the correct front end.

The C# release build, driving and collision suites, and native Godot checks with all six imported cars passed.

**EXE export remains blocked:** the project is missing `export_presets.cfg`.
