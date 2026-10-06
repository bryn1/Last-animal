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
#       Since MC 10146 the run also pins the vignette paint-on-change surface:
#       Hud.VignetteColorWrites == 0/0/1/5 (zero colour writes while OFF across
#       idle redraws, one at the pulse ON edge, one per frame while ON) — drop
#       the change-guard in Hud.PaintVignette and run A goes RED.
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
#   (G) MC 10123 stage S6 — UI_TOKENS_SINGLE_SOURCE: the three panel files
#       (Hud.cs, SkillsPanel.cs, EmpathyPanel.cs) must carry ZERO raw theme
#       literals — no `new Color(` and no numeric font_size override. Their
#       single home is src/ui/UiTheme.cs (which must exist). Planted-bad
#       (evidence dir 20261005-s6-hud): re-adding
#       `label.Modulate = new Color(1f, 0f, 0f);` to Hud.cs turns this RED.
#   (H) MC 10123 stage S6 — HUD_VIGNETTE: the low-health red tint layer, ON
#       strictly BELOW 30 and OFF AT 30 (the strict `<` boundary), via two
#       graphical-test-helper runs of tests/ui/HudVignetteCaptureTest.cs
#       (LA_VIGNETTE_HEALTH=29 / =30). Each run's self-check marker pins the
#       alpha state in-code; the pixel bar measures a corner crop far from
#       every label/panel rect — pure uniform backdrop in the OFF run, so
#       the ONLY thing that can move it is the vignette: the ON run's tint
#       pixel must be non-blank red AND measurably redder than the OFF run.
#   (I) MC 10138 — HUD_QUESTLINE_ACT2: the tracker line follows the ACT-TWO
#       log once the act opens. TWO runs of tests/ui/
#       HudQuestAct2CaptureTest.cs (LA_QUESTLINE_ACT2=0 / =1). Run 0 (the
#       baseline) must self-check the ACT-ONE active title; run 1 opens the
#       act through the REAL save path (FromSaveRows diverge + SaveGame/
#       LoadGame — the shipped restore-sync open, the quest_arc2 idiom) and
#       must self-check the act-two title mid-arc ("The Way Down"). The
#       pixel bar proves the quest row is painted and its pixels actually
#       changed between the runs. PLANTED-BAD (the card's own): revert
#       WorldDirector.Ui.cs LiveQuestLine to the act-one-only read and run
#       1 shows "Quest: none" -> the self-check marker goes RED. The run-1
#       save round-trip writes user://savegame.json: run this gate under the
#       SAVEGATE flock whenever other save-touching legs may run (the fleet
#       convention, kb c8e1f2bb).
# The gate passes only if A–I all behave as expected.
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
UI_VIGNETTE="res://tests/ui/HudVignetteCaptureTest.cs"
UI_QUESTLINE="res://tests/ui/HudQuestAct2CaptureTest.cs"
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
# S6 vignette corner crop: far from every HUD label row (top-left) and from
# the SkillsPanel rect (x 8..428, y 440..700). In the capture scene nothing
# else paints there, so the ONLY thing that can move it is the vignette.
VIGNETTE_CROP="200x120+900+480"
# Leg I quest-row crop: the Quest gauge is the 6th HUD row (y = 8 + 5*28 =
# 148, the BuildUi loop pitch), label at window x 8. Measured here (MC 10138,
# deterministic across runs): the 1152x648 play window maps at +64+36 inside
# the helper's 1280x720 Xvfb root (identical trim-box on every capture — the
# shipped F/H crops tolerate the same offset with coarser bars), so the root
# crop = window rect + (64,36). 500px wide covers the longest tracker line;
# the dialogue box is CenterBottom — outside this crop.
QUEST_CROP="500x26+72+182"

fail() { echo "UI_HUD_TEST: GATE FAIL: $*" >&2; exit 1; }
[ -d "$TESTS_UI" ] || fail "ui tests dir not found: $TESTS_UI"
[ -f "$TESTS_UI/UiRenderTest.cs" ] || fail "UiRenderTest.cs not found"
[ -f "$TESTS_UI/UiHarnessSelfTest.cs" ] || fail "UiHarnessSelfTest.cs not found"
[ -f "$PROJ/tests/story/StoryTextCaptureTest.cs" ] || fail "StoryTextCaptureTest.cs not found"
[ -f "$PROJ/tests/ui/SkillsPanelTest.cs" ] || fail "SkillsPanelTest.cs not found"
[ -f "$PROJ/tests/ui/SkillsPanelCaptureTest.cs" ] || fail "SkillsPanelCaptureTest.cs not found"
[ -f "$PROJ/tests/ui/HudVignetteCaptureTest.cs" ] || fail "HudVignetteCaptureTest.cs not found"
[ -f "$PROJ/tests/ui/HudQuestAct2CaptureTest.cs" ] || fail "HudQuestAct2CaptureTest.cs not found"
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

# ---------------------------------------------------------------------------
# (G) MC 10123 stage S6 — UI_TOKENS_SINGLE_SOURCE: the three panel files carry
#     ZERO raw theme literals (their home is src/ui/UiTheme.cs, which must
#     exist). Pure static grep — no godot, no framebuffer. PLANTED-BAD (the
#     gate's own anti-rubber-stamp): add `label.Modulate = new Color(1f,0f,0f);`
#     to MakeLabel in src/ui/Hud.cs and this leg goes RED (see evidence dir
#     .audits/*-s6-hud/ ui_tokens_planted_bad.log).
# ---------------------------------------------------------------------------
echo "UI_HUD_TEST: run G — UI_TOKENS_SINGLE_SOURCE (three panel files raw-literal-clean)"
[ -f "$PROJ/src/ui/UiTheme.cs" ] || fail "run G: UiTheme.cs absent (the single token home is missing)"
for PANEL in Hud.cs SkillsPanel.cs EmpathyPanel.cs; do
  HITS="$(grep -nE 'new Color\(|AddThemeFontSizeOverride\([^)]*,[[:space:]]*[0-9]' \
          "$PROJ/src/ui/$PANEL" 2>/dev/null)"
  if [ -n "$HITS" ]; then
    echo "run G: UI_TOKENS_SINGLE_SOURCE — $PANEL reintroduced a raw theme literal (move it to UiTheme.cs):"
    printf '%s\n' "$HITS"
    fail "run G: raw theme literal(s) in $PANEL — UiTheme.cs is the single source"
  fi
done
echo "UI_HUD_TEST: G ok — Hud/SkillsPanel/EmpathyPanel carry no raw colour/font-size literal (UiTheme.cs is the sole home)"

# ---------------------------------------------------------------------------
# (H) MC 10123 stage S6 — HUD_VIGNETTE: the low-health red tint layer, ON
#     strictly below 30 and OFF at 30. TWO runs of HudVignetteCaptureTest.cs
#     (LA_VIGNETTE_HEALTH=29 vs =30) under Xvfb, each self-checking its alpha
#     state IN-SCENE (LA_VIGNETTE: PASS marker) AND measured at a far corner
#     that ONLY the vignette can move: the 29-run tint pixel must be non-blank
#     red, AND measurably redder than the 30-run (the strict `<` boundary).
#     PLANTED-BAD: flip Hud.cs PaintVignette's `Life < UiTheme.LowHealthThreshold`
#     to `<=` and the health=30 run turns the corner red -> this leg goes RED.
# ---------------------------------------------------------------------------
echo "UI_HUD_TEST: run H — HUD_VIGNETTE on/below boundary + tint pixel non-blank"
LOGH_ON="$("$GUI_HELPER" --cmd "LA_VIGNETTE_HEALTH=29 $GODOT --path $PROJ --script $UI_VIGNETTE > $CAPDIR/app-vig-29.log 2>&1" \
          --wait 6 --out "$CAPDIR/la-vig-29.png" 2>&1)"
printf '%s\n' "$LOGH_ON"
[[ "$LOGH_ON" == *'RESULT=PASS'* ]] \
  || fail "run H: health=29 capture not RESULT=PASS (game frame did not render)"
[[ "$(cat "$CAPDIR/app-vig-29.log" 2>/dev/null)" == *'LA_VIGNETTE: PASS'* ]] \
  || fail "run H: health=29 self-check marker missing (vignette did not report alpha>0 below 30)"

LOGH_OFF="$("$GUI_HELPER" --cmd "LA_VIGNETTE_HEALTH=30 $GODOT --path $PROJ --script $UI_VIGNETTE > $CAPDIR/app-vig-30.log 2>&1" \
           --wait 6 --out "$CAPDIR/la-vig-30.png" 2>&1)"
printf '%s\n' "$LOGH_OFF"
[[ "$LOGH_OFF" == *'RESULT=PASS'* ]] \
  || fail "run H: health=30 capture not RESULT=PASS (game frame did not render)"
[[ "$(cat "$CAPDIR/app-vig-30.log" 2>/dev/null)" == *'LA_VIGNETTE: PASS'* ]] \
  || fail "run H: health=30 self-check marker missing (vignette did not report alpha==0 at the boundary)"

# Red-channel mean of the far corner (a uniform backdrop OFF, red-tinted ON).
# The 29-run must be non-blank red AND strictly redder than the 30-run: that
# mean delta IS the on/off boundary painted on real pixels (t=0 pulse floor
# 0.10 over a dark backdrop clears the 0.02 bar comfortably; a no-op vignette
# leaves both means equal -> RED).
RED29="$(convert "$CAPDIR/la-vig-29.png" -crop "$VIGNETTE_CROP" +repage \
        -channel R -separate -delete 1,2 -format '%[fx:mean]' info: 2>/dev/null)"
RED30="$(convert "$CAPDIR/la-vig-30.png" -crop "$VIGNETTE_CROP" +repage \
        -channel R -separate -delete 1,2 -format '%[fx:mean]' info: 2>/dev/null)"
RED_DELTA="$(awk -v a="$RED29" -v b="$RED30" 'BEGIN { printf "%.6f", a - b }')"
echo "UI_HUD_TEST: H vignette-corner red mean: health29=$RED29  health30=$RED30  delta=$RED_DELTA (bar: red29>=0.15, delta>=0.02)"
fnum "$RED29" 0.15 \
  || fail "run H: health=29 corner red mean=$RED29 — the low-health tint pixel is blank (no red paint in the far corner)"
fnum "$RED_DELTA" 0.02 \
  || fail "run H: corner red delta=$RED_DELTA too small — the vignette does not turn ON below 30 / OFF at 30 (boundary not painted)"
echo "UI_HUD_TEST: H ok — vignette ON <30 (tint pixel non-blank red), OFF at the 30 boundary"

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

echo "UI_HUD_TEST: GATE PASS — C13 Hud/Dialogue/EmpathyPanel checks green (A), harness self-test red (B), non-blank framebuffer render (C), story authored node exact-text capture (D), 2f skills/quest UI logic + palette guard (E), 2f framebuffer Manna digits + TAB panel pixel diff (F), S6 UI tokens single-sourced (G), S6 low-health vignette on/off at the 30 boundary + tint pixel (H), act-two quest tracker mid-arc (I)"
exit 0
