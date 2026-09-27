# HANDOFF — run 202609262020-9cdd18c5 (MC 1388 bridge_mvp gate fix)

WHAT: bridge_mvp regression gate red ("companion did not follow, moved=0.435, dist
3.493 -> 3.501") root-caused and fixed. Classification: STALE PROOF SCENARIO, not a game
bug — CompanionFollowBody's contract (unchanged since a7829ed) is to hold a 3.5-unit ring
behind the player; commit 65b769d (MC 1344.2) moved the follow baseline to teleport time,
where dist0 lands exactly on the ring, so the "dist must shrink 0.25" assert was
unsatisfiable. Fix: BridgeMvpProof.cs repositions the player +6 X at the stage-3→4
transition and recaptures the baseline there (race-free); the follow window then measures
a real closing run (dist 9.25 → 3.52, moved ~5.74).

EVIDENCE:
- Fix commit: dd1dbcb; evidence commits: 18d8eda (both authored coder (MC 1388), not pushed)
- Evidence file: /srv/workspace/last-animal/.audits/202609261400-bridgefix/evidence.md (mode 644)
- Run dir: /home/svarkor/last-animal/.audits/202609262020-9cdd18c5/
  (ARCH-opening.md, TEST-verdict.md PASS, DA-verdict.md SHIP, ARCH-verdict.md PASS,
  DONE.md all-PASS, CYCLES.md, test_dod_evidence.py — 7 passed)
- Gate logs: run dir .tmp/{red-run,green-1..3,test-phase-run,build,runtime}.log

VERIFY (all executed this session):
- bash skeleton/ci/bridge_mvp_test.sh → exit 0 ×4 (3 consecutive + 1 re-run), GATE PASS
- planted-bad red run → exit 1 (gate proven able to fail), then restored
- dotnet build skeleton/LastAnimalPreflight.csproj → exit 0
- bash skeleton/ci/runtime_integration_test.sh → exit 0, GATE PASS
- pytest test_dod_evidence.py → 7 passed

OPEN ITEMS:
- Fan-out limitation: this session is at subagent depth 1; `subagent` spawns were
  rejected by the harness (maxDepth 1), so the mandated phase children ran inline in
  phase order. The parent may want to re-run the verifying phases as true children if
  independent-spawn assurance is required.
- Commits are local only (per brief: do NOT push).
