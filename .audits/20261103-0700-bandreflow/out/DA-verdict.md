# DA-verdict — MC 11891 dialogue-band reflow (commit `43c35b6`, parent `6a1ca1d`, branch `la/11891`)

Adversarial review (devils-advocate seat), 2026-11-03. Method: full diff read;
parent→head mechanical leg-diffs; committed evidence re-read (logs + frames);
`verify-11891.sh` structural legs re-run INDEPENDENTLY this session; geometry
derived by hand from `LayoutBand()`; and FIVE live engine runs of the real test
on the graded bytes (headless, 100/360/480 px planted widths, frame+rc runs
under the shipped helper path), under the SAVEGATE mkdir mutex with isolated
HOME/XDG — owner `savegame.json` re-verified byte-identical after
(`fdd1cc43a512…`), lock identity-released. Scratch: `.tmp/da-11891/`
(`probe.log`, `headless.out`, `forced-{100,360,480}{,.rc}.log`, `frames/`).
No file outside scratch was written; nothing committed or pushed.

## Verdict summary

**SHIP** — merge `la/11891` — with three follow-up rows that MUST be filed as
cards at merge time (rows A–C under “Merge recommendation”), the reflow’s
degenerate-width ceiling narrowed, and the durable-set ignore pattern cut to
the summary class. No P0/P1 found: the fix is real, the new assertion has a
proven red face, the shipped default is byte-identical, the gate split lost no
assertion, and the owner ruling is followed for the band down to ~112 px
measured. I tried to re-break each claimed cure and could not at any width the
card claims coverage.

## The seven attacks, in order

### 1. Frame-vs-verdict, one layer up — CLOSED (not the race relocated), one weak auxiliary assertion left

Pressed as asked: the frame run and the rc run ARE two process runs, so the
commit’s own framing (“a separate rc run to the own-verdict quit”) invites
exactly the error class. What closes it is what the commit undersells: the
gate’s verdict markers — `LA_GATE: PASS — … DISJOINT`, `DIALOGUE_IN_VIEW: ok`,
and the negative `FAIL — DIALOGUE_IN_VIEW` — are grepped **from the frame run’s
own app log** (`ui_test.sh` `overlap_run`: the helper writes `$CAPDIR/$png.png`
and `$CAPDIR/app-$png.png.log` from the SAME invocation, and every marker bar
reads `$log`). So the assertion the gate believes is computed in-process from
the painted rects of the very process whose window was screenshotted; the
helper’s `RESULT=PASS path=…` line names the PNG it measured. The historical
race — capture of a state already torn down — is closed by the measured
FAIL-holds fix (red frames are now produced deterministically, not raced).

What the second run contributes is only the OS-exit-code property, which
cannot wash a failed assert green: a false gate-green needs frame markers AND
`GAME_RC=0`, and the frame markers dominate. Cross-run divergence can only
turn a leg falsely RED (rc run missing its t=20 quit inside the 45 s wait),
the acceptable direction.

Residual weakness, named (P3-2): the rc run asserts ONLY `GAME_RC=0` — its own
log lines are echoed with `|| true` (`ui_test.sh:251`), and `GAME_RC=0` is
also satisfiable by a compose-failure exit (`return true` → SceneTree quit rc 0
with no verdict marker). One line closes it: require the rc log to carry the
PASS marker too. Single-run design that closes it completely (my
recommendation for the next hand on this leg): have the test save the frame
ITSELF (`GetViewport().GetTexture().GetImage().SavePng(…)`) immediately before
its own verdict-quit — one process emits frame + markers + rc; the helper
stays where it is genuinely needed (the pixel-diff legs C/F/H).

### 2. Did curing the clip re-break `TAB_DIALOGUE_DISJOINT`? — NO at every width; YES for two OTHER shapes the ceiling comment half-names

Measured, not reasoned: at the shipped default the band lands the old
byte-identical rect (x 440..1144 vs panel right edge 428 — disjoint, RED-plant
log and GREEN log print the same numbers — a perfect no-behaviour-change
control). At 800 px: band (8,332,704,100), panel y 440..700, 8 px clear; the
banked GREEN frame shows it (I looked: text complete, band above panel, no
overlap — and no book, see P2-1). The RED frame at 800 shows the line cut
mid-word at the window edge — the F-P1-1 face, seen failing.

Algebra shows the disjointness CANNOT re-break at ANY width: right-of-column
mode starts at `x ≥ RegionX+RegionWidth+8 = 436 > 428`; full-width mode has
`y ≤ min(332, H−108) ≤ 332`, band bottom ≤ 432 < panel top 440. My live runs
confirm the reflow is green well beyond the gate’s pair: **360 px and 480 px
both PASS all four checks with `GAME_RC=0`** — the gate plants 800 only because
at 800 the band is still the shipped 704 wide, the first planted width that
exercises the reflow at all.

What curing the clip DOES re-expose is the wrapped-label tail: at 100 px my run
printed `FAIL — TAB_DIALOGUE_OVERLAP dialogue=(22,342),(68,167) under TAB
panel=(8,440)…` — a wrapped label taller than the fixed `BoxHeight=100` spills
below the band surface (the named ceiling says “still in-window” — true) AND
lands ON the TAB panel (not named) — violating the MC 11303 invariant, at a
width the gate never runs. See P3-1.

### 3. `MinBandWidth=96` — not the banned floor at the band; but it re-creates the edge clip below 112 px, and its stated justification does not need 96

The banned thing (a minimum WINDOW size) is verifiably absent: grep over the
full diff and the whole repo finds no window clamp, `project.godot` has no
`[display]` min-size (no `[display]` section at all — the 1152×648 default is
Godot’s built-in), `LA_OVERLAP_FORCE_W` is a test plant, and the game boots and
plays at any size (my runs planted 100 px and the window obeyed).

But the floor’s own comment claims it exists because “a zero-sized control is
the MC 11293 P1, never again” — and that rationale does not justify 96. At a
100 px window, `vis.X − 2·BoxEdge = 84` is still a perfectly paintable band;
zero-size only looms below 16 px. The constant floor instead makes the band
OVERSPRING the window: live run at `LA_OVERLAP_FORCE_W=100` printed
`FAIL — DIALOGUE_IN_VIEW band=(8,332),(96,100) outside window vis=(0,0),(100,648)`
— the card’s own new law, violated by the shipped code, at widths 16..111
(headless 64×64 reproduces it too: band (8,8,96,100) over a 64×64). So “the
band reflows to fit ANY window” holds for every window ≥ 112 px (VERIFIED at
360/480/800/default; measured-broken at 100/64) — the ruling honoured in
substance for the playable range, with a one-integer regression at the
degenerate tail: `Mathf.Min(96, …)` → `Mathf.Min(MinBandWidth, (int)vis.X −
2*BoxEdge)` keeps the band paintable at every window and in-view by
construction. Ruling honoured in wording, ~94 % in substance; P3-1, fix rides
the ceiling-narrowing row.

### 4. The retired headless J1 — the fiction claim is TRUE, and the retirement was FORCED by the fix, not convenient

Independently verified this session (not accepted): running the CURRENT test
headless prints `composed — viewport=(64, 64)` whatever the real window — the
nominal-64 fiction is alive exactly as MC 11303 F-P2-1 measured. And with the
fallback stood down for the band, the headless face CANNOT be green by
construction: my run exits **rc=1** naming `DIALOGUE_IN_VIEW band=(8,8),(96,100)
outside window vis=(0,0),(64,64)` plus six `HUD_DIALOGUE_OVERLAP` FAILs. The
old J1 was green ONLY through the fallback pinning the shipped geometry —
keeping it meant shipping a leg that asserts a layout that can no longer
compose, in an environment that never paints it. Was it load-bearing? Its
assert set (all four checks + the surfaces-armed wall) now runs windowed at
two real geometries plus the new IN_VIEW assert; the numbers it pinned at the
fallback are byte-identical to J2’s real-1152 measurements. Net-net coverage
UP; the only loss is an X-free fast face for these asserts (the leg now needs
Xvfb + helper timing — a flake surface, and the J1 headless face remains
documented as runnable-but-meaningless, “retired loud” in header +
ARCHITECTURE §6 + BUILD-result). This was retiring a check because it measured
a fiction. P4 note only: no remaining gate leg exercises the overlap test
without a display.

### 5. The gate split — legs survived unchanged, none downgraded, stamps true; +1 doc-sync surface named

- **Mechanical leg-diff (my session):** extracted every assert/threshold line
  of I/K/L from the 538-l parent vs `ui_tracker_battery.sh` → byte-identical;
  C/F/H vs `ui_pixel_battery.sh` → identical except one typo fix in a fail
  MESSAGE (`<$4` → `<4`). Thresholds (`0.02/4`, `0.01/0.003`, `0.15/0.02`,
  `--wait 3/6`) all carried. No leg became a grep.
- **Leg J actually strengthened:** parent J2 grepped only the loose prefix
  `LA_GATE: PASS`; new `overlap_run` greps the full DISJOINT verdict string,
  the IN_VIEW ok marker, a negative FAIL-by-name bar, the helper `RESULT=PASS`
  pixel bar, and `GAME_RC=0` — plus the 800 px face.
- **Stamps:** `wc -l` at head = 273/177/191 = the header claims; `verify-11891.sh`
  re-derives stamps vs wc on every run (a real anti-stale mechanism, ran green
  for me: `VERIFY_EXIT=0` structural legs, incl. engine pin
  `4.7.2.stable.mono.official.ed1daf0bf` and battery diff 0 lines).
- **Second-place-to-remember:** YES, +1 — a new leg now touches battery file +
  entry header A–L map + entry tail `GATE PASS` echo + ARCHITECTURE §6. That is
  a relocated mechanism with one added doc-sync point, not a second mechanism
  (one gate name, one build, entry runs the batteries). Related P4: running a
  battery STANDALONE silently measures whatever assembly is on disk — the
  “runs after the entry build” assumption is a comment, not a check.

### 6. Evidence packaging — the naming convention IS the only thing standing between this repo and the bulk-repeat; narrow the pattern

Mechanically confirmed (`git check-ignore -v`): `gate-GREEN-final.log` matches
the durable-set NEGATION at `.gitignore:29` (`gate-*.log`) — it and its
280 KB twin are **untracked, not ignored** (exactly what the builder reported;
`git status` still shows `??` in the worktree). The corrected commit itself is
clean (ls-files: 4 PNGs + 2 SUMMARY logs + docs), but the durable set admits
ANY file named `gate-*.log` of ANY size — the gatewardens counter-case is
structurally true: one future `git add -A` after a chatty or crash-looping gate
re-commits the bulk, and the first cut of THIS card is the existence proof that
the failure mode fires. The pattern needs narrowing, named:
`!.audits/*/out/logs/gate-*.log` → `!.audits/*/out/logs/gate-*-summary.log`,
plus `!.audits/*/out/logs/gate-*.rc` (the committed EVIDENCE.md/BUILD-result.md
cite `logs/gate-GREEN-final.rc` — the process rc, the single strongest durable
fact — and that file is IGNORED (`.audits/**`, `.gitignore:22`): committed docs
currently point at uncommittable bytes). Fix belongs to the MC 11840 durable-set
home (workspace-convention §Durable evidence — “edit there, never here”). P2-2.

### 7. Was the ruling actually followed / scope riders

Ruling quoted verbatim in commit, LEDGER row, and both docs; traced through the
diff: no minimum window size anywhere (attack 3). The two odd files are NOT
scope riding along — both are comment-only (verified hunk-by-hunk):
`WorldDirector.cs` re-truths a composition comment that would otherwise still
cite `UiTheme.PanelViewport` as the DIALOGUE idiom after the stand-down (a
leaved-behind stale claim is the CONTRADICTED class the fleet law bans), and
`launch_linux_smoke.sh` removed the line-number cite `ci/ui_test.sh:235` that
THIS split invalidated, replaced line-free. Belongs in the change. P4 nits:
LEDGER claims `+2/-2` for the smoke file, `git show --numstat` says `+3/-1`;
sibling stale line-cites survive (`GameLoop.cs:16` → `smoke.sh:60`,
`launch_linux_smoke.sh:15` → `smoke.sh:74`) — the sweep de-staled only the cite
this card broke.

## Findings ranked

- **P2-1 — the Empathy-Book is fully INVISIBLE (not “clipped”) below ~1136 px; the gate’s own GREEN frame proves it.** `EmpathyPanel.BuildIfNeeded` anchors `x = vp.X−308` through the retained `PanelViewport` fallback → at the planted 800 px the opened book sits at x 844..1144, i.e. ENTIRELY off the right edge: press B, see nothing. `frames/green/la-overlap-narrow-800.png` (the card’s ruling-B proof frame) visibly lacks the book that `frames/green/la-overlap-default.png` shows — the leg REQUIRES the book open at measure time and the measurement passed because off-screen rects intersect nothing: `HUD_EMPATHY_DISJOINT` is vacuous at narrow widths, and the card one `foreach` iteration away from its own empathy rects extended IN_VIEW to them. Out of the card’s boundary (the card names the band; the follow-up is named in three files) — a defensible scope ruling, but: the shipped GREEN evidence frame depicts a window that does NOT “fit any window” in substance; EVIDENCE/BUILD describe it as a “clip” when at 800 px it is invisibility; and the leg’s vacuous-empathy face is the same false-green shape this card just cured for the band. Merge must FILE the follow-up card (row A).
- **P2-2 — durable-set negation admits arbitrary-size raw transcripts (`gate-*.log`); naming is the only guard; `gate-*.rc` is ignored while committed docs cite it.** Narrow to `gate-*-summary.log` + negate `gate-*.rc`. Pattern in attack 6. Mechanism is pre-existing (MC 11840 on master), not introduced by this commit; this card was the first to trip it — fix at the convention home (row B).
- **P3-1 — degenerate widths 16..111 px: the band’s 96 floor overshoots the window (card’s own law violated — VERIFIED at 100 px: `DIALOGUE_IN_VIEW` FAIL) and wrapped labels reach onto the TAB panel (VERIFIED at 100 px: `TAB_DIALOGUE_OVERLAP` FAIL).** The ceiling comment names “spill below the band surface, still in-window” but not the panel collision nor the floor’s edge overshoot, and the floor’s zero-size rationale only needs the band > 0 px, not 96: `Mathf.Min(MinBandWidth, (int)vis.X − 2*BoxEdge)` keeps it paintable at every window. Reaching those windows is WM-clamped in practice but the ruling said ANY. Ceiling-narrow + clamp fix ride together (row C).
- **P3-2 — rc run asserts only `GAME_RC=0`; its marker lines are echoed not asserted, and rc-0 is also what a compose-failure exit prints.** Not a false-green path (frame-run marker bars dominate — verdict identity is single-run, attack 1), but the auxiliary property is weaker than described. One-line fix or the in-process-save in attack 1.
- **P3-3 — the height axis is untested law-space.** `LA_OVERLAP_FORCE_W` forces height 648 always; `LayoutBand` full-width mode sits `y = max(8, min(332, H−108))` — for H < ~284 the band drops onto the HUD gauge block (x starts at 8, HUD spans x ≤ ~427, rows end ~y 176): the MC 11270 P1 class re-enters at short windows; `HUD_DIALOGUE_OVERLAP` would name it but the gate never runs H ≠ 648, and the ceiling comment covers width only. INFERRED from the arithmetic + HUD row rects (the two live bars are exact; not run — no height plant exists). Fold into row C.
- **P4 — batch.** split adds one leg-doc coordination point (attack 5); batteries standalone-run an unbuilt tree silently; stale line-cites `smoke.sh:60/74` survive elsewhere; LEDGER `+2/-2` vs `+3/-1` on the smoke file; `DialogueSystem.cs` carries raw `font_size 22` outside leg G’s three-file list (pre-existing); leg J has no X-free face anymore (attack 4).

## Claim classification (truth-audit)

| Claim | Level |
|---|---|
| Band derives from live rect; re-layout on SizeChanged | VERIFIED (code read + 5 live runs incl. 360/480 beyond the gate) |
| Shipped default byte-identical (440,540,704,100) | VERIFIED (same numbers in RED-plant and GREEN logs + code derivation) |
| `DIALOGUE_IN_VIEW` red-capable, named | VERIFIED (banked plant log re-read + my independent FAIL at 100/64) |
| Frame depicts the verdict state | VERIFIED (markers grepped from the frame run’s own log; helper `path=` pins the PNG) |
| Disjointness green at default AND 800 | VERIFIED (logs + frames seen) |
| “band fits ANY window” | TESTED down to 112 px (360/480/800/1152 green); **CONTRADICTED at ≤111 px** (P3-1, own assert red) |
| “no minimum window size anywhere” | VERIFIED (grep diff + repo + project config) |
| headless face = 64×64 fiction; retirement sound | VERIFIED (live headless run this session, rc=1 + (64,64)) |
| Split moved legs unchanged; stamps true | VERIFIED (mechanical leg-diffs; wc + verify script) |
| Full A–L green rc 0 on these bytes | TESTED (banked GREEN summary + `.rc` seen; not re-run end-to-end — verify structural legs re-ran green for me) |
| Battery zero-diff vs 0e2e98a | VERIFIED (verify script leg, ran green) |
| Owner save byte-identical | VERIFIED (re-measured this session, incl. before/after my five runs) |
| Evidence = summaries only, raw untracked-not-ignored | VERIFIED (`git check-ignore -v`, `git ls-files`) |
| Empathy follow-up “same clip class” | OVERSTATED-UNDERSTATED: at 800 px it is full off-screen invisibility (P2-1) |

## Merge recommendation

**SHIP `la/11891`** (merge is the parent’s call per card law). File at merge —
before the close, so they are cards, not sentences:

- **Row A (was P2-1):** `EmpathyPanel` any-window reflow card — extend the
  IN_VIEW law to the Empathy labels (killing the vacuous-off-screen face),
  same mechanism as the band; cite this run’s narrow GREEN frame as its
  evidence-of-invisibility.
- **Row B (was P2-2):** one-line `.gitignore` + workspace-convention durable-set
  narrowing: `gate-*-summary.log` admitted, raw `gate-*.log` ignored, `gate-*.rc`
  admitted (fixes the dangling rc references).
- **Row C (was P3-1/3-3):** degenerate-window row — clamp the band floor to the
  live width, name both spill consequences in the ceiling comment, and add a
  short-window plant (force HEIGHT) so the HUD-disjointness law is run on the
  axis it currently never measures. P3-2’s one-liner (PASS marker asserted in
  the rc-run log) fits the same card.

# JUDGED: 6caf6f09a665748cb6bd348d4d96ebad40663bf6fc43d2c4625ed0dd070420e8
# VERDICT: SHIP
