#pragma once

#include "Settings.h"
#include "JsonSerialization.h"

// Serialization declarations only: no validation, preset expansion, or callbacks.
NLOHMANN_DEFINE_TYPE_NON_INTRUSIVE(struct Settings::RenderPipeline,
    Preset, VSync, MaxGpuFramesInFlight, MaxPresentLatency, WaitForPresentation,
    BackBufferCount, AllowTearing, WaitStrategy)

NLOHMANN_DEFINE_TYPE_NON_INTRUSIVE(Settings::SphereMesh,
    UResolution, VResolution)

NLOHMANN_DEFINE_TYPE_NON_INTRUSIVE(Settings::Paint,
    BaseColor, Brightness, Shift, RotationDegrees, DarkPoint, LightPoint)

NLOHMANN_DEFINE_TYPE_NON_INTRUSIVE(Settings,
    RenderPipeline, SimplePaintShader_Axles, SimplePaintShader_Body,
    SimplePaintShader_Cabin, SimplePaintShader_Headlights, SimplePaintShader_Wheels,
    SimplePaintShader_Sphere, Sphere)
