#!/usr/bin/env bash
# quest_test.sh — MC 3904 stage 2c quest-core pure-logic gate (code, 2026-10-02).
#
# The stage-2c DoD (inc2 PLAN §B): the pure QuestTable/QuestLog pair carries
# the 5-quest placeholder arc (ids are the 2c/2d contract, owner ruling D6:
# ends at a zone boss), the state machine holds its chain and DENIES every
# illegal transition by name, the F2 roster-key exclusion and the single
# wage-riding path hold, and the QuestStates "id:status" wire round-trips
# through the REAL SaveSystem schema — as pure C# under `dotnet test` (I3).
#
# Runs the suite TWICE (exact ci/save_test.sh two-pass shape):
#   (1) WITH the harness self-test -> exactly 1 failure (deliberately broken),
#       all other tests green.
#   (2) WITHOUT the harness self-test -> 0 failures.
# Gate passes only if BOTH behave as expected. The bus signal-count leg here
# (QuestBusContractTests) is the planted-bad trap for a SIXTH signal on the
# bus; the guard tests are the trap for a deleted transition guard.
#
# Usage:
#   ./ci/quest_test.sh [project_dir]   (defaults to dir above ci/)
set -uo pipefail

HERE="$(cd "$(dirname "$0")" && pwd)"
PROJ="${1:-$(dirname "$HERE")}"
TESTS_DIR="$PROJ/tests/quest"
SELFTEST="$TESTS_DIR/QuestHarnessSelfTest.cs"

fail() { echo "QUEST_TEST: GATE FAIL: $*" >&2; exit 1; }
[ -d "$TESTS_DIR" ] || fail "tests dir not found: $TESTS_DIR"
[ -f "$SELFTEST" ] || fail "harness self-test not found: $SELFTEST"

CSPROJ="$TESTS_DIR/LastAnimalQuestTests.csproj"
[ -f "$CSPROJ" ] || fail "test project not found: $CSPROJ"

echo "QUEST_TEST: project=$PROJ"

# (1) WITH the harness self-test -> expect exactly 1 failure.
echo "QUEST_TEST: run 1 — with harness self-test (expect 1 failure)"
LOG1="$(cd "$PROJ" && dotnet test "$CSPROJ" 2>&1)"
CODE1=$?
printf '%s\n' "$LOG1"

[ "$CODE1" -ne 0 ] || fail "run 1: expected non-zero exit (harness self-test must fail), got 0"

FAILED1=$(printf '%s\n' "$LOG1" | grep -oP 'Failed:\s+\K[0-9]+' | head -1)
PASSED1=$(printf '%s\n' "$LOG1" | grep -oP 'Passed:\s+\K[0-9]+' | head -1)
TOTAL1=$(printf '%s\n' "$LOG1" | grep -oP 'Total:\s+\K[0-9]+' | head -1)
echo "QUEST_TEST: run 1 — Failed=$FAILED1 Passed=$PASSED1 Total=$TOTAL1"

[ "$FAILED1" = "1" ] || fail "run 1: expected exactly 1 failure, got $FAILED1"
[ "$PASSED1" != "" ] || fail "run 1: no passed count found in output"

# (2) WITHOUT the harness self-test -> expect 0 failures.
echo "QUEST_TEST: run 2 — without harness self-test (expect 0 failures)"
LOG2="$(cd "$PROJ" && dotnet test "$CSPROJ" -p:IncludeHarness=false 2>&1)"
CODE2=$?
printf '%s\n' "$LOG2"

[ "$CODE2" -eq 0 ] || fail "run 2: expected exit 0 (all tests green), got $CODE2"

FAILED2=$(printf '%s\n' "$LOG2" | grep -oP 'Failed:\s+\K[0-9]+' | head -1)
PASSED2=$(printf '%s\n' "$LOG2" | grep -oP 'Passed:\s+\K[0-9]+' | head -1)
TOTAL2=$(printf '%s\n' "$LOG2" | grep -oP 'Total:\s+\K[0-9]+' | head -1)
echo "QUEST_TEST: run 2 — Failed=$FAILED2 Passed=$PASSED2 Total=$TOTAL2"

[ "$FAILED2" = "0" ] || fail "run 2: expected 0 failures, got $FAILED2"
[ "$PASSED2" != "" ] || fail "run 2: no passed count found in output"

# The total must differ by exactly the 1 harness self-test.
[ "$TOTAL1" = "$((TOTAL2 + 1))" ] || fail "total mismatch: run1=$TOTAL1 run2=$TOTAL2 (expected run1 = run2 + 1)"

echo "QUEST_TEST: GATE PASS — harness self-test went red (1 failure), real suite green ($PASSED2 passed, 0 failed)"
exit 0
