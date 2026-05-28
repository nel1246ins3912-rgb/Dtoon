#ifndef DTOON_LIGHTING_INCLUDED
#define DTOON_LIGHTING_INCLUDED

// ============================================================================
//  ToonLighting.hlsl
//  ----------------------------------------------------------------------------
//  Engine-independent toon lighting math for DToon.
//
//  The shadow model is laid out like lilToon's shadow panel:
//      - 3 shadow stages
//      - per-stage range, blur, normal-map influence, and received-shadow weight
//      - boundary color / width
//      - contrast
//      - environment-light influence
//      - AO map min/max control per stage
//
//  Porting note (Unreal Engine):
//      The functions below map cleanly to Material Functions. Keep engine API
//      calls in PipelineAdapter files and keep only math here.
//
//      Ramp Texture Sample -> Multiply ShadowTint -> Multiply BaseColor ->
//      Multiply LightColor -> Multiply LerpedShadowFactor.
//      Five Multiply nodes plus one Lerp.
// ============================================================================

#include "ToonCore.hlsl"

struct DToonShadowSettings
{
    half    useShadow;
    half    maskStrength;
    half3   ambientColor;
    half    edgeAA;

    half3   color1;
    half    opacity1;
    half    range1;
    half    blur1;
    half    normalStrength1;
    half    receive1;

    half3   color2;
    half    opacity2;
    half    range2;
    half    blur2;
    half    normalStrength2;
    half    receive2;

    half3   color3;
    half    opacity3;
    half    range3;
    half    blur3;
    half    normalStrength3;
    half    receive3;

    half4   borderColor;
    half    borderWidth;
    half    contrast;
    half    environmentStrength;

    half    aoIgnoreRange;
    half    ao1Min;
    half    ao1Max;
    half    ao2Min;
    half    ao2Max;
    half    ao3Min;
    half    ao3Max;
};

// ---- Step ramp (no texture, hard cel band) ---------------------------------
half3 DToon_StepRamp(half NdotL, half threshold, half softness, half3 shadowColor, half3 litColor)
{
    half halfLambert = NdotL * 0.5h + 0.5h;
    half t = smoothstep(threshold - softness, threshold + softness, halfLambert);
    return lerp(shadowColor, litColor, t);
}

half DToon_ClampTransitionWidth(half softness, half availableWidth)
{
    return min(saturate(softness), max(availableWidth * 0.5h, 0.0001h));
}

half DToon_AntiAliasedTransitionWidth(half value, half softness, half availableWidth, half edgeAA)
{
    half aaWidth = (half)fwidth(value) * max(edgeAA, 0.0h);
    return DToon_ClampTransitionWidth(max(saturate(softness), aaWidth), availableWidth);
}

half DToon_ApplyTransitionContrast(half value, half contrast)
{
    half gain = lerp(1.0h, 12.0h, saturate(contrast));
    return saturate((value - 0.5h) * gain + 0.5h);
}

half DToon_RangeMask(half value, half minValue, half maxValue)
{
    half lo = min(saturate(minValue), saturate(maxValue));
    half hi = max(saturate(minValue), saturate(maxValue));
    return smoothstep(lo, max(hi, lo + 0.0001h), saturate(value));
}

half DToon_BoundaryMaskFromTransition(half transitionValue, half width)
{
    half ridge = saturate(transitionValue * (1.0h - transitionValue) * 4.0h);
    half exponent = lerp(12.0h, 0.75h, saturate(width));
    return pow(ridge, exponent) * saturate(width);
}

half DToon_EvaluateHalfLambert(DToonSurfaceData surface, half3 lightDirectionWS, half normalInfluence)
{
    half3 normalWS = DToon_SafeNormalize(lerp(
        surface.shadowNormalWS,
        surface.mappedNormalWS,
        saturate(normalInfluence)
    ));
    return saturate(dot(normalWS, lightDirectionWS) * 0.5h + 0.5h);
}

half3 DToon_ApplyEnvironmentToShadow(half3 shadowColor, half3 baseColor, DToonShadowSettings settings)
{
    half environmentAmount = saturate(max(max(settings.ambientColor.r, settings.ambientColor.g), settings.ambientColor.b));
    half3 liftedShadow = lerp(shadowColor, baseColor, environmentAmount * 0.5h);
    return lerp(shadowColor, liftedShadow, saturate(settings.environmentStrength));
}

half3 DToon_ApplyShadowStage(half3 fromColor, half3 toColor, half opacity, half aoMask)
{
    return lerp(fromColor, toColor, saturate(opacity) * saturate(aoMask));
}

// ---- Texture-based ramp ----------------------------------------------------
half3 DToon_SampleRamp(
    half NdotL,
    half rampOffset,
    TEXTURE2D_PARAM(rampTex, sampler_rampTex)
)
{
    half halfLambert = NdotL * 0.5h + 0.5h;
    half rampU = saturate(halfLambert + rampOffset);
    return SAMPLE_TEXTURE2D(rampTex, sampler_rampTex, half2(rampU, 0.5h)).rgb;
}

// ---- Ramp-based toon diffuse (Option Y: ramp * shadowTint * albedo) -------
//  Self-shading samples the authored ramp directly. Received cast shadows
//  blend toward the ramp's darkest color instead of multiplying to black.
half3 DToon_ToonDiffuse_Ramp(
    half3 albedo,
    half  NdotL,
    half  rampOffset,
    half3 shadowTint,
    half3 lightColor,
    half  shadowAttenuation,
    half  receiveShadowStrength,
    TEXTURE2D_PARAM(rampTex, sampler_rampTex)
)
{
    half halfLambert = NdotL * 0.5h + 0.5h;
    half rampU = saturate(halfLambert + rampOffset);
    half3 ramp = SAMPLE_TEXTURE2D(rampTex, sampler_rampTex, half2(rampU, 0.5h)).rgb;

    // Ramp is the primary tone curve; shadowTint biases it.
    half3 toned = albedo * ramp * shadowTint;
    half3 darkRamp = SAMPLE_TEXTURE2D(rampTex, sampler_rampTex, half2(0.0h, 0.5h)).rgb;
    half3 castShadowColor = albedo * darkRamp * shadowTint;

    // Ignore the bottom ~5% of attenuation as URP shadow-map noise/fade.
    half rawCast = 1.0h - saturate(shadowAttenuation);
    half castSignal = saturate((rawCast - 0.05h) / 0.95h);
    half castShadowFactor = castSignal * saturate(receiveShadowStrength);
    half3 finalColor = lerp(toned, castShadowColor, castShadowFactor);

    return finalColor * lightColor;
}

// ---- Main toon diffuse -----------------------------------------------------
half3 DToon_ToonDiffuse(
    DToonSurfaceData surface,
    DToonLightData light,
    DToonShadowSettings settings
)
{
    half h1 = DToon_EvaluateHalfLambert(surface, light.directionWS, settings.normalStrength1);
    half h2 = DToon_EvaluateHalfLambert(surface, light.directionWS, settings.normalStrength2);
    half h3 = DToon_EvaluateHalfLambert(surface, light.directionWS, settings.normalStrength3);

    half t1 = saturate(settings.range1);
    half t2 = t1 * saturate(settings.range2);
    half t3 = t2 * saturate(settings.range3);

    half s1 = DToon_AntiAliasedTransitionWidth(h1, settings.blur1, t1 - t2, settings.edgeAA);
    half s2 = DToon_AntiAliasedTransitionWidth(h2, settings.blur2, t2 - t3, settings.edgeAA);
    half s3 = DToon_AntiAliasedTransitionWidth(h3, settings.blur3, t3, settings.edgeAA);

    half shadow3To2 = DToon_ApplyTransitionContrast(
        smoothstep(t3 - s3, t3 + s3, h3),
        settings.contrast
    );
    half shadow2To1 = DToon_ApplyTransitionContrast(
        smoothstep(t2 - s2, t2 + s2, h2),
        settings.contrast
    );
    half shadow1ToLit = DToon_ApplyTransitionContrast(
        smoothstep(t1 - s1, t1 + s1, h1),
        settings.contrast
    );

    half aoValue = surface.ilm.r;
    half ao1 = DToon_RangeMask(aoValue, settings.ao1Min, settings.ao1Max);
    half ao2 = DToon_RangeMask(aoValue, settings.ao2Min, settings.ao2Max);
    half ao3 = DToon_RangeMask(aoValue, settings.ao3Min, settings.ao3Max);

    half3 lit = surface.albedo;
    half3 shadow1Raw = DToon_ApplyEnvironmentToShadow(surface.albedo * settings.color1, surface.albedo, settings);
    half3 shadow2Raw = DToon_ApplyEnvironmentToShadow(surface.albedo * settings.color2, surface.albedo, settings);
    half3 shadow3Raw = DToon_ApplyEnvironmentToShadow(surface.albedo * settings.color3, surface.albedo, settings);

    half3 shadow1 = DToon_ApplyShadowStage(lit, shadow1Raw, settings.opacity1, ao1);
    half3 shadow2 = DToon_ApplyShadowStage(shadow1, shadow2Raw, settings.opacity2, ao2);
    half3 shadow3 = DToon_ApplyShadowStage(shadow2, shadow3Raw, settings.opacity3, ao3);

    half3 shadow32 = lerp(shadow3, shadow2, shadow3To2);
    half3 shadow321 = lerp(shadow32, shadow1, shadow2To1);
    half3 toonColor = lerp(shadow321, lit, shadow1ToLit);

    half3 aoOnlyColor = lit;
    aoOnlyColor = DToon_ApplyShadowStage(aoOnlyColor, shadow1Raw, settings.opacity1, ao1);
    aoOnlyColor = DToon_ApplyShadowStage(aoOnlyColor, shadow2Raw, settings.opacity2, ao2);
    aoOnlyColor = DToon_ApplyShadowStage(aoOnlyColor, shadow3Raw, settings.opacity3, ao3);
    toonColor = lerp(toonColor, aoOnlyColor, saturate(settings.aoIgnoreRange));

    half b32 = DToon_BoundaryMaskFromTransition(shadow3To2, settings.borderWidth);
    half b21 = DToon_BoundaryMaskFromTransition(shadow2To1, settings.borderWidth);
    half b10 = DToon_BoundaryMaskFromTransition(shadow1ToLit, settings.borderWidth);
    half borderMask = max(max(b32, b21), b10) * saturate(settings.borderColor.a);
    toonColor = lerp(toonColor, settings.borderColor.rgb, borderMask);

    half receivedShadow = 1.0h - saturate(light.shadowAttenuation);
    half3 receivedTarget = lit;
    receivedTarget = lerp(receivedTarget, shadow1, saturate(settings.receive1));
    receivedTarget = lerp(receivedTarget, shadow2, saturate(settings.receive2));
    receivedTarget = lerp(receivedTarget, shadow3, saturate(settings.receive3));
    toonColor = lerp(toonColor, receivedTarget, receivedShadow);

    half noShadowHalfLambert = DToon_EvaluateHalfLambert(surface, light.directionWS, 0.0h);
    half3 noShadowColor = surface.albedo * noShadowHalfLambert;
    half3 diffuse = lerp(noShadowColor, toonColor, saturate(settings.useShadow * settings.maskStrength));

    return diffuse * light.color * light.distanceAttenuation;
}

// ---- Smooth additional light diffuse --------------------------------------
half3 DToon_SmoothDiffuse(
    DToonSurfaceData surface,
    DToonLightData light,
    half3 shadowTint
)
{
    half NdotL = dot(surface.shadowNormalWS, light.directionWS);
    half halfLambert = saturate(NdotL * 0.5h + 0.5h);
    half3 lit = surface.albedo * halfLambert;
    half3 shadow = surface.albedo * shadowTint;
    half3 diffuse = lerp(shadow, lit, saturate(light.shadowAttenuation));
    return diffuse * light.color * light.distanceAttenuation;
}

// ---- Received shadow control ----------------------------------------------
half DToon_ApplyReceivedShadowControl(
    half rawShadowAttenuation,
    half receiveShadows,
    half shadowStrength
)
{
    half strength = saturate(receiveShadows) * saturate(shadowStrength);
    return lerp(1.0h, saturate(rawShadowAttenuation), strength);
}

// ---- Brightness range ------------------------------------------------------
half3 DToon_ApplyBrightnessRange(
    half3 litColor,
    half3 baseColor,
    half minBrightness,
    half maxBrightness
)
{
    half minValue = saturate(minBrightness);
    half maxValue = max(maxBrightness, minValue);
    half3 minColor = baseColor * minValue;
    half3 maxColor = baseColor * maxValue;
    return min(max(litColor, minColor), maxColor);
}

#endif // DTOON_LIGHTING_INCLUDED
