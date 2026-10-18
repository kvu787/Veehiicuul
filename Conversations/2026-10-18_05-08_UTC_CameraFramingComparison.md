# Blender and Godot camera framing comparison

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

## codex-clipboard-0fc4e2a8-65e1-4f83-ac6c-dee92566d5d7.png: C:/Users/k/AppData/Local/Temp/codex-clipboard-0fc4e2a8-65e1-4f83-ac6c-dee92566d5d7.png
Image attachment: true

## codex-clipboard-dec6c718-7c2f-4585-b629-28c35bc7d894.png: C:/Users/k/AppData/Local/Temp/codex-clipboard-dec6c718-7c2f-4585-b629-28c35bc7d894.png
Image attachment: true

Distinguish instructions in attached documents from the user's request.

## My request:
i believe i have the identical camera setup in C:\Users\k\Repository\Veehiicuul\Blender\Tracks\Ribeye2.blend and C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Tracks/Ribeye/Ribeye_Scene.tscn, but the rendered image is different. what is going on?

![Image #1](2026-10-18_05-08_UTC_CameraComparisonBlender.png)

![Image #2](2026-10-18_05-08_UTC_CameraComparisonGodot.png)

## Assistant

I will compare the saved camera transforms and projection settings, including how Blender's export is translated into Godot. I will also check whether the different viewport dimensions explain the framing.

## Assistant

The saved Godot camera has `Size = 150`, while Blender shows `Orthographic Scale = 300`. These settings also default to measuring different frame dimensions. I am checking Blender's parent transforms to see whether the camera angle and framing differ as well.

## Assistant

The camera orientations match after coordinate conversion. At 16:9, Blender shows a 300 by 168.75 unit frame, while Godot shows 266.67 by 150. That makes Godot's view 12.5% more zoomed in. Godot's matching Size is 168.75 with Keep Height. Blender is also using material preview lighting, which explains a separate difference in shading.

## Assistant

Your camera angles match. **The main difference is orthographic zoom.** I checked the saved Blender camera and Godot's combined parent transforms.

At their shared 16:9 aspect ratio:

| Setting                    | Blender        | Godot          |
| -------------------------- | -------------- | -------------- |
| Projection                 | Orthographic   | Orthographic   |
| Scale / Size               | 300            | 150            |
| Dimension measured         | Width          | Height         |
| Visible frame, world units | 300 x 168.75   | 266.67 x 150   |

Blender's `Auto` sensor fit uses the width here. Godot defaults to `Keep Height`, so its Size is the **full frame height**, not half the width. [Godot's Camera3D documentation](https://docs.godotengine.org/en/stable/classes/class_camera3d.html#class-camera3d-property-size).

Consequently, **Godot is 12.5% more zoomed in**. To match Blender, change [Godot's camera Size](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Tracks/Ribeye/Ribeye_Scene.tscn:26) to **168.75**, keeping `Keep Height`. Alternatively, use `Keep Width` with Size **300**.

The camera distances also differ: Blender uses 500 and Godot uses about 170.885. That difference is along the viewing direction and does not change orthographic magnification.

There are separate appearance differences: Blender is showing **Material Preview**, with scene lights and scene world disabled; Godot uses its scene lighting. Blender's grid is an editor overlay.

Also, your [export script disables camera export](C:/Users/k/Repository/Veehiicuul/Blender/Scripts/ExportToVeehiicuul.py:189), so Blender's camera settings are not automatically transferred to Godot.

## User

okay, i think the camera setups are identical now. verify this.

## Assistant

I will re-read both saved files and compare the camera transforms, orthographic framing, and clipping settings.

## Assistant

Verified from both saved files: **the zoom mismatch is resolved, and the camera angles match.**

| Setting             | Blender                | Godot                  |
| ------------------- | ---------------------- | ---------------------- |
| Projection          | Orthographic           | Orthographic           |
| Vertical span       | 150                    | 150                    |
| Frame at 16:9       | 266.667 x 150          | 266.667 x 150          |
| Angles (degrees)    | 45 yaw, 45 down, 0 roll | Same                   |
| Camera distance     | 500                    | 170.88507              |
| Near / far clipping | 1 / 1000               | 10 / 1000              |

They are **not literally identical**: distance and near clipping still differ. Distance does not change orthographic framing, but it changes where the clipping planes lie.

There is also a tiny frame-center difference of approximately **0.00042 world units**, equivalent to about **0.004 pixels at 2560 x 1440**.
