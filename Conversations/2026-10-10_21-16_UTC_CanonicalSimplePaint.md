# Canonical SimplePaint conversation

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
When it makes sense, split work into multiple commits to make it easier to review.
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
  <current_date>2026-10-10</current_date>
  <timezone>America/Los_Angeles</timezone>
  <filesystem><workspace_roots><root>C:\Users\k\Repository\Veehiicuul</root><root>C:\Users\k\.codex\visualizations\2026\10\10\01a127a6-b88b-7a12-aab3-75046c9deb4e</root></workspace_roots><permission_profile type="managed"><file_system type="restricted"><entry access="read"><special>:root</special></entry><entry access="write"><path>C:\Users\k\Repository\Veehiicuul</path></entry><entry access="write"><path>C:\Users\k\.codex\visualizations\2026\10\10\01a127a6-b88b-7a12-aab3-75046c9deb4e</path></entry><entry access="write"><special>:slash_tmp</special></entry><entry access="write"><special>:tmpdir</special></entry><entry access="read"><path>C:\Users\k\Repository\Veehiicuul\.git</path></entry><entry access="read"><path>C:\Users\k\.codex\visualizations\2026\10\10\01a127a6-b88b-7a12-aab3-75046c9deb4e\.git</path></entry><entry access="read"><path>C:\Users\k\Repository\Veehiicuul\.agents</path></entry><entry access="read"><path>C:\Users\k\.codex\visualizations\2026\10\10\01a127a6-b88b-7a12-aab3-75046c9deb4e\.agents</path></entry><entry access="read"><path>C:\Users\k\Repository\Veehiicuul\.codex</path></entry><entry access="read"><path>C:\Users\k\.codex\visualizations\2026\10\10\01a127a6-b88b-7a12-aab3-75046c9deb4e\.codex</path></entry><entry access="read"><path>C:\Users\k\Repository\Veehiicuul\.aws</path></entry><entry access="read"><path>C:\Users\k\.codex\visualizations\2026\10\10\01a127a6-b88b-7a12-aab3-75046c9deb4e\.aws</path></entry></file_system></permission_profile></filesystem>
</environment_context>

## User

<external_codex_apps_open_page>{"page_id":null}</external_codex_apps_open_page>

## User

i will now treat "C:\Users\k\Repository\Veehiicuul\3dTestScene_CppDx12\Source\SimplePaint" as the canonical specification and implementation of the SimplePaint shader. review everything in that folder recursively and make any changes to suit this treatment. for example, it shouldn't refer to any godot or previous implementations.


## Assistant

I'll review the entire SimplePaint folder as the canonical shader source, remove references to earlier implementations, and check that its documentation and code agree.


## Assistant

The folder includes historical reports about rejected parameter ranges and earlier shaders. I'll move those reports into the application's documentation archive, make the specification self-contained, and update test descriptions to refer to SimplePaint's own equations.


## Assistant

The shader math and material validation agree with the documented contract. I found outdated references in shader comments, test labels, and integration notes. I'll preserve the equations and keep the canonical docs focused on the reusable module's behavior and verification.


## Assistant

The canonical docs now define the contract without Godot or prior implementation references. The reports and reproduction data are archived outside the module, and the GPU tests now probe the specified silhouette boundary. I'll run the Release build and full suite, including the copied-module check.


## Assistant

The Release build and application integration checks have passed so far. The copied-module test is still running; it verifies that SimplePaint builds and tests successfully without the archived reports or host sources. The documentation checks also found no stale references or broken local links.


## Assistant

The shader's CPU and both GPU checks passed. The copied-module check failed during CMake's compiler probe with an MSVC program-database manager mismatch, before compiling SimplePaint. I'll investigate the build environment and rerun that check.


## Assistant

All 13 Release tests passed outside the sandbox, including the fresh copied-module test and hardware/WARP GPU checks. The compiler-probe failure was environmental. I'll commit the canonical-module changes and the conversation record separately.


## Assistant

Reviewed the folder recursively and made [SimplePaint](C:/Users/k/Repository/Veehiicuul/3dTestScene_CppDx12/Source/SimplePaint/README.md) canonical.

- Removed Godot and earlier-implementation references from docs, comments, and tests.
- Moved historical reports to [the documentation archive](C:/Users/k/Repository/Veehiicuul/3dTestScene_CppDx12/Documentation/Reports/SimplePaint/README.md).
- Made the specification and integration guide self-contained.

All 13 Release tests passed, including hardware/WARP GPU and standalone-copy checks. Shader behavior is unchanged. Changes and conversation were committed separately.

## User

"C:\Users\k\Repository\Veehiicuul\3dTestScene_CppDx12\Source\SimplePaint" shouldn't call itself canonical. I am saying that it is canon in the context of the development history of the SimplePaint shader. any specification or implementation of SimplePaint doesn't need to discuss this context.

i made a commit that removes this "canon" from "C:\Users\k\Repository\Veehiicuul\3dTestScene_CppDx12\Source\SimplePaint". review "C:\Users\k\Repository\Veehiicuul\3dTestScene_CppDx12\Source\SimplePaint" to see if any further changes are needed.


## Assistant

I'll review your commit and the full SimplePaint folder for remaining references to its place in development history.


## Assistant

I found a remaining "canonical contract" comment in Material.h and the same label in the application's README. I'll remove those and simplify the angle wording too. The rest describes the shader's behavior without discussing its place in development history.


## Assistant

Removed the remaining 'canonical contract' comment in [Material.h](C:/Users/k/Repository/Veehiicuul/3dTestScene_CppDx12/Source/SimplePaint/Material.h:11), simplified the angle wording, and fixed the application's README label.

No further changes were needed. Documentation checks passed. Only docs/comments changed, so runtime tests weren't rerun.

Committed as `4ff0399`; conversation recorded separately.
