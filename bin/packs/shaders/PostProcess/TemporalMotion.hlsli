#ifndef ZONEREBORN_TEMPORAL_MOTION
#define ZONEREBORN_TEMPORAL_MOTION

float2 ClipToTemporalUv(float4 position)
{
    float2 ndc = position.xy / max(position.w, 0.000001f);
    return float2(ndc.x * 0.5f + 0.5f, 0.5f - ndc.y * 0.5f);
}

float EncodeTemporalDepth(float viewDepth)
{
    return log2(1.0f + max(viewDepth, 0.0f));
}

// xy: current minus previous jittered UV; z: previous surface depth;
// w: history validity. The resolve removes jitter delta from xy exactly once.
float4 MakeTemporalMotion(float4 currentClip, float4 previousClip, float valid)
{
    if (valid < 0.5f || currentClip.w <= 0.00001f || previousClip.w <= 0.00001f ||
        previousClip.z <= 0.0f || previousClip.z >= previousClip.w)
        return float4(0.0f, 0.0f, 0.0f, 0.0f);

    float2 motion = ClipToTemporalUv(currentClip) - ClipToTemporalUv(previousClip);
    return float4(motion, EncodeTemporalDepth(previousClip.w), 1.0f);
}

#endif
