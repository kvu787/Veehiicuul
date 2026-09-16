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

## User

okay, let's do some input latency experiments.

i would like to use "C:\Users\k\Program\PresentMon-2.5.1-x64.exe". the plan is to use presentmon to capture data from a minimal godot scene and the program in "C:\Users\k\Repository\Veehiicuul\Cpp". for each game, i will open it, click in the window to focus it, and continually do keyboard keypresses, mouse movement, and mouse click. presentmon should capture input latency data for those inputs. then, we can compare the captures and see if there is a meaningful difference in input latencies between godot engine vs custom cpp/dx12.

does this make sense? analyze the presentmon source code at C:\Users\k\Repository\External\PresentMon_2-5-1 to determine how it captures input latency.

## Assistant

I’ll trace how PresentMon 2.5.1 detects input and associates it with displayed frames, then check which parts of your proposed experiment it can measure reliably.

## Assistant

The experiment makes sense as a first comparison, but the source reveals an important limitation: PresentMon associates Windows input events with subsequently presented frames; it does not check whether those frames visibly respond to the input. It also combines input events, so mixing constant mouse movement with keypresses would make the results harder to interpret.

## Assistant

I found a detail that affects the test design: although the documentation says “earliest” input, this version’s code keeps the most recent general input timestamp. Continuous mouse movement can therefore replace the timestamp of an earlier keypress. I’d run clicks, keyboard presses, and mouse movement separately, starting with a short capture to confirm that each produces valid samples.

## Assistant

**Yes—as a first comparison of input-to-presentation timing. But PresentMon alone cannot prove how quickly the game visibly reacts to each input.** Your experiment is useful if we account for that distinction and test each input type separately.

I inspected the local source at tag **v2.5.1**, commit `3e06c7dcb922`, and confirmed that your executable reports version 2.5.1.

PresentMon captures input latency through **Windows tracing events**, without needing changes to either game:

1. **Windows reads an input device.** PresentMon records the timestamp and whether it was a mouse or keyboard, using `InputDeviceRead_Stop`.
2. **A process retrieves input for a window.** PresentMon associates the tracked input timestamp with that process, using `RetrieveInputMessage_Info`. Mouse clicks receive additional tracking through `OnInputXformUpdate_Info`.
3. **That process presents a frame.** PresentMon assigns the pending input information to the newly tracked frame.
4. **Windows reports the frame’s display timing.** PresentMon subtracts the associated input timestamp from that display timestamp.

Those steps are implemented in the [input-event handlers](C:/Users/k/Repository/External/PresentMon_2-5-1/PresentData/PresentMonTraceConsumer.cpp:1698), [input-to-frame assignment](C:/Users/k/Repository/External/PresentMon_2-5-1/PresentData/PresentMonTraceConsumer.cpp:2576), and [latency calculation](C:/Users/k/Repository/External/PresentMon_2-5-1/IntelPresentMon/CommonUtilities/mc/MetricsCalculatorInput.cpp:14).

The two relevant default CSV columns are:

| Column                      | What it represents                                      |
| --------------------------- | ------------------------------------------------------- |
| `MsAllInputToPhotonLatency` | Associated keyboard or mouse input → frame display time |
| `MsClickToPhotonLatency`    | Associated mouse click → frame display time             |

With `--v2_metrics`, the names omit the `Ms` prefix; values remain milliseconds. There is no separate keyboard-only or mouse-movement-only column.

**There are three important limitations for your experiment.**

**First, it infers which frame contains the response.** It does not inspect the game’s input handlers, simulation state, or pixels. A program can retrieve a keypress, ignore it, and still receive a latency measurement. Likewise, if Godot postpones the visible response until a later physics update, PresentMon does not automatically discover that delay. The association is made at the process/presentation level.

Therefore, your existing C++ program and a minimal Godot scene can potentially produce these metrics without reacting visibly. To make the comparison represent actual responsiveness, I would give both a simple immediate response—such as changing a rectangle’s color on a keypress or click—and ensure that state reaches the next rendered frame.

**Second, it does not preserve a separate measurement for every input event.** This is especially relevant to your proposal to keep moving, clicking, and typing simultaneously.

Although the [documentation](https://github.com/GameTechDev/PresentMon/blob/v2.5.1/README-ConsoleApplication.md) describes the “earliest” contributing input, the source explicitly keeps the **most recent general input timestamp** and replaces older timestamps. Keyboard and mouse activity share that tracking. Continuous mouse movement can therefore replace the timestamp of an earlier keypress. [Tracking state and its explanatory comments](C:/Users/k/Repository/External/PresentMon_2-5-1/PresentData/PresentMonTraceConsumer.hpp:526)

Consequently, a low value during continuous mouse movement does not mean every movement event or keypress had that latency.

**Third, “to photon” is a metric name, not an optical measurement.** The normal capture starts at a Windows input-read event and ends at a display timestamp derived from system events. It does not measure the physical button’s travel, earlier device/USB delays, or the monitor’s actual pixel response.

There are also normal missing samples: frames without associated input can contain `NA`. Inputs associated with a frame that was not displayed can be carried forward to a later displayed frame. Neither behavior gives us a one-row-per-keypress log. [Calculation and carry-forward behavior](C:/Users/k/Repository/External/PresentMon_2-5-1/IntelPresentMon/CommonUtilities/mc/MetricsCalculatorInput.cpp:30)

**I would adjust your experiment as follows:**

1. **Start with a short validation capture for each application.** Focus the window before the measurement interval, then verify that clicks, keyboard presses, and movement actually populate the expected columns. Godot and C++ use different input-reading paths, so successful coverage should be checked.
2. **Run three separate tests:** mouse clicks with the mouse stationary; keyboard presses with the mouse stationary; and mouse movement without clicking or typing. Use discrete presses rather than holding a key for automatic repeats.
3. **Keep the conditions identical:** standalone release builds, D3D12, the same window size, monitor, 100 FPS driver cap, VSync/VRR settings, and comparable rendering work. Capture the game process, not the Godot editor.
4. **Repeat in alternating order.** Compare valid-sample counts, median latency, and the slower tail, such as the 95th percentile. Report missing samples separately; never treat `NA` as zero.

Input and display tracking are **enabled by default**. These basic columns do not require the optional application-instrumentation flags. [Capture defaults](C:/Users/k/Repository/External/PresentMon_2-5-1/PresentMon/CommandLine.cpp:391)

This would give us a useful comparison of the two presentation pipelines under your driver cap. A claim about actual physical input-to-visible-response latency would additionally require verified input-to-frame association and, for the display itself, a camera or sensor measurement.

## User

okay, this is the part that concerns me:
First, it infers which frame contains the response. It does not inspect the game’s input handlers, simulation state, or pixels. A program can retrieve a keypress, ignore it, and still receive a latency measurement. Likewise, if Godot postpones the visible response until a later physics update, PresentMon does not automatically discover that delay. The association is made at the process/presentation level.
Therefore, your existing C++ program and a minimal Godot scene can potentially produce these metrics without reacting visibly. To make the comparison represent actual responsiveness, I would give both a simple immediate response—such as changing a rectangle’s color on a keypress or click—and ensure that state reaches the next rendered frame.

first thing i need to emphasize: the game *does not* have a separate physics update or any kind of other separate update/step. the game is very simple. it has one update loop that should get the freshest possible input, compute game state, and then draw the game state.

for custom cpp/dx12, i'm confident that we can use the proper api to get the freshest input.

my concern is about godot engine. once again, my game only needs one update loop. i simply need to ensure that i get the freshest input when reading input from within the update loop in godot engine. how do i ensure this?

## Assistant

I’ll trace Godot’s Windows input processing into `_process()` and check whether you can refresh input there, so the answer fits your single-loop design.

## Assistant

Godot exposes the call you need: `DisplayServer.process_events()`. In your Windows source, it reads pending Windows input and flushes it into Godot’s input state. Calling it at the start of `_process()` lets you refresh input immediately before computing game state; input getters alone just read Godot’s stored state.

## Assistant

**Your single-update-loop design is supported by Godot. Use `_process()`, and call `DisplayServer.process_events()` immediately before reading input and computing game state. No engine modification is needed for that.**

My earlier example about waiting for a physics update does not apply to the design you’ve described.

Godot normally processes Windows input before entering its main iteration. An explicit `DisplayServer.process_events()` at the start of your update refreshes input again, catching events that arrived since that earlier processing. [Normal Windows loop](C:/Users/k/Repository/External/godot/platform/windows/os_windows.cpp:2355)

In your local Godot source, that call:

- Reads pending raw mouse input.
- Processes queued keyboard, mouse-button, and other window messages.
- Flushes the resulting events into Godot’s input state before returning.

That behavior is directly visible in the [Windows implementation](C:/Users/k/Repository/External/godot/platform/windows/display_server_windows.cpp:4515), and the method is exposed to scripts. [Godot API documentation](https://docs.godotengine.org/en/latest/classes/class_displayserver.html#class-displayserver-method-process-events)

For example, with a `ColorRect` child named `Indicator`:

```gdscript
extends Node

@onready var Indicator: ColorRect = $Indicator

func _process(_Delta: float) -> void:
    # Refresh input immediately before this frame's game update.
    DisplayServer.process_events()

    # Read input and compute this frame's state.
    var Pressed := (
        Input.is_physical_key_pressed(KEY_SPACE)
        or Input.is_mouse_button_pressed(MOUSE_BUTTON_LEFT)
    )

    # Give that state to the renderer during this same update.
    Indicator.color = Color.WHITE if Pressed else Color.BLACK
```

Here, the rectangle is white while Space or the left mouse button is held. Godot runs the rendering step after the game’s process callbacks, so this does not intentionally defer the new color to a later game update. [Update-before-render ordering](C:/Users/k/Repository/External/godot/main/main.cpp:5055)

**The important distinction is between refreshing input and reading stored input:**

| Operation                        | What it does                                                |
| -------------------------------- | ----------------------------------------------------------- |
| `Input.is_action_pressed(...)`   | Reads Godot’s current stored input state.                   |
| `Input.flush_buffered_events()`  | Processes events Godot has already collected.               |
| `DisplayServer.process_events()` | Processes pending OS events and, on Windows, flushes input. |

Calling only `Input.flush_buffered_events()` does **not** fetch newly arrived Windows messages. In this Windows implementation, adding it after `DisplayServer.process_events()` is redundant.

For your setup, I would also:

- Keep the controller’s `_process()` on the **main thread**, and use rendering thread model **Safe** (`1`).
- Keep physics interpolation disabled and apply the computed state directly to the visible nodes.
- For discrete presses, use a mapped action’s `Input.is_action_just_pressed()` when you need to retain a brief press that was released between updates.
- For captured relative mouse movement, collect and sum motion events in `_input()`, then consume that sum during `_process()`. That callback collects input; it does not need to run a second game update. Do not call `process_events()` recursively from `_input()`.

**You do not need to disable input accumulation to make this refresh work.** The explicit Windows processing call flushes accumulated events before returning. Disabling accumulation changes event coalescing and processing cost; it is not the essential freshness control here.

The precise guarantee is that you process available Windows input immediately before your game update. It is not direct hardware polling, and input arriving after that sampling point can still miss the current frame—as it can in C++. Later GPU, presentation, or driver-limiter waits remain separate from this input-refresh question.
