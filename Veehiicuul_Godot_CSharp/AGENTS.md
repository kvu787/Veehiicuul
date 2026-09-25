## Platform compatibility

Support Windows 11 x64 as the only development and target platform.

# Application-specific

`Veehiicuul_Godot_CSharp.Main` should contain the only `Godot._Init`, `Godot._Ready`, and `Godot._Process`.

The editor-only `Source/Editor/DisableSpecularImport.gd` is an intentional exception
to the C#-only rule. It must run on a fresh clone before any C# build. Application
runtime code remains C#.
