 # Last Animal — build LEDGER (updated 2026-10-04, MC 10059 record de-stale pass; prev. 2026-10-02 VM350 standby seat; M00 chain below: svarkor-session MC 1344)

Goal: Godot 4 C# ARPG 'Last Animal' — owner DoD (card 1344, verbatim): "spelbart spel utan buggar
som går att köra på en windowsdator" — a playable, bug-free game that runs on a Windows computer,
according to the specs (PHASE0.md: mandate I1-I6, constraints C1-C5, gate contracts C1-C17,
modules M00-M14).

Size note (file hygiene, MC 10059): one subject — this repo's project state — and fleet doctrine
names LEDGER.md the ONE ledger, so it is kept whole past the ~300-line split guideline.

CURRENT STATUS (2026-10-04): increments project — the "AAA goal" section below is authoritative
(REOPENED 2026-10-01, owner ruling quoted there). C16 Windows launch smoke remains the owner's
open acceptance step (never executed on Windows as of the 2026-10-04 workspace audit).

[SUPERSEDED 2026-10-04 MC 10059 — kept as history; both status claims below were untrue at this
pass: "CHAIN COMPLETE" stands superseded by the REOPENED ruling below, and the push blocker was
resolved 2026-10-01 (PUSH row in REMAINING; bryn1 remote master verified = 5922b25 by
`git ls-remote bryn1 master` 2026-10-04).] STATUS: CHAIN COMPLETE 2026-09-25 — final fresh audit
(T7, MC 1373) verdict **SHIP-READY, 0 P0, 0 P1** at HEAD 46eabe9. All CI gates green from clean
clones; runtime-verified playable; Windows zip packages the complete launch payload. Documented
exception: C16 launch smoke on real Windows/wine unexecuted (the owner's acceptance step).
Non-blocking register: B1-B7 + A7/A8/A9/A10 (P2/P3/P4) — next card.

## Repo (layout v2, MC 3671)
- Repo root / project dir: `/srv/workspace/last-animal/` (migrated 2026-09-24 from
  `svarkor-last-animal-phase2/gunilla`; git history continuous; the old sibling dir is gone —
  checked 2026-10-04). On VM350 `/srv/workspace/last-animal` is a symlink to
  `/home/svarkor/last-animal` (same .git — verified 2026-10-04, inode-identical).
- Game tree: `skeleton/`; vendored engine: `engine/` (Godot 4.7.2-stable mono + export templates
  per `engine/PIN.txt` — note F6: PIN.txt names the templates dir without `.mono`, stale).
- Spec of record: `.audits/legacy-svarkor-last-animal/PHASE0.md`.
- Evidence: `.audits/legacy-svarkor-last-animal-integration-audit/<seat>/` (migrated from the old
  sibling dir; MC cards' old paths resolve here). The 839/890-era seat evidence the 2026-09-24
  migration left in `.audits/legacy-phase2/` was content-hash-audited on 2026-10-04 (MC 10059;
  66 unique non-cache files incl. the only copies of those era reports, captures and the MC
  890.8 bake-repro harness) and archive-moved under
  `.audits/legacy-svarkor-last-animal-integration-audit/legacy-phase2/`; the stray
  `_cleanclone-test/` full clones inside it (HEADs 67cd759, ffb1e44 — both ancestors of HEAD,
  zero unique untracked evidence, no unique commits/stashes) were deleted from disk there.
  `.audits/` is gitignored (`.gitignore:21`) and its 31 pre-rule tracked files were untracked
  2026-10-04 (MC 10059, `git rm --cached`, kept on disk).

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
- P2 register (offered to the parallel crew, unclaimed): A6 ZoneProgression production wiring
  (446a1e4 may have addressed — re-verify), A7 load regresses ecosystem ObservedCount N→1,
  A8 DnaLanguage.Counter no production callers, A9 tampered-save crash (unguarded tally[nucleotide]++),
  A10 zone environments never change (main.tscn instances only meadow).
- P3/P4 register: F2 (wine smoke asserts exit 0 only — the C16 ">0 non-blank frames" leg needs a
  wine-capable host), F6 (PIN.txt stale: templates dir name + dotnet version — re-confirmed
  2026-10-04: live templates dir is `4.7.2.stable.mono`, live `dotnet --version` = 8.0.131 vs the
  pinned 8.0.130), F7 (README per-run byte counts — DESTALED 2026-10-04 MC 10059: artifact block
  marked M00-era with a pointer to the latest export evidence), F8 (data dir named "Preflight"
  though it ships the full game), A14 (PlayerActions/UI-build split), WorldDirector.cs over the
  400-line ceiling (header reason stands — 501 l at 5922b25, SIZE header present).
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
(df8ff23 base → graphics commits). Working branch `vm350/last-animal`. The prior remote on the
old (parallel-orchestrator) account is NOT this seat's to push; that account is now deleted (MC 10093).

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
  from 3895 TEST) — still queued. [2026-10-04 MC 10059: card 3897 is not on the current board
  (old board 3887-3953 archived — 2026-10-04 workspace audit header); "still queued" is
  UNVERIFIED since — treat as open-but-untraced, re-file under the 10026 anchor if still wanted.]
- Increment 2 PLANNED (.audits/20261002-inc2-plan/PLAN.md, 8 stages 2a-2h: story data layer +
  quest core, DNA-mutation skills on the dead Manna seam, follower ROSTER of the existing
  companion stack, save v2->v3 one ratified break; owner decisions D1-D8 RATIFIED 2026-10-02,
  owner artifact "All rec" — PLAN.md §D; DA gate ran to DA-verdict-c3.md; the row's original
  "pending rec / running" wording was stale and is corrected here per MC 10059; Inc-2 COMPLETE
  per the MC 10030 row below). Two seed-notes claims refuted against code: no "6 weapon types"
  exist (grep weapon = 0 hits), AdaptationSystem is really src/dna/EcosystemAdaptation.cs.
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
- W3 FIX VERDICT BOARD CLOSED at ef816ec, one pinned tree, all three children on identical
  hash 340c6e2a (--base d9288c4): TEST-c2 PASS (full battery + N2-pin drift RED-probe),
  DA-c2 SHIP (all c1 items closed; every guide number cross-read vs code constants), ARCH
  PASS (5 dims; one NEW P3 A1 = §3 WorldDirector.Skills.cs row — landed next commit).
  Carries: 3915 (reward emitter + guide cap-location reword + F-key wording), +x on
  runtime_integration_test.sh + stale header (:17) next code round, economy consts literal-
  pin (cheap now), MC 3910 proof-size warning (565/600, split at next proof touch).
  Per-child loop dirs ruled DECLINED terminal (standing doctrine).
- W4 CLOSED (orchestrator-verified, 2026-10-02): MC 3910 (death_load owns its save: delete-
  before-write + PlayerHealth=42 stamp, DEATH_SAVE_OWNED leg; SIZE headers) + MC 3933 2f
  (SkillsPanel TAB + live HUD Manna/skills/ACTIVE-quest, legs E+F, zero new colors).
  Verdict board .audits/20261002-W4wave: TEST PASS + DA SHIP (hash 712e1df3), ARCH PASS
  (hash 046052 = same tree + my in-flight docs sync; delta docs-only, noted honestly).
  Docs synced by orchestrator per §G F1 (G1 six-readouts guide claim + TAB row + F-key
  wording + cap-location reword; G2 §2/§3/input-map rows; G3 tests/ui csproj exception).
  Comment-only residuals carried to MC 3915 (G4 518->539, G5 1280x720, F2 --wait, DA F1
  director-DNA gate leg). 7-gate battery green at 23472da on my runs.
- MC 3943 stage 2g DELIVERED (code, 2026-10-02, this commit): follower roster cap 3
  (owner D1 "All rec", 4th REFUSED) — src/companion/CompanionRoster.cs (pure) +
  world/WorldDirector.Roster.cs (live loop MOVED from root 539->501l, SIZE honest);
  recruit = interact-wild + FIRST wage via existing pay_wage; INDEPENDENT per-follower
  wages, wage seam re-keyed off singleton (W2, each settle emits own WagePaid); UNIQUE
  bus keys <name>-<EntityId> deltas first + roster-mean LAST under reserved "roster"
  (D6, no Hud edit; F2 key-ignored pinned on real QuestLog); cycle_follower/break_bond
  actions, Forgive/PermanentBreak glue in Roster partial only (EmpathyPanel untouched);
  save Followers fills N, load restores N; roster[0] visible (boot machine IS roster[0]).
  Gates all 0 on this tree: ci/roster_test.sh two-pass (14 green, self-test red), full
  runtime_integration_test.sh incl. RosterIntegrationProof roster_follow + roster_neg
  (NEG_ROSTER), 14 regression gates, planted-bad x2 RED-named + byte-identical revert.
  Deviation: standalone ci_proofs/RosterIntegrationProof.cs (RuntimeIntegrationProof.cs
  2g-forbidden, partial cannot register modes; Godot file==class rule). Verdict board:
  parent's wave. Docs (docs/ARCHITECTURE.md roster row + input map C/J) owed at close.
- W5 CLOSED (orchestrator-verified): MC 3943 2g roster (0b6df84: CompanionRoster + Roster
  partial, cap 3, per-follower wages re-keyed off singleton, bus keys <name>-<EntityId> +
  mean LAST under "roster", cycle_follower C / break_bond J, save N) + fix round (607ac53:
  DA W5 F1 betrayed-guard lost in move [P1 — betrayers re-payable/forgivable, quest-fed],
  F2 BusKey latch, F3 id-allocator seed, F4 load cap). DA W5 board: TEST PASS, DA FIX
  (caught F1 P1), producer fix verified (my battery: roster/companion/quest/save/runtime
  ALL 0 at 607ac53 incl PAY_AFTER_BREAK_REFUSED leg). Final verdict wave at 607ac53 +
  docs rows (this commit) owed at close. Docs synced: §2 roster, §3 partial row + input
  map C/J, §6 roster_test + RosterIntegrationProof, §8 load cap; player-guide roster
  section + C/J rows + mean-hearts truth.
- W5 remediation round CLOSED (orchestrator-verified, 2026-10-03): a64e401 (F-A fix — runtime
  oversized-save leg hand-writes a 5-entry save, real load, asserts N=3 + bodies=3 + DROPPED
  marker; TEST-c3 F-A closed empirically) + 3ea8c63 (docs truth: hearts-mean counts betrayer
  at zero until save+load drops it, recruit=E+P, J book-gate, §3 guard-pin note; DA c2 NF1-NF4
  closed). Final board at 3ea8c63, ALL THREE on identical hash 88b9e623: TEST-c4 PASS + DA-c3
  SHIP + ARCH PASS. Proof at 600 EXACT ceiling — split owed at next touch (own partial halves,
  RuntimeIntegrationProof idiom). Carry: NF5/NF6/NF7/NF9 P4 latents (hardening backlog),
  F-A-era P4s endorsed no-action (err RED). Banner + §6 leg enumerations restamped by
  orchestrator at this close.
- W6 CLOSED (orchestrator-verified, 2026-10-03): MC 3915 reward-beat emitter — c218d3b (emitter on
  QuestStatus.Completed rides DialogueSystem, fromReward guard skips _nodesSeen so reward chatter can
  never satisfy DialogueShown objectives) + 7394387 (dna_speak false-green kill: Check now requires the
  interact path's own npc_ node) + e12eb4e (c3 beat-coalescing doc generalization) + 4e119b0 (c4:
  player-guide playback truth + coalescing boundary corrected FRAME-granular, pay+attack co-press
  lost-render case named). Board .audits/20261003-W6wave: TEST-c3 PASS + DA-c3 SHIP + ARCH PASS, ALL
  THREE on identical hash 0e85c22f at 4e119b0 (c1-c2 caught F-1/F-1c2 doc-lies, closed by c3/c4; TEST-c2
  proved the false-green class real). Owner ruling (2026-10-03): beats -> "Queue + auto-close box" =
  NEW FEATURE owed (card under new-board anchor 10026; docs' "pending/planned" wording flips there).
  [SUPERSEDED 2026-10-04 MC 10059: shipped as MC 10026.1 DLQ at 4fece21, closed 7079a00 — see the DLQ
  row below; guide wording flipped at b50f9a7 (MC 10031 close).]
  Carries: wiring-lambda guard residual (accepted), ci/*.sh mode 644 chmod owed (2h), pending-wording
  staleness at DLQ card. Banner restamped at this close.
- DLQ CLOSED (orchestrator-verified, 2026-10-04): MC 10026.1 dialogue lifecycle — 4fece21 (DialogueSystem
  FIFO _pending queue: Show R1-R5 first-match, R3 head-take re-queues old head for full dwell; ONE pop
  point; uniform auto-close as the ONLY _Process Close (rev-4 F-M); RewardLineFrames=240 painted-tick TTL;
  ClearPresentation() on load restore, queue never persisted) + quest_arc 4 legs w/ named planted-bads
  (owner rulings "Queue + auto-close box" + "All dialogue lines" — DA P3-α: R5 stays UNCOVERED by the
  legs, disclosed in gate banner). Wave .audits/20261003-DLQwave at 4fece21: TEST PASS (own battery +
  falsifiability a/b/c2/d each exit-1 named-FAIL + green-after-revert, determinism 44-marker cmp
  byte-identical, units 46/46 + ui + story 14/14) + DA SHIP (all 8 axes re-derived; R3 Add+RemoveAt ==
  MoveItem proven) + ARCH PASS (docs = code truth, stale-claim sweep clean), pins fa41ada0/18ad2292
  reproduced by orchestrator on clean tree. No P0-P2. Rides: P3-beta design rationale wording (design
  pinned), P3-gamma SPEAK-leg latch hardening recipe (future wave), 4xP4; NF5-9 latents unchanged.
- MC 10031 Calming Speak (03367376): third launch skill live — F/skill_3 (2e
  reserved binding, map zero-diff), 12 Manna spent AFTER the target scan
  (refusal spends zero), opens the SAME RecruitOffered behind a 600-frame
  window (9.0 reach); pay_wage -> TryRecruitOfferedWild stays the SOLE join
  authority; runtime-only state, save schema v3 zero-delta; load seam
  ClearCalmWindows (ONE caller). skill_test F7-F9 + calm_use/calm_neg green
  (TEST-verdict PASS, planted-bad reds in evidence/). Docs rows closed per
  DESIGN §8 (this row's commit).
- MC 10030 clean-clone sweep (20d3bf1): reproducibility proven — 19 gates x 2 CLEAN
  clones (branch-tip + remote-master b50f9a7) all exit 0 incl. full runtime battery
  25 modes serial (603s/601s) + export_check + determinism cmp IDENTICAL; ci
  runtime_integration_test.sh exec bit +x in git; banner header now states the true
  25-leg count (comment-only diff, proven zero non-comment lines); strays gone
  (890.15 root note + skeleton/_scratch, LibDiag zero refs); orphan .uid census: all
  94 tracked .uid have siblings -> zero removals (count corrected 2026-10-04 MC 10059: the
  tracked tree at 20d3bf1 holds 108 .uid with zero orphans — re-counted via `git ls-tree -r
  20d3bf1`; conclusion unchanged; later cards added more — 115 tracked at 5922b25, incl. the
  6 strays from a36fa0d); screenshots meadow/canyon/ruins/
  hud under run dir (never committed); §9 stale WageBetrayalPillarTests 0600 row
  removed (file is 644 claudecode). Inc-2 COMPLETE.
- MC 10079 spawn-order fix (3eaca1e): SpawnWildFollower AddChild BEFORE GlobalPosition
  (Godot 4.7 tree-entry requirement) — battery warning 11 -> 0 hits, placement final
  value unchanged (^ROSTER stream byte-identical), battery+roster gates green before
  and after; row retroactively appended at MC 10080 close (close-commit of 10079
  carried no LEDGER row — doctrine slip, declared).
- MC 10080 roster split (903b9a6): WorldDirector.Roster.cs (467l, header CLAIMED 422 — the
  lie died) split PURE into roster-core half (331l) + WILD half
  world/WorldDirector.Roster.Wild.cs (158l: offer pair, TryRecruitOfferedWild,
  TEST-seam SpawnWildFollower, TryRosterNpc,
  ClearCalmWindows); 25 member decls byte-identical before/after, both halves under the 400
  ceiling, engine .uid in-commit, csproj explicit Compile (file is NOT glob-based).
- MC 10058 gate-fix (runtime leg, 2026-10-04, code seat VM350, audit Top-2b):
  runtime_integration_test.sh ENEMIES_EXIST_TARGETED leg revived — it ended
  `|| true` (can never fail) and the marker printed nowhere in the repo, so
  the assert moved into the proof: after the DNA kill stage a live
  director-set enemy must exist AND its director-fed AI target
  (EnemyActor.PlayerTarget*, fed every frame by WorldDirector._Process) must
  track the live player position (tol 0.05) before the marker prints; the
  gate line is now a named non-zero fail. Planted reds: marker print removed
  with proof exit 0 -> gate red; sabotaged aim assert -> proof red (marker is
  not free). Real-run aim reads == player exactly. Full battery 25 legs green
  (the one run). Proof file at the 600-l test ceiling, header reason updated.
- MC 10058 gate-fix (smoke leg, 2026-10-04, code seat VM350, audit Top-2a):
  smoke.sh render leg now REJECTS a boot-splash frame — the old bar (helper
  default min-colors 8) PASSed on the Godot splash (colors=495
  stddev=0.127484, stats byte-identical to the audit's own capture). The leg
  passes --min-colors 1200 to the EXISTING graphical-test-helper content bar
  (no new mechanism): live game frames measure 2380-3038 colors / ~0.27
  stddev at this HEAD. Planted red (12 s _Ready splash hold: pre-fix smoke
  FALSE-green on the splash, fixed smoke FAIL; plant reverted), real smoke
  green colors=2967. Audit finding (d) JUDGED-pin identical across design
  revs inspected: mechanism is fleet-side dod_judged_hash.py (its RUN_DIRS
  skip makes .audits deliverables invisible to the pin) — DECLARED to the
  orchestrator, no sibling mechanism authored game-side.
- MC 10058 fix-cycle (verdict wall, 2026-10-04, code seat VM350): PREFLIGHT §4
  sample annotated pre-MC-10058; close-evidence copied into run dir evidence/;
  revived-leg gate FAIL text made token-free (marker only in pass-grep + proof
  print — shell probe red/green PROBE_EXIT=0); smoke --wait 6→10 (single-
  capture false-red risk), bar stays 1200; smoke re-green SMOKE_EXIT=0
  colors=2967; battery not re-run (shell/docs-only, no proof .cs touched).
- MC 10059 record de-stale (tech-writer VM350, 2026-10-04): this pass corrected the stale/
  contradicted claims named in its close evidence (.tmp/10059-close-evidence.md) and did the
  hygiene the record depends on — 31 pre-rule tracked .audits files untracked via
  `git rm --cached` (files kept on disk; `git ls-files .audits` = 0 after), and the stray
  `.audits/legacy-phase2/` (leftover from the 2026-09-24 layout-v2 migration) was removed from
  disk: its seat dirs were CONTENT-HASH-audited against all of `.audits` (66 unique non-cache
  files — the only copies of the 839/890-era seat reports, zone/HUD captures, the MC 890.8
  bake-repro harness and two M13 windows binaries + smoke png) and archive-moved, not deleted,
  into `.audits/legacy-svarkor-last-animal-integration-audit/legacy-phase2/`; the two
  `_cleanclone-test/` clones inside it (HEADs 67cd759 + ffb1e44, both ancestors of this row's
  HEAD, zero untracked evidence, zero unique commits/stashes) were deleted. Size before/after:
  `.audits` 6.5G → 345M, `legacy-phase2` 6.3G → gone (archive ~150M rides inside the 345M).
  Card 10036's dangling citation (pre-amend 2a81b91) re-cited here to its current reachable
  descendant 3514d92 (verified `git log --all`; amend diff = docs/ARCHITECTURE.md +4/−1 only).
  Rebased on top of the MC 10058/10079/10080 rows — none of those rows reverted.
- [SUPERSEDED 2026-10-04 by OWNER ACTION — not by this seat. The old account is DELETED; the deletion
  itself is the literal owner artifact (MC 10093, commit fad172d), so the archive+pointer decision
  below lost its object. Text kept verbatim as history. See the two dated 2026-10-04 rows below.]
  OPEN OWNER DECISION (NOT executed, MC 10059 — owner ratifies separately): the old public repo
  `Last-animal` on the parallel orchestrator's old account (master 9f4173f — 62 commits behind canonical
  `5922b25` at the 2026-10-04 audit; corrected 2026-10-04 fix-cycle-2 from the stale "50": instruments
  `git rev-list --count 9f4173f..5922b25` = 62, `git rev-list --count 9f4173f..7a3c755` = 65, both
  pinned to shas — never `..HEAD` per DA-c2 F-c2-2; the copy the owner downloaded
  2026-09-28 — per the read-only workspace audit of 2026-10-04, §1/§3/§6) disagrees with the
  canonical `bryn1/Last-animal` (master `5922b25` as of 2026-10-04, dated per DA-c2 F-c2-1; current
  truth: `git ls-remote bryn1 master` = `fad172d8c01b65ffee78e628db253a131bebf3a1` as of 2026-10-04
  19:00 UTC, re-run this session — the undated pin read as present-state and was CONTRADICTED).
  Repo-vs-served disagreement = OWNER decision
  (doctrine rule 2 — no artifact = no decision). NOT TOUCHED (parallel orchestrator's account;
  this seat never pushed there). SUPERSEDED 2026-10-04: that account is deleted, so its repo is
  gone and the archive/pointer-README option no longer applies — `bryn1/Last-animal` is now the
  only home (audit §6 rec.4 mooted; MC 10093).
- MC 10059 FACT-UPDATE (tech-writer VM350, 2026-10-04 19:00 UTC, orchestrator-authorized): the old
  account is DELETED — owner action; the deletion itself IS the literal owner artifact (rule 2
  satisfied), so no separate ratification of the archive+pointer option is owed. The OPEN OWNER
  DECISION above is SUPERSEDED by that owner action, NOT by this seat (nothing here executed).
  Cites: MC 10093 (the account deletion) + commit `fad172d` ("remove svarkor-ai provenance note
  (account deleted, MC 10093)"). `bryn1/Last-animal` is now the only home.
- MC 10059 fix-cycle 2 (tech-writer VM350, 2026-10-04 19:00 UTC): re-applied the cycle-1 corrections
  to the fad172d LEDGER rewrite — corrected the stale "50 commits behind" on the OPEN OWNER DECISION
  line to the re-run instrument (62 to `5922b25` / 65 to `7a3c755`, pinned to shas, never `..HEAD` —
  DA-c2 F-c2-2), and dated the remote-master pin (DA-c2 F-c2-1: `5922b25` as of 2026-10-04;
  `ls-remote bryn1 master` = `fad172d8` 2026-10-04, run this session). Re-verify over DA-c2:
  `.audits/202610041822-10059-da-c2/DA-verdict-c2.md` (JUDGED
  `faa820d35666fda8a57ee2b23819353ec1ed8ca504231d2814be5cd786649f82`; digest + workspace-path form —
  `.audits` is untracked by design, `a06ecda` / `.gitignore:21`).

- MC 10098 S0 bus-emit seam (Inc-3 W0, 2026-10-04, code seat VM350, branch
  vm350/s0-bus-emit): EventBus +4 ADDITIVE signals/emits (DialogueShown/
  DialogueClosed/PlayerHurt/BossFallen — 11→15); emitter = per-frame edge-detect
  poll inside the TickUi seam (WorldDirector.Ui.cs), DialogueSystem/queue/
  BossController file-ZERO (F3); F-2 obeyed — BossFallen edge-detects the
  TRACKED BossActor IsDead, zone-exit null-swap re-arms silently. Split duty
  executed: RuntimeIntegrationProof.cs (was AT 600l) 445l, stages 1-4 moved
  VERBATIM to Chain.cs, bus_emit stage machine in Bus.cs (csproj+uid in-commit).
  Planted-bad red→green x3 (re-poll double-emit hurt=31 RED / missed close
  "close edge MISSED" RED / HasLiveBoss-flip FALSE BossFallen on zone exit RED
  — all green after byte-identical restore, evidence/.tmp cmp). Gates F5
  single-mode: boot 0, audio 0, bus_emit 0 (BUS_SHOWN_ONCE, BUS_CLOSED_ONCE,
  BUS_HURT_ONCE, BUS_NO_FALSE_FALLEN, BUS_BOSS_LIVE, BUS_FALLEN_ONCE); split
  regression 9 modes re-run expected-exit incl. save under SAVEGATE MUTEX.
  Battery leg append + leg-count/§6 restamp at WAVE CLOSE (not here).
  Evidence: .tmp/10098-close-evidence.md.

- MC 10099 / 10026.13.2 S7 latent closure + the cmp tool (code VM350, 2026-10-04, Inc-3 W0):
  (a) F-4 pinned — multiplier literal `3` asserted in SkillEconomyTests F3 AND the skill_use
  proof leg; plant 3→4 went RED at BOTH (the derived-vs-literal split made delta=80 FAIL while
  every derived assert stayed ok — TEST-verdict-10030 F-1 closed empirically). (b) CSK P4-1
  VERIFY-AND-DROP: the CALM_WINDOW_EXPIRES/CALM_LOAD_CLEARED battery greps already ship at
  runtime_integration_test.sh:236-237 — absent latent, zero change, grep recorded. (c) NF9 dead
  assertion repaired — RosterTests cap test now pins the per-iteration TRUE/FALSE contract;
  cap-guard off-by-one plant RED at RosterTests.cs:435. (d) P3-γ SPEAK latches landed (recipe
  as pinned): (a)-pass latches the (b) transition, intro paint latches (c); the named
  drain-threshold-decoupled plant slips PRE-LATCH code through green (control log) and goes RED
  latched; drain-paint-drop plant RED via the (c) FAIL. First plant attempt (uniform-close
  threshold) proved behaviorally INERT — branch is empty-queue-gated; recorded, v2 plant used.
  (e) NEW ci/determinism_cmp.sh — the F4-CMP comparator (TOOL, not a battery leg; gate-LIST
  restamp owed at wave close): (i) byte-stable streams RED on any delta (^ROSTER digit-flip
  plant RED), (ii) dist/position-field LA_GATE lines numeric-masked (drift allowed, 10080 §2
  noise class), masked hunks RED unless --declare. Classes KEYED ON FIELD NAMES — the tool's own
  first A/A caught its float-shape keying flipping a line whose 0.### render collapsed to an
  integer (dist0=3.997 vs dist0=4); field keys, restated, A/A GREEN. Gates green this card:
  skill_test 24 + roster_test 18 + quest_arc/skill_use/calm_use single-mode + determinism_cmp
  A/A (positive) — savegate mutex honored (all save runs in mkdir-locked windows). D-2 honored:
  asserts only, ZERO balance change. Evidence: .audits/20261004-1846-s7-latent/evidence/.

- MC 10097 / W0 wave-close DRIFT restamp (design VM350, 2026-10-04, Inc-3 W0): docs-only,
  ZERO gate/godot runs. The three owed drift rows, each owed by a wall verdict, discharged:
  (1) ARCHITECTURE §3 EventBus census restamped 11→15 from the tree (grep
  `[Signal] public delegate` skeleton/autoload/EventBus.cs = 15 at a4c7155); the four S0
  presentation signals (3bfcee5) listed additive and the `PlayerHurt` Int-payload exception
  stated honestly — owed by ARCH-verdict DRIFT-1, .audits/202610041758-e91b0813/ARCH-verdict.md.
  (2) §6 proof enumeration now lists the S0 partials Chain.cs (stages 1-4 verbatim) and Bus.cs
  (stage 80 bus_emit, markers incl. BUS_BOSS_LIVE per FIX-1) with the 600-l proof-ceiling
  context — same DRIFT row. (3) §6 ci/ inventory now carries `determinism_cmp.sh` marked TOOL,
  not a battery leg (the guard wiring around it, card 10111, is in flight — NOT claimed here);
  owed by .audits/202610041758-1e37b441/ARCH-verdict.md §6 DRIFT ROW.

- MC 10111 / 10026.13.3 determinism_cmp content guard (code VM350, 2026-10-04, Inc-3 W0):
  DA P2-1 closed (verdict 202610041758-1e37b441) — the comparator can no longer go GREEN on a
  comparison that proves nothing. Two content guards at cmp_pair entry (the one path both --cmp
  and live A/A share): (a) a side keying ZERO lines is RED ("0 lines, IDENTICAL" was an
  empty-vs-empty vacuous pass); (b) a side carrying the harness's literal FAIL banner
  'LA_GATE: FAIL — ' is RED, naming the side — exit-code equality alone was not proof because
  _Finalize PrintErrs its banner WITHOUT Quit(1) (RuntimeIntegrationProof.cs:408-409; the same
  literal is Fail()'s banner at :442). Still a TOOL: battery legs and the ci gate-LIST untouched.
  Planted pairs: identical FAIL-pair RED naming the side (both banner forms), empty pair RED;
  identical GREEN pair and the S7 builder's real positive A/A pair stay GREEN exit 0. Before/after
  proof: the BASE a4c7155 tool compared the SAME identical FAIL-pair GREEN (exit 0) — the hole was
  real and is closed. skill_test 24 + roster_test 18 GATE PASS save-free. Tool 139 lines.
  Evidence: .audits/20261004-2147-10111-cmpguard/.

- MC 10112 / 10026.13.4 W0 barrier drift (code VM350, 2026-10-04, branch vm350/10112-barrier):
  three battery concerns, one root-cause + one fix each, no bundling. (C1 quest_test exit 1:
  the bus-batch contract test still pinned the 2c count 11 while RATIFIED S0 (3bfcee5, wall
  PASS bd4c9255) carries 15 [Signal] delegates — TEST-UPDATED to 6 pre-2c + 5 quest + the 4
  S0 signals NAMED explicitly (DialogueShown/DialogueClosed/PlayerHurt/BossFallen); a
  non-ratified sixth still trips the count (plant RED logged, removed green). (C2 bridge_mvp
  exit 139: MECHANISM VERIFIED — BridgeMvpProof is the C# MainLoop and its live-scene fields
  root the scene's C# wrappers past native teardown; their finalizers then hit freed ObjectDB
  entries (csharp_script.cpp:179 leaked-unsafe-reference; abort shape non-deterministic 134
  or 139). FIX (stands): ReleaseHeldRefsBeforeQuit() at the PASS Quit — all 7 wrapper fields
  cleared, GC-flush while the ObjectDB is alive; TEST stress 5/5 green zero leaked lines,
  plant drop-call -> RED detected + RESULT=PASS render bar. WHICH S0 HUNK shifted the GC
  timing: INFERRED, demoted per DA JUDGED 241cd301 — bisect COLORS are VERIFIED (red at
  3bfcee5 + a4c7155 deterministic, 2x each; green at 6db030c + S7 f1892f4, 2x each; full-S0
  revert control green) and two ablations are VALID controls (proof ran, 51 leak lines):
  PollBusEmits-call-off still red, Ui.cs-only pre-S0 revert still red. But the minus-signals
  (expC) and the first Ui.cs-revert (expB variant) logs are BUILD-FAILURE logs ("build
  callback failed", zero leak lines — proof never ran): INVALID controls. "Not the new
  signals" rests on an invalid control; "not the proof split" was never ablated (sub-reverts
  don't compile — proof.cs calls the Bus-partial stages); the surviving csproj +2 Compile
  lines could BE the timing shifter. TEST T-1: plant detection measured 1/4 — a SINGLE green
  bridge_mvp is weak evidence; run the gate N>=5 in series before trusting a green series.
  No shipping-scene instance of the pattern: grep-VERIFIED (wall).
  (C3 export_check aborted line 47: NOT reproducible solo — identical command AND the full
  gate exit 0 on a4c7155 and on this branch (PE magic + assembly/GodotSharp/runtimeconfig
  markers, 135.7 MB exe). toolchain.sh TMPDIR guard did NOT fire and was never in play:
  /tmp/godot-publish-dotnet is claudecode-owned WRITABLE (the guard handles non-writable
  only) and line 47 PRECEDES any publish. fad172d hunks are company_name strings. Battery
  log mtimes prove the W0 barrier ran legs CONCURRENTLY on the SAME project dir (runtime
  integration still writing 21:29 while save/skill logs closed 21:22) — export_check's
  rm -rf .godot + headless --build-solutions racing sibling legs is the INFERRED cause; the
  gate's >/dev/null swallowed the abort reason (observability gap for the battery harness,
  not fixable from the repo side). Battery runs legs one-at-a-time -> this leg exits 0.
  Gates green on this branch (all solo, savegate mutex held 21:37–close): quest_test
  QUEST_EXIT=0 (46 passed), bridge_mvp BRIDGE_EXIT=0, export EXPORT_EXIT=0.
  Evidence: .audits/202610042128-e55426c7/evidence/10112-evidence.md (+ copied logs).

- MC 10122 / 10026.15.3 S4 music ducking + bus mix (code VM350, 2026-10-05, Inc-3
  W1, branch vm350/s4-duck): MusicManager per-bus const levels table + S4 duck —
  -8 dB while the S0 DialogueShown/DialogueClosed bracket holds (subscribed
  SfxRouter-style in SubscribeMix, self-wired from Boot via /root/EventBus; zero
  edit to DialogueSystem), -12 dB under SetBossStance (boss wins over dialogue),
  release decays on a 30-frame INTEGER counter (F4; no Tween/Timer/delta).
  Audio state runtime-only (F2 zero save delta) and the _Process path prints
  nothing, so the marker streams stay untouched (F4-CMP) — legs assert bus
  STATE through the audio gate. audio_test.sh +3 named legs (pattern copied):
  MIX_DUCK_ON_SHOW/MIX_DUCK_OFF_CLOSE/MIX_BOSS_STANCE green; planted-bad
  (never-release mutation) -> MIX_DUCK_OFF_CLOSE RED, gate exit 1, mutation
  reverted; audio_test + boot_test solo exit 0 on final tree, existing SfxRouter
  legs green. Evidence: .audits/20261005-0111-s4-duck/EVIDENCE.md (+3 logs).

- MC 10103 (10026.12) S0-wall DA P2 bus presentation-edge fixes (2026-10-05, code seat VM350,
  branch vm350/10103-bus-edges): F-A — same-frame kill+travel could SWALLOW BossFallen (kill at
  _Process :249 precedes travel's ClearZoneEnemies swap; the FIRST poll that could observe
  IsDead never happened — the Ui.cs swap branch re-armed without emitting). Fix: the swap branch
  now emits the swapped-OUT actor's unemitted death before re-arming (IsDead gates — an ALIVE
  swap still never emits, F-2 unchanged; reads are pure-managed, safe post-QueueFree). F-B —
  loading a LOWER-HP save emitted a FALSE PlayerHurt (restore-lowers-Health read as damage by
  the frame-boundary poll). Fix: SaveLoadController gains the BusHurtRebaseline seam (same
  injection idiom as Manna/QuestStates), invoked after _restoreHealth; the Ui partial wires it
  BEFORE the UICanvas guard (bus poll runs in no-UI compositions) and re-arms _busHealthBaseline.
  Proofs EXTENDED in the existing bus_emit stage machine (no parallel harness): phases 17-22 F-A
  named F-act BUS_FALLEN_SWAP (release-then-press edges — a held travel produces no JustPressed
  edge, CalmingSpeak idiom; BUS_SWAP_STRIKE proves the same-frame window opened; absorbed-hit
  leg reruns, never asserts on a missed window); phase 23 F-B named F-act BUS_NO_LOAD_HURT
  (F4 frame-boundary sampling honored: >=2 polls must observe the HIGH HP before the load drop;
  owns its save delete-first per MC 3910). Plant REDs on this branch: F-A ->
  "F-A: boss died unemitted — the same-frame swap swallowed BossFallen" exit 1; F-B ->
  "F-B: loading a lower-HP save emits NO PlayerHurt edge" FAIL hurt=1 (payload = restored 70),
  exit 1. GREEN: LA_GATE_MODE=bus_emit exit 0 — all six S0 markers byte-identical +
  BUS_FALLEN_SWAP + BUS_NO_LOAD_HURT. All runs solo under the SAVEGATE mutex (/tmp/la-savegate.lock).
  Regression: ci/smoke.sh exit 0 (render RESULT=PASS colors=2968), ci/combat_test.sh exit 0
  (21 passed; harness self-test red leg as designed). F-B harness lesson (VERIFIED): the
  frame-boundary poll samples, it does not watch — an intra-frame restore+damage round trip
  shows NO edge; a false-edge proof must spread the writes across observed ticks.
  Evidence: .audits/20261005-0049-10103-bus-edges/ (EVIDENCE.md + 5 logs).

- MC 10117 / 10026.13.5 rooted-fields-at-Quit latent sweep (code VM350, 2026-10-05, branch
  vm350/10117-latent-sweep): the MC 10112 class extended to the sibling harnesses, idiom REUSED
  (ReleaseHeldRefsBeforeQuit — null live-scene wrapper fields + GC.Collect/WaitForPendingFinalizers
  while ObjectDB alive, BEFORE Quit). Three sites FIXED, zero SAFE-BY-CITATION: (1)
  RuntimeIntegrationProof stage-6 PASS Quit (:358) — the shared PASS exit for positive/save/
  dna_speak/quest/skill/calm (all partials end _stage=6 — grep VERIFIED their only own Quits are
  Fail/Quit(1)); (2) RuntimeIntegrationProof.Bus.cs bus_emit PASS Quit (:337) — bypasses stage 6,
  fields declared in the entry partial, call added; (3) P1FixProof all four PASS Quit(0)
  (:156/:198/:240/:287) — every mode roots Compose()'s bus/director/player/empathy/main, plus
  _victim (corpse) and _bootEnemies (zone_travel). Detection (T-1): plant-drop RED DID NOT FIRE
  at N=20/15/20 headless (+5 positive X-render) — and pre-fix baseline is likewise 0/5 green:
  at master 99d457d these legs sit BELOW the flush threshold (INFERRED; the W0 shifter colored
  the bridge leg, not these). Structural rootedness at every Quit is line-cited VERIFIED, so the
  sweep ships the idiom as the shared-mechanism corollary demands; plant non-detection recorded,
  not a false RED. Restored GREEN 15/15 + head-check 2/2, zero "Leaked unsafe reference".
  Regression ci/bridge_mvp_test.sh BRIDGE_EXIT=0. runtime_integration_test.sh full battery NOT
  run (orchestrator owns). Evidence: .audits/20261005-0035-10117-latent-sweep/ (91 logs,
  EVIDENCE.md VERIFY_EXIT=0).

- MC 10123 / 10026.15.4 S6 HUD tokens + readability (code VM350, 2026-10-05,
  Inc-3 W1, branch vm350/s6-hud): NEW src/ui/UiTheme.cs const class — the single
  literal home for the three panel files (gauge colour/font, Empathy colour/font,
  byte-equal to the literals they replace: a single-source tidy, not a restyle;
  csproj Include + .uid same commit). Hud.cs: the Life DIGITS VISUAL-ease to the
  exact value within 10 redraws on an INTEGER counter (F4; CalmedWindow idiom, no
  Tween/Timer/delta) — logic values untouched, Manna digits stay EXACT (plan §G
  D4 freshness contract). R6 vignette: a NEW ColorRect "Vignette" layer (first
  child, MouseFilter Ignore) under the readout labels, alpha triangle-wave pulse
  on the redraw counter while the Life readout is strictly <30, alpha EXACTLY 0
  at/above (reads the health mirror, never writes game state — I4). NEW
  HudVignetteCaptureTest.cs (+.uid+csproj). ui_test.sh +2 named legs:
  UI_TOKENS_SINGLE_SOURCE (panel files raw-literal-clean) + HUD_VIGNETTE (on<30 /
  off@30 boundary + far-corner red-mean tint pixel non-blank: mean 0.28@29 vs
  0.10@30, delta 0.18); planted-bad #1 (raw literal reappears in Hud.cs) ->
  UI_TOKENS_SINGLE_SOURCE RED exit 1, planted-bad #2 (< flipped to <=) ->
  HUD_VIGNETTE RED exit 1, both mutations reverted; ui_test + boot_test +
  main_composition_test solo exit 0 on final tree, existing legs A–F unchanged
  green. Bonus AudioTest.cs comment hygiene = VERIFIED NO-OP (comment already
  correct at master; no misleading wording ever committed — see EVIDENCE). SAVE-
  FREE (only save_game input writes user://, no leg presses it) — no savegate lock.
  Evidence: .audits/20261005-0142-s6-hud/EVIDENCE.md (+logs, 2 capture PNGs).

- MC 10120 / 10026.15.1 S1 hit-flash + VISUAL knockback (Inc-3 W1, 2026-10-05,
  code VM350, branch vm350/s1-hitflash): 6-frame white flash + 6f VISUAL-NODE
  offset punch on the composed enemy root — JuiceTuning consts = RULING-1 face
  (ratified 6f/+6f; PunchDistance 0.35 m declared). Node3D has no Modulate: the
  per-enemy SHARED StandardMaterial3D MaterialOverride is the only Modulate
  analogue on the MeshInstance3D tree — flash = white tint of that one instance,
  exact stored colours restored at +6f. Punch = Visual root local Position only
  (presentation transform; boss Scale + LookAt orthogonal, body never written).
  Visual root becomes VisualJuice (partial Node3D, INTEGER-frame decay, F4
  CalmedWindow idiom; exact base return, no float accumulation). Trigger =
  3-line hunk at the ONE DealDamage site (TryAttack, WorldDirector.cs): the
  race-ledger's WorldDirector.Skills.cs guess is SUPERSEDED by reading — the hit
  is known only there; merge order S8-after-S1 unaffected. EnemyActor.cs ZERO
  diff (verified). JUICE_HITFLASH leg appended per the leg mechanism (+1: 26
  legs = 16 positive + 10 negative; proof partial RuntimeIntegrationProof.Juice.cs,
  stage 95, real attack wire): target physics frozen BEFORE the hit (juice is
  then the ONLY candidate body-mover) — asserts flash active + punch off base
  on the hit frame, BOTH still on at +4f (no early decay), BODY GlobalPosition
  EXACT-equal at +4f and +8f, punch + tint at EXACT base by +6f
  (JUICE_BODY_STILL position= rides the masked (ii) class). Planted-bad (punch
  ALSO applied to the body, decayed — the insidious variant) -> leg RED exit 1
  "BODY MOVED during the juice window" (log in evidence; reverted, green
  re-proven). determinism_cmp A/A JUICE_HITFLASH under savegate: (i) 11 lines
  byte-identical, (ii) 1 masked — DETERMINISM: IDENTICAL. Solo (savegate held
  for legs+cmp): JUICE_HITFLASH green; regression boot_test / smoke /
  main_composition_test / combat_test exit 0. Full battery NOT run (master-
  only per F5; header leg-count carried to wave-close restamp as 26=16+10).
  Evidence: .audits/20261005-0116-s1-hitflash/ (EVIDENCE.md + logs).
