# Using Visual Studio with this project

This guide covers the full Visual Studio IDE on Windows. Use the repository's CMake project directly.

Verified on 2026-09-06: this computer has Visual Studio Community 2026 version 18.9.2, the required MSVC and CMake components, bundled CMake 4.3.1, and Windows SDK 10.0.26100.0 with DXC. The repository documents Visual Studio 2022 or newer as its prerequisite. Menu wording can vary between versions.

## 1. Check the installation

Open **Visual Studio Installer**, find your Visual Studio installation, and choose **Modify**. Ensure these are installed:

- **Desktop development with C++**.
- The current **MSVC x64/x86 C++ build tools**.
- **C++ CMake tools for Windows**.
- A current **Windows SDK** containing the DirectX Shader Compiler, `dxc.exe`.

Use Windows x64 for this project. The build already selects C++20 and links the Windows and DirectX libraries. Ordinary builds use checked-in assets; Blender is only needed when regenerating those assets. See the [repository prerequisites](../README.md#run).

For Direct3D validation during development, search Windows Settings for **Optional features** and install **Graphics Tools** if needed. The renderer enables the Direct3D 12 debug layer in Debug builds when it is available; failure to obtain that optional layer does not itself stop this code from starting. [Microsoft DirectX tools](https://devblogs.microsoft.com/directx/gpu-plugins-improved-sdk-layers-and-hang-debugging-bringing-directx-12-tools-to-the-next-level/)

## 2. Open the correct folder

1. Start Visual Studio.
2. Choose **Open a local folder** on the start screen, or **File > Open > Folder**.
3. Open the Veehiicuul repository folder containing `Run.cmd` and the root `CMakeLists.txt`.
4. Confirm that Solution Explorer contains the root `CMakeLists.txt`, `Source`, `assets`, and `tests`.
5. Allow the initial CMake configuration and IntelliSense indexing to finish.

Open the repository root, rather than just `Source`, a generated build directory, or a single C++ file. Visual Studio can consume CMake directly for editing, building, and debugging; no solution conversion is necessary. [Microsoft CMake project guide](https://learn.microsoft.com/en-us/cpp/build/cmake-projects-in-visual-studio?view=msvc-170)

## 3. Set up predictable Debug and Release configurations

The checked-in [CMakePresets.json](../CMakePresets.json) provides `vs-debug` and `vs-release` configure, build, and test presets. Their explicit `Out/Build/${presetName}` output path preserves the folder capitalization when Visual Studio creates new build directories. Personal presets can inherit these configurations in the ignored `CMakeUserPresets.json` file.

The `RunDebug` and `RunRelease` launcher presets inherit these shared settings and use the launcher's `MyBuildOutput/Debug` and `MyBuildOutput/Release`. Both `Out/` and `CMakeUserPresets.json` are already ignored by Git. The shared presets are versioned; personal overrides remain local. [CMake preset format](https://cmake.org/cmake/help/latest/manual/cmake-presets.7.html)

In **Tools > Options > CMake > General**, enable preset-based configuration, using **Always use CMakePresets.json** or the equivalent option in your version. Close and reopen the folder if necessary. Select **Local Machine**, **Windows x64 Debug**, and the associated `vs-debug` build preset if that selector is shown. Wait for successful configuration. Visual Studio supplies the MSVC environment for the preset's external x64 architecture. [Visual Studio preset setup](https://learn.microsoft.com/en-us/cpp/build/cmake-presets-vs?view=msvc-170)

Use the generated output paths below after following this setup:

| Purpose                 | Directory               |
| ----------------------- | ----------------------- |
| Visual Studio Debug     | `Out/Build/vs-debug`     |
| Visual Studio Release   | `Out/Build/vs-release`   |
| Run.cmd default Release | `MyBuildOutput/Release`  |
| Run.ps1 explicit Debug  | `MyBuildOutput/Debug`    |

The first CMake configure checks the compiler and locates DXC. Configuration generates the build system; building then compiles the C++ and shaders and stages the assets.

## 4. Build and launch the game

1. Choose **Build > Build All**.
2. Open **View > Output** and inspect the build output if anything fails.
3. In the startup-item dropdown beside the green run button, select **Veehiicuul.exe**. Some views show the target as **Veehiicuul**.
4. Press **F5** to build as needed and launch under the debugger.
5. Press **Ctrl+F5** to launch without the debugger.

There are several test executables. Select the game explicitly. In Solution Explorer's **CMake Targets View**, you can also right-click the game target and set it as the startup item. Folder View remains useful for browsing all repository files. [Microsoft CMake IDE workflow](https://learn.microsoft.com/en-us/cpp/build/cmake-projects-in-visual-studio?view=msvc-170)

The Debug executable is:

```text
Out/Build/vs-debug/Veehiicuul.exe
```

Its adjacent `assets` folder should contain `Settings.json` and `SceneBackground.png`. The game locates these relative to its executable, so its working directory normally needs no adjustment.

Expected result: a window displaying the car, sphere, and backdrop. In the game, **V** toggles VSync, **F11** toggles borderless fullscreen, and **Esc** exits. F11 is also a debugger shortcut; which action occurs depends on which window has focus.

## 5. Find your way around the code

Read these files in this order:

| File or directory                             | What to look for                                              |
| --------------------------------------------- | ------------------------------------------------------------- |
| `Source/Main.cpp`                             | Entry point, settings loading, top-level exception handling.  |
| `Source/Application.h` and `.cpp`             | Window creation, input, message processing, application loop. |
| `Source/Settings.h` / `.cpp`                  | File model and typed JSON loading.                            |
| `Source/Renderer.h`                           | Renderer interface and ownership of graphics resources.       |
| `Source/Renderer.cpp`                         | Device, swap chain, resources, drawing, synchronization.      |
| `Source/SimplePaint/Material.h` / `.cpp`      | Paint parameters and their numerical contract.                |
| `Source/SimplePaint/OrthographicTransforms.h` | Orthographic transform helpers and validation.                |
| `Source/SimplePaint/SimplePaint.hlsl`         | Paint shader entry points.                                    |
| `Source/SimplePaint/SimplePaintCore.hlsli`    | Shared paint shading logic.                                   |
| `Source/Shaders/Background.hlsl`              | Background rendering.                                         |
| `Source/UVSphere.h`                           | Procedural sphere generation.                                 |
| `Tests/`                                      | Executable examples of expected behavior and edge cases.      |

Useful editor commands are **Go To Definition**, **Peek Definition**, **Find All References**, **Go To All**, and **Find in Files**. Use the symbol's context menu or Visual Studio command search if your keyboard mapping differs. Navigation becomes most useful after CMake configuration and indexing succeed.

Start with `wWinMain`, follow `Application::Run`, then inspect `Renderer::Initialize`, `PrepareFrame`, and `Render`. For configuration changes, trace `LoadSettings`: `Deserialize<Settings>(input)`, then `ValidateSettings(settings)`. See [Settings architecture](Settings.md) for the model, JSON mappings, plain C++ validator, and renderer preparation.

## 6. Debug C++ behavior

Select **Windows x64 Debug** and the game startup target.

1. Open `Source/Main.cpp`.
2. Place the cursor on the settings-loading statement and press **F9**.
3. Press **F5**. Execution should stop before the statement runs.
4. Press **F10** to execute it without entering the function.
5. Inspect `settings` in **Debug > Windows > Locals**, or hover over it.
6. Use **F11** to enter a called function, **Shift+F11** to return to its caller, and **F5** to continue.
7. Use **Debug > Windows > Call Stack** to inspect the chain of calls. Add selected expressions to a Watch window.

These are the usual debugger shortcuts; customized keyboard settings may differ. [Microsoft stepping guide](https://learn.microsoft.com/en-us/visualstudio/debugger/navigating-through-code-with-the-debugger?view=visualstudio)

Useful breakpoint locations include:

- `LoadSettings`: file access and validation.
- `Renderer::CreateDevice`: hardware selection and WARP fallback.
- `Renderer::CreatePipelines`: shader and pipeline setup.
- `Renderer::Render`: submitted frame work.
- `Renderer::Resize`: resize handling.

A breakpoint in the render loop fires repeatedly. Disable it after inspection or give it a condition through its context menu.

For exceptions, open **Debug > Windows > Exception Settings** and enable breaking when **C++ Exceptions** are thrown while investigating a failure. This is helpful because `wWinMain` catches standard exceptions and displays a message box. Breaking at the throw reveals the originating code. Some contract tests deliberately throw, so adjust this setting when debugging tests. [Microsoft debugger overview](https://learn.microsoft.com/en-us/visualstudio/debugger/debugger-feature-tour?view=visualstudio)

The renderer sends its pipeline description through `OutputDebugStringW`; look in the Output window's **Debug** stream. If you need custom launch arguments for a test target, use that target's **Debug and Launch Settings** command and let Visual Studio generate `.vs/launch.vs.json`. Keep its generated target identity and add the needed arguments there. [CMake launch configuration](https://learn.microsoft.com/en-us/cpp/build/configure-cmake-debugging-sessions?view=msvc-170)

## 7. Edit settings, C++, and shaders

**Settings:** edit the repository's `Assets/Settings.json`, save, build the game target, and restart. The `RuntimeAssets` dependency copies updated settings next to the selected executable. Settings load at startup; there is no live reload. Editing only the output copy is temporary because a subsequent build can replace it.

**C++:** stop debugging, edit, save, build, and restart. Add new `.cpp` files to the relevant CMake target. Merely creating a file in Folder View does not ensure it is compiled. The main application target is in the root CMake file; the reusable paint library has its own `Source/SimplePaint/CMakeLists.txt`.

**Shaders:** edit the original `.hlsl` or `.hlsli` files, then build and restart. CMake invokes DXC and embeds generated shader byte arrays in the executable. Do not edit the generated headers under the build directory's `Generated/Shaders`.

The current shader command always uses optimization and strips debug information, including when C++ is built in Debug. A C++ Debug build therefore does not provide shader source stepping. Adding shader-debug compilation would require a separate build change.

**Mesh/background assets:** ordinary builds reuse `Source/Generated/CarMesh.generated.h` and `Assets/SceneBackground.png`. Regenerate them from Blender only when needed. The optional `RegenerateAssets` CMake target appears when a supported Blender installation is detected. It rewrites those source assets; see [asset generation](Assets.md).

## 8. Run the tests

Build **all targets** before running the suite; building just the game does not build every test executable.

In Visual Studio, use **Test > Run CTests for ...** or **Test > Run Test Preset for ...** when available. Choose the test preset matching your configuration. If the menu differs or Test Explorer is empty, the command-line route below runs the repository's CTest suite directly. [Microsoft CTest integration](https://learn.microsoft.com/en-us/visualstudio/test/how-to-use-ctest-for-cpp?view=visualstudio)

Open the installed Visual Studio's **x64 Native Tools Command Prompt** from Start. This supplies the x64 MSVC environment; the preset alone does not initialize that environment in a normal terminal.

```bat
cd /d "<path to the Veehiicuul repository>"
cmake --preset vs-debug
cmake --build --preset vs-debug
ctest --preset vs-debug
```

These commands use the same output as the IDE presets. To run one test or list the suite:

```bat
ctest --test-dir Out/Build/vs-debug -N
ctest --test-dir Out/Build/vs-debug -R "^SimplePaintContract$" --output-on-failure
ctest --test-dir Out/Build/vs-debug -R "Warp" --output-on-failure
```

The current root configuration registers 13 tests, covering paint contracts, standalone module consumption, sphere geometry, orthographic transforms, application settings, hardware/WARP rendering, pipeline behavior, and window lifecycle.

For step-by-step test debugging, select an executable such as `SimplePaintTests` as the startup item and press F5. Return the startup item to the game afterward.

The launcher uses the inherited `RunDebug` and `RunRelease` configure, build, and test presets and prepares the compiler environment automatically. From PowerShell in the repository root:

```powershell
.\Run.ps1 -Test -Configuration Debug
.\Run.ps1 -Test
```

These build and test `MyBuildOutput/Debug` and `MyBuildOutput/Release`, respectively, rather than the IDE output. `-Test` runs tests instead of launching the normal game.

## 9. Use Release builds and the launcher

Switch to **Windows x64 Release**, build, select the game, then use **Ctrl+F5** for ordinary performance observations. Keep window size, VSync, and pipeline settings consistent when comparing results.

Release with the presets produces:

```text
Out/Build/vs-release/Veehiicuul.exe
```

Double-clicking `Run.cmd` uses the `RunRelease` preset to build and launch Release in `MyBuildOutput/Release`. It does not use whichever preset is active in Visual Studio.

Other existing PowerShell commands are:

```powershell
.\Run.ps1 -BuildOnly
.\Run.ps1 -BuildOnly -Configuration Debug
.\Run.ps1 -Configuration Debug
```

They build Release, build Debug, and build/launch Debug, respectively. For distribution or moving the output, preserve the executable's adjacent assets directory.

Debug builds enable additional validation when available and have different CPU optimization. Breakpoints also disrupt timing. Do performance comparisons using Release without an attached debugger.

## 10. Troubleshoot common problems

**CMake configuration fails before anything builds**

Read the first actual failure in the CMake Output stream. Check that you opened the repository root, selected Local Machine/x64, and installed the required workload. A later missing-target error can be a consequence of failed configuration.

**DXC is not found**

On the checked machine, DXC exists here:

```text
C:/Program Files (x86)/Windows Kits/10/bin/10.0.26100.0/x64/dxc.exe
```

First verify that Visual Studio is supplying the SDK environment. If discovery still fails, add this entry to the `vs-debug` preset's `cacheVariables`, with the required comma after the previous entry:

```json
"DXC_EXECUTABLE": "C:/Program Files (x86)/Windows Kits/10/bin/10.0.26100.0/x64/dxc.exe"
```

Use the actual installed path on another machine. Release inherits this value. Reconfigure afterward.

**Only default presets appear**

Ensure the root `CMakePresets.json` is present and enable preset mode. Check the JSON syntax of any personal `CMakeUserPresets.json` overrides. In the x64 developer prompt, run `cmake --list-presets` from the repository root. Close and reopen the folder after changing CMake integration settings. [Preset troubleshooting](https://learn.microsoft.com/en-us/cpp/build/cmake-presets-vs?view=msvc-170)

**Headers have red squiggles**

Complete configuration, build once to create shader headers, and let IntelliSense finish. Do not hard-code SDK include paths into the source to repair an IDE configuration problem. Compare diagnostics with the actual build output.

**The linker cannot write the executable**

Close the running game or stop the debugging session, then build again.

**A breakpoint is hollow or variables are optimized away**

Check that the active configuration is Debug and the startup executable comes from its output directory. Stop, rebuild, and restart. If necessary, inspect **Debug > Windows > Modules** for the executable path and symbol status.

**Settings changes appear to do nothing**

Edit the source JSON, rebuild the selected game target, and restart the correct executable. The launcher and IDE each have their own staged settings copy.

**The game cannot load its image or settings**

Build the game target so its asset dependency runs. Confirm both files are under `assets` beside the executable. Changing the process working directory will not fix missing executable-relative assets.

**CMake reports a generator mismatch or an old repository path**

Use Visual Studio's **Delete Cache and Reconfigure** command for the affected configuration; wording and menu placement vary. Keep different configurations in separate build directories. The launcher has its own moved-folder detection for its caches. [CMake cache management](https://learn.microsoft.com/en-us/cpp/build/cmake-projects-in-visual-studio?view=msvc-170)

**A test executable cannot be found**

Build all targets and check that the CTest directory or preset matches the build configuration. Do not point a Debug test command at the Release directory.

**GPU initialization fails**

Read the message and inspect `Renderer::CreateDevice`. The renderer tries qualifying hardware, then WARP. The current runtime still needs Shader Model 6.0, including on WARP; older WARP runtimes remain a documented implementation gap. No particular hardware GPU is a repository prerequisite.

## 11. Keep changes reviewable

Review edits in Visual Studio's Git Changes window. Commit source, intended assets, tests, and documentation as appropriate. The repository already ignores IDE state and build output; keep machine-specific preset settings local.

Follow the shared [README constraints](../README.md#constraints): Windows x64, vendor-neutral graphics interfaces, and no application-owned frame-rate limiter. Rendering synchronization and VSync are permitted. Record conversations under `Conversations` and keep their `[cnv]` commits separate from implementation or documentation commits.

A normal development cycle is: choose Debug, edit, build, debug, run relevant tests, then check Release behavior before committing.
