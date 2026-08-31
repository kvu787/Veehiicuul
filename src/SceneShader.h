#pragma once

namespace SceneShader
{
inline constexpr char Source[] = R"hlsl(
cbuffer ObjectConstants : register(b0)
{
    row_major float4x4 worldViewProjection;
    row_major float4x4 world;
    float4 baseColor;
    float4 lighting;
};

struct VertexInput
{
    float3 position : POSITION;
    float3 normal : NORMAL;
};

struct PixelInput
{
    float4 position : SV_POSITION;
    float3 worldNormal : NORMAL;
};

PixelInput VSMain(VertexInput input)
{
    PixelInput output;
    output.position = mul(float4(input.position, 1.0f), worldViewProjection);
    output.worldNormal = mul(float4(input.normal, 0.0f), world).xyz;
    return output;
}

float4 PSMain(PixelInput input) : SV_TARGET
{
    if (lighting.w > 0.5f)
    {
        return float4(baseColor.rgb, 1.0f);
    }

    const float3 normal = normalize(input.worldNormal);
    const float diffuse = saturate(dot(normal, normalize(lighting.xyz)));
    const float brightness = 0.28f + 0.72f * diffuse;
    return float4(baseColor.rgb * brightness, 1.0f);
}
)hlsl";
}
