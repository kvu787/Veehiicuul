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
