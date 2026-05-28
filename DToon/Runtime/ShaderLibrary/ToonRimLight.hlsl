#ifndef DTOON_RIMLIGHT_INCLUDED
#define DTOON_RIMLIGHT_INCLUDED

// ============================================================================
//  ToonRimLight.hlsl
//  ----------------------------------------------------------------------------
//  Engine-independent rim light math.
//
//  Two modes:
//    Uniform:    fresnel = pow(1 - saturate(dot(N, V)), power)
//    LightAware: fresnel * saturate(dot(-L, V) * 0.5 + 0.5)
//                where L is light direction (toward light, not from)
//                so the rim appears strongest on the back-lit side.
//
//  Porting note (Unreal Engine):
//    Material Function with these inputs:
//      Normal (WS), CameraVector (V), LightDirection (L)
//    Outputs RimColor multiplied by Intensity, ready to add to
//    base diffuse.
// ============================================================================

#include "ToonCore.hlsl"

half DToon_RimMask(
    half3 normalWS,
    half3 viewWS,
    half3 lightWS,
    half  power,
    half  softness,
    bool  lightAware
)
{
    half NdotV = saturate(dot(normalWS, viewWS));
    half fresnel = pow(1.0h - NdotV, power);

    // Smoothstep just for edge anti-aliasing on the rim base.
    // Softness widens the bottom edge of the rim band; the rim
    // remains as wide as the fresnel falloff allows.
    half mask = smoothstep(0.0h, max(softness, 0.001h), fresnel);

    if (lightAware)
    {
        half LdotV = saturate(dot(-lightWS, viewWS) * 0.5h + 0.5h);
        mask *= LdotV;
    }

    return saturate(mask);
}

half3 DToon_RimColor(half3 rimColor, half intensity, half mask)
{
    return rimColor * intensity * mask;
}

#endif // DTOON_RIMLIGHT_INCLUDED
