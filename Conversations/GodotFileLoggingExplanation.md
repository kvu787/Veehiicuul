# Godot File Logging Explanation

## User

```
Lines and columns are 1-based. Columns are StartInclusive:EndExclusive. Columns count UTF-16 code units.
c:\Users\k\Repository\Veehiicuul\InputLatency_Godot\project.godot:20:1-20:42
```
explain what this does

## Assistant

The setting has shifted to [line 19](C:/Users/k/Repository/Veehiicuul/InputLatency_Godot/project.godot:19):

```ini
file_logging/enable_file_logging.pc=false
```

This **disables Godot’s built-in log files on desktop platforms**, including Windows. Normally, Godot saves output and error messages to `user://logs/godot.log`; this turns that off. The `.pc` suffix means it overrides the setting specifically for desktop platforms. [Godot documentation](https://docs.godotengine.org/en/4.6/classes/class_projectsettings.html#class-projectsettings-property-debug-file-logging-enable-file-logging-pc)

Console output and any separate logging implemented by the application or launch script can still work. This setting only controls Godot’s own file logging.
