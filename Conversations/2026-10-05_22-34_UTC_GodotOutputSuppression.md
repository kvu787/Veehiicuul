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
Prefix these files with the PowerShell evaluation of `[DateTime]::UtcNow.ToString("yyyy-MM-dd_HH-mm") + "_UTC"`.
Prefix these commits with `[cnv]`.
If I attach images to prompts, save and record these in the conversation logs.
If the conversation begins with `dnr`, then do not record the conversation.

## Application compatibility

Do not attempt to maintain any sort of application compatibility between different commits of the repo. This creates unwanted complexity.

## Target platform compatibility

Support Windows 11 x64 as the only target platform.

</INSTRUCTIONS>

## User

<external_codex_apps_open_page>{"page_id":null}</external_codex_apps_open_page>

## User

```
c:\Users\k\Repository\Veehiicuul\Veehiicuul_Godot_CSharp\Veehiicuul\MyRun_Simple.ps1:5:1-5:133
Lines and columns are 1-based. Columns are StartInclusive:EndExclusive. Columns count UTF-16 code units.
```
is there a  way to get this godot game process to not write gd.print and other stuff to stdout/stderr

## Assistant

I'll check the launch script and Godot's output options to see how to silence the game process.

## Assistant

Yes. For `GD.Print` and other normal Godot output, add **`--quiet`** to your existing launch line:

```powershell
$process = Start-Process -FilePath $ProcessPath -ArgumentList "--quiet --log-file `"$GodotLogFilePath`"" -PassThru
```

`--quiet` silences stdout, but errors still appear on stderr. [Godot command-line documentation](https://docs.godotengine.org/en/stable/tutorials/editor/command_line_tutorial.html).

To disable **both Godot stdout and stderr**, set these in `project.godot` under its existing `[application]` section, then rebuild/export:

```ini
[application]

run/disable_stdout=true
run/disable_stderr=true
```

These settings suppress Godot printing, including its error and warning messages. They do not control direct writes from other code, such as C# `Console.WriteLine`. [Godot project settings](https://docs.godotengine.org/en/stable/classes/class_projectsettings.html#class-projectsettings-property-application-run-disable-stdout).

## User

```
c:\Users\k\Repository\Veehiicuul\Veehiicuul_Godot_CSharp\Veehiicuul\MyRun_Simple.ps1:35:72-35:102
Lines and columns are 1-based. Columns are StartInclusive:EndExclusive. Columns count UTF-16 code units.
```
```
--log-file `"$GodotLogFilePath
```
is this sufficient to capture all log output that would be omitted if --quiet and run/disable_stdout=true and run/disable_stderr=true are used

## Assistant

I'll check whether Godot's file logger still receives messages when stdout and stderr are disabled.

## Assistant

No. **Those options also suppress messages from Godot's file logger.** `--quiet` / `run/disable_stdout=true` disable normal logging, and `run/disable_stderr=true` disables error logging. The file logger checks the same flags, so `--log-file` cannot recover the suppressed messages. [Godot initialization](https://github.com/godotengine/godot/blob/master/main/main.cpp), [file logger implementation](https://github.com/godotengine/godot/blob/master/core/io/logger.cpp).

To keep the logs while keeping the console quiet, leave both settings `false`, omit `--quiet`, and redirect the process streams to files:

```powershell
$process = Start-Process `
    -FilePath $ProcessPath `
    -ArgumentList "--log-file `"$GodotLogFilePath`"" `
    -RedirectStandardOutput "$LogFolderPath\GodotStandardOutput.log" `
    -RedirectStandardError "$LogFolderPath\GodotStandardError.log" `
    -PassThru
```

This preserves Godot's own log and separately captures stdout/stderr, including direct C# console writes. Some Godot messages will appear in both.

## User

instead of doing this:
    -RedirectStandardOutput "$LogFolderPath\GodotStandardOutput.log" `
    -RedirectStandardError "$LogFolderPath\GodotStandardError.log" `

can they be redirected to null or something that omits them

## Assistant

I'll check whether `Start-Process` can send both streams to Windows' `NUL` device.

## Assistant

Yes. Replace those two lines with:

```powershell
    -RedirectStandardOutput 'NUL' `
    -RedirectStandardError '\\.\NUL' `
```

Both refer to Windows' null device and discard the output. The different spellings avoid `Start-Process` rejecting identical stdout/stderr destinations. I tested this successfully with PowerShell 7.6 and Windows PowerShell 5.1. [Windows device names](https://learn.microsoft.com/en-us/windows/win32/fileio/naming-a-file).

Keep `--log-file`, omit `--quiet`, and leave `run/disable_stdout` and `run/disable_stderr` set to `false` so Godot can still write its log file.
