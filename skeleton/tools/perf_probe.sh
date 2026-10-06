#!/usr/bin/env bash
# perf_probe.sh — MC 10165 S11 per-zone perf capture driver.
#
# Captures one perf pass (measured window per zone) through the SAME
# graphical-test-helper.sh Xvfb capture mechanism every ci/*_test.sh gate
# uses — the wrapper scene is capture_scene.tscn (zone instance from
# ZONE_SCENE), sampled by its PERF_WINDOW mode; this script authors NO second
# capture system. One run = meadow, canyon, ruins, each printed as a
# PROBE_SUMMARY line plus the raw per-frame LA_PERF lines in <OUT_DIR>/<zone>.perf.
#
# The frame-time numbers are only meaningful under this REAL Xvfb GL
# surface — headless ms/frame is meaningless (S11 pinned plan) — so the
# probe REJECTS a zone whose window sampled 0.0 draw calls (headless
# monitors read zero), instead of silently recording a fake measurement.
#
# Usage (from the repo, toolchain sourced, GODOT exported):
#   OUT_DIR=/abs/dir RUN=aa1 bash tools/perf_probe.sh [project_dir]
#   PERF_WINDOW=60 per-zone frame window; CAPTURE_WAIT=60 helper capture
#   delay (the game stays alive past it via MIN_LIFE_MS — on this software-GL
#   box a zone frame costs seconds under load). GUI_HELPER override like the
#   ci gates. Exit 0 iff every zone captured RESULT=PASS and produced LA_PERF
#   samples with draw_calls > 0.
set -uo pipefail

HERE="$(cd "$(dirname "$0")" && pwd)"
PROJ="${1:-$(dirname "$HERE")}"
GUI_HELPER="${GUI_HELPER:-/usr/local/bin/graphical-test-helper.sh}"
PERF_WINDOW="${PERF_WINDOW:-60}"
CAPTURE_WAIT="${CAPTURE_WAIT:-60}"
RUN="${RUN:-run}"
fail() { echo "PERF_PROBE: FAIL: $*" >&2; exit 1; }

[ -n "${OUT_DIR:-}" ] || fail "OUT_DIR not set (dir for per-zone logs)"
mkdir -p "$OUT_DIR"
: "${GODOT:?set GODOT to the engine binary (see engine/PIN.txt)}"
[ -x "$GODOT" ] || fail "GODOT not executable: $GODOT"
[ -f "$GUI_HELPER" ] || fail "graphical-test-helper not found: $GUI_HELPER"
CAP_SCENE="res://capture_scene.tscn"
[ -f "$PROJ/scripts/capture_wrapper.gd" ] || fail "capture_wrapper.gd missing in $PROJ"

ZONE_FAILS=0
for ZONE in meadow canyon ruins; do
  LOG="$OUT_DIR/$ZONE.log"
  PERF="$OUT_DIR/$ZONE.perf"
  echo "PERF_PROBE: run=$RUN zone=$ZONE window=$PERF_WINDOW wait=$CAPTURE_WAIT"
  HELPER_LOG="$("$GUI_HELPER" --cmd "ZONE_SCENE=res://zones/$ZONE/$ZONE.tscn PERF_WINDOW=$PERF_WINDOW PERF_LOG=$PERF MIN_LIFE_MS=$(( (CAPTURE_WAIT + 15) * 1000 )) timeout 1500 $GODOT --path $PROJ $CAP_SCENE > $LOG 2>&1" \
                --wait "$CAPTURE_WAIT" --out "$OUT_DIR/$ZONE.png" 2>&1)"
  CODE=$?
  printf '%s\n' "$HELPER_LOG"
  [[ "$HELPER_LOG" == *'RESULT=PASS'* ]]
  HELPER_OK=$?
  if [ "$CODE" -ne 0 ] || [ "$HELPER_OK" -ne 0 ]; then
    echo "PROBE: run=$RUN zone=$ZONE exit=$CODE helper_pass=$([ "$HELPER_OK" -eq 0 ] && echo 1 || echo 0) samples=0 (capture failed)"
    ZONE_FAILS=$((ZONE_FAILS + 1))
    continue
  fi
  # Samples source: the wrapper's flush-on-write PERF log first (survives a
  # mid-window timeout kill); stdout log as fallback.
  SAMPLES="$LOG"; [ -s "$PERF" ] && SAMPLES="$PERF"
  NSAMP="$(grep -c '^LA_PERF: ' "$SAMPLES" 2>/dev/null || true)"
  [ "${NSAMP:-0}" -gt 0 ] || { echo "PROBE: run=$RUN zone=$ZONE exit=$CODE samples=0 (no LA_PERF lines)"; ZONE_FAILS=$((ZONE_FAILS + 1)); continue; }
  # Real-surface bar: headless monitors read 0 — reject a silent-zero window.
  MAXDC="$(awk '/^LA_PERF: / { sub(/.*draw_calls=/,""); print $1 }' "$SAMPLES" | sort -n | tail -1)"
  if [ "${MAXDC:-0}" -le 0 ] 2>/dev/null; then
    echo "PROBE: run=$RUN zone=$ZONE exit=$CODE samples=$NSAMP max_draw_calls=$MAXDC (headless zero-surface — measurement invalid)"
    ZONE_FAILS=$((ZONE_FAILS + 1))
    continue
  fi
  awk -v run="$RUN" -v zone="$ZONE" '
    /^LA_PERF: / {
      for (i = 1; i <= NF; i++) {
        if ($i ~ /^ms_wall=/)     { sub(/.*ms_wall=/, "", $i);     wt[++n] = $i + 0 }
        if ($i ~ /^ms_proc=/)     { sub(/.*ms_proc=/, "", $i);     pr[n]   = $i + 0 }
        if ($i ~ /^draw_calls=/)  { sub(/.*draw_calls=/, "", $i);  dc[n]   = $i + 0 }
      }
    }
    END {
      asort(wt); asort(dc)
      mw = 0; md = 0
      for (i = 1; i <= n; i++) { mw += wt[i]; md += dc[i] }
      medw = (n % 2) ? wt[(n + 1) / 2] : (wt[n / 2] + wt[n / 2 + 1]) / 2
      medd = (n % 2) ? dc[(n + 1) / 2] : (dc[n / 2] + dc[n / 2 + 1]) / 2
      printf "PROBE_SUMMARY: run=%s zone=%s n=%d mean_ms_wall=%.3f median_ms_wall=%.3f max_ms_wall=%.3f mean_draw_calls=%.1f median_draw_calls=%.1f max_draw_calls=%d\n", \
             run, zone, n, mw / n, medw, wt[n], md / n, medd, dc[n]
    }' "$SAMPLES"
done

[ "$ZONE_FAILS" -eq 0 ] || fail "$ZONE_FAILS zone(s) failed this pass (run=$RUN)"
echo "PERF_PROBE: PASS run=$RUN zones=meadow,canyon,ruins window=$PERF_WINDOW out=$OUT_DIR"
