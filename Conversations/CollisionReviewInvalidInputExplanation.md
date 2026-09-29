# Collision Review Invalid Input Explanation

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
  <filesystem><workspace_roots><root>C:\Users\k\Repository\Veehiicuul</root><root>C:\Users\k\.codex\visualizations\2026\09\29\01a0ef80-8a98-7961-865a-ea9b6ccf1044</root></workspace_roots><permission_profile type="managed"><file_system type="restricted"><entry access="read"><special>:root</special></entry><entry access="write"><path>C:\Users\k\Repository\Veehiicuul</path></entry><entry access="write"><path>C:\Users\k\.codex\visualizations\2026\09\29\01a0ef80-8a98-7961-865a-ea9b6ccf1044</path></entry><entry access="write"><special>:slash_tmp</special></entry><entry access="write"><special>:tmpdir</special></entry><entry access="read"><path>C:\Users\k\Repository\Veehiicuul\.git</path></entry><entry access="read"><path>C:\Users\k\.codex\visualizations\2026\09\29\01a0ef80-8a98-7961-865a-ea9b6ccf1044\.git</path></entry><entry access="read"><path>C:\Users\k\Repository\Veehiicuul\.agents</path></entry><entry access="read"><path>C:\Users\k\.codex\visualizations\2026\09\29\01a0ef80-8a98-7961-865a-ea9b6ccf1044\.agents</path></entry><entry access="read"><path>C:\Users\k\Repository\Veehiicuul\.codex</path></entry><entry access="read"><path>C:\Users\k\.codex\visualizations\2026\09\29\01a0ef80-8a98-7961-865a-ea9b6ccf1044\.codex</path></entry></file_system></permission_profile></filesystem>
</environment_context>

## User

```
c:\Users\k\Repository\Veehiicuul\Veehiicuul_Godot_CSharp\Veehiicuul\Documentation\CollisionReviewFromScratch20260929\Report.md:502:1-525:24
Lines and columns are 1-based. Columns are StartInclusive:EndExclusive. Columns count UTF-16 code units.
```
```
### 8. Invalid input

Every invalid input that was tried is refused with an `ArgumentException` or a
type derived from it: 38 cases of construction and 10 of queries. They cover
missing, empty, and null outlines; fewer than three vertices; repeated
vertices, including positive and negative zero; coordinates, bounds, poses, and
cell sizes that are not finite; bounds without extent; and rectangles whose
corners overflow binary32. Both index families refuse the same queries.

Extreme valid input is answered exactly in 59 cases: coordinates from the
smallest subnormal values to the largest finite one, yaw up to the largest
finite value, positions beyond 1e12, and index footprints from 0.002 to 2,000
wide.

Two messages name a cause that is not the cause:

| Input                                    | Message                                        | Cause                                             |
| ---------------------------------------- | ---------------------------------------------- | ------------------------------------------------- |
| Collider text whose keys are `x` and `y` | `Outline 0 contains a zero-length segment.`    | The keys are not `X` and `Y`; every vertex is 0,0 |
| A manager without any vehicle            | `The value must be finite. (Parameter 'minX')` | The list of vehicles is empty                     |

The first row used `System.Text.Json` with its default options. The
application's own reader was replaced by a stand-in in this review and may
treat keys differently.
```


explain what this is

## Assistant Commentary

I’ll read the surrounding report and the relevant code so I can explain what was tested and what the two misleading messages mean.

## Assistant Commentary

The excerpt is a validation summary. The tested bad inputs were rejected, and the two examples show exceptions whose wording points to the wrong cause. I also checked the application reader: it keeps JSON property names case sensitive, so the lowercase-key example appears relevant to the app too. I’ll explain the test’s limit along with that finding.

## Assistant

This is a summary of **input-validation tests** for the collision detector. The path and `502:1-525:24` identify the selected lines and columns in [Report.md](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Documentation/CollisionReviewFromScratch20260929/Report.md:502); they are not an error message.

- The reviewer tried 38 invalid inputs while constructing the detector and 10 during collision queries. All were rejected with `ArgumentException` or a subclass. The 59 extreme but valid cases matched the test’s independent geometry calculation. Those counts describe the cases tested, not every possible input.
- The table identifies **misleading exception messages**. Lowercase JSON keys `x` and `y` leave the expected `X` and `Y` values at zero, so the detector reports a zero-length edge. An empty vehicle list leaves the manager’s `minX` bound at infinity, so it reports a nonfinite bound. In both cases, the message describes a downstream symptom rather than the input mistake.

The JSON example used a test stand-in. The application’s [JsonUtility.cs](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Source/Utility/JsonUtility.cs:9) also leaves property-name matching case sensitive, so its code suggests the same lowercase-key behavior; that exact application path was not tested in the review.

The practical finding is that these inputs are rejected, but the two messages should identify the invalid JSON keys or the empty vehicle list.
