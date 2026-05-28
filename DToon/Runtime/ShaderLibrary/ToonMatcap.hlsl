#ifndef DTOON_MATCAP_INCLUDED
#define DTOON_MATCAP_INCLUDED

// ============================================================================
//  ToonMatcap.hlsl
//  ----------------------------------------------------------------------------
//  View-space normal matcap sampling with 3 composition modes.
//
//  The view-space normal's XY (after re-mapping from [-1,1] to [0,1])
//  becomes the UV for sampling a 2D matcap texture. The matcap thus
//  appears to "stick" to surface orientation relative to the camera.
//
//  Composition modes:
//    Additive       - base + matcap * intensity
//    Multiplicative - base * matcap * intensity
//    Lerp           - lerp(base, matcap, intensity)
//
//  Porting note (Unreal Engine):
//    Matcap is "Spherical Reflection" or "MatCap" Material Function.
//    View-space normal transform via TransformDirection node.
// ============================================================================

#include "ToonCore.hlsl"

#define DTOON_MATCAP_MODE_ADD  0
#define DTOON_MATCAP_MODE_MUL  1
#define DTOON_MATCAP_MODE_LERP 2

float2 DToon_MatcapUV(half3 normalWS, float4x4 viewMatrix)
{
    half3 viewNormal = mul((float3x3)viewMatrix, normalWS);
    return viewNormal.xy * 0.5h + 0.5h;
}

half3 DToon_ApplyMatcap(
    half3 base,
    half3 matcapSample,
    half3 matcapTint,
    half  intensity,
    half  mode
)
{
    half3 tinted = matcapSample * matcapTint * intensity;

    if (mode < 0.5h)
    {
        return base + tinted;
    }

    if (mode < 1.5h)
    {
        return base * tinted;
    }

    return lerp(base, tinted, saturate(intensity));
}

#endif // DTOON_MATCAP_INCLUDED
