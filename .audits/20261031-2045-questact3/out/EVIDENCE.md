# CLOSE EVIDENCE — MC 11311 (act-three quest tracker: LiveQuestLine union leg + frame-banked RED/GREEN)

Task: fix the player-visible stale-quest-tracker bug — `LiveQuestLine` unioned ONLY act-one +
act-two rows, so act-three rows never painted. Game `/home/claudecode/Games/last-animal`,
Godot 4.7.2 **mono** pinned. BUILD = commit `f31b4f474b08a4c9f205f346860f2ce439bd2b79` on branch
`la/11311`, parent `eeeacc9ddf3e8e9ac2de6b67e107b5091a9cc35c` (live master; `base..commit` diff
7 files +217/−13 — non-empty). NO push (card law). Author `code (11311) <code@agent-town.local>`
via `-c` only (no `git config`).

## Provenance
- Base: worktree `/home/claudecode/Games/la-work/11311` (branch `la/11311` off live master eeeacc9).
- Engine PIN honoured: mono zip sha512 `1855960b27ee…` == `engine/local.sha512`; binary
  `--version` = `4.7.2.stable.mono.official.ed1daf0bf` == `engine/PIN.txt` commit ed1daf0bf
  (a non-mono editor would build zero C# and still print green — this is the pinned mono binary).
  Fresh-worktree `--import` law PAID (logs/import.log rc=0) before any graphical run.
- Prior evidence read: `.audits/202610251405-d3/EVIDENCE.md` §6 exists no more (it lived in the
  retired `/home/svarkor/last-animal` tree); its §6/F1 content survives VERBATIM on the MC 11295
  board card (close evidence, "F1 … LiveQuestLine never paints act-3 rows → ALREADY CARDED as
  MC 11311") and in LEDGER.md:1307-1309 ("LiveQuestLine (Ui.cs) unions ONLY act1+act2 —
  act-three rows never paint"). Both re-read this session.

## The reading used (card wording vs mechanism — named per DoD)
The card says a player at act three "sees a stale act-two tracker line". That exact state is
UNREACHABLE: act three opens only at act two's `IsArcComplete` (event edge) or via
`ActChainSync` Adopt/FreshOpen over a completed/rewound act-two log — no reachable state has an
act-two row Active while act three lives. What the pre-fix product paints at the act-three-open
state is the exhausted fallthrough **"Quest: none"** — the act-three row never paints (the
11295-named gap). The RED capture banks THAT face (FAIL marker `line=[Quest: none]` at
`act3Active=q_h_arrival`, `live=True` — a read-side union gap, HUD == director, not staleness).
The gap and the capture pin are the same under both readings.

## Fix (DoD 1 — the ONE existing idiom, union leg extended, no second mechanism)
`skeleton/world/WorldDirector.Ui.cs` `LiveQuestLine()`: the existing act2-wins-over-act1 ternary
union is extended with the act-three leg FIRST. Precedence named in the in-file comment:
**the newest opened act wins — act three over act two over act one** (an act opens only when the
previous arc completes, so the deepest log with a still-Active row IS the current beat). Doc +
header comments updated from "both act logs" to the three-log union. Stays a READ-side union —
`_quests` keeps its shipped act-one identity (the MC 10138 constraint kept verbatim). Zero new
fields/signals/save fields; ZERO battery diff (`git diff` of `ci/runtime_integration_test.sh` empty).

## RED-before / GREEN-after — captures, not frames-of-prose (DoD 2)
Proof is the repo's capture idiom extended: `tests/ui/HudQuestAct3CaptureTest.cs` (leg-I parent
twin; act opens through the REAL save path — Story3 phase-1 diverge + SaveGame/LoadGame, the
shipped FreshOpen arm) + `ci/ui_test.sh` leg **(L) HUD_QUESTLINE_ACT3** (gate now A–L).
- **RED (banked `out/frames/red/`)**: new test + gate leg run at the UNFIXED union, engine built
  from that tree (logs/build-unfixed.log rc=0). Marker
  `LA_QUESTLINE_ACT3: FAIL — line=[Quest: none] act3Active=q_h_arrival live=True`;
  frames `la-ql3-act3.png` + `quest-row-act3-crop.png` SHOW "Quest: none" painted while the
  act-three row is Active. Baseline control run PASSED (act-one boot face) — the leg is not
  vacuously red: it discriminates. The gate has thus been SEEN to go RED on the unfixed product
  (negative-testing law).
- Engine REBUILT between the halves (logs/build-fixed.log rc=0), fix applied.
- **GREEN (banked `out/frames/green/`)**: `LA_QUESTLINE_ACT3: PASS — line=[Quest: The Hollow —
  Reach hollow] act3Active=q_h_arrival live=True`; crop legible "Quest: The Hollow — Reach
  hollow". Pixel bars: row stddev 0.261 ≥ 0.02, colors 239 ≥ 4; base-vs-act diff stddev 0.283 ≥
  0.01, mean 0.134 ≥ 0.003 (the row actually repainted).

## Full gates (DoD 3)
- **FULL ui_test.sh A–L** at the fixed head: rc=**0**, `GATE FAIL` count 0, tail
  `UI_HUD_TEST: GATE PASS … MC 11311 act-three quest tracker mid-arc — the act-three row paints
  once the act opens (L)` — 1-hit (tail law). Log `out/logs/ui-test-AL.log`.
- **FULL battery** `ci/runtime_integration_test.sh` at the fixed head: rc=**0**, `GATE FAIL` 0,
  tail `RUNTIME_INTEGRATION_TEST: GATE PASS — authoritative runtime path verified …` (full text
  in `out/logs/battery-full.log`, 4173 l); `FALL_RECOVERED_CENSUS 2/2 OK`; `MODE_SETS_CHECK:
  GREEN` (RuntimeIntegrationProof=41 — NO new mode this card); `PAIRING OK — 36 == 36`; run_mode
  count `grep -cE '^run_mode '` = **49 = 38 + 11** at this head (battery untouched, count
  unchanged).
- Re-runnable gate: `out/verify-11311.sh` → `MC11311-VERIFY: PASS`, and its own process exit
  captured UN-PIPED (`logs/verify-run.log`, tail `VERIFY_EXIT=0`).

## SAVEGATE + owner profile (repo law)
Foreign check `/tmp/la-savegate.lock` FREE at intake → `mkdir` mutex acquired,
`holder-code-11311.txt` + long-lived sleep lease, held across every engine run, identity-checked
release at card end (below). EVERY save-touching run under isolated HOME/XDG
(`/tmp/la11311-home`): the owner's REAL `user://savegame.json` sha256
`fdd1cc43a5124fee3d12b007f5874f0d9a2e969ab93d3c602e1ca96f9697cd4e` byte-identical before/after
(`out/real-save-before.sha256` + release re-hash) — never written, so no restore owed.

## File hygiene (DoD / owner ruling 2026-09-23)
`WorldDirector.Ui.cs` 213→225 l (<400, no header owed); `ui_test.sh` 471→523 l — >400 SIZE header
line 2 RE-STAMPED with the MC 11311 clause (existing reason carried); new capture test 133 l
(test ceiling 600); LEDGER row + ARCHITECTURE §6 runs row + length note restamped in-line.
No TODOs; no second union mechanism.

## Out-of-scope named, untouched
MC 11295 D3 ordering fix, HUD/dialogue layering (11293), death handling (11294), battery legs,
save format, `/home/claudecode/Games/babayaga`.

## Limitations
- The capture opens act three on the shipped restore path (FreshOpen), not a from-scratch live
  boss-kill playthrough into act three — the live event-edge open is MC 11295's covered ordering;
  both opens end in the same act-three-open state that this leg pins.
- Judge note on MC 11295 named frames unreadable there — here every frame is a banked file in
  THIS run dir under `.audits/`, and the semantic markers ride the same logs.

VERIFY_EXIT=0
