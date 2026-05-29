# Design Decisions Log

Records *why* something was decided, so future-you (or future-AI) doesn't
relitigate solved problems.

Format:
  ## YYYY-MM-DD — <topic>
  **Decision**: <what>
  **Alternatives considered**: <list>
  **Rationale**: <why this one>
  **Reversibility**: easy / medium / hard

---

## 2026-04-29 — Cel shading default: ramp texture (Option B)
**Decision**: Use 1D ramp textures for cel shading tone curve, not step
functions or shader-keyword switches.
**Alternatives considered**:
  - A: Step function only (no texture dependency)
  - C: Both, switched by shader keyword
**Rationale**: Ramps give artists per-character control over tone curves
without code changes. This is the NiloToon / HoYoverse standard. Step 7
will layer ILM on top to drive per-region ramp offsets.
**Reversibility**: medium — switching to step function means removing
_RampMap property and rewriting the inspector UI.

## 2026-04-29 — Shadow tint × ramp combination (Option Y)
**Decision**: Final color = albedo × ramp × shadowTint × lightColor ×
lerpedShadowFactor. ShadowTint defaults to (1,1,1) so it has no effect
unless the artist tweaks it.
**Alternatives considered**:
  - X: Ramp alone determines shadow tone (no separate ShadowTint slider)
  - Z: lerp(ramp, shadowTint, blend) — too unintuitive
**Rationale**: Lets artists make quick per-character tweaks without
re-authoring the ramp PNG. Default-white means the ramp drives the look
out of the box.
**Reversibility**: easy — drop _ShadowTint property if it goes unused.

## 2026-04-29 — Harness scene lighting (key light at (30, 60, 0))
**Decision**: Step 1 harness scene's directional light uses Rotation
(30, 60, 0).
**Alternatives considered**:
  - (50, -30, 0) — original; coincidentally near-aligned with camera
  - (50, 145, 0) — 3/4 backlight, dramatic but unusual
  - (45, -135, 0) — left-rear, also good
**Rationale**: Standard 3-point key light position. Produces NdotL across
the full [-1, 1] range on a centered sphere, so the entire ramp shows on
screen. Coincident light/camera directions caused false banding in the
first capture.
**Reversibility**: easy — change the scene's light Transform.

## 2026-05-02 — Ramp_Generic_Cool default stops (tightened)
**Decision**: Default Ramp_Generic_Cool uses stops at 0.00 / 0.45 /
0.50 / 0.55 / 1.00 with the cool blue-gray to white palette specified
in RampTextureGenerator.
**Alternatives considered**:
  - Original Phase 1 stops 0.35 / 0.50 / 0.65 — too soft, looked like
    PBR Lit not cel shading
  - Hard 2-tone (Ramp_HardCel_2Tone) — preserved as a separate ramp
    for hoyoverse-style hard cel use cases
**Rationale**: 0.10-wide transition band reads as a cel band on a
1024px sphere capture without aliasing harshly. Confirmed visually
by Dean via Step1_RampLit_Frontlit harness.
**Reversibility**: easy — change stops in RampTextureGenerator and
regenerate; existing reference would need to be re-approved.

## 2026-05-03 - Step 2 partial closeout: receive shadow visual deferred
**Decision**: Close Step 2 with AlphaClip_Hair as the only registered
reference. The ReceiveShadow_OnOff scene's visual validation is
deferred to character-import time.
**Alternatives considered**:
  - Continue diagnosing the shader function internals (instrument
    every intermediate variable in DToon_ToonDiffuse_Ramp)
  - Rewrite the shadow composition with a different formula
**Rationale**: All structural elements (CBUFFER access, keyword
variants, material toggles) are verified working. The visual
validation requires geometry that creates obvious self-occluding
shadows - a sphere-vs-cube test scene is geometrically marginal.
Real character meshes will produce that geometry naturally.
**Reversibility**: easy - re-open the diagnosis when a character
is imported. The shader code remains as currently written.

## 2026-05-03 - Two-renderer outline pattern (URP per-renderer pass limitation)
**Decision**: DToon outlines are rendered via a paired auxiliary
renderer using a separate DToon/Outline shader, not as a second pass
in DToon/Character. The OutlinePairCreator editor utility automates
the material pairing.
**Alternatives considered**:
  - Single shader with two passes - URP Forward Renderer does not
    auto-invoke custom-tagged passes from the same shader
  - Custom Renderer Feature - overkill for outline-only,
    complicates per-material toggling
**Rationale**: NiloToon and lilToon use the same workaround for the
same reason. Two-renderer keeps each pass simple, lets artists
toggle outline per-renderer, and avoids URP-specific renderer feature
authoring.
**Reversibility**: medium - changing to single-pass rendering would
require reauthoring DToon/Character to host the outline pass and
authoring a custom Renderer Feature to invoke it.

## 2026-05-03 - Step 4 Phase 1 partial closeout: light-aware rim visual deferred
**Decision**: Phase 1 ships with uniform rim as the verified default
for primitive scenes. Light-aware rim is implemented and code-path-
verified (RimOnly debug showed cyan band, LightAware debug showed
correct factor distribution) but visual validation with cel-shading +
outline composite is deferred to character import.
**Alternatives considered**: Continue tuning the verification scene
(8 cycles already attempted); rewrite the rim formula (RimOnly debug
confirmed formula works in isolation, formula is not the cause).
**Rationale**: Primitive sphere + directional light + outline +
light-aware rim is a four-way constraint that cannot simultaneously
satisfy all four on a primitive verification scene. Real character
imports will provide the geometry and lighting context where light-
aware rim is most visible.
**Reversibility**: easy - re-validate when first character mesh
imports.

## 2026-05-03 - Step 4 Phase 2 closeout: matcap with 4 default presets and 3 composition modes
**Decision**: Matcap implemented with Additive/Multiplicative/Lerp
composition modes selected via _MatcapMode property. 4 default
matcaps (Eye_Glossy, Metal_Chrome, Skin_Soft, Cloth_Velvet) generated
procedurally via MatcapGenerator editor utility, reproducible across
reimports.
**Alternatives considered**:
  - Single composition mode (Additive only) - too restrictive for
    varied surface types
  - Hand-painted default matcaps - non-reproducible, harder to iterate
**Rationale**: View-space normal matcap sampling is a NiloToon parity
feature for stylized eye highlights, metal surfaces, skin tones, and
special materials like velvet. The 3 composition modes cover the full
range of artistic uses. MatcapGenerator follows the RampTextureGenerator
pattern (Step 1) for default-asset reproducibility.
**Reversibility**: easy - material toggle disables matcap, generator can
be modified to produce different defaults.

## 2026-05-08 - Step 4 Phase 3: stepped specular formula + closeout
**Decision**: specMask = smoothstep(thr-soft, thr+soft, pow(NdotH,
power)). pow FIRST for the peaked Blinn-Phong shape, smoothstep
SECOND for cel-edge AA in specRaw space.
**Alternatives considered**: smoothstep on raw NdotH then pow (cycle 1
bug - produced broad lobes, mask approximately 1 across lit hemisphere);
hard step (aliases on curves).
**Rationale**: pow establishes the tight highlight; smoothstep only
anti-aliases the cel edge. Matches NiloToon/lilToon. Visual PASS on
x2-intensity 4-sphere harness, Claude-approved.
**Reversibility**: easy - one-block edit in ToonSpecular.hlsl.

## 2026-05-29 - Step 5 inspector layout: disabled controls remain visible
**Decision**: DToonCharacterGUI keeps inactive feature controls visible
but grayed out via disabled groups instead of hiding them.
**Alternatives considered**:
  - Hide inactive controls entirely
  - Keep every control editable regardless of feature toggle
**Rationale**: Artists can discover available controls without enabling
each feature, while disabled state still communicates which properties
currently affect rendering. This matches the Step 5 foldout spec and
keeps the inspector educational without changing material values.
**Reversibility**: easy - one-block GUI edit per foldout section.

## 2026-05-29 - Step 6 smooth-normal channel: UV4/TEXCOORD3
**Decision**: Keep the existing SmoothNormalBaker contract: averaged
object-space outline normals are stored in UV4 / shader TEXCOORD3 as
xyz, with w=1 as a valid-marker. Outline shaders consume that channel
only when _USE_SMOOTH_NORMAL is enabled.
**Alternatives considered**:
  - Tangent.xyz - rejected because Step 9 hair anisotropic needs real
    tangents and normal-map helpers already assume tangent/bitangent data
  - UV3 - functionally acceptable but would churn the existing baker
    implementation for no gain
  - Vertex color RGB - keep free for future artist masks/import data
**Rationale**: UV4 avoids tangent and common UV2/lightmap collisions,
matches the pre-existing baker implementation found in the repo, and
gives the outline pass an opt-in fallback-safe contract.
**Reversibility**: medium - changing channels would require touching the
baker, outline shader vertex inputs, and any baked meshes.
