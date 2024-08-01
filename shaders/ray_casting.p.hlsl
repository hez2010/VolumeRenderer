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

float4 mix_color(float4 c1, float4 c2)
{
    float4 c = float4(0, 0, 0, 0);
    c.r = 1 - sqrt((pow(1 - c1.r, 2) + pow(1 - c2.r, 2)) / 2);
    c.g = 1 - sqrt((pow(1 - c1.g, 2) + pow(1 - c2.g, 2)) / 2);
    c.b = 1 - sqrt((pow(1 - c1.b, 2) + pow(1 - c2.b, 2)) / 2);
    c.a = 1 - sqrt((pow(1 - c1.a, 2) + pow(1 - c2.a, 2)) / 2);
    return c;
}

float3 rgb2yuv(float3 rgb)
{
    float3x3 m = {
        0.2126f, 0.7152f, 0.0722f,
        -0.114572f, -0.385428f, 0.5f,
        0.5f, -0.454153f, -0.045847f
    };

    return mul(m, rgb);
}

float mix_value(float c1, float c2)
{
    float c = 0;
    c = 1 - sqrt((pow(1 - c1, 2) + pow(1 - c2, 2)) / 2);
    return c;
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

    float4 color = float4(0.0f, 0.0f, 0.0f, 0.0f);
    float acum_length = 0.0;

    [loop]
    while (acum_length <= ray_length && color.a < 1.0)
    {
        float r = RawData3.Sample(rawSampler3, position).r;
        float g = RawData2.Sample(rawSampler2, position).r;
        float b = RawData1.Sample(rawSampler1, position).r;
        float4 c = float4(r, g, b, r < 0.3 || b < 0.3 || g < 0.3 ? 0 : rgb2yuv(float3(r, g, b)).r);
        color.rgb = c.a * c.rgb + (1 - c.a) * color.a * color.rgb;
        color.a = c.a + (1 - c.a) * color.a;
        acum_length += step;
        position += delta;
    }

    color.rgb = color.a * color.rgb + (1 - color.a) * float3(1.0, 1.0, 1.0);
    color.a = 1.0;
    
    return color;
}