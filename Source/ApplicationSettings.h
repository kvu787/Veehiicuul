#pragma once

#include "SimplePaint/Material.h"
#include <array>
#include <cstdint>
#include <filesystem>
#include <istream>
#include <string_view>

enum class PipelineMode { MinimizeInputLatency, Standard, MaximizeFps, Custom };
enum class WaitStrategy { Event, Spin };

struct PipelineSettings
{
    std::uint32_t maxGpuFramesInFlight = 2;
    std::uint32_t maxPresentLatency = 2;
    bool waitForPresentation = true;
    std::uint32_t backBufferCount = 3;
    bool allowTearing = false;
    WaitStrategy waitStrategy = WaitStrategy::Event;
    bool operator==(const PipelineSettings&) const = default;
};

inline constexpr PipelineSettings StandardPipeline{};
inline constexpr PipelineSettings MinimumLatencyPipeline{
    .maxGpuFramesInFlight = 1,
    .maxPresentLatency = 1,
    .waitForPresentation = true,
    .backBufferCount = 2,
    .allowTearing = true,
    .waitStrategy = WaitStrategy::Spin,
};
inline constexpr PipelineSettings MaximizeFpsPipeline{
    .maxGpuFramesInFlight = 3,
    .maxPresentLatency = 2, // Inactive without presentation admission waiting.
    .waitForPresentation = false,
    .backBufferCount = 4,
    .allowTearing = true,
    .waitStrategy = WaitStrategy::Spin,
};

struct ApplicationSettings
{
    PipelineMode pipelineMode = PipelineMode::Standard;
    PipelineSettings pipeline = StandardPipeline;
    bool vsync = false;
    std::uint32_t sphereUResolution = 64;
    std::uint32_t sphereVResolution = 32;
    std::array<SimplePaint::GpuMaterial, 6> paintMaterials{};
};

[[nodiscard]] ApplicationSettings ParseApplicationSettings(std::istream& input);
[[nodiscard]] ApplicationSettings LoadApplicationSettings(const std::filesystem::path& path);
[[nodiscard]] std::filesystem::path ModuleDirectory();
[[nodiscard]] std::wstring_view PipelineModeName(PipelineMode mode);
void ValidatePipelineSettings(const PipelineSettings& settings);
