#!/usr/bin/env python3
# ============================================================================
#  compare.py
#  ----------------------------------------------------------------------------
#  Pixel-level comparison between a freshly captured harness frame and a
#  human-approved reference frame.
#
#  Usage:
#      python compare.py <current.png> <reference.png> [--threshold 0.02]
#                                                      [--diff-out diff.png]
#
#  Exit codes:
#      0 - PASS (within threshold)
#      1 - FAIL (RMSE exceeds threshold)
#      2 - ERROR (shape mismatch, missing file, etc.)
#
#  Dependencies:
#      pip install pillow numpy
# ============================================================================

import argparse
import sys
from pathlib import Path

try:
    import numpy as np
    from PIL import Image
except ImportError as e:
    print(f"ERROR: missing dependency. Run: pip install pillow numpy", file=sys.stderr)
    print(f"  {e}", file=sys.stderr)
    sys.exit(2)


def load_image(path: Path) -> np.ndarray:
    if not path.exists():
        raise FileNotFoundError(f"Image not found: {path}")
    img = Image.open(path).convert("RGB")
    return np.asarray(img, dtype=np.float32) / 255.0


def main() -> int:
    parser = argparse.ArgumentParser(description="Compare two PNG images for shader regression testing.")
    parser.add_argument("current", type=Path, help="Path to the freshly captured PNG.")
    parser.add_argument("reference", type=Path, help="Path to the approved reference PNG.")
    parser.add_argument("--threshold", type=float, default=0.02,
                        help="RMSE threshold (default 0.02). Lower = stricter.")
    parser.add_argument("--diff-out", type=Path, default=None,
                        help="Optional path to write a visualized diff image on failure.")
    args = parser.parse_args()

    try:
        cur = load_image(args.current)
        ref = load_image(args.reference)
    except FileNotFoundError as e:
        print(f"ERROR: {e}")
        return 2

    if cur.shape != ref.shape:
        print(f"ERROR: shape mismatch. current={cur.shape} reference={ref.shape}")
        return 2

    diff = np.abs(cur - ref)
    rmse = float(np.sqrt(np.mean(diff ** 2)))
    max_pixel_diff = float(np.max(diff))
    pct_pixels_off = float(np.mean(np.any(diff > 0.05, axis=-1)) * 100.0)

    print(f"--- compare.py ---")
    print(f"current       : {args.current}")
    print(f"reference     : {args.reference}")
    print(f"RMSE          : {rmse:.6f}")
    print(f"max pixel diff: {max_pixel_diff:.6f}")
    print(f"% pixels >5%  : {pct_pixels_off:.2f}%")
    print(f"threshold     : {rmse:.6f} <= {args.threshold:.6f} ?")

    passed = rmse <= args.threshold

    if not passed:
        if args.diff_out is not None:
            # Amplify diff for visibility
            diff_vis = np.clip(diff * 5.0, 0.0, 1.0)
            Image.fromarray((diff_vis * 255).astype(np.uint8)).save(args.diff_out)
            print(f"diff image    : {args.diff_out}")
        print("RESULT: FAIL")
        return 1

    print("RESULT: PASS")
    return 0


if __name__ == "__main__":
    sys.exit(main())
