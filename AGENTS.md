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

## Platform and GPU policy

- Runtime preconditions are Windows 10 or Windows 11 and x86_64 (x64).
- There are no GPU preconditions. Do not require a particular GPU vendor,
  model, generation, or hardware feature set.
- Do not implement or integrate GPU-vendor-specific code, APIs, SDKs,
  extensions, optimizations, workarounds, or vendor-ID-based behavior.
  NVIDIA Reflex and AMD Anti-Lag 2 are explicitly prohibited, including
  optional integrations.
- Use vendor-neutral Windows, Direct3D 12, and DXGI APIs for capability
  detection and fallback paths.
- Apply this policy to rendering, latency, queueing, and all other repository
  code. Resolve unsupported capabilities through vendor-neutral fallbacks.

## Frame rate policy

The app must not implement any frame rate limiting of its own. Do not add
an FPS cap, target-frame-rate or `FrameRateLimit` setting, or timer, sleep,
spin, or pacing logic intended to enforce a frame rate. This prohibition
includes optional limiters and background FPS caps.

GPU fences, resource-availability waits, DXGI presentation waits, and VSync
remain valid synchronization mechanisms. They must not be supplemented with
app-owned timing delays to impose an FPS target.
