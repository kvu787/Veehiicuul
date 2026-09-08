#pragma once

#include "ApplicationSettings.h"
#include "JsonSerialization.h"

// Serialization declarations only: no validation, preset expansion, or callbacks.
NLOHMANN_DEFINE_TYPE_NON_INTRUSIVE(ApplicationSettings::Pipeline,
    Preset, VSync, MaxGpuFramesInFlight, MaxPresentLatency, WaitForPresentation,
    BackBufferCount, AllowTearing, WaitStrategy)

NLOHMANN_DEFINE_TYPE_NON_INTRUSIVE(ApplicationSettings::SphereMesh,
    UResolution, VResolution)

NLOHMANN_DEFINE_TYPE_NON_INTRUSIVE(ApplicationSettings::Paint,
    BaseColor, Brightness, Shift, RotationDegrees, DarkPoint, LightPoint)

NLOHMANN_DEFINE_TYPE_NON_INTRUSIVE(ApplicationSettings,
    RenderPipeline, SimplePaintShader_Axles, SimplePaintShader_Body,
    SimplePaintShader_Cabin, SimplePaintShader_Headlights, SimplePaintShader_Wheels,
    SimplePaintShader_Sphere, Sphere)
