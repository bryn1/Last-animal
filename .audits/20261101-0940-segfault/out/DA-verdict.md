# DA-verdict — MC 11312 (adversarial review, devils-advocate seat, 2026-11-01)

Subject: `cec5533ca1742e9c31402046f8dadca56a5ec9a2` (branch `la/11312`, worktree `/home/claudecode/Games/la-work/11312`, parent `dbf2e08`). 13 files, +120/−10 — one new helper `skeleton/ci/build_solutions.sh` (101 l) + ten caller swaps + LEDGER row + ARCHITECTURE row.
Re-point honored: `491e642` is the superseded pre-amend sha. **Amend proven message-only:** `git diff 491e642 cec5533` EMPTY, trees identical (`2239f02` both). All review below is my own measurement this session unless a level says otherwise.

**Gate outcome: SHIP — with a named fix-list (no P0/P1; DoD met on every line). The single verdict line is the last line of this file.**

## Attack 1 — is the false-red/false-green inversion actually prevented? YES, with honest coverage limits.

The dangerous shape is a retry that eats genuine failures. The condition (`build_solutions.sh:72-91`) retries ONLY `rc=139` AND only after `build_landed_p` (line 54) re-runs `dotnet build` and requires the conjunction **rc 0 + "Build succeeded" + DLL on disk** (line 57). Green is only ever reached by the real shipped step returning rc 0 (`step_attempt`, line 76) — a full fresh verdict, never a swallowed one.

The measured premise is real, not asserted: `out/buildstep-realbuildfail.log` (banked, pinned engine) shows a genuine compile error STILL printing `[ DONE ] dotnet_build_project` and exiting rc=1 — the engine DONE names the STEP, not the BUILD. So riding that marker WOULD have been false-green; the design refused it (VERIFIED: my read of the transcript + code comment match, `build_solutions.sh:15-21`).

**What the plants prove vs what they don't:**
- RETRY FIRES WHEN ASKED: plants 1, 2, 5 (forced-139 exhaust, bound-0, flaky→green) — I re-ran all of them MYSELF in scratch (`out/.tmp/da-*.log`, my own independent fake-engine wrapper): exhaust rc=1 with exactly 2 named retries; bound-0 rc=1 with 0; flaky 139×2→clean rc=0 named attempt=3. Matches banked logs exactly. TESTED.
- GENUINE FAILURE SURVIVES: the key question. rc=1 (the rc a real build failure actually returns) → named RED, 0 retries (plant 3, re-run by me). rc=124 budget kill → BUDGET-DIED RED, 0 retries (plant 4, re-run by me). And plant 6 — **139 riding a real compile error** (garbage in a `<Compile Include>`'d file, real `dotnet build` in the loop): RED by name `exit-139-build-NOT-landed`, ZERO retries, with `dll=present` still RED (banked log proves the DLL leg alone cannot pass it). Not-mock: the verify uses the real dotnet against the real csproj. TESTED — this is the discrimination, not just the trigger.
- I ADDED two shapes the producer never banked (scratch, this session): 139 + **missing solution file** (verify fails on tool rc, DLL stale-present) → named RED zero retries; `LA_BUILD_RETRY_MAX` = `-1` and `abc` → guard RED before any attempt. Both fail-closed. TESTED.
- NOT proven by plants (say it plainly): that `rc=0-clean` can never lie about the build — that is the engine's own contract; it is banked as measured at THIS pinned binary (realbuildfail transcript: build failure ⇒ rc 1, never 0) and held through the full battery. INFERRED-from-transcript, acceptable.
- A real-world 139 recovery had NEVER been observed on disk before my run — all banked gate/battery logs show `attempt=1`. **My own scratch green run caught a LIVE one**: after 8 recompile-heavy plant runs, attempt 1 segfaulted (the exact named "when it also recompiles" condition), the helper printed `build-verified` + `quit-segfault retry 1/2`, attempt 2 exited 0 (`out/.tmp/da-green.log:23,24,42`), total wall 19.8 s. The class and the cure are both real. VERIFIED-live (in scratch, not the shipped tree).

## Attack 2 — granularity and cost. Retry belongs exactly where it is.

Where the 139 occurs: at engine QUIT teardown, AFTER the build verdict has been computed internally. The verdict drives rc only when the process exits normally (`buildstep-realbuildfail.log` shows rc=1 coming from the build callback "Aborting" path); the crash overwrites the exit code with 139 and the verdict is lost to the shell. The remedy therefore is verdict RECOVERY (a dotnet re-run) at the crashing step — which is what `build_landed_p` is. The brief's alternative "classify a post-verdict teardown crash distinctly" is effectively implemented: 139-with-verdict = retryable class; 139-without-verdict = distinct named RED, zero retries (line 90).

Measured cost (my own timing, warm tree, scratch): one full engine step **≈8–20 s**; one build-verify dotnet build **2.4 s**. One retry ≈ 10–25 s; worst bound-2 exhaustion ≈ 70 s — inside a ~40-min battery (log-size/mtime envelope, INFERRED), NOT a battery re-run. Pre-card remedies were strictly worse: the whole battery leg died (LEDGER 11295 finding (2): "the shipped battery/ui steps hard-fail on it"), and the documented ad-hoc fix was `.godot` wipe + full re-import (LEDGER 11296/11297 rows cite it). Wrong-shape cure rejected: this does NOT re-run a 15-minute battery to recover a teardown crash.

## Attack 3 — silence absorption. Real, classified P2, with a minimal fix.

Every retry IS named on stdout (`quit-segfault retry k/N`, every outcome prints its `BUILD_STEP: signature=...`), and those lines land in caller logs (battery-cert.log:20, my scratch logs) — per-run visibility exists at 9 of 10 callers. But:
- `export_check.sh:64` sends helper output to `/dev/null` — there, a silent recovery leaves NO trace at all.
- Nothing records a retry ACROSS runs. No counter, no marker file, no tally line. Ten flakes that all recover on attempt 2 look identical to a clean tree forever; the flake rate becomes unmeasurable precisely because the gate goes green.
- Classification: **P2** (reliability/observability, not correctness — exhaustion still REDs, and a systematically worsening flake will eventually out-run the bound and scream). DoD did not ask for telemetry, and the repo has no CI log store to point it at; requiring one here would invent a mechanism beside the run-logs that already carry the named lines. Minimal record for a follow-up card: a one-line per-run tally (helper or caller prints `QUIT_SEGFAULT_RETRIES=<n>` once; verify/battery tail echoes it; grep-able from the banked log) + stop `/dev/null`-ing the helper at export_check.

## Attack 4 — mechanism monogamy. One mechanism; the retired marker is gone; swaps honest with one named exception.

- Routed: all ten `skeleton/ci` engine-build-step callers go through the helper (each a 2-line swap, uniform `GODOT="$GODOT" bash "$HERE/build_solutions.sh" "$PROJ" <N>`, `HERE` defined at every site — I checked all ten). Grep-to-zero re-run by me: no direct `timeout …--build-solutions --quit-after` remains anywhere in `skeleton/ci` outside the helper itself.
- One helper only: `git grep retry` across the tree returns the helper + `sky_real_mount_probe.sh`, and the probe is a REPLACEMENT route (dotnet route, sanctioned prior art, no retry loop — its "settle-retry" print is informational, :77) — not a second retry mechanism.
- `retarget_bake.sh:34` still calls the engine step directly: exclusion JUSTIFIED, both claims verified — its engine path hard-codes the destroyed vm105 tree (`test -e /srv/workspace/svarkor-last-animal-phase2` → GONE, :21) and its target is a different bake-harness project (`BakeCheck.cs`, not LastAnimalPreflight; the helper's SLN/DLL are pinned by design).
- Marker ownership: nothing anywhere now greps `dotnet_build_project` as a success condition (only comments cite it); the `BUILD_STEP:` signature exists solely in the helper and its run logs; the battery file has zero line-delta beyond the swap — so the "marker move" left no retired second marker. VERIFIED by grep, not trust.
- **Named semantic exception (F-DA3, P3):** at the parent, only boot_test/smoke/audio referenced toolchain.sh; battery, ui_test, export_check, bridge_mvp, determinism_cmp, empathy_book, main_composition did NOT. The helper sources toolchain.sh, so for those SEVEN callers the engine's shelled-out dotnet is now the PINNED one (`toolchain.sh:22-27` PATH-prepends `/usr/lib/dotnet`). Direction: toward the toolchain's own stated purpose; fail-closed (no dotnet → WARNING + RED); battery-cert ran green under the new wiring (log line 2 `TOOLCHAIN:` printed). Not 100% "semantically equivalent swap" — true, one-directional, and undisclosed — record it in the LEDGER row; not a blocker.

## Attack 5 — ghost audit. Nothing ghostly resolves as present; nothing present resolves as ghost.

- `f27905c`: absent — `git cat-file -t` fatal; `git grep` rc=1 (zero hits) at `cec5533`. `run-with-named-retry.sh`: absent, zero hits tree-wide. No file, no ref, no claim credits them. VERIFIED-absent (ran the instrument that would show presence).
- The prior false report's "5-file +101/−34" exists nowhere; every current artifact (LEDGER row, BUILD-result, EVIDENCE §7) says 13 files +120/−10 and that matches `git show --stat` exactly.
- The `VERIFY_EXIT=<N>` placeholder: current `EVIDENCE.md` contains none; its LAST line is the literal `VERIFY_EXIT=0`, and `out/verify-run.log` (11:19, same tree content) ends `VERIFY_EXIT=0` with every leg PASS. VERIFIED.
- MC 11311 sweep incident: tree at `cec5533` vs `dbf2e08` on `.audits/` — **empty diff**; all three files present at cec5533 (`EVIDENCE.md` + 2 PNGs); worktree status clean. Restore verified by me. BUT name the hazard (F-DA6, P3, process not commit): `.gitignore:21` ignores `.audits/` while TRACKED files live inside it — any "clean ignored paths" `rm -rf` pass will sweep them again, and the next one may not be caught. Doctrine line recommended (use `git clean -nX` or restore-and-verify like this one was).
- Claims I cannot resolve from this seat (honest list): (a) the MC 11281 board read (`evidence_ok=1`) — board is not my surface, UNVERIFIED here, knut/parent check it; (b) the battery cert's PROCESS rc — the `BATTERY_EXIT=0` line is at log end with tail GATE PASS and 0 `GATE FAIL` lines (all verified on disk), but the historical rc itself is INFERRED from the line's shape — correctly weighted per the parent's note, disclosure stands; (c) MC 11295's "4x vs 0x" raw counts (history, cited verbatim from tracked LEDGER — quotes verified present, counts not re-derivable); (d) savegate lease pids (historical; the banked owner-save sha matches `owner-save-before.sha256` byte-for-byte).

## DoD, line by line (my levels)

| Acceptance line | Level | Evidence |
|---|---|---|
| bounded named retry at the affected step | VERIFIED | helper on disk; named lines; bound default 2; my 5 re-runs + 3 new plants; battery/gate logs show routing |
| build-verified condition | VERIFIED | conjunction rc+marker+dll (`:57`); engine-marker dishonesty measured; RED unmasked even with dll=present |
| RED-capable plants | TESTED | 6 banked + re-run by me in scratch with an independent wrapper; 8 total RED outcomes observed this session |
| battery green | VERIFIED (banked) / rc INFERRED | battery-cert.log: BATTERY_EXIT=0 line, 0 GATE FAIL, tail GATE PASS, census 2/2, 41 modes, 36==36, run_mode 49 |
| coordination row | VERIFIED (row exists) / board-read UNVERIFIED here | EVIDENCE §5 + LEDGER row; board check belongs to knut/parent |
| EVIDENCE VERIFY_EXIT=0 | VERIFIED | EVIDENCE.md last line; verify-run.log ends VERIFY_EXIT=0 |

## Findings (for the fix-list / future cards; none block the merge)

- **F-DA1 P2** — no cross-run flake telemetry; `export_check.sh:64` also erases the per-run trace (`>/dev/null`). Minimal: per-run `QUIT_SEGFAULT_RETRIES=<n>` tally line + keep the helper's signature visible at export_check.
- **F-DA2 P3** — the class was FIRST named on an `--import` run (MC 10209 verbatim, inside EVIDENCE §1), and `--import` legs (runtime_integration_test.sh:72, smoke.sh:32, determinism_cmp.sh:125) plus smoke's `--quit-after 3` load leg (smoke.sh:50) remain unguarded and can still false-RED on the same teardown crash. Scoping to the build step is defensible (the remedy shape for import is wipe+re-import), but the evidence named only retarget_bake/sky-probe as out-of-scope survivors — this surface was not named. One card should.
- **F-DA3 P3** — swap silently added pinned-dotnet toolchain wiring to seven callers (above). Record the delta; it is fail-closed and battery-proven.
- **F-DA4 P4** — `build_landed_p` marker match `*"Build succeeded"*` is locale-dependent (dotnet honors DOTNET_CLI_UI_LANGUAGE); a non-English-locale seat gets a false-RED (fail-closed, never false-green).
- **F-DA5 P4** — `verify-11312.sh` re-runs write into the live worktree (transient garbage in a tracked file + engine/build artifacts in-tree); proven restored, but concurrent sessions on that worktree would race. Note in the script header or run on a copy.
- **F-DA6 P3 (process)** — the `.gitignore` `.audits/` + tracked-files-inside-it trap that produced the disclosed rm-rf sweep; add the doctrine line before the next seat repeats it.

## What this change actually buys, measured

Pre-card, one in-gate teardown flake = a hard-failed gate leg and a manual wipe/re-import or full battery re-run (LEDGER 11295/11296/11297 rows). Post-card: ≈10–25 s named retry with the verdict re-proved, exhaustion still loud, real failures unmasked (observed live by me recovering a genuine 139 in scratch at 19.8 s total). The inversion the class threatened (good tree, red gate) is closed at the right granularity; the inversion this fix could have introduced (bad tree, green gate) is closed by the conjunction and demonstrated RED by ten plant outcomes across two independent operators.

# JUDGED: d2a0f1e9802d02491d1cd183316da7f1ddd36674db0bbad8c7964bad87af52c0
# VERDICT: SHIP
