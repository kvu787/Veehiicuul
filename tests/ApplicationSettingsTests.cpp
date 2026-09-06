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
    "[Pipeline.Custom]\nMaxGpuFramesInFlight=3\nMaxPresentLatency=1\nWaitForPresentation=false\n"
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
        for (const auto& mode : {std::string("Standard"), std::string("MinimizeInputLatency"),
            std::string("MaximizeFps"), std::string("Custom")})
        {
            const std::string prefix = "[Pipeline]\nMode=" + mode + "\n[Rendering]\nVSync=";
            const auto off = Parse(prefix + "false\n" + custom);
            const auto on = Parse(prefix + "true\n" + custom);
            Require(!off.vsync && on.vsync, "VSync was overridden by mode");
            Require(off.pipeline == on.pipeline, "VSync changed pipeline configuration");
            if (mode == "Standard") Require(off.pipeline == StandardPipeline, "Standard preset overridden");
            if (mode == "MinimizeInputLatency") Require(off.pipeline == MinimumLatencyPipeline, "Latency preset overridden");
            if (mode == "MaximizeFps")
            {
                Require(off.pipelineMode == PipelineMode::MaximizeFps && off.pipeline == MaximizeFpsPipeline,
                    "MaximizeFps preset not selected or overridden");
                Require(PipelineModeName(off.pipelineMode) == L"MaximizeFps", "MaximizeFps diagnostic name lost");
            }
            if (mode == "Custom")
            {
                Require(off.pipeline.maxGpuFramesInFlight == 3 && off.pipeline.backBufferCount == 2, "Independent buffer/frame counts lost");
                Require(!off.pipeline.waitForPresentation && off.pipeline.allowTearing && off.pipeline.waitStrategy == WaitStrategy::Spin,
                    "Custom settings not applied");
            }
            Require(on.sphereUResolution == 64 && on.sphereVResolution == 32, "Sphere defaults changed");
        }
        // Position, duplicate keys, unknown keys, and invalid values in inactive custom sections do not matter.
        const std::string inactive = "[Pipeline.Custom]\nMaxGpuFramesInFlight=garbage\nMaxGpuFramesInFlight=-3\nVSync=bad\nUnknown=\n";
        for (const auto mode : {"Standard", "MinimizeInputLatency", "MaximizeFps"})
        {
            const auto preset = "[Rendering]\nVSync=false\n[Pipeline]\nMode=" + std::string(mode) + "\n";
            const auto expected = Parse(preset);
            for (const auto& text : {inactive + preset, preset + inactive})
            {
                const auto result = Parse(text);
                Require(result.pipelineMode == expected.pipelineMode && result.pipeline == expected.pipeline,
                    "Inactive custom values affected preset");
            }
        }
        const std::string base = "[Pipeline]\nMode=Custom\n[Rendering]\nVSync=false\n";
        Reject(base, "Missing [Pipeline.Custom].MaxGpuFramesInFlight");
        Reject(base + custom + "VSync=true\n", "unknown setting");
        Reject(base + custom + "MaxGpuFramesInFlight=1\n", "duplicate setting");
        Reject(base + custom + "[Rendering]\nVSync=true\n", "duplicate setting");
        Reject("[Pipeline]\nMode=unknown\n[Rendering]\nVSync=false\n", "line 2");
        Reject("[Pipeline]\nMode=MaximizeFPS\n[Rendering]\nVSync=false\n", "MaximizeFps");
        Reject("[Pipeline]\nMode=Standard\n", "Missing [Rendering].VSync");
        Reject("[Rendering]\nVSync=false\n", "Missing [Pipeline].Mode");
        Reject("[Pipeline]\nMode=Standard\n[Rendering]\nVSync=1\n", "expected true or false");
        Reject("[Pipeline]\nMode=Standard\n[Pipeline.Custom\n", "Malformed");
        for (const auto& [key, value] : {std::pair{"MaxGpuFramesInFlight", "0"}, {"MaxGpuFramesInFlight", "17"},
            {"MaxGpuFramesInFlight", "2junk"}, {"MaxPresentLatency", "0"}, {"BackBufferCount", "1"},
            {"BackBufferCount", "17"}, {"WaitForPresentation", "yes"}, {"WaitStrategy", "Sleep"}})
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
        Require(maximum.pipeline.maxGpuFramesInFlight == 16 && maximum.pipeline.maxPresentLatency == 16 &&
            maximum.pipeline.backBufferCount == 16, "Maximum supported limits rejected");
        Reject(base + custom + "[Sphere]\nUResolution=2\n", "UResolution");
        Reject(base + custom + "[SimplePaintShader_Body]\nBrightness=0\n", "SimplePaintShader_Body");
        std::cout << "Pipeline presets, custom isolation/validation, independent VSync, and paint/sphere validation passed.\n";
        return 0;
    }
    catch (const std::exception& e) { std::cerr << e.what() << '\n'; return 1; }
}
