cbuffer Screen : register(b0) { float2 ScreenSize; };
Texture2D<float4> Atlas : register(t0);
SamplerState AtlasSampler : register(s0);
struct Vertex { float2 Position : POSITION; float2 Texture : TEXCOORD; float4 Color : COLOR; };
struct Pixel { float4 Position : SV_POSITION; float2 Texture : TEXCOORD; float4 Color : COLOR; };
Pixel VertexMain(Vertex input)
{
    Pixel output;
    output.Position = float4(input.Position.x / ScreenSize.x * 2 - 1, 1 - input.Position.y / ScreenSize.y * 2, 0, 1);
    output.Texture = input.Texture;
    output.Color = input.Color;
    return output;
}
float4 PixelMain(Pixel input) : SV_TARGET { return input.Color * Atlas.Sample(AtlasSampler, input.Texture); }
