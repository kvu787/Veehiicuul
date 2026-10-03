# MaximizeFps pipeline discussion

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

## Constraints

Follow the shared developer and agent constraints in [README.md](README.md#constraints).
That section is the single source of truth for these constraints.

</INSTRUCTIONS>
<environment_context>
  <cwd>C:\Users\k\Repository\CPlusPlus\Simple_DirectX12_3D_Game</cwd>
  <shell>powershell</shell>
  <current_date>2026-09-06</current_date>
  <timezone>America/Los_Angeles</timezone>
  <filesystem><workspace_roots><root>C:\Users\k\Repository\CPlusPlus\Simple_DirectX12_3D_Game</root></workspace_roots><permission_profile type="managed"><file_system type="restricted"><entry access="read"><special>:root</special></entry></file_system></permission_profile></filesystem>
</environment_context>

## User

if there was a pipeline mode for maximizing fps (named "MaximizeFps"), what would it do and what would the tradeoffs be?



## Assistant (commentary)

I’ll inspect the existing rendering modes and constraints, then outline what a `MaximizeFps` mode would change and what visual quality it would trade away.

## Assistant (commentary)

The existing modes control frame queueing and presentation; they don’t change shaders or resolution. A `MaximizeFps` preset would allow more overlap between CPU and GPU work and relax presentation waiting. The main tradeoffs would be potentially higher input latency, tearing, and power use. `Standard` already allows overlap, so any FPS gain would need measurement.

## Assistant final response

**`MaximizeFps` would prioritize rendering throughput by keeping the CPU and GPU busy, while accepting more queued work and potentially higher input latency.** In this project, it would preserve resolution, geometry, and SimplePaint shading.

Based on the [existing pipeline settings](C:/Users/k/Repository/CPlusPlus/Simple_DirectX12_3D_Game/Documentation/Pipeline.md), I would benchmark this initial candidate:

| Setting                | Standard | Proposed MaximizeFps |
| ---------------------- | -------- | -------------------- |
| `MaxGpuFramesInFlight` | 2        | 3                    |
| `MaxPresentLatency`    | 2        | Inactive             |
| `WaitForPresentation`  | true     | false                |
| `BackBufferCount`      | 3        | 4                    |
| `AllowTearing`         | false    | true                 |
| `WaitStrategy`         | Event    | Spin                 |

The intended effects would be:

- **More CPU/GPU overlap.** Extra frame resources let the CPU prepare work while the GPU processes earlier frames, reducing idle gaps when the current queue limits are restrictive. Microsoft documents this throughput-versus-latency tradeoff in its [frame-latency guidance](https://learn.microsoft.com/en-us/windows/uwp/gaming/reduce-latency-with-dxgi-1-3-swap-chains).
- **Less presentation admission waiting.** An ordinary, non-waitable swap chain removes the explicit DXGI readiness wait. GPU resource-safety waits remain mandatory, and `Present` or buffer availability can still block. Disabling this wait does not guarantee higher FPS.
- **Permit tearing when supported and VSync is off.** This enables the presentation path Microsoft describes for [unthrottled flip-model rendering](https://learn.microsoft.com/en-us/windows/win32/direct3ddxgi/for-best-performance--use-dxgi-flip-model).
- **Actively poll necessary GPU waits.** `Spin` may reduce wake-up overhead for very short waits. It needs comparison against `Event`; consuming more CPU time does not necessarily produce more frames.

The tradeoffs would be **potentially higher input latency**, visible tearing, greater CPU/power use and heat, and additional buffer memory. Higher average FPS also would not guarantee more consistent frame times or more complete frames reaching the display: DXGI can discard superseded frames at sync interval zero. [Presentation behavior](https://learn.microsoft.com/en-us/windows/win32/api/dxgi/nf-dxgi-idxgiswapchain-present).

**VSync would remain independent**, as you requested. With it enabled, this mode would maximize throughput subject to VSync. All settings would remain vendor-neutral, with no application FPS limiter.

The proposed **3 frames / 4 buffers are experimental starting values**. Setting every queue to 16 would mostly risk adding backlog once the GPU is already busy. Since `Standard` already permits overlap, this scene might gain little. I would select the smallest queue that reaches the measured throughput plateau, and keep `Spin` only if it actually improves sustained FPS.


## User

go ahead and implement this so i can experiment and measure perf

## Assistant (commentary)

I’ll add `MaximizeFps` with the proposed settings, make it the active mode for your first run, and update the documentation and tests. VSync will remain independent.

## Assistant (commentary)

The renderer already supports the proposed queue and wait settings, so this can use the existing synchronization code. I’m adding coverage for the new mode’s settings, GPU rendering, and window lifecycle, plus a `Custom` example for comparing individual settings.

## Assistant (commentary)

The Release build and all 11 tests passed, including hardware/WARP rendering and the new `MaximizeFps` window lifecycle test. The staged settings select `MaximizeFps` with VSync off. I’m finishing the documentation and commits; these checks verify behavior, not an FPS improvement.

## Assistant final response

Implemented and selected `MaximizeFps` in [Settings.ini](C:/Users/k/Repository/CPlusPlus/Simple_DirectX12_3D_Game/assets/Settings.ini).

Double-click [Run.cmd](C:/Users/k/Repository/CPlusPlus/Simple_DirectX12_3D_Game/Run.cmd) to try it: **3 GPU frames, 4 buffers, Spin waits, presentation waiting disabled, and tearing permitted**. VSync remains independent.

The [experiment guide](C:/Users/k/Repository/CPlusPlus/Simple_DirectX12_3D_Game/Documentation/Pipeline.md#experimenting-with-maximizefps) includes an equivalent `Custom` configuration for tuning.

**All 11 Release tests passed**, including hardware/WARP rendering and window lifecycle checks. Performance gains still need measurement. Implementation, documentation, and conversation are committed separately.
