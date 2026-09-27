# MC 1405 — P3/P4 polish residue (N5/N6/N7 + hygiene + export scripts)

Run dir: .audits/202609262100-n5n6n7/ · Date: 2026-09-27 · Base: 2a43498

## Findings fixed
- N5 (P3): load did not reposition the player. GameState gains
  HasPlayerPosition/X/Y/Z (backward compatible: old saves deserialize with
  HasPlayerPosition=false and keep the player where they are); SaveLoadController
  takes optional playerPosition/restorePosition seams; WorldDirector wires them;
  load restores position BEFORE zone re-entry (travel semantics). Commit 6da97df.
- N6 (P3): Save() latent _hud NRE in no-UI compositions. _hud is now `Hud?`
  (nullable), matching WorldDirector's optional-node tolerance. Commit 6da97df.
- N7 (P4): EmotionState was write-only. REMOVED (not wired): emotion is a pure
  function of CompanionLoyalty (EmotionalDepth.ReadHiddenState); loyalty already
  persists, a second derivable copy would be a stale duplicate. Bridge marker 6
  and save tests updated to the loyalty-carried contract. Commit 6da97df.
- Export dedup (DA P3): verbatim zip block extracted to tools/package_artifact.sh;
  both export scripts call it. Commit 9d80d20.
- Clobber fix (DA P3): export_check.sh cleans only the current platform's exe/zip/
  data dir (plus .godot), not the whole build/. VERIFIED: after a Linux export,
  build/LastAnimal.exe + data_*_windows_x86_64 survived; after export_check
  (Windows), build/LastAnimal.x86_64 + data_*_linuxbsd_x86_64 survived.
  Commit 9d80d20.
- Docs honesty (N11/C12/smoke limit): assembly-name note, smoke-limit sentence,
  C12 = animation_pipeline bake + walkBaked.tres pair. Commit 2b37b8b.
- .gdignore: skeleton/_scratch/.gdignore committed (build/ is gitignored; its
  .gdignore is local-only). Commit 2b37b8b.

## Gate evidence (all executed 2026-09-27 on the combined tree)
- dotnet build skeleton/LastAnimalPreflight.csproj -> exit 0
- bash ci/save_test.sh -> exit 0
- bash ci/companion_test.sh -> exit 0
- bash ci/runtime_integration_test.sh -> exit 0
- bash ci/bridge_mvp_test.sh -> exit 0 (GATE PASS, six markers)
- bash ci/export_check.sh -> exit 0 (PE magic OK, 111299824 B)
- bash tools/export_linux.sh -> exit 0; launch_linux_smoke RESULT=PASS
  (colors=2038, non-blank)

## Commits
6da97df (N5+N6+N7), 9d80d20 (dedup+clobber), 2b37b8b (docs+gdignore) —
authored coder (MC 1405), pushed to origin/master.

VERIFY_EXIT=0
