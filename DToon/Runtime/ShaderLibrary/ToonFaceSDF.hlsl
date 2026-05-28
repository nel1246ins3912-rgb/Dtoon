#ifndef DTOON_FACESDF_INCLUDED
#define DTOON_FACESDF_INCLUDED

// ============================================================================
//  ToonFaceSDF.hlsl
//  ----------------------------------------------------------------------------
//  HoYoverse-style SDF face shadow. ENGINE-INDEPENDENT math.
//
//  Algorithm:
//      1. Project light direction onto the face's local horizontal plane
//      2. Compute a signed angle between light and face-forward
//      3. Sample SDF from the appropriate side (left or right map)
//      4. Compare against the angle threshold to produce a binary shadow mask
// ============================================================================

#include "ToonCore.hlsl"

// TODO Step 8: implement
//  half DToon_FaceSDFShadow(half3 lightDirWS, half3 faceForwardWS,
//                              half3 faceRightWS, half2 uv,
//                              TEXTURE2D_PARAM(sdfTex, samplerSdf))

#endif // DTOON_FACESDF_INCLUDED
