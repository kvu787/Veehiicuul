# Game platform analysis

Prepared September 24, 2026, for the Windows-first racing and world-simulator projects, with eventual Android and other accessible platforms in mind.

Start with the [full decision report](C:/Users/k/Repository/External/godot/MyAnalysis/EnginePlatformReport.md).

For the follow-up question, read the [AI development comparison](C:/Users/k/Repository/External/godot/MyAnalysis/AiDevelopmentComparison.md). It compares five levels, from occasional assistance to milestone delegation, with project-specific examples and a practical trial design. **AI-specific productivity has no measured winner here:** Godot and Unity both support extensive automation, while native's strongest case is a deliberately small framework with excellent verification. The focused report includes Unity's newer official CLI/Pipeline route.

The [exact-version source-access addendum](C:/Users/k/Repository/External/godot/MyAnalysis/ExactVersionSourceAccess.md) evaluates the added assumption that agents can inspect the exact source versions in use, including Godot 4.7.2. It explains where this provides a small, moderate or large advantage and how it changes the recommendation.

**Preferred long-term platform: Godot .NET with C#, using engine-independent game cores.** Confidence is moderate versus Unity. The durable advantage is engine ownership; modern .NET and the existing Godot simulator strengthen the fit. The racer uses custom portable logic, so moving it does not require replacing a sophisticated Unity physics stack.

**Unity is a close and credible alternative.** It preserves the current racer and provides the more established C# Android path. Godot 4.7 still labels C# Android support experimental, and official Godot 4 C# web export is unavailable. Those risks require a representative Android export before committing substantial migration effort. The evidence does not prove that Godot has lower total migration cost.

**Custom C++/Vulkan/DX12 is viable, but currently hard to justify as the shared production platform.** The native prototype demonstrates useful graphics control, not a complete equivalent game or proven end-to-end latency superiority. Keep native code available for specific measured needs, or choose the custom platform deliberately if runtime development itself becomes an important goal.

The full report covers project maturity, migration, rendering, language/runtime differences, simulation architecture, latency, Android, additional platforms, AI-assisted workflows, ownership/licensing, costs and a bounded validation plan. It distinguishes observations from judgments and proposed experiments.

Supporting evidence:

- [Godot AI workflow](C:/Users/k/Repository/External/godot/MyAnalysis/GodotAiWorkflowEvidence.md)
- [Unity AI workflow](C:/Users/k/Repository/External/godot/MyAnalysis/UnityAiWorkflowEvidence.md)
- [Native C++ AI workflow](C:/Users/k/Repository/External/godot/MyAnalysis/NativeAiWorkflowEvidence.md)
- [Automatou](C:/Users/k/Repository/External/godot/MyAnalysis/AutomatouAssessment.md)
- [ZoomTracks](C:/Users/k/Repository/External/godot/MyAnalysis/ZoomTracksAssessment.md)
- [Veehiicuul and latency experiments](C:/Users/k/Repository/External/godot/MyAnalysis/VeehiicuulAssessment.md)
- [Godot engine source](C:/Users/k/Repository/External/godot/MyAnalysis/GodotSourceAssessment.md)
- [Latency recalculation script](C:/Users/k/Repository/External/godot/MyAnalysis/RecheckExistingLatency.ps1) and [results with source hashes](C:/Users/k/Repository/External/godot/MyAnalysis/RecheckedExistingLatency.json)

The source repositories were inspected read-only. No game builds, game test suites, new runtime benchmarks or Android exports were performed. Four saved latency recordings were independently recalculated. All work products from this analysis are in this folder.
