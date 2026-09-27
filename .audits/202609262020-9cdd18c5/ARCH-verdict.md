# ARCH-verdict — MC 1388 bridge_mvp gate fix (run 202609262020-9cdd18c5)

Closing architecture-vs-reality + hygiene pass over the tree at dd1dbcb/18d8eda
(HEAD after the fix and the evidence commits), re-derived from the artifacts, not from
any report.

## 1. docs/ARCHITECTURE.md vs the actual tree

Re-derived this pass:
- Modules: `src/{combat,companion,dna,ecosystem,empathy,npc,save,ui}` all exist and are
  listed in the doc's module table (lines 29–46, incl. `world/` with
  `CompanionFollowBody.cs`). MATCH.
- Entrypoints: `ci/` gates on disk (audio, boot, bridge_mvp, combat, companion, dna_npc,
  ecosystem, empathy_book, export_check, main_composition, runtime_integration, save,
  smoke, toolchain, ui) match the doc's gate list (line 85–86); `ci_proofs/` files match
  the doc's proof list (line 91), BridgeMvpProof.cs included. MATCH.
- Dependencies / ports / data stores: the fix touched only a proof scenario's stage
  transition — no new dependency, port, or data store; save still flows through
  `src/save/SaveSystem` (System.Text.Json, schema-guarded GameState), as documented.
  MATCH.
- The doc names files and gates, not assertion thresholds, so the scenario correction
  introduces no doc drift. No update required.

## 2. File hygiene

- `skeleton/ci_proofs/BridgeMvpProof.cs`: 438 lines — over the 400 hard ceiling WITH the
  required one-line reason in its header (SIZE REASON block, lines 15–19, verified this
  pass). Compliant with the stated exception.
- `skeleton/world/CompanionFollowBody.cs`: 84 lines, one concern. PASS.
- New files this run: `.audits/202609261400-bridgefix/evidence.md` (chmod 644, verified),
  `.audits/202609262020-9cdd18c5/{ARCH-opening.md,test_dod_evidence.py}` — all at
  layout-v2 paths, committed (18d8eda). No stray sibling dirs created. PASS.
- No TODO/FIXME added to source (grep count 0 on the touched file). PASS.

## 3. Layout v2

One canonical project tree at `/srv/workspace/last-animal/`; run artifacts under
`.audits/<run>/`; scratch under the run dir's `.tmp/`. No violation found.

python3 /usr/local/bin/dod_judged_hash.py /home/svarkor/last-animal/.audits/202609262020-9cdd18c5 --base abbbffbd1bea1d719882570c2e63140d121e55b1
# JUDGED: 282cd8e9bddac877665bee46f19a844af4240079d86b4d2fd265c2baa9638a97
# VERDICT: PASS
