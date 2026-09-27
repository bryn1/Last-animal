# HANDOFF — MC 1405 cycle 2 P2 fixes (out dir 202609271343-0d130992)

WHAT: Both P2 findings from the MC 1405 verify phase are closed in /srv/workspace/last-animal.
- N5: the runtime save/load proof now captures the player position before save, asserts the
  post-teleport position differs (SAVE_POSITION_MUTATION_NONVACUOUS), and asserts after load that
  the player is back at the saved position (LOAD_RESTORED_POSITION). The previously dead fields
  `_playerPosBeforeSave`/`_playerPosMutated` are used; no dead code remains.
- N6: new headless suite skeleton/tests/save/SaveLoadControllerNullHudTests.cs constructs
  SaveLoadController with hud:null and proves Save()/Load() complete without NRE (3 tests). The
  controller's hardwired GodotSaveStore segfaults headless (verified: SIGSEGV exit 139), so
  TestEngineSeams.cs stands in for the two engine seams following the existing TempDirSaveStore
  pattern; the REAL src/world/SaveLoadController.cs is what runs under test.

EVIDENCE PATHS:
- /srv/workspace/last-animal/.audits/202609262100-n5n6n7/p2-fix-evidence.md (per-finding what/why,
  gate outputs, VERIFY_EXIT=0 lines, commit hash)
- This out dir: PLACEMENT.md, TEST-verdict.md (PASS), DA-verdict.md (SHIP), ARCH-verdict.md (PASS),
  DONE.md, CYCLES.md, raw gate logs under .tmp/
- Commit: ec28b84 in /srv/workspace/last-animal (author "coder (MC 1405)", NOT pushed); the out
  dir is committed in the /home/svarkor/last-animal mirror tree.

OPEN ITEMS:
- None for this task. Recorded limitation: the harness maxDepth-1 cap made the mandated phase
  fan-out impossible from this session; all phases ran inline with executed checks (see CYCLES.md).
- Pre-existing untracked .audits/ dirs in both trees were left alone per the task brief.
