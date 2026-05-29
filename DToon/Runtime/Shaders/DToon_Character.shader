Shader "DToon/Character"
{
    // ========================================================================
    //  DToon/Character
    //  ------------------------------------------------------------------------
    //  Main character toon shader. URP 14 forward path.
    //
    //  Step 1: Ramp-texture cel shading. The portable toon math lives under
    //          ShaderLibrary and URP-specific light access stays in the
    //          URP14 adapter.
    // ========================================================================

    Properties
    {
        [Header(Base)]
        _BaseMap            ("Base Map",        2D)     = "white" {}
        _BaseColor          ("Base Color",      Color)  = (1, 1, 1, 1)

        [Header(Cel Shading)]
        [NoScaleOffset]
        _RampMap            ("Ramp Map (1D)",   2D)     = "white" {}
        _RampOffset         ("Ramp Offset",     Range(-0.5, 0.5)) = 0
        _ShadowTint         ("Shadow Tint",     Color)  = (1, 1, 1, 1)
        _ReceiveShadowsStrength ("Receive Shadows Strength", Range(0, 1)) = 1

        [Header(Alpha Clip)]
        [Toggle(_ALPHACLIP)] _AlphaClip ("Enable Alpha Clip", Float) = 0
        _Cutoff             ("Alpha Cutoff Threshold", Range(0, 1)) = 0.5

        [Header(Outline)]
        [Toggle(_OUTLINE)] _OutlineEnable ("Enable Outline", Float) = 1
        _OutlineWidth       ("Outline Width (world units)", Range(0, 0.05)) = 0.005
        _OutlineDarkening   ("Outline Color Darkening", Range(0, 1)) = 0.3
        _OutlineDistanceScale ("Distance Scale (0=fixed world, 1=screen-space)", Range(0, 2)) = 1.0
        _OutlineMaxWidth    ("Max Outline Width (world units)", Range(0, 0.5)) = 0.05
        [Toggle(_USE_SMOOTH_NORMAL)] _UseSmoothNormal ("Use Baked Smooth Normals", Float) = 0
        _SmoothNormalStrength ("Smooth Normal Strength", Range(0, 1)) = 1

        [Header(Rim Light)]
        [Toggle(_RIM)] _RimEnable ("Enable Rim Light", Float) = 1
        [Toggle(_RIM_LIGHT_AWARE)] _RimLightAware ("Light-Aware Rim", Float) = 1
        [HDR] _RimColor     ("Rim Color", Color) = (1, 1, 1, 1)
        _RimIntensity       ("Rim Intensity", Range(0, 5)) = 1.0
        _RimPower           ("Rim Power (Falloff)", Range(0.5, 16)) = 4.0
        _RimSoftness        ("Rim Edge Softness", Range(0, 0.5)) = 0.05

        [Header(Matcap)]
        [Toggle(_MATCAP)] _MatcapEnable ("Enable Matcap", Float) = 0
        [Enum(Additive,0,Multiplicative,1,Lerp,2)] _MatcapMode ("Matcap Mode", Float) = 0
        _MatcapTex          ("Matcap Texture", 2D) = "black" {}
        [HDR] _MatcapColor  ("Matcap Tint", Color) = (1, 1, 1, 1)
        _MatcapIntensity    ("Matcap Intensity", Range(0, 5)) = 1.0

        [Header(Stepped Specular)]
        [Toggle(_SPECULAR)] _SpecularEnable ("Enable Specular", Float) = 0
        [HDR] _SpecularColor ("Specular Color", Color) = (1, 1, 1, 1)
        _SpecularIntensity  ("Specular Intensity", Range(0, 5)) = 1.0 // Harness demo uses 1.2-3.0 for primitive visibility; shipped default 1.0 is conservative for ILM-driven character workflows (Step 7).
        _SpecularPower      ("Specular Power (Sharpness)", Range(1, 256)) = 32
        _SpecularThreshold  ("Specular Threshold", Range(0, 1)) = 0.5
        _SpecularSoftness   ("Specular Edge Softness", Range(0, 0.5)) = 0.05
    }

    SubShader
    {
        Tags
        {
            "RenderType"        = "Opaque"
            "RenderPipeline"    = "UniversalPipeline"
            "Queue"             = "Geometry"
        }

        // --------------------------------------------------------------------
        //  ForwardLit pass
        // --------------------------------------------------------------------
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Stencil
            {
                Ref 1
                Comp Always
                Pass Replace
            }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex   DToon_Vert
            #pragma fragment DToon_Frag
            #pragma shader_feature_local _ALPHACLIP
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma shader_feature_local _RIM
            #pragma shader_feature_local _RIM_LIGHT_AWARE
            #pragma shader_feature_local _MATCAP
            #pragma shader_feature_local _SPECULAR

            // URP shader keywords for realtime shadows and additional lights.
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "../PipelineAdapter/URP14/URP14_VertexFragment.hlsl"
            ENDHLSL
        }

        // --------------------------------------------------------------------
        //  Inverted-hull outline
        // --------------------------------------------------------------------
        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            Cull Front
            ZWrite On
            ZTest LEqual
            Stencil
            {
                Ref 1
                Comp NotEqual
            }

            HLSLPROGRAM
            #pragma vertex   DToonOutline_Vert
            #pragma fragment DToonOutline_Frag
            #pragma shader_feature_local _OUTLINE
            #pragma shader_feature_local _ALPHACLIP
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma multi_compile _ _USE_SMOOTH_NORMAL

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "../ShaderLibrary/ToonCore.hlsl"
            #include "../ShaderLibrary/ToonOutline.hlsl"

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

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                float4 smoothNormalOS : TEXCOORD3;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
            };

            Varyings DToonOutline_Vert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
            #if defined(_OUTLINE)
                float3 extrudeNormalOS = DToon_SelectOutlineNormalOS(
                    IN.normalOS,
                    IN.smoothNormalOS,
                    _SmoothNormalStrength
                );
                OUT.positionHCS = DToon_OutlineClipPos(
                    IN.positionOS.xyz,
                    extrudeNormalOS,
                    _OutlineWidth,
                    _OutlineDistanceScale,
                    _OutlineMaxWidth
                );
            #else
                OUT.positionHCS = float4(0, 0, 0, 1);
            #endif
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            half4 DToonOutline_Frag(Varyings IN) : SV_Target
            {
            #if !defined(_OUTLINE)
                discard;
            #endif

                half4 baseSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
            #if defined(_ALPHACLIP) || defined(_ALPHATEST_ON)
                clip(baseSample.a * _BaseColor.a - _Cutoff);
            #endif

                half3 albedo = baseSample.rgb * _BaseColor.rgb;
                half3 outlineColor = DToon_OutlineColor(albedo, (half)_OutlineDarkening);
                return half4(outlineColor, 1);
            }
            ENDHLSL
        }

        // --------------------------------------------------------------------
        //  ShadowCaster (so the character casts shadows correctly)
        // --------------------------------------------------------------------
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma vertex   ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma shader_feature_local _ALPHACLIP
            #pragma shader_feature_local _ALPHATEST_ON
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            // Required base headers. Do not include LitInput.hlsl here; it is
            // URP Lit-specific. CommonMaterial supplies helpers such as
            // LerpWhiteTo that URP 14 Shadows.hlsl expects.
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"

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

            #if defined(_ALPHACLIP) || defined(_ALPHATEST_ON)
                #define _ALPHATEST_ON 1
            #endif

            half Alpha(half albedoAlpha, half4 color, half cutoff)
            {
                half alpha = albedoAlpha * color.a;
                alpha = AlphaDiscard(alpha, cutoff);
                return alpha;
            }

            half4 SampleAlbedoAlpha(float2 uv, TEXTURE2D_PARAM(albedoAlphaMap, sampler_albedoAlphaMap))
            {
                return half4(SAMPLE_TEXTURE2D(albedoAlphaMap, sampler_albedoAlphaMap, uv));
            }

            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }
    }

    CustomEditor "DToon.DToonCharacterGUI"
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
