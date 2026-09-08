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
        for (double value : {-1.0, 0.0, 1.5, 17.0, 1e100,
            std::numeric_limits<double>::infinity(), std::numeric_limits<double>::quiet_NaN()})
        {
            Reject([&](auto& s) { s.RenderPipeline.MaxGpuFramesInFlight = value; });
            Reject([&](auto& s) { s.RenderPipeline.MaxPresentLatency = value; });
        }
        for (double value : {1.0, 2.5, 17.0}) Reject([&](auto& s) { s.RenderPipeline.BackBufferCount = value; });
        for (double value : {2.0, 3.5, 513.0, 1e100}) Reject([&](auto& s) { s.Sphere.UResolution = value; });
        for (double value : {1.0, 2.5, 513.0}) Reject([&](auto& s) { s.Sphere.VResolution = value; });
        settings = MakeTestSettings();
        settings.Sphere = {.UResolution=512, .VResolution=2};
        ValidateSettings(settings);
        const auto paints = {
            &ApplicationSettings::SimplePaintShader_Axles, &ApplicationSettings::SimplePaintShader_Body,
            &ApplicationSettings::SimplePaintShader_Cabin, &ApplicationSettings::SimplePaintShader_Headlights,
            &ApplicationSettings::SimplePaintShader_Wheels, &ApplicationSettings::SimplePaintShader_Sphere};
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
        for (auto field : {&ApplicationSettings::Paint::Brightness, &ApplicationSettings::Paint::Shift,
            &ApplicationSettings::Paint::DarkPoint, &ApplicationSettings::Paint::LightPoint,
            &ApplicationSettings::Paint::RotationDegrees})
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
