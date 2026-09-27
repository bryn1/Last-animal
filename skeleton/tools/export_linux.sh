#!/usr/bin/env bash
# export_linux.sh — Linux counterpart of tools/export_windows.sh for Last
# Animal (MC 1344 follow-up, 2026-09-25).
#
# Proves, in order:
#   (1) the FULL-GAME Linux release export produces an ELF binary carrying the
#       C# assembly (delegated to ci/export_check.sh, which owns those checks);
#   (2) the artifact is packaged into a distributable .zip — the binary AND the
#       .NET assemblies data dir beside it (the Linux mono template loads
#       hostfxr + the assemblies from disk, so an exe-only zip cannot launch);
#   (3) a NATIVE launch smoke of the packaged binary under Xvfb — delegated to
#       tools/launch_linux_smoke.sh (no wine involved; see the Wine 9.0 known
#       issue in docs/build-and-run.md).
#
# Usage:  bash tools/export_linux.sh
set -euo pipefail

HERE="$(cd "$(dirname "$0")" && pwd)"
PROJ="$(cd "$HERE/.." && pwd)"
OUT_EXE="build/LastAnimal.x86_64"
OUT_ZIP="build/LastAnimal-linux-x86_64.zip"

# shellcheck source=../ci/toolchain.sh
source "$PROJ/ci/toolchain.sh"
: "${GODOT:?set GODOT to the engine binary (see engine/PIN.txt or source ci/toolchain.sh)}"

echo "EXPORT_LINUX: step 1/3 — full-game export gate (ci/export_check.sh)"
bash "$PROJ/ci/export_check.sh" Linux "$PROJ" "$OUT_EXE"

cd "$PROJ"
echo "EXPORT_LINUX: step 2/3 — packaging $OUT_EXE + .NET data dir -> $OUT_ZIP"
DATA_DIR="$(cd build && ls -d data_*_linuxbsd_x86_64 2>/dev/null | head -1 || true)"
[ -n "$DATA_DIR" ] || { echo "$(basename "$0" .sh | tr a-z A-Z): FAIL: no .NET assemblies data dir (build/data_*_linuxbsd_x86_64) beside $OUT_EXE" >&2; exit 1; }
# Packaging is the shared helper (DA P3 dedup, MC 1405): one implementation
# of the binary + data-dir zip concern for both platforms.
bash "$HERE/package_artifact.sh" "$OUT_ZIP" "$OUT_EXE" "build/$DATA_DIR" "EXPORT_LINUX"

echo "EXPORT_LINUX: step 3/3 — native packaged-binary launch smoke (Xvfb)"
bash "$HERE/launch_linux_smoke.sh"

echo "EXPORT_LINUX: PASS — $OUT_ZIP ($(stat -c%s "$OUT_ZIP") bytes)"
