# Rename export presets

## User

is it possible to rename export_presets.cfg to something else and still get Build.ps1 to work?

## Assistant

I’ll check how `Build.ps1` uses the export presets and whether Godot allows a different filename.

## Assistant

**Yes, but only with a workaround in `Build.ps1`.**

Godot hardcodes `export_presets.cfg`; its command-line exporter has no option to specify another filename. Your [Build.ps1](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Build.ps1:33) also explicitly checks for that file. [Godot documentation](https://docs.godotengine.org/en/latest/tutorials/editor/command_line_tutorial.html)

You could store it as `ExportPresets.cfg` and modify `Build.ps1` to copy it to `export_presets.cfg` before invoking Godot, then remove the temporary copy afterward. However, Godot’s editor would still read and write the standard filename, making editor changes harder to keep synchronized.

I recommend keeping `export_presets.cfg`. Your naming rule already exempts names imposed by external tools: “This only applies to things that we have the freedom to name as wanted.”
