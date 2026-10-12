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

## User

how do we get the godot game to match the input latency of the c++ app

## Assistant Commentary

I'll check which Godot settings can reduce the queue and what needs to change in its DirectX 12 backend to reproduce the C++ frame loop.

## Assistant Commentary

One useful detail: Godot 4.7.2 clamps `frame_queue_size` to at least two, so setting it to one would have no effect. The useful change is presentation admission control, with the wait placed before Windows input processing and the game update.

## Assistant Final

**To match the C++ application's behavior with VSync still on, we should add the same presentation admission control to Godot's D3D12 backend and build a custom Godot export template.** The game's C# API does not expose that swap-chain control.

Your project already uses two swap-chain images and disables accumulated input. Also, setting `frame_queue_size=1` would accomplish nothing: the inspected Godot 4.7.2 engine [clamps it to at least two](C:/Users/k/Repository/External/Godot_4-7-2/servers/rendering/rendering_device.cpp:8366).

The engine change should do this:

1. **Create a waitable swap chain.** Add `DXGI_SWAP_CHAIN_FLAG_FRAME_LATENCY_WAITABLE_OBJECT`, call `SetMaximumFrameLatency(1)`, and obtain its wait handle. Preserve this configuration through resizing and swap-chain recreation.
2. **Wait before processing fresh input.** At the start of each game frame, wait for presentation admission and the previous frame's GPU completion. In Godot's Windows loop, this belongs before [input event processing](C:/Users/k/Repository/External/Godot_4-7-2/platform/windows/os_windows.cpp:2349). Placing the wait immediately before Present would leave the frame based on older input.
3. **Process input, update the game, render, and present immediately.** Keep the wait responsive to window messages, and refresh input after any blocking wait. Microsoft documents this [frame-loop ordering](https://learn.microsoft.com/en-us/windows/uwp/gaming/reduce-latency-with-dxgi-1-3-swap-chains).
4. **Keep resource buffering separate from admission.** Godot can retain two allocated frame contexts while allowing only one frame of work to proceed. Simply removing its minimum-two clamp needs a wider audit and would not implement the presentation control we need.

Then build a Windows x64 .NET release export template from the patched Godot 4.7.2 source, configure the game's export to use it, and repeat the captures under the same NVIDIA VSync override and monitor conditions. Your captured render-thread setting is already `Safe`; the implementation should preserve that and avoid letting simulation run ahead of admitted rendering.

The validation targets are:

- Present-to-GPU-completion falls from about 16 ms toward the C++ application's sub-millisecond result.
- Present-to-display falls from about 24 ms toward one refresh interval, about 8.33 ms.
- The two later Present calls before each frame's display disappear.
- PresentMon's input estimate approaches the C++ result of about 9 ms.

That is a concrete route to matching the pipeline. The resulting latency still needs measurement; matching actual gamepad response additionally requires associating a gamepad reading with the frame that displays its effect.

## User

i would like you to do this engine change and somehow have this saved as part of this repo. tell me how you would do this.

## Assistant Commentary

I'll inspect the Godot checkout and build tools, then propose a way to keep the engine patch, build process, and export integration reproducible in this repo.

## Assistant Commentary

The local Godot checkout is clean and matches the captured engine version exactly: `4.7.2-stable`, commit `ed1daf0bf001b61586d9930840f2f1394092c079`. I'd store our patch and build scripts in this repo, apply them to a separate generated checkout, and point both game export presets at the resulting template. Visual Studio and .NET are installed; SCons needs to be set up.

## Assistant Final

I would keep this as a **pinned Godot source revision plus a reviewable patch and build scripts in this repo**. A fresh checkout would contain everything needed to reconstruct our modified engine.

The tracked files would look like this:

```text
Engine/
  Godot/
    Upstream.json
    Requirements.txt
    Patches/
      PresentationLatency.patch
    Build.cmd
    Build.ps1
    Readme.md
```

`Upstream.json` would pin Godot `4.7.2-stable` at `ed1daf0bf001b61586d9930840f2f1394092c079`, which matches your installed engine and the clean local source checkout. It would also record the required dependency versions. `PresentationLatency.patch` would contain our engine changes.

The build script would create a separate, ignored source checkout under `Engine/Godot/`, verify that revision, apply the patch, and compile an optimized Windows x64 .NET release export template with D3D12 enabled. It would use a local Python environment with pinned SCons dependencies. Visual Studio and .NET are already installed; SCons currently isn't. Generated source, dependencies, binaries, and timestamped `LogOutput` would be gitignored. `Build.cmd` would invoke `pwsh`.

For the engine patch, I would:

- Add the DXGI waitable swap chain and maximum presentation latency of one.
- Add a frame admission step before Windows input processing and the game update, including GPU readiness, so input is sampled after the waits.
- Handle the first frame, resize, minimize/restore, shutdown, and swap-chain recreation without hanging the message loop.
- Keep Godot's two allocated frame contexts, while limiting admitted work separately.
- Log the engine revision, patch identity, and active queue policy at startup so captures identify the running implementation.

Godot supports [custom Windows export templates](https://docs.godotengine.org/en/stable/engine_details/development/compiling/compiling_for_windows.html), including [builds with .NET support](https://docs.godotengine.org/en/stable/engine_details/development/compiling/compiling_with_dotnet.html). I'd keep the C# API unchanged so the installed Godot editor can continue importing and exporting the project using our custom release template.

I'd then integrate it into [the game build script](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/Build.ps1) and both release export presets in [export_presets.cfg](C:/Users/k/Repository/Veehiicuul/Veehiicuul_Godot_CSharp/Veehiicuul/export_presets.cfg). The game build would check the engine manifest and patch fingerprint, rebuilding when needed and failing if it cannot produce the required template. This makes the normal build produce the modified game consistently.

Finally, I'd test the window lifecycle and take matching PresentMon captures on **NE18NZ2 at 120 Hz**, retaining your NVIDIA VSync override. Success means the two future presents disappear, Present-to-GPU-completion approaches the C++ result, and Present-to-display approaches one refresh interval. I'd save the capture analysis and measured result in the repo; reaching roughly 9 ms would be a measured outcome, not an assumption.

I'd split the work into separate commits for the engine patch/build support, game export integration, and validation results. Conversation commits would remain separate.

## User

i have moved to a higher tier model and effort level.
reanswer this prompt:

i would like you to do this engine change and somehow have this saved as part of this repo. tell me how you would do this.

## Assistant Commentary

I'll review the proposed engine hook and repository layout again, focusing on where Godot can wait safely and how to make the modified build reproducible.

## Assistant Commentary

Two details need to be explicit in the plan. The latency mode must work while Godot reports VSync disabled, because your NVIDIA override is external. Also, the wait must run at a safe frame boundary before fresh input is processed; putting it inside Present or framebuffer acquisition would be too late for that frame's input.

## Assistant Commentary

I'd compare an unmodified source build with the patched build using the same compiler settings and game executable path. That controls for build differences and the NVIDIA application profile. Both export presets also need to package the D3D12 runtime dependencies consistently; they currently differ there.

## Assistant Final

I recommend committing **the exact Godot revision, a small series of engine patches, and scripts that build and validate the modified engine**. That keeps the change reviewable and lets a fresh checkout reconstruct it.

I would add this package at the repo root:

```text
Engine/
  Godot/
    Upstream.json
    BuildConfiguration.json
    Requirements.txt
    Patches/
      WaitableSwapChain.patch
      FrameAdmission.patch
    PrepareSource.ps1
    UpdatePatches.ps1
    Build.cmd
    Build.ps1
    Readme.md
    Validation/
      CompareCaptures.ps1
```

The upstream revision would be Godot `4.7.2-stable`, commit `ed1daf0bf001b61586d9930840f2f1394092c079`. The scripts would obtain that exact source, using your existing local checkout as a source when available, and prepare a separate working copy. They would verify the base revision and apply the committed patches. I would generate those patches from engine Git commits, so subsequent edits have a straightforward path back into this repository.

Generated source, downloaded dependencies, the Python environment, build products, and timestamped `LogOutput` would be ignored. Git would hold our modifications and the reconstruction recipe. A fresh machine would also need the documented compiler prerequisites and access to the pinned upstream sources and dependencies.

I would carry out the work in four stages:

1. **Establish a reproducible baseline build.** Build an unmodified Windows x64 .NET D3D12 export template first and verify that it runs your game. Pin downloaded dependency versions and hashes, record the MSVC/Windows SDK versions and build options, and write those identities into a build manifest. Then use the same configuration for the patched build. This lets us attribute a latency change to the patch. SCons would be installed in the local build environment; Visual Studio and .NET are already available.

2. **Implement the frame admission policy in the engine.** The D3D12 backend would create a waitable swap chain, set maximum frame latency to one, and manage the handle through creation, resize, recreation, and destruction. A native engine hook would wait for presentation admission and the relevant GPU completion before processing fresh input and advancing the game frame. It must run outside rendering locks and service window messages safely. Microsoft requires the wait for every rendered frame, including the first. [DXGI documentation](https://learn.microsoft.com/en-us/windows/win32/api/dxgi1_3/nf-dxgi1_3-idxgiswapchain2-getframelatencywaitableobject)

   **The policy must be enabled independently of Godot's VSync setting.** Your game reports VSync disabled while NVIDIA forces it on. Conditioning the new behavior on `VSYNC_ENABLED` would miss your actual use case. I would enable the policy explicitly for this game and initially support its existing `Safe` render-thread mode. Godot's two resource contexts would remain allocated; the new admission policy would limit how far execution advances.

   The handle and GPU fence need separate treatment: Godot currently [signals the frame fence before calling Present](C:/Users/k/Repository/External/Godot_4-7-2/drivers/d3d12/rendering_device_driver_d3d12.cpp:2472). GPU completion alone cannot establish that presentation has caught up. I would add diagnostic timestamps to verify that the final wait precedes the input used by the frame.

3. **Make the normal game build use the custom engine.** Update the game build and both export presets to select the custom release template through repository-relative paths. Package the matching D3D12 runtime dependencies consistently; the two presets currently differ there. Before export, check the template's source, patch, and configuration identities and rebuild when they change. Record the active engine identity and latency policy in the game's startup log.

   The initial measurement would use the exported executable, matching your captures. Running through the editor's Play button uses the editor binary, so I would also provide a matching patched editor build for that workflow. The build must include the appropriate .NET bindings and support files, following [Godot's .NET build process](https://docs.godotengine.org/en/stable/engine_details/development/compiling/compiling_with_dotnet.html).

4. **Verify both correctness and latency.** Exercise startup, resize, fullscreen transitions, minimize/restore, closing during a wait, and swap-chain recreation. Then compare repeated unmodified and patched captures on **NE18NZ2 at 120 Hz**, using the same game executable path, NVIDIA application profile, scene, and presentation mode. Save the build identities, captures, analysis script, and results in the repo.

The performance target is to eliminate the two later presents currently submitted before each frame reaches display, bring Present-to-display near one refresh interval, and retain steady 120 FPS. I would compare distributions and outliers as well as means. Approaching the C++ application's roughly 9 ms PresentMon input estimate is the expected result to test; actual gamepad response would need input-to-frame instrumentation.

I would commit the build infrastructure, engine patches, game integration, and validation results as separate reviewable changes, with conversation commits kept separate.
