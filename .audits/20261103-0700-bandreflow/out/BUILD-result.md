# MC 11891 — BUILD result (dialogue band reflows to fit any window)

Branch `la/11891`, base master `0e2e98a` (MC 11303 build). Owner ruling, verbatim:
**`B`** (2026-11-03) — reflow the dialogue band to fit any window; NO minimum
window size. This card implements that ruling; it does not invent a window floor.

## What was wrong (the reviewer's F-P1-1, in one breath)

Below the 1152 px play width the band's `UiTheme.PanelViewport` constant-span
fallback pinned it at x 440..1144 regardless of the real window: at 800 px the
window edge cut the right half off every dialogue line — and leg J certified
THAT clipped screen PASS (the old leg measured headless, where a `--script`
run's visible rect sits at the nominal 64×64 whatever the planted width — F-P2-1).

## The fix (one live path, no second mechanism)

- `src/ui/DialogueSystem.cs`: panel + label are built once, then `LayoutBand()`
  derives the band from the LIVE visible rect (`GetViewport().GetVisibleRect()`)
  and re-runs on `Viewport.SizeChanged`. At the shipped default it lands the
  OLD rect byte-identically (measured: band (440, 540, 704, 100) at 1152×648,
  J2 markers in both red and green logs). Below the width where the band no
  longer fits RIGHT of the TAB column it reflows full-width ABOVE the panel
  (at 800 px: band (8, 332, 704, 100), right edge 712 ≤ 800, clear of the panel
  at y 440..700 — disjointness holds at BOTH widths, J logs). The constant-span
  fallback is STOOD DOWN for the band; `UiTheme.PanelViewport` now serves
  EmpathyPanel only (docs rewritten there). `MinBandWidth = 96` is the P1
  zero-size paintable floor — NOT a minimum window size, said so at the site.
  NAMED CEILING: under ~720 px a wrapped line can spill below the band
  surface — still inside the window, still readable; named in the header.
- The empathy panel carries the SAME narrow-width clip class through its own
  `PanelViewport` use — OUT of this card by construction (the card names the
  dialogue band); it rides a follow-up card, named here and in UiTheme.cs docs.
- `tests/ui/HudDialogueOverlapTest.cs`: new named assert `DIALOGUE_IN_VIEW` —
  band + text label fully inside the live visible rect — FAIL names the rect,
  the window rect and the cause. Verdict/FAIL lines extended; PASS marker
  keeps its old prefix so standing greps still match.
- Gate: leg J rebuilt in `ci/ui_test.sh` (the assertion's home per DoD row 2).
  The headless J1 face is RETIRED (it asserted a 64×64 layout that never
  paints — F-P2-1; the retirement is NAMED in the header, not silently lost).
  Each width runs the SAME script twice through the SAME helper path: a frame
  run capturing mid-hold (frame beside the very log its markers came from) and
  an rc run letting the game reach its own verdict-quit — `GAME_RC=` is then
  the process's own OS exit code, never a pipe tail. J2 = shipped default,
  J3 = planted `LA_OVERLAP_FORCE_W=800`.
  MEASURED, banked in the leg's comment: a FAIL returning true from `_Process`
  QUITS via MainLoop semantics (the old red frames only ever existed because
  the helper happened to capture before the verdict); a FAIL now HOLDS the
  opened state and quits at t=20 s with rc 1, so the RED frame is deterministic.
- Split (F-P3-2 + the 538-l ceiling row): the A–L file is now a 273-l entry +
  `ui_pixel_battery.sh` (C/F/H, 177 l) + `ui_tracker_battery.sh` (I/K/L,
  191 l) — legs, markers and pixel bars moved UNCHANGED; the entry runs both
  batteries and keeps the gate name + the tail-law echoes; stamps re-derived
  by wc AT this head.
- De-stale (row 4): the `x 8..428` comment and the hard-coded `420x200+8+440`
  PANEL_CROP are gone — the crop is SED-DERIVED from `SkillsPanel.RegionX/Y/W`
  with a loud `PANEL_MEASURE_H ≤ RegionHeight` check and a loud
  vignette-crop-clearance check; the quest crop keeps its literal with the
  measured window→root offset named as the coupling (pixel bars ARE the check).
  `tools/launch_linux_smoke.sh` stale cite `ci/ui_test.sh:235` → named pointer.

## RED/GREEN pair (DoD row 2 — a guard never seen failing is not a guard)

- RED: throwaway copy (`.tmp/plant-pre`, tree + NEW test + NEW gate,
  `src/ui/DialogueSystem.cs` reverted to 0e2e98a bytes — the fallback live;
  other src deltas vs master are comment-only) run through the FULL gate:
  J2 GREEN (proves shipped-default behaviour unchanged byte-for-byte) and
  **J3 RED by name**: `FAIL — DIALOGUE_IN_VIEW band=(440, 540), (704, 100)
  outside window vis=(0, 0), (800, 648)` (label same face) — exactly the
  reviewer's clipped rects; gate exit rc 1 (`logs/gate-RED-prefix-800.log`,
  `.rc`); `frames/red/la-overlap-narrow-800.png` SHOWS the clipped line cut
  at the window edge. An rc-run probe of the same plant printed `GAME_RC=1`
  from the process (`.tmp/probe/fix800.log`, host-local).
- GREEN: full A–L gate on these bytes — rc from the process, J2 + J3 markers
  + `GAME_RC=0` ×2, `logs/gate-GREEN-final.log` + `.rc`;
  `frames/green/la-overlap-default.png` + `la-overlap-narrow-800.png` (band
  inside the 800 px window, panel clear).

## DoD rows, evidence level each

1. band from live visible rect, fallback stood down, no second live path — TESTED (markers above) + code read. No floor needed: B is implementable without one (the 96 px degenerate guard is a paintable floor, not a window minimum) → not STOP/BLOCKED.
2. `DIALOGUE_IN_VIEW` in `ci/ui_test.sh`, RED at 800 / GREEN at default, rc from process — TESTED, banked (names above).
3. leg J surface set reused (TAB carries 11303), no parallel mechanism/capture path — VERIFIED (one helper path, same test file extended in place; `grep graphical-test-helper` leg).
4. `x 8..428` + `420x200+8+440` de-staled — VERIFIED (`git grep '420x200' = 0`, derived crop + loud checks in `ui_pixel_battery.sh`).
5. split landed, stamps re-derived (273/177/191 l by wc AT this head) — VERIFIED (`wc` rows in the file headers; no >400 file left).
6. nothing regresses: full A–L green on these bytes rc from process (EVIDENCE.md `VERIFY_EXIT`), battery diff 0 lines (measured `git diff 0e2e98a -- skeleton/ci/runtime_integration_test.sh | wc -l` = 0), disjointness at default AND 800 (both J runs' markers) — TESTED.

Out of scope, named untouched: empathy-panel narrow-width reflow (follow-up
card — same clip class, its own ruling territory), the 64-px headless-rect
engine behaviour itself (fleet-wide property, only worked around honestly by
retiring the headless leg), `xvfb-run` capture idioms elsewhere.

Evidence: `EVIDENCE.md` (+ `verify-11891.sh`, re-runnable, `LA_VERIFY_FULL=1`
for the whole gate) — ends `VERIFY_EXIT=<n>` from the gate process.
