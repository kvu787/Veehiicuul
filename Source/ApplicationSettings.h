#pragma once

#include <cstdint>
#include <string>
#include <vector>

// The complete Settings.json contract. Members use the JSON property names.
// Every property is required when loading a file. Configured values come only
// from Settings.json; this type declares no configuration defaults.
// No derived renderer data or JSON-library types belong in this model.
struct ApplicationSettings
{
    struct RenderPipeline
    {
        std::string Preset; // MinimizeInputLatency, Standard, MaximizeFps, Custom.
        bool VSync;
        // These six controls affect the renderer only with Preset == "Custom".
        std::int32_t MaxGpuFramesInFlight; // Whole number in [1, 16].
        std::int32_t MaxPresentLatency;   // Whole number in [1, 16].
        bool WaitForPresentation;
        std::int32_t BackBufferCount;     // Whole number in [2, 16].
        bool AllowTearing;
        std::string WaitStrategy; // Event or Spin.
        bool operator==(const RenderPipeline&) const = default;
    };

    struct SphereMesh
    {
        std::int32_t UResolution; // Whole number in [3, 512].
        std::int32_t VResolution; // Whole number in [2, 512].
        bool operator==(const SphereMesh&) const = default;
    };

    struct Paint
    {
        // Keep the complete input array until validation checks its length.
        std::vector<double> BaseColor; // Exactly three sRGB channels.
        double Brightness;
        double Shift;
        double RotationDegrees;
        double DarkPoint;
        double LightPoint;
        // RGB and Brightness: [1/1024, 1-1/1024]; Shift and DarkPoint: [0, 1-1/1024].
        // RotationDegrees: [0, 360); LightPoint: [1/1024, 1]. All numbers must be finite.
        bool operator==(const Paint&) const = default;
    };

    RenderPipeline RenderPipeline;
    Paint SimplePaintShader_Axles;
    Paint SimplePaintShader_Body;
    Paint SimplePaintShader_Cabin;
    Paint SimplePaintShader_Headlights;
    Paint SimplePaintShader_Wheels;
    Paint SimplePaintShader_Sphere;
    SphereMesh Sphere;
    bool operator==(const ApplicationSettings&) const = default;
};
