# BUILD-result — MC 11312 (code profile, VM350, 2026-11-01)

**BUILD = committed.** Branch `la/11312` in worktree `/home/claudecode/Games/la-work/11312` (off live master `dbf2e08`, `ls-remote origin master` == base at intake AND card end). **Commit `cec5533ca1742e9c31402046f8dadca56a5ec9a2`, parent `dbf2e08a5dfb9ea9bc1aba0d64f41b9d6d3aa2b3`** (both read back with `git rev-parse HEAD HEAD^`; author `code (11312) <code@agent-town.local>` via `-c` flags only; the first commit 491e642 ran body into subject and was AMENDED per the repo amend-before-push law — the sha above is final). NOT pushed, NOT merged (merge is the orchestrator's, per card law). Master untouched. One commit — evidence stays OUT of git (repo `.gitignore` ignores `.audits/`); full evidence in this out dir.

## Acceptance line-by-line

| DoD leg | evidence | level |
|---|---|---|
| bounded named retry at the affected step | `skeleton/ci/build_solutions.sh` (101 l) wired into ALL TEN ci engine-build-step callers (battery, smoke, boot, bridge, main-comp, audio, empathy, ui, determinism, export-check); retries only exit 139, named `quit-segfault retry k/N`, bound `LA_BUILD_RETRY_MAX` (default 2) after the first attempt, exhaustion RED by name | VERIFIED (ran in battery/ui/determinism/export logs; plants) |
| build-verified condition | `dotnet build` re-run rc 0 + “Build succeeded” + dll present — chosen because MEASURED: the engine’s `[ DONE ] dotnet_build_project` prints even on a FAILED build (rc 1) (out/buildstep-realbuildfail.log) | VERIFIED |
| RED-capable plants | 6 plants banked out/plant-*.log + re-run by verify: exhaust-at-bound-2, bound-0 boundary, rc1 never retried, budget-124 BUDGET-DIED, flaky-139x2→green attempt=3 (bounded recovery), **139 riding a REAL compile error → RED immediately named, zero retries (no masking)** | VERIFIED (verify legs) |
| battery green | certification run: `BATTERY_EXIT=0` (from the process), 0 `GATE FAIL` lines, tail `GATE PASS`, `FALL_RECOVERED_CENSUS 2/2`, `MODE_SETS_CHECK: GREEN` (41 modes, no mode added), `PAIRING 36==36`, run_mode 49 = 38 + 11 unchanged (out/logs/battery-cert.log); + ui_test A–L rc=0, boot/smoke/empathy/audio/bridge/main-comp rc=0, determinism IDENTICAL, export Windows full-game rc=0 (156 MB PE) — every touched caller ran | VERIFIED |
| coordination row | MC 11281 board-read at intake: **status verified, evidence_ok=1** — battery-file lane closed; battery-adjacent edit lawful (MC 11320 precedent); this card’s battery diff is a zero-line-delta swap, no mode, no leg | VERIFIED (`mc-job json 11281` this session) |
| EVIDENCE VERIFY_EXIT=0 | `out/EVIDENCE.md` ends `VERIFY_EXIT=0`; runner: `bash out/verify-11312.sh > out/verify-run.log 2>&1` → process rc 0, every leg PASS, final line `VERIFY_EXIT=0` (out/verify-run.log) — value from the gate process itself, never a pipe tail | VERIFIED |

## Laws honoured

- Engine PIN: `--version` = `4.7.2.stable.mono.official.ed1daf0bf` (mono at pinned commit; non-mono sibling prints WITHOUT `.mono` — banked contrast; every gate executes C# through it). mono zip no longer on disk (disclosed); `editor.zip` sha matches the non-mono local.sha512 line byte-exact.
- SAVEGATE: `/tmp/la-savegate.lock` acquired FREE at intake (`holder-code-11312`, setsid lease pid 849847), held across every engine run, identity-checked release (own holder only, `LOCK-RELEASED`). Isolated HOME/XDG for every engine run. Owner’s `user://savegame.json` sha256 `fdd1cc43a512…` byte-identical before/between/after — never written.
- Fresh-worktree `--import` ran first, rc=0. No push, no merge, no bare `git config`, no secrets printed, no spawn.

## Disclosures (self-caught, all honest)

1. First battery runner didn’t bank the process rc (log showed tail GATE PASS, 0 GATE FAIL); the rc-certification run is the proof (flaw of my runner, not the change).
2. First flaky-plant run was mis-armed (toolchain’s `--version` probe consumed a counter increment); wrapper fixed, plant re-run — both facts in EVIDENCE §3.
3. Mid-banking `rm -rf` of the worktree’s ignored `.audits/` swept the TRACKED MC 11311 evidence; caught immediately via `git status`, restored with `git restore`, tree verified clean against HEAD.
4. Isolated-HOME export needed host `export_templates` (engine resolves under XDG_DATA_HOME; the script’s own check is HOME-based) + host `.nuget` cache symlinked; one `ln`-into-symlinked-dir trap self-cleaned.

## Findings NAMED for future cards (not fixed here — scope law)

- The pinned engine prints `[ DONE ] dotnet_build_project` on a FAILED build (rc 1, zero CS lines on stdout) — a toolchain-honesty gap worth its own card.
- `animation_pipeline/tools/retarget_bake.sh` engine path hard-codes the destroyed vm105 tree (rot; also a different bake-project, hence out of scope here).
- dotnet pin drift: PIN.txt says 8.0.130, measured 8.0.133 (toolchain still accepts the 8.x line).
- Standing `dead_code.py` DRIVERLESS lines for 9 manual/wall-invoked ci entry points (pre-existing).

**Pointer for the orchestrator:** `git -C /home/claudecode/Games/la-work/11312 rev-parse HEAD` → `cec5533ca1742e9c31402046f8dadca56a5ec9a2`; evidence: `/home/claudecode/Games/last-animal/.audits/20261101-0940-segfault/out/EVIDENCE.md`.
