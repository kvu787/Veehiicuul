# Native C++ and graphics APIs under increasing AI assistance

Assessment date: September 24, 2026. This note provides the strongest evidence-based case for a small custom native platform in the broader Godot/Unity decision. Source and official documentation were inspected; no build, application, capture, or test was run. New material is confined to `MyAnalysis`. AI productivity conclusions below are engineering judgments about the available feedback surfaces, not measured model success rates or forecasts.

**Extensive AI assistance makes native development materially more plausible. A small, explicit, code-defined game framework can be exceptionally easy for an agent to inspect, modify, compile, and test. That can make individual native tasks easier to automate than editor-heavy tasks in either engine. It does not automatically make the complete native game cheaper to deliver, because the project still owns a larger set of platform and runtime responsibilities.**

The appropriate distinction is between AI producing source, AI completing verified engineering tasks, and AI delivering the intended game across target devices. Those are related but different achievements. There is no basis here for claiming fully AI-implemented native development is impossible; nor is there evidence that it already delivers these two games with less total effort than Godot or Unity.

## 1. The strongest native case is a small game framework

The favorable native scenario is not building a general-purpose competitor to Unity. It is implementing the small runtime actually needed by the overhead racer and world simulator, reusing suitable libraries for platform services and content tooling. With modest visuals, fixed design needs, and a deliberately limited editor/tool scope, the owned code may remain small enough for an agent to understand its complete execution path.

This has practical advantages for extensive agent work:

- Data and control flow can be explicit instead of distributed across scene state, import settings, packages, callbacks, and editor operations.
- Build settings, shaders, game data, and test expectations can be ordinary files under version control.
- A renderer defect can be traced directly to the owning code, including resource transitions and synchronization.
- Deterministic test entry points can run without interactive content authoring.
- New checks can expose exactly the runtime state the agent needs: resource counts, frame queues, simulation hashes, or pixel outputs.

These are design possibilities, not exclusive C++ features. Godot and Unity can also use code-defined scenes, engine-independent domain libraries, scripted import/export, and reproducible tests. An established engine's internal complexity is often reusable implementation the agent does not need to modify. Conversely, a native framework loses its simplicity advantage if it accumulates speculative abstraction layers and multiple backends before the games need them.

For this user, the existing C++ source is a better starting point than a blank sheet for Windows graphics experiments. It is not a portable game runtime already at feature parity: its car is animated by a clock-derived transform, and its scene includes a sphere and flattened backdrop. [Renderer.cpp:1165](C:/Users/k/Repository/Veehiicuul/3dTestScene_CppDx12/Source/Renderer.cpp:1165), [scope of the sample](C:/Users/k/Repository/Veehiicuul/3dTestScene_CppDx12/README.md:31).

## 2. Concrete agent-friendly features already present

The native repository contains more useful automation infrastructure than the phrase “a graphics demo” conveys.

| Existing mechanism                                 | Local evidence                                                                                                                                                                                                                                 | What an agent can learn from it                                                                     |
| -------------------------------------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | --------------------------------------------------------------------------------------------------- |
| Named configure/build/test presets                 | [CMakePresets.json:3](C:/Users/k/Repository/Veehiicuul/3dTestScene_CppDx12/CMakePresets.json:3), [test presets:58](C:/Users/k/Repository/Veehiicuul/3dTestScene_CppDx12/CMakePresets.json:58)                                                  | Repeatable Debug/Release entry points and visible failure output                                    |
| Shader compilation with warnings treated as errors | [CMakeLists.txt:26](C:/Users/k/Repository/Veehiicuul/3dTestScene_CppDx12/CMakeLists.txt:26)                                                                                                                                                    | Invalid HLSL and interface mistakes become tool-visible build failures                              |
| CPU geometry and settings contract tests           | [CMakeLists.txt:189](C:/Users/k/Repository/Veehiicuul/3dTestScene_CppDx12/CMakeLists.txt:189)                                                                                                                                                  | Math/data changes can be checked without relying on screenshots                                     |
| Hardware and WARP renderer tests                   | [CMakeLists.txt:211](C:/Users/k/Repository/Veehiicuul/3dTestScene_CppDx12/CMakeLists.txt:211)                                                                                                                                                  | Two execution paths exercise actual production rendering code                                       |
| Queue admission and cancellation assertions        | [RenderPipelineGpuTests.cpp:87](C:/Users/k/Repository/Veehiicuul/3dTestScene_CppDx12/Tests/RenderPipelineGpuTests.cpp:87), [blocked-queue test:115](C:/Users/k/Repository/Veehiicuul/3dTestScene_CppDx12/Tests/RenderPipelineGpuTests.cpp:115) | GPU budget, resize/restore, and responsiveness during a blocked queue have executable expectations  |
| Application lifecycle smoke tests                  | [ApplicationSmokeTests.cpp:35](C:/Users/k/Repository/Veehiicuul/3dTestScene_CppDx12/Tests/ApplicationSmokeTests.cpp:35)                                                                                                                        | VSync toggles, fullscreen, minimize/restore, and shutdown can be driven automatically               |
| Production shader pixel readback                   | [SimplePaintGpuTests.cpp:302](C:/Users/k/Repository/Veehiicuul/3dTestScene_CppDx12/Source/SimplePaint/Tests/SimplePaintGpuTests.cpp:302)                                                                                                       | Shader output is compared numerically with a CPU reference, including finite/range checks           |
| Separately formulated reference calculation        | [SimplePaintReference.h:37](C:/Users/k/Repository/Veehiicuul/3dTestScene_CppDx12/Source/SimplePaint/Tests/SimplePaintReference.h:37)                                                                                                           | A binary64 mathematical reference offers more independent evidence than duplicating optimized HLSL  |
| Validation messages become failing tests           | [Renderer.cpp:1345](C:/Users/k/Repository/Veehiicuul/3dTestScene_CppDx12/Source/Renderer.cpp:1345)                                                                                                                                             | Graphics API warnings/errors can enter the same repair loop as compiler and unit-test errors        |

The shader comparison uses explicit linear and sRGB error tolerances of 0.001, rather than demanding bitwise identity. That is a substantive graphics verification technique, not merely a build-success check. The queue tests deliberately block the GPU, send a cancellation message, and verify recovery; those are useful tests of synchronization behavior that ordinary happy-path gameplay might miss.

The tests also have limits visible in source. The lifecycle test checks Windows state/title responses, not whether the whole image or gameplay feels correct. Hardware and WARP are Windows execution paths, not a substitute for Android hardware. The shader test reports when its debug interface is unavailable; autonomous infrastructure must distinguish a requested validation check from one that actually ran. [Optional debug layer setup](C:/Users/k/Repository/Veehiicuul/3dTestScene_CppDx12/Source/SimplePaint/Tests/SimplePaintGpuTests.cpp:69), [reported validation status](C:/Users/k/Repository/Veehiicuul/3dTestScene_CppDx12/Source/SimplePaint/Tests/SimplePaintGpuTests.cpp:327).

This infrastructure demonstrates **potential for repeatable automated feedback**. Its presence alone does not measure how many tasks AI completed successfully, how much human debugging occurred, or whether the current binaries pass today.

## 3. What current primary documentation enables

### Build and test orchestration

CTest supports failure output, machine-readable JUnit results, JSON test enumeration, test selection, timeouts, and repeated execution. These give an agent a structured way to discover checks, invoke the relevant subset, inspect failures, and rerun after a repair. Repetition can help expose intermittent errors; accepting a test merely because it eventually passes would conceal them. These capabilities are available independently of any AI product. [Official CTest manual](https://cmake.org/cmake/help/latest/manual/ctest.1.html).

### Vulkan diagnostics

Khronos provides validation layers and identifiable valid-usage rules. This can turn an opaque graphics failure into a specific violated API requirement that an agent can investigate. Invalid usage that appears to work on one implementation may fail on another; validation is development instrumentation with overhead. [Khronos validation overview](https://docs.vulkan.org/guide/latest/validation_overview.html).

Validation is not a complete correctness oracle. The current synchronization validator documents gaps including memory aliasing, host memory races, and precise tracking of descriptors accessed by shaders. Zero reported errors therefore cannot establish universal synchronization correctness or cross-device reliability. [Official synchronization validation limitations](https://github.com/KhronosGroup/Vulkan-ValidationLayers/blob/main/docs/syncval_usage.md#known-limitations).

### Direct3D 12 diagnostics

Microsoft's GPU-based validation can detect problems that CPU-side validation cannot, including invalid descriptors and shader accesses in incompatible resource states. It instruments shaders/commands, changes some execution behavior, can substantially slow workloads, and emits messages asynchronously on the GPU timeline. Small validation scenes and separate performance runs are therefore appropriate. [Microsoft GPU-based validation documentation](https://learn.microsoft.com/en-us/windows/win32/direct3d12/using-d3d12-debug-layer-gpu-based-validation).

The inspected renderer and test code enable ordinary D3D12 debugging; no explicit `SetEnableGPUBasedValidation` call was found in the inspected source. This is an opportunity for a future diagnostic configuration, not evidence of a current bug and not a claim that external tools never enable it. [Renderer debug setup](C:/Users/k/Repository/Veehiicuul/3dTestScene_CppDx12/Source/Renderer.cpp:331), [renderer-test requirement](C:/Users/k/Repository/Veehiicuul/3dTestScene_CppDx12/Tests/RenderPipelineGpuTests.cpp:50).

### Capture automation

RenderDoc exposes an in-application API for triggering captures at chosen points and a Python interface for inspection/automation. Together, these support a workflow where an agent reproduces a fixed scene, captures the relevant frame, and extracts useful rendering evidence rather than depending entirely on interactive mouse operation. Version-matched setup and a supported runtime environment remain necessary; no such complete automated capture workflow was demonstrated in this repository inspection. [RenderDoc capture API](https://github.com/baldurk/renderdoc/blob/v1.x/docs/in_application_api.rst), [RenderDoc Python API](https://github.com/baldurk/renderdoc/blob/v1.x/docs/python_api/index.rst).

## 4. How increasing assistance changes the comparison

These levels describe **operating arrangements**, not intelligence rankings or predicted future capability.

| AI arrangement                         | Native C++ becomes attractive when...                                                  | What still determines the outcome                                                              |
| -------------------------------------- | -------------------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------- |
| Occasional explanations/snippets       | The developer already understands the platform and wants narrow technical control      | Most graphics/platform implementation and diagnosis still consumes personal effort             |
| AI writes bounded changes; human tests | Code-defined features are easier to specify/review than equivalent editor manipulation | Human review, integration, and testing can become the bottleneck as generated volume grows     |
| Agent builds, tests, repairs changes   | Compiler, contracts, validation, and pixel/replay tests expose failures clearly        | Feedback quality and actual tested behavior matter more than source-generation speed           |
| Agent delivers larger features         | Small architecture and stable interfaces keep cross-system effects understandable      | Regression coverage, content integration, device access, and acceptance criteria matter        |
| Extensive autonomous implementation    | The complete delivery loop is instrumented, including devices, captures, and packaging | Unobserved behavior, changing requirements, taste, and long-tail device faults remain relevant |

As assistance increases, native's **marginal implementation cost** can fall disproportionately because setup code and repetitive API plumbing are large parts of its initial burden. An agent may also inspect a small native codebase more completely than a complex editor-driven project. This strengthens the native option; it should not be dismissed with a blanket rule that solo developers must never own rendering code.

But both engines receive the same benefit from automated coding, import/build scripts, tests, and diagnostics. They also supply platform services the native project would otherwise build or integrate. Native wins the overall economic comparison only when the cost saved through a smaller, more controllable workflow exceeds the work added by owning those services. No numerical crossover point can be calculated from these repositories alone.

High autonomy can actually increase the value of an established engine if it removes classes of work the agent would otherwise have to validate. It can favor native if the game is tightly scoped, most content is generated or described as data, the developer does not need a broad visual editor, and the owned execution path remains small. These are conditional advantages, not universal rankings.

## 5. Why Android remains engineering work even with extensive AI

The current native path is Win32/DXGI/D3D12. Implementing a Vulkan renderer would address graphics portability, not all operating-system integration. The Android GameActivity library provides lifecycle, input, and text-input support for C/C++ games, and its integration includes native glue, an activity, manifest configuration, and the Android build/package setup. This makes a native Android path practical without inventing everything, while still requiring integration and verification. [Official GameActivity overview](https://developer.android.com/games/agdk/game-activity), [integration guide](https://developer.android.com/games/agdk/game-activity/get-started).

An agent can implement and automate much of that work. It needs an actual device or suitable execution service to verify surface recreation, pause/resume, touch/controller behavior, install/update, and sustained performance. A desktop compile and WARP pass contain no observations of those outcomes. Emulators and one physical phone provide useful but bounded coverage.

For Windows plus Android, one shared Vulkan backend or an established portable rendering layer can reduce the number of custom implementations to own. Retaining a separate DX12 backend may still be worthwhile for a demonstrated Windows need, but “AI can write another backend” is insufficient justification for permanently maintaining and checking it. This is an architectural judgment, not a requirement to discard existing DX12 experiments.

## 6. What a persuasive native qualification would establish

The most informative comparison is a **small complete game slice implemented with the intended AI workflow**, not a rendering microbenchmark or a count of generated lines.

For the racer, use one real track, the intended motion/collision rules, controller/touch input, required visual style, restart flow, and packaged Windows/Android builds. For the simulator, use a representative encounter/editor operation, real rule execution, save/load, and the intended presentation. Apply comparable acceptance criteria to Godot and Unity slices.

Record actual elapsed developer intervention, autonomous repair cycles, build/run reliability, observed regressions, frame-time tails, visible-response checks, and mobile behavior. State which parts were already present so incumbent implementation is not silently treated as free in one candidate and charged to another. A different test author or independent oracle should challenge behavior rather than simply accepting the implementation's output as the expected answer.

The native repository already contains particularly good patterns to extend: separately formulated numeric references, production GPU readback, explicit queue assertions, and automated lifecycle driving. A similarly observable gameplay/device pipeline would make the strongest case for extensive AI-driven native development. Until that evidence exists, the appropriate conclusion is **credible and potentially competitive under a disciplined small scope**, with a larger delivery-validation burden than the engines already carrying those platform responsibilities.
