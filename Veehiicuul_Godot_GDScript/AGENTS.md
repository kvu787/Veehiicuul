# GDScript application

This application intentionally uses GDScript only and standard Godot 4.7.2,
as explicitly requested by the user. The root C#/.NET requirements do not apply
to this application. All other root instructions still apply.

Only `Veehiicuul/Source/Main.gd` implements application `_ready` and `_process`.
Use typed, reference-counted helper classes. Avoid reference cycles and avoid
building arrays, dictionaries, or scene objects in the per-frame collision path.

Preserve DirectX 12, Forward+, disabled VSync, and an unlimited frame rate.
Build and run scripts live at this application's root. Session logs belong in
the root `MyLogOutput` directory.
