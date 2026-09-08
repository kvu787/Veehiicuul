#pragma once

#include <string>
#include <vector>

// The complete Settings.json contract. Members use the JSON property names.
// Every property is required when loading a file. Initializers are convenient
// starting values for C++ callers and match the shipped configuration.
// No derived renderer data or JSON-library types belong in this model.
struct ApplicationSettings
{
    struct RenderPipeline
    {
        std::string Preset = "MinimizeInputLatency"; // MinimizeInputLatency, Standard, MaximizeFps, Custom.
        bool VSync = false;
        // These six controls affect the renderer only with Preset == "Custom".
        // Counts remain double until validation, so fractions cannot be truncated on input.
        double MaxGpuFramesInFlight = 2; // Whole number in [1, 16].
        double MaxPresentLatency = 2;   // Whole number in [1, 16].
        bool WaitForPresentation = true;
        double BackBufferCount = 3;     // Whole number in [2, 16].
        bool AllowTearing = false;
        std::string WaitStrategy = "Event"; // Event or Spin.
        bool operator==(const RenderPipeline&) const = default;
    };

    struct SphereMesh
    {
        double UResolution = 64; // Whole number in [3, 512].
        double VResolution = 32; // Whole number in [2, 512].
        bool operator==(const SphereMesh&) const = default;
    };

    struct Paint
    {
        // Keep the complete input array until validation checks its length.
        std::vector<double> BaseColor{0.107, 0.223, 0.578}; // Exactly three sRGB channels.
        double Brightness = 0.5;
        double Shift = 0.0;
        double RotationDegrees = 0.0;
        double DarkPoint = 0.0;
        double LightPoint = 0.8;
        // RGB and Brightness: [1/1024, 1-1/1024]; Shift and DarkPoint: [0, 1-1/1024].
        // RotationDegrees: [0, 360); LightPoint: [1/1024, 1]. All numbers must be finite.
        bool operator==(const Paint&) const = default;
    };

    RenderPipeline RenderPipeline;
    Paint SimplePaintShader_Axles{.BaseColor = {0.678429127, 0.678431321, 0.678431321}};
    Paint SimplePaintShader_Body{.BaseColor = {0.0009765625, 0.436627067, 0.9990234375}};
    Paint SimplePaintShader_Cabin{.BaseColor = {0.506386429, 0.756053146, 0.9990234375}};
    Paint SimplePaintShader_Headlights{.BaseColor = {0.9990234375, 0.815686771, 0.0009765625}};
    Paint SimplePaintShader_Wheels{.BaseColor = {0.345097446, 0.345097446, 0.345097446}};
    Paint SimplePaintShader_Sphere{.BaseColor = {0.107, 0.223, 0.578}, .Brightness = 0.126, .LightPoint = 1.0};
    SphereMesh Sphere;
    bool operator==(const ApplicationSettings&) const = default;
};
