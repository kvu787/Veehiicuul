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
        std::string Preset;
        bool VSync;
        std::int32_t MaxGpuFramesInFlight;
        std::int32_t MaxPresentLatency;
        bool WaitForPresentation;
        std::int32_t BackBufferCount;
        bool AllowTearing;
        std::string WaitStrategy;
        bool operator==(const RenderPipeline&) const = default;
    };

    struct SphereMesh
    {
        std::int32_t UResolution;
        std::int32_t VResolution;
        bool operator==(const SphereMesh&) const = default;
    };

    struct Paint
    {
        std::vector<double> BaseColor; // This should be exactly three sRGB channels. Validation will check the length.
        double Brightness;
        double Shift;
        double RotationDegrees;
        double DarkPoint;
        double LightPoint;
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
