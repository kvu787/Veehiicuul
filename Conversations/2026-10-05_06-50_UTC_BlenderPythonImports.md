# Conversation

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
</INSTRUCTIONS>

## User

<external_codex_apps_open_page>{"page_id":null}</external_codex_apps_open_page>

## User

when running a python script in the blender 4.5 text editor, how can i get that script to load other python files like libraries

## Assistant

I'll check Blender 4.5's documentation for how scripts in the Text Editor import other Python files.

## Assistant

Use normal Python `import` statements, but first add the folder containing your library files to `sys.path`. Blender supports importing modules from scripts run in its Text Editor. [Blender 4.5 documentation](https://docs.blender.org/api/4.5/info_overview.html)

For example, save a file called `GeometryLibrary.py` in `C:\Users\k\Repository\Veehiicuul\Blender`:

```python
# GeometryLibrary.py
import bpy


def CreateCube():
    bpy.ops.mesh.primitive_cube_add()
```

Then run this in Blender's Text Editor:

```python
import sys

libraryFolder = r"C:\Users\k\Repository\Veehiicuul\Blender"
if libraryFolder not in sys.path:
    sys.path.insert(0, libraryFolder)

import GeometryLibrary

GeometryLibrary.CreateCube()
```

Import the filename without `.py`. The library must be saved on disk for this example.

If your library folder is beside your saved `.blend` file, you can instead determine its path with:

```python
import bpy

libraryFolder = bpy.path.abspath("//")
```

Here, `//` means the folder containing the `.blend` file. Save the `.blend` first. Blender documents this search-path approach in its [scripting tips](https://docs.blender.org/api/main/info_tips_and_tricks.html).

**When editing the library:** Python caches imported modules. To pick up changes during the same Blender session, reload it before calling its functions:

```python
import importlib
import GeometryLibrary

importlib.reload(GeometryLibrary)
GeometryLibrary.CreateCube()
```

Reloading a module does not automatically reload other modules it imports.

## User

can a saved script reference another saved script using a relative path without adding anything to sys.path

## Assistant

I'll check how Blender handles script paths and Python's file-based module loading.

## Assistant

**Yes.** Use `importlib` to load a module directly from a file path, without changing `sys.path`. [Python documentation](https://docs.python.org/3.11/library/importlib.html#importing-a-source-file-directly)

For two saved files beside each other:

```text
Blender/
    Main.py
    GeometryLibrary.py
```

Run this from `Main.py` in Blender's Text Editor:

```python
import bpy
import importlib.util
import sys
from pathlib import Path

# Get the saved location of the script in the active Text Editor.
scriptPath = bpy.context.space_data.text.filepath
if not scriptPath:
    raise RuntimeError("Save this script to disk first.")

scriptFolder = Path(bpy.path.abspath(scriptPath)).resolve().parent
libraryPath = scriptFolder / "GeometryLibrary.py"

spec = importlib.util.spec_from_file_location("GeometryLibrary", libraryPath)
library = importlib.util.module_from_spec(spec)
sys.modules[spec.name] = library
spec.loader.exec_module(library)

library.CreateCube()
```

You can also use `scriptFolder / "Libraries" / "GeometryLibrary.py"` or `scriptFolder / ".." / "GeometryLibrary.py"`.

The `sys.modules` assignment registers the loaded module; it does **not** change the search path. This code executes the library again each time you run the main script, picking up saved edits.

Ordinary `import GeometryLibrary` does not automatically search beside a script run in Blender's Text Editor. Also, loading one file this way does not automatically make its neighboring files available to its own ordinary imports.

## User

what about python scripts in general (not tied to blender)? can they use any simpler ways?

## Assistant

For a normal Python script, importing a file beside it is much simpler. I'll show the basic layout and how subfolders work.

## Assistant

**Yes. For a normal script launched with `python Main.py`, a plain import is enough for files beside it.** Python automatically puts the main script's folder on `sys.path`; you don't need to modify it yourself. [Python documentation](https://docs.python.org/3/library/sys_path_init.html)

```text
MyProject/
    Main.py
    GeometryLibrary.py
```

In `Main.py`:

```python
import GeometryLibrary

GeometryLibrary.CreateCube()
```

Or import a particular function:

```python
from GeometryLibrary import CreateCube

CreateCube()
```

For a subfolder, make it a package:

```text
MyProject/
    Main.py
    Libraries/
        __init__.py
        GeometryLibrary.py
```

The `__init__.py` file can be empty. In `Main.py`:

```python
from Libraries import GeometryLibrary

GeometryLibrary.CreateCube()
```

Inside that package, one module can import a sibling using a **package-relative import**:

```python
# Inside Libraries/GeometryLibrary.py:
from . import MathLibrary
```

That assumes `MathLibrary.py` is also in `Libraries` and `GeometryLibrary` is loaded as part of the package. [Python modules and packages](https://docs.python.org/3/tutorial/modules.html)

Python imports use module names, not filesystem strings: `import "../GeometryLibrary.py"` is invalid. For files outside your project/package structure, direct loading with `importlib` remains an option.

## User

why doesn't that approach work with blender python

## Assistant

I'll check how Blender runs a Text Editor script, since that determines which folders Python searches for imports.

## Assistant

Because **adding the main script's folder is something Python does when starting up with `python Main.py`**. It is not something `import` does for every file being executed. [Python documentation](https://docs.python.org/3/library/sys_path_init.html)

Blender already has a running Python interpreter. When you click **Run Script**, Blender compiles and executes the Text Editor's contents inside that interpreter. It does not perform the startup step that adds the script's folder to `sys.path`. Saving the script on disk does not change that execution method. [Blender 4.5 implementation](https://github.com/blender/blender/blob/v4.5.0/source/blender/python/intern/bpy_interface_run.cc#L120-L154)

So with:

```text
MyProject/
    Main.py
    GeometryLibrary.py
```

- **`python Main.py`:** Python automatically includes `MyProject` in its import search path.
- **Blender's Run Script:** `MyProject` is not automatically included, so `import GeometryLibrary` fails unless that folder is already searchable.

Once you add that folder to Blender's `sys.path`, the same ordinary imports and subfolder packages work. Package-relative imports such as `from . import MathLibrary` still require the importing module to be loaded as part of a package.
