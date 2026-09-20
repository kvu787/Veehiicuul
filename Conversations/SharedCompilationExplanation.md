# Shared compilation explanation

## User

is `<UseSharedCompilation>false</UseSharedCompilation>` strictly necessary? what happens if it is set to true?

## Assistant Commentary

I’ll check where that setting is used in this project and what changes when shared compilation is enabled.

## Assistant Commentary

The repository records a previous export stall caused by the idle compiler server. Disabling shared compilation fixed it. This is a launcher workaround; it does not change how the compiled game runs.

## Assistant

**No—`false` is not inherently required by C# or Godot. In this repository, it works around a launcher problem.**

- **`true`:** Builds can reuse a background Roslyn compiler server (`VBCSCompiler`). This can speed up repeated compilations by keeping compiler code and reference metadata loaded. The process stays alive between builds and retains some memory.
- **`false`:** Compilations use a separate compiler process that exits when finished, giving up those reuse benefits.

This controls how compilation runs; it does **not** change the game’s intended behavior, graphics settings, or runtime performance. [Roslyn compiler-server documentation](https://github.com/dotnet/roslyn/blob/main/docs/compilers/Compiler%20Server.md)

The complication here is **Godot’s Windows console wrapper can wait for the compiler server to exit, even after exporting has finished**. Consequently, `Run.cmd` can appear stuck before launching the game. The repository’s [previous investigation](C:/Users/k/Repository/Veehiicuul/Conversations/20260918GodotEditorSaveChanges.md:235) reproduced this in `InputLatency_Godot` and verified that `false` fixed it. The current Veehiicuul launcher uses the same console wrapper, so enabling it could reintroduce that delay; it need not happen on every run.

**I would keep `false` with the current launcher.** Enabling shared compilation is reasonable if the launcher is adjusted and verified to finish without waiting for the server.
