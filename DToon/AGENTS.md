# AGENTS.md - DToon Shader Package

This package is a Unity URP toon shader project. Keep edits incremental and
verify shader behavior with the harness when a Unity project is available.

## 1. Scope

The source package lives under `DToon/`.

When copied into a Unity project for harness testing, it should live at:

```text
Assets/DToon
```

The current harness scripts assume that `Assets/DToon` path. Local Package
Manager import is fine for shader-only checks, but the visual harness is not
package-path aware yet.

## 2. Verification

For every change to a shader file (`.shader`, `.hlsl`) or renderer feature:

1. Make the edit.
2. Run the relevant harness test when Unity is available.
3. Read the output log and fix shader compile errors before moving on.
4. Do not update reference images unless the user approves the new look.
5. The first visual run will usually fail because there is no reference image
   yet. Inspect the generated PNG, then update the reference only with
   approval.
6. **After completing or hitting a blocker, prepend an entry to
   Documentation~/HANDOFF.md** following the format documented there.
   End with a "## Codex → Claude" section if anything needs Claude's
   visual review or design judgment.

Windows harness command from a Unity project root:

```powershell
.\Assets\DToon\tools\test_shader.ps1 -TestName Step0_UnlitBaseline
```

## 3. Folder Rules

| Path | Rule |
| --- | --- |
| `Runtime/ShaderLibrary/*.hlsl` | Engine-independent math only. Do not include URP or HDRP headers here. |
| `Runtime/PipelineAdapter/URP14/` | Unity 2022 LTS / URP 14 specific code. |
| `Runtime/PipelineAdapter/URP17/` | Reserved for Unity 6 / URP 17 work. |
| `Runtime/Shaders/*.shader` | Thin ShaderLab wrappers and pass declarations. |
| `Editor/` | Editor-only C# code. |
| `Samples/Harness/` | Active harness scenes when under `Assets/DToon`. |
| `Samples/Harness/References/` | Approved reference PNGs. |
| `Samples~/` | UPM sample folder. Hidden from direct Unity import. |
| `HarnessOutput/` | Temporary harness output. Git-ignored. |

If code needs URP APIs such as `GetMainLight`, it belongs under
`PipelineAdapter/URP14`, not `ShaderLibrary`.

## 4. Coding Conventions

HLSL:

- Every `.hlsl` file uses include guards.
- Public functions use the `DToon_` prefix.
- Use `half` for color and lighting math.
- Use `float` for positions, UV transforms, and clip-space operations.
- Add a short porting note when the math should later become an Unreal
  Material Function.

ShaderLab:

- Properties use `_PascalCase`.
- Passes have explicit names.
- URP shader keywords are listed intentionally, even before every feature is
  wired in.

C#:

- Namespace root is `DToon`.
- Editor-only code stays under `Editor/`.
- `EditorApplication.Exit` should only be called from `HarnessRunner.cs`.

## 5. Ramp Texture Rules

### Where ramp textures live

All ramp textures go in `Samples/RampTextures/`. Naming:
`Ramp_<Region>_<Variant>.png` where Region is one of
`Generic | Skin | Hair | Cloth | <CharacterName>` and Variant describes
the style, for example `Cool`, `Warm`, `2Tone`, or `4Tone`.

### How to create or edit a ramp

Never hand-edit ramp PNGs in an external image editor. Always use the
generator at `Tools/DToon/Generate Default Ramp Textures`, or extend
`RampTextureGenerator.cs` with a new `RampDefinition`. This guarantees
the importer settings stay correct.

### Importer settings - never change manually

The following must always be true for ramp textures:
sRGB ON, no alpha, no mipmap, wrap Clamp, no compression, not readable.
If you find these out of sync, regenerate via the menu - do not patch
them in the inspector.

## 6. Step Plan

| Step | Status | Adds |
| --- | --- | --- |
| 0 | done | Folder structure, asmdefs, baseline unlit shader, harness scaffolding |
| 1 | done | Cel shading via 1D ramp texture, M_DToon_Test_Generic + Ramp_Generic_Cool, harness reference registered. |
| 2 | partial done | Alpha clipping (hair shadow silhouette) registered as reference. Receive shadow strength validated through CBUFFER and material toggle; visual effect deferred to Step 6+ character import for natural re-validation. |
| 3 | done | Inverted-hull outline pass with albedo darkening, screen-space constant thickness, alpha-clip support. Implemented as DToon/Outline shader paired with main DToon/Character via OutlinePairCreator. Outline applied to all baseline scenes (Step1, Step2, Step3). |
| 4 Phase 1 | partial done | Rim light infrastructure (DToon_RimMask, uniform + light-aware modes, _RimColor with HDR support, _RimPower/Intensity/Softness properties). Step4_Rim_Closeup reference registered with uniform mode. Light-aware visual deferred to character import. |
| 4 Phase 2 | done | Matcap with Additive/Multiplicative/Lerp composition modes, view-space normal sampling, MatcapGenerator utility producing 4 default matcaps (Eye_Glossy, Metal_Chrome, Skin_Soft, Cloth_Velvet). Step4_Matcap_Closeup reference registered. Phase 1 PARTIAL retained. |
| 4 Phase 3 | done | Stepped specular: pow(NdotH,power) Blinn-Phong peak then smoothstep AA in specRaw space. Per-material Power/Threshold/Softness/Intensity. 4 test materials (Metal/Skin/Plastic/Hair) with differentiated highlights. Step4_Specular_Closeup reference registered. |
| 5 | in progress | `DToonCharacterGUI` custom material inspector authored with 7 foldouts (Base, Cel Shading, Outline, Rim, Matcap, Specular, Alpha Clip), explicit keyword sync, and disabled groups for inactive features. Harness regressions pass; pending Dean inspector visual QA. |
| 6 | pending | `SmoothNormalBaker` editor tool |
| 7 | pending | ILM texture system |
| 8 | pending | SDF face shadow |
| 9 | pending | Hair anisotropic highlight |
| 10 | pending | URP14 renderer feature for outline pass |

## 7. Harness Capture Workflow

Prefer Unity menu captures for quick visual diagnostics:

```text
Tools/DToon/Harness/Capture Step1 RampLit Frontlit
```

PowerShell harness runs are still valid for batch-mode checks, but menu
captures are easier for iterative visual debugging inside Unity.

### Harness scene lighting checklist

- The directional light should NOT face the same direction as the camera. Aim
  for a key-light rotation around (30, 60, 0) so NdotL varies across the full
  [-1, 1] range on a centered sphere.
- This is critical for cel-shading harness scenes - coincident light/camera
  directions compress the ramp into a tiny region of the surface and produce
  false "banding" diagnostics.

## 8. When you're stuck

Stop changing shader code and isolate the rendering layer that is actually
wrong. Prefer one-frame diagnostic outputs over speculative rewrites. If a
diagnostic capture contradicts the current theory, update the theory before
touching more code.

## 9. Shader debugging discipline

When a shader produces an unexpected visual result, **suspect the scene
setup before suspecting the shader code.** This is non-negotiable. The
agent's instinct to immediately re-read shader code is wrong here - it
wastes hours.

### Standard diagnostic order

Run these diagnostics IN ORDER. Each one isolates a different layer. Stop at
the first one that reveals an anomaly.

1. **Normal visualization.** Replace the fragment body with
   `return half4(normalize(IN.normalWS) * 0.5h + 0.5h, 1);`.
   Expected: smooth rainbow gradient on the test sphere. Banding, seams, or
   solid colors mean the mesh or vertex pipeline is broken.
2. **Light direction visualization.** Replace fragment body with
   `return half4(light.directionWS * 0.5 + 0.5, 1);`.
   Expected: a single uniform color across the entire surface because a
   directional light gives every fragment the same L. The color should match
   the scene's directional light rotation. If it does not, the URP14_Inputs
   adapter is wrong; if it does, the scene rotation is wrong.
3. **HalfLambert visualization.** Replace fragment body with grayscale
   halfLambert:
   `half hl = dot(N, L) * 0.5 + 0.5; return half4(hl, hl, hl, 1);`.
   Expected: a smooth grayscale gradient covering the FULL [0, 1] range -
   black on the back-lit side, white on the front-lit side. If most of the
   surface is near-white or near-black, the camera and light are too aligned;
   rotate the light.
4. **Ramp UV visualization** only after step 3 passes. Output
   `return half4(rampU, rampU, rampU, 1);` to confirm UV math is sweeping the
   full ramp.
5. **Ramp sample visualization.** Output the raw ramp sample without any
   further multiplication. This isolates ramp texture issues from composition
   issues.

Only after all five pass, suspect the lighting composition code.

### Scene-side preconditions

Before running ANY harness test for the first time, confirm:

- Camera and directional light are NOT aligned. The angle between
  `camera.forward` and `light.forward` should be > 60 degrees. Otherwise the shading
  range collapses to a tiny slice of the ramp and produces false "banding"
  results.
- Camera FOV is between 25 and 50. Below 25 makes the test sphere look almost
  orthographic and hides perspective issues; above 50 exaggerates them.
- Background is a NEUTRAL mid-gray, not pure black or white. Pure black hides
  outline-vs-background issues; pure white blows out highlights.
- Post-processing is OFF. Bloom and tonemapping change the captured PNG and
  break determinism.
- HDR is OFF on the harness camera. Reference PNGs are 8-bit sRGB.

### When the diagnosis says "scene is wrong"

Do NOT try to compensate in the shader. Fix the scene. The shader's job is to
render correctly given valid scene inputs; it should not have to be defensive
against misconfigured lights or cameras.

### Visual review handoff

When you produce a PNG that requires visual judgment (cel-shading
quality, shadow color, lighting feel), do NOT register it as a
reference yourself. Append a HANDOFF.md entry pointing to the PNG and
stop. Dean will show the PNG to Claude for visual review, and Claude
will respond via HANDOFF.md.

### Lessons-learned log

When a shader bug turns out to have been a scene-setup issue or any
non-shader cause, append a one-line entry to `Documentation~/LESSONS.md` so
the same trap is not stepped into twice. Format:
`<date> | <symptom> | <real cause> | <how to detect next time>`.

## 10. Migration Discipline

For Unity 6 / URP 17, the goal is to rewrite only the adapter layer where
possible.

For Unreal Engine, the goal is to rebuild the same math as Material Functions
and Material Instances. Do not treat Unity ShaderLab files as portable; only
the algorithms and texture conventions are portable.

## 11. Lessons Learned

Keep `Documentation~/LESSONS.md` updated when a debugging session reveals a
misleading symptom or a non-shader root cause.
