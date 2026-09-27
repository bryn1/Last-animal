# p2-fix-evidence.md — MC 1405 cycle 2, the two P2 findings closed

Judge: code profile (inline — the harness caps subagent depth at 1, so phase children could not
be spawned from this session; every check below was executed in this session) · Date: 2026-09-27
· Repo /srv/workspace/last-animal · Base ecd0c30 · Commit **ec28b84**

## Finding 1 — N5 dead fields / missing runtime position-restore assert

**What was done.** `skeleton/ci_proofs/RuntimeIntegrationProof.Save.cs`: the save mode now
captures `_playerPosBeforeSave = _playerBody!.GlobalPosition` before `SaveGame()`, captures
`_playerPosMutated` after `TeleportIntoRange()` (and again when the mutation kill lands), asserts
the two differ (`SAVE_POSITION_MUTATION_NONVACUOUS`, > 0.5 m), and after `LoadGame()` asserts the
player is back at the saved position (`LOAD_RESTORED_POSITION`, < 0.05 m). The fields are used;
no dead code remains.

**Why this shape.** The proof mode already mutates the player through the real path
(`TeleportIntoRange` + the second kill), so the assert rides the existing stage flow — no new
mechanism.

**Green output (this session, runtime_integration_test.sh, save mode):**
```
LA_GATE: check: SAVE_POSITION_MUTATION_NONVACUOUS: ... ok saved=(0.5005265, 2.6497777, 0.011737733) mutated=(-0.83478713, 2.6497777, 1.4927144)
LA_GATE: check: LOAD_RESTORED_POSITION: ... ok now=(0.5005265, 2.6497777, 0.011737733) (saved (0.5005265, 2.6497777, 0.011737733), mutated (-0.93375784, 0.8282702, 1.4927144))
```
saved != mutated (non-vacuous); restored == saved.

**Planted-bad red run.** Inverted the `LOAD_RESTORED_POSITION` comparison (`< 0.05f` → `> 0.05f`),
re-ran the gate:
```
LA_GATE: check: LOAD_RESTORED_POSITION: ... FAIL now=(0.5005265, 2.6497777, 0.011737733) (saved (...), mutated (-0.93375784, 0.92933273, 1.4927144))
LA_GATE: FAIL — LOAD_RESTORED_POSITION: ...
```
exit 1 — the assert bites. Restored; the real fix was committed BEFORE the planted-bad run
(lesson applied after an early `git checkout --` briefly reverted it; re-applied verbatim).

## Finding 2 — N6 null-HUD path tested

**What was done.** New headless suite `skeleton/tests/save/SaveLoadControllerNullHudTests.cs`
(3 tests) constructs `SaveLoadController` with `hud: null` and proves `Save()` and `Load()`
complete without NRE, and that a null-HUD load still restores companion id/loyalty, health,
re-enters the zone and seeds progression. `skeleton/tests/save/TestEngineSeams.cs` stands in for
the two engine seams the controller hardwires (`Hud` surface; `GodotSaveStore` → temp-dir store,
the existing `TempDirSaveStore` pattern). `LastAnimalSaveTests.csproj` compiles the REAL
`src/world/SaveLoadController.cs` plus its engine-free deps.

**Why the stand-in.** A direct null-HUD unit is impossible headless: `GodotSaveStore` calls
`ProjectSettings.GlobalizePath`, which **segfaults outside the engine** (verified this session: a
plain dotnet console probe calling it died with SIGSEGV, exit 139). The constructor signature
itself is fine (`Hud? hud`); the store seam is the obstacle, and the codebase pattern for that is
an engine-free store — so the stand-in follows the pattern instead of adding a guard to
production code (which the task's file constraints forbid).

**Green output:** `save_test.sh` run 2: `Passed! - Failed: 0, Passed: 19` (was 16 before this run;
+3 new tests). Planted-bad: inverted `Load()`'s assert to `Assert.False` → the test went RED
(`Failed ... Load_CompletesWithoutNre_WhenHudIsNull`), restored, 3/3 green.

## Gate outputs (all executed this session on the committed tree)

| Gate | Result |
|---|---|
| `dotnet build skeleton/LastAnimalPreflight.csproj` | 0 errors — VERIFY_EXIT=0 |
| `bash skeleton/ci/save_test.sh` | GATE PASS (run 1: exactly 1 deliberate harness failure; run 2: 19/19) — VERIFY_EXIT=0 |
| `bash skeleton/ci/runtime_integration_test.sh` (with `source ci/toolchain.sh`) | GATE PASS — positive green, all negative controls red with named markers, save round-trip green incl. the new position asserts — VERIFY_EXIT=0 |
| `bash skeleton/ci/bridge_mvp_test.sh` (with `source ci/toolchain.sh`) | GATE PASS — six markers, RESULT=PASS colors=3565 — VERIFY_EXIT=0 |

Raw logs: /home/svarkor/last-animal/.audits/202609271343-0d130992/.tmp/{build,save_test,runtime_test,bridge_test}.log

## Commit

- **ec28b84** — `fix(save): write the N5 runtime position-restore assert; test the N6 null-HUD
  path (MC 1405 c2)` — author `coder (MC 1405) <coder@agent-town.local>`, not pushed.
- Audit-evidence commit in the mirror tree: see the out dir's CYCLES.md.

## Constraints honoured

Only `skeleton/ci_proofs/RuntimeIntegrationProof.Save.cs` and `skeleton/tests/save/` were touched
(`git show --stat ec28b84`: 7 files, all under those two paths, plus engine-generated `.uid`
files for the new sources). WorldDirector.cs untouched — no wiring change was required.
