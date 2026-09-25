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
# Usage:  bash tools/launch_linux_smoke.sh
set -euo pipefail

HERE="$(cd "$(dirname "$0")" && pwd)"
PROJ="$(cd "$HERE/.." && pwd)"
OUT_EXE="$PROJ/build/LastAnimal.x86_64"
GUI_HELPER="${GUI_HELPER:-/usr/local/bin/graphical-test-helper.sh}"
SMOKE_OUT_DIR="${SMOKE_OUT_DIR:-/home/svarkor/last-animal/.audits/202609251217-45babbbb}"
PNG="$SMOKE_OUT_DIR/linux-smoke-capture.png"

fail() { echo "LAUNCH_LINUX_SMOKE: FAIL: $*" >&2; exit 1; }

[ -f "$OUT_EXE" ] || fail "no exported binary at $OUT_EXE (run tools/export_linux.sh first)"
[ -x "$OUT_EXE" ] || fail "$OUT_EXE is not executable"
[ -f "$GUI_HELPER" ] || fail "graphical-test-helper not found: $GUI_HELPER"
mkdir -p "$SMOKE_OUT_DIR"

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
