# Blender export path explanation

## User

```
Lines and columns are 1-based. Columns are StartInclusive:EndExclusive. Columns count UTF-16 code units.
c:\Users\k\Repository\Veehiicuul\Blender\Scripts\ExportToVeehiicuul.py:2:14-2:28
```
```
"//export.glb"
```
what does this path mean

## Assistant Commentary

I’ll check the Blender script to explain how that path is used.

## Assistant

In Blender, `//` means **relative to the folder containing the currently open `.blend` file**.

So `"//export.glb"` exports a file named `export.glb` beside your `.blend` file. For example:

```text
Blend file: C:\Projects\Car\Car.blend
Export:     C:\Projects\Car\export.glb
```

The script’s `bpy.path.abspath(OUTPUT)` converts that Blender-relative path into a full filesystem path. Save the `.blend` file first so it has a known folder.
