#!/usr/bin/env bash
# ui_test.sh — M10 ui-hud DoD gate (MC 890.14, dobbie, 2026-09-06). SIZE (file hygiene law, MC 11431 R1): 273 l by wc AT this head — the >400 re-justification chain (471→523→538, MC 11431 R1 → 11311 → 11303) is RETIRED here, not ratcheted a fifth time: MC 11303 F-P3-2 named the split and MC 11891 lands it. This file is the gate ENTRY — build + the no-framebuffer assert legs (A, B, D, E, G) + the band leg J (windowed since MC 11891, see the (J) note). The Xvfb pixel legs C/F/H moved to ci/ui_pixel_battery.sh and the save-touching quest-tracker legs I/K/L to ci/ui_tracker_battery.sh — legs, markers and pixel bars moved UNCHANGED; section docs moved with them. Each of the three files answers fewer questions than the one did (coding-discipline §split rule 6); the entry keeps the gate name and the MC 11243 tail-law echoes.
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
#   (C) MC framebuffer render proof   -> MOVED to ci/ui_pixel_battery.sh (leg C):
#       graphical-test-helper RESULT=PASS is the Phase-11 DoD's non-blank bar.
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
#   (F) MC 3933 stage 2f — headless framebuffer legs -> MOVED to
#       ci/ui_pixel_battery.sh (F1 Manna digits, F2 TAB panel pixel diff).
#   (G) MC 10123 stage S6 — UI_TOKENS_SINGLE_SOURCE: the three panel files
#       (Hud.cs, SkillsPanel.cs, EmpathyPanel.cs) must carry ZERO raw theme
#       literals — no `new Color(` and no numeric font_size override. Their
#       single home is src/ui/UiTheme.cs (which must exist). Planted-bad
#       (evidence dir 20261005-s6-hud): re-adding
#       `label.Modulate = new Color(1f, 0f, 0f);` to Hud.cs turns this RED.
#   (H) MC 10123 stage S6 — HUD_VIGNETTE -> MOVED to ci/ui_pixel_battery.sh (leg H).
#   (I) MC 10138 — HUD_QUESTLINE_ACT2    -> MOVED to ci/ui_tracker_battery.sh (leg I).
#   (J) MC 11293 — HUD_UI_DISJOINT + MC 11303 TAB-OPEN + MC 11891
#       DIALOGUE_IN_VIEW: tests/ui/HudDialogueOverlapTest.cs boots the REAL
#       playable scene, opens the Empathy Book (B), a dialogue line (E) and
#       the TAB panel, and measures the painted bounding boxes (global rect
#       widened to the text's combined min-size — the broken layouts painted
#       from a ZERO-sized control, so a bare rect compare went green on the
#       bug): any overlap prints a NAMED FAIL (HUD_DIALOGUE_OVERLAP /
#       HUD_EMPATHY_OVERLAP / TAB_DIALOGUE_OVERLAP); band or text past a
#       window edge prints DIALOGUE_IN_VIEW (MC 11891); an unopened surface
#       prints OVERLAP_PROOF_NOT_ARMED (the vacuous-green wall). Since owner
#       ruling B (verbatim "B", 2026-11-03) the dialogue band REFLOWS to the
#       live visible rect (the constant-span PanelViewport fallback is stood
#       down for the band), so the leg runs at TWO widths: the shipped default
#       and the planted narrow LA_OVERLAP_FORCE_W=800 — the exact width where
#       the old fallback clipped half of every line and leg J certified THAT
#       screen PASS (MC 11303 F-P1-1, frames/control-narrow-800.png).
#       J1 (the old HEADLESS assert leg) is RETIRED here, named: it only
#       measured sense THROUGH the fallback — headless composes at the nominal
#       64x64 whatever is planted (composed viewport=(64, 64), MC 11303
#       F-P2-1), so it asserted a layout that never paints. The asserts ride
#       the WINDOWED runs now; per width the SAME script runs twice through
#       the SAME helper path (no second capture mechanism): a frame run that
#       captures mid-hold with the frame beside the very log its markers were
#       read from, and an rc run that lets the game reach its own
#       verdict-quit — GAME_RC= is then the process's own OS exit code,
#       written by the shell that ran it (rc from the process, never a pipe
#       tail; a hang dies before the line and dies RED).
#       PLANTED-BAD (red pair banked .audits/20261103-0700-bandreflow/out/):
#       this leg's own bars run against the PRE-fix bytes at the planted
#       800 px width exit GAME_RC=1 naming the clipped rects (DIALOGUE_IN_VIEW
#       FAIL). The MC 11293 zero-size-collapse revert still redds the
#       HUD_*_OVERLAP legs (older pair .audits/202610251405-d1/).
#   (K) MC 11295 — HUD_QUEST_MONO        -> MOVED to ci/ui_tracker_battery.sh (leg K).
#   (L) MC 11311 — HUD_QUESTLINE_ACT3    -> MOVED to ci/ui_tracker_battery.sh (leg L).
# The gate passes only if A–L all behave as expected (this entry + the two
# battery files it runs).
#
# Usage:
#   ./ci/ui_test.sh [project_dir]   (defaults to dir above ci/)
#   GUI_HELPER=...  to override the graphical-test-helper path (default /usr/local/bin).
#   FRAME_DIR=...   (optional) bank the leg-J capture frames into this dir —
#                   the SAME files the leg asserted on (MC 11840 evidence class).
set -uo pipefail

HERE="$(cd "$(dirname "$0")" && pwd)"
PROJ="${1:-$(dirname "$HERE")}"
# MC 11243 F4 pointer: PROJ is NOT absolutized here (latent style — this script passes no LA_*CAP*_PATH env to the engine, MC 11152 sibling audit); if one is ever added, adopt the MC 11152 cd-and-pwd idiom — see the PROJ= block at the top of skeleton/ci/runtime_integration_test.sh.
TESTS_UI="$PROJ/tests/ui"
GUI_HELPER="${GUI_HELPER:-/usr/local/bin/graphical-test-helper.sh}"
UI_RENDER="res://tests/ui/UiRenderTest.cs"
UI_HARNESS="res://tests/ui/UiHarnessSelfTest.cs"
STORY_CAPTURE="res://tests/story/StoryTextCaptureTest.cs"
UI_2F_LOGIC="res://tests/ui/SkillsPanelTest.cs"
UI_OVERLAP="res://tests/ui/HudDialogueOverlapTest.cs"
PASS_MARKER="M10_UI_RENDER_TEST: PASS"
STORY_CAPTURE_MARKER="LA_STORY_CAPTURE: PASS"
UI_2F_MARKER="LA_2F_UI_TEST: PASS"
# MC 11891: the planted narrow width — the window the independent reviewer
# captured CLIPPED (control frame banked .audits/20261101-1215-taboverlap/).
# Owner ruling B: no minimum window size ships; the gate PROVES the band
# fits at this width and goes RED on a name when it does not.
NARROW_W=800
# The frame run must capture while the game HOLDS the opened state (a
# self-quit tears the window down -> black capture, measured MC 11891; the
# hold now survives FAIL too — a FAIL that returns true from _Process QUITS
# via MainLoop semantics, so the red state historically died one tick before
# any capture, which is why the MC 11303 red PNG raced its verdict) and
# AFTER first paint (llvmpipe boots this scene in 4–15 s; the capture window
# is boot+3 .. boot+20, so 18 s with the rc run outliving boot+20 s — both
# measured MC 11891). A boot outside that band fails the leg loudly (black /
# missing line), never silently.
OVERLAP_FRAME_WAIT=18
OVERLAP_RC_WAIT=45

fail() { echo "UI_HUD_TEST: GATE FAIL: $*" >&2; exit 1; }
[ -d "$TESTS_UI" ] || fail "ui tests dir not found: $TESTS_UI"
[ -f "$TESTS_UI/UiRenderTest.cs" ] || fail "UiRenderTest.cs not found"
[ -f "$TESTS_UI/UiHarnessSelfTest.cs" ] || fail "UiHarnessSelfTest.cs not found"
[ -f "$PROJ/tests/story/StoryTextCaptureTest.cs" ] || fail "StoryTextCaptureTest.cs not found"
[ -f "$PROJ/tests/ui/SkillsPanelTest.cs" ] || fail "SkillsPanelTest.cs not found"
[ -f "$PROJ/tests/ui/HudDialogueOverlapTest.cs" ] || fail "HudDialogueOverlapTest.cs not found"
: "${GODOT:?set GODOT to the engine binary (see ../engine/PIN.txt)}"
[ -x "$GODOT" ] || fail "GODOT not executable: $GODOT"
[ -f "$GUI_HELPER" ] || fail "graphical-test-helper not found: $GUI_HELPER (install per infra/vm105)"

echo "UI_HUD_TEST: project=$PROJ  godot=$("$GODOT" --version 2>/dev/null | tail -1)"

# ---------------------------------------------------------------------------
# build through the pinned engine so the C# assembly (incl. src/ui/) is current
# ---------------------------------------------------------------------------
echo "UI_HUD_TEST: build through engine"
GODOT="$GODOT" bash "$HERE/build_solutions.sh" "$PROJ" 1 \
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
# (J) MC 11293 + MC 11303 + MC 11891 — HUD_UI_DISJOINT, TAB-OPEN stage and
#     DIALOGUE_IN_VIEW, WINDOWED (J1 headless RETIRED at MC 11891 — the (J)
#     header note names why). One overlap_run per width: the same test script,
#     the same helper capture path leg J always used — the narrow run is the
#     existing LA_OVERLAP_FORCE_W plant, not a second mechanism.
# ---------------------------------------------------------------------------
CAPDIR="$(mktemp -d "${TMPDIR:-/tmp}/la-ui-band-legJ.XXXXXX")" || fail "run J: mktemp failed"
trap 'rm -rf "$CAPDIR"' EXIT

overlap_run() { # overlap_run <label> <out-png-name> <env-prefix-or-empty> — TWO
  # helper runs of the SAME script on the SAME helper path (leg J's capture
  # mechanism, no second one): (a) the FRAME run captures mid-hold, the frame
  # beside the very log its markers were read from; (b) the RC run lets the
  # game reach its own verdict-quit so the process's OS exit code lands as
  # GAME_RC= (written by the shell that ran the process — rc from the
  # process, never a pipe tail; its own capture is post-exit-blank and NOT
  # a bar). A hang kills (b) before GAME_RC= — missing line is a FAIL.
  local label="$1" png="$2" envpre="${3:-}"
  local log="$CAPDIR/app-$png.log" rclog="$CAPDIR/app-$png.rc.log" helper_log app
  helper_log="$("$GUI_HELPER" --cmd "$envpre$GODOT --path $PROJ --script $UI_OVERLAP > $log 2>&1" \
               --wait "$OVERLAP_FRAME_WAIT" --out "$CAPDIR/$png.png" 2>&1)"
  printf '%s\n' "$helper_log"
  app="$(cat "$log" 2>/dev/null)"
  printf '%s\n' "$app" | grep -E 'LA_GATE' || true
  # bank the frame the leg MEASURED (red runs must bank their clipped frame too)
  if [ -n "${FRAME_DIR:-}" ]; then mkdir -p "$FRAME_DIR"; cp "$CAPDIR/$png.png" "$FRAME_DIR/$png.png" 2>/dev/null || true; fi
  # named asserts first: the DIALOGUE_IN_VIEW FAIL line is the diagnosis a
  # reader needs; RESULT is the render bar beneath it
  [[ "$app" != *'FAIL — DIALOGUE_IN_VIEW'* ]] \
    || fail "run $label: DIALOGUE_IN_VIEW red — the window edge cuts the band at this width (MC 11303 F-P1-1 is back)"
  [[ "$helper_log" == *'RESULT=PASS'* ]] \
    || fail "run $label: capture not RESULT=PASS (game frame did not render)"
  [[ "$app" == *'LA_GATE: PASS — HUD/dialogue/empathy/TAB bounding boxes DISJOINT'* ]] \
    || fail "run $label: expected the DISJOINT PASS marker (the measure leg never ran — a named FAIL/NOT_ARMED line is above)"
  [[ "$app" == *'DIALOGUE_IN_VIEW: ok'* ]] \
    || fail "run $label: expected the DIALOGUE_IN_VIEW ok marker (the MC 11891 in-view assert never ran)"
  "$GUI_HELPER" --cmd "$envpre$GODOT --path $PROJ --script $UI_OVERLAP > $rclog 2>&1; echo GAME_RC=\$? >> $rclog" \
      --wait "$OVERLAP_RC_WAIT" --out "$CAPDIR/$png-rconly.png" >/dev/null 2>&1
  app="$(cat "$rclog" 2>/dev/null)"
  printf '%s\n' "$app" | grep -E 'LA_GATE: (rects|check|PASS|FAIL)|GAME_RC' || true
  [[ "$app" == *'GAME_RC=0'* ]] \
    || fail "run $label: the game process did not exit 0 (GAME_RC= line missing or non-zero — verdict-quit rc read from the process)"
}

echo "UI_HUD_TEST: run J2 — HUD_UI_DISJOINT + DIALOGUE_IN_VIEW at the shipped default window (expect GAME_RC=0 + markers, frame beside the log)"
overlap_run J2 la-overlap-default ""

echo "UI_HUD_TEST: run J3 — planted narrow window LA_OVERLAP_FORCE_W=$NARROW_W: the band REFLOWS to fit and stays in view (MC 11891 owner ruling B; expect GAME_RC=0 + markers)"
overlap_run J3 la-overlap-narrow-"$NARROW_W" "LA_OVERLAP_FORCE_W=$NARROW_W "

echo "UI_HUD_TEST: J ok — dialogue + Empathy-Book + TAB panel paint on their own rects AND band + text sit fully inside the window, at the shipped default AND the planted $NARROW_W px width (MC 11293 D1 + MC 11303 F-DA1 + MC 11891 B)"
echo "UI_HUD_TEST: SECTION-OK (MC 11243 tail-law — the tail echo owns the gate name) — C13 Hud/Dialogue/EmpathyPanel checks green (A), harness self-test red (B), story authored node exact-text capture (D), 2f skills/quest UI logic + palette guard (E), S6 UI tokens single-sourced (G), dialogue/empathy/TAB-panel/HUD boxes DISJOINT + DIALOGUE_IN_VIEW at BOTH widths with all three surfaces open on the real scene (J); pixel legs C/F/H and tracker legs I/K/L ride their battery files next"

# ---------------------------------------------------------------------------
# battery files (the MC 11891 A–L split) — the legs and their bars live there;
# this entry owns the build they ride and the final GATE PASS echo.
# ---------------------------------------------------------------------------
bash "$HERE/ui_pixel_battery.sh" "$PROJ" || fail "pixel battery (legs C, F, H) red — see UI_PIXEL_TEST lines above"
bash "$HERE/ui_tracker_battery.sh" "$PROJ" || fail "tracker battery (legs I, K, L) red — see UI_TRACKER_TEST lines above (save-touching legs: whole-gate runs ride the SAVEGATE, leg I law)"

echo "UI_HUD_TEST: GATE PASS — C13 Hud/Dialogue/EmpathyPanel checks green (A), harness self-test red (B), non-blank framebuffer render (C), story authored node exact-text capture (D), 2f skills/quest UI logic + palette guard (E), 2f framebuffer Manna digits + TAB panel pixel diff (F), S6 UI tokens single-sourced (G), S6 low-health vignette on/off at the 30 boundary + tint pixel (H), act-two quest tracker mid-arc (I), dialogue/empathy/TAB-panel/HUD bounding boxes DISJOINT with all three surfaces open on the real scene + the dialogue band DIALOGUE_IN_VIEW at the shipped default AND the planted $NARROW_W px width + fixed-layout captures (J), MC 11295 boss-kill-ordering quest-line frame bank — advanced beat + saved state shown (K), MC 11311 act-three quest tracker mid-arc — the act-three row paints once the act opens (L)"
exit 0
