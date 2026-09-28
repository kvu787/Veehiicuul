# Debug Godot C# with Visual Studio 2026

This guide covers Godot 4.7.2 .NET and Visual Studio 2026 on Windows 11 x64. It uses an **Executable launch profile** to start the game with Visual Studio's debugger attached, then explains the debugger choices and launch-profile warning discussed during setup.

The examples use this repository's current paths. Substitute your own paths when using another checkout or project.

## Step-by-step setup

### 1. Install the Visual Studio components

Open **Visual Studio Installer → Visual Studio 2026 → Modify**. Select the **.NET desktop development** workload and ensure that the **.NET 10 SDK for x64** is installed.

This repository's C# project targets `net10.0`. Visual Studio 2026 supports .NET 10. The SDK supplies the compiler and build tools; installing only the .NET runtime is insufficient for building the project.

Use the portable Godot 4.7.2 .NET installation at:

```text
C:\Users\k\Program\Godot_v4.7.2-stable_mono_win64
```

Sources: [Visual Studio 2026 compatibility](https://learn.microsoft.com/en-us/visualstudio/releases/2026/compatibility), [Godot C# prerequisites](https://docs.godotengine.org/en/4.7/tutorials/scripting/c_sharp/c_sharp_basics.html#prerequisites).

### 2. Prepare the Godot project

Open the project in Godot 4.7.2 .NET and allow asset importing to finish. Ensure that the project has a main scene assigned. This project's main scene is `res://Scenes/Main.tscn`.

The project directory is:

```text
C:\Users\k\Repository\Veehiicuul\Veehiicuul_Godot_CSharp\Veehiicuul
```

It contains `project.godot` and the C# project and solution files.

Optionally, set **Editor → Editor Settings → Dotnet → Editor → External Editor** to **Visual Studio**. This lets Godot open C# scripts in Visual Studio. Opening the solution manually, as described next, also works.

### 3. Open the solution in Visual Studio

Choose **File → Open → Project/Solution** and open:

```text
C:\Users\k\Repository\Veehiicuul\Veehiicuul_Godot_CSharp\Veehiicuul\Veehiicuul_Godot_CSharp.slnx
```

For another project, open its `.sln` or `.slnx` solution. If a new Godot project has no C# project files yet, create its first C# script through Godot to generate them.

In **Solution Explorer**, right-click the C# project and choose **Set as Startup Project**, if needed.

### 4. Select Debug and build

Select **Debug** in Visual Studio's solution-configuration dropdown. Choose **Build → Build Solution** and resolve any build errors before continuing.

The solution contains `Debug`, `ExportDebug`, and `ExportRelease` configurations. Use `Debug` for this editor-executable workflow. The repository's `Build.cmd` performs release exports and serves a different workflow.

**Any CPU is suitable for the C# library.** The x64 Godot executable hosts it in an x64 process; creating an x64 solution configuration is unnecessary just to debug this library. [Microsoft's platform-target explanation](https://learn.microsoft.com/en-gb/dotnet/csharp/language-reference/compiler-options/output)

Debug builds normally provide symbols and disable compiler optimizations, making stepping and variable inspection easier. Avoid forcing optimization on for the Debug configuration. [Visual Studio build and debug settings](https://learn.microsoft.com/en-us/visualstudio/debugger/project-settings-for-csharp-debug-configurations-dotnetcore?view=visualstudio)

### 5. Create an Executable launch profile

Open **Debug → Veehiicuul_Godot_CSharp Debug Properties**.

Alternatively, right-click the project and choose **Properties → Debug → Open debug launch profiles UI**.

Click **Create a new profile**, choose **Executable**, and name the profile `GodotDebug`. An existing profile named `Profile 1` works equally well; the name only identifies it in the launch dropdown.

See [Visual Studio launch-profile settings](https://learn.microsoft.com/en-us/visualstudio/debugger/project-settings-for-csharp-debug-configurations-dotnetcore?view=visualstudio).

### 6. Configure the launch profile

Enter these settings:

| Setting                     | Value                                                                                  |
| --------------------------- | -------------------------------------------------------------------------------------- |
| Executable                  | `C:\Users\k\Program\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64.exe` |
| Working directory           | `C:\Users\k\Repository\Veehiicuul\Veehiicuul_Godot_CSharp\Veehiicuul`                  |
| Command line arguments      | `--path "."`                                                                           |
| Debug engines               | `Managed (automatic) with native`                                                      |
| Use remote machine          | Unchecked                                                                              |
| Enable SQL Server debugging | Unchecked                                                                              |
| Enable WebView2 debugging   | Unchecked                                                                              |

The executable must be the main Godot `.exe`, not the `_console.exe` wrapper. The working directory must contain `project.godot`.

**Managed (automatic) with native** is the newer UI equivalent of the **Enable native code debugging** or **Enable unmanaged code debugging** checkbox mentioned in documentation. It enables managed and native debugging together. [Godot's Visual Studio setup](https://docs.godotengine.org/en/4.7/tutorials/scripting/c_sharp/c_sharp_basics.html#visual-studio-windows-only), [Microsoft's example using the newer selector](https://github.com/Microsoft/MIEngine/wiki/Running-CppTests#automatically-attach-in-visual-studio)

The `--path "."` argument points Godot at the working directory. These settings launch the game. Adding `--editor` would launch the editor instead. [Godot command-line behavior](https://docs.godotengine.org/en/4.7/tutorials/editor/command_line_tutorial.html#running-the-game)

If the window displays a yellow `StartArguments` warning, see the FAQ below before assuming the arguments will be used.

### 7. Verify the saved profile

Visual Studio saves launch profiles in [launchSettings.json](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Properties/launchSettings.json). A profile matching this guide looks like this:

```json
{
  "profiles": {
    "GodotDebug": {
      "commandName": "Executable",
      "executablePath": "C:\\Users\\k\\Program\\Godot_v4.7.2-stable_mono_win64\\Godot_v4.7.2-stable_mono_win64.exe",
      "commandLineArgs": "--path \".\"",
      "workingDirectory": "C:\\Users\\k\\Repository\\Veehiicuul\\Veehiicuul_Godot_CSharp\\Veehiicuul",
      "debugEngines": "managed,native"
    }
  }
}
```

The profile inspected during this conversation was named `Profile 1` and contained these same launch settings. The [captured Launch Profiles window](C:/Users/k/Repository/Veehiicuul/Conversations/VisualStudioGodotDebuggingLaunchProfile.png) shows the corresponding UI.

### 8. Set a breakpoint and start the game

Open [Main_GodotAdapter.cs](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Source/Main_GodotAdapter.cs). Inside `_Ready()`, place the cursor on this statement and press **F9**:

```csharp
this.Main = new Main(mainNode: this);
```

For another project, choose an executable statement in `_Ready()` on a script attached to a node in the main scene.

Close the Launch Profiles window. In Visual Studio's toolbar, select **GodotDebug**, or **Profile 1** if you retained that name. Confirm that the solution configuration is **Debug**, then press **F5 in Visual Studio**.

Execution should stop before the breakpoint's statement runs. Starting from Visual Studio allows the debugger to observe startup code such as `_Ready()`.

### 9. Step through code and inspect state

| Shortcut    | Action                                  |
| ----------- | --------------------------------------- |
| F9          | Toggle a breakpoint on the current line |
| F10         | Step over the current statement         |
| F11         | Step into a method call                 |
| Shift + F11 | Continue until the current method exits |
| F5          | Continue execution                      |
| Shift + F5  | Stop debugging                          |

Hover over variables to inspect values. Under **Debug → Windows**, open:

- **Locals** to inspect local variables and objects in the current scope.
- **Watch → Watch 1** to evaluate selected variables and expressions.
- **Call Stack** to see how execution reached the current statement.

For this project, you can step into `this.Main.Ready()` and then inspect the C# game state. [Microsoft's debugger overview](https://learn.microsoft.com/visualstudio/debugger/debugger-feature-tour)

### 10. Break when C# exceptions are thrown

Open **Debug → Windows → Exception Settings**. Check **Common Language Runtime Exceptions**, or expand the category and select particular exception types.

This makes Visual Studio stop where an exception is thrown, before a handler catches it. It is useful with Godot because the engine can catch and log exceptions from C# callbacks. For example, enabling a break on `NullReferenceException` helps identify the original failing statement.

If breaking on every CLR exception is too noisy, select only the exception types relevant to the problem. [Visual Studio exception settings](https://learn.microsoft.com/en-us/visualstudio/debugger/managing-exceptions-with-the-debugger?view=visualstudio)

### 11. Repeat after editing

Stop debugging, save the changes, rebuild the Debug configuration, and press **F5** again. Use this workflow when you need a predictable fresh run, including another opportunity to break in `_Ready()`.

## FAQ

### What changes if I select Managed (automatic) instead?

Visual Studio uses only its managed .NET debugger. Your C# breakpoints, stepping, variable inspection, and exception settings still apply once it connects to Godot's .NET runtime. Godot's native C++ code continues executing, but the native debugger is unavailable.

| Capability                                   | Managed (automatic) | Managed (automatic) with native       |
| -------------------------------------------- | ------------------- | ------------------------------------- |
| C# breakpoints and stepping                  | Yes                 | Yes                                   |
| Inspect C# variables and objects             | Yes                 | Yes                                   |
| Break on C# exceptions                       | Yes                 | Yes                                   |
| Step through native C++ source               | No                  | Yes, with matching symbols and source |
| Investigate a crash with the native debugger | No                  | Yes                                   |

The combined option is called **mixed-mode debugging**. Native source-level debugging requires matching native debug symbols and source; choosing the option does not provide those files. [Microsoft's mixed-mode guide](https://learn.microsoft.com/en-us/visualstudio/debugger/how-to-debug-in-mixed-mode?view=visualstudio), [debugging DLLs and locating source](https://learn.microsoft.com/en-us/visualstudio/debugger/how-to-debug-from-a-dll-project?view=visualstudio)

For example, managed-only debugging lets you inspect car state or stop on a C# `NullReferenceException`. When your code calls a Godot API, you may be able to step through its managed wrapper, but you cannot follow the native C++ implementation with the managed debugger.

### Does Automatic enable native debugging when needed?

No. **Automatic** refers to choosing the managed debugger. The **with native** part explicitly adds native debugging.

### Is native debugging necessary for my C# gameplay code?

Not inherently. A managed DLL can be debugged while a native application hosts it. Managed-only debugging can therefore be sufficient for your own C# code. [Microsoft's DLL debugging guide](https://learn.microsoft.com/en-us/visualstudio/debugger/how-to-debug-from-a-dll-project?view=visualstudio)

This guide starts with mixed-mode debugging because that follows Godot's documented Visual Studio setup. If you choose managed-only, verify that launching the game reaches your `_Ready()` breakpoint. That confirms that the managed debugger connected successfully in your installation.

Use native debugging when you need to investigate the engine or other native code, particularly native crashes. Managed-only launching was not interactively tested during this conversation.

### How does this affect Hot Reload?

Visual Studio's .NET Hot Reload is unsupported during mixed-mode debugging. Selecting managed-only removes that particular restriction. It does not guarantee that every code edit or Godot workflow supports Hot Reload; other limitations still apply. [Microsoft's Hot Reload documentation](https://learn.microsoft.com/en-us/visualstudio/debugger/hot-reload?view=visualstudio)

Stopping, rebuilding, and restarting remains the workflow used in this guide.

### What does the yellow StartArguments warning mean?

The captured Launch Profiles window warns that the MSBuild property `StartArguments` overrides the profile's **Command line arguments** field. The property's definition may come from the project, a `.csproj.user` file, or imported build settings.

A command-line MSBuild evaluation of this project's **Debug** configuration returned an empty `StartArguments` value during the conversation. That observation does not establish the source of the UI warning or prove that Visual Studio's in-memory evaluation is identical.

If an empty override causes `--path "."` to be omitted, this particular launch can still work: the working directory already contains `project.godot`, and Godot can start the game from that directory without a `--path` argument. [Godot's launch behavior](https://docs.godotengine.org/en/4.7/tutorials/editor/command_line_tutorial.html#running-the-game)

To inspect the current evaluated value from PowerShell:

```powershell
Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

dotnet msbuild 'C:\Users\k\Repository\Veehiicuul\Veehiicuul_Godot_CSharp\Veehiicuul\Veehiicuul_Godot_CSharp.csproj' -nologo -property:Configuration=Debug -getProperty:StartArguments
```

If you later need additional arguments and Visual Studio ignores them, locate the `StartArguments` definition and reconcile it with the profile. Remove the overriding property only when you intend the launch profile to supply those arguments.

### Why is my breakpoint hollow or never reached?

While the game is running under the debugger:

1. Hover over the breakpoint and read its warning.
2. Open **Debug → Windows → Modules**.
3. Find `Veehiicuul_Godot_CSharp.dll` and inspect its loaded path and **Symbol Status**.
4. Verify that it is the Debug assembly built from the current source, with matching debugging symbols.

This project's expected Debug output directory is:

```text
C:\Users\k\Repository\Veehiicuul\Veehiicuul_Godot_CSharp\Veehiicuul\.godot\mono\temp\bin\Debug
```

If the assembly or its matching PDB is stale, stop debugging and rebuild. For symbol-loading details, right-click the module and select **Symbol Load Information**.

If the breakpoint is bound but never reached, check that its node and scene actually run. Also check whether you attached after the relevant code already executed. [Microsoft's breakpoint troubleshooting](https://learn.microsoft.com/en-us/troubleshoot/developer/visualstudio/debuggers/troubleshooting-breakpoints)

### Can I start the game from Godot and attach Visual Studio afterward?

Yes:

1. Open the solution in Visual Studio and set your breakpoints.
2. Start the game from Godot.
3. In Visual Studio, choose **Debug → Attach to Process** and set the connection type to **Local**.
4. Select the **game's Godot process**. Use the **Command Line** column to distinguish it from the editor process.
5. Ensure the code type includes **Managed (.NET Core, .NET 5+)**, or the equivalent wording in your Visual Studio version. Include native debugging if you need it.
6. Select **Attach** and trigger the code you want to inspect.

Attaching afterward misses code that already executed, including startup `_Ready()` calls. Launch through Visual Studio when you need to debug startup. [Visual Studio attach-to-process instructions](https://learn.microsoft.com/en-us/visualstudio/debugger/attach-to-running-processes-with-the-visual-studio-debugger?view=visualstudio)

### Do I need a Godot extension for Visual Studio?

No Godot-specific Visual Studio extension is required for the Executable launch-profile workflow described here. Visual Studio's .NET debugging support supplies the C# debugger.

### Do I need to change Any CPU to x64?

No. The C# library can remain **Any CPU** while running inside the x64 Godot process. Use the x64 Godot executable and x64 .NET SDK described in the prerequisites.

## Verification scope

The guide was checked against the linked Godot and Microsoft documentation, this repository's project and solution, the saved launch profile, and the supplied screenshot on September 28, 2026. Paths and configuration values were inspected locally. An interactive Visual Studio debugging session was not performed as part of writing the guide.
