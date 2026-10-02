#!/usr/bin/env bash
# roster_test.sh — MC 3943 stage 2g follower-roster pure-logic gate (code).
#
# Two-pass calibration (harness self-test red WITH it, 0 failures WITHOUT —
# dropped via /p:IncludeHarness=false, the ci/skill_test.sh idiom):
#   (1) WITH the harness self-test  -> exactly 1 failure (the broken one).
#   (2) WITHOUT it (moved aside)    -> 0 failures (the real suite is green).
# The gate passes only if BOTH runs behave as expected ("GATE PASS 0" = pass
# with 0 real failures). The suite itself pins (RosterTests.cs):
#   recruit/cap 3 (4th refused, owner D1), INDEPENDENT per-follower wages +
#   the W2 settled-list contract (what TickRoster maps onto per-follower
#   WagePaid emits), one follower betrays while the others keep following,
#   the 3x betrayal burst at the cap is deterministic (DA-c2 D9-6), UNIQUE
#   bus keys, the roster-mean-LAST emit-order contract (D6), and the F2
#   regression on the REAL QuestLog: loyalty predicates ignore "roster".
#
# Usage:
#   ./ci/roster_test.sh [project_dir]   (defaults to dir above ci/)
set -uo pipefail

HERE="$(cd "$(dirname "$0")" && pwd)"
PROJ="${1:-$(dirname "$HERE")}"
TESTS_DIR="$PROJ/tests/roster"
SELFTEST="$TESTS_DIR/RosterHarnessSelfTest.cs"
CSPROJ="$TESTS_DIR/LastAnimalRosterTests.csproj"

fail() { echo "ROSTER_TEST: GATE FAIL: $*" >&2; exit 1; }
[ -d "$TESTS_DIR" ] || fail "roster tests dir not found: $TESTS_DIR"
[ -f "$SELFTEST" ] || fail "harness self-test not found: $SELFTEST"
[ -f "$CSPROJ" ] || fail "test project not found: $CSPROJ"

echo "ROSTER_TEST: project=$PROJ"

# (1) Run WITH the harness self-test present. Expected: 1 failure.
# No --no-restore: it silently no-ops on a never-restored clean clone (MC 1344.2).
echo "ROSTER_TEST: run 1 — with harness self-test (expect 1 failure)"
LOG1="$(cd "$PROJ" && dotnet test "$CSPROJ" 2>&1)"
CODE1=$?
printf '%s\n' "$LOG1"

[ "$CODE1" -ne 0 ] || fail "run 1: expected non-zero exit (harness self-test should fail), got 0"

FAILED1=$(printf '%s\n' "$LOG1" | grep -oP 'Failed:\s+\K[0-9]+' | head -1)
PASSED1=$(printf '%s\n' "$LOG1" | grep -oP 'Passed:\s+\K[0-9]+' | head -1)
TOTAL1=$(printf '%s\n' "$LOG1" | grep -oP 'Total:\s+\K[0-9]+' | head -1)
echo "ROSTER_TEST: run 1 — Failed=$FAILED1 Passed=$PASSED1 Total=$TOTAL1"

[ "$FAILED1" = "1" ] || fail "run 1: expected exactly 1 failure, got $FAILED1"
[ "$PASSED1" != "" ] || fail "run 1: no passed count found in output"

# (2) Run WITHOUT the harness self-test. Expected: 0 failures. The self-test
# is dropped via /p:IncludeHarness=false (the ci/skill_test.sh idiom — the
# csproj compiles it conditionally).
echo "ROSTER_TEST: run 2 — without harness self-test (expect 0 failures)"
LOG2="$(cd "$PROJ" && dotnet test "$CSPROJ" /p:IncludeHarness=false 2>&1)"
CODE2=$?
printf '%s\n' "$LOG2"

[ "$CODE2" -eq 0 ] || fail "run 2: expected exit 0 (all tests green), got $CODE2"

FAILED2=$(printf '%s\n' "$LOG2" | grep -oP 'Failed:\s+\K[0-9]+' | head -1)
PASSED2=$(printf '%s\n' "$LOG2" | grep -oP 'Passed:\s+\K[0-9]+' | head -1)
TOTAL2=$(printf '%s\n' "$LOG2" | grep -oP 'Total:\s+\K[0-9]+' | head -1)
echo "ROSTER_TEST: run 2 — Failed=$FAILED2 Passed=$PASSED2 Total=$TOTAL2"

[ "$FAILED2" = "0" ] || fail "run 2: expected 0 failures, got $FAILED2"
[ "$PASSED2" != "" ] || fail "run 2: no passed count found in output"
[ "$TOTAL1" = "$((TOTAL2 + 1))" ] || fail "total mismatch: run1=$TOTAL1 run2=$TOTAL2 (expected run1 = run2 + 1)"

echo "ROSTER_TEST: GATE PASS — harness self-test went red (1 failure), real roster suite green ($PASSED2 passed, 0 failed)"
exit 0
