# TEST verdict — MC 11312 named bounded quit-segfault retry

Graded commit: `cec5533ca1742e9c31402046f8dadca56a5ec9a2` (branch `la/11312`), parent `dbf2e08a5dfb9ea9bc1aba0d64f41b9d6d3aa2b3`, worktree `/home/claudecode/Games/la-work/11312`.
Card acceptance, verbatim: *"bounded named retry at the affected step with build-verified condition + RED-capable plants + battery green + coordination row + EVIDENCE VERIFY_EXIT=0"*.

**HEADLINE — the retry cannot swallow a genuine failure.** A real compile error riding a forced-139 with the engine's `[ DONE ]` trap line printed anyway goes named-RED in ZERO retries (plant P6a); the decisive false-green trap — broken build + 139-then-CLEAN sequence, where a marker-riding condition WOULD have retried onto the clean exit and gone GREEN — also goes named-RED in ZERO retries (plant P6b); a real build failure through the REAL pinned engine exits rc 1 (`[ DONE ]` prints anyway — independently reproduced), RED by name in 14 s, zero retries (P6c). The central risk is closed on planted faults, not read off the source.

The originally briefed sha `491e642` was SUPERSEDED before grading: the producer amended the commit
message before close (repo amend-before-push law; orchestrator STOP-message 2026-11-01). I measured
`git diff 491e642 cec5533` → **empty (identical trees, same parent)**; every gate below ran against
`cec5533` content (scratch trees built with `git archive cec5533`).

Scratch: everything under `out/.tmp/verifier/` (probe copy + battery copy, both `git archive cec5533`
+ `.godot/imported`; wrappers under `wrap/`; transcripts under `logs/P*.log`). Tracked tree untouched:
`git status --porcelain` shows only the pre-existing `?? engine/` symlink — TRACKED_CLEAN before and
after. No edits, commits, pushes or merges made by this session.

## Row 1 — a real build failure still goes RED, and is NOT retried — VERIFIED (my own runs)

**P6c, real pinned mono engine, planted genuine compile error** (garbage line appended to
`<Compile Include>`-scope `src/dna/DnaLanguage.cs` in the scratch copy; plant line `this is not
valid C#;`, CS1001×2+CS1003+CS1040+CS1041+CS1513+CS1514 = 9 errors):
- engine build step exited **rc=1** — and my transcript independently confirms the disclosed trap:
  the engine printed `[ DONE ] dotnet_build_project` **on the failed build** (`logs/P6c-realengine-brokenplant.log`).
- helper: `BUILD_STEP: signature=nonzero-rc1 (NOT the quit-139 class, NOT retried) attempt=1` +
  `GATE FAIL: … non-zero rc is never swallowed (MC 11243 F5)`; helper exit **1**.
- retries attempted: **ZERO** (`grep -c retry` on the transcript = 0).
- speed: **14 s** end-to-end (real engine startup + failed compile included). No retry loop, no swallow.
- plant restored **byte-exact** (`cp -a` back, sha256 `f5754353…` re-verified).

**P6a, 139 RIDING the same broken build** (forced-139 wrapper, healthy-looking `[ DONE ]` line printed
by the wrapper): `BUILD_STEP: signature=exit-139-build-NOT-landed (REAL build failure riding a 139 —
NOT retried, NOT masked) attempt=1`, rc 1, ZERO retries, dotnet CS-error lines echoed to stderr
(`logs/P6a-139-realbuildfail.log`).

**P6b — THE false-green trap plant**: same broken build, FLAKE wrapper (139, 139, then **clean rc 0**).
A marker-riding condition would have retried onto the clean exit and gone GREEN over a broken assembly.
Observed: `exit-139-build-NOT-landed … NOT retried, NOT masked`, rc **1**, zero retries
(`logs/P6b-139-flake-realbuildfail.log`). The retry **cannot convert a false RED into a false GREEN** —
verified on a planted fault whose green path is a clean rc 0.

## Row 2 — the bound is real, named, finite — VERIFIED

- Code, `build_solutions.sh:42`: `RETRY_MAX="${LA_BUILD_RETRY_MAX:-2}"` — NAMED env constant
  `LA_BUILD_RETRY_MAX`, value **2**; named on every output line (`(bound $RETRY_MAX …)`); validation at
  :43 (non-negative integer or named GATE FAIL — my plant **P8** `LA_BUILD_RETRY_MAX=abc` →
  `GATE FAIL: LA_BUILD_RETRY_MAX must be a non-negative integer, got 'abc'`, rc 1).
- **P1** forced-139 forever, healthy build (`logs/P1-exhaust.log`): attempt 1 → `quit-segfault retry 1/2`,
  attempt 2 → `quit-segfault retry 2/2`, attempt 3 → `retry EXHAUSTED after 2 named retries (bound 2
  reached; giving up, NOT looping) attempts=3` + `GATE FAIL`, rc **1**. Exactly 3 attempts, then RED —
  not a loop, not a success. 8 s.
- **P2** `LA_BUILD_RETRY_MAX=0` boundary (`logs/P2-bound0.log`): `EXHAUSTED after 0 named retries …
  attempts=1`, rc 1 — boundary behaves, N counts retries AFTER the first attempt (total attempts N+1;
  producer's stated reading confirmed by measurement).
- **P3** flake 139×2-then-clean, bound 2 (`logs/P3-flake.log`): 2 named retries, then
  `exit-0-clean attempt=3`, rc **0** — recovery inside the bound works (my wrapper skipped the
  toolchain `--version` probe per the producer's disclosed mis-arm trap).

## Row 3 — the condition is build-verified, not marker-verified — VERIFIED

Read of `build_solutions.sh:50-93`: the ONLY retry gate is `build_landed_p` — a **re-run of
`dotnet build "$SLN" -c Debug`** requiring rc 0 AND the literal `Build succeeded` AND the assembly on
disk (`LastAnimalPreflight.dll`). The engine's `[ DONE ]` marker appears ONLY in the header comment
(:16-24) naming why it is NOT used; no code path reads it. My P6c run above independently reproduced
the disclosed `[ DONE ]`-on-failure behaviour (my transcript, not the producer's).

DIFFERENT PATHS, same rc=139, same printed `[ DONE ]` line — the discriminator is only the dotnet verdict:
- healthy build + 139 (P1): `quit-segfault-rc139-build-verified` → named retries → exhaustion RED.
- broken build + 139 (P6a/P6b): `exit-139-build-NOT-landed` → immediate RED, zero retries.

Negative strength: P6a/P6b also show the stale-assembly hole is closed — `build-verify FAILED
(… marker=no dll=present)`: the DLL left by the PREVIOUS healthy build did not rescue a failing verify
(rc + `Build succeeded` marker both required, not file existence alone). rc=1 with the DONE marker
printed (P4, `logs/P4-rc1.log`) → `nonzero-rc1 … NOT retried` rc 1. rc=124 (P5, `logs/P5-rc124.log`) →
`budget-kill-rc124 (BUDGET-DIED, NOT retried)` rc 1. Green is reachable ONLY through engine rc 0
(P7, real engine healthy tree → `exit-0-clean attempt=1`, rc 0, 19 s).

## Row 4 — battery green at THIS closure head, by my own run — VERIFIED (my run, rc from the process)

My run: `out/.tmp/verifier/logs/battery-test-run.log`, driver `run-battery.sh` — `bash ci/runtime_integration_test.sh <scratch battery copy of cec5533>`, rc captured from the process (`$?` written to the log; NEVER a pipe tail). Isolated HOME/XDG (`home-battery/`); savegate held by me across the whole run (§6). Started 11:57:06Z, ended 12:38:5xZ, **2507 s wall**. The banked producer `logs/battery-cert.log` was NOT used as evidence. (Disclosed driver history: a first driver launch died on a MISSING-GODOT gate REFUSAL rc=1 within seconds — my driver bug, never exported GODOT; named in the log header, re-launched correctly. The refusal itself is the F5 law working, not a battery verdict.)

Measured in MY log: run_mode legs **49 = 38 pass + 11 fail** (matches the head's anchored count — no mode, no leg added); **`GATE FAIL` count 0**; tail line `RUNTIME_INTEGRATION_TEST: GATE PASS — authoritative runtime path verified (positive green; no_bus/…/save round-trip green; …)`; `FALL_RECOVERED_CENSUS 2/2 OK`; `MODE_SETS_CHECK: GREEN — 5 dispatching proofs (RuntimeIntegrationProof=41 …)`; `PAIRING OK — 36 stage bodies == 36 entry arms`; and the wrapper proven IN the battery: `BUILD_STEP: signature=exit-0-clean attempt=1` at log line 17044 (the battery's build step ran THROUGH `build_solutions.sh` on the real pinned engine). Exit: **`TEST_BATTERY_EXIT=0`**, the literal line written from the process's `$?`.

## Row 5 — one marker, owned by exactly one mechanism — VERIFIED (reading stated)

The DoD line's marker-name pair does not name itself, so the reading I used: the build-step VERDICT
mechanism and its marker. Measured greps across `skeleton/ci/`:
- NEW marker `BUILD_STEP`: **13 hits in exactly ONE file** (`build_solutions.sh`); 0 in every other ci file.
- OLD mechanism (direct `"$GODOT" … --build-solutions --quit-after …` engine invocation): **1 hit in
  exactly ONE file** — `build_solutions.sh:69` (the wrapper's own `step_attempt`); **0 in all ten callers**
  — every one of them (smoke, boot_test, bridge_mvp, main_composition, audio, empathy_book,
  runtime_integration, ui_test, determinism_cmp, export_check) now invokes `build_solutions.sh`.
  (sky_real_mount_probe.sh keeps its MC 11243-sanctioned bare-`dotnet build` route — prior art, not an
  engine-step caller; retarget_bake.sh is a different project under `animation_pipeline/tools/`, named
  out of scope by the card, engine path points at the destroyed vm105 tree — ROT finding.)
- The engine's own `dotnet_build_project` string in `skeleton/ci/`: 2 hits, BOTH comments (the two
  files' headers explaining the trap) — zero code acts on it.
- `BATTERY_EXIT` appears in ZERO ci scripts (it is a runner-written rc line, not a script mechanism).
Exactly one mechanism prints the build-step verdict and every outcome carries it; the old per-caller
inline step is retired, not left beside the new one. Battery-file zero line-delta measured: base 1159 l
== head 1159 l; run_mode anchored count 49 = 38 pass + 11 fail by grep at the head.

## Row 6 — laws — VERIFIED (one named limitation)

- **Pinned mono engine:** vendored binary `engine/Godot_v4.7.2-stable_mono_linux_x86_64/…` (symlink to
  `/home/claudecode/Games/.tools/godot/…`) prints `4.7.2.stable.mono.official.ed1daf0bf` == PIN.txt
  commit `ed1daf0bf`, WITH `.mono` — and I measured the contrast: the non-mono sibling on this host
  prints `4.7.2.stable.official.ed1daf0bf` WITHOUT `.mono`. Every engine run above (P6c, P7, the whole
  battery) executes C# through the mono binary. LIMITATION (named, matching the producer's own
  disclosure): `engine/local.sha512` pins the two distribution ZIPS; the mono zip is ABSENT on disk, so
  the mono binary's integrity cannot be re-derived from the pin at this head — binary identity is
  version-string-verified only. The non-mono zip line I could verify: `editor.zip` sha512 matches the
  non-mono row byte-exact (measured).
- **SAVEGATE:** foreign check FREE at acquire → `mkdir /tmp/la-savegate.lock` +
  `holder-test-mc11312-verify` file (11:48:05Z), held across EVERY save-touching run (the battery),
  released identity-checked at session end with my own holder file only.
- **Isolated HOME/XDG:** battery ran with `HOME=out/.tmp/verifier/home-battery`,
  `XDG_{DATA,CACHE,CONFIG}_HOME` under it (only `/home/claudecode/.nuget` symlinked read-side for the
  dotnet restore — env plumbing, same idiom the producer disclosed). Probe engine runs likewise under a
  scratch HOME.
- **Owner save byte-identical:** `fdd1cc43a5124fee3d12b007f5874f0d9a2e969ab93d3c602e1ca96f9697cd4e` before, mid-battery, and after (before/after sha files `cmp`-identical, `out/.tmp/verifier/logs/owner-save-{before,after}.sha256`) — isolation honoured, never written, no restore needed. Savegate released identity-checked at 12:39Z (holder file listed, contained ONLY `holder-test-mc11312-verify.txt`, own file removed, own lock rmdir'd — `LOCK-RELEASED`, gate FREE after).
- **Untouched:** `/home/claudecode/Games/babayaga`, `/srv/workspace/simplify` — never entered. No secret printed.
- **Hygiene:** helper 101 l (< 250), one concern (the named bounded retry machine;
  concern_check: 0 findings over the ≥400-l files of ci/, no MIXED flags); `bash -n` clean on all 11
  touched scripts. `dead_code.py`: `build_solutions.sh` NOT flagged (WIRED — ten live callers); the 3
  DRIVERLESS entries it printed (`bridge_mvp_test.sh`, `determinism_cmp.sh`, `sky_real_mount_probe.sh`)
  are pre-existing manual/wall entry-points, untouched by this card. No orphan script added.

## Coordination row + EVIDENCE VERIFY_EXIT (acceptance items outside my six rows)

- Coordination row PRESENT (EVIDENCE.md §5; card-mandated; battery-lane owner MC 11281 board-read at
  intake claimed VERIFIED evidence_ok=1 — the board-read itself is the producer's claim, UNVERIFIED by
  me; the battery diff it authorizes is the zero-line-delta swap I measured).
- EVIDENCE.md last line: `VERIFY_EXIT=0`; `verify-run.log` ends `VERIFY_EXIT=0` (producer's re-runnable
  verify; my row-4 battery run is INDEPENDENT of it, captured at closure head by me).

## Evidence levels

Rows 1–6: VERIFIED by my own runs/greps this session (row 4 by MY battery run, rc from the process).
The producer's banked logs were NOT used as evidence for any PASS.

## Findings (none blocking)

- F-N1 (P3, already named by producer, re-confirmed here): engine prints `[ DONE ] dotnet_build_project`
  on a FAILED build (rc 1, no CS lines on stdout) — toolchain honesty gap; the helper correctly refuses
  to ride it; keep as its own future card. `retarget_bake.sh` vm105-path rot + dotnet pin drift
  8.0.130→8.0.133: named by producer, unchanged, out of scope.
- F-N2 (P4): the mono-zip sha512 cannot be re-derived on this host (zip absent); version-string identity
  is the live check. Producer disclosed it; a pin-refresh card would close it.

## What this verdict does NOT cover

- The intermittent quit-segfault itself (low-rate engine behaviour): the retry targets the named class per MC 10209 / MC 11295 finding (2) counts; I verified the MECHANISM's behaviour at the signature, not a recurrence of the flake.
- The producer's board read (MC 11281 evidence_ok=1) — board-side claim, UNVERIFIED here; the artifact row exists and the battery diff it relies on is zero-line as measured by me.
- Mono-zip sha512 re-derivation (zip absent on disk) — F-N2 above.

## Transcripts (all under out/.tmp/verifier/logs/)

P1-exhaust.log · P2-bound0.log · P3-flake.log · P4-rc1.log · P5-rc124.log · P6a-139-realbuildfail.log · P6b-139-flake-realbuildfail.log · P6c-realengine-brokenplant.log · P7-realengine-green.log · P8-badbound.log · battery-test-run.log (with the `TEST_BATTERY_EXIT=0` line) · owner-save-{before,after}.sha256 · dotnet-warm.log. Wrappers under out/.tmp/verifier/wrap/; scratch trees under out/.tmp/verifier/{probe,battery}/ (both `git archive cec5533` + `.godot/imported`).


# JUDGED: d2a0f1e9802d02491d1cd183316da7f1ddd36674db0bbad8c7964bad87af52c0
# VERDICT: PASS
