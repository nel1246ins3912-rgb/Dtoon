# DToon

Anime-style toon shader package for Unity URP, designed with future Unity 6
and Unreal Engine migration in mind.

Status: Step 2. The current shader applies cel shading with controllable
received shadows.

## Layout

```text
DToon/
  Runtime/
    Shaders/                 ShaderLab entry points
    ShaderLibrary/           Engine-independent HLSL math
    PipelineAdapter/
      URP14/                 Current Unity 2022 LTS target
      URP17/                 Reserved for Unity 6
  Editor/
    ShaderGUI/               Custom material inspectors
    Tools/                   Authoring tools
    Harness/                 Batch-mode visual test runner
  Samples/                   Active harness assets when copied under Assets
  Samples~/                  UPM distribution samples
  Documentation~/            Porting and algorithm notes
  tools/                     Test scripts and image comparison helpers
```

## Unity Setup

For normal shader-only testing, this folder can be added as a local Unity
package:

1. Open a Unity 2022.3 LTS URP project.
2. Open Package Manager.
3. Choose `Add package from disk...`.
4. Select `DToon/package.json`.
5. Create a material and set its shader to `DToon/Character`.

For harness-based visual testing, copy or place the folder at:

```text
Assets/DToon
```

The current harness scripts and `HarnessRunner.cs` assume that path. We can
make the harness package-path aware later.

## Current Shaders

- `DToon/Character`: cel shaded character baseline with received shadows.
- `DToon/Outline`: no-op outline placeholder for Step 3.

## Step Plan

1. Step 0: structure, asmdefs, baseline shader, harness scaffolding.
2. Step 1: cel shading with threshold and softness.
3. Step 2: main light shadows.
4. Step 3: inverted-hull outline.
5. Step 4: rim light, matcap, and stepped specular.
6. Step 5: custom material inspector.
7. Step 6: smooth normal baking.
8. Step 7: ILM texture channels.
9. Step 8: SDF face shadow.
10. Step 9: hair anisotropic highlight.
11. Step 10: URP14 renderer feature for outline passes.

## Design Rule

Keep portable toon math in `Runtime/ShaderLibrary`.

Keep Unity or URP-specific code in `Runtime/PipelineAdapter`.

That split is the main reason this package can later move to Unity 6 or be
rebuilt as Unreal Material Functions.
