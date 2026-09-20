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

## User

C:\Users\k\Repository\Automatou\Experiments\Automatou_FastPrototyping doesn't use `<UseSharedCompilation>false</UseSharedCompilation>`. why is that?

## Assistant Commentary

I’ll compare its build and launch process with Veehiicuul’s to see whether the same compiler-server issue applies.

## Assistant Commentary

Automatou builds C# directly before starting Godot, then runs the project without exporting it. Its compiler server therefore starts outside Godot’s process tree. The wrapper waits only for Godot and its descendants, which is the distinction my earlier explanation was missing.

## Assistant

**Automatou’s launcher builds and runs differently, so its normal launch path avoids the problem.**

In its [Run.ps1](C:/Users/k/Repository/Automatou/Experiments/Automatou_FastPrototyping/Run.ps1:32):

1. PowerShell runs `dotnet build` directly. Any compiler server it starts belongs to that build’s process tree.
2. After the build finishes, PowerShell starts Godot with `--path` to run the project.
3. **There is no export step**, so Godot does not need to start another C# compilation during export.

Godot’s [console wrapper](C:/Users/k/Repository/External/Godot_4-7-2/platform/windows/console_wrapper_windows.cpp:163) waits for **Godot and its descendants**, not every compiler server on the computer. The server started by the earlier, separate `dotnet build` is outside that tree.

[Veehiicuul’s launcher](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Run.ps1:42) also performs a Godot `--export-release`. If that export starts a compiler server, the server becomes a descendant that can keep the wrapper waiting. Disabling shared compilation prevents that situation.

**My earlier explanation was too broad:** using Godot’s console wrapper alone does not require `UseSharedCompilation=false`. The relevant condition is starting a persistent compiler server *under that wrapper*. Automatou can leave shared compilation enabled with its current launch process.
