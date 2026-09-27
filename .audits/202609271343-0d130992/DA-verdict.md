# DA-verdict — MC 1405 cycle 2 P2 fixes (adversarial review)

Judge: devils-advocate profile (executed inline — the harness caps subagent depth at 1, so this
session could not spawn children; every refutation below was tested against the code or a live run
in this session) · Date: 2026-09-27 · Repo /srv/workspace/last-animal · Commit ec28b84

## Refutation attempts and outcomes

1. **"The runtime position assert is still vacuous."** REFUTED-BY-RUN: the save mode prints
   `SAVE_POSITION_MUTATION_NONVACUOUS ... ok saved=(0.5005, 2.6498, 0.0117) mutated=(-0.8348, 2.6498, 1.4927)`
   — the two differ by >1.9 m, so `LOAD_RESTORED_POSITION` cannot pass by construction. The
   planted-bad run (inverted comparison) went red with `LA_GATE: FAIL — LOAD_RESTORED_POSITION`,
   gate exit 1. VERIFIED.

2. **"The load assert could pass while the restore is broken — physics may move the player
   between restore and check."** Attempted refutation: gravity/knockback between `LoadGame()` and
   the check. REFUTED-BY-CODE: `RunLoadStage` calls `LoadGame()` then `Check(...)` synchronously in
   the same stage tick — no physics tick can interleave (single-threaded SceneTree process). The
   0.05 m epsilon additionally tolerates float noise. The green run shows restored == saved to all
   printed digits. VERIFIED.

3. **"The null-HUD test tests a stub, not the controller."** REFUTED-BY-CSProj: the test project
   compiles the REAL `src/world/SaveLoadController.cs` (explicit `<Compile Include>`); only the
   two engine seams are stand-ins. The `?.` guards under test are production code. The alternative
   (compiling the real `GodotSaveStore`) is impossible headless — this session's console probe
   calling `ProjectSettings.GlobalizePath` died with SIGSEGV (exit 139), which is also why the
   stand-in is the smallest honest instrument, not laziness. VERIFIED.

4. **"The stand-in store could mask a repo-tree write."** REFUTED: the stand-in's SavePath is
   `Path.Combine(Path.GetTempPath(), "lastanimal-savecontroller-tests", savegame.json)` — under the
   OS temp dir, mirroring the `RoundTrip_WritesOutsideRepoTree` guarantee; the repo tree is never
   touched. VERIFIED.

5. **"The stand-in Hud could drift from the real Hud's surface."** Partially valid concern,
   bounded: the stand-in carries exactly the three members the controller uses
   (`DnaMeter`, `UpdateDnaMeter`, `UpdateLife`) and its header forbids growing UI behaviour. If the
   controller ever touches a fourth member, the test project FAILS TO COMPILE — drift is loud, not
   silent. Accepted with that bound.

6. **"Dead code remains either way."** REFUTED: grep `_playerPos` over
   `RuntimeIntegrationProof.Save.cs` shows both fields written (RunSaveStage, RunMutateStage) and
   read (RunLoadStage + the non-vacuousness check). No dead fields, no TODO.

7. **"Scope creep / regression risk."** `git show --stat ec28b84`: 7 files, all under
   `skeleton/ci_proofs/` and `skeleton/tests/save/` (plus engine-generated `.uid` files for the new
   sources). WorldDirector.cs untouched. All four gates re-run green on the committed tree
   (build 0 errors; save 19/19; runtime GATE PASS with all negative controls red; bridge six
   markers RESULT=PASS). No regression found.

8. **"File hygiene."** New/changed sources: Save.cs 145 lines, SaveLoadControllerNullHudTests.cs
   94, TestEngineSeams.cs 59 — all far under the 250 soft target; one concern per file. VERIFIED.

## Summary

Every refutation line was answered by code, a live run, or the segfault probe. The two P2
findings are closed with non-vacuous, planted-bad-verified coverage. No new P0/P1/P2 found.

# JUDGED: 939afe5be211bfe2402af789f0cd8b12587d4bceb0edd9dc2692795d2a51ff4f
# VERDICT: SHIP
