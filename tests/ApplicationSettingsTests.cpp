#include "ApplicationSettings.h"
#include <iostream>
#include <sstream>
#include <stdexcept>
#include <string>

namespace
{
void Require(bool value, const char* reason) { if (!value) throw std::runtime_error(reason); }
ApplicationSettings Parse(const std::string& text) { std::istringstream input(text); return ParseApplicationSettings(input); }
const std::string custom =
    "MaxGpuFramesInFlight=3\nMaxPresentLatency=1\nWaitForPresentation=false\n"
    "BackBufferCount=2\nAllowTearing=true\nWaitStrategy=Spin\n";
void Reject(const std::string& text, const std::string& fragment)
{
    try { Parse(text); }
    catch (const std::exception& e)
    {
        Require(std::string(e.what()).find(fragment) != std::string::npos, e.what());
        return;
    }
    throw std::runtime_error("Invalid settings accepted: " + text);
}
}
int main()
{
    try
    {
        // Exercise the real startup file as well as the isolated configuration cases.
        (void)LoadApplicationSettings(ModuleDirectory() / "assets" / "Settings.ini");
        for (const auto& preset : {std::string("Standard"), std::string("MinimizeInputLatency"),
            std::string("MaximizeFps"), std::string("Custom")})
        {
            const std::string prefix = "[RenderPipeline]\nPreset=" + preset + "\nVSync=";
            const auto off = Parse(prefix + "false\n" + custom);
            const auto on = Parse(prefix + "true\n" + custom);
            Require(!off.vsync && on.vsync, "VSync was overridden by preset");
            Require(off.renderPipeline == on.renderPipeline, "VSync changed render pipeline configuration");
            if (preset == "Standard") Require(off.renderPipeline == StandardRenderPipeline, "Standard preset overridden");
            if (preset == "MinimizeInputLatency") Require(off.renderPipeline == MinimizeInputLatencyRenderPipeline, "Latency preset overridden");
            if (preset == "MaximizeFps")
            {
                Require(off.renderPipelinePreset == RenderPipelinePreset::MaximizeFps && off.renderPipeline == MaximizeFpsRenderPipeline,
                    "MaximizeFps preset not selected or overridden");
                Require(RenderPipelinePresetName(off.renderPipelinePreset) == L"MaximizeFps", "MaximizeFps diagnostic name lost");
            }
            if (preset == "Custom")
            {
                Require(off.renderPipeline.maxGpuFramesInFlight == 3 && off.renderPipeline.backBufferCount == 2, "Independent buffer/frame counts lost");
                Require(!off.renderPipeline.waitForPresentation && off.renderPipeline.allowTearing && off.renderPipeline.waitStrategy == WaitStrategy::Spin,
                    "Custom settings not applied");
                const auto reordered = Parse("[RenderPipeline]\n" + custom + "VSync=false\nPreset=Custom\n");
                Require(reordered.renderPipelinePreset == off.renderPipelinePreset && reordered.renderPipeline == off.renderPipeline &&
                    reordered.vsync == off.vsync, "Custom settings depend on key order");
            }
            Require(on.sphereUResolution == 64 && on.sphereVResolution == 32, "Sphere defaults changed");
            Reject(prefix + "false\n" + custom + "VSync=true\n", "duplicate setting");
            Reject(prefix + "false\n" + custom + "Preset=Custom\n", "duplicate setting");
            Reject(prefix + "bad\n" + custom, "expected true or false");
            Reject(prefix + "false\n" + custom + "Unknown=\n", "unknown setting");
            Reject(prefix + "false\n" + custom + "Mode=Standard\n", "unknown setting");
        }
        // Inactive custom controls are ignored even before Preset, including duplicates and invalid values.
        const std::string inactive =
            "MaxGpuFramesInFlight=garbage\nMaxGpuFramesInFlight=-3\nMaxPresentLatency=0\n"
            "WaitForPresentation=bad\nBackBufferCount=1\nAllowTearing=bad\nWaitStrategy=bad\n";
        for (const auto preset : {"Standard", "MinimizeInputLatency", "MaximizeFps"})
        {
            const auto selection = "VSync=false\nPreset=" + std::string(preset) + "\n";
            const auto expected = Parse("[RenderPipeline]\n" + selection);
            for (const auto& text : {inactive + selection, selection + inactive})
            {
                const auto result = Parse("[RenderPipeline]\n" + text);
                Require(result.renderPipelinePreset == expected.renderPipelinePreset && result.renderPipeline == expected.renderPipeline &&
                    result.vsync == expected.vsync, "Inactive custom values affected preset or VSync");
            }
        }
        const std::string base = "[RenderPipeline]\nPreset=Custom\nVSync=false\n";
        for (const auto key : {"MaxGpuFramesInFlight", "MaxPresentLatency", "WaitForPresentation", "BackBufferCount", "AllowTearing", "WaitStrategy"})
        {
            auto missing = custom;
            const auto start = missing.find(std::string(key) + '=');
            missing.erase(start, missing.find('\n', start) - start + 1);
            Reject(base + missing, std::string("Missing [RenderPipeline].") + key);
        }
        Reject(base + custom + "MaxGpuFramesInFlight=1\n", "duplicate setting");
        Reject(base + custom + "[RenderPipeline]\nVSync=true\n", "duplicate setting");
        Reject("[RenderPipeline]\nPreset=unknown\nVSync=false\n", "line 2");
        Reject("[RenderPipeline]\nPreset=MaximizeFPS\nVSync=false\n", "MaximizeFps");
        Reject("[RenderPipeline]\nPreset=Standard\n", "Missing [RenderPipeline].VSync");
        Reject("[RenderPipeline]\nVSync=false\n", "Missing [RenderPipeline].Preset");
        Reject("[RenderPipeline]\nPreset=Standard\nVSync=1\n", "expected true or false");
        Reject("[RenderPipeline]\nPreset=Standard\n[RenderPipeline\n", "Malformed");
        // The replaced sections are no longer part of the settings format.
        for (const auto section : {"Rendering", "Pipeline", "Pipeline.Custom"})
            Reject("[" + std::string(section) + "]\n", "Unknown Settings.ini section");
        for (const auto& [key, value] : {std::pair{"MaxGpuFramesInFlight", "0"}, {"MaxGpuFramesInFlight", "17"},
            {"MaxGpuFramesInFlight", "2junk"}, {"MaxPresentLatency", "0"}, {"BackBufferCount", "1"},
            {"BackBufferCount", "17"}, {"WaitForPresentation", "yes"}, {"AllowTearing", "1"}, {"WaitStrategy", "Sleep"}})
        {
            auto changed = custom;
            auto start = changed.find(std::string(key) + '=');
            auto end = changed.find('\n', start);
            changed.replace(start, end - start, std::string(key) + '=' + value);
            Reject(base + changed, key);
        }
        auto bounds = custom;
        for (const auto& [from, to] : {std::pair{"MaxGpuFramesInFlight=3", "MaxGpuFramesInFlight=16"},
            {"MaxPresentLatency=1", "MaxPresentLatency=16"}, {"BackBufferCount=2", "BackBufferCount=16"}})
            bounds.replace(bounds.find(from), std::string_view(from).size(), to);
        const auto maximum = Parse(base + bounds);
        Require(maximum.renderPipeline.maxGpuFramesInFlight == 16 && maximum.renderPipeline.maxPresentLatency == 16 &&
            maximum.renderPipeline.backBufferCount == 16, "Maximum supported limits rejected");
        Reject(base + custom + "[Sphere]\nUResolution=2\n", "UResolution");
        Reject(base + custom + "[SimplePaintShader_Body]\nBrightness=0\n", "SimplePaintShader_Body");
        std::cout << "Render pipeline presets, custom isolation/validation, independent VSync, and paint/sphere validation passed.\n";
        return 0;
    }
    catch (const std::exception& e) { std::cerr << e.what() << '\n'; return 1; }
}
