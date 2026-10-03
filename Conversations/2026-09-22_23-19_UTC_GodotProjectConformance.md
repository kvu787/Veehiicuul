# Godot project conformance

## User

i recently changed AGENTS.md. ensure that all godot projects conform with it.


## Assistant

I’ll review the updated instructions, check every Godot project, and bring them into compliance.


## Assistant

I found two Godot projects, and the required portable editor and export templates are installed. The main gaps are in Veehiicuul: its Run script still builds the app, includes a Debug build, and its local instructions still require Linux/macOS support. I’ll separate Build from Run, align the platform instructions, and verify both release exports.


## Assistant

The configuration updates are in place. I’m finishing the Build/Run split and prerequisite checks, then I’ll test both exports and confirm that Run exits with a clear error when build files are missing.


## Assistant

Both release builds succeeded with zero warnings or errors. The exported apps also passed their existing headless checks: 19 input checks for InputLatency and the intentional startup-failure check for Veehiicuul. I confirmed that neither Run script changed the build files; I’m now checking missing-build handling and reviewing the final changes.


## Assistant

All six missing-build checks passed. I also aligned the Markdown tables to match the formatting rule. I’m committing each project’s changes separately, followed by the required conversation log.


## Assistant

Updated both Godot projects to conform with AGENTS.md, including separate Build/Run scripts, optimized release exports, required C# settings, Windows 11 x64 support, and aligned Markdown tables.

Both builds passed with zero warnings/errors. InputLatency’s 19 checks, Veehiicuul’s intentional startup-exit check, and all six missing-build checks passed.

Changes and the conversation log are committed separately. Working tree is clean.
