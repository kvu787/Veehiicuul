#pragma once

#include <DirectXMath.h>

#include <cstdint>
#include <vector>

struct Vertex
{
    DirectX::XMFLOAT3 position;
    DirectX::XMFLOAT3 normal;
};

struct MeshData
{
    std::vector<Vertex> vertices;
    std::vector<std::uint32_t> indices;
};

[[nodiscard]] MeshData CreateCube(float size);
[[nodiscard]] MeshData CreateSphere(float radius, std::uint32_t latitudeSegments, std::uint32_t longitudeSegments);
[[nodiscard]] MeshData CreateGroundPlane(float width, float depth);
