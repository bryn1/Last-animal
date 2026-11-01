# MC 11312 EVIDENCE — named bounded retry for the engine build-step quit-segfault class

Branch `la/11312` @ base/live-master `dbf2e08a5dfb9ea9bc1aba0d64f41b9d6d3aa2b3` (ls-remote == master == the card's named base, VERIFIED at intake and at card end). Game `/home/claudecode/Games/last-animal`, worktree `/home/claudecode/Games/la-work/11312`. Acceptance: *bounded named retry at the affected step with build-verified condition + RED-capable plants + battery green + coordination row + EVIDENCE VERIFY_EXIT=0.*

## 1. The failure signature — identified BEFORE writing (quoted, not guessed)

- **MC 10209 LEDGER row, verbatim:** “ONE --import — all import stages DONE, 6,578 artifacts in .godot/imported, but the engine **SEGFAULTED at quit after the final DONE (exit 139)** — recorded honestly, every battery leg ran green against those artifacts”.
- **MC 11295 finding (2), LEDGER verbatim:** “the engine's --build-solutions --quit-after 1 gate step SEGFAULTS at quit at a LOW rate when the step also RECOMPILES the assembly (exit 139, the quit-after-DONE class MC 10209 named in the LEDGER; observed 4x in this card's worktree, 0x on steady main …); the shipped battery/ui steps hard-fail on it … a card should own the in-gate retry-or-class fix.” ← THIS card.
- **`ci/sky_real_mount_probe.sh` header (prior art, MC 11243):** “the known editor quit-quirk SEGFAULT class on its post-build scan/quit path even when the build itself printed dotnet_build_project [DONE] (the C# compiled)” — the sanctioned dotnet-route workaround for ONE probe; the shipped gate steps hard-failed.
- Class = **exit 139 at engine QUIT, after the build step printed `[ DONE ] dotnet_build_project`, when the step also recompiles; low rate; assembly fine.**

**The measured fact that pinned the condition design (new this card, out/buildstep-realbuildfail.log):** a REAL compile error in a `<Compile Include>`'d file (planted `this is not valid C#;` in `src/dna/DnaLanguage.cs`) makes the engine exit **rc=1** while STILL printing `[ DONE ] dotnet_build_project` and zero CS-error lines on stdout. The engine's DONE names the STEP, not the BUILD — so “assembly actually built” CANNOT ride the engine marker; it rides the build tool: re-run `dotnet build LastAnimalPreflight.sln -c Debug` (the same route MC 11243 sanctioned) and require **rc 0 + “Build succeeded” + `LastAnimalPreflight.dll` present**. Normal-run control: out/buildstep-normal.log (rc 0, marker present); helper green: out/helper-green.log.

## 2. The change

- **NEW `skeleton/ci/build_solutions.sh` (101 l, one concern)** — the shipped engine step VERBATIM (`timeout 300 "$GODOT" --headless --path "$PROJ" --build-solutions --quit-after N`) wrapped in the retry machine:
  - **(a) accepts exit 0** → `BUILD_STEP: signature=exit-0-clean attempt=k` and done.
  - **(b) retries ONLY exit 139**, and ONLY after the build-verified check above passes; each retry NAMED: `quit-segfault retry k/N`. **Bounded:** `LA_BUILD_RETRY_MAX`, default **2** — N = named retries AFTER the first attempt (total attempts N+1; the reading used, stated per the ambiguity rule). Exhaustion prints `retry EXHAUSTED after N named retries (bound N reached; giving up, NOT looping)` + GATE FAIL — it gives up and REPORTS; it never loops.
  - **(b′) 139 with build NOT landed** = a REAL build failure riding the segfault → immediate named hard FAIL, **zero retries** (no masking).
  - **(c) every outcome prints which signature matched.** Budget kills 124/137/143 → `BUDGET-DIED` RED LOUDLY; any other rc → `nonzero-rc<N>` — both **never retried** (MC 11243 F5 “non-zero rc is never swallowed” preserved verbatim in the fail line).
- **All ten `ci/*.sh` engine-build-step callers route through it** (fix once where callers go, not per-caller guards): smoke.sh, boot_test.sh, bridge_mvp_test.sh, main_composition_test.sh, audio_test.sh, empathy_book_test.sh, runtime_integration_test.sh (the battery), ui_test.sh, determinism_cmp.sh, export_check.sh. Each site: 2 lines → 2 lines; battery file **zero line-delta** (1159 l, run_mode count 49 = 38 + 11 untouched — battery zero-diff was NOT expected per card; line-delta is zero anyway).
- **Named out of scope, not touched:** `animation_pipeline/tools/retarget_bake.sh` (DIFFERENT bake-harness project with its own sln — the helper's verify paths are LastAnimalPreflight-pinned by design — AND its engine path hard-codes the destroyed vm105 tree: separate rot finding, filed by name here for the orchestrator to card). `sky_real_mount_probe.sh`'s sanctioned dotnet-build swap stays as prior art. LEDGER prose matches are history.

## 3. RED-capable plants (deterministic throwaway forced-rc wrapper, out/plant-*.log)

| # | input | expected | observed | verdict |
|---|-------|----------|----------|---------|
| 1 | forced 139 forever, bound 2 | retry 1/2, 2/2, EXHAUSTED, rc 1 | exactly 2 retry lines + EXHAUSTED + HELPER_RC=1 | PASS — bound hit and reported, no loop |
| 2 | forced 139, LA_BUILD_RETRY_MAX=0 | 0 retries, named fail, rc 1 | `EXHAUSTED after 0 named retries`, rc 1 | PASS — boundary |
| 3 | rc=1 (the rc a REAL build failure actually returns, §1) | named, 0 retries, rc 1 | `signature=nonzero-rc1 … NOT retried`, rc 1 | PASS |
| 4 | rc=124 budget kill | BUDGET-DIED loud, 0 retries, rc 1 | `signature=budget-kill-rc124 (BUDGET-DIED, NOT retried)`, rc 1 | PASS |
| 5 | FLAKE: 139 twice then clean, bound 2 | 2 named retries then green attempt 3, rc 0 | retry 1/2, retry 2/2, `exit-0-clean attempt=3`, rc 0 | PASS — recovery inside the bound |
| 6 | **139 riding a REAL compile error** (garbage appended to a Compile-scope file) | immediate named RED, 0 retries, rc 1 | `signature=exit-139-build-NOT-landed (REAL build failure riding a 139 — NOT retried, NOT masked)`, rc 1, dotnet CS1001 lines printed, file restored byte-exact | PASS — **the retry cannot hide a red build** |

Self-caught, disclosed: the first run of plant 5 was MIS-ARMED — `toolchain.sh` runs `"$GODOT" --version` once, and the wrapper counted that probe as an attempt (1 retry line instead of 2). Wrapper fixed to skip `--version` probes; plant 5 re-run as tabulated. The wrapper fix, not the mechanism, was at fault; both transcripts exist (the re-run overwrote the mis-armed log at the same name).

## 4. GREEN — the existing gates RAN this session (not cited from old logs)

- **FULL battery CERTIFICATION run** `ci/runtime_integration_test.sh` at the branch head, isolated HOME/XDG under the savegate: **out/logs/battery-cert.log — `BATTERY_EXIT=0` (the literal line written from the process's own `$?`), `grep -c 'GATE FAIL'` = 0, tail: `RUNTIME_INTEGRATION_TEST: GATE PASS — authoritative runtime path verified (positive green; no_bus/no_spawn/no_controller/no_dna/save_bad_version all red with named markers; save round-trip green; …)`, `FALL_RECOVERED_CENSUS 2/2 OK`, `MODE_SETS_CHECK: GREEN — 5 dispatching proofs (RuntimeIntegrationProof=41 …)` + `PAIRING OK — 36 stage bodies == 36 entry arms`, run_mode 49 = 38 + 11 — NO mode, NO leg added.** The build step itself ran THROUGH the helper in this run: `BUILD_STEP: signature=exit-0-clean attempt=1`. DISCLOSED: an earlier battery run (out/logs/battery-full.log) reached the same tail GATE PASS with 0 GATE FAIL lines but MY RUNNER did not bank its rc into the log — the certification run above is the rc-bearing proof.
- boot_test rc=0 — “BOOT_TEST: GATE PASS — signal received, DI round-trip, boot order = registration order” (logs/gate-boot_test.log)
- empathy_book_test rc=0 — “M04_EMPATHY_BOOK: GATE PASS — Query+RouteResolution suite green (16 passed, 0 failed), harness self-test red, …”
- audio_test rc=0 — “M06_AUDIO_TEST: GATE PASS — EventBus-triggered non-silent SFX on the Sfx bus, music on Music bus, S4 duck legs …”
- bridge_mvp_test rc=0 — “BRIDGE_MVP_TEST: GATE PASS — six-marker playable MVP verified …”
- main_composition_test rc=0 — “MAIN_COMPOSITION_TEST: GATE PASS — composition root playable (RG2+RG3+C10+C17; non-blank render)”
- smoke rc=0 — “SMOKE: PASS (headless quit + framebuffer) — capture at /tmp/last-animal-smoke.png”
- ui_test rc=0 — 0 GATE FAIL lines, tail “UI_HUD_TEST: GATE PASS — C13 … (A) … (L) …”; its build step shows `BUILD_STEP: signature=exit-0-clean attempt=1` (logs/gate-ui_test.log)
- determinism_cmp rc=0 — “DETERMINISM: IDENTICAL” (A/A, savegate held; logs/gate-determinism.log)
- export_check rc=0 — “EXPORT_CHECK: PASS — build/LastAnimal.exe (156,006,800 bytes, PE magic OK)” + assembly/GodotSharp/.godot-mono markers (logs/gate-export_check.log). DISCLOSED env detours (isolated HOME, not the change): the engine resolves templates under **XDG_DATA_HOME** (the script's own TDIR check is HOME-based) — symlinked the host templates dir + the host .nuget cache into the isolated HOME; one `ln`-into-symlinked-dir trap self-caught and cleaned mid-way (only my own nested links removed).

## 5. Coordination row (the card's required row)

Battery-file ownership question per card: board read at intake (`mc-job json 11281`): **“[type:code] savegate holder discipline + Q2 named-RC” — status VERIFIED (evidence_ok=1)** — the battery lane's card is CLOSED, so the battery-adjacent edit is lawful and needs no claim/fold/sequence; precedent: MC 11320 armed the battery after the same 11281-verified check. This card's battery diff is the two-line build-step swap only (zero new legs, zero mode changes — MODE_SETS + run_mode count untouched, §2).

## 6. Laws

- **Engine PIN:** pinned binary VERIFIED MONO: `--version` = `4.7.2.stable.mono.official.ed1daf0bf` (== engine/PIN.txt commit ed1daf0bf); the non-mono sibling on the same host prints `4.7.2.stable.official.ed1daf0bf` — the contrast is the anti-“loads zero C# but prints green” check, and every gate run above EXECUTES C# through it. Disclosure: the mono ZIP is no longer on disk (unpacked to `/home/claudecode/Games/.tools/godot/…`, engine/local.sha512 pins the zip); the on-disk `editor.zip` sha512 matches the NON-mono line of local.sha512 byte-exact (measured). dotnet: 8.0.133 on the pinned `/usr/lib/dotnet` route via toolchain.sh (8.x OK; PIN.txt names 8.0.130 — drift NAMED, not this card's to retune).
- **SAVEGATE:** foreign-holder check FREE at intake → acquired `/tmp/la-savegate.lock` `holder-code-11312` (long-lived setsid sleep lease, pid 849847), held across EVERY engine run; RELEASED at card end identity-checked (holder file listed before removal, contained ONLY holder-code-11312.txt; own lease killed; own lock rmdir'd — `LOCK-RELEASED`). Every engine run under isolated HOME/XDG (`la-work/11312/.tmp/home*`). The owner's REAL `user://savegame.json` sha256 `fdd1cc43a5124fee3d12b007f5874f0d9a2e969ab93d3c602e1ca96f9697cd4e` banked BEFORE (out/owner-save-before.sha256), re-checked inside verify leg (8) after EVERY engine run AND at release — byte-identical every time (isolation honoured: never written, no restore needed).
- **Fresh-worktree law:** `--import` ran once first, rc=0 (out/engine-import.log).
- **Sizes:** helper 101 l (< 250); no touched file grew (battery 1159 l, ui_test 523 l, all others unchanged); battery SIZE header stays TRUE (no growth → no restamp owed).
- **No secrets** in any artifact. No `git config` used; commit via `-c` flags only.

## 7. VERIFY + COMMIT

`out/verify-11312.sh` — re-runnable: engine-pin row, bash -n × 11, grep-to-zero sweep, battery count/size integrity, plants 1–6 + the real-engine green leg, owner-save byte-check, banked battery certification verdict; computes its own total and prints the literal line. Run this session: `bash out/verify-11312.sh > out/verify-run.log 2>&1` → **process exit code 0, every leg `VERIFY: PASS`, final line `VERIFY_EXIT=0`** (out/verify-run.log — the value is the line as printed by the process, and its `$?` equalled it — never taken from a pipe tail).

**Commit (branch `la/11312`, NOT pushed, master untouched):** `cec5533ca1742e9c31402046f8dadca56a5ec9a2`, parent `dbf2e08a5dfb9ea9bc1aba0d64f41b9d6d3aa2b3` (both read back with `git rev-parse HEAD HEAD^`), author `code (11312) <code@agent-town.local>` via `-c` flags only; 13 files, +120/−10. (The first commit `491e642` ran its body into the subject — amended before close per the repo’s own amend-before-push law; the sha quoted above is the final one.) **Evidence placement:** the repo's `.gitignore` line 21 ignores `.audits/` — this run's evidence is NOT force-added into git (fleet law: evidence is not product repo material; the MC 11311 tracked files are the historical exception); the full set lives in the card-designated out dir (this directory), including the size-heavy battery logs under out/logs/. During banking I `rm -rf`'d the worktree's ignored .audits path and swept the TRACKED 11311 files with it — caught at once via `git status`, restored with `git restore .audits/`, tree verified byte-clean against HEAD (self-caught disclosure).

VERIFY_EXIT=0
