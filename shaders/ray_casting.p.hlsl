struct PS_IN
{
    float4 pos : SV_POSITION;
};

cbuffer PS_CONSTANT_BUFFER : register(b1)
{
    float step;
    float padding;
    float2 screen;
};

Texture2D Front : register(t0);
Texture2D Back : register(t1);
Texture3D RawData : register(t2);
Texture1D TfFunc : register(t3);

SamplerState faceSampler : register(s0);
SamplerState rawSampler : register(s1);
SamplerState tfSampler : register(s2);

float4 main(PS_IN input) : SV_TARGET
{
    float2 texC = input.pos.xy / screen;

    float3 entryPoint = Front.Sample(faceSampler, texC).rgb;
    float3 exitPoint = Back.Sample(faceSampler, texC).rgb;
    float3 ray = exitPoint - entryPoint;
    float ray_length = length(ray);
    float3 delta = step * (ray / ray_length);
    float3 position = entryPoint.xyz;
    float4 color = float4(0.0f, 0.0f, 0.0f, 0.0f);
    
    float acum_length = 0.0;
    
    [loop]
    while (acum_length <= ray_length && color.a < 1.0)
    {
        float intensity = RawData.Sample(rawSampler, position).r;
        float4 c = TfFunc.Sample(tfSampler, intensity);
        color.rgb = c.a * c.rgb + (1 - c.a) * color.a * color.rgb;
        color.a = c.a + (1 - c.a) * color.a;
        acum_length += step;
        position += delta;
    }
    
    color.xyz = color.a * color.rgb + (1 - color.a) * float3(1.0, 1.0, 1.0);
    color.w = 1.0;
    
    return color;
}