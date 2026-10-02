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
# The gate passes only if A, B, C and D all behave as expected.
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
PASS_MARKER="M10_UI_RENDER_TEST: PASS"
STORY_CAPTURE_MARKER="LA_STORY_CAPTURE: PASS"

fail() { echo "UI_HUD_TEST: GATE FAIL: $*" >&2; exit 1; }
[ -d "$TESTS_UI" ] || fail "ui tests dir not found: $TESTS_UI"
[ -f "$TESTS_UI/UiRenderTest.cs" ] || fail "UiRenderTest.cs not found"
[ -f "$TESTS_UI/UiHarnessSelfTest.cs" ] || fail "UiHarnessSelfTest.cs not found"
[ -f "$PROJ/tests/story/StoryTextCaptureTest.cs" ] || fail "StoryTextCaptureTest.cs not found"
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

echo "UI_HUD_TEST: GATE PASS — C13 Hud/Dialogue/EmpathyPanel checks green (A), harness self-test red (B), non-blank framebuffer render (C), story authored node exact-text capture (D)"
exit 0
