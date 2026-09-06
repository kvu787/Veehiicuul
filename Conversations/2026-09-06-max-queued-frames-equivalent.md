# Unity maxQueuedFrames equivalent

## User

<recommended_plugins>
Here is a list of plugins that are available but not installed.

- Airtable (airtable@openai-curated-remote)
- Alpaca (alpaca@openai-curated-remote)
- Apollo.io (apollo@openai-curated-remote)
- Spotify (app-68de829bf7648191acd70a907364c67c@openai-curated-remote)
- AllTrails (app-68f1afc5a6008191a701eaaab428816c@openai-curated-remote)
- Instacart (app-691eab1e001081919e57189f8b2f03bc@openai-curated-remote)
- Apple Music (app-6938a94a61d881918ef32cb999ff937c@openai-curated-remote)
- LONA Trading Assistant (app-694336b0c0948191a4ad234f9942885b@openai-curated-remote)
- SciSpace (app-69439d715a7c8191aed9e2f6649e105f@openai-curated-remote)
- Tarot (app-6943a2c078b0819188de39e4fe168d9b@openai-curated-remote)
- Todoist: To Do List & Calendar (app-6943b73823548191a9f9216c6790c453@openai-curated-remote)
- Consensus (app-6943e6f4a928819195962de16fb9ffe4@openai-curated-remote)
- Sider Scholar (app-6948b485f5bc8191adb4df13f369cec7@openai-curated-remote)
- True Sky (app-69490a4a06148191a0dd78606a3dbf1f@openai-curated-remote)
- Bigdata.com (app-69491eceef3c8191beb70788b7840429@openai-curated-remote)
- Gamma (app-698a098735908191989f5788d7ee317e@openai-curated-remote)
- Tredict (app-69aef5b699a0819184512d57743fc1cd@openai-curated-remote)
- Maersk (app-69b2b5a768d4819190d3a86c5f12e6d9@openai-curated-remote)
- Dropbox (app-69b31dc2110c8191b8b47dc98fe5a052@openai-curated-remote)
- Parqet (app-69b68652f0308191a27d7c7096cab4f6@openai-curated-remote)
- Interactive Brokers (IBKR) (app-69bc11db874881918718abaca20b68ce@openai-curated-remote)
- Financial Datasets (app-69cacd9394a88191ba6564e1bb0430fa@openai-curated-remote)
- Fathom (app-69d88b99c5c481918e8da9225737e1e9@openai-curated-remote)
- vidIQ (app-69dd11f3e50c8191b1ca48d03cf7e2ad@openai-curated-remote)
- TickTick:To-Do List & Calendar (app-69ddbaba3fb48191a825f22c21b0599d@openai-curated-remote)
- Plaud (app-69f3c30d68288191bbd428a394a78407@openai-curated-remote)
- Wolfram (app-69fe0bf66c8481919c513d799406436e@openai-curated-remote)
- Runway (app-6a05e3b201788191be12b590b43e6ce3@openai-curated-remote)
- Caliber (app-6a05e8f22d408191b13ba3897157f6df@openai-curated-remote)
- COROS (app-6a0694cbb2608191bbefb74ba810ab68@openai-curated-remote)
- TradingCursor (app-6a0d835ff1dc8191972eeabd14967446@openai-curated-remote)
- CoinMarketCap (app-6a172fe86f5481919f73cbc3bc3ad5bb@openai-curated-remote)
- Trello (app-6a20b18a639081918c1b438f8381b27e@openai-curated-remote)
- Longbridge (app-6a2baf2fad748191812393c3e00308ef@openai-curated-remote)
- freddy (app-6a322b52a82c8191b7fb653f9e9f7891@openai-curated-remote)
- Higgsfield (app-6a3293e129088191abf0875820e839da@openai-curated-remote)
- Stocktwits (app-6a427a19b1f481919c5db13838af00c2@openai-curated-remote)
- Webull (app-6a4b8801d1e8819182f624f012878a81@openai-curated-remote)
- CoinGecko (app-6a4f02d735388191959c8328877e0bbd@openai-curated-remote)
- Asana (asana@openai-curated-remote)
- Atlassian Rovo (atlassian-rovo@openai-curated-remote)
- Base44 (base44@openai-curated-remote)
- Binance (binance@openai-curated-remote)
- Box (box@openai-curated-remote)
- Canva (canva@openai-curated-remote)
- ClickUp (clickup@openai-curated-remote)
- Cloudflare (cloudflare@openai-curated-remote)
- Codex Security (codex-security@openai-curated-remote)
- Figma (figma@openai-curated-remote)
- Finances (finances@openai-curated-remote)
</recommended_plugins>
# AGENTS.md instructions for C:\Users\k\Repository\CPlusPlus\Simple_DirectX12_3D_Game

<INSTRUCTIONS>
# Base template

## External tools

You may use the tools in `%UserProfile%\Program`.
You may refer to local copies of source repos in `%UserProfile%\Repository\External`.

## Godot

If you create a Godot project, include a "Run.cmd" file that builds and launches the standalone exe of the Godot project by double-clicking the Run.cmd from File Explorer.

## Git

When implementing stuff, avoid difficult-to-review "mega-commits".
Split large work into multiple commits to make it easier to review.
Separate commits that record conversations from other commits.

## Markdown tables

Tables in Markdown must be padded and aligned in a way to make them easy to read in a plaintext editor, not only in a Markdown viewer.

## Mathematical notation in Markdown

Any mathematical notation in Markdown files (LaTeX, KaTeX, MathJax, etc) must display properly in VSCode's Markdown previewer, GitHub.com's Markdown displayer, and the markdown viewer in the Windows 11 ChatGPT app.

# Base template additions

## Conversations

Record verbatim and commit all conversations in a folder named `Conversations` located at the root of this Git repo.
Use one file per conversation.
Prefix these commits with `[cnv]`.
If I attach images to prompts, save and record these in the conversation logs.

## Compatibility

Do not attempt to maintain any sort of application compatibility between different commits of the repo. This creates unwanted complexity.

# Repository-specific

Implement a Run.cmd file that launches the project when double-clicking the Run.cmd from File Explorer.

Refer to `%UserProfile%\Repository\Godot\SimplePaintShaders` for the original development of SimplePaint shader.

</INSTRUCTIONS>
<environment_context>
  <cwd>C:\Users\k\Repository\CPlusPlus\Simple_DirectX12_3D_Game</cwd>
  <shell>powershell</shell>
  <current_date>2026-09-06</current_date>
  <timezone>America/Los_Angeles</timezone>
  <filesystem><workspace_roots><root>C:\Users\k\Repository\CPlusPlus\Simple_DirectX12_3D_Game</root></workspace_roots><permission_profile type="managed"><file_system type="restricted"><entry access="read"><special>:root</special></entry></file_system></permission_profile></filesystem>
</environment_context>

## User

i've learned from unity engine experiments that unity's maxqueuedframes setting has a major impact on the max fps that can be reached on a minimal bare-bones unity scene. mqf=1 can get roughly 1000 fps while mqf=2 can get roughly 2500 fps.

what is the equivalent of mqf for this application?

## Assistant

I’ll inspect how this app presents frames and limits work in flight, then compare that with Unity’s `maxQueuedFrames` setting.

## Assistant

The renderer uses two frame slots and waits for a GPU fence before reusing each slot. It has no separate queue-depth setting. I’m checking the distinction between that limit and DXGI’s presentation-queue limit, and recording the conversation as required by `AGENTS.md`.

## Assistant final response

The closest control already in this app is **`FrameCount = 2`, combined with its GPU fence waits**. There is no separate `maxQueuedFrames` setting.

In [Renderer.h:33](C:/Users/k/Repository/CPlusPlus/Simple_DirectX12_3D_Game/Source/Renderer.h:33):

```cpp
static constexpr std::uint32_t FrameCount = 2;
```

This controls both the swap-chain buffer count and the number of per-frame resource slots. [Render()](C:/Users/k/Repository/CPlusPlus/Simple_DirectX12_3D_Game/Source/Renderer.cpp:1354) waits for the selected slot’s previous GPU work to finish before reusing it. With two slots, the CPU can prepare the next frame while the GPU processes the previous one; on returning to the first slot, it must wait if that slot is still busy.

**That gives this renderer capacity for two frames in flight, including the frame executing on the GPU.** It does not establish an exact numerical equivalence with Unity’s `mqf=2`, because rendering-resource reuse and presentation queuing are distinct limits.

DXGI’s explicit queue-depth API is **`IDXGISwapChain2::SetMaximumFrameLatency(n)`**. This app currently does not use it. It requires creating the swap chain with `DXGI_SWAP_CHAIN_FLAG_FRAME_LATENCY_WAITABLE_OBJECT` and using its waitable object to pace rendering. [Microsoft: queue limit](https://learn.microsoft.com/en-us/windows/win32/api/dxgi1_3/nf-dxgi1_3-idxgiswapchain2-setmaximumframelatency), [frame waiting](https://learn.microsoft.com/en-us/windows/win32/api/dxgi1_3/nf-dxgi1_3-idxgiswapchain2-getframelatencywaitableobject).

For a **one-versus-two frames-in-flight experiment**, I would add a separate `MaxFramesInFlight` setting enforced by fences, while keeping at least two swap-chain buffers. Simply changing `FrameCount` to `1` would make this flip-model swap chain invalid. [Microsoft: buffer requirements](https://learn.microsoft.com/en-us/windows/win32/api/dxgi1_2/ns-dxgi1_2-dxgi_swap_chain_desc1).

Your Unity result is consistent with a tighter queue reducing CPU/GPU overlap and increasing time spent waiting; Unity documents that lowering MQF can reduce FPS. Whether this app shows a similar jump needs measurement. [Unity documentation](https://docs.unity3d.com/ScriptReference/QualitySettings-maxQueuedFrames.html).


## User

what is all the queuing stuff that can be configured by a low-level c++/dx12 app like this one? i would like to have a configuration that does zero or minimum queuing to minimize input latency, and then be able to adjust queuing settings to balance input latency vs other factors.

## Assistant

I’ll map the queueing controls across the app, D3D12, and DXGI, then identify what a minimum-latency configuration would require in this renderer.

## Assistant question

Would you like me to implement configurable queueing controls in this app, or explain the controls and recommend a design first?

- Implement configurable controls
- Explain and recommend first

## User

<send_user_message_question_reply>
[{"questionItemId":"[\"request_user_input_async\",\"call_xkOKmJSJhAtUL8efeqegyAfN\",0]","question":"Would you like me to implement configurable queueing controls in this app, or explain the controls and recommend a design first?","answer":"Explain and recommend first"}]
</send_user_message_question_reply>

## Assistant

The key distinction is between minimizing queued work and minimizing measured input latency. Waiting for every GPU frame to finish gives a clear minimum-queue baseline, but it also removes CPU/GPU overlap. I recommend keeping the GPU-work limit, presentation limit, buffer count, and frame pacing independent, with input sampled after the waits.

## Assistant

I have a proposed minimum-queue baseline and a sequence of settings to vary. The current app would also need to refresh input after its fence wait; otherwise, input arriving during that wait cannot affect the frame about to render. I’m finishing the explanation and recording this follow-up in the same conversation log.

## Assistant final response

**I recommend separate controls for GPU work in flight, DXGI presentation queuing, buffer count, and frame pacing.** Input sampling and CPU scheduling also matter: a small GPU queue cannot compensate for rendering an old input state.

“Zero queuing” should mean **no intentional backlog of old frames from this app**. It cannot eliminate GPU execution time, Windows scheduling, display scanout, or the monitor’s own processing. Also, the configuration with the smallest queue is not necessarily the one with the lowest measured input latency: strict serialization can substantially reduce how often you sample input and produce frames.

The main controls are:

| Layer                         | What the app controls                                        | Latency versus other factors                                       |
| ----------------------------- | ------------------------------------------------------------ | ------------------------------------------------------------------ |
| Input processing              | When events are consumed and current input is sampled        | Later sampling gives fresher input; preserve clicks and key edges. |
| CPU simulation/render queue   | How many prepared frames wait for the render thread          | Zero prepared frames avoids extra CPU-side frame delay.            |
| GPU frames in flight          | Submitted but unfinished frames, bounded with CPU fences     | One is the strict baseline; two or more allow more overlap.        |
| DXGI presentation queue       | Maximum frame latency and the swap-chain waitable object     | A shallow queue limits presentation backlog.                       |
| Swap-chain/resource buffering | Back buffers, command allocators, and dynamic resource slots | More capacity can prevent stalls, but needs explicit queue limits. |
| Presentation timing           | VSync, tearing permission, and flip-model presentation       | Trades immediate updates against tearing and refresh alignment.    |
| Frame pacing                  | FPS cap and when the CPU starts preparing the next frame     | Can avoid saturation, improve consistency, and reduce power.       |
| Submission scheduling         | Command-list batching, other GPU queues, and their priority  | Trades submission overhead and GPU utilization against delay.      |

**The two queue limits need precise definitions.**

For a proposed `MaxGpuFramesInFlight = 1`, I would wait until the previous frame’s GPU fence completes **before sampling input and preparing the next frame**. The count includes the executing frame. This creates no intentional queue of additional frames behind it, but sacrifices inter-frame CPU/GPU overlap.

At `2`, the CPU can prepare the next frame while the previous one executes. There is no D3D12 “queue length” setter for this: the application enforces the policy. Crucially, `ID3D12CommandQueue::Wait` is a **GPU-side** wait that returns immediately; throttling CPU submission requires a CPU-side fence wait. [Microsoft’s synchronization documentation](https://learn.microsoft.com/en-us/windows/win32/api/d3d12/nf-d3d12-id3d12commandqueue-wait).

Separately, `IDXGISwapChain2::SetMaximumFrameLatency(1)` limits swap-chain queuing. Use it with `DXGI_SWAP_CHAIN_FLAG_FRAME_LATENCY_WAITABLE_OBJECT` and wait on that object before beginning each frame. This controls a different boundary from GPU resource completion. **A completed GPU fence does not prove that the image has reached the screen**, and the two limits should not be added together to calculate latency. [DXGI queue-limit API](https://learn.microsoft.com/en-us/windows/win32/api/dxgi1_3/nf-dxgi1_3-idxgiswapchain2-setmaximumframelatency).

**Buffer count should be independent of both limits.** Two swap-chain buffers are the minimum for flip-model presentation. Three buffers can provide useful capacity without requiring three queued frames if the other limits remain tight. Command allocators and changing constant-buffer data also need sufficient storage for the permitted GPU work. [Microsoft’s buffer requirements](https://learn.microsoft.com/en-us/windows/win32/api/dxgi1_2/ns-dxgi1_2-dxgi_swap_chain_desc1).

For this app, my proposed **strict minimum-queue baseline** would be:

```ini
MaxGpuFramesInFlight = 1
MaxPresentLatency = 1
WaitForPresentation = true
BackBufferCount = 2

VSync = false
AllowTearing = true
FrameRateLimit = 0

WaitStrategy = event
```

These are proposed settings, not currently implemented options. `FrameRateLimit = 0` means uncapped; tearing must be supported by the system. Keep the current single render thread and normal-priority graphics queue.

The frame loop’s ordering is essential:

```text
Wait for frame-pacing deadline, presentation permission,
and GPU/resource availability
    ↓
Consume pending input and sample the latest state
    ↓
Simulate → record commands → submit → Present
    ↓
Record the frame's completion fence
```

Keep the window responsive during waits and refresh input afterward. Sleeping after sampling input makes that input older before it reaches the GPU. Microsoft explicitly recommends waiting first, then processing input and rendering current data. [DXGI low-latency loop](https://learn.microsoft.com/en-us/windows/uwp/gaming/reduce-latency-with-dxgi-1-3-swap-chains).

I would expose these adjustments for experiments:

1. **GPU limit: 1, 2, 3.** Measure how much overlap improves FPS and frame-time consistency.
2. **Presentation limit: 1, 2, 3.** Test alongside the GPU limit; the tighter constraint may hide changes to the other.
3. **Back buffers: 2 or 3.** Check whether extra capacity removes stalls while preserving shallow queues.
4. **Presentation pacing: waitable versus ordinary uncapped presentation.** Keep the GPU fence limit in both. Treat the strict baseline as a reference, not a guaranteed latency winner on every display path.
5. **FPS cap and wait strategy.** Compare uncapped with a sustainable cap. Start with event waits, then optionally test a bounded spin or hybrid wait to trade CPU usage for reduced wake-up delay.

For presentation, `Present(0, ALLOW_TEARING)` is the appropriate supported windowed/borderless path for immediate updates. `Present(1, 0)` synchronizes to refresh. With compatible VRR hardware, test a cap with headroom below the display’s refresh ceiling; the app’s tearing permission also enables the DXGI VRR path. [Microsoft’s VSync-off and VRR guidance](https://learn.microsoft.com/en-us/windows/win32/direct3ddxgi/variable-refresh-rate-displays).

A few additional mechanisms are relevant, but are not interchangeable queue-depth settings:

- **Dropping presentations:** sync interval zero can discard superseded pending presentations. It does not undo already-submitted rendering work. `DXGI_PRESENT_DO_NOT_WAIT` returns a busy error instead of blocking; it does not eliminate queues. [Present behavior](https://learn.microsoft.com/en-us/windows/win32/api/dxgi/nf-dxgi-idxgiswapchain-present), [presentation flags](https://learn.microsoft.com/en-us/windows/win32/direct3ddxgi/dxgi-present).
- **Command-list batching:** the app decides when recorded work is submitted. Batching across frames can add delay; splitting a tiny frame into many submissions adds overhead. Keep this scene's compact per-frame submission as the baseline.
- **Multiple GPU queues and priority:** graphics, compute, and copy work can overlap or block on dependencies. Higher priority changes scheduling competition, not your own frame backlog. I would keep one normal-priority graphics queue for this scene. [D3D12 queue scheduling](https://learn.microsoft.com/en-us/windows/win32/direct3d12/executing-and-synchronizing-command-lists).
- **Composition:** flip-model borderless presentation can reach Independent Flip and bypass desktop composition, but the app cannot guarantee that mode under every desktop condition. Measure the actual presentation path. [Microsoft’s flip-model guidance](https://learn.microsoft.com/en-us/windows/win32/direct3ddxgi/for-best-performance--use-dxgi-flip-model).
- **Vendor latency scheduling:** NVIDIA Reflex and AMD Anti-Lag 2 can schedule CPU work closer to when the GPU needs it, reducing backlog without always imposing full serialization. These are optional integrations after establishing the generic baseline. [NVIDIA Reflex](https://developer.nvidia.com/performance-rendering-tools/reflex), [AMD Anti-Lag 2](https://gpuopen.com/anti-lag-2/).

In the current code, [`FrameCount`](C:/Users/k/Repository/CPlusPlus/Simple_DirectX12_3D_Game/Source/Renderer.h:33) couples buffers and resource slots. [The message loop](C:/Users/k/Repository/CPlusPlus/Simple_DirectX12_3D_Game/Source/Application.cpp:36) processes input before [the renderer’s fence wait](C:/Users/k/Repository/CPlusPlus/Simple_DirectX12_3D_Game/Source/Renderer.cpp:1354). I would separate those counts and move the frame-admission waits before the final input sample.

For choosing a default, record **input-to-visible-response latency, frame-time variation, FPS, and CPU/GPU usage**, plus time spent in each wait. PresentMon can help analyze CPU, GPU, and display timing; a camera or latency sensor is needed to validate the full physical input-to-photon path. A higher FPS counter alone cannot select the best configuration. [PresentMon](https://github.com/GameTechDev/PresentMon).

## User

before continuing, record this somewhere for devs and agents:
- this repo must not implement vendor specific stuff such as NVIDIA Reflex and AMD Anti-Lag 2
- preconditions to running this repo are windows 10/11 and x86_64
- however, there are no preconditions for gpu, and the repo must not write any code that is specific to gpu vendor

## Assistant

I’ll add these rules to `AGENTS.md` and the developer documentation, then commit the documentation and conversation separately.

## Assistant

The policy is now recorded for developers and agents. I also replaced the old GPU requirement in the setup guide with the new policy and documented the renderer’s existing Shader Model 6.0 fallback gap.

## Assistant final response

Recorded in [AGENTS.md](C:/Users/k/Repository/CPlusPlus/Simple_DirectX12_3D_Game/AGENTS.md:45) and the [developer documentation](C:/Users/k/Repository/CPlusPlus/Simple_DirectX12_3D_Game/Documentation/README.md:29):

- Windows 10/11 and x86_64 are required.
- No GPU preconditions.
- No GPU-vendor-specific code or integrations, including NVIDIA Reflex and AMD Anti-Lag 2.

Also documented the existing Shader Model 6.0 fallback gap. Documentation and conversation are committed separately; working tree is clean.

## User

for this:
MaxGpuFramesInFlight = 1
MaxPresentLatency = 1
WaitForPresentation = true
BackBufferCount = 2

VSync = false
AllowTearing = true
FrameRateLimit = 0

WaitStrategy = event

is the implementation of a FrameRateLimit setting necessary to implement the other settings

## Assistant

The queue controls can work without an FPS limiter. I’ll record that distinction in the conversation log.

## Assistant final response

**No. `FrameRateLimit` is independent and can be omitted entirely.** In the proposed configuration, `FrameRateLimit = 0` means no explicit FPS cap, so it requires no timer or frame-rate-limiting implementation.

The other settings can operate using GPU fences, the DXGI presentation waitable object, and buffer management. Those waits naturally constrain throughput without targeting a particular FPS.

`WaitStrategy = event` refers to waiting for GPU/presentation readiness; it does not require a frame-rate timer.

For the initial implementation, the loop can simply be:

```text
Wait for GPU/presentation/resource readiness
→ sample input → simulate → render → present
```

An FPS limiter can be added independently later.

## User

provide a detailed explanation for each of these:
MaxGpuFramesInFlight = 1
MaxPresentLatency = 1
WaitForPresentation = true
BackBufferCount = 2
VSync = false
AllowTearing = true
WaitStrategy = event

## Assistant

I’ll explain what each setting controls, where it makes the CPU wait, and how the settings interact. These are still proposed controls, rather than options the app currently implements.

## Assistant

Two distinctions matter here: GPU completion does not mean an image has reached the screen, and `WaitForPresentation` does not mean VSync. The first limits unfinished rendering work; the second waits for DXGI to admit another frame, even when VSync is off.

## Assistant final response

These are **proposed application settings**, with the meanings described below. Some map directly to DXGI APIs; others define policies that we would implement ourselves. All can use vendor-neutral Windows/D3D12/DXGI interfaces.

**`MaxGpuFramesInFlight = 1` limits how far the CPU gets ahead of GPU completion.**

A frame is “in flight” after its commands have been submitted and before its completion fence has finished. The count includes a frame currently executing on the GPU.

For the strict policy we discussed, a value of `1` means:

1. Submit frame A and arrange a completion fence.
2. Before sampling input and preparing frame B, wait for A’s fence.
3. Sample fresh input, prepare B, and submit it.

This prevents the app from accumulating additional frames behind its own unfinished frame. It does not make the entire GPU idle: Windows or another application can still have work scheduled.

The placement of the wait is part of the policy. Merely limiting submission while allowing the CPU to prepare B early would permit B’s input state to become stale while waiting.

With `2`, preparation of B can overlap GPU execution of A. That can substantially improve throughput. For example, with hypothetical CPU preparation of 0.2 ms and GPU execution of 0.3 ms, serialization takes approximately 0.5 ms per frame; ideal overlap approaches 0.3 ms. Presentation and synchronization overhead add to both.

Implementation uses a queue-signaled D3D12 fence and a **CPU-side** wait. `ID3D12CommandQueue::Wait` alone does not implement this limit: it queues a GPU-side wait and returns immediately. [Microsoft: CPU versus GPU fence waits](https://learn.microsoft.com/en-us/windows/win32/api/d3d12/nf-d3d12-id3d12commandqueue-wait).

**`MaxPresentLatency = 1` limits queuing at the swap chain.**

This maps to:

```cpp
swapChain->SetMaximumFrameLatency(1);
```

It sets DXGI’s maximum number of queued back-buffer frames for that swap chain. The swap chain must have been created with `DXGI_SWAP_CHAIN_FLAG_FRAME_LATENCY_WAITABLE_OBJECT`. [Microsoft: SetMaximumFrameLatency](https://learn.microsoft.com/en-us/windows/win32/api/dxgi1_3/nf-dxgi1_3-idxgiswapchain2-setmaximumframelatency).

This is a different boundary from the GPU fence:

- The GPU fence answers whether the relevant submitted GPU work has completed.
- The presentation mechanism governs admitting and presenting swap-chain frames.

A presentation can be queued while its rendering is still unfinished. Therefore, this is not simply a count of fully rendered images waiting on a shelf.

The value `1` is a queue limit measured in frames. It does **not** mean 1 ms of latency or guarantee exactly one monitor refresh of input latency. Likewise, a completed GPU fence does not prove that the monitor has displayed the image.

Increasing this limit can give the presentation pipeline more room, potentially reducing stalls at the cost of allowing more backlog. Another constraint—such as the GPU frame limit—may prevent that extra capacity from being used.

**`WaitForPresentation = true` makes the CPU wait for permission to begin another frame.**

The app obtains DXGI’s waitable handle:

```cpp
HANDLE ready = swapChain->GetFrameLatencyWaitableObject();
```

Before each frame, including the first, the app waits for that handle. Operationally, we use it as permission from DXGI to begin the next frame under the configured presentation limit. [Microsoft: presentation waitable object](https://learn.microsoft.com/en-us/windows/win32/api/dxgi1_3/nf-dxgi1_3-idxgiswapchain2-getframelatencywaitableobject).

The benefit comes from **waiting before sampling input and doing rendering work**. Otherwise, the app can prepare an image using older input and subsequently stall inside `Present`. Waiting earlier lets it prepare the image closer to when the presentation pipeline can accept it. [Microsoft’s low-latency rendering loop](https://learn.microsoft.com/en-us/windows/uwp/gaming/reduce-latency-with-dxgi-1-3-swap-chains).

`MaxPresentLatency` and `WaitForPresentation` work together: one selects the queue limit, and the other selects proactive waiting at frame start. They do not describe two additional queues.

**This is compatible with VSync being off.** Waiting for presentation capacity does not request synchronization to vertical blank.

For the ordinary, non-waitable presentation mode discussed earlier, this wait would be absent and `MaxPresentLatency` would be inactive. GPU/resource fence waits would still apply, and `Present` could still block. Switching between waitable and non-waitable swap chains requires recreating the swap chain; that creation flag cannot be changed through `ResizeBuffers`. [Microsoft’s swap-chain creation guidance](https://learn.microsoft.com/en-us/windows/uwp/gaming/reduce-latency-with-dxgi-1-3-swap-chains).

**`BackBufferCount = 2` provides two presentable image buffers.**

This maps to `DXGI_SWAP_CHAIN_DESC1::BufferCount`. These are the image resources into which the renderer draws and which DXGI manages for presentation. Two is the minimum for the flip-model swap chain this app uses. [Microsoft: swap-chain buffer requirements](https://learn.microsoft.com/en-us/windows/win32/api/dxgi1_2/ns-dxgi1_2-dxgi_swap_chain_desc1).

Buffer count describes **storage capacity**, not how many frames the CPU must queue.

Consequently, these settings are compatible:

```ini
BackBufferCount = 2
MaxGpuFramesInFlight = 1
MaxPresentLatency = 1
```

The renderer has two images to rotate between, while the other settings restrict outstanding work. It still must respect GPU resource ownership and DXGI’s buffer-availability rules.

Increasing the count to `3` uses more memory and may reduce buffer-availability stalls. It does not automatically add a frame of latency if the queue limits remain tight, nor does it guarantee higher FPS.

The app currently uses one `FrameCount` constant for both swap-chain buffers and per-frame resource slots. We would separate those concepts. Command allocators and changing constant-buffer data need enough independent storage for the permitted GPU work, but their count need not be identical to the swap-chain buffer count.

**`VSync = false` requests presentation without a vertical-blank synchronization interval.**

In this app’s presentation call, this means a sync interval of zero:

```cpp
swapChain->Present(0, presentFlags);
```

With `VSync = true`, the usual equivalent is `Present(1, 0)`, requesting synchronization for at least one vertical blank. Sync interval zero also allows superseded flip-model presentations to be discarded instead of displaying every submitted frame. It does not cancel GPU rendering commands already submitted. [Microsoft: Present semantics](https://learn.microsoft.com/en-us/windows/win32/api/dxgi/nf-dxgi-idxgiswapchain-present).

Turning VSync off removes that requested refresh synchronization. It does **not** remove:

- GPU completion waits;
- presentation-admission waits;
- buffer-availability stalls;
- Windows scheduling and composition effects.

Thus, `VSync = false` neither guarantees an immediately returning `Present` call nor guarantees a particular FPS.

It also does not make a 144 Hz monitor scan out 2,500 complete images per second. Rendering and presentation submissions can occur more frequently than complete display refreshes.

**`AllowTearing = true` permits presenting updates without preserving a complete refresh of one image.**

When supported, the app enables `DXGI_SWAP_CHAIN_FLAG_ALLOW_TEARING` at swap-chain creation and passes `DXGI_PRESENT_ALLOW_TEARING` when presenting with sync interval zero.

For this app’s windowed/borderless mode, the intended call is:

```cpp
swapChain->Present(0, DXGI_PRESENT_ALLOW_TEARING);
```

The presentation flag is valid only with sync interval zero and the appropriate swap-chain configuration. It is not used with VSync enabled. [Microsoft: tearing flag requirements](https://learn.microsoft.com/en-us/windows/win32/direct3ddxgi/dxgi-present).

If an update becomes visible during scanout, different parts of the screen can come from different frames, producing a tear line. Allowing this can expose newer input-dependent pixels sooner, at the cost of image continuity.

“Allow” means permission; it does not force visible tearing. The same DXGI permission is used for variable-refresh-rate presentation, which can provide tear-free updates under suitable display conditions. [Microsoft: VSync-off and VRR support](https://learn.microsoft.com/en-us/windows/win32/direct3ddxgi/variable-refresh-rate-displays).

Under your repository policy, unsupported tearing must not become a GPU prerequisite. We would query support through DXGI and use a vendor-neutral fallback, reporting that the effective tearing setting is off.

**`WaitStrategy = event` determines how the CPU waits when a required condition is not ready.**

For a GPU fence, the app first checks whether the target fence value has completed. If it has, execution continues immediately. Otherwise, it registers an event using `SetEventOnCompletion` and waits for that event. The presentation wait uses the handle supplied by DXGI.

An event wait suspends the waiting thread until the condition is signaled, rather than continuously polling on a CPU core. If the handle is already signaled, the wait can return immediately. This is not an instruction to sleep for a fixed interval such as 1 ms. [Microsoft: wait behavior](https://learn.microsoft.com/en-us/windows/win32/api/synchapi/nf-synchapi-waitforsingleobject).

Its tradeoff is CPU efficiency versus wake-up overhead. Once signaled, the thread still needs to be scheduled to run. At very high frame rates, that overhead can be a meaningful fraction of a frame. A spin or hybrid strategy could reduce some wake-up delay while consuming more CPU time; it would not change the configured queue limits.

Because this app renders on its window thread, the implementation should use message-aware waiting, such as `MsgWaitForMultipleObjectsEx`, so it can respond to window messages while waiting. After handling messages, it must still satisfy the rendering readiness conditions. [Microsoft: message-aware waits](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-msgwaitformultipleobjectsex).

With all seven settings together, the intended sequence is:

```text
Wait for DXGI to admit another frame
Wait for the previous GPU frame to complete
Ensure the selected resources can be reused
    ↓
Consume pending input and sample the latest state
    ↓
Simulate and record commands
    ↓
Submit rendering, present with sync interval 0
and tearing enabled if supported, and signal a GPU fence
    ↓
Repeat
```

Both readiness conditions must be satisfied; if either is already satisfied, it adds no deliberate delay. There is no FPS timer in this loop. Its throughput is determined by rendering, presentation, resource availability, and synchronization overhead.

This provides a strict baseline for minimizing intentional frame backlog. Measuring input-to-visible-response latency would establish whether allowing some CPU/GPU overlap produces a better result on a particular system.

## User

record somewhere for agents/devs that this app must not implement any frame rate limiting of its own.

## Assistant

I’ll add that rule to `AGENTS.md` and the developer documentation, then commit it separately from the conversation log.

## Assistant final response

Recorded in [AGENTS.md](C:/Users/k/Repository/CPlusPlus/Simple_DirectX12_3D_Game/AGENTS.md:59) and [README.md](C:/Users/k/Repository/CPlusPlus/Simple_DirectX12_3D_Game/README.md:53): **no app-owned FPS limiting**, including optional limiters, background caps, or `FrameRateLimit` settings.

GPU/DXGI synchronization waits and VSync remain allowed.

Policy and conversation committed separately.

## User

i want the following:
- A "pipeline mode" setting that supports these values "MinimizeInputLatency", "Standard", "Custom"
- MinimizeInputLatency configures things to minimize input latency above all else
- Standard configures things to match a typical game in which input latency is balanced with other factors
- Custom configures things as specified by a set of custom settings in the INI. those custom settings are ignored if pipeline mode isn't custom

how would you implement this?

## Assistant

I’ll check how the INI is loaded and how the frame loop is structured, then propose the mode definitions, custom settings, and implementation changes.

## Assistant

I’d resolve the selected mode into one set of settings before creating the renderer. Presets would completely ignore the custom values, and the frame loop would use only the resolved settings. For `MinimizeInputLatency`, I’d start with shallow queues and active waiting, then validate response latency: the smallest queue is not always the fastest response.

## Assistant final response

I would implement this as **two built-in presets plus one explicit custom configuration**, all resolved into the same renderer settings before initialization.

The renderer would not contain scattered checks for `MinimizeInputLatency` or `Standard`. It would receive one validated configuration and implement those settings consistently.

**The INI would have a mode selector and a separate custom section.**

```ini
[Pipeline]
Mode = Standard

; Used only when Mode = Custom.
[Pipeline.Custom]
MaxGpuFramesInFlight = 2
MaxPresentLatency = 2
WaitForPresentation = true
BackBufferCount = 3
VSync = true
AllowTearing = false
WaitStrategy = Event
```

`Mode` would accept exactly your three names: `MinimizeInputLatency`, `Standard`, and `Custom`. I would ship `Standard` as the default selection.

There would be **no frame-rate-limit setting or app-owned FPS limiter**, in any mode.

**My initial preset definitions would be:**

| Setting              | MinimizeInputLatency | Standard |
| -------------------- | -------------------- | -------- |
| MaxGpuFramesInFlight | 1                    | 2        |
| MaxPresentLatency    | 1                    | 2        |
| WaitForPresentation  | true                 | true     |
| BackBufferCount      | 2                    | 3        |
| VSync                | false                | true     |
| AllowTearing         | true                 | false    |
| WaitStrategy         | Spin                 | Event    |

For **MinimizeInputLatency**, the intent is fresh input, minimal intentional backlog, and immediate presentation where supported. `Spin` means actively checking synchronization readiness, accepting higher CPU usage to avoid parking and waking the render thread. It does not wait for an FPS deadline.

For **Standard**, two frames allow CPU/GPU overlap, three image buffers provide presentation capacity, VSync favors tear-free output, and event waits avoid consuming a CPU core while blocked. Microsoft describes increasing presentation latency from one to two as a way to improve CPU/GPU parallelism. [DXGI latency guidance](https://learn.microsoft.com/en-us/windows/uwp/gaming/reduce-latency-with-dxgi-1-3-swap-chains).

These would be our documented definitions of the presets. There is no single configuration used by every “typical game.”

**I would treat the latency preset’s values as initial candidates to validate.** Your requirement prioritizes actual input latency, which is not always minimized by the smallest queue or by spinning. A second frame can improve the sampling cadence enough to help, and active waiting can compete with other CPU work, especially software rendering. Before finalizing that preset, I would compare actual response latency for one versus two GPU frames and waitable versus ordinary presentation. Any preset adjustment would follow those measurements; FPS alone would not select it.

**Custom settings would be genuinely ignored outside Custom mode.**

I would use two-stage loading:

```text
Read the INI structure and Pipeline.Mode
    ↓
MinimizeInputLatency → use the built-in latency preset
Standard            → use the built-in standard preset
Custom              → parse and validate Pipeline.Custom
```

For either preset, custom values would not be parsed, validated, merged, or used as overrides. For example, this would still run in Standard mode:

```ini
[Pipeline]
Mode = Standard

[Pipeline.Custom]
MaxGpuFramesInFlight = invalid
```

The file must still have valid INI structure. In Custom mode, missing required settings, unknown keys, invalid values, and duplicate settings would produce useful errors naming the section, key, and line.

The resolver would be a small function that can be tested without creating a window or D3D12 device:

```cpp
PipelineSettings ResolvePipelineSettings(const IniDocument& ini)
{
    switch (ReadPipelineMode(ini))
    {
    case PipelineMode::MinimizeInputLatency:
        return MinimizeInputLatencyPreset;

    case PipelineMode::Standard:
        return StandardPreset;

    case PipelineMode::Custom:
        return ParseAndValidateCustomPipeline(ini);
    }

    throw std::runtime_error("Invalid pipeline mode.");
}
```

I would extract the current INI reading from [Renderer.cpp](C:/Users/k/Repository/CPlusPlus/Simple_DirectX12_3D_Game/Source/Renderer.cpp:1106) into a small application-settings component. The existing paint and sphere settings would keep their own validation.

**The renderer would separate frame resources from presentation buffers.**

Currently, [`FrameCount`](C:/Users/k/Repository/CPlusPlus/Simple_DirectX12_3D_Game/Source/Renderer.h:33) controls both. I would introduce:

- **Frame contexts:** command allocators, changing constants, and completion-fence records, sized for the permitted GPU frames in flight.
- **Swap-chain buffers:** presentable images and render-target descriptors, sized by `BackBufferCount`.
- **A frame sequence:** tracks submitted frames independently of DXGI’s current back-buffer index.

Before preparing another frame, the CPU would wait until doing so fits the configured GPU-frame budget. Resource-reuse checks would remain mandatory in every mode.

When `WaitForPresentation = true`, create a waitable swap chain, apply `MaxPresentLatency`, and obtain its wait handle. When false, use ordinary non-waitable presentation and mark `MaxPresentLatency` inactive. The per-swap-chain setter requires a waitable swap chain. [Microsoft’s API contract](https://learn.microsoft.com/en-us/windows/win32/api/dxgi1_3/nf-dxgi1_3-idxgiswapchain2-setmaximumframelatency).

**The frame loop would wait before taking the input state used for rendering.**

```text
Process window events
    ↓
Wait for enabled presentation admission,
GPU-frame budget, and reusable frame resources
    ↓
Process newly arrived input and sample the latest state
    ↓
Simulate → record → submit → present → record completion fence
```

Event mode would use message-aware Windows waits. Spin mode would poll readiness with CPU pause instructions and periodically service messages. A message arriving during a wait would not count as rendering permission.

Resize, minimize, or shutdown messages would cancel the pending frame attempt as needed. After a resize, readiness would be checked against the current resources. The loop would service input without requiring the Windows message queue to become permanently empty, avoiding starvation from a sustained stream of messages.

These waits enforce synchronization and queue limits. Neither strategy would insert a timer-based delay to control FPS.

**Requested settings and effective settings would be recorded separately.**

For example, if tearing is requested but unavailable:

```text
Mode: MinimizeInputLatency
AllowTearing: requested=true, effective=false
Reason: DXGI tearing support unavailable
```

Likewise, the tearing presentation flag is inactive when VSync is enabled. Capability checks and fallbacks would use vendor-neutral interfaces, without GPU-vendor branches or SDKs. The app already has a DXGI tearing capability check that this design can reuse.

**I would initially load the pipeline configuration only at startup.**

Changing the INI would require a restart, matching the existing settings workflow. This avoids runtime swap-chain reconstruction and partially applied configurations. Waitable-swap-chain mode changes require recreation rather than just changing the resize flags. [Microsoft’s creation requirements](https://learn.microsoft.com/en-us/windows/uwp/gaming/reduce-latency-with-dxgi-1-3-swap-chains).

I would also remove the current `V` hotkey’s direct VSync override. Otherwise, selecting `MinimizeInputLatency` could silently become a different configuration, or Custom could stop matching the INI. A future settings UI could explicitly change the mode to Custom when editing individual settings.

Validation would cover preset resolution, complete isolation of inactive custom settings, invalid active settings, GPU-frame-budget invariants, and D3D12 rendering with different buffer counts. Runtime checks would include resizing and closing while waiting, both wait strategies, and available hardware plus WARP. The latency preset would additionally need an input-driven visual response test to compare latency and its variation, rather than relying on the existing time-driven animation or an FPS counter.

## User

i want VSync to be a setting outside of pipelinemode setting that may be set to true/false in any mode. is that possible

## Assistant

Yes. VSync can be independent of pipeline mode. I’ll record that change to the design, including how it affects tearing and the `V` hotkey.

## Assistant final response

**Yes. VSync should be an independent rendering setting that applies in every pipeline mode.**

For example:

```ini
[Rendering]
VSync = false

[Pipeline]
Mode = MinimizeInputLatency
```

The rules would be:

- Neither preset sets or overrides VSync.
- `[Pipeline.Custom]` contains the other pipeline settings, with no VSync entry.
- The renderer combines the selected pipeline configuration with the independent VSync value.
- The existing `V` hotkey can toggle VSync in any mode without changing the pipeline mode. As today, that would be a runtime change rather than rewriting the INI.

**Tearing would follow the effective VSync state:**

With VSync enabled, the tearing presentation flag is disabled. With VSync disabled, tearing is permitted if requested by the pipeline and supported.

Enabling VSync would not erase the pipeline’s `AllowTearing` preference. Turning VSync off again would make that preference applicable again.

`MinimizeInputLatency` with `VSync = true` would mean **minimize input latency subject to the user’s VSync requirement**. `Standard` with `VSync = false` would retain its balanced queueing and waiting settings while presenting without VSync.

This also removes the need to disable the `V` hotkey, which I suggested in the previous design.
