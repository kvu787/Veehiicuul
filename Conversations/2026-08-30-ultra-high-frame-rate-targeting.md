# Targeting 1,000 to 5,000 FPS

Date: 2026-08-30

Repository: `Simple_DirectX12_3D_Game`

## User

> presentmon is reporting that this app runs consistently in the 5500 to 6000 fps range, which is promising.
>
> list things i should be aware of when writing a game that targets the 1000 to 5000 fps range as the intended fps range, instead of the more common 60 to 500 fps range.

## Assistant

The biggest caveat is that 5,500–6,000 FPS probably means **presents per second**, not 5,500–6,000 complete images scanned out by the monitor. It does prove that this renderer can currently produce a present every 167–182 microseconds. However, a 5,000-FPS budget is 200 microseconds, so the current demo has only 18–33 microseconds of headroom before adding gameplay, input, audio, UI, or more content.

| Target | Frame budget | Approximate cycles on one 5 GHz core |
|---:|---:|---:|
| 1,000 FPS | 1,000 us | 5.0 million |
| 2,000 FPS | 500 us | 2.5 million |
| 5,000 FPS | 200 us | 1.0 million |

Things to design around:

1. **Define which FPS you mean.** PresentMon distinguishes FPS-Presents from FPS-Display. Inspect `MsBetweenPresents`, `MsCPUBusy`, `MsCPUWait`, `MsGPUTime`/`MsGPUBusy`, `DisplayLatency`, `DisplayedTime`, and `PresentMode`, rather than relying on one overlay number. A frame with `DisplayedTime = NA` was not displayed. Optimize end-to-end input age and latency, not merely calls to `Present`.

2. **The monitor remains the final bottleneck.** A 240 Hz display scans one image over about 4.17 ms. With tearing enabled, several newer frames can theoretically replace portions of the scanout during that interval, which can reduce the age of some screen regions, but 5,000 presents do not become 5,000 complete visible images. At 5,000 FPS on 240 Hz there are roughly 21 render intervals per scanout; the benefit is lower information age and smaller tear bands, with strongly diminishing returns.

3. **Decouple simulation from rendering.** Use a fixed simulation step and render an interpolated or extrapolated snapshot. Physics, AI, cooldowns, particle emission, animation, camera smoothing, RNG consumption, replay recording, networking, and audio must not run “once per rendered frame.” Otherwise behavior and cost change by a factor of five across the intended 1,000–5,000-FPS range.

4. **Treat input as timestamped events, not one poll per frame.** A 1,000 Hz mouse supplies at most one new sample per millisecond, so most 5,000-FPS renders reuse input. An 8,000 Hz mouse can deliver every 125 microseconds, but Windows may queue several events. Raw Input's buffered path is important. Sample the newest available state as late as practical before simulation/render submission, without making gameplay correctness depend on the render rate.

5. **Use microsecond-grade timing correctly.** Use QPC, integer ticks, or `steady_clock` backed by QPC, and keep absolute/accumulated time in `double` or integer units. Never accumulate total runtime in `float`: its precision eventually becomes larger than a 0.2 ms frame. The current animation computes a fresh `double` elapsed time and only converts the final bounded values to `float`, which is a sound pattern.

6. **Accurate caps become difficult.** `Sleep(1)` consumes an entire 1,000-FPS frame and is far too coarse for 5,000 FPS. A cap needs absolute QPC deadlines, a wait for the coarse portion, and possibly a very short spin for the tail. Advance from the previous deadline rather than `now` to avoid drift. Windows is not a hard-real-time OS: preemption, DPCs, interrupts, and scheduler wake-up jitter can exceed 200 microseconds, so define percentile goals rather than promising every frame meets the deadline. Higher timer resolution and busy-spinning also cost power.

7. **Queue depth matters more than raw throughput.** CPU-ahead and GPU-ahead frames can make a high FPS counter coexist with poor input latency. Keep frames in flight deliberately bounded and measure input-to-photon latency. The current two-back-buffer fence scheme bounds resource reuse, while sync-interval-zero plus tearing is the appropriate unthrottled presentation path. Also test an explicit low-queue mode using the DXGI frame-latency waitable object; do not assume the mode with the most presents has the least latency.

8. **Fixed per-frame overhead dominates.** Command allocator/list resets, barriers, `ExecuteCommandLists`, `Present`, fence signals, atomics, locks, logging, allocator calls, and cache misses all become large at 200 microseconds. Batch and instance draws, use persistent descriptor/resource storage, preallocate transient rings, and avoid hot-path file I/O. Do not split tiny work into jobs when dispatch and synchronization cost more than the work; a compact single render thread can win at this scale.

9. **Pixel throughput still scales linearly.** At 5,000 FPS, 1920x1080 is 10.4 billion pixel samples per second, 2560x1440 is 18.4 billion, and 4K is 41.5 billion—before overdraw, MSAA, blending, and post-processing. A fullscreen pass that seems free at 500 FPS may dominate at 5,000. Build quality levels around overdraw, resolution, shader cost, and bandwidth, not only draw-call count.

10. **Hitches become enormous relative to budget.** A 1 ms allocation, page fault, lock contention event, asset upload, shader/PSO creation, device transition, or logging flush loses five 5,000-FPS frames. Precompile PSOs and shaders, stream asynchronously, keep data resident, and record telemetry into a preallocated memory ring that is flushed later. Report median plus p95, p99, p99.9, and worst frame time; average FPS hides the failures that matter.

11. **Profiling can be larger than the thing measured.** Compare runs with and without overlays/tracing. PresentMon notes that GPU execution timing with Hardware-Accelerated GPU Scheduling can be inaccurate by roughly 0.5 ms in some workloads—more than the entire 5,000-FPS budget. Use D3D12 timestamp queries or PIX for GPU duration, QPC for CPU phases, and PresentMon for presentation/display behavior.

12. **Power and heat are product requirements.** Uncapped multi-kilohertz rendering can hold a CPU core and GPU at high clocks for a latency improvement the player may not perceive. Expect fans, battery drain, thermal throttling, and less headroom for streaming or background processes. Offer caps and an uncapped option; sharply throttle when unfocused, occluded, minimized, paused, or on a power-saving profile. Soak-test long enough to reach thermal equilibrium.

13. **Test the presentation environment, not just the renderer.** Validate borderless/windowed modes, VRR on/off, multiple monitors, overlays, capture software, HAGS, hybrid-GPU laptops, vendor control-panel overrides, and different driver versions. Independent flip can fall back to composed presentation when the desktop environment changes, altering latency without changing game code.

For this repository specifically:

- The flip-discard, tearing-capable swap chain and `Present(0, ALLOW_TEARING)` path are the right foundation.
- The wall-clock animation is already render-rate-independent.
- The message loop currently removes only one Windows message and then skips rendering for that iteration. High-rate Raw Input can therefore reduce or even starve rendering. Drain the message queue in a batch, drain buffered Raw Input, then perform the next game/render iteration.
- The current result comes from a deliberately tiny workload: two draws, embedded precompiled shaders, persistent mapped constants, and no physics, input system, audio, networking, UI, or streaming. Treat 5,500–6,000 as the baseline overhead measurement, not yet as demonstrated 5,000-FPS game capacity.

My recommended next milestone is not adding more visual content first. Add low-overhead CPU phase timings, GPU timestamp queries, percentile reporting, buffered Raw Input, and a fixed-tick simulation snapshot. Then create synthetic CPU and GPU load controls so you can discover exactly how much real game work fits inside 200, 500, and 1,000 microseconds.

Sources: [PresentMon metrics](https://github.com/GameTechDev/PresentMon/blob/main/README-ConsoleApplication.md), [Direct3D 12 swap-chain guidance](https://learn.microsoft.com/en-us/windows/win32/direct3d12/swap-chains), [DXGI flip-model guidance](https://learn.microsoft.com/en-us/windows/win32/direct3ddxgi/for-best-performance--use-dxgi-flip-model), [high-resolution timing](https://learn.microsoft.com/en-us/windows/win32/sysinfo/acquiring-high-resolution-time-stamps), [buffered Raw Input](https://learn.microsoft.com/en-us/windows/win32/inputdev/using-raw-input), and [D3D12 GPU timing](https://learn.microsoft.com/en-us/windows/win32/direct3d12/timing).
