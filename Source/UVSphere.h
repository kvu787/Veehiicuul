#pragma once

#include "generated/CarMesh.generated.h"

#include <cmath>
#include <cstdint>
#include <numbers>
#include <stdexcept>
#include <vector>

namespace UVSphere
{
struct Mesh
{
    // Use the car's position/normal/material layout for the shared paint shader.
    std::vector<GeneratedCarMesh::Vertex> vertices;
    std::vector<std::uint32_t> indices;
};

inline Mesh Generate(
    const std::uint32_t uResolution,
    const std::uint32_t vResolution,
    const std::uint32_t materialIndex)
{
    if (uResolution < 3 || uResolution > 512 || vResolution < 2 || vResolution > 512)
    {
        throw std::invalid_argument("Sphere resolution requires U in [3, 512] and V in [2, 512].");
    }

    Mesh mesh;
    mesh.vertices.reserve(2 + uResolution * (vResolution - 1));
    mesh.indices.reserve(6 * uResolution * (vResolution - 1));
    mesh.vertices.push_back({0.0f, 1.0f, 0.0f, 0.0f, 1.0f, 0.0f, materialIndex});
    for (std::uint32_t v = 1; v < vResolution; ++v)
    {
        const float latitude = std::numbers::pi_v<float> * static_cast<float>(v) /
            static_cast<float>(vResolution);
        for (std::uint32_t u = 0; u < uResolution; ++u)
        {
            const float longitude = 2.0f * std::numbers::pi_v<float> * static_cast<float>(u) /
                static_cast<float>(uResolution);
            const float x = std::sin(latitude) * std::cos(longitude);
            const float y = std::cos(latitude);
            const float z = std::sin(latitude) * std::sin(longitude);
            mesh.vertices.push_back({x, y, z, x, y, z, materialIndex});
        }
    }
    const auto bottom = static_cast<std::uint32_t>(mesh.vertices.size());
    mesh.vertices.push_back({0.0f, -1.0f, 0.0f, 0.0f, -1.0f, 0.0f, materialIndex});

    // Wrap longitude without duplicating the seam. Separate pole fans avoid
    // zero-area triangles, including at the minimum V resolution.
    for (std::uint32_t u = 0; u < uResolution; ++u)
    {
        const std::uint32_t next = (u + 1) % uResolution;
        mesh.indices.insert(mesh.indices.end(), {0, 1 + next, 1 + u});
        for (std::uint32_t v = 0; v + 2 < vResolution; ++v)
        {
            const std::uint32_t a = 1 + v * uResolution + u;
            const std::uint32_t b = 1 + v * uResolution + next;
            const std::uint32_t c = a + uResolution;
            const std::uint32_t d = b + uResolution;
            mesh.indices.insert(mesh.indices.end(), {a, b, c, b, d, c});
        }
        const std::uint32_t lastRing = 1 + (vResolution - 2) * uResolution;
        mesh.indices.insert(mesh.indices.end(), {bottom, lastRing + u, lastRing + next});
    }
    return mesh;
}
}
