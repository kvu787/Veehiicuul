# Rendering pipeline configuration

The application reads `assets/Settings.ini` beside the executable at startup.
Edit the source INI and launch through `Run.cmd` to stage and apply changes.
The [repository constraints](../README.md#constraints) apply to all modes.

## Mode and independent VSync

```ini
[Rendering]
VSync = false

[Pipeline]
Mode = Standard

[Pipeline.Custom]
MaxGpuFramesInFlight = 2
MaxPresentLatency = 2
WaitForPresentation = true
BackBufferCount = 3
AllowTearing = false
WaitStrategy = Event
```

`[Rendering].VSync` and `[Pipeline].Mode` are required. Names, mode values,
`true`/`false`, and wait-strategy values are case-sensitive. Whitespace around
keys and values is ignored; `;` and `#` introduce comments.

VSync is independent of the pipeline. Neither preset can override it. The `V`
key toggles it in every mode without rewriting the INI. VSync uses sync
interval one; disabling it uses sync interval zero. Enabling VSync suppresses
the tearing presentation flag without changing the pipeline's AllowTearing
preference. That preference applies again after VSync is disabled.

## Presets

| Setting              | MinimizeInputLatency | Standard |
| -------------------- | -------------------- | -------- |
| MaxGpuFramesInFlight | 1                    | 2        |
| MaxPresentLatency    | 1                    | 2        |
| WaitForPresentation  | true                 | true     |
| BackBufferCount      | 2                    | 3        |
| AllowTearing         | true                 | false    |
| WaitStrategy         | Spin                 | Event    |

MinimizeInputLatency favors minimal deliberate frame backlog, late input
processing, and active polling of synchronization readiness. It minimizes
latency subject to the separately selected VSync state. Spin waiting consumes
CPU time; it does not wait for a frame-rate deadline. This is a fixed initial
latency preset, not a guarantee of the lowest physical input-to-photon latency
on every system. Extra CPU/GPU overlap or a different presentation path can
win on some systems; use Custom to compare actual response latency.

Standard permits two GPU frames and two queued presentation frames, provides
three image buffers, and uses event waits to balance throughput, queue depth,
and CPU usage. With its AllowTearing=false preference, turning VSync off does
not request tearing. Neither mode imposes an application FPS target.

## Custom settings and validation

Only `Mode = Custom` interprets `[Pipeline.Custom]`. All six keys in the example
are required in Custom. Presets do not parse, validate, or merge custom
entries, regardless of where the custom section appears in the file. Inactive
custom entries may contain unknown keys, duplicate keys, or invalid values;
the enclosing file must still be syntactically valid INI.

In Custom, unknown or duplicate keys, missing settings, and invalid values
are errors. Diagnostics identify the section, key, and line where available.
A VSync entry in the active custom section is an error: use `[Rendering]`.

- `MaxGpuFramesInFlight`: integer 1 through 16, including the executing frame.
- `MaxPresentLatency`: integer 1 through 16. Required and validated in Custom,
  but inactive when WaitForPresentation is false.
- `WaitForPresentation`: `true` or `false`.
- `BackBufferCount`: integer 2 through 16, independent of frame-context count.
- `AllowTearing`: `true` or `false`, effective only with VSync off and DXGI
  tearing support. Unsupported tearing falls back to the flag being off.
- `WaitStrategy`: `Event` or `Spin`.

Counts are upper bounds, not guarantees that all available slots are occupied.
A smaller buffer count or another tighter queue limit can constrain throughput.
Buffers are never reused while their tracked GPU work is unfinished.

## Frame admission and cancellation

Frame contexts own command allocators, dynamic constants, and completion-fence
values. A submission sequence selects contexts independently of the swap-chain
back-buffer index. The renderer waits for the selected context and buffer to
be safe before accepting the next frame. At a GPU limit of one this waits for
the previous frame before computing the next frame's animation state.

WaitForPresentation=true creates a DXGI waitable swap chain and applies
SetMaximumFrameLatency. The presentation wait must admit the first frame as
well as later frames. False creates an ordinary non-waitable swap chain and
leaves the per-swap-chain maximum-latency setting inactive.

Event mode checks completion first, then uses GPU fence events and the DXGI
handle with message-aware Windows waits. Spin mode polls completion and DXGI
readiness with CPU pause instructions, servicing messages regularly. Neither
strategy sleeps or spins to enforce an FPS cap.

The application processes bounded batches of window messages so sustained
input cannot prevent all rendering. It processes newly arrived messages again
after readiness waits and before computing and submitting the frame. Resize,
minimize, interactive sizing, and shutdown can cancel an admission attempt;
resize is applied outside the wait callback. An already consumed presentation
permit is retained until a frame is actually presented, including across a
cancelled attempt or buffer resize. Presentation readiness and GPU completion
are distinct conditions and both must be satisfied when enabled.

## Diagnostics and verification

The window title shows the mode, GPU frame limit, active presentation limit
(or `inactive`), buffer count, wait strategy, independent VSync state, and
tearing state (`on`, `not requested`, `unsupported`, or `inactive (VSync)`).
The startup pipeline description is also sent to the Windows debug output.

`Run.ps1 -Test` runs configuration tests, production renderer integration tests
on the selected adapter and WARP, and an application lifecycle smoke test.
Tests cover inactive custom isolation, invalid active settings, VSync in every
mode, independently sized rings, limits up to 16, both wait strategies,
cancellation while GPU work is blocked, resize/restore, VSync hotkeys,
fullscreen transitions, and shutdown. GPU tests inspect the D3D12 debug layer
and require Windows Graphics Tools. Test fixtures do not overwrite user INI
settings or target windows belonging to other processes.

These checks establish configuration and synchronization behavior. They do
not measure physical input-to-photon latency or establish that the latency
preset is optimal for every GPU, CPU, and display combination.
