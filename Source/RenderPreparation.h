#pragma once

#include "ApplicationSettings.h"
#include "SimplePaint/Material.h"
#include <array>
#include <cstdint>
#include <string_view>

enum class RenderPipelinePreset { MinimizeInputLatency, Standard, MaximizeFps, Custom };
enum class WaitStrategy { Event, Spin };

// Derived renderer data. This is not the JSON settings model.
struct ResolvedRenderPipeline
{
    RenderPipelinePreset preset = RenderPipelinePreset::Custom;
    std::uint32_t maxGpuFramesInFlight = 2;
    std::uint32_t maxPresentLatency = 2;
    bool waitForPresentation = true;
    std::uint32_t backBufferCount = 3;
    bool allowTearing = false;
    WaitStrategy waitStrategy = WaitStrategy::Event;
    bool operator==(const ResolvedRenderPipeline&) const = default;
};

inline constexpr ResolvedRenderPipeline StandardRenderPipeline{.preset = RenderPipelinePreset::Standard};
inline constexpr ResolvedRenderPipeline MinimizeInputLatencyRenderPipeline{
    .preset = RenderPipelinePreset::MinimizeInputLatency,
    .maxGpuFramesInFlight = 1, .maxPresentLatency = 1, .waitForPresentation = true,
    .backBufferCount = 2, .allowTearing = true, .waitStrategy = WaitStrategy::Spin,
};
inline constexpr ResolvedRenderPipeline MaximizeFpsRenderPipeline{
    .preset = RenderPipelinePreset::MaximizeFps,
    .maxGpuFramesInFlight = 3, .maxPresentLatency = 2, .waitForPresentation = false,
    .backBufferCount = 4, .allowTearing = true, .waitStrategy = WaitStrategy::Spin,
};

// Resolution validates before narrowing numeric counts. It never changes the input.
[[nodiscard]] ResolvedRenderPipeline ResolveRenderPipeline(const struct ApplicationSettings::RenderPipeline& pipeline);
// Produces GPU constants in Axles, Body, Cabin, Headlights, Wheels, Sphere order.
[[nodiscard]] std::array<SimplePaint::GpuMaterial, 6> CompilePaintMaterials(const ApplicationSettings& settings);
[[nodiscard]] std::wstring_view RenderPipelinePresetName(RenderPipelinePreset preset);
