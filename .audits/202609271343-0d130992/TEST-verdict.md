# TEST-verdict — MC 1405 cycle 2 P2 fixes (independent verification)

Judge: test profile (executed inline — the harness caps subagent depth at 1, so this session could
not spawn children; every check below was executed in this session) · Date: 2026-09-27 ·
Repo /srv/workspace/last-animal · Commit ec28b84
Evidence read then distrusted: /srv/workspace/last-animal/.audits/202609262100-n5n6n7/p2-fix-evidence.md

## Finding 1 — N5 runtime position-restore assert — TESTED

- `RuntimeIntegrationProof.Save.cs` now writes `_playerPosBeforeSave` before `SaveGame()`,
  `_playerPosMutated` after `TeleportIntoRange()` and at the mutation-kill transition, and asserts
  in `RunLoadStage`. grep `_playerPos` shows every field read as well as written — no dead code.
- Executed `bash skeleton/ci/runtime_integration_test.sh` (with `source ci/toolchain.sh`) →
  RUNTIME_EXIT=0, GATE PASS. The save mode printed, verbatim:
  ```
  LA_GATE: check: SAVE_POSITION_MUTATION_NONVACUOUS: ... ok saved=(0.5005265, 2.6497777, 0.011737733) mutated=(-0.83478713, 2.6497777, 1.4927144)
  LA_GATE: check: LOAD_RESTORED_POSITION: ... ok now=(0.5005265, 2.6497777, 0.011737733) (saved (0.5005265, 2.6497777, 0.011737733), mutated (-0.93375784, 0.8282702, 1.4927144))
  ```
  saved != mutated → the restore assert is non-vacuous; restored == saved.
- Planted-bad red run (executed): inverted the `LOAD_RESTORED_POSITION` comparison →
  `LA_GATE: FAIL — LOAD_RESTORED_POSITION`, gate exit 1. The assert bites. Restored afterwards.

## Finding 2 — N6 null-HUD path — TESTED

- `skeleton/tests/save/SaveLoadControllerNullHudTests.cs` constructs `SaveLoadController` with
  `hud: null`; 3 tests prove `Save()` and `Load()` complete without NRE and that a null-HUD load
  still restores companion id/loyalty, health, zone re-entry and progression.
- The controller compiled into the test project is the REAL `src/world/SaveLoadController.cs`
  (csproj Compile list). The engine seams are stood in by `TestEngineSeams.cs` — justified: the
  real `GodotSaveStore` calls `ProjectSettings.GlobalizePath`, which this session verified
  SEGFAULTS headless (dotnet console probe, SIGSEGV exit 139). The stand-in follows the existing
  `TempDirSaveStore` pattern; the `?.` guards under test are production code.
- Executed `bash skeleton/ci/save_test.sh` → SAVE_EXIT=0, GATE PASS (run 1: exactly 1 deliberate
  harness failure; run 2: 19/19 — the 3 new tests included).
- Planted-bad red run (executed): `Assert.True(controller.Load())` → `Assert.False(...)` →
  `Failed ... Load_CompletesWithoutNre_WhenHudIsNull`; restored, 3/3 green.

## Gate outputs (all executed this session on commit ec28b84)

- `dotnet build skeleton/LastAnimalPreflight.csproj` → 0 errors, BUILD_EXIT=0
- `bash skeleton/ci/save_test.sh` → GATE PASS, SAVE_EXIT=0
- `bash skeleton/ci/runtime_integration_test.sh` → GATE PASS, RUNTIME_EXIT=0
- `bash skeleton/ci/bridge_mvp_test.sh` → GATE PASS, BRIDGE_EXIT=0 (six markers, RESULT=PASS colors=3565)

Raw logs: .tmp/{build,save_test,runtime_test,bridge_test}.log in this out dir.

## Adversarial answers

- Could the new runtime assert be vacuous? No — the non-vacuousness is itself asserted
  (SAVE_POSITION_MUTATION_NONVACUOUS) and the planted-bad run went red on the restore assert.
- Could the null-HUD test pass while the production guards are broken? No — the compiled
  controller is the production file; only the engine seams are stand-ins, and the segfault probe
  proves the real store cannot run headless.
- Any untracked file created during the run in the out-dir tree? The out dir is committed (see
  CYCLES.md); scratch lives under .tmp/.

# JUDGED: 939afe5be211bfe2402af789f0cd8b12587d4bceb0edd9dc2692795d2a51ff4f
# VERDICT: PASS
