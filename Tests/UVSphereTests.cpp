#include "UVSphere.h"

#include <algorithm>
#include <iostream>
#include <unordered_map>

namespace
{
void Require(const bool condition, const char* message)
{
    if (!condition)
    {
        throw std::runtime_error(message);
    }
}

void CheckMesh(const std::uint32_t u, const std::uint32_t v)
{
    const auto mesh = UVSphere::Generate(u, v, 5);
    Require(mesh.vertices.size() == 2 + u * (v - 1), "Unexpected vertex count");
    Require(mesh.indices.size() == 6 * u * (v - 1), "Unexpected index count");
    for (const auto& vertex : mesh.vertices)
    {
        const float squaredLength = vertex.positionX * vertex.positionX +
            vertex.positionY * vertex.positionY + vertex.positionZ * vertex.positionZ;
        Require(std::abs(squaredLength - 1.0f) < 1.0e-5f, "Vertex is not on the unit sphere");
        Require(vertex.normalX == vertex.positionX && vertex.normalY == vertex.positionY &&
            vertex.normalZ == vertex.positionZ, "Normal is not radial");
        Require(vertex.materialIndex == 5, "Wrong sphere material");
    }

    struct Edge { unsigned count = 0; int direction = 0; };
    std::unordered_map<std::uint64_t, Edge> edges;
    for (std::size_t i = 0; i < mesh.indices.size(); i += 3)
    {
        for (std::size_t j = 0; j < 3; ++j)
        {
            const auto a = mesh.indices[i + j];
            const auto b = mesh.indices[i + (j + 1) % 3];
            Require(a < mesh.vertices.size() && b < mesh.vertices.size(), "Index out of range");
            const std::uint64_t key = (static_cast<std::uint64_t>(std::min(a, b)) << 32) |
                std::max(a, b);
            auto& edge = edges[key];
            ++edge.count;
            edge.direction += a < b ? 1 : -1;
        }
        const auto& a = mesh.vertices[mesh.indices[i]];
        const auto& b = mesh.vertices[mesh.indices[i + 1]];
        const auto& c = mesh.vertices[mesh.indices[i + 2]];
        const double bx = b.positionX - a.positionX, by = b.positionY - a.positionY,
            bz = b.positionZ - a.positionZ;
        const double cx = c.positionX - a.positionX, cy = c.positionY - a.positionY,
            cz = c.positionZ - a.positionZ;
        const double outward = (by * cz - bz * cy) * a.positionX +
            (bz * cx - bx * cz) * a.positionY + (bx * cy - by * cx) * a.positionZ;
        Require(outward > 0.0, "Degenerate or inward-facing triangle");
    }
    for (const auto& [key, edge] : edges)
    {
        static_cast<void>(key);
        Require(edge.count == 2 && edge.direction == 0, "Mesh has an open or inconsistent seam");
    }
    Require(mesh.vertices.size() + mesh.indices.size() / 3 == edges.size() + 2,
        "Mesh topology is not spherical");
}
}

int main()
{
    try
    {
        CheckMesh(3, 2);
        CheckMesh(7, 5);
        CheckMesh(64, 32);
        CheckMesh(512, 512); // Exercises indices beyond 16 bits.
        for (const auto [u, v] : {std::pair{2u, 32u}, {64u, 1u}, {513u, 32u}, {64u, 513u}})
        {
            bool rejected = false;
            try { static_cast<void>(UVSphere::Generate(u, v, 5)); }
            catch (const std::invalid_argument&) { rejected = true; }
            Require(rejected, "Invalid resolution was accepted");
        }
        std::cout << "UV sphere geometry checks passed.\n";
        return 0;
    }
    catch (const std::exception& error)
    {
        std::cerr << error.what() << '\n';
        return 1;
    }
}
