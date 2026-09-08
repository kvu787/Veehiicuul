#pragma once

#include "SimplePaint/Material.h"
#include <array>
#include <cstdint>
#include <filesystem>
#include <istream>
#include <string_view>

enum class RenderPipelinePreset { MinimizeInputLatency, Standard, MaximizeFps, Custom };
enum class WaitStrategy { Event, Spin };

struct RenderPipelineSettings
{
    std::uint32_t maxGpuFramesInFlight = 2;
    std::uint32_t maxPresentLatency = 2;
    bool waitForPresentation = true;
    std::uint32_t backBufferCount = 3;
    bool allowTearing = false;
    WaitStrategy waitStrategy = WaitStrategy::Event;
    bool operator==(const RenderPipelineSettings&) const = default;
};

inline constexpr RenderPipelineSettings StandardRenderPipeline{};
inline constexpr RenderPipelineSettings MinimizeInputLatencyRenderPipeline{
    .maxGpuFramesInFlight = 1,
    .maxPresentLatency = 1,
    .waitForPresentation = true,
    .backBufferCount = 2,
    .allowTearing = true,
    .waitStrategy = WaitStrategy::Spin,
};
inline constexpr RenderPipelineSettings MaximizeFpsRenderPipeline{
    .maxGpuFramesInFlight = 3,
    .maxPresentLatency = 2, // Inactive without presentation admission waiting.
    .waitForPresentation = false,
    .backBufferCount = 4,
    .allowTearing = true,
    .waitStrategy = WaitStrategy::Spin,
};

struct ApplicationSettings
{
    RenderPipelinePreset renderPipelinePreset = RenderPipelinePreset::Standard;
    RenderPipelineSettings renderPipeline = StandardRenderPipeline;
    bool vsync = false;
    std::uint32_t sphereUResolution = 64;
    std::uint32_t sphereVResolution = 32;
    std::array<SimplePaint::GpuMaterial, 6> paintMaterials{};
};

[[nodiscard]] ApplicationSettings ParseApplicationSettings(std::istream& input);
[[nodiscard]] ApplicationSettings LoadApplicationSettings(const std::filesystem::path& path);
[[nodiscard]] std::filesystem::path ModuleDirectory();
[[nodiscard]] std::wstring_view RenderPipelinePresetName(RenderPipelinePreset preset);
void ValidateRenderPipelineSettings(const RenderPipelineSettings& settings);
