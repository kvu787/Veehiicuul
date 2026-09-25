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
