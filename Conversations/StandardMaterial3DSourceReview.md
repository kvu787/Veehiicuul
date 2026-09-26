# StandardMaterial3D source review

## User — repository instructions

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

## User

with "C:\Users\k\Repository\Veehiicuul\Veehiicuul_Godot_CSharp\Veehiicuul\Testyo\Testyo.tres", i've adjusted the material settings to only provide what i think I need.

provide this specific review: for every property in StandardMaterial3D and all possible values for that property, i want to know:
- the behavior
- the performance behavior
- if there are bugs/issues

also, provide a general review.

use the actual source code at C:\Users\k\Repository\External\Godot_4-7-2

## Assistant Commentary

I’ll inspect your material and the local Godot source, then build a property-by-property reference with behavior, performance implications, and source-backed issues. I’ll also review how your current settings fit together and save the review in the repository.

## Assistant Commentary

The material is still opaque and lit per pixel; the omitted settings keep Godot’s defaults. Your explicit settings remove direct specular highlights, shadow reception, and fog, but leave diffuse and ambient lighting active. I’m checking the shader paths now, since some controls remove shader work while others only change a numeric input.

## Assistant Commentary

I found an important performance detail: this version still generates albedo, metallic, and roughness texture samples when those slots are empty, using fallback textures. The optional feature switches do remove their shader sections, but an empty texture slot does not necessarily remove a sample.

I’ve also found several edge cases to document separately from confirmed runtime bugs, including UV2 transforms, triplanar normal maps, and zero roughness.

## Assistant Commentary

The inventory contains 131 material properties, including hidden controls. I’m organizing the reference by Inspector section, with every enum choice, both Boolean states, numeric ranges, defaults, performance notes, and relevant issues. It also covers inherited resource properties and calls out places where this version’s documentation disagrees with its implementation.

## Assistant Commentary

Your settings are consistent with a matte, diffusely lit material. The main remaining simplification to consider is `diffuse_mode = Lambert`: the default Burley model still does extra view- and roughness-dependent work even with specular disabled. It would change the appearance somewhat, so I’m presenting it as an option.

The test sphere also has about 66,000 triangles. That matters when comparing per-pixel and per-vertex lighting; neither option’s speed can be inferred from its name alone.

## Assistant Commentary

The draft covers all 131 registered material properties, plus inherited controls, with no missing or duplicate entries. I’m doing a final consistency check of the renderer code, especially transparency and stencil: those paths have separate sorting, depth, and shadow rules that are easy to oversimplify.

## Assistant Commentary

The review is complete: 139 entries cover the 131 material properties and inherited controls, and the general review includes 14 documented issues or edge cases with evidence labels. Coverage and source-link checks pass. I’ll save the review and conversation in separate commits; the material and project settings remain unchanged.

## Assistant

Completed using your local Godot 4.7.2 source:

- [Complete property reference — all values, behavior, performance and issues](C:/Users/k/Repository/Veehiicuul/Documentation/StandardMaterial3DProperties.md)
- [General review and 14 issue findings](C:/Users/k/Repository/Veehiicuul/Documentation/StandardMaterial3DReview.md)

**Your configuration is sensible for matte, diffuse lighting.** The main further simplification to consider is Lambert instead of the default Burley. Empty texture slots still generate fallback samples, and disabling shadow reception does not disable casting.

Material unchanged. Performance conclusions are source-based, not benchmark results. Review and conversation committed separately.
