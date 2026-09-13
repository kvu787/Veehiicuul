#include "SettingsTestData.h"
#include "SettingsValidation.h"
#include "RenderPreparation.h"
#include <cmath>
#include <iostream>
#include <limits>
#include <stdexcept>

namespace
{
void Require(bool value) { if (!value) throw std::runtime_error("Settings contract failed"); }
template<class Edit> void Reject(Edit edit)
{
    auto settings = MakeTestSettings();
    settings.RenderPipeline.Preset = "Custom";
    edit(settings);
    try { ValidateSettings(settings); }
    catch (const std::invalid_argument&) { return; }
    throw std::runtime_error("Invalid settings accepted");
}
}
int main()
{
    try
    {
        auto settings = MakeTestSettings();
        const auto original = settings;
        ValidateSettings(settings);
        Require(settings == original);
        Require(ResolveRenderPipeline(settings.RenderPipeline) == MinimizeInputLatencyRenderPipeline);
        for (const auto& name : {"Standard", "MaximizeFps", "MinimizeInputLatency"})
        {
            settings.RenderPipeline.Preset = name;
            settings.RenderPipeline.BackBufferCount = -100;
            const auto before = settings;
            ValidateSettings(settings);
            const auto resolved = ResolveRenderPipeline(settings.RenderPipeline);
            Require(settings == before);
            Require(resolved == (std::string_view(name) == "Standard" ? StandardRenderPipeline :
                std::string_view(name) == "MaximizeFps" ? MaximizeFpsRenderPipeline : MinimizeInputLatencyRenderPipeline));
        }
        settings = MakeTestSettings();
        settings.RenderPipeline = {.Preset="Custom", .VSync=true, .MaxGpuFramesInFlight=16,
            .MaxPresentLatency=1, .WaitForPresentation=false, .BackBufferCount=2,
            .AllowTearing=true, .WaitStrategy="Spin"};
        const auto resolved = ResolveRenderPipeline(settings.RenderPipeline);
        Require(resolved.maxGpuFramesInFlight == 16 && resolved.maxPresentLatency == 1 &&
            !resolved.waitForPresentation && resolved.backBufferCount == 2 &&
            resolved.allowTearing && resolved.waitStrategy == WaitStrategy::Spin);
        Reject([](auto& s) { s.RenderPipeline.Preset = "Typo"; });
        Reject([](auto& s) { s.RenderPipeline.WaitStrategy = "Typo"; });
        for (std::int32_t value : {-1, 0, 17, std::numeric_limits<std::int32_t>::min(),
            std::numeric_limits<std::int32_t>::max()})
        {
            Reject([&](auto& s) { s.RenderPipeline.MaxGpuFramesInFlight = value; });
            Reject([&](auto& s) { s.RenderPipeline.MaxPresentLatency = value; });
        }
        for (std::int32_t value : {-1, 1, 17}) Reject([&](auto& s) { s.RenderPipeline.BackBufferCount = value; });
        for (std::int32_t value : {-1, 2, 513, std::numeric_limits<std::int32_t>::max()}) Reject([&](auto& s) { s.Sphere.UResolution = value; });
        for (std::int32_t value : {-1, 1, 513}) Reject([&](auto& s) { s.Sphere.VResolution = value; });
        settings = MakeTestSettings();
        settings.Sphere = {.UResolution=512, .VResolution=2};
        ValidateSettings(settings);
        const auto paints = {
            &Settings::SimplePaintShader_Axles, &Settings::SimplePaintShader_Body,
            &Settings::SimplePaintShader_Cabin, &Settings::SimplePaintShader_Headlights,
            &Settings::SimplePaintShader_Wheels, &Settings::SimplePaintShader_Sphere};
        for (auto member : paints)
        {
            Reject([&](auto& s) { (s.*member).BaseColor.pop_back(); });
            Reject([&](auto& s) { (s.*member).BaseColor.push_back(.5); });
            for (double value : {0.0, 1.0, std::numeric_limits<double>::quiet_NaN()})
                Reject([&](auto& s) { (s.*member).BaseColor[1] = value; });
            Reject([&](auto& s) { (s.*member).Brightness = 0; });
            Reject([&](auto& s) { (s.*member).Shift = 1; });
            Reject([&](auto& s) { (s.*member).DarkPoint = 1; });
            Reject([&](auto& s) { (s.*member).LightPoint = 0; });
            Reject([&](auto& s) { (s.*member).RotationDegrees = 360; });
        }
        for (auto field : {&Settings::Paint::Brightness, &Settings::Paint::Shift,
            &Settings::Paint::DarkPoint, &Settings::Paint::LightPoint,
            &Settings::Paint::RotationDegrees})
        {
            Reject([&](auto& s) { s.SimplePaintShader_Sphere.*field = std::numeric_limits<double>::infinity(); });
            Reject([&](auto& s) { s.SimplePaintShader_Sphere.*field = std::numeric_limits<double>::quiet_NaN(); });
        }
        Reject([](auto& s) { s.SimplePaintShader_Sphere.Brightness = std::nextafter(SimplePaint::Margin, 0.0); });
        Reject([](auto& s) { s.SimplePaintShader_Sphere.Shift = std::nextafter(SimplePaint::InteriorMaximum, 1.0); });
        settings = MakeTestSettings();
        const auto baseline = CompilePaintMaterials(settings);
        settings.SimplePaintShader_Sphere.RotationDegrees = 90;
        const auto changed = CompilePaintMaterials(settings);
        Require(baseline[0].warp == changed[0].warp);
        Require(baseline[5].warp != changed[5].warp);
        std::cout << "Pure C++ validation and renderer preparation passed.\n";
        return 0;
    }
    catch (const std::exception& error) { std::cerr << error.what() << '\n'; return 1; }
}
