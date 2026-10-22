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
# Usage: bash tools/package_artifact.sh <out_zip> <out_exe> <data_dir> <label> \
#        [extra_file ...]
# Optional extra files (MC 11129: the Windows package carries the console
# wrapper + the two playtest launchers) ride the zip TOP LEVEL beside the
# binary, and the gate goes red if any of them is missing from the zip.
#
# MC 11129 FIX-ROUND-1 (DA stage-hygiene P2): the SAME packaging concern also
# owns the integrity manifest of a STAGED payload dir (an unzipped delivery
# folder: exe + data dir + launchers + one-pager), so the owner can prove
# what landed. Manifest mode:
#   bash tools/package_artifact.sh --sums <dir>
# writes <dir>/SHA256SUMS.txt — per-file sha256, relative paths,
# `sha256sum -c` compatible — over EVERY file in the tree (the manifest
# excludes itself by name), then RE-READS the written lines and re-hashes
# every file: any mismatch (or a file that vanished between write and read)
# goes red with a nonzero exit. ONE implementation, reused by the staging
# step — not hand-rolled.
if [ "${1:-}" = "--sums" ]; then
  DIR="${2:?dir}"
  python3 - "$DIR" <<'PY'
import hashlib, os, sys

d = sys.argv[1]
MANIFEST = "SHA256SUMS.txt"

def digest(path):
    h = hashlib.sha256()
    with open(path, "rb") as fh:
        for chunk in iter(lambda: fh.read(1 << 20), b""):
            h.update(chunk)
    return h.hexdigest()

files = []
for root, _dirs, names in os.walk(d):
    for n in sorted(names):
        rel = os.path.relpath(os.path.join(root, n), d)
        if rel == MANIFEST:
            continue
        files.append(rel)
files.sort()
with open(os.path.join(d, MANIFEST), "w", newline="\n") as out:
    for rel in files:
        out.write(f"{digest(os.path.join(d, rel))}  {rel}\n")
want = {}
with open(os.path.join(d, MANIFEST)) as f:
    for line in f:
        h, sep, rel = line.rstrip("\n").partition("  ")
        if sep:
            want[rel] = h
missing = [rel for rel in files if rel not in want]
mismatch = [rel for rel in files if want.get(rel) not in (None,)
            and want[rel] != digest(os.path.join(d, rel))]
if missing or mismatch:
    sys.exit(f"--sums: FAIL: manifest lost {len(missing)} lines, {len(mismatch)} files "
             f"do not re-hash to the lines just written: {(missing + mismatch)[:5]}")
print(f"--sums: {len(files)} files hashed + re-read verified -> {os.path.join(d, MANIFEST)}")
PY
  exit 0
fi
set -euo pipefail

OUT_ZIP="${1:?out_zip}"
OUT_EXE="${2:?out_exe}"
DATA_DIR="${3:?data_dir}"
LABEL="${4:?label}"
EXTRA_FILES=("${@:5}")

rm -f "$OUT_ZIP"
# `zip` is not installed on this host; python3's zipfile is the stdlib fallback.
python3 - "$OUT_ZIP" "$OUT_EXE" "$DATA_DIR" "$LABEL" "${EXTRA_FILES[@]}" <<'PY'
import os, sys, zipfile
out_zip, out_exe, data_dir, label = sys.argv[1], sys.argv[2], sys.argv[3], sys.argv[4]
extras = sys.argv[5:]
with zipfile.ZipFile(out_zip, "w", zipfile.ZIP_DEFLATED) as z:
    z.write(out_exe, arcname=os.path.basename(out_exe))
    for root, _dirs, files in os.walk(data_dir):
        for f in sorted(files):
            p = os.path.join(root, f)
            z.write(p, arcname=os.path.relpath(p, os.path.dirname(data_dir)))
    for x in extras:
        z.write(x, arcname=os.path.basename(x))
# The gate must go red if either half of the payload is missing from the zip.
names = zipfile.ZipFile(out_zip).namelist()
if os.path.basename(out_exe) not in names:
    sys.exit(f"{label}: FAIL: {os.path.basename(out_exe)} missing from {out_zip}")
if not any(n.startswith("data_") and n.endswith("/LastAnimalPreflight.dll") for n in names):
    sys.exit(f"{label}: FAIL: data_*/LastAnimalPreflight.dll missing from {out_zip}")
for x in extras:
    if os.path.basename(x) not in names:
        sys.exit(f"{label}: FAIL: {os.path.basename(x)} missing from {out_zip}")
suffix = " + " + ", ".join(os.path.basename(x) for x in extras) if extras else ""
print(f"{label}: zip payload OK — {len(names)} entries (binary + data dir{suffix})")
PY
[ -s "$OUT_ZIP" ] || { echo "$LABEL: FAIL: $OUT_ZIP is empty" >&2; exit 1; }
