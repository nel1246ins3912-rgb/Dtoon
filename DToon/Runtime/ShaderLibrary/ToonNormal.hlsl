#ifndef DTOON_NORMAL_INCLUDED
#define DTOON_NORMAL_INCLUDED

// ============================================================================
//  ToonNormal.hlsl
//  ----------------------------------------------------------------------------
//  Normal map helpers for DToon.
//
//  This file is mostly engine-independent math. The sampler macros are Unity
//  style, but the unpack and TBN transform map directly to Unreal material
//  functions later.
// ============================================================================

#include "ToonCore.hlsl"

half3 DToon_UnpackNormalScale(half4 packedNormal, half strength)
{
    half3 normalTS;

#if defined(UNITY_NO_DXT5nm)
    normalTS = packedNormal.xyz * 2.0h - 1.0h;
#else
    normalTS.xy = packedNormal.ag * 2.0h - 1.0h;
    normalTS.z = sqrt(saturate(1.0h - dot(normalTS.xy, normalTS.xy)));
#endif

    normalTS.xy *= max(strength, 0.0h);
    normalTS.z = sqrt(saturate(1.0h - dot(normalTS.xy, normalTS.xy)));
    return normalTS;
}

half3 DToon_TangentToWorldNormal(
    half3 normalTS,
    half3 tangentWS,
    half3 bitangentWS,
    half3 normalWS
)
{
    half3x3 tbn = half3x3(
        DToon_SafeNormalize(tangentWS),
        DToon_SafeNormalize(bitangentWS),
        DToon_SafeNormalize(normalWS)
    );
    return DToon_SafeNormalize(mul(normalTS, tbn));
}

#endif // DTOON_NORMAL_INCLUDED
