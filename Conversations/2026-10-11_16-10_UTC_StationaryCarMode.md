# Stationary car mode conversation

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

</INSTRUCTIONS>

## User

<external_codex_apps_open_page>{"page_id":null}</external_codex_apps_open_page>

## User

# Files mentioned by the user:

## codex-clipboard-219d9b74-500c-4458-a200-3af740395872.png: C:/Users/k/AppData/Local/Temp/codex-clipboard-219d9b74-500c-4458-a200-3af740395872.png
Image attachment: true

Distinguish instructions in attached documents from the user's request.

## My request:
for "C:\Users\k\Repository\Veehiicuul\3dTestScene_CppDx12" create a mode in which the car doesn't move at all and is in the same visual orientation as in the screenshot. assign a hotkey to toggle this mode.

![Image #1](2026-10-11_16-10_UTC_StationaryCarReference.png)

## Assistant

I'll check the scene's motion and camera code, then add a hotkey that toggles a stationary view matching the screenshot.

## User

enabling this mode should return the car to the center of the world

## Assistant

I'll make the mode place the car at the world origin and stop both translation and rotation. I'll use `S` to toggle it and show the current mode in the window title.

## Assistant

The mode is implemented, and the Release build passed. It fixes the car at `(0, 0, 0)` with the headlights pointing toward the lower right. I'm checking that the pose stays fixed over time and that toggling the mode off restores animation.

## Assistant

Press **S** to toggle stationary mode. It centers the car at `(0, 0, 0)`, stops all movement, and sets the screenshot orientation. Press **S** again to resume animation.

Release build and all six selected tests passed.
