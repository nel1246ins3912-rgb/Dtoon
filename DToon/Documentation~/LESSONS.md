# Lessons Learned

A running log of bugs that wasted significant time and the lesson each
one taught. Add an entry every time a debugging session concludes with
"the real cause was something other than what I first suspected".

Format:
  YYYY-MM-DD | <symptom> | <real cause> | <how to detect next time>

---

2026-04-29 | Step 1 ramp shading showed horizontal banding | Camera and
directional light were nearly co-aligned (camera 0,0,-3 looking +Z;
light rotation 50,-30,0 also pointing roughly +Z). NdotL stayed in
[0.7, 1.0] across the visible hemisphere, compressing the ramp's bright
end onto the screen. | Run normal + light-direction + halfLambert debug
visualizations BEFORE suspecting the shader code. The halfLambert pass
will show as nearly uniform white when this happens.

2026-05-02 | Step 1 first-pass ramp default looked like PBR Lit, not
cel shading | Color stops 0.35/0.50/0.65 spread the transition band
over 30% of the ramp; on a smooth sphere this looked smoothly shaded
rather than cel-banded | When designing default ramps, narrow the
transition band to ≤10% of total range. Verify against
Ramp_HardCel_2Tone as a "what hard cel looks like" reference, then
tune softer until the band is just barely visible.

2026-05-03 | URP shadowAttenuation has multiple sources of "noise"
even with no caster | Distance fade, cascade transitions, shadow map
precision, and self-shadow bleeding all push attenuation below 1.0
by small amounts that are invisible in multiplicative use but
amplified when used as a lerp factor | Apply a noise floor (0.05)
before treating attenuation as a meaningful cast-shadow signal,
AND set _ReceiveShadowsStrength = 0 in materials that don't need
cast-shadow handling.

2026-05-03 | Step 2 cast-shadow visual check was unreliable on
geometry-marginal test scenes | Sphere-vs-cube and sphere-vs-quad
both produced inconsistent visual results despite all inputs
being correct | For shadow visual validation, use real character
geometry (hair-on-face) rather than primitive geometry. Primitives
give too few photons of certainty.

2026-05-03 | multi_compile_fragment vs multi_compile changes shader
variant set | Switching shadow keywords from multi_compile_fragment
to multi_compile changed which variants Unity produces; previous
debug captures may have been from different variant compilations |
When debugging shader variant behavior, ALWAYS recapture all
diagnostic visualizations after any keyword scope change. Old debug
captures from before the change are not comparable.

2026-05-03 | URP Forward Renderer doesn't auto-call multiple Pass
blocks from the same material if they have non-standard LightMode
tags | Custom passes only render via a Renderer Feature or a separate
material/renderer pair | For outline and similar effects, use
OutlinePairCreator pattern (paired auxiliary renderer with dedicated
shader). Document the pairing in any new harness scenes.

2026-05-03 | Outline thickness must be tuned per-asset class | Sphere
outline 0.005, hair strand outline 0.003 - different geometry needs
different absolute thicknesses to read as "right" | Default _OutlineWidth
is 0.005 for general use. For thin/dense geometry like hair strands
or eyelashes, drop to 0.003. For chunky props, may need to go up to
0.008-0.010.

2026-05-02 | Unity batch harness fails with project lock when Unity
GUI is open on the same project | Unity allows only one editor
instance per project; batchmode harness conflicts with an open GUI
session | When running test_shader.ps1, ensure the Unity Editor
GUI is closed for that project. If lock errors occur, close GUI
and retry. Document this in the harness usage guide.

2026-05-02 | Codex's "applied" reports cannot be trusted blindly -
verification must come from inspecting the actual file content |
During Step 2 ReceiveShadow debugging, Codex reported applying spec
changes multiple times, but only direct inspection of the .hlsl
files revealed the actual code state | After any non-trivial shader
spec, request the relevant .hlsl file content verbatim before
further diagnosis. "It compiled and ran" is not proof the spec was
implemented as written.

2026-05-02 | Step 1 regressed when castShadowFactor was injected
into ramp's UV coordinate | URP's shadowAttenuation noise (distance
fade, self-shadow bleeding) shifted rampU even on objects with no
real cast shadow, collapsing self-shading toward the ramp's shadow
region | Cast shadows should compose as a SEPARATE color
contribution (lerp toward darkRamp) rather than injecting into the
ramp's lookup coordinate. Self-shading and cast-shadow should
compose independently, not interfere.

2026-05-02 | Separate debug shaders may compile under different
variant sets than production shaders, leading to misleading
diagnostics | A custom shader that visualizes light.shadowAttenuation
can hit different keyword variants than the production shader's
URP14_VertexFragment, producing inconsistent values for the same
conceptual variable | When debugging URP shadow/light behavior,
instrument the EXISTING production fragment by temporarily
replacing its body, rather than authoring a parallel debug shader.
The keyword variant set must be identical.

2026-05-03 | Step 4 Phase 1 first capture made the body uniformly
dark even though rim worked correctly | Light rotation (30, 150, 0)
pointed the directional light away from the camera, so the ramp's
shadow region covered the camera-facing hemisphere, hiding the
cel-shading band that should appear under direct light | When
designing a harness scene to verify a single feature, keep the
body's existing cel-shading visible. The new feature should ADD to
a recognizable baseline, not replace it.

2026-05-03 | Light-aware rim weighting can fully zero the rim when
light and camera are aligned | The dot(-L, V) factor approaches 0
when the light shines from the camera's direction (typical
front-lit setup), making light-aware rim invisible | When designing
a verification scene for light-aware rim, the directional light
must have a meaningful angle BETWEEN camera-aligned (60 degrees)
and back-lit (180 degrees). Y rotation of 90-120 degrees works as
a portrait back-light compromise.

2026-05-03 | Rim mask formula combined fresnel * smoothstep(fresnel)
producing razor-thin rim band invisible against outline | Multiplying
fresnel twice (once raw, once through smoothstep) collapses the rim
to a 1-2 pixel edge that visually merges with the outline color,
making the rim appear missing | Use smoothstep for edge AA only,
do NOT re-multiply by fresnel afterward. Reference NiloToon and
lilToon rim formulas before authoring custom blends.

2026-05-03 | Rim verification capture appeared empty when rim color
was close to body albedo | Warm white rim (1, 0.95, 0.85) on pink
body (0.85, 0.55, 0.55) added onto the cel-shading's bright region
without visible contrast | For verification scenes, choose rim
color that contrasts with body albedo. Cool cyan or saturated
yellow against warm body works. Rim color choice is a verification
concern, not just artistic.

2026-05-03 | Rim light invisible despite working math because
outline hull geometry covered the silhouette pixels | The auxiliary
OutlineHull renderer's inflated mesh draws on top of where rim
would appear; narrow rim formulas produce 1-2 pixel rim bands that
sit entirely within the outline coverage area | When outline + rim
coexist, rim falloff (_RimPower) must be set so the rim band extends
INSIDE the outline. Power 0.5-1.0 with soft falloff works; Power
2.0+ produces silhouette-only rims that get hidden by outline.

2026-05-03 | Light-aware rim verification on a primitive sphere
with directional light is geometrically marginal | The dot(-L, V)
factor produces meaningful rim only when light direction is far
from view direction; primitive scenes have difficulty creating the
back-lit portrait setup where light-aware rim is most visible
without simultaneously breaking body cel-shading | Verify
light-aware features on real character meshes with proper
back-light setups. For primitive verification, use uniform rim
mode and treat light-aware as code-path-verified.

2026-05-03 | Material scalar properties may transiently read as 0
in the production fragment despite valid inspector values | Unity's
shader compilation cache or asset reimport timing can leave a window
where new scalar properties (rim intensity, matcap intensity, etc)
haven't fully propagated to the GPU; symptoms include features
appearing as if intensity = 0 even with verified material settings,
leading to multi-cycle visual debugging chasing the wrong cause |
When a feature appears silent and isolated math debug shows expected
output, IMMEDIATELY add an in-fragment grayscale visualization of the
relevant scalar prop (e.g. return half4(_FeatureIntensity.xxx, 1)).
If it reads 0 there but the inspector shows nonzero, force material
reimport via Assets > Reimport, or restart Unity. Do NOT work around
with texture-baked alternatives without confirming root cause.

2026-05-03 | Step 4 Phase 1 spent 8 cycles tuning rim that wasn't
receiving its intensity scalar | The "rim invisible" symptom was
caused by the transient CBUFFER scalar issue, but we hypothesized
scene/formula/outline causes and tuned those without verifying the
input | When verifying any composited shader feature visually, ALWAYS
run an in-fragment scalar visualization of the input controls FIRST
before tuning visual parameters. Five minutes of probe work prevents
many cycles of "looks subtle, try more intensity" loops.

2026-05-03 | Light-aware rim verification on primitive sphere is
geometrically impossible across all tested parameters | Power 0.7
(broad falloff) produces body-wide color overlay, not edge rim. Power
1.5+ (narrow falloff) produces silhouette pixels that the outline hull
renderer covers. No middle ground exists for the combination of
primitive sphere + outline + directional light + light-aware rim |
When the same constraint set across multiple attempts produces the
same failure mode, the constraint set itself is the problem - defer
visual verification to a different geometry context, do not continue
parameter tuning. Light-aware rim validates naturally on character
meshes (hair-on-face shadows, jaw-on-neck shadows) where outlines
don't dominate the silhouette.
