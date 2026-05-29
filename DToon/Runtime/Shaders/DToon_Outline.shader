Shader "DToon/Outline"
{
    // ========================================================================
    //  DToon/Outline
    //  ------------------------------------------------------------------------
    //  Inverted-hull outline-only shader for cases where URP needs a
    //  dedicated hull renderer instead of relying on material multi-pass
    //  execution.
    // ========================================================================

    Properties
    {
        _OutlineColor       ("Outline Color (legacy, unused for albedo mode)", Color) = (0, 0, 0, 1)
        _OutlineWidth       ("Outline Width", Range(0, 0.05)) = 0.005
        _OutlineDarkening   ("Outline Darkening", Range(0, 1)) = 0.3
        _OutlineDistanceScale ("Distance Scale", Range(0, 2)) = 1.0
        _OutlineMaxWidth    ("Max Outline Width", Range(0, 0.5)) = 0.05
        [Toggle(_USE_SMOOTH_NORMAL)] _UseSmoothNormal ("Use Baked Smooth Normals", Float) = 0
        _SmoothNormalStrength ("Smooth Normal Strength", Range(0, 1)) = 1
        [HideInInspector] _OutlineCull ("Outline Cull", Float) = 0
        [HideInInspector] _OutlineZTest ("Outline ZTest", Float) = 8

        [Header(Alpha Clip)]
        [Toggle(_OUTLINE_ALPHACLIP)] _OutlineAlphaClip ("Enable Alpha Clip", Float) = 0
        _BaseMap            ("Base Map (for alpha)", 2D) = "white" {}
        _BaseColor          ("Base Color (for alpha)", Color) = (1, 1, 1, 1)
        _Cutoff             ("Alpha Cutoff", Range(0, 1)) = 0.5
    }

    SubShader
    {
        Tags
        {
            "RenderType"        = "Opaque"
            "RenderPipeline"    = "UniversalPipeline"
            "Queue"             = "Geometry-1"
        }

        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "UniversalForward" }

            Cull [_OutlineCull]
            ZWrite Off
            ZTest [_OutlineZTest]

            HLSLPROGRAM
            #pragma vertex   DToonOutlineOnly_Vert
            #pragma fragment DToonOutlineOnly_Frag
            #pragma shader_feature_local _OUTLINE_ALPHACLIP
            #pragma multi_compile _ _USE_SMOOTH_NORMAL
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "../ShaderLibrary/ToonCore.hlsl"
            #include "../ShaderLibrary/ToonOutline.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4   _OutlineColor;
                float   _OutlineWidth;
                float   _OutlineDarkening;
                float   _OutlineDistanceScale;
                float   _OutlineMaxWidth;
                float   _UseSmoothNormal;
                float   _SmoothNormalStrength;
                float   _OutlineAlphaClip;
                float4  _BaseMap_ST;
                half4   _BaseColor;
                float   _Cutoff;
            CBUFFER_END

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);
            float4 _BaseMap_TexelSize;

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

            Varyings DToonOutlineOnly_Vert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
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
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                return OUT;
            }

            half4 DToonOutlineOnly_Frag(Varyings IN) : SV_Target
            {
                half4 baseSample = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv);
                if (_OutlineAlphaClip > 0.5)
                {
                    // Feathered hair alpha needs a few texels of dilation so
                    // the outline lands outside the cutoff edge instead of under it.
                    float2 texel = max(_BaseMap_TexelSize.xy * 3.0, float2(0.012, 0.012));
                    half sourceAlpha = baseSample.a * _BaseColor.a;
                    half outlineAlpha = sourceAlpha;
                    outlineAlpha = max(outlineAlpha, SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv + float2( texel.x, 0.0)).a * _BaseColor.a);
                    outlineAlpha = max(outlineAlpha, SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv + float2(-texel.x, 0.0)).a * _BaseColor.a);
                    outlineAlpha = max(outlineAlpha, SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv + float2(0.0,  texel.y)).a * _BaseColor.a);
                    outlineAlpha = max(outlineAlpha, SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv + float2(0.0, -texel.y)).a * _BaseColor.a);
                    outlineAlpha = max(outlineAlpha, SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv + float2( texel.x,  texel.y)).a * _BaseColor.a);
                    outlineAlpha = max(outlineAlpha, SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv + float2(-texel.x,  texel.y)).a * _BaseColor.a);
                    outlineAlpha = max(outlineAlpha, SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv + float2( texel.x, -texel.y)).a * _BaseColor.a);
                    outlineAlpha = max(outlineAlpha, SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv + float2(-texel.x, -texel.y)).a * _BaseColor.a);
                    clip(outlineAlpha - _Cutoff);
                    clip(_Cutoff - sourceAlpha);
                }

                half3 albedo = baseSample.rgb * _BaseColor.rgb;
                return half4(DToon_OutlineColor(albedo, (half)_OutlineDarkening), 1);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
