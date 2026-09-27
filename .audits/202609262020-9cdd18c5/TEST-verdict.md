# TEST-verdict — MC 1388 bridge_mvp gate fix (run 202609262020-9cdd18c5)

Independent test pass over the fix in commit dd1dbcb (proof-scenario correction in
`skeleton/ci_proofs/BridgeMvpProof.cs`). Evidence under test:
`/srv/workspace/last-animal/.audits/202609261400-bridgefix/evidence.md`.

## Checks run this pass (independently executed, not taken from the author's report)

1. `python3 -m pytest test_dod_evidence.py -q` (this out dir) → **7 passed**, exit 0.
   The suite grades: evidence file exists, classification + git evidence present, red run
   recorded (RED_EXIT=1 + GATE FAIL), 3 consecutive green runs, BUILD_EXIT=0 and
   RUNTIME_EXIT=0 lines, commit hash dd1dbcb, fix present in source and planted-bad
   assertion restored.
2. Fresh gate re-run (this pass's own decisive check, 4th green overall):
   `bash skeleton/ci/bridge_mvp_test.sh` → exit 0,
   `MARKER 5/6 COMPANION_FOLLOWS — companion closed on the player (dist 9.247 -> 3.516, moved 5.731)`,
   `BRIDGE_MVP_TEST: GATE PASS — six-marker playable MVP verified`.
3. Red capability cross-checked from the recorded planted-bad log
   (`.tmp/red-run.log`): `RED_EXIT=1`, `GATE FAIL: headless proof exited 1` — the gate
   demonstrably fails, and the same log shows the fixed scenario closing 9.256 → 3.516.

## Findings

- The gate now tests the real contract (companion holds/closes to its FollowDistance ring
  behind the player) with a baseline captured before convergence is possible — no race.
- The planted-bad run inverts the assertion (always-fail) rather than breaking the follow
  call; the brief names either as acceptable. A `LerpSpeed=0` plant would additionally
  prove detection of a non-following companion; noted, not blocking — the moved>=0.2 arm
  of the assertion covers that case by construction.
- No test file changed in the git tree; no TODO/FIXME added.

python3 /usr/local/bin/dod_judged_hash.py /home/svarkor/last-animal/.audits/202609262020-9cdd18c5 --base abbbffbd1bea1d719882570c2e63140d121e55b1
# JUDGED: 282cd8e9bddac877665bee46f19a844af4240079d86b4d2fd265c2baa9638a97
# VERDICT: PASS
