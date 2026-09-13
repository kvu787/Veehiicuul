# Conversation: Cpp move review

## User

in preparation for the Godot rewrite of ZoomTracks which will be in this repo, i have moved the current application in a subfolder called Cpp. check to see if anything in the repo needs to be fixed due to this move.

## Assistant

I’ll check the new layout, launch scripts, build paths, and documentation for references affected by the move into `Cpp`, then fix and verify what I find.

## Assistant

I found a broken asset-regeneration path: CMake now looks for `Cpp/Blender/Car.blend`, but `Blender` remains at the repository root. The constraints link in `AGENTS.md` is also broken, and the Visual Studio instructions still say to open the old location. The launcher and presets already use paths relative to `Cpp`; I’ll verify them with the existing tests.

## Assistant

The path and documentation fixes are in place. I restored the root README with the shared constraints and added a root launcher that delegates to `Cpp`, keeping build output and logs there. The Release build succeeded; GPU and window-lifecycle tests are now running.

## Assistant

Release passed all 13 tests. The Debug launcher also detected and refreshed its moved CMake cache successfully. I checked 126 documentation links and the ignore rules; those paths resolve correctly. I’m now checking asset regeneration against the existing mesh and background.

## Assistant

Both Debug and Release passed all 13 tests. Asset regeneration produced matching mesh content and an identical background image. The root `Run.cmd` also launched and closed successfully from outside the repository. I’m committing the fixes and recording this conversation in a separate commit.

## Assistant

Fixed and committed as `95034fa`:

- Corrected the Blender asset path.
- Restored the root README and shared constraints.
- Updated documentation paths and Git ignore rules.
- Added a root [Run.cmd](C:/Users/k/Repository/Veehiicuul/Run.cmd) that launches `Cpp`.

Verified: **13/13 tests passed in both Debug and Release**, regenerated assets matched, and the launcher worked.

Conversation committed separately. Working tree is clean.
