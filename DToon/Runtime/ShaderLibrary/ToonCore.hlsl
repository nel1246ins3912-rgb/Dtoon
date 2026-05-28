#ifndef DTOON_CORE_INCLUDED
#define DTOON_CORE_INCLUDED

// ============================================================================
//  ToonCore.hlsl
//  ----------------------------------------------------------------------------
//  Common data structures, macros, and utility functions used across all
//  DToon shader modules.
//
//  This file is ENGINE-INDEPENDENT. Do not include URP/HDRP-specific headers
//  here. All pipeline-specific code lives under PipelineAdapter/<version>/.
//
//  Porting note (Unreal Engine):
//      Equivalents of these macros and structs map to UE Material Editor as:
//      - half3/half4 -> float3/float4 (UE prefers float)
//      - SAMPLE_TEXTURE2D -> Texture Sample node
//      - struct fields -> custom Material Function inputs
// ============================================================================

// ---- Numeric constants -----------------------------------------------------
#define DTOON_PI         3.14159265359
#define DTOON_TWO_PI     6.28318530718
#define DTOON_INV_PI     0.31830988618
#define DTOON_EPSILON    1e-5

// ---- Common surface input struct -------------------------------------------
//  Filled in by the pipeline adapter (URP14_VertexFragment.hlsl etc.) and
//  passed to lighting / outline / rim functions in ShaderLibrary.
//  Keeping this struct stable across modules is what makes the algorithms
//  portable.
struct DToonSurfaceData
{
    half3   albedo;         // base color (sRGB-decoded)
    half    alpha;          // opacity
    half3   normalWS;       // world-space normal (normalized)
    half3   geometryNormalWS;
    half3   shadowNormalWS;
    half3   mappedNormalWS;
    half3   viewDirWS;      // world-space view dir (toward camera)
    half3   positionWS;     // world-space position
    half2   uv;             // primary UV
    half4   ilm;            // ILM texture (R: shadow threshold, G: spec mask,
                            //              B: rim mask,        A: outline width)
};

// ---- Common light input struct ---------------------------------------------
struct DToonLightData
{
    half3   directionWS;    // direction TOWARD the light
    half3   color;          // light color * intensity
    half    shadowAttenuation;
    half    distanceAttenuation;
};

// ---- Utility ---------------------------------------------------------------
half DToon_Remap01(half value, half minV, half maxV)
{
    return saturate((value - minV) / max(maxV - minV, DTOON_EPSILON));
}

half3 DToon_SafeNormalize(half3 v)
{
    half lenSq = max(dot(v, v), DTOON_EPSILON);
    return v * rsqrt(lenSq);
}

#endif // DTOON_CORE_INCLUDED
