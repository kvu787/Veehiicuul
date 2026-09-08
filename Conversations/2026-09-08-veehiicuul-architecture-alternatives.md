# Conversation: VeeHiiCuuL architecture alternatives

## User

this codebase will evolve to become a port of https://github.com/kvu787/ZoomTracks and eventually the canonical implementation of a my solo-developed racing, vehicle customization, and track building game called VeeHiiCuuL.

one of the main goals of this game is to achieve optimal performance and latency on x64 windows pc.

i'm curious about alternative architectures for this game what the pros/cons would be.

## Assistant

I’ll review the repository’s constraints and the ZoomTracks project, then compare architectures against your priorities: Windows x64 performance, latency, and solo development.

## Assistant

ZoomTracks’ current scope is unusually favorable for a small custom runtime: flat 2D gameplay, a static environment, and a vehicle rendered in 3D for appearance. The bigger architectural decisions are likely to be simulation timing, frame queueing, and how the track builder shares code with the game—not just which graphics API you use.

## Assistant

My recommendation is a small native runtime with a reusable simulation core and an integrated track builder. I would keep DX12 as the baseline and treat DX11 as a benchmark candidate; the current scene is too small to assume DX12’s lower draw-call overhead will produce a meaningful latency advantage. I’ll also record this discussion in the repository, as your instructions require.

## Assistant

**For VeeHiiCuuL, I would favor a small C++/Win32/DX12 runtime, with gameplay separated from rendering and shared with the track builder.** That gives you control over latency while keeping the engine manageable for solo development. It is a recommendation based on your scope, not a measured performance winner.

I’m assuming VeeHiiCuuL retains ZoomTracks’ flat 2D gameplay and static environment, with 3D vehicle rendering for appearance. That is what its current [README describes](https://github.com/kvu787/ZoomTracks). Elevation, moving scenery, or large numbers of vehicles would change some tradeoffs.

**The main runtime alternatives**

| Architecture                         | Advantages for VeeHiiCuuL                                 | Costs and tradeoffs                                        |
| ------------------------------------ | -------------------------------------------------------- | ---------------------------------------------------------- |
| C++ / Win32 / DX12                    | Direct control over frame submission, memory, and input   | You own GPU synchronization, resource lifetimes, and tooling |
| C++ / Win32 / DX11                    | Simpler renderer; worth testing for this small workload   | Less explicit control; additional API/driver overhead       |
| Rust / Win32 / DX12                   | Stronger memory safety for ordinary gameplay and tooling  | Rewrite cost; unsafe graphics boundaries; no automatic speed gain |
| Unity or Godot runtime               | Existing editing, UI, asset, and debugging infrastructure | More engine behavior to understand and tune                 |
| Custom runtime + external editor     | Small shipping runtime with richer authoring tools       | Export pipeline, integration, and preview consistency costs |

DX12 transfers synchronization and resource-lifetime responsibilities from the runtime to your application. That control is useful, but **DX12 is not a guarantee of lower input-to-photon latency than DX11**. With a backdrop and a few vehicle draws, its draw-call scaling advantages may be relatively unimportant. That last point is my inference about your workload. [Microsoft’s DX11/DX12 comparison](https://learn.microsoft.com/en-us/windows/win32/direct3d12/important-changes-from-directx-11-to-directx-12).

I would keep your existing DX12 investment. A small DX11 comparison becomes worthwhile once you have representative driving and collision code to benchmark. Vulkan would add another implementation to maintain without an obvious benefit for your Windows-only goal.

**The internal architecture matters at least as much**

My proposed split is:

- **Game core:** vehicle dynamics, collision, race rules, track definitions, and customization data. No window or graphics dependencies.
- **Application:** input collection, simulation timing, mode transitions, and coordination with presentation.
- **Renderer:** consumes vehicle poses, materials, and track visuals; owns GPU resources and submission.
- **Track builder and customization UI:** edit the same definitions the game consumes, with immediate playtesting.
- **Content processing:** turns editable data into collision acceleration structures, meshes, and cached scenery.

These can be modules in one executable. They do not require separate processes, DLLs, a plugin system, or a general-purpose engine.

Your current renderer computes its demonstration animation inside `Renderer::Render()`. The first useful boundary would be moving gameplay state outside the renderer and passing it the resulting pose. That makes driving testable without a GPU and lets the builder use precisely the same driving behavior.

For the current small dynamic world, I would start with ordinary structs and explicit update functions. An ECS becomes attractive if you acquire many entities with varied combinations of behavior; adopting one now would not inherently improve latency.

**Threading: keep the input-to-render path short**

Start with one main thread coordinating input, simulation, and graphics submission. Use background workers for loading and expensive track rebuilds.

A separate simulation thread and render thread can increase overlap, but they introduce synchronization and potentially older snapshots. For a tiny simulation, that overhead may outweigh the saved work. Add that separation only when profiling identifies a bottleneck.

The intended order is:

`Wait for frame availability → collect fresh input → advance simulation → render → present`

Your current pipeline already waits before computing animation. Preserve that property when inserting gameplay. Microsoft specifically recommends waiting before preparing a frame so rendering uses fresher data. [DXGI latency guidance](https://learn.microsoft.com/en-us/windows/uwp/gaming/reduce-latency-with-dxgi-1-3-swap-chains).

**Simulation timing is the biggest unresolved choice**

| Timing architecture                  | Benefit                                      | Cost                                                     |
| ------------------------------------ | -------------------------------------------- | -------------------------------------------------------- |
| Variable timestep per rendered frame | Simple; fresh input affects the next frame   | Numerical behavior can change with frame duration         |
| Fixed timestep with interpolation    | Consistent integration steps; smooth visuals | Conventional interpolation displays older physics state  |
| Fixed timestep with visual prediction | Consistent core with a fresher displayed pose | Prediction errors, especially near collisions            |

For racing and vehicle tuning, I lean toward a fixed-step authoritative simulation. It makes comparisons between vehicles more meaningful and supports repeatable tests, although fixed steps alone do not guarantee deterministic replays.

I would benchmark rendering the latest state against interpolation and short visual prediction before choosing. Conventional interpolation adds delay; increasing the physics rate reduces the interval but costs CPU time. [Godot’s explanation of this tradeoff](https://docs.godotengine.org/en/stable/tutorials/rendering/jitter_stutter.html).

A fixed simulation timestep does **not** require an FPS limiter: the loop can run zero or more simulation steps per render, without sleeping to enforce a frame rate. That fits your repository policy.

**Exploit static scenery without constraining the builder**

Keep editable track geometry as the source of truth, then derive collision data and cached visuals from it. A flattened image should be an output, not the only representation of a track.

A single cached image is excellent for a bounded, fixed view. Larger tracks or substantial zoom may justify tiled caches or directly rendered geometry. Rebuild affected regions after edits, rather than making every racing frame pay for editable scenery.

Similarly, keep vehicle handling parameters distinct from visual parts, with explicit rules connecting them where customization changes performance. Precompute assembled geometry and collision footprints when customization changes.

**What would decide this empirically**

Compare implementations using the same track, vehicle, resolution, input sequence, and presentation settings. Measure CPU/GPU time, frame-time tails, and physical input-to-photon latency—not just average FPS. Include track editing and customization transitions, where allocation and upload stalls may appear.

Your constraints require vendor-neutral behavior, no application FPS cap, and no GPU preconditions. The existing SM6/WARP compatibility gap therefore remains relevant; software fallback is also not a promise of good performance.

I would first port one complete driving-and-editing slice into the modular native design. That gives you evidence about the actual game before investing in a new language, graphics backend, ECS, or multithreaded pipeline.


## User

I’m assuming VeeHiiCuuL retains ZoomTracks’ flat 2D gameplay and static environment, with 3D vehicle rendering for appearance. That is what its current README describes.

> yes, this is correct

after reading your reply, i am particularly curious about dx11 vs dx12. elaborate on this.

## Assistant

I’ll focus on DX11 versus DX12 for your confirmed workload: static 2D scenery, a 3D vehicle, and very little draw work. I’ll distinguish API overhead from the queueing and synchronization choices that affect latency.

## Assistant

Both APIs can use modern DXGI flip presentation and waitable swap chains; low-latency presentation is not exclusive to DX12. One nuance to my earlier answer: a tiny scene can make API overhead more significant as a fraction of frame time, so “few draws” alone does not establish that DX11 will be competitive.

## Assistant

**DX11 is a serious candidate for VeeHiiCuuL. DX12 gives you more explicit control, but your workload does not establish a clear performance winner.** The strongest reason to choose DX11 is simpler renderer ownership. The strongest reason to keep DX12 is the control you already have over submission and resource reuse.

One refinement to my previous answer: **few draw calls do not necessarily make API overhead irrelevant.** When GPU work is tiny and frames are produced extremely quickly, even small CPU and submission costs can become important.

**What actually changes between them**

Both APIs ultimately ask the GPU to draw your background and vehicle. The biggest difference is how much work the application handles before and around those draws.

| Responsibility       | DX11                                        | DX12                                         |
| -------------------- | ------------------------------------------- | -------------------------------------------- |
| Graphics state       | Bind individual state objects               | Use precreated pipeline state objects        |
| Resource hazards     | Runtime/driver handles much of the tracking  | Application manages resource transitions      |
| Updating busy buffers | Runtime can provide fresh storage on discard | Application selects storage safe for reuse    |
| Command submission   | Immediate context; implicit batching        | Explicit command lists and queue submission  |
| GPU resource lifetime | More dependencies handled by the runtime    | Application must retain resources until safe  |

DX12 removes work from the driver by making the application responsible for more bookkeeping. It pays off when your application can do that work efficiently using knowledge of its workload. It does not eliminate the work altogether. [Microsoft’s architectural comparison](https://learn.microsoft.com/en-us/windows/win32/direct3d12/important-changes-from-directx-11-to-directx-12).

For example, updating the car transform:

- **DX11:** map a dynamic constant buffer with discard, write the transform, unmap, and draw. The runtime can give you different backing storage while the GPU uses the previous contents.
- **DX12:** write into a mapped frame slot that you know is no longer in use, then submit commands referencing it. You track completion and decide when that slot can be reused.

Your current implementation already does the latter. It is a good match for a tiny, predictable update.

**Why DX12 could win here**

Your renderer can exploit a very regular frame: mostly unchanged resources, a small transform update, a few draws, and one submission.

That provides an opportunity to keep CPU work exceptionally small. You do not need a large engine’s general resource management or multithreaded command recording to benefit from DX12.

It also makes unexpected waits easier to reason about: you explicitly identify resource reuse and presentation admission as separate reasons to wait. That is valuable when tuning latency.

However, explicit control also creates opportunities to lose performance. Unnecessary barriers, overly conservative fences, or excessive submissions can undo the savings. Microsoft describes the application’s responsibility for coordinating the CPU and GPU timelines in its [porting guide](https://learn.microsoft.com/en-us/windows/win32/direct3d12/porting-from-direct3d-11-to-direct3d-12).

**Why DX11 could win—or be effectively tied**

DX11 lets you express this scene with relatively little application bookkeeping. A straightforward immediate-context renderer might perform very well without much tuning.

Its additional per-draw overhead is multiplied by only a few draws. Meanwhile, fixed costs such as presentation, scheduling, and submission may dominate the frame. Which implementation handles those costs better on a particular system is an empirical question.

DX11 could therefore be faster, slower, or indistinguishable in the measurements that matter. I would not assign a percentage advantage to either API without testing your game.

There are two useful cases to distinguish:

- **GPU work dominates:** drawing the same pixels with equivalent shaders leaves less opportunity for CPU API savings to change the result.
- **CPU/submission work dominates:** small differences in driver and application overhead can substantially affect maximum FPS.

Shader compilation and driver optimization can differ too, so equivalent HLSL does not guarantee identical GPU execution time.

**Latency is not an API version number**

Both DX11 and DX12 can use modern DXGI flip-model presentation, waitable swap chains, and supported tearing paths. A DX11 comparison should use those facilities, not an older presentation model. [Microsoft’s flip-model guidance](https://devblogs.microsoft.com/directx/dxgi-flip-model/).

For either API, a key question is:

**How old is the input represented by a frame when that frame becomes visible?**

Sampling input, preparing a frame, and then waiting behind older work ages that input. Waiting for availability first and preparing the frame using fresh input can reduce that delay. This technique is available with DX11 as well. [DXGI latency guidance](https://learn.microsoft.com/en-us/windows/uwp/gaming/reduce-latency-with-dxgi-1-3-swap-chains).

Your DX12 renderer currently checks both GPU resource readiness and presentation admission, then services messages again before rendering. That ordering is worth preserving in either implementation.

Also, your one-GPU-frame configuration reduces CPU/GPU overlap. That is a **pipeline policy**, not a requirement of DX12. Comparing it with a DX11 renderer that permits more outstanding work could show higher DX11 FPS mostly because the queueing policies differ.

Conversely, lower CPU render time does not guarantee lower physical latency if additional work accumulates in queues. A fence signals GPU progress; it does not establish when a pixel appeared on the monitor.

**The solo-development difference grows during editing**

During racing, your resource use is unusually simple. Track building and vehicle customization introduce replacement textures, rebuilt geometry, and changing buffers.

With DX12, replacing an asset includes retaining the old GPU resources and descriptors until outstanding work finishes using them. DX11 handles more of that dependency management for you. This is where its simplicity may save meaningful development time.

Neither API requires rebuilding everything each frame. Both can cache static track visuals and rebuild only after edits.

A DX11 port would also need a compatible shader build path; the existing SM6 DXIL binaries are not drop-in DX11 shaders. The SimplePaint mathematics may be reusable, but compiler output and numerical behavior would need validation. [Microsoft’s shader-model reference](https://learn.microsoft.com/en-us/windows/win32/direct3dhlsl/dx-graphics-hlsl-models).

**What I would do in this repository**

Keep DX12 as the baseline, then build a deliberately small native DX11 comparison when you want evidence. Share the scene, simulation, assets, and input handling; keep backend code separate without designing a large graphics abstraction.

Measure two comparisons:

1. **Matched behavior:** equivalent visuals, presentation settings, and comparable outstanding-work limits, to isolate backend differences.
2. **Best result per backend:** tune each independently, then compare latency, throughput, frame-time tails, and CPU usage.

Use Release builds without debug layers, repeat runs, and measure physical latency separately from software timings. Test more than one hardware configuration before treating a result as general. No application FPS limiter or vendor-specific integration is needed.

**Starting from scratch, I would lean toward DX11 for implementation simplicity. Given your existing compact DX12 renderer and your emphasis on optimal performance and latency, I would keep DX12 until a controlled comparison gives a concrete reason to switch.** Your game’s scope makes that comparison relatively affordable.


## User

i'm not sure if i understand this:
There are two useful cases to distinguish:
- GPU work dominates: drawing the same pixels with equivalent shaders leaves less opportunity for CPU API savings to change the result.
- CPU/submission work dominates: small differences in driver and application overhead can substantially affect maximum FPS.

## Assistant

I’ll use a hypothetical timing example to show what “dominates” means here.

## Assistant

Each frame involves two kinds of work:

- **CPU work:** run the game logic and prepare/send drawing commands.
- **GPU work:** execute those commands to draw the image.

DX11 versus DX12 can change the CPU cost of preparing and sending commands. My point was that **reducing that cost helps more when it is what’s holding up the next frame.**

Imagine these hypothetical timings:

| Situation     | CPU time per frame | GPU time per frame |
| ------------- | ------------------ | ------------------ |
| GPU does more | 1 ms               | 5 ms               |
| CPU does more | 5 ms               | 1 ms               |

The CPU and GPU can overlap: the CPU prepares one frame while the GPU draws an earlier frame. With sufficient overlap, throughput is roughly limited by whichever takes longer.

**In the first situation**, the GPU needs 5 ms per frame. Reducing CPU time from 1 ms to 0.5 ms does not let the GPU finish sooner. Maximum throughput stays around **200 FPS**: 1,000 ms divided by 5 ms.

**In the second situation**, the GPU could finish in 1 ms, but the CPU takes 5 ms to supply each frame. The GPU spends time waiting for work. Reducing CPU time from 5 ms to 2 ms could raise throughput from roughly **200 to 500 FPS**.

Those are illustrative numbers, not predictions about DX11 or DX12.

For VeeHiiCuuL, the backdrop and small vehicle may give the GPU so little work that **preparing and submitting each frame becomes the limiting factor**. In that case, API overhead can matter even though you have very few draw calls. At a sufficiently high resolution or on a slower GPU, drawing the pixels could instead become the limiting factor.

One qualification: **this explains maximum FPS, not input latency.** Your one-GPU-frame setting restricts overlap, so the “whichever takes longer” approximation may not describe it well. And even when a CPU saving does not increase FPS, it can still help latency by shortening the path from fresh input to the finished image.


## User

well, we already pretty much know what the game has to do in terms of sending commands to the gpu. it needs to render a static background image and then a 3d model on top of that shaded with SimplePaint. as far as i can think, that's it.

given that, do you have a better answer for dx11 vs dx12

## Assistant

I’ll narrow the recommendation to that exact rendering workload and distinguish what we can reasonably conclude from what still needs measurement.

## Assistant

**Yes. Given that exact workload and your performance-first goal, I recommend keeping DX12.** My earlier answers put too much emphasis on hypothetical future rendering complexity.

The reason is that **your renderer is simple enough to use DX12 efficiently without needing a complicated engine.** DX11’s main advantage here would be simpler implementation—not an established performance or latency advantage.

Your normal frame can stay approximately this small:

1. Wait until the required frame resources and presentation slot are available.
2. Read input, advance gameplay, and write the vehicle transform.
3. Draw the background texture.
4. Draw the vehicle with SimplePaint.
5. Submit and present.

The texture, mesh, shaders, and material data can remain on the GPU between frames. A static background still needs drawing into each presented frame, but it does not need uploading each time.

**Why this favors keeping DX12**

You know almost everything about the rendering work in advance:

- Which resources each draw uses.
- Which resources change each frame.
- Which pipeline states are needed.
- When a frame’s temporary storage can be reused.

That lets you handle DX12’s explicit responsibilities with a small, predictable implementation. You do not need multithreaded rendering, multiple GPU queues, or a general resource scheduler.

DX12’s advantages include improving **single-threaded** CPU efficiency through preparation and reuse; they are not limited to games with thousands of draws. Your workload can use that aspect while ignoring the machinery it does not need. [Microsoft’s CPU-efficiency specification](https://microsoft.github.io/DirectX-Specs/d3d/CPUEfficiency.html).

**What would switching to DX11 buy you?**

Mostly, fewer explicit responsibilities for command recording, buffer reuse, and resource lifetime. That is valuable when choosing an API from scratch.

But you already have a compact DX12 implementation handling those responsibilities. Switching would replace working infrastructure without a demonstrated gain. And DX11 does not have an inherent low-latency presentation advantage: both can wait for presentation availability before preparing a fresh frame. [Microsoft’s DXGI latency guidance](https://learn.microsoft.com/en-us/windows/uwp/gaming/reduce-latency-with-dxgi-1-3-swap-chains).

**What the known workload still cannot tell us**

It tells us the commands, but not their execution costs on a particular CPU, GPU, and driver. In such a short frame, differences in command submission, synchronization, and presentation can matter. Those costs are not determined solely by the number of draws.

So I can recommend **DX12 as the architecture to continue with**, but I cannot honestly declare it the measured fastest API. DX11 could still win a controlled comparison on particular systems.

There is also a useful correction to my previous recommendation: **you do not need to wait for the complete game to compare them.** Your current scene is already close to the intended graphics workload. A comparison using just the background and vehicle, with identical animation and equivalent presentation policies, would meaningfully test the rendering question now. Later gameplay integration would validate the result under the complete CPU workload.

**My decision would be: continue developing VeeHiiCuuL on DX12. Build a small DX11 benchmark only if you want to establish which implementation actually wins, rather than because the game is too simple to benefit from DX12.**


## User

does godot use dx11 at all?

## Assistant

I’ll check Godot’s documentation for its supported rendering backends.

## Assistant

**Yes—indirectly through ANGLE, but Godot does not have a native DX11 renderer.**

On Windows, Godot 4 has these paths:

| Rendering method | Graphics API path                          |
| ---------------- | ------------------------------------------ |
| Forward+ / Mobile | Vulkan or native Direct3D 12               |
| Compatibility    | OpenGL, or OpenGL ES → ANGLE → Direct3D 11 |

ANGLE translates the Compatibility renderer’s OpenGL ES commands into DX11 calls. Godot documents this as a way to improve compatibility on systems with problematic OpenGL drivers. [Renderer overview](https://docs.godotengine.org/en/stable/tutorials/rendering/renderers.html), [ANGLE support](https://docs.godotengine.org/en/stable/engine_details/development/compiling/compiling_for_windows.html#compiling-with-angle-support).

For our comparison, **Godot using ANGLE would not be a clean test of native DX11 versus your native DX12 renderer**: it also changes the engine, rendering implementation, and adds a translation layer. That does not automatically make it slow, but it prevents attributing the result solely to DX11.

## User

for me, a tie breaker for choosing between dx11 vs dx12 for my own racing game could be that if i use dx12, then it may be easier to understand godot's dx12 renderer. how applicable is the understanding of my game's dx12 to understanding godot's dx12 stuff

## Assistant

I’ll check how Godot divides its renderer from its DX12 backend, so I can distinguish the knowledge that transfers directly from the engine-specific parts.

## Assistant

**That is a good tie-breaker in favor of DX12. Understanding your renderer would transfer strongly to Godot’s DX12 backend, especially its synchronization, resource management, and presentation code.** It would transfer less directly to Godot’s overall rendering architecture.

Your small renderer is a useful learning environment precisely because you can see those mechanisms without navigating a whole engine.

| What you learn in VeeHiiCuuL          | How it helps when reading Godot                         |
| ------------------------------------ | ------------------------------------------------------ |
| Command lists and command queues      | Understand how recorded work reaches the GPU           |
| Fences and frame-resource reuse       | Understand waits and when memory can safely be reused  |
| Resource states and barriers          | Understand why GPU operations need synchronization     |
| Descriptor heaps and root signatures | Understand how textures and buffers reach shaders      |
| Pipeline state objects                | Understand creation, caching, and binding of pipelines |
| DXGI swap chains and presentation     | Understand the Windows-specific path to the display    |

These are actual mechanisms used in [Godot’s DX12 backend](https://github.com/godotengine/godot/blob/master/drivers/d3d12/rendering_device_driver_d3d12.cpp). The engine wraps and generalizes them, but the underlying DX12 concepts remain the same.

**The main extra thing to learn is how Godot organizes those mechanisms.**

Conceptually, Godot’s Forward+ and Mobile rendering paths look like this:

`Rendering algorithms → RenderingDevice abstraction → DX12 backend → GPU`

Godot also has a render graph that tracks dependencies between rendering operations and coordinates their execution. Much of its rendering logic is shared across graphics APIs rather than written directly against DX12. [Godot’s architecture documentation](https://docs.godotengine.org/en/stable/engine_details/architecture/internal_rendering_architecture.html), [render graph source](https://github.com/godotengine/godot/blob/master/servers/rendering/rendering_device_graph.cpp).

In your game, you know the background draw comes before the vehicle draw, and you can write the required transitions explicitly. Godot has to accommodate many combinations of passes and resources. Your DX12 knowledge helps you understand **what its scheduling and synchronization machinery must accomplish**; you still need to learn how Godot accomplishes it.

A particularly relevant example is resource lifetime:

- In your renderer, you learn why a frame’s transform storage cannot be overwritten while the GPU is reading it.
- In Godot, you recognize the same underlying problem when reading code that recycles buffers or delays resource destruction.
- The remaining challenge is following Godot’s allocation, ownership, and frame-tracking structures.

**Compared with learning DX11, DX12 gives you a more direct foundation for this purpose.** DX11 would still teach you textures, shaders, geometry, depth testing, and presentation. But it hides more of the explicit synchronization and resource-management work that you would encounter inside Godot’s DX12 backend.

The transfer is strongest if your goal is to **read, debug, or improve Godot’s backend**. It is more limited if your goal is simply to write Godot gameplay or materials. Learning DX12 also will not, by itself, teach you Godot’s lighting algorithms, shader-generation system, or scene management.

For your latency interests, I would start by understanding your own `PrepareFrame → Render → Present → SignalFrame` sequence, then trace the equivalent submission, waiting, and presentation operations in Godot.

**Given that you also want to understand Godot internals, I would count this as a substantial additional reason to keep DX12—even if DX11 and DX12 ultimately perform similarly for VeeHiiCuuL.**

## User

eventually, i would like to make a highly performant and latency-optimized racing game like this one for android. what graphics api would i use for that, and would it be closer to dx11 or dx12

## Assistant

I’ll check Android’s current graphics guidance and compare its APIs with the DX12 concepts you’re learning.

## Assistant

**For a new Android version focused on performance and latency, I would choose Vulkan. It is much closer to DX12 than DX11.** Google identifies Vulkan as Android’s primary low-level graphics API; OpenGL ES remains an option for older devices or compatibility fallbacks. [Android’s guidance](https://developer.android.com/games/develop/vulkan/native-engine-support).

The broad comparison is:

| Windows API | Closest Android counterpart | Shared approach                                              |
| ----------- | -------------------------- | ------------------------------------------------------------ |
| DX11        | OpenGL ES                  | Driver handles more state tracking and synchronization       |
| DX12        | Vulkan                     | Application explicitly records, submits, and synchronizes work |

These are architectural similarities, not interchangeable APIs.

**Your DX12 knowledge would transfer substantially.** In Vulkan you would again:

- Record drawing commands into command buffers and submit them to queues.
- Create graphics pipelines ahead of use.
- Bind textures and buffers through explicit resource-binding structures.
- Manage GPU memory and retain resources until their work finishes.
- Specify synchronization and image-layout transitions.

You would need to learn Vulkan’s particular rules. For example, its fences and semaphores divide synchronization responsibilities differently from DX12’s fence model. Understanding the underlying CPU/GPU coordination problem makes that much easier. [Vulkan synchronization guide](https://docs.vulkan.org/guide/latest/synchronization.html).

**SimplePaint could probably retain much of its HLSL source.** Vulkan consumes SPIR-V shader binaries, and DXC can compile HLSL to SPIR-V. Bindings, buffer layouts, coordinate conventions, and numerical results would need checking, but you would not automatically need to rewrite the shader in GLSL. [Khronos’s HLSL guide](https://docs.vulkan.org/guide/latest/hlsl.html).

The part that transfers less directly is **presentation and latency tuning**. Android has its own input delivery, display compositor, and presentation scheduling. You would preserve the principle of avoiding old queued frames, but not copy your DXGI wait loop. Android’s documentation specifically discusses how filling the presentation queue can add latency. [Android frame pacing](https://developer.android.com/games/sdk/frame-pacing).

Phones also make sustained power use and heat more important: a renderer that is fast briefly but triggers thermal throttling is a poor outcome for a racing game. Your background-plus-vehicle design gives you a good starting point for keeping work small, although Vulkan alone does not guarantee the lowest latency.

For your project, I would therefore plan around **shared C++ gameplay and asset data, a DX12 Windows backend, and a Vulkan Android backend**, with separate platform input and presentation code.

Vulkan also runs on Windows, so using it for both platforms is another possible architecture if Android becomes a near-term priority. That would be a code-sharing argument for reconsidering the Windows backend—not evidence that Vulkan is faster than DX12.

**Learning DX12 now supports both of your interests: understanding Godot’s DX12 backend and later building an efficient Vulkan renderer for Android.**
