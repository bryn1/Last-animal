#!/usr/bin/env bash
# skill_test.sh — MC 3912 stage 2e skill-core pure-logic gate (code, 2026-10-02).
#
# The stage-2e DoD (inc2 PLAN §B, DA-c2 D3/D4): the unlock authority
# (PlayerMutations) is a PURE function of the per-position consensus
# Counters ONLY (never ObservedCount/Coverage — the §G D2 Option B remedy),
# the Manna economy (SkillState over the real PlayerController) pays per use
# EXACTLY once, arms a multiplier consumed by exactly one hit, refuses on a
# short balance, gains per kill and clamps at the cap — and the unlock set
# survives a save/load round-trip EQUAL (the §G N2 guard) through the REAL
# v3 SaveSystem schema — as pure C# under `dotnet test` (I3).
#
# Runs the suite TWICE (exact ci/quest_test.sh two-pass shape):
#   (1) WITH the harness self-test -> exactly 1 failure (deliberately broken),
#       all other tests green.
#   (2) WITHOUT the harness self-test -> 0 failures.
# Then a NAMED-F-ACT leg: every DoD F-act must be present in the compiled
# test list by name (F1..F6 + the N2 round-trip guard) — a renamed/deleted
# F-act fails the gate, not silently shrinks it.
# Gate passes only if ALL behave as expected. The ObservedCount-independence
# test is the planted-bad trap for an authority re-reading ObservedCount;
# the multiplier arithmetic is pinned headless here and end-to-end by the
# runtime skill_use mode (planted-bad: drop the wrap, ARMED_HIT_MULTIPLIED
# goes RED there).
#
# Usage:
#   ./ci/skill_test.sh [project_dir]   (defaults to dir above ci/)
set -uo pipefail

HERE="$(cd "$(dirname "$0")" && pwd)"
PROJ="${1:-$(dirname "$HERE")}"
TESTS_DIR="$PROJ/tests/skill"
SELFTEST="$TESTS_DIR/SkillHarnessSelfTest.cs"

fail() { echo "SKILL_TEST: GATE FAIL: $*" >&2; exit 1; }
[ -d "$TESTS_DIR" ] || fail "tests dir not found: $TESTS_DIR"
[ -f "$SELFTEST" ] || fail "harness self-test not found: $SELFTEST"

CSPROJ="$TESTS_DIR/LastAnimalSkillTests.csproj"
[ -f "$CSPROJ" ] || fail "test project not found: $CSPROJ"

echo "SKILL_TEST: project=$PROJ"

# (1) WITH the harness self-test -> expect exactly 1 failure.
echo "SKILL_TEST: run 1 — with harness self-test (expect 1 failure)"
LOG1="$(cd "$PROJ" && dotnet test "$CSPROJ" 2>&1)"
CODE1=$?
printf '%s\n' "$LOG1"

[ "$CODE1" -ne 0 ] || fail "run 1: expected non-zero exit (harness self-test must fail), got 0"

FAILED1=$(printf '%s\n' "$LOG1" | grep -oP 'Failed:\s+\K[0-9]+' | head -1)
PASSED1=$(printf '%s\n' "$LOG1" | grep -oP 'Passed:\s+\K[0-9]+' | head -1)
TOTAL1=$(printf '%s\n' "$LOG1" | grep -oP 'Total:\s+\K[0-9]+' | head -1)
echo "SKILL_TEST: run 1 — Failed=$FAILED1 Passed=$PASSED1 Total=$TOTAL1"

[ "$FAILED1" = "1" ] || fail "run 1: expected exactly 1 failure, got $FAILED1"
[ "$PASSED1" != "" ] || fail "run 1: no passed count found in output"

# (2) WITHOUT the harness self-test -> expect 0 failures.
echo "SKILL_TEST: run 2 — without harness self-test (expect 0 failures)"
LOG2="$(cd "$PROJ" && dotnet test "$CSPROJ" -p:IncludeHarness=false 2>&1)"
CODE2=$?
printf '%s\n' "$LOG2"

[ "$CODE2" -eq 0 ] || fail "run 2: expected exit 0 (all tests green), got $CODE2"

FAILED2=$(printf '%s\n' "$LOG2" | grep -oP 'Failed:\s+\K[0-9]+' | head -1)
PASSED2=$(printf '%s\n' "$LOG2" | grep -oP 'Passed:\s+\K[0-9]+' | head -1)
TOTAL2=$(printf '%s\n' "$LOG2" | grep -oP 'Total:\s+\K[0-9]+' | head -1)
echo "SKILL_TEST: run 2 — Failed=$FAILED2 Passed=$PASSED2 Total=$TOTAL2"

[ "$FAILED2" = "0" ] || fail "run 2: expected 0 failures, got $FAILED2"
[ "$PASSED2" != "" ] || fail "run 2: no passed count found in output"

# The total must differ by exactly the 1 harness self-test.
[ "$TOTAL1" = "$((TOTAL2 + 1))" ] || fail "total mismatch: run1=$TOTAL1 run2=$TOTAL2 (expected run1 = run2 + 1)"

# (3) NAMED-F-ACT leg: the DoD F-acts must exist BY NAME in the compiled
# suite (plan §B 2e / §G N2 + task MC 3912 DoD 1) — a renamed or deleted
# F-act must fail this gate, never silently shrink it.
echo "SKILL_TEST: run 3 — named F-act presence"
LIST="$(cd "$PROJ" && dotnet test "$CSPROJ" -p:IncludeHarness=false --list-tests 2>&1)"
for ACT in \
  F1_pay_drops_Manna_exactly_once_per_use \
  F2_kill_gains_Manna \
  F3_armed_hit_damage_is_MeleeDamage_times_multiplier \
  F4_unarmed_hit_damage_is_base_MeleeDamage \
  F5_arm_flag_is_consumed_after_one_hit \
  F6_insufficient_Manna_is_rejected_without_spending \
  F7_calm_cost_spend_once \
  F8_calm_refuse_no_spend \
  F9_calm_unlock_positions \
  N2_unlock_from_consensus_after_roundtrip_equals_unlock_live \
  F10_passive_derivation_table_pinned \
  F11_resonant_draw_adds_bonus_at_kill_seam \
  F12_deep_mend_adds_bonus_at_heal_seam \
  F13_band_threshold_boundaries_exact \
  F14_live_extraction_reaches_both_bands_census_confirmed \
  N3_passives_load_shape_equals_live_at_same_width
do
  printf '%s\n' "$LIST" | grep -q "$ACT" || fail "named F-act missing from the suite: $ACT"
done
echo "SKILL_TEST: named F-acts present (F1..F6 + F7..F9 calm rows (MC 10031) + N2 round-trip guard + F10..F14/N3 resonance-passive rows (MC 10200 S16))"

# (4) S16 save-absence row (MC 10200, S9 trait-absence idiom REUSED): the
# passives are RUNTIME-ONLY derivations — no save field may ever carry them.
# The whole-tree DoD row is `grep -c Passive src/save/` == 0; this leg is the
# strictly stronger case-INSENSITIVE count over the directory (a save field
# named for a passive would put the §G D2 round-trip claim on a NEW schema
# member — that delta is what the row forbids).
SAVE_HITS="$(grep -ric "passive" "$PROJ/src/save/" | awk -F: '{s+=$NF} END {print s+0}')"
[ "$SAVE_HITS" = "0" ] || fail "S16 save-absence: $SAVE_HITS 'passive' match(es) under src/save/ — passives must stay runtime-only (zero save delta)"
echo "SKILL_TEST: S16 save-absence row — zero 'passive' matches under src/save/ (runtime-only, S9 idiom)"

echo "SKILL_TEST: GATE PASS — harness self-test went red (1 failure), real suite green ($PASSED2 passed, 0 failed), named F-acts present"
exit 0
