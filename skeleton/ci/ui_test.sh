#!/usr/bin/env bash
# ui_test.sh — M10 ui-hud DoD gate (MC 890.14, dobbie, 2026-09-06).
#
# The Phase-11 DoD (PHASE0.md Phase 11, C13):
#   "headless render of the HUD scene is non-blank with all bound values
#    updated from EventBus (graphical-test-helper exit 0); EmpathyPanel opens
#    a real BookEntry read from M04."
#
# This gate proves the C13 contract on the Godot side (the UI modules live in
# src/ui/ and are compiled by the main engine project). Following the fleet's
# two-sided calibration discipline:
#   (A) run the render test HEADLESS  -> exit 0 AND the 'M10_UI_RENDER_TEST:
#       PASS' marker. Its in-code checks assert the C13 surface: the HUD binds
#       four gauges and moves DnaMeter (DnaExtracted/DnaSpoken) + CompanionHearts
#       (LoyaltyChanged) FROM the EventBus; DialogueSystem.Show paints a node;
#       EmpathyPanel.Open surfaces a REAL M04 BookEntry read from EmpathyBook.Query.
#   (B) run the UiHarnessSelfTest     -> must exit NON-zero (deliberately broken;
#       proves the gate can fail — not a rubber stamp).
#   (C) run the UI under graphical-test-helper (Xvfb) -> RESULT=PASS means the
#       framebuffer is non-blank (the Phase-11 DoD's named render bar).
#   (D) MC 3900 stage 2a — story exact-text capture: run
#       tests/story/StoryTextCaptureTest.cs headless -> exit 0 AND the
#       'LA_STORY_CAPTURE: PASS' marker. Standing ruling (PLAN 2a, DA-c1
#       P3-12): non-blank is the floor, NOT the bar — this leg asserts the
#       dialogue Label's Text EQUALS the authored DialogueTable string for a
#       newly authored node (exact-text equality, read from the live node
#       tree), plus the frozen Close/fallback contract.
#   (E) MC 3933 stage 2f — skills/quest UI logic: run
#       tests/ui/SkillsPanelTest.cs headless -> exit 0 AND the
#       'LA_2F_UI_TEST: PASS' marker. Panel content derived from the live
#       unlock authority (a REAL consensus unlock, not a hand-fed fixture);
#       HUD freshness legs (labels track the world with no manual poke —
#       the planted-bad "stale HUD cache" goes RED here); bus refresh on
#       QuestCompleted/SkillUsed; the TAB toggle round-trip; a palette guard
#       asserting the new labels reuse the Hud gauge colour (zero new
#       colours — the palette rule; legs A-D untouched, no pixel-palette leg
#       exists at HEAD to keep).
#   (F) MC 3933 stage 2f — headless framebuffer legs (graphical-test-helper
#       on Xvfb, two runs of tests/ui/SkillsPanelCaptureTest.cs which boots
#       the REAL WorldDirector and TAB-toggles only when LA_2F_TOGGLED=1):
#        F1  the Manna DIGITS region (HUD row 2 crop) is non-blank — the
#            persistent HUD readout genuinely paints its numbers;
#        F2  the panel region differs before/after TAB (pixel diff of the
#            fixed SkillsPanel rect across the two runs) — the real
#            ui_toggle handler paints the panel. Remove the TAB handler and
#            the two frames are byte-identical there and this leg goes RED
#            (planted-bad #2). No Camera3D exists in the capture scene, so
#            every other pixel is static 2D — the diff is the panel or zero.
# The gate passes only if A–F all behave as expected.
#
# Usage:
#   ./ci/ui_test.sh [project_dir]   (defaults to dir above ci/)
#   GUI_HELPER=... to override the graphical-test-helper path (default /usr/local/bin).
set -uo pipefail

HERE="$(cd "$(dirname "$0")" && pwd)"
PROJ="${1:-$(dirname "$HERE")}"
TESTS_UI="$PROJ/tests/ui"
GUI_HELPER="${GUI_HELPER:-/usr/local/bin/graphical-test-helper.sh}"
UI_RENDER="res://tests/ui/UiRenderTest.cs"
UI_HARNESS="res://tests/ui/UiHarnessSelfTest.cs"
STORY_CAPTURE="res://tests/story/StoryTextCaptureTest.cs"
UI_2F_LOGIC="res://tests/ui/SkillsPanelTest.cs"
UI_2F_CAPTURE="res://tests/ui/SkillsPanelCaptureTest.cs"
PASS_MARKER="M10_UI_RENDER_TEST: PASS"
STORY_CAPTURE_MARKER="LA_STORY_CAPTURE: PASS"
UI_2F_MARKER="LA_2F_UI_TEST: PASS"

# 2f capture rects — MUST sit inside SkillsPanel.RegionX/Y/Width/Height and
# the Hud row pitch (y = 8 + 28*row; Manna is row 1). The play window is the
# project default 1152x648 mapped at the Xvfb root's top-left, so the panel
# crop stays inside both the panel rect and the window (440..640). A drift
# here fails F1/F2 loudly, never silently.
MANNA_CROP="260x26+8+36"
PANEL_CROP="420x200+8+440"

fail() { echo "UI_HUD_TEST: GATE FAIL: $*" >&2; exit 1; }
[ -d "$TESTS_UI" ] || fail "ui tests dir not found: $TESTS_UI"
[ -f "$TESTS_UI/UiRenderTest.cs" ] || fail "UiRenderTest.cs not found"
[ -f "$TESTS_UI/UiHarnessSelfTest.cs" ] || fail "UiHarnessSelfTest.cs not found"
[ -f "$PROJ/tests/story/StoryTextCaptureTest.cs" ] || fail "StoryTextCaptureTest.cs not found"
[ -f "$PROJ/tests/ui/SkillsPanelTest.cs" ] || fail "SkillsPanelTest.cs not found"
[ -f "$PROJ/tests/ui/SkillsPanelCaptureTest.cs" ] || fail "SkillsPanelCaptureTest.cs not found"
: "${GODOT:?set GODOT to the engine binary (see ../engine/PIN.txt)}"
[ -x "$GODOT" ] || fail "GODOT not executable: $GODOT"
[ -f "$GUI_HELPER" ] || fail "graphical-test-helper not found: $GUI_HELPER (install per infra/vm105)"

echo "UI_HUD_TEST: project=$PROJ  godot=$("$GODOT" --version 2>/dev/null | tail -1)"

# ---------------------------------------------------------------------------
# build through the pinned engine so the C# assembly (incl. src/ui/) is current
# ---------------------------------------------------------------------------
echo "UI_HUD_TEST: build through engine"
timeout 300 "$GODOT" --headless --path "$PROJ" --build-solutions --quit-after 1 \
  || fail "engine --build-solutions exited nonzero (C# build failed)"

# ---------------------------------------------------------------------------
# (A) headless C13 assertions — render test must exit 0 + PASS marker
# ---------------------------------------------------------------------------
echo "UI_HUD_TEST: run A — headless render test (expect exit 0 + PASS)"
LOGA="$(timeout 180 "$GODOT" --headless --path "$PROJ" --script "$UI_RENDER" 2>&1)"
CODEA=$?
printf '%s\n' "$LOGA"
[ "$CODEA" -eq 0 ] || fail "run A: render test exited $CODEA (non-zero)"
# bash-native substring checks (no pipe => no SIGPIPE race under pipefail)
[[ "$LOGA" == *"$PASS_MARKER"* ]] \
  || fail "run A: expected '$PASS_MARKER' marker (a C13 check went red)"

# ---------------------------------------------------------------------------
# (B) two-sided calibration — harness self-test must go red (non-zero)
# ---------------------------------------------------------------------------
echo "UI_HUD_TEST: run B — harness self-test (expect non-zero exit)"
LOGB="$(timeout 60 "$GODOT" --headless --path "$PROJ" --script "$UI_HARNESS" 2>&1)"
CODEB=$?
printf '%s\n' "$LOGB"
[ "$CODEB" -ne 0 ] || fail "run B: harness self-test exited 0 (gate cannot fail — broken)"

# ---------------------------------------------------------------------------
# (C) framebuffer render proof — graphical-test-helper must report RESULT=PASS
# ---------------------------------------------------------------------------
echo "UI_HUD_TEST: run C — graphical-test-helper non-blank render (expect RESULT=PASS)"
LOGC="$("$GUI_HELPER" --cmd "$GODOT --path $PROJ --script $UI_RENDER" --wait 3 2>&1)"
CODEc=$?
printf '%s\n' "$LOGC"
[ "$CODEc" -eq 0 ] || fail "run C: graphical-test-helper exited $CODEc (non-blank render bar not met)"
[[ "$LOGC" == *'RESULT=PASS'* ]] \
  || fail "run C: expected 'RESULT=PASS' from graphical-test-helper (framebuffer blank)"

# ---------------------------------------------------------------------------
# (D) MC 3900 stage 2a — story exact-text capture (authored node paints its
#     EXACT string; non-blank is NOT enough)
# ---------------------------------------------------------------------------
echo "UI_HUD_TEST: run D — story exact-text capture (expect exit 0 + PASS)"
LOGD="$(timeout 180 "$GODOT" --headless --path "$PROJ" --script "$STORY_CAPTURE" 2>&1)"
CODED=$?
printf '%s\n' "$LOGD"
[ "$CODED" -eq 0 ] || fail "run D: story capture exited $CODED (authored node did not paint its exact string — see LA_STORY_CAPTURE lines above)"
[[ "$LOGD" == *"$STORY_CAPTURE_MARKER"* ]] \
  || fail "run D: expected '$STORY_CAPTURE_MARKER' marker (exact-text or Close/fallback check went red)"

# ---------------------------------------------------------------------------
# (E) MC 3933 stage 2f — skills/quest UI logic (derive-from-state + freshness)
# ---------------------------------------------------------------------------
echo "UI_HUD_TEST: run E — 2f skills/quest UI logic (expect exit 0 + PASS)"
LOGE="$(timeout 180 "$GODOT" --headless --path "$PROJ" --script "$UI_2F_LOGIC" 2>&1)"
CODEE=$?
printf '%s\n' "$LOGE"
[ "$CODEE" -eq 0 ] || fail "run E: 2f logic leg exited $CODEE (content/freshness/palette check went red — see LA_2F_UI_TEST lines above)"
[[ "$LOGE" == *"$UI_2F_MARKER"* ]] \
  || fail "run E: expected '$UI_2F_MARKER' marker (panel content not derived from live state, or a HUD freshness leg went red)"

# ---------------------------------------------------------------------------
# (F) MC 3933 stage 2f — headless framebuffer legs (Manna digits + TAB diff)
#     Two runs of the REAL WorldDirector under Xvfb (static 2D-only scene):
#     closed (LA_2F_TOGGLED=0) and open (LA_2F_TOGGLED=1), each captured by
#     graphical-test-helper into CAPDIR. The capture scene has NO Camera3D:
#     every pixel outside the UI is one uniform colour and cannot move, so
#     the region diff is the panel or nothing.
# ---------------------------------------------------------------------------
command -v convert >/dev/null 2>&1 \
  || fail "run F: ImageMagick 'convert' missing (the helper's own bar, needed for the region asserts)"
CAPDIR="$(mktemp -d "${TMPDIR:-/tmp}/la-2f-capture.XXXXXX")" || fail "run F: mktemp failed"
trap 'rm -rf "$CAPDIR"' EXIT

fnum() { awk -v a="$1" -v b="$2" 'BEGIN { exit !(a >= b) }'; }

echo "UI_HUD_TEST: run F1 — Manna digits region non-blank (framebuffer)"
# NOTE: graphical-test-helper backgrounds the command and sends its stdout to
# its own temp app.log (gone at cleanup), so each capture command redirects
# into CAPDIR from inside the helper's bash -c — the gate then reads the
# capture script's self-check marker from the very log the PNG was taken of.
LOGF1="$("$GUI_HELPER" --cmd "LA_2F_TOGGLED=0 $GODOT --path $PROJ --script $UI_2F_CAPTURE > $CAPDIR/app-closed.log 2>&1" \
        --wait 6 --out "$CAPDIR/la-2f-closed.png" 2>&1)"
printf '%s\n' "$LOGF1"
[[ "$LOGF1" == *'RESULT=PASS'* ]] \
  || fail "run F1: closed-run capture not RESULT=PASS (game frame did not render)"
[[ "$(cat "$CAPDIR/app-closed.log" 2>/dev/null)" == *'LA_2F_CAPTURE: PASS'* ]] \
  || fail "run F1: closed-run self-check marker missing (panel state at boot wrong)"
MAN_SD="$(convert "$CAPDIR/la-2f-closed.png" -crop "$MANNA_CROP" +repage -colorspace Gray \
         -format '%[fx:standard_deviation]' info:)"
MAN_COLORS="$(convert "$CAPDIR/la-2f-closed.png" -crop "$MANNA_CROP" +repage -format '%k' info:)"
echo "UI_HUD_TEST: F1 manna-region stddev=$MAN_SD colors=$MAN_COLORS (bar: stddev>=0.02, colors>=4)"
fnum "$MAN_SD" 0.02 || fail "run F1: Manna digits region blank-ish (stddev=$MAN_SD) — the HUD did not paint the digits"
fnum "$MAN_COLORS" 4 || fail "run F1: Manna digits region has <$4 colours (colors=$MAN_COLORS) — no glyph shapes present"

echo "UI_HUD_TEST: run F2 — TAB before/after pixel diff on the panel region"
LOGF2="$("$GUI_HELPER" --cmd "LA_2F_TOGGLED=1 $GODOT --path $PROJ --script $UI_2F_CAPTURE > $CAPDIR/app-open.log 2>&1" \
        --wait 6 --out "$CAPDIR/la-2f-open.png" 2>&1)"
printf '%s\n' "$LOGF2"
[[ "$LOGF2" == *'RESULT=PASS'* ]] \
  || fail "run F2: open-run capture not RESULT=PASS (game frame did not render)"
[[ "$(cat "$CAPDIR/app-open.log" 2>/dev/null)" == *'LA_2F_CAPTURE: PASS'* ]] \
  || fail "run F2: open-run self-check marker missing (TAB press did not open the panel)"
DIFF_SD="$(convert "$CAPDIR/la-2f-closed.png" "$CAPDIR/la-2f-open.png" -compose difference -composite \
          -crop "$PANEL_CROP" +repage -colorspace Gray -format '%[fx:standard_deviation]' info:)"
DIFF_MEAN="$(convert "$CAPDIR/la-2f-closed.png" "$CAPDIR/la-2f-open.png" -compose difference -composite \
            -crop "$PANEL_CROP" +repage -colorspace Gray -format '%[fx:mean]' info:)"
echo "UI_HUD_TEST: F2 panel-region diff stddev=$DIFF_SD mean=$DIFF_MEAN (bar: stddev>=0.01, mean>=0.003)"
fnum "$DIFF_SD" 0.01 || fail "run F2: panel region identical before/after TAB (stddev=$DIFF_SD) — the ui_toggle handler did not paint the panel"
fnum "$DIFF_MEAN" 0.003 || fail "run F2: panel region diff too small (mean=$DIFF_MEAN) — panel content missing"

echo "UI_HUD_TEST: GATE PASS — C13 Hud/Dialogue/EmpathyPanel checks green (A), harness self-test red (B), non-blank framebuffer render (C), story authored node exact-text capture (D), 2f skills/quest UI logic + palette guard (E), 2f framebuffer Manna digits + TAB panel pixel diff (F)"
exit 0
