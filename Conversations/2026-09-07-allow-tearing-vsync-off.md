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
