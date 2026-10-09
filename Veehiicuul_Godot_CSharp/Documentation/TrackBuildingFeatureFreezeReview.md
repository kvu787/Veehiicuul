# Track building feature freeze review

Reviewed on October 9, 2026, against repository baseline `d7009a9`, using Windows 11 x64, Blender 4.5.14 LTS, Godot 4.7.2 .NET, and .NET SDK 10.0.401.

The current Blender-to-Godot feature set is sufficient for continuing to author and play tracks while development focus moves elsewhere. Freeze its feature scope now and finish the bounded reliability work below before treating the workflow as settled. New track content, explicit track registration, and release rebuilds remain routine authoring work.

The existing game builds and starts successfully. A new track created from `TrackTemplate.blend` also builds, starts, switches to Ribeye and back, and switches through all six configured cars. These automated checks ran headless; visual appearance and physical controller play remain part of the final acceptance check.

## Findings to close before settling the workflow

### P1 Preserve the model and collider pair when export fails

[ExportToVeehiicuul.py](../../Blender/Scripts/ExportToVeehiicuul.py) writes the live `<TrackName>_ColliderData.json` before calling the GLB exporter, at lines 173 and 175. If GLB export raises or returns `CANCELLED`, the old GLB and new collider JSON remain together. The game can then show one track boundary and collide against another.

A failure injection returning `CANCELLED` reproduced this: the previous model remained while the collider JSON was overwritten. Stage both files before publishing either. Add a failure regression proving that both previous files survive a cancelled or failed GLB export, and define restoration behavior if publishing the pair fails.

### P1 Complete the new track and rebuild instructions

[HowTo-CreateANewTrack.md](HowTo-CreateANewTrack.md) omits required steps:

- Each track needs `<TrackName>_Settings.json`. [TrackSwitcher.cs](../Veehiicuul/Source/GameDataAndLogic/Track/TrackSwitcher.cs) reads it before loading the scene. Copy Ribeye's settings as a starting point, then verify car names, acceleration values, camera settings, and `StartCarIndex`.
- The scene filename is `<TrackName>_Scene.tscn`, but its root must be a `Node3D` named exactly `Track`. A scene created with the track name as its root fails initialization.
- After preparing a track for export, re-enable both `TrackBuilder/Input` and `TrackBuilder/Input/Outlines` before rebuilding. Ribeye is saved with both excluded; attempting an immediate rebuild fails. TrackBuilder's success state keeps the input outlines enabled and excludes the generated `OutlineMeshes`.
- For export, the exporter requires the individual `Camera`, `Templates`, `TrackBuilder/Input/Outlines`, and `TrackBuilder/Output/OutlineMeshes` layer collections to have their own exclusion flags set. Excluding only their parent does not satisfy this check.
- Also exclude `ColorBlocks`, `Checkpoints_Default`, and `Decorations_Default` when preparing a playable track. The guide's `*_Defaults` spelling does not match the actual singular `_Default` names.
- Preserve the `Model/SlopeCarPlaceholder` mesh, configured car meshes, complete camera hierarchy, and identity transforms for the track root and `Model`. Camera and sunlight transforms can follow the existing scene.
- After asset export and Godot import, rebuild the release application. `Run.cmd` launches an existing build, so editing source assets alone does not update the packaged game. Preserve the `DisableSpecularImport.gd` import callback and current GLB import settings.
- Copying only the contents of `TrackBuilder.py` into a Blender text block does not run a build. Preserve or add the final `build_track(...)` invocation and track-specific parameters. The template and Ribeye currently contain matching library code followed by their own invocation.

Write one complete new-track procedure and a shorter repeat-edit procedure. Run them against a newly named track before signoff. The isolated new-track probe passed after supplying the missing settings, exact root name, import settings, registration, and rebuild steps.

### Resolved NURBS contact and offset station alignment

[TrackBuilder.py](../../Blender/TrackBuilder/TrackBuilder.py), lines 650-710, aligns independently canonicalized curve evaluations to the nearest dense vertex and then treats that vertex as an exact shared parameter origin. A nearby dense sample need not represent the authored sample's parameter when the resolutions do not divide evenly.

The template's supported cyclic NURBS track, with both authored resolutions set to 7 and object Z rotation set to 0.23 radians, produced 21 contact points and 768 reference points. At barrier width 1, the first forced offset station differed by approximately **0.07758 units** from the dense offset interpolated at the same source parameter. The documented adaptive tolerance is **0.001 units**. A complete build still succeeded.

The independent comparison preserved Blender's common sample origin and winding before canonicalization; the two evaluation origins coincided exactly. Preserve a shared spline parameter origin through both evaluations, or account for the fractional phase in pairing. Test against that shared-origin reference rather than the existing legacy merge, which repeats the same assumption. This is already identified in the TrackBuilder TODO and applies to the NURBS workflow actually supported here.

The fix preserves Blender's shared parameter origin and carries the canonical contact start station into the integer merge without snapping. Copied curve transforms remain exact. A regression independently samples Blender's cyclic NURBS parameters at resolutions 7, 75, and 38, checks both windings and outer/inner offsets, and verifies forced stations within 0.000001 units and adaptive error within 0.001 units. The originally failing resolution 7 case passes, as do the existing mesh and representative-curve geometry hashes.

The exact reproduced curve now has zero measured first-station alignment error and a maximum offset deviation of 0.000999582169 units, below the 0.001-unit limit. TrackTemplate and Ribeye contain the corrected embedded builder and regenerated output. Their contact outlines are unchanged, and Ribeye's exported model remains byte-identical. The Godot release build and exported startup pass. Of 35 Blender test methods, 34 pass; only the previously identified numeric-minimum method still fails in its three parameter subtests.

The exact track-facing contact boundary is preserved. The defect affected offset correspondence and the claimed away-edge accuracy; it did not establish a collider mismatch in the Ribeye export.

### P2 Restore a useful green regression suite

The Blender integration suite ran **34 tests** in approximately **18.6 seconds**. Thirty-three test methods passed; the numeric-boundary method failed in three parameter subtests. All ten committed fixtures produced their expected success or failure outcomes.

[TestTrackBuilder.py](../../Blender/TrackBuilder/TestTrackBuilder.py), lines 1313-1332, still expects the former minimum of `0.1`. The implementation's minimum is `0.00390625`, matching the thin barriers in both saved authoring files. Update the boundary tests and the documented contract to the intended current minimum. Do not restore the old restriction merely to satisfy obsolete tests. Exercise a complete build at the minimum as well as each parameter's immediate lower neighbor.

The README's minimum-dimension contract and example executable paths are stale. Test commands name Blender 4.5.12, while the available and tested installation is 4.5.14. The test documentation also claims `TestArtifacts` is gitignored, but the current ignore rules do not ignore it; a test run leaves untracked artifacts. Correct these maintenance instructions and ignore generated artifacts and Python bytecode caches.

## Smaller existing defects

These can remain explicitly recorded maintenance work without reopening feature design:

- Construction rollback preserves the old output, but a failure injected into `Mesh.from_pydata` leaves one orphan mesh datablock. Collection setup also precedes the cleanup boundary. Track every newly created datablock through construction and test cleanup at each failure point. The observed failure did not replace the previous track.
- Fixture generation looks up file-wide `Output` rather than the direct `TrackBuilder/Output` child. Keep fixture regeneration out of routine track authoring; scope this cleanup correctly before using arbitrary files as generator inputs.
- Test and benchmark geometry hashes omit edges. Existing explicit outline-connectivity assertions still provide coverage, but hashes alone cannot certify edge-only collision topology. Include edge connectivity when strengthening the geometry oracle.
- Blender scene validation does not enforce every runtime requirement, such as the presence of the placeholder and all car names referenced by settings. Keep these in the acceptance checklist; stronger preflight validation is a maintenance improvement if it prevents recurring authoring failures.

## Proposed freeze boundary

Freeze the existing implementation and contracts for:

- Closed planar mesh and cyclic NURBS authoring, with the documented supported curve feature set and material rules.
- Ground, track, island, barrier, and collision-outline generation; the current barrier material segmentation behavior.
- The Blender template hierarchy, vehicle and spawn conventions, GLB and collider JSON export, and Godot import configuration.
- Track scene and settings conventions, collision loading, car selection, and the existing camera behavior needed to play authored tracks.
- The Windows release build and run workflow with the specified local Blender, Godot, and .NET toolchain.

Continue to allow:

- New or revised `.blend` files, exported models and colliders, decorations, materials, camera framing, and per-track settings.
- Appending names to `Main.TrackNames`, selecting `InitialTrackIndex`, and rebuilding. These are the current explicit content-registration steps.
- Bug fixes, recovery improvements, accurate documentation, and regressions protecting the existing workflow.

Defer automatic discovery, hot reload, new outline types, new segmentation rules, object batching, caching, broad editor tooling, and unrelated rendering or gameplay redesigns. The performance TODO does not need to be completed to freeze this workflow. No cross-commit asset compatibility layer is needed.

Retain the tested local toolchain while relying on the frozen workflow. Treat a toolchain upgrade as a separate maintenance change with the same acceptance checks.

## Acceptance checklist

1. Close the remaining findings above and rerun the Blender suite to a green result. Verify failed export preserves both old live files; the corrected curve regression already checks the documented error contract.
2. Follow the revised instructions from a newly named template copy. Export, import, configure its settings and scene, register it, and run `Build.cmd` and `Run.cmd`.
3. Play the new track and Ribeye with the physical controller. Check the start position, every configured car, outer and inner boundary resets, track switching both ways, camera follow, zoom, reset, and quit. Decorations and checkpoints remain visual content; custom track collision comes from the generated outlines.
4. Change the new track outline and repeat the edit, rebuild, export, import, and release-build loop. Confirm the packaged game's displayed boundary and collision behavior both follow the change.
5. Record the tested commit as the baseline and put the scoped implementation into maintenance mode. Keep authoring content changes separate from workflow fixes so either remains easy to review.

## Verification evidence

- Template and Ribeye validation passed with the Text Editor environment guard bypassed only for background inspection.
- Embedded export and validation scripts matched their repository sources. Embedded TrackBuilder library code matched the source; each text block adds its own build invocation.
- A fresh Ribeye GLB and collider export matched the committed files byte for byte. Rebuilding Ribeye after re-enabling its input hierarchy also reproduced the committed collider JSON.
- The application's optimized `ExportRelease` build and Windows export passed with zero warnings and zero errors. The exported application's headless startup exited with code 0 and constructed the 800-edge Ribeye collision index.
- The isolated template-derived track's release export and startup passed. Its 377-edge collision index loaded, input actions switched to Ribeye and back, and all six car selections succeeded before a clean exit.
- Failure probes reproduced the mixed-generation export and the orphan-mesh cleanup defect. The supported NURBS probe reproduced the phase-alignment error while successfully building.

Ignored local evidence from the original review is in `Build/TrackWorkflowReview`: `Inspection.log`, `BoundaryChecks.log`, `GameProbeBuild.log`, the two game console logs, exported assets, and the verification scripts. The original integration report is retained there as `TrackBuilderTestReport.txt`. The follow-up alignment fix's before/after regression logs, saved-track verification, staged asset backups, and Godot build/startup logs are in `Build/NurbsAlignmentFix`. The original review left source files unchanged; the follow-up fix updates TrackBuilder and both embedded authoring copies.
