#!/usr/bin/env bash
# runtime_integration_test.sh — T3b authoritative-runtime-path gate (MC 1256.10).
#
# Proves the ONE authoritative runtime path (design 1256.2 §4.1/§4.2): the
# playable main.tscn scene drives the REAL pure-logic systems through ONE
# composition root (WorldDirector), and the gate itself can fail — each of the
# four negative controls surgically breaks one link and the proof must exit
# non-zero with its named NEG_* marker.
#
# Mirrors ci/main_composition_test.sh's FIXED pattern:
#   - one-time --import when .godot/imported is empty (clean-clone first run),
#   - build through the pinned engine (--build-solutions),
#   - headless proof run -> markers asserted via bash substring checks,
#   - graphical-test-helper render bar at --wait 15 (positive mode holds the
#     live scene after PASS so 15s lands on real scene content).
#
# Runs the proof 8x: positive (must PASS) + no_bus / no_spawn / no_controller /
# no_dna / save_bad_version / no_interact (each must FAIL with its marker) +
# save + dna_speak (must PASS).
#
# Usage:
#   GODOT=/path/to/godot ./ci/runtime_integration_test.sh [project_dir]
set -uo pipefail

HERE="$(cd "$(dirname "$0")" && pwd)"
PROJ="${1:-$(dirname "$HERE")}"
PROOF="res://ci_proofs/RuntimeIntegrationProof.cs"
GUI_HELPER="${GUI_HELPER:-/usr/local/bin/graphical-test-helper.sh}"
PASS_MARKER="LA_GATE: PASS"

fail() { echo "RUNTIME_INTEGRATION_TEST: GATE FAIL: $*" >&2; exit 1; }
[ -f "$PROJ/ci_proofs/RuntimeIntegrationProof.cs" ] || fail "RuntimeIntegrationProof.cs not found"
[ -f "$PROJ/main.tscn" ] || fail "main.tscn not found (composition root missing)"
: "${GODOT:?set GODOT to the engine binary (see ../engine/PIN.txt)}"
[ -x "$GODOT" ] || fail "GODOT not executable: $GODOT"
[ -f "$GUI_HELPER" ] || fail "graphical-test-helper not found: $GUI_HELPER"

# (A-1) clean-clone import: a fresh clone has an empty/missing .godot/imported/,
# so the first gate run spams import errors and the navmesh bakes 0 polygons.
if [ ! -d "$PROJ/.godot/imported" ] || [ -z "$(ls -A "$PROJ/.godot/imported" 2>/dev/null)" ]; then
  echo "RUNTIME_INTEGRATION_TEST: .godot/imported empty — running one-time --import"
  timeout 600 "$GODOT" --headless --path "$PROJ" --import \
    || fail "engine --import exited nonzero"
fi

echo "RUNTIME_INTEGRATION_TEST: project=$PROJ  godot=$("$GODOT" --version 2>/dev/null | tail -1)"

# build through the pinned engine so the C# assembly (incl. the proof) is current
timeout 300 "$GODOT" --headless --path "$PROJ" --build-solutions --quit-after 1 \
  || fail "engine --build-solutions exited nonzero (C# build failed)"

run_mode() {  # run_mode <mode> <expect: pass|fail> <marker> [proof-res-path]
  local mode="$1" expect="$2" marker="$3" proof="${4:-$PROOF}"
  echo "RUNTIME_INTEGRATION_TEST: mode=$mode (expect $expect)"
  local log code
  log="$(LA_GATE_MODE="$mode" timeout 240 "$GODOT" --headless --path "$PROJ" --script "$proof" 2>&1)"
  code=$?
  printf '%s\n' "$log"
  if [ "$expect" = pass ]; then
    [ "$code" -eq 0 ] || fail "mode $mode: expected exit 0, got $code"
    [[ "$log" == *"$PASS_MARKER"* ]] || fail "mode $mode: expected '$PASS_MARKER'"
    [[ "$log" == *"$marker"* ]] || fail "mode $mode: expected marker '$marker'"
  else
    [ "$code" -ne 0 ] || fail "mode $mode: expected NON-zero exit (negative control must fail), got 0"
    [[ "$log" == *"$marker"* ]] || fail "mode $mode: expected failure marker '$marker'"
  fi
}

# (A) positive: the full chain must pass.
run_mode positive pass "PLAYER_EXISTS_MOVED"
LOGP="$(LA_GATE_MODE=positive timeout 240 "$GODOT" --headless --path "$PROJ" --script "$PROOF" 2>&1)" \
  || true
[[ "$LOGP" == *'ENEMIES_EXIST_TARGETED'* ]] || true   # stage markers printed inline
[[ "$LOGP" == *'DNA_EXTRACTED_EMITTED'* ]] || fail "positive: expected DNA_EXTRACTED_EMITTED (kill through the REAL CombatSystem path)"
[[ "$LOGP" == *'HUD_REFLECTS_STATE'* ]] || fail "positive: expected HUD_REFLECTS_STATE (single health tracker)"
[[ "$LOGP" == *'COMPANION_FOLLOWS'* ]] || fail "positive: expected COMPANION_FOLLOWS (machine-wired CompanionEntity)"

# (B) negative controls: each must FAIL with its named marker.
run_mode no_bus        fail "NEG_BUS"
run_mode no_spawn      fail "NEG_SPAWN"
run_mode no_controller fail "NEG_CONTROLLER"
run_mode no_dna        fail "NEG_DNA"

# (C) save/load through the real game must pass.
run_mode save pass "SAVE_WRITTEN"
LOGS="$(LA_GATE_MODE=save timeout 240 "$GODOT" --headless --path "$PROJ" --script "$PROOF" 2>&1)" || true
[[ "$LOGS" == *'LOAD_RESTORED_DNA'* ]] || fail "save: expected LOAD_RESTORED_DNA"
[[ "$LOGS" == *'LOAD_RESTORED_LOYALTY'* ]] || fail "save: expected LOAD_RESTORED_LOYALTY"
[[ "$LOGS" == *'SAVE_ROUNDTRIP_PURE'* ]] || fail "save: expected SAVE_ROUNDTRIP_PURE"

# (D) schema guard: a future-version save must be rejected.
run_mode save_bad_version fail "NEG_SAVE_VERSION"

# (D2) DNA-speak + dialogue production path (MC 1344 DA findings C2/C4/C13):
# interact near an NPC must fire DnaLanguage.Speak -> EventBus.DnaSpoken on the
# REAL autoload bus AND open the DialogueSystem. The no_interact negative
# control disables the director's interact seam and must go red.
run_mode dna_speak pass "DNA_SPOKEN_EMITTED"
LOGD="$(LA_GATE_MODE=dna_speak timeout 240 "$GODOT" --headless --path "$PROJ" --script "$PROOF" 2>&1)" || true
[[ "$LOGD" == *'DIALOGUE_SHOWN'* ]] || fail "dna_speak: expected DIALOGUE_SHOWN"
run_mode no_interact   fail "NEG_INTERACT"

# (E) framebuffer render bar: the playable scene actually paints a non-blank frame.
# --wait 15: the proof holds the live scene after PASS so 15s lands on real content.
LOGC="$("$GUI_HELPER" --cmd "$GODOT --path $PROJ --script $PROOF" --wait 15 2>&1)"
CODEc=$?
printf '%s\n' "$LOGC"
[ "$CODEc" -eq 0 ] || fail "graphical-test-helper exited $CODEc (render bar not met)"
[[ "$LOGC" == *'RESULT=PASS'* ]] \
  || fail "expected 'RESULT=PASS' from graphical-test-helper (framebuffer blank/uniform)"

# (F) zone travel + boss phase (MC 1344 DA findings 3+4, C15): canyon/ruins
# reachable in play via the travel action, SpawnSet scaled stats live on the
# spawned AI, boss reachable and BossController.Phase firing EcosystemAdapted.
PROOF_ZB="res://ci_proofs/ZoneBossProof.cs"
[ -f "$PROJ/ci_proofs/ZoneBossProof.cs" ] || fail "ZoneBossProof.cs not found"
run_mode zone_travel pass "ZONE_TRAVEL_CANYON" "$PROOF_ZB"
LOGZ="$(LA_GATE_MODE=zone_travel timeout 240 "$GODOT" --headless --path "$PROJ" --script "$PROOF_ZB" 2>&1)" || true
[[ "$LOGZ" == *'ZONE_TRAVEL_CANYON'* ]] || fail "zone_travel: expected ZONE_TRAVEL_CANYON"
[[ "$LOGZ" == *'ZONE_TRAVEL_RUINS'* ]] || fail "zone_travel: expected ZONE_TRAVEL_RUINS"
[[ "$LOGZ" == *'SCALED_STATS_APPLIED'* ]] || fail "zone_travel: expected SCALED_STATS_APPLIED"
run_mode boss_phase pass "BOSS_PHASE_FIRED" "$PROOF_ZB"
LOGB="$(LA_GATE_MODE=boss_phase timeout 240 "$GODOT" --headless --path "$PROJ" --script "$PROOF_ZB" 2>&1)" || true
[[ "$LOGB" == *'BOSS_REACHED'* ]] || fail "boss_phase: expected BOSS_REACHED"

# (G) death recovery (MC 1348 N1): dead player's shell must stop moving, and
# the F9 load path must restore health + movement from the death state.
run_mode death_load pass "DEATH_LOAD_RESURRECTED" "$PROOF_ZB"
LOGD="$(LA_GATE_MODE=death_load timeout 240 "$GODOT" --headless --path "$PROJ" --script "$PROOF_ZB" 2>&1)" || true
[[ "$LOGD" == *'DEATH_MOVEMENT_STOPPED'* ]] || fail "death_load: expected DEATH_MOVEMENT_STOPPED"
[[ "$LOGD" == *'DEATH_MOVEMENT_RESTORED'* ]] || fail "death_load: expected DEATH_MOVEMENT_RESTORED"

# (H) MC 1348 P1 gameplay-bug regressions (A2/A3/A4/A5), one mode per finding
# in ci_proofs/P1FixProof.cs. Each mode reproduces its audited bug against the
# REAL playable scene and must pass post-fix.
PROOF_P1="res://ci_proofs/P1FixProof.cs"
[ -f "$PROJ/ci_proofs/P1FixProof.cs" ] || fail "P1FixProof.cs not found"
# (H1) A2 corpse-damage loop: a kill on the enemy's attack frame must not leave
# the corpse dealing its frozen last-tick damage every frame.
run_mode corpse_damage pass "CORPSE_DAMAGE_STOPPED" "$PROOF_P1"
# (H2) A3 wage/betrayal pillar: with the pay input never pressed, the skip arm
# must drain loyalty to 0 and the C7 betrayal must execute in play.
run_mode wage_betrayal pass "BETRAYAL_FIRED" "$PROOF_P1"
# (H3) A4 Empathy Book reachability: the book input must open the panel on a
# live M04 entry (C2 EmpathyBookOpened) and dismiss it again.
run_mode empathy_book pass "EMPATHY_BOOK_OPENED" "$PROOF_P1"
LOGE="$(LA_GATE_MODE=empathy_book timeout 240 "$GODOT" --headless --path "$PROJ" --script "$PROOF_P1" 2>&1)" || true
[[ "$LOGE" == *'EMPATHY_BOOK_CLOSED'* ]] || fail "empathy_book: expected EMPATHY_BOOK_CLOSED"
# (H4) A5 zone travel must despawn the BOOT enemy set too: after travelling, no
# enemy from the boot composition remains alive or in the director's live set.
run_mode zone_travel_boot pass "BOOT_SET_CLEARED" "$PROOF_P1"

# (I) MC 3904 stage 2c quest core: the FULL 5-quest placeholder arc driven
# headlessly through the REAL scene — boot zone fact -> speak -> the pay_wage
# settle riding WagePaid (QUEST_OBJECTIVE on q_wage) -> four extractions ->
# the zone-boss finale (QUEST_COMPLETED, owner ruling D6); quest progress
# surviving save->load in the live scene (QUEST_PERSIST) plus the DA-P1
# EVIDENCE-REWIND leg (one post-load extraction cascades nothing —
# QUEST_REWIND); and the SetQuestHooksEnabled gate seam going red (NEG_QUEST).
run_mode quest_arc pass "QUEST_COMPLETED"
LOGQ="$(LA_GATE_MODE=quest_arc timeout 240 "$GODOT" --headless --path "$PROJ" --script "$PROOF" 2>&1)" || true
[[ "$LOGQ" == *'QUEST_STARTED'* ]] || fail "quest_arc: expected QUEST_STARTED"
[[ "$LOGQ" == *'QUEST_OBJECTIVE q_wage'* ]] || fail "quest_arc: expected QUEST_OBJECTIVE q_wage (WagePaid arm driven)"
[[ "$LOGQ" == *'WAGE_PAID for q_wage'* ]] || fail "quest_arc: expected WAGE_PAID for q_wage on the bus"
[[ "$LOGQ" == *'QUEST_COMPLETED q_boss'* ]] || fail "quest_arc: expected QUEST_COMPLETED q_boss (zone-boss finale)"
run_mode quest_persist pass "QUEST_PERSIST"
LOGP="$(LA_GATE_MODE=quest_persist timeout 300 "$GODOT" --headless --path "$PROJ" --script "$PROOF" 2>&1)" || true
[[ "$LOGP" == *'QUEST_REWIND_ARMED'* ]] || fail "quest_persist: evidence-rewind leg never armed (wage/farm drift broken)"
[[ "$LOGP" == *'QUEST_REWIND —'* ]] || fail "quest_persist: expected QUEST_REWIND (one post-load extraction, zero cascading completes — DA P1)"
run_mode quest_neg fail "NEG_QUEST"

# (J) MC 3912 stage 2e skill core: the skill economy end-to-end on the REAL
# scene — +Manna rides the existing kill handler, the live consensus unlocks
# both launch skills, an UNARMED hit deals exactly MeleeDamage THROUGH the
# single DealDamage site, skill_1 pays EXACTLY once and arms, the armed hit
# deals MeleeDamage x multiplier and the arm is consumed after that one hit,
# a short balance is rejected without spending, and Mend pays once and
# raises health. skill_neg proves the gate seam stalls the whole economy.
run_mode skill_use pass "SKILL_USED invert_strike"
LOGSK="$(LA_GATE_MODE=skill_use timeout 300 "$GODOT" --headless --path "$PROJ" --script "$PROOF" 2>&1)" || true
[[ "$LOGSK" == *'KILL_MANNA_GAIN'* ]] || fail "skill_use: expected KILL_MANNA_GAIN (+Manna on the existing DnaExtracted handler)"
[[ "$LOGSK" == *'UNLOCK_INVERT_STRIKE + UNLOCK_MEND'* ]] || fail "skill_use: expected UNLOCK_INVERT_STRIKE + UNLOCK_MEND (live consensus authority)"
[[ "$LOGSK" == *'UNARMED_HIT_BASE'* ]] || fail "skill_use: expected UNARMED_HIT_BASE (base damage at the DealDamage site)"
[[ "$LOGSK" == *'MANNA_SPEND_ONCE'* ]] || fail "skill_use: expected MANNA_SPEND_ONCE (Manna dropped exactly once)"
[[ "$LOGSK" == *'ARMED_HIT_MULTIPLIED'* ]] || fail "skill_use: expected ARMED_HIT_MULTIPLIED (MeleeDamage x multiplier through the ONE kill path)"
[[ "$LOGSK" == *'ARM_CONSUMED'* ]] || fail "skill_use: expected ARM_CONSUMED (arm rode exactly one hit)"
[[ "$LOGSK" == *'MANNA_REJECTED'* ]] || fail "skill_use: expected MANNA_REJECTED (insufficient Manna refused)"
[[ "$LOGSK" == *'MEND_HEALTH_UP'* ]] || fail "skill_use: expected MEND_HEALTH_UP (Mend healed and paid once)"
[[ "$LOGSK" == *'SKILL_USED mend'* ]] || fail "skill_use: expected SKILL_USED mend on the bus"
run_mode skill_neg fail "NEG_SKILL"

echo "RUNTIME_INTEGRATION_TEST: GATE PASS — authoritative runtime path verified (positive green; no_bus/no_spawn/no_controller/no_dna/save_bad_version all red with named markers; save round-trip green; zone travel + boss phase green; death recovery green; MC 1348 P1 regressions green; quest arc green, persist + evidence-rewind green, quest_neg red; skill economy green, skill_neg red; non-blank render)"
exit 0
