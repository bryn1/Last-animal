#!/usr/bin/env bash
# verify-11891.sh — re-runnable proof for MC 11891 (dialogue-band reflow,
# owner ruling B: "B" = reflow the band to fit any window, NO minimum window
# size). Structural legs always run; the FULL gate leg (LA_VERIFY_FULL=1)
# re-runs ci/ui_test.sh A-L end to end. Every rc is captured FROM THE
# PROCESS (no pipe tails). Prints VERIFY_EXIT=<n> as the LAST line.
set -uo pipefail

HERE="$(cd "$(dirname "$0")" && pwd)"          # <repo>/.audits/20261103-0700-bandreflow/out
PROJ_ROOT="$(cd "$HERE/../../.." && pwd)"      # the repo root
Skel="$PROJ_ROOT/skeleton"
fail() { echo "VERIFY-11891: FAIL: $*" >&2; exit 1; }

# 1. engine pin: the mono binary named by engine/PIN.txt reports the pinned commit
GODOT="${GODOT:-$PROJ_ROOT/engine/Godot_v4.7.2-stable_mono_linux_x86_64/Godot_v4.7.2-stable_mono_linux.x86_64}"
[ -x "$GODOT" ] || fail "engine binary not executable: $GODOT (fetch per engine/PIN.txt + local.sha512)"
ver="$("$GODOT" --version 2>/dev/null | tail -1)"
[[ "$ver" == *"4.7.2.stable.mono.official.ed1daf0bf"* ]] \
  || fail "engine version '$ver' != pinned 4.7.2.stable.mono.official.ed1daf0bf"
echo "VERIFY-11891: engine ok ($ver)"

# 2. syntax + hygiene: the split gate trio and the carved files exist under ceiling
for f in ci/ui_test.sh ci/ui_pixel_battery.sh ci/ui_tracker_battery.sh; do
  [ -f "$Skel/$f" ] || fail "missing $f"
  bash -n "$Skel/$f" || fail "bash -n $f"
  n="$(wc -l < "$Skel/$f")"; [ "$n" -le 400 ] || fail "$f grew to $n l (>400 ceiling — the MC 11891 split exists to keep this true)"
  grep -q "by wc AT this head" "$Skel/$f" || fail "$f: SIZE stamp line missing"
  stamp="$(sed -n 's/.*SIZE[^:]*: \([0-9]\+\) l.*/\1/p' "$Skel/$f" | head -1)"
  [ "$stamp" = "$n" ] || fail "$f: SIZE stamp says $stamp l, wc says $n l (stale stamp)"
done
echo "VERIFY-11891: gate trio ok (syntax, ceiling, stamps)"

# 3. the fix is in place and the stood-down fallback cannot have crept back
grep -q "LayoutBand" "$Skel/src/ui/DialogueSystem.cs" \
  || fail "DialogueSystem.LayoutBand missing — the reflow is not in the tree"
grep -q "SizeChanged += LayoutBand" "$Skel/src/ui/DialogueSystem.cs" \
  || fail "the band no longer re-layouts on Viewport.SizeChanged"
grep -vE '^[[:space:]]*(//|\*)' "$Skel/src/ui/DialogueSystem.cs" | grep -q "PanelViewport" \
  && fail "the constant-span PanelViewport fallback is back in DialogueSystem CODE (stood down by MC 11891 B; comments naming the stand-down are fine)"
grep -q "DIALOGUE_IN_VIEW" "$Skel/tests/ui/HudDialogueOverlapTest.cs" \
  || fail "the DIALOGUE_IN_VIEW assert is not in the test"
grep -q "DIALOGUE_IN_VIEW" "$Skel/ci/ui_test.sh" \
  || fail "DIALOGUE_IN_VIEW is not asserted by ci/ui_test.sh (DoD row 2)"
echo "VERIFY-11891: reflow + assert present; fallback absent from the band"

# 4. no parallel mechanism / battery untouched: leg J rides the one helper
#    path; the runtime battery moved ZERO lines base->head
grep -q "graphical-test-helper" "$Skel/ci/ui_test.sh" || fail "leg J lost the shared helper"
if git -C "$PROJ_ROOT" rev-parse 0e2e98a >/dev/null 2>&1; then
  d="$(git -C "$PROJ_ROOT" diff 0e2e98a HEAD -- skeleton/ci/runtime_integration_test.sh | wc -l)"
  [ "$d" = "0" ] || fail "runtime_integration_test.sh moved $d lines (must stay 0 — DoD row 6)"
  echo "VERIFY-11891: battery diff 0 lines vs 0e2e98a"
fi

# 5. owner's real save untouched (isolated HOME law)
SAVE="/home/claudecode/.local/share/godot/app_userdata/Last Animal/savegame.json"
if [ -f "$SAVE" ]; then
  sha="$(sha256sum "$SAVE" | cut -d' ' -f1)"
  [ "$sha" = "fdd1cc43a5124fee3d12b007f5874f0d9a2e969ab93d3c602e1ca96f9697cd4e" ] \
    || fail "owner savegame.json sha256 $sha != banked fdd1cc43... (isolation law broken)"
  echo "VERIFY-11891: owner save byte-identical"
fi

# 6. banked red/green pair exists (a guard never seen failing is not a guard)
#    Checks run against the COMMITTED named-marker SUMMARY logs — the raw
#    transcripts stay host-local (MC 11891 durable-set packaging ruling), so
#    a clean checkout must not be asked to find them.
for p in frames/red/la-overlap-narrow-800.png frames/green/la-overlap-narrow-800.png \
         frames/green/la-overlap-default.png logs/gate-RED-prefix-800-summary.log \
         logs/gate-GREEN-final-summary.log; do
  [ -f "$HERE/$p" ] || fail "banked evidence missing: $p"
done
grep -q "FAIL — DIALOGUE_IN_VIEW" "$HERE/logs/gate-RED-prefix-800-summary.log" \
  || fail "the banked RED summary does not name DIALOGUE_IN_VIEW"
grep -q "GATE FAIL: run J3" "$HERE/logs/gate-RED-prefix-800-summary.log" \
  || fail "the banked RED gate did not fail at run J3"
grep -q "GATE PASS" "$HERE/logs/gate-GREEN-final-summary.log" \
  || fail "the banked GREEN summary lacks the GATE PASS tail"
echo "VERIFY-11891: red/green evidence pair banked and named"

# 7. optional FULL gate leg: ci/ui_test.sh A-L against THIS tree, rc from the
#    process; frames land beside the log under .tmp so re-runs never clobber
#    the banked pair.
if [ "${LA_VERIFY_FULL:-0}" = "1" ]; then
  : "${GODOT:?}"
  tmpd="$HERE/.tmp/verify-full"; rm -rf "$tmpd"; mkdir -p "$tmpd/frames"
  HOME="$PROJ_ROOT/.tmp/verify-home" XDG_DATA_HOME="$PROJ_ROOT/.tmp/verify-home/data" \
  XDG_CACHE_HOME="$PROJ_ROOT/.tmp/verify-home/cache" \
    bash "$Skel/ci/ui_test.sh" "$Skel" > "$tmpd/gate.log" 2>&1
  rc=$?
  echo "VERIFY-11891: full gate rc=$rc (log $tmpd/gate.log)"
  [ "$rc" = "0" ] || fail "full ui_test.sh A-L exited $rc"
fi

echo "VERIFY_EXIT=0"
