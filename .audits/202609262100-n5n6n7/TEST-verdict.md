# TEST-verdict — MC 1405 P3/P4 polish fixes (independent verification)

Judge: test profile · Date: 2026-09-27 · HEAD ecd0c30 (origin/master) · Repo /srv/workspace/last-animal
Evidence read then distrusted: .audits/202609262100-n5n6n7/evidence.md

## Per-claim findings

### 1. N5 — save/load persists + restores player position — VERIFIED (code) / TESTED (unit) / PARTIAL (runtime proof)
- Diff 6da97df: GameState gains `HasPlayerPosition/X/Y/Z` (default false/0); SaveLoadController
  takes optional `playerPosition`/`restorePosition` seams; WorldDirector wires them to
  `Player.GlobalPosition`. Load restores position BEFORE `_enterZone(loaded.ZoneId)`
  (SaveLoadController.cs, RunLoad path) — travel semantics confirmed by code inspection.
- Backward compat is REAL and TESTED: `Load_OldSaveWithoutPosition_HasNoPositionFlag`
  writes a pre-N5 JSON (`{"Version":N,"ZoneId":"canyon"}`) and asserts
  `HasPlayerPosition == false` — no teleport to origin. Passed in the green run.
- **FINDING (P2, dead code / incomplete runtime proof):** RuntimeIntegrationProof.Save.cs
  declares `_playerPosBeforeSave` / `_playerPosMutated` (lines 16-17) but NEVER uses them —
  the intended non-vacuous runtime position-restore assert was never written. The runtime
  save/load gate (runtime_integration_test.sh) does NOT assert position restoration; only
  the unit suite does. grep `_playerPos` returns exactly the two declarations.

### 2. N6 — nullable `_hud` — VERIFIED (code) / UNTESTED (null path)
- Diff: `_hud` is `Hud?`; Save uses `_hud?.DnaMeter ?? 0`; Load uses `_hud?.UpdateDnaMeter`
  and `_hud?.UpdateLife`. No NRE path remains by inspection.
- **FINDING (P2):** the null path is only theoretically safe — `new SaveLoadController(`
  appears exactly once in the tree (WorldDirector.cs:155, always with a real HUD). No test
  or proof constructs a no-UI composition. The `?.` guards are trivially correct, but the
  claim "no-UI compositions cannot NRE" is inspection-verified, not test-verified.

### 3. N7 — write-only EmotionState removed — VERIFIED / TESTED
- `GameState.EmotionState` removed; `Representative()` now carries position instead.
- grep `EmotionState` over skeleton: remaining hits are (a) the DIFFERENT `EmotionState`
  class in src/npc/EmotionalDepth.cs (the derived emotion type — legitimate, still read via
  `ReadHiddenState`), (b) comments explaining the removal. ZERO stale readers/writers of the
  removed GameState field; BridgeMvpProof marker 6 and SaveSystemTests updated to the
  loyalty-carried contract. Companion suite green (15/15).

### 4. Export dedup — VERIFIED
- tools/package_artifact.sh is the single zip implementation; export_linux.sh and
  export_windows.sh both call it with only the label differing. The verbatim python zip
  block is gone from both callers (diff 9d80d20 confirms).

### 5. Clobber fix — VERIFIED (executed this session)
- export_check.sh now removes only `$OUT_EXE`, the current platform's zip and
  `build/data_*_<PLAT>_x86_64`, plus `.godot`.
- Executed `bash tools/export_linux.sh` -> exit 0. AFTER the run:
  `build/LastAnimal.exe` (111299824 B, mtime 12:45 — untouched) and
  `build/data_LastAnimalPreflight_windows_x86_64/` (mtime 12:45) SURVIVED, while
  `LastAnimal.x86_64` + `data_..._linuxbsd_x86_64` were rebuilt (mtime 13:05).
  Linux smoke: `RESULT=PASS ... colors=2034` (non-blank).

### 6. Docs honesty — VERIFIED
- docs/build-and-run.md: assembly-name note (lines 7-12), Linux smoke limit stated
  ("asserts launch and a non-blank framebuffer" only), "The C12 animation pipeline: what
  actually exists" section present. `skeleton/_scratch/.gdignore` committed (empty file).

## Gate outputs (all executed this session, exit codes observed)
- `dotnet build skeleton/LastAnimalPreflight.csproj` -> Build succeeded, 0 warnings, 0 errors, exit 0
- `bash ci/save_test.sh` -> GATE PASS (run 1: 1 deliberate harness failure; run 2: 16/16), exit 0
- `bash ci/companion_test.sh` -> GATE PASS (15/15), exit 0
- `bash ci/runtime_integration_test.sh` -> GATE PASS (LA_GATE: PASS, all red-modes red), exit 0
  (note: requires `source ci/toolchain.sh` first — scripts demand $GODOT)
- `bash ci/bridge_mvp_test.sh` -> GATE PASS (six markers, RESULT=PASS colors=3586), exit 0
- `bash tools/export_linux.sh` -> exit 0, zip 62980451 B, smoke RESULT=PASS

## Planted-bad red run (save_test.sh gate)
Broke `Assert.Equal(12.5f, loaded.PlayerX)` -> `Assert.Equal(99f, ...)` in
tests/save/SaveSystemTests.cs, ran dotnet test:
```
Failed LastAnimal.Tests.Save.SaveSystemTests.RoundTrip_PreservesPlayerPosition [2 ms]
Failed!  - Failed: 2, Passed: 15, Skipped: 0, Total: 17
```
Gate went RED on exactly the N5 assertion. Restored with `git checkout --` (working tree clean).

## Adversarial answers
- Is the N5 runtime assert non-vacuous? **There is no runtime position assert at all** —
  the fields for it exist but are unused (finding above). The unit round-trip assert IS
  non-vacuous (12.5/0.25/-7.75 vs defaults 0/0/0, and the planted-bad run proves it bites).
- Any test still referencing removed EmotionState field? No — zero stale references.

## Housekeeping notes for orchestrator
- Godot gate runs regenerated three untracked `.uid` files (tests/BossControllerTests.cs.uid,
  tests/companion/WageBetrayalPillarTests.cs.uid, tests/save/ZoneProgressionTests.cs.uid) —
  engine-generated, not deliverables; commit or gitignore as you see fit.
- The dod-loop out dir /home/svarkor/last-animal/.audits/202609271254-db737fde/ did not
  exist; created it and mirrored this verdict there.

# JUDGED: 44136fa355b3678a1146ad16f7e8649e94fb4fc21fe77e8310c060f61caaff8a
# VERDICT: PASS
