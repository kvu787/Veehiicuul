**Godot persistence audit — conversation**

**User — repository instructions and supplied environment**

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
  <current_date>2026-09-25</current_date>
  <timezone>America/Los_Angeles</timezone>
  <filesystem><workspace_roots><root>C:\Users\k\Repository\Veehiicuul</root><root>C:\Users\k\.codex\visualizations\2026\09\26\01a0db65-3e8b-7700-834b-e3417c011492</root></workspace_roots><permission_profile type="managed"><file_system type="restricted"><entry access="read"><special>:root</special></entry><entry access="write"><path>C:\Users\k\Repository\Veehiicuul</path></entry><entry access="write"><path>C:\Users\k\.codex\visualizations\2026\09\26\01a0db65-3e8b-7700-834b-e3417c011492</path></entry><entry access="write"><special>:slash_tmp</special></entry><entry access="write"><special>:tmpdir</special></entry><entry access="read"><path>C:\Users\k\Repository\Veehiicuul\.git</path></entry><entry access="read"><path>C:\Users\k\.codex\visualizations\2026\09\26\01a0db65-3e8b-7700-834b-e3417c011492\.git</path></entry><entry access="read"><path>C:\Users\k\Repository\Veehiicuul\.agents</path></entry><entry access="read"><path>C:\Users\k\.codex\visualizations\2026\09\26\01a0db65-3e8b-7700-834b-e3417c011492\.agents</path></entry><entry access="read"><path>C:\Users\k\Repository\Veehiicuul\.codex</path></entry><entry access="read"><path>C:\Users\k\.codex\visualizations\2026\09\26\01a0db65-3e8b-7700-834b-e3417c011492\.codex</path></entry></file_system></permission_profile></filesystem>
</environment_context>

**User**

i want a complete understanding of the files/data that self-contained godot 4.7.2 .NET persists other than the files/data that i know about.

this includes, but is not limited to, these:
- C:\Users\k\Repository\Veehiicuul\Veehiicuul_Godot_CSharp\Veehiicuul\.godot\
- C:\Users\k\Program\Godot_v4.7.2-stable_mono_win64\editor_data\
- C:\Users\k\AppData\Local\Temp\Godot\
- C:\Users\k\AppData\Roaming\Godot\
- C:\Users\k\AppData\Local\Godot\

this excludes these files that i know about:
- files committed to the repo of a godot project
- files included in a fresh download of godot 4.7.2 .net and its export templates, which are here: "C:\Users\k\Program\Godot 4.7.2 .NET default files - DO NOT EDIT"

use the actual source code at "C:\Users\k\Repository\External\Godot_4-7-2" to inform and support all parts of your answer.

**Assistant — commentary**

I’ll trace the Godot source and compare it with the portable installation and project folders. I’ll distinguish Godot’s own writes from .NET tooling and application-defined data, and cite the code behind each finding.

**Assistant — commentary**

The source is a clean `4.7.2-stable` checkout. The first key distinction is that `_sc_` redirects editor storage, while project runtime data follows a separate path. Your Roaming `Godot` folder currently contains Direct3D 12 shader caches for Veehiicuul, which I’ll trace separately from the editor caches.

**Assistant — commentary**

I found persistence outside all five listed folders: .NET exports use `%TEMP%\godot-publish-dotnet`, Windows drag-and-drop can create `%TEMP%\godot_tmp_*`, and exported Windows applications explicitly write a friendly-name value to the registry. I’m checking the triggers and cleanup behavior for each so the report separates routine writes from conditional ones.

**Assistant — commentary**

The installed editor and template files all match the fresh-download baseline by SHA-256. That lets me exclude them precisely. I’m also separating replaceable caches from saved state: `.godot` can contain export credentials and editor preferences, so deleting the whole folder is more than a cache reset.

**Assistant — commentary**

The remaining details matter for cleanup: editor layout and history files are saved state, while imported assets and shader files are regenerated. I also confirmed that this version’s Direct3D 12 backend does not write Godot’s Vulkan pipeline-cache files, although the shared renderer code can still create an empty `user://vulkan` directory.

**Assistant — commentary**

The disk check found seven crash dumps named for Godot or your exported applications, totaling about 102.9 MiB, under `%LOCALAPPDATA%\CrashDumps`. I’m recording these as Windows/debugger-managed artifacts: Godot’s crash-handler source prints backtraces, but it does not establish who created those dump files. The report will include this attribution limit alongside the confirmed Godot writes.

**Assistant — commentary**

The draft report now covers the storage paths, what each file family contains, when it is written, and what survives cleanup. I’m checking the source links and snapshot totals before committing the report and the required conversation record.

**Assistant — final**

**Self-contained Godot does not keep all persistent data inside its installation.** The `_sc_` marker redirects editor storage; project data, runtime data, several temporary-file paths, external .NET tooling, and Windows-managed data follow separate rules.

I documented the findings in the [full persistence audit](C:/Users/k/Repository/Veehiicuul/Documentation/GodotPersistence.md), with **97 references into your actual 4.7.2 source checkout**. The [file inventory](C:/Users/k/Repository/Veehiicuul/Documentation/GodotPersistenceInventory.json) records filenames, directories, sizes, and baseline comparisons. The report covers contents, creation triggers, cleanup behavior, relocation settings, and what would be lost by deletion.

The main findings are:

- **`.godot` contains more than caches:** imported assets, C# build output, shader caches, editor layouts/history, and potentially `export_credentials.cfg`. Deleting everything also loses saved editor state and any stored export credentials. [Credential writer](C:/Users/k/Repository/External/Godot_4-7-2/editor/export/editor_export.cpp:79)
- **`editor_data` contains shared editor settings, project lists, thumbnails, documentation caches, customizations, build logs, and temporary staging.** Its newly added data totals **5.89 MiB**, after excluding your unchanged templates. [Path routing](C:/Users/k/Repository/External/Godot_4-7-2/editor/file_system/editor_paths.cpp:128)
- **Roaming `Godot\app_userdata` remains active in portable mode.** Your current files there are Veehiicuul D3D12 shader caches. Built-in writers can also store game logs, editor screenshots, startup recovery locks, and ObjectDB snapshots there. [Runtime path routing](C:/Users/k/Repository/External/Godot_4-7-2/core/os/os.cpp:338)
- **`%TEMP%\Godot` is not a universal temporary directory in this version.** Both it and `%LOCALAPPDATA%\Godot` are currently absent. Windows .NET exports instead stage files in `%TEMP%\godot-publish-dotnet`; that parent currently exists but is empty. [Publish staging](C:/Users/k/Repository/External/Godot_4-7-2/modules/mono/editor/GodotTools/GodotTools/Export/ExportPlugin.cs:265)

Beyond your five locations, the report traces these additional categories:

- Virtual drag-and-drop files under `%TEMP%\godot_tmp_*`, random C# translation temporary files, and generic temporary API outputs.
- Conditional extracted .NET exports under `%LOCALAPPDATA%\data_<project>_windows_x86_64`. Your current export disables embedding, so it uses the adjacent build-output folder.
- An IDE messaging log beside its helper assembly, temporary native-extension DLL/PDB copies, and safe-save files beside their targets.
- **An explicit registry write by exported Windows applications:** `MuiCache\<executable path>.FriendlyAppName`. Matching entries exist for your applications. [Registry writer](C:/Users/k/Repository/External/Godot_4-7-2/platform/windows/display_server_windows.cpp:7993)
- Shared NuGet/.NET/MSBuild storage, plus your repository’s generated `Build` and `MyLogOutput` files. Godot invokes the external tooling; its shared storage follows separate rules. [Godot process launch](C:/Users/k/Repository/External/Godot_4-7-2/modules/mono/editor/GodotTools/GodotTools/Build/BuildSystem.cs:98), [NuGet storage documentation](https://learn.microsoft.com/en-us/nuget/consume-packages/managing-the-global-packages-and-cache-folders)
- **Seven existing Godot/application-named crash dumps totaling 102.9 MiB** under `%LOCALAPPDATA%\CrashDumps`. These are separately attributed diagnostic artifacts; their exact producer was not established.

All **109 installed files with fresh-download counterparts matched by SHA-256**. No matching stock file had changed.

This is a source audit and filesystem snapshot, **not a trace of every write during execution**. The report explicitly distinguishes confirmed Godot writers from external-tool and Windows behavior, whose complete persistence cannot be established from Godot’s source alone.
