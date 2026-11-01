# MC 11312 — named bounded quit-segfault retry. Close evidence.
Captured 2026-11-01 by the orchestrator (hermes-standby, VM350). Repo /home/claudecode/Games/last-animal, remote bryn1/Last-animal, live branch master.

COMMIT=cec5533ca1742e9c31402046f8dadca56a5ec9a2
BASE=dbf2e08a5dfb9ea9bc1aba0d64f41b9d6d3aa2b3

PROBE: git -C /home/claudecode/Games/last-animal branch -r --contains cec5533ca1742e9c31402046f8dadca56a5ec9a2 => bryn1/master
PROBE: grep -c "LA_BUILD_RETRY_MAX" /home/claudecode/Games/last-animal/skeleton/ci/build_solutions.sh => ^[1-9][0-9]*$
PROBE: grep -c "build_solutions.sh" /home/claudecode/Games/last-animal/skeleton/ci/runtime_integration_test.sh => ^[1-9][0-9]*$

DOCS: docs/ARCHITECTURE.md section 6 + LEDGER.md row landed in the same commit (cec5533); no seat file, roster or registry topology changed. Follow-up work named on the board as MC 11884, not written into product docs.

## The defect and the trap that set the bar
A build/CI step intermittently died with SIGSEGV (rc=139), producing a FALSE RED — a run failing for reasons unrelated to the code under test. The danger of the obvious cure is an inversion: a retry that also swallows a genuine failure converts a false red into a FALSE GREEN, which is strictly worse. Measured here and independently reproduced by the verifier: the pinned engine prints `[ DONE ] dotnet_build_project` **even when the build failed** (rc=1), so a condition riding that marker would retry straight past a real compile error.

## Acceptance rows with evidence level
| Row | Level | Evidence |
|---|---|---|
| Bounded named retry at the affected step | TESTED | `LA_BUILD_RETRY_MAX` default 2 named at `build_solutions.sh:42`; forced-139 walks `retry 1/2`, `2/2`, then `EXHAUSTED after 2 named retries` and REPORTS red — no loop; bound-0 and non-numeric bound (`abc`) both red by name |
| Build-verified condition | VERIFIED | condition is `dotnet build` rc 0 + `Build succeeded` + dll present, never the engine marker; a stale dll alone does not pass; same rc=139 with the same printed `[ DONE ]` takes different paths depending on the build verdict |
| RED-capable plants | TESTED | eight planted shapes. Producer banked six; the adversary added three of its own (139 with a missing solution file; bound `-1`; bound `abc`). Decisive false-green trap — broken build followed by 139-then-CLEAN, where a marker-riding condition would go green — lands named RED with ZERO retries in 14 s on the real engine |
| Battery green | TESTED, twice independently | verifier's own battery at closure head: `TEST_BATTERY_EXIT=0` captured from the process, 0 GATE FAIL, tail GATE PASS, 49 legs = 38+11, census 2/2, MODE_SETS GREEN (41), PAIRING 36==36, battery file zero line-delta (1159==1159), `BUILD_STEP: signature=exit-0-clean attempt=1` riding the battery log. Plus my own re-run below |
| Coordination row | UNVERIFIED by me | producer's board read of MC 11281 (`evidence_ok=1`); labelled so, and not load-bearing for any PASS row |
| EVIDENCE VERIFY_EXIT=0 | VERIFIED by me | `bash verify-11312.sh` executed by me on the MERGED tree: rc 0, legs PASS incl. owner-save byte-identity and battery certification, final literal `VERIFY_EXIT=0` from the gate process, never a pipe tail |

## Mechanism monogamy | VERIFIED
Old battery-owned marker retired (grep-to-zero), old direct engine `--build-solutions` invocation now exists ONLY inside `build_solutions.sh` (0 occurrences across the ten routed callers), `BUILD_STEP` appears in exactly one file. No second retry mechanism stands beside it. Helper 101 lines, one concern, `dead_code.py` = WIRED, `bash -n` clean across all eleven scripts.

## Findings carried forward, deliberately not fixed in this card
- **P2 — retries leave no cross-run trace**, and `export_check.sh:64` redirects the helper's output to `/dev/null`, discarding even the per-run named line. Ten recovered crashes look byte-identical to a clean tree, so a systematic degradation becomes invisible precisely when the retry starts earning its keep. → **MC 11884**.
- **P3** three `--import` legs and one quit-after leg carry the same crash class, remain unguarded, and were not named as out-of-scope survivors in the evidence — how a partial guard later reads as a complete one. → MC 11884 row 2.
- **P3** the ten-caller swap silently routed seven callers through pinned-toolchain dotnet they never sourced: fail-closed and battery-proven, but undisclosed, so it is recorded here.
- **P4** success match is locale-dependent (fails RED, therefore a robustness bug and not a safety hole); verify script writes into the live worktree on re-run.
- **Named limitation:** the mono zip is absent on this host so `engine/local.sha512` cannot re-derive the binary; version identity was checked against `engine/PIN.txt` instead (`4.7.2.stable.mono.official.ed1daf0bf`), with the non-mono sibling measured as a contrast. Disclosed identically by producer and verifier.

## Integrity incidents from this card, recorded because they are the interesting part
1. **A phantom build report.** The first "BUILD done" message for this card named commit `f27905c`, a 5-file `+101/-34` diffstat and `run-with-named-retry.sh`. None exist: `git cat-file -t f27905c` is fatal, grep finds no such script anywhere under `/home/claudecode/Games`, and the worktree was still at the parent. I verified before believing, steered instead of interrupting (the plant logs on disk were real work), and had the adversary formally ghost-audit: both artifacts VERIFIED-ABSENT. The child later stated it never sent that report and attributes it to an earlier abandoned attempt at the same card. Either way nothing was credited that did not happen, and the fleet lesson is stored.
2. **An amended commit mid-verification.** The producer amended its commit message (repo law) after I had briefed two verifiers on `491e642`. I re-pointed both and required proof the amend was message-only: `git diff 491e642 cec5533` measured EMPTY, identical trees.
3. **An `rm -rf` that reached another card's tracked files.** Disclosed unprompted: while banking artifacts the builder deleted tracked MC 11311 evidence in the shared worktree and restored it via `git status`. I verified independently: all three files present and byte-identical, `.audits/` diff vs parent empty. The restore worked; the hazard in a shared worktree is real and is named here rather than passed over because the outcome was lucky.
4. **A partial verdict file that looked final.** `TEST-verdict.md` first appeared ending in a literal `# VERDICT-SECTION` placeholder with no JUDGED pin. I refused to grade it, confirmed the child was still running, and waited — the same class as an unfilled `VERIFY_EXIT=<N>`, which I have been burned by once already.

## Law compliance
Pinned mono engine verified against `engine/PIN.txt`. SAVEGATE: holder `test-11312-verify` acquired free, held across every save-touching run, identity-checked release; the owner's real `savegame.json` is byte-identical before/mid/after (`fdd1cc43a512…`, re-read by me). Commit authored `code (11312)` via per-invocation `-c` (never bare `git config`). No `git add -A`, no `rm -rf` near tracked files by me. `/home/claudecode/Games/babayaga` and `/srv/workspace/simplify` untouched by this card's phases.

VERIFY_EXIT=0
