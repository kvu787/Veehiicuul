#include "RenderPreparation.h"
#include "SettingsValidation.h"
#include "PaintParameters.h"
#include <stdexcept>

ResolvedRenderPipeline ResolveRenderPipeline(const struct ApplicationSettings::RenderPipeline& pipeline)
{
    ValidatePipeline(pipeline);
    if (pipeline.Preset == "MinimizeInputLatency") return MinimizeInputLatencyRenderPipeline;
    if (pipeline.Preset == "Standard") return StandardRenderPipeline;
    if (pipeline.Preset == "MaximizeFps") return MaximizeFpsRenderPipeline;
    return {
        .preset = RenderPipelinePreset::Custom,
        .maxGpuFramesInFlight = static_cast<std::uint32_t>(pipeline.MaxGpuFramesInFlight),
        .maxPresentLatency = static_cast<std::uint32_t>(pipeline.MaxPresentLatency),
        .waitForPresentation = pipeline.WaitForPresentation,
        .backBufferCount = static_cast<std::uint32_t>(pipeline.BackBufferCount),
        .allowTearing = pipeline.AllowTearing,
        .waitStrategy = pipeline.WaitStrategy == "Event" ? WaitStrategy::Event : WaitStrategy::Spin,
    };
}

std::array<SimplePaint::GpuMaterial, 6> CompilePaintMaterials(const ApplicationSettings& settings)
{
    // Also protects direct C++ callers, independently of how settings were loaded.
    ValidateSettings(settings);
    const auto compile = [](const ApplicationSettings::Paint& paint) {
        return SimplePaint::Material::Compile(ToPaintParameters(paint)).Constants();
    };
    return {
        compile(settings.SimplePaintShader_Axles), compile(settings.SimplePaintShader_Body),
        compile(settings.SimplePaintShader_Cabin), compile(settings.SimplePaintShader_Headlights),
        compile(settings.SimplePaintShader_Wheels), compile(settings.SimplePaintShader_Sphere),
    };
}

std::wstring_view RenderPipelinePresetName(RenderPipelinePreset preset)
{
    switch (preset)
    {
    case RenderPipelinePreset::MinimizeInputLatency: return L"MinimizeInputLatency";
    case RenderPipelinePreset::Standard: return L"Standard";
    case RenderPipelinePreset::MaximizeFps: return L"MaximizeFps";
    case RenderPipelinePreset::Custom: return L"Custom";
    }
    throw std::invalid_argument("Invalid render pipeline preset.");
}
