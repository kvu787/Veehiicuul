#include "SettingsValidation.h"
#include "PaintParameters.h"

#include <cmath>
#include <format>
#include <stdexcept>
#include <string_view>

namespace
{
void RequireWholeNumber(double value, double minimum, double maximum, std::string_view name)
{
    if (!std::isfinite(value) || value < minimum || value > maximum || std::trunc(value) != value)
        throw std::invalid_argument(std::format("{}: expected a whole number in [{}, {}].", name, minimum, maximum));
}

void ValidatePaint(const ApplicationSettings::Paint& paint, std::string_view name)
{
    if (paint.BaseColor.size() != 3)
        throw std::invalid_argument(std::format("{}.BaseColor: expected exactly three RGB channels.", name));
    try { SimplePaint::ValidateParameters(ToPaintParameters(paint)); }
    catch (const std::invalid_argument& error)
    {
        throw std::invalid_argument(std::format("{}: {}", name, error.what()));
    }
}
}

void ValidatePipeline(const ApplicationSettings::Pipeline& pipeline)
{
    if (pipeline.Preset != "MinimizeInputLatency" && pipeline.Preset != "Standard" &&
        pipeline.Preset != "MaximizeFps" && pipeline.Preset != "Custom")
        throw std::invalid_argument("RenderPipeline.Preset: expected MinimizeInputLatency, Standard, MaximizeFps, or Custom.");

    if (pipeline.Preset == "Custom")
    {
        RequireWholeNumber(pipeline.MaxGpuFramesInFlight, 1, 16, "RenderPipeline.MaxGpuFramesInFlight");
        RequireWholeNumber(pipeline.MaxPresentLatency, 1, 16, "RenderPipeline.MaxPresentLatency");
        RequireWholeNumber(pipeline.BackBufferCount, 2, 16, "RenderPipeline.BackBufferCount");
        if (pipeline.WaitStrategy != "Event" && pipeline.WaitStrategy != "Spin")
            throw std::invalid_argument("RenderPipeline.WaitStrategy: expected Event or Spin.");
    }
}

void ValidateSettings(const ApplicationSettings& settings)
{
    ValidatePipeline(settings.RenderPipeline);
    RequireWholeNumber(settings.Sphere.UResolution, 3, 512, "Sphere.UResolution");
    RequireWholeNumber(settings.Sphere.VResolution, 2, 512, "Sphere.VResolution");
    ValidatePaint(settings.SimplePaintShader_Axles, "SimplePaintShader_Axles");
    ValidatePaint(settings.SimplePaintShader_Body, "SimplePaintShader_Body");
    ValidatePaint(settings.SimplePaintShader_Cabin, "SimplePaintShader_Cabin");
    ValidatePaint(settings.SimplePaintShader_Headlights, "SimplePaintShader_Headlights");
    ValidatePaint(settings.SimplePaintShader_Wheels, "SimplePaintShader_Wheels");
    ValidatePaint(settings.SimplePaintShader_Sphere, "SimplePaintShader_Sphere");
}
