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
