# Rendering pipeline configuration

## Pipeline mode presets

Select a preset with `[Pipeline].Mode` in [assets/Settings.ini](../assets/Settings.ini).
Each preset supplies a fixed combination of the six pipeline settings below.
`[Rendering].VSync` is chosen separately and remains independent of every mode.
The shipped INI selects `MaximizeFps` with `VSync = false`.

| Setting                | MinimizeInputLatency | Standard | MaximizeFps  |
| ---------------------- | -------------------- | -------- | ------------ |
| `MaxGpuFramesInFlight` | 1                    | 2        | 3            |
| `MaxPresentLatency`    | 1                    | 2        | 2 (inactive) |
| `WaitForPresentation`  | true                 | true     | false        |
| `BackBufferCount`      | 2                    | 3        | 4            |
| `AllowTearing`         | true                 | false    | true         |
| `WaitStrategy`         | Spin                 | Event    | Spin         |

### MinimizeInputLatency

This preset favors a small frame backlog so newly processed input and animation
state can reach the screen sooner. It permits one GPU frame in flight, sets the
DXGI presentation limit to one, and allocates two swap-chain image buffers.
Before preparing the next frame, the application waits for the previous GPU
frame to finish and for DXGI to admit another presentation. It services window
messages again after these waits, before computing the frame's animation state.

`Spin` actively polls readiness, trading CPU time and power for the possibility
of reacting sooner than an event wait. `AllowTearing = true` permits tearing
when VSync is off and DXGI supports it. With VSync on, presentation still obeys
VSync and the tearing flag is suppressed.

Use this preset as a starting point when responsiveness matters most. Its name
expresses an optimization goal: it does not guarantee the lowest physical
input-to-photon latency on every system. A one-frame GPU limit reduces overlap
between CPU preparation and GPU execution, which can lower throughput; another
combination may perform better on a particular CPU, GPU, and display.

### Standard

This preset balances CPU/GPU overlap, queue depth, and CPU usage. It permits two
GPU frames in flight, uses a DXGI presentation limit of two, and allocates three
swap-chain image buffers. The CPU can prepare another frame while earlier GPU
work is still executing, subject to presentation and resource availability.

`Event` waits let the thread block while it has no frame to prepare, waking for
readiness or window messages. `AllowTearing = false` means the renderer does not
request tearing, including when VSync is off. VSync off still uses a zero sync
interval; it does not become equivalent to VSync on.

Use this preset as a balanced baseline, especially when sustained CPU usage
matters. Its additional overlap can improve throughput relative to a one-frame
GPU limit, while allowing more older work to remain ahead of newly sampled state.

### MaximizeFps

This experimental preset favors throughput. It permits three GPU frames in
flight, allocates four swap-chain image buffers, and disables the explicit DXGI
presentation admission wait. The swap chain is non-waitable, so the stored
`MaxPresentLatency = 2` is inactive. `Spin` polls GPU resource readiness, and
`AllowTearing = true` permits tearing with VSync off when supported.

Use this preset to test whether additional overlap keeps the CPU and GPU busier.
Mandatory resource-reuse waits remain, and `Present` or buffer availability can
still block progress. The preset does not guarantee maximum FPS: more queue
capacity cannot remove an already saturated bottleneck, and spinning can reduce
throughput on some systems.

Possible costs include increased input latency, CPU/power use, heat, tearing,
and buffer memory. Resolution, geometry, and shading are unchanged. With VSync
enabled, presentation remains subject to VSync.

All modes follow the [repository constraints](../README.md#constraints):
synchronization uses vendor-neutral Windows, Direct3D 12, and DXGI interfaces,
and the application implements no FPS cap or timed frame-rate limiter.

## How `Mode = Custom` works

`Custom` lets you supply all six pipeline settings explicitly. To start with the
same pipeline behavior as `MaximizeFps`, replace the corresponding sections in
`assets/Settings.ini` with:

```ini
[Rendering]
VSync = false

[Pipeline]
Mode = Custom

[Pipeline.Custom]
MaxGpuFramesInFlight = 3
MaxPresentLatency = 2
WaitForPresentation = false
BackBufferCount = 4
AllowTearing = true
WaitStrategy = Spin
```

Leave the material and sphere sections in place. Relaunch through `Run.cmd` to
stage the edited source INI beside the executable and apply the settings.

All six `[Pipeline.Custom]` keys are required. Custom does not inherit omitted
values from a preset; a missing key is an error. `MaxPresentLatency` must be
present and valid even when `WaitForPresentation = false` makes it inactive.
`VSync` belongs in `[Rendering]` and is required in every mode. Placing it in an
active `[Pipeline.Custom]` section is an unknown-setting error.

The three numeric controls are independently configurable within their accepted
ranges. There is no requirement that they match, or that `BackBufferCount` equal
`MaxGpuFramesInFlight + 1`. A valid combination may nevertheless leave capacity
unused because another limit or resource becomes the bottleneck.

When a preset is selected, `[Pipeline.Custom]` entries are ignored completely:
they do not override, merge with, or alter that preset. This is true regardless
of where the custom section appears in the file. Even unknown keys, duplicate
keys, and invalid values inside the inactive custom section are ignored. The
file must still have valid INI syntax and recognized section names.

For example, the shipped INI's custom values match `Standard`, but its selected
mode is `MaximizeFps`. Changing only `Mode` to `Custom` therefore activates those
Standard-like values. To modify a particular preset, first copy all six of its
values from the preset table, then change the desired setting.

In Custom, unknown or duplicate keys, missing settings, and invalid values cause
startup errors. Diagnostics identify the section, key, and line where available.
Section names, keys, mode names, booleans, and wait-strategy values are
case-sensitive. Whitespace around keys and values is ignored; `;` and `#` start
comments. Use `true` and `false`, not `1`, `0`, `yes`, or `no`.

## Individual settings

`VSync` is read from `[Rendering]` in every mode. The other six settings are read
from `[Pipeline.Custom]` only in Custom mode; presets supply their fixed values.
INI changes take effect at startup. The `V` key is the runtime exception: it
toggles VSync without changing the selected mode or rewriting the INI.

### VSync

- **Location:** `[Rendering]`
- **Accepted values:** `true` or `false`
- **Shipped value:** `false`, independent of the selected mode.

VSync controls the sync interval supplied to `Present`:

- `true`: calls `Present(1, 0)`, requesting presentation synchronized to vertical
  refresh with a sync interval of one. The tearing presentation flag is off.
- `false`: uses sync interval zero. The tearing flag then depends on
  `AllowTearing` and DXGI support.

For flip-model presentation, sync interval zero allows queued frames to be
superseded by newer ones; rendering or presenting a frame does not guarantee
that the monitor displays it. See Microsoft's
[`Present` documentation](https://learn.microsoft.com/en-us/windows/win32/api/dxgi/nf-dxgi-idxgiswapchain-present).

VSync can trade responsiveness and throughput for synchronized, tear-free
presentation. Its effect depends on refresh rate, frame duration, queueing, and
the Windows presentation path. Turning it off does not guarantee an unblocked
`Present` call or eliminate GPU and presentation waits.

Enabling VSync leaves the pipeline's `AllowTearing` preference intact. When you
press `V` again to disable VSync, that preference applies again if supported.
VSync also leaves `WaitForPresentation`, queue limits, buffer count, and
`WaitStrategy` unchanged.

### MaxGpuFramesInFlight

- **Location:** `[Pipeline.Custom]`
- **Accepted values:** integer `1` through `16`, inclusive
- **Preset values:** MinimizeInputLatency `1`; Standard `2`; MaximizeFps `3`.

This limits submitted frames whose tracked GPU work has not finished, including
the frame currently executing. A value of `1` means one unfinished GPU frame in
total, rather than one waiting frame in addition to an executing frame.

The renderer allocates this many frame contexts. Each context owns a command
allocator, a slot for dynamic constants, and a completion-fence value. Contexts
are selected in submission order. Before reusing one, the renderer waits for its
previous GPU work to complete; it separately checks the selected back buffer.
A fence is the GPU completion marker used to decide when reuse is safe.

With `1`, the previous GPU frame must complete before the next frame's animation
state is computed and its commands are prepared. Larger values allow more CPU
preparation to overlap unfinished GPU work, which can improve throughput. They
also allocate more per-frame resources and can let older frames accumulate,
increasing the delay before newly processed state becomes visible.

This limit remains active when `WaitForPresentation = false`. It does not set
the number of image buffers or bound the entire input-to-display path: GPU
completion and display presentation are separate milestones. Raising it may do
nothing if presentation, buffer availability, or another bottleneck already
restricts progress. The limit is capacity, not a target queue occupancy.

### MaxPresentLatency

- **Location:** `[Pipeline.Custom]`
- **Accepted values:** integer `1` through `16`, inclusive
- **Preset values:** MinimizeInputLatency `1`; Standard `2`; MaximizeFps `2` (inactive).

This is the maximum frame queue allowance passed to DXGI for the swap chain
when `WaitForPresentation = true`. It counts frames, not milliseconds. The
renderer applies it through
[`IDXGISwapChain2::SetMaximumFrameLatency`](https://learn.microsoft.com/en-us/windows/win32/api/dxgi1_3/nf-dxgi1_3-idxgiswapchain2-setmaximumframelatency),
which requires a swap chain created with the frame-latency waitable-object flag.

A lower value makes the application seek permission to begin another frame with
less presentation backlog allowed. A higher value permits more work ahead,
which may improve overlap or tolerate uneven frame times, but can increase
latency. Neither value guarantees a particular measured queue occupancy or
input-to-photon delay.

When `WaitForPresentation = false`, the renderer creates a non-waitable swap
chain and does not call `SetMaximumFrameLatency`. This setting is then inactive,
and the title shows `Present:inactive`. It is still required and range-checked
in Custom; `0` is invalid and is not how presentation waiting is disabled.

This is distinct from `MaxGpuFramesInFlight`: a completed GPU frame can still
await presentation. The two limits constrain different parts of frame progress;
do not add their values to calculate total latency. Buffer availability and the
Windows display path also affect how far work can advance.

### WaitForPresentation

- **Location:** `[Pipeline.Custom]`
- **Accepted values:** `true` or `false`
- **Preset values:** MinimizeInputLatency `true`; Standard `true`; MaximizeFps `false`.

This decides whether the application waits for DXGI to admit a new frame before
computing its animation state and recording its rendering commands.

- `true`: creates a swap chain with
  `DXGI_SWAP_CHAIN_FLAG_FRAME_LATENCY_WAITABLE_OBJECT`, applies
  `MaxPresentLatency`, and obtains a DXGI readiness handle. Frame preparation
  requires both presentation permission and safe GPU resource reuse.
- `false`: creates an ordinary non-waitable swap chain and skips the explicit
  presentation admission check. `MaxPresentLatency` has no effect, while GPU
  completion and resource-reuse checks remain mandatory.

The readiness check also applies to the first frame when enabled, following
Microsoft's
[`GetFrameLatencyWaitableObject` guidance](https://learn.microsoft.com/en-us/windows/win32/api/dxgi1_3/nf-dxgi1_3-idxgiswapchain2-getframelatencywaitableobject).
This is a check before starting a frame, not a wait inserted after rendering to
prove that all its pixels have appeared on the monitor.

Enabling the check can reduce time spent with already prepared work waiting to
be presented. Disabling it can permit more overlap, but does not guarantee higher
FPS, unlimited queueing, or nonblocking `Present` calls. DXGI and buffer
availability can still hold up progress.

This setting is independent of VSync: either value works with VSync on or off.
`WaitStrategy` controls how the enabled readiness checks wait. With presentation
waiting disabled, that strategy still applies to GPU readiness.

### BackBufferCount

- **Location:** `[Pipeline.Custom]`
- **Accepted values:** integer `2` through `16`, inclusive
- **Preset values:** MinimizeInputLatency `2`; Standard `3`; MaximizeFps `4`.

This is the number of image buffers requested for the flip-discard swap chain.
These buffers hold the rendered images used for presentation. Two provides a
double-buffered chain, three a triple-buffered chain, and so on; this is the
swap chain's buffer count, not an extra count to add to a front buffer.

The renderer obtains the current back-buffer index from DXGI. It chooses the
frame context separately, so `BackBufferCount` does not need to equal
`MaxGpuFramesInFlight`. It tracks each image buffer's last GPU fence and waits
before reusing unfinished resources. DXGI also governs when a presentation
buffer becomes available for GPU writes.

More buffers consume more image memory and may prevent buffer availability from
restricting CPU/GPU overlap. They do not force the renderer to queue that many
frames or guarantee more FPS. Fewer buffers reduce allocated image storage but
may constrain throughput even when other queue limits are larger.

For example, `MaxGpuFramesInFlight = 3` with `BackBufferCount = 2` is valid.
There are three frame contexts but only two swap-chain images, so image reuse
can constrain progress before all frame contexts are usefully occupied. Likewise,
four image buffers with a GPU limit of one still allow only one unfinished GPU
frame. Additional image capacity does not change either configured queue limit.

### AllowTearing

- **Location:** `[Pipeline.Custom]`
- **Accepted values:** `true` or `false`
- **Preset values:** MinimizeInputLatency `true`; Standard `false`; MaximizeFps `true`.

This permits the renderer to request presentation that can update the image
during a display refresh. Parts of different frames may then appear in one
refresh, producing a visible tear line. Permission does not guarantee visible
tearing on a particular frame or presentation path.

At startup, the renderer checks DXGI tearing support. If the preference is
`true` and supported, it creates the swap chain with
`DXGI_SWAP_CHAIN_FLAG_ALLOW_TEARING`. For each `Present`, it uses
`DXGI_PRESENT_ALLOW_TEARING` only when VSync is also off. This follows Microsoft's
[tearing flag requirements](https://learn.microsoft.com/en-us/windows/win32/direct3ddxgi/dxgi-present).

| VSync   | AllowTearing | DXGI support | Renderer call                            |
| ------- | ------------ | ------------ | ---------------------------------------- |
| `true`  | Either       | Either       | `Present(1, 0)`                          |
| `false` | `false`      | Either       | `Present(0, 0)`                          |
| `false` | `true`       | Unavailable  | `Present(0, 0)`                          |
| `false` | `true`       | Available    | `Present(0, DXGI_PRESENT_ALLOW_TEARING)` |

With VSync off, `true` can let newer content reach the screen sooner, reducing
presentation latency at the cost of possible tearing. The improvement is not a
fixed number of milliseconds and depends on the presentation path and display.
Microsoft describes the potential for lower latency in its
[flip-model guidance](https://learn.microsoft.com/en-us/windows/win32/direct3ddxgi/for-best-performance--use-dxgi-flip-model).

With VSync off and `false`, this windowed/borderless renderer does not request
tearing. Windows handles presentation without that permission; it may wait for
a refresh/composition opportunity while newer frames replace older queued ones.
This differs from VSync on because the sync interval remains zero. `Present`
can still block, and submitted FPS can differ from displayed FPS.

The tearing flags are also required for DXGI's variable-refresh-rate path;
allowing tearing does not itself guarantee VRR is active. See Microsoft's
[variable refresh rate documentation](https://learn.microsoft.com/en-us/windows/win32/direct3ddxgi/variable-refresh-rate-displays).

The app uses windowed or F11 borderless fullscreen presentation. If tearing is
unsupported, it falls back to omitting the flag. Toggling VSync on temporarily
suppresses the flag without changing the stored preference or recreating the
swap chain. This setting changes presentation permission, not input polling or
the configured queue limits.

### WaitStrategy

- **Location:** `[Pipeline.Custom]`
- **Accepted values:** `Event` or `Spin`
- **Preset values:** MinimizeInputLatency `Spin`; Standard `Event`; MaximizeFps `Spin`.

This controls how the CPU waits during normal frame preparation when GPU
resources or presentation permission are not ready. Both strategies enforce the
same readiness conditions and check completion before admitting a frame.

- `Event`: registers a GPU fence completion event when needed and waits on that
  event and/or the DXGI presentation handle using message-aware Windows waits.
  The thread can block while idle, reducing CPU use, and also wakes for window
  messages. Resuming it can involve scheduler wake-up overhead.
- `Spin`: repeatedly polls the GPU fence and, when enabled, the DXGI handle.
  Between polls it uses CPU pause instructions and services messages regularly.
  This may react sooner to readiness, but consumes CPU time and power while
  waiting. Contention and heat can offset the benefit or reduce throughput.

Changing this setting does not change queue sizes, VSync, tearing permission,
or whether presentation admission is required. `Spin` does not bypass waits,
and `Event` does not impose a deliberate frame delay. With
`WaitForPresentation = false`, both still wait for safe GPU resource reuse.

This choice applies to frame admission. The renderer's separate full-GPU flush
used for operations such as resize still uses a fence event. Neither strategy
sleeps or spins toward an FPS deadline; there is no application frame-rate cap.

## How a frame progresses

The application processes window messages and applies pending resize work before
attempting a frame. Normal frame preparation then proceeds as follows:

1. Select the next frame context by submission sequence and obtain the current
   swap-chain image index from DXGI.
2. Require completed GPU work for both selected resources. If presentation
   waiting is enabled, also require a DXGI admission permit. These conditions
   are checked together; permission can arrive before or after GPU completion.
3. Service newly arrived window messages again, then compute animation state,
   update dynamic constants, and record and submit rendering commands.
4. Call `Present` with the current VSync and tearing choices, then signal the
   completion fence associated with this frame context and image buffer.

Message processing uses bounded batches so sustained input cannot prevent all
rendering. Resize, minimize, interactive sizing, and shutdown can cancel a frame
admission attempt; resize is applied outside the wait callback. An already
consumed presentation permit is retained until the next frame is presented,
including across a cancelled attempt or buffer resize.

## Applying changes and reading diagnostics

The application reads `assets/Settings.ini` beside the executable at startup.
Edit the repository's [source INI](../assets/Settings.ini) and launch through
[Run.cmd](../Run.cmd) to build and stage it. Restart after configuration changes;
the `V` key toggles only the current run's VSync state.

The window title reports the selected mode, GPU frame limit, active presentation
limit or `inactive`, image-buffer count, wait strategy, VSync state, and tearing
state. For the shipped preset, its pipeline portion begins:

```text
MaximizeFps | GPU:3 Present:inactive Buffers:4 | Spin
```

Tearing diagnostics use these labels:

| Label              | Meaning                                                    |
| ------------------ | ---------------------------------------------------------- |
| `on`               | Requested, supported, and VSync is off.                    |
| `not requested`    | The selected pipeline has `AllowTearing = false`.          |
| `unsupported`      | Requested, but DXGI tearing support is unavailable.        |
| `inactive (VSync)` | Requested and supported, but VSync suppresses the flag.    |

`on` reports the renderer's effective request; it is not a measurement of visible
tearing or proof that VRR is active. The startup pipeline description is also
sent to Windows debug output.

## Comparing configurations and verifying behavior

For a useful comparison, start with a preset or its equivalent Custom values,
then change one setting at a time. Compare `Event` and `Spin`, smaller and larger
GPU/image counts, and presentation admission enabled and disabled. Keep
resolution, window/fullscreen state, VSync, scene settings, and capture method
consistent; allow warm-up and compare repeated runs.

Measure sustained FPS and frame-time variation, distinguishing rendered or
presented frames from frames actually displayed. Prefer the smallest queues
that reach the measured throughput plateau when latency also matters. FPS alone
does not measure physical input-to-photon latency. No preset adds benchmark
instrumentation to the render loop.

`Run.ps1 -Test` runs configuration tests, production renderer integration tests
on the selected adapter and WARP, and application lifecycle smoke tests for
Standard and MaximizeFps. Coverage includes inactive Custom isolation, invalid
active settings, VSync in every mode, independently sized resource rings, limits
up to 16, both wait strategies, cancellation during blocked GPU work,
resize/restore, VSync hotkeys, fullscreen transitions, and shutdown. GPU tests
inspect the D3D12 debug layer and require Windows Graphics Tools. Test fixtures
do not overwrite user INI settings or target other processes' windows.

These tests verify configuration and synchronization behavior. They do not
establish which preset has the lowest physical latency or highest throughput
for every system.

Preset definitions are in [ApplicationSettings.h](../Source/ApplicationSettings.h),
parsing and validation in [ApplicationSettings.cpp](../Source/ApplicationSettings.cpp),
frame admission and presentation in [Renderer.cpp](../Source/Renderer.cpp), and
message processing in [Application.cpp](../Source/Application.cpp).
