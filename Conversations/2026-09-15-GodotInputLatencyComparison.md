# Conversation: Godot Direct3D 12 input latency comparison

## User

what would it take to make a godot engine game that has its dx12 rendering pipeline configured in a way that matches the MinimizeInputLatency profile for "C:\Users\k\Repository\Veehiicuul\Cpp"? in other words, how much input latency optimization am i sacrificing by using godot engine instead of a custom c++/dx12 renderer?

the godot source code is here: C:\Users\k\Repository\External

## Assistant

I’ll compare the C++ latency profile with Godot’s Direct3D 12 frame loop, then identify what Godot can match through settings and what would require engine changes.

## Assistant

The source shows a real gap: Godot can match VSync-off, tearing, and two swap-chain buffers, but it enforces at least two GPU frame slots and lacks your C++ renderer’s explicit DXGI presentation-admission wait. I’m checking the wait timing relative to input processing, because queue counts alone don’t tell us the latency cost.

## Assistant

The timing check confirms that Godot does not impose an unavoidable extra input-buffering frame in its default rendering mode. The main differences are GPU overlap and presentation admission. Matching those looks like a focused engine modification; the latency cost in milliseconds still needs a matched benchmark, since the existing C++ captures did not measure physical input-to-photon latency.

## Assistant

**Stock Godot cannot exactly match your `MinimizeInputLatency` profile through settings alone. A custom Godot build could reproduce its queueing and synchronization behavior without replacing Godot’s renderer.** The remaining difference would be the CPU/GPU cost of running your game through Godot, plus how you handle input and simulation.

I inspected your local **Godot 4.8.dev**, commit `dfa06cafb454`, and the current C++ implementation. This is a source comparison, not a measured latency benchmark.

| Control                    | Your C++ profile                 | Closest stock Godot D3D12                  |
| -------------------------- | -------------------------------- | ----------------------------------------- |
| GPU frames in flight       | 1                                | Minimum 2 frame slots                      |
| DXGI maximum frame latency | Explicitly set to 1              | No explicit setting                       |
| Presentation admission     | Waitable swap chain              | Not implemented                           |
| Swap-chain buffers         | 2                                | Configurable to 2; default 3               |
| VSync and tearing          | VSync off; tearing when supported | Can match                                 |
| GPU wait strategy          | Spin                             | Event wait; no selectable spin             |
| Application FPS cap        | None                             | Can disable                               |

VSync is separate from your preset; the shipped configuration disables it. Sources: [C++ profile](C:/Users/k/Repository/Veehiicuul/Cpp/Source/RenderPreparation.h:26), [Godot’s hard minimum](C:/Users/k/Repository/External/godot/servers/rendering/rendering_device.cpp:8619), [swap-chain implementation](C:/Users/k/Repository/External/godot/drivers/d3d12/rendering_device_driver_d3d12.cpp:2903), and [event wait](C:/Users/k/Repository/External/godot/drivers/d3d12/rendering_device_driver_d3d12.cpp:2549).

The important distinction is **when fresh input enters a frame relative to unfinished work**. Your C++ renderer waits for GPU capacity and DXGI presentation admission, processes window messages again, and then computes the frame’s state. That is a stronger policy than merely using two buffers. [C++ frame preparation](C:/Users/k/Repository/Veehiicuul/Cpp/Source/Renderer.cpp:1274)

Godot’s default “Safe” rendering mode does not inherently add a separate render-thread frame of delay. Its frame-resource wait happens at the preceding draw’s tail, before the next Windows input pump. However, with two frame slots, the immediately preceding GPU frame can still be unfinished when the next frame’s input and simulation are processed. Also, GPU completion does not establish presentation readiness. [Godot frame sequence](C:/Users/k/Repository/External/godot/servers/rendering/rendering_device.cpp:8119)

**How much latency does that sacrifice? There is no fixed millisecond penalty.**

- **When the GPU finishes quickly:** the extra queue capacity may remain unused, so its latency cost can be small.
- **When the GPU is saturated:** stock Godot permits one additional older GPU frame compared with your profile. The resulting delay can be on the order of a GPU frame’s duration, depending on CPU/GPU overlap.
- **When presentation is the bottleneck:** the missing DXGI admission wait can matter separately, because a frame may already contain older input when presentation blocks. Microsoft describes why waiting before producing the frame can reduce this delay. [DXGI latency guidance](https://learn.microsoft.com/en-us/windows/uwp/gaming/reduce-latency-with-dxgi-1-3-swap-chains)

For scale, one frame interval is **16.7 ms at 60 FPS, 6.9 ms at 144 FPS, or 4.2 ms at 240 FPS**. Those are illustrations, not measured Godot penalties, and the queueing effects should not simply be added together.

Your C++ policy also sacrifices CPU/GPU overlap. Godot could achieve higher throughput from that overlap, partially offsetting the latency disadvantage. Conversely, more expensive Godot rendering or game logic could make the difference larger. Neither engine choice alone determines the result.

The closest stock configuration would use D3D12 with Forward+ or Mobile, VSync disabled, and:

- `rendering/rendering_device/vsync/swapchain_image_count = 2`
- `rendering/rendering_device/vsync/frame_queue_size = 2` — entering `1` is silently clamped to `2`.
- `rendering/driver/threads/thread_model = 1` — “Safe,” for the simplest comparison.
- `application/run/max_fps = 0`, `frame_delay_msec = 0`, and `low_processor_mode = false`. Set `low_processor_mode_sleep_usec = 0` as well to eliminate the automatic undrawable-window delay under your no-background-cap policy.

For gameplay, avoid unnecessarily delaying camera or immediate visual feedback until a 60 Hz physics tick or an interpolated transform. Waiting for a physics tick can itself approach 16.7 ms. You can also test `Input.use_accumulated_input = false` for mouse handling, but that changes event coalescing; it does not remove GPU queueing.

To match your profile’s behavior, I would make a focused engine modification:

1. **Add the DXGI presentation gate:** create a waitable swap chain, set maximum frame latency to one, and manage its handle through swap-chain recreation.
2. **Enforce one unfinished GPU frame:** preferably keep Godot’s existing resource allocations initially and add an independent admission limit. Simply changing the minimum frame-slot count is not the complete fix. A separate fence placed after `Present` would also mirror your C++ ordering without repurposing Godot’s existing fences.
3. **Gate before input and simulation:** wait for both GPU and presentation readiness, including the initial frame, then process fresh Windows input. Preserve this ordering if separate-thread rendering is later enabled.
4. **Add spin waiting for exact parity**, while keeping window messages responsive. Spin is a CPU/power tradeoff, not a guaranteed latency improvement.

That is engine/backend work, not just a GDScript setting. My rough estimate for someone familiar with Godot and D3D12 is **a few working days for a prototype and a few weeks for a tested implementation**, including resize, minimize/restore, presentation-mode changes, and measurements. I have not built or tested such a patch.

For your decision, I would first compare a minimal Godot release build against C++ using the **same input-driven visual response and workload**, then test the engine patch if the gap matters. Your existing C++ captures cannot answer this: their input-to-photon fields were unavailable, and their very high presentation FPS is not an input-latency measurement. [Capture limitations](<C:/Users/k/Repository/Veehiicuul/Cpp/SavedLogOutput/2026-09-11 MinimumInputLatency/Report.md:329>)

## User

okay, i don't fully understand a lot of this. let's say that the game is extremely performant with respect to a target fps of 100, in that the game's frametime (cpu + gpu work) easily finishes within 500 microseconds. the game's fps is limited to 100 using nvidia app/nvidia control panel

what would be the minimum achievable input latency with godot engine vs custom c++/dx12?

## Assistant

I’ll use your 0.5 ms workload and 100 FPS limit to separate the delay from rendering from the delay caused by waiting between frames. The key uncertainty is where the NVIDIA limiter pauses the game relative to input processing.

## Assistant

**In that scenario, Godot could be essentially as responsive as your custom C++ renderer. Godot does not automatically add an extra 10 ms just because it allows another frame in flight.**

At **100 FPS**, a new frame is produced every **10 ms**. If producing it takes only **0.5 ms**, roughly **9.5 ms is spent waiting**. What matters most is whether the game reads your input **before or after that waiting**.

For an ideal comparison, assume:

- Both versions take exactly 0.5 ms to process input and produce the frame.
- They read fresh input after waiting, once per rendered frame.
- The response does not wait for a separate physics update, and no completed frames are held in a queue.

Then the numbers are identical:

| Input timing                                  | Godot    | Custom C++/DX12 |
| --------------------------------------------- | -------- | --------------- |
| Just before the game reads input: minimum      | ~0.5 ms  | ~0.5 ms         |
| Randomly timed input: average                  | ~5.5 ms  | ~5.5 ms         |
| Just after the game reads input: longest delay | ~10.5 ms | ~10.5 ms        |

**These are input-available-to-completed-frame times, not physical button-to-screen times.** Since you said the work finishes *within* 0.5 ms, those figures decrease if the actual work takes less time.

The minimum is small because an input arriving just before the game reads it can affect the next frame immediately. The average includes about 5 ms waiting for the next opportunity to read input.

**The NVIDIA limiter prevents us from promising those numbers in practice.** Consider two simplified sequences:

- **Fresh input:** wait 9.5 ms → read input → render for 0.5 ms → present.
- **Older input:** read input → render for 0.5 ms → wait 9.5 ms → present.

Both produce 100 FPS, but the second makes the rendered input about **9.5 ms older**. These illustrate possible timing arrangements; I am not claiming NVIDIA necessarily uses either one. NVIDIA documents the frame-rate cap, but its public description does not establish where that wait occurs relative to your game’s input processing. [NVIDIA’s settings reference](https://www.nvidia.com/content/Control-Panel-Help/vLatest/en-us/mergedProjects/nv3d/Manage_3D_Settings_%28reference%29.htm)

Your C++ profile controls its own waiting carefully, but it cannot guarantee that an external driver limiter will not introduce another delay afterward.

**For your very lightweight game, I would not assume a meaningful latency sacrifice from choosing Godot.** The ideal minimum is the same; the actual difference needs measurement with that limiter enabled. Physical input-to-screen latency also adds mouse/controller delay and display delay, which depend on the monitor’s refresh rate, VSync/VRR settings, scanout, and pixel response.
