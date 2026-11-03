# EVIDENCE — MC 11303 build (leg-J TAB-overlap guard) — run 20261101-1215-taboverlap

Commit: `cd3917d59ac461f881143eb79cd5f968654ab0f3`  Parent: `dbf2e08a5dfb9ea9bc1aba0d64f41b9d6d3aa2b3`
(read back `git rev-parse HEAD HEAD^` in `/home/claudecode/Games/la-work/11303`; parent == master == ls-remote-verified live master; branch `la/11303`, NO push, NO merge)

Engine law: pinned mono zip sha512 `1855960b27ee…` == `engine/local.sha512` mono row; binary prints
`4.7.2.stable.mono.official.ed1daf0bf` == `engine/PIN.txt` commit. Fresh-worktree `--import` rc=0.
Card cites `.audits/202610251815-d1gate/out/DA-verdict.md` — ABSENT on this host (gitignored, prior-host
loss tracked MC 11840); the card text (verbatim findings) was the source of truth, card unedited.

## Gate (DoD row 5 — full leg set A–L, committed bytes)

`cd /home/claudecode/Games/la-work/11303/skeleton && HOME=<out>/.tmp/home XDG_DATA_HOME=<out>/.tmp/home/.local/share GODOT=<wt>/engine/…mono… timeout 1200 ./ci/ui_test.sh`

- `logs/ui-test-AL-final.log` — ran on the EXACT committed bytes (stamp applied before this run):
  rc captured from the process (`echo $?` directly, never a pipe tail): **0**; `GATE FAIL` count 0;
  tail `UI_HUD_TEST: GATE PASS …` naming (J) with the restamped TAB text.
- `logs/ui-test-AL.log` + `logs/ui-test-rc.txt` — first full run (pre-stamp bytes, behaviour identical): rc 0.
- J markers inside the final log: `TAB_DIALOGUE_DISJOINT: ok dialogue=(454, 550), (676, 31) clear of the TAB panel (8, 440), (420, 260)` and verdict line `LA_GATE: PASS — HUD/dialogue/empathy/TAB bounding boxes DISJOINT (MC 11293 D1 + MC 11303 F-DA1)` (J1 + J2 legs).

## RED-capability (DoD row 2 — the guard seen failing, banked)

- `j-default-green.log` — headless default width: rc 0, all three DISJOINT checks green.
- `probe-resize.log` — resize probe: a lone `Root.Size` is reverted by the first DisplayServer sync (headless visible rect pinned (64,64); windowed reverts to the DS size); `DisplayServer.WindowSetSize` FIRST + `Root.Size` after sticks (windowed `vis=(0,0),(800,648)` at f1/f31/f61/f91). This is why the planted RED is a WINDOWED-face proof.
- CONTROL `control-narrow-windowed.log` + `frames/green/control-narrow-800.png` — shipped code, planted width 800, `composed — viewport=(800, 648)` (real width!), band fallback-pinned to 440, PASS marker + RESULT=PASS. Proves shipped clamp holds the pair disjoint at the planted width.
- RED `plant-red-narrow-windowed.log` + `frames/red/plant-narrow-800.png` + `plant-red-narrow-xvfbrun.log` — throwaway copy (`.tmp/red-plant/`, PanelViewport fallback dropped = the drift F-DA1 names) at planted width 800:
  `LA_GATE: FAIL — TAB_DIALOGUE_OVERLAP dialogue=(102, 550), (676, 31) under TAB panel=(8, 440), (420, 260)` then `LA_GATE: FAIL — overlap legs red (0 dialogue + 0 empathy + 1 TAB, above)`. The RED frame PNG SHOWS the dialogue text painted underneath the Skills panel (clipped — "the player loses text").
- RED exit-code face `plant-red-headless.log` — headless planted pair (degenerate 64px width): rc **1** with named FAIL lines — proves the assert leg carries the gate exit code.
- `control-narrow-800-headless.log` — headless narrow control (shipped): rc 0 (fallback; headless viewport is 64px regardless — disclosed by the probe).

## Coupling + stamp (DoD rows 3–4)

- `git show cd3917d -- skeleton/src/ui/` : comment-only diff, three sites; literal `428` comment removed —
  DialogueSystem/EmpathyPanel cite `SkillsPanel.RegionX+RegionWidth`, SkillsPanel Region docblock names its
  consumers + `TAB_DIALOGUE_DISJOINT` as the drift net. Zero behaviour lines changed (comments only).
- ui_test.sh SIZE header: `523->538 l by wc AT this head`, +15 comment-only on the existing leg, ceiling re-justified in the header line (git numstat +20/−5 = net +15; the 5 modified lines are the marker/echo restamps).
- Battery: `git diff HEAD^ HEAD -- skeleton/ci/runtime_integration_test.sh | wc -l` == **0** (re-check on the commit: run this command in the worktree).

## Save law

- SAVEGATE `/tmp/la-savegate.lock`: foreign holder `holder-test-mc11312-verify.txt` seen at intake → WAITED (armed waiter acquired `12:40:12Z`, `holder=code-11303`); identity-checked release `13:11:13Z`, RELEASED-VERIFIED-GONE.
- Every engine run under isolated `HOME`/`XDG_DATA_HOME` under `out/.tmp/home/`.
- Owner save `~/.local/share/godot/app_userdata/Last Animal/savegame.json` sha256 BEFORE == AFTER ==
  `fdd1cc43a5124fee3d12b007f5874f0d9a2e969ab93d3c602e1ca96f9697cd4e` (`.tmp/save-sha-before.txt`, `.tmp/save-sha-after.txt`) — BYTE-IDENTICAL, never written.

## Re-derive (verifier)

```
cd /home/claudecode/Games/la-work/11303
git rev-parse HEAD HEAD^                                    # expect cd3917d / dbf2e08
git diff HEAD^ HEAD -- skeleton/ci/runtime_integration_test.sh | wc -l   # expect 0
wc -l skeleton/ci/ui_test.sh                                # expect 538 (header stamp cites it)
# full gate (savegate + isolated HOME law): ~6 min
cd skeleton && HOME=… XDG_DATA_HOME=… GODOT=<mono> ./ci/ui_test.sh; echo $?   # expect 0
grep "FAIL — TAB_DIALOGUE_OVERLAP" ../last-animal/.audits/20261101-1215-taboverlap/out/plant-red-narrow-windowed.log   # expect the line
```

VERIFY_EXIT=0

## Rebase — cd3917d → abc1af1 (parent brief 2026-11-01), re-runs on the rebased bytes

New: HEAD `abc1af1290508b422820cde2a8f4cfa22c1f9313`, parent `4d07bf26ef07fafafd27b77e877220486904427b` (= bryn1/master; ANCESTRY_OK via `git merge-base --is-ancestor bryn1/master la/11303`). Conflict: LEDGER.md both-appended tail → both rows kept; ui_test.sh auto-merged (MC 11312 build_solutions.sh caller routing + this leg's +15 comment lines — zero invented code). SIZE stamp re-derived: wc 538 at the new head (11312's swap zero line-delta) — clause true unedited.

- `logs/ui-test-AL-rebased.log` — FULL ui_test.sh A–L ON REBASED BYTES, rc from the process: 0; `GATE FAIL` count 0; TAB marker present; build step through the new wrapper (`BUILD_STEP: signature=exit-0-clean attempt=1`).
- `logs/plant-red-narrow-windowed-rebased.log` + `frames/red/plant-narrow-800-rebased.png` — planted RED reproduced byte-identically at windowed 800 (`FAIL — TAB_DIALOGUE_OVERLAP dialogue=(102,550),(676,31) under TAB panel=(8,440),(420,260)`; composed viewport genuinely (800,648)).
- `logs/plant-red-headless-rebased.log` — planted headless rc 1, named FAILs.
- Battery vs NEW parent: `git diff HEAD^ HEAD -- skeleton/ci/runtime_integration_test.sh | wc -l` == **0** (measured at abc1af1).
- Save law: BEFORE==AFTER sha256 `fdd1cc43a5124fee3d12b007f5874f0d9a2e969ab93d3c602e1ca96f9697cd4e` (`.tmp/save-sha-before-rebase.txt` / `.tmp/save-sha-after-rebase.txt`), savegate acquired FREE 13:25:28Z / released 13:31:39Z, isolated HOME/XDG for every engine run.
- Disclosure: two interim shas existed mid-rebase (wrong-author commit corrected by amend; LEDGER-number amend) — the only sha that exists on the branch now is abc1af1; the gate files are byte-identical between the tested tree and abc1af1.

VERIFY_EXIT=0
