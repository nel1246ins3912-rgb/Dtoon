#ifndef DTOON_URP14_VERTFRAG_INCLUDED
#define DTOON_URP14_VERTFRAG_INCLUDED

// ============================================================================
//  URP14_VertexFragment.hlsl
//  ----------------------------------------------------------------------------
//  Vertex and fragment programs for the URP 14 forward pass.
//  Wires together pipeline inputs with engine-independent shader algorithms.
//
//  Step 1 Phase 2: ramp-texture cel shading from the URP main light.
// ============================================================================

#include "URP14_Inputs.hlsl"
#include "../../ShaderLibrary/ToonCore.hlsl"
#include "../../ShaderLibrary/ToonLighting.hlsl"
#include "../../ShaderLibrary/ToonRimLight.hlsl"
#include "../../ShaderLibrary/ToonMatcap.hlsl"
#include "../../ShaderLibrary/ToonSpecular.hlsl"

// ---- Material properties (declared in the .shader file's Properties block) -
CBUFFER_START(UnityPerMaterial)
    float4  _BaseMap_ST;
    float4  _BaseColor;
    float   _RampOffset;
    float4  _ShadowTint;
    float   _ReceiveShadowsStrength;
    float   _AlphaClip;
    float   _Cutoff;
    float   _OutlineEnable;
    float   _OutlineWidth;
    float   _OutlineDarkening;
    float   _OutlineDistanceScale;
    float   _OutlineMaxWidth;
    float   _UseSmoothNormal;
    float   _SmoothNormalStrength;
    float   _RimEnable;
    float   _RimLightAware;
    float4  _RimColor;
    float   _RimIntensity;
    float   _RimPower;
    float   _RimSoftness;
    float   _MatcapEnable;
    float   _MatcapMode;
    float4  _MatcapColor;
    float   _MatcapIntensity;
    float   _SpecularEnable;
    float4  _SpecularColor;
    float   _SpecularIntensity;
    float   _SpecularPower;
    float   _SpecularThreshold;
    float   _SpecularSoftness;
CBUFFER_END

TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);
TEXTURE2D(_RampMap);
SAMPLER(sampler_RampMap);
TEXTURE2D(_MatcapTex);
SAMPLER(sampler_MatcapTex);

// ---- Vertex / fragment IO --------------------------------------------------
struct Attributes
{
    float4 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    float2 uv           : TEXCOORD0;
};

struct Varyings
{
    float4 positionHCS  : SV_POSITION;
    float3 positionWS   : TEXCOORD0;
    float3 normalWS     : TEXCOORD1;
    float2 uv           : TEXCOORD2;
};

// ---- Vertex ----------------------------------------------------------------
Varyings DToon_Vert(Attributes IN)
{
    Varyings OUT = (Varyings)0;
    VertexPositionInputs vpi = GetVertexPositionInputs(IN.positionOS.xyz);
    VertexNormalInputs vni = GetVertexNormalInputs(IN.normalOS);

    OUT.positionHCS = vpi.positionCS;
    OUT.positionWS = vpi.positionWS;
    OUT.normalWS = vni.normalWS;
    OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
    return OUT;
}

// ---- Fragment --------------------------------------------------------------
half4 DToon_Frag(Varyings IN) : SV_Target
{
    // 1. Sample base color.
    half4 baseSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
    #if defined(_ALPHACLIP) || defined(_ALPHATEST_ON)
        clip(baseSample.a * _BaseColor.a - _Cutoff);
    #endif

    half3 albedo = baseSample.rgb * _BaseColor.rgb;

    // 2. Get main light + shadow coordinate.
    float4 shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
    DToonLightData light = DToon_URP14_FromMainLight(IN.positionWS, shadowCoord);

    // 3. Compute raw light angle for ramp-space toon shading.
    half3 N = normalize(IN.normalWS);
    half NdotL = dot(N, light.directionWS);

    // 4. Combine albedo, ramp, tint, light color, and realtime shadows.
    half3 col = DToon_ToonDiffuse_Ramp(
        albedo,
        NdotL,
        _RampOffset,
        _ShadowTint.rgb,
        light.color,
        light.shadowAttenuation,
        (half)_ReceiveShadowsStrength,
        TEXTURE2D_ARGS(_RampMap, sampler_RampMap)
    );

    #if defined(_RIM)
    if (_RimEnable > 0.5f)
    {
        half3 V = normalize(GetWorldSpaceViewDir(IN.positionWS));
        bool lightAware = false;
        #if defined(_RIM_LIGHT_AWARE)
            lightAware = _RimLightAware > 0.5f;
        #endif

        half rimMask = DToon_RimMask(
            N,
            V,
            light.directionWS,
            (half)_RimPower,
            (half)_RimSoftness,
            lightAware
        );
        half3 rim = DToon_RimColor(_RimColor.rgb, (half)_RimIntensity, rimMask);
        col += rim;
    }
    #endif

    #if defined(_MATCAP)
    if (_MatcapEnable > 0.5f)
    {
        float2 matcapUV = DToon_MatcapUV(N, UNITY_MATRIX_V);
        half4 matcapSample = SAMPLE_TEXTURE2D(_MatcapTex, sampler_MatcapTex, matcapUV);
        col = DToon_ApplyMatcap(
            col,
            matcapSample.rgb,
            _MatcapColor.rgb,
            (half)_MatcapIntensity,
            (half)_MatcapMode
        );
    }
    #endif

    #if defined(_SPECULAR)
    if (_SpecularEnable > 0.5f)
    {
        half3 V_spec = normalize(GetWorldSpaceViewDir(IN.positionWS));
        half specMask = DToon_SpecularMask(
            N,
            light.directionWS,
            V_spec,
            (half)_SpecularPower,
            (half)_SpecularThreshold,
            (half)_SpecularSoftness
        );
        half3 spec = DToon_SpecularColor(
            _SpecularColor.rgb,
            (half)_SpecularIntensity,
            specMask
        );
        col += spec * light.color;
    }
    #endif

    return half4(col, baseSample.a * _BaseColor.a);
}


#endif // DTOON_URP14_VERTFRAG_INCLUDED
