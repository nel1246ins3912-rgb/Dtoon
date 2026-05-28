#ifndef DTOON_URP14_INPUTS_INCLUDED
#define DTOON_URP14_INPUTS_INCLUDED

// ============================================================================
//  URP14_Inputs.hlsl
//  ----------------------------------------------------------------------------
//  URP 14 (Unity 2022 LTS) specific input mapping.
//  Bridges URP's Light struct and shader inputs to DToon's
//  engine-independent DToonSurfaceData / DToonLightData structs.
//
//  When porting to URP 17 (Unity 6) or other pipelines, create a sibling file
//  that produces the SAME output structs but reads from the new pipeline's
//  inputs. The ShaderLibrary algorithms stay untouched.
// ============================================================================

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
#include "../../ShaderLibrary/ToonCore.hlsl"

// ---- Bridge: URP Light -> DToonLightData --------------------------------
DToonLightData DToon_URP14_FromMainLightUnshadowed()
{
    Light mainLight = GetMainLight();

    DToonLightData ld;
    ld.directionWS          = mainLight.direction;
    ld.color                = mainLight.color;
    ld.shadowAttenuation    = 1.0;
    ld.distanceAttenuation  = mainLight.distanceAttenuation;

    return ld;
}

DToonLightData DToon_URP14_FromAdditionalLight(uint lightIndex, float3 positionWS, half4 shadowMask)
{
#if defined(_ADDITIONAL_LIGHT_SHADOWS)
    Light additionalLight = GetAdditionalLight(lightIndex, positionWS, shadowMask);
#else
    Light additionalLight = GetAdditionalLight(lightIndex, positionWS);
#endif

    DToonLightData ld;
    ld.directionWS          = additionalLight.direction;
    ld.color                = additionalLight.color;
    ld.shadowAttenuation    = additionalLight.shadowAttenuation;
    ld.distanceAttenuation  = additionalLight.distanceAttenuation;
    return ld;
}

DToonLightData DToon_URP14_FromAdditionalLight(uint lightIndex, float3 positionWS)
{
    return DToon_URP14_FromAdditionalLight(lightIndex, positionWS, half4(1.0h, 1.0h, 1.0h, 1.0h));
}

half DToon_URP14_LightEnergy(DToonLightData light)
{
    half colorEnergy = max(max(light.color.r, light.color.g), light.color.b);
    return colorEnergy * light.distanceAttenuation;
}

DToonLightData DToon_URP14_FromMainLight(float3 positionWS, float4 shadowCoord)
{
    Light mainLight = GetMainLight(shadowCoord);

    DToonLightData ld;
    ld.directionWS          = mainLight.direction;
    ld.color                = mainLight.color;
    ld.shadowAttenuation    = mainLight.shadowAttenuation;
    ld.distanceAttenuation  = mainLight.distanceAttenuation;
    return ld;
}

DToonLightData DToon_URP14_FromMainLight(float3 positionWS)
{
    float4 shadowCoord = TransformWorldToShadowCoord(positionWS);
    return DToon_URP14_FromMainLight(positionWS, shadowCoord);
}

#endif // DTOON_URP14_INPUTS_INCLUDED
