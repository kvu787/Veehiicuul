# DirectX application conformance conversation

## User

C:\Users\k\Repository\Veehiicuul\3dTestScene_CppDx12

fix this conform with agents.md.

Exception: don't rename things just to conform with this:
## Folder and file naming

This only applies to things that we have the freedom to name as wanted.
Use CamelCase.
Use complete proper words. Don't use typical shortenings. Good: Source, Documentation. Bad: src, docs.

## Assistant Commentary

I will check `3dTestScene_CppDx12` against the repository instructions and fix the mismatches, keeping existing file and folder names as requested.

## Assistant Commentary

The main mismatch is the launcher: `Run.cmd` currently builds before every launch, and `Build.cmd` is missing. I will separate building from running and make a missing build produce a clear error. The existing session logging and Git ignore rules already meet the requirements.

## Assistant Commentary

The launcher split is in place. The first build exposed a CMake cache issue: the preset's `cl` value differs from the saved compiler path and triggers a reconfiguration that loses the Ninja setting. I will make the build script use the discovered compiler path, then verify the build, launcher, and tests.

## Assistant Commentary

The Release build now succeeds, and the existing test suite is running. I also added strict mode to the saved PowerShell utility, aligned the Markdown tables, and updated the guides for the separate build and run steps. The native compiler check needed to run outside the sandbox.

## Assistant Commentary

All 13 tests passed in both Release and Debug. The launcher also correctly exits with code 1 when either build is missing. I am checking that it launches an existing build without rebuilding and writes both logs to the session folder, then I will commit the changes and conversation separately.

## Assistant Commentary

Both launcher checks passed: Release and Debug start and close cleanly without changing build outputs, and both logs go to the same timestamped session folder. The implementation and formatting commits are complete; I am saving the required conversation log.

## Assistant

Fixed and committed; existing names preserved.

- [Build.cmd](C:/Users/k/Repository/Veehiicuul/3dTestScene_CppDx12/Build.cmd) builds; [Run.cmd](C:/Users/k/Repository/Veehiicuul/3dTestScene_CppDx12/Run.cmd) launches an existing build and exits if missing.
- Updated platform checks, PowerShell settings, logging, and documentation formatting.
- All 13 tests passed in Release and Debug; launcher and logging checks passed.

Conversation committed separately.
