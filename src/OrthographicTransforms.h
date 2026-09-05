#pragma once

#include <DirectXMath.h>
#include <array>

namespace Orthographic
{
// Projection is always orthographic, and object/view transforms are affine.
// Only clip XYZ is stored; the vertex shader supplies the invariant clip W = 1.
struct ObjectTransforms
{
    std::array<DirectX::XMFLOAT4, 3> worldToClip;
    std::array<DirectX::XMFLOAT4, 3> normalToView;
};
static_assert(sizeof(ObjectTransforms) == 96);

struct Projection
{
    DirectX::XMFLOAT3 scale;
    float depthOffset;
};

inline Projection MakeProjection(float width, float height, float nearZ, float farZ)
{
    const float depthScale = 1.0f / (nearZ - farZ);
    return {{2.0f / width, 2.0f / height, depthScale}, depthScale * nearZ};
}

inline ObjectTransforms BuildObjectTransforms(
    DirectX::FXMMATRIX worldView, const Projection& projection)
{
    const DirectX::XMMATRIX columns = DirectX::XMMatrixTranspose(worldView);
    ObjectTransforms result;
    for (unsigned axis = 0; axis < 3; ++axis)
    {
        // Normals use XYZ only. As before, object scales must be uniform.
        DirectX::XMStoreFloat4(&result.normalToView[axis], columns.r[axis]);
    }
    DirectX::XMStoreFloat4(&result.worldToClip[0],
        DirectX::XMVectorScale(columns.r[0], projection.scale.x));
    DirectX::XMStoreFloat4(&result.worldToClip[1],
        DirectX::XMVectorScale(columns.r[1], projection.scale.y));
    DirectX::XMStoreFloat4(&result.worldToClip[2],
        DirectX::XMVectorAdd(
            DirectX::XMVectorScale(columns.r[2], projection.scale.z),
            DirectX::XMVectorSet(0.0f, 0.0f, 0.0f, projection.depthOffset)));
    return result;
}
}
