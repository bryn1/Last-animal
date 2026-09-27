# DA-verdict — MC 1388 bridge_mvp gate fix (run 202609262020-9cdd18c5)

Adversarial review of commit dd1dbcb (BridgeMvpProof follow-stage scenario fix) and the
root-cause classification in `.audits/202609261400-bridgefix/evidence.md`.

## Refutation attempts

1. **"The fix masks a real follow regression."** Refuted: the assertion keeps BOTH arms —
   `moved < 0.2f` fails a frozen companion, `d1 > dist0 - 0.25f` fails a companion that
   stops closing. A companion that does not follow at all still goes red (moved ≈ 0).
2. **"The companion only 'moved' because the proof teleported it."** Refuted: only the
   PLAYER is repositioned (+6 X); the companion's 5.74 units of movement come from its own
   `_Process` lerp toward the follow ring — the exact production behaviour.
3. **"The classification (stale scenario, not game bug) is wrong."** Checked against git:
   `git log -S "FollowDistance { get; set; } = 3.5f"` → only a7829ed; f527c4c (wage
   cadence/betrayal) did not touch CompanionFollowBody.cs. The movement contract is
   unchanged; 65b769d (MC 1344.2) moved the baseline to teleport time, where dist0 ≈ 3.499
   ≈ FollowDistance — the assertion became unsatisfiable. Classification holds.
4. **"dist0 ≈ 9.25 is luck; the fix is flaky."** Four independent runs (3 green + the
   red-run log) show dist0 9.247–9.256 and d1 3.515–3.516 — margins of ~5.7 and ~0.24
   against thresholds 0.2/0.25. The gate's flake history was about process-frame races;
   the window is physics-tick gated (MC 1345 mechanism) and the baseline is captured
   before any convergence to the new spot, so the race is structurally removed.
5. **"Side effects on later stages."** Stage 5 (SAVE_ROUNDTRIP) is a pure save seam not
   wired to player position; all green runs pass it. The reposition happens after the kill
   and book markers, so no marker depends on the old position.
6. **"Hygiene / scope."** Diff is 1 source file, 17+/6-; BridgeMvpProof.cs is 438 lines but
   carries a SIZE REASON header (verified lines 15–19); no TODO/FIXME added
   (`grep -c` → 0); no new mechanism beside an existing one — the fix reuses the existing
   baseline-capture machinery.
7. **"Red-run validity."** The planted-bad run inverts the condition (always-fail) rather
   than breaking the follow call. The brief names either as acceptable; the moved-arm
   analysis in (1) covers the break-the-call case by construction. Accepted.

No finding survives. The claims in the evidence file match the artifacts and the git record.

python3 /usr/local/bin/dod_judged_hash.py /home/svarkor/last-animal/.audits/202609262020-9cdd18c5 --base abbbffbd1bea1d719882570c2e63140d121e55b1
# JUDGED: 282cd8e9bddac877665bee46f19a844af4240079d86b4d2fd265c2baa9638a97
# VERDICT: SHIP
