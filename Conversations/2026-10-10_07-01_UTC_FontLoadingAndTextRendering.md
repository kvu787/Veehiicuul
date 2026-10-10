# Font loading and text rendering conversation

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

"C:\Users\k\Repository\Veehiicuul\InputLatency_CppDx12"
explain how this app loads fonts and renders text

## Assistant

I'll inspect the app's font loading and text rendering code, then explain how they fit together.

## Assistant

The app rasterizes Consolas once at startup into a small texture containing printable ASCII characters. After that, DirectX 12 draws text as textured triangles, with fixed character spacing and no font rasterization during frames.

## Assistant

The app uses **Windows GDI to rasterize the font once at startup**, then **DirectX 12 to render text on every frame**.

1. **Select the font.** [CreateFontPixels](C:/Users/k/Repository/Veehiicuul/InputLatency_CppDx12/Source/Renderer.cpp:55) calls `CreateFontW` to request installed **Consolas**, normal weight, with an 18-pixel character height and grayscale antialiasing. It does not load a bundled font file or verify which font Windows actually selected.

2. **Build a font atlas.** GDI draws the 95 printable ASCII characters, space through `~`, into a **192 x 144** pixel bitmap. This is an atlas: a single image containing all the character images, arranged in 16 columns with **12 x 24** pixel cells. Each character is drawn separately using `TextOutW`. The code converts pixel brightness into alpha coverage and makes RGB white, so the renderer can tint the characters later. It then deletes the temporary GDI font, bitmap, and drawing context.

3. **Upload the atlas to the GPU.** [CreatePipelineAndAtlas](C:/Users/k/Repository/Veehiicuul/InputLatency_CppDx12/Source/Renderer.cpp:240) copies those pixels through an upload buffer into a DirectX 12 RGBA texture. It transitions the texture to a shader-readable state and creates a shader resource view. The texture is retained and reused throughout the run.

4. **Turn strings into triangles.** [Text](C:/Users/k/Repository/Veehiicuul/InputLatency_CppDx12/Source/Renderer.cpp:437) walks the string byte by byte. For each non-space character, it calculates the matching atlas cell and adds a **12 x 24** pixel rectangle at the requested screen position. [Quad](C:/Users/k/Repository/Veehiicuul/InputLatency_CppDx12/Source/Renderer.cpp:317) represents each rectangle as two triangles, or six vertices, carrying position, texture coordinates, and color. Every character advances the cursor by exactly 12 pixels; spaces advance it without adding triangles.

5. **Draw the batch.** The [vertex shader](C:/Users/k/Repository/Veehiicuul/InputLatency_CppDx12/Source/Dashboard.hlsl:6) converts pixel positions into GPU clip coordinates. The pixel shader samples the atlas and multiplies it by the requested text color. Point sampling preserves the atlas pixels, and alpha blending produces antialiased edges against the background. [DrawAndPresent](C:/Users/k/Repository/Veehiicuul/InputLatency_CppDx12/Source/Renderer.cpp:565) copies the combined dashboard geometry into a mapped vertex buffer and draws it with one `DrawInstanced` call.

The font has **one fixed rasterized size, with no scaling**. The 18-pixel font height and 12 x 24 drawing cells describe different things: the latter includes the space allocated around each character.

Text layout is deliberately basic: there is no kerning, Unicode decoding, wrapping, or newline handling. Bytes outside printable ASCII become `?`, and text stops before crossing the content area's right margin. Most dashboard geometry is rebuilt every **250 ms**; current controller values are added each frame.

## User

how does this compare to using directwrite

## Assistant

I'll compare this atlas approach with DirectWrite, including what would change in the app's DirectX 12 rendering path.

## Assistant

**The current approach is a small, fixed text renderer. DirectWrite would add better font handling and text layout, while still allowing the app to use its existing DirectX 12 atlas renderer.**

DirectWrite handles font selection, character shaping, layout, and glyph rasterization. It is independent of the graphics system, so using it does not require switching the dashboard to Direct2D. [Microsoft's DirectWrite overview](https://learn.microsoft.com/en-us/windows/win32/directwrite/introducing-directwrite).

| Aspect           | Current app                         | With DirectWrite                                      |
| ---------------- | ----------------------------------- | ----------------------------------------------------- |
| Characters       | Printable ASCII only                | Unicode, shaping, bidirectional text, font fallback    |
| Character spacing| Fixed 12-pixel advance              | Actual font metrics and fractional positioning        |
| Layout           | One line, cut off at the right edge  | Measurement, wrapping, alignment, trimming             |
| Font size        | One rasterized size                 | Supports different sizes; can still use one fixed size |
| Antialiasing     | GDI grayscale coverage in an atlas  | Grayscale or ClearType, with rendering controls        |
| Frame rendering  | Batched textured triangles in DX12  | Can retain that approach or use Direct2D               |

DirectWrite's typography and layout features are documented in Microsoft's [overview](https://learn.microsoft.com/en-us/windows/win32/directwrite/introducing-directwrite) and [programming guide](https://learn.microsoft.com/en-us/windows/win32/directwrite/programming-guide).

There are two useful ways to integrate it:

- **Use DirectWrite to generate the atlas.** Replace the startup GDI rasterizer with DirectWrite glyph rasterization, then retain the existing texture, triangles, shaders, and batching. DirectWrite exposes glyph coverage through [glyph-run analysis](https://learn.microsoft.com/en-us/windows/win32/api/dwrite/nn-dwrite-idwriteglyphrunanalysis). If the app still uses ASCII and 12-pixel spacing, this changes rasterization but leaves most existing limitations intact. Unicode support would also require decoding strings, obtaining shaped glyph runs, and caching the required glyphs.
- **Use DirectWrite with Direct2D to draw text.** This provides a more complete text drawing API. Microsoft's DX12 integration example uses Direct2D through D3D11On12, adding wrapped resources and acquire/release operations around drawing. That is a larger change to this app's rendering path. [Microsoft's integration example](https://learn.microsoft.com/en-us/windows/win32/direct3d12/d2d-using-d3d11on12).

**For this latency dashboard, the existing approach is reasonable:** it has one font size, simple labels and numbers, and performs font rasterization only at startup. DirectWrite does not inherently make the per-frame rendering faster; a DirectWrite-generated atlas could have essentially the same GPU work. Performance differences would need measurement.

The clearest reason to adopt DirectWrite here would be **correctly displaying non-ASCII controller names or improving font metrics and appearance**. I would retain the DX12 atlas renderer and add DirectWrite font/layout support as needed. DirectWrite also does not require ClearType; grayscale rendering fits the current alpha-mask approach.

## User

what approach in general (it doesn't have to be anything you already mentioned) to font loading and text rendering is the best for a highly-performant dx12 app that does 3d rendering

## Assistant

I'll look at the approaches used in 3D renderers and distinguish fixed-size UI text from text that needs to scale in the scene.

## Assistant

**My default for a high-performance DX12 3D app would be cached glyph atlases rendered directly by the app's DX12 renderer, with DirectWrite handling fonts and text shaping on the CPU.** Choose the atlas format according to how the text is displayed.

There is no universally fastest implementation, but this architecture keeps font processing separate from the frame's drawing work.

| Text usage                         | My default choice                                  |
| ---------------------------------- | -------------------------------------------------- |
| Small HUD text at predictable sizes | Rasterized grayscale glyph atlases                 |
| Labels in the 3D world              | MSDF atlases for text that changes apparent size    |
| Large or frequently zoomed UI text  | MSDF, or rasterized atlases for selected sizes      |
| Extruded 3D letters                | Glyph outlines converted into meshes               |

**For ordinary HUD text, use cached bitmap glyphs.** Rasterize at the sizes actually displayed, store coverage in an `R8_UNORM` texture, and draw glyph rectangles with a simple sampling/blending shader. Compared with the current app's RGBA atlas, R8 uses one byte per texel instead of four. Small text also benefits from rasterization and hinting tailored to its displayed size. Microsoft's [DirectXTK12 SpriteFont](https://github.com/microsoft/DirectXTK12/wiki/SpriteFont) is an existing native DX12 bitmap-font implementation if you want a ready-made starting point; its text/layout capabilities are more limited than DirectWrite's.

**For text that scales in 3D, use MSDF.** A multi-channel signed distance field stores information about the glyph boundary rather than coverage at one output size. A shader reconstructs the edge at the displayed scale, including under perspective. It preserves corners better than a conventional single-channel SDF. It costs more texture storage and shader work than a grayscale bitmap and still has quality limits at extreme scales. The [msdfgen project](https://github.com/Chlumsky/msdfgen) documents generation and reconstruction, including perspective-aware filtering.

For font loading and layout on Windows, I would use **DirectWrite** to resolve font faces, apply fallback, and turn Unicode strings into positioned glyph runs. Cache those results and rasterize only missing glyphs. DirectWrite supports custom graphics renderers, so it can feed your atlas renderer without a Direct2D drawing pass. [Microsoft's DirectWrite overview](https://learn.microsoft.com/en-us/windows/win32/directwrite/introducing-directwrite).

The performance decisions I would prioritize are:

- **Prepare predictable glyphs ahead of use.** Generate packaged atlases during the build for known fonts/character sets, or warm a runtime cache during loading. Handle unexpected glyphs without making the render thread wait for rasterization.
- **Cache layout as well as glyph images.** Recompute shaped text when its content or formatting changes. Index glyph images by font face and glyph ID, plus size and rasterization settings where applicable.
- **Batch compatible text draws.** Use indexed quads or instanced quads; neither is automatically faster in every workload. Preserve transparency order and clipping while minimizing pipeline and texture changes.
- **Use buffers that respect frames in flight.** Recycle transient vertex/instance data only after the corresponding GPU fence completes. Avoid a CPU/GPU wait just to submit text.
- **Control pixel work.** Cull invisible labels and keep glyph bounds tight. Render HUD text after temporal scene upscaling/antialiasing when it should remain crisp at the final output resolution.

**For your app if you add 3D rendering, I would start with DirectWrite plus a grayscale bitmap atlas for the dashboard.** Add an MSDF path if you need scalable scene labels. The biggest architectural benefit is reusing cached font/layout work and drawing text through the same DX12 renderer; replacing GDI at startup alone would not establish a performance improvement.
