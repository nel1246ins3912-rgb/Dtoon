# Project State

Last updated: 2026-05-29 by codex

## Where we are
- Step 4 Phase 1: PARTIAL DONE
  - Rim infrastructure complete (uniform + light-aware modes)
  - Step4_Rim_Closeup reference registered (uniform mode)
  - Light-aware visual deferred to character import (verified
    infeasible on primitive sphere across all tested params)
- Step 4 Phase 2: DONE
  - Matcap infrastructure complete (Add/Mul/Lerp modes, 4 default
    matcaps, MatcapGenerator utility)
  - Step4_Matcap_Closeup reference registered
- Step 4 Phase 3: DONE
  - Stepped specular infrastructure complete
  - Step4_Specular_Closeup reference registered
- Step 4: COMPLETE
  - Phase 1: PARTIAL DONE
  - Phase 2: DONE
  - Phase 3: DONE
- Step 5: DONE
  - DToonCharacterGUI custom inspector
  - 7 grouped foldouts, keyword-synced enable toggles
  - Dean-QA confirmed
- Step 6: DONE
  - SmoothNormalBaker pre-existing UV4 baker wired into outline shaders
  - Cube bake math verified by HarnessOutput/SmoothNormal_CubeReport.txt
  - Existing harness references still exit 0 with smooth normals disabled
- Awaiting: Step 7 (ILM texture system)

## What's not yet validated
- Cast shadow visual lerp on production fragment - code is correct
  by inspection but not confirmed visually with primitives.
- Light-aware rim visual validation with cel-shading + outline composite.
- Smooth-normal seam-closing visual validation on a hard-edged character mesh.
- To be re-validated when first character mesh imports.
