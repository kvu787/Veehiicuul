cbuffer BackgroundConstants : register(b0)
{
    float horizontalUvScale;
};

Texture2D<float4> backgroundTexture : register(t0);
SamplerState backgroundSampler : register(s0);

struct PixelInput
{
    float4 position : SV_POSITION;
    float2 textureCoordinate : TEXCOORD0;
};

PixelInput VSMain(const uint vertexId : SV_VERTEXID)
{
    const float2 corner = float2((vertexId << 1u) & 2u, vertexId & 2u);

    PixelInput output;
    output.position = float4(corner.x * 2.0f - 1.0f, 1.0f - corner.y * 2.0f, 0.0f, 1.0f);
    output.textureCoordinate = float2(
        0.5f + (corner.x - 0.5f) * horizontalUvScale,
        corner.y);
    return output;
}

float4 PSMain(const PixelInput input) : SV_TARGET
{
    if (any(input.textureCoordinate < 0.0f) || any(input.textureCoordinate > 1.0f))
    {
        // The baked image's top-left texel is the scene's neutral clear color.
        // Reuse it as a side matte when a window is wider than the 32:9 bake.
        return backgroundTexture.Load(int3(0, 0, 0));
    }
    return backgroundTexture.Sample(backgroundSampler, input.textureCoordinate);
}
