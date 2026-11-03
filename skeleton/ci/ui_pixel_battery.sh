#!/usr/bin/env bash
# ui_pixel_battery.sh — legs C, F1, F2, H of the M10 ui-hud DoD gate. SIZE (file hygiene law): 177 l by wc AT this head — carved out of ci/ui_test.sh by MC 11891 (MC 11303 F-P3-2 named the split; the >400 re-justification chain retired with it). ONE concern here: proofs that run the game under Xvfb and measure PIXELS (the render bar + panel paint + vignette boundary). Runs after the entry build; runnable standalone against a built tree with the same args (GODOT=... [GUI_HELPER=...]). Echo labels keep the gate name UI_HUD_TEST (one gate, three files — ci/ui_test.sh owns the entry + final GATE PASS).
#
#   (C) MC framebuffer render proof — run the UI under graphical-test-helper
#       (Xvfb) -> RESULT=PASS means the framebuffer is non-blank (the
#       Phase-11 DoD's named render bar).
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
#   (G) lives in the ENTRY (ci/ui_test.sh) — labelled G there, skipped here.
#   (H) MC 10123 stage S6 — HUD_VIGNETTE: the low-health red tint layer, ON
#       strictly BELOW 30 and OFF AT 30 (the strict `<` boundary), via two
#       graphical-test-helper runs of tests/ui/HudVignetteCaptureTest.cs
#       (LA_VIGNETTE_HEALTH=29 / =30). Each run's self-check marker pins the
#       alpha state in-code; the pixel bar measures a corner crop far from
#       every label/panel rect — pure uniform backdrop in the OFF run, so
#       the ONLY thing that can move it is the vignette: the ON run's tint
#       pixel must be non-blank red AND measurably redder than the OFF run.
#       PLANTED-BAD: flip Hud.cs PaintVignette's `Life < UiTheme.LowHealthThreshold`
#       to `<=` and the health=30 run turns the corner red -> this leg goes RED.
set -uo pipefail

HERE="$(cd "$(dirname "$0")" && pwd)"
PROJ="${1:-$(dirname "$HERE")}"
TESTS_UI="$PROJ/tests/ui"
GUI_HELPER="${GUI_HELPER:-/usr/local/bin/graphical-test-helper.sh}"
UI_RENDER="res://tests/ui/UiRenderTest.cs"
UI_2F_CAPTURE="res://tests/ui/SkillsPanelCaptureTest.cs"
UI_VIGNETTE="res://tests/ui/HudVignetteCaptureTest.cs"

fail() { echo "UI_HUD_TEST: GATE FAIL: $*" >&2; exit 1; }
[ -d "$TESTS_UI" ] || fail "ui tests dir not found: $TESTS_UI"
[ -f "$TESTS_UI/UiRenderTest.cs" ] || fail "UiRenderTest.cs not found"
[ -f "$TESTS_UI/SkillsPanelCaptureTest.cs" ] || fail "SkillsPanelCaptureTest.cs not found"
[ -f "$TESTS_UI/HudVignetteCaptureTest.cs" ] || fail "HudVignetteCaptureTest.cs not found"
: "${GODOT:?set GODOT to the engine binary (see ../engine/PIN.txt)}"
[ -x "$GODOT" ] || fail "GODOT not executable: $GODOT"
[ -f "$GUI_HELPER" ] || fail "graphical-test-helper not found: $GUI_HELPER (install per infra/vm105)"
command -v convert >/dev/null 2>&1 \
  || fail "run F: ImageMagick 'convert' missing (the helper's own bar, needed for the region asserts)"
CAPDIR="$(mktemp -d "${TMPDIR:-/tmp}/la-2f-capture.XXXXXX")" || fail "run F: mktemp failed"
trap 'rm -rf "$CAPDIR"' EXIT

fnum() { awk -v a="$1" -v b="$2" 'BEGIN { exit !(a >= b) }'; }

# 2f capture rects — MUST sit inside SkillsPanel.RegionX/Y/Width/Height and
# the Hud row pitch (y = 8 + 28*row; Manna is row 1). The play window is the
# project default 1152x648 mapped at the Xvfb root's top-left, so the panel
# crop stays inside both the panel rect and the window. MC 11891 F-P3-1: the
# Region rect is DERIVED from src/ui/SkillsPanel.cs here (it was a hard-coded
# 420x200+8+440 + an `x 8..428` comment — silent-drift coupling); a Region
# change now MOVES the crop with it, and the loud checks below fail the leg
# instead of measuring the wrong pixels.
SP_X="$(sed -n 's/.*public const int RegionX = \([0-9]*\).*/\1/p' "$PROJ/src/ui/SkillsPanel.cs")"
SP_Y="$(sed -n 's/.*public const int RegionY = \([0-9]*\).*/\1/p' "$PROJ/src/ui/SkillsPanel.cs")"
SP_W="$(sed -n 's/.*public const int RegionWidth = \([0-9]*\).*/\1/p' "$PROJ/src/ui/SkillsPanel.cs")"
SP_H="$(sed -n 's/.*public const int RegionHeight = \([0-9]*\).*/\1/p' "$PROJ/src/ui/SkillsPanel.cs")"
{ [ -n "$SP_X" ] && [ -n "$SP_Y" ] && [ -n "$SP_W" ] && [ -n "$SP_H" ]; } \
  || fail "pixel battery: cannot derive the SkillsPanel Region from src/ui/SkillsPanel.cs"
PANEL_MEASURE_H=200   # F2's diff-crop height INSIDE the Region (RegionHeight is 260)
[ "$PANEL_MEASURE_H" -le "$SP_H" ] \
  || fail "pixel battery: F2 crop height $PANEL_MEASURE_H exceeds RegionHeight $SP_H — the Region law changed, update this leg"
PANEL_CROP="${SP_W}x${PANEL_MEASURE_H}+${SP_X}+${SP_Y}"
MANNA_CROP="260x26+8+36"        # Hud row-1 readout (pitch y = 8 + 28*row); F1
# S6 vignette corner crop: far from every HUD label row (top-left) and from
# the SkillsPanel rect (x SP_X..SP_X+SP_W, y SP_Y..SP_Y+SP_H — DERIVED above,
# MC 11891 F-P3-1: it was the literal `x 8..428` this comment carried). In the
# capture scene nothing else paints there, so the ONLY thing that can move it
# is the vignette. Loud coupling: if a Region drift ever pulls the TAB panel
# into this corner, fail HERE — never let leg H measure panel pixels.
VIGNETTE_CROP="200x120+900+480"
[ 900 -ge "$((SP_X + SP_W))" ] \
  || fail "pixel battery: vignette crop $VIGNETTE_CROP no longer clear of the TAB panel (x ${SP_X}..$((SP_X + SP_W))) — leg H would measure panel pixels"

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
# (F) MC 3933 stage 2f — headless framebuffer legs (Manna digits + TAB diff)
#     Two runs of the REAL WorldDirector under Xvfb (static 2D-only scene):
#     closed (LA_2F_TOGGLED=0) and open (LA_2F_TOGGLED=1), each captured by
#     graphical-test-helper into CAPDIR. The capture scene has NO Camera3D:
#     every pixel outside the UI is one uniform colour and cannot move, so
#     the region diff is the panel or nothing.
# ---------------------------------------------------------------------------
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
fnum "$MAN_COLORS" 4 || fail "run F1: Manna digits region has <4 colours (colors=$MAN_COLORS) — no glyph shapes present"

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
# (H) MC 10123 stage S6 — HUD_VIGNETTE (header docs; two runs 29/30, tint
#     pixel non-blank red AND strictly redder than the 30-run — the strict
#     `<` boundary painted on real pixels).
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

echo "UI_HUD_TEST: PIXEL-BATTERY OK (MC 11243 tail-law) — non-blank framebuffer render (C), 2f framebuffer Manna digits + TAB panel pixel diff on the DERIVED Region crop (F), low-health vignette on/off at the 30 boundary + tint pixel (H)"
exit 0
