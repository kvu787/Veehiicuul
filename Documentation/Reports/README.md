# Historical reports

These reports describe the commits and parameter proposals named in each report. Their numerical settings, shader behavior, and performance results are historical; they do not override the implemented [SimplePaint specification](../Specification.md) or [usage guide](../Usage.md).

- [Orthographic renderer optimization](Rendering/OrthographicOptimization.md).
- [Initial shader numerical analysis](ShaderNumerics/Analysis.md), with [examples](ShaderNumerics/Examples.csv) and [presets](ShaderNumerics/Presets).
- [Constrained-input analysis](ShaderNumerics/ConstrainedAnalysis.md), with [examples](ShaderNumerics/ConstrainedExamples.csv).
- [RGB-constrained analysis](ShaderNumerics/RgbConstrainedAnalysis.md), with [examples](ShaderNumerics/RgbConstrainedExamples.csv).
- [Abstract-contract analysis](ShaderNumerics/AbstractContractAnalysis.md).

Command examples and source paths in these reports are relative to the repository root. The reports and their attachments now live under `Documentation/Reports`.

The INI presets in these reports preserve historical inputs. Current runtime
settings use [assets/Settings.json](../../assets/Settings.json); see the
[usage guide](../Usage.md) for the supported JSON format.
