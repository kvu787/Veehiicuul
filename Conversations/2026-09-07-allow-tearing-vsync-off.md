# Conversation: AllowTearing with VSync off

## User

what happens when allowtearing is true or false when vsync is off

## Assistant

I’ll check how this project uses `AllowTearing` with VSync off.

## Assistant

With **VSync off**, this project uses a sync interval of zero in both cases:

- **`AllowTearing = true`**: passes `DXGI_PRESENT_ALLOW_TEARING` when supported. The display may switch frames during a refresh, producing a visible tear line. This permits lower-latency presentation and supports the DXGI variable-refresh-rate path; it does not force visible tearing.
- **`AllowTearing = false`**: calls `Present(0, 0)`. In this project’s windowed/borderless mode, Windows handles presentation without requesting tearing. Rendering can still run faster than the monitor; intermediate frames can be discarded before display. This is not equivalent to turning VSync on, and presentation can still block.

Microsoft documents the [tearing flag](https://learn.microsoft.com/en-us/windows/win32/direct3ddxgi/dxgi-present) and [variable refresh rate requirements](https://learn.microsoft.com/en-us/windows/win32/direct3ddxgi/variable-refresh-rate-displays).

**For your current settings:** `Mode = MaximizeFps` uses `AllowTearing = true`. The `AllowTearing = false` under `[Pipeline.Custom]` is ignored unless you select `Mode = Custom`.

## User

if vsync is off, does either setting of allowtearing have any effect on input latency

## Assistant

I’ll check how the two presentation paths affect input latency.

## Assistant

**Yes—`AllowTearing = true` can reduce input-to-screen latency even with VSync off.**

- **`true`:** permits a newer frame to become visible during the current refresh, so part of the screen can reflect your input sooner.
- **`false`:** in this project’s windowed/borderless mode, presentation remains tear-free and can wait for a refresh/composition opportunity, delaying when you see the response.

The difference is **not a fixed number of milliseconds or a guaranteed improvement**. It depends on the Windows presentation path, monitor/VRR, FPS, and queued frames. Microsoft explicitly notes that allowing tearing can enable [lower presentation latency](https://learn.microsoft.com/en-us/windows/win32/direct3ddxgi/for-best-performance--use-dxgi-flip-model).

For your project, **use `AllowTearing = true` when prioritizing latency**. It affects when input becomes visible; it does not directly make input polling faster. Your current `MaximizeFps` preset already enables it.

## User

for a unity engine game like C:\Users\k\Repository\Unity\ZoomTracks, how do i know what allowtearing policy the renderer uses

## Assistant

I’ll inspect ZoomTracks’ Unity version and graphics settings, then check how to verify the tearing policy used by the running game.

## Assistant

ZoomTracks already has saved PresentMon captures. The August 2 captures include frames where Unity passed the tearing flag, while PresentMon reported `AllowsTearing = 0` for the composed display path. That distinction matters: the flag tells you what Unity requested; `AllowsTearing` describes how Windows presented the frame.

## Assistant

**Check PresentMon's `PresentFlags` column. Your saved ZoomTracks capture already shows Unity requesting tearing with VSync off.**

In the [August 2 capture](<C:/Users/k/Repository/Unity/ZoomTracks/ZoomTracks/SavedLogOutput/2026-08-02_05-16-02 -- maxQueuedFrames 0/PresentMon.csv>), I found:

| Field           | Recorded value                    | Meaning                                |
| --------------- | --------------------------------- | -------------------------------------- |
| `SyncInterval`  | `0`                               | Unity requested VSync off.              |
| `PresentFlags`  | `512` (`0x200`)                    | Unity passed the allow-tearing flag.    |
| `AllowsTearing` | `1` for 3,027 frames               | Partial frames could reach the screen. |
| `PresentMode`   | `Hardware: Independent Flip` there | Those frames used independent flip.    |

Two other frames had the same `PresentFlags = 512`, but `AllowsTearing = 0` and `Composed: Flip`. **Permission to tear and the actual presentation path are separate.** `AllowsTearing = 1` also does not prove a visible tear occurred. [PresentMon field definitions](https://github.com/GameTechDev/PresentMon/blob/v2.5.1/README-ConsoleApplication.md).

The accompanying log identifies **Unity 6000.3.21f1, Direct3D 12, borderless fullscreen**. Your current project uses **6000.3.23f1**, so this establishes the historical behavior; a fresh capture verifies the current build.

To check again, launch the standalone game with VSync off, run this in PowerShell, and return focus to the game during the five-second delay:

```powershell
& "C:\Users\k\Program\PresentMon-2.5.1-x64.exe" `
  --process_name ZoomTracks.exe `
  --delay 5 --timed 15 --terminate_after_timed `
  --output_file "C:\Users\k\ZoomTracks-tearing.csv"
```

For each frame, **`(PresentFlags & 0x200) != 0` means Unity requested tearing**. Check the bit rather than equality to `512`, because other flags can be combined. True DXGI exclusive fullscreen uses different rules: this flag is prohibited there, so its absence does not establish tear-free presentation. [Microsoft's flag documentation](https://learn.microsoft.com/en-us/windows/win32/direct3ddxgi/dxgi-present).

The Unity **Use DXGI Flip Model Swapchain** checkbox alone cannot answer this: it selects the D3D11 swap-chain model, and D3D12 always uses flip model. [Unity documentation](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/PlayerSettings-useFlipModelSwapchain.html).
