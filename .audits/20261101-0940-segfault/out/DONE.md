# DONE — MC 11312 (named bounded quit-segfault retry). Acceptance with an evidence level per row.
| Acceptance | Claim | Level |
|---|---|---|
| bounded named retry at the affected step | `LA_BUILD_RETRY_MAX` default 2, named at `build_solutions.sh:42`; exhaustion reports RED, never loops; bound-0 and non-numeric bound both RED by name | TESTED — verifier's own plants, plus my re-run of the gate |
| build-verified condition | keys on `dotnet build` rc + `Build succeeded` + dll presence, NOT the engine's `[ DONE ]` marker, which was measured to print on a FAILED build | VERIFIED — trap independently reproduced by the verifier on the real engine; same rc=139 + same printed `[ DONE ]` take different paths |
| RED-capable plants | real compile error riding a forced 139, and the decisive false-green trap (broken build + 139-then-clean) both go named RED with ZERO retries | TESTED — 8 planted shapes; three of them invented by the adversary, not the producer |
| battery green | my own gate re-run on the merged tree `VERIFY_EXIT=0`; verifier's own battery at closure head `TEST_BATTERY_EXIT=0`, 0 GATE FAIL, tail GATE PASS, 49 legs = 38+11, census 2/2, MODE_SETS GREEN, PAIRING 36==36 | TESTED — two independent full runs, neither reusing the producer's log |
| coordination row | MC 11281 board-read `evidence_ok=1` | UNVERIFIED by me — producer's board claim, not load-bearing for any PASS row, named as such |
| EVIDENCE VERIFY_EXIT=0 | literal line from the gate process, placeholder resolved | VERIFIED — re-executed by me this session |
| mechanism monogamy | blanket old marker retired; old direct engine `--build-solutions` invocation now lives only inside the helper, 0 occurrences in the ten routed callers; battery file zero line-delta (1159==1159) | VERIFIED — grep counts, not assertions |

## Findings carried, not fixed here
F-DA1 **P2** retries leave no cross-run trace and one caller discards even the per-run line to `/dev/null` (MC 11884). F-DA2 **P3** three `--import` legs plus a quit-after leg share the crash class and stay unguarded, unnamed as survivors. F-DA3 **P3** the swap routed seven callers through pinned-toolchain dotnet without disclosure. F-DA4/5 **P4** locale-dependent success match (fails RED, so not a safety issue) and verify script writing into the live worktree. Also named: engine `[ DONE ]`-on-failure (toolchain honesty), `retarget_bake.sh` vm105 path rot, dotnet pin drift 8.0.130 -> 8.0.133.

## Ship state
`la/11312` fast-forwarded into master; master pushed `dbf2e08..cec5533` to bryn1/master. Containment checked, not equality: `git branch -r --contains cec5533` -> bryn1/master.
