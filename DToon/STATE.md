# Project State

Last updated: 2026-05-05 by codex

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
- Awaiting: Step 4 Phase 3 (stepped specular)

## What's not yet validated
- Cast shadow visual lerp on production fragment - code is correct
  by inspection but not confirmed visually with primitives.
- Light-aware rim visual validation with cel-shading + outline composite.
- To be re-validated when first character mesh imports.
