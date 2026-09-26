# Godot material extraction and the specular import script

Investigated on September 25, 2026 using the installed Godot
`4.7.2.stable.mono.official.ed1daf0bf` and the matching source checkout at
`C:/Users/k/Repository/External/Godot_4-7-2`, commit
`ed1daf0bf001b61586d9930840f2f1394092c079`.

## Finding

The existing `DisableSpecularImport.gd` changes all 39 materials in the track,
but with either extraction option it changes them **after their external files
have already been saved**. It never saves those external materials itself.
Godot subsequently saves the imported scene with references to those files,
rather than embedding the modified material values.

Consequently, a material can have `metallic_specular = 0.0` in memory while its
file loads as `0.5`. Keeping some material objects alive, then loading others
from disk, produces a mixture. This was reproduced with the user's actual GLB
and installed engine in disposable copies of the project.

The repository's current import file selects Keep Internal and has an empty
`_subresources` dictionary. No extracted track materials were present in the
working project when inspected. The tests establish a mechanism that reproduces
the reported behavior; they do not identify which particular objects the user's
earlier editor session retained.

## Source trace

1. [`_post_fix_node()`](C:/Users/k/Repository/External/Godot_4-7-2/editor/import/3d/resource_importer_scene.cpp:1667)
   writes a material when the destination does not exist or extraction mode is
   `2` (Extract and Overwrite). It then loads the file using `CACHE_MODE_REPLACE`
   and assigns the external material to the mesh.
2. [The importer calls that processing step](C:/Users/k/Repository/External/Godot_4-7-2/editor/import/3d/resource_importer_scene.cpp:3338)
   before running the configured
   [`EditorScenePostImport` callback](C:/Users/k/Repository/External/Godot_4-7-2/editor/import/3d/resource_importer_scene.cpp:3435).
3. [The project's callback](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Source/Editor/DisableSpecularImport.gd:17)
   assigns `material.metallic_specular = 0.0` to each active `BaseMaterial3D`
   encountered during its scene traversal. It has no `ResourceSaver.save()` call.
4. [The importer saves the PackedScene](C:/Users/k/Repository/External/Godot_4-7-2/editor/import/3d/resource_importer_scene.cpp:3476).
   [The binary resource saver](C:/Users/k/Repository/External/Godot_4-7-2/core/io/resource_format_binary.cpp:1992)
   records external resources as dependencies and stops traversing their
   properties. Saving the scene does not recursively save those material files.
5. [The importer frees its temporary scene](C:/Users/k/Repository/External/Godot_4-7-2/editor/import/3d/resource_importer_scene.cpp:3491).
   Material objects retained elsewhere can preserve the callback's changes;
   materials subsequently loaded from disk use the saved values.

An extracted `.tres` need not explicitly say `metallic_specular = 0.5`.
[The material constructor defaults to 0.5](C:/Users/k/Repository/External/Godot_4-7-2/scene/resources/material.cpp:3913),
and [the text saver omits default-valued properties](C:/Users/k/Repository/External/Godot_4-7-2/scene/resources/resource_format_text.cpp:1986).
The freshly extracted `CheckeredLineWhite.tres`, for example, contained only
`resource_name` in its resource body.

The documented [EditorScenePostImport hook](https://docs.godotengine.org/en/latest/classes/class_editorscenepostimport.html)
is a scene post-processing callback. The precise extraction/save ordering above
comes from the supplied 4.7.2 source, not an assumption about another release.

## Reproduction results

Each test used the tracked files from `Veehiicuul_Godot_CSharp/Veehiicuul` copied
to a separate temporary directory. The original project and engine source were
not edited. The existing import script was instrumented only in the copies.
Imports used the installed .NET executable with `--headless --editor --import`.

| Import setting        | Immediately after callback | Extracted files, uncached load | Scene loaded in a fresh process |
| --------------------- | -------------------------- | ------------------------------ | ------------------------------- |
| Keep Internal         | All 39 at 0.0              | No extracted files             | All 39 at 0.0                   |
| Extract Once          | All 39 at 0.0              | All 39 at 0.5                  | All 39 at 0.5                   |
| Extract and Overwrite | All 39 at 0.0              | All 39 at 0.5                  | All 39 at 0.5                   |

The file values were read using `ResourceLoader.CACHE_MODE_IGNORE`, so the
check could not silently return the callback's modified cached object. The
fresh-process check instantiated the imported PackedScene and examined its
distinct active materials.

To reproduce the mixed result, the diagnostic callback retained exactly one
material reference using an Engine metadata entry. A deferred probe ran after
the import and loaded each external material normally. In **both** extraction
modes, the retained material was cached and returned `0.0`; the other 38 were
uncached and returned `0.5`. The diagnostic retained reference was then released.

A separate test seeded the external files with one material at `0.0` and 38 at
`0.5`, then reimported with the original callback behavior:

- **Extract Once** preserved that mixture on disk: it keeps existing files.
- **Extract and Overwrite** left all 39 at `0.5` on disk: it wrote the newly
  imported materials before the callback modified their loaded counterparts.

The importer reached the diagnostic callbacks and every test process exited
with code 0. Headless editor logs also contained unrelated sandbox diagnostics
about reading the system certificate store and saving the portable editor's
global settings. No permission escalation was needed for the material checks.

## Fix and scope

The smallest change to the existing feature would be to collect distinct
materials, set their specular to zero, and explicitly save each external
material to its existing resource path, checking the returned error. Built-in
materials should continue to be saved as part of the imported scene. Avoid
saving the same shared material repeatedly for every surface.

This was tested in the disposable copies: an explicit save of each distinct
external material after the assignment produced **39 of 39 at 0.0**, both with
uncached file loads and when loading the scene in a fresh process, for both
extraction modes.

Such a fix deliberately enforces zero specular on existing external materials
even in Extract Once mode. It can therefore overwrite a deliberate specular edit
to an external material. If the intended policy is instead to initialize newly
extracted files while preserving later manual edits, use an earlier material
import hook: the
[`EditorScenePostImportPlugin` material callback](C:/Users/k/Repository/External/Godot_4-7-2/editor/import/3d/resource_importer_scene.cpp:1653)
runs before extraction. That alternative was source-reviewed, not implemented
or runtime-tested here, and would require editor plugin registration.

Keep Internal already works for this feature, provided materials are actually
internal rather than separately configured to use external resources.

This investigation changes documentation and records the conversation. It does
not change the application's importer, its import settings, or engine source.

## Follow-up: Keep Internal exceptions and preview behavior

With the track's current `materials/extract=0` and `_subresources={}`, all 39
distinct materials survive a fresh-process load at zero specular. The extraction
persistence problem does not occur in that configuration. The following related
cases were checked after the user asked about other impactful issues.

**Per-material Use External overrides still apply.** The
[`use_external/enabled` branch](C:/Users/k/Repository/External/Godot_4-7-2/editor/import/3d/resource_importer_scene.cpp:1676)
runs independently of the automatic material extraction setting. A disposable
copy with Keep Internal and only `CheckeredLineWhite` redirected to an external
material at specular 0.5 finished the callback with all 39 materials at 0.0,
but a fresh process loaded 38 at 0.0 and that external material at 0.5.

**A separately saved mesh has the same save-order problem.**
[`_generate_meshes()` saves external mesh files](C:/Users/k/Repository/External/Godot_4-7-2/editor/import/3d/resource_importer_scene.cpp:2857)
before the post-import script runs. Materials embedded in that mesh file are
saved too early, even though automatic material extraction is Keep Internal.
In a disposable copy, enabling Save to File only for
`Track009_MiniComb4_Plane_005` produced an external mesh containing the two
checkered-line materials. The callback set all 39 materials to 0.0, but a fresh
scene load found 37 at 0.0 and those two at 0.5. The source GLB was unchanged.
The current track enables neither this setting nor per-material external paths.

The earlier proposed fix of saving external materials after modification covers
the two automatic material extraction modes. To cover an externally saved mesh
with built-in materials as well, the owning mesh file must also be saved after
the change, or the modification must move to an earlier import hook.

**The Advanced Import Settings preview does not run this script.** The dialog
[calls `pre_import()`](C:/Users/k/Repository/External/Godot_4-7-2/editor/import/3d/scene_import_settings.cpp:803),
whose [implementation](C:/Users/k/Repository/External/Godot_4-7-2/editor/import/3d/resource_importer_scene.cpp:3092)
performs the format import and early fixups, then returns without invoking
`EditorScenePostImport`. The preview can therefore show the original specular
even when the saved imported scene is correct. This is a source-confirmed preview
limitation; the graphical dialog was not automated in these tests.

No additional persistence failure was found for the current configuration with
both meshes and materials kept inside the imported scene. This statement concerns
the investigated material-import workflow, not all Godot functionality.

## Upstream reports and fixes

Checked September 25, 2026 (Pacific time) using GitHub's public API, issue and
pull-request discussions, and current upstream source. Searches covered the
Godot engine, proposals, and documentation repositories, including
`EditorScenePostImport`, post-import scripts, material extraction, external
resources, and mesh saving. A search cannot prove that no other report exists.

The closest report for the save-order problem is
[Godot issue #85738](https://github.com/godotengine/godot/issues/85738), opened
December 4, 2023. It describes animation changes made by `EditorScenePostImport`
appearing in the editor but disappearing when running or reopening the project
if Save to File is enabled. The reporter's
[source analysis](https://github.com/godotengine/godot/issues/85738#issuecomment-1925774082)
identifies saving the animation before the script runs. This is the same ordering
mechanism we found for materials, but the upstream reproduction concerns
animations. Its status is **open**, labeled `bug` and `needs testing`, with no
assignee, milestone, or linked development PR at the time of checking. Later
comments describe explicitly saving the resource or using an earlier import
plugin hook as workarounds.

No exact material-specific report or active fix was identified in the searches.
This does not establish that maintainers have acknowledged the material variant.
It would be inaccurate to treat the animation report as a confirmed reproduction
of the user's particular material workflow.

Current upstream `master` was checked at commit
`46173009dc4ab6e582cf978e2d68b0ddfeb1e934` (September 25, 2026). It still:

- [Saves extracted materials during node processing, line 1679](https://github.com/godotengine/godot/blob/46173009dc4ab6e582cf978e2d68b0ddfeb1e934/editor/import/3d/resource_importer_scene.cpp#L1679).
- [Saves separately exported meshes during mesh generation, line 2921](https://github.com/godotengine/godot/blob/46173009dc4ab6e582cf978e2d68b0ddfeb1e934/editor/import/3d/resource_importer_scene.cpp#L2921).
- [Invokes the custom post-import script afterward, line 3498](https://github.com/godotengine/godot/blob/46173009dc4ab6e582cf978e2d68b0ddfeb1e934/editor/import/3d/resource_importer_scene.cpp#L3498).

This was source-reviewed, not rebuilt or runtime-tested. It confirms that the
relevant ordering has not changed in the current development branch.

### Related changes that must not be mistaken for a fix

- [PR #107211](https://github.com/godotengine/godot/pull/107211), merged June 10,
  2025, introduced the automatic material extraction options. Its discussion
  [explicitly moved extraction after the material plugin callback](https://github.com/godotengine/godot/pull/107211#issuecomment-2954268617).
  That callback is `EditorScenePostImportPlugin`'s material processing hook;
  it is not the later `EditorScenePostImport._post_import()` used here.
- [PR #120870](https://github.com/godotengine/godot/pull/120870), merged July 3,
  2026, fixes missing extraction when no per-material settings exist. It does not
  move material saving after the custom script. The supplied local source already
  contains the corresponding handling of absent per-material settings.
- [Issue #79779](https://github.com/godotengine/godot/issues/79779) remains open
  and describes material overrides disappearing when external meshes are
  reimported. It is a separate refresh/override-loss report, not an established
  duplicate of this specular-persistence failure.
- [Issue #86751](https://github.com/godotengine/godot/issues/86751) remains open
  for an Advanced Import Settings preview that fails to show an external
  material correctly. It is related to the preview discrepancy, but specifically
  concerns external material display, not execution of our custom script.

### Separate Blender/glTF specular import fix

[Issue #83320](https://github.com/godotengine/godot/issues/83320) explicitly tracks
Blender/glTF specular being ignored and defaulting to 0.5. It remains open.
[PR #89344](https://github.com/godotengine/godot/pull/89344) proposes importing
`KHR_materials_specular` and remains **open and unmerged**. It has requested
changes in its review history, and its milestone is the unspecified `4.x`, not
a committed release. The latest maintainer follow-up in the PR discussion is
[March 28, 2025](https://github.com/godotengine/godot/pull/89344#issuecomment-2762123192),
asking the author to address review comments. An open proposal exists, but that
does not demonstrate active development or an imminent release.

That PR targets the reason a Godot-side specular correction is needed for Blender
materials. It does not fix saving extracted resources before post-import scripts.
No issues, comments, or pull requests were posted during this investigation.
