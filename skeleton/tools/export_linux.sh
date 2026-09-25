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
rm -f "$OUT_ZIP"
# The mono export writes the .NET assemblies to data_<Assembly>_<arch>/ beside
# the binary, and the Linux template loader loads hostfxr + the assemblies from
# there — a binary-only zip cannot launch, so the data dir is mandatory payload
# and its absence must fail the gate here.
DATA_DIR="$(cd build && ls -d data_*_linuxbsd_x86_64 2>/dev/null | head -1 || true)"
[ -n "$DATA_DIR" ] || { echo "EXPORT_LINUX: FAIL: no .NET assemblies data dir (build/data_*_linuxbsd_x86_64) beside $OUT_EXE" >&2; exit 1; }
# `zip` is not installed on this host; python3's zipfile is the stdlib fallback.
python3 - "$OUT_ZIP" "$OUT_EXE" "build/$DATA_DIR" <<'PY'
import os, sys, zipfile
out_zip, out_exe, data_dir = sys.argv[1], sys.argv[2], sys.argv[3]
with zipfile.ZipFile(out_zip, "w", zipfile.ZIP_DEFLATED) as z:
    z.write(out_exe, arcname=os.path.basename(out_exe))
    for root, _dirs, files in os.walk(data_dir):
        for f in sorted(files):
            p = os.path.join(root, f)
            z.write(p, arcname=os.path.relpath(p, os.path.dirname(data_dir)))
# The gate must go red if either half of the payload is missing from the zip.
names = zipfile.ZipFile(out_zip).namelist()
if os.path.basename(out_exe) not in names:
    sys.exit(f"EXPORT_LINUX: FAIL: {os.path.basename(out_exe)} missing from {out_zip}")
if not any(n.startswith("data_") and n.endswith("/LastAnimalPreflight.dll") for n in names):
    sys.exit(f"EXPORT_LINUX: FAIL: data_*/LastAnimalPreflight.dll missing from {out_zip}")
print(f"EXPORT_LINUX: zip payload OK — {len(names)} entries (binary + data dir)")
PY
[ -s "$OUT_ZIP" ] || { echo "EXPORT_LINUX: FAIL: $OUT_ZIP is empty" >&2; exit 1; }

echo "EXPORT_LINUX: step 3/3 — native packaged-binary launch smoke (Xvfb)"
bash "$HERE/launch_linux_smoke.sh"

echo "EXPORT_LINUX: PASS — $OUT_ZIP ($(stat -c%s "$OUT_ZIP") bytes)"
