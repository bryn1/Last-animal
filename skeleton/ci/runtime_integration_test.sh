#!/usr/bin/env bash
# runtime_integration_test.sh — T3b authoritative-runtime-path gate (MC 1256.10;
# grown through MC 1344/1348/3904/3910/3912/3915/3943/10026.1/10031 into the
# full battery below).
#
# Proves the ONE authoritative runtime path (design 1256.2 §4.1/§4.2): the
# playable main.tscn scene drives the REAL pure-logic systems through ONE
# composition root (WorldDirector), and the gate itself can fail — each
# negative control below surgically breaks one link and the proof must exit
# non-zero with its named NEG_* marker.
#
# Mirrors ci/main_composition_test.sh's FIXED pattern:
#   - one-time --import when .godot/imported is empty (clean-clone first run),
#   - build through the pinned engine (--build-solutions),
#   - headless proof run -> markers asserted via bash substring checks,
#   - graphical-test-helper render bar at --wait 15 (positive-mode legs hold
#     the live scene after PASS so 15s lands on real scene content).
#
# Battery at this HEAD: 29 run_mode legs = 19 positive modes + 10 negative
# controls (no_bus, no_spawn, no_controller, no_dna, save_bad_version,
# no_interact, quest_neg, skill_neg, calm_neg, roster_neg), over four proof
# classes: RuntimeIntegrationProof.cs (positive/save/dna_speak/quest/story/
# skill/calm/juice/dissolve legs), ZoneBossProof.cs (zone_travel/boss_phase/death_load),
# P1FixProof.cs (corpse_damage/wage_betrayal/empathy_book/zone_travel_boot)
# and RosterIntegrationProof.cs (roster_follow/roster_neg). Most positive legs
# re-run their proof a second time to grep extra inline markers, so total
# proof invocations exceed the 29-leg count; the (E) framebuffer render bar
# runs once more through graphical-test-helper.
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
[[ "$LOGP" == *'ENEMIES_EXIST_TARGETED'* ]] || fail "positive: expected live-enemy-aimed marker (MC 10058 — a live director-set enemy AIMED at the live player position, asserted in-proof)"
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
# MC 3910 (DA W2): the leg also greps DEATH_SAVE_OWNED — the mode deletes the
# ONE shared user://savegame.json before its own write and stamps health, so
# the resurrection can never pass off a stale save from a sibling mode.
run_mode death_load pass "DEATH_LOAD_RESURRECTED" "$PROOF_ZB"
LOGD="$(LA_GATE_MODE=death_load timeout 240 "$GODOT" --headless --path "$PROJ" --script "$PROOF_ZB" 2>&1)" || true
[[ "$LOGD" == *'DEATH_MOVEMENT_STOPPED'* ]] || fail "death_load: expected DEATH_MOVEMENT_STOPPED"
[[ "$LOGD" == *'DEATH_MOVEMENT_RESTORED'* ]] || fail "death_load: expected DEATH_MOVEMENT_RESTORED"
[[ "$LOGD" == *'DEATH_SAVE_OWNED'* ]] || fail "death_load: expected DEATH_SAVE_OWNED (phase must own its save — delete-then-write, health stamped; MC 3910)"

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
# MC 3915 adds the reward-beat legs to quest_arc: every Completed transition
# shows the row's Reward node through the dialogue view (REWARD_SHOWN marker
# x5, view state asserted in-proof) and the named REWARD_GUARD leg proves a
# reward-shown node never satisfies a DialogueShown objective while a normal
# show of the same node does. MC 10026.1 adds the dialogue-lifecycle legs: a
# direct reply is never starved by a queued beat (DLQ_SPEAK_PRECEDENCE), a
# same-tick pair drains in strict emission order (DLQ_DRAINED), the queue
# draining closes the box via the one uniform auto-close (DLQ_CLOSED) and an
# unauthored direct npc_ line auto-closes too (DLQ_NPC_CLOSED — "All dialogue
# lines"; R5 itself stays UNCOVERED by these legs — DA wave ruling P3-α).
run_mode quest_arc pass "QUEST_COMPLETED"
LOGQ="$(LA_GATE_MODE=quest_arc timeout 240 "$GODOT" --headless --path "$PROJ" --script "$PROOF" 2>&1)" || true
[[ "$LOGQ" == *'QUEST_STARTED'* ]] || fail "quest_arc: expected QUEST_STARTED"
[[ "$LOGQ" == *'QUEST_OBJECTIVE q_wage'* ]] || fail "quest_arc: expected QUEST_OBJECTIVE q_wage (WagePaid arm driven)"
[[ "$LOGQ" == *'WAGE_PAID for q_wage'* ]] || fail "quest_arc: expected WAGE_PAID for q_wage on the bus"
[[ "$LOGQ" == *'QUEST_COMPLETED q_boss'* ]] || fail "quest_arc: expected QUEST_COMPLETED q_boss (zone-boss finale)"
for q in q_intro q_speak q_wage q_kills q_boss; do
  [[ "$LOGQ" == *"REWARD_SHOWN $q "* ]] || fail "quest_arc: expected REWARD_SHOWN $q (reward beat emitter, MC 3915)"
done
[[ "$LOGQ" == *'REWARD_GUARD'* ]] || fail "quest_arc: expected REWARD_GUARD (reward beats never feed DialogueShown — MC 3915)"
for m in DLQ_SPEAK_PRECEDENCE DLQ_DRAINED DLQ_CLOSED DLQ_NPC_CLOSED; do
  [[ "$LOGQ" == *"$m"* ]] || fail "quest_arc: expected $m (dialogue lifecycle leg — MC 10026.1)"
done
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

# (J2) MC 10031 Calming Speak: the skill→recruitment bridge on the REAL
# scene — a cast spends 12 AFTER a target scan (refusals spend ZERO), opens
# the SAME RecruitOffered flag with a 600-frame window, the pay-first wage
# path stays the SOLE join authority (pay-in-window joins, expiry joins
# nobody), E's standing offer is never downgraded and outlives the window,
# and the load-restore seam kills every window with its body (no save-scum
# economy rollback). calm_neg proves the gate seam leaks nothing.
run_mode calm_use pass "CALM_CAST"
LOGCLM="$(LA_GATE_MODE=calm_use timeout 300 "$GODOT" --headless --path "$PROJ" --script "$PROOF" 2>&1)" || true
[[ "$LOGCLM" == *'UNLOCK_CALMING_SPEAK'* ]] || fail "calm_use: expected UNLOCK_CALMING_SPEAK (live third-rule authority)"
[[ "$LOGCLM" == *'CALM_PAY_IN_WINDOW'* ]] || fail "calm_use: expected CALM_PAY_IN_WINDOW (the unchanged wage path joined)"
[[ "$LOGCLM" == *'CALM_REFUSE_SHORT'* ]] || fail "calm_use: expected CALM_REFUSE_SHORT (short balance spent nothing)"
[[ "$LOGCLM" == *'CALM_REFUSE_STANDING'* ]] || fail "calm_use: expected CALM_REFUSE_STANDING (E's standing offer refused, zero spend)"
[[ "$LOGCLM" == *'CALM_E_STANDS'* ]] || fail "calm_use: expected CALM_E_STANDS (window retired under E, offer outlives it)"
[[ "$LOGCLM" == *'CALM_WINDOW_EXPIRES'* ]] || fail "calm_use: expected CALM_WINDOW_EXPIRES (expiry withdrew the offer, pay joined nobody)"
[[ "$LOGCLM" == *'CALM_LOAD_CLEARED'* ]] || fail "calm_use: expected CALM_LOAD_CLEARED (real load_game press cleared the window, kept the standing offer)"
run_mode calm_neg fail "NEG_CALM"

# (K) MC 3943 stage 2g follower roster (ci_proofs/RosterIntegrationProof.cs):
# recruit WILD creatures in play (interact-offer + FIRST wage via the existing
# pay_wage), followers follow, INDEPENDENT per-follower wages each emitting
# their own WagePaid (ARCH W2), save->load restores N=3 (LOAD_RESTORED
# extended), cycle_follower + Forgive + break_bond arms drive the Empathy Book
# (one follower betrays, the others keep following), and the hearts MEAN rides
# the LAST LoyaltyChanged of the frame under the reserved "roster" key — the
# emit-order contract is asserted on the real wire order. roster_neg proves
# the cap: the 4th recruit is REFUSED (owner ruling D1) — red with NEG_ROSTER.
PROOF_ROSTER="res://ci_proofs/RosterIntegrationProof.cs"
[ -f "$PROJ/ci_proofs/RosterIntegrationProof.cs" ] || fail "RosterIntegrationProof.cs not found"
run_mode roster_follow pass "ROSTER_RECRUITED" "$PROOF_ROSTER"
LOGR="$(LA_GATE_MODE=roster_follow timeout 300 "$GODOT" --headless --path "$PROJ" --script "$PROOF_ROSTER" 2>&1)" || true
[[ "$LOGR" == *'ROSTER_TWO_RECRUITED'* ]] || fail "roster_follow: expected ROSTER_TWO_RECRUITED"
[[ "$LOGR" == *'FOLLOWERS_FOLLOW'* ]] || fail "roster_follow: expected FOLLOWERS_FOLLOW (two recruited bodies trail the player)"
[[ "$LOGR" == *'WAGE_INDEPENDENT_A'* ]] || fail "roster_follow: expected WAGE_INDEPENDENT_A (only the DUE follower settled)"
[[ "$LOGR" == *'WAGE_INDEPENDENT_B'* ]] || fail "roster_follow: expected WAGE_INDEPENDENT_B (non-boot follower settled its OWN wage + WagePaid — singleton read would mute it)"
[[ "$LOGR" == *'ROSTER_RESTORED'* ]] || fail "roster_follow: expected ROSTER_RESTORED (load restores N=3)"
[[ "$LOGR" == *'CYCLE_SELECTED'* ]] || fail "roster_follow: expected CYCLE_SELECTED (cycle_follower action)"
[[ "$LOGR" == *'FORGIVE_APPLIED'* ]] || fail "roster_follow: expected FORGIVE_APPLIED (book-open pay_wage = Forgive, not a wage)"
[[ "$LOGR" == *'BREAK_BOND_SELECTED'* ]] || fail "roster_follow: expected BREAK_BOND_SELECTED (one betrays, others keep following)"
[[ "$LOGR" == *'PAY_AFTER_BREAK_REFUSED'* ]] || fail "roster_follow: expected PAY_AFTER_BREAK_REFUSED (pay press on a broken bond must never settle — DA W5 F1)"
[[ "$LOGR" == *'HEARTS_MEAN_LAST'* ]] || fail "roster_follow: expected HEARTS_MEAN_LAST (mean emitted LAST under the reserved key)"
[[ "$LOGR" == *'ROSTER_OVERSIZED_TRIMMED'* ]] || fail "roster_follow: expected ROSTER_OVERSIZED_TRIMMED (over-cap restore trims: N=3, bodies=N, DROPPED marker)"
[[ "$LOGR" == *'restored entry id'* && "$LOGR" == *'DROPPED — roster cap'* ]] || fail "roster_follow: expected the F4 DROPPED marker on the over-cap restore (world-side drop-arm silent)"
run_mode roster_neg fail "NEG_ROSTER" "$PROOF_ROSTER"

# (L) MC 10120 Inc-3 S1 juice hit-flash: the white flash + VISUAL-NODE offset
# punch on the live scene — active on the hit frame, held at +4f, EXACT base
# return by +6f, and the enemy BODY GlobalPosition UNCHANGED across the whole
# window (the target's physics process is off, so juice is the only candidate
# mover — a punch applied to the body goes RED). Real attack wire, stage 95.
run_mode JUICE_HITFLASH pass "JUICE_BODY_STILL"

echo "RUNTIME_INTEGRATION_TEST: GATE PASS — authoritative runtime path verified (positive green; no_bus/no_spawn/no_controller/no_dna/save_bad_version all red with named markers; save round-trip green; zone travel + boss phase green; death recovery green; MC 1348 P1 regressions green; quest arc green, persist + evidence-rewind green, reward beats + guard green, quest_neg red; skill economy green, skill_neg red; roster follow/save-load/book arms + mean-last emit order green, pay-after-break refused green, oversized-save trim green, roster_neg cap red; juice hit-flash green; non-blank render)"
# (M) MC 10121 Inc-3 S2 juice camera shake: FollowCamera SUBSCRIBES S0's
# PlayerHurt/BossFallen on the live scene — the window is armed by a real
# TakeDamage-driven emit, HELD at +4f (camera off its follow base), and the
# camera sits back on its EXACT shake-free follow base by +12f (integer
# decay, F4 — the planted delta-time decay goes RED). BossFallen arm reacts
# too. Real bus wire, stage 96; the camera itself prints NOTHING (F4-CMP).
run_mode JUICE_SHAKE pass "JUICE_SHAKE_AT_BASE"

echo "RUNTIME_INTEGRATION_TEST: GATE PASS — authoritative runtime path verified (positive green; no_bus/no_spawn/no_controller/no_dna/save_bad_version all red with named markers; save round-trip green; zone travel + boss phase green; death recovery green; MC 1348 P1 regressions green; quest arc green, persist + evidence-rewind green, reward beats + guard green, quest_neg red; skill economy green, skill_neg red; roster follow/save-load/book arms + mean-last emit order green, pay-after-break refused green, oversized-save trim green, roster_neg cap red; juice shake green; non-blank render)"
# (N) MC 10129 Inc-3 S3 death dissolve: a real-wire kill pins the death-tick
# suppression — body collision mask 0 (the plan-named collision-off line; the
# engine's layer-number API is 1-based, the 0-based literal is a silent no-op —
# MC 10129 probe, evidence dir) while the director's EXISTING IsDead targeting
# predicate (WorldDirector.cs:375, reused not re-authored) keeps the corpse out
# of every targeting/count print — and the visual dissolves on a 20-frame
# INTEGER counter (F4), freeing itself at f+21 (visual despawn). Through
# f+1..+20 forced attacks deal 0: input presses never land (zero DamageDealt
# events name the corpse id) AND a direct forced DealDamage(9999) returns
# false with health pinned at 0 (the ghost-body evidence leg, DA P2-4; the
# planted skip-collision-off goes RED at the mask read). Stage 97, real spawn
# set, no save touched.
run_mode DISSOLVE_SUPPRESS pass "DISSOLVE_GONE"



# (O) MC 10132 Inc-3 S10 story ACT TWO: the +4 ruins-deep QuestTable.RuinsArc()
# rows play on the live scene through the SHIPPED pure QuestLog machine — the
# act opens off a REAL save->load edge (the restore sync) with its table-driven
# open card painted guarded (ACT2_CARD_ON_SCREEN); each of the four completions
# rides its OWN distinct drive (W6 pin: zone-enter / two fresh speaks / the
# single wage path resolving the act-two row / six fresh extractions — four
# separate QUEST_COMPLETED q_r_* greps, never one cascade); the act-two rows
# round-trip save->load on the SHIPPED v3 QuestStates wire (ACT2_PERSIST, zero
# save-file schema delta — data rides existing rows); and the close card drains
# in emission order behind the finale beat to the uniform auto-close (DLQ
# untouched, F3). Stage 55, proof partial RuntimeIntegrationProof.Story2.cs.
run_mode quest_arc2 pass "ACT2_CLOSED"
LOGA2="$(LA_GATE_MODE=quest_arc2 timeout 300 "$GODOT" --headless --path "$PROJ" --script "$PROOF" 2>&1)" || true
[[ "$LOGA2" == *'ACT2_OPENED'* ]] || fail "quest_arc2: expected ACT2_OPENED (restore sync opened the act)"
[[ "$LOGA2" == *'ACT2_CARD_ON_SCREEN'* ]] || fail "quest_arc2: expected ACT2_CARD_ON_SCREEN (table-driven open card, guarded beat)"
[[ "$LOGA2" == *'ACT_CARD act_two open'* ]] || fail "quest_arc2: expected the open ACT_CARD marker (REWARD_SHOWN idiom)"
for q in q_r_descent q_r_tongue q_r_bread q_r_bones; do
  [[ "$LOGA2" == *"QUEST_COMPLETED $q"* ]] || fail "quest_arc2: expected QUEST_COMPLETED $q (W6: its own observation, its own completion)"
done
for q in q_r_descent q_r_tongue q_r_bread q_r_bones; do
  [[ "$LOGA2" == *"REWARD_SHOWN $q "* ]] || fail "quest_arc2: expected REWARD_SHOWN $q (reward beat through the shipped DLQ path)"
done
[[ "$LOGA2" == *'WAGE_PAID for q_r_bread'* ]] || fail "quest_arc2: expected WAGE_PAID for q_r_bread (the ONE wage path resolved the act-two row)"
[[ "$LOGA2" == *'ACT2_PERSIST'* ]] || fail "quest_arc2: expected ACT2_PERSIST (act-two rows round-tripped on the shipped QuestStates wire)"
[[ "$LOGA2" == *'ACT2_CLOSED'* ]] || fail "quest_arc2: expected ACT2_CLOSED (close card drained + uniform auto-close)"
[[ "$LOGA2" == *'ACT_CARD act_two close'* ]] || fail "quest_arc2: expected the close ACT_CARD marker"

# (P) MC 10198 Inc-4 S14 CHARACTER LIFE: the integer-phase motion driver on
# the LIVE scene — a chasing goblin's visual bobs off base off the read-only
# SimVelocity feed (CHAR_ENEMY_WALK); the player's walk rides real WASD input
# on a probed-clear direction, its phase pinned to a velocity-fed INTEGER
# tick-counter by a two-point exact-delta check (a delta-time phase lands RED;
# CHAR_WALK_ACTIVE); release returns the rig to its EXACT captured base with
# phase 0 (CHAR_WALK_AT_REST — no float drift); the attack lean arms on the
# hit frame at the ONE DealDamage hunk and decays to exact base on an integer
# 6f counter (CHAR_LEAN_ACTIVE / CHAR_LEAN_AT_BASE); and the class-swapped
# player VisualJuice root flashes WHITE at the real player-damage site while
# the BODY position stays bit-unchanged across the juice window with physics
# frozen — a motion-on-body plant lands here RED (CHAR_PLAYER_FLASH /
# CHAR_BODY_STILL). Stage 98, proof partial RuntimeIntegrationProof.Motion.cs.
run_mode CHAR_MOTION pass "CHAR_BODY_STILL"
LOGP="$(LA_GATE_MODE=CHAR_MOTION timeout 300 "$GODOT" --headless --path "$PROJ" --script "$PROOF" 2>&1)" || true
for m in CHAR_ENEMY_WALK CHAR_WALK_ACTIVE CHAR_WALK_AT_REST CHAR_LEAN_ACTIVE CHAR_LEAN_AT_BASE CHAR_PLAYER_FLASH; do
  [[ "$LOGP" == *"$m"* ]] || fail "CHAR_MOTION: expected $m"
done

echo "RUNTIME_INTEGRATION_TEST: GATE PASS — authoritative runtime path verified (positive green; no_bus/no_spawn/no_controller/no_dna/save_bad_version all red with named markers; save round-trip green; zone travel + boss phase green; death recovery green; MC 1348 P1 regressions green; quest arc green, persist + evidence-rewind green, reward beats + guard green, quest_neg red; skill economy green, skill_neg red; roster follow/save-load/book arms + mean-last emit order green, pay-after-break refused green, oversized-save trim green, roster_neg cap red; dissolve suppress green; story act-two arc green; S14 character life green — integer-phase walk feed, exact rest return, lean at the hit hunk, player flash at the damage site, body untouched; non-blank render)"
exit 0
