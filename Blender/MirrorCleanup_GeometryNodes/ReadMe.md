# Geometry Nodes mirror cleanup prototype

This folder contains a conventional, single-object modifier workflow for Blender 4.5.14 LTS on Windows 11 x64:

```text
Editable mesh -> Mirror -> Selective Mirror Cleanup -> Subdivision
```

The cleanup is an actual Geometry Nodes modifier. The saved node groups and ordinary property drivers do all evaluation. There are no Python update handlers, timers, generated output objects, or add-ons required while modeling, reopening the file, or rendering. Python is used once to construct the node graph.

Everything needed is in this folder, apart from Blender itself. The fixture meshes are copies of the original example's geometry, stored in `ExampleMeshes.json`; preparing or testing this prototype does not read any repository files outside this folder.

## Try the prepared example

Open `MirrorWorkflowGeometryNodes.blend`, or double-click `Run.cmd`.

- `AttemptedInput` is the editable object on the left. Its modifier stack contains Mirror, Selective Mirror Cleanup, and Subdivision, in that order.
- `DesiredOutput` on the right is an independent reference. It can be deleted without affecting the workflow.
- No script needs to be run when opening the example. Automatic Python script execution can stay disabled.
- Disable Subdivision to inspect the cleaned control mesh. Disable Selective Mirror Cleanup to compare with ordinary Mirror topology.

Select `AttemptedInput`, enter Edit Mode, and select a center vertex. In Object Data Properties > Vertex Groups, choose `RemoveCenter`:

- **Remove the selected vertex from this group to keep it.**
- **Assign it to the group at weight 1 to request removal.** Weights above 0.5 count as marked.

The rear center is marked for removal. The raised center is kept. These settings reproduce the supplied DesiredOutput topology and its level-2 subdivision result. Editing the group changes the control topology before Subdivision, so the subdivided surface changes with it.

Toggle axes on the normal Mirror modifier. The cleanup's axis inputs follow through property drivers. X, Y, Z, XY, XZ, YZ, XYZ, and all axes off are supported. All off passes the mesh through unchanged.

## Use on another object

1. Add a Mirror modifier to the editable mesh and enable Merge. Enable its Edit Mode display as well.
2. Select that mesh in Object Mode. Open `GeometryNodesMirrorCleanup.py` in Blender's Text Editor and run it with Alt+P.
3. Add Subdivision after Selective Mirror Cleanup, or keep the existing later Subdivision modifier.
4. Assign chosen center vertices to the new `RemoveCenter` group.

Running setup again replaces this prototype's cleanup modifier and refreshes its attribute/material configuration. It preserves the source mesh, vertex groups, Mirror, and other modifiers. It uses the last Mirror in the stack.

If the Mirror modifier uses a Mirror Object, setup copies that reference into the cleanup. Later movement, rotation, and nonuniform scaling of that object are evaluated automatically. **If you change which object is used as the Mirror Object, rerun setup**, or update both `Mirror Object` and `Use Mirror Object` in the cleanup modifier. Axis toggles follow automatically; object-reference changes do not.

## How the node graph works

1. Identify manifold edges whose two endpoints lie on the same enabled mirror plane and whose adjacent face normals agree within tolerance.
2. Keep creased, sharp, material-boundary, and smoothing-boundary edges. Protect the incident seams of unmarked internal junctions so those vertices remain connected.
3. Pass unaffected faces through directly. Group affected faces across the remaining removable seam edges, then iterate over those groups in a Repeat Zone.
4. Verify that each group is planar, has a single closed boundary, and contains no retained interior edges or required interior vertices. Convert its boundary into an ordered cyclic curve. Delete marked, straight boundary points only when their degree after seam removal is two and they carry no subdivision crease.
5. Generate an ngon with that exact corner count, place its vertices at the boundary coordinates, and correct its winding using the original face normal.
6. Join polygons by original vertex identity, restore positions and supported attributes, and pass the result to the next modifier.

Welding uses encoded original vertex indices, rather than proximity in the actual model. Coincident disconnected shells remain disconnected. Concave polygon boundaries are supported. Loose edges and isolated vertices are retained.

The original editable mesh keeps its center vertices. Only the modifier's evaluated topology loses the selected points. This is why the operation remains reversible and why center vertices can still be selected in Edit Mode.

## Prototype limits

- The conservative geometric rules remain: non-coplanar seam edges, bends, and junctions with retained incident edges survive. A removal request is not an unconditional dissolve.
- A region must be planar and have one closed, non-branching boundary with at least three remaining points. It cannot enclose retained edges or vertices that still connect to other geometry. If any region fails these checks, the modifier displays a warning and passes the **entire mirrored mesh, including its attributes,** through unchanged. This avoids a partial cleanup producing mismatched neighboring boundaries. Matching average face normals alone do not qualify warped faces for cleanup.
- **UV maps and other corner attributes, UV seam flags, custom split normals, and arbitrary face attributes are not transferred to rebuilt faces.** Blender's `.uv_seam` attribute is not accessible through Named Attribute nodes, so it cannot act as a boundary in this prototype. Setup adds a modifier warning when it finds user corner attributes. This prototype is intended for control-mesh topology experiments, not a finished textured-asset pipeline.
- Vertex and edge subdivision creases, sharp edges, smooth/flat face shading, and assigned materials (including object-linked material overrides and empty slots) are supported. Any positive crease weight protects its feature, even below 0.001. Creased vertices and vertices touching creased or sharp edges are preserved even when marked for removal.
- Point attributes of types FLOAT, INT, INT8, FLOAT_VECTOR, FLOAT2, FLOAT_COLOR, BYTE_COLOR, BOOLEAN, and QUATERNION, plus vertex groups, are copied by original vertex identity. Compact storage types retain their original type and values.
- Material slots and the list of copied point attributes are captured at setup. Rerun setup after adding/replacing material slots or adding custom point attributes/vertex groups that should propagate. Changes to already configured values evaluate normally.
- Other edge attributes and point attribute types are not transferred to rebuilt geometry. Internal temporary attribute names start with `__MirrorCleanup`; reserve that prefix.
- The Repeat Zone scans the affected geometry for each affected face region; unaffected faces bypass reconstruction. This is a prototype for modest subdivision control meshes; large or highly fragmented seams can evaluate slowly. Do not place Subdivision before cleanup.
- Mirror Merge must be enabled, and its merge distance must actually connect the halves. The cleanup's Plane Tolerance is separate from Mirror's merge distance. It limits distance to a mirror plane in mirror-frame coordinates, and also limits a rebuilt region's deviation from its face plane in object-local coordinates. Direction Tolerance limits differences between unit face normals and between opposite boundary directions. Both tolerances must be nonnegative.
- The topology identity encoding uses 32-bit floating point coordinates internally. Meshes with 16 million or more vertices are outside the supported scope.
- Verify the modifier's warning indicator when a more complex mesh unexpectedly retains its seams.

## Files and commands

`GeometryNodesMirrorCleanup.py` constructs the reusable graph and installs it on the active mesh. `PrepareExample.py` rebuilds the prepared `.blend` from the local JSON fixture. `TestGeometryNodes.py` runs topology and behavior regression checks. `VerifySavedExample.py` checks the saved graph in a fresh process with automatic Python execution disabled.

The PowerShell wrapper defaults to `%USERPROFILE%\Program\blender-4.5.14-windows-x64\blender.exe`. Pass `-BlenderPath` to select a different installation of that Blender version.

```powershell
.\Run.cmd
.\Run.cmd -Mode Test
.\Run.cmd -Mode Prepare
.\Run.cmd -Mode Verify
.\Run.cmd -Mode Preview
```

Each wrapper run writes logs under `LogOutput/yyyy-MM-dd_HH-mm-ss/`. LogOutput is ignored by Git. No build step or custom Blender executable is required. Preview renders the saved modifier stack to `Preview.png` without saving camera or display changes into the example.

The tests check exact control-mesh coordinates, edges, face boundaries and winding, matching level-2 subdivision, reversible point choices, all eight axis states, transformed Mirror Objects, kept plane intersections, small and ordinary subdivision creases, all supported point attribute types, vertex groups, mesh/object material assignment, smooth shading, new Edit Mode geometry, frame changes, disabling the cleanup modifier, complete fallback for holes/retained interior geometry/warped faces, concave regions, bends, coincident disconnected shells, and empty/point-only/wire-only meshes.

`BenchmarkGeometryNodes.py` measures repeated evaluation after source edits. On the development machine, half-grids with 100 and 900 input faces produced 190 and 1770 cleaned faces in approximately 5 ms and 20 ms per warm evaluation, respectively. These timings cover Mirror and cleanup, exclude Subdivision, and are examples rather than performance guarantees for other topology.

Blender references: [modifier evaluation and Geometry Nodes](https://docs.blender.org/manual/en/4.5/modeling/modifiers/introduction.html), [Geometry Nodes in Blender 4.5](https://developer.blender.org/docs/release_notes/4.5/geometry_nodes/). The polygon reconstruction here is custom node composition; it does not depend on a built-in Dissolve node.
