# Godot project review conversation

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

</INSTRUCTIONS><environment_context>
  <cwd>C:\Users\k\Repository\Veehiicuul</cwd>
  <shell>powershell</shell>
  <current_date>2026-10-03</current_date>
  <timezone>America/Los_Angeles</timezone>
  <filesystem><workspace_roots><root>C:\Users\k\Repository\Veehiicuul</root><root>C:\Users\k\.codex\visualizations\2026\10\03\01a103ff-f58e-7bc1-b91c-f079418a26a9</root></workspace_roots><permission_profile type="managed"><file_system type="restricted"><entry access="read"><special>:root</special></entry><entry access="write"><path>C:\Users\k\Repository\Veehiicuul</path></entry><entry access="write"><path>C:\Users\k\.codex\visualizations\2026\10\03\01a103ff-f58e-7bc1-b91c-f079418a26a9</path></entry><entry access="write"><special>:slash_tmp</special></entry><entry access="write"><special>:tmpdir</special></entry><entry access="read"><path>C:\Users\k\Repository\Veehiicuul\.git</path></entry><entry access="read"><path>C:\Users\k\.codex\visualizations\2026\10\03\01a103ff-f58e-7bc1-b91c-f079418a26a9\.git</path></entry><entry access="read"><path>C:\Users\k\Repository\Veehiicuul\.agents</path></entry><entry access="read"><path>C:\Users\k\.codex\visualizations\2026\10\03\01a103ff-f58e-7bc1-b91c-f079418a26a9\.agents</path></entry><entry access="read"><path>C:\Users\k\Repository\Veehiicuul\.codex</path></entry><entry access="read"><path>C:\Users\k\.codex\visualizations\2026\10\03\01a103ff-f58e-7bc1-b91c-f079418a26a9\.codex</path></entry><entry access="read"><path>C:\Users\k\Repository\Veehiicuul\.aws</path></entry><entry access="read"><path>C:\Users\k\.codex\visualizations\2026\10\03\01a103ff-f58e-7bc1-b91c-f079418a26a9\.aws</path></entry></file_system></permission_profile></filesystem>
</environment_context>

## User

<external_codex_apps_open_page>{"page_id":null}</external_codex_apps_open_page>

## User

do a comprehensive review of C:\Users\k\Repository\Veehiicuul\Veehiicuul_Godot_CSharp

## Assistant Commentary

I'll inspect the project, its build and run scripts, and the main gameplay code, then check for concrete correctness and configuration issues. I'll also record this conversation as required by the repository instructions.

## Assistant Commentary

The release C# build passes with zero warnings; the export is still running. I'm tracing the frame update order and collision code next, particularly whether a car can cross a barrier between collision checks.

## Assistant Commentary

The build and export both pass. The collision code explicitly permits tunneling and uses perimeter contact, so those are intended behavior. I found a launcher mismatch: Build.cmd writes to `Build`, while both MyRun commands launch `MyBuildOutput`, which can select an older export. I'm checking input behavior, material importing, and capture cleanup next.

## Assistant Commentary

The exported application starts and exits cleanly in a headless smoke run. I'm now using a temporary copy to test controller input and track initialization with the real Godot engine. The source also shows that MyRun leaves PresentMon running after the game exits, which can prevent the next capture from starting.

## Assistant Commentary

The engine tests reproduced the controller mismatch: a controller with ID 7 can switch cars, but its stick and brake inputs are ignored. A fresh import also left all 28 materials without the callback's settings. Movement, braking, camera toggling, and repeated track reloads passed, as did 30,000 collision index comparisons. I'm consolidating the findings and committing the conversation log.

## Assistant Final

Found six issues and reconfirmed two previously reported footprint defects. No P0/P1 findings.

::code-comment{title="[P2] MyRun launches a different output than Build.cmd creates" body="Build.cmd exports to Veehiicuul/Build, but both MyRun commands launch Veehiicuul/MyBuildOutput. After a successful build, MyRun_NoPresentMon.cmd failed because that executable was absent. If an older manual export exists there, these launchers instead run stale code. Use the same build output and completeness checks as Run.ps1." file="C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/MyRun.ps1" start=5 priority=2}

::code-comment{title="[P2] Analog controls ignore controllers with a nonzero device ID" body="Digital actions accept every controller, while acceleration, braking, and zoom poll device 0. The engine fixture reproduced device 7 switching cars while its stick and brake inputs remained zero in InputManager. Select an active controller ID and use it consistently for analog controls." file="C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Source/GameDataAndLogic/Input/InputManager.cs" start=48 end=49 priority=2}

::code-comment{title="[P2] The material import callback is disconnected" body="The import script path is empty, so DisableSpecularImport.gd never applies its material settings. A fresh import produced 28 materials with none using Lambert, disabled specular, or disabled fog. Register the callback and reimport to apply the documented material policy." file="C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Tracks/Ribeye/Ribeye_Model.glb.import" start=38 priority=2}

::code-comment{title="[P2] PresentMon outlives the game and blocks subsequent captures" body="The capture is launched without terminate_on_proc_exit, and its process handle is discarded. Waiting for the game does not stop PresentMon; the next capture can fail because its default ETW session is still active. Add process-exit termination and own the capture lifecycle, including cleanup when game launch fails." file="C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/MyRun.ps1" start=14 end=18 priority=2}

::code-comment{title="[P2] Wall-clock changes can reactivate the car control timeout" body="DateTime.Now makes this timeout sensitive to clock corrections and daylight-saving changes. A backwards adjustment after a reset can disable driving until the clock catches up, even if the original timeout already expired. The fixture confirmed the resulting timeout state. Use Stopwatch timestamps or elapsed frame time." file="C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Source/Main.cs" start=122 end=123 priority=2}

::code-comment{title="[P3] The documented generator command uses obsolete paths" body="The script is now Tools/GenerateDigitalInputMap.py, and its output belongs in Source/GameDataAndLogic/Input/DigitalInputMap.cs. The documented command references a missing script directory and the old output location. Updating both paths restores the regeneration workflow; the generator itself matches the checked-in source." file="C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/README.md" start=100 end=101 priority=3}

::code-comment{title="[P2] Reflected scale loses the collider reflection" body="This previously reported defect remains: basis-vector lengths erase scale signs. A reflected mesh occupying X [-4, -2] produced collision bounds [2, 4]. Reject reflected roots under the positive-scale contract or preserve the reflection. The six current cars are unaffected." file="C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Source/GameDataAndLogic/CollisionDetection/VehicleCollisionFootprint.cs" start=54 end=56 priority=2}

::code-comment{title="[P2] Footprint traversal ignores transform boundaries" body="This previously reported defect remains: traversal composes transforms across ordinary Node parents and TopLevel boundaries where Godot does not inherit them. The ordinary-Node fixture occupied local X [-18, -16], while the footprint returned [2, 4]. Follow Godot transform relationships or reject unsupported hierarchies. Current car meshes have no children." file="C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Source/GameDataAndLogic/CollisionDetection/VehicleCollisionFootprint.cs" start=89 end=90 priority=2}

Validation passed: release build/export with zero warnings, exported headless startup, movement/braking/camera tests, repeated track reloads, 30,000 indexed-versus-direct collision comparisons, PowerShell syntax, and generator consistency. [Test output](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/MyLogOutput/2026-10-03_16-02-06/ReviewRuntime/Runtime.log).

Application source is unchanged. The conversation is committed separately. Graphics presentation and NVIDIA profile behavior were not exercised.
