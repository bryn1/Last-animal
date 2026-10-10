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
  400-line ceiling (header reason stands — 506 l re-derived at the MC 10167 W4
  close; was 501 l at 5922b25, +4 l S1 trigger hunk a6704d0, +1 l W1 comment-only
  F-DA1 note 87ba446 — SIZE header restamped in the W4 close commit).
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
  Cites: MC 10093 (the account deletion) + commit `fad172d` ("remove <retired-account> provenance note
  (account deleted, MC 10093)" — subject quoted with the retired account name dropped per MC 10082; fad172d itself is history, untouched). `bryn1/Last-animal` is now the only home.
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
- MC 10121 (10026.15.2) S2 camera shake (2026-10-05, code seat VM350, branch
  vm350/s2-shake): FollowCamera SUBSCRIBES S0's PlayerHurt/BossFallen — SfxRouter-style
  consumer wiring on the autoload bus, with the MC 1344.1 deferred-resolve idiom (headless
  --script loads autoloads AFTER scene _Ready: _Process retries until wired). Shake:
  amp <= 0.15 m (JuiceShakeTuning.ShakeMaxAmp), 12-frame INTEGER-counter decay (F4 — zero
  delta-time/Tween/Timer in the juice path); the kick is ADDED on top of a NEW
  _followBase (the smoothed follow position, previously folded into GlobalPosition) and
  never written into it, so the base return at window end is bitwise EXACT (proof asserts
  GlobalPosition == _followBase). Camera prints NOTHING (F4-CMP, DA-inspection: zero
  GD.Print in the shake path — every marker is proof output). The ratified JuiceTuning
  block is NOT on master (S1 unmerged) — shake consts live in world/FollowCamera.cs as
  JuiceShakeTuning with JuiceTuning-consistent naming; orchestrator folds them into S1's
  JuiceTuning at merge (task-card note honored; distinct class name = no merge type-collision).
  Leg JUICE_SHAKE (+1, run_mode mechanism, stage 96, partial
  RuntimeIntegrationProof.Shake.cs + .uid + csproj Compile same commit): hurt arm driven by
  the REAL wire (PlayerModel.TakeDamage -> S0 Ui-poll emit), window HELD +4f (camera off
  base), EXACT base return by +12f (+2f readout margin), BossFallen arm reacts too (direct
  bus emit = AudioTest consumer-test idiom; emit AUTHORITY stays S0's, proven by bus_emit).
  Spawning off at boot (bus_emit quiet-leg idiom — stray hurts would re-arm mid-assert).
  .sh hunk kept at the file END (S1's JUICE_HITFLASH hunk mid-file appends cleanly; leg-count
  + banner + ARCHITECTURE §6 restamp are wave-close duties per F5, deliberately NOT done here).
  GREEN: LA_GATE_MODE=JUICE_SHAKE solo exit 0 x3 (JUICE_SHAKE_ACTIVE/HELD/AT_BASE/
  BOSS_ACTIVE/BOSS_AT_BASE, zero leaked refs) + boot_test + smoke solo exit 0 — all solo
  under the SAVEGATE mutex (/tmp/la-savegate.lock; W1 S1||S2 serialization observed working,
  9 waits on a sibling holder). PLANTED-BAD: decay site replaced with delta-time decay
  (RoundToInt(ShakeFrames*delta*60)/frame) -> exit 1 RED (window collapsed inside a tick;
  named FAIL in log), revert, re-green after revert — all solo under the mutex.
  INCIDENT logged honestly: the first plant phase reverted with `git checkout --` while the
  S2 change was UNCOMMITTED and wiped the whole camera edit (boot/smoke then failed on the
  broken tree); file rebuilt and the full green series re-run (R1 logs) BEFORE any plant;
  plant revert thereafter by cp-restore (md5-checked). Lesson: COMMIT before mutating.
  Evidence: .audits/20261005-0221-s2-shake/ (EVIDENCE.md + series.log + logs/).
- MC 10121 (S2, follow-up) stage-96 PASS exit adopts MC 10117 rooted-fields release idiom (_shakeCam=null + ReleaseHeldRefsBeforeQuit(), 2 lines, cherry-pick c2369a1 delta applied by hand after rebase-base conflict; JUICE_SHAKE re-green on master this commit).

- MC 10119 / W1 wave-close restamp (design VM350, 2026-10-05, Inc-3 W1): docs restamp,
  base aa7ee5d; ZERO gate/godot runs; only code edits are comment lines (battery header
  + one WorldDirector note). (1) ARCHITECTURE §6 proof-partials inventory gains the W1
  halves RuntimeIntegrationProof.Juice.cs (stage 95, mode JUICE_HITFLASH) + Shake.cs
  (stage 96, mode JUICE_SHAKE); ALL proof wc counts re-derived at tip and the stale W0-era
  445/221/342 restamped: Proof.cs 510 l (SIZE header kept), Chain.cs 221 l (unchanged),
  Bus.cs 566 l (grew past its 342-l split size via MC 10103 edge fixes + MC 10117
  rooted-fields release), Juice.cs 126 l, Shake.cs 119 l, P1FixProof.cs 356 l — every
  proof file under the 600 ceiling (max = Bus.cs 566). NOTE honestly (TOOL/NOTE, not
  fixed here — comment-only budget was WorldDirector): Proof.cs's in-file SIZE header
  still reads 466 l (MC 10117's stamp) vs wc 510 at tip. (2) Battery header restamped
  27 run_mode legs = 17 positive + 10 negative (was 26=16+10: S1 had restamped its own
  leg from 25 onto 26 — wording restamped at W2 wave-close per the MC 10119 DA P3 dissent:
  git log -L of the header shows S1's stamp moved 25 → 26, "onto 25" mis-read;
  S2 deferred per F5; the merge left it one short — re-derived by anchored
  run_mode-count grep, not by the +2 arithmetic) + "exceed the 27-leg count". (3) §3
  signal census re-derived from skeleton/autoload/EventBus.cs: grep `[Signal] public
  delegate` = 15 — UNCHANGED (MC 10103 added no signal; S2 FollowCamera subscribes,
  never declares); doc already read 15, no edit owed. (4) §6 named-legs inventory gains
  battery legs JUICE_HITFLASH/JUICE_SHAKE (markers JUICE_BODY_STILL/JUICE_SHAKE_AT_BASE),
  ui_test runs G UI_TOKENS_SINGLE_SOURCE + H HUD_VIGNETTE (MC 10123 S6) and audio_test
  mix legs MIX_DUCK_ON_SHOW/MIX_DUCK_OFF_CLOSE/MIX_BOSS_STANCE (MC 10122 S4) — every
  string existence-verified in its named gate script before listing. (5) ONE comment-only
  touch at the world/WorldDirector.cs load arm: the ratified F-DA1 semantics (MC 10103
  DA, P4) — a same-frame load+damage suppresses that frame's PlayerHurt edge (payload
  unresolvable at frame granularity; the TickUi rebaseline owns it) — zero behaviour.
  Evidence: .audits/20261005-0250-w1-restamp/ (EVIDENCE.md last line VERIFY_EXIT=0).

- MC 10130 / 10026.15.6 S5 SFX coverage remap (code VM350, 2026-10-05, Inc-3
  W2, branch vm350/s5-sfx): SfxRouter routes ONLY the previously-unmapped
  signals, each exactly once, onto the five EXISTING streams (D-1 zero new
  assets — assets/audio/ untouched, git diff proves it). QuestStarted/
  QuestObjective/WagePaid -> loyalty, QuestCompleted -> dna_extract,
  EmpathyBookOpened -> dna_spoken, PlayerHurt -> betrayal, BossFallen ->
  ecosystem. DialogueShown/DialogueClosed deliberately NOT routed — S4
  MusicManager.SubscribeMix owns their presentation audio; an SFX route would
  double-route them. The 6 pre-existing routes (incl. SkillUsed->dna_spoken
  SfxRouter.cs:31) byte-unchanged. EntityId pitch variation via
  SfxRouter.PitchFor = integer-keyed fold (h=17; h=h*31+c; 1.00+((uint)h%5)*0.05,
  five steps) — NO System.Random, NOT string.GetHashCode (per-process seed,
  F4). Fire() print line untouched so existing pairs' SFX_ROUTER lines stay
  byte-identical (warm-baseline diff: only additions). audio_test.sh mapping-
  table leg (pattern copied from S4 mix legs): 7 MAP pair legs + pitch-
  determinism pin + per-stream exact-count guard (double-route trips the count);
  planted-bad (double WagePaid) -> MAP WagePaid->loyalty FAIL (spawns+=2), gate
  exit 1, reverted; audio_test + boot_test solo exit 0 on final tree, two
  independent runs byte-identical across MAP/SFX_ROUTER lines.
  Evidence: .audits/20261005-0445-s5-sfx/EVIDENCE.md (VERIFY_EXIT=0).
- MC 10129 / 10026.15.5 S3 death dissolve (Inc-3 W2, 2026-10-05, code VM350,
  branch vm350/s3-dissolve): KillHide (the ONE death tick, WorldDirector.cs:392)
  now PINS the full suppression — collision mask 0 + physics stop + damage zero —
  and arms the 20-frame VISUAL dissolve instead of the instant hide: alpha +
  scale-down on the composed root ride ONE integer frame counter (F4, the S1
  VisualJuice idiom REUSED; JuiceTuning.DissolveFrames=20), the visual node
  frees ITSELF at f+21 and clears the body's Visual ref (OnVisualDespawned).
  Targeting exclusion REUSES the director's EXISTING predicate (WorldDirector.cs
  :375 TryAttack "if (e.IsDead) continue;", mirrored at :228 damage sum and :416
  TryInteract) — no second targeting list. API FACT pinned by probe: Godot 4.x
  collision layer NUMBERS are 1-BASED — the plan's literal SetCollisionLayerValue
  (0,false) is a SILENT NO-OP (probe: mask stays 1; the first plant of it proved
  a VACUOUS-TRUE leg read); the suppression is SetCollisionLayerValue(1,false),
  mask→0, asserted via GetCollisionLayer(). BossController.cs: comment-only —
  the boss IS an EnemyActor in _enemies, its death rides the same KillHide
  choke; a boss-specific hook would duplicate it and break I3 (the plan's
  "death-site hook" read: named, cited, zero behaviour). Companion-broken beat
  DROPPED per plan. Battery leg (N) DISSOLVE_SUPPRESS (marker DISSOLVE_GONE,
  stage-97 partial RuntimeIntegrationProof.Dissolve.cs + .csproj + .uid same
  commit): real-wire kill; f+1..+20 forced input presses never land (zero
  DamageDealt events name the corpse id) AND a direct forced DealDamage(9999)
  returns false, health pinned 0; DISSOLVE_ABSENT mirror scan prints the corpse
  absent from the live/targeting population; corpse-scoped asserts (other live
  enemies cannot false-redden it). Solo leg green x4 + planted skip-collision-
  off RED (mask read, named) + revert re-green x2, all under the SAVEGATE mutex;
  ecosystem_test + combat_test solo exit 0; ZoneBossProof boss_phase +
  DEATH_SAVE_OWNED + corpse_damage (A2, KillHide neighbour) + JUICE_HITFLASH
  (S1, VisualJuice neighbour) all UNCHANGED green; bash -n on the battery.
  Leg-count/ARCHITECTURE §6 restamp deferred to W2 wave-close per F5. F4-CMP
  declaration: collision-off legitimately changes numeric body-mover hunk(s) of
  any leg that walks through a fresh corpse (mask was ON before this card —
  the ghost body); byte-stable ROSTER-family streams untouched (no persisted
  fields — F2 zero-delta). Evidence: .audits/20261005-0532-s3-dissolve/
  (EVIDENCE.md last line VERIFY_EXIT=0; probe, RED + GREEN logs).

- MC 10132 / 10026.15.8 S10 story act-two (code VM350, 2026-10-05, Inc-3 W2):
  build on SHIPPED seams only — QuestTable.RuinsArc() +4 ruins-deep quests over
  the pure QuestLog machine (shipped objective kinds only, R5; Default() and
  the save schema untouched — act-two rows ride the v3 QuestStates wire, F2
  zero save-file delta proven by git-diff-empty src/save/), 8 authored
  DialogueTable nodes, table-driven act open/close cards through the shipped
  DLQ queue (no cutscene system, F3 — quest_arc DLQ_* legs re-run green),
  act opens off the shipped save->load restore-sync edge, boss-corpse cascade
  deliberately NOT consumed. New mode quest_arc2 (stage 55, partial
  RuntimeIntegrationProof.Story2.cs, battery leg (N) at file END): W6 pin held
  — each of the four completions rides its OWN distinct driven observation
  (zone-enter / fresh speaks / the single wage path / six fresh extractions).
  quest_test 58+ (full act-two state-machine walk incl. illegal-transition
  throws + F-DA2 double-load leg, fix cycle 2), story_test 15+, save/boot/smoke solo green; planted-bad x3 RED then
  revert + re-green (dropped restore row -> ACT2_PERSIST RED; act-open gate
  disabled -> ACT2_OPENED RED; relaxed guard -> named unit tests RED).
  Known limitation flagged: HUD LiveQuestLine reads act-one log ("Quest: none"
  during act two) — Ui.cs is S0-owned, out of this card's file list.
  [CLOSED 2026-10-05 by the MC 10138 rider row below.]
  Evidence: .audits/20261005-0632-s10-story/EVIDENCE.md (VERIFY_EXIT=0).

- MC 10126 / 10026.13.6 rooted-fields-at-Quit latent sweep PART 2 (code VM350, 2026-10-05, branch
  vm350/latent2 off 5e1f3e2): the MC 10117 sweep's recorded out-of-scope observation closed — idiom
  REUSED again (ReleaseHeldRefsBeforeQuit: null live-scene wrappers + GC flush while ObjectDB alive,
  BEFORE Quit; BridgeMvpProof/MC 10112 mechanism, no new mechanism). 6 PASS Quit(0) sites FIXED,
  zero SAFE-BY-CITATION among PASS paths: (1-3) ZoneBossProof zone_travel/boss_phase/death_load
  (Compose() roots _bus/_director/_playerBody, _Process :100-101 guards prove them non-null at all
  three Quits; method +file :324); (4) RosterIntegrationProof.Follow.cs stage-16 roster_follow Quit
  :333 (roots all of Compose()'s bus/director/player/hud/empathy + _main; method lives in the entry
  file per the Bus.cs MC 10117 precedent, fields declared there); (5-6) MainCompositionProof
  headless PASS Quit + GUI-hold-cap Quit (all four wrappers _main/_player/_body/_camera assigned in
  _Initialize on every path reaching _Process; the :124 guard proves body/camera non-null at both
  Quits — the gate's GUI leg is helper-KILLED so it never Quits and cannot leak). Bypass Quit(1)
  sites untouched per 10117 scope (Fail paths + roster_neg detection), negative control re-proved
  red (NEG_ROSTER, exit 1). Detection: build-through CONTROL validated (sed marker -> leg log),
  plant-drop at the zone_travel site measured N=10 exit 0 / leaks 0 — plant RED DID NOT FIRE on
  this HEAD (matches MC 10117's N=15-20 non-detection; below the flush threshold here, INFERRED),
  recorded honestly, no false RED promised. GREEN N=5/site x5 sites = 25/25 exit 0, zero "Leaked
  unsafe reference" over all 45 logs (savegate flock held for save-touching legs, lockfile never
  deleted, no run queued out). Regression solo: ci/bridge_mvp_test.sh EXIT=0, ci/main_composition_test.sh
  EXIT=0 (render bars PASS). Full runtime_integration battery NOT run (orchestrator owns). SIZE
  headers restamped: ZoneBossProof 419->441, RosterIntegrationProof 278->298. Evidence:
  .audits/20261005-1003-latent2/EVIDENCE.md (VERIFY_EXIT=0).
- MC 10138 / 10026.15.9 HUD act-two tracker rider (code VM350, 2026-10-05,
  closes S10's flagged limitation): WorldDirector.Ui.cs LiveQuestLine is now
  a union/priority read across BOTH act logs — the act-two row wins while its
  log has a still-Active one (act-open edge through the arc), else act one's
  first still-Active row; READ-SIDE only, _quests keeps its shipped act-one
  identity (no reference swap — quest_arc case 5 reads QStatus("q_boss")
  through Quests post-open; quest_arc solo green = preservation proof). New
  ui_test run (I) HUD_QUESTLINE_ACT2 — tests/ui/HudQuestAct2CaptureTest.cs
  opens the act through the shipped restore-sync save path (quest_arc2
  FromSaveRows+Save/Load idiom, no proof-side API): baseline run pins the
  act-one title, act run pins "The Way Down" mid-arc + quest-row pixel bars
  (measured: play window maps +64+36 in the helper's 1280x720 root).
  Planted-bad: union reverted -> run I GATE RED ("Quest: none" symptom) then
  revert + re-green; ui_test/boot/main_composition/quest_test/quest_arc solo
  green under SAVEGATE flock. Evidence: .audits/20261005-0954-hudact2/
  EVIDENCE.md (VERIFY_EXIT=0).

- MC 10139 / W2 wave-close restamp (design VM350, 2026-10-05, Inc-3 W2): docs
  restamp at tip 44806a1; ZERO gate/godot runs; only code edits are comment
  lines (Proof.cs SIZE header + battery header). (1) ARCHITECTURE §6 proof-
  partials inventory gains the W2 halves RuntimeIntegrationProof.Dissolve.cs
  (stage 97, mode DISSOLVE_SUPPRESS, leg (N), markers DISSOLVE_GONE/ABSENT) +
  RuntimeIntegrationProof.Story2.cs (stage 55, mode quest_arc2, leg (O),
  markers ACT2_*, stage numbers grepped off the Proof.cs dispatch switch);
  ALL proof wc re-derived at tip: Proof.cs 555 l, Chain.cs 221 l (unchanged),
  Bus.cs 566 l (unchanged; TOOL/NOTE: still no in-file SIZE header though
  >400 — inside the 600 test-class ceiling, flagged not fixed), Juice.cs 126 l
  (unchanged), Shake.cs 121 l (+2 l since W1 at 95f65cd rooted-fields PASS
  exit), Story2.cs 288 l, Dissolve.cs 209 l, P1FixProof.cs 356 l (unchanged) —
  every proof file under 600 (max Bus.cs 566); stale 510/119-era counts gone
  (grep zero). (2) KNOWN DEBT CLOSED: Proof.cs in-file SIZE header restamped
  466 -> 555 l (MC 10119's disclosed drift) with reason clause; ALL other
  in-file SIZE headers checked vs wc — BridgeMvp 507/507, Roster 298/298,
  ZoneBoss 441/441 exact, Quests ~560 vs 559 + Story2 ~300 vs 288 honest
  tildes — none else drifted. (3) Battery header restamped 29 run_mode legs =
  19 positive + 10 negative (was 27=17+10; +DISSOLVE_SUPPRESS +quest_arc2,
  anchored whitespace-tolerant run_mode grep re-derived the split; negative
  list unchanged) + "exceed the 29-leg count"; class parenthetical gains
  story/dissolve. (4) §3 signal census re-derived: grep '[Signal] public
  delegate' skeleton/autoload/EventBus.cs = 15 — UNCHANGED (W2 added no
  signal); doc already read 15, no edit owed. (5) §6 named-legs inventory
  gains battery legs DISSOLVE_SUPPRESS/quest_arc2, ui_test run (I)
  HUD_QUESTLINE_ACT2 (MC 10138) and the audio_test S5 mapping legs
  MAP QuestStarted->loyalty/QuestObjective->loyalty/WagePaid->loyalty/
  QuestCompleted->dna_extract/EmpathyBookOpened->dna_spoken/PlayerHurt->
  betrayal/BossFallen->ecosystem + MAP pitch-determinism (MC 10130) — every
  string existence-grepped in its gate script first; bus_emit STILL not a
  battery leg re-verified (grep -c BUS_ runtime_integration_test.sh = 0).
  (6) §2 src/story listing gains ActTwoSync.cs (pure restore-sync arms,
  MC 10132 F-DA2); the MC 10119 DA P3 dissent clause in the W1 row restamped
  one word ("onto 25" -> "from 25 onto 26", dissent cited in-row).
  Evidence: .audits/20261005-1102-w2-restamp/ (EVIDENCE.md last line
  VERIFY_EXIT=0; comment-only diff proven, RED self-test shown).
- MC 10145 / 10026.16 S9 follower traits + NF harden (code VM350, 2026-10-05,
  Inc-3 W3, branch vm350/s9-traits): TRAIT = CompanionRoster.TraitFor(EntityId)
  — pure-int fold unchecked(17*31+id) %4 over {Steadfast,Forager,Sentinel,
  Bonded}, S5 PitchFor idiom, NO System.Random (F4); latched with the identity
  at construction, re-latched ONLY via Follower.RestoreIdentity (NF5 seam: the
  reuse path's stale latched BusKey is gone, key format unchanged per record).
  RULING-4 held: trait NEVER persists — F2 grep -c Trait src/save/ == 0 AND
  save_test V3HasNoTraitField proof-of-absence on the REAL serialized tree
  (planted-bad persist -> RED, reverted). Presentation + print only: Forgive
  loyalty-delta print carries the tag ([Steadfast] etc., no new signal, bus
  diff zero), CompanionVisual head ACCENT from EXISTING palette constants only
  (UiTheme tokens + gold-as-constant, zero new colours). NF6: restore seam
  BOUNDS VALUES not sizes (RestoreBondId out of the -1 sentinel class,
  RestoreLoyalty into the M03 clamp); F4 cap/trim legs additive-only zero-diff
  (F6, git diff proven). NF7 CITATE-BEFORE-ACT: wage_betrayal leg drives the
  BONDED skip arm (runtime_integration_test.sh:154-156 -> P1FixProof.cs case
  20, quits at first Betrayal) — a DIFFERENT arm from the broken-bond
  continuation (BetrayalSystem.cs:59 keeps manual-break state at Needing, the
  state gate never excluded it, SkipPayment ran into the clamp forever);
  option = guarded DELETE: skip-arm gate now also requires HasCompanion (the
  F1 authority). Plants RED->GREEN all logged (hash-seed, NF5, NF6, trait-into-
  save, NF7 gate-invert -> wage_betrayal RED "loyalty=3, state=Needing").
  Gates solo + savegate: roster_test 23, companion_test 15, save_test 23, legs
  roster_follow/roster_neg/wage_betrayal green at the markers, (i)-streams
  A/A byte-stable. Battery sweep + docs restamp: W3 barrier (not this card).
  Evidence: .audits/20261005-1227-s9-traits/ (EVIDENCE.md last line
  VERIFY_EXIT=0).
- MC 10146 / 10026.17 tune-sweep (Inc-3 post-W3 playtest items, code VM350, 2026-10-06,
  branch vm350/tune-sweep off aee5650): 7 card items closed — (1) S1 DA P4 punch-axis
  NOT-A-BUG math row: Node3D.Position is in the PARENT frame and the body never rotates
  (EnemyActor.cs:82 LookAt rotates the Visual child only; _visuals chain identity,
  main.tscn rotation-free) so world-XZ punch dirs are correct; (2) ActorVisual at-cap
  NOT-A-BUG: file holds zero cap/instancing logic (one spawn-time Build caller,
  EnemyActor.cs:52; the per-enemy material IS the S1 tint surface); (3) S3 F-DA1..4
  four rows: F-DA1 by-design (F4 integer counter, suppression UNTIMED — nothing to
  outlive), F-DA2 dead seam hygiene-DEFER (inert, !IsDead-guarded, no caller), F-DA3
  by-design (pre-existing A2; corpse timer = second despawn mechanism, refused), F-DA4
  NOT-A-BUG (end-of-frame-deferred QueueFree order proven, cosmetic); (4) F-DA5 reopen
  TRIGGER recorded (fires only if a boss-conditional kill ever ships — none exists at
  tip, zero boss branches in the kill choke), no code; (5) vignette FIXED: PaintVignette
  writes the tint layer on CHANGE only (ColorRect.set_color queue_redraws
  unconditionally — the OFF-state constant colour re-queued the full-screen layer every
  TickUi frame), run A gains the VignetteColorWrites pin leg (RED pre-fix 7/12/13/17 ->
  GREEN 0/0/1/5; the check went red on the un-fixed code itself — no plant needed),
  full ui_test.sh A–I solo green under savegate incl. run H byte-side pins; (6) S9 c2
  DEFER-P3 (re-bonding a dead bond needs save surgery — Snapshot writes bonded-only,
  restore drops broken stacks first; effect bounded to ONE skip into the loyalty clamp),
  c4 WONTFIX (grep-verified no-consumer: DueSeconds lives ONLY in CompanionNeeds, its
  reader path sits behind the HasCompanion gate, runtime-only — no save path); (7) R1
  juice defaults + R2 damage numbers = OWNER GATE standing row, zero constants touched.
  Zero new signals, F2 zero save delta, no new mechanism. Gates solo: ui_test exit 0.
  Evidence: .audits/20261005-0643-tune-sweep/ (EVIDENCE.md last line VERIFY_EXIT=0).
- MC 10164 / W3 wave-close restamp + battery barrier (design VM350, 2026-10-06, Inc-3 W3):
  FULL battery swept at tip 93962d1 (branch vm350/w3-close) — ALL exit 0: one-time
  --import + ui/audio/boot/combat/companion/dna_npc/ecosystem/empathy_book/
  main_composition/quest/roster/save/skill/story tests + smoke + bridge_mvp +
  runtime_integration (all 29 legs driven to their markers — 19 positive green,
  10 negative controls red with their named NEG_* markers) + determinism_cmp
  positive A/A (byte-stable IDENTICAL + numeric-mask IDENTICAL) + export_check
  (Windows exe, PE magic OK). savegate held across the whole sweep (stale Oct-5
  regular-file lock archived, re-taken as mkdir per convention). Docs restamp,
  comment/doc-only: (1) ARCHITECTURE §6 named-legs + §2 src inventory gain the
  MC 10146 vignette paint-on-change surface — ui_test run (A) VignetteColorWrites
  pin legs 0/0/1/5 (UiRenderTest.cs, 4 existence-grepped checks) + the Hud.cs
  change-guard and read-only capture-test counter; run (H) HUD_VIGNETTE is
  MC 10123's and was already listed (MC 10146 added no run-H leg — cited only
  what exists); ui_test.sh run (A) header comment gained the same disclosure
  (comment-only). (2) Battery header re-derived by anchored whitespace-tolerant
  run_mode grep: 29 legs = 19 positive + 10 negative, UNCHANGED since the W2
  stamp (MC 10146 added zero battery legs) — header already true, zero edit.
  (3) ALL proof wc re-derived at tip: Proof.cs 555 / Bus.cs 566 / Chain.cs 221 /
  Juice.cs 126 / Shake.cs 121 / Story2.cs 288 / Dissolve.cs 209 / P1FixProof.cs
  356 — all unchanged (MC 10146 touched zero ci_proofs files); Hud.cs 372 < 400
  (no SIZE header required per the MC 10146 verdict), UiRenderTest.cs 166 < 600;
  stale-era count greps zero tree-wide; §6 size parenthetical restamped
  re-derived-at-93962d1. (4) every in-file SIZE header checked vs wc: Proof.cs
  555/555, BridgeMvp 507/507, Roster 298/298, ZoneBoss 441/441 exact; Quests
  ~560 vs 559 + Story2 ~300 vs 288 honest tildes — no drift. (5) §3 signal
  census re-derived: grep '[Signal] public delegate' skeleton/autoload/EventBus.cs
  = 15 — UNCHANGED (MC 10146 added zero signals); doc already read 15, no edit
  owed. (6) RED self-tests of the restamp instrument (verify.sh): a planted
  NON-COMMENT battery line -> RED; a planted false count 28=18+10 -> RED; both
  reverted, re-green.
  Evidence: .audits/20261006-0700-w3-close/ (EVIDENCE.md last line VERIFY_EXIT=0).
- MC 10165 / 10026.20 S11 perf pass (code VM350, 2026-10-06, Inc-3 W4):
  capture under the REAL Xvfb surface reusing the gates' own mechanism —
  capture_wrapper.gd PERF_WINDOW mode (per-frame Performance.get_monitor
  TIME_PROCESS + RENDER_TOTAL_DRAW_CALLS_IN_FRAME + wall interval, flushed
  PERF_LOG; M07 PNG path untouched) + tools/perf_probe.sh (meadow/canyon/
  ruins x 60-frame windows; headless zero-surface rejected by design). A/A
  noise-control row FIRST on the unchanged tip: draw_calls spread 0.0 all
  zones, wall-ms spread <=4.65. Action classes capped per plan, zero src/
  gameplay edits: (1) zone-visibility culling fc115a3 — engine VisibilityRange
  tried first, REJECTED at runtime by GL Compatibility (FINDING F2), shipped a
  zone-root throttled 45m distance hide (spawn-view decor reach <=~36m ->
  playable frame untouched, solo ci/smoke.sh green in-band); per-zone draw
  calls meadow 46->28 canyon 63->8 ruins 38->32: CLAIMED on draw_calls
  alone (constant per frame, spread 0.0, mechanically corroborated).
  b1 (fix-1, DA 202610060741-377fb068): wall-ms -5.7..-6.4 re-ruled
  DIRECTIONAL/no-claim — the render-identical a1 control alone swung
  meadow -4.591 (81% of the claimed delta) and canyon's 0.017 "spread"
  vs its +2.476 control swing; pooled render-identical bound 11.317ms
  contains all three deltas; ships on draw_calls, not wall-ms.
  (2) SfxRouter pool reuse 2e90edf (idle-first, cap 12; MAP legs moved to the
  router routing seam, planted double-route RED routed+=2 then revert green;
  solo ci/audio_test.sh 0) — capture windows contain no router: HONEST-DOWNGRADE,
  table WITHOUT threshold (draw_calls identical +-0.0; wall moves = between-pass
  drift ~10ms on render-identical trees, FINDING F3). (3) import/export flags:
  NOT IMPLEMENTED FINDING F1 — import flags live in gitignored .import files,
  export flags invisible to the dev-scene harness. FINDING F4: headless stdout
  block-buffers; perf sampling must flush to file. Verify instrument re-derives
  the whole claim table from raw samples under the fix-1 rule (wall-ms claims
  must exceed the pooled render-identical control bound), proven red (planted
  missing file; planted wall-ms CLAIMED regression in perf.md).
  OWNER call surfaced 2026-10-06 (DA W3): 45m hard cull pops visibly in
  walk-play (no fog/fade/bounds); spawn view pinned (<=36.0m reach) —
  accept vs fog/fade vs hysteresis.
  Evidence: /home/svarkor/last-animal/.audits/20261006-0840-s11-perf/
  (perf.md + EVIDENCE.md last line VERIFY_EXIT=0). Close battery: wave barrier.
- MC 10167 / W4 wave-close restamp + FINAL Inc-3 battery barrier (design VM350,
  2026-10-06, Inc-3 W4): FULL battery swept at tip da0bdaf (branch vm350/w4-close)
  — ALL 20 rows exit 0: one-time --import (fresh worktree) + ui/audio/boot/combat/
  companion/dna_npc/ecosystem/empathy_book/main_composition/quest/roster/save/
  skill/story tests + smoke + bridge_mvp + runtime_integration (29 distinct mode=
  legs x 2 log lines = 58 lines, all driven to their markers — 19 positive green,
  10 negative controls RED-by-design with their named NEG_* markers; GATE PASS
  banners x3) + determinism_cmp positive A/A ((i) 11 lines byte-stable IDENTICAL,
  (ii) 4 masked-IDENTICAL) + export_check (Windows exe 138,045,192 B, PE magic OK).
  savegate held across the whole sweep as a mkdir DIRECTORY per convention,
  released after. Docs restamp, comment/doc-only: (1) ARCHITECTURE gains the
  S11 surfaces, existence-grepped first — §3 capture row: capture_wrapper.gd
  PERF_WINDOW mode (env-flag-gated per-frame Performance sampling, flushed
  PERF_LOG, MIN_LIFE_MS; M07 PNG path byte-identical unset); §3 zones row:
  zone.gd decor-cull (throttled 0.5 s zone-root distance test,
  DECOR_CULL_DISTANCE_M=45, spawn-view reach <=~36 m, GL Compatibility REJECTS
  VisibilityRange — FINDING F2); §6: tools/perf_probe.sh TOOL row + perf-instrument
  note (draw_calls the stable column, spread 0.0 / wall-ms ~10 ms between-pass
  drift on render-identical trees, no wall-ms claims) citing the S11 LEDGER row;
  §6 audio MAP legs restamped — since S11 counted on the router routing seam
  (SfxRouter.RoutedCount/LastRouted; pool idle-first, PoolCap 12, bounded
  round-robin re-arm; MusicManager.SpawnSfxPlayer QueueFree churn contract
  dropped), strictly stronger than the child-count proxy; stale S5-era
  "per-stream fired-counts" wording removed. (2) Battery header re-derived by
  anchored run_mode grep: 29 = 19 positive + 10 negative UNCHANGED (S11 added
  zero legs) — header already true, zero edit. (3) §3 signal census re-derived:
  grep '[Signal] public delegate' skeleton/autoload/EventBus.cs = 15 — UNCHANGED
  (S11 zero new signals), zero edit. (4) ALL proof/source wc re-derived: every
  ci_proofs count UNCHANGED (S11 touched zero — Proof 555 / Bus 566 / Chain 221 /
  Juice 126 / Shake 121 / Story2 288 / Dissolve 209 / P1Fix 356; §6 parenthetical
  restamped re-derived-at-da0bdaf); S11-touched files zone.gd 88 / SfxRouter 114 /
  capture_wrapper 99 / perf_probe 89 / AudioTest 257 / MusicManager 187 — all
  <400, no SIZE header owed (grepped: none grew one). (5) SIZE-header sweep FOUND
  ONE REAL DRIFT: WorldDirector.cs header read 501 l vs wc 506 at tip (+4 l S1
  trigger hunk a6704d0, +1 l W1's own comment-only F-DA1 note 87ba446; prior
  wave-closes swept ci_proofs SIZE headers only and missed it) — header
  comment-only restamped to a self-consistent 506 l, REMAINING register row
  restamped the same commit. (6) The three MC 10164 DA-noted prose imprecisions
  ("mode= = 29" vs 58 lines / "48 checks" vs 45 / "15 module gates" vs
  14+smoke+bridge): grep of repo docs (LEDGER/ARCHITECTURE/ci headers) = ZERO
  hits AT W4 STAMP — re-grepped at W1 close (MC 10196): the ONLY repo hits for
  the three quoted strings are this row's own self-quotes (grep the strings and
  the hit is this very line — the claims themselves lived only in the W3
  EVIDENCE folder, out of repo scope);
  W4 evidence wording carries the corrected forms up front. (7) RED self-tests
  of the restamp instrument (verify.sh): planted NON-COMMENT battery line ->
  RED; planted false count 28=18+10 -> RED; both reverted, re-green.
  INC-3 ARC CLOSURE: S0-S11 ALL SHIPPED OR GATED — S0 bus-emit seam (3bfcee5,
  11->15 signals), S1 hit-flash, S2 camera shake, S3 death dissolve, S4 music
  duck, S5 SFX remap, S6 HUD tokens+vignette, S7 latent closure + the
  determinism_cmp tool, S9 follower traits (+NF5/6/7 harden), S10 story act-two
  (+10138 HUD tracker rider), S11 perf pass (fix-1 merged at da0bdaf). S8 Command
  Bark NEVER SHIPPED — stays QUEUED, HARD-GATED on the owner RULING-3 rec
  artifact (board MC 10131/10026.15.7 status read 2026-10-06; the LEDGER names
  S8 once, the S1 row's merge-order note — queue status is board-carried).
  Wave-close chain COMPLETE: W0 (10097 restamp + 10112 barrier) -> W1 (10119) ->
  W2 (10139) -> W3 (10164) -> W4 (this row). Items THIS LEDGER carries forward:
  R1 juice defaults + R2 damage numbers — OWNER GATE standing row (MC 10146);
  F-DA5 reopen TRIGGER — fires only if a boss-conditional kill ships (MC 10146);
  S9 c2 re-bond DEFER-P3, save-surgery-bound, ONE-skip effect (MC 10146);
  F-DA2 dead-seam hygiene-DEFER, inert + guard-read (MC 10146); S11 45 m
  walk-play hard-pop OWNER call — accept vs fog/fade vs hysteresis (MC 10165);
  C16 real-Windows launch smoke — owner acceptance step (REMAINING register);
  Bus.cs 566 l no in-file SIZE header — TOOL/NOTE inside the 600 test ceiling,
  flagged not fixed (W2-era). NOT LEDGER-carried: the dispatch-named "gh-token
  release leg" greps ZERO in the repo tree — carried by the Inc-4 plan draft
  (MC 10026.18) and the board only, not cited here per no-invention.
  Evidence: .audits/20261006-1021-w4-close/ (EVIDENCE.md last line VERIFY_EXIT=0).
- MC 10184 / 10026.23 S13 world render pass + decor soft fade (code VM350,
  2026-10-06, Inc-4 W1): two commits, zero C# mechanism, F2 zero-delta. (A)
  Per-zone LOOK as pure data INSIDE the existing Environment sub-resources of
  all three zone tscns — glow + volumetric-exempt env fog + adjustment
  (color-grade), light_energy re-pulsed (meadow 1.4->1.5 warmth/glow; canyon
  1.2->1.35 DEFINED haze: warm fog + contrast/sat 1.15/1.18 + aerial
  perspective 0.4, graded NOT washed out; ruins 1.1->1.15 dark-but-graded:
  cool moonlit fog, black-point 0.02 lift against crush — identity darkness
  kept, NOT brightened away; LEDGER Inc-1 register honored). Gotcha pinned
  for S15/S18: fog_sky_affect defaults 1.0 — env fog WASHED THE SKY on the
  first pass (a1-washout colors 141/85/447, mean 0.25->0.74); shipped fix
  fog_sky_affect=0 + aerial perspective. A/A noise row FIRST (colors
  identical across aa1/aa2, spread 0): meadow 870->1160, canyon
  1814->2656, ruins 1333->1805; smoke spawn-view colors 2977->3435 (historical
  band 2380-3038 restated: the grade pass intentionally adds richness ABOVE
  it; content bar --min-colors 1200 stays green). Grade is a declared
  TUNING-EVIDENCE class per plan S13 (captures + colors table = the proof;
  no fake pixel gate). (B) OWNER-ACCEPTED FADE (ruling 2026-10-06 verbatim
  "Rec on all" accepting "accept 45m cull now + soft distance-fade in S13"):
  zone.gd's single 45 m pop line became a staggered multi-band hide — root i
  hides beyond 45/42/39 m by band (i % 3), re-shows with 2 m hysteresis;
  alpha/material fades REJECTED for GL Compatibility (shared imported
  materials cross-fade every instance; TRANSPARENT_alpha pulls decor into the
  sorted transparent pass — flicker + full-opacity shadows). Spawn view
  UNCHANGED BY CONSTRUCTION: max spawn-camera decor distances derived 33.91/
  36.01/33.58 m vs nearest hide threshold 39 m (margins 5.09/2.99/5.42);
  smoke colors 3435 (A) vs 3481 (A+B) same-frame-noise apart, mean/stddev
  identical to 3 decimals; capture windows show the fade honestly (meadow
  hidden roots 5->7 draw-calls 28->18; canyon byte-identical — all 12 roots
  beyond 45 m under BOTH schemes for that fixed camera; ruins 2->3,
  32->29). S11 marker string kept verbatim + fade suffix on the same line
  ("decor distance cull armed at 45m over 12 roots; soft fade 3 bands
  39..45m, hysteresis 2m" — live in evidence b1 logs). THE LEDGER CARRIED
  ITEM "S11 45 m walk-play hard-pop OWNER call — accept vs fog/fade vs
  hysteresis (MC 10165)" IS NOW RESOLVED: accepted + fade shipped. Gates:
  ci/smoke.sh green x3, ci/export_check.sh green (PE OK 135 MB). Battery +0
  legs (plan §S13). Evidence: .audits/20261006-1820-s13/ (EVIDENCE.md last
  line VERIFY_EXIT=0).
- MC 10183 / 10026.22 S12 feel-debt pass (code VM350, 2026-10-06, Inc-4 W1, branch
  vm350/s12-feeldebt off 1be0b82): playtest-tune rows 1–3, 5–7 closed, row 4 held —
  (1) punch-axis NOT-A-BUG, independent re-derivation this session: PlayHit's world-XZ
  dir lands in the Visual child's local Position whose PARENT (EnemyActor CharacterBody3D)
  frame is rotation-free — the only LookAt in the world/ grep (EnemyActor.cs:82) rotates
  the VISUAL child, and a node's Position is parent-frame regardless of its own rotation;
  world dirs correct by construction, the MC 10146 math row holds, zero code;
  (2) VisualJuice + JuiceTuning SPLIT out of world/ActorVisual.cs into world/VisualJuice.cs
  (at-cap hygiene unblocking S14; code moved as-is, csproj Compile + engine-minted .uid in
  the same commit, static facade unchanged for callers; ActorVisual 357→219 l);
  (3) S3 F-DA rows — F-DA1 NOT-A-BUG (KillHide suppression is UNTIMED: collision-off /
  physics-off / damage-0 pinned at the death tick, EnemyActor.cs:123–125, nothing a
  render-tick-stretched dissolve can outlive; F4 idiom); F-DA2 FIXED — dead
  EnemyActor.Damage(int) DELETED (repo-wide grep zero callers incl. .gd/.tscn/Call; the
  ONLY damage door is the one CombatSystem.DealDamage site); F-DA3 by-design (corpse-until-
  zone-clear is the A2 contract every consumer IsDead-guards; a corpse timer would be a
  SECOND despawn mechanism — refused); F-DA4 NOT-A-BUG (end-of-frame-deferred QueueFree,
  cosmetic-only loss, S3 ordering proof cited); (4) F-DA5 REOPEN-TRIGGER re-checked at tip
  (kill choke still zero boss-conditional branches), untouched per brief; (5) vignette
  re-draw-rate CLOSED on the MC 10146 paint-on-change guard + honest S12 annotation
  (OFF=0 writes pinned 0/0 by run A; ON=every-redraw IS the ratified R6 pulse — further
  smoothing = damping the pulse, refused; ci/ui_test.sh re-verified exit 0 at this HEAD);
  (6) S9 c2 save-surgery check landed at UNIT-gate level in the roster suite (reuse-seam
  rebond of a dead-bond stack: clock BOUNDED, AT MOST ONE deferred SkipPayment asserted —
  red on the un-bound code, 580≠30, before green 24/24) + S9 c4 DUESECONDS BOUND at
  PayIntervalSeconds (Math.Min in CompanionNeeds.TickAccompaniment — invisible on the live
  cadence, closes the dead-bond growth, makes the c2 "ONE skip" enforced not argued;
  runtime-only, F2 zero save delta; brief's "BetrayalSystem.cs" cite grep-corrected:
  DueSeconds lives ONLY in CompanionNeeds.cs); the run_mode leg stays DEFERRED per the
  ratified DA W5 c2 rationale — reproduced verbatim in the evidence dir, declared not
  silent; (7) R1/R2 owner-discretion numbers (verbatim "Rec on all" 2026-10-06) with
  A/A-controlled evidence: PunchDistance 0.35→0.30 (0.35 = 39% of the 0.9 m collision box
  read as teleport-off-footprint at the 6f return; exactly 1/3 at PunchDistance 0.30 /
  0.9 m box — DA restamp MC 10196, W1 close: NOT strictly under a third, the VisualJuice.cs
  comment wording carries to W2; DIRECTIONAL class — smoke color-count A/A
  2977/2974 baseline, 2977 after, inside spread; JUICE_HITFLASH asserts states not
  magnitude) + Skeleton 45hp/10dmg→65/18 (the shipped depth ramp INVERTED at ruins:
  spawned skeleton 45a+25 < orc 60a+20 hp and 10<15 dmg, tier-2 boss 223/40 < tier-1
  264/54; 65/18 restores both ladders — deterministic integer table in evidence; EnemyAI
  ctor + EcosystemSpawner.StatsOf + ZoneBossProof.BaseHealth moved together; 1:1-port note
  annotated as the first deviation). Timing consts (HitFlashFrames/PunchReturnFrames/
  DissolveFrames) declared UNCHANGED — the juice/dissolve legs pin that face in their own
  PASS-marker text. Zero new signals (EventBus [Signal] census 15 re-counted, diff zero),
  F2 zero save delta (save_test green; GameState diff zero), battery +0 legs (existing
  legs re-verified: 15 named runtime legs green BEFORE and AFTER the pass — before/after
  legs-summaries in the evidence dir; gates solo under savegate, all exit 0 at the FINAL
  tree: build + combat/ecosystem/companion/save/ui/boot/smoke + roster re-run 24/24).
  RED proofs: row6-leg red on the un-bound code (pre-fix log) → green after (MC 10146
  idiom — the check goes red on the un-fixed code itself, no plant needed); roster gate
  two-pass calibration re-asserts its own red every run. NOT touched: F-DA5, the
  battery-leg assertions (ZoneBossProof moved ONLY its BaseHealth track-table to follow
  the tuned const), the shared checkout. Evidence: .audits/20261006-1844-s12/
  (EVIDENCE.md last line VERIFY_EXIT=0).
- MC 10196 / 10026.24 Inc-4 W1 wave-close (code VM350, 2026-10-06): barrier
  battery at 60d1e96 (fresh detached worktree, one-time --import first) — 20/20
  lines exit 0: import + 16 ci/*_test.sh + smoke + export_check (Windows exe
  138,047,504 B, PE magic OK) + determinism_cmp positive A/A tool run ((i) 11
  lines byte-stable IDENTICAL, (ii) 4 masked-IDENTICAL) + the runtime battery
  runtime_integration_test.sh = 29/29 legs (19 positive green to their markers,
  GATE PASS banners x3, render bar RESULT=PASS; 10 negative controls RED-by-design
  with their named NEG_* markers — 8 print the literal "break detected",
  NEG_CONTROLLER and NEG_SAVE_VERSION carry their own markers, expected not a
  defect). savegate held across the whole sweep as a mkdir DIRECTORY per
  convention, released after. Zero new mechanism: existing ci/ scripts verbatim.
  S12 (10183, merge 60d1e96) + S13 (10184, merge aaee0c3) DA-gated SHIP.
  Restamp in this row's commit: smoke spawn colors 3401 at this HEAD (the
  historical 2380-3038 band is superseded LOW-END by the S13 grade — intentional
  richness; content bar --min-colors 1200 unaffected); S12 row ruins-boss dmg
  223/41 -> 223/40 (Math.Round ToEven on 40.5) + punch box-fraction restamped to
  "exactly 1/3 at PunchDistance 0.30 / 0.9 m box" (the code-comment "strictly
  under a third" wording carries to W2 with S14); ARCHITECTURE zones row
  restamped from the stale single 45 m hard-hide to the shipped S13 mechanism
  (three staggered bands hide 45/42/39 m by root index %3, 2 m re-show
  hysteresis, GDScript zone.gd; no-pop by construction: spawn-reach
  33.91/36.01/33.58 m vs nearest band 39 m). W4 self-quoted grep-ZERO row
  re-grepped at this close: the ONLY repo hits for the three quoted strings are
  that row's own self-quotes (restated inline there); the gh-token row re-verified
  zero-hit (zero edit); da0bdaf mentions checked — all SHA-pinned historical,
  zero edit. Carries into W2: S14 stale-comment fix (ActorVisual.Build LookAt
  wording + VisualJuice.cs PunchDistance "strictly under a third"), S15/S18
  fog_sky_affect=0 rule, S18 main.tscn-instances-meadow-only fact.
  Evidence: .audits/202610062247-w1-close/ (EVIDENCE.md last line VERIFY_EXIT=0).
- MC 10200 / 10026.27 S16 Resonance passives (code VM350, 2026-10-07, Inc-4 W2, branch
  vm350/s16-passives off 0968010): counter-band passives WITHOUT new bindings. STEP 0 = the
  counter-position census, FILED as artifact of record for BOTH consumers (S16 lands first;
  S8 consumes): the player consensus Counters has exactly ONE live writer — the kill-
  extraction append (WorldDirector.cs:450, the ONLY _spokenDna.Add) whose signatures come
  from the fixed-6 allocation DnaLanguage.SignatureForEntity (src/dna/DnaLanguage.cs:54) —
  so Counters.Length is reachable ONLY at {0, 6}: every position 1..6 exists at extraction #1,
  the state is never 1..5 wide (the first extract steps 0->6 whole), and >=7 is structurally
  capped out (save RestoreDna can only reproduce a writer-1 length; LanguageSignature.Generate
  is test-only; GameState.Representative's width-4 is a DoD fixture). Consequences pinned:
  thresholds 1..6 census-confirmed (all light on extraction #1 — recorded, not hidden), the
  hypothetical >=7 third band CUT per S8 P2-1 (no const, no code, census row is the record);
  S8's rule-4 precondition (FOUR positions reachable) CONFIRMED — reachable, as part of the
  width-6 state. Authority: PlayerMutations.Passives(profile), pure fn of Counters.Length
  ONLY (same §G D2 Option B discipline as Unlocked — never ObservedCount/Coverage; band
  round-trip stability STRUCTURAL, N3 test). Bands: ResonantDraw @ >=1 (+2 Manna/kill),
  DeepMend @ >=4 (+10 Mend heal); the S16 tuning block (4 consts) is the ONE numeric home.
  Effects land ONLY at the two shipped write seams (SkillState.OnKill(int bonusManna=0)
  under the existing OnDnaExtracted rider; SkillState.TryMend(bool, int bonusHeal=0)), caller
  passes the live verdict (plan §G D4 — no cache); optional params keep every existing call
  site source-compatible. NO roster seam touched (S17 owns settle). Zero save delta (S9
  trait-absence idiom REUSED — skill_test run 4 greps src/save/ for "passive" case-
  INSENSITIVE, expects 0, and a new save-named field can't slip the §G D2 claim); zero new
  Bus signals ([Signal] delegate census 15, diff zero — no edit near EventBus.cs). skill_test
  F10..F14+N3 (derivation table pinned row-by-row widths 0..7+null, seam arithmetic exact +
  cap-honest, threshold-boundary exact, live-extraction width 6 re-derived through the REAL
  builder = invented-unreachability trap, load-shape equality). Runtime proof: new battery
  leg passives/stage 65 (RuntimeIntegrationProof.Passives.cs, csproj ItemGroup-END append,
  Shake.cs precedent) — baseline width-0/no-bands/Manna-0, ONE REAL extract -> width 6,
  kill credited 7 != base 5 (observed number change at the shipped seam), SECOND extract
  credits the same 7 (band persists, one payment each), real skill_2 Mend heals 25+10=35;
  planted spend-site mutation (drop the kill bonus at the director wiring) went RED here
  (manna=5 vs 7), threshold drift (DeepMendPositions 4->5) went RED in skill_test F10+F13.
  skill_use leg expectation-swept in the SAME commit (its farm/mend now ride the band: 3
  kills credit 21 = 3x(5+2), Mend heals 35) and re-greened; skill_neg untouched, still red.
  Battery script header count line left for the orchestrator (siblings append too). Gates
  solo green: engine build-solutions, skill_test (2-sided + 16 named F-acts + save-absence
  row), combat_test 21, smoke RESULT=PASS (3405 colors), passives + skill_use + skill_neg
  legs. Census: .audits/202610070005-s16/census.md.
  Evidence: .audits/202610070005-s16/ (EVIDENCE.md last line VERIFY_EXIT=0).

## Inc-4 S17 — trait effects (MC 10201, branch vm350/s17-traits, 2026-10-07)

  SHIPPED inside the engine-free CompanionRoster settle, per-SLOT
  deterministic (RULING-4), zero save delta (F2 greps 0), zero new Bus
  signals (EventBus diff 0 lines): Steadfast wage-miss decay -25%
  (effective max(1,floor(D*75/100)) over the RAW M03 drain — floor keeps
  betrayal reachable: 50->48, edges 4->2, 3->1, 2->1, 1->0); Forager landed
  pay +5->+3 (wage -2, min(3, granted) so a pay can never penalize);
  Bonded +1 Manna on landed pay riding the EXISTING settle return, consumed
  at the shipped SkillState.GainManna add site. Sentinel CUT — kill-assist
  seam absent at pristine 0968010 (grep -i assist = 0 hits; no seam
  invented). Wage-free upkeep band SHIPPED at 4: the SHARED S16/S8 census
  landed at build time (.audits/202610070005-s16/census.md in the shared
  checkout, filed 00:05Z; live Counters.Length {0,6}, positions 1..6
  census-confirmed, k>=7 cut) — upkeep waived while the shipped
  ModelPlayerDna(_spokenDna).Counters.Length >= 4 (no PlayerMutations edit,
  no S16 build dependency; census threshold-collapse — band lights at the
  first extraction — recorded). roster_test 35 rows (11 S17, pinned rule
  table incl. both CUT/SHIP rows) + battery section (P) trait_effects:
  live settles observed expected-vs-base (Bonded Manna 0->1 vs +0, Forager
  50->53 vs 55) + live band A/B (width 0 -2 control vs width 6 waived with
  interval consumed); planted-bad REDs per rule incl. two engine-side
  (Bonded rider muted, band waiver muted -> grep-legs RED). Legacy restamp:
  OnePayPress id 22 55->53 (Forager rule landing, honest restamp).
  Cap/trim/F6 legs UNCHANGED (tail append only). Gates solo all 0
  (build/companion 15/roster 35 two-pass/save 23/runtime 2118-line log).
  Evidence: .audits/20261007-0127-s17/ (EVIDENCE.md last line VERIFY_EXIT=0).
- MC 10199 / 10026.26 S15 day-night driver (code VM350, 2026-10-07, Inc-4 W2, branch
  vm350/s15-daynight off 0968010): runtime-only day-night on the world tick — the clock is
  an INTEGER FRAME COUNTER on the deterministic 60 Hz physics tick (F4 idiom, NEVER
  wall-clock: the planted-bad DateTime feed goes RED at the leg's exact linear-clock assert
  3/3, and the pre-proof caught+fixed a real leg race — the never-ticked budget now rides
  the leg's own arming, not an absolute tick count, because the blocking navmesh bake eats
  a variable number of catch-up physics ticks); NEW world/WorldDirector.DayNight.cs holds
  the ONE tuning block (DayNightTuning.DayLengthFrames=1800=30 s) + the PURE per-zone
  const keyframe tables (meadow/canyon/ruins x dawn/noon/dusk/night; noon rows sit ON the
  shipped S13 values — the grade is modulated, never replaced; fog color/density are NOT
  keyframe channels) + the evaluator the driver WRITES FROM and the leg ASSERTS AGAINST
  (anchor frames t=0 -> bit-exact; rotation within 1e-4 quaternion read-back tol only);
  writes hit ONLY the shipped Environment + DirectionalLight3D nodes (zero .tscn edits),
  fog_sky_affect re-pinned 0.0 EVERY frame, never raised (PINNED S13 carry — leg proves by
  READ at all four anchors); presentation-only: gameplay reads nothing (zero DayNight refs
  src/+autoload), NO bus signal (census 15 holds), ZERO save delta (RULING-7 "Rec on all"
  = persistence NO — the counter never reaches GameState); battery +1 leg = 30 (20 pos +
  10 neg): DAYNIGHT_STATE stage 98 (RuntimeIntegrationProof.DayNight.cs) — driver paused at
  construction, resumed at compose so the DAWN window is unskippable, EXACT node reads at
  the named 0/450/900/1350 windows, three distinct zone noon tables, continuous night->dawn
  wrap; tests/DayNightCaptureTest.cs parks via the presentation seam with IMMEDIATE write
  (headful idle callbacks sample slower than 60 Hz physics — measured, a next-tick-only
  value is skippable) + pauses: dawn/night anchors non-blank AND non-equal (RESULT=PASS
  x2, AE 251 660 px / RMSE 0.278) — evidence rows, not a pixel gate; csproj Compile +
  engine-minted .uid in the same commit (ItemGroup END, Shake.cs precedent). Solo gates
  green at this HEAD: build (engine), boot, smoke (colors 3915), battery 30/30,
  determinism_cmp A/A DAYNIGHT_STATE IDENTICAL (16 byte-stable lines, savegate held),
  planted DateTime RED 3/3. Evidence: .audits/202610070040-s15/
  (EVIDENCE.md last line VERIFY_EXIT=0).
- MC 10198 / 10026.25 S14 character life (code VM350, 2026-10-07, Inc-4 W2, branch vm350/s14-life off 0968010): velocity-fed WALK cycle + idle breath/sway + attack lean on PRESENTATION roots ONLY via the new partial world/WorldDirector.Motion.cs — the base WorldDirector keeps its _Process; the motion tick runs via MotionPhysicsTick, invoked FIRST and unconditionally by the ONE WorldDirector._PhysicsProcess override (kept in the DayNight partial per the c94eb0d merge fixup; W2-close restamp of the pre-fix wording), delta READ-BUT-NEVER-USED (every phase an INTEGER per-physics-tick counter, F4) — reading the new read-only EnemyActor.SimVelocity feed / CharacterBody3D.Velocity and writing ONLY the new Lean.Rotation Node3D + limb part Positions (Lean re-parent added in ActorVisual.Build and main.tscn; per-type gait table Goblin 12f / Skeleton 16f / Demon 18f / Orc 20f cycles, Player 14f, breath 96f — rest lands bit-EQUAL on the captured base by Sin(0)=0 exactness); TWO one-line seams at the ONE shipped hunks: TriggerAttackLean after ActorVisual.PlayHitFx (lean armed at the DealDamage hit frame, 6f integer decay) and PlayerHurtMotion inside the incoming>0 TakeDamage hunk (PlayerHurt's origin) driving the class-swapped player VisualJuice (main.tscn Visual gains script 5_juice + Lean child; flash/punch, zero new save/signal surface). Player-parts tint adoption via FirstComposedTint resolving GetActiveMaterial(0) — covers both MaterialOverride (composed enemies) and surface_material_override/0 (tscn player) wirings; W1 carries closed: ActorVisual "rotates the body" stale wording restamped visual-child-only (RULING-1), PunchDistance "strictly under a third" restamped exactly 1/3. Battery leg (P) stage 99 mode CHAR_MOTION (renumbered at merge off S15's 98) (partial RuntimeIntegrationProof.Motion.cs, csproj + engine-minted .uids same commit): chase-bob off the read-only feed (CHAR_ENEMY_WALK); WASD walk on a physics-probed-clear direction (spawn abuts terrain — the leg settles + probes deterministically) with the phase pinned by a TWO-POINT exact-delta check (CHAR_WALK_ACTIVE — the delta-time plant lands RED, phase=9 over 7 ticks; the pin was hardened after a fast-rate plant aliased the single-sample window through the mod-cycle wrap); exact-base rest return (CHAR_WALK_AT_REST); lean armed/returned at/after the hit hunk; player flash at the REAL damage site + body GlobalPosition bit-unchanged with physics frozen, S1 precedent (CHAR_BODY_STILL — the motion-on-body plant lands RED). A/A byte-IDENTICAL solo and in-battery (position=(-0.83148324, 0.7988844, 0.76390064) both); determinism_cmp IDENTICAL across positive/CHAR_MOTION/JUICE_HITFLASH/DISSOLVE_SUPPRESS, the position= line the whole (ii) numeric stream, masked-IDENTICAL, zero declares. Gates solo: build, full battery (echo restamped S14 green), combat, smoke, boot, main_composition, ui, save EXIT=0. Census 15 re-counted (diff zero new signals), zero save-touching lines in the diff (F2 zero-delta by construction). Evidence: .audits/20261007-0145-s14/ (EVIDENCE.md last line VERIFY_EXIT=0).
- MC 10203 / 10026.29 S16 economy follow-up (W2-close restamp: this row's commit was authored with the pre-filing placeholder id MC 10204; the board card is 10203 — see card append #3; the later MC 10204 row below is the bridge_mvp consumer fix, 10026.30) + dispatch hardening (code VM350, 2026-10-07, Inc-4 W2, branch vm350/s16fix off c94eb0d): (i) EXPECTATION SWEEP — the calm_use farm pin still expected 3 x KillMannaGain after S16 (MC 10200) shipped the Resonant Draw rider, so the merged battery went RED at '3 kills credited 3 x KillMannaGain' manna=21 (/tmp/merged-battery.log:1456, the leg's own FAIL line); the pin now COMPUTES 3 * (SkillState.KillMannaGain + PlayerMutations.ResonantDrawKillManna) from the consts the product pays with (RuntimeIntegrationProof.Skills.cs S16 sweep idiom, skill_test F10..F14 precedent — no magic number), leg SEMANTICS untouched: all eight CALM_* markers still land (cast spend-after-scan, pay-in-window join, two refusals, E-stands, expiry, load-cleared), and the width-6 consensus is created by the FIRST extraction the shipped handler appends (WorldDirector.Skills.cs:70), so all three kills are band-on. (ii) HARDENING from the S17 TEST finding F1 — RuntimeIntegrationProof._Initialize now HALTS an unrecognized LA_GATE_MODE with 'LA_GATE: FAIL: unknown mode <x>' exit 1 over the KnownModes allow-list (24 modes = this dispatch + its 14 partials (restamp: Motion partial came in at b2a2b2d; W2-close); the sibling proofs' class-local modes — roster_*/trait_effects, ZoneBoss, P1Fix — ride their own dispatchers and are unaffected); the guard's OWN red-capability is logged (bogus_mode exit 1, plus a second unknown word and an empty value) and the pre-guard VACUOUS GREEN was VERIFIED on this same tree (bogus_mode exited 0 with the positive-chain PASS marker — the finding was real, not a paperwork claim); the header mode doc gained the `passives` row the S16 keep-both merge dropped, so header list == dispatch arms == KnownModes. (iii) SIZE reason line restamped to the measured 683 l with the full drift split — the 613-l stamp at e69e1bc PREDATED the S14 merge arm (637 l measured at b2a2b2d), so the W1 line is arithmetic again instead of a stale claim. NOTE for the wall (not mine to rewrite): the S14 row's "the partial owns the _PhysicsProcess motion tick" wording is CONTRADICTED by the merge fix at c94eb0d (the DayNight partial keeps the ONE override, MotionPhysicsTick invoked first and unconditionally) — needs an orchestrator/DA restamp of that row. Gates SOLO (the battery stays at the W2 barrier, savegate held for the save-touching legs): dotnet build + engine --build-solutions 0 errors at every step (stale-DLL trap closed), calm_use RED-before/GREEN-after (14 checks ok, exit 0), calm_neg + skill_neg RED with their NEG_* markers, skill_use + passives + positive (LA_GATE_MODE unset → default) GREEN, DAYNIGHT_STATE + CHAR_MOTION re-green AFTER the guard edit (stage 98/99 arms unaffected, full marker sets), bogus_mode RED on the FINAL dll, ci/skill_test.sh GATE PASS (30 passed, 0 failed, named F-acts incl. F10..F14 present), ci/smoke.sh RESULT=PASS (colors 3822, unpinned). Evidence: .audits/20261007-0323-s16fix/ (EVIDENCE.md last line VERIFY_EXIT=0).
- MC 10204 / 10026.30 W2 consumer break: bridge_mvp VISUAL_CONTENT vs the S14 Lean re-parent (code VM350, 2026-10-07, Inc-4 W2, branch vm350/s14fix off 48e75b9): RED at the W2 merge ('VISUAL_CONTENT: enemy Spawned1000 (Goblin) Visual subtree has 0 MeshInstance3D', orchestrator repro /tmp/bridge-merged.log; reproduced on this branch's base), GREEN at base 0968010. ROOT CAUSE = consumer-side false premise, NOT product: the debug tree dump AT the sampling moment (worktree-only, never committed) shows the meshes WHERE S14 put them — Visual(VisualJuice) → Lean(Node3D) → the 8 named MeshInstance3D parts, in-tree — presentation never escaped the Visual root (the BLOCKED-P0 branch does not apply). The proof counted via `e.Visual.GetChildren(true)` BELIEVING the bool recursed; in GodotSharp 4.7.2 that sole bool is include_internal and the binding exposes NO recursion overload (Godot.Node.GetChildren(System.Boolean) is the whole surface — engine-shipped GodotSharp.xml; the 2-arg call is CS1501), so the loop counted DIRECT children only — pre-S14 every part WAS a direct child and the gate passed by luck; the Lean node moved parts one level deeper and the luck ran out (a false RED on a tree verifiably holding 8 meshes). FIX consumer-only, contract kept: a static manual walk CountMeshesInSubtree (the shipped VisualJuice.FirstComposedTint recursion idiom) counts the subtree INCLUDING the root (pre-3895 placeholder shape still counts; null still 0); the Fail text, all six markers and the pre-3895 enemy scan untouched; zero product code, zero relaxation. Gates: bridge RED-before on this branch's base (exit 1), GREEN-after (exit 0 — six markers + RESULT=PASS render bar), PLANTED-RED of the FIXED counter (worktree mutation: Build strips the parts and re-parents nothing → VISUAL_CONTENT FAIL exit 1 — the gate did NOT degenerate to vacuous), ci/smoke.sh RESULT=PASS (colors 3902), ci/boot_test.sh GATE PASS; dotnet build + engine --build-solutions 0 errors at every step (stale-DLL trap closed). SIZE line restamped truthful 527 l (+20-l hunk + 14-l helper). NOTE attribution: this row/commit carries the TRUE board card 10204 (board search VERIFIED: 10026.30 claimed); the S16 row above stamps board card 10203 as MC 10204 per the 48e75b9 merge note, and the s14fix brief file's MC 10205 is a drafting slip — board wins. Evidence: .audits/20261007-0412-s14fix/ (EVIDENCE.md last line VERIFY_EXIT=0).
- MC 10209 / 10026.31 Inc-4 W2 wave-close (code VM350, 2026-10-07): barrier battery at master f84c299 in a FRESH worktree (dotnet build 0 errors; ONE --import — all import stages DONE, 6,578 artifacts in .godot/imported, but the engine SEGFAULTED at quit after the final DONE (exit 139) — recorded honestly, every battery leg ran green against those artifacts) — battery EXIT=0, 33 run_mode legs (METHOD NOTE: count = grep -cE '^run_mode ' at this commit = 23 positive + 10 negative, re-derived by anchored grep not arithmetic — W1 close carried 29, the four W2 cards appended passives/trait_effects/DAYNIGHT_STATE/CHAR_MOTION), ZERO 'GATE FAIL' lines, final GATE PASS banner; determinism_cmp A/A: DAYNIGHT_STATE (i) 16 lines byte-IDENTICAL (ii) 0 numeric lines, CHAR_MOTION (i) 14 lines IDENTICAL (ii) 1 masked-IDENTICAL — both DETERMINISM: IDENTICAL, declares=0 (ZERO --declare); ci/smoke.sh RESULT=PASS colors 3845; ci/boot_test.sh GATE PASS — all exit 0; SAVEGATE (mkdir /tmp/la-savegate.lock + owner file) HELD across battery + determinism + smoke + boot, released at end (own lock + own holder only; foreign holders checked before acquire, never touched). W2 SHIPPED FIVE systems: S16 resonance passives + counter-position census-of-record (MC 10200, merge 7b37da3 — band-gated ResonantDraw@1 / DeepMend@4 at the two shipped write seams; the shared S16/S8 census .audits/202610070005-s16/census.md pins Counters.Length reachable ONLY at {0,6}, >=7 third band CUT); S17 trait effects (MC 10201, merge 5bf18b6 — Steadfast/Forager/Bonded inside the engine-free settle, Sentinel CUT (no kill-assist seam at pristine 0968010), wage-free upkeep band SHIPPED@4 on the same census); S15 day-night (MC 10199, merge 67a138c — INTEGER frame-clock on the 60 Hz world tick never wall-clock (planted DateTime RED 3/3 named assert), per-zone const keyframe tables on the shipped Environment/DirectionalLight3D, fog_sky_affect 0.0 re-pinned EVERY applied frame and READ-proven — the S13 washout carry); S14 character life (MC 10198, merge b2a2b2d — integer-phase walk/idle/lean on PRESENTATION roots only, delta read-never-used, lean + player flash at the ONE shipped damage hunks, CHAR_BODY_STILL pins the body bit-equal across the juice window); S16-economy follow-up (MC 10203, merge 48e75b9 — calm_use expectation swept to the COMPUTED pin 3 x (KillMannaGain + ResonantDrawKillManna), leg semantics unchanged, + unknown-LA_GATE_MODE dispatch guard whose red-capability was proven incl. the pre-guard vacuous-green demonstration). TWO merge-integration root causes, one line each + WHO found them: (1) CS0111 — the keep-both merge duplicated _PhysicsProcess across S14's Motion and S15's DayNight partials and the ENGINE BUILD went RED at b2a2b2d; found by the S17 TEST wall child (TEST-verdict 202610070005-1a0f5bd4, STATE 2 FAIL); fixed at c94eb0d: ONE override kept in the DayNight partial, MotionPhysicsTick invoked FIRST and unconditionally. (2) bridge_mvp GetChildren(bool) is include_internal in GodotSharp 4.7.2, NOT recursion — the VISUAL_CONTENT count had been green-by-luck while parts were direct children and false-redded when S14's Lean wrapper added depth; found by the S14 ARCH wall child (ARCH-verdict 202610062354-b5180840, F1 P1); fixed at 92d5e40/e1138a8 with the consumer-side manual subtree walk (root+subtree, pre-3895 placeholder still counts), non-vacuity kept via the planted no-parts Build RED. f84c299 restamps carried at this close (the mandated list, all comment/doc-only + one tail fix): battery header leg-count 33 by the grep method; S17 section 'BAND CUT (census not filed)' -> SHIPPED@4 naming the census artifact; CHAR_MOTION stage 98 -> 99 (truth: Proof.cs _stage=99); LEDGER S14 row _PhysicsProcess fold wording + s16fix row id mapping MC 10204 -> board card 10203 (+ '13 partials' -> 14); battery tail glued '…)“exit 0' split back into echo + own-line exit 0 (the echo printed a stray 'exit 0' and the exit never ran). Per-stage WALL outcomes read from the verdict files: S16 202610062354-bb3a3992 ARCH PASS + DA SHIP + TEST PASS; S17 202610070005-1a0f5bd4 ARCH PASS + DA SHIP + TEST FAIL at b2a2b2d (the CS0111 discovery above — cleared by the c94eb0d fold and THIS barrier battery); S15 202610062354-eecbf6fe ARCH PASS x2 + DA SHIP x2 + TEST PASS (planted DateTime RED + honest overflow-variant row); S14 202610062354-b5180840 ARCH FAIL x2 (F1 P1 bridge-gate RED = root cause (2) above; F2-F4 doc/hygiene carries all closed by 92d5e40/e1138a8 + f84c299) + DA SHIP x2 + TEST PASS at f84c299 (the TEST verdict LANDED mid-this-close — read, not pending); 10203 202610070307-25024946 ARCH PASS + DA SHIP + TEST PASS (pass-2 re-check at f84c299); 10204 202610070407-70fc3e6a DA SHIP x2 + TEST PASS (R2 at f84c299). NOTE to the wall (comment-only, non-normative, NOT touched by this LEDGER-only close): battery header :27 still reads 'exceed the 29-leg count' vs the header line's 33 — one-line comment carry for the next restamp family. Evidence: .audits/20261007-0535-w2close/ (EVIDENCE.md last line VERIFY_EXIT=0).
  - 2026-10-08 MC 10255 note (APPEND only — the row above untouched): every evidence path cited in this W2 wave-close row (its .audits/<run>/ dirs and the /tmp/la-<card> close dirs) is HOST-LOCAL inside the /tmp/la-* worktrees — .audits/ and .tmp/ are gitignored by doctrine (.gitignore "scratch / staging"), so no evidence file exists in the repo at ANY tip; the durable trail for these closes is the Mission Control card appends plus the verdict files preserved on the build host under the named .audits/<run>/ paths.
- MC 10210 / 10026.32 W2 tail: unknown-mode dispatch guards + tree-relative mode_sets_check (code VM350, 2026-10-07, Inc-4 W2, branch vm350/w2tail off b8e8776): (i) P1FixProof + ZoneBossProof stage-0 dispatchers routed ANY unrecognized LA_GATE_MODE to the DEFAULT leg (switch-expression `_ => 10` / nested ternary) — the VACUOUS GREEN was VERIFIED, not claimed: base files rebuilt, LA_GATE_MODE=bogus exited 0 WITH the corpse_damage / zone_travel PASS markers in both proofs (the MC 10204 S17 F1 class at the sibling dispatchers; ARCH F-C + orchestrator ruling 10203 append #5); fix copies the RosterIntegrationProof fail-safe idiom — one named `case` arm per documented mode, `default: Fail($"unknown mode {_mode}")` halts BY NAME exit 1 at dispatch-top before any stage side-effect (each proof keeps its own `LA_GATE: FAIL — ` marker style), UNSET defaults byte-unchanged (both unset legs GREEN with mode=corpse_damage / mode=zone_travel); RED pair per proof: bogus + empty env-value + (P1) the corpse_neg typo each exit 1 with the named line — empty fails closed because GetEnvironmentVariable returns "" for a set-but-empty var, so the ?? default never fires; every positive mode re-run solo reproducing the battery's exact invocations (all four P1 modes + zone_travel/boss_phase/death_load with full marker sets incl. the MC 3910 DEATH_SAVE_OWNED ownership markers); (ii) NEW tree-relative ci/mode_sets_check.sh + ci/mode_sets_check.py (stdlib-only, no engine; string/comment-safe C# mask so switch-block walks cannot be fooled by interpolated strings): for EACH dispatching proof asserts header-doc modes == allow-list == dispatch arms — RuntimeIntegrationProof doc==KnownModes==arms (24), RosterIntegrationProof doc==case-arms (3), ZoneBossProof (3), P1FixProof (4) — exit 1 with named MODE_SETS_CHECK: VIOLATION lines on drift; RED-capability SHIPS as --selftest: temp copies get one dispatch arm added WITHOUT a doc row (P1 + ZoneBoss) and one KnownModes entry without arms (RIntP), all three plants go RED by name, the tree is never touched; wired into ci/runtime_integration_test.sh as the final leg before the GATE PASS echo (bash -n clean; run_mode count stays 33 — the new leg is a NON-run_mode tree leg, battery header restamped); (iii) gates: dotnet build 0 errors + engine --build-solutions exit 0 with a FRESH dll before every run set (stale-DLL trap closed), ci/skill_test.sh GATE PASS, savegate acquired 0s wait (foreign holders checked: none — a sibling wall child was building in its OWN tree), HELD across death_load + skill_test + the FULL battery in a fresh worktree: 33 run_mode legs EXIT=0, ZERO GATE FAIL lines, MODE_SETS_CHECK: GREEN printed inside the battery right before the final banner, lock released (own lock + own holder; runner detached via setsid nohup per the orphan-lock lesson; engine quit resource-in-use noise recorded honestly, exit 0). SIZE: ZoneBossProof restamped to measured 459 l (441 + 18 net). Evidence: .audits/20261007-0654-w2tail/ (EVIDENCE.md last line VERIFY_EXIT=0; verify.sh re-runnable).
- MC 10217 / 10026.35 Inc-4 S20 camera language (code VM350, 2026-10-07, Inc-4 W3, branch vm350/s20-camera off b8e8776): boss framing + kill pulse in world/FollowCamera.cs ONLY (S20-only file per race ledger; mount Main/Camera + Camera3D child unchanged — main_composition re-green proves the contract holds) — KILL PULSE (plan pin N-3): FollowCamera gains a DnaExtracted bus CONSUMER beside the shipped S2 shake subscriptions (census stays 15 — EventBus untouched, zero new signals; the camera NEVER emits), CameraPulseTuning consts (8f INTEGER window, 0.75 m peak), punch-in kick ALONG the sight-line added on top of _followBase exactly like the shipped shake envelope (extend-the-pattern, one implementation per concern: shake=jitter, pulse=punch-in, framing=preset — three windows, one additive kick sum, zero delta math, F4); BOSS FRAMING: presentation authority — the camera READS WorldDirector.HasLiveBoss (shipped boss-threshold state, resolved via GetNodeOrNull("..") with the bus retry idiom) and an INTEGER progress clock (0..18, +1/-1 per _Process) eases a pull-back/height PRESET (BossFramingTuning consts 3.0 m + 1.5 m) linear in progress; progress 0 => contribution EXACTLY zero => rest is bit-exact on _followBase (mid-window reversal lands exact too); IsAtShakeBase/IsShaking UNCHANGED (JUICE_SHAKE contract), new readers IsPulsing/IsFramingBoss/BossFramingProgress/IsAtRestBase (S2 proof-reader idiom). TWO battery legs, stages next-free 100/101, letters (Q)/(R): CAMERA_KILL_PULSE (partial RuntimeIntegrationProof.KillPulse.cs, quiet boot bus_emit idiom + DIRECT emit = shipped consumer-test idiom, JUICE_SHAKE BossFallen / AudioTest precedent; player physics FROZEN Motion.cs:250 idiom => bit-still base => rest asserted BIT-EQUAL to the pre-trigger transform, not a tolerance — CHAR_BODY_STILL-strength pin; markers CAMERA_PULSE_ACTIVE/HELD/AT_BASE) + BOSS_FRAME (partial RuntimeIntegrationProof.BossFrame.cs, live spawn set, ZoneBossProof KillLoop/PressTravel idioms farm to EcosystemSpawner.BossThreshold then the next entry fields the boss; ENTER/HELD (full progress + camera verifiably pulled back)/KILLED (REAL attack-wire kill — the ONLY kill past the threshold, OnDnaExtracted's dead-boss early-return keeps the SpawnSet steady, no framing flicker)/AT_BASE (IsAtRestBase: framing+shake+ALL windows closed, bit-exact on the event-free base); rides QuestFrameBudget). csproj: two Compile items at the ItemGroup END (S2 Shake.cs precedent), engine-minted .cs.uid same commit; dispatch wiring = header mode-doc rows + KnownModes pair (header list == KnownModes VERIFIED set-equal) + quiet-boot flag + two compose routes + stage cases + budget arm; SIZE restamp 683->736 VERIFIED == wc -l. PLANTED-BAD RED pre-proofs: fold-the-punch-into-the-base => CAMERA_KILL_PULSE exit 1 'rest NOT bit-equal' (exact pin named); framing residual-at-1 => BOSS_FRAME exit 1 'camera never returned to its exact follow base (framing=1)'; both restored, PLANTED grep clean. Gates SOLO: build 0 errors + fresh DLL before every run; new legs x2 GREEN; JUICE_SHAKE + DISSOLVE_SUPPRESS + CHAR_MOTION (7 markers) + positive re-green EXIT=0; smoke PASS (colors per log), boot GATE PASS, main_composition GATE PASS; determinism_cmp A/A under HELD savegate (acquired AFTER foreign holder code-MC10210 released, own holder file, released — foreign lock never touched): CAMERA_KILL_PULSE (i) 8 lines IDENTICAL (ii) 1 masked-IDENTICAL, BOSS_FRAME (i) 11 (ii) 1 masked (drift-in-mask noise class), JUICE_SHAKE (i) 11 (ii) 1 masked — ALL 'DETERMINISM: IDENTICAL', declares=0; census-15 + zero-save + zero-new-signal greps in EVIDENCE; battery script bash -n OK, run_mode count 35 (banner restamp = wave-close duty). Full battery (save legs) belongs to the W3 barrier wall. NOTE for the wall (pre-existing, NOT touched): docs/ARCHITECTURE.md never got rows for the S15/S14 proof files; S20 added its two. Evidence: .audits/20261007-0642-s20/ (verify.sh re-checks every claim from the logged runs; EVIDENCE.md last line VERIFY_EXIT=0).
- MC 10216 / 10026.34 S18 zone four + enemy four (code VM350, 2026-10-07, Inc-4 W3, branch vm350/s18-zone4 off b8e8776; RULING-8 owner-ratified YES — card 10026 append #9 "R8 zone four + act three = YES -> S18 + S19 proceed" (wording corrected MC 10273); the brief file's MC 10213 is a drafting slip — board card 10216, its append #1): ZONE FOUR "hollow" through the EXISTING travel seam ONLY — EcosystemSpawner Tables + ZoneIds gain the 4th row, TravelToNextZone (modulo wrap) UNTOUCHED (table-only zone: no new scene, no new signal, no new save field — S15 values-only precedent); ["hollow"]=ZoneTable(Wraith, 6, 3) keeps Depth=denizens monotone (3,4,5,6 — MC 10183 R2 ramp) and hollow's tier-3 boss rides the EXISTING MakeBoss path (Skeleton base, Demon apex, deepBonus 2.5 — CHOICE stated: BossThreshold 4 untouched, no new boss branch); DAY-NIGHT 4th const keyframe table Hollow (dawn/noon/dusk/night; noon = hollow's base identity — no tscn exists to anchor), the fog_sky_affect=0.0 S13 pin carried for FREE by the one shipped writer (ApplyTo re-pins every frame — reused, not duplicated; READ-proven at all four anchors); ENEMY FOUR Type.Wraith appended after EnemyAI.Type.Demon (80/20/2.4 — ladder 65<80<100 monotone; Demon STAYS the apex boss type; the adaptation ladder deliberately keeps Wraith OFF it — deep hollow slots stay wraiths) with StatsOf mirror + ActorVisual spectral-teal BuildWraith (9 parts — unique part-count fingerprint 9 vs 8/10/10/11; the family's only legless silhouette, the rig no-ops absent limbs) + MotionTuning gait row (cycle 14f, integer frames, F4) + ZoneBossProof BaseHealth row tracking the EnemyAI table (AnyScaledEnemy premise kept TRUE, not relaxed). Battery leg ZONE4 stage 100 (next free; stage 99 = CHAR_MOTION was the max), partial RuntimeIntegrationProof.Zone4.cs (253 l; csproj ItemGroup END + engine-minted .uid same commit; header mode row + KnownModes + dispatch 3-way EQUAL — S16 lesson; SIZE line arithmetic restamped 572+24+17+24+46+36=719=wc -l): boots the LIVE meadow set, 3 real travel presses (ZoneBossProof PressTravel idiom) reach hollow (ZONE4_TRAVEL: zone + entry-reposition + exactly 6 Kind==Wraith denizens ZONE4_TABLE; ZONE4_ENEMY4: 9-part Lean-subtree mesh count via the BridgeMvpProof MANUAL WALK — never GetChildren(bool)), the wraith gait advances EXACTLY +1 mod 14 per physics tick incl. one wrap over >=20 moving samples (ZONE4_GAIT_INT — rest ticks RESYNC, never false-RED), then pause+park reads the Hollow row BIT-EXACT at all four anchors on the shipped Environment + DirectionalLight3D with fog_sky_affect READ 0.0 at each (ZONE4_ANCHOR_* / ZONE4_FOGPIN_0), noon rows pairwise distinct across the FOUR tables (ZONE4_KEY4_DISTINCT), and the 4th travel WRAPS hollow->meadow on the shipped modulo (ZONE4_WRAP). RED-CAPABILITY (planted-bad in PRODUCT code, reverted after, logs kept): delta-time DateTime-scaled gait increment -> RED named (prev=8 now=6 cycle=14); driver 1%-off-row ApplyTo write -> RED named (energy=0.909 want=0.9); fog pin raised -> RED named; HONEST FINDING: a TABLE-VALUE tamper stayed GREEN — both sides of an exact-anchor leg read the same const table, so this leg class pins the WRITE path (driver->node), not table content; table content is guarded by the pairwise-distinct assert + it is a data change, not a driver fault (same property holds for S15's leg); bogus_mode halt intact on the final dll. Gates solo fresh-DLL: dotnet build + engine --build-solutions 0 errors at every step (stale-DLL trap closed; first-ever tree import EXIT=0 after the no---quit-after shutdown-quirk saga, worklog .audits/20261007-s18-worklog.md), ecosystem_test + story_test (ZoneIds cross-check) EXIT=0, ZONE4 EXIT=0 full 12-marker set, DAYNIGHT_STATE + CHAR_MOTION RE-GREEN full marker sets, positive, ZoneBossProof zone_travel + boss_phase (the 4-zone cycle keeps the shipped travel legs green — first two travels unchanged), smoke RESULT=PASS colors 3858 unpinned, boot GATE PASS, main_composition GATE PASS. SAVEGATE: foreign holder 'test-engineer s20wall PID 290682' found at acquire time — NOT touched, save legs QUEUED behind it; released naturally, own lock acquired+released; battery save EXIT=0 (5 markers), save_bad_version EXIT=1 NEG_SAVE_VERSION, save_test 23 passed. DETERMINISM A/A under savegate declares=0: ZONE4 (i) 25 lines byte-IDENTICAL (ii) 0 lines; DAYNIGHT_STATE 16/0 IDENTICAL; CHAR_MOTION 14/(1 masked) IDENTICAL — matches the W2-pinned baselines exactly. Census grep '[Signal] public delegate' = 15 UNCHANGED; git diff b8e8776..HEAD greps SaveGame|user://|JsonSerializer|GameState|SaveSystem|SaveLoad = 0 lines (F2 zero save delta by construction; RULING-7). 11 files, +413/-16. Evidence: .audits/20261007-0757-s18/ (EVIDENCE.md last line VERIFY_EXIT=0).
- MC 10131 / 10026.x S8 command bark (code VM350, 2026-10-07, Inc-4 W3, branch vm350/s8-bark off 0dd5f65 — base per orchestrator note, supersedes the brief's 781ea66 mention; owner RULING-3 APPROVED verbatim card 10026 append #9, scope = MINIMAL): the bark command SHIPPED PURELY as presentation — one input action "bark" (key G, the ONLY input-map change) opens a 16f INTEGER bubble window on every ACTIVE roster follower body; CompanionStateMachine carries NO attention/hold-ish state (Following/Needing/Betrayed only) so the purely-presentation branch applies — zero command semantics invented; the bubble rides the shipped CalmedWindowFrames idiom (MC 10031): BarkWindowFrames int on CompanionFollowBody (runtime-only, R7 zero save fields), swept down EXACTLY -1 per roster tick (sweep-BEFORE-press, the calm §2 frame contract; zero delta timing, F4), one small "BarkBubble" primitive child on the EXISTING body visual (UiTheme.GaugeColor — existing token, no new palette, no new widget system; the Hud is the four-readout View and is the wrong seam for per-companion bubbles), visibility follows the counter and NOTHING else is written (presentation authority: bark READS the roster, zero gameplay writes, F4 doctrine); the poll is one IsActionJustPressed read beside the shipped cycle_follower/break_bond polls — zero new Bus signals (census re-counted 15, EventBus.cs diff ZERO lines), new partial world/WorldDirector.Roster.Bark.cs (47 l) + one TickBark call in TickRoster (Roster.cs 386 l < 400). Battery leg BARK stage 102 (partial RuntimeIntegrationProof.Bark.cs 278 l, csproj + engine-minted .uid same commits; entry trio header-doc == KnownModes == dispatch arms MODE_SETS_CHECK GREEN RuntimeIntegrationProof=28 — dispatch call-site grep VERIFIED after the entry edit, cycle-1 P0 lesson): ONE real ActionPress("bark") per roster shape on the live scene, quiet boot (bus_emit idiom); bubble authority = MANUAL scene-tree walk of VISIBLE "BarkBubble" nodes (BridgeMvpProof idiom — non-vacuous on the EMPTY roster: ghosts from a stale list or un-freed body land RED) cross-checked against the per-body counter EVERY frame; roster 1 (boot seed) + 3 (OWNED planted Followers [7,21,22] through the REAL LoadGame rebuild, trait_effects precedent, MC 3910 delete-then-write + deleted after use, BARK_SAVE_OWNED): walked count EXACTLY == roster size at open and every held frame, counter EXACTLY -1/frame (BARK_STEP), gone on the EXACT end frame open+16 last-visible open+15 (BARK_GONE); EMPTY roster (EMPTY planted list — the product rebuild FREES bodies with the roster, DA W5 F4) presses to ZERO bubbles across window+4 (BARK_EMPTY_ZERO) + exit 0; every press BARK_NO_GAMEPLAY_DELTA (hp/Manna/DNA/position/roster/loyalty bit-unchanged, player physics frozen over the window Motion/KillPulse idiom); census-15 re-grep IN-LEG (System.IO read of the bus source, BARK_CENSUS_15). RED-CAPABILITY planted pair (logs kept, reverted, BARK re-greened after): delta-time sweep feed => exit 1 "window step NOT exactly -1 (frame +13 reads 4, expected 3)"; press-opens-bodies[0]-only => exit 1 "BARK_OPEN_3: walked bubble count 1 != roster size 3". Gates (SAVEGATE acquired and HELD all session, foreign-holder check clean): dotnet build 0 at every step; BARK x2 GREEN, A/A logs BYTE-IDENTICAL under savegate; named re-greens DAYNIGHT_STATE/ZONE4/CAMERA_KILL_PULSE/BOSS_FRAME green x1 + CHAR_MOTION green after the worktree's one-time clean-clone --import (first red was the missing import, navmesh 0-poly — environmental, not the diff); FULL battery at final head EXIT=0 (run_mode count 37 = 27 pos + 10 neg, single final GATE PASS echo carries the S8 clause); smoke EXIT=0 RESULT=PASS colors 3915; diff file-list touches NO camera/save/Bus file (input map +1 action block exactly). Entry SIZE restamped honestly 775->814 (known >600 carry, split card MC 10218, reason line grew the +39 l S8 clause). Evidence: .audits/20261007-1020-s8/ (EVIDENCE.md last line VERIFY_EXIT=0).
- MC 10239 / 10026.39 Inc-4 W3 wave-close (code VM350, 2026-10-07): barrier battery on the FINAL merged W3 tip 6ad4007 in a FRESH worktree off the pinned tree (ONE --import EXIT=0 — the CHAR_MOTION-class navmesh env condition, documented; dotnet build 0 errors after rm -rf .godot/mono/temp/bin — stale-DLL trap closed) at the comment-only restamp head 27137f9 (zero behavior hunks over 6ad4007; the LEDGER row below is the only file the battery does not read, same shape as the W2 close at f84c299+8ad328b) — battery EXIT=0, 37 run_mode legs (log 2567 lines, log tail single final GATE PASS echo) (METHOD NOTE: count = grep -cE '^run_mode ' at this commit = 27 positive + 10 negative, re-derived by anchored grep not arithmetic — W2 close carried 33, the W3 cards appended CAMERA_KILL_PULSE + BOSS_FRAME (S20), ZONE4 (S18), BARK (S8)), ZERO 'GATE FAIL' lines, MODE_SETS_CHECK: GREEN (RuntimeIntegrationProof=28) printed ONCE mid-battery (script :408; log :2314) BEFORE the last four legs CAMERA_KILL_PULSE/BOSS_FRAME/ZONE4/BARK and the final echo :486; shipped order is the tested order — DA-c1 P1 wording fix, value re-proven by DA own-run and this close; SAVEGATE (mkdir /tmp/la-savegate.lock + holder file) acquired with a clean foreign-holder check and HELD across the barrier, released at end (own lock + own holder only); engine quit ran CLEAN this run — no segfault line and clean post-echo quit tail (BATTERY_EXIT=0); 10x resources-still-in-use ERRORs + 10x ObjectDB-leak WARNINGs mid-log DISCLOSED as pre-existing quit-time noise class (DA-c1 P2, not a leg failure) (W2-close KNOWN carry NOT observed at this head, recorded honestly either way). W3 SHIPPED FIVE stages, merge SHAs + wall outcomes READ from the verdict dirs: (1) guards/sets-gate (MC 10210, merge e20b691) — narrow wall 20261007-0725-10210wall: TEST PASS @ e20b691 (JUDGED 1b743282; mode_sets_check --selftest RED-capable proven, planted drifts x3 RED), ARCH/DA N/A BY WALL DESIGN — guards + pure-tree gate, the drift gate's own red-capability IS the adversarial DoD; (2) S20 camera language (MC 10217, merge 5d8166f) — narrow wall 20261007-0749-s20wall W1 TEST PASS + W2 DA SHIP (cycle 20261007-0742-s20da), full loop 202610070736-1616790b: TEST PASS + DA SHIP + ARCH PASS (Q1-Q5 CLEAN; P2-1 size row closed comment-only at 618d843; DA mutation re-proof: sets-check stays GREEN on an arm-DROP — battery leg goes RED, pairing gate owed card 10218); DA P2 (camera-leg strength) filed for W4 as card 10229 — NOT a W3 blocker; (3) ARCHITECTURE re-derivation (MC 10212, merge 371d4cf, card CLOSED) — docs-only stage, the re-derived doc IS the deliverable; consumed by the S18 ARCH wall at 0dd5f65 (its Q6 doc-lag closed at 570996e); (4) S18 zone four + enemy four (MC 10216, merge 781ea66) — wall 202610070623-5d3729b5: TEST c1 FAIL = P0 MERGE-INTEGRATION DEFECT (merge 781ea66 dropped the S18 stage dispatch — orchestrator merge defect, not producer code), fixed 4677b7c + 0dd5f65 (mode-gated stage-100 dispatch restored), TEST c2 PASS @ 0dd5f65 (26/26 pairing, A/A IDENTICAL), DA c1 FIX (6 claims HELD; P1 = the merge defect; P2 pairing-check -> card 10218) then adjudicated SHIP @ 618d843 by the S20 DA c2 pass (identical dispatch block + identical attack class — dedicated re-fan retired as redundant), ARCH FIX (Q1-Q4+Q5 PASS, Q6 doc-lag -> 570996e, comment-only); (5) S8 command bark (MC 10131, merge 6ad4007 — merged LAST per ratified plan, owner RULING-3) — wall 202610070903-1afbf15b: TEST PASS (pairing 24/24, BARK x2, A/A byte-IDENTICAL under savegate, 5 named re-greens, sets 28 + selftest RED-capable, 37 legs, 0 conflict markers, census 15, zero src/save hunks) + DA SHIP (6/6 claims HELD, the stale-index attack FAILED to break, own plant went named-RED; P3s F1/F2 filed as card 10238) + ARCH PASS (zero P0/P1; F1 P2 = the 6-item ARCHITECTURE restamp — CARRIED BY THIS CLOSE at 27137f9, so the F1 contradiction never became real); producer evidence 20261007-1020-s8 VERIFY_EXIT=0; card 10131 closes on the ARCH verdict, now in. WAVE ROOT CAUSE (one defect, two downstream REDs — one line each + WHO found it): the 3-way conflict integration at 781ea66 dropped BOTH the S18 stage dispatch AND the S20 BOSS_FRAME return/close cases — found by the S18 TEST wall c1 (P0), fixed at 4677b7c; lesson encoded in ARCHITECTURE §6 (stage numbers are MODE-SCOPED; mode_sets_check cannot see the stage switch — pairing gate owed card 10218) and in the dispatch-call-site grep-after-entry-edit practice (S8 cycle-1 P0 lesson held: S8's merge 6ad4007 shipped the defect-free path). RESTAMPS carried at this close (all comment/doc-only, zero behavior): battery header :27 'exceed the 36-leg count' -> 37 (the W2-close NOTE-to-wall carry); ARCHITECTURE restamped to merged W3 truth per ARCH S8 F1/Q6 six spots (battery 37=27+10, Proof.cs 814 l split owed card 10218, NEW Bark.cs mode/marker/stage-102 row + presentation note, input-map bark (G), roster TickBark partial naming, W3-close tip stamps); P1FixProof.cs size 356 -> 368 = wc at this tree (the same restamped size sentence claims currency). W2-close carries truth-check: BridgeMvpProof 527-l SIZE line PAID at 92d5e40; s16fix NOTE restamped at the W2 close row — both PAID, no action. W4 follow-ups filed, NOT W3 blockers: 10218 (proof-entry split + pairing gate), 10229 (camera-leg strength), 10238 (S8 DA P3s). Evidence: .audits/20261007-1148-w3close/ (EVIDENCE.md last line VERIFY_EXIT=0; battery-27137f9.log + chain.txt).
  - 2026-10-08 MC 10255 note (APPEND only — the row above untouched): same disambiguation as the W2 row — the evidence paths cited in this W3 wave-close row (.audits/ dirs + /tmp/la-* close dirs) are HOST-LOCAL and gitignored by doctrine (.gitignore "scratch / staging"), never in the repo at any tip; the durable trail is the Mission Control card appends plus the verdict files preserved on the build host under the named .audits/<run>/ paths.
- MC 10218 / 10026.x Inc-4 W4 proof-entry SPLIT + stage pairing gate (code VM350, 2026-10-07, branch vm350/s22-split off master tip 71d001b W3-closed; code head a3a3c2f): SPLIT the 814-l RuntimeIntegrationProof.cs (the MC 10212 finding) VERBATIM by concern — _ComposeDeferred -> the 270-l RuntimeIntegrationProof.Compose.cs, Fail/Check/FirstLiveEnemy/TeleportIntoRange/ReleaseHeldRefsBeforeQuit -> the 70-l RuntimeIntegrationProof.Harness.cs (one missing using LastAnimal.Companion added in Compose; csproj Compile items appended at ItemGroup END, Shake.cs precedent; uids engine-minted) — entry back to 510 l keeping SIZE stamp (restamped 510 measured, split PAID here) + mode doc + KnownModes + shared fields + dispatch; EVERY RuntimeIntegrationProof*.cs < 600 (max 566 Bus.cs untouched); ONE _PhysicsProcess override total (CS0111 clear). ZERO behavior proof: LA_GATE marker-log-diff pre/post split on 8 modes incl BARK+ZONE4+CAMERA_KILL_PULSE+positive chain+no_bus+save+dna_speak+passives — exit codes IDENTICAL (7x0 + no_bus NEG_BUS red), 4 legs byte-IDENTICAL, 4 IDENTICAL float-masked (the two follow-dist lines proven SAME-TREE A/A wall-jitter). SHIPPED the ADDED-SCOPE stage pairing gate INSIDE mode_sets_check (same family, no new mechanism): every void/bool Run*Stage def in ci_proofs/RuntimeIntegrationProof*.cs needs a call-site arm in the ENTRY switch (_stage), unpaired body + dead arm both named VIOLATION + exit 1, wired into check_project so battery leg (Q) gates the stage switch (the 781ea66 class is now RED-detectable in CI); --selftest gained BOTH plants (unpaired body in Chain.cs, dead arm in the entry) — RED-before-trusted logged: 5/5 plants named-RED on temp copies, tree untouched, --selftest EXIT=0. Gates at the final head under the SAVEGATE (acquired clean, released own-only): dotnet build 0 errors (16 pre-existing warnings, rm -rf temp/bin first — stale-DLL trap); mode_sets_check GREEN (RuntimeIntegrationProof=28, header kept in entry) + PAIRING OK 24 stage bodies == 24 entry arms; FULL battery EXIT=0 — 37 legs by the anchored grep method (grep -cE '^run_mode ' = 37; battery script untouched), ZERO GATE FAIL lines, final GATE PASS echo (NOTE: 4 GATE PASS echoes exist in the log — pre-existing mid-file banner seams at script lines 274/283/382, the tail is the single final one; battery unchanged by this card, comment carry NOT mine); ci/smoke.sh RESULT=PASS colors 3912; ci/boot_test.sh GATE PASS. Evidence: .audits/20261007-1320-proofsplit/EVIDENCE.md (ends VERIFY_EXIT=0; re-executed verify block GREEN at the close head) + copy .tmp/close-10218/EVIDENCE.md. Carries PAID: MC 10212 size finding, S20/S18 DA P2 pairing-gate owes.
- MC 10258 / (W4 container) S26 pairing-gate STAGE_DEF_RE widen, branch vm350/s26-gate-regex bcc2f8d+0df813b merged FF at 0df813b (code child 8f371efe, 2026-10-07): closes F2 (DA 9c2368df wall) + P3-B false-GREEN twin (ARCH 44136fa3 wall) — off-type stage body + arm was FALSE-RED "DEAD dispatch arm", off-type WITHOUT arm was SILENT-GREEN vs §6 promise. Option (a): regex widened to any type-token Run*Stage( def shape + signature suffix so calls cannot mask arms; BOTH plant directions in --selftest (paired-int GREEN-as-expected + unpaired-int RED UNPAIRED-by-name); BEFORE log shows both old behaviors, AFTER 7/7 named EXIT=0. Orchestrator solo @ merge: bare GREEN 28 + PAIRING OK 24==24, selftest EXIT=0, tree untouched, battery coupling leg Q IS this script (run directly), 37 legs intact. No .cs, no battery script, no ARCHITECTURE (naming sentence home = 10255). Wall: producer RED/GREEN proof + orchestrator re-run; full TEST/DA/ARCH deferred to W4 wave-close wall per checker-only scope.
- MC 10229 / (W4 container) S23 camera-leg strength, branch vm350/s23-camera-legs rebased onto b3a85d5 (clean, zero conflicts — S26 gate regex untouched by camera hunks), FF-merged a062aa0 (code child 178974b3, 2026-10-07): CAMERA_KILL_PULSE leg now pins punch sight-line (CAMERA_PULSE_DIR projection assert), PulseFrames=8 (CAMERA_PULSE_WINDOW_8 kick-envelope measure — wording nit MC 10255: the pin measures the FULL 8f envelope, the +4f HELD sample is the separate CAMERA_PULSE_HELD marker) + PunchMetres=0.75 (peak-displacement pin); BOSS_FRAME_CONSTANTS MEASURES PullBack=3.0/Height=1.5 at progress==18 — kills the closer/farther-only blindness (wrong-direction punch / gutted preset now caught, plants proven by the child; B3 plant widened in-run so only the new pin can catch gutting, orchestrator-endorsed). 227 insertions, ZERO new run_mode legs (grep 37 at tip), zero signals/save fields. Child full battery EXIT=0 (log /tmp/la-10229-logs/s23-battery.log, 19:29:28) + A/A IDENTICAL both modes (s23-determinism logs); orchestrator solo: mode_sets GREEN 28 + PAIRING 24==24 + selftest EXIT=0 on branch AND rebased tip AND master a062aa0; savegate incident window (18:41-19:12, this card + MC 10238) fully arbitrated, all holders byte-preserved in close-10238 dir.
- MC 10238 / 10026.23.3 S24 bark load-seam follow-up, branch vm350/s24-bark-seam 617e109+20941b1+2f677fa rebased onto master a062aa0, LEDGER-tail keep-both conflict resolved by orchestrator, FF-merged 85b1b13 (orchestrator merge-commit 2026-10-07; battery2 at code-identical tree audited by orchestrator: 37/37, 0 gate-fails, BARK_LOAD_CLEAR echoed, leg-set identical to held battery1; tip gates GREEN 28/24==24/selftest-0) (code child, 2026-10-07): closes the S8 DA wall 202610070903-1afbf15b P3 pair F1+F2. F2 (product, presentation-only): the bark window survived the load seam — the calm idiom ships TWO halves (integer window + its ClearCalmWindows load-seam clear, R9 mirror) and bark copied only the window half, so press + LoadGame within <=16f re-identified the REUSED body from the saved entry while a stale bubble rode it (DA probe: window 7 carried, id 7->21). Fix mirrors the idiom exactly: ClearBarkWindows() called at the END of RestoreFollowers beside the calm clear (the ONE load seam — both the player load_game key AND proof-only LoadGame route through it), the method beside TickBark in WorldDirector.Roster.Bark.cs zeroing the per-body runtime counters (R7: zero save fields, zero signals, census stays 15, one _Process unchanged, zero gameplay writes). F1 (leg coverage): the EMPTY-roster proof leg waited window+margin (20) < BarkSettleFrames (30) — measured exactly ONE walk (DA probe frame 31) while the marker claimed every frame; the wait is floored ABOVE the settle and the walk count ASSERTED (20/20 post-press walks), the marker sentence is the truth. NEW regression phase (BARK_LOAD_CLEAR): fresh press then LoadGame INSIDE the open window -> zero walked bubbles AND zero open counters on every post-load frame across window+margin (reused bodies come back window-free). Plant-first shape (S26 precedent): the leg commit 617e109 lands BEFORE the fix commit 20941b1; the plant (drop the one-line call in a /tmp copy) went named-RED on the FIRST post-load walk ("stale bubble rode the REUSED body ... walk=3, open-windows=3, V-at-load=16", exit 1), revert -> re-green exit 0 PASS (bark-red.log + bark-regreen.log in EVIDENCE). Gates (one real savegate hold 19:33:21-19:48:53Z after first-come waits + the orchestrator go-signal; savegate.txt in EVIDENCE discloses ALL THREE self-caught incidents — the 18:46 stale-orphan acquire and 19:05 phantom-monitor acquire (both reversed byte-exact, zero save runs taken under them) and the post-rebase re-gate window 20:03-20:18 running UNHELD because my second script omitted the mandated mkdir -p (holder write failed silently; DIR verified absent across the window = zero collision; late orphan holder deleted own-only) — battery2 numbers labeled accordingly): BARK x3 exit 0, A/A (r1,r2) + (r1,r3) BYTE-IDENTICAL; FULL battery EXIT=0, ZERO 'GATE FAIL' lines, 2572 log lines, final GATE PASS echo, run_mode count re-derived by anchored grep = 37 BOTH pre- and post-rebase (the leg commit adds NO new leg, only a marker grep); REBASE onto a062aa0 (S23 merged) applied CLEAN zero conflicts (S23 battery hunks in the camera sections, mine in the BARK block + tail echo — keep-both automatic), POST-REBASE re-gate (engine build 0 + BARK exit 0 + FULL battery EXIT=0, battery-gated-postrebase.log) + MODE_SETS_CHECK GREEN (RuntimeIntegrationProof=28, PAIRING 24==24); EventBus census grep = 15; ONE-TIME --import EXIT=0 + dotnet build 0 errors (16 pre-existing warnings) after rm -rf .godot/mono/temp/bin. Restamps: Proof.cs SIZE 510->516->529 (mode doc 161-l: +6 S24 BARK + 13 S23 camera-strength doc lines, restamp commit 2f677fa), Roster.cs +1 truthfully counted, ARCHITECTURE Bark partial 47->65 + BARK leg row (BARK_LOAD_CLEAR, F1 floor, seam-clear naming) + Proof.cs 529 stamp; battery (S) block + BARK marker loop + tail echo updated (S23 phrases coexist, grepped). Evidence: /tmp/la-10238/.tmp/close-10238/ (EVIDENCE.md last line VERIFY_EXIT=0). Wall: producer RED/GREEN proof; TEST/DA walls are W4 wave-close scope.
- MC 10280 / display 10026.24 S23c camera-leg per-frame kick-SIGN pin, branch vm350/s23c-signpin off 5ecaba7, FF-merged ef425b7 (orchestrator merge 03:1xZ, parent solo gates: sets GREEN + PAIRING 24==24 + selftest 0 + KP leg x2 + BF exit 0 named markers; battery 37 at this tip — the 38th leg rides unmerged S19; stage gate: the S23 re-gate happens via TEST/DA c2 AFTER this fix merges) (code child, 2026-10-08): closes DA S23 verdict (dir 202610071711-b568eb26) finding P2-1 — an alternating-sign punch shipped fully GREEN: CAMERA_PULSE_DIR pins direction at the ONE sampled +4f frame and CAMERA_PULSE_PUNCH_075 pins an UNSIGNED magnitude, so the DA's plant (pulse kick parity-flipped like the shake idiom, excursion +0.75/+0.56/+0.37/+0.18 alternating OUTWARD) kept all five markers GREEN. Fix proof-side ONLY, zero product diff: the existing phase-2 EVERY-FRAME WALK in RuntimeIntegrationProof.KillPulse.cs now projects every off-base displacement pos-restPos onto the frozen player->camera sight and PulseFails BY NAME (CAMERA_PULSE_SIGN) when the dot is >= 0 — the DIR assert's own convention (dot<0 = punch-IN), sign of the unnormalized dot is scale-identical; adds no state, sample ordering untouched (A/A VERIFIED, the two GREEN runs BYTE-IDENTICAL). Honest doc line: the header claimed the off-axis residual bound as 1e-3 where the code enforces |perp|<1e-2 (DA P4-4) — DOC corrected, assert strength untouched. RED-capability proof: DA-1 re-applied verbatim to the pulse site in a THROWAWAY /tmp copy of the fixed tree (tree itself stayed clean) — leg went NAMED RED at the FIRST outward frame ("CAMERA_PULSE_SIGN: off-base kick (0.4723053, 0.34110928, 0.4723053) NOT toward the player at +1f", exit 1), revert BYTE-EQUAL (diff empty, EXIT=0). Gates: dotnet build 0 errors after rm -rf .godot/mono/temp/bin (fresh worktree); engine --build-solutions printed dotnet_build_project [DONE] 0 error lines but the headless editor did NOT self-exit — the quit-hang class the TEST wall recorded at 202610070307-25024946; stopped own process only, live DLL VERIFIED fresh AND carrying the new marker string; KP leg GREEN x2 exit 0, all five markers, values bit-equal producer/DA records (kick dist=-0.4688, window 8f, peak 0.75), KP r1 vs r2 byte-IDENTICAL; BF control exit 0 (pull-back=2.999982 height=1.499996 resid=0); MODE_SETS_CHECK GREEN (RuntimeIntegrationProof=28) + PAIRING OK 24==24 + --selftest EXIT=0; battery script UNTOUCHED — anchored grep '^run_mode ' stays 37 identical to base 5ecaba7 (the card brief said 38 — that number does NOT hold at this base, measured honestly, zero battery diff either way). Savegate: acquired 01:40:52Z held across the import/build window, RELEASED own-token 02:47Z per orchestrator operational nudge — remaining runs (KP x2, BF, planted-KP on the copy) provably CANNOT touch user://savegame.json (SaveGame()/save_game grep: zero call sites reachable from the KillPulse/BossFrame partials; only Save/Story2/Bus/Quests/ZoneBoss/Roster.Follow/CalmingSpeak press saves); foreign savegame.json writes observed 02:49:07-02:51:33 (foreign holder active — never touched). Evidence: /tmp/la-10280/.tmp/close-10280/ (EVIDENCE.md last line VERIFY_EXIT=0).
- MC 10273 / 10026.23.6 S19 story ACT THREE (the close), branch vm350/s19-act3 off master tip 5ecaba7 (contains S23 a062aa0 + S24 85b1b13), commits rebased 0456f6f..4fd8878 (7) onto 4446088 (code child + fix-cycles; 2026-10-07/08, **FF-merged 4fd8878 by orchestrator 08:0xZ — see merge clause**): RULING-8, board line MC 10026 append #9 row 895: "R8 zone four + act three = YES -> S18 + S19 proceed" (owner chat "Rec on all."). QuestTable.ActThreeArc(): the +4 zone-4 rows mirroring RuinsArc() idiom exactly (existing kinds, arrival->tongue->wage->reckoning own-fact chain, ZoneReached arg = S18-shipped "hollow", BossDead/LoyaltyAtLeast/DialogueShown stay unused per the same no-cascade ruling) + table-driven ActThree open/close cards on the shipped S10 ActCard mechanism + 8 new DialogueTable nodes (D6 tone). ACT-TWO-SYNC PATTERN act-3 equivalent: the mid-quest restore edge EXISTS, so the machine is REUSED not duplicated — ActTwoSync RENAMED ActChainSync (file/class/enum/csprojs/tests, zero arm changes; arms are (prevAct,thisAct)-parameterized), act three rides it over (act2, act3); WorldDirector.Story.Act3.cs NEW partial (third QuestLog; OpenActThree/AdoptActThreeOpen/ShowActThreeClose mirror the S10 seams; act-completion condition = ActThreeArc.IsArcComplete prints the named ACT_THREE_COMPLETE state-truth marker + close card); Story.cs deltas are chain not mechanism (third log on the ONE OnQuestChanged event, act-two finale also opens act three, reward+wage attribution across THREE tables, third log joins the ONE shipped v3 QuestStates save wire = DATA, ZERO save fields R7 holds, act-3 sync runs after act-2's so the chain composes). PlayActCard gains the act-id param — act_two marker byte-identical (quest_arc live-run). Zero new bus signals (census grep 15 + BARK leg in-leg re-grep inside the battery), zero scene edits, integer frame counters only. NEW battery leg (T) mode ACT_THREE stage 103 (UNIQUE number, pairing-gate enforced; 3-way doc/KnownModes/dispatch declared): opens via the REAL save path (owned save -> restore chain FreshOpens, act2 ADOPTS silently), 3 travels + 3 speaks + 1 wage + 8 kills = four completions four distinct drives (W6), mid-quest save round-trip ACT3_PERSIST no card replay, close card drained in order; battery header 37->38 restamped to the true grep IN THE LEG COMMIT (stale-count failure closed). Gates (one savegate hold 22:35:48Z-release, token 3fa4b295dd1a9125): dotnet build 0 errors; leg x2 EXIT=0; A/A determinism_cmp IDENTICAL (46 byte-stable + 2 masked); quest_arc/quest_persist/quest_arc2/wage_betrayal/positive/save EXIT=0, quest_neg red NEG_QUEST; boot GATE PASS; smoke RESULT=PASS colors 3698; mode_sets GREEN 29 + PAIRING 25==25 + --selftest EXIT=0; FULL battery EXIT=0, ZERO GATE FAIL, 38 in-log invocations, 2729 lines, final echo carries S19 clause. THREE plant-proofs (/tmp copy, tree untouched): FreshOpen gutted -> named RED 'act three opened by the restore sync chain'; act-completion edge disabled -> ACT_THREE_COMPLETE never printed + named RED 'close-card drain budget exceeded'; act-3 rows dropped from the save wire -> named RED 'ACT3_PERSIST: saved act-three rows reapplied'; all reverted byte-equal, revert run EXIT=0. savegate CONTENTION disclosed: foreign unheld taker cc-takeover backlog-rerun started 22:31:46Z (S24 DA-wall report), overlapped ALL my save runs; per the DA finding it can only flip RED, never fake green — every overlap leg GREEN except designed/red-by-plant attributions, holder survived every run (re-verified 23:00-23:28), zero unattributable REDs; foreign holders never touched. Restamps: entry SIZE 554 l / mode doc 183 l (awk, measure documented) / 29 modes; ARCHITECTURE src/story row (ActChainSync rename + ActThreeArc), Story.Act3 partial row, Story3 proof-mode row, battery 38=28+10, size list (Proof.cs 554, Story3.cs 281). Evidence: /tmp/la-10273/.tmp/close-10273/ (EVIDENCE.md last line VERIFY_EXIT=0; battery-full.log + a3-run1/2 + determinism + plant logs). MERGE CLAUSE (orchestrator, 2026-10-08): wall cycles closed both drivers — P2-1 (closeShown rewind never armed on load-path; planted both-clears-deleted = 4 fix legs NAMED RED, per-arm splits 2+2, single-clear proof both clears load-bearing; fix 444259c renumbered, TEST r3/r4 + DA-c2 digit-exact) + P2-1-c2 (wrapped fake-verbatim quote QuestTable.cs:291-292 defeated single-line grep — wrap-aware git grep -e "yes to" -e "zone4+act3" adopted as the census idiom, DA-c3 33-window scan collapses to the ONE board row-895 string; fix 4fd8878 comment-only, LEDGER:1245 carry corrected with disclosure). Verdicts (.audits/202610072059-22ac1aa8/out/): TEST-3/4/5 PASS + DA-c2 FIX->closed + DA-c3 FINAL SHIP (no P0-P2; P4 ride: wrap census + 8cdffe3 msg digit). Parent merge gates at rebased tip: build 0 err; quest suite 69/69 self-red run; ACT_THREE + quest_arc/2/persist legs EXIT=0; quest_neg NEG_QUEST red-by-design; mode_sets 29 + PAIRING 25==25 + selftest 0; grep 38; wrap-aware grep ZERO. Ride rows: MC 10281 (Adopt-arm rewind semantics, after-S19, own cycle) + ARCH-S19 wall fans post-merge.
- MC 10243 / W4 wave-close BARRIER (orchestrator, 2026-10-08): held full battery at combined tip 3f5008f (savegate token c9097419369ae2c7 held 12:38:42Z->release-at-close; log /tmp/w4-battery-full.log 2721 l): RUNTIME_INTEGRATION_TEST GATE PASS, 38 in-log invocations (28 positive + 10 red-by-design), ZERO GATE FAIL, ZERO LA_GATE: FAIL, BATTERY_EXIT=0. Wave stages on this tip: S22 gate split (10218), S23 camera-leg strength + S23b/c (10229/10280), S24 bark seam (10238), S19 ACT THREE the story close (10273), S25 docs map-sweep (10255), S26 grep widen — each TEST+DA-walled (S23 also ARCH-walled incl c2 re-seal PASS 12:35; S19 ARCH c1 FIX->3f5008f->c2 PASS 12:2x). Findings closed: P2-1 + P2-1-c2 (S19), 516-stamp family (S23/S25), wrapped-quote census class (wrap-aware grep adopted fleet-idiom in this repo's ci docs). P4 rides to owner report: BARK .uid one-commit lag, 8cdffe3 msg digit, leg-letter collisions NOTE-d, Quests ~560 tilde-stamp, MC 10281 owes its own pin. Infra recs #1 (--build-solutions quit-hang) #2 (pin-staleness) #3 (no-base hash form) stand.
- MC 10281 / S19f Adopt-arm closeShown rewind, branch vm350/s19f-adopt-rewind off master 628f7f0, commit b645bfd (code child, 2026-10-08, ****FF-merged 982ee25 as-is by orchestrator 2026-10-08 (sha unchanged — pure FF off 628f7f0); walls green BEFORE merge: TEST-verdict PASS (independent plant-redo, mutant F -> exactly the 2 ArmGate legs RED, P2-1 four legs GREEN) + DA-verdict SHIP (5 plants incl wrong-order D RED; P4-1/P4-2 ride); parent solo gates at tip: build 0 err, sets 29 + PAIRING 25==25 + selftest 0 + grep 38, quest 73/73 self-red, ACT_THREE/quest_arc2/quest_persist legs EXIT=0 (savegate held+released); commit amended ONCE to carry this LEDGER row (b645bfd->982ee25, disclosed, zero code drift)**): spec = finding DA-verdict 22ac1aa8 P3-2 (probes S-C/S-D: after the session shows act-N's close card, loading an OLDER mid-act-N save takes the ADOPT arm (rows exist, act incomplete) and closeShown stayed latched — act-N's close card could never print again in that session) + DA-c2 G2 (mutant F, a global closeShown=false at the TOP of Run, stayed 69/69 GREEN — the suite could not see arm-gating of the clear). MECHANISM: the ONE existing machine, the fix the finding itself names — the ActChainSync ADOPT arm's incomplete branch rewinds the close latch (`else closeShown = false;`); both act-two and act-three instances ride it (intended + disclosed, RULING-8 already ratified the shared (act2,act3) machine); rewind computed in-session from the loaded rows — ZERO new save fields (R7 holds), ZERO new bus signals, ZERO scene edits, no new sync class, zero engine-site deltas (both seams pass the latch by ref and re-latch nothing on Adopt). FOUR new legs, quest pure suite (69->73): ActTwoArcTests.Restore_AdoptArm_IntoOlderMidActTwoSave_CloseCardReplays + ActThreeArcTests.Sync_AdoptArm_IntoOlderMidActThreeSave_ActThreeCloseReplays (AdoptRewind: close shown in session via the Adopt latch, load-shuffle into an older mid-act save stays on the Adopt arm with rows adopted not rolled back, latch REWOUND, finale edge prints the close card AGAIN once) + ActTwoArcTests.Run_FreshOpenArm_PassesTheCloseLatch_Untouched + ActThreeArcTests.Run_FreshOpenArm_PassesTheCloseLatch_Untouched_ActThree (ArmGate: FreshOpen passes the latch UNTOUCHED — the mutant-F counter-proof G2 demanded). Plant-proofs: (a) rewind removed -> the two named AdoptRewind legs RED (2F/71P, plant-a.log); (b) mutant F global-at-top + arm-gated clears removed -> the two named ArmGate legs RED while the other 71 — incl. all four P2-1 legs and the AdoptRewind legs — stay GREEN (2F/71P, plant-b-mutantF.log, G2 reproduced exactly); (c) all reverts byte-equal (sha256 before/fixed-tip/after-reverts in .tmp/close-10281/), final clean run GREEN. Gates (pure dotnet, NO engine process — SAVEGATE not needed, none held): dotnet build LastAnimalPreflight.csproj 0 errors; ci/quest_test.sh GATE PASS TWICE at the tip (run1 self-red Failed=1 Total=74 with harness self-test, run2 73/73 GREEN); count restamped HONEST 69->73 at own tip (runner header carries NO static stamp — grep zero, count is dynamic off dotnet output); the four P2-1 legs' semantics untouched (their arm-clear sites unmoved, GREEN in every run incl. mutant F). Restamps (wc): ActChainSync.cs 77->81 l, ActTwoArcTests.cs 400->510 l, ActThreeArcTests.cs 392->507 l (test ceiling 600), LEDGER.md 1256->1257 l. Evidence /tmp/la-10281/.tmp/close-10281/EVIDENCE.md last line VERIFY_EXIT=0.
- MC 10339 / 10026.26 S21 Inc-4 CLOSE-OUT (export + perf re-run + close restamp) (code VM350, 2026-10-08, branch vm350/s21-close off master dedb016): Inc-4 close at dedb016 — parent-held barrier re-read at this tip: FULL battery 38 in-log invocations, ZERO 'GATE FAIL' lines, final GATE PASS echo, BATTERY_EXIT=0 (/tmp/s21-battery.log) + determinism_cmp positive A/A (i) 11 byte-stable lines IDENTICAL + (ii) 4 masked-IDENTICAL = DETERMINISM: IDENTICAL (/tmp/s21-cmp.log). EXPORT leg (savegate HELD 19:46:40Z->released post-perf, holder 'card 10339 host=vm350' — written as card 10282 pre-card-correction, holder updated to 10339 at 19:54:57Z and disclosed; foreign holders checked before acquire, never touched): ci/export_check.sh ran BOTH Windows presets per its own contract (default full-game + explicit M00 preflight) — EXPORT_CHECK: PASS build/LastAnimal.exe 135,383,432 B PE magic + all three C# markers; EXPORT_CHECK: PASS build/preflight.exe 109,389,856 B; tools/export_linux.sh EXPORT_LINUX: PASS — Linux ELF gate 99,710,664 B + LastAnimal-linux-x86_64.zip 81,697,814 B (187 entries: binary + data_LastAnimalPreflight_linuxbsd_x86_64/ carrying libhostfxr.so + libcoreclr.so + 170 .dll incl LastAnimalPreflight.dll; pck EMBEDDED — embed_pck=true, preset.2, GDPC magic x7 inside the binary, no loose .pck BY DESIGN); independent UNZIP verification in a throwaway /tmp dir (exe ELF magic + hostfxr/coreclr/assemblies all present) + the UNPACKED exe ran headless under a 45 s timeout (exit 0, booted: 'director ready', navmesh baked — the known engine-quit resource-in-use noise class recorded honestly); the repo's OWN packaged-launch render bar tools/launch_linux_smoke.sh RESULT=PASS (non-blank Xvfb framebuffer capture, colors 4815, PNG 196,584 B) — no display-less fallback needed, the defined proof exists and passed. Artifacts STAY at skeleton/build/ for the orchestrator's release leg (gitignored 'skeleton/build/', zero build output committed). PERF leg (tools/perf_probe.sh RUN=close10339 window=60 wait=60, same harness+machine as S11, all zones RESULT=PASS, real-surface, n=60/zone): per-surface table vs the S11 aa-mean baseline (perf.md baseline column): meadow wall 35.067 vs 30.658 (+14.4%), draw_calls 18 vs 28 (-10, constant every frame); canyon 34.950 vs 32.842 (+6.4%), draw 8 vs 8 (0); ruins 39.267 vs 32.875 (+19.4% — the ONLY delta past the 15% material threshold, NAMED honestly), draw 29 vs 32 (-3). NO material S13-S23 render-stage frame-time regression CLAIMED: the named ruins wall delta sits INSIDE S11's own pooled render-identical control bound (11.317 ms, FINDING F3 — wall-ms drifts up to ~10-11 ms between passes on render-identical trees on this shared llvmpipe box) while the STABLE draw_calls column (S11: spread 0.0) held or IMPROVED in all three zones — fewer draw calls + higher wall-ms is not render-stage regression; honest labels: single unpaired pass, so per F3 no wall-ms reading here can carry a threshold claim (an adjacent A/A re-run is the owed instrument if the ruins row is to be RULED, not just named); the alternate anchor a2 (meadow 26.650) reads +31.6% on meadow — same noise-bound treatment, both anchors disclosed. RESTAMP leg (doc-only, zero behavior): docs/ARCHITECTURE.md re-derived at dedb016 — §3 module-row size stamps ALL TRUE by wc (Story.Act3 110 / Roster.Bark 65 / DayNight 280 / Motion 285 / VisualJuice 186); §6 proof-size sentence ALL TRUE (Proof.cs 557==in-file SIZE header, Chain 221, Bus 566, Juice 126, Shake 121, Story2 288, Story3 283, Dissolve 209, DayNight 138, Motion 307, Passives 138, P1Fix 368, Quests 559, Compose 285, Harness 70 — 20 files wc'd, zero drift); §6/§6.1 battery+gate rows TRUE: 38 = 28+10 by anchored grep, EventBus census grep = 15, mode_sets GREEN (29 modes) + PAIRING OK 25==25 + --selftest EXIT=0, mode-doc block 185 l by the doc's own awk, max stage 103 / NEXT FREE 104 (zero case-104/_stage-104 hits), 29 ci_proofs .cs.uid sidecars tracked, §1 22 distinct .glb grep re-derived, §5 export rows TRUE (ran THIS close: ci/export_check.sh x2 [its Windows presets, EXPORT_CHECK PASS] + tools/export_linux.sh + packaged smoke; tools/export_windows.sh NOT exercised at v0.4.0 — the published LastAnimal-windows_x86_64.zip is the release-leg assembly of those gate-checked build/ artifacts, digest-verified by the DA wall). TWO doc changes rode this commit: §5 Linux packaging row glob restamped `data_*_linux_x86_64/` -> `data_*_linuxbsd_x86_64/` (the name the mono export WRITES and the script globs is linuxbsd — the ONE real drift found; this close's zip proves it); §1 top-level table gained a `skeleton/build/` row (exports + data dirs + zips — generated, not source; the fact already lived whole as the §8 Build-artifacts bullet — ADD of a layout-map row, not a drift fix; a suspected dangling §7 fragment turned out to be a misread of that intact §8 bullet by the close session's doc view — verified against git, no §7 edit was needed nor made). STATUS: INC-4 CLOSED at dedb016 + this close (W1 restamp through S26 + the W4 BARRIER MC 10243 + S23c MC 10280 + S19f MC 10281 + this export/perf/restamp leg): battery 38/0 EXIT=0, cmp IDENTICAL, exports PASS both platforms packaged+smoked, perf no material regression (one >15% wall delta named + bounded) — NOT-YET-MERGED: this commit rides branch vm350/s21-close off dedb016; the FF-merge to master + release leg is the orchestrator's. Evidence: /tmp/la-10282/.tmp/close-10339/ (EVIDENCE.md last line VERIFY_EXIT=0; export/perf/unzip/mode-sets logs + smoke PNG; host-local, gitignored by doctrine).
- ORCHESTRATOR MC 10026 / S21 RELEASE LEG (2026-10-08): master 7177227 FF + pushed; GitHub release v0.4.0 PUBLISHED https://github.com/bryn1/Last-animal/releases/tag/v0.4.0 (linux zip 81,697,814 B + windows zip 130,262,791 B attached for owner C16 smoke). Walls TEST/DA/ARCH over 7177227 pending lane GO (lane saturated by fleet, not this session).
- ORCHESTRATOR MC 10026 / S21 WALL CLOSE (2026-10-09): TEST PASS 14/14 + plant-red; ARCH PASS (0 P1, 68 rows swept); DA c1 FIX (2 P2 truth-patches -> 5144eba, LEDGER-only) -> c2 SHIP (rows true vs disk, release wording honest, digests unchanged). DONE.md + 4 verdicts: .audits/202610081930-8291690c/out/. STATE: INC-4 stages S11-S21 all merged + walled; release v0.4.0 published + digest-verified; remaining = owner-only: C16 Windows smoke (asset attached), W5 pick, R1/R2 ratify. Rides to W5/floor: P3-1 log commit-binding, P3-2 stale .git/config user.name, P3-3 dead evidence pointers x2, P3-4 publish-after-walls rule, perf paired A/A if ruins RULED, linux ICU, verify.sh raw-perf + plant-red.
- MC 10404 / 10026.27 P1 fall-through-floor death — floor-coverage proof + collision seam fix + fall-recovery guarantee (code child VM350, 2026-10-09, branch vm350/floorfix off master b107cd5, commits eea184f + e88222e): OWNER EVIDENCE: v0.4.0 Windows playtest screenshot — camera UNDER the terrain mesh, decor seen from below, Life 0, magenta death dissolve (owner-2-below-floor-death.png, .audits/20261009-0615-ownerplaytest/). ROOT CAUSE (VERIFIED by the new instrument at b107cd5, coords in evidence): terrain_builder.gd laid the HeightMapShape3D at the PNG PIXEL COUNTS (map_width=_w/map_depth=_d) — a HeightMapShape3D spans map_width x map_depth METRES (one sample per metre) while the visual mesh + navmesh span pixels x h_scale metres; h_scale is 1.5 meadow / 2.0 canyon / 1.6 ruins (meadow.tscn etc.), so EVERY shipped zone had a floorless ring beyond +/- map-half: meadow +/-32 m under a +/-48 m visible ground — main.tscn instantiates ONLY Meadow, the owner's exact fall; canyon +/-128 under +/-256; ruins +/-128 under +/-204.8. Grid probe (2 m step, downward rays, zone-booted alone): uncovered == 1 - 1/h_scale^2 TO THE PERCENT (canyon 49920/66049 = 75.6 %, meadow 1440/2401 = 60.0 %, ruins 25807/42025 = 61.4 %). SECONDARY, VERIFIED: body.position y = v_scale*0.5 floated the remaining collider v_scale/2 above the surface (meadow hits median +0.82 m over a 0..1.5 ground — Godot 4.7 map_data is RAW local-Y, no top-origin pivot; the file's header claim "collision and rendering provably agree" was FALSE for h_scale != 1 — restamped). CONTRADICTED-IN-LEG: the suspected 255x255 map-dimension cap — 256x256 canyon/ruins DO mount collision (middle-region hits measured); pixel count was never a cap. TUNNELING/SPAWN-UNDER: not needed as explanations (weak, owner-note agrees). FIX (e88222e): build_collision resamples the SAME nearest-pixel field at ONE SAMPLE PER METRE over the scaled extent, mw = ceil(_w*h_scale)+2 (+2 MEASURED: grids ending AT the border gave raycast-inconsistent edge rings — mw=ceil exactly missed canyon +/-256; +1 missed meadow -48 while +48 hit; +2 all interior, deterministic), zero body position (offset gone), heights raw, edge ring clamps to border pixel; Godot 4 IGNORES PhysicsShape3D node scaling — geometry resampling is the ONE seam fix, no second ground, no invisible walls. GUARANTEE (same commit, WorldDirector + terrain floor_min_y meta): player below the loaded ground's floor_min minus 2 m -> back to the zone-entry spawn (reuse of the TravelToNextZone reposition point), fall velocity CLEARED, Life/model UNTOUCHED, product line FALL_RECOVERED from=... -> zone-entry=...; missing ground meta disarms LOUD (PushWarning + the leg goes RED, never silently); rescue, never punishment. LEGS (eea184f, leg-first RED captured at b107cd5): battery 38->40 (30 positive + 10 negative, grep -cE '^run_mode ' == 40); FLOOR_COVERAGE = new companion class FloorCoverageProof.cs (csproj ItemGroup END, .uid committed, mode_sets PROOFS += it — GREEN 5 proofs, unknown-mode FAIL LOUD from day one; probes every zones/*.tscn ALONE, no main.tscn; decor-only bluetest/redtest print NAMED skips); fall_recovered = P1FixProof stages 50/51 (player forced y=-30 below the LIVE world -> recovery byte-exact XZ, health 100==100, 90f no re-fall). PLANTED-BAD PAIRS (doctrine, both logs kept): meadow collider return-removed -> RED naming meadow 2401/2401 -> restore GREEN; clamp short-circuited -> RED no-recovery-120f -> restore GREEN. GREEN: FLOOR_COVERAGE_CLEAR zones=3 probes=110475 uncovered=0 EXIT=0; FALL_RECOVERED_EXACT EXIT=0. FULL BATTERY at e88222e: 40 mode invocations, ZERO GATE FAIL, ZERO LA_GATE: FAIL, final GATE PASS echo, BATTERY_EXIT=0 (log /tmp/la-10404/.tmp/close-10404/battery-full.log, 2959 l; savegate held through, released at close). DISCLOSED IN-LEG INTERACTION: calm_use fires the guarantee TWICE (product FALL_RECOVERED at x=66.69) because the leg CO-LOCATES the player with wild id 24 and that spawn sits beyond the meadow ground edge — pre-existing choreography (player used to sink there silently and the leg passed anyway, its asserts are state-not-position); the guarantee now RESCUES it, all calm asserts stay GREEN. Docs restamped in-file: ARCHITECTURE 40=30+10, proof sizes, FloorCoverageProof census, sidecars 30, fail-safe law + failless clause; camera dual-current canyon/ruins .tscn NOT touched (rides S39). Evidence /tmp/la-10404/.tmp/close-10404/EVIDENCE.md last line VERIFY_EXIT=0.
- MC 10404 / CORRECTION (append-only, walls cycle 1; corrects the IDENTITY wording of the 10404 row above — its ROOT CAUSE stands UNCHANGED): the claim "per-zone uncovered ratio == 1 - 1/h_scale^2 TO THE PERCENT" is FALSE for meadow (DA wall F1, .audits/20261010-0040-floorgate/out/DA-verdict.md, independent math): measured meadow 1440/2401 = 59.97 % (discrete 2 m grid over the +/-48 m visual span) vs the continuous-area ideal 1 - 1/1.5^2 = 55.56 % — the gap is grid quantization plus edge-sample misses on the small 49x49-point map, NOT a different mechanism. Canyon 75.6 vs 75.0 and ruins 61.4 vs 60.9 DO hold within ~1 %. Correct identity: uncovered ratio == the DISCRETE probe count, which APPROACHES 1 - 1/h_scale^2 as grid resolution grows and terrain relief flattens (canyon/ruins within ~1 %, meadow +4.4 pts on a 2401-point grid). The footprint-mismatch root cause (pixel-count metres vs pixels x h_scale visual span, floorless ring beyond +/- map-half on every zone) is independently wall-confirmed; only the percent-exactness wording is corrected. Walls this cycle: ARCH F-1 (WorldDirector SIZE stamp restamped 572->573 by wc AT the head — the prior delta chain summed 569, the §6.1-7 arithmetic-off-a-prior-stamp class), ARCH F-2 (§6.1 step-4 PROOFS parenthetical gained FloorCoverageProof — gate prints 5), DA F1 (this row). Product code and battery UNTOUCHED (docs-only commit).
