 # Last Animal — build LEDGER (updated 2026-10-02, VM350 standby seat; M00 chain below: svarkor-session MC 1344)

Goal: Godot 4 C# ARPG 'Last Animal' — owner DoD (card 1344, verbatim): "spelbart spel utan buggar
som går att köra på en windowsdator" — a playable, bug-free game that runs on a Windows computer,
according to the specs (PHASE0.md: mandate I1-I6, constraints C1-C5, gate contracts C1-C17,
modules M00-M14).

STATUS: CHAIN COMPLETE 2026-09-25 — final fresh audit (T7, MC 1373) verdict **SHIP-READY, 0 P0,
0 P1** at HEAD 46eabe9. All CI gates green from clean clones; runtime-verified playable; Windows
zip packages the complete launch payload. Documented exceptions: C16 launch smoke on real
Windows/wine unexecuted (no wine on the build host — the owner's acceptance step); push BLOCKED
(no deploy key on vm105). Non-blocking register: B1-B7 + A7/A8/A9/A10 (P2/P3/P4) — next card.

## Repo (layout v2, MC 3671)
- Repo root / project dir: `/srv/workspace/last-animal/` (migrated 2026-09-24 from
  `svarkor-last-animal-phase2/gunilla`; git history continuous).
- Game tree: `skeleton/`; vendored engine: `engine/` (Godot 4.7.2-stable mono + export templates
  per `engine/PIN.txt` — note F6: PIN.txt names the templates dir without `.mono`, stale).
- Spec of record: `.audits/legacy-svarkor-last-animal/PHASE0.md`.
- Evidence: `.audits/legacy-svarkor-last-animal-integration-audit/<seat>/` (migrated from the old
  sibling dir; MC cards' old paths resolve here).

## INTEGRATED + COMMITTED
| Phase | Module | Commit |
|---|---|---|
| 2 | M00 preflight | 2751839 |
| 3 | M01 core-framework | 5c76c4d |
| 5 | M02+M03 dna/npc | 9251126 |
| 4 | M09 animation-pipeline | 6c6dd6a |
| 6 | M07 world-environment | bc0720e |
| 7 | M06 audio | 34a2713 |
| 8-13 | M04/M05/M08/M10/M11/M12 | 4afe4f7 + 67cd759 |
| 14 | M13 installer-package | ab89eba (original) → ed5f294 (zip fix) |
| 15 | M14 docs-playbook | 1d1a983 → b53a311/c47cee7/9d1cf24 |

## Audit + fix chain (MC 1344 parent, 2026-09-24/25)
- T1 (MC 1345) CLOSED: runtime-gate companion-follow assert fixed (d79a903 — capture follow baseline
  at teleport). Evidence: .audits/legacy-…/svarkor/1345-close-evidence.md.
- T2 (MC 1346) CLOSED: # VERDICT: PASS — all 14 ci/*.sh gates green from a CLEAN CLONE at ffd3663;
  bridge render-capture flake fixed (65b769d teleport baseline + tick-gated hold, 3× consecutive
  green); --no-restore silently no-ops on never-restored projects (ffd3663). Evidence:
  .audits/legacy-…/svarkor/1346-close-evidence.md.
- T3 (MC 1347) CLOSED: T3-verify # VERDICT: FAIL — F1 P1: the Windows zip shipped ONLY the exe; the
  77 MB .NET assemblies data dir was omitted (the exe cannot launch without it — template's own error
  strings prove it). T3-fix FIXED it: ed5f294 packages exe + data dir (187 entries, fails loud both
  ways), b53a311/c47cee7/9d1cf24 docs. Zip 73 MB, extraction reproduces the launch layout, negative
  check red 2 ways, export_check no-regression. C16: SATISFIED-WITH-EXCEPTION — launch smoke on real
  Windows/wine unexecuted (no wine on the build host; honestly documented, not faked). Evidence:
  .audits/legacy-…/svarkor/1347-close-evidence.md.
- T4 (MC 1348) CLOSED: fresh adversarial audit (devils-advocate child, HEAD bab1815) SHIP-BLOCKED —
  0 P0, 5 P1. Parent adjudicated against current HEAD: A1 already fixed by the parallel crew
  (446a1e4 health rides the snapshot + 7794030 death_load proof); A2/A3/A4/A5 stand. Reports:
  .audits/legacy-…/artemis/1348-fresh-audit-report-20260924-svarkor-session.md,
  .audits/legacy-…/svarkor/1348-close-evidence.md.
- T5 (MC 1372) CLOSED: the four standing P1s FIXED, one commit each, red/green-proven, gates green:
  aba951c (A2 corpse-damage loop: KillHide zeroes DamageDealt + director skips dead actors),
  f527c4c (A3 wage/betrayal pillar: seconds cadence 20 s/30 s + pay_wage key P player decision +
  ExecuteBetrayal wired on the Betrayed transition), 5951515 (A4 Empathy Book: book key B toggle via
  existing C9 Query, closed-by-default panel), 46eabe9 (A5 boot SpawnEnemies ring STOOD DOWN — zone
  pipeline owns spawning; proofs moved to the authoritative set; PressTravel re-arm fix).
  Parent re-ran the runtime gate from a FRESH clone at 46eabe9: GATE PASS, H1-H4 green, dotnet build
  0 warnings. Evidence: .audits/legacy-…/svarkor/1372-close-evidence.md,
  .audits/legacy-…/dobbie/1348-p1fix-result.md.
- T7 (MC 1373) CLOSED: FINAL fresh adversarial audit at HEAD 46eabe9 — **SHIP-READY, 0 P0, 0 P1**.
  The four T5 fixes held under adversarial load (A2 every DamageDealt consumer covered; A3 skip arm
  red/green-proven, betrayal fires once, cadence consistent across load; A4 toggle + closed-by-default
  asserted; A5 zero remnants, no_spawn still red, meadow fields 3). New non-blocking findings:
  B1 P2 (no in-game wage-due indicator), B2 P2 (book labels loyalty <=19 "Betrayed" though betrayal
  fires at 0), B3 P2 (C9 Forgive/PermanentBreak routing computed but never rendered/acted on),
  B4-B6 P3, B7 P4. Standing P2s re-verified: A7/A8/A9/A10. Report:
  .audits/legacy-…/artemis/1348-final-audit-report-20260925.md; evidence:
  .audits/legacy-…/svarkor/1373-close-evidence.md.

## Parallel crew (hermes-standby) on the same chain
Landed: ab89eba/1d1a983 (M13/M14), 2bc93c6 (dna refactor), 5da802a (save/load + interact wiring),
881750f (zone travel + boss phase), d82c3e1 (ZoneBossProof), 446a1e4/e15ae6a/7794030/21397dd
(N1/N2 fixes incl. health-in-save), 0ccff93/01ee5d9 (N3-N10 doc fixes). Performed the layout-v2
migration (2026-09-24). Its own audit-fix loop runs on MC 1348 N-findings; A6-A10 (P2) offered to it.

## REMAINING
- T7 verdict (final gate) — running.
- P2 register (offered to the parallel crew, unclaimed): A6 ZoneProgression production wiring
  (446a1e4 may have addressed — re-verify), A7 load regresses ecosystem ObservedCount N→1,
  A8 DnaLanguage.Counter no production callers, A9 tampered-save crash (unguarded tally[nucleotide]++),
  A10 zone environments never change (main.tscn instances only meadow).
- P3/P4 register: F2 (wine smoke asserts exit 0 only — the C16 ">0 non-blank frames" leg needs a
  wine-capable host), F6 (PIN.txt stale: templates dir name + dotnet version), F7 (README per-run
  byte counts), F8 (data dir named "Preflight" though it ships the full game), A14 (PlayerActions/
  UI-build split), WorldDirector.cs over the 400-line ceiling (header reason stands).
- PUSH: ~~BLOCKED~~ RESOLVED 2026-10-01 — the bryn1 account key (`~/.ssh/github_bryn1`) pushed the
  full lineage to the canonical remote (see AAA section). Original blocker (no deploy key on vm105)
  recorded here for history.
- C16 launch smoke on real Windows (or wine) — the owner's acceptance step on a Windows machine.

## AAA goal (owner ruling 2026-10-01, standing goal round 9; MC 3887 seat card)
STATUS: REOPENED 2026-10-01 as an increments project — owner ruling, verbatim: "Bryn1/last-animal is
the repo. The goal is a AAA game with graphics, story, gameplay, visualisera, skills, followers abd
more." Art style ruling: "Mix of both" (CC0 Kenney packs for environment/props + hand-composed
models for player/companion/boss/enemies). Scope ruling: "Everything." The M00 chain above remains
the historical base (MC 1344 achieved: playable, gates green, Windows-packageable).

Canonical remote: `git@github.com:bryn1/Last-animal.git`, master = pushed lineage
(df8ff23 base → graphics commits). Working branch `vm350/last-animal`. Prior svarkor-ai/* remote is
NOT this seat's to push (parallel-orchestrator account; untouched).

### Increment 1 — graphics pass (2026-10-01)
- MC 3889 CLOSED (completed_unverified, DONE-gate requested): composed player (main.tscn Visual
  root: torso/head/arms, 8ac1794) + gold quadruped companion (CompanionVisual.cs, 0b5ada2);
  verdicts DA SHIP / ARCH-c2 PASS / TEST PASS; orchestrator re-ran build+gates and visually
  verified a live capture (player moves, companion follows, HUD intact). Evidence:
  .audits/202610012100-char-art/evidence.md + player-companion-walk.png.
- MC 3888 CLOSED (completed_unverified, DONE-gate requested; wave-2 verdicts TEST-c2 PASS /
  DA-c2 SHIP / ARCH-c2 closed via 702c7db): per-zone sky/light (2831aad), 36 Kenney GLB decor
  instances from 24 pack files, 12/zone (55a8d6b), procedural-sky v2 after panorama dropped
  runtime terrain on software GL (3afc3d1; equirect claim recorded BLOCKED pending hardware-GPU
  check). Cycle 1 history: DA F1 "props buried" = FALSE POSITIVE (PIL clips 16-bit PNG
  heightmaps); engine-authoritative AnchorProbe (run by producer, DA-c2 and orchestrator: worst
  grounding delta -0.09/-0.04/-0.08 m) proves all 34 props correctly sunk 2-9 cm. F2 wording +
  F3 citation fixes landed; ARCH assets_packs doc row landed (20651de/b509dc3/11df6ce + 702c7db
  measured 4096x2048 skybox fix). DoD by orchestrator's own runs at 702c7db: export_check 0,
  export_linux 0, smoke capture 1280x800 1819-color render, zone screenshots visually verified.
  Evidence: .audits/202610012100-env-art/evidence.md.
- MC 3895 CLOSED (completed_unverified, DONE-gate requested; final wave TEST-c2 PASS / DA-c2
  SHIP / ARCH PASS, all three pinned 50caf2d3): per-type composed enemy silhouettes
  (ActorVisual.cs static builder riding the ONE existing visual-rides-sim-body mechanism,
  ccb5cd9) — goblin green / orc brown / skeleton arcane violet (0.62,0.45,0.90, sky-collision
  0 @tol28) / player blue / companion gold; spawn wiring through the existing
  WorldDirector.ApplySpawnSet (253020d); docs census (1a40bde); SIZE headers + ruins capture
  (00ca870). Boss 2.75x + emissive via isBoss; runtime-captured frames for goblin (meadow),
  orc (canyon), skeleton (ruins, Life 0/100 = combat real); planted-bad cycle proves the pixel
  gate can fail (778→0→777 px). Fix cycle 1 history at .audits/202610020330-verdict3895/
  (DA c1 FIX: SIZE headers + P3 capture gap — both closed). Honest rows: boss-in-frame
  UNVERIFIED (DNA=0 boot cannot spawn boss; boss build-path proven via boss_phase gate leg),
  P4-5 "449"-string nit in DONE N2 (true 451, substance unchanged).
  Evidence: .audits/20261002-enemy-art/evidence.md, checker dir .audits/202610020155-86f82502/.
- MC 3896 CLOSED (completed_unverified, DONE-gate requested): main_composition capture/quit race
  fixed — proof detects the GUI leg and holds its painted window for the graphical-test-helper's
  capture-then-kill contract (+41 lines, MainCompositionProof.cs only, headless leg untouched;
  1a5f382 + docs 3d6e813). Orchestrator: GATE PASS x3 consecutive + runtime_integration +
  bridge green; producer reproduced pre-fix RED and two distinct red-capability legs (planted
  camera bug, forced early quit) reverted clean. Evidence: .audits/20261002-3896/.
- INCREMENT 1 EXIT CRITERION MET (2026-10-02 03:57): full ci sweep at 3d6e813 — 12 ci/*_test.sh
  + smoke + export_check ALL exit 0; tools/export_linux.sh PASS (81MB zip) and its packaged-
  binary smoke capture shows the NEW ART (player, gold companion, goblins, Kenney trees,
  procedural sky, HUD). The 15th gate path in LEDGER prose (ci/export_linux) actually lives at
  skeleton/tools/export_linux.sh — registered here so the path is never hunted again.
- Known limitations: ruins zone intentionally dark; canyon reads hazy — polish-pass register.
  MC 3897 filed: bridge proof counts bodies, not Visual-subtree content (mutation probe P2
  from 3895 TEST) — still queued.
- Increment 2 PLANNED (.audits/20261002-inc2-plan/PLAN.md, 8 stages 2a-2h: story data layer +
  quest core, DNA-mutation skills on the dead Manna seam, follower ROSTER of the existing
  companion stack, save v2->v3 one ratified break; 8 owner decisions D1-D8 pending "rec";
  DA gate running). Two seed-notes claims refuted against code: no "6 weapon types" exist
  (grep weapon = 0 hits), AdaptationSystem is really src/dna/EcosystemAdaptation.cs.
- MC 3900 stage 2a BUILT (code, 2026-10-02): story DATA layer behind the existing dialogue
  view — new engine-free src/story/DialogueTable.cs (id -> authored text + optional
  Func<string,bool> condition, nil for now) REPLACES DialogueSystem's private hardcoded
  DialogueFor switch; table injected in WorldDirector.BuildUi (1 line); Show/Close/ActiveNode
  + never-blank fallback unchanged; production trigger unchanged (interact -> Show npc_<id>).
  3 migrated nodes verbatim + 2 new authored arc seeds (first_speak, wage_duty; owner ruling
  D6 English diegetic-minimal). Gates: NEW ci/story_test.sh two-pass green, ci/ui_test.sh leg D
  asserts EXACT painted text for an authored node, runtime dna_speak DIALOGUE_SHOWN + bridge
  regression green, planted-bad (removed authored node) went RED naming it, reverted clean.
  Evidence: .audits/20261002-2a/evidence.md, checker dir .audits/202610020519-e9b6c9c6/out/.
- MC 3901 stage 2b CLOSED (orchestrator-verified, 2026-10-02): save schema v3 at ff2786c —
  GameState v3 fields QuestStates ("id:status") / Manna / Followers{EntityId,Loyalty}; single-
  companion fields removed at the one ratified break ("All rec", D2); no LearnedMutations
  (absence pinned by wire test); CurrentVersion 2->3, rejection logged both directions;
  NEG_SAVE_VERSION leg version-relative (plan §G D9). Orchestrator battery green at ff2786c
  (save/runtime/bridge). W1 verdict wave (dir .audits/202610020519-e9b6c9c6, pin 49c70e52):
  TEST PASS / ARCH PASS / DA FIX -> both DA P2s trail-level and now fixed (this LEDGER row +
  evidence-form addendum in .audits/20261002-2b/evidence.md). Map amendment (ARCH F-1):
  SaveLoadController.cs QuestStates persistence joins 2c's file set (W2 solo).
- Follow-up registered (ARCH F-2 P3): skeleton/docs/player-guide.md:64-67 still describes
  dialogue text as hardcoded DialogueFor — rewrite lands with stage 2d.
- MC 3904 stage 2c CLOSED (orchestrator-verified, 2026-10-02): quest core. 2c build ed4a5b2
  (pure QuestTable/QuestLog, ONE batched 5-signal bus edit, InitStory root seam, QuestStates
  live persistence per W1 F-1 map amendment); DA cycle-1 FIX found a REAL P1 (evidence counters
  survived load -> post-load observation fast-forwarded the arc); fixed at 4ce42de (+f30cf67
  frame-budget grant) with named rewind tests + extended quest_persist leg (QUEST_REWIND).
  W2 verdict board at f30cf67, pin 64ecf261 (--base ed4a5b2): TEST-c3 PASS (7/7 + 4 probes),
  DA-c2 SHIP (P1 6/6 fields, non-vacuous tests, axes judged), ARCH PASS. Carried: MC 3910
  (death_load stale-save leg + RuntimeIntegrationProof.cs 536L SIZE/split); 2d gains
  objective-arg cross-validation test (DA P2) + QuestTable doc-comment refresh; pre-W5 map
  row for WorldDirector.Story.cs + 2g must re-key wage seam per follower. docs/ARCHITECTURE.md
  synced (story row, Story partial, 11-signal bus, quest gates/modes, §8 v3).
- MC 3911 stage 2d CLOSED (orchestrator-verified, 2026-10-02): authored 5-quest arc content
  (Arrival / Answer in Tongue / Bread Before Bonds / Blood Teaches / The Watcher Falls) + arc
  dialogue + QuestArcCrossCheckTests (DA W2 P2 ruling) + player-guide rewrite. ids untouched.
- MC 3912 stage 2e CLOSED (orchestrator-verified, 2026-10-02): skill core — PlayerMutations
  (pure consensus-Counters unlock), SkillState Manna economy (spend-once/+kill/cap-100/load-
  verbatim), Invert Strike (Q, x3 next hit) + Mend (R) at the ONE DealDamage site (wrap, call
  count 1), SfxRouter consume-only zero-asset, skill_3 (F) reserved per owner D4 deferral
  (MC 3914 Calming Speak). W3 verdict board: TEST-c3 PASS (probes incl. cross-wave 2c guard),
  DA-c3 FIX -> fix round c70bc13 (guide truthfulness P1, reward "authored note, playback not
  wired" reword P2 -> emitter deferred MC 3915, N2 mirror pin P3 proven-red-twice). DA-c2/
  ARCH re-verdicts on the fix tree = next wave. NOTE: first battery run post-fix hit a
  msbuild-race transient (gate red once: 2 failures incl. the new pin); two serial re-runs
  green; producer overlap ruled cause, no code fault found.
