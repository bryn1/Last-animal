# DONE.md — MC 1405 cycle 2 P2 fixes (out dir 202609271343-0d130992)

| ID | claim | STATUS | evidence |
|---|---|---|---|
| D1 | N5 runtime position-restore assert written and non-vacuous (fields used, no dead code) | PASS | runtime_test.log: `SAVE_POSITION_MUTATION_NONVACUOUS ... ok saved=(0.5005265, 2.6497777, 0.011737733) mutated=(-0.83478713, 2.6497777, 1.4927144)` and `LOAD_RESTORED_POSITION ... ok now=(0.5005265, 2.6497777, 0.011737733)`; saved != mutated |
| D2 | N6 null-HUD Save/Load path unit-tested (no NRE) | PASS | save_test.log run 2: `Passed! - Failed: 0, Passed: 19` (3 new SaveLoadControllerNullHudTests tests; was 16 tests before this run) |
| D3 | `dotnet build skeleton/LastAnimalPreflight.csproj` exit 0 | PASS | build.log: `0 Error(s)`, `BUILD_EXIT=0` |
| D4 | `bash skeleton/ci/save_test.sh` exit 0 | PASS | save_test.log: `SAVE_TEST: GATE PASS`, `SAVE_EXIT=0` |
| D5 | `bash skeleton/ci/runtime_integration_test.sh` exit 0 | PASS | runtime_test.log: `RUNTIME_INTEGRATION_TEST: GATE PASS`, `RUNTIME_EXIT=0` |
| D6 | `bash skeleton/ci/bridge_mvp_test.sh` exit 0 | PASS | bridge_test.log: `BRIDGE_MVP_TEST: GATE PASS ... RESULT=PASS colors=3565`, `BRIDGE_EXIT=0` |
| D7 | at least one commit created this run | PASS | /srv/workspace/last-animal commit `ec28b84` (`git log --oneline -1`: `ec28b84 fix(save): write the N5 runtime position-restore assert; test the N6 null-HUD path (MC 1405 c2)`); out-dir commit in the mirror tree (see CYCLES.md) |
| D8 | evidence file covers both findings with VERIFY_EXIT=0 for all four gates | PASS | /srv/workspace/last-animal/.audits/202609262100-n5n6n7/p2-fix-evidence.md (chmod 644) |
| D9 | pytest in the out dir passes calling every .py source there | N/A | the out dir contains no .py source files (only .md verdicts and .tmp logs) |
| N1 | dead declared-but-unused private fields in the save proof are impossible | PASS | `grep -n "_playerPos" skeleton/ci_proofs/RuntimeIntegrationProof.Save.cs` → both fields written (RunSaveStage, RunMutateStage) and read (RunLoadStage); `dotnet build` reports 0 warnings (CS0414 would flag an unused private field) — build.log `0 Warning(s)` |
| S1 | surface backend | N/A | no backend surface exists in this Godot game change; nothing outside skeleton/ touched |
| S2 | surface db | N/A | the only data store is the `user://savegame.json` file, unchanged this run |
| S3 | surface frontend | PASS | runtime + bridge gates render the real game non-blank: bridge_test.log `RESULT=PASS ... colors=3565`; runtime_test.log `non-blank render` |
| S4 | surface api contract | N/A | no API surface; the save schema contract is covered by SaveSystemTests (19/19 green) |
| S5 | surface tests | PASS | save_test.log GATE PASS 19/19; runtime_test.log GATE PASS (positive green, all negative controls red with named markers); both planted-bad red runs bit (see TEST-verdict.md) |
| S6 | surface docs | PASS | docs/ARCHITECTURE.md re-derived against the tree in ARCH-verdict.md (VERDICT: PASS); no doc drift introduced |
| S7 | surface deploy | N/A | no deploy/packaging surface touched (tools/export_* untouched this run) |
