# DirectX window modes conversation

## User repository instructions

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
When it makes sense, split large work into multiple commits to make it easier to review.
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

## User

<external_codex_apps_open_page>{"page_id":null}</external_codex_apps_open_page>

## User

what is the proper terminology for each of the window modes for modern directx 12 games/applications

## Assistant

I'll check Microsoft's terminology for window modes and DXGI presentation modes, since those describe different aspects of a DirectX 12 application.

## Assistant

For modern DirectX 12 applications on Windows 11, distinguish **window mode** from **presentation mode**.

The usual window-mode terms are:

| Term                      | Meaning                                                        |
|---------------------------|----------------------------------------------------------------|
| **Windowed**              | A normal window, usually with a title bar and borders.          |
| **Borderless windowed**    | A window without a title bar or borders; can be any size.       |
| **Borderless fullscreen**  | A borderless window covering an entire monitor.                 |

"Fullscreen windowed" and "windowed fullscreen" are common synonyms for **borderless fullscreen**. A maximized window is still windowed; maximizing it does not inherently remove its borders or cover the taskbar.

**Fullscreen exclusive (FSE)** is the traditional term for an application taking exclusive control of a display. However, Microsoft's current documentation explicitly says **Direct3D 12 does not support FSE**. Its `SetFullscreenState(TRUE)` changes resolution and refresh rate to support fullscreen optimizations rather than entering traditional exclusive fullscreen. Consequently, a DX12 game's setting labeled "Fullscreen" or "Exclusive Fullscreen" is not proof of actual exclusivity. [Microsoft: Direct3D 12 swap chains](https://learn.microsoft.com/en-us/windows/win32/direct3d12/swap-chains)

The underlying presentation terminology is separate:

| Term                 | Meaning                                                             |
|----------------------|---------------------------------------------------------------------|
| **Flip model**       | The swap-chain presentation model required by Direct3D 12.           |
| **Composed flip**    | Frames participate in Desktop Window Manager (DWM) composition.      |
| **DirectFlip**       | An optimization allowing application buffers to be scanned out.      |
| **Independent Flip** | Frames reach the display independently of regular DWM composition.  |

Multi-plane overlays (**MPO**) can also enable hardware composition and Independent Flip. These paths depend on the window, buffers, hardware, and other visible content; they are not additional window modes selected from a menu. Borderless fullscreen can use Independent Flip and achieve presentation efficiency comparable to traditional FSE. [Microsoft: DXGI flip model](https://learn.microsoft.com/en-us/windows/win32/direct3ddxgi/for-best-performance--use-dxgi-flip-model)

For naming settings in your application, use **Windowed**, **Borderless Windowed**, and **Borderless Fullscreen** for those respective behaviors. Reserve **presentation mode** for the actual composed/independent flip path.

## User

should 3dTestScene_CppDx12 call SetFullscreenState?

## Assistant

I'll inspect how 3dTestScene_CppDx12 creates its window and swap chain, then check whether SetFullscreenState serves its intended fullscreen behavior.

## Assistant

**No. For its current borderless fullscreen behavior, 3dTestScene_CppDx12 should keep avoiding `SetFullscreenState(TRUE)`.**

I inspected the implementation:

- [Application.cpp](C:/Users/k/Repository/Veehiicuul/3dTestScene_CppDx12/Source/Application.cpp:244) implements F11 by removing window decorations and resizing the window to the monitor.
- [Renderer.cpp](C:/Users/k/Repository/Veehiicuul/3dTestScene_CppDx12/Source/Renderer.cpp:427) creates a windowed flip-discard swap chain and disables DXGI's automatic Alt+Enter switching.
- [Its Present call](C:/Users/k/Repository/Veehiicuul/3dTestScene_CppDx12/Source/Renderer.cpp:1213) uses `DXGI_PRESENT_ALLOW_TEARING` when VSync is off and tearing is enabled and supported.

That matches Microsoft's documented approach for allowing tearing in fullscreen applications: **use a borderless fullscreen window and keep the swap chain windowed**. Entering DXGI fullscreen state would conflict with that presentation flag's requirements. [Microsoft: DXGI_PRESENT](https://learn.microsoft.com/en-us/windows/win32/direct3ddxgi/dxgi-present)

Calling `SetFullscreenState(TRUE)` is also **not required to obtain Independent Flip**. Windows can select that path for an eligible borderless window. [Microsoft: DXGI flip model](https://learn.microsoft.com/en-us/windows/win32/direct3ddxgi/for-best-performance--use-dxgi-flip-model)

Reconsider the call only if you deliberately add display resolution/refresh-rate switching through DXGI. For the current application, it provides no needed functionality. [Microsoft: D3D12 swap chains](https://learn.microsoft.com/en-us/windows/win32/direct3d12/swap-chains)

