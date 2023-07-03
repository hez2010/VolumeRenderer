cbuffer VS_CONSTANT_BUFFER : register(b0)
{
    float4x4 mvp;
};

struct VS_IN
{
    float3 pos : POSITION;
};

struct PS_IN
{
    float4 pos : SV_POSITION;
    float3 col : COLOR;
};

PS_IN main(VS_IN input)
{
    PS_IN output;
    output.pos = mul(mvp, float4(input.pos, 1.0f));
    output.col = input.pos;
    
    return output;
}
