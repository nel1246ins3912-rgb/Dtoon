# Algorithms Reference

This document tracks the math behind each DToon module.

## Step 1: Cel-Shaded Diffuse

### Half-Lambert Remap

Toon shading often remaps the standard `NdotL` term so the dark side of the
model is not fully black.

```hlsl
halfLambert = NdotL * 0.5 + 0.5; // [-1, 1] to [0, 1]
```

### Step Ramp

```hlsl
t = smoothstep(threshold - softness, threshold + softness, halfLambert);
diffuse = lerp(shadowColor, litColor, t) * lightColor * shadowAtten;
```

`threshold` controls where the shadow line falls. `softness` controls how
wide the transition band is.

### Texture Ramp

```hlsl
rampU = saturate(halfLambert + offset);
ramp = tex2D(rampTex, float2(rampU, 0.5)).rgb;
diffuse = ramp * albedo * lightColor * shadowAtten;
```

The ramp is usually a small horizontal texture with dark tones on the left
and light tones on the right.

### Ramp Texture Authoring Spec

| Property | Value |
| --- | --- |
| Resolution | 256 x 4 |
| Format | RGBA32 (PNG) |
| Color space | sRGB (importer ON) |
| Wrap | Clamp |
| Filter | Bilinear (Point for hard 2-tone) |
| Mipmap | OFF |
| Compression | None |

The U axis encodes `halfLambert = NdotL * 0.5 + 0.5`. U = 0 is fully
back-lit, U = 1 is fully front-lit. The V axis is unused; sample at
V = 0.5.

Ramps are generated programmatically by
`Editor/Tools/RampTextureGenerator.cs` rather than hand-painted. This
keeps importer settings consistent and makes ramp variants reproducible.

### Three-Stage Toon Shadow

DToon uses three shadow stages before texture ramps are introduced:

```hlsl
t1 = saturate(shadow1Range);
t2 = t1 * saturate(shadow2RangeOfShadow1);
t3 = t2 * saturate(shadow3RangeOfShadow2);

shadow3To2 = smoothstep(t3 - s3, t3 + s3, halfLambert);
shadow2To1 = smoothstep(t2 - s2, t2 + s2, halfLambert);
shadow1ToLit = smoothstep(t1 - s1, t1 + s1, halfLambert);
```

The color is blended in order: `Shadow 3 -> Shadow 2 -> Shadow 1 -> Lit`.
Each transition width is clamped so it cannot overlap the neighboring stage.

`Shadow 1 Range` is absolute. `Shadow 2 Range Of Shadow 1` is relative to
Shadow 1, and `Shadow 3 Range Of Shadow 2` is relative to Shadow 2. For
example, if Shadow 1 is `0.6` and Shadow 2 is `1.0`, Shadow 2 reaches `0.6`.
If Shadow 2 is `0.5`, it reaches `0.3`.

`Boundary Color` adds a controllable color band around the first and deep
shadow thresholds. The boundary color alpha is used as boundary strength. The
band width follows each stage's softness value.

### Brightness Range

DToon keeps characters readable by clamping the final lit color against the
base color.

```hlsl
minColor = baseColor * minBrightness;
maxColor = baseColor * maxBrightness;
result = min(max(litColor, minColor), maxColor);
```

`minBrightness` prevents characters from becoming fully black when lights are
off. `maxBrightness` prevents strong lights from washing out toon bands.

## Step 2: Received Shadows

URP provides realtime shadow attenuation per light. DToon first blends that
value back toward fully lit so each material can decide how strongly it
receives scene shadows.

```hlsl
strength = saturate(receiveShadows) * saturate(receivedShadowStrength);
shadowAttenuation = lerp(1.0, rawShadowAttenuation, strength);
```

The attenuation is then used as a tint blend rather than a black multiplier.

```hlsl
receivedShadowDiffuse = lerp(baseColor * shadowTint, celDiffuse, shadowAttenuation);
```

`Receive Shadows` can be set to `0` to ignore realtime shadows. `Received
Shadow Strength` keeps shadows present but lets the artist soften their impact
without changing the actual Unity light.

## Step 3: Inverted-Hull Outline

```hlsl
posOS_offset = posOS + normalOS * outlineWidth * distanceScale;
```

The mesh is expanded along normals and front faces are culled, leaving the
expanded back faces visible as a silhouette shell.

Hard normal seams can split the outline. The usual fix is to bake smoothed
normals into tangent, vertex color, or a spare UV channel and extrude with
those normals instead.

## Step 4: Rim Light

```hlsl
fresnel = 1 - saturate(dot(N, V));
rim = pow(fresnel, power) * intensity * rimColor;
```

Backlight-only rim:

```hlsl
rim *= saturate(-dot(N, L));
```

## Step 4: Matcap

```hlsl
normalVS = mul(UNITY_MATRIX_V, float4(normalWS, 0)).xyz;
matcapUV = normalVS.xy * 0.5 + 0.5;
matcap = tex2D(matcapTex, matcapUV).rgb;
```

Matcap textures bake a lighting response into a texture lookup.

## Step 4: Stepped Specular

```hlsl
H = normalize(L + V);
NdotH = saturate(dot(N, H));
specRaw = pow(NdotH, gloss * 128);
spec = smoothstep(0.5 - softness, 0.5 + softness, specRaw) * specColor;
```

The `smoothstep` creates a hard-edged anime highlight shape.

## Step 7: ILM Channels

An ILM texture packs artist controls into RGBA channels.

| Channel | Meaning |
| --- | --- |
| R | Per-region shadow threshold offset |
| G | Specular or highlight mask |
| B | Rim light mask |
| A | Outline width modulation |

## Step 8: SDF Face Shadow

The face artist authors an SDF texture in UV space. Each pixel stores how
close that region is to crossing the face shadow boundary.

```hlsl
lightOnPlane = normalize(lightDir - dot(lightDir, U) * U);
cosTheta = dot(lightOnPlane, F);
side = sign(dot(lightOnPlane, R));
sdf = side > 0 ? sampleR(uv) : sampleL(uv);
threshold = (1 - cosTheta) * 0.5;
shadowMask = step(threshold, sdf);
```

`F`, `R`, and `U` are the face forward, right, and up vectors.

## Step 9: Hair Anisotropic Highlight

```hlsl
T = normalize(tangentWS + shift1 * normalWS);
TdotH = sqrt(1 - dot(T, H) * dot(T, H));
spec1 = pow(TdotH, glossiness1) * color1;

T2 = normalize(tangentWS + shift2 * normalWS);
T2dotH = sqrt(1 - dot(T2, H) * dot(T2, H));
spec2 = pow(T2dotH, glossiness2) * color2;

hairSpec = (spec1 + spec2) * mask;
```

The shifts are usually driven by a painted or procedural texture to break the
highlight into hair strands.
