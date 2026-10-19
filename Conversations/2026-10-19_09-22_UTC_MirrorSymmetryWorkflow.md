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

## User

i don't fully understand this. explain:
Qualification: the current cleanup only dissolves coplanar seam edges and marked, straight center vertices connecting two edges. It preserves bends, junctions, and non-coplanar seam edges. That restriction may become limiting for more complex subdivision control meshes.

## Assistant

I'll explain what each restriction means for the control mesh, and how it can affect your choice of subdivision topology.

## Assistant

**These are restrictions I added to the tool, not requirements of mirroring or subdivision.** They mean that clicking **Remove Center** can sometimes leave the vertex in place.

The tool performs two separate operations:

1. **Remove edges running along the symmetry plane.** If the faces on either side form one flat surface, it removes their dividing edge. That is what "coplanar" means. If the faces meet at an angle, like the two sides of a roof, it keeps that edge.

2. **Remove the center vertices you marked.** After the first step, it removes a marked vertex only when exactly two edges meet there and form a straight line: `A---V---B` becomes `A-------B`. It keeps `V` if the edges bend there, or if three or more edges connect to it. A point where several edges meet is a "junction".

I chose these rules to preserve the **unsubdivided polygon surface**. Removing a straight midpoint can still change the **subdivided surface**, which is the effect you want to control.

The troublesome part is that these rules depend on the geometry. Moving a control point could make a previously flat seam non-flat. The tool would then keep that seam edge, potentially bringing back a center vertex and changing the subdivision result.

**For your intended workflow, this is a real limitation:** your keep/remove choices currently work only when those checks allow them. The tool does not yet give your explicit choices unconditional control over the center topology.

## User

does this work with any combination of mirror axis xyz toggles

## Assistant

I'll check which axis combinations the add-on currently supports.

## Assistant

No. **The current add-on accepts exactly one enabled axis:** X only, Y only, or Z only.

**XY, XZ, YZ, and XYZ are unsupported.** Enabling multiple axes produces "Use one mirror axis at a time" and leaves the previous generated output unchanged.

Supporting combinations would require cleanup across every enabled symmetry plane, including handling vertices where those planes intersect. This is a limitation of my add-on, not Blender's Mirror modifier.

## User

implement support for any combination of mirror axis xyz toggles

## Assistant

I'll extend the cleanup to all enabled mirror planes and test every axis combination, including vertices where planes intersect.

## Assistant

At plane intersections, removing all seam edges could also remove a vertex you chose to keep. I'll preserve its connecting edges in that case. I'll also support all toggles off by passing the input through unchanged.

## Assistant

All eight axis states pass: X, Y, Z, XY, XZ, YZ, XYZ, and all off. The tests also cover moved and rotated Mirror Objects, keep/remove choices at plane intersections, new geometry in Edit Mode, and cleanup before Subdivision. I'll update the instructions and embedded script in the working example.

## Assistant

Implemented all eight states: **X, Y, Z, XY, XZ, YZ, XYZ, and all axes off**.

Axis changes rebuild automatically. Kept vertices at plane intersections retain their necessary connections, and cleanup still runs before the unapplied Subdivision modifier.

Use the updated [add-on](C:/Users/k/Repository/Veehiicuul/Blender/MirrorCleanup/LiveMirrorCleanup.py) or open [MirrorWorkflowLive.blend](C:/Users/k/Repository/Veehiicuul/Blender/MirrorWorkflowLive.blend) and run its embedded script again.

Tests pass in Blender 4.5.14 for every combination, transformed Mirror Objects, intersection choices, and Subdivision. Existing bend and crease restrictions still apply.

## User

based on "C:\Users\k\Repository\Veehiicuul\Blender\MirrorWorkflowLive.blend", it appears that this workflow does not use the typical "stacking modifiers on one object" approach, because i don't see a subd modifier stacked atop a mirror modifier on the same object. explain what's going on.

## Assistant

I'll check how the two objects and their modifier stacks are connected in the saved file.

## Assistant

You're reading it correctly. **This implementation uses two objects, with Python connecting them.**

| Object                 | Role                                      | Modifier    |
| ---------------------- | ----------------------------------------- | ----------- |
| `AttemptedInput`       | The half mesh you edit                    | Mirror      |
| `AttemptedInputOutput` | The generated, cleaned control mesh       | Subdivision |
| `DesiredOutput`        | Your reference, independent of the system | Subdivision |

When the input changes, the add-on:

1. Reads the input's evaluated mesh **after Mirror**.
2. Copies that mesh and performs the seam cleanup on the copy.
3. Replaces `AttemptedInputOutput`'s base mesh with the cleaned result.
4. Blender evaluates the output object's **Subdivision modifier** on that new base mesh.

Thus, **Mirror -> cleanup -> Subdivision describes the processing order across two objects**, not a modifier stack on one object.

I implemented the custom cleanup as a Python mesh-generation step rather than a real modifier. The input appears as a wire cage and is hidden from rendering; the output supplies the visible subdivided surface.

Subdivision remains unapplied, and cleanup is reversible by rebuilding from the editable input. However, **the connection between the objects depends on the add-on running**. Without it, the output remains the last generated snapshot. Cleanup also cannot be reordered or disabled through the normal modifier stack.

So this provides editable input and non-destructive subdivision, but **it is not the conventional single-object modifier workflow**.

## User

what would it take to make this a conventional single-object modifier workflow?

## Assistant

I'll check Blender's options for putting the cleanup directly between Mirror and Subdivision on one object, and what each would take to implement.

## Assistant

Blender 4.5.14 cannot register a new mesh modifier from Python, and its Geometry Nodes have no direct Dissolve node. The cleanup therefore needs a substantial rewrite: either a Geometry Nodes reconstruction, whose feasibility still needs testing, or native C++ changes in a custom Blender build.

## Assistant

**The cleanup must become part of Blender's modifier evaluation**, producing this stack on one object:

```text
Editable mesh -> Mirror -> Seam Cleanup -> Subdivision
```

That is a substantial rewrite. I checked your installed Blender 4.5.14: it rejects registering a new modifier type from Python. The existing cleanup functions cannot simply be wrapped in a Python modifier class.

There are two possible routes:

1. **Implement it in Blender's C++ source.** Add a native Seam Cleanup modifier, or add the cleanup options directly to the existing Mirror modifier. The latter would give you **Mirror with selective cleanup -> Subdivision**. We would port the algorithm, expose the vertex-group controls, preserve mesh attributes, and test axis combinations, Edit Mode, undo, rendering, and subdivision. You would need to build and use a **custom Windows Blender executable**, and maintain the changes when upgrading. [Blender development setup](https://developer.blender.org/docs/handbook/new_developers/setup/).

2. **Reimplement it using Geometry Nodes.** A Geometry Nodes modifier can occupy the cleanup slot in a normal stack and run in stock Blender. However, Blender 4.5.14 has no direct Dissolve geometry node. Reproducing our operation would require constructing the resulting topology using other nodes. That needs a feasibility prototype; I have not established a robust equivalent that preserves the required topology and attributes. [Geometry Nodes as custom modifiers](https://docs.blender.org/manual/en/4.5/modeling/modifiers/introduction.html).

**My recommendation is to extend the native Mirror modifier if a custom Blender build is acceptable.** That is the most direct route to the conventional workflow you described. If stock Blender is required, the next step is a Geometry Nodes feasibility prototype.

Moving cleanup into the stack would not automatically remove its current bend and crease restrictions; changing those is a separate decision about how the topology should be generated.

## User

attempt to implement this with geometry nodes in this folder: "C:\Users\k\Repository\Veehiicuul\Blender\MirrorCleanup_GeometryNodes"

## Assistant

I will build a Geometry Nodes prototype in that folder and test whether it can preserve your selective center topology before subdivision in a single-object modifier stack.

## Assistant

I found a viable approach: group faces across removable seam edges, rebuild each group's boundary as a polygon, then remove only marked, straight center points. This can run entirely inside Geometry Nodes. I am testing the topology reconstruction first, especially face winding and vertices where mirror planes intersect.

## Assistant

The first prototype matches your `DesiredOutput` exactly, including face winding, while leaving the editable input unchanged. It now runs as `Mirror -> Selective Mirror Cleanup -> Subdivision` on one object. I am checking axis combinations, live edits, and subdivision creases before saving the example.

## Assistant

All eight axis states pass, including transformed Mirror Objects and kept vertices at plane intersections. The subdivision result also matches the reference, with edge creases preserved. The remaining work is packaging and a few safeguards: regions with holes will produce a visible warning and pass through unchanged.

## Assistant

The saved file works in a fresh Blender process with automatic Python execution disabled, including live keep/remove changes and axis toggles. The main remaining limitation is attribute handling: this prototype preserves subdivision creases and materials, but rebuilt faces lose UV maps and other corner attributes. I am documenting that clearly and finishing the checks.

## Assistant

The prototype now skips faces that do not need cleanup. On this machine, test grids with 100 and 900 input faces evaluated in about 5 ms and 20 ms respectively, before subdivision. The saved stack also renders successfully, and its result matches the reference.

## Assistant

Implemented a working Geometry Nodes prototype.

Open [MirrorWorkflowGeometryNodes.blend](C:/Users/k/Repository/Veehiicuul/Blender/MirrorCleanup_GeometryNodes/MirrorWorkflowGeometryNodes.blend). `AttemptedInput` now uses **Mirror -> Selective Mirror Cleanup -> Subdivision** on one object. It works after reopening without running Python.

- Assign center vertices to **RemoveCenter** at weight 1 to request removal; remove membership to keep them.
- Change XYZ on the normal Mirror modifier; cleanup follows every combination.
- `DesiredOutput` is only a reference object.

Tests confirm exact reference topology and subdivision, live Edit Mode updates, crease preservation, reopening, and rendering.

**Prototype limits:** bends and non-coplanar seams remain; regions with holes pass through with a warning; rebuilt faces do not preserve UV/corner data. [Full instructions and limitations](C:/Users/k/Repository/Veehiicuul/Blender/MirrorCleanup_GeometryNodes/ReadMe.md).
