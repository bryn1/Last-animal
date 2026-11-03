# CYCLES — MC 11891

**Cycle 1 — build → rejected by the orchestrator, not by a verifier.** Builder delivered the reflow, the named guard with a proven red face, and the three-way gate split. Two law violations found in the commit by my own check and absent from its handoff: committer stamped with the owner's personal identity (`larbild-mayor`), and two ~280 KB raw gate transcripts committed against the ruled durable set. No verifier was briefed until both were fixed.

**Cycle 2 — corrected, then verified by two independent gates.** Builder amended with per-invocation identity including the committer variables and replaced the transcripts with named summaries, then disclosed two extra files it chose to correct rather than leave pointing at paths the commit no longer carried. Tester: PASS on its own planted fault, twice identical. Adversary: SHIP, no P0/P1, five live engine runs, and the sharpest finding of the card — the empathy panel is invisible below ~1136 px while its own assertion passes vacuously.

## What the cycles cost, honestly
Cycle 1's defects were both cheap to fix and both were *reported as successes by omission* — the handoff quoted the author field and said nothing about the committer. That shape is exactly what MC 11892 exists to catch mechanically, and this run is the second identity incident in this repo in a week.

## One orchestration error of my own, in this run's ledger
I believed a merge because its stdout said `Updating 6a1ca1d..43c35b6`. It had not happened: the abort was on stderr, master did not move, and there was no reflog entry. Cause measured: the builder had mirrored its evidence into the main checkout as untracked files, and the commit adds those same paths. Fix: compared all nine loose copies to the committed blobs (byte-identical), parked them, re-merged, and read the remote head back before claiming anything. Stored as a fleet lesson.
