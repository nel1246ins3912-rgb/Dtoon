#ifndef DTOON_OUTLINE_INCLUDED
#define DTOON_OUTLINE_INCLUDED

// ============================================================================
//  ToonOutline.hlsl
//  ----------------------------------------------------------------------------
//  Inverted-hull outline vertex offset. ENGINE-INDEPENDENT geometry math.
//
//  Algorithm:
//      Push each vertex along its (smoothed) normal in clip-space, scaled by
//      the camera distance so the outline thickness stays roughly constant
//      in screen space.
//
//  Porting note (Unreal Engine):
//      UE has a built-in "Pixel Depth Offset" / "World Position Offset" pin
//      on the Material output; the math here goes into WorldPositionOffset
//      with a Two-Sided / Reverse Culling material setting.
// ============================================================================

#include "ToonCore.hlsl"

float4 DToon_OutlineClipPos(
    float3 positionOS,
    float3 normalOS,
    float  outlineWidth,
    float  distanceScale,
    float  maxWidth
)
{
    // Transform to clip space first to get baseline depth.
    float4 positionCS = TransformObjectToHClip(positionOS);

    // Scale width by clip-space W to keep screen-space size more stable
    // when distanceScale = 1. When distanceScale = 0, width stays in
    // world units and visually thins with distance.
    float screenScale = lerp(1.0, positionCS.w, distanceScale);
    float scaledWidth = min(outlineWidth * screenScale, maxWidth);

    float3 pushedPositionOS = positionOS + normalize(normalOS) * scaledWidth;
    return TransformObjectToHClip(pushedPositionOS);
}

half3 DToon_OutlineColor(half3 albedo, half darkening)
{
    return albedo * saturate(darkening);
}

#endif // DTOON_OUTLINE_INCLUDED
