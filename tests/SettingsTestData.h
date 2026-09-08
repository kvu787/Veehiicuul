#pragma once

#include "ApplicationSettings.h"

// Explicit inputs for tests, independent of the shipped configuration.
inline ApplicationSettings MakeTestSettings()
{
    const ApplicationSettings::Paint paint{
        .BaseColor = {.2, .4, .6}, .Brightness = .5, .Shift = 0,
        .RotationDegrees = 0, .DarkPoint = .1, .LightPoint = .9};
    return {
        .RenderPipeline = {.Preset = "MinimizeInputLatency", .VSync = false,
            .MaxGpuFramesInFlight = 2, .MaxPresentLatency = 2,
            .WaitForPresentation = true, .BackBufferCount = 3,
            .AllowTearing = false, .WaitStrategy = "Event"},
        .SimplePaintShader_Axles = paint,
        .SimplePaintShader_Body = paint,
        .SimplePaintShader_Cabin = paint,
        .SimplePaintShader_Headlights = paint,
        .SimplePaintShader_Wheels = paint,
        .SimplePaintShader_Sphere = paint,
        .Sphere = {.UResolution = 16, .VResolution = 8},
    };
}
