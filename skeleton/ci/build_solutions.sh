#!/usr/bin/env bash
# build_solutions.sh — the engine `--build-solutions --quit-after N` step with a
# NAMED BOUNDED retry for the quit-after-DONE segfault class (MC 11312; class
# named by MC 10209 in the LEDGER and re-counted by MC 11295 finding (2)).
#
# THE CLASS (measured, not guessed): `godot --headless --build-solutions
# --quit-after N` SEGFAULTS AT QUIT (exit 139) at a LOW rate WHEN the step also
# RECOMPILES — 4x observed vs 0x controls (MC 11295 worktree counts), and
# MC 10209's wave close recorded it verbatim: "all import stages DONE ... the
# engine SEGFAULTED at quit after the final DONE (exit 139)". The crash rides
# the engine's post-build scan/quit teardown, NEVER the dotnet compile itself.
# Shipped steps hard-failed on it (MC 11243 F5 law: a non-zero rc is never
# swallowed) — correct for real failures, but a wall burned a whole retry on an
# engine-teardown flake with a perfectly good assembly on disk.
#
# WHY THE CONDITION IS dotnet build, NOT THE ENGINE'S DONE LINE (measured at
# dbf2e08, transcripts .audits/20261101-0940-segfault/out/buildstep-*.log): a
# REAL compile error in a <Compile Include>'d file still prints
# `[ DONE ] dotnet_build_project` and exits 1 — the engine's DONE names the
# STEP, not the BUILD. So exit-139-at-quit is only retryable after proving the
# assembly built, and the only sound proof is the build tool itself: re-run
# `dotnet build` (the MC 11243 sky-probe sanctioned route) and require
# "Build succeeded" + rc 0 + the assembly on disk. Build did NOT land => the
# 139 rode a REAL failure => hard fail BY NAME, zero retries (no masking).
#
# BOUNDED, NAMED, NEVER BLANKET: max LA_BUILD_RETRY_MAX (default 2) retries
# after the first attempt, each logged "quit-segfault retry k/N"; exhaustion is
# a hard named FAIL, never a loop. NO other non-zero rc is ever retried:
# budget kills (124/137/143) go RED LOUDLY as BUDGET-DIED, any other rc RED BY
# NAME (the MC 11243 F5 idiom preserved). Every outcome prints its matched
# signature line (BUILD_STEP: signature=...). RED-capability proven with a
# forced-139 wrapper (exhaustion) + a broken-plant 139 (no-masking) — see
# .audits/20261101-0940-segfault/out/EVIDENCE.md.
#
# Usage:  GODOT=/path/to/godot ci/build_solutions.sh [project_dir] [quit-after]
#   defaults: the dir above ci/, 1. Exits 0 only on exit-0-clean.
set -uo pipefail

HERE="$(cd "$(dirname "$0")" && pwd)"
PROJ="$(cd "${1:-$(dirname "$HERE")}" && pwd)"
QA="${2:-1}"
RETRY_MAX="${LA_BUILD_RETRY_MAX:-2}"
case "$RETRY_MAX" in (*[!0-9]*|'') echo "BUILD_STEP: GATE FAIL: LA_BUILD_RETRY_MAX must be a non-negative integer, got '$RETRY_MAX'" >&2; exit 1;; esac
# shellcheck source=toolchain.sh
source "$HERE/toolchain.sh"   # pinned dotnet (the VERIFY leg needs it on PATH)
: "${GODOT:?set GODOT to the engine binary (see engine/PIN.txt)}"
SLN="$PROJ/LastAnimalPreflight.sln"
DLL="$PROJ/.godot/mono/temp/bin/Debug/LastAnimalPreflight.dll"

# build_landed_p — did the CURRENT source compile, per the build tool itself?
# Re-running dotnet build is safe here: the crash is at engine quit, AFTER the
# build step, so a healthy tree re-verifies as an incremental no-op ("Build
# succeeded", 0 errors). Prints its own named verdict line.
build_landed_p() {
  local out rc
  out="$(timeout 300 dotnet build "$SLN" -c Debug 2>&1)"; rc=$?
  if [ "$rc" -eq 0 ] && [[ "$out" == *"Build succeeded"* ]] && [ -f "$DLL" ]; then
    echo "BUILD_STEP: build-verified (dotnet build rc=0 + 'Build succeeded' + assembly present)"
    return 0
  fi
  printf '%s\n' "$out" | tail -20 >&2
  echo "BUILD_STEP: build-verify FAILED (dotnet build rc=$rc, marker/assembly: rc=$rc marker=$( [[ "$out" == *"Build succeeded"* ]] && echo yes || echo no ) dll=$([ -f "$DLL" ] && echo present || echo missing))" >&2
  return 1
}

# the shipped engine step, verbatim — every attempt runs exactly what every
# shipped caller ran before this card
step_attempt() {
  timeout 300 "$GODOT" --headless --path "$PROJ" --build-solutions --quit-after "$QA"
}

k=1
while :; do
  step_attempt
  rc=$?
  if [ "$rc" -eq 0 ]; then
    echo "BUILD_STEP: signature=exit-0-clean attempt=$k — engine build step exited 0"
    exit 0
  fi
  if [ "$rc" -ne 139 ]; then
    case "$rc" in
      (124|137|143) echo "BUILD_STEP: signature=budget-kill-rc$rc (BUDGET-DIED, NOT retried) attempt=$k" >&2 ;;
      (*)           echo "BUILD_STEP: signature=nonzero-rc$rc (NOT the quit-139 class, NOT retried) attempt=$k" >&2 ;;
    esac
    echo "BUILD_STEP: GATE FAIL: engine --build-solutions exited $rc — non-zero rc is never swallowed (MC 11243 F5)" >&2
    exit 1
  fi
  # exit 139: the segfault-at-quit candidate — retry ONLY with the assembly proven built.
  if ! build_landed_p; then
    echo "BUILD_STEP: signature=exit-139-build-NOT-landed (REAL build failure riding a 139 — NOT retried, NOT masked) attempt=$k" >&2
    echo "BUILD_STEP: GATE FAIL: engine --build-solutions exited 139 AND the build never landed — this is a REAL build failure" >&2
    exit 1
  fi
  if [ "$k" -ge "$((RETRY_MAX + 1))" ]; then
    echo "BUILD_STEP: signature=quit-segfault-rc139-build-verified — retry EXHAUSTED after $RETRY_MAX named retries (bound $RETRY_MAX reached; giving up, NOT looping) attempts=$((RETRY_MAX + 1))" >&2
    echo "BUILD_STEP: GATE FAIL: quit-segfault persisted through every named retry — assembly builds but the engine quit step keeps segfaulting" >&2
    exit 1
  fi
  k=$((k + 1))
  echo "BUILD_STEP: signature=quit-segfault-rc139-build-verified — quit-segfault retry $((k - 1))/$RETRY_MAX (bound $RETRY_MAX, named per attempt)" >&2
done
