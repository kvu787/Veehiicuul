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
