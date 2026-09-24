# Public Godot project audit conversation

## User

<recommended_plugins>
Here is a list of plugins that are available but not installed.

- Dropbox (app-69b31dc2110c8191b8b47dc98fe5a052@openai-curated-remote)
- Box (box@openai-curated-remote)
- Codex Security (codex-security@openai-curated-remote)
- Figma (figma@openai-curated-remote)
- Finances (finances@openai-curated-remote)
- GitHub (github@openai-curated-remote)
- Gmail (gmail@openai-curated-remote)
- Google Calendar (google-calendar@openai-curated-remote)
- Google Drive (google-drive@openai-curated-remote)
- Linear (linear@openai-curated-remote)
- Notion (notion@openai-curated-remote)
- OpenAI Developers (openai-developers@openai-curated-remote)
- Outlook Calendar (outlook-calendar@openai-curated-remote)
- Outlook Email (outlook-email@openai-curated-remote)
- SharePoint (sharepoint@openai-curated-remote)
- Slack (slack@openai-curated-remote)
- Teams (teams@openai-curated-remote)
</recommended_plugins>
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
- Keep off vsync
- Keep max fps limiter off
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
  <current_date>2026-09-24</current_date>
  <timezone>America/Los_Angeles</timezone>
  <filesystem><workspace_roots><root>C:\Users\k\Repository\Veehiicuul</root><root>C:\Users\k\.codex\visualizations\2026\09\24\01a0d23c-1fe3-7050-8dfa-0af6a22e4ca8</root></workspace_roots><permission_profile type="managed"><file_system type="restricted"><entry access="read"><special>:root</special></entry><entry access="write"><path>C:\Users\k\Repository\Veehiicuul</path></entry><entry access="write"><path>C:\Users\k\.codex\visualizations\2026\09\24\01a0d23c-1fe3-7050-8dfa-0af6a22e4ca8</path></entry><entry access="write"><special>:slash_tmp</special></entry><entry access="write"><special>:tmpdir</special></entry><entry access="read"><path>C:\Users\k\Repository\Veehiicuul\.git</path></entry><entry access="read"><path>C:\Users\k\.codex\visualizations\2026\09\24\01a0d23c-1fe3-7050-8dfa-0af6a22e4ca8\.git</path></entry><entry access="read"><path>C:\Users\k\Repository\Veehiicuul\.agents</path></entry><entry access="read"><path>C:\Users\k\.codex\visualizations\2026\09\24\01a0d23c-1fe3-7050-8dfa-0af6a22e4ca8\.agents</path></entry><entry access="read"><path>C:\Users\k\Repository\Veehiicuul\.codex</path></entry><entry access="read"><path>C:\Users\k\.codex\visualizations\2026\09\24\01a0d23c-1fe3-7050-8dfa-0af6a22e4ca8\.codex</path></entry></file_system></permission_profile></filesystem>
</environment_context>

## User

a while back, i configured a godot WorldEnvironment node that had the settings i wanted. however, i don't remember what project had this. examine each public repo in my github at https://github.com/kvu787, compile a list of each godot project, and state if they configure a worldenvironment. also, for each godot project, if it does any custom/non-default configuration of lighting, rendering, shadows, etc, state that too.


## Assistant

I’ll enumerate your public repositories, find the Godot projects, and inspect their environment and rendering settings, including configuration applied in code.


## Assistant

I found 13 Godot projects across 10 of your 41 public repositories. I’m checking scene resources and scripts next, since environment settings can be created in code as well as saved in a `WorldEnvironment` node.


## User

actually, it may have been a WorldEnvironment or an Environment node


## Assistant

I’m checking both—Godot’s `Environment` is a resource that can be assigned to a `WorldEnvironment` node or a camera. So far, the two `SimplePaintShaders` projects explicitly disable ambient and reflected light. The older .NET stutter-test project also has substantial shadow tuning, although it has no `WorldEnvironment`.


## Assistant

Only three current projects contain an `Environment` resource: the standalone `ShaderTest` project has a default one, and both `SimplePaintShaders` projects disable ambient and reflected light. I also found an important distinction in `Veehiicuul`: code expects an environment and defines graphics settings, but its current startup deliberately stops before that code runs.


## User

also, include if the project uses any custom/non-default post-processing config


## Assistant

I’ll include post-processing explicitly. The main finding is debanding enabled in `VsyncStutterTest-Godot-4_4_1-Dotnet`; I found no enabled custom glow, fog, tonemapping, SSAO, SSIL, or SDFGI in the inspected source. The confirmed total is 13 projects across **9** repositories—my earlier repository count was off by one.


## Assistant

**The strongest matches are the two projects in `SimplePaintShaders`.** Both have an `Environment.tres` attached to `WorldEnvironment` nodes, with these overrides:

```ini
ambient_light_source = 1
reflected_light_source = 1
```

These disable ambient light and environment reflections. Open [ShaderTest’s environment](https://github.com/kvu787/SimplePaintShaders/blob/abdf14856696f6437224abf3e20230333a58aae0/Godot/ShaderTest/Environment.tres) or [FixPerspectiveProjection’s environment](https://github.com/kvu787/SimplePaintShaders/blob/abdf14856696f6437224abf3e20230333a58aae0/Godot/FixPerspectiveProjection/Environment.tres).

I examined all **41 public repositories**, finding **13 Godot projects in 9 repositories**. This covers current default branches; it does **not** exhaustively search historical commits or other branches.

| Repository / project                              | WorldEnvironment / Environment                | Custom rendering, lighting, shadows, or post-processing                                                                                |
| ------------------------------------------------- | --------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------- |
| Automatou / SimplePaint3DShell                     | No                                            | DirectX 12; fallbacks disabled; custom background; MSDF fonts and antialiased 2D drawing. No custom post-processing found.                 |
| Automatou / Automatou2                            | No                                            | DirectX 12; 8× 2D MSAA plus custom polygon antialiasing; custom background; VSync off; 60 FPS cap. No custom post-processing found.         |
| Automatou / RectangularGridMotion                 | No                                            | DirectX 12; fallbacks disabled; custom background. No custom lighting/shadows/post-processing found.                                    |
| CurveModeler                                      | No                                            | Compatibility renderer; custom background. No custom lighting/shadows/post-processing found.                                           |
| LowFpsInvestigation / Godot_Minimal3dScene          | No                                            | Rotated directional light; unshaded material; VSync off; two swapchain images; benchmark switches rendering backends. No post-processing. |
| ShaderTest / ShaderTest                           | **Yes, default resource**                     | 8× MSAA; 2× render scale; custom unshaded paint shaders. Environment has no overrides; no custom post-processing found.                   |
| SimplePaintShaders / Godot/FixPerspectiveProjection | **Yes, customized**, used in five scenes      | Ambient/reflected light disabled; DirectX 12; custom perspective-normal shaders disable lighting, shadows and fog. No post-processing.  |
| SimplePaintShaders / Godot/ShaderTest              | **Yes, customized**, used in 16 scenes         | Ambient/reflected light disabled; 8× MSAA; **roughness limiter disabled**; extensive custom paint shaders. No custom Environment effects. |
| UST / UST                                         | No                                            | VSync off; 120 FPS cap. No custom 3D lighting/shadows/post-processing found.                                                             |
| Veehiicuul / InputLatency_Godot                    | No                                            | DirectX 12; light energy 1.2; shadows off; sphere roughness 0.65; custom background; VSync off. No custom post-processing found.           |
| Veehiicuul / Veehiicuul_Godot_CSharp               | No; code expects an Environment               | Test scene light energy 1.5. Code defines MSAA/TAA, render-scale and VSync toggles, but startup stops before initialization.               |
| VsyncStutterTest / Godot                           | No                                            | DirectX 12; rotated directional light; unshaded blue material; VSync off; two swapchain images. No custom post-processing found.           |
| VsyncStutterTest-Godot-4_4_1-Dotnet                | No                                            | **Extensive shadow tuning; debanding on; roughness limiter off; 8× MSAA; 2× render scale.**                                              |

If you remember **shadow settings**, inspect [the older .NET stutter test](https://github.com/kvu787/VsyncStutterTest-Godot-4_4_1-Dotnet/blob/c9ea39a90b30a3390d05996bd6a1182cc688ff09/Scenes/Main.tscn): it enables directional shadows, uses orthogonal shadow mode, sets fade start to `1.0` and pancake size to `0`, and disables specular lighting. Its project settings use a **16384 shadow map** and hard shadow filtering.

No custom glow, tonemapping, fog enablement, SSAO, SSIL, SSR, SDFGI, depth-of-field, auto-exposure, or custom screen-space post-processing shader was found in the inspected source. The paint shaders are material shaders.

Saved the [full audit with exact settings, source links, and all 41 repository snapshots](C:/Users/k/Repository/Veehiicuul/Documentation/PublicGodotProjectAudit.md).
