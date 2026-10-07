#!/usr/bin/env bash
# mode_sets_check.sh — MC 10210 (W2 tail, F-C + orchestrator ruling 10203
# append #5): LA_GATE_MODE vocabulary drift gate over the dispatching proof
# classes. Thin wrapper so the battery (and any CI shell) calls ONE named leg;
# the check itself lives in mode_sets_check.py next to this file.
#
# Asserts per proof class: header-doc modes == allow-list (where one exists —
# RuntimeIntegrationProof.KnownModes) == dispatch arms; exits 1 with named
# VIOLATION lines on drift. Red-capability ships as --selftest: a temp copy
# of ci_proofs gets planted drift (one dispatch arm with no doc row; one
# KnownModes entry with no dispatch arm) and every plant MUST go RED — the
# tree is never touched.
#
# MC 10218 second leg (same script, no new mechanism): STAGE PAIRING over
# RuntimeIntegrationProof* — every Run*Stage body needs a call-site arm in
# the entry switch (_stage) and every arm needs its body (the 781ea66 merge
# dropped stage arms invisibly to the mode vocabulary); --selftest gained an
# unpaired-body and a dead-arm plant, both RED, tree untouched.
#
# Usage:
#   ./ci/mode_sets_check.sh [--project <skeleton_dir>] [--selftest]
set -uo pipefail

HERE="$(cd "$(dirname "$0")" && pwd)"
[ -f "$HERE/mode_sets_check.py" ] || { echo "MODE_SETS_CHECK: FAIL: mode_sets_check.py missing beside this wrapper" >&2; exit 1; }
command -v python3 >/dev/null 2>&1 || { echo "MODE_SETS_CHECK: FAIL: python3 not on PATH" >&2; exit 1; }

exec python3 "$HERE/mode_sets_check.py" "$@"
