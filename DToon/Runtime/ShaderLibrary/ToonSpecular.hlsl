#ifndef DTOON_SPECULAR_INCLUDED
#define DTOON_SPECULAR_INCLUDED

// ============================================================================
//  ToonSpecular.hlsl
//  ----------------------------------------------------------------------------
//  Engine-independent stepped specular (Blinn-Phong) for cel shading.
//
//  Standard Blinn-Phong: pow(saturate(dot(N, H)), power)
//  Stepped version: smoothstep(threshold-soft, threshold+soft, raw)
//
//  This produces a hard-edged highlight band that matches NiloToon
//  and lilToon's stepped specular behavior.
//
//  Porting note (Unreal Engine):
//    Material function inputs: WorldNormal, LightVector, ViewVector
//    Half vector via: normalize(LightVector + ViewVector)
//    Smoothstep node provided by UE Material editor.
// ============================================================================

#include "ToonCore.hlsl"

// Compute stepped specular mask. Returns [0..1] intensity.
// normalWS, lightWS, viewWS: world-space, normalized
// power: typical 8 (broad) ~ 256 (sharp)
// threshold: typical 0.5, applied in specRaw space after the
//            Blinn-Phong pow(NdotH, power) peak has been computed.
// softness: edge anti-aliasing width around the threshold
half DToon_SpecularMask(
    half3 normalWS,
    half3 lightWS,
    half3 viewWS,
    half  power,
    half  threshold,
    half  softness
)
{
    half3 H = normalize(lightWS + viewWS);
    half NdotH = saturate(dot(normalWS, H));

    // Blinn-Phong peaked shape FIRST. pow(NdotH, power) approaches 0
    // away from the perfect specular angle, giving the tight highlight
    // that defines anime spec.
    half specRaw = pow(NdotH, max(power, 1.0h));

    // Smoothstep on the peaked specRaw provides the cel hard-edge AA.
    // Threshold is in specRaw space (0..1), centered around 0.5 by
    // convention.
    return smoothstep(
        saturate(threshold - softness),
        saturate(threshold + softness),
        specRaw
    );
}

// Final specular color contribution.
half3 DToon_SpecularColor(
    half3 specularColor,
    half  intensity,
    half  mask
)
{
    return specularColor * intensity * mask;
}

#endif // DTOON_SPECULAR_INCLUDED
