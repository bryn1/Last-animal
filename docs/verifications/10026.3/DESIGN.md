# DESIGN.md — Calming Speak (MC 10031) — THIRD skill, the skill→recruitment bridge

Status: DESIGN rev 3 — DA cycle-2 DELTA applied (c1 verdict `DA-verdict.md` 12aef143: all
findings implemented, DA verified fixed; c2 verdict `DA-verdict-c2.md` b44daf94 FIX: F-1
`ClearCalmWindows()` re-pinned to the END of `RestoreFollowers` — the in-`Load()` restore
callback hit by BOTH load entries (the player `load_game` key bypasses proof-only `LoadGame()`;
F-3c moot); F-2 PB-PRESS-ORDER detector → CALM_REFUSE_STANDING's zero-delta assert; F-3a pair
clause; F-3b §5 header aligned). Repo `/home/svarkor/last-animal`, branch `vm350/last-animal`,
gate base HEAD `3514d92d2e7c2985c6c0d3b7117bc8682b24334f`. Author: `plan` profile child. This
is a DESIGN — it names code, it does not contain it.

## 0. Ruling, scope, placement plan

**Ratified owner ruling** (Inc-2 PLAN §G D4 item 4, `.audits/202610020433-820b032c/PLAN.md:128`,
verbatim): *"Skill set — Invert Strike + Mend in this increment; Calming Speak = follow-up card
after 2g (own DA). **RATIFIED.**"* — and PLAN line 118 (DA-c1 P3-10, ACCEPTED): *"Recruit flow
stays pay-first; the bridge would put a skill authority on joining the roster…"* This card is
that follow-up: a skill becomes a way to OPEN a recruitment, never a way to SKIP the wage.

**Mechanic in one line:** F (skill_3, bound-inert today in `project.godot` — activated, zero
input-map diff) spends 12 Manna to calm the nearest WILD creature in speak range, opening a
frame-bounded 600-frame recruit window — joining still requires the first wage (`pay_wage`, unchanged).

**Placement plan (workspace-convention):** not a new project — the canonical tree is the existing
repo; `docs/ARCHITECTURE.md` stays the Architect's doc (its §2/§3/§6 rows gain the skill_3 facts
at close, §8). This run's dir is `.audits/20261003-CSK/`; DoD-loop files in
`.audits/202610040005-a9dfe75c/`; scratch `.tmp/`. Code edits land in the existing module map
(`src/skills/`, `src/dna/`, `src/ui/`, `world/`, `ci_proofs/`, `tests/skill|ui/`) — nothing new outside §5.

**DLQ — LANDED (`4fece211`, docs close `7079a00`; rev 1 said "in flight"):** this design calls
`DialogueSystem.Show` ZERO times and queues zero reward beats — NO dialogue-box interaction.
Post-DLQ note for the record: if a future beat ever speaks this skill, a reward beat queues
(DLQ §2.1) and `DialogueSystem.RewardLineFrames = 240` governs the uniform auto-close (DLQ §2.2).

## 1. The rule machine (normative — implementer transcribes, does not repair)

### 1.1 State: what "calmed" IS, and where it lives

`CompanionFollowBody.CalmedWindowFrames` — a new `public int { get; set; }` sitting directly
beside the existing `RecruitOffered` (`skeleton/world/CompanionFollowBody.cs:54`). Calmed =
`CalmedWindowFrames > 0` on a body in `_wild`. That is the whole state; there is no second flag.
- It is VIEW/run-time body state, NOT a roster or empathy contract — those stay frozen (§6).
- Not saved (schema v3 ZERO-delta): the offer is not persisted (`FollowerEntry` = roster only), so
  calm state must NOT survive a load. Rev 1's claim that a load "clears every window with its
  body" was FALSE (DA-c1 P1-2, the DLQ-R9 bug class): `SaveLoadController.Load()`
  (`world/SaveLoadController.cs:172-190`) restores Manna/Followers/QuestStates IN PLACE onto the
  live scene and `LoadGame()` (`WorldDirector.cs:483-488`) only `ResetRosterDeltas()` — `_wild`
  bodies AND their ints SURVIVE. Without a rule: save@30 → cast(→18, window open) → load ⇒ Manna
  back to 30 with the window still ticking = repeatably-FREE calming (save-scum economy rollback).
  NORMATIVE (R9 mirror — the pattern of `QuestStatesRestore = rows => { …;
  _dialogue?.ClearPresentation(); }`, `WorldDirector.Story.cs:69`): the load-restore seam calls
  `ClearCalmWindows()` (§3) — every `_wild` body with `CalmedWindowFrames > 0` gets zeroed AND
  `CloseRecruitOffer(w)`; E-standing offers (window 0) untouched, by seam and sweep alike.
  Pinned by CALM_LOAD_CLEARED (§4).
- ZONE TRAVEL PERSISTS windows (DA P4-1, explicit stated behavior, not a bug):
  `TravelToNextZone()` (`WorldDirector.cs:350-360`) despawns only `_zoneEnemies` — wild bodies
  ride through with their offers/windows and the frame-decay keeps running (same persistence
  class as E's standing offer today). §6 row; no leg.
- Decay: `PollSkillActions()` (Skills partial) sweeps `_wild` EVERY frame, decrementing each body
  with `CalmedWindowFrames > 0` by 1; at the frame it reaches 0 it calls `CloseRecruitOffer(w)`
  (§3) and prints `ROSTER: calm window EXPIRED on wild id <id> — offer withdrawn`. The sweep runs
  BEFORE the press chain (§2). Gate seam: it lives after the `if (!_skillHooksEnabled …) return;`
  guard — a mid-window `SetSkillActionsEnabled(false)` FREEZES windows like it stalls 2e economy.

### 1.2 Cast conditions (F = skill_3), in EXACT order; rejection spends NOTHING

Added as the third arm of the existing `else if` press chain in `PollSkillActions`
(`WorldDirector.Skills.cs:82-102`; Q-press wins over F on the same frame — chain order, like Q
over R today). Per press, first match wins:
1. Gate seam off (`!_skillHooksEnabled`) → inert. (skill_neg leg, §4.)
2. Unlock read LIVE — `PlayerMutations.Unlocked(ModelPlayerDna(_spokenDna)).CalmingSpeak` (§1.4)
   — locked → spend nothing (2e idiom: "a locked use is rejected WITHOUT spending").
3. Target scan — BEFORE any spend (rev 2, DA-c1 P1-1: `TrySpend` deducts on true,
   `SkillState.cs:125-131`; the Q/R arms spend early only because they need no TARGET — a
   targeted skill knows a target exists before it takes the player's Manna). Nearest body in
   `_wild` within `CalmingSpeakRange` that is castable: `!RecruitOffered ||
   CalmedWindowFrames > 0` (a fresh wild, or one already inside a window). A wild with a STANDING
   offer (`RecruitOffered && CalmedWindowFrames == 0` — E-opened) is NOT castable: F must never
   silently downgrade E's standing offer. No castable target → REFUSED, nothing spent — the
   design's one money rule: a refusal NEVER spends (header; legs CALM_REFUSE_SHORT/REFUSE_STANDING).
4. Economy: `SkillState.TryCalmingSpeak(unlocked)` = `TrySpend(CalmingSpeakCost)`; short balance
   → nothing spent (`TrySpend` unchanged, the single spend path). Chosen rejection-without-spend,
   like a locked press — deliberately NOT Mend's pay-when-clamped: a Mend always lands on the
   caster; a cast at nothing lands on nobody.
5. On success, in THIS order: `OpenRecruitOffer(w)` (§3) → `w.CalmedWindowFrames =
   CalmingSpeakWindowFrames` (set AFTER open: `OpenRecruitOffer` retires any prior window, §3)
   → `_bus.EmitSkillUsed(new SkillId(PlayerMutations.CalmingSpeakId))` → `GD.Print` W3-style
   line. The existing `SfxRouter` subscription `SkillUsed → Fire("dna_spoken")`
   (`SfxRouter.cs:31`) plays the chime with ZERO new assets/signals; `SkillsPanel`/HUD refresh on
   the same bus signal. Re-cast inside an open window (step 3 allows it): refreshes the window to
   full and pays AGAIN — spend-per-use, the Invert Strike re-arm idiom (`SkillState.cs:75-76`).
- Join NEVER happens in this arm. No `_roster.TryAdd` call, no body move out of `_wild`. Joining
  stays exclusively `TryRecruitOfferedWild` under a `pay_wage` press (unchanged, §6 N1).

### 1.3 Window ↔ wage ↔ interact-E interaction (the whole point — pay-first preserved)

- WAGE STILL REQUIRED: the ONLY join path is the existing `payPressed` arm →
  `TryRecruitOfferedWild(ppos)` (`WorldDirector.Roster.cs:100,200-232`), which recruits the
  nearest OFFERED wild in `TalkRange` with the first wage, cap 3, "4th join REFUSED, the offer
  stands". The skill only ever opens the SAME `RecruitOffered` flag it already reads; a calmed
  creature you never pay for simply stays wild and expires (§2). Join cost via the skill = 12
  Manna of CAST + a pay press settling the first wage — the wage is Needs-based via
  `PayDueFollowers` (`:141-143`), the press debits NO Manna (DA P4-3); the E route costs the pay
  press alone. A reach/timing option, never a discount, never an instant-join.
- WINDOW EXPIRY: `CloseRecruitOffer` clears the flag; a later pay press finds no offer and
  `TryRecruitOfferedWild` returns false (press then falls through to the unchanged skip-arm /
  roster-pay semantics — `recruitAteThePress == false`, zero new branches).
- INTERACT-E STILL OPENS THE STANDING (time-unbounded) OFFER, unchanged behavior, regression
  must stay green (§4 REG-ROSTER). Interaction rule pinned: E (`TryRosterNpc`) on a wild whose
  offer is windowed UPGRADES it to standing — the E branch calls `OpenRecruitOffer`, which
  retires the window (§3). Once standing, no sweep can ever close it (sweep only touches
  `CalmedWindowFrames > 0`). So the skill never harms an E route, and there is exactly ONE
  offer flag with ONE decay authority (the window) and ONE unbounded opener (E).

### 1.4 Unlock (third rule in the frozen authority) + cost rows

`PlayerMutations` (the pure unlock authority, plan §G D2 Option B) gains, same idiom:
- `public const string CalmingSpeakId = "calming_speak";` — the wire id is ALREADY pinned by
  `PlayerMutationsTests.cs:85-89` (2-position profile → its `false` row KEEPS its outcome under
  rule 3; only the stale `// unknown id` comment changes, DA P3-b) and rides the existing
  string-`SkillUsed` carrier (no bus edit; W3fixwave ARCH-verdict §3 forward-fit).
- `public const int CalmingSpeakPositions = 3;` → `SkillUnlocks` gains a `CalmingSpeak` bool
  (`positions >= 3`). HONEST note: every in-game signature is a 6-mer
  (`DnaLanguage.SignatureForEntity`, `DnaLanguage.cs:52-57`), so all three mutations flip
  together at first consensus — a degeneracy INHERITED from the existing 1/2 ladder, not new, and
  the authority may not read anything but `Counters` (§G D2 frozen). The ladder is kept pure.
- Economy rows (all in the two existing tunable homes, engine-free file stays engine-free):
  `SkillState.CalmingSpeakCost = 12` (between Q=10 and R=15; utility bridge, not a power spike);
  `SkillState.TryCalmingSpeak(bool unlocked) => unlocked && TrySpend(CalmingSpeakCost)`-shaped
  (one payment, refuse spends zero).
- **Cooldown row: NONE.** Neither existing skill has one; Manna is the throttle (owner economy
  ruling D3), a cooldown would be a new mechanism beside the economy. Re-cast is bounded by
  balance, cap 100 (`PlayerController.MannaCap`, unchanged).

## 2. Frame-traced account (every number in FRAMES; const names named; 60 fps)

Root `_Process` order (facts at base, `WorldDirector.cs:247-263`): `TickRoster` (owns the
pay_wage arm + recruit) → attack → **interact** → … → `PollSkillActions` → `TickUi`. Cast frame = T.

| frame | what happens (all inside one deterministic `_Process` order) |
|---|---|
| T | press chain arm 3: spend 12 (`SkillState.CalmingSpeakCost`), `CalmedWindowFrames = CalmingSpeakWindowFrames` (600), offer opened, `SkillUsed("calming_speak")` → `dna_spoken` chime + panel refresh, same frame. A pay press on T CANNOT use this cast (TickRoster already ran) — pinned, not a bug. |
| T+1 … T+600 | each frame: sweep decrements BEFORE the press chain; value `600 − k` at frame `T+k`. `TickRoster` sees the offer TRUE all these frames → a pay_wage press anywhere in **T+1..T+600** (600 payable frames) recruits via the unchanged path. |
| T+600 | sweep (later this frame, after TickRoster) takes `1 → 0` → `CloseRecruitOffer` + EXPIRED print. Offer flag FALSE from end of T+600. |
| T+601 | first `TickRoster` seeing no offer: a pay press recruits NOTHING (`TryRecruitOfferedWild` false → skip-arm semantics unchanged). |

Window expiry is frame arithmetic on one `int--`: zero wallclock, zero `Timer`, zero
`Time.GetTicks*` (§6). The only other frame constant is the REFERENCE-only
`DialogueSystem.RewardLineFrames = 240` (§0, unused, named for the record). Proof budgets reuse
the existing named-constant idiom in the new proof partial.

## 3. Shared offer helpers — extraction and WHAT THEY REPLACE (replace, do not add beside)

Two private helpers in `WorldDirector.Roster.cs` (the offer's home partial), the single mutation
points for `RecruitOffered`:
- `OpenRecruitOffer(CompanionFollowBody w)`: prints the existing "recruit OFFERED to wild id …
  pay the first wage" line ONLY when flipping false→true, sets `RecruitOffered = true`, and
  RETIRES the window (`CalmedWindowFrames = 0`; window retire + its line only if it was > 0).
  REPLACES (call-site + helper, not a beside-add): the inline three-line block in `TryRosterNpc`
  (`WorldDirector.Roster.cs:280-285`, `if (!w.RecruitOffered) { w.RecruitOffered = true;
  GD.Print(...); }`) — E branch becomes a call. The skill arm calls it too (before re-setting
  the window, §1.2 step 5).
- `CloseRecruitOffer(CompanionFollowBody w)`: clears the flag AND zeroes `CalmedWindowFrames`
  (belt, DA-c1 P4-2: the join site clears only the flag today — a recruited follower must never
  carry a stale window; harmless on the expiry path, where the window is already 0); used by
  (a) the EXPIRY path (§1.1), (b) the load seam below, and (c) REPLACES the bare
  `wild.RecruitOffered = false;` in the recruit-success path (`WorldDirector.Roster.cs:222`) —
  that call site keeps its "RECRUITED" print; the helper's own "offer cleared" print is fine
  there (two lines, one truth).
- `ClearCalmWindows()` (Roster partial; the R9 mirror, §1.1): every `_wild` body with
  `CalmedWindowFrames > 0` → `CloseRecruitOffer(w)` (zero + close + print, through the helper).
  ONE caller: the END of `RestoreFollowers` (`WorldDirector.Roster.cs:373-408`), after its
  `ResetRosterDeltas()` (:406) — the `FollowersRestore` callback (`:69`) runs INSIDE `Load()` on
  EVERY successful load from BOTH entries (player `load_game` calls `Load()` directly,
  `WorldDirector.cs:259-260`; `LoadGame()` is proof-harness-only — DA-c2 F-1): R9's own
  inside-the-callback pattern.
The offer flag keeps exactly two writers through two helpers (the load seam clears THROUGH
`CloseRecruitOffer`); `TryRecruitOfferedWild` stays byte-identical apart from line (c).

## 4. Gates to ADD — each leg with its NAMED planted-bad (a check that never went RED is not a check)

Runtime proof `RuntimeIntegrationProof.CalmingSpeak.cs` (new partial, LA_GATE_MODE modes, the
`skill_use`/`skill_neg` idiom; main dispatcher + input-release list gain their lines):
- `calm_use` mode legs (green, live scene, real input via the existing `SpawnWildFollower` seam):
  - **CALM_CAST** — farm extraction(s) until `LiveUnlocks().CalmingSpeak`; press skill_3 near a
    wild → exactly one `SkillUsed` marker with id `calming_speak`, Manna drops EXACTLY 12,
    `RecruitOffered == true`, `CalmedWindowFrames == 600`.
    planted-bad **PB-PRESS-ORDER** (builder-side): move the spend BEFORE the target scan (rev-1
    order!) or drop the `EmitSkillUsed` — spend-before-scan REDS CALM_REFUSE_STANDING's zero-delta
    assert (standing target, adequate balance: spend fires, scan refuses; REFUSE_SHORT stays GREEN
    — short-balance `TrySpend` refuses atomically; DA-c2 F-2); marker-drop REDS CALM_CAST.
  - **CALM_PAY_IN_WINDOW** — pay_wage at frame T+2 → roster.Count +1, body leaves `_wild`,
    the existing "first wage paid via pay_wage" line prints. planted-bad **PB-SKILL-JOINS**:
    make the cast join directly (TryAdd in the skill arm) → REDS: CALM_CAST (post-cast asserts
    `RecruitOffered == true && CalmedWindowFrames == 600` on a WILD body — a direct join removes
    it from `_wild` and destroys both) + CALM_WINDOW_EXPIRES (no wild survives → no EXPIRED
    line; re-verified against the leg assertions, DA P2). REG-ROSTER is NOT claimed as detection
    — it drives E+pay with the skill never pressed and stays GREEN under this mutation (rev-1's
    claim was wrong). The PAIR detects; per-leg redness varies with the mutation's completeness
    (TryAdd-only leaves CALM_CAST green; CALM_WINDOW_EXPIRES still reds — DA-c2 F-3a).
  - **CALM_REFUSE_SHORT** — Manna < 12, press F: zero Manna delta, no offer, no SkillUsed marker.
    planted-bad **PB-SPEND-FIRST**: spend before the balance check → Manna delta ≠ 0 goes RED.
  - **CALM_REFUSE_STANDING** — interact E first, then press F on the same wild: refused, nothing
    spent, window stays 0. planted-bad **PB-DOWNGRADE**: let F overwrite a standing offer's window
    → RED ON THE CAST FRAME (Manna delta ≠ 0 where a refusal must spend 0 — DA P4-4; the T+601
    flag loss is downstream, not the first red).
  - **CALM_WINDOW_EXPIRES** — fresh wild, cast, wait 602 frames: flag false, a pay press joins
    NOBODY (roster.Count unchanged), EXPIRED line seen. planted-bad **PB-NEVER-DECAYS**: comment
    the decrement → flag still true at T+602 goes RED.
  - **CALM_E_STANDS** — cast, interact E (window retires), wait 700 frames: flag STILL true.
    planted-bad **PB-EXPIRY-CLEARS-E**: drop the window-retire in `OpenRecruitOffer` → flag false
    at T+700 goes RED (E's standing offer eaten by the skill's window).
  - **CALM_LOAD_CLEARED** (rev 2, DA-c1 P1-2) — wild A: press F (window 600); wild B: press E
    (standing offer); press save_game then load_game: Manna reads the SAVED value (the economy
    rollback is real — that is why the window must die) AND A shows `CalmedWindowFrames == 0 &&
    RecruitOffered == false` + the cleared print, AND B keeps its offer (true, window 0). planted-bad
    **PB-NO-LOAD-CLEAR**: drop the `ClearCalmWindows()` call at the end of `RestoreFollowers` →
    A's flag still true after a REAL load_game press goes RED (the R9 bug class).
- `calm_neg` mode (red leg, the `skill_neg` idiom — `SetSkillActionsEnabled(false)` on the
  instantiated root BEFORE `AddChild`, no_spawn): press skill_3 near a wild → zero Manna delta, no
  offer, no marker; prints **NEG_CALM** and exits non-zero if any effect leaked. planted-bad
  **PB-GATE-BYPASS**: handle skill_3 before the `_skillHooksEnabled` guard → marker appears → RED.
- Unit legs (pure, `tests/skill/`, ride `ci/skill_test.sh`'s two-pass + NAMED-F-ACT list — add
  names F7/F8/F9 or the gate fails on the census, not silently):
  **F7-CALM-COST-SPEND-ONCE** (12 exact, one payment), **F8-CALM-REFUSE-NO-SPEND** (locked and
  short-balance both spend zero), **F9-CALM-UNLOCK-POSITIONS** (≥3 positions unlocks; 2 does not;
  ObservedCount-independence rides the existing planted-bad trap). Test-row fix (DA P3-b, rev-1's
  "flip" was wrong): `:85-89`'s 2-position row KEEPS `false` — refresh its stale comment to the
  rule-3 reason, GAIN a 3-position row asserting TRUE, keep a genuinely-unknown id asserting FALSE
  (`_ => false` arm stays reachable and proven).
- **REG-ROSTER (must stay green, the anti-regression row)**: `ci/roster_test.sh` +
  `runtime_integration_test.sh` roster modes + `ci/skill_test.sh` + `ci/ui_test.sh` run UNCHANGED
  by any diff here (their scripts gain only the new mode/name rows). planted-bad
  **PB-SKILL-MANDATORY**: gate the E branch's offer on `CalmedWindowFrames > 0` (make the skill
  required for recruitment — the exact scope creep PLAN:118 forbids) → RosterIntegrationProof's
  OFFERED leg (`ci_proofs/RosterIntegrationProof.cs:183` — the OFFERED poll, DA P3-a) goes RED.

## 5. Files touched (per-file diff budgets in NEW lines; hygiene: the note under the table — DA-c2 F-3b)

| file | change | budget |
|---|---|---|
| `skeleton/src/skills/SkillState.cs` | `CalmingSpeakCost=12` const + `TryCalmingSpeak` + header line | +14 |
| `skeleton/src/dna/PlayerMutations.cs` | `CalmingSpeakId`, `CalmingSpeakPositions=3`, `SkillUnlocks.CalmingSpeak` field/ctor/Ids/Equals/GetHashCode/ToString, `IsUnlocked` arm | +20 |
| `skeleton/world/WorldDirector.Skills.cs` | third press arm + target scan + `TryCastCalmingSpeak` + window sweep + consts `CalmingSpeakWindowFrames=600`, `CalmingSpeakRange=9.0f` | +55 |
| `skeleton/world/WorldDirector.Roster.cs` | `OpenRecruitOffer`/`CloseRecruitOffer` REPLACING the inline block (:280-285) and the bare clear (:222), + `ClearCalmWindows()` + its call at the end of `RestoreFollowers` (load seam, §3) | +20 / −6 |
| `skeleton/world/CompanionFollowBody.cs` | `CalmedWindowFrames` property | +3 |
| `skeleton/src/ui/SkillsPanel.cs` | third row: field + MakeRow + the ONE Refresh row-assignment line (ARCH W5 "one-row" note: the row itself is one line; honest total budget) + `DisplayName` arm; Manna row y-shift 120→154 (fits the fixed 260 px rect the ui_test crop uses) | +6 |
| `skeleton/ci_proofs/RuntimeIntegrationProof.CalmingSpeak.cs` | NEW partial: `calm_use` + `calm_neg` stages, legs §4 | ≤230 |
| `skeleton/ci_proofs/RuntimeIntegrationProof.cs` | mode dispatch + `Input.ActionRelease("skill_3")` in the release list | +3 |
| `skeleton/tests/skill/*.cs` | F7–F9 per §4 (new 3-position TRUE row; 2-position FALSE row comment refreshed, no flip — DA P3-b; unknown-id FALSE kept) | +24 |
| `skeleton/tests/ui/SkillsPanelTest.cs` | third-row assert (locked + unlocked paint) | +4 |
| `skeleton/ci/skill_test.sh` | NAMED-F-ACT rows F7/F8/F9 | +3 |
| `skeleton/ci/runtime_integration_test.sh` | `calm_use`/`calm_neg` mode rows | +2 |
| ZERO-diff (explicitly): `project.godot` (skill_3 already bound), `EventBus.cs`, `SfxRouter.cs`, save schema, `DialogueSystem.cs`, palette, assets | — | 0 |

Hygiene, honest (DA-c1 P3-d): `WorldDirector.Roster.cs` is 422 lines at HEAD and its header
(:1-4) ALREADY carries the reason-per-MC 3895 note with "split is owed before any further
growth here". This design grows it to ~436 — still under the 600 project-file gate, and the
reason stays truthfully in its first lines — but the owed split is now DUE: name it a follow-up
card in the commit body; do not add a second split note. Other files stay far under ~250.

## 6. §DISPOSITION — one row per binding constraint (none waived)

| constraint | disposition |
|---|---|
| no instant-join / pay-first ruling preserved (PLAN:118) | Skill arm contains NO roster write; join stays `TryRecruitOfferedWild` (wage) only. Pinned by PB-SKILL-JOINS's REAL reds CALM_CAST + CALM_WINDOW_EXPIRES (DA-c1 P2 — REG-ROSTER does NOT claim this detection, it stays green under the mutation); REG-ROSTER + PB-SKILL-MANDATORY pin the separate E-route non-regression. Proof-of-absence row N1 in DONE.md. |
| zero new assets / signals / packs | Reuses `SkillUsed` (string-id carrier, 2c batch) + `SfxRouter.cs:31` `dna_spoken` subscription; no new `[Signal]`, no new asset, no new audio. |
| no new 3D art | None; feedback = chime + panel row + GD.Print (all existing idioms); palette zero-delta (2f colour rule held — row reuses the gauge colour). |
| frame-deterministic (no wallclock timers) | Window is `int--` per `_Process` tick; every timing named in frames with its const (§2). No Timer/ticks-of-wallclock anywhere in the diff. |
| roster/empathy contracts frozen (bus keys `<name>-<EntityId>`, mean-last, NotifyWagePay) | No BusKey/emit-order/wage-authority site touched; `TryRecruitOfferedWild` body unchanged except line (b)→helper. `CalmedWindowFrames` is view-body state, not a contract; roster-emits block untouched. |
| Manna economy idiom (Q=10/R=15, spend-per-use, refuse-spends-nothing, cap 100) | Cost 12 in the same const home, `TrySpend` primitive, cap untouched; refusal-without-spend chosen and justified (§1.2 steps 3-4, SPEND AFTER SCAN). No cooldown (economy is the throttle). |
| third skill = ONE-row SkillsPanel.Refresh edit per ARCH note | Refresh gains exactly one row-assignment line; the +6 budget covers the supporting field/build/DisplayName — honest line itemized (§5). |
| extract shared RecruitOffered(wild) helper per ARCH | `OpenRecruitOffer`/`CloseRecruitOffer` REPLACE the inline offer block :280-285 and the bare clear :222 by name (§3). |
| skill_3 bound-inert in project.godot | Activated as the third press arm (this card's purpose); input map ZERO-diff. |
| save schema v3 zero-delta AND no save-scum economics | Calm state is runtime-only AND dies at the load-restore seam (`ClearCalmWindows()`, R9 mirror, §1.1/§3), pinned by CALM_LOAD_CLEARED/PB-NO-LOAD-CLEAR. ZONE TRAVEL persists windows + decay — explicit stated behavior (§1.1), same class as E's persisted offer. |
| DLQ — LANDED (`4fece211`) | Zero Show calls in the diff; post-DLQ note in §0; no dialogue surface touched. |
| ARCH forward-fit (W3fixwave ARCH-verdict §3) | Press-chain slot, arbitrary skill-id carrier, no DealDamage re-wrap (non-damage skill), pay-first seam kept — all honored. |

## 7. Rejected alternatives (named so the DA can hit the right ghosts)

- **Instant-join on cast** — violates the pay-first ruling, no owner artifact. DEAD.
- **Skill required for ALL recruitment** (E no longer opens offers) — scope creep PLAN:118 forbids
  while "2g isn't specified"; breaks REG-ROSTER by design. DEAD (named as PB-SKILL-MANDATORY).
- **Cooldown const** — a second throttle beside the Manna economy = new mechanism; doctrine says
  no. DEAD.
- **Unlock via ObservedCount/boss-phase** — forbidden authority read (§G D2 frozen). DEAD.
- **Dialogue line on cast ("speak")** — touches the in-flight DLQ surface for zero mechanic; the
  dna_spoken chime already carries the voice. DEFERRED (not this card).

## 8. Docs follow-through for the close phase (ARCHITECT owns ARCH-verdict; not this file's edits)

`docs/ARCHITECTURE.md` rows to update when the build lands: §2 skills row (calming_speak), §3
composition-root inventory (`WorldDirector.Skills.cs` — the A1 P3 residual from W3fixwave, absorb
it here), §6 `skill_test.sh` row (F7-F9, calm modes). ARCH-verdict PASS requires these TRUE.

Build order note: PlayerMutations+SkillState first (pure, F7-F9 green), then helpers+property+
Roster call-sites + load seam (REG-ROSTER green immediately — refactor before behavior), then
Skills arm + sweep, then panel row, then proofs/ci last; all ci suites runnable at each step.
# JUDGED: da994bc3df9d109946484db0a8afeb9cd196090cd8b5a1851034477143af6571
# REV: 3
