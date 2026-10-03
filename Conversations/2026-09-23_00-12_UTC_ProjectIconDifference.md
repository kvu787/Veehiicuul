# Project icon difference

## User

why is there
```
Lines and columns are 1-based. Columns are StartInclusive:EndExclusive. Columns count UTF-16 code units.
c:\Users\k\Repository\Veehiicuul\Veehiicuul_Godot_CSharp\project.godot:16:1-16:29
```
```
config/icon="res://icon.svg"
```

but not in the inputlatency project?

## Assistant

I’ll check both projects’ icon settings and history to see why they differ.

## Assistant

It’s a difference in how the projects were created.

- **Veehiicuul** started as an empty Godot project in commit `7b3ad1e`. Godot’s project creator automatically adds `icon.svg` and this setting. The file is the standard Godot logo.
- **InputLatency** was added in commit `bcb232a` without an icon file or icon setting, and neither was subsequently added.

`config/icon="res://icon.svg"` tells Godot which image to use as the application icon; `res://` means the project’s root folder. It’s optional and has no effect on input latency or rendering settings. InputLatency isn’t missing a required setting.
