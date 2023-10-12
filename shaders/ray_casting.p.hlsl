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
Texture3D RawData1 : register(t2);
Texture1D TfFunc1 : register(t3);
Texture3D RawData2 : register(t4);
Texture1D TfFunc2 : register(t5);
Texture3D RawData3 : register(t6);
Texture1D TfFunc3 : register(t7);

SamplerState faceSampler : register(s0);
SamplerState rawSampler1 : register(s1);
SamplerState tfSampler1 : register(s2);
SamplerState rawSampler2 : register(s3);
SamplerState tfSampler2 : register(s4);
SamplerState rawSampler3 : register(s5);
SamplerState tfSampler3 : register(s6);

float4 ray_casting(float ray_length, float3 delta, float3 position, Texture3D rawData, SamplerState rawSampler, Texture1D tfFunc, SamplerState tfSampler)
{
    float4 color = float4(0.0f, 0.0f, 0.0f, 0.0f);
    float acum_length = 0.0;

    [loop]
    while (acum_length <= ray_length && color.a < 1.0)
    {
        float intensity = rawData.Sample(rawSampler, position).r;
        float4 c = tfFunc.Sample(tfSampler, intensity);

        color.rgb = c.a * c.rgb + (1 - c.a) * color.a * color.rgb;
        color.a = c.a + (1 - c.a) * color.a;
        acum_length += step;
        position += delta;
    }

    color.rgb = color.a * color.rgb + (1 - color.a) * float3(1.0, 1.0, 1.0);
    return color;
}

float4 main(PS_IN input) : SV_TARGET
{
    float2 texC = input.pos.xy / screen;

    float3 entryPoint = Front.Sample(faceSampler, texC).rgb;
    float3 exitPoint = Back.Sample(faceSampler, texC).rgb;
    float3 ray = normalize(exitPoint - entryPoint);
    float ray_length = length(ray);
    float3 delta = step * (ray / ray_length);
    float3 position = entryPoint.xyz;
    
    float4 color1 = ray_casting(ray_length, delta, position, RawData1, rawSampler1, TfFunc1, tfSampler1);
    float4 color2 = ray_casting(ray_length, delta, position, RawData2, rawSampler2, TfFunc2, tfSampler3);
    float4 color3 = ray_casting(ray_length, delta, position, RawData3, rawSampler3, TfFunc3, tfSampler3);
    
    float4 color =
        color1.a >= color2.a && color1.a >= color3.a
        ? color1
        : color2.a >= color1.a && color2.a >= color3.a
        ? color2
        : color3;

    color.a = 1.0;
    
    return color;
}