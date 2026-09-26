# Evidence — MC 1388: bridge_mvp gate failure (companion did not follow)

Run: 202609262020-9cdd18c5 · Repo: /srv/workspace/last-animal (HEAD abbbffb → dd1dbcb) · Engine: godot 4.7.2.stable.mono (ci/toolchain.sh), dotnet 8.0.131

## 1. Failure (reproduced this session, pre-fix)

```
BRIDGE_MVP_PROOF: FAIL — companion did not follow (moved=0.435, dist 3.499 -> 3.501)
BRIDGE_MVP_TEST: GATE FAIL: headless proof exited 1 (non-zero)
```

## 2. Root cause — CLASSIFICATION: STALE PROOF SCENARIO (not a game bug)

Git evidence:
- `git log -S "FollowDistance { get; set; } = 3.5f" -- skeleton/world/CompanionFollowBody.cs`
  → only a7829ed ("delete CompanionActor … add CompanionFollowBody"). The follow movement code
  has NOT changed since; commit f527c4c (wage cadence/betrayal, MC 1344.5 A3) touched
  `src/companion/` needs/state files but NOT CompanionFollowBody.cs (`git show --stat f527c4c`).
- `git log -S "_followDist0 = _companion.GlobalPosition.DistanceTo" -- skeleton/ci_proofs/BridgeMvpProof.cs`
  → 65b769d "fix(ci): bridge proof teleport baseline + tick-gated hold (MC 1344.2)" moved the
  follow baseline capture to teleport time.

Mechanism: `CompanionFollowBody._Process` lerps toward `player + dir * FollowDistance(3.5)` —
its contract is to HOLD a 3.5-unit ring behind the player, not to close to zero. The MC 1344.2
baseline-at-teleport change made `dist0` land almost exactly on that ring (dist0 = 3.499): the
companion was already at its target distance, so it correctly stayed put (moved 0.435) and the
assertion `d1 > dist0 - 0.25` fired. The scenario, not the game, regressed.

## 3. Fix (skeleton/ci_proofs/BridgeMvpProof.cs, commit dd1dbcb)

At the stage-3 → 4 transition the proof repositions the player +6 X and recaptures the follow
baseline there (no race: captured before the companion can converge on the new spot). The
follow window then measures a real closing run against the real contract. The stage-1
teleport-time capture is kept as a diagnostic only. No game code changed.

## 4. Planted-bad RED run (gate proven able to fail)

Planted: assertion inverted to `if (moved > -1.0f || d1 > _followDist0 - 0.25f)` (always true).
```
BRIDGE_MVP_PROOF: FAIL — companion did not follow (moved=5.74, dist 9.256 -> 3.516)
BRIDGE_MVP_TEST: GATE FAIL: headless proof exited 1 (non-zero)
RED_EXIT=1
```
(Full log: .audits/202609262020-9cdd18c5/.tmp/red-run.log. The same log shows the fixed
scenario's companion closing dist 9.256 → 3.516, moved 5.74.) Restored afterwards
(`grep -c "moved < 0.2f"` → 1).

## 5. Green runs (3 consecutive)

```
GREEN1_EXIT=0 … MARKER 5/6 COMPANION_FOLLOWS — dist 9.254 -> 3.516, moved 5.739 … GATE PASS
GREEN2_EXIT=0 … MARKER 5/6 COMPANION_FOLLOWS — dist 9.256 -> 3.516, moved 5.741 … GATE PASS
GREEN3_EXIT=0 … MARKER 5/6 COMPANION_FOLLOWS — dist 9.252 -> 3.515, moved 5.737 … GATE PASS
```
(Logs: .audits/202609262020-9cdd18c5/.tmp/green-{1,2,3}.log)

## 6. VERIFY_EXIT lines

```
BUILD_EXIT=0      # dotnet build skeleton/LastAnimalPreflight.csproj
RUNTIME_EXIT=0    # bash skeleton/ci/runtime_integration_test.sh → "RUNTIME_INTEGRATION_TEST: GATE PASS"
```

## 7. Commit

```
dd1dbcb fix(ci): bridge proof follow stage repositions the player before the follow window (MC 1388)
```
Authored `coder (MC 1388) <coder@agent-town.local>`, not pushed.
