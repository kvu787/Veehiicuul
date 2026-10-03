# Blender Principled BSDF specular in Godot

## User

is there a way i can use blender's default principled bsdf shader, change the color, and change settings such that there will be no specular when exported to glb and importing that glb into godot 4.7.2?

## Assistant Commentary

I’ll check how Blender exports the Principled BSDF settings to glTF and how Godot handles them.

## Assistant Commentary

Blender exports the zero-specular setting, but the Godot 4.7.2 source in your local checkout doesn’t import it. You can keep the Principled BSDF material and its color; disabling specular needs a Godot-side material setting.

## Assistant

**Not through Blender settings alone with Godot 4.7.2’s built-in GLB importer.** You can keep Principled BSDF and your chosen Base Color, but removing specular requires a Godot-side adjustment.

In your installed Blender exporter, setting **Specular → IOR Level = 0** writes zero specular into the GLB’s `KHR_materials_specular` extension. Your local Godot 4.7.2 importer does not support that extension, so the material retains Godot’s default specular value of **0.5**. [Importer source](C:/Users/k/Repository/External/Godot_4-7-2/modules/gltf/gltf_document.cpp:6967)

The practical setup is:

1. **Blender:** Set your Base Color, **Metallic = 0**, **Specular → IOR Level = 0**, and keep **Coat → Weight = 0**. Export to GLB normally.
2. **Godot:** On an editable copy or external version of the imported `StandardMaterial3D`, set **Metallic → Specular = 0** (`MetallicSpecular = 0.0f` in C#). This removes specular reflections while retaining diffuse lighting. [Godot documentation](https://docs.godotengine.org/en/latest/classes/class_basematerial3d.html#class-basematerial3d-property-metallic-specular)

**Roughness = 1 does not disable specular**; it spreads out the reflection. Also, Godot’s **Shading → Specular → Disabled** only disables highlights from lights; **Metallic → Specular = 0** also addresses environment reflections.

A Godot import script can apply this automatically on every import, so your ongoing workflow can still be just changing colors in Blender and exporting.

## User

A Godot import script can apply this automatically on every import, so your ongoing workflow can still be just changing colors in Blender and exporting.

how would this work

## Assistant Commentary

I’ll check how Godot’s import script hook works with C# and how you would attach it to your GLB.

## Assistant

**You assign a small C# script to the GLB’s import settings once. Godot then runs it every time that GLB is imported or reimported.**

The sequence would be:

1. You export from Blender, overwriting the GLB in your Godot project.
2. Godot reads the GLB and creates its meshes and materials, including your updated colors.
3. Your import script visits the meshes and sets each material’s specular strength to zero.
4. Godot saves that modified result in its import cache. Your game uses the modified materials automatically.

The script runs **during import**, so there is no additional material-processing work during gameplay, and your original GLB stays unchanged. Godot calls the script before saving the generated scene. [Godot 4.7.2 source](C:/Users/k/Repository/External/Godot_4-7-2/editor/import/3d/resource_importer_scene.cpp:3433)

For the one-time setup:

1. Create a C# class derived from `EditorScenePostImport`, marked `[Tool]`.
2. Override `_PostImport(Node scene)`. Walk through the scene’s mesh materials, apply the change below, and return `scene`.
3. Compile the C# project in the editor.
4. Select the GLB in Godot’s **FileSystem** dock. In the **Import** dock, set **Import Script → Path** to that C# file, then click **Reimport**. [Import script documentation](https://docs.godotengine.org/en/stable/tutorials/assets_pipeline/importing_3d_scenes/import_configuration.html#using-import-scripts-for-automation)

The actual material change is just:

```csharp
material.MetallicSpecular = 0.0f;
```

This assumes **Metallic = 0** and **Coat Weight = 0**, as discussed above. Base Color continues to come from Blender.

**The simplest script would enforce zero specular on every material in the assigned GLB**, regardless of Blender’s specular slider. It could also filter by material name if you want some materials to remain shiny. Multiple GLBs can share the same script.

## User

A Godot import script can apply this automatically on every import, so your ongoing workflow can still be just changing colors in Blender and exporting.

^implement this feature for C:\Users\k\Repository\Veehiicuul\Veehiicuul_Godot_CSharp

## Assistant Commentary

I’ll add the C# import script, connect it to the project’s GLB, and verify that reimporting preserves the colors and sets specular to zero.

## Assistant Commentary

The project has one GLB to connect. I’ll also set the script as the default for newly imported 3D scenes. The build needs one extra step: compile the editor script before importing assets, then export the game as usual.

## Assistant Commentary

The optimized build and Windows export passed, and Godot ran the import callback for the track. I’m now checking the saved material values and testing a changed color in a disposable GLB to confirm the full re-export workflow.

## User

i closed godot editor in the middle of this, so do another verification pass after your receive this prompt

## Assistant Commentary

I’ll run another verification pass with the editor closed, including a fresh build/import and a check of the saved materials.

## Assistant Commentary

The pass with the editor closed succeeded. All 39 materials across 110 surfaces have zero specular, and every other saved material property is unchanged. A changed-color reimport also passed. The release build and startup check passed; I’m finishing the review and commits.

## Assistant

Implemented and committed as `fec8946`.

- Added [DisableSpecularImport.cs](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Source/Editor/DisableSpecularImport.cs), which sets material specular to zero during import.
- Connected the existing track and made it the default for new 3D scene imports.
- Updated `Build.cmd`’s build process to compile the import script before importing assets.

**Verified again after you closed Godot:** all 39 materials across 110 surfaces have zero specular; other material properties are preserved. Changing a GLB color and reimporting works. Release build, export, and startup verification passed.

You can now change colors in Blender and overwrite the GLB. Keep materials internal, with Blender’s **Metallic = 0** and **Coat Weight = 0**.

## User

explain how the compilation for the import script works. for example, if i clone this repo on a new pc and open the project for the first time in godot editor, what happens?

## Assistant Commentary

I’ll check the build setup and Godot’s first-import behavior so I can explain what happens on a fresh clone.

## Assistant Commentary

I reproduced a first open with no compiled files. Godot imported the track, but the callback did not run: all 39 materials retained specular 0.5. The current setup requires a build before the first import, or an explicit reimport after building.

## Assistant

**With the current implementation, cloning and immediately opening the project is not sufficient.** The C# project must be compiled before the import script can run.

I tested that exact situation using an isolated copy containing only tracked files. Godot imported the track without compiling C#, and **all 39 materials retained specular 0.5**. The zero-specular adjustment was not applied.

The import script is compiled into the **same project DLL as the other C# code**. It is not compiled separately whenever a GLB changes. `[Tool]` permits execution inside the editor; it does not trigger compilation.

The [build process](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Build.ps1:36) now has this order:

1. **Compile `ImportRelease`.** This optimized configuration includes Godot’s editor API and defines `TOOLS`, so the code inside `#if TOOLS` is included. Its output goes into the editor’s expected `Debug` output directory, despite being optimized.
2. **Import assets.** Godot loads that DLL, runs the import callback, and saves the modified scene materials.
3. **Compile and export `ExportRelease`.** This configuration excludes the import script. The game uses the already-processed materials.

On a new PC, install the required .NET 10 SDK and portable Godot 4.7.2 .NET setup, then **run Build.cmd before opening the project**. The full build also requires the matching export templates.

If you only want to prepare the editor without building an executable, run this from the application folder **before first opening Godot**:

```powershell
dotnet build Veehiicuul_Godot_CSharp.slnx --configuration ImportRelease
```

Then open the project; its first asset import can use the compiled callback.

**If you already opened it before compiling:** use Godot’s **Build C# Project** button, then select the GLB and click **Reimport**. The editor’s normal `Debug` build also includes the import script. Compiling alone does not guarantee that an already-imported GLB gets processed again.

After that, changing colors and overwriting the GLB triggers normal automatic reimport using the existing DLL. You only need another C# build when the script changes or its compiled output is missing.

## User

I tested that exact situation using an isolated copy containing only tracked files. Godot imported the track without compiling C#, and all 39 materials retained specular 0.5. The zero-specular adjustment was not applied.

^why does godot do this? if it knows that it should run a c# import script but that script isn't compiled, then shouldn't it at least report an error

## Assistant Commentary

I’ll trace how Godot reports that failure and check whether my headless test missed an error shown only in the editor.

## Assistant

**Yes. And I need to clarify my earlier explanation: Godot does queue an error, but it does not treat this particular error as an import failure.** My test ran headless, which concealed the editor-dialog part of the behavior.

In your Godot 4.7.2 source, the sequence is:

1. Godot loads the `.cs` script resource.
2. It checks whether its class derives from `EditorScenePostImport`. Without the compiled class, it cannot establish that inheritance.
3. It queues this misleading error:

   > Script is not a subtype of EditorScenePostImport:

4. **That branch does not return a failure.** Godot continues importing and saves the scene without running the callback. [Import code](C:/Users/k/Repository/External/Godot_4-7-2/editor/import/3d/resource_importer_scene.cpp:3404)

The reporting function, `EditorNode::add_io_error()`, adds the message to the editor’s **Load Errors** dialog. It does not print it to the console through the normal error logger. Based on that code, the graphical editor should show a dialog; I have verified the source path, not the visible dialog itself. [Error-dialog code](C:/Users/k/Repository/External/Godot_4-7-2/editor/editor_node.cpp:5523)

That explains why my headless test showed an apparently successful import, exit code **0**, and unchanged specular values.

**I would consider this a failure-handling bug:** an explicitly configured import step could not execute, yet Godot saved the result as successful. A clearer diagnostic would identify the missing compiled C# class, and the import should fail—or explicitly remain incomplete—until the callback can run. Compiling first avoids this path, but does not fix Godot’s underlying reporting behavior.

## User

A Godot import script can apply this automatically on every import, so your ongoing workflow can still be just changing colors in Blender and exporting.

^if this feature was rewritten in gdscript, could we avoid the compilation step and make the overall process smoother?

## Assistant Commentary

I’ll check whether a GDScript import callback would run on the first import without a separate build.

## Assistant

**Yes. A self-contained GDScript import script would remove the need to build C# before importing assets.** Godot loads and compiles GDScript automatically; there would be no separate compilation command for you to run.

On a fresh clone, the workflow would become:

1. Open the project in Godot.
2. Godot loads the `.gd` import script, marked `@tool` and extending `EditorScenePostImport`.
3. During the first GLB import, it runs the script and saves the materials with specular set to zero. [Godot import-script documentation](https://docs.godotengine.org/en/stable/classes/class_editorscenepostimport.html)

For our implementation, that would let us remove:

- The custom `ImportRelease` build configuration.
- Its explicit `GodotSharpEditor` package reference.
- The extra C# build before asset import.

Your game code would remain C# and would still need compilation to run or export. The import script would operate independently of that code, with no additional work during gameplay.

**For this small editor-only task, GDScript would make setup simpler.** It would be an exception to your repository’s current “Don’t use GDScript” rule. I haven’t changed the implementation.

## User

okay, rewrite the feature using gdscript instead of c#

## Assistant Commentary

I’ll replace the C# import callback with GDScript, remove the extra build configuration, and verify that a fresh clone imports the materials correctly without compiling C# first.

## Assistant Commentary

The GDScript callback is in place, and the custom `ImportRelease` setup is removed. I’m testing a copy with no compiled C# files, including both the existing track and a new GLB that uses the project’s default import settings.

## Assistant Commentary

The first-import test passed with no C# assemblies present: all 39 materials across 110 surfaces had zero specular. Color changes also survived reimport, and the release build, export, and startup check passed. I’m finishing the review and commits.

## Assistant

Rewritten in [DisableSpecularImport.gd](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Source/Editor/DisableSpecularImport.gd) and committed as `9cdfdfa`.

- Removed the C# callback, `ImportRelease` configuration, and extra build step.
- Verified first import with **no compiled C# assemblies**: all 39 materials have zero specular.
- Verified color changes survive reimport, plus release build, export, and startup checks.

**You can now clone the repo and open the project directly in Godot.** Material importing needs no preliminary C# build.
