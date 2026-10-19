# Live Mirror Cleanup

Blender 4.5.14 LTS on Windows 11 x64. This add-on keeps an editable half mesh with a standard Mirror modifier and generates a separate mesh with selective center cleanup. It does not apply or change the input geometry.

## Try the prepared example

1. Open `Blender/MirrorWorkflowLive.blend`.
2. Change an editor to **Text Editor**, select the embedded `LiveMirrorCleanup.py`, and press **Alt+P** with the mouse over that editor. This activates the tool for the current session.
3. In the 3D Viewport, press **N** and open **Mirror Cleanup**.
4. Select `AttemptedInput`, enter **Edit Mode**, and model the half normally. The solid `AttemptedInputOutput` updates automatically; the input is displayed as a wire cage.
5. In vertex selection mode, select a center vertex on the input and click **Remove Center** or **Keep Center** in the panel.

The rear center vertex is already assigned to `RemoveCenter`. The raised ridge center is kept. The generated base mesh matches `DesiredOutput` exactly: 6 vertices, 9 edges, and 5 faces, including the same face winding. Both result objects have level 2 Subdivision. Disable Subdivision on the generated output to inspect its base topology.

The original `Blender/MirrorWorkflow.blend` is unchanged. The live file contains a saved generated mesh, so its last saved result is visible even before the add-on runs. Run the script or install the add-on to resume automatic updates.

## Install for normal use

Open **Edit > Preferences > Add-ons**, open the menu at the top right, and choose **Install from Disk**. Select `Blender/MirrorCleanup/LiveMirrorCleanup.py` and enable **Live Mirror Cleanup**. Installation makes the panel available in future Blender sessions, without running the embedded script. Use either installation or the embedded script.

For another mesh:

1. Model a half mesh and add a standard **Mirror** modifier with one axis and **Merge** enabled. Keep its Edit Mode display enabled. Mirror must be the last enabled modifier on the input.
2. Select the input and click **Create Live Output** in the **Mirror Cleanup** panel.
3. Continue editing the input. Assign selected center vertices with **Remove Center** and **Keep Center**.
4. Add **Subdivision Surface** or other finishing modifiers to the output object. Keep them off the input after Mirror.

The two buttons assign or remove the selected input vertices from the ordinary `RemoveCenter` vertex group. A weight greater than 0.5 requests removal. All other center vertices are kept. Newly extruded or subdivided vertices may inherit group weights; use **Keep Center** if necessary.

## What the cleanup changes

The tool evaluates the input's modifier stack, including Mirror, into a separate mesh. It dissolves manifold edges whose endpoints lie on the mirror plane and whose adjacent faces are coplanar. It then dissolves only marked center vertices that connect two straight edge segments. Other input vertices survive.

Corners, junctions, and non-coplanar seam creases are preserved, even when marked. They define the shape, and removing them would change it. If a marked vertex remains, check whether it still has more than two connected edges or lies on a bend.

The output follows the input's object transform and retains materials and mesh data layers supported by BMesh. UV boundaries may change when faces are joined; finish UV work on the generated result after modeling. The output is regenerated, so edit the input rather than the output. Use **Select Input** and **Select Output** in the panel to switch objects.

Updates are processed on the main thread about every 0.1 seconds after dependency changes. Undo, redo, and file loading queue a rebuild. A failed update leaves the previous result and displays an error in the panel. **Refresh Output** performs an immediate rebuild.

This is an interactive modeling tool, not a native modifier or an animation baking system. Press **Refresh Output** before rendering or exporting. The input is hidden from renders when the output is created; render/export the generated output. For export, select the output and enable your exporter's selected-objects option. Source animation during rendering is not supported by this timer-based update system.

## Verification

Run from the repository root in modern PowerShell:

```powershell
& "$env:USERPROFILE\Program\blender-4.5.14-windows-x64\blender.exe" --background --factory-startup --python-exit-code 1 --python 'Blender\MirrorCleanup\TestLiveMirrorCleanup.py'
```

The checks compare exact base geometry and face winding to `DesiredOutput`, toggle the two center choices independently, exercise the marking buttons in Edit Mode, add new input geometry, verify mirrored movements and transforms, preserve a new seam crease, and check invalid-setting recovery and registration cleanup.

Recreate the sample with `PrepareMirrorWorkflow.py` using the same Blender command. It saves only `MirrorWorkflowLive.blend` and embeds the current add-on source.
