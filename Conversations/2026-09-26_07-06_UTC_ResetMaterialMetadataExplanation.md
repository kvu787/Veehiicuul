# Reset Material Metadata Explanation

## User

```
c:\Users\k\Repository\Veehiicuul\Veehiicuul_Godot_CSharp\Veehiicuul\Source\Editor\ResetStandardMaterials.gd:57:1-58:44
Lines and columns are 1-based. Columns are StartInclusive:EndExclusive. Columns count UTF-16 code units.
```

```
    for metadata_name in material.get_meta_list():
        material.remove_meta(metadata_name)
```
what is the purpose of thsi

## Assistant

I’ll check the surrounding script to explain why it removes that metadata.

## Assistant

It **removes all metadata attached to the material**.

- `get_meta_list()` returns the names of its metadata entries.
- `remove_meta(metadata_name)` deletes each entry.

Metadata is extra named data attached to a resource, separate from ordinary material settings such as color or roughness. This loop makes the reset clear that extra data too.

**It isn’t necessary just to reset the material’s rendering settings.** It also deletes any metadata your tools or scripts intentionally attached.
