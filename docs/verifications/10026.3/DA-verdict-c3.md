# DA-verdict-c3.md — Calming Speak DESIGN rev 3 (MC 10031) — devils-advocate gate, cycle 3 (final)

Target: `.audits/20261003-CSK/DESIGN.md` rev 3 (300 lines; 2-line footer; body-exclusion verified
this cycle: `head -n -2 DESIGN.md | sha256sum` = `c871dd15…` == orchestrator-validated
`da994bc3` manifest entry ✓). Delta-only per cycle-2 disposition (c1 `DA-verdict.md` 12aef143,
c2 `DA-verdict-c2.md` b44daf94 — both frozen history). STATIC ONLY; live tree re-read this cycle.

## F-1 delta — VERIFIED FIXED (FALSIFIABLE)
- §3 pins ONE caller: END of `RestoreFollowers` (`WorldDirector.Roster.cs:373-408`), after its
  `ResetRosterDeltas()` (:406). Live tail re-read: `ResetRosterDeltas();` then the
  `load restored N=` print, then close — the pinned placement is inside that tail ✓.
- Both load entries fire: `_Process` `load_game` key calls `_saveLoad.Load()` directly
  (`WorldDirector.cs:259-260`, re-read live) and `LoadGame()` also routes through `Load()`
  (`:487`); inside `SaveLoadController.Load()` the `FollowersRestore` callback fires on EVERY
  successful load (`SaveLoadController.cs:189-190`; seam always wired at `Roster.cs:69`,
  cycle-1 read) → `RestoreFollowers` runs → clear runs — player path covered, `LoadGame()`
  bypass (c2 F-1) structurally gone. §3's own wording now states both entries ✓.
- E-standing bodies untouched: the clear iterates only `CalmedWindowFrames > 0` (§1.1/§3);
  standing offers (window 0) are never matched — by seam AND sweep ✓ (matches live code shapes).
- §5: the `WorldDirector.cs` +1 row is GONE from the table (full-table read confirms); the
  Roster.cs row carries `ClearCalmWindows()` + its call, +20/−6 → ~436, still gate-legal with
  the honest owed-split paragraph. F-3c (brace cost) moot as designed ✓.
- PB-NO-LOAD-CLEAR now drops the call AT THE NEW SITE: correct code → real `load_game` press
  greenlights all three CALM_LOAD_CLEARED asserts (Manna == saved; A flag 0/window 0/cleared
  print; B keeps standing offer) — green IS reachable; drop-the-call → A's flag true after the
  real press → RED. Not red-by-construction anymore ✓. Falsifiable.

## F-2 delta — VERIFIED FIXED (attribution correct, no cross-breakage)
- §4 now: spend-before-scan REDS **CALM_REFUSE_STANDING's zero-delta assert** (standing target,
  adequate balance: spend fires, scan refuses), with the correct disclosure that
  **CALM_REFUSE_SHORT STAYS GREEN** under that mutation (short-balance `TrySpend` refuses
  atomically — live `SkillState.cs:125-131` re-checked); marker-drop stays on CALM_CAST.
- Re-traced once more: ordering mutation leaves REFUSE_SHORT green (atomic refuse, no spend,
  no offer, no marker) and reds REFUSE_STANDING (−12 delta at the cast frame) — exactly as the
  row claims; PB-SPEND-FIRST is a DIFFERENT mutation (balance bypass) and its RED on
  REFUSE_SHORT is untouched by the re-attribution ✓.
- F-3a pair clause present ("The PAIR detects; per-leg redness varies…" with the TryAdd-only
  example); F-3b fixed — `~250` appears exactly ONCE in the file, in the honest paragraph with
  correct scope ("Other files stay far under ~250"); the §5 header claim is gone.

## Nothing else moved (delta scope respected)
Spot-greps confirm §2 frame table, §6 pay-first row, helper writer census ("two writers through
two helpers", load seam clearing THROUGH `CloseRecruitOffer`), `RosterIntegrationProof.cs:183`
citation, and the F7-F9/:85-89 test rows are byte-equivalent to the cycle-2-verified state.

## UNCHECKED (carried, honest)
No gates/dotnet run (STATIC ONLY) — RED/GREEN claims are code-read assertions (INFERRED), to be
proven by the build's planted-bad pairs; `ModelPlayerDna` growth semantics untouched
(inherited+flagged degeneracy, design's own); proof-mode choreography is builder-scope.

## Disposition
Both c2 findings (the P2 seam mis-pin and the P3 detector mis-attribution) are verified fixed
against live code this cycle; F-3a/b present; no new delta defects found within the checked
scope, and the pay-first / frame-account / helper-ownership invariants all HOLD across all three
cycles. Cycle 3/3 closes clean — pin rev 3 and proceed to build (builder: the owed Roster.cs
split goes in the commit body as a follow-up card per §5).
# JUDGED: 1001722e426c1582f9f8e86c6d0aa155a9a8e1c8a9a0e4ec34571c6a87dd45dd
# VERDICT: SHIP
