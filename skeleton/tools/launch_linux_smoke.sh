#!/usr/bin/env bash
# launch_linux_smoke.sh — native packaged-launch smoke for Last Animal
# (MC 1344 follow-up, 2026-09-25).
#
# Launches the EXPORTED Linux binary (build/LastAnimal.x86_64) natively under
# Xvfb via the existing graphical-test-helper.sh capture mechanism — the same
# helper the ci/*_test.sh gates use; this script authors NO second capture
# mechanism. RESULT=PASS means the framebuffer capture is non-blank, i.e. the
# packaged game actually boots and renders. The capture PNG is written into
# this run's audit dir as evidence (override with SMOKE_OUT_DIR).
#
# MC 10103: the SMOKE_OUT_DIR default used to be a hardcoded stale run dir, so
# a bare run silently overwrote that run's captured evidence (gitignored bytes,
# lost once). A bare run now derives a FRESH run-unique dir under $PROJ/.audits/
# — the repo's mktemp idiom (the mktemp pattern at ci/smoke.sh:74, the -d form
# at ci/ui_test.sh:235), no host path in any default — and NO run, default or
# SMOKE_OUT_DIR, overwrites an existing capture unless SMOKE_FORCE=1 says so.
#
# Usage:  bash tools/launch_linux_smoke.sh                       # fresh run-unique evidence dir
#         SMOKE_OUT_DIR=<dir> bash tools/launch_linux_smoke.sh  # explicit capture dir (unchanged)
#         SMOKE_FORCE=1 bash tools/launch_linux_smoke.sh        # allow overwriting an existing capture
set -euo pipefail

fail() { echo "LAUNCH_LINUX_SMOKE: FAIL: $*" >&2; exit 1; }

HERE="$(cd "$(dirname "$0")" && pwd)"
PROJ="$(cd "$HERE/.." && pwd)"
OUT_EXE="$PROJ/build/LastAnimal.x86_64"
GUI_HELPER="${GUI_HELPER:-/usr/local/bin/graphical-test-helper.sh}"
if [ -n "${SMOKE_OUT_DIR:-}" ]; then
  mkdir -p "$SMOKE_OUT_DIR"
else
  # Fresh run-unique evidence dir (MC 10103) — never a fixed, shared target.
  mkdir -p "$PROJ/.audits"
  SMOKE_OUT_DIR="$(mktemp -d "$PROJ/.audits/$(date -u +%Y%m%d%H%M)-launch-linux-smoke.XXXXXX")" \
    || fail "mktemp -d failed deriving the run evidence dir under $PROJ/.audits/"
fi
PNG="$SMOKE_OUT_DIR/linux-smoke-capture.png"

# MC 10103 refuse-overwrite: captured evidence is never clobbered silently.
if [ -e "$PNG" ] && [ -z "${SMOKE_FORCE:-}" ]; then
  fail "capture already exists: $PNG — refusing to overwrite an existing capture (MC 10103); re-run with SMOKE_FORCE=1 to overwrite"
fi

[ -f "$OUT_EXE" ] || fail "no exported binary at $OUT_EXE (run tools/export_linux.sh first)"
[ -x "$OUT_EXE" ] || fail "$OUT_EXE is not executable"
[ -f "$GUI_HELPER" ] || fail "graphical-test-helper not found: $GUI_HELPER"

echo "LAUNCH_LINUX_SMOKE: launching $OUT_EXE natively under Xvfb (helper: $GUI_HELPER)"
# The helper starts its own throwaway Xvfb (1280x800x24) and captures THAT
# display's root window — nesting xvfb-run inside --cmd would put the game on
# a second display the helper cannot see, so the binary is passed directly.
LOG="$("$GUI_HELPER" --cmd "$OUT_EXE" --geometry 1280x800x24 --wait 15 --out "$PNG" 2>&1)" || CODE=$?
CODE="${CODE:-0}"
printf '%s\n' "$LOG"
[ "$CODE" -eq 0 ] || fail "graphical-test-helper exited $CODE (render bar not met)"
grep -q "RESULT=PASS" <<<"$LOG" \
  || fail "expected 'RESULT=PASS' from graphical-test-helper (framebuffer blank/uniform)"
[ -s "$PNG" ] || fail "capture PNG missing or empty: $PNG"

echo "LAUNCH_LINUX_SMOKE: RESULT=PASS — packaged binary booted and rendered non-blank"
echo "LAUNCH_LINUX_SMOKE: capture: $PNG ($(stat -c%s "$PNG") bytes)"
