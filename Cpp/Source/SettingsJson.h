#pragma once

#include "Settings.h"
#include "JsonDeserialization.h"

// Required-field input mappings only: no validation, preset expansion, or callbacks.
// nlohmann 3.12 has no input-only mapping macro. Reuse its field expansion
// without generating to_json overloads, and keep this helper local to this header.
#define DEFINE_SETTINGS_FROM_JSON(Type, ...) \
    inline void from_json(const JsonIO::Json& nlohmann_json_j, Type& nlohmann_json_t) \
    { NLOHMANN_JSON_EXPAND(NLOHMANN_JSON_PASTE(NLOHMANN_JSON_FROM, __VA_ARGS__)) }
DEFINE_SETTINGS_FROM_JSON(struct Settings::RenderPipeline,
    Preset, VSync, MaxGpuFramesInFlight, MaxPresentLatency, WaitForPresentation,
    BackBufferCount, AllowTearing, WaitStrategy)

DEFINE_SETTINGS_FROM_JSON(Settings::SphereMesh,
    UResolution, VResolution)

DEFINE_SETTINGS_FROM_JSON(Settings::Paint,
    BaseColor, Brightness, Shift, RotationDegrees, DarkPoint, LightPoint)

DEFINE_SETTINGS_FROM_JSON(Settings,
    RenderPipeline, SimplePaintShader_Axles, SimplePaintShader_Body,
    SimplePaintShader_Cabin, SimplePaintShader_Headlights, SimplePaintShader_Wheels,
    SimplePaintShader_Sphere, Sphere)

#undef DEFINE_SETTINGS_FROM_JSON
