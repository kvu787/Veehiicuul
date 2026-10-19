# Blender mirror and symmetry workflow

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
When it makes sense, split work into multiple commits to make it easier to review.
Separate commits that record conversations from other commits.

## Markdown tables

Tables in Markdown must be padded and aligned in a way to make them easy to read in a plaintext editor, not only in a Markdown viewer.

## Mathematical notation in Markdown

Any mathematical notation in Markdown files (LaTeX, KaTeX, MathJax, etc) must display properly in VSCode's Markdown previewer, GitHub.com's Markdown displayer, and the markdown viewer in the Windows 11 ChatGPT app.

## PowerShell

All PowerShell scripts must use:

- Set-StrictMode -Version Latest
- $ErrorActionPreference = 'Stop'

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

## Using physical displays

If you need a display, only use NE18NZ2.

</INSTRUCTIONS><environment_context>
  <cwd>C:\Users\k\Repository\Veehiicuul</cwd>
  <shell>powershell</shell>
  <current_date>2026-10-19</current_date>
  <timezone>America/Los_Angeles</timezone>
  <filesystem><workspace_roots><root>C:\Users\k\Repository\Veehiicuul</root><root>C:\Users\k\.codex\visualizations\2026\10\19\01a15376-42ab-7431-a607-88d5cc4966cf</root></workspace_roots><permission_profile type="managed"><file_system type="restricted"><entry access="read"><special>:root</special></entry><entry access="write"><path>C:\Users\k\Repository\Veehiicuul</path></entry><entry access="write"><path>C:\Users\k\.codex\visualizations\2026\10\19\01a15376-42ab-7431-a607-88d5cc4966cf</path></entry><entry access="write"><special>:slash_tmp</special></entry><entry access="write"><special>:tmpdir</special></entry><entry access="read"><path>C:\Users\k\Repository\Veehiicuul\.git</path></entry><entry access="read"><path>C:\Users\k\.codex\visualizations\2026\10\19\01a15376-42ab-7431-a607-88d5cc4966cf\.git</path></entry><entry access="read"><path>C:\Users\k\Repository\Veehiicuul\.agents</path></entry><entry access="read"><path>C:\Users\k\.codex\visualizations\2026\10\19\01a15376-42ab-7431-a607-88d5cc4966cf\.agents</path></entry><entry access="read"><path>C:\Users\k\Repository\Veehiicuul\.codex</path></entry><entry access="read"><path>C:\Users\k\.codex\visualizations\2026\10\19\01a15376-42ab-7431-a607-88d5cc4966cf\.codex</path></entry><entry access="read"><path>C:\Users\k\Repository\Veehiicuul\.aws</path></entry><entry access="read"><path>C:\Users\k\.codex\visualizations\2026\10\19\01a15376-42ab-7431-a607-88d5cc4966cf\.aws</path></entry></file_system></permission_profile></filesystem>
</environment_context>

## User

<external_codex_apps_open_page>{"page_id":null}</external_codex_apps_open_page>

## User

i am trying to setup what should be a simple mirror/symmetry workflow, but it's not working.

in "C:\Users\k\Repository\Veehiicuul\Blender\MirrorWorkflow.blend", there are two objects. I want the end result to be the "DesiredOutput" object. As you can see in the output object, two things can happen when an edge crosses the mirror axis: a vertex may or may not bisect the edge. in the input object (demonstrated by the "AttemptedInput" object) i should be able to control whether or not a vertex bisects each edge that crosses the mirror.

## Assistant

I'll inspect the Blender file and its mirror setup, then work out how to preserve your choice of a center vertex for each crossing edge.

## Assistant

The mismatch is in the topology: Mirror keeps a seam down the center, while your target joins faces across that seam. Your target also keeps the center vertex on the raised ridge but removes it from the rear edge. I'll check which built-in workflow preserves that distinction.

## Assistant

I verified an exact match to `DesiredOutput`: apply Mirror, dissolve the three center seam edges while keeping their vertices, then dissolve only the rear center vertex.

For continued editing, X symmetry preserves this topology and mirrors vertex movements. It does not mirror every topology operation; deleting a vertex in my test affected only one side. Standard Mirror has no per-edge control for removing center vertices.

## Assistant

I inspected and tested your file. **The standard Mirror modifier cannot provide this per-edge choice.**

Your modifier has **Bisect off** already. The center vertices come from your input mesh. **Merge** welds them to their mirrored copies; it does not remove them or join faces across the seam. Enabling Bisect would cut crossing geometry, without adding selective control. [Mirror documentation](https://docs.blender.org/manual/en/5.0/modeling/modifiers/generate/mirror.html).

To turn `AttemptedInput` into your exact target:

1. In Object Mode, **apply Mirror**.
2. In Edit Mode, select the **three edges lying entirely on the center plane**. Use **Dissolve Edges**, then press **F9** and disable **Dissolve Vertices**.
3. Select only the **rear center vertex**, at local `(0, 1, 0)`, and use **Dissolve Vertices**. Keep the raised ridge's center vertex.

I verified that this produces exactly the same vertex positions, edges, and faces as `DesiredOutput`: **6 vertices, 9 edges, 5 faces**.

For ongoing editing, you can use the complete `DesiredOutput` mesh and enable **X symmetry** in the Edit Mode tool settings. This mirrors movements of existing vertex pairs while preserving your chosen center topology. [Symmetry settings](https://docs.blender.org/manual/en/5.0/modeling/meshes/tools/tool_settings.html).

**The limitation:** Edit Mode symmetry does not mirror every topology operation. If you need to model only one half, automatically mirror new geometry, and independently choose which center vertices disappear, that requires an additional cleanup step or custom tooling.

Your `.blend` file is unchanged.

## User

this is what i want: If you need to model only one half, automatically mirror new geometry, and independently choose which center vertices disappear, that requires an additional cleanup step or custom tooling.

are there other 3d modeling programs that provide this workflow

## Assistant

I'll check which modeling programs support this workflow, especially whether they let you control the center topology while continuing to model only one half.

## Assistant

Houdini looks promising because it can keep mirroring and selective cleanup as live steps. 3ds Max also supports cleanup above a live Symmetry modifier, though changes to the input topology can invalidate those edits. I haven't found a documented single symmetry tool with the exact per-vertex control you want.

## Assistant

**Yes, through built-in procedural workflows. Houdini is the strongest fit I found.** I haven't verified a program with your exact behavior packaged as a single symmetry setting.

**Houdini:** You can keep the half mesh editable, mirror it, and perform selective cleanup downstream. Its Mirror node welds the seam; Dissolve merges selected faces across edges; point groups can identify individual center points for removal. A suitable setup would be:

`Half mesh -> Mirror -> Dissolve seam edges -> Remove chosen center points -> Subdivision`

That lets the input retain modeling vertices while the output omits selected ones. This is a proposed setup using documented native tools, rather than a tested import of your example. [Mirror](https://www.sidefx.com/docs/houdini/nodes/sop/mirror.html), [Dissolve](https://www.sidefx.com/docs/houdini/nodes/sop/dissolve.html), [Blast point groups](https://www.sidefx.com/docs/houdini/nodes/sop/blast.html).

**3ds Max:** A stack of `Editable Poly -> Symmetry -> Edit Poly -> TurboSmooth` lets you model the half below Symmetry and selectively remove seam edges and vertices in the Edit Poly above it. However, adding or deleting input geometry can invalidate the stored cleanup selections. It supports the arrangement, but that limitation matters for your requirement to keep creating geometry. [Symmetry](https://help.autodesk.com/cloudhelp/2026/ENU/3DSMax-Modifiers/files/GUID-EB0B7B9B-117D-4CC3-A966-A3E007E0C68A.htm), [Edit Poly](https://help.autodesk.com/cloudhelp/2026/ENU/3DSMax-Modifiers/files/GUID-1230619A-8CB3-4411-B07D-C133716F61F9.htm), [topology dependencies](https://help.autodesk.com/cloudhelp/2017/ENU/3DSMax/files/GUID-BBC25115-E5C2-4E81-BF39-0CE1BC727070.htm).

**Cinema 4D:** It has live half-mesh symmetry, and its separate Symmetrize command has a Dissolve option for removing cut-generated points and edges. However, I could not verify your required independent keep/remove choice within the live Symmetry object, so I would not call it a confirmed match. [Live Symmetry](https://help.maxon.net/c4d/en-us/Content/html/OSYMMETRYGENERATOR.html), [Symmetrize options](https://help.maxon.net/c4d/2025/en-us/Content/html/Symmeterize_Tool.html).

For your exact requirement, **I'd investigate Houdini first**, with the understanding that it requires a small node setup.

## User

this is what i want: If you need to model only one half, automatically mirror new geometry, and independently choose which center vertices disappear, that requires an additional cleanup step or custom tooling.

how do i get this in blender?

## Assistant

I'll set up a live cleanup step after Mirror, with a vertex group to choose which center vertices disappear, and test it against your `DesiredOutput` mesh.

## Assistant

The tool will leave your half mesh editable and generate a separate result. Mirror will still create new geometry; the cleanup will join coplanar faces across the seam and remove straight center vertices assigned to `RemoveCenter`. I'll also save a working copy of your example with the rear center vertex marked for removal.

## Assistant

The tests pass in Blender 4.5.14. The output matches `DesiredOutput` exactly, both center-vertex choices work independently, and new geometry added in Edit Mode appears on both sides. The cleanup also preserves corners and seam creases, since removing those would change the shape.

## Assistant

The working copy is ready. Save/reopen tests also pass, and the generated mesh remains visible when the add-on is inactive. I'm checking the embedded script now; once activated, the panel provides `Remove Center` and `Keep Center` buttons for the input mesh.

## Assistant

I made a working add-on and a prepared example.

1. Open [MirrorWorkflowLive.blend](C:/Users/k/Repository/Veehiicuul/Blender/MirrorWorkflowLive.blend).
2. Change an editor to **Text Editor**, select the embedded `LiveMirrorCleanup.py`, and press **Alt+P**.
3. In the 3D Viewport, press **N** and open **Mirror Cleanup**.
4. Edit `AttemptedInput`. Select center vertices and click **Remove Center** or **Keep Center**.

Continue modeling one half; the separate `AttemptedInputOutput` mesh updates automatically. The example already removes the rear center vertex and keeps the raised one, matching `DesiredOutput`.

For permanent installation, use **Preferences > Add-ons > Install from Disk** with [LiveMirrorCleanup.py](C:/Users/k/Repository/Veehiicuul/Blender/MirrorCleanup/LiveMirrorCleanup.py), then enable it. [Full instructions](C:/Users/k/Repository/Veehiicuul/Blender/MirrorCleanup/ReadMe.md).

Removal is limited to straight center vertices connecting two edges. Corners, junctions, and seam creases are preserved.

Tested in Blender 4.5.14, including new geometry and save/reopen. Your original file is unchanged.

## User

a key requirement for this workflow is that it works well with non-destructive subdivision, because the presence or not of vertices for the topology that is input into subd affects the end result. does your approach meet this requirement?

## Assistant

I'll verify that Subdivision receives the cleaned topology and that changing a center-vertex choice updates the subdivided result.

## Assistant

Yes, for your example: cleanup produces the control mesh before Subdivision evaluates it. I verified that changing either center-vertex choice changes the subdivided result, and restoring your choices reproduces `DesiredOutput`.

One limitation remains: the cleanup preserves bends, junctions, and non-coplanar seam edges. Those restrictions may matter as you develop a more complex control mesh.

## Assistant

**Yes, for your example. Cleanup happens before Subdivision:**

`Editable half -> Mirror -> selective cleanup -> Subdivision`

The output's Subdivision modifier remains **unapplied and adjustable**. Rebuilding the output replaces only its base control mesh.

I verified that:

- Your intended 6-vertex control mesh produces the same subdivided result as `DesiredOutput`.
- Keeping the rear center vertex changes the control mesh to 7 vertices and changes the subdivided surface.
- Removing the raised center vertex as well produces 5 vertices and another different subdivided surface.
- Restoring your choices restores the target result, within floating-point tolerance.

**Qualification:** the current cleanup only dissolves coplanar seam edges and marked, straight center vertices connecting two edges. It preserves bends, junctions, and non-coplanar seam edges. That restriction may become limiting for more complex subdivision control meshes.

I added these subdivision checks to the [regression tests](C:/Users/k/Repository/Veehiicuul/Blender/MirrorCleanup/TestLiveMirrorCleanup.py); they pass in Blender 4.5.14.
