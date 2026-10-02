# Godot input action names

## User

C:\Users\k\Repository\Veehiicuul\Veehiicuul_Godot_CSharp\Veehiicuul

if i configure godot's InputMap via the godot editor gui, does godot generate memory optimal enums or something similar that i can use to refer to the action names?

## Assistant

I'll check how this project references input actions and what Godot provides for C#.

## Assistant

**No.** Configuring actions in Godot's Input Map GUI saves them in `project.godot`; Godot does not generate C# enums or named C# fields for those actions. Its input APIs accept `StringName` identifiers. [InputMap documentation](https://docs.godotengine.org/en/stable/classes/class_inputmap.html)

For efficient C# access, **create each `StringName` once and reuse it**:

```csharp
public static class InputActions {
    public static readonly StringName ResetCar = new("ResetCar");
}

// In your input polling code:
bool resetCar = Input.IsActionJustPressed(InputActions.ResetCar);
```

The GUI action must be named `ResetCar` exactly.

`StringName` uses interned names internally, allowing fast comparisons and shared name storage. Caching it also avoids creating a new C# `StringName` wrapper and performing string-to-name conversion on every input check. [StringName documentation](https://docs.godotengine.org/en/stable/classes/class_stringname.html)

Your existing [DigitalInputMap.cs](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Source/GameDataAndLogic/DigitalInputMap.cs:7) already caches its `StringName` properties this way. That caching works equally well with GUI-configured bindings.

A C# enum would still need a mapping to `StringName`, because Godot does not accept integer action IDs. If you want the C# identifiers generated automatically from the GUI configuration, that requires a custom generator or plugin.
