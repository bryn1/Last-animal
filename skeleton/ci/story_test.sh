#!/usr/bin/env bash
# story_test.sh — MC 3900 stage 2a story-data-layer pure-logic gate (code, 2026-10-02).
#
# The stage-2a DoD (inc2 PLAN §B): the authored DialogueTable (src/story/)
# carries the migrated node texts verbatim + the new arc seeds, and its
# lookup/condition/integrity contract holds — as pure C# under `dotnet test`
# (I3), no engine.
#
# Runs the suite TWICE (exact ci/save_test.sh two-pass shape):
#   (1) WITH the harness self-test -> exactly 1 failure (deliberately broken),
#       all other tests green.
#   (2) WITHOUT the harness self-test -> 0 failures.
# Gate passes only if BOTH behave as expected. Removing an authored node from
# DialogueTable.Default() turns run (2) — and run (1)'s failure count — RED,
# naming the node.
#
# Usage:
#   ./ci/story_test.sh [project_dir]   (defaults to dir above ci/)
set -uo pipefail

HERE="$(cd "$(dirname "$0")" && pwd)"
PROJ="${1:-$(dirname "$HERE")}"
TESTS_DIR="$PROJ/tests/story"
SELFTEST="$TESTS_DIR/StoryHarnessSelfTest.cs"

fail() { echo "STORY_TEST: GATE FAIL: $*" >&2; exit 1; }
[ -d "$TESTS_DIR" ] || fail "tests dir not found: $TESTS_DIR"
[ -f "$SELFTEST" ] || fail "harness self-test not found: $SELFTEST"

CSPROJ="$TESTS_DIR/LastAnimalStoryTests.csproj"
[ -f "$CSPROJ" ] || fail "test project not found: $CSPROJ"

echo "STORY_TEST: project=$PROJ"

# (1) WITH the harness self-test -> expect exactly 1 failure.
echo "STORY_TEST: run 1 — with harness self-test (expect 1 failure)"
LOG1="$(cd "$PROJ" && dotnet test "$CSPROJ" 2>&1)"
CODE1=$?
printf '%s\n' "$LOG1"

[ "$CODE1" -ne 0 ] || fail "run 1: expected non-zero exit (harness self-test must fail), got 0"

FAILED1=$(printf '%s\n' "$LOG1" | grep -oP 'Failed:\s+\K[0-9]+' | head -1)
PASSED1=$(printf '%s\n' "$LOG1" | grep -oP 'Passed:\s+\K[0-9]+' | head -1)
TOTAL1=$(printf '%s\n' "$LOG1" | grep -oP 'Total:\s+\K[0-9]+' | head -1)
echo "STORY_TEST: run 1 — Failed=$FAILED1 Passed=$PASSED1 Total=$TOTAL1"

[ "$FAILED1" = "1" ] || fail "run 1: expected exactly 1 failure, got $FAILED1"
[ "$PASSED1" != "" ] || fail "run 1: no passed count found in output"

# (2) WITHOUT the harness self-test -> expect 0 failures.
echo "STORY_TEST: run 2 — without harness self-test (expect 0 failures)"
LOG2="$(cd "$PROJ" && dotnet test "$CSPROJ" -p:IncludeHarness=false 2>&1)"
CODE2=$?
printf '%s\n' "$LOG2"

[ "$CODE2" -eq 0 ] || fail "run 2: expected exit 0 (all tests green), got $CODE2"

FAILED2=$(printf '%s\n' "$LOG2" | grep -oP 'Failed:\s+\K[0-9]+' | head -1)
PASSED2=$(printf '%s\n' "$LOG2" | grep -oP 'Passed:\s+\K[0-9]+' | head -1)
TOTAL2=$(printf '%s\n' "$LOG2" | grep -oP 'Total:\s+\K[0-9]+' | head -1)
echo "STORY_TEST: run 2 — Failed=$FAILED2 Passed=$PASSED2 Total=$TOTAL2"

[ "$FAILED2" = "0" ] || fail "run 2: expected 0 failures, got $FAILED2"
[ "$PASSED2" != "" ] || fail "run 2: no passed count found in output"

# The total must differ by exactly the 1 harness self-test.
[ "$TOTAL1" = "$((TOTAL2 + 1))" ] || fail "total mismatch: run1=$TOTAL1 run2=$TOTAL2 (expected run1 = run2 + 1)"

echo "STORY_TEST: GATE PASS — harness self-test went red (1 failure), real suite green ($PASSED2 passed, 0 failed)"
exit 0
