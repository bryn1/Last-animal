# MC 11891 — EVIDENCE (dialogue band reflow, owner ruling `B`)

Owner ruling, verbatim: **`B`** (2026-11-03) — reflow the dialogue band to
fit any window; NO minimum window size. Branch `la/11891` off master
`0e2e98a`; engine `4.7.2.stable.mono.official.ed1daf0bf` == `engine/PIN.txt`
(the untracked engine symlink in the worktree points at the pinned
`/home/claudecode/Games/.tools/godot/...` binary, never staged).
Every gate engine run: isolated `HOME`/`XDG` (`out/.tmp/verify-home`),
SAVEGATE `/tmp/la-savegate.lock` held by `code (11891) ... acquired
2026-11-03T05:49:41Z` across every save-touching leg, identity-held to the
end. The owner's real `user://savegame.json` sha256
`fdd1cc43a5124fee3d12b007f5874f0d9a2e969ab93d3c602e1ca96f9697cd4e`
byte-identical before and after every run (re-measured after the final gate —
above file never written; isolation honoured).

## RED pair — the new assertion seen failing (pre-fix bytes)

Throwaway copy `.tmp/plant-pre/` = this tree with `src/ui/DialogueSystem.cs`
reverted to `0e2e98a` bytes (constant-span fallback live = the pre-fix band;
the other src deltas vs master are comment-only), NEW test + NEW gate kept,
rebuilt through `ci/build_solutions.sh` (exit-0-clean). Full gate
(`.tmp/plant-pre/...` PROJ, `FRAME_DIR=out/frames/red`):

- `logs/gate-RED-prefix-800-summary.log` (committed named-marker extract) —
  full transcript `logs/gate-RED-prefix-800.log` (280 KB raw app lines) stays
  HOST-LOCAL per the durable-set ruling; process rc **1** (`logs/gate-RED-prefix-800.rc`).
  Run J2 (shipped default, pre-fix) GREEN with band `(440, 540, 704, 100)` —
  the byte-identical shipped rect (no-default-behaviour-change control);
  run J3 (`LA_OVERLAP_FORCE_W=800`) RED **by name**:
  `LA_GATE: FAIL — DIALOGUE_IN_VIEW band=(440, 540), (704, 100) outside window vis=(0, 0), (800, 648) (window edge cuts the band)` (+ the label, right edge 1130 > 800) — exactly the reviewer's F-P1-1 clipped rects — gate exit named
  `UI_HUD_TEST: GATE FAIL: run J3: DIALOGUE_IN_VIEW red — ...`.
- `frames/red/la-overlap-narrow-800.png` — the LIVE failing frame: dialogue
  line "You answer in its own tongue. It" cut mid-word at the window edge
  (the old gate's false-green face, now caught).
  `frames/red/la-overlap-default.png` — default control, uncut.
- rc-from-process face at the planted width: a same-command rc probe printed
  `GAME_RC=1` (game's own verdict-quit exit code; host log
  `.tmp/probe/fix800.log` — the gate itself died named at J3 before its own
  rc run, which is the point of the named FAIL).

## GREEN — full gate A–L on THESE bytes

`logs/gate-GREEN-final-summary.log` (committed named-marker extract; full
transcript `logs/gate-GREEN-final.log`, 278 KB raw, HOST-LOCAL per the same
ruling) — full `ci/ui_test.sh` (entry + both batteries)
on the worktree bytes; process rc **0** (`logs/gate-GREEN-final.rc`), tail
`UI_HUD_TEST: GATE PASS` naming A–L incl. J's
"DIALOGUE_IN_VIEW at the shipped default AND the planted 800 px width":
- J2 default: `DIALOGUE_IN_VIEW: ok band=(440, 540), (704, 100)` inside
  `vis=(0, 0), (1152, 648)` — shipped default byte-identical — `GAME_RC=0`.
- J3 planted 800: `DIALOGUE_IN_VIEW: ok band=(8, 332), (704, 100)` inside
  `vis=(0, 0), (800, 648)` — reflowed full-width ABOVE the TAB panel
  (panel y 440..700 clear), right edge 712 ≤ 800 — `GAME_RC=0`.
- `frames/green/la-overlap-default.png`, `frames/green/la-overlap-narrow-800.png`
  — the frames the markers were read beside.
- Disjointness (dialogue/empathy/TAB/HUD) green at BOTH widths in the same
  logs; legs C/F/H pixel bars + I/K/L tracker legs green (moved files run).

## Rows (card DoD), measured

- Battery `runtime_integration_test.sh`: `git diff 0e2e98a -- …| wc -l` = **0**.
- Stamps by `wc` AT this head: entry 273 l, pixel battery 177 l, tracker
  battery 191 l — all ≤400; old 538-l ceiling split landed (F-P3-2), the
  >400 re-justification chain retired, named in the entry header.
- `git grep '420x200'` / `'x 8..428'` = 0; PANEL_CROP sed-derived from
  `SkillsPanel.Region*` + loud ≤-RegionHeight and vignette-clearance checks;
  `tools/launch_linux_smoke.sh` line-cite de-staled to a named pointer.
- No minimum window size exists anywhere (grep: none added); `MinBandWidth`
  is a degenerate-window paintable floor, named NOT a minimum at the site.
- Follow-up named (not this card): EmpathyPanel carries the same narrow-width
  clip class through its `PanelViewport` use — its own card/ruling territory.

Re-run: `bash verify-11891.sh` (structural; `LA_VERIFY_FULL=1` re-runs the
whole gate; every rc from its own process, never a pipe tail).

VERIFY_EXIT=0
