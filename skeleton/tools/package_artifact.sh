#!/usr/bin/env bash
# package_artifact.sh — shared zip-packaging step for tools/export_linux.sh and
# tools/export_windows.sh (DA P3 dedup, MC 1405): ONE implementation of the
# "binary + .NET assemblies data dir -> distributable zip" concern.
#
# The mono export writes the .NET assemblies to data_<Assembly>_<arch>/ beside
# the binary, and the platform template loader loads hostfxr + the assemblies
# from there — a binary-only zip cannot launch, so the data dir is mandatory
# payload and its absence must fail the gate here.
#
# Usage: bash tools/package_artifact.sh <out_zip> <out_exe> <data_dir> <label>
set -euo pipefail

OUT_ZIP="${1:?out_zip}"
OUT_EXE="${2:?out_exe}"
DATA_DIR="${3:?data_dir}"
LABEL="${4:?label}"

rm -f "$OUT_ZIP"
# `zip` is not installed on this host; python3's zipfile is the stdlib fallback.
python3 - "$OUT_ZIP" "$OUT_EXE" "$DATA_DIR" "$LABEL" <<'PY'
import os, sys, zipfile
out_zip, out_exe, data_dir, label = sys.argv[1], sys.argv[2], sys.argv[3], sys.argv[4]
with zipfile.ZipFile(out_zip, "w", zipfile.ZIP_DEFLATED) as z:
    z.write(out_exe, arcname=os.path.basename(out_exe))
    for root, _dirs, files in os.walk(data_dir):
        for f in sorted(files):
            p = os.path.join(root, f)
            z.write(p, arcname=os.path.relpath(p, os.path.dirname(data_dir)))
# The gate must go red if either half of the payload is missing from the zip.
names = zipfile.ZipFile(out_zip).namelist()
if os.path.basename(out_exe) not in names:
    sys.exit(f"{label}: FAIL: {os.path.basename(out_exe)} missing from {out_zip}")
if not any(n.startswith("data_") and n.endswith("/LastAnimalPreflight.dll") for n in names):
    sys.exit(f"{label}: FAIL: data_*/LastAnimalPreflight.dll missing from {out_zip}")
print(f"{label}: zip payload OK — {len(names)} entries (binary + data dir)")
PY
[ -s "$OUT_ZIP" ] || { echo "$LABEL: FAIL: $OUT_ZIP is empty" >&2; exit 1; }
