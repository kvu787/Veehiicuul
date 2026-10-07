# InputLatency fonts and text rendering conversation

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

InputLatency_CppDx12
explain how fonts and text rendering works

## Assistant Commentary

I'll inspect InputLatency_CppDx12 and trace how it loads fonts and draws text.

## Assistant Commentary

The app requests Consolas from Windows and rasterizes its characters into a small image once at startup. During rendering, DirectX 12 draws those cached character images as textured rectangles. The text code uses fixed spacing and supports printable ASCII only.

## Assistant Final

**InputLatency_CppDx12 uses a cached bitmap font atlas:** Windows creates the character images once, then DirectX 12 draws them each frame.

1. **Create the font images.** At startup, [CreateFontPixels](C:/Users/k/Repository/Veehiicuul/InputLatency_CppDx12/Source/Renderer.cpp:54) requests regular **Consolas at an 18-pixel character height** through Windows GDI. It draws the 95 printable ASCII characters, from space through `~`, into a **192 x 144 pixel image**, arranged in 16 columns of **12 x 24 pixel cells**. This image is called the *font atlas*. No font file is bundled; Windows supplies the installed font.

2. **Turn the images into transparency.** GDI draws white characters on black. The code converts each pixel's brightness into its alpha value: black becomes transparent, white becomes opaque, and gray edge pixels become partly transparent. This preserves grayscale antialiasing. The character's color is chosen later when drawing.

3. **Upload the atlas to the GPU.** [CreatePipelineAndAtlas](C:/Users/k/Repository/Veehiicuul/InputLatency_CppDx12/Source/Renderer.cpp:197) copies this image into a DirectX 12 texture once. The texture stays available for subsequent frames.

4. **Build rectangles for a string.** [Text](C:/Users/k/Repository/Veehiicuul/InputLatency_CppDx12/Source/Renderer.cpp:418) locates each character's cell in the atlas and creates a rectangle made of two triangles. Its texture coordinates select only that character's image. Every character advances the drawing position by 12 pixels; spaces advance without creating geometry.

5. **Draw and blend.** The [shader](C:/Users/k/Repository/Veehiicuul/InputLatency_CppDx12/Source/Dashboard.hlsl:6) converts screen pixel positions into GPU coordinates, samples the atlas, and multiplies it by the requested text color. Alpha blending combines the character with the background, giving smooth edges. Text and dashboard shapes share one batched draw call. Shapes sample a reserved solid white pixel in the same atlas.

**What this means for appearance and performance:**

- Text has one fixed size and fixed spacing. Resizing the window does not enlarge the font.
- Sampling uses nearest-neighbor filtering. Edge smoothing is already baked into the atlas; there is no ClearType subpixel rendering.
- Only printable ASCII is supported. Unsupported bytes become `?`, so Unicode device names can display incorrectly.
- There is no kerning, word wrapping, or newline layout. Long strings stop before the window's right margin.
- GDI font rasterization happens only at startup. Dashboard text geometry is normally rebuilt every 250 ms; live stick and trigger values are rebuilt each frame. The GPU redraws the text every frame using the cached texture.
