# Folder rename audit

## User

i renamed the C:\Users\k\Repository\Veehiicuul\3dTestScene_CppDx12 folder.

check to see if that breaks anything in the repo. (ignore checking the C:\Users\k\Repository\Veehiicuul\Conversations folder)

## Assistant

I’ll check the renamed folder, its references, and the build and launch scripts for anything the rename may have broken. I’ll exclude `Conversations` from the check.

## Assistant

The rename was from `Cpp` to `3dTestScene_CppDx12`. I found broken documentation links and instructions that still use `Cpp`. The launcher already handles moved folders; I’ll run its build and tests to verify that it refreshes the old build paths successfully.

## Assistant

I confirmed 18 links in the input-latency guide that broke because of the rename, plus stale folder names in the setup instructions. The launcher detected the move correctly, but the build check has stalled during compiler detection. I’m checking whether the restricted execution environment is causing that.

## Assistant

The Release build and all 13 tests passed, including GPU and window tests. The remaining rename-related issues are in documentation. I’m recording this conversation in a separate commit, as required by the repository instructions.

## Assistant

The rename broke documentation references. **The Release build and all 13 tests passed.**

- **18 broken links** in [InputLatency.md](C:/Users/k/Repository/Veehiicuul/Documentation/InputLatency.md:235) still point to `../Cpp/`.
- **Outdated folder instructions** remain in [README.md](C:/Users/k/Repository/Veehiicuul/3dTestScene_CppDx12/README.md:7), [VisualStudio.md](C:/Users/k/Repository/Veehiicuul/3dTestScene_CppDx12/Documentation/VisualStudio.md:143), [Assets.md](C:/Users/k/Repository/Veehiicuul/3dTestScene_CppDx12/Documentation/Assets.md:16), and [GenerateAssets.py](C:/Users/k/Repository/Veehiicuul/3dTestScene_CppDx12/Tools/GenerateAssets.py:4). In particular, the documented `cd ...\Cpp` command now fails.

The launcher successfully refreshed the old build paths automatically. `Conversations` was excluded from the audit; this conversation was recorded in a separate `[cnv]` commit.
