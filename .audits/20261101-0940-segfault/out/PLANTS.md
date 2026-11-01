# PLANTS — the planted faults that prove the MC 11312 retry cannot swallow a real failure

Why this file exists: MC 11312 was closed once and knut returned it FAIL, with two acceptance rows marked
UNVERIFIABLE because the plant logs and reproduction scripts were not in the repository — the verifier sees
only what is committed, and `.audits/` was gitignored when the card was first closed (MC 11840). The ignore
rule now admits these files, so the proof is banked here and summarised below with the named line each plant
printed. Raw logs sit beside this file in `logs/` and `plant-*.log`.

The claim under test: the engine's build step intermittently exits 139 (SIGSEGV) *after* a successful build.
A retry that keys on the crash alone would also retry a genuine build failure, turning a false red into a
false green — strictly worse. So the acceptance bar was never "does it retry", it was "prove it cannot hide
a broken build". Every row below is a planted fault, and the named output it produced.

## Producer plants (this run, `plant-*.log`)

| Plant | Fault injected | Named output | Meaning |
|---|---|---|---|
| `plant-rc1-real-buildfail-rc.log` | real compile error, no crash | `GATE FAIL: engine --build-solutions exited 1 — no` | a build failure with no 139 is red immediately |
| `plant-139-with-real-builderror.log` | **real compile error riding a forced 139** | `GATE FAIL: engine --build-solutions exited 139 AN` | the inversion case: red, **zero retries** — a genuine failure is never retried away |
| `plant-forced-139-exhaust.log` | 139 on every attempt | `quit-segfault retry 1/2` … then `EXHAUSTED after 2 named retries` | the bound is finite and the exhaustion is *reported red*, not looped |
| `plant-forced-139-bound0.log` | bound set to 0 | `EXHAUSTED after 0 named retries (bound 0 reached;` | the boundary behaves; N counts retries after the first attempt |
| `plant-flaky-139x2-then-green.log` | 139 twice, then clean | `quit-segfault retry 1/2` … green at attempt 3 | a real flake is recovered inside the bound |
| `plant-budget-kill-124.log` | budget kill (124) | `GATE FAIL: engine --build-solutions exited 124 —` | a timeout is not a segfault: red by name, no retry |

## Adversary plants (added by the independent review, not by the producer)

| Plant | Fault injected | Result |
|---|---|---|
| 139 with the solution file missing | crash *and* an unresolvable project | red by name, zero retries |
| `LA_BUILD_RETRY_MAX=-1` | negative bound | red by name, zero retries |
| `LA_BUILD_RETRY_MAX=abc` | non-numeric bound | red by name (`GATE FAIL`), no silent coercion |

## The decisive pair

`plant-139-with-real-builderror.log` is the one that matters, and the reviewer reproduced it independently on
the real pinned engine: a genuine C# compile error arriving *with* a 139, while the engine still printed its
`[ DONE ] dotnet_build_project` marker — that marker was measured to print on a **failed** build, which is why
the retry condition keys on the build tool's verdict (exit code + `Build succeeded` + assembly present) and
never on the marker. Outcome: named red, zero retries, ~14 s.

The mirror-image trap was also run: a broken build followed by 139-*then-clean-exit*. A condition that rode the
crash or the marker would have retried onto the clean exit and gone green. It went named red, zero retries.

## Live reproduction, not only simulation

During its own green run on the pinned engine, the reviewer **caught a real rc=139** occurring naturally after
recompile-heavy plants — the exact named condition — and watched it recover: build-verified, `retry 1/2`,
clean on attempt 2, 19.8 s. That is the failure mode reproduced in the wild and it quantifies the cure at
10–25 s against losing a full battery run.

## What the retry still cannot see (filed, not fixed here)

A recovered crash leaves **no cross-run record**, and `skeleton/ci/export_check.sh` sends the helper's output to
`/dev/null`, discarding even the per-run line. Ten recovered crashes therefore look byte-identical to a clean
tree, so a worsening trend is invisible precisely when the retry starts earning its keep. Tracked as MC 11884.
