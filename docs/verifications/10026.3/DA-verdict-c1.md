# DA-verdict.md — Calming Speak DESIGN rev 1 (MC 10031) — devils-advocate gate, cycle 1

Target: `.audits/20261003-CSK/DESIGN.md` (258 lines; body hash verified intact:
`head -n -1 DESIGN.md | sha256sum` = `ebb8b628…` == `out/DESIGN.md` entry in
`.tmp/judged/674b0d6d*.json`). Judged against current HEAD (DLQ impl `4fece211`, close `7079a00`
docs-only — the DLQ commit touched only Story/Dialogue/Quests/QuestTable/docs, so every
Roster/Skills/Body line citation below was checked against the current tree). STATIC ONLY:
read/grep/git show — no gates, no dotnet (runtime claims are code-reads, labeled INFERRED).

Verdict driver: two P1s in the normative rule machine/persistence claims; one P2 in the
planted-bad pinning of the pay-first invariant. FIX.

## Findings

### P1-1 — rule machine spends BEFORE it knows a target exists; contradicts its own legs
DESIGN §1.2 pins "in EXACT order": step 3 = `TryCalmingSpeak(unlocked)` = `TrySpend(12)`
(step 4 scan, §1.4 confirms signature `(bool unlocked)`), step 4 = target scan with
"No castable target → REFUSED, nothing spent", header "rejection spends NOTHING".
`TrySpend` (`src/skills/SkillState.cs:147-153`) DEDUCTS when it returns true — the existing
arms (TryInvertStrike/TryMend, SkillState.cs:82-104) spend first because they need no target;
a targeted skill copying that order eats 12 Manna on every no-target and every
standing-offer press. The design's own leg CALM_REFUSE_STANDING (§4: "refused, nothing spent")
goes RED against the transcribed order — the contradiction surfaces only at build, and §1
forbids the implementer to repair ("transcribes, does not repair").
Fix (no re-plan): swap steps 3 and 4 — scan for a castable target first, then call
`TryCalmingSpeak`, then step 5; PB-SPEND-FIRST still pins short-balance refusal.

### P1-2 — "A load clears every window with its body" (§1.1) is FALSE; this is the DLQ-R9 bug class
Load restores IN PLACE: `SaveLoadController.Load()` (`world/SaveLoadController.cs:172-190`)
invokes `MannaRestore`/`FollowersRestore`/`QuestStatesRestore` against the LIVE scene, and
`LoadGame()` (`world/WorldDirector.cs:483-488`) only `ResetRosterDeltas()`. Nothing touches
`_wild` on load — DLQ needed R9 `ClearPresentation()` precisely because of this
(`world/WorldDirector.Story.cs:69`; its commit says "queue never persisted" = cleared on
restore). Bodies and their ints SURVIVE load. Consequence: save at Manna=30 → cast (Manna 18,
window open) → load → Manna rolls back to 30 while the offer+window persist and keep ticking —
free, repeatably-free calming (save-scum economy rollback), and §1.1's "stated honestly, not a
bug" line states the opposite of the code. Zone travel also persists windows
(`TravelToNextZone` `WorldDirector.cs:350-360` despawns only `_zoneEnemies`) — see P4-1.
Fix (no re-plan): mirror R9 — on the load-restore seam, zero every `_wild` body's
`CalmedWindowFrames` and `CloseRecruitOffer` each body whose window was > 0 (free E-standing
offers untouched); zero save-schema delta is preserved; add a `CALM_LOAD_CLEARED` leg.

### P2 — PB-SKILL-JOINS names a red set that cannot go red (pay-first pin cites a phantom leg)
§4: "make the cast join directly → … CALM_WINDOW_REFUSE + REG-ROSTER go RED" and §6 pins
"pay-first … Pinned by CALM_PAY_IN_WINDOW + CALM_WINDOW_REFUSE pair". `CALM_WINDOW_REFUSE` is
never defined (§4's legs: CAST / PAY_IN_WINDOW / REFUSE_SHORT / REFUSE_STANDING /
WINDOW_EXPIRES / E_STANDS), and REG-ROSTER (roster/skill/ui suites driven E+pay, skill unused)
is UNAFFECTED by a direct-join mutation — it stays green. The real reds for that mutation are
CALM_CAST (post-cast asserts `RecruitOffered==true && CalmedWindowFrames==600` on a WILD body
— a direct join destroys it) and CALM_WINDOW_EXPIRES (no wild → no EXPIRED line). Same DLQ F-M
shape: a planted-bad whose claimed detection is vacuous.
Fix (no re-plan): re-point PB-SKILL-JOINS at CALM_CAST + CALM_WINDOW_EXPIRES and rename the
§6 pair accordingly.

### P3-a — wrong file cited for the REG-ROSTER OFFERED leg
§4/§6 cite "RuntimeIntegrationProof.cs:183 region"; the OFFERED leg is
`ci_proofs/RosterIntegrationProof.cs:183` (`if (wild.RecruitOffered) { Input.ActionRelease(
"interact"); _sub = 3; }`) — commit 3514d92 split RosterIntegrationProof by concern.
Fix: correct the filename.

### P3-b — the `PlayerMutationsTests.cs:89` "flip" is wrong as written
:89 is `Assert.False(PlayerMutations.IsUnlocked("calming_speak", profile))` with
`profile = CounterProfile(new[]{1,1},…)` — 2 positions. With `CalmingSpeakPositions = 3` the
rule-correct verdict for THAT profile stays FALSE (§4's own F9: "2 does not"). A literal flip
to `Assert.True` fails. Fix: rewrite the row with a 3-counter profile asserting True; keep the
2-counter row asserting false (F9) and one genuinely-unknown id false.

### P3-c — cost 12 has no owner number (NOTE only; non-gating, per HANDOFF)
`CalmingSpeakCost = 12` is designer-chosen, sitting honestly between the real
`InvertStrikeCost=10`/`MendCost=15` (`SkillState.cs:46-49`), in the sanctioned tunable home.
The pay-first inequality (12 + pay-press > pay-press) holds at any cost > 0, so nothing gates:
proceed at 12; retune needs only a one-const edit, an owner ruling is optional.

### P3-d — §5 hygiene claim contradicts reality for the file it grows
"every file stays far under ~250" is false at the site the design grows:
`world/WorldDirector.Roster.cs` is 422 lines whose header (:1-4) says "split is owed before
any further growth here"; the design adds +14/−6 (→ ~430; passes the mechanical gate — reason
in first 5 lines, < 600 — but the claim and the owed-split note must be owned, not ignored).
Fix: one §5 row acknowledging the growth against the owed-split (helpers live in the offer's
home partial; split owed to a later card).

### P4 (notes, no action demanded)
- P4-1: window persists across ZONE TRAVEL (decay keeps running while the player teleports
  away); §1.1 dispositions only (falsely) the load case — add a zone row with the P1-2 fix.
- P4-2: join leaves a stale `CalmedWindowFrames > 0` on the new follower (`TryRecruitOfferedWild`
  `Roster.cs:220-226` clears the flag, never the window; sweep only sweeps `_wild`) — zero it
  at the join helper line; dead state today, confusing tomorrow.
- P4-3: "12 Manna + the real first wage" overstates: recruit debits no Manna (no balance check
  in `TryRecruitOfferedWild`; wage settlement is Needs-based via `PayDueFollowers`,
  `Roster.cs:141-143`) — the pay press costs an opportunity, the inequality still holds.
- P4-4: PB→leg attributions loose though every mutation IS caught: PB-PRESS-ORDER variant "open
  before spend" reds CALM_REFUSE_SHORT's no-offer assert, not the CALM_CAST marker asserts;
  PB-DOWNGRADE reds immediately at cast (Manna spent), not at T+601.

## Attacks that HOLD (traced, no finding — the DA checked, don't re-fight)
- **Pay-first invariant intact**: the ONLY writers of `RecruitOffered` are `Roster.cs:222` and
  `:281-283` (grep: no others in world/src/ci_proofs/tests); the ONLY exit of `_wild` into the
  roster is `TryRecruitOfferedWild` `Roster.cs:220`, whose ONLY caller is the `payPressed` arm
  `Roster.cs:100`; `_roster.TryAdd` callers are boot (:66), load (:321), recruit (:217). A
  calmed window alone can never join, upgrade, or bypass the pay press.
- **Frame account correct**: `_Process` order verified — `TickRoster` (WorldDirector.cs:247) →
  attack → interact (:253) → … → `PollSkillActions` (:262) → `TickUi` (:263). Sweep-after-guard
  ⇒ T+1..T+600 payable, close at T+600 post-TickRoster, T+601 falls through to the unchanged
  roster-pay/skip semantics; same-frame cast-then-pay impossible exactly as §2 pins.
- **Helper extraction E-path-preserving**: under §3's flip-only-print spec (helper called
  unconditionally — the only self-consistent reading), E on never-offered / already-standing
  wilds prints and writes exactly as today; §1.3's E-upgrades-windowed-to-standing follows, and
  the sweep (only `CalmedWindowFrames > 0`) can never eat a standing offer. CALM_E_STANDS +
  PB-EXPIRY-CLEARS-E are provably red.
- **No unlock race**: `_spokenDna` has ONE writer, append-only (`WorldDirector.cs:447`) — live
  read cannot flip CalmingSpeak OFF mid-run; unlocks round-trip via LearnedDnaCounters
  (PlayerMutations header), so the §1.4 live-read idiom is race-free. The 6-mer degeneracy
  (`DnaLanguage.cs:52-57`, all-signatures-6) is inherited and honestly flagged (existing gate
  already flips both skills on one marker line, runtime_integration_test.sh:198).
- **Scope-zero claims true**: `SfxRouter.cs:31` is literally `bus.SkillUsed += _ => Fire(
  "dna_spoken")` (autoload/); `skill_3` = physical 70 (F) already bound, skill_1/2 = Q/R
  (81/82) so the chain-order claim holds; `MannaCap=100` setter clamp
  (`PlayerController.cs:44-52`); `RewardLineFrames = 240` + uniform auto-close real
  (`DialogueSystem.cs:53,83,143`); `SpawnWildFollower` seam public; neg idiom real
  (RuntimeIntegrationProof.cs:147-148, set before AddChild); NAMED-F-ACT census F1..F6+N2 in
  ci/skill_test.sh:79-95; `SkillFrameBudget = 6000` is a same-class private const (new partial
  may share it; 602/700-frame waits have the DLQ 3×240+8 precedent); `CalmingSpeakRange 9.0 >
  TalkRange 4.5` so "reach option" is honest; release list (RuntimeIntegrationProof.cs:522-528)
  genuinely needs the `skill_3` row; extra prints break only substring-positive asserts — the
  recruit-path "offer cleared" line is harmless to every named regression suite; SkillsPanel
  rows 8/42/76/120, +row 110 and Manna 154 fit the fixed `RegionHeight = 260` crop.

## UNCHECKED (honest partial; static-only + time-boxed per orchestrator)
- Did NOT read `.audits/202610040005-a9dfe75c/out/HANDOFF.md` directly — used the task's
  distilled watch-items (cost-note framing may drift from the designer's literal wording).
- `EcosystemAdaptation.ModelPlayerDna` counter-growth semantics not traced (degeneracy claim
  taken as design-inherited + gate-marker evidence; a designer fix needs it neither way).
- No gate/dotnet run: every runtime statement here is a code-read (INFERRED), not executed.
- player-guide's skill_3 copy not line-compared against the input map beyond the DLQ commit note.

## Disposition
FIX — producer (design profile) re-spawn with P1-1 (scan-before-spend reorder), P1-2
(R9-mirror window clear on load restore + honest §1.1/zone rows), P2 (re-point PB-SKILL-JOINS
at its real red set), P3-a/b/d as one-clause doc fixes; P3-c is a non-gating note; P4 optional.
No mechanic-shape problem: the bridge design (window opens offers, wage stays the sole join
authority) is sound — nothing here is RECONSIDER-material, and the pay-first ruling stands.
# JUDGED: 12aef143efb23f7e6109a422335d534153c05050552829a89ff13d3f49148bb2
# VERDICT: FIX
