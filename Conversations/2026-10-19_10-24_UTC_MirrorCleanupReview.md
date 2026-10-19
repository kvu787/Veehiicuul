# Mirror cleanup review

## User

# AGENTS.md instructions for C:\Users\k\Repository\Veehiicuul

<INSTRUCTIONS>
# Base template

## Style

For single and double quotes, only use the ASCII forms: ', "
Never use these: “, ”, ‘, ’, etc.

## Development platform compatibility

Support Windows 11 x64 as the only development platform.

## Folder and file naming

This only applies to things that we have the freedom to name as wanted.
Use CamelCase.
Use complete proper words. Don't use typical shortenings. Good: Source, Documentation. Bad: src, docs.

## External tools

You may use the tools in `%UserProfile%\Program`.
You may refer to local copies of source repos in `%UserProfile%\Repository\External`.

# PowerShell

Use modern PowerShell whose command should be `pwsh`, not legacy PowerShell.

## Git

When implementing stuff, avoid difficult-to-review "mega-commits".
When it makes sense, split work into multiple commits to make it easier to review.
Separate commits that record conversations from other commits.

## Markdown tables

Tables in Markdown must be padded and aligned in a way to make them easy to read in a plaintext editor, not only in a Markdown viewer.

## Mathematical notation in Markdown

Any mathematical notation in Markdown files (LaTeX, KaTeX, MathJax, etc) must display properly in VSCode's Markdown previewer, GitHub.com's Markdown displayer, and the markdown viewer in the Windows 11 ChatGPT app.

## PowerShell

All PowerShell scripts must use:

- Set-StrictMode -Version Latest
- $ErrorActionPreference = 'Stop'

## Applications

### Running

If you create a runnable application, create files called `Build.cmd` and `Run.cmd` that respectively build and run the application when double-clicked from File Explorer.
These must be located at the root of the application's folder in the Git repo.
These must be simple wrappers for PowerShell scripts named `Build.ps1` and `Run.ps1` which contain the actual logic to minimize the amount of batch code written.
Run.cmd must exit if it doesn't discover a build of the application at the place that Build.cmd outputs to.
If the application doesn't need to be "built" for it to be run (such as a PowerShell script), then omit Build.cmd and Build.ps1.

### Logging

When creating an application, create a folder called `LogOutput` at the root of the application's folder in the git repo.
For each run of the application, a folder must be created in LogOutput and named with the current timestamp. This PowerShell code shows what the name of the folder should be:

```powershell
$logFolderPath = "$env:UserProfile\Repository\Godot\VsyncStutterTest\LogOutput\$(Get-Date -Format "yyyy-MM-dd_HH-mm-ss")"
New-Item -ItemType "Directory" -Path $logFolderPath
```

Any logs for that application session must be put in that log folder.
`LogOutput/` must be gitignored.

# Base template additions

## Conversations

Record verbatim and commit all conversations in a folder named `Conversations` located at the root of this Git repo.
Use one file per conversation.
Prefix these files with the PowerShell evaluation of `[DateTime]::UtcNow.ToString("yyyy-MM-dd_HH-mm") + "_UTC"`.
Prefix these commits with `[cnv]`.
If I attach images to prompts, save and record these in the conversation logs.
If the conversation begins with `dnr`, then do not record the conversation.

## Application compatibility

Do not attempt to maintain any sort of application compatibility between different commits of the repo. This creates unwanted complexity.

## Target platform compatibility

Support Windows 11 x64 as the only target platform.

## Using physical displays

If you need a display, only use NE18NZ2.

</INSTRUCTIONS><environment_context>
  <cwd>C:\Users\k\Repository\Veehiicuul</cwd>
  <shell>powershell</shell>
  <current_date>2026-10-19</current_date>
  <timezone>America/Los_Angeles</timezone>
  <filesystem><workspace_roots><root>C:\Users\k\Repository\Veehiicuul</root><root>C:\Users\k\.codex\visualizations\2026\10\19\01a153af-1366-7ee2-bf40-7f631d1b1c70</root></workspace_roots><permission_profile type="managed"><file_system type="restricted"><entry access="read"><special>:root</special></entry><entry access="write"><path>C:\Users\k\Repository\Veehiicuul</path></entry><entry access="write"><path>C:\Users\k\.codex\visualizations\2026\10\19\01a153af-1366-7ee2-bf40-7f631d1b1c70</path></entry><entry access="write"><special>:slash_tmp</special></entry><entry access="write"><special>:tmpdir</special></entry><entry access="read"><path>C:\Users\k\Repository\Veehiicuul\.git</path></entry><entry access="read"><path>C:\Users\k\.codex\visualizations\2026\10\19\01a153af-1366-7ee2-bf40-7f631d1b1c70\.git</path></entry><entry access="read"><path>C:\Users\k\Repository\Veehiicuul\.agents</path></entry><entry access="read"><path>C:\Users\k\.codex\visualizations\2026\10\19\01a153af-1366-7ee2-bf40-7f631d1b1c70\.agents</path></entry><entry access="read"><path>C:\Users\k\Repository\Veehiicuul\.codex</path></entry><entry access="read"><path>C:\Users\k\.codex\visualizations\2026\10\19\01a153af-1366-7ee2-bf40-7f631d1b1c70\.codex</path></entry><entry access="read"><path>C:\Users\k\Repository\Veehiicuul\.aws</path></entry><entry access="read"><path>C:\Users\k\.codex\visualizations\2026\10\19\01a153af-1366-7ee2-bf40-7f631d1b1c70\.aws</path></entry></file_system></permission_profile></filesystem>
</environment_context>

## User

<external_codex_apps_open_page>{"page_id":null}</external_codex_apps_open_page>

## User

do a comprehensive analysis/review pass of "C:\Users\k\Repository\Veehiicuul\Blender\MirrorCleanup_GeometryNodes" and changes as you see fit


## Assistant

I'll review the Blender geometry nodes tool, make any fixes the review supports, and run the available checks. I'll also record this conversation in the repository as requested.


## Assistant

The tool already has a substantial regression suite and documents its topology limits clearly. I'm checking those guarantees against edge cases, especially retained seams, attribute preservation, and repeated setup. Blender's first test launch failed with an access error before running the tests; I'll check whether that comes from the execution environment.


## Assistant

The existing tests pass when Blender runs outside the sandbox. Additional cases exposed three defects: object-level material overrides are lost, byte-color attributes are corrupted during rebuilding, and a retained creased edge can disappear when removable edges connect faces around it. I'll fix these and add regression cases.


## Assistant

I also found that Blender's default float comparison tolerance treats very small crease weights as zero, allowing requested creases to disappear. I'll use exact comparisons for crease checks and make unsupported regions return the original input, including its attributes.


## Assistant

The expanded tests pass for the fixes so far, including all nine supported point-attribute types and small crease weights. I'm now checking repeated setup and the saved example so installation failures preserve a working modifier and the packaged file stays consistent with the source.


## Assistant

A further regression case confirmed that warped quads can have matching average normals and still be merged incorrectly. The new planarity check preserves them. The geometry and attribute fixes are committed; next are installation recovery, launcher checks, and rebuilding the saved example.


## Assistant

The setup checks pass: a simulated construction failure preserves the working modifier, shared node groups survive replacement, and rerunning setup retains tolerances and display settings. Concurrent launcher runs also get separate logs. The benchmark remains around 5-7 ms for 100 input faces and 19 ms for 900.


## Assistant

The rebuilt example passes fresh-process verification with automatic Python execution disabled and still matches the reference topology and subdivision result. A copied standalone folder also passes through `Run.cmd`, and the rendered preview shows matching results. I'm finishing the commits and conversation record.


## Assistant

Completed the review and committed the fixes.

- Protected retained edges, attached wires, and warped faces from incorrect reconstruction.
- Fixed small-crease loss, object-level material overrides, and byte-color corruption; expanded point-attribute preservation.
- Made setup recoverable on failure while retaining tolerances and display settings.
- Improved logging and verification, and rebuilt the example and preview.

Validation passed in Blender 4.5.14: expanded regression suite, fresh-process verification with automatic script execution disabled, standalone `Run.cmd`, preview rendering, and benchmarks.

UV/corner attributes and custom split normals remain unsupported on rebuilt faces; details are in [ReadMe](C:/Users/k/Repository/Veehiicuul/Blender/MirrorCleanup_GeometryNodes/ReadMe.md).

Three implementation commits and a separate conversation commit are recorded. The working tree is clean.
