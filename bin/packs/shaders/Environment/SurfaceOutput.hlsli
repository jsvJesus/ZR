struct SurfaceOutput
{
    float4 Color : SV_Target0;
    float4 Ambient : SV_Target1;
};

SurfaceOutput MakeSurfaceOutput(float4 color, float3 ambient)
{
    SurfaceOutput output;
    output.Color = color;
    output.Ambient = float4(max(ambient, 0.0f), color.a);
    return output;
}
