# NVIDIA VSync latency comparison

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

with vsync on via nvidia control panel and running on a 120 hz monitor, the mean input latency is roughly one interval (9 ms) for "C:\Users\k\Repository\Veehiicuul\SavedInputLogs\2026-10-12_01-08-17 3dTestScene_CppDx12 nvcp vsync on" and roughly three intervals (25 ms) for "C:\Users\k\Repository\Veehiicuul\SavedInputLogs\2026-10-12_01-12-35 Veehiicuul_Godot_CSharp nvcp vsync on". these captures are for the "C:\Users\k\Repository\Veehiicuul\3dTestScene_CppDx12" and "C:\Users\k\Repository\Veehiicuul\Veehiicuul_Godot_CSharp" applications, correspondingly. explain why

## Assistant Commentary

I'll compare the captures and each application's input, rendering, and presentation path to see where the extra latency comes from.

## Assistant Commentary

Both captures contain the same input-latency metric, and it confirms about 9.0 ms versus 25.1 ms. The clearest difference is after Present: Godot takes about 16.1 ms to reach render completion, while C++ takes about 0.23 ms. Both show roughly 0.25 ms of active GPU work, which points to queued work and synchronization waits.

## Assistant Commentary

The source matches that timing difference. C++ selects a preset that limits GPU and presentation queues to one frame, and waits for presentation admission before draining input messages again. Godot uses ordinary DXGI Present calls and two GPU frame contexts, without that explicit presentation admission wait. Its two swap-chain images do not impose the same queue limit.

## Assistant Commentary

The frame ordering confirms the backlog: in all 2,640 compared C++ frames, each frame reaches display before the next Present call. In all 3,925 Godot frames, two later Present calls occur before that frame reaches display. That directly shows the two additional frames queued ahead.

## Assistant Final

**Godot is submitting frames roughly two refresh intervals farther ahead of display.** NVIDIA's VSync override makes both applications display at 120 Hz, but their queue limits remain different.

I analyzed both CSVs, excluded the first and last second, and compared frames using the same `Hardware Composed: Independent Flip` presentation mode:

| Mean timing                         | C++     | Godot    |
| ----------------------------------- | ------- | -------- |
| All input to display                | 9.00 ms | 25.05 ms |
| Present to GPU completion           | 0.23 ms | 16.05 ms |
| Present to display                  | 8.16 ms | 24.10 ms |
| Active GPU work                     | 0.23 ms | 0.25 ms  |
| Time between displayed frames       | 8.33 ms | 8.33 ms  |

These are calculated from your [C++ capture](<C:/Users/k/Repository/Veehiicuul/SavedInputLogs/2026-10-12_01-08-17 3dTestScene_CppDx12 nvcp vsync on/PresentMon.csv>) and [Godot capture](<C:/Users/k/Repository/Veehiicuul/SavedInputLogs/2026-10-12_01-12-35 Veehiicuul_Godot_CSharp nvcp vsync on/PresentMon.csv>).

The ordering confirms this: every compared C++ frame reaches display before the next Present call, while Godot issues two later Present calls before that frame reaches display.

**Nearly all the extra latency appears between Present and GPU completion.** Godot adds about 15.8 ms there, despite almost identical active GPU work. After GPU completion, both take approximately another 8 ms to reach display. That points to queued work and synchronization waits; the rendering itself takes only about a quarter of a millisecond. PresentMon defines these timing boundaries in its [CSV documentation](https://github.com/GameTechDev/PresentMon/blob/v2.6.0/README-ConsoleApplication.md#csv-columns).

The C++ application's `MinimizeInputLatency` preset resolves to one GPU frame in flight, maximum presentation latency of one, two buffers, and an explicit presentation admission wait. The numeric custom fields in Settings.json are ignored when this preset is selected. See [preset resolution](C:/Users/k/Repository/Veehiicuul/3dTestScene_CppDx12/Source/RenderPreparation.cpp:6) and [preset values](C:/Users/k/Repository/Veehiicuul/3dTestScene_CppDx12/Source/RenderPreparation.h:26).

It creates a [waitable swap chain](C:/Users/k/Repository/Veehiicuul/3dTestScene_CppDx12/Source/Renderer.cpp:427), calls `SetMaximumFrameLatency(1)`, and [waits before rendering](C:/Users/k/Repository/Veehiicuul/3dTestScene_CppDx12/Source/Renderer.cpp:1288). It also drains input messages again after the waits. This keeps the next frame fresh and prevents a backlog of future frames. Microsoft describes this exact [latency reduction technique](https://learn.microsoft.com/en-us/windows/uwp/gaming/reduce-latency-with-dxgi-1-3-swap-chains).

Godot 4.7.2's inspected D3D12 backend instead creates an [ordinary swap chain](C:/Users/k/Repository/External/Godot_4-7-2/drivers/d3d12/rendering_device_driver_d3d12.cpp:2795) and [submits work followed by Present](C:/Users/k/Repository/External/Godot_4-7-2/drivers/d3d12/rendering_device_driver_d3d12.cpp:2472). It has no corresponding waitable swap chain or explicit presentation limit of one. Its GPU frame queue defaults to two contexts. Your `swapchain_image_count=2` controls image buffers; it does not impose the C++ application's presentation queue limit. Godot documents these as [separate settings](https://docs.godotengine.org/en/stable/classes/class_projectsettings.html#class-projectsettings-property-rendering-rendering-device-vsync-frame-queue-size).

With forced VSync, that pipeline settles into displaying frames about three refresh intervals after submission, while C++ stays near one. Both can sustain 120 FPS because throughput and the age of the displayed frame are different quantities. The CSVs establish the extra delay, although they cannot separate every internal Godot, DXGI, and NVIDIA queue contribution.

The 9 and 25 ms figures specifically match `MsAllInputToPhotonLatency`, PresentMon's keyboard/mouse estimate. It associates input events with presented frames; it does not inspect pixels to verify a visible response or measure gamepad input. The C++ scene mostly animates independently of input, so these captures demonstrate the pipeline difference rather than a verified physical button-to-pixel measurement.

