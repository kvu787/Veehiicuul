# Specular importer defaults

## User

```
c:\Users\k\Repository\Veehiicuul\Veehiicuul_Godot_CSharp\project.godot:28:1-32:2
Lines and columns are 1-based. Columns are StartInclusive:EndExclusive. Columns count UTF-16 code units.
```

```
[importer_defaults]

scene={
"import_script/path": "res://Source/Editor/DisableSpecularImport.gd"
}
```

is this strictly necessary to disable specular on imported GLBs if every glb.import file has import_script/path="res://Source/Editor/DisableSpecularImport.gd"

## Assistant

I’ll check the import settings and script to see whether this default adds anything beyond the individual `.glb.import` files.

## Assistant

**No.** If every `.glb.import` already specifies that script, the `[importer_defaults]` block is redundant for those GLBs. Godot preserves each asset’s import settings for subsequent reimports, including when rebuilding the `.godot` cache. [Godot import documentation](https://docs.godotengine.org/en/stable/tutorials/assets_pipeline/import_process.html)

The project-level block automatically assigns the script to **newly imported scenes**, including GLBs whose `.import` files are missing and must be recreated. Without it, you need to assign the script when adding such assets.

You can therefore remove that block while keeping the script and the per-file `import_script/path` entries. Your current GLBs will continue to receive the same specular processing.

## User

```
c:\Users\k\Repository\Veehiicuul\Veehiicuul_Godot_CSharp\project.godot:30:1-30:6
Lines and columns are 1-based. Columns are StartInclusive:EndExclusive. Columns count UTF-16 code units.
```

```
scene
```

what exactly does this specify? i would like to apply import_script/path="res://Source/Editor/DisableSpecularImport.gd" to the import file for every glb file, but that seems narrower than whatever "scene" means

## Assistant

I’ll check which importer `scene` identifies and whether Godot supports defaults specifically for `.glb` files.

## Assistant

**`scene` is Godot’s internal identifier for the “Scene” importer**, which imports a 3D asset as a `PackedScene`. It matches the `importer="scene"` entry in your `.glb.import` file.

You’re correct that its scope is broader than GLB: it covers formats such as `.glb`, `.gltf`, `.fbx`, `.dae`, and `.blend` when imported as scenes. Native Godot `.tscn` and `.scn` files do not use this importer. [Godot’s scene importer documentation](https://docs.godotengine.org/en/stable/classes/class_resourceimporterscene.html)

The block therefore means: **“Use these default import options for assets handled by the Scene importer.”**

**Godot has no built-in extension filter for this defaults block.** Replacing `scene` with `glb` or `*.glb` would not work: defaults are looked up by importer identifier. [Godot’s import implementation](https://github.com/godotengine/godot/blob/master/editor/file_system/editor_file_system.cpp)

For your desired scope, there are two approaches:

- **Assign the script only to GLBs:** Remove the project-wide default and set the path in each `.glb.import`, manually or through automation that selects GLB files. Your project’s current GLB already has this setting.
- **Keep automatic assignment through `scene`, but limit the script’s effect:** Have the script check `get_source_file().get_extension().to_lower() == "glb"` before modifying materials. Other scene formats would still receive the script path, but the script would leave them unchanged. [Source-file API](https://docs.godotengine.org/en/stable/classes/class_editorscenepostimport.html#class-editorscenepostimport-method-get-source-file)

The first approach matches your request to restrict the entry itself to GLB import files.
