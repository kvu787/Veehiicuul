# Blender To Godot Material Roughness

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

</INSTRUCTIONS><environment_context>
  <cwd>C:\Users\k\Repository\Veehiicuul</cwd>
  <shell>powershell</shell>
  <current_date>2026-09-26</current_date>
  <timezone>America/Los_Angeles</timezone>
  <filesystem><workspace_roots><root>C:\Users\k\Repository\Veehiicuul</root><root>C:\Users\k\.codex\visualizations\2026\09\26\01a0dca4-4dd3-7513-9050-e6d9d023932a</root></workspace_roots><permission_profile type="managed"><file_system type="restricted"><entry access="read"><special>:root</special></entry><entry access="write"><path>C:\Users\k\Repository\Veehiicuul</path></entry><entry access="write"><path>C:\Users\k\.codex\visualizations\2026\09\26\01a0dca4-4dd3-7513-9050-e6d9d023932a</path></entry><entry access="write"><special>:slash_tmp</special></entry><entry access="write"><special>:tmpdir</special></entry><entry access="read"><path>C:\Users\k\Repository\Veehiicuul\.git</path></entry><entry access="read"><path>C:\Users\k\.codex\visualizations\2026\09\26\01a0dca4-4dd3-7513-9050-e6d9d023932a\.git</path></entry><entry access="read"><path>C:\Users\k\Repository\Veehiicuul\.agents</path></entry><entry access="read"><path>C:\Users\k\.codex\visualizations\2026\09\26\01a0dca4-4dd3-7513-9050-e6d9d023932a\.agents</path></entry><entry access="read"><path>C:\Users\k\Repository\Veehiicuul\.codex</path></entry><entry access="read"><path>C:\Users\k\.codex\visualizations\2026\09\26\01a0dca4-4dd3-7513-9050-e6d9d023932a\.codex</path></entry></file_system></permission_profile></filesystem>
</environment_context>

## User

when exporting a mesh that uses a principled bsdf material from blender to glb, and importing that glb into godot 4.7.2, is there anything blender-side that affects the resulting godot-side StandardMaterial3D.roughness value? use the source code at C:\Users\k\Repository\External\blender and C:\Users\k\Repository\External\Godot_4-7-2 to inform your answer.


## Assistant (commentary)

I’ll trace roughness through Blender’s glTF exporter and Godot’s importer using the local source trees.

## Assistant (commentary)

The normal export path copies the Principled BSDF’s Roughness value into glTF’s `roughnessFactor`, which Godot assigns directly to `StandardMaterial3D.roughness`. I’m checking connected nodes and export options for cases where the imported value differs from the Blender slider.

## Assistant (commentary)

Two cases matter here: a texture connected directly to Roughness leaves Godot’s numeric value at `1.0`, while a recognized texture-times-constant setup exports that constant. Also, Blender’s **Materials → Viewport** export option uses the separate **Viewport Display → Roughness** setting. The source trace is complete; I’m recording the conversation as required by the repository instructions.

## Assistant

Yes. With Blender’s normal **Materials → Export** option, the Principled BSDF’s **Roughness input directly controls Godot’s `StandardMaterial3D.roughness`**.

In your local sources:

- Blender’s [roughness exporter](C:/Users/k/Repository/External/blender/scripts/addons_core/io_scene_gltf2/blender/exp/material/pbr_metallic_roughness.py:233) reads that input and exports it as `pbrMetallicRoughness.roughnessFactor`.
- Godot’s [glTF importer](C:/Users/k/Repository/External/Godot_4-7-2/modules/gltf/gltf_document.cpp:3080) passes that factor directly to `material->set_roughness(...)`. There is no numerical remapping.

The result depends on what feeds Blender’s Roughness input:

| Blender setup                               | Godot’s numeric `roughness` |
| ------------------------------------------- | -------------------------- |
| Unconnected Roughness slider set to `0.35`   | `0.35`                     |
| Value node supplying `0.35`                  | `0.35`                     |
| Image texture connected directly            | `1.0`                      |
| Image texture × `0.35`, using Math: Multiply | `0.35`                     |

These cases follow Blender’s [constant and multiplier extraction](C:/Users/k/Repository/External/blender/scripts/addons_core/io_scene_gltf2/blender/exp/material/search_node_tree.py:310). It recognizes specific node patterns; it does not evaluate arbitrary shader graphs. If it cannot extract a factor, or the factor equals `1.0`, Blender omits it. Godot then uses `1.0`, matching the [glTF default](https://raw.githubusercontent.com/KhronosGroup/glTF/main/specification/2.0/schema/material.pbrMetallicRoughness.schema.json).

**A roughness texture affects the rendered roughness separately from that numeric property.** Godot imports the packed texture’s green channel and [multiplies its sampled value by `roughness`](C:/Users/k/Repository/External/Godot_4-7-2/scene/resources/material.cpp:1693). Thus, an Inspector value of `1.0` can still produce smooth regions through the texture.

**Export-option exception:** choosing **Materials → Viewport** makes Blender export **Material Properties → Viewport Display → Roughness** instead of the Principled input. The [viewport exporter](C:/Users/k/Repository/External/blender/scripts/addons_core/io_scene_gltf2/blender/exp/material/material_viewport.py:8) reads `blender_material.roughness` directly.

## User

provide a blender python script run from a blender text editor pane that prints out every material's name, its type (ie principled bsdf), and the roughness value if the type is principled bsdf


## Assistant (commentary)

I’ll write a script you can paste into Blender’s Text Editor that lists every material and the roughness inputs on its Principled BSDF nodes.

## Assistant

Paste this into Blender’s **Text Editor** and press **Alt+P**. View the output through **Window → Toggle System Console**.

A material can contain multiple shader types, so this lists every shader node, including disconnected nodes and nodes inside groups.

```python
import bpy


def find_shader_nodes(node_tree, prefix=""):
    for node in node_tree.nodes:
        node_path = prefix + node.name

        if node.type == "GROUP" and node.node_tree is not None:
            yield from find_shader_nodes(node.node_tree, node_path + " / ")
        elif any(socket.type == "SHADER" for socket in node.outputs):
            yield node_path, node


for material in sorted(bpy.data.materials, key=lambda item: item.name.casefold()):
    print(f"\nMaterial: {material.name}")

    if not material.use_nodes or material.node_tree is None:
        print("  Type: No node shader")
        continue

    shader_nodes = list(find_shader_nodes(material.node_tree))

    if not shader_nodes:
        print("  Type: No shader nodes found")
        continue

    for node_path, node in shader_nodes:
        print(f"  Type: {node.bl_label} | Node: {node_path}")

        if node.type == "BSDF_PRINCIPLED":
            roughness = node.inputs["Roughness"]

            if roughness.is_linked:
                print(
                    f"    Roughness: LINKED "
                    f"(stored slider value: {roughness.default_value:.6g}; "
                    "connected nodes are not evaluated)"
                )
            else:
                print(f"    Roughness: {roughness.default_value:.6g}")
```

**Linked inputs are flagged** because their stored slider value does not necessarily represent the roughness supplied by the connected nodes.
