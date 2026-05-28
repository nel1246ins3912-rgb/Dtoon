#!/usr/bin/env bash
# ============================================================================
#  test_shader.sh
#  ----------------------------------------------------------------------------
#  macOS / Linux equivalent of test_shader.ps1.
#  Usage: ./tools/test_shader.sh <TestName> [--compile-only] [--update-reference]
# ============================================================================
set -euo pipefail

TEST_NAME="${1:-}"
if [ -z "$TEST_NAME" ]; then
    echo "Usage: $0 <TestName> [--compile-only] [--update-reference]" >&2
    exit 2
fi
shift || true

COMPILE_ONLY=0
UPDATE_REFERENCE=0
for arg in "$@"; do
    case "$arg" in
        --compile-only)     COMPILE_ONLY=1 ;;
        --update-reference) UPDATE_REFERENCE=1 ;;
        *) echo "Unknown arg: $arg" >&2; exit 2 ;;
    esac
done

# Default Unity locations; override via env var if needed.
if [ -z "${UNITY_EXE:-}" ]; then
    if [ "$(uname)" = "Darwin" ]; then
        UNITY_EXE="/Applications/Unity/Hub/Editor/2022.3.40f1/Unity.app/Contents/MacOS/Unity"
    else
        UNITY_EXE="$HOME/Unity/Hub/Editor/2022.3.40f1/Editor/Unity"
    fi
fi

PROJECT_PATH="$(pwd)"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PACKAGE_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
OUTPUT_DIR="$PROJECT_PATH/HarnessOutput"
REFERENCE_DIR="$PACKAGE_ROOT/Samples/Harness/References"
CURRENT_PNG="$OUTPUT_DIR/$TEST_NAME.png"
REFERENCE_PNG="$REFERENCE_DIR/$TEST_NAME.png"
DIFF_PNG="$OUTPUT_DIR/$TEST_NAME.diff.png"
LOG_FILE="$OUTPUT_DIR/$TEST_NAME.log"
THRESHOLD="${THRESHOLD:-0.02}"

mkdir -p "$OUTPUT_DIR" "$REFERENCE_DIR"

if [ ! -x "$UNITY_EXE" ] && [ ! -f "$UNITY_EXE" ]; then
    echo "ERROR: Unity not found at $UNITY_EXE (set UNITY_EXE env var)." >&2
    exit 2
fi

if [ "$COMPILE_ONLY" = "1" ]; then
    METHOD="DToon.Editor.Harness.HarnessRunner.CompileCheck"
else
    METHOD="DToon.Editor.Harness.HarnessRunner.RenderTest"
fi

echo "==> [$TEST_NAME] Method: $METHOD"
"$UNITY_EXE" \
    -batchmode \
    -projectPath "$PROJECT_PATH" \
    -executeMethod "$METHOD" \
    -testName "$TEST_NAME" \
    -outputPath "$CURRENT_PNG" \
    -logFile "$LOG_FILE" \
    -quit
UNITY_EXIT=$?

if [ $UNITY_EXIT -ne 0 ]; then
    echo "==> Unity exited with $UNITY_EXIT. Tail of log:" >&2
    tail -n 30 "$LOG_FILE" >&2 || true
    exit 2
fi

if [ "$COMPILE_ONLY" = "1" ]; then
    echo "==> [$TEST_NAME] CompileCheck PASS"
    exit 0
fi

if [ "$UPDATE_REFERENCE" = "1" ]; then
    cp "$CURRENT_PNG" "$REFERENCE_PNG"
    echo "==> [$TEST_NAME] Reference updated -> $REFERENCE_PNG"
    exit 0
fi

if [ ! -f "$REFERENCE_PNG" ]; then
    echo "==> [$TEST_NAME] No reference yet at $REFERENCE_PNG"
    echo "    Inspect $CURRENT_PNG. If correct, re-run with --update-reference."
    exit 1
fi

python3 "$PACKAGE_ROOT/tools/compare.py" \
    "$CURRENT_PNG" "$REFERENCE_PNG" \
    --threshold "$THRESHOLD" \
    --diff-out "$DIFF_PNG"
exit $?
