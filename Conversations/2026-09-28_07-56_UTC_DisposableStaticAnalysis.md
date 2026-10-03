# Disposable Static Analysis

## User (Repository Instructions and Environment)

````text
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
  <current_date>2026-09-28</current_date>
  <timezone>America/Los_Angeles</timezone>
  <filesystem><workspace_roots><root>C:\Users\k\Repository\Veehiicuul</root><root>C:\Users\k\.codex\visualizations\2026\09\28\01a0e702-5bf7-7f90-9b7d-3bc98f634d47</root></workspace_roots><permission_profile type="managed"><file_system type="restricted"><entry access="read"><special>:root</special></entry><entry access="write"><path>C:\Users\k\Repository\Veehiicuul</path></entry><entry access="write"><path>C:\Users\k\.codex\visualizations\2026\09\28\01a0e702-5bf7-7f90-9b7d-3bc98f634d47</path></entry><entry access="write"><special>:slash_tmp</special></entry><entry access="write"><special>:tmpdir</special></entry><entry access="read"><path>C:\Users\k\Repository\Veehiicuul\.git</path></entry><entry access="read"><path>C:\Users\k\.codex\visualizations\2026\09\28\01a0e702-5bf7-7f90-9b7d-3bc98f634d47\.git</path></entry><entry access="read"><path>C:\Users\k\Repository\Veehiicuul\.agents</path></entry><entry access="read"><path>C:\Users\k\.codex\visualizations\2026\09\28\01a0e702-5bf7-7f90-9b7d-3bc98f634d47\.agents</path></entry><entry access="read"><path>C:\Users\k\Repository\Veehiicuul\.codex</path></entry><entry access="read"><path>C:\Users\k\.codex\visualizations\2026\09\28\01a0e702-5bf7-7f90-9b7d-3bc98f634d47\.codex</path></entry></file_system></permission_profile></filesystem>
</environment_context>
````

## User

does c# or visual studio provide any static analysis to ensure that idisposables are disposed properly

## Assistant (Commentary)

I’ll check Microsoft’s documentation for the available disposal analyzers and their limits.

## Assistant (Commentary)

The main disposal rules are disabled by default in .NET 10. Your repository’s general “analyzers as errors” setting doesn’t enable them; each rule needs an explicit entry. These checks can catch missed disposal, but they can’t prove every object’s lifetime is correct.

## Assistant

Yes. **.NET provides Roslyn analyzers that run in Visual Studio and during builds.** They can detect missed disposal, although they cannot guarantee that every `IDisposable` is handled correctly. [Microsoft’s analyzer overview](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/overview)

The main rules are:

- **[CA2000](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca2000):** Detects locally created disposable objects that leave scope without being disposed.
- **[CA2213](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca2213):** Detects disposable fields that their owning disposable type fails to dispose.
- **[CA1001](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca1001):** Detects classes that own disposable fields but don’t implement a disposal interface.

**All three are disabled by default in .NET 10.** To enable them as build errors, add these entries under `[*.cs]` in your [.editorconfig](C:/Users/k/Repository/Veehiicuul/.editorconfig):

```ini
dotnet_diagnostic.CA2000.severity = error
dotnet_diagnostic.CA2213.severity = error
dotnet_diagnostic.CA1001.severity = error
```

Your existing `dotnet_analyzer_diagnostic.severity = error` only changes the severity of already-enabled rules; it doesn’t activate these disabled rules. [Configuration documentation](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/configuration-options#scope)

The main limitation is **ownership tracking**: determining which object or caller must dispose a resource. For example, CA2213 generally skips fields whose objects were created outside the containing type, and its search through helper methods is bounded. Consequently, a clean analyzer result is useful evidence, but not proof that disposal is correct. [CA2213 limitations](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca2213)
