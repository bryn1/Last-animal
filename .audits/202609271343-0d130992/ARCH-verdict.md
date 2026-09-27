# ARCH-verdict — MC 1405 cycle 2 P2 fixes (architecture-vs-reality + hygiene, closing phase)

Judge: design profile (executed inline — the harness caps subagent depth at 1, so this session
could not spawn children) · Date: 2026-09-27 · Repo /srv/workspace/last-animal · Commit ec28b84

## 1. docs/ARCHITECTURE.md vs the actual tree

`/srv/workspace/last-animal/docs/ARCHITECTURE.md` exists and was re-derived from the artifact, not
from a report:

- Modules/entrypoints: the doc's proof-harness paragraph (lines 92-93) names
  `RuntimeIntegrationProof.cs` + the `RuntimeIntegrationProof.Save.cs` / `.Interact.cs` partial
  halves — both still exist at that path; this run changed only stage bodies inside the existing
  partial class, no new module or entrypoint.
- The doc names `SaveLoadController.cs` (line 47) — still at `skeleton/world/`, unchanged this run.
- Dependencies: the save test project (`tests/save/LastAnimalSaveTests.csproj`) now compiles the
  controller + its engine-free deps with test-only seam stand-ins. This is below the doc's
  granularity (it documents the engine-free test-project pattern generically); no module,
  entrypoint, port or data store changed, so no doc edit is required. The doc's claim that saves
  live in `user://` (production) is untouched — the stand-in writes to the OS temp dir in TESTS
  only, which matches the doc's engine-free-test framing.
- Data stores: `user://savegame.json` unchanged; no new store.

## 2. File hygiene

- `ci_proofs/RuntimeIntegrationProof.Save.cs` — 145 lines (was 132), one concern (save-mode
  stages), far under the 250 soft target.
- `tests/save/SaveLoadControllerNullHudTests.cs` — 94 lines, one concern (null-HUD controller
  behaviour).
- `tests/save/TestEngineSeams.cs` — 59 lines, one concern (test-only engine-seam stand-ins), with
  a header stating why it exists and that it must not grow UI behaviour.
- No file over the 400 hard ceiling; no TODO/FIXME added (`grep -rn "TODO\|FIXME"` over the changed
  files: zero hits).

## 3. Layout v2 paths

- Code changes: only under `skeleton/ci_proofs/` and `skeleton/tests/save/` — existing layout-v2
  locations.
- Evidence: `/srv/workspace/last-animal/.audits/202609262100-n5n6n7/p2-fix-evidence.md` — inside
  the existing audit dir named by the task.
- Verdicts/DONE/CYCLES: this out dir, `/home/svarkor/last-animal/.audits/202609271343-0d130992/`,
  committed to that tree so no untracked file remains. No stray sibling dirs invented.

All three checks hold.

# JUDGED: 939afe5be211bfe2402af789f0cd8b12587d4bceb0edd9dc2692795d2a51ff4f
# VERDICT: PASS
