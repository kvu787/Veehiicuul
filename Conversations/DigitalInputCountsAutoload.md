# Digital input counts autoload

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
  <current_date>2026-09-29</current_date>
  <timezone>America/Los_Angeles</timezone>
  <filesystem><workspace_roots><root>C:\Users\k\Repository\Veehiicuul</root><root>C:\Users\k\.codex\visualizations\2026\09\29\01a0ec1b-d716-7f72-b88d-9f7a4debea49</root></workspace_roots><permission_profile type="managed"><file_system type="restricted"><entry access="read"><special>:root</special></entry><entry access="write"><path>C:\Users\k\Repository\Veehiicuul</path></entry><entry access="write"><path>C:\Users\k\.codex\visualizations\2026\09\29\01a0ec1b-d716-7f72-b88d-9f7a4debea49</path></entry><entry access="write"><special>:slash_tmp</special></entry><entry access="write"><special>:tmpdir</special></entry><entry access="read"><path>C:\Users\k\Repository\Veehiicuul\.git</path></entry><entry access="read"><path>C:\Users\k\.codex\visualizations\2026\09\29\01a0ec1b-d716-7f72-b88d-9f7a4debea49\.git</path></entry><entry access="read"><path>C:\Users\k\Repository\Veehiicuul\.agents</path></entry><entry access="read"><path>C:\Users\k\.codex\visualizations\2026\09\29\01a0ec1b-d716-7f72-b88d-9f7a4debea49\.agents</path></entry><entry access="read"><path>C:\Users\k\Repository\Veehiicuul\.codex</path></entry><entry access="read"><path>C:\Users\k\.codex\visualizations\2026\09\29\01a0ec1b-d716-7f72-b88d-9f7a4debea49\.codex</path></entry></file_system></permission_profile></filesystem>
</environment_context>

## User

C:\Users\k\Repository\Veehiicuul\Veehiicuul_Godot_CSharp\Veehiicuul

write an autoloaded class that collects a count of down events for every digital input (mouse, keyboard, gamepad, etc), makes those counts available via a public api that the game loop can query for a given mouse/keyboard/gamepad input code, and clears the counts at the end of every frame

## Assistant

I’ll check the Godot project and its input handling, then add an autoload with per-input press counts and end-of-frame clearing.

## Assistant

The project keeps frame processing in its main adapter. I’ll preserve that structure, count press events—including multiple presses within one frame—and provide queries for keyboard keys, mouse buttons, individual gamepads, and touch contacts. I’m treating keyboard auto-repeat as a held key, so it won’t add extra presses.

## Assistant

The collector is in place. It listens to the main window before UI handlers can consume events, and the main adapter clears the counts in a `finally` block after the game update. I’m checking compilation and testing event delivery and frame clearing in Godot.

## Assistant

The release build succeeds with three existing unused-field warnings in `InputManager`. The runtime test also caught Godot’s default touch-to-mouse emulation: a touch can produce a mouse press too. I’m separating those event types in the test so each count can be verified precisely.

## Assistant

The Godot runtime checks passed: multiple presses, repeat filtering, gamepad separation, touch contacts, handled input, and clearing after early returns and exceptions. I’m recording the implementation and the required conversation log in separate commits.

## Assistant

Added the [DigitalInputCounts autoload](/C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Source/EngineIntegration/DigitalInputCounts.cs). Query it inside `Main.Process`:

```csharp
var input = DigitalInputCounts.Instance;
int keys = input.GetPhysicalKeyDownCount(Key.Space);
int clicks = input.GetMouseButtonDownCount(MouseButton.Left);
int buttons = input.GetGamepadButtonDownCount(0, JoyButton.A);
```

Also supports logical keys, mouse wheels, and touch contacts. Reads preserve counts; they clear automatically after `Main.Process`, including early returns and exceptions. Keyboard auto-repeat is ignored.

Verified with 61 synthetic-event checks in Godot and a release build. Three existing `InputManager` warnings remain.

## User

```
c:\Users\k\Repository\Veehiicuul\Veehiicuul_Godot_CSharp\Veehiicuul\project.godot:20:1-20:75
Lines and columns are 1-based. Columns are StartInclusive:EndExclusive. Columns count UTF-16 code units.
```

```
DigitalInputCounts="*res://Source/EngineIntegration/DigitalInputCounts.cs"
```

what does that asterisk mean in the path?

## Assistant

I’ll check how Godot interprets that autoload entry and explain what the `*` changes.

## Assistant

The `*` marks the autoload as a **globally named singleton**. Godot removes it before resolving the actual resource path. [Godot’s parser](/C:/Users/k/Repository/External/Godot_4-7-2/core/config/project_settings.cpp:364)

- **With `*`:** Godot loads the node and exposes `DigitalInputCounts` as a global name in GDScript.
- **Without `*`:** Godot still loads the node at `/root/DigitalInputCounts`, but doesn’t expose that global name.

It corresponds to the **Enable** checkbox in the Autoload settings. That checkbox has no effect on C# access: our `DigitalInputCounts.Instance` property provides that access independently, so it works either way. [Godot documentation](https://docs.godotengine.org/en/stable/tutorials/scripting/singletons_autoload.html)

## User

if that checkbox has no effect on C# access, then why did you add the asterisk? is it recommended practice for things like this?

## Assistant

I’ll check whether Godot recommends that setting for C# autoloads or simply enables it by default.

## Assistant

I included it because **Godot enables that checkbox by default** when adding an autoload. I followed that convention; this implementation doesn’t require it. [Godot documentation](https://docs.godotengine.org/en/stable/tutorials/scripting/singletons_autoload.html)

It isn’t a C#-specific recommended practice. For our service, `*` provides no benefit because access comes from the static `Instance` property. Keeping it matches the editor’s default; removing it is equally valid and leaves the C# behavior unchanged.
