#include "Geometry.h"

#include <array>
#include <cmath>
#include <numbers>

using DirectX::XMFLOAT3;

MeshData CreateCube(const float size)
{
    MeshData mesh;
    mesh.vertices.reserve(24);
    mesh.indices.reserve(36);

    const float half = size * 0.5f;

    struct Face
    {
        XMFLOAT3 normal;
        std::array<XMFLOAT3, 4> corners;
    };

    const std::array faces = {
        Face{{0.0f, 0.0f, 1.0f}, {{{-half, -half, half}, {half, -half, half}, {half, half, half}, {-half, half, half}}}},
        Face{{0.0f, 0.0f, -1.0f}, {{{half, -half, -half}, {-half, -half, -half}, {-half, half, -half}, {half, half, -half}}}},
        Face{{1.0f, 0.0f, 0.0f}, {{{half, -half, half}, {half, -half, -half}, {half, half, -half}, {half, half, half}}}},
        Face{{-1.0f, 0.0f, 0.0f}, {{{-half, -half, -half}, {-half, -half, half}, {-half, half, half}, {-half, half, -half}}}},
        Face{{0.0f, 1.0f, 0.0f}, {{{-half, half, half}, {half, half, half}, {half, half, -half}, {-half, half, -half}}}},
        Face{{0.0f, -1.0f, 0.0f}, {{{-half, -half, -half}, {half, -half, -half}, {half, -half, half}, {-half, -half, half}}}},
    };

    for (const Face& face : faces)
    {
        const std::uint32_t baseIndex = static_cast<std::uint32_t>(mesh.vertices.size());
        for (const XMFLOAT3& corner : face.corners)
        {
            mesh.vertices.push_back({corner, face.normal});
        }

        mesh.indices.insert(mesh.indices.end(), {
            baseIndex, baseIndex + 1, baseIndex + 2,
            baseIndex, baseIndex + 2, baseIndex + 3,
        });
    }

    return mesh;
}

MeshData CreateSphere(
    const float radius,
    const std::uint32_t latitudeSegments,
    const std::uint32_t longitudeSegments)
{
    MeshData mesh;
    if (latitudeSegments < 3 || longitudeSegments < 3)
    {
        return mesh;
    }

    const std::uint32_t ringVertexCount = longitudeSegments + 1;
    mesh.vertices.reserve(static_cast<std::size_t>(latitudeSegments + 1) * ringVertexCount);
    mesh.indices.reserve(static_cast<std::size_t>(latitudeSegments) * longitudeSegments * 6);

    for (std::uint32_t latitude = 0; latitude <= latitudeSegments; ++latitude)
    {
        const float theta = std::numbers::pi_v<float> * static_cast<float>(latitude) /
            static_cast<float>(latitudeSegments);
        const float ringRadius = std::sin(theta);
        const float y = std::cos(theta);

        for (std::uint32_t longitude = 0; longitude <= longitudeSegments; ++longitude)
        {
            const float phi = 2.0f * std::numbers::pi_v<float> * static_cast<float>(longitude) /
                static_cast<float>(longitudeSegments);
            const XMFLOAT3 normal{
                ringRadius * std::cos(phi),
                y,
                ringRadius * std::sin(phi),
            };
            mesh.vertices.push_back({
                {normal.x * radius, normal.y * radius, normal.z * radius},
                normal,
            });
        }
    }

    for (std::uint32_t latitude = 0; latitude < latitudeSegments; ++latitude)
    {
        for (std::uint32_t longitude = 0; longitude < longitudeSegments; ++longitude)
        {
            const std::uint32_t topLeft = latitude * ringVertexCount + longitude;
            const std::uint32_t bottomLeft = (latitude + 1) * ringVertexCount + longitude;

            if (latitude != 0)
            {
                mesh.indices.insert(mesh.indices.end(), {
                    topLeft, bottomLeft, topLeft + 1,
                });
            }

            if (latitude + 1 != latitudeSegments)
            {
                mesh.indices.insert(mesh.indices.end(), {
                    topLeft + 1, bottomLeft, bottomLeft + 1,
                });
            }
        }
    }

    return mesh;
}

MeshData CreateGroundPlane(const float width, const float depth)
{
    const float halfWidth = width * 0.5f;
    const float halfDepth = depth * 0.5f;

    MeshData mesh;
    mesh.vertices = {
        {{-halfWidth, 0.0f, -halfDepth}, {0.0f, 1.0f, 0.0f}},
        {{-halfWidth, 0.0f, halfDepth}, {0.0f, 1.0f, 0.0f}},
        {{halfWidth, 0.0f, halfDepth}, {0.0f, 1.0f, 0.0f}},
        {{halfWidth, 0.0f, -halfDepth}, {0.0f, 1.0f, 0.0f}},
    };
    mesh.indices = {0, 1, 2, 0, 2, 3};
    return mesh;
}
