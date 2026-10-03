# Message pumping with high-rate input

Date: 2026-08-30

Repository: `Simple_DirectX12_3D_Game`

## User

> elaborate on this:
> The loop in `Application.cpp` removes only one Windows message and then skips rendering for that iteration. High-rate Raw Input could reduce or starve rendering. Drain all pending messages and buffered input before the next game/render iteration.

## Assistant

The precise issue is that `Render()` is reachable only when `PeekMessageW()` reports that the queue is empty at that exact moment:

```cpp
if (PeekMessageW(..., PM_REMOVE))
{
    // Process exactly one message.
}
else
{
    // Render only if no message was available.
    m_renderer.Render();
}
```

Processing a message does **not** inherently lose a frame. The loop immediately goes around again, so if message handling is faster than messages arrive, it clears the queue and reaches `Render()` shortly afterward. The previous wording was intentionally cautionary, but “one message means one dropped frame” would be inaccurate.

The real failure condition is:

```text
messages arrive at least as quickly as this thread can remove and dispatch them
                              ↓
PeekMessageW keeps returning true
                              ↓
the else branch containing Render() is never reached
```

Even below that failure point, bursts of messages produce uneven rendering. Instead of a regular sequence such as `render, render, render`, the program can do `render, message, message, message, render`. The average render rate may remain high while individual frame intervals become noisier.

At the current 5,500–6,000 presents per second, one render takes roughly 167–182 microseconds. An 8,000 Hz mouse can produce a report every 125 microseconds, so one or two reports can arrive during each render. If their handlers are very short, the thread will catch up. If each report triggers gameplay work, allocation, logging, locking, or other messages, the queue can grow instead. Ordinary `WM_MOUSEMOVE` traffic is often coalesced; high-rate Raw Input is the more relevant case because multiple raw reports can accumulate.

There are two related but distinct improvements.

### 1. Structure the outer loop around one game/render iteration

A conventional baseline is:

```cpp
MSG message{};
bool running = true;

while (running)
{
    while (PeekMessageW(&message, nullptr, 0, 0, PM_REMOVE))
    {
        if (message.message == WM_QUIT)
        {
            running = false;
            break;
        }

        TranslateMessage(&message);
        DispatchMessageW(&message);
    }

    if (!running)
    {
        break;
    }

    ApplyPendingResize();

    if (!m_minimized && !m_inSizeMove)
    {
        // Finalize input state, update simulation as needed, then render.
        m_renderer.Render();
    }
    else
    {
        WaitMessage();
    }
}
```

This does not magically make message handling cheaper, but it creates a clear boundary: collect pending OS events, reduce them into current application state, then perform one game/render iteration. It also calls `ApplyPendingResize()` once after the batch. If several `WM_SIZE` messages arrived, only the most recent pending dimensions need to be applied.

An unbounded inner `while (PeekMessageW(...))` can still starve rendering if messages truly arrive continuously. For an ordinary application that is rare because dispatch is normally much faster than message arrival. For a deliberate multi-kilohertz design, it is reasonable to enforce fairness: place a small count or time budget on each message batch, then let rendering proceed and drain the remainder on the next iteration. Window lifecycle messages must remain prompt, and the message handlers themselves should do minimal work.

### 2. Batch Raw Input instead of treating each report as a game update

Draining the Windows message queue and draining Raw Input are not quite the same thing. `WM_INPUT` tells the application that raw records are available. With a high-frequency device, several records may accumulate, and `GetRawInputBuffer()` can retrieve them in a batch instead of paying the complete per-message/per-call path for every report.

The input layer should reduce that batch into a compact state for the next simulation/render operation:

- Accumulate relative mouse-motion deltas.
- Keep the newest absolute state where appropriate.
- Preserve every button/key transition and its timestamp; those cannot simply be coalesced away.
- Avoid running physics, camera logic, allocation, logging, or renderer work from `WM_INPUT`.
- Publish a stable input snapshot at the game-loop boundary.

At 8,000 Hz input and 5,000 FPS, the expected average is 1.6 mouse reports per rendered frame. That is manageable if reports are accumulated cheaply. It is wasteful and behaviorally dangerous if every report independently advances gameplay.

### Why the restructured loop helps

It gives the program three explicit phases:

```text
OS messages and raw device records
              ↓
compact input/window-state snapshot
              ↓
fixed-step simulation as required → render latest state → present
```

That makes render frequency independent of how Windows happens to packetize input messages. It also makes input latency easier to measure because there is a defined “input snapshot finalized” point immediately before simulation/render work.

For this project today, there is no high-rate Raw Input implementation, so the current loop is not presently being starved by it. This is a design issue to address when mouse/game input is added. My practical recommendation would be:

1. Restructure the loop with an inner message drain and move `ApplyPendingResize()` after it.
2. Keep every window procedure handler short and state-oriented.
3. Add buffered Raw Input whose handler only accumulates records.
4. If profiling with an 8,000 Hz mouse shows the queue can remain continuously nonempty, impose a small message-processing budget per render iteration rather than waiting indefinitely for a perfectly empty queue.
5. Consider a separate input/message thread only if measurements justify it; cross-thread synchronization can itself be significant in a 200-microsecond frame.

So the short version is: the existing loop is correct for light message traffic, and skipping one outer-loop pass is nearly free. Its weakness is that **queue emptiness is an absolute prerequisite for rendering**. At multi-kilohertz input rates, rendering should instead receive fair scheduling while input reports are processed cheaply in batches and reduced to one coherent state snapshot.
