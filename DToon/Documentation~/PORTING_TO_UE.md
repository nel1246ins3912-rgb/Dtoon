# Porting DToon To Unreal Engine

Unity shader files cannot be copied directly into Unreal Engine. The portable
parts are the algorithms, texture conventions, and material parameter design.

## Conceptual Mapping

| Unity / DToon | Unreal Engine 5 |
| --- | --- |
| ShaderLab Properties | Material Parameters |
| HLSL function | Material Function |
| `SAMPLE_TEXTURE2D` | Texture Sample node |
| URP main light input | Custom lighting input or controlled light vector |
| Custom ShaderGUI | Material Instance parameter groups |
| `multi_compile` | Static Switch parameters |
| ScriptableRendererFeature | Post Process Material or custom render code |
| Inverted-hull outline | Second material or duplicated mesh with normal offset |

## Module Mapping

### `ToonCore.hlsl`

Structs do not map directly into Unreal materials. Convert the same fields
into Material Function inputs such as `Albedo`, `NormalWS`, `ViewDirWS`, and
`PositionWS`.

### `ToonLighting.hlsl`

Rebuild `DToon_StepRamp` as a Material Function:

```text
Dot(Normal, LightVector)
Multiply 0.5
Add 0.5
SmoothStep(Threshold - Softness, Threshold + Softness)
Lerp(ShadowColor, LitColor, T)
```

For ramp textures, sample with:

```text
UV = (NdotL * 0.5 + 0.5 + Offset, 0.5)
```

### `ToonOutline.hlsl`

Use a second material or duplicated mesh:

```text
World Position Offset = Normal * OutlineWidth * CameraDistanceScale
```

Render the expanded shell with front-face culling or equivalent material
settings.

### `ToonRimLight.hlsl`

```text
Power(1 - Saturate(Dot(Normal, CameraVector)), Power)
Multiply Intensity
Multiply RimColor
```

### `ToonMatcap.hlsl`

Transform the normal into view space, use `xy * 0.5 + 0.5`, then sample the
matcap texture.

### `ToonFaceSDF.hlsl`

Use per-character vectors for face forward, right, and up. These can come
from Blueprint parameters or Custom Primitive Data. The SDF math stays the
same as the Unity version.

## What Does Not Translate Directly

- Multi-pass ShaderLab shaders.
- Unity-specific renderer features.
- Unity material inspector code.
- Unity mesh import and tangent baking assumptions.

## Suggested Porting Order

1. Cel diffuse as a Material Function.
2. Inverted-hull outline as a second material.
3. Rim, matcap, and stepped specular.
4. Material Instance template.
5. SDF face shadow.
6. Hair anisotropic highlight.
