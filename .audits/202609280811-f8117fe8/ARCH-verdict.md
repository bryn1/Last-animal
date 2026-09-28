# ARCH closing verdict — NuGet restore portability (MC 1405 follow-up)

NOTE (honesty): executed INLINE by the parent session, not by a spawned
`design` child — the harness rejected the spawn (`subagent depth 2 exceeds
maxDepth 1`). All three checks below were re-derived from the artifacts this
session, not from any report.

## 1. docs/ARCHITECTURE.md vs the actual tree

`/srv/workspace/last-animal/docs/ARCHITECTURE.md` exists and was re-read. The
change (commit 61ed5fc) touched two NuGet.Config files and one docs subsection:
NO module, entrypoint, port or data store moved. ARCHITECTURE.md names no
NuGet feed (grep: zero hits), so nothing in it became false. Its §1 layout
table (skeleton/, engine/, animation_pipeline/, docs/, .audits/) still matches
the tree. MATCHES REALITY.

## 2. File hygiene

- Changed source files: none (config + docs only). Config files are exempt
  from the 250/400 ceilings.
- `skeleton/docs/build-and-run.md`: 170 lines — under the ~300 doc-split
  guidance.
- Largest source files re-measured: `skeleton/src/ecosystem/EcosystemSpawner.cs`
  248, `animation_pipeline/BakeCheck.cs` 221, `skeleton/src/combat/EnemyAI.cs`
  205 — all under the soft target. No file over 400.
- No TODO/FIXME added (git diff of 61ed5fc contains none).
- One concern per file holds; no new mechanism added beside an existing one
  (the local feed was REMOVED, not replaced by a second guard — the exact pin
  already existed).

## 3. Layout v2 placement

- Both NuGet.Config edits are in-place at their existing canonical paths.
- The docs subsection is in the existing `skeleton/docs/build-and-run.md`
  (the brief's `docs/build-and-run.md` path does not exist; real path used and
  recorded in the evidence file).
- Evidence at `.audits/202609271500-nuget-portable/evidence.md` (chmod 644);
  out dir + verdicts at `/home/svarkor/last-animal/.audits/202609280811-f8117fe8/`
  (the [dod-loop]-named dir); scratch under its `.tmp/`. No stray sibling dirs
  invented.

## Git tree state

`/srv/workspace/last-animal` HEAD = 61ed5fc, tracked tree clean. The out dir's
tree (`/home/svarkor/last-animal`) is synced to 61ed5fc; run files there are
committed at run end (see DONE.md).

# JUDGED: b0b70e76d5c74470970e2fc1c5cc3668971ef1cc78ba050908e1860fd601c022
# VERDICT: PASS
