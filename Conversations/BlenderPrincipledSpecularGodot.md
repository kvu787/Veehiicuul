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
