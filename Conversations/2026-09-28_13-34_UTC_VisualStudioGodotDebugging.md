# Visual Studio Godot Debugging

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

</INSTRUCTIONS><environment_context>
  <cwd>C:\Users\k\Repository\Veehiicuul</cwd>
  <shell>powershell</shell>
  <current_date>2026-09-28</current_date>
  <timezone>America/Los_Angeles</timezone>
  <filesystem><workspace_roots><root>C:\Users\k\Repository\Veehiicuul</root><root>C:\Users\k\.codex\visualizations\2026\09\28\01a0e835-37b7-72b2-9122-bd4aad2d7d9e</root></workspace_roots><permission_profile type="managed"><file_system type="restricted"><entry access="read"><special>:root</special></entry><entry access="write"><path>C:\Users\k\Repository\Veehiicuul</path></entry><entry access="write"><path>C:\Users\k\.codex\visualizations\2026\09\28\01a0e835-37b7-72b2-9122-bd4aad2d7d9e</path></entry><entry access="write"><special>:slash_tmp</special></entry><entry access="write"><special>:tmpdir</special></entry><entry access="read"><path>C:\Users\k\Repository\Veehiicuul\.git</path></entry><entry access="read"><path>C:\Users\k\.codex\visualizations\2026\09\28\01a0e835-37b7-72b2-9122-bd4aad2d7d9e\.git</path></entry><entry access="read"><path>C:\Users\k\Repository\Veehiicuul\.agents</path></entry><entry access="read"><path>C:\Users\k\.codex\visualizations\2026\09\28\01a0e835-37b7-72b2-9122-bd4aad2d7d9e\.agents</path></entry><entry access="read"><path>C:\Users\k\Repository\Veehiicuul\.codex</path></entry><entry access="read"><path>C:\Users\k\.codex\visualizations\2026\09\28\01a0e835-37b7-72b2-9122-bd4aad2d7d9e\.codex</path></entry></file_system></permission_profile></filesystem>
</environment_context>
````

## User

for a godot 4.7.2 .net project, provide a step-by-step guide for debugging c# code with visual studio 2026 on windows x64

## Assistant Commentary

I’ll check the Godot and Visual Studio documentation, then put together a Windows x64 debugging guide and record this conversation as required by the repository instructions.

## Assistant Commentary

Godot documents an “Executable” debug profile that launches the game from Visual Studio. I also found the expected Godot installation and your project’s `.slnx` file, so I can include the exact paths for this repository.

## Assistant Commentary

Two settings matter here: build the C# project in `Debug`, and launch Godot’s main executable—the console wrapper is unsuitable for this profile. I’m also including how to break when a C# exception is thrown, since Godot can catch and log exceptions before they reach Visual Studio’s default unhandled-exception break.

## Assistant Commentary

The guide is ready, including a breakpoint example from your current startup code and troubleshooting for missing symbols. I’m saving and committing the conversation record now.

## Assistant

Use Visual Studio’s **Executable launch profile** to start Godot with the debugger attached. This supports C# breakpoints, stepping, and variable inspection without a Godot-specific Visual Studio extension.

The paths below match your current repository and Godot installation.

1. **Install the Visual Studio components.**

   Open **Visual Studio Installer → Visual Studio 2026 → Modify**. Select **.NET desktop development** and ensure the **.NET 10 SDK for x64** is installed. Your project targets `net10.0`, which Visual Studio 2026 supports. [Microsoft compatibility documentation](https://learn.microsoft.com/en-us/visualstudio/releases/2026/compatibility)

2. **Open the project in Godot and Visual Studio.**

   Open your project in **Godot 4.7.2 .NET**, let asset importing finish, and ensure a main scene is assigned.

   In Visual Studio, choose **File → Open → Project/Solution** and open your [.slnx solution](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Veehiicuul_Godot_CSharp.slnx). For another project, open its `.sln` or `.slnx` file.

   Optionally, set Godot’s **Editor → Editor Settings → Dotnet → Editor → External Editor** to **Visual Studio** so opening C# scripts uses Visual Studio.

3. **Select the Debug configuration and build.**

   In Visual Studio’s toolbar, select **Debug**. Then choose **Build → Build Solution** and resolve any build errors.

   Use Visual Studio’s Debug build for this workflow; the repository’s `Build.cmd` is intended for release exports.

   **Any CPU is fine for the C# library:** it runs inside the x64 Godot process. You do not need to create an x64 solution configuration just to debug it. [Microsoft’s platform-target explanation](https://learn.microsoft.com/en-gb/dotnet/csharp/language-reference/compiler-options/output)

4. **Create an Executable launch profile.**

   In **Solution Explorer**, right-click the C# project and select **Set as Startup Project**.

   Open **Debug → Veehiicuul_Godot_CSharp Debug Properties**. Alternatively, right-click the project → **Properties → Debug → Open debug launch profiles UI**.

   Click **Create a new profile**, choose **Executable**, and name it `GodotDebug`. [Visual Studio launch-profile settings](https://learn.microsoft.com/en-us/visualstudio/debugger/project-settings-for-csharp-debug-configurations-dotnetcore?view=visualstudio)

5. **Enter these launch settings.**

   | Setting                      | Value                                                                                            |
   | ---------------------------- | ------------------------------------------------------------------------------------------------ |
   | Executable                   | `C:\Users\k\Program\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64.exe`           |
   | Working directory            | `C:\Users\k\Repository\Veehiicuul\Veehiicuul_Godot_CSharp\Veehiicuul`                            |
   | Command-line arguments       | `--path "."`                                                                                     |
   | Enable native code debugging | Checked                                                                                          |

   The working directory must contain `project.godot`. Use the main executable shown above; Godot’s documentation specifically excludes the `_console.exe` wrapper for this profile and instructs enabling native code debugging. The checkbox may be labelled **Enable unmanaged code debugging**. [Godot’s Visual Studio instructions](https://docs.godotengine.org/en/4.7/tutorials/scripting/c_sharp/c_sharp_basics.html#visual-studio-windows-only)

   These arguments run the game. Adding `--editor` would launch the editor instead.

6. **Set a breakpoint and start debugging.**

   Open [Main_GodotAdapter.cs](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Source/Main_GodotAdapter.cs:10). Put the cursor on this statement inside `_Ready()` and press **F9**:

   ```csharp
   this.Main = new Main(mainNode: this);
   ```

   For another project, choose an executable statement in a script’s `_Ready()` method attached to a node in the main scene.

   Close the launch-profile window, select **GodotDebug** beside the green Start button, and press **F5 in Visual Studio**. Execution should pause before the breakpoint’s statement runs.

7. **Step through code and inspect values.**

   | Shortcut     | Action                             |
   | ------------ | ---------------------------------- |
   | F10          | Step over the current statement    |
   | F11          | Step into a method call            |
   | Shift + F11  | Run until the current method exits |
   | F5           | Continue execution                 |
   | Shift + F5   | Stop debugging                     |

   Hover over variables to inspect them. Under **Debug → Windows**, use **Locals**, **Watch → Watch 1**, and **Call Stack** to examine state and how execution reached the current line. [Microsoft’s debugging guide](https://learn.microsoft.com/visualstudio/debugger/debugger-feature-tour)

8. **Enable breaking when C# exceptions are thrown.**

   Open **Debug → Windows → Exception Settings**. Check **Common Language Runtime Exceptions**, or expand it and check specific exception types.

   This pauses at the original throw even if a handler later catches the exception—useful because Godot can catch and log C# callback exceptions. [Visual Studio exception settings](https://learn.microsoft.com/en-us/visualstudio/debugger/managing-exceptions-with-the-debugger?view=visualstudio)

   For subsequent edits, stop debugging, save, rebuild, and press **F5** again.

If a breakpoint remains hollow while the game runs, hover over it to read the warning. Open **Debug → Windows → Modules**, find `Veehiicuul_Godot_CSharp.dll`, and check its path and **Symbol Status**. This project’s Debug assembly should come from `.godot\mono\temp\bin\Debug` beneath the project directory. Rebuild if the loaded assembly or its matching PDB is stale. [Microsoft’s breakpoint troubleshooting](https://learn.microsoft.com/sr-latn-rs/troubleshoot/developer/visualstudio/debuggers/troubleshooting-breakpoints)

You can also attach to a game already running from Godot: choose **Debug → Attach to Process**, set the connection to **Local**, select the **game’s Godot process**, and ensure the code type includes managed **.NET Core / .NET 5+** debugging. Use the process’s command line to distinguish it from the editor. Attaching after startup will miss code that has already executed, including `_Ready()`. [Attach-to-process instructions](https://learn.microsoft.com/en-us/visualstudio/debugger/attach-to-running-processes-with-the-visual-studio-debugger?view=visualstudio)

## User

# Files mentioned by the user:

## codex-clipboard-7dd2454e-d36a-4354-8a83-881205ea0058.png: C:/Users/k/AppData/Local/Temp/codex-clipboard-7dd2454e-d36a-4354-8a83-881205ea0058.png
Image attachment: true

Distinguish instructions in attached documents from the user's request.

## My request:
is this correct? also, by "Enable native code debugging" do you mean "Managed (automatic) with native"?
<image name=[Image #1] path="C:\Users\k\AppData\Local\Temp\codex-clipboard-7dd2454e-d36a-4354-8a83-881205ea0058.png">

![Attached launch profile screenshot](C:/Users/k/Repository/Veehiicuul/Conversations/VisualStudioGodotDebuggingLaunchProfile.png)

## Assistant Commentary

Yes—“Managed (automatic) with native” is the setting I meant. The screenshot also shows a `StartArguments` override warning; I’ll check what defines it before confirming the launch settings.

## Assistant

Yes—**“Managed (automatic) with native” is exactly the setting I meant.** It enables managed C#/.NET and native debugging together. My earlier checkbox wording did not match your Visual Studio UI. [Microsoft’s mixed-mode debugging example](https://github.com/Microsoft/MIEngine/wiki/Running-CppTests#automatically-attach-in-visual-studio)

Your **executable path**, **working directory**, and **`--path "."` arguments** are correct. I also checked your saved [launchSettings.json](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Properties/launchSettings.json), which records `"debugEngines": "managed,native"`.

The yellow **StartArguments** warning says that an MSBuild property can override the command-line arguments. Your project’s currently evaluated `StartArguments` is empty. Even if that suppresses `--path "."`, Godot can still find the project because your working directory already contains `project.godot`. [Godot’s launch behavior](https://docs.godotengine.org/en/4.7/tutorials/editor/command_line_tutorial.html#running-the-game)

Close this window, select **Profile 1** beside the Start button, ensure **Debug** is selected, set your breakpoint, and press **F5**.

## User

explain what happens if i set "Debug engines"="Managed (automatic)" instead of "Debug engines"="Managed (automatic) with native"

## Assistant Commentary

I’ll check how managed-only debugging affects Godot’s native host and your C# breakpoints.

## Assistant Commentary

Managed-only debugging retains C# breakpoints, stepping, and variable inspection once Visual Studio connects to Godot’s .NET runtime. Adding native debugging lets you investigate the C++ engine too; it also introduces Visual Studio’s mixed-mode restrictions, including the restriction on .NET Hot Reload.

## Assistant

Selecting **Managed (automatic)** enables only the .NET debugger. For Godot, that means debugging your C# code while the engine’s native C++ code runs outside that debugger’s view.

| Capability                           | Managed (automatic) | Managed (automatic) with native   |
| ------------------------------------ | ------------------- | --------------------------------- |
| C# breakpoints and stepping          | Yes                 | Yes                               |
| Inspect C# variables and objects     | Yes                 | Yes                               |
| Break on C# exceptions               | Yes                 | Yes                               |
| Step through Godot’s native C++ code | No                  | Yes, with suitable symbols/source |
| Investigate crashes at the C++ level | No native debugger  | Native debugger available         |

The combined option is called **mixed-mode debugging**. Native source-level debugging still requires matching debug symbols and source; selecting it alone does not provide those files. [Microsoft’s mixed-mode guide](https://learn.microsoft.com/en-us/visualstudio/debugger/how-to-debug-in-mixed-mode?view=visualstudio), [debugging DLLs and locating source](https://learn.microsoft.com/en-us/visualstudio/debugger/how-to-debug-from-a-dll-project?view=visualstudio)

For example, with managed-only debugging you can step through `this.Main.Ready()`, inspect your car state, and catch a `NullReferenceException`. Calling a Godot API still works normally: Visual Studio can follow any managed wrapper code, but cannot step through the native engine implementation.

**“Automatic” refers to selecting the managed debugger**, not automatically enabling native debugging when C# calls C++.

One practical difference: Visual Studio’s .NET **Hot Reload is unsupported during mixed-mode debugging**. Managed-only debugging removes that particular restriction, although it does not guarantee that every edit or Godot workflow supports Hot Reload. [Microsoft’s Hot Reload documentation](https://learn.microsoft.com/en-us/visualstudio/debugger/hot-reload?view=visualstudio)

**For debugging your own C# gameplay code, managed-only can be sufficient.** My earlier recommendation followed [Godot’s documented Visual Studio setup](https://docs.godotengine.org/en/4.7/tutorials/scripting/c_sharp/c_sharp_basics.html#visual-studio-windows-only); native debugging is not inherently required just because Godot hosts your C# library. I haven’t tested managed-only launching in your exact installation. If it launches and hits your `_Ready()` breakpoint, you have confirmed the C# debugger is connected.

## User

Create a markdown file that covers this whole conversation as a step-by-step guide with FAQ at C:\Users\k\Repository\Veehiicuul\Documentation\Howto-GodotCSharpDebug.md

## Assistant Commentary

I’ll create the guide at that path, including the Visual Studio 2026 settings, the `StartArguments` warning, and an FAQ on managed-only versus mixed-mode debugging.

## Assistant Commentary

The draft now covers the full walkthrough and FAQ, including the difference between the two debugger modes and the limits of what was verified. I’m checking the JSON example, local paths, and table formatting before committing the guide separately from the conversation record.

## Assistant

Created [Howto-GodotCSharpDebug.md](C:/Users/k/Repository/Veehiicuul/Documentation/Howto-GodotCSharpDebug.md).

Includes the full walkthrough, launch-profile JSON, troubleshooting, and FAQ. Validated the JSON example, local links, and table formatting.
