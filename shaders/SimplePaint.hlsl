// D3D12/HLSL port of K12 Simple Paint Shader by Kevin Vu.
// Source: https://github.com/kvu787/SimplePaintShaders/blob/793126205e028f06f635f23e87a9bac856bf669a/Godot/ShaderTest/Shaders/K12.gdshader
//
// Orthographic projection is a permanent renderer invariant. It reduces K12's
// view correction to a normalized view-space normal. Material coefficients,
// rotation trig, and range terms are precomputed on the CPU. The original slice/Schlick/remap sequence is reduced
// algebraically to one square root and one scalar divide per shifted pixel.

static const uint MaterialCount = 6u;

struct PaintMaterial
{
    // x = cos(-rotation), y = sin(-rotation), z = shift,
    // w = sqrt(1 - shift * shift)
    float4 paintWarp;
    // x = lightPoint - darkPoint, y = darkPoint,
    // z = facing cutoff, w = numerical epsilon
    float4 paintTone;
    float4 k1;
    float4 k2;
    float4 k3;
};

cbuffer ObjectConstants : register(b0)
{
    // Three packed columns of each affine transform; no perspective W column.
    float4 worldToClip[3];
    float4 normalToView[3];
};

cbuffer MaterialConstants : register(b1)
{
    PaintMaterial paintMaterials[MaterialCount];
};

struct VertexInput
{
    float3 position : POSITION;
    float3 normal : NORMAL;
    uint materialIndex : MATERIAL;
};

struct PixelInput
{
    float4 position : SV_POSITION;
    noperspective float3 paintNormal : NORMAL;
    nointerpolation uint materialIndex : MATERIAL;
};

PixelInput VSMain(const VertexInput input)
{
    PixelInput output;
    const float4 position = float4(input.position, 1.0f);
    output.position = float4(
        dot(position, worldToClip[0]),
        dot(position, worldToClip[1]),
        dot(position, worldToClip[2]), 1.0f);
    float3 normal = float3(
        dot(input.normal, normalToView[0].xyz),
        dot(input.normal, normalToView[1].xyz),
        dot(input.normal, normalToView[2].xyz));
    const PaintMaterial material = paintMaterials[input.materialIndex];
    // Every triangle has one material. This constant orthogonal rotation
    // commutes with interpolation and preserves length. Normalize per pixel.
    // Keep the zero-shift path free of rotation, including nonzero angles.
    [branch]
    if (material.paintWarp.z > material.paintTone.w)
    {
        normal.xy = float2(
            mad(normal.x, material.paintWarp.x, -normal.y * material.paintWarp.y),
            mad(normal.x, material.paintWarp.y, normal.y * material.paintWarp.x));
    }
    output.paintNormal = normal;
    output.materialIndex = input.materialIndex;
    return output;
}

float4 PSMain(const PixelInput input) : SV_TARGET
{
    const PaintMaterial material = paintMaterials[input.materialIndex];
    const float4 paintWarp = material.paintWarp;
    const float4 paintTone = material.paintTone;
    const float3 normal = normalize(input.paintNormal);
    float facing = 0.0f;

    if (normal.z >= paintTone.z)
    {
        if (paintWarp.z <= paintTone.w)
        {
            // The exact default fast path: shift is zero, so K12 reduces to N.z
            // and rotation has no visual effect.
            facing = normal.z;
        }
        else
        {
            const float sliceExtent = sqrt(saturate(mad(normal.x, normal.x, normal.z * normal.z)));
            const float denominator = max(
                sliceExtent - paintWarp.z * normal.x,
                paintTone.w);
            facing = saturate(
                sliceExtent * normal.z * paintWarp.w / denominator);
        }
    }

    const float tone = mad(paintTone.x, facing, paintTone.y);
    const float3 denominator = max(
        mad(material.k2.xyz, tone, material.k3.xyz),
        paintTone.www);
    const float3 color = material.k1.xyz * tone / denominator;
    return float4(saturate(color), 1.0f);
}
