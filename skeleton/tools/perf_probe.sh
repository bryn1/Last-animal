#!/usr/bin/env bash
# perf_probe.sh — MC 10165 S11 per-zone perf capture driver; MC 10520 S44
# per-stage assert extension.
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
# S44 assert law (proposal §3, verbatim): the +19.4% ruins row is NAMED, NOT
# RULED — it sits inside S11's pooled render-identical control bound (FINDING
# F3, 11.317 ms); a PAIRED A/A re-run is owed before any stage can RED; an
# unpaired delta >15% is a FLAG (paired re-run owed), not an auto-RED.
# The assert section encodes exactly that: GREEN inside the bound, FLAG for
# unpaired past the bound, RED only past the bound under PAIRED=1.
#
# Usage (from the repo, toolchain sourced, GODOT exported):
#   OUT_DIR=/abs/dir RUN=aa1 bash tools/perf_probe.sh [project_dir]
#   PERF_WINDOW=60 per-zone frame window; CAPTURE_WAIT=60 helper capture
#   delay (the game stays alive past it via MIN_LIFE_MS — on this software-GL
#   box a zone frame costs seconds under load). GUI_HELPER override like the
#   ci gates. Exit 0 iff every zone captured RESULT=PASS and produced LA_PERF
#   samples with draw_calls > 0.
#   S44 add-ons (all optional, measurement-only):
#   PERF_STAGE=Sxx   stage whose delta rows this pass records; asserts the
#                    stage table carries one row per zone after the append.
#   PAIRED=1         this pass is the paired arm — a bound-miss is RED, not FLAG.
#   PERF_PLANT_N=k   wrapper instantiates the zone scene k extra times
#                    (heavy plant for the planted-RED proof; 0/unset = shipped tree).
set -uo pipefail

HERE="$(cd "$(dirname "$0")" && pwd)"
PROJ="${1:-$(dirname "$HERE")}"
# MC 11243 F4 pointer: PROJ is NOT absolutized here (latent style — this script passes no LA_*CAP*_PATH env to the engine, MC 11152 sibling audit; its PERF_LOG rides OUT_DIR, not PROJ); if one is ever added, adopt the MC 11152 cd-and-pwd idiom — see the PROJ= block at the top of skeleton/ci/runtime_integration_test.sh.
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
TABLE="$HERE/perf_stages.tsv"
[ -f "$TABLE" ] || fail "S44 stage table missing: $TABLE"
: "${PERF_STAGE:?S44: PERF_STAGE=<Sxx> not set — asserts are per-stage; S11/S21 anchors live as table rows}"

# S44 constants (measured, cited): AA_BOUND = S11 pooled render-identical
# control bound (FINDING F3 — wall-ms drift between passes on render-identical
# trees on this shared llvmpipe box); BASE_* = S11 aa-mean anchor per zone.
AA_BOUND=11.317
BASE_meadow=30.658
BASE_canyon=32.842
BASE_ruins=32.875

ZONE_FAILS=0
SUM="$OUT_DIR/summaries.$RUN"
: > "$SUM"
for ZONE in meadow canyon ruins; do
  LOG="$OUT_DIR/$ZONE.log"
  PERF="$OUT_DIR/$ZONE.perf"
  echo "PERF_PROBE: run=$RUN zone=$ZONE window=$PERF_WINDOW wait=$CAPTURE_WAIT"
  HELPER_LOG="$("$GUI_HELPER" --cmd "ZONE_SCENE=res://zones/$ZONE/$ZONE.tscn PERF_WINDOW=$PERF_WINDOW PERF_LOG=$PERF MIN_LIFE_MS=$(( (CAPTURE_WAIT + 15) * 1000 )) PERF_PLANT_N=${PERF_PLANT_N:-0} timeout 1500 $GODOT --path $PROJ $CAP_SCENE > $LOG 2>&1" \
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
    }' "$SAMPLES" | tee -a "$SUM"
done

[ "$ZONE_FAILS" -eq 0 ] || fail "$ZONE_FAILS zone(s) failed this pass (run=$RUN)"

# ---------------------------------------------------------------------------
# S44 per-zone per-stage asserts (MC 10520). For this pass's PROBE_SUMMARY
# means: delta vs the S11 aa-mean anchor; GREEN inside the pooled A/A bound;
# unpaired past the bound = FLAG (paired A/A re-run owed); PAIRED=1 past the
# bound = RED. Then append this pass's rows to the stage table and require
# one row per zone for PERF_STAGE after the append (fail-closed naming gaps).
# ---------------------------------------------------------------------------
ASSERT_RED=0
while read -r _; do :; done < /dev/null
for LINE in $(grep '^PROBE_SUMMARY' "$SUM"); do :; done
while IFS= read -r LINE; do
  ZONE="${LINE#*zone=}"; ZONE="${ZONE%% *}"
  MEAN="${LINE#*mean_ms_wall=}"; MEAN="${MEAN%% *}"
  case "$ZONE" in
    meadow) BASE="$BASE_meadow" ;;
    canyon) BASE="$BASE_canyon" ;;
    ruins)  BASE="$BASE_ruins" ;;
    *) fail "unknown zone in summary: $ZONE" ;;
  esac
  OUT_A="$(awk -v z="$ZONE" -v mean="$MEAN" -v base="$BASE" -v bound="$AA_BOUND" -v paired="${PAIRED:-0}" 'BEGIN {
    d = mean - base; pct = 100 * d / base
    if (d <= bound)       printf "ASSERT: GREEN zone=%s mean=%.3f anchor=%.3f delta_ms=%.3f delta_pct=%.1f (inside pooled A/A bound %.3f ms — F3)\n", z, mean, base, d, pct, bound
    else if (paired == 1) printf "ASSERT: RED zone=%s mean=%.3f anchor=%.3f delta_ms=%.3f delta_pct=%.1f (paired arm past bound %.3f)\n", z, mean, base, d, pct, bound
    else                  printf "ASSERT: FLAG zone=%s mean=%.3f anchor=%.3f delta_ms=%.3f delta_pct=%.1f (unpaired past bound %.3f — paired A/A re-run owed, not auto-RED)\n", z, mean, base, d, pct, bound
  }')"
  printf '%s\n' "$OUT_A"
  printf '%s\t' "$OUT_A" >> "$SUM" 2>/dev/null || true
  case "$OUT_A" in "ASSERT: RED"*) ASSERT_RED=$((ASSERT_RED + 1)) ;; esac
  awk -v s="$PERF_STAGE" -v z="$ZONE" -v mean="$MEAN" -v base="$BASE" -v run="$RUN" -v arm="${PAIRED:-0}" 'BEGIN { d = mean - base; printf "%s\t%s\t%.3f\t%.3f\t%.3f\t%.1f\t%s\t%s\n", s, z, mean, base, d, 100 * d / base, (arm == 1 ? "paired" : "single"), run }' >> "$TABLE"
done < "$SUM"

# fail-closed stage-table check: after the append, one row per zone exists
for ZONE in meadow canyon ruins; do
  N="$(awk -v s="$PERF_STAGE" -v z="$ZONE" '$1==s && $2==z {n++} END {print n+0}' "$TABLE")"
  [ "$N" -gt 0 ] || { echo "ASSERT: RED zone=$ZONE stage=$PERF_STAGE has no stage-table row after append"; ASSERT_RED=$((ASSERT_RED + 1)); }
done

[ "$ASSERT_RED" -eq 0 ] || fail "$ASSERT_RED zone(s) RED on S44 stage asserts (run=$RUN)"

echo "PERF_PROBE: PASS run=$RUN stage=$PERF_STAGE zones=meadow,canyon,ruins window=$PERF_WINDOW out=$OUT_DIR"
