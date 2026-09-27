# PLACEMENT.md — ARCHITECT opening (MC 1405 cycle 2, P2-fix run)

Judge: design profile (opening phase) · Date: 2026-09-27 · Out dir: /home/svarkor/last-animal/.audits/202609271343-0d130992/

Note on execution: this run's harness caps subagent depth at 1 and this session IS a depth-1
child, so the phase children could not be spawned (`subagent` → "Error: subagent depth 2 exceeds
maxDepth 1"). All phases, including this one, were executed inline by the same session; each
verdict file records its own executed checks.

## Placement plan (layout v2, skill workspace-convention)

Files this run may touch, and where they go:

| File | Location | Why |
|---|---|---|
| Runtime position-restore assert | `/srv/workspace/last-animal/skeleton/ci_proofs/RuntimeIntegrationProof.Save.cs` | existing proof-mode partial class; the dead N5 fields live here |
| Null-HUD unit test + engine-seam stand-ins | `/srv/workspace/last-animal/skeleton/tests/save/` | existing headless save test project (`LastAnimalSaveTests.csproj`) |
| Evidence file | `/srv/workspace/last-animal/.audits/202609262100-n5n6n7/p2-fix-evidence.md` | the run's named evidence path, inside the existing audit dir |
| Verdict files, DONE.md, CYCLES.md, scratch | `/home/svarkor/last-animal/.audits/202609271343-0d130992/` (scratch under its `.tmp/`) | the dod-loop out dir named by the task gate |

No new sibling dirs are invented; no file lands outside the project tree, the existing audit
dirs, or the out dir.

## docs/ARCHITECTURE.md

`test -e /srv/workspace/last-animal/docs/ARCHITECTURE.md` → exists. It already documents
`SaveLoadController.cs` (line 47) and `RuntimeIntegrationProof.Save.cs` (lines 92-93). This run
adds no module, entrypoint, port or data store — a proof-mode assert and a test project compile
list are below the doc's granularity — so no doc update is required; the closing ARCH phase
re-derives this from the tree.

# VERDICT: PASS
