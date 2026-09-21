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

## User

```
Lines and columns are 1-based. Columns are StartInclusive:EndExclusive. Columns count UTF-16 code units.
c:\Users\k\Repository\Veehiicuul\InputLatency_Godot\project.godot:19:1-19:42
```

if this is false, then why do i still see Godot.log in C:\Users\k\Repository\Veehiicuul\InputLatency_Godot\MyLogOutput\2026-09-21_05-15-22

## Assistant

Because [Run.ps1, line 56](C:/Users/k/Repository/Veehiicuul/InputLatency_Godot/Run.ps1:56) explicitly launches Godot with:

```text
--log-file "...\MyLogOutput\2026-09-21_05-15-22\Godot.log"
```

**`--log-file` overrides the `false` project setting**, enabling Godot’s built-in logging at that specified path. [Godot source](https://gitlab.com/godotengine/godot/blob/master/main/main.cpp)

So this configuration disables the default log under `user://logs/`, while the launcher explicitly enables a log in the session’s `MyLogOutput` folder.

My previous explanation was incomplete: this is still Godot’s own file logging, enabled by the launcher’s override.
