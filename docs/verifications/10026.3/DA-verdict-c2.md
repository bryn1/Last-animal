# DA-verdict-c2.md — Calming Speak DESIGN rev 2 (MC 10031) — devils-advocate gate, cycle 2

Target: `.audits/20261003-CSK/DESIGN.md` rev 2 (300 lines; footer = last TWO lines `# JUDGED:
d92a89e7…` + `# REV: 2`; body validated by BODY-EXCLUSION ONLY: `head -n -2 DESIGN.md | sha256sum`
= `6bc1989e…` == `out/DESIGN.md` entry in `.tmp/judged/d92a89e7*.json` ✓ — footer never
self-reproduces, by design, not flagged). Tree HEAD `7079a00` (DLQ close, docs-only over
`4fece211`). STATIC ONLY (read/grep/git show — runtime claims are code-reads, INFERRED).
Delta-gate over cycle-1 FIX (`DA-verdict.md`, pin 12aef143).

## Cycle-1 findings — verified fixed / not

- **P1-1 (spend-after-scan) — FIXED.** §1.2 steps now 1 gate → 2 unlock → 3 TARGET SCAN →
  4 economy → 5 open/window/emit; the money rule is stated once ("a refusal NEVER spends").
  Coherent with `TrySpend`'s atomic refuse (live `SkillState.cs:125-131` — the design's
  re-cited number is correct; my cycle-1 :147-153 was stale, designer right). PB-SPEND-FIRST
  (balance-bypass) genuinely reds CALM_REFUSE_SHORT. One attribution error remains — F-1 below.
- **P1-2 (load clears windows) — PARTIALLY FIXED: the seam is named right, the pin is wrong.**
  §1.1's honest load rule + threat model are correct; but §3 pins ONE caller `LoadGame()`
  (`WorldDirector.cs:485-488`) — F-1 (P2) shows the player's own load key bypasses it.
- **P2 (PB-SKILL-JOINS phantom reds) — FIXED** at the named-reds level: §4/§6 now claim only
  CALM_CAST + CALM_WINDOW_EXPIRES and explicitly disclose REG-ROSTER stays GREEN under the
  mutation. Spot-check of the reds: full join mutation (move out of `_wild`) → both die as
  claimed; a `TryAdd`-only mutation keeps CALM_CAST green but CALM_WINDOW_EXPIRES still reds
  (pre-cast roster.Count baseline +1) — detection holds via the pair; one wording nit, F-3a.
- **P3-a — FIXED:** §4 now cites `ci_proofs/RosterIntegrationProof.cs:183` — matches the live
  OFFERED poll (`if (wild.RecruitOffered) { Input.ActionRelease("interact"); _sub = 3; }`).
- **P3-b — FIXED correctly:** the naive "flip" is gone; `:85-89`'s 2-position row KEEPS
  `false` under rule 3 (positions 2 < 3 — outcome-preserving, comment refreshed), a NEW
  3-position row asserts TRUE, and a genuinely-unknown id keeps the `_ => false` arm proven.
- **P3-d — FIXED:** §5 hygiene paragraph is honest (422 → ~436, reason-in-header stays true,
  owed split → follow-up card in the commit body). Budget +20/−6 matches ~436 ✓.
- **P4-1 zone row — HONEST:** `TravelToNextZone` (`WorldDirector.cs:350-360`) despawns only
  `_zoneEnemies`; "windows + decay persist, same class as E's persisted offer, no leg" matches
  the code exactly. P4-2 — FIXED: `CloseRecruitOffer` now zeroes the window (belt) at join.
  P4-3 — FIXED: §1.3 states the pay press debits NO Manna, wage is Needs-based. P4-4 —
  PB-DOWNGRADE now correctly reds ON THE CAST FRAME ✓; PB-PRESS-ORDER re-attributed to the
  WRONG leg — F-2.

## Findings (rev 2)

### F-1 (P2) — `ClearCalmWindows()` is pinned to a caller the PLAYER never hits
§3: "ONE caller: `LoadGame()` after `ResetRosterDeltas()` (`WorldDirector.cs:485-488`)"; §5
budgets it as the ONE line there. But the in-game `load_game` key calls `_saveLoad.Load()`
DIRECTLY (`WorldDirector.cs:259-260`) — `LoadGame()` is called ONLY from proof harnesses
(grep: `RuntimeIntegrationProof.Quests.cs:434,483`, `Save.cs:94`, `ZoneBossProof.cs:277`,
`RosterIntegrationProof.Follow.cs:141`). Transcribed literally, the player path save→cast→
load_game keeps rolling Manna back over a live, still-ticking window — the exact §1.1 threat
the fix exists to kill. §1.1's own words ("the load-restore seam … R9 mirror") contradict §3's
pin: R9 (`WorldDirector.Story.cs:69`) lives INSIDE the `QuestStatesRestore` callback so it runs
on BOTH entries, through `Load()`; `LoadGame()` is not a seam. (CALM_LOAD_CLEARED presses the
real `load_game` key, so the leg would go RED at first build — the trap is the contract, not
the gate: an implementer forbidden to repair faces a §3-pin-vs-leg contradiction, and a
docs-close following §3 would record the clear at a site it does not run.)
Fix (one clause): pin the call at the END of `RestoreFollowers` (the Followers restore
callback that already runs inside `Load()` on every successful load from both entries —
`WorldDirector.Roster.cs:373-408`, its tail already carries `ResetRosterDeltas()` at :406),
drop the `WorldDirector.cs` +1 row; §3's ONE-caller sentence then names that site.

### F-2 (P3) — PB-PRESS-ORDER's RED is attributed to a leg that cannot see it
§4: spend-before-scan mutation "goes RED on CALM_REFUSE_SHORT's no-spend/no-offer assert under
short balance". Under short balance `TrySpend` atomically refuses (`SkillState.cs:127-128`) —
spend-before-scan never spends when the purse is short, so REFUSE_SHORT stays GREEN under this
mutation. The true detector is **CALM_REFUSE_STANDING** (adequate balance, standing target:
spend fires, then the scan refuses → zero-delta assert RED at the cast frame). Detection
exists; attribution names the wrong leg for the SECOND cycle running — a builder "proving" the
PB by watching REFUSE_SHORT sees a false-green gate.
Fix (one clause): re-attribute PB-PRESS-ORDER's ordering-variant RED to CALM_REFUSE_STANDING's
zero-delta assert (marker-drop variant stays on CALM_CAST's marker assert; PB-SPEND-FIRST
stays where it is).

### F-3 (P4, notes)
- F-3a §4 PB-SKILL-JOINS says BOTH named legs go RED; a `TryAdd`-only mutation leaves CALM_CAST
  green (body stays `_wild`, flag/window intact) — detection still lands via CALM_WINDOW_EXPIRES.
  One clause: "the PAIR detects; per-leg redness varies with the mutation's completeness".
- F-3b §5 header sentence still reads "every file stays far under ~250" — contradicted by its own
  hygiene paragraph four rows below; align or delete the header clause.
- F-3c `LoadGame()` currently has a brace-less single-statement `if` (`WorldDirector.cs:487-488`);
  if any call is added there it costs braces (+2 lines, not the +1 budgeted) — moot if F-1's
  RestoreFollowers placement is taken.

## New-risk sweep of the fix (the delta itself)
- **Writer census RE-RUN against rev 2**: `RecruitOffered` writers remain exactly the two helper
  bodies — open at `Roster.cs:281-283` (→ `OpenRecruitOffer` call) + skill-arm call; close at
  `Roster.cs:222` + expiry sweep + load clear, all through `CloseRecruitOffer`. §3's "writer
  count unchanged" claim TRUE as designed. Join remains exclusively `TryRecruitOfferedWild`
  (`Roster.cs:200-228`, sole caller the `payPressed` arm `:100`) — pay-first intact in the delta;
  no new `_roster.TryAdd` or `_wild`-exit site anywhere in rev 2.
- Load-seam prints (one per windowed body) and join's "offer cleared" line: proofs assert on
  substring PRESENCE — extra lines are harmless; the only absence-style checks are the NEG modes
  for markers (`NEG_CALM` counts emits/offers, not prints) — green-safe.
- CALM_LOAD_CLEARED falsifiability: three-way asserts (Manna == saved; A flag FALSE + window 0 +
  cleared print; B keeps standing offer) + PB-NO-LOAD-CLEAR drop-the-call — genuinely falsifiable
  ONCE F-1's placement lands (under the §3 pin it is red-by-construction, which is a broken leg,
  not a proof).
- §2 frame table unchanged from the cycle-1-verified account (order `TickRoster :247 → … →
  PollSkillActions :262 → TickUi :263`; T+600/T+601 boundary sound).
- `SkillState.cs:75-76` re-arm-idiom cite: points into the TryInvertStrike doc block; substance
  holds (live file read this cycle).

## UNCHECKED (honest partial)
- No gates/dotnet run (STATIC ONLY) — every RED/GREEN statement is an assertion against code,
  INFERRED, to be proven by the build's planted-bad pairs.
- Did not re-trace `EcosystemAdaptation.ModelPlayerDna` counter growth (degeneracy claim is the
  design's, inherited+flagged, cycle-1 disposition unchanged).
- Proof-stage choreography INSIDE the new calm_use mode (leg ordering, Manna<12 setup after
  prior spends, per-phase budgets) is builder-scope; only per-leg budgets' precedent
  (`SkillFrameBudget = 6000`, DLQ 3×240+8 waits) was re-confirmed.

## Disposition
FIX — one P2 (F-1: re-pin `ClearCalmWindows()` to the in-`Load()` restore site — R9's own
pattern — and align §3/§5) plus one P3 (F-2: correct the PB-PRESS-ORDER detector, second-cycle
repeat) are cheap, no-re-plan doc edits; everything else the designer implemented is verified
against live code this cycle. Mechanic shape remains sound: pay-first, helper ownership, frame
account and economy all HOLD. No owner escalation. After F-1/F-2, cycle 3 (if reached) should
verify only the two edited rows' deltas.
# JUDGED: b44daf946e13641d4b860e3e31dd91613c4549e5b43a07be78d8421ea20d5349
# VERDICT: FIX
