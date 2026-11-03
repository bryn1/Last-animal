#!/usr/bin/env bash
# ui_tracker_battery.sh — legs I, K, L of the M10 ui-hud DoD gate. SIZE (file hygiene law): 191 l by wc AT this head — carved out of ci/ui_test.sh by MC 11891 (MC 11303 F-P3-2 named the split; the >400 re-justification chain retired with it). ONE concern here: the quest-TRACKER legs — the HUD quest line following the live act/beat log through the REAL save path. These are the save-touching legs: run whole-gate passes under the SAVEGATE mkdir mutex (fleet convention, kb c8e1f2bb). Runs after the entry build; runnable standalone against a built tree with the same args (GODOT=... [GUI_HELPER=...]). Echo labels keep the gate name UI_HUD_TEST (one gate, three files — ci/ui_test.sh owns the entry + final GATE PASS).
set -uo pipefail

HERE="$(cd "$(dirname "$0")" && pwd)"
PROJ="${1:-$(dirname "$HERE")}"
TESTS_UI="$PROJ/tests/ui"
GUI_HELPER="${GUI_HELPER:-/usr/local/bin/graphical-test-helper.sh}"
UI_QUESTLINE="res://tests/ui/HudQuestAct2CaptureTest.cs"
UI_QUESTMONO="res://tests/ui/HudQuestMonoCaptureTest.cs"
UI_QUESTLINE_ACT3="res://tests/ui/HudQuestAct3CaptureTest.cs"

fail() { echo "UI_HUD_TEST: GATE FAIL: $*" >&2; exit 1; }
[ -d "$TESTS_UI" ] || fail "ui tests dir not found: $TESTS_UI"
[ -f "$TESTS_UI/HudQuestAct2CaptureTest.cs" ] || fail "HudQuestAct2CaptureTest.cs not found"
[ -f "$TESTS_UI/HudQuestMonoCaptureTest.cs" ] || fail "HudQuestMonoCaptureTest.cs not found"
[ -f "$TESTS_UI/HudQuestAct3CaptureTest.cs" ] || fail "HudQuestAct3CaptureTest.cs not found"
: "${GODOT:?set GODOT to the engine binary (see ../engine/PIN.txt)}"
[ -x "$GODOT" ] || fail "GODOT not executable: $GODOT"
[ -f "$GUI_HELPER" ] || fail "graphical-test-helper not found: $GUI_HELPER (install per infra/vm105)"
command -v convert >/dev/null 2>&1 \
  || fail "tracker battery: ImageMagick 'convert' missing (the helper's own bar, needed for the quest-row asserts)"
CAPDIR="$(mktemp -d "${TMPDIR:-/tmp}/la-tracker-capture.XXXXXX")" || fail "tracker battery: mktemp failed"
trap 'rm -rf "$CAPDIR"' EXIT

fnum() { awk -v a="$1" -v b="$2" 'BEGIN { exit !(a >= b) }'; }

# Leg I quest-row crop: the Quest gauge is the 6th HUD row (y = 8 + 5*28 =
# 148, the BuildUi loop pitch), label at window x 8. Measured here (MC 10138,
# deterministic across runs): the 1152x648 play window maps at +64+36 inside
# the helper's 1280x720 Xvfb root (identical trim-box on every capture — the
# shipped F/H crops tolerate the same offset with coarser bars), so the root
# crop = window rect + (64,36). 500px wide covers the longest tracker line;
# the dialogue band reflows with the window (MC 11891) and at the shipped
# default sits y 540..640 — outside this crop.
QUEST_CROP="500x26+72+182"

# ---------------------------------------------------------------------------
# (I) MC 10138 — HUD_QUESTLINE_ACT2: the tracker line follows the act-two
#     log once the act opens. Baseline run (LA_QUESTLINE_ACT2=0) shows the
#     act-one active title; act run (=1) opens the act through the REAL save
#     path (the quest_arc2 restore-sync idiom) and must show the act-two
#     title mid-arc. The quest-row crop proves the row is painted and that
#     its pixels actually moved between the runs. PLANTED-BAD: revert the
#     LiveQuestLine union in world/WorldDirector.Ui.cs -> the act run shows
#     "Quest: none" and its LA_QUESTLINE marker goes RED.
#     NOTE run 1 writes user://savegame.json — gate-level SAVEGATE flock.
# ---------------------------------------------------------------------------
echo "UI_HUD_TEST: run I — HUD_QUESTLINE_ACT2 baseline + act-two mid-arc title"
LOGI_BASE="$("$GUI_HELPER" --cmd "LA_QUESTLINE_ACT2=0 $GODOT --path $PROJ --script $UI_QUESTLINE > $CAPDIR/app-ql-base.log 2>&1" \
            --wait 6 --out "$CAPDIR/la-ql-base.png" 2>&1)"
printf '%s\n' "$LOGI_BASE"
[[ "$LOGI_BASE" == *'RESULT=PASS'* ]] \
  || fail "run I: baseline capture not RESULT=PASS (game frame did not render)"
[[ "$(cat "$CAPDIR/app-ql-base.log" 2>/dev/null)" == *'LA_QUESTLINE: PASS'* ]] \
  || fail "run I: baseline self-check marker missing (act-one tracker title not painted at boot — see app-ql-base.log)"

LOGI_ACT2="$("$GUI_HELPER" --cmd "LA_QUESTLINE_ACT2=1 $GODOT --path $PROJ --script $UI_QUESTLINE > $CAPDIR/app-ql-act2.log 2>&1" \
            --wait 6 --out "$CAPDIR/la-ql-act2.png" 2>&1)"
printf '%s\n' "$LOGI_ACT2"
[[ "$LOGI_ACT2" == *'RESULT=PASS'* ]] \
  || fail "run I: act-two capture not RESULT=PASS (game frame did not render)"
[[ "$(cat "$CAPDIR/app-ql-act2.log" 2>/dev/null)" == *'LA_QUESTLINE: PASS'* ]] \
  || fail "run I: act-two self-check marker missing (the tracker did NOT follow the act-two log mid-arc — the S10 limitation is back)"

# The quest-row crop must carry glyphs in the act-two run AND differ from the
# baseline run's row (a different title painted). Bars mirror F1/F2.
QL_SD="$(convert "$CAPDIR/la-ql-act2.png" -crop "$QUEST_CROP" +repage -colorspace Gray \
        -format '%[fx:standard_deviation]' info:)"
QL_COLORS="$(convert "$CAPDIR/la-ql-act2.png" -crop "$QUEST_CROP" +repage -format '%k' info:)"
echo "UI_HUD_TEST: I act2 quest-row stddev=$QL_SD colors=$QL_COLORS (bar: stddev>=0.02, colors>=4)"
fnum "$QL_SD" 0.02 || fail "run I: act-two quest row blank-ish (stddev=$QL_SD) — the tracker line did not paint"
fnum "$QL_COLORS" 4 || fail "run I: act-two quest row has <4 colours (colors=$QL_COLORS) — no glyph shapes present"
QL_DIFF_SD="$(convert "$CAPDIR/la-ql-base.png" "$CAPDIR/la-ql-act2.png" -compose difference -composite \
             -crop "$QUEST_CROP" +repage -colorspace Gray -format '%[fx:standard_deviation]' info:)"
QL_DIFF_MEAN="$(convert "$CAPDIR/la-ql-base.png" "$CAPDIR/la-ql-act2.png" -compose difference -composite \
               -crop "$QUEST_CROP" +repage -colorspace Gray -format '%[fx:mean]' info:)"
echo "UI_HUD_TEST: I quest-row diff stddev=$QL_DIFF_SD mean=$QL_DIFF_MEAN (bar: stddev>=0.01, mean>=0.003)"
fnum "$QL_DIFF_SD" 0.01 || fail "run I: quest row identical baseline/act-two (stddev=$QL_DIFF_SD) — the tracker did not repaint on act open"
fnum "$QL_DIFF_MEAN" 0.003 || fail "run I: quest row diff too small (mean=$QL_DIFF_MEAN) — act-two title not actually painted"
echo "UI_HUD_TEST: I ok — tracker paints act-one title at boot, act-two title mid-arc (pixels moved)"

# ---------------------------------------------------------------------------
# (K) MC 11295 — HUD_QUEST_MONO: the BOSS-KILL-ORDERING quest-line frame bank
#     (playthrough D3: "The Watcher Falls" -> back to "The Way Down — Reach
#     ruins" AT THE RUINS, and the SAVE held it). THREE runs of tests/ui/
#     HudQuestMonoCaptureTest.cs (LA_MONO_STAGE=0/1/2) on the REAL main.tscn
#     (spawning off at boot — the shipped quiet gate):
#       0 BEFORE: act-one driven to q_boss Active — tracker paints "The Watcher
#         Falls" (the playthrough frame-22 beat).
#       1 AFTER the boss-kill beat AT THE RUINS (two REAL travels + the shipped
#         Completed arm of OnQuestChanged): tracker must paint the ADVANCED
#         act-two row "The Older Tongue" — the UNFIXED product paints the
#         REGRESSED "The Way Down — Reach ruins" and THIS run's LA_MONO marker
#         goes RED (the leg's RED-before-fix is the product's own before-face;
#         the banked red frame SHOWS the bug painted).
#       2 the same drive + SaveGame/LoadGame AT THE RUINS: the tracker must
#         STILL read advanced AND the disk rows must carry q_r_descent:completed
#         — the saved state shown (D3's evidence class).
#     The in-code LA_MONO markers are the semantic pins (HUD == director,
#     no-cache law from leg I); the pixel bars corroborate (row painted +
#     BEFORE-vs-AFTER pixels moved — on the live scene the background also
#     moves, so the DIFF bar is corroboration, never the verdict).
#     Runs 1-2 write user://savegame.json: gate-level SAVEGATE flock (leg I law).
# ---------------------------------------------------------------------------
echo "UI_HUD_TEST: run K — HUD_QUEST_MONO boss-kill ordering frame bank (BEFORE / AFTER / SAVE+LOAD)"
for MONO_ST in 0 1 2; do
  MONO_W=8; [ "$MONO_ST" -ge 1 ] && MONO_W=45   # two REAL travels bake two zones SYNCHRONOUSLY
  # (heightfield + navmesh, ~5 s each, inside the director's travel handler) — the
  # AFTER stages need ~37 s wall before the verdict prints; the helper kill is the
  # script's life-support (leg I law).
  LOGK="$("$GUI_HELPER" --cmd "LA_MONO_STAGE=$MONO_ST $GODOT --path $PROJ --script $UI_QUESTMONO > $CAPDIR/app-mono-$MONO_ST.log 2>&1" \
          --wait $MONO_W --out "$CAPDIR/la-mono-$MONO_ST.png" 2>&1)"
  printf '%s\n' "$LOGK"
  [[ "$LOGK" == *'RESULT=PASS'* ]] \
    || fail "run K stage $MONO_ST: capture not RESULT=PASS (game frame did not render)"
  [[ "$(cat "$CAPDIR/app-mono-$MONO_ST.log" 2>/dev/null)" == *'LA_MONO: PASS'* ]] \
    || fail "run K stage $MONO_ST: LA_MONO self-check marker missing/RED (boss-kill ordering NOT advanced at stage $MONO_ST — see app-mono-$MONO_ST.log)"
done
# the saved state shown: stage 2 printed the DISK file's quest rows beside its frame
grep -o 'LA_MONO: SAVEDROWS .*' "$CAPDIR/app-mono-2.log" \
  | sed 's/^LA_MONO: SAVEDROWS //' > "$CAPDIR/la-mono-saved-rows.txt" || true
grep -q 'q_r_descent:completed' "$CAPDIR/la-mono-saved-rows.txt" \
  || fail "run K: the SAVED state does not carry the advanced q_r_descent:completed row (D3 is back — the save holds a regressed beat; see la-mono-saved-rows.txt)"
grep -q 'q_r_tongue:active' "$CAPDIR/la-mono-saved-rows.txt" \
  || fail "run K: the SAVED state does not carry q_r_tongue:active (the beat did not ADVANCE past the descent row)"
# pixel bars: the quest row painted in the AFTER run + actually moved BEFORE->AFTER
MJ_SD="$(convert "$CAPDIR/la-mono-1.png" -crop "$QUEST_CROP" +repage -colorspace Gray \
        -format '%[fx:standard_deviation]' info:)"
MJ_COLORS="$(convert "$CAPDIR/la-mono-1.png" -crop "$QUEST_CROP" +repage -format '%k' info:)"
echo "UI_HUD_TEST: K after-run quest-row stddev=$MJ_SD colors=$MJ_COLORS (bar: stddev>=0.02, colors>=4)"
fnum "$MJ_SD" 0.02 || fail "run K: AFTER quest row blank-ish (stddev=$MJ_SD) — the tracker line did not paint"
fnum "$MJ_COLORS" 4 || fail "run K: AFTER quest row has <4 colours (colors=$MJ_COLORS) — no glyph shapes present"
MJ_DIFF_SD="$(convert "$CAPDIR/la-mono-0.png" "$CAPDIR/la-mono-1.png" -compose difference -composite \
             -crop "$QUEST_CROP" +repage -colorspace Gray -format '%[fx:standard_deviation]' info:)"
echo "UI_HUD_TEST: K BEFORE-vs-AFTER quest-row diff stddev=$MJ_DIFF_SD (bar: stddev>=0.01)"
fnum "$MJ_DIFF_SD" 0.01 || fail "run K: quest row identical BEFORE/AFTER (stddev=$MJ_DIFF_SD) — the tracker did not repaint on the boss-kill beat"
echo "UI_HUD_TEST: K ok — quest line reads Watcher Falls BEFORE, the advanced act-two row AFTER the boss-kill beat at the ruins, and the SAVED state carries the advanced rows"

# ---------------------------------------------------------------------------
# (L) MC 11311 — HUD_QUESTLINE_ACT3: the tracker line follows the ACT-THREE
#     log once the act opens (MC 11295 F1: LiveQuestLine unioned ONLY act one
#     + act two — an act-three-open state painted the exhausted fallthrough
#     "Quest: none" and the act-three rows NEVER painted; the tracker never
#     showed the final act's objective). TWO runs of tests/ui/
#     HudQuestAct3CaptureTest.cs (LA_QUESTLINE_ACT3=0 / =1), leg-I idiom:
#     run 0 is the boot control (act-one active title, the same face leg I
#     pins); run 1 opens the act through the REAL save path (the Story3
#     phase-1 diverge + SaveGame/LoadGame — the shipped FreshOpen restore
#     arm) and must self-check the act-three row title ("The Hollow") while
#     its row is Active. PLANTED-BAD (this card's RED half, banked): run the
#     leg at the UNFIXED union and run 1 prints LA_QUESTLINE_ACT3: FAIL
#     line=[Quest: none] — the frame shows the act-three row NOT painted.
#     NOTE run 1 writes user://savegame.json — gate-level SAVEGATE flock.
# ---------------------------------------------------------------------------
echo "UI_HUD_TEST: run L — HUD_QUESTLINE_ACT3 baseline + act-three mid-arc title"
LOGL_BASE="$("$GUI_HELPER" --cmd "LA_QUESTLINE_ACT3=0 $GODOT --path $PROJ --script $UI_QUESTLINE_ACT3 > $CAPDIR/app-ql3-base.log 2>&1" \
            --wait 6 --out "$CAPDIR/la-ql3-base.png" 2>&1)"
printf '%s\n' "$LOGL_BASE"
[[ "$LOGL_BASE" == *'RESULT=PASS'* ]] \
  || fail "run L: baseline capture not RESULT=PASS (game frame did not render)"
[[ "$(cat "$CAPDIR/app-ql3-base.log" 2>/dev/null)" == *'LA_QUESTLINE_ACT3: PASS'* ]] \
  || fail "run L: baseline self-check marker missing (act-one tracker title not painted at boot — see app-ql3-base.log)"

LOGL_ACT3="$("$GUI_HELPER" --cmd "LA_QUESTLINE_ACT3=1 $GODOT --path $PROJ --script $UI_QUESTLINE_ACT3 > $CAPDIR/app-ql3-act3.log 2>&1" \
            --wait 6 --out "$CAPDIR/la-ql3-act3.png" 2>&1)"
printf '%s\n' "$LOGL_ACT3"
[[ "$LOGL_ACT3" == *'RESULT=PASS'* ]] \
  || fail "run L: act-three capture not RESULT=PASS (game frame did not render)"
[[ "$(cat "$CAPDIR/app-ql3-act3.log" 2>/dev/null)" == *'LA_QUESTLINE_ACT3: PASS'* ]] \
  || fail "run L: act-three self-check marker missing (the tracker did NOT follow the act-three log — the MC 11295 F1 gap is back)"

# The quest-row crop must carry glyphs in the act-three run AND differ from
# the baseline run's row (a different title painted). Bars mirror leg I.
QL3_SD="$(convert "$CAPDIR/la-ql3-act3.png" -crop "$QUEST_CROP" +repage -colorspace Gray \
        -format '%[fx:standard_deviation]' info:)"
QL3_COLORS="$(convert "$CAPDIR/la-ql3-act3.png" -crop "$QUEST_CROP" +repage -format '%k' info:)"
echo "UI_HUD_TEST: L act3 quest-row stddev=$QL3_SD colors=$QL3_COLORS (bar: stddev>=0.02, colors>=4)"
fnum "$QL3_SD" 0.02 || fail "run L: act-three quest row blank-ish (stddev=$QL3_SD) — the tracker line did not paint"
fnum "$QL3_COLORS" 4 || fail "run L: act-three quest row has <4 colours (colors=$QL3_COLORS) — no glyph shapes present"
QL3_DIFF_SD="$(convert "$CAPDIR/la-ql3-base.png" "$CAPDIR/la-ql3-act3.png" -compose difference -composite \
             -crop "$QUEST_CROP" +repage -colorspace Gray -format '%[fx:standard_deviation]' info:)"
QL3_DIFF_MEAN="$(convert "$CAPDIR/la-ql3-base.png" "$CAPDIR/la-ql3-act3.png" -compose difference -composite \
               -crop "$QUEST_CROP" +repage -colorspace Gray -format '%[fx:mean]' info:)"
echo "UI_HUD_TEST: L quest-row diff stddev=$QL3_DIFF_SD mean=$QL3_DIFF_MEAN (bar: stddev>=0.01, mean>=0.003)"
fnum "$QL3_DIFF_SD" 0.01 || fail "run L: quest row identical baseline/act-three (stddev=$QL3_DIFF_SD) — the tracker did not repaint on act open"
fnum "$QL3_DIFF_MEAN" 0.003 || fail "run L: quest row diff too small (mean=$QL3_DIFF_MEAN) — act-three title not actually painted"
echo "UI_HUD_TEST: L ok — tracker paints act-one title at boot, act-three title mid-arc (pixels moved)"

echo "UI_HUD_TEST: TRACKER-BATTERY OK (MC 11243 tail-law) — act-two quest tracker mid-arc (I), MC 11295 boss-kill-ordering quest-line frame bank with the saved state shown (K), MC 11311 act-three quest tracker mid-arc (L)"
exit 0
