# MC 11891 close — dialogue band reflows to fit any window, owner ruling B. Acceptance with an evidence level per row.
PROBE: git -C /home/claudecode/Games/last-animal branch -r --contains 43c35b6e5308bec0d643f012a4165bd295e9717e => bryn1/master
PROBE: grep -c "DIALOGUE_IN_VIEW" /home/claudecode/Games/last-animal/skeleton/ci/ui_test.sh => ^[1-9][0-9]*$
PROBE: grep -c "EmpathyPanel" /home/claudecode/Games/last-animal/skeleton/src/ui/UiTheme.cs => ^[1-9][0-9]*$

COMMIT=43c35b6e5308bec0d643f012a4165bd295e9717e
BASE=6a1ca1df373a39bd10988067bc58f31192d10052
EVIDENCE-COMMIT=3accd90 (both verdicts committed on bryn1/master)

## Ruling carried, verbatim
Owner, 2026-11-03: **`B`** — of the two options put to him (A = enforce a minimum supported window width, B = reflow the band to fit whatever window the player has) he chose reflow. The card forbade introducing a minimum window size and required the constant-span fallback be stood down rather than left beside the new derivation. Quoted in the LEDGER row and the commit body, per the card.

## Acceptance rows
| Row | Level | Evidence |
|---|---|---|
| Band derives from the live visible rect, constant-span fallback stood down | VERIFIED by two independent reviewers | tester read rects from its own runs: right edge 1144 ≤ 1152 at default and **712 ≤ 800** at the planted width; adversary confirmed algebra forbids the old constant span at any width and grepped for a window clamp in the diff, the repo and `project.godot` — none exists |
| `DIALOGUE_IN_VIEW` fails on a planted fault, twice identically | VERIFIED by an independent plant, not the producer's log | tester's own one-line bypass of the reflow produced `GATE_RC=1` **twice with byte-identical named red lines**, and the game's own `GAME_RC=1` at the hold deadline |
| The other guard still holds after the reflow | VERIFIED | `TAB_DIALOGUE_DISJOINT` green at default, 800, 400 and 200 px in the tester's runs and at 360/480 in the adversary's |
| No minimum window size in substance, not only in wording | VERIFIED with a named residual | 400 and 200 px fully green; **below the `MinBandWidth=96` floor (~110 px and under) the band overshoots the edge and a wrapped label can reach the TAB panel — both fail LOUD by name, nothing silent.** Filed as MC 11976 with the clamp and the reason the floor outruns its own rationale |
| The oversized gate was split honestly | VERIFIED | parent legs A,B,C,D,E,F1,F2,G,H,I,J,K,L traced to the trio at 273/177/191 lines, stamps equal `wc -l`, every moved leg diffed body-for-body (one cosmetic word in an F1 fail message), leg J strengthened, no fourth battery file in `git ls-tree` |
| The retired headless face was not a silent coverage loss | VERIFIED independently | adversary reproduced the basis: headless composes at 64×64 whatever is planted, so the face was load-bearing only as a mirror of the fallback's fiction. Coverage nets up; the retirement is loud |
| Nothing regressed | TESTED, three independent full runs | builder, tester and adversary each ran the full leg set green with rc from the process; battery file diff 0 lines; engine `4.7.2.stable.mono.official.ed1daf0bf` == `engine/PIN.txt`; owner save `fdd1cc43a512…` byte-identical before and after |
| EVIDENCE VERIFY_EXIT=0 in-repo | VERIFIED | run `EVIDENCE.md` plus both verdicts are committed at 43c35b6 / 3accd90 so the external verifier reads them from the repo |

## What I did not do myself, labelled
**I did not execute the gate this close.** Three independent runs exist — the producer's and two verifiers who each treated the producer's logs as claims rather than evidence, planted their own faults and re-ran from `git archive` bytes under isolated HOME/XDG. I verified merge hygiene myself: both identities read back as `code (11891) <code@agent-town.local>`, ancestry by `git merge-base --is-ancestor` with containment re-checked by `git branch -r --contains` rather than sha equality, and the committed file list inspected for scratch and engine paths.

## Two law corrections the first cut needed, and the root cause of one
The first commit stamped **committer `larbild-mayor`**, the owner's personal identity, while authoring correctly. Root cause, measured: the per-invocation `-c user.*` form sets only the **author**; the host environment supplied `GIT_COMMITTER_NAME`/`GIT_COMMITTER_EMAIL`. So a check that greps the author would pass that commit, and an instruction to "always use `-c user.*`" does not prevent recurrence — recorded on MC 11892, which asks for both fields to be checked. The second violation was packaging: two ~280 KB raw gate transcripts, against the ruled durable set; replaced with named summaries, and the adversary then found the deeper problem now routed to MC 11840 — the pattern's `gate-*.log` negation makes raw transcripts committable, so only a naming convention stands between this repo and a gatewardens-shaped 245 MB repeat.

## A merge hazard worth stating, because it nearly produced a false report
`git merge --ff-only` printed `Updating 6a1ca1d..43c35b6` and left master at `6a1ca1d` with **no reflog entry**. I first reported that internally as "rolled back by something". The measured cause is narrower and reproducible: the producer had re-synced its own evidence into the **main checkout** as untracked files, and because the commit adds those same paths git aborts rather than overwrite untracked files — the abort message was on stderr and the success line on stdout, so a reader who looks only at stdout sees a merge that happened nowhere. I compared all nine loose copies against the committed blobs (byte-identical), parked them, and the merge then landed and pushed. Two consequences: **a merge is not done until the remote head is read back**, and a producer's practice of mirroring run evidence into the main checkout blocks merges of its own commit — MC 11840's convention home should say where run evidence goes so it cannot collide with the commit that admits it.

## Follow-ups named, not absorbed
**MC 11972** empathy panel, severity raised by the adversary: it is fully **invisible** below ~1136 px and its own disjointness assert passes vacuously on off-screen rects — the same false-green shape this card cured, provable from the committed 800 px frame that reported PASS. **MC 11976** degenerate-window overshoot plus the missing HEIGHT axis. **MC 11840** append: narrow the log negation, handle the cited-but-ignored `.rc` pointer. **MC 11973** capture-race inventory for the remaining windowed legs. **MC 11892** commit-identity enforcement, now with the committer-variable root cause. P3/P4 left in place: `DialogueSystem.cs` at 279 lines over the 250 target, and a floor-boundary gate width.

Three independent full-gate runs at 43c35b6 (producer, tester, adversary), each rc captured from the process; my own non-execution is labelled in the artifact above.
VERIFY_EXIT=0
