# ARCH opening — placement plan: NuGet restore portability (MC 1405 follow-up)

NOTE (honesty): this phase was executed INLINE by the parent session, not by a spawned
`design` child — the harness rejected the spawn (`subagent depth 2 exceeds maxDepth 1`:
this session is itself a depth-1 child and cannot spawn children). The plan was still
written BEFORE any file in the project tree was touched, per the gate's ordering rule.

## Placement plan (layout v2, workspace-convention)

Repo: `/srv/workspace/last-animal/` (git tree at root, HEAD f519921, clean).

| File | Action | Why there |
|---|---|---|
| `skeleton/NuGet.Config` | EDIT (existing) | The defect lives here; config file, exempt from line ceilings. |
| `animation_pipeline/NuGet.Config` | EDIT (existing) | Same defect, retired-seat path; config file. |
| `skeleton/docs/build-and-run.md` | EDIT (existing) | The task brief says `docs/build-and-run.md`, but the real doc lives at `skeleton/docs/build-and-run.md` (verified: `docs/` holds only ARCHITECTURE.md). The "Building on Windows from source" subsection goes beside the existing "Building from source (Linux host)" section. |
| `.audits/202609271500-nuget-portable/evidence.md` | CREATE | Run evidence per layout v2 (`.audits/<YYYYMMDD-HHMM>-<slug>/`), chmod 644. |
| out dir `/home/svarkor/last-animal/.audits/202609280811-f8117fe8/` | CREATE (already created) | Named by the [dod-loop] line; holds CYCLES.md, DONE.md, verdict files; scratch under its `.tmp/`. |

## docs/ARCHITECTURE.md status

`/srv/workspace/last-animal/docs/ARCHITECTURE.md` EXISTS (read this session). It describes
`skeleton/`, `engine/`, `animation_pipeline/`, `docs/`, `.audits/`. This change removes a
local NuGet feed from two config files and adds a docs subsection — it changes NO module,
entrypoint, port or data store. The only architecture-adjacent fact is that NuGet restore
now relies solely on nuget.org plus the exact `Godot.NET.Sdk/4.7.2` pin; that is a config
detail documented in the NuGet.Config comments and build-and-run.md, not a module/dep
change. ARCHITECTURE.md needs NO content change; the closing verdict will re-derive the
tree to confirm it still matches.

## Hygiene risks

- Both NuGet.Config files are config → exempt from the 250/400-line ceilings.
- build-and-run.md is 155 lines; one short subsection keeps it well under the ~300-line
  doc split guidance.
- No new source files, so no ceiling concerns.
- The out dir lives in the SECOND clone at `/home/svarkor/last-animal/` (same repo, same
  HEAD); its git tree must not gain untracked run files — the out dir will be committed
  there at the end, scratch stays under its `.tmp/`.

# VERDICT: PASS
