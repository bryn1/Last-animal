#!/usr/bin/env bash
# sky_real_mount_probe.sh — MC 11113 / 10026.44.12 S50-SKY: STANDALONE run of
# the SKY_STRIP REAL-MOUNT render probe (green pair + planted-bad red pair),
# the idiom of ci/boot_test.sh (GODOT env contract, exit-code AND marker gates,
# the gate proves it can fail).
#
# WHY STANDALONE (parallel-lane law): the battery's section (S) already runs
# the SAME two modes (LA_GATE_MODE=SKY_STRIP / SKY_STRIP_PLANT, the raw
# xvfb-run pair from MC 11088 FIX-ROUND-1) and the battery file
# ci/runtime_integration_test.sh is UNDER EDIT by MC 11152 while this card
# ships — no new battery leg is ADDED here. The leg upgrade happened IN PLACE
# inside ci_proofs/RuntimeIntegrationProof.SkyStrip.cs (option (b) real mount:
# zones/canyon/canyon.tscn "SkyStrip" mounted by the ONE WorldMount route),
# so the battery's existing greps keep proving the REAL mount with zero .sh
# edit. This script exists (a) as the card's evidence runner and (b) as the
# drop-in one-liner a battery card can call if it ever wants the pair as its
# OWN named leg. The battery comment block at section (S) still DESCRIBES the
# old beside-scene proxy mount — that prose restamp is the named handback
# (see the EVIDENCE row; prose only, the greps are mechanism-agnostic).
#
# What the legs prove (markers emitted by stage 109, mode doc in
# ci_proofs/RuntimeIntegrationProof.cs): the SHIPPED strip canyon_sky_v1.png
# rides the PRODUCT mount — painted at RUN-derived screen points of the
# mirrored shipped view, mapped as authored through the mounted node's own
# transform; the planted alpha-0 copy of the product material goes RED by
# name (a transparent strip cannot pass), red rides beside green.
#
# Usage:
#   GODOT=/path/to/godot ./ci/sky_real_mount_probe.sh [project_dir]
#   (defaults to the dir above ci/; LA_SKYCAP_PATH defaults to <proj>/.tmp/s50-sky)
set -uo pipefail

HERE="$(cd "$(dirname "$0")" && pwd)"
PROJ="${1:-$(dirname "$HERE")}"
: "${GODOT:?set GODOT to the engine binary (see the repo engine/PIN.txt)}"
CAP="${LA_SKYCAP_PATH:-$PROJ/.tmp/s50-sky}"
PROOF="res://ci_proofs/RuntimeIntegrationProof.cs"

fail() { echo "SKY_REAL_MOUNT: GATE FAIL: $*" >&2; exit 1; }
[ -x "$GODOT" ] || fail "GODOT not executable: $GODOT"
[ -f "$PROJ/project.godot" ] || fail "not a godot project: $PROJ"
command -v xvfb-run >/dev/null || fail "xvfb-run missing — the windowed pair cannot run"

echo "SKY_REAL_MOUNT: godot=$("$GODOT" --version 2>/dev/null | tail -1)  project=$PROJ  caps=$CAP"

# (1) current C# assembly through the pinned engine (the shipped PREFLIGHT law)
timeout 300 "$GODOT" --headless --path "$PROJ" --build-solutions --quit-after 1 \
  || fail "engine --build-solutions exited nonzero (C# build failed)"

# (2) GREEN leg — windowed real-mount probe (the battery's spawn idiom verbatim)
LOGSKY="$(LA_SKYCAP_PATH="$CAP" LA_GATE_MODE=SKY_STRIP xvfb-run -a timeout 300 "$GODOT" --path "$PROJ" --script "$PROOF" 2>&1)" \
  || { printf '%s\n' "$LOGSKY" | tail -30 >&2; fail "SKY_STRIP: the windowed real-mount probe exited nonzero (named FAIL lines above)"; }
[[ "$LOGSKY" == *'FXSKY_RENDERER method=gl_compatibility'* ]] \
  || fail "SKY_STRIP: renderer pin line missing — verdicts at an unpinned renderer are vacuous"
[[ "$LOGSKY" == *'FXSKY_REALMOUNT'* ]] \
  || fail "SKY_STRIP: FXSKY_REALMOUNT missing — the leg did NOT resolve the PRODUCT mount (zones/canyon/canyon.tscn SkyStrip through the WorldMount route); a probe of a proxy would defeat MC 11113"
[[ "$LOGSKY" == *'FXSKY_CLOCK_PARKED'* ]] \
  || fail "SKY_STRIP: FXSKY_CLOCK_PARKED missing — the NIGHT capture anchor was not read back (the kept DayNight law would be unproven)"
[[ "$LOGSKY" == *'SKY_STRIP_RENDER_OK'* ]] \
  || fail "SKY_STRIP: SKY_STRIP_RENDER_OK missing — the shipped strip is NOT proven painted-as-authored THROUGH THE PRODUCT MOUNT"
[[ "$LOGSKY" == *'SKY_STRIP measured pt'* && "$LOGSKY" == *'SKY_STRIP measured pts='* ]] \
  || fail "SKY_STRIP: the per-point measured lines or the summary are missing — verdicts must print their margins"
grep -qE '^LA_GATE: (FXSKY_UNSETTLED|ZONE4_FAIL|FALL_RECOVERED)' <<<"$LOGSKY" \
  && fail "SKY_STRIP: the log carries a unsettled/FAIL/FALL_RECOVERED line (an unstable or falling leg has no pixel verdicts)"
[ "$(grep -c 'FXSKY_CAPTURE' <<<"$LOGSKY")" -ge 2 ] \
  || fail "SKY_STRIP: fewer than 2 FXSKY_CAPTURE lines"
for f in skystrip_skybase.png skystrip_skyon.png; do
  [ -s "$CAP/$f" ] || fail "SKY_STRIP: capture missing/empty: $CAP/$f (screenshot/frame-capture duty — the plate must EXIST on disk)"
done
echo "SKY_REAL_MOUNT: green pair OK (captures: $(ls -la "$CAP" | awk 'NR>1{print $5,$9}' | tr '\n' ' '))"

# (3) RED pair — planted alpha-0 PRODUCT material (gate-red law; the plant is
# a RUNTIME override swap inside the mounted scene; the scene file is never written)
PLRC=0
LOGPL="$(LA_SKYCAP_PATH="$CAP-red" LA_GATE_MODE=SKY_STRIP_PLANT xvfb-run -a timeout 300 "$GODOT" --path "$PROJ" --script "$PROOF" 2>&1)" || PLRC=$?
[[ $PLRC -ne 0 ]] || { printf '%s\n' "$LOGPL" | tail -20 >&2; fail "SKY_STRIP_PLANT: the planted-bad leg EXITED GREEN — the checks are VACUOUS (a transparent product strip cannot pass the paint gate)"; }
[[ "$LOGPL" == *'SKY_STRIP_RENDER_FAIL'* && "$LOGPL" == *'FXSKY_PLANT_RED'* ]] \
  || fail "SKY_STRIP_PLANT: exited nonzero WITHOUT the named SKY_STRIP_RENDER_FAIL / FXSKY_PLANT_RED lines (a red leg must go red BY NAME)"
if grep -qE '^LA_GATE: SKY_STRIP_RENDER_OK' <<<"$LOGPL"; then
  fail "SKY_STRIP_PLANT: a planted leg printed the verdict OK line — verdict collision"
fi
echo "SKY_REAL_MOUNT: GATE PASS — real-mount green pair painted-as-authored THROUGH the WorldMount product mount + planted alpha-0 strip caught RED by name, beside the green (caps: $CAP + $CAP-red)"
exit 0
