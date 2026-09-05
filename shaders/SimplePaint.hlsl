// D3D12/HLSL port of K12 Simple Paint Shader by Kevin Vu.
// Source: https://github.com/kvu787/SimplePaintShaders/blob/793126205e028f06f635f23e87a9bac856bf669a/Godot/ShaderTest/Shaders/K12.gdshader
//
// The fixed orthographic camera reduces K12's view correction to a normalized
// view-space normal. Material coefficients, rotation trig, and range terms are
// precomputed on the CPU. The original slice/Schlick/remap sequence is reduced
// algebraically to one square root and one scalar divide per shifted pixel.

static const uint MaterialCount = 6u;

struct PaintMaterial
{
    float4 k1;
    float4 k2;
    float4 k3;
};

cbuffer CarConstants : register(b0)
{
    row_major float4x4 worldViewProjection;
    row_major float4x4 worldView;
    // x = cos(-rotation), y = sin(-rotation), z = shift,
    // w = sqrt(1 - shift * shift)
    float4 paintWarp;
    // x = lightPoint - darkPoint, y = darkPoint,
    // z = facing cutoff, w = numerical epsilon
    float4 paintTone;
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
    float3 viewNormal : NORMAL;
    nointerpolation uint materialIndex : MATERIAL;
};

PixelInput VSMain(const VertexInput input)
{
    PixelInput output;
    output.position = mul(float4(input.position, 1.0f), worldViewProjection);
    output.viewNormal = mul(float4(input.normal, 0.0f), worldView).xyz;
    output.materialIndex = input.materialIndex;
    return output;
}

float4 PSMain(const PixelInput input) : SV_TARGET
{
    const float3 normal = normalize(input.viewNormal);
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
            const float rotatedX = mad(normal.x, paintWarp.x, -normal.y * paintWarp.y);
            const float sliceExtent = sqrt(saturate(mad(rotatedX, rotatedX, normal.z * normal.z)));
            const float denominator = max(
                sliceExtent - paintWarp.z * rotatedX,
                paintTone.w);
            facing = saturate(
                sliceExtent * normal.z * paintWarp.w / denominator);
        }
    }

    const float tone = mad(paintTone.x, facing, paintTone.y);
    const PaintMaterial material = paintMaterials[input.materialIndex];
    const float3 denominator = max(
        mad(material.k2.xyz, tone, material.k3.xyz),
        paintTone.www);
    const float3 color = material.k1.xyz * tone / denominator;
    return float4(saturate(color), 1.0f);
}
