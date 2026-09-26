# ARCHITECT OPENING — placement plan, MC 1388 bridge_mvp gate fix (run 202609262020-9cdd18c5)

Project: `last-animal` — one canonical tree at `/srv/workspace/last-animal/` (git repo at root,
HEAD abbbffb). `/home/svarkor/last-animal/` resolves to the SAME git tree (identical .git inode,
verified `stat -c '%d:%i'` → 2049:1888615 for both). No sibling project dirs may be created.

## Placement plan (layout v2)

| Artifact | Path | Why |
|---|---|---|
| Code fix | `/srv/workspace/last-animal/skeleton/ci_proofs/BridgeMvpProof.cs` (and, only if the root cause says so, `skeleton/world/CompanionFollowBody.cs` / `skeleton/src/companion/*`) | The defect lives in the project tree; fixes originate in the source repo only. |
| Evidence file | `/srv/workspace/last-animal/.audits/202609261400-bridgefix/evidence.md` | Matches layout v2 `.audits/<YYYYMMDD-HHMM>-<slug>/`; the task brief names this exact path. |
| Run-dir verdicts | `/home/svarkor/last-animal/.audits/202609262020-9cdd18c5/{TEST,DA,ARCH}-verdict*.md` | The [dod-loop] line names this out dir; one new file per cycle, never edited. |
| Run-dir bookkeeping | same dir: `DONE.md`, `CYCLES.md`, `ARCH-opening.md`, pytest file | Same out dir. |
| Scratch | `.audits/202609262020-9cdd18c5/.tmp/` | Never a deliverable; keeps the git tree clean. |

## docs/ARCHITECTURE.md status

- EXISTS (119 lines, VERIFIED via `test -e`).
- Already documents `world/CompanionFollowBody.cs` (line 46), `src/companion/` (line 29),
  `bridge_mvp_test.sh` (line 85) and `BridgeMvpProof.cs` (line 91).
- The planned fix is a proof-scenario correction, not a module/entrypoint/deps/port/data-store
  change → no architecture-doc drift is expected; the closing ARCH verdict must re-derive the
  tree and confirm the doc still matches (it names the files, not the assertion thresholds).

## Layout-v2 violation risks checked

- No `last-animal-audit-<date>/` or `svarkor-last-animal-<task>/` siblings are created — all
  run artifacts go under the existing `.audits/<run>/` dirs. PASS.
- Evidence dir `202609261400-bridgefix` follows the `<YYYYMMDD-HHMM>-<slug>` convention. PASS.

## Note on fan-out

This session runs at subagent depth 1; the harness rejects `subagent` spawns
(`maxDepth 1`). The phase loop is therefore executed inline, in phase order, with each
phase's discipline and verdict file produced by a separate, independently-run pass.
