# Mirror cleanup comprehensive review

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
  <filesystem><workspace_roots><root>C:\Users\k\Repository\Veehiicuul</root><root>C:\Users\k\.codex\visualizations\2026\10\19\01a153c2-5c52-7352-944c-fb7c7102e37c</root></workspace_roots><permission_profile type="managed"><file_system type="restricted"><entry access="read"><special>:root</special></entry><entry access="write"><path>C:\Users\k\Repository\Veehiicuul</path></entry><entry access="write"><path>C:\Users\k\.codex\visualizations\2026\10\19\01a153c2-5c52-7352-944c-fb7c7102e37c</path></entry><entry access="write"><special>:slash_tmp</special></entry><entry access="write"><special>:tmpdir</special></entry><entry access="read"><path>C:\Users\k\Repository\Veehiicuul\.git</path></entry><entry access="read"><path>C:\Users\k\.codex\visualizations\2026\10\19\01a153c2-5c52-7352-944c-fb7c7102e37c\.git</path></entry><entry access="read"><path>C:\Users\k\Repository\Veehiicuul\.agents</path></entry><entry access="read"><path>C:\Users\k\.codex\visualizations\2026\10\19\01a153c2-5c52-7352-944c-fb7c7102e37c\.agents</path></entry><entry access="read"><path>C:\Users\k\Repository\Veehiicuul\.codex</path></entry><entry access="read"><path>C:\Users\k\.codex\visualizations\2026\10\19\01a153c2-5c52-7352-944c-fb7c7102e37c\.codex</path></entry><entry access="read"><path>C:\Users\k\Repository\Veehiicuul\.aws</path></entry><entry access="read"><path>C:\Users\k\.codex\visualizations\2026\10\19\01a153c2-5c52-7352-944c-fb7c7102e37c\.aws</path></entry></file_system></permission_profile></filesystem>
</environment_context>

## User

<external_codex_apps_open_page>{"page_id":null}</external_codex_apps_open_page>

## User

do a comprehensive analysis/review pass of "C:\Users\k\Repository\Veehiicuul\Blender\MirrorCleanup_GeometryNodes" and changes as you see fit

## Assistant

I'll review the Blender setup, scripts, and documentation, fix issues I find, and run the available checks. I'll also record this conversation as required by the repository instructions.

## Assistant

The folder already has a substantial regression suite, including fixes from the earlier review. I'll focus this pass on cases those tests do not cover. Blender failed before running any tests inside the sandbox, so I'll retry the background checks with the required process access.

## Assistant

The existing tests pass. One uncovered case does change topology unexpectedly: disabling Mirror in the viewport still lets cleanup dissolve edges in the original mesh. I'll add a bypass for disabled Mirror evaluation and check for attribute-name collisions before finalizing the fixes.

## Assistant

The attribute checks exposed a more serious issue: a vertex group named `position` makes the cleanup graph crash Blender, while ordinary Mirror evaluation succeeds. I'll reject names that conflict with geometry data before setup changes the modifier stack. The additional material-slot and untouched-attribute cases passed.

## Assistant

The expanded suite passes. Cleanup now follows Mirror's viewport and render switches independently, verified against Blender's actual render dependency graph. Invalid attribute names are rejected without altering an existing setup. Negative-scale mirror frames also passed all eight axis combinations.

## Assistant

The rebuilt example passes fresh-process verification with automatic script execution disabled. Benchmark timings remain about 5-7 ms for 100 input faces and 18-21 ms for 900. I'll finish by checking the rendered preview and standalone copy, then commit the source changes, packaged example, and conversation separately.

## Assistant

Completed the review and committed the fixes.

- Prevented a Blender crash caused by conflicting vertex group names such as `position`; validation preserves existing setups.
- Made cleanup respect Mirror's viewport and render switches independently, preserving geometry and attributes when bypassed.
- Expanded regression tests and rebuilt the saved example and preview.

Validation passed in Blender 4.5.14: full regression suite, actual render-context checks, fresh-process verification with autoexec disabled, standalone `Run.cmd`, preview rendering, and benchmarks.

Remaining limitations include UV/corner attribute transfer and manually synchronized Edit Mode visibility; see [ReadMe](C:/Users/k/Repository/Veehiicuul/Blender/MirrorCleanup_GeometryNodes/ReadMe.md).

Two implementation commits and a separate conversation commit are recorded. The working tree is clean.

## User

explain the preconditions for running GeometryNodesMirrorCleanup.py. i assume one precondition is a mirror modifier with merge enabled. what happens if the object has other modifiers after the mirror modifier?

## Assistant

I'll check the setup code for its preconditions and how it handles modifiers after Mirror.

## Assistant

Yes: **the active mesh must have a Mirror modifier with Merge enabled.** Mirror does not need to be the last modifier.

The setup preconditions are:

- Run the script inside Blender, with the intended mesh as the **active object**, in **Object Mode**. The supported Blender version is 4.5.14.
- Have at least one Mirror modifier. If there are several, setup uses the **last Mirror in the stack**, and that one must have Merge enabled.
- Avoid conflicting vertex group or attribute names. For example, a vertex group named `position` is rejected. The full rules are in [ReadMe](C:/Users/k/Repository/Veehiicuul/Blender/MirrorCleanup_GeometryNodes/ReadMe.md).

**Other modifiers after Mirror are preserved.** Setup inserts cleanup immediately after the chosen Mirror, pushing the later modifiers down while keeping their relative order. For example:

```text
Before: Mirror -> Subdivision -> Bevel
After:  Mirror -> Selective Mirror Cleanup -> Subdivision -> Bevel
```

Those later modifiers operate on the cleaned geometry. They are not applied or otherwise reconfigured. Because cleanup changes topology, downstream operations that depend on particular vertex indices or attributes may need checking.

A few distinctions matter:

- **Clipping is not required.** Merge is required, and its distance must actually join the halves for seam cleanup to work.
- **Subdivision is optional.** If present, putting it after cleanup is the intended workflow.
- **You do not need to create `RemoveCenter` first.** Setup creates the vertex group if missing. Assigning vertices at weights above 0.5 requests their removal; eligible seam edges can be dissolved independently of those vertex requests.
- All mirror axes can be off, or Mirror can be disabled for the viewport/render. Setup still succeeds; cleanup bypasses evaluation in the corresponding case.
- Rerunning setup replaces this tool's existing cleanup modifier while retaining its tolerances and display settings.

The insertion and validation logic is in [setup()](C:/Users/k/Repository/Veehiicuul/Blender/MirrorCleanup_GeometryNodes/GeometryNodesMirrorCleanup.py:506).
