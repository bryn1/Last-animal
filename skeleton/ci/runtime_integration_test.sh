#!/usr/bin/env bash
# SIZE reason (file hygiene law, past the 600 test ceiling at 667 l by wc AT the MC 10420 c2 head): the battery IS the leg matrix, and the MC 10026.31(b) battery-wide census (FR_CENSUS, tail) needs ONE log spanning every leg — splitting legs across files would orphan that count; MC 10516/S38 grew it +62 to 729 l by wc AT this head (section X: the two FLOOR_TRUST legs + per-zone greps + census/header restamps); MC 10519/S27 grew it +48 to 777 l by wc AT this head (section Q2: the ZONE_VISUAL_MOUNT leg + per-zone census greps + hollow.png evidence check); MC 10420 grew it +35 (section W + the calm_use zero-FALL_RECOVERED in-leg grep + census restamps); MC 10420 c2 +12 (the FOLLOWER_EDGEHOVER + PLAYER_UNCLAMPED marker checks + the section-W comment restamp); MC 11018/S45 +25 to 802 l by wc AT this head (section R: the CAMERA_RIG_ISO default-off run_mode leg + the FLAGGED fixed-rig leg — LA_RIG_EXPECT on the env, the cmdline user flag after the -- separator, seven-marker grep loop + the tail GATE PASS echo clause); MC 11019/S48 STEP-0 +66 to 868 l by wc AT this head (section S: the FIRST WINDOWED legs — the GL-Compat capability probe green spawn + the planted-bad red pair, xvfb-run, renderer pin + verdict greps + capture bytes + in-leg zero-FALL_RECOVERED); MC 11019/S48 FIX-ROUND-1 +18 to 886 l by wc AT this head (section S gates: the DECAL_WINDOW_ALIVE + MULTILIGHT additivity + Forward+-clause greps, capture count 7->8, and the RED leg widened to the four-plant pair — every plant named, every OK-collision checked ANCHORED to the verdict line (the armed line NAMES the verdicts; a whole-log substring false-collided — proven RED-capable on the round-1 battery)). MC 11099/S49 +14 to 900 l by wc AT this head (section A2: the LA_MAX_FPS forced-low-framerate follow probe re-running the positive chain in-process — the S49-COMP red/green pair, no cold-HOME dependence). MC 11088/S48 BATCH-4 FIX-ROUND-1 +44 to 944 l by wc AT this head (section S: the SKY_STRIP render-probe green leg + its planted alpha-0 red pair — the Batch-4 card DoD leg DA F1 closed: xvfb-run, FXSKY_RENDERER pin + RUN-derived verdict greps + the OK-collision check ANCHORED '^LA_GATE: SKY_STRIP_RENDER_OK' (the armed line names the verdict — the FX-pair collision lesson) + capture bytes + in-leg zero-FALL_RECOVERED; the ride-alongs: GATE PASS echo names all four plants + additivity + DECAL_WINDOW + SKY_STRIP, and the header/§6 count stamps restamp); MC 11129 FIX-ROUND-1 (DA P1-GATE) +19 to 963 l by wc AT this head (section R: the PLAYTEST_RIG_MARKER_MISSING PAIR — the flagged spawn's ISO boot line asserted from the SAME capture the seven-marker loop reads + an unflagged re-spawn under the LOGP capture-and-grep idiom asserting CLASSIC; run_mode count UNCHANGED 45 = 35 + 10 — no mode, no leg added; the tail GATE PASS echo names the gate; planted print-removal RED pair with the engine REBUILT between plant and run, transcripts .audits/202610221501-s45play/out/). MC 11189 DEFAULT FLIP +5 to 968 l by wc AT this head (section R flipped IN PLACE: the unflagged run_mode leg now pins the ISO default — marker CAMERA_RIG_DEFAULT_ISO, the proof's default expectation rides the flipped boot; the seven-marker LA_RIG_EXPECT=fixed capture loses its `-- --iso-cam` argv because ISO IS the default now; the LA_PLAYTEST_RIG pair is flipped — ISO asserted on the UNFLAGGED capture, CLASSIC on a `-- --classic-cam` + LA_RIG_EXPECT=follow re-spawn; the tail GATE PASS echo names the flipped pair + the owner ruling verbatim "Iso,"; count and mode vocabulary UNCHANGED). MC 11152 FIX +10 to 978 l by wc AT the FIX head (ONE top-of-script change: PROJ absolutized at the top with the HERE cd-and-pwd idiom — the capture spawn's env path and the shell-side -s checks share ONE absolute path, closing the CWD-mismatch that made the section-Q2 ZONE_VISUAL_MOUNT leg RED under a relative PROJ arg while the proof itself printed LA_GATE: PASS (diag spawns .audits/202610222340-s45playfixwalls/out/diag-mount*.log); the section-S LA_FXCAP_PATH/LA_SKYCAP_PATH captures ride the same fix; NO mode, NO leg added — count UNCHANGED 45 = 35 + 10); MC 11152 MICRO-FIX (the MC 11113 builder's named handback) +10 to 988 l by wc AT this head — COMMENT-ONLY: the section-S green SKY_STRIP prose restamped to state what the §S greps ASSERT at this head (a HARNESS-SIDE render PROBE: the leg's own unshaded native-4:1 quad BESIDE the composed scene; greps = FXSKY_RENDERER pin + SKY_STRIP_RENDER_OK + per-point/summary measured margins + >=2 FXSKY_CAPTURE lines + both capture PNGs non-empty + in-leg zero FALL_RECOVERED) + one honest clause that the option-(b) REAL product mount (canyon.tscn SkyStrip on the ONE WorldMount route, procedural sky KEPT) landed on branch la/11113 with the proof leg upgraded IN PLACE at the SAME battery greps and is UNMERGED at this head; ZERO executable-line change — the assert/before=9ce2699 executable lines byte-identical; MC 11037 +39 to 1027 l by wc AT this head (section Q2: the GROUND_TEX_WIRING ground-plate leg — the four wired ground_texture_path entries parsed from their OWN zone tscns, on-disk plate presence, the proof's ZONE_MOUNT_GROUNDTEX material read-back compared PATH-EXACT with painted=1, per-wired-path AND capture-wide zero-engine-'Resource file not found' greps — all riding the existing Q2 ZONE_VISUAL_MOUNT mount capture; NO mode, NO new spawn, count UNCHANGED 45 = 35 + 10; the tail GATE PASS echo names the gate; the proof-side print is +18 l in RuntimeIntegrationProof.ZoneMount.cs, 450 -> 468 l by wc, still under the 600 ceiling so no SIZE header there); MC 11243 HARDENING +38 to 1065 l by wc AT this head (F3: pre-spawn rm of EVERY capture target — the Q2 hollow plate + all four windowed FX/SKY capture dirs — defense in depth BESIDE the proof-side SavePng==Ok gating, not instead of it, sky-wall append 1 class; F5+ZONE4: the ZONE4 marker capture NAMES its result — ZONE4_CAPTURE_RC=<rc> + wall-clock echo, budget kills (124/137/143) and the proof's own 'budget exhausted' FAIL go RED LOUDLY as BUDGET-DIED, any other non-zero rc RED BY NAME with its LA_GATE: FAIL lines (the append 3 absorption close: non-zero rc is never swallowed even with every marker printed), marker greps assert CONSISTENCY with the rc and the twelve green greps survive verbatim (append 4 misattribution close); F5: the three MC 10120-era mid-body cumulative GATE PASS echoes renamed SECTION-OK so a bare GATE PASS grep is tail-specific, the tail echo owns the gate name and now names this round's gates; run_mode count UNCHANGED 45 = 35 + 10 by anchored grep at this head; NO mode, NO new spawn; the 17 sibling PROJ= idiom pointers (F4, comment-only) and the sky-probe dotnet-build route ride their own files — see the LEDGER row); MC 11320 +17 to 1099 l by wc AT this head (section (Y): the bus_emit RE-CERT NAMED leg — ONE run_mode line + its prose; the MC 11297 tame pair had grown this file +17 to 1082 l AT the 11297 head WITHOUT restamping this SIZE header or the §6 rows — named by MC 11320, this stamp is wc/grep-measured AT this head; count 48 = 37 + 11 by anchored grep). MC 11431 R1 comment-only hygiene wave restamps THIS header to 1141 l by wc AT this head (CC-9): the +42 the MC 11295 quest_mono battery legs grew at 4d95982 landed with no restamp here; run_mode count UNCHANGED 48 = 37 + 11 by anchored grep AT this head.
# runtime_integration_test.sh — T3b authoritative-runtime-path gate (MC 1256.10;
# grown through MC 1344/1348/3904/3910/3912/3915/3943/10026.1/10031 into the
# full battery below).
#
# Proves the ONE authoritative runtime path (design 1256.2 §4.1/§4.2): the
# playable main.tscn scene drives the REAL pure-logic systems through ONE
# composition root (WorldDirector), and the gate itself can fail — each
# negative control below surgically breaks one link and the proof must exit
# non-zero with its named NEG_* marker.
#
# Mirrors ci/main_composition_test.sh's FIXED pattern:
#   - one-time --import when .godot/imported is empty (clean-clone first run),
#   - build through the pinned engine (--build-solutions),
#   - headless proof run -> markers asserted via bash substring checks,
#   - graphical-test-helper render bar at --wait 15 (positive-mode legs hold
#     the live scene after PASS so 15s lands on real scene content).
#
# Battery at this HEAD: 48 run_mode legs = 37 positive modes + 11 negative (W2 close restamp: +passives S16 +trait_effects S17 +DAYNIGHT_STATE S15 +CHAR_MOTION S14; S20: +CAMERA_KILL_PULSE +BOSS_FRAME; S18: +ZONE4; W3: +BARK S8; S19: +ACT_THREE; MC 10404: +FLOOR_COVERAGE +FALL_RECOVERED; MC 10420: +GROUND_CLAMP; MC 10516: +FLOOR_TRUST x2 — the S38 mode runs on BOTH FloorCoverageProof (zones alone) and RuntimeIntegrationProof (live edge-walk); MC 10519: +ZONE_VISUAL_MOUNT — the S27 zone-scene mount leg, section Q2; MC 11018: +CAMERA_RIG_ISO — the S45 rig-mode toggle leg, with a SECOND engine spawn of the SAME mode carrying the `-- --iso-cam` launch flag (the fixed side; not a third run_mode — the flag must ride the process argv, not the env); MC 11088: SKY_STRIP/SKY_STRIP_PLANT add NO run_mode line — the Batch-4 render-probe PAIR rides two raw xvfb-run spawns in section (S) beside the FX pair (the windowed legs the --headless dummy cannot honestly run; the S48 STEP-0 precedent); count 45 = 35 + 10 AT THAT HEAD (superseded — 48 = 37 + 11 by grep at this tip) by grep; MC 11189 flipped the section-R spawn pair IN PLACE — the `-- --iso-cam` argv is RETIRED (FIXED ISO is the boot default, owner ruling verbatim "Iso,"), the session-only launch flag is now `-- --classic-cam` riding the CLASSIC-side spawn's argv (the flag still rides argv, not env — not a run_mode line); count = grep -cE '^run_mode ' here. MC 11037: the GROUND_TEX_WIRING ground-plate gate rides the ZONE_VISUAL_MOUNT capture in section Q2 — a ZONE_MOUNT_GROUNDTEX material READBACK print in the mount census + the leg's tscn-vs-disk-vs-capture greps; NO run_mode line, NO new spawn; count 45 = 35 + 10 AT THAT HEAD, superseded — 48 = 37 + 11 by grep at this tip). MC 11243: NO run_mode line, NO new spawn — the capture pre-spawn rm, the NAMED ZONE4 capture RC and the SECTION-OK echo rename all ride existing legs; count 45 = 35 + 10 AT THAT HEAD, superseded — 48 = 37 + 11 by grep at this tip). MC 11297: +tame_use +tame_neg — the D5 PRODUCTION tame leg (tames the boot-zone wild via the real calm→offer→first-wage join, WILD_PRESENT_PROD + TAME_RECRUITED) + its before-fix negative control (wild gate OFF → no castable target, NEG_TAME); count 47 = 36 + 11 by grep. MC 11320: +bus_emit section (Y) — the S0 four-edge bus leg ARMED into the battery as a NAMED leg (the mode was DEAD CI CODE at head: never a counted leg, last honest green 2026-10-05, re-certified off the MC 11301 findings L1/L2/L3 — L1 exit-tracked F-2 assert, L2 keep-alive seam on the travel waits under the 11294 gate, L3 roster-settle silence at source, marker BUS_FB_QUIET_ROSTER; leg header names all three); count 48 = 37 + 11 by grep; count = grep -cE '^run_mode ' here.
# controls (no_bus, no_spawn, no_controller, no_dna, save_bad_version,
# no_interact, quest_neg, skill_neg, calm_neg, roster_neg, tame_neg), over four proof
# classes: RuntimeIntegrationProof.cs (positive/save/dna_speak/quest/story/
# skill/calm/juice/dissolve/day-night/ground-clamp/floor-trust legs), ZoneBossProof.cs (zone_travel/boss_phase/death_load),
# P1FixProof.cs (corpse_damage/wage_betrayal/empathy_book/zone_travel_boot/
# fall_recovered), RosterIntegrationProof.cs (roster_follow/roster_neg)
# and FloorCoverageProof.cs (floor_coverage + floor_trust — the standalone
# dense grid probes, booted WITHOUT main.tscn so the zone under test is alone
# in the physics space). Most positive legs
# re-run their proof a second time to grep extra inline markers, so total
# proof invocations exceed the 40-leg count; the (E) framebuffer render bar
# runs once more through graphical-test-helper.
#
# Usage:
#   GODOT=/path/to/godot ./ci/runtime_integration_test.sh [project_dir]
set -uo pipefail

HERE="$(cd "$(dirname "$0")" && pwd)"
# MC 11152 FIX (ZONE_VISUAL_MOUNT leg, section Q2, MC 10519-era): PROJ is
# absolutized ONCE HERE, the same cd-and-pwd idiom as HERE above. The env-
# bearing capture spawns resolve LA_MOUNT_CAPTURE_PATH (and the section-S
# LA_FXCAP_PATH / LA_SKYCAP_PATH, same class) verbatim against the GODOT
# process CWD — the engine chdirs to the project dir on --path — while the
# shell-side -s check resolves the same relative string against the SHELL
# CWD: a relative PROJ arg landed the plate in $PWD/$PROJ/$PROJ/.tmp/s39/
# and the leg went RED although the proof printed LA_GATE: PASS (3 diag
# spawns: .audits/202610222340-s45playfixwalls/out/diag-mount*.log). One
# absolute PROJ = ONE anchored path for every spawn AND every check.
PROJ="$(cd "${1:-$(dirname "$HERE")}" && pwd)"
PROOF="res://ci_proofs/RuntimeIntegrationProof.cs"
GUI_HELPER="${GUI_HELPER:-/usr/local/bin/graphical-test-helper.sh}"
PASS_MARKER="LA_GATE: PASS"

fail() { echo "RUNTIME_INTEGRATION_TEST: GATE FAIL: $*" >&2; exit 1; }
[ -f "$PROJ/ci_proofs/RuntimeIntegrationProof.cs" ] || fail "RuntimeIntegrationProof.cs not found"
[ -f "$PROJ/main.tscn" ] || fail "main.tscn not found (composition root missing)"
: "${GODOT:?set GODOT to the engine binary (see ../engine/PIN.txt)}"
[ -x "$GODOT" ] || fail "GODOT not executable: $GODOT"
[ -f "$GUI_HELPER" ] || fail "graphical-test-helper not found: $GUI_HELPER"

# MC 10026.31(b): the battery runs as battery_body() below and its WHOLE
# stdout is tee'd to a census log, so the tail can grep product lines ACROSS
# EVERY LEG (see FR_CENSUS at the tail). Body legs are verbatim; a fail()
# inside exits the pipeline subshell, the wrapper propagates its code.
battery_body() {

# (A-1) clean-clone import: a fresh clone has an empty/missing .godot/imported/,
# so the first gate run spams import errors and the navmesh bakes 0 polygons.
if [ ! -d "$PROJ/.godot/imported" ] || [ -z "$(ls -A "$PROJ/.godot/imported" 2>/dev/null)" ]; then
  echo "RUNTIME_INTEGRATION_TEST: .godot/imported empty — running one-time --import"
  timeout 600 "$GODOT" --headless --path "$PROJ" --import \
    || fail "engine --import exited nonzero"
fi

echo "RUNTIME_INTEGRATION_TEST: project=$PROJ  godot=$("$GODOT" --version 2>/dev/null | tail -1)"

# build through the pinned engine so the C# assembly (incl. the proof) is current
timeout 300 "$GODOT" --headless --path "$PROJ" --build-solutions --quit-after 1 \
  || fail "engine --build-solutions exited nonzero (C# build failed)"

run_mode() {  # run_mode <mode> <expect: pass|fail> <marker> [proof-res-path]
  local mode="$1" expect="$2" marker="$3" proof="${4:-$PROOF}"
  echo "RUNTIME_INTEGRATION_TEST: mode=$mode (expect $expect)"
  local log code
  log="$(LA_GATE_MODE="$mode" timeout 240 "$GODOT" --headless --path "$PROJ" --script "$proof" 2>&1)"
  code=$?
  printf '%s\n' "$log"
  if [ "$expect" = pass ]; then
    [ "$code" -eq 0 ] || fail "mode $mode: expected exit 0, got $code"
    [[ "$log" == *"$PASS_MARKER"* ]] || fail "mode $mode: expected '$PASS_MARKER'"
    [[ "$log" == *"$marker"* ]] || fail "mode $mode: expected marker '$marker'"
  else
    [ "$code" -ne 0 ] || fail "mode $mode: expected NON-zero exit (negative control must fail), got 0"
    [[ "$log" == *"$marker"* ]] || fail "mode $mode: expected failure marker '$marker'"
  fi
}

# (A) positive: the full chain must pass.
run_mode positive pass "PLAYER_EXISTS_MOVED"
LOGP="$(LA_GATE_MODE=positive timeout 240 "$GODOT" --headless --path "$PROJ" --script "$PROOF" 2>&1)" \
  || true
[[ "$LOGP" == *'ENEMIES_EXIST_TARGETED'* ]] || fail "positive: expected live-enemy-aimed marker (MC 10058 — a live director-set enemy AIMED at the live player position, asserted in-proof)"
[[ "$LOGP" == *'DNA_EXTRACTED_EMITTED'* ]] || fail "positive: expected DNA_EXTRACTED_EMITTED (kill through the REAL CombatSystem path)"
[[ "$LOGP" == *'HUD_REFLECTS_STATE'* ]] || fail "positive: expected HUD_REFLECTS_STATE (single health tracker)"
[[ "$LOGP" == *'COMPANION_FOLLOWS'* ]] || fail "positive: expected COMPANION_FOLLOWS (machine-wired CompanionEntity)"

# (A2) MC 11099 / 10026.44.11 S49-COMP framerate-independence probe: the POSITIVE
# chain re-run with the process frame rate FORCED LOW IN-PROCESS (LA_MAX_FPS env
# -> the Engine.MaxFps boot hook beside the shipped 60 pin, Initialize.cs — the
# LA_RIG_CAPTURE env idiom). The MC 11018 cold-repro killed the (E) leg's follow
# check at a first-boot llvmpipe frame rate ("companion did NOT follow"); this
# leg pins that condition deterministically — no cold HOME, no raster choice —
# so the battery catches the class WARM on any host. Not a run_mode call (the
# mode vocabulary is unchanged; grep -cE '^run_mode ' stays 45 at that head (48 = 37 + 11 by grep at this tip)): the SAME
# positive mode with the fps cap on the env.
LOGLOWFPS="$(LA_GATE_MODE=positive LA_MAX_FPS=5 timeout 240 "$GODOT" --headless --path "$PROJ" --script "$PROOF" 2>&1)"; CODElf=$?
printf '%s\n' "$LOGLOWFPS"
[ "$CODElf" -eq 0 ] || fail "S49 low-fps probe: positive chain exited $CODElf at LA_MAX_FPS=5 — follow starved when frames dropped (MC 11099)"
[[ "$LOGLOWFPS" == *'COMPANION_FOLLOWS'* ]] || fail "S49 low-fps probe: COMPANION_FOLLOWS missing at LA_MAX_FPS=5 — companion stalled when frames dropped (MC 11099)"

# (B) negative controls: each must FAIL with its named marker.
run_mode no_bus        fail "NEG_BUS"
run_mode no_spawn      fail "NEG_SPAWN"
run_mode no_controller fail "NEG_CONTROLLER"
run_mode no_dna        fail "NEG_DNA"

# (C) save/load through the real game must pass.
run_mode save pass "SAVE_WRITTEN"
LOGS="$(LA_GATE_MODE=save timeout 240 "$GODOT" --headless --path "$PROJ" --script "$PROOF" 2>&1)" || true
[[ "$LOGS" == *'LOAD_RESTORED_DNA'* ]] || fail "save: expected LOAD_RESTORED_DNA"
[[ "$LOGS" == *'LOAD_RESTORED_LOYALTY'* ]] || fail "save: expected LOAD_RESTORED_LOYALTY"
[[ "$LOGS" == *'SAVE_ROUNDTRIP_PURE'* ]] || fail "save: expected SAVE_ROUNDTRIP_PURE"

# (D) schema guard: a future-version save must be rejected.
run_mode save_bad_version fail "NEG_SAVE_VERSION"

# (D2) DNA-speak + dialogue production path (MC 1344 DA findings C2/C4/C13):
# interact near an NPC must fire DnaLanguage.Speak -> EventBus.DnaSpoken on the
# REAL autoload bus AND open the DialogueSystem. The no_interact negative
# control disables the director's interact seam and must go red.
run_mode dna_speak pass "DNA_SPOKEN_EMITTED"
LOGD="$(LA_GATE_MODE=dna_speak timeout 240 "$GODOT" --headless --path "$PROJ" --script "$PROOF" 2>&1)" || true
[[ "$LOGD" == *'DIALOGUE_SHOWN'* ]] || fail "dna_speak: expected DIALOGUE_SHOWN"
run_mode no_interact   fail "NEG_INTERACT"

# (E) framebuffer render bar: the playable scene actually paints a non-blank frame.
# --wait 15: the proof holds the live scene after PASS so 15s lands on real content.
LOGC="$("$GUI_HELPER" --cmd "$GODOT --path $PROJ --script $PROOF" --wait 15 2>&1)"
CODEc=$?
printf '%s\n' "$LOGC"
[ "$CODEc" -eq 0 ] || fail "graphical-test-helper exited $CODEc (render bar not met)"
[[ "$LOGC" == *'RESULT=PASS'* ]] \
  || fail "expected 'RESULT=PASS' from graphical-test-helper (framebuffer blank/uniform)"

# (F) zone travel + boss phase (MC 1344 DA findings 3+4, C15): canyon/ruins
# reachable in play via the travel action, SpawnSet scaled stats live on the
# spawned AI, boss reachable and BossController.Phase firing EcosystemAdapted.
PROOF_ZB="res://ci_proofs/ZoneBossProof.cs"
[ -f "$PROJ/ci_proofs/ZoneBossProof.cs" ] || fail "ZoneBossProof.cs not found"
run_mode zone_travel pass "ZONE_TRAVEL_CANYON" "$PROOF_ZB"
LOGZ="$(LA_GATE_MODE=zone_travel timeout 240 "$GODOT" --headless --path "$PROJ" --script "$PROOF_ZB" 2>&1)" || true
[[ "$LOGZ" == *'ZONE_TRAVEL_CANYON'* ]] || fail "zone_travel: expected ZONE_TRAVEL_CANYON"
[[ "$LOGZ" == *'ZONE_TRAVEL_RUINS'* ]] || fail "zone_travel: expected ZONE_TRAVEL_RUINS"
[[ "$LOGZ" == *'SCALED_STATS_APPLIED'* ]] || fail "zone_travel: expected SCALED_STATS_APPLIED"
run_mode boss_phase pass "BOSS_PHASE_FIRED" "$PROOF_ZB"
LOGB="$(LA_GATE_MODE=boss_phase timeout 240 "$GODOT" --headless --path "$PROJ" --script "$PROOF_ZB" 2>&1)" || true
[[ "$LOGB" == *'BOSS_REACHED'* ]] || fail "boss_phase: expected BOSS_REACHED"

# (G) death recovery (MC 1348 N1): dead player's shell must stop moving, and
# the F9 load path must restore health + movement from the death state.
# MC 3910 (DA W2): the leg also greps DEATH_SAVE_OWNED — the mode deletes the
# ONE shared user://savegame.json before its own write and stamps health, so
# the resurrection can never pass off a stale save from a sibling mode.
run_mode death_load pass "DEATH_LOAD_RESURRECTED" "$PROOF_ZB"
LOGD="$(LA_GATE_MODE=death_load timeout 240 "$GODOT" --headless --path "$PROJ" --script "$PROOF_ZB" 2>&1)" || true
[[ "$LOGD" == *'DEATH_MOVEMENT_STOPPED'* ]] || fail "death_load: expected DEATH_MOVEMENT_STOPPED"
[[ "$LOGD" == *'DEATH_MOVEMENT_RESTORED'* ]] || fail "death_load: expected DEATH_MOVEMENT_RESTORED"
[[ "$LOGD" == *'DEATH_SAVE_OWNED'* ]] || fail "death_load: expected DEATH_SAVE_OWNED (phase must own its save — delete-then-write, health stamped; MC 3910)"

# (H) MC 1348 P1 gameplay-bug regressions (A2/A3/A4/A5), one mode per finding
# in ci_proofs/P1FixProof.cs. Each mode reproduces its audited bug against the
# REAL playable scene and must pass post-fix.
PROOF_P1="res://ci_proofs/P1FixProof.cs"
[ -f "$PROJ/ci_proofs/P1FixProof.cs" ] || fail "P1FixProof.cs not found"
# (H1) A2 corpse-damage loop: a kill on the enemy's attack frame must not leave
# the corpse dealing its frozen last-tick damage every frame.
run_mode corpse_damage pass "CORPSE_DAMAGE_STOPPED" "$PROOF_P1"
# (H2) A3 wage/betrayal pillar: with the pay input never pressed, the skip arm
# must drain loyalty to 0 and the C7 betrayal must execute in play.
run_mode wage_betrayal pass "BETRAYAL_FIRED" "$PROOF_P1"
# (H3) A4 Empathy Book reachability: the book input must open the panel on a
# live M04 entry (C2 EmpathyBookOpened) and dismiss it again.
run_mode empathy_book pass "EMPATHY_BOOK_OPENED" "$PROOF_P1"
LOGE="$(LA_GATE_MODE=empathy_book timeout 240 "$GODOT" --headless --path "$PROJ" --script "$PROOF_P1" 2>&1)" || true
[[ "$LOGE" == *'EMPATHY_BOOK_CLOSED'* ]] || fail "empathy_book: expected EMPATHY_BOOK_CLOSED"
# (H4) A5 zone travel must despawn the BOOT enemy set too: after travelling, no
# enemy from the boot composition remains alive or in the director's live set.
run_mode zone_travel_boot pass "BOOT_SET_CLEARED" "$PROOF_P1"

# (I) MC 3904 stage 2c quest core: the FULL 5-quest placeholder arc driven
# headlessly through the REAL scene — boot zone fact -> speak -> the pay_wage
# settle riding WagePaid (QUEST_OBJECTIVE on q_wage) -> four extractions ->
# the zone-boss finale (QUEST_COMPLETED, owner ruling D6); quest progress
# surviving save->load in the live scene (QUEST_PERSIST) plus the DA-P1
# EVIDENCE-REWIND leg (one post-load extraction cascades nothing —
# QUEST_REWIND); and the SetQuestHooksEnabled gate seam going red (NEG_QUEST).
# MC 3915 adds the reward-beat legs to quest_arc: every Completed transition
# shows the row's Reward node through the dialogue view (REWARD_SHOWN marker
# x5, view state asserted in-proof) and the named REWARD_GUARD leg proves a
# reward-shown node never satisfies a DialogueShown objective while a normal
# show of the same node does. MC 10026.1 adds the dialogue-lifecycle legs: a
# direct reply is never starved by a queued beat (DLQ_SPEAK_PRECEDENCE), a
# same-tick pair drains in strict emission order (DLQ_DRAINED), the queue
# draining closes the box via the one uniform auto-close (DLQ_CLOSED) and an
# unauthored direct npc_ line auto-closes too (DLQ_NPC_CLOSED — "All dialogue
# lines"; R5 itself stays UNCOVERED by these legs — DA wave ruling P3-α).
run_mode quest_arc pass "QUEST_COMPLETED"
LOGQ="$(LA_GATE_MODE=quest_arc timeout 240 "$GODOT" --headless --path "$PROJ" --script "$PROOF" 2>&1)" || true
[[ "$LOGQ" == *'QUEST_STARTED'* ]] || fail "quest_arc: expected QUEST_STARTED"
[[ "$LOGQ" == *'QUEST_OBJECTIVE q_wage'* ]] || fail "quest_arc: expected QUEST_OBJECTIVE q_wage (WagePaid arm driven)"
[[ "$LOGQ" == *'WAGE_PAID for q_wage'* ]] || fail "quest_arc: expected WAGE_PAID for q_wage on the bus"
[[ "$LOGQ" == *'QUEST_COMPLETED q_boss'* ]] || fail "quest_arc: expected QUEST_COMPLETED q_boss (zone-boss finale)"
for q in q_intro q_speak q_wage q_kills q_boss; do
  [[ "$LOGQ" == *"REWARD_SHOWN $q "* ]] || fail "quest_arc: expected REWARD_SHOWN $q (reward beat emitter, MC 3915)"
done
[[ "$LOGQ" == *'REWARD_GUARD'* ]] || fail "quest_arc: expected REWARD_GUARD (reward beats never feed DialogueShown — MC 3915)"
for m in DLQ_SPEAK_PRECEDENCE DLQ_DRAINED DLQ_CLOSED DLQ_NPC_CLOSED; do
  [[ "$LOGQ" == *"$m"* ]] || fail "quest_arc: expected $m (dialogue lifecycle leg — MC 10026.1)"
done
run_mode quest_persist pass "QUEST_PERSIST"
LOGP="$(LA_GATE_MODE=quest_persist timeout 300 "$GODOT" --headless --path "$PROJ" --script "$PROOF" 2>&1)" || true
[[ "$LOGP" == *'QUEST_REWIND_ARMED'* ]] || fail "quest_persist: evidence-rewind leg never armed (wage/farm drift broken)"
[[ "$LOGP" == *'QUEST_REWIND —'* ]] || fail "quest_persist: expected QUEST_REWIND (one post-load extraction, zero cascading completes — DA P1)"
run_mode quest_neg fail "NEG_QUEST"

# (J) MC 3912 stage 2e skill core: the skill economy end-to-end on the REAL
# scene — +Manna rides the existing kill handler, the live consensus unlocks
# both launch skills, an UNARMED hit deals exactly MeleeDamage THROUGH the
# single DealDamage site, skill_1 pays EXACTLY once and arms, the armed hit
# deals MeleeDamage x multiplier and the arm is consumed after that one hit,
# a short balance is rejected without spending, and Mend pays once and
# raises health. skill_neg proves the gate seam stalls the whole economy.
run_mode skill_use pass "SKILL_USED invert_strike"
LOGSK="$(LA_GATE_MODE=skill_use timeout 300 "$GODOT" --headless --path "$PROJ" --script "$PROOF" 2>&1)" || true
[[ "$LOGSK" == *'KILL_MANNA_GAIN'* ]] || fail "skill_use: expected KILL_MANNA_GAIN (+Manna on the existing DnaExtracted handler)"
[[ "$LOGSK" == *'UNLOCK_INVERT_STRIKE + UNLOCK_MEND'* ]] || fail "skill_use: expected UNLOCK_INVERT_STRIKE + UNLOCK_MEND (live consensus authority)"
[[ "$LOGSK" == *'UNARMED_HIT_BASE'* ]] || fail "skill_use: expected UNARMED_HIT_BASE (base damage at the DealDamage site)"
[[ "$LOGSK" == *'MANNA_SPEND_ONCE'* ]] || fail "skill_use: expected MANNA_SPEND_ONCE (Manna dropped exactly once)"
[[ "$LOGSK" == *'ARMED_HIT_MULTIPLIED'* ]] || fail "skill_use: expected ARMED_HIT_MULTIPLIED (MeleeDamage x multiplier through the ONE kill path)"
[[ "$LOGSK" == *'ARM_CONSUMED'* ]] || fail "skill_use: expected ARM_CONSUMED (arm rode exactly one hit)"
[[ "$LOGSK" == *'MANNA_REJECTED'* ]] || fail "skill_use: expected MANNA_REJECTED (insufficient Manna refused)"
[[ "$LOGSK" == *'MEND_HEALTH_UP'* ]] || fail "skill_use: expected MEND_HEALTH_UP (Mend healed and paid once)"
[[ "$LOGSK" == *'SKILL_USED mend'* ]] || fail "skill_use: expected SKILL_USED mend on the bus"
run_mode skill_neg fail "NEG_SKILL"

# (J2) MC 10031 Calming Speak: the skill→recruitment bridge on the REAL
# scene — a cast spends 12 AFTER a target scan (refusals spend ZERO), opens
# the SAME RecruitOffered flag with a 600-frame window, the pay-first wage
# path stays the SOLE join authority (pay-in-window joins, expiry joins
# nobody), E's standing offer is never downgraded and outlives the window,
# and the load-restore seam kills every window with its body (no save-scum
# economy rollback). calm_neg proves the gate seam leaks nothing.
run_mode calm_use pass "CALM_CAST"
LOGCLM="$(LA_GATE_MODE=calm_use timeout 300 "$GODOT" --headless --path "$PROJ" --script "$PROOF" 2>&1)" || true
[[ "$LOGCLM" == *'UNLOCK_CALMING_SPEAK'* ]] || fail "calm_use: expected UNLOCK_CALMING_SPEAK (live third-rule authority)"
[[ "$LOGCLM" == *'CALM_PAY_IN_WINDOW'* ]] || fail "calm_use: expected CALM_PAY_IN_WINDOW (the unchanged wage path joined)"
[[ "$LOGCLM" == *'CALM_REFUSE_SHORT'* ]] || fail "calm_use: expected CALM_REFUSE_SHORT (short balance spent nothing)"
[[ "$LOGCLM" == *'CALM_REFUSE_STANDING'* ]] || fail "calm_use: expected CALM_REFUSE_STANDING (E's standing offer refused, zero spend)"
[[ "$LOGCLM" == *'CALM_E_STANDS'* ]] || fail "calm_use: expected CALM_E_STANDS (window retired under E, offer outlives it)"
[[ "$LOGCLM" == *'CALM_WINDOW_EXPIRES'* ]] || fail "calm_use: expected CALM_WINDOW_EXPIRES (expiry withdrew the offer, pay joined nobody)"
[[ "$LOGCLM" == *'CALM_LOAD_CLEARED'* ]] || fail "calm_use: expected CALM_LOAD_CLEARED (real load_game press cleared the window, kept the standing offer)"
run_mode calm_neg fail "NEG_CALM"

# (J2) MC 11297 D5+D6 PRODUCTION tame (RuntimeIntegrationProof.Tame.cs, stage 110):
# the D5 fix makes a recruit-able wild stand in the shipped meadow (FieldBootWild,
# behind the SetWildSpawnEnabled gate — tame_use is the ONLY mode leaving it ON); the
# D6 fix gives the short-balance Mend refusal a NAMED line (before: silence). tame_use
# farms to unlock, tames the PRODUCTION wild through the real calm→offer→first-wage
# join with NO test seam (TAME_RECRUITED, named-GREEN), then proves the Mend refusal
# PRINTS (MEND_REFUSAL_PRINTED + the director's named "Mend REFUSED — short balance"
# transcript line). tame_neg = the wild gate OFF (the before-fix state): the same
# funded unlocked press finds no castable target and leaks nothing — NEG_TAME red.
run_mode tame_use pass "TAME_RECRUITED"
LOGTAME="$(LA_GATE_MODE=tame_use timeout 300 "$GODOT" --headless --path "$PROJ" --script "$PROOF" 2>&1)" || true
[[ "$LOGTAME" == *'WILD_PRESENT_PROD'* ]] || fail "tame_use: expected WILD_PRESENT_PROD (production fields the recruit-able wild, no seam — D5)"
[[ "$LOGTAME" == *'TAME_RECRUITED'* ]] || fail "tame_use: expected TAME_RECRUITED (calm→offer→first-wage joined the PRODUCTION wild)"
[[ "$LOGTAME" == *'MEND_REFUSAL_PRINTED'* ]] || fail "tame_use: expected MEND_REFUSAL_PRINTED (D6 refusal witness)"
[[ "$LOGTAME" == *'Mend REFUSED — short balance'* ]] || fail "tame_use: expected the D6 named Mend-REFUSED line on the wire (silent before MC 11297)"
run_mode tame_neg fail "NEG_TAME"

# (K) MC 3943 stage 2g follower roster (ci_proofs/RosterIntegrationProof.cs):
# recruit WILD creatures in play (interact-offer + FIRST wage via the existing
# pay_wage), followers follow, INDEPENDENT per-follower wages each emitting
# their own WagePaid (ARCH W2), save->load restores N=3 (LOAD_RESTORED
# extended), cycle_follower + Forgive + break_bond arms drive the Empathy Book
# (one follower betrays, the others keep following), and the hearts MEAN rides
# the LAST LoyaltyChanged of the frame under the reserved "roster" key — the
# emit-order contract is asserted on the real wire order. roster_neg proves
# the cap: the 4th recruit is REFUSED (owner ruling D1) — red with NEG_ROSTER.
PROOF_ROSTER="res://ci_proofs/RosterIntegrationProof.cs"
[ -f "$PROJ/ci_proofs/RosterIntegrationProof.cs" ] || fail "RosterIntegrationProof.cs not found"
run_mode roster_follow pass "ROSTER_RECRUITED" "$PROOF_ROSTER"
LOGR="$(LA_GATE_MODE=roster_follow timeout 300 "$GODOT" --headless --path "$PROJ" --script "$PROOF_ROSTER" 2>&1)" || true
[[ "$LOGR" == *'ROSTER_TWO_RECRUITED'* ]] || fail "roster_follow: expected ROSTER_TWO_RECRUITED"
[[ "$LOGR" == *'FOLLOWERS_FOLLOW'* ]] || fail "roster_follow: expected FOLLOWERS_FOLLOW (two recruited bodies trail the player)"
[[ "$LOGR" == *'WAGE_INDEPENDENT_A'* ]] || fail "roster_follow: expected WAGE_INDEPENDENT_A (only the DUE follower settled)"
[[ "$LOGR" == *'WAGE_INDEPENDENT_B'* ]] || fail "roster_follow: expected WAGE_INDEPENDENT_B (non-boot follower settled its OWN wage + WagePaid — singleton read would mute it)"
[[ "$LOGR" == *'ROSTER_RESTORED'* ]] || fail "roster_follow: expected ROSTER_RESTORED (load restores N=3)"
[[ "$LOGR" == *'CYCLE_SELECTED'* ]] || fail "roster_follow: expected CYCLE_SELECTED (cycle_follower action)"
[[ "$LOGR" == *'FORGIVE_APPLIED'* ]] || fail "roster_follow: expected FORGIVE_APPLIED (book-open pay_wage = Forgive, not a wage)"
[[ "$LOGR" == *'BREAK_BOND_SELECTED'* ]] || fail "roster_follow: expected BREAK_BOND_SELECTED (one betrays, others keep following)"
[[ "$LOGR" == *'PAY_AFTER_BREAK_REFUSED'* ]] || fail "roster_follow: expected PAY_AFTER_BREAK_REFUSED (pay press on a broken bond must never settle — DA W5 F1)"
[[ "$LOGR" == *'HEARTS_MEAN_LAST'* ]] || fail "roster_follow: expected HEARTS_MEAN_LAST (mean emitted LAST under the reserved key)"
[[ "$LOGR" == *'ROSTER_OVERSIZED_TRIMMED'* ]] || fail "roster_follow: expected ROSTER_OVERSIZED_TRIMMED (over-cap restore trims: N=3, bodies=N, DROPPED marker)"
[[ "$LOGR" == *'restored entry id'* && "$LOGR" == *'DROPPED — roster cap'* ]] || fail "roster_follow: expected the F4 DROPPED marker on the over-cap restore (world-side drop-arm silent)"
run_mode roster_neg fail "NEG_ROSTER" "$PROOF_ROSTER"

# (L) MC 10120 Inc-3 S1 juice hit-flash: the white flash + VISUAL-NODE offset
# punch on the live scene — active on the hit frame, held at +4f, EXACT base
# return by +6f, and the enemy BODY GlobalPosition UNCHANGED across the whole
# window (the target's physics process is off, so juice is the only candidate
# mover — a punch applied to the body goes RED). Real attack wire, stage 95.
run_mode JUICE_HITFLASH pass "JUICE_BODY_STILL"

echo "RUNTIME_INTEGRATION_TEST: SECTION-OK — section-local cumulative snapshot; the TAIL GATE PASS echo is the gate-name owner (renamed by MC 11243 F5 — mid-body GATE PASS copies made a bare grep non-tail-specific) (positive green; no_bus/no_spawn/no_controller/no_dna/save_bad_version all red with named markers; save round-trip green; zone travel + boss phase green; death recovery green; MC 1348 P1 regressions green; quest arc green, persist + evidence-rewind green, reward beats + guard green, quest_neg red; skill economy green, skill_neg red; roster follow/save-load/book arms + mean-last emit order green, pay-after-break refused green, oversized-save trim green, roster_neg cap red; juice hit-flash green; non-blank render)"
# (M) MC 10121 Inc-3 S2 juice camera shake: FollowCamera SUBSCRIBES S0's
# PlayerHurt/BossFallen on the live scene — the window is armed by a real
# TakeDamage-driven emit, HELD at +4f (camera off its follow base), and the
# camera sits back on its EXACT shake-free follow base by +12f (integer
# decay, F4 — the planted delta-time decay goes RED). BossFallen arm reacts
# too. Real bus wire, stage 96; the camera prints ONLY its boot rig line (F4-CMP).
run_mode JUICE_SHAKE pass "JUICE_SHAKE_AT_BASE"

echo "RUNTIME_INTEGRATION_TEST: SECTION-OK — section-local cumulative snapshot; the TAIL GATE PASS echo is the gate-name owner (renamed by MC 11243 F5 — mid-body GATE PASS copies made a bare grep non-tail-specific) (positive green; no_bus/no_spawn/no_controller/no_dna/save_bad_version all red with named markers; save round-trip green; zone travel + boss phase green; death recovery green; MC 1348 P1 regressions green; quest arc green, persist + evidence-rewind green, reward beats + guard green, quest_neg red; skill economy green, skill_neg red; roster follow/save-load/book arms + mean-last emit order green, pay-after-break refused green, oversized-save trim green, roster_neg cap red; juice shake green; non-blank render)"
# (N) MC 10129 Inc-3 S3 death dissolve: a real-wire kill pins the death-tick
# suppression — body collision mask 0 (the plan-named collision-off line; the
# engine's layer-number API is 1-based, the 0-based literal is a silent no-op —
# MC 10129 probe, evidence dir) while the director's EXISTING IsDead targeting
# predicate (WorldDirector.cs:375, reused not re-authored) keeps the corpse out
# of every targeting/count print — and the visual dissolves on a 20-frame
# INTEGER counter (F4), freeing itself at f+21 (visual despawn). Through
# f+1..+20 forced attacks deal 0: input presses never land (zero DamageDealt
# events name the corpse id) AND a direct forced DealDamage(9999) returns
# false with health pinned at 0 (the ghost-body evidence leg, DA P2-4; the
# planted skip-collision-off goes RED at the mask read). Stage 97, real spawn
# set, no save touched.
run_mode DISSOLVE_SUPPRESS pass "DISSOLVE_GONE"



# (O) MC 10132 Inc-3 S10 story ACT TWO: the +4 ruins-deep QuestTable.RuinsArc()
# rows play on the live scene through the SHIPPED pure QuestLog machine — the
# act opens off a REAL save->load edge (the restore sync) with its table-driven
# open card painted guarded (ACT2_CARD_ON_SCREEN); each of the four completions
# rides its OWN distinct drive (W6 pin: zone-enter / two fresh speaks / the
# single wage path resolving the act-two row / six fresh extractions — four
# separate QUEST_COMPLETED q_r_* greps, never one cascade); the act-two rows
# round-trip save->load on the SHIPPED v3 QuestStates wire (ACT2_PERSIST, zero
# save-file schema delta — data rides existing rows); and the close card drains
# in emission order behind the finale beat to the uniform auto-close (DLQ
# untouched, F3). Stage 55, proof partial RuntimeIntegrationProof.Story2.cs.
run_mode quest_arc2 pass "ACT2_CLOSED"
LOGA2="$(LA_GATE_MODE=quest_arc2 timeout 300 "$GODOT" --headless --path "$PROJ" --script "$PROOF" 2>&1)" || true
[[ "$LOGA2" == *'ACT2_OPENED'* ]] || fail "quest_arc2: expected ACT2_OPENED (restore sync opened the act)"
[[ "$LOGA2" == *'ACT2_CARD_ON_SCREEN'* ]] || fail "quest_arc2: expected ACT2_CARD_ON_SCREEN (table-driven open card, guarded beat)"
[[ "$LOGA2" == *'ACT_CARD act_two open'* ]] || fail "quest_arc2: expected the open ACT_CARD marker (REWARD_SHOWN idiom)"
for q in q_r_descent q_r_tongue q_r_bread q_r_bones; do
  [[ "$LOGA2" == *"QUEST_COMPLETED $q"* ]] || fail "quest_arc2: expected QUEST_COMPLETED $q (W6: its own observation, its own completion)"
done
for q in q_r_descent q_r_tongue q_r_bread q_r_bones; do
  [[ "$LOGA2" == *"REWARD_SHOWN $q "* ]] || fail "quest_arc2: expected REWARD_SHOWN $q (reward beat through the shipped DLQ path)"
done
[[ "$LOGA2" == *'WAGE_PAID for q_r_bread'* ]] || fail "quest_arc2: expected WAGE_PAID for q_r_bread (the ONE wage path resolved the act-two row)"
[[ "$LOGA2" == *'ACT2_PERSIST'* ]] || fail "quest_arc2: expected ACT2_PERSIST (act-two rows round-tripped on the shipped QuestStates wire)"
[[ "$LOGA2" == *'ACT2_CLOSED'* ]] || fail "quest_arc2: expected ACT2_CLOSED (close card drained + uniform auto-close)"
[[ "$LOGA2" == *'ACT_CARD act_two close'* ]] || fail "quest_arc2: expected the close ACT_CARD marker"

# MC 11295 (playthrough D3, MC 11270 verdict) — QUEST BEAT MONOTONICITY.
# The diverge-based act proofs (quest_arc2/ACT_THREE) open the act through the
# save path IN THE MEADOW — they never drive the boss-death ORDERING: the act
# opening while the player ALREADY STANDS in the zone the first row waits on.
# That ordering is exactly where D3 regressed the line ("The Watcher Falls" ->
# "The Way Down — Reach ruins" AT THE RUINS, and the SAVE held it). quest_mono
# drives it REAL: boot facts -> interact -> wage -> four kills -> travel to the
# RUINS -> kill the tier-2 boss -> the open edge must ADVANCE (q_r_descent met
# at open by the standing-zone observation, tracker on "The Older Tongue"), the
# ADVANCED rows must survive save -> DISK -> load (the disk file is the witness
# — the D3 evidence class is saved state, not HUD staleness), and four wrap
# travels — last the ruins RE-entry, the seeded fact re-fired — regress nothing.
# RED-capability of THIS leg is the product's own before-face: run at the
# pre-fix head the MONO_ADVANCE check fails BY NAME, printing the stranded
# [q_r_descent:active] account (banked: .audits/202610251405-d3/out/red-*).
# NO run_mode line (the SKY_STRIP raw-spawn precedent): the leg is ONE raw
# spawn + in-line greps; the battery count was UNCHANGED at 45 = 35 + 10 AT THAT HEAD (superseded — 48 = 37 + 11 by grep at this tip).
LOGQM="$(LA_GATE_MODE=quest_mono timeout 300 "$GODOT" --headless --path "$PROJ" --script "$PROOF" 2>&1)" || true
printf '%s\n' "$LOGQM" > "$PROJ/.tmp/quest-mono.log" 2>/dev/null || true
for m in MONO_BOOT MONO_SPOKEN MONO_WAGE MONO_KILLS MONO_BOSS_LIVE; do
  [[ "$LOGQM" == *"$m"* ]] || fail "quest_mono: expected $m (the pre-kill chain stalled — see .tmp/quest-mono.log)"
done
[[ "$LOGQM" == *'MONO_TRACKER_BEFORE'*'The Watcher Falls'* ]] \
  || fail "quest_mono: expected MONO_TRACKER_BEFORE with the act-one finale line (the beat the regression rewound from)"
[[ "$LOGQM" == *'MONO_ADVANCE'* ]] \
  || fail "quest_mono: THE GATE — expected MONO_ADVANCE: the boss-kill at the ruins must ADVANCE the beat (q_r_descent met AT OPEN); the missing marker IS the D3 regression (tracker back to \"The Way Down\")"
[[ "$LOGQM" == *'MONO_TRACKER_AFTER'*'Older Tongue'* ]] \
  || fail "quest_mono: expected MONO_TRACKER_AFTER on the ADVANCED act-two row (the beat must read forward after the finale)"
[[ "$LOGQM" == *'QUEST_COMPLETED q_r_descent'* ]] \
  || fail "quest_mono: expected QUEST_COMPLETED q_r_descent (the open-edge completion rode the real bus)"
[[ "$LOGQM" == *'REWARD_SHOWN q_r_descent '* ]] \
  || fail "quest_mono: expected REWARD_SHOWN q_r_descent (the guarded reward-beat path, one completion one observation)"
[[ "$LOGQM" == *'ACT_CARD act_two open'* ]] || fail "quest_mono: expected the open ACT_CARD marker (REWARD_SHOWN idiom)"
[[ "$LOGQM" == *'MONO_SAVEDROWS'*'q_r_descent:completed'* ]] \
  || fail "quest_mono: expected MONO_SAVEDROWS carrying q_r_descent:completed ON DISK (the D3 evidence class is the SAVED state)"
[[ "$LOGQM" == *'MONO_PERSIST'* ]] \
  || fail "quest_mono: expected MONO_PERSIST (load round-trips the ADVANCED beat, Adopt silent — a load is not a game beat)"
[[ "$LOGQM" == *'MONO_REENTER —'* ]] \
  || fail "quest_mono: expected MONO_REENTER (four wrap travels incl. the ruins re-entry regressed nothing)"
[[ "$LOGQM" == *'LA_GATE: PASS — quest beat monotonicity'* ]] \
  || fail "quest_mono: expected the terminal PASS marker (see .tmp/quest-mono.log)"

# (P) MC 10200 Inc-4 S16 Resonance passives: ONE passive proven end-to-end
# on the LIVE scene, band reached via REAL extracts (not a hand-fed fixture).
# Baseline consensus width 0 = no bands + zero Manna; the FIRST real kill
# through the ONE kill path takes the consensus to its census width 6 (both
# bands live); the kill credited at the SHIPPED add seam shows the OBSERVED
# NUMBER CHANGE (base + Resonant Draw, not base alone); a second real extract
# proves the band persists (same credited amount, one payment each); and a
# real skill_2 Mend press heals base + Deep Mend through the shipped heal
# seam. Stage 65, proof partial RuntimeIntegrationProof.Passives.cs. The
# spend-site planted-bad (drop a bonus at either seam) goes RED on the live
# number; band-threshold drift goes RED in ci/skill_test.sh F10/F13 first.
run_mode passives pass "PASSIVES_MEND_BONUS"
LOGPA="$(LA_GATE_MODE=passives timeout 300 "$GODOT" --headless --path "$PROJ" --script "$PROOF" 2>&1)" || true
[[ "$LOGPA" == *'PASSIVES_BASELINE'* ]] || fail "PASSIVES: expected PASSIVES_BASELINE (width 0, no bands, Manna=0)"
[[ "$LOGPA" == *'PASSIVES_KILL_BONUS'* ]] || fail "PASSIVES: expected PASSIVES_KILL_BONUS (first real extract -> width 6, kill credited base + Resonant Draw)"
[[ "$LOGPA" == *'PASSIVES_KILL_CREDIT'* ]] || fail "PASSIVES: expected PASSIVES_KILL_CREDIT (second real extract credited the same band-on amount)"
[[ "$LOGPA" == *'PASSIVES_MEND_BONUS'* ]] || fail "PASSIVES: expected PASSIVES_MEND_BONUS (Mend healed base + Deep Mend on the live scene)"


# (P) MC 10201 Inc-4 S17 TRAIT_EFFECTS: ONE live pay press on the real scene
# settles known traits and the marker prints the OBSERVED delta AGAINST BASE
# with the S9 trait tag — [Bonded] Manna +1 vs base +0 (world rider consumed
# at the shipped SkillState.GainManna add site), [Forager] loyalty +3 vs +5
# (wage -2); [Steadfast] idle as the rule-specificity control (its -25% is
# the wage-MISS arm — headless table owns it). Sentinel CUT (no kill-assist
# seam) and the wage-free upkeep BAND SHIPPED at counter-width 4 (S16/S8 census FILED pre-build: .audits/202610070005-s16/census.md — W2-close restamp of the stale CUT wording the merge flagged)
# time) ride the EVIDENCE CUT-decisions row. Known-id reachability rides the
# ratified restore seam (cap 3 + wild pool 21+ cannot recruit a Bonded fresh).
# The cap/trim/F6 legs (roster_follow/roster_neg above) are UNCHANGED.
run_mode trait_effects pass "TRAIT_EFFECTS" "$PROOF_ROSTER"
LOGTR="$(LA_GATE_MODE=trait_effects timeout 300 "$GODOT" --headless --path "$PROJ" --script "$PROOF_ROSTER" 2>&1)" || true
[[ "$LOGTR" == *'TRAIT_EFFECTS: [Bonded] settle delta Manna +1 vs base +0'* ]] || fail "trait_effects: expected the [Bonded] expected-vs-base delta line (world Manna rider broken)"
[[ "$LOGTR" == *'[Forager] settle delta +3 vs base +5'* ]] || fail "trait_effects: expected the [Forager] wage -2 delta line (roster settle rule broken)"
# WAGEFREE_UPKEEP (the DA-F1 band hunk; S16/S8 census filed at build time,
# threshold 4 census-confirmed): the live skip-arm A/B — the SAME wage-miss
# drive decays -2 at DNA width 0 (Steadfast control) and is WAIVED (delta 0,
# interval consumed) at the census live width 6. Threshold drift/census-less
# code -> this marker goes missing -> RED.
[[ "$LOGTR" == *'WAGEFREE_UPKEEP: width 6'* ]] || fail "trait_effects: expected the WAGEFREE_UPKEEP waived-tick marker (band hunk broken)"

# (P) MC 10199 / 10026.26 Inc-4 S15 DAY-NIGHT DRIVER: the world-tick INTEGER
# FRAME clock is exact every physics tick (a wall-clock/DateTime feed diverges
# at the clock-nonlinear assert — the planted-bad RED pre-proof, evidence
# .audits/*-s15/), the SHIPPED zone Environment + DirectionalLight3D carry the
# per-zone const keyframe tables EXACTLY at the named dawn/noon/dusk/night
# frame windows, fog_sky_affect READS 0.0 (PINNED S13 carry, re-pinned never
# raised), and the three zone noon tables are distinct with a continuous
# night->dawn wrap. Runtime-only (RULING-7: zero save delta), no bus signal,
# presentation-only. Stage 98, proof partial RuntimeIntegrationProof.DayNight.cs.
run_mode DAYNIGHT_STATE pass "DAYNIGHT_NIGHT"
LOGDN="$(LA_GATE_MODE=DAYNIGHT_STATE timeout 300 "$GODOT" --headless --path "$PROJ" --script "$PROOF" 2>&1)" || true
for m in DAYNIGHT_DAWN DAYNIGHT_NOON DAYNIGHT_DUSK DAYNIGHT_FOGPIN_0 DAYNIGHT_TABLE_CANYON DAYNIGHT_WRAP_CONTINUOUS DAYNIGHT_LINEAR; do
  [[ "$LOGDN" == *"$m"* ]] || fail "DAYNIGHT_STATE: expected $m (S15 clock/window/table/fog-pin leg)"
done

echo "RUNTIME_INTEGRATION_TEST: SECTION-OK — section-local cumulative snapshot; the TAIL GATE PASS echo is the gate-name owner (renamed by MC 11243 F5 — mid-body GATE PASS copies made a bare grep non-tail-specific) (positive green; no_bus/no_spawn/no_controller/no_dna/save_bad_version all red with named markers; save round-trip green; zone travel + boss phase green; death recovery green; MC 1348 P1 regressions green; quest arc green, persist + evidence-rewind green, reward beats + guard green, quest_neg red; skill economy green, skill_neg red; roster follow/save-load/book arms + mean-last emit order green, pay-after-break refused green, oversized-save trim green, roster_neg cap red; dissolve suppress green; story act-two arc green; day-night frame clock + exact anchor windows + fog pin green; non-blank render)"
# (P) MC 10198 Inc-4 S14 CHARACTER LIFE: the integer-phase motion driver on
# the LIVE scene — a chasing goblin's visual bobs off base off the read-only
# SimVelocity feed (CHAR_ENEMY_WALK); the player's walk rides real WASD input
# on a probed-clear direction, its phase pinned to a velocity-fed INTEGER
# tick-counter by a two-point exact-delta check (a delta-time phase lands RED;
# CHAR_WALK_ACTIVE); release returns the rig to its EXACT captured base with
# phase 0 (CHAR_WALK_AT_REST — no float drift); the attack lean arms on the
# hit frame at the ONE DealDamage hunk and decays to exact base on an integer
# 6f counter (CHAR_LEAN_ACTIVE / CHAR_LEAN_AT_BASE); and the class-swapped
# player VisualJuice root flashes WHITE at the real player-damage site while
# the BODY position stays bit-unchanged across the juice window with physics
# frozen — a motion-on-body plant lands here RED (CHAR_PLAYER_FLASH /
# CHAR_BODY_STILL). Stage 99 (renumbered at merge off S15's 98), proof partial RuntimeIntegrationProof.Motion.cs.
run_mode CHAR_MOTION pass "CHAR_BODY_STILL"
LOGP="$(LA_GATE_MODE=CHAR_MOTION timeout 300 "$GODOT" --headless --path "$PROJ" --script "$PROOF" 2>&1)" || true
for m in CHAR_ENEMY_WALK CHAR_WALK_ACTIVE CHAR_WALK_AT_REST CHAR_LEAN_ACTIVE CHAR_LEAN_AT_BASE CHAR_PLAYER_FLASH; do
  [[ "$LOGP" == *"$m"* ]] || fail "CHAR_MOTION: expected $m"
done

# (Q) MC 10210 W2 tail (F-C): the LA_GATE_MODE vocabulary drift gate — a pure
# TREE parse (no engine): for every dispatching proof, header-doc modes ==
# allow-list (KnownModes, where one exists) == dispatch arms, named VIOLATION
# lines on drift. Red-capability ships with the check (--selftest plants one
# arm without a doc row and one KnownModes entry on temp copies; all must go
# RED, tree untouched — run it solo for the logged RED/GREEN pair).
bash "$HERE/mode_sets_check.sh" \
  || fail "mode_sets_check exited nonzero — mode vocabulary drift (VIOLATION lines above)"


# (Q) MC 10217 / 10026.35 Inc-4 S20 KILL PULSE: the FollowCamera's DnaExtracted
# CONSUMER (plan pin N-3: NO new signal, census stays 15) on the live scene —
# the 8f INTEGER punch window opens on the emit, HOLDS +4f with the camera
# strictly CLOSER to the player (the punch-in actually moves it), and with the
# player's physics frozen (Motion.cs idiom, bit-still base) the camera returns
# BIT-EQUAL to its pre-trigger transform — not a tolerance, a bit comparison
# (a delta-time decay or a base-writing pulse lands RED here; planted-bad RED
# pre-proof in evidence .audits/*-s20/). Quiet boot + DIRECT bus emit (the
# shipped consumer-test idiom, JUICE_SHAKE BossFallen precedent). Stage 100,
# proof partial RuntimeIntegrationProof.KillPulse.cs; the camera prints ONLY its boot line.
run_mode CAMERA_KILL_PULSE pass "CAMERA_PULSE_AT_BASE"
LOGKP="$(LA_GATE_MODE=CAMERA_KILL_PULSE timeout 300 "$GODOT" --headless --path "$PROJ" --script "$PROOF" 2>&1)" || true
# S23 strength asserts (MC 10229, DA S20 residual P2) ride this SAME leg (no
# new run_mode): CAMERA_PULSE_DIR asserts the punch kick vector lies ON the
# player->camera sight-line and opposes it (a wrong-direction or off-axis
# punch REDs even while strictly closer); CAMERA_PULSE_WINDOW_8 pins the
# measured kick-envelope length to the PulseFrames=8 LITERAL; CAMERA_PULSE_
# PUNCH_075 pins the measured peak displacement to PunchMetres=0.75 within
# float tolerance (a gutted preset REDs — planted-bad evidence, card 10229).
for m in CAMERA_PULSE_ACTIVE CAMERA_PULSE_HELD CAMERA_PULSE_DIR CAMERA_PULSE_WINDOW_8 CAMERA_PULSE_PUNCH_075; do
  [[ "$LOGKP" == *"$m"* ]] || fail "CAMERA_KILL_PULSE: expected $m"
done

# (R) MC 10217 / 10026.35 Inc-4 S20 BOSS FRAMING: the camera READS the shipped
# boss-threshold state (WorldDirector.HasLiveBoss — presentation authority:
# zero Bus/GameState writes, zero new signals/save fields) and eases its
# pull-back/height PRESET in on an INTEGER progress clock (BOSS_FRAME_ENTER,
# then BOSS_FRAME_HELD at full progress with the camera verifiably PULLED BACK
# from the framed player). A REAL-wire boss kill (the only kill past the
# threshold — OnDnaExtracted's dead-boss early-return keeps the SpawnSet
# steady) flips the state back and the clock eases out; every presentation
# window (framing/shake/pulse) closing leaves the camera BIT-EXACTLY on its
# event-free follow base (BOSS_FRAME_AT_BASE). Farm+travel ride the shipped
# ZoneBossProof boss_phase idioms. Stage 101, proof partial
# RuntimeIntegrationProof.BossFrame.cs; rides the QuestFrameBudget.
run_mode BOSS_FRAME pass "BOSS_FRAME_AT_BASE"
LOGBF="$(LA_GATE_MODE=BOSS_FRAME timeout 300 "$GODOT" --headless --path "$PROJ" --script "$PROOF" 2>&1)" || true
# S23 (MC 10229): BOSS_FRAME_CONSTANTS additionally MEASURES the framing
# preset delta at progress==18 (player lifted clear + physics frozen, so the
# follow base sits on its exact lerp fixed point) and pins it to the shipped
# PullBack=3.0 / Height=1.5 literals within float tolerance — the older
# "dist > atLive + 1.0" check alone stayed GREEN for a gutted preset.
for m in BOSS_FRAME_ENTER BOSS_FRAME_HELD BOSS_FRAME_CONSTANTS BOSS_FRAME_KILLED; do
  [[ "$LOGBF" == *"$m"* ]] || fail "BOSS_FRAME: expected $m"
done

# S45 CAMERA RIG-MODE (MC 11018 / 10026.44.6), FLIPPED by MC 11189: the ONE
# FollowCamera carries a rig-mode switch behind the SESSION-ONLY
# `--classic-cam` cmdline USER flag, DEFAULT FIXED ISO (owner ruling
# verbatim "Iso," — PoE/D2 ground-dominated iso; view basis
# design-lab/camera-ref/VIEW-REFERENCE.md. The "Toggle" ruling MC 10995
# append 9 STANDS as mechanism: both rigs stay launchable per session,
# session-only; only the default the owner named earlier is replaced by his
# own later ruling). The flag (game channel) and LA_RIG_EXPECT (leg channel)
# are SEPARATE spawns' inputs, so neither proves the other. LEG 1 (this
# run_mode, NO flag, NO LA_RIG_EXPECT): the boot rig must be FIXED ISO —
# THE shipped default (CAMERA_RIG_DEFAULT_ISO) — pinned live through the
# whole fixed chain (zero-lag BIT-EQUAL rebase CAMERA_RIG_EXACT_REBASE +
# locked orientation + pulse + clearance). LEG 2 (custom spawn,
# LA_RIG_EXPECT=fixed, NO flag): the SAME fixed chain re-witnessed with the
# leg-channel expectation explicit, plus the owner-facing boot print grep
# (LA_PLAYTEST_RIG=ISO). LEG 3 (custom spawn, flag `--classic-cam` +
# LA_RIG_EXPECT=follow): the follow rig with its lerp lag ALIVE
# (CAMERA_RIG_CLASSIC_ON) plus the boot print grep (LA_PLAYTEST_RIG=CLASSIC).
# Quiet boot, the camera prints ONLY its boot rig line (F4-CMP). Stage 107,
# partial RuntimeIntegrationProof.CameraRig.cs. Planted-bad RED pair in
# evidence.
run_mode CAMERA_RIG_ISO pass "CAMERA_RIG_DEFAULT_ISO"
LOGCRIF="$(LA_RIG_EXPECT=fixed LA_GATE_MODE=CAMERA_RIG_ISO timeout 300 "$GODOT" --headless --path "$PROJ" --script "$PROOF" 2>&1)" || true
printf '%s\n' "$LOGCRIF"
[[ "$LOGCRIF" == *"$PASS_MARKER"* ]] || fail "CAMERA_RIG_ISO (fixed): the default (unflagged) spawn did not PASS — default ISO broken? (LA_GATE: PASS missing)"
for m in CAMERA_RIG_BOOT CAMERA_RIG_EXACT_REBASE CAMERA_RIG_ROT_LOCKED CAMERA_RIG_PULSE_ARMED CAMERA_RIG_PULSE_AT_BASE CAMERA_RIG_OCCL_CLAMPED CAMERA_RIG_OCCL_RELEASED; do
  [[ "$LOGCRIF" == *"$m"* ]] || fail "CAMERA_RIG_ISO (fixed): expected $m"
done
# MC 11129 FIX-ROUND-1 (DA P1-GATE), PAIR FLIPPED IN PLACE by MC 11189: the
# OWNER-FACING boot rig line is GATED on BOTH sides of this pair — the one
# user-visible line that names which rig the owner played must not be
# deletable without a red check. UNFLAGGED (DEFAULT ISO) side: the SAME
# LOGCRIF capture the seven-marker loop above reads must also carry
# LA_PLAYTEST_RIG=ISO. FLAGGED (--classic-cam) side: a re-spawn under the
# established capture-and-grep idiom (LOGP/LOGS/LOGBF — proof invocations
# already exceed the leg count by design) must carry LA_PLAYTEST_RIG=CLASSIC.
# NO run_mode line is added: the count 45 = 35 + 10 AT THAT HEAD (superseded — 48 = 37 + 11 by grep at this tip), no mode is added.
# Red-capability: deleting the FollowCamera boot print takes the FIRST
# assert below RED BY NAME (PLAYTEST_RIG_MARKER_
# MISSING) — planted transcript + green revert pair kept in
# .audits/202610231652-iso11189/out/, engine REBUILT between plant and run
# (the S45 stale-assembly lesson: an unstamped assembly keeps dead green).
[[ "$LOGCRIF" == *'LA_PLAYTEST_RIG=ISO'* ]] \
  || fail "CAMERA_RIG_ISO (default): PLAYTEST_RIG_MARKER_MISSING — the unflagged spawn printed no LA_PLAYTEST_RIG=ISO boot line (the owner-facing rig name is gone — MC 11129 P1-GATE pair, flipped by MC 11189)"
LOGCRID="$(LA_RIG_EXPECT=follow LA_GATE_MODE=CAMERA_RIG_ISO timeout 300 "$GODOT" --headless --path "$PROJ" --script "$PROOF" -- --classic-cam 2>&1)" || true
printf '%s\n' "$LOGCRID"
[[ "$LOGCRID" == *'LA_PLAYTEST_RIG=CLASSIC'* ]] \
  || fail "CAMERA_RIG_ISO (classic): PLAYTEST_RIG_MARKER_MISSING — the --classic-cam spawn printed no LA_PLAYTEST_RIG=CLASSIC boot line (MC 11129 P1-GATE pair, flipped by MC 11189)"


# (Q) MC 10216 / 10026.34 Inc-4 S18 ZONE FOUR + ENEMY FOUR: the EXISTING travel
# seam cycles 4 zones on real input — meadow -> canyon -> ruins -> hollow and the
# shipped modulo WRAPS hollow -> meadow (ZONE4_TRAVEL / ZONE4_WRAP); the 4th
# denizen table fields exactly 6 enemy-four Wraiths (ZONE4_TABLE); the composed
# wraith silhouette counts EXACTLY 9 MeshInstance3D through the Lean subtree —
# the BridgeMvpProof manual walk, never GetChildren(bool) (ZONE4_ENEMY4); the
# wraith rig's phase advances EXACTLY +1 mod its per-type cycle 14 per physics
# tick incl. one wrap — a delta-time phase lands RED (ZONE4_GAIT_INT, planted and
# evidence .audits/*s18*); the Hollow keyframe table reads BIT-EXACT at the four
# named anchors on the shipped Environment + DirectionalLight3D with
# fog_sky_affect READ 0.0 at every one (ZONE4_ANCHOR_* / ZONE4_FOGPIN_0 — the
# driver write-path plant lands RED there too) and the four noon rows are
# pairwise distinct (ZONE4_KEY4_DISTINCT). Runtime-only, zero save delta, census
# unchanged (15). Stage 100, proof partial RuntimeIntegrationProof.Zone4.cs.
run_mode ZONE4 pass "ZONE4_WRAP"
# MC 11243 (F5+ZONE4, sky-wall appends 1-4): the marker capture was wrapped in
# `|| true` — RC swallowed, no wall-clock: a timeout kill or a crash-on-quit
# could be silently absorbed while the run_mode half stayed green, and a
# full-battery load death at the proof's own sub-leg budget surfaced as a
# missing marker (wrong name). The capture result is now NAMED: a marker line
# prints its rc + elapsed wall-clock (ZONE4_CAPTURE_RC=<rc>), budget kills
# (timeout 124 / SIGKILL 137 / SIGTERM 143) and the proof's own
# 'budget exhausted' FAIL go RED LOUDLY as BUDGET-DIED (never 'pass'), any
# other non-zero rc fails BY NAME with its LA_GATE: FAIL lines (rc non-zero is
# RED even if every marker printed — the absorption close), and the marker
# greps assert CONSISTENCY with that rc: markers missing at rc 0 keep the
# pre-existing named 'expected $m' RED. All twelve green marker greps unchanged.
Z4RC=0; Z4T0=$SECONDS
LOGZ4="$(LA_GATE_MODE=ZONE4 timeout 300 "$GODOT" --headless --path "$PROJ" --script "$PROOF" 2>&1)" || Z4RC=$?
Z4ELAPSED=$((SECONDS - Z4T0))
echo "RUNTIME_INTEGRATION_TEST: ZONE4_CAPTURE_RC=$Z4RC elapsed=${Z4ELAPSED}s"
if [[ $Z4RC -eq 124 || $Z4RC -eq 137 || $Z4RC -eq 143 ]] \
   || grep -qE 'ZONE4: sub-leg [0-9]+ budget exhausted' <<<"$LOGZ4"; then
  printf '%s\n' "$LOGZ4" | tail -30 >&2
  fail "ZONE4: BUDGET-DIED — the marker capture died at rc=$Z4RC after ${Z4ELAPSED}s (the 300 s timeout kill or the proof's own sub-leg budget): a time-budget death is named at THIS leg, loud — not a pass, and never a later leg's fault (MC 11243, sky-wall append 4 misattribution class)"
fi
if [[ $Z4RC -ne 0 ]]; then
  printf '%s\n' "$LOGZ4" | grep -E '^LA_GATE: (FAIL|PASS)' >&2
  printf '%s\n' "$LOGZ4" | tail -30 >&2
  fail "ZONE4: the marker capture exited $Z4RC after ${Z4ELAPSED}s — named FAIL lines above; the capture RC is NAMED, never swallowed (a non-zero rc is RED even with every marker present — MC 11243, sky-wall append 3 absorption class)"
fi
for m in ZONE4_TRANSIT_CANYON ZONE4_TRANSIT_RUINS ZONE4_TRAVEL ZONE4_TABLE ZONE4_ENEMY4 ZONE4_GAIT_INT ZONE4_ANCHOR_DAWN ZONE4_ANCHOR_NOON ZONE4_ANCHOR_DUSK ZONE4_ANCHOR_NIGHT ZONE4_FOGPIN_0 ZONE4_KEY4_DISTINCT; do
  [[ "$LOGZ4" == *"$m"* ]] || fail "ZONE4: expected $m (S18 zone/enemy/keyframe/gait leg)"
done

# (Q2) MC 10519 / 10026.35 S27 ZONE-SCENE MOUNTING: travel changes what you SEE,
# not only what spawns. Boot ENTERZONE now mounts the zone's own scene (main.tscn
# ships NO static zone instance any more) and every real travel press swaps the
# mount — so the ENV identity under World/<Zone>/Environment is a per-zone node
# set, and the day-night driver's one-shot binding to World/Meadow/* cannot be
# allowed to outlive the node it bound. Leg (quiet boot, bus_emit idiom; it
# re-arms spawning before its first press so each entered zone fields its own
# SpawnSet): the mounted root IS World/<Zone> and the previous root is GONE
# (ZONE_MOUNT_MOUNTED); the SpawnSet survives every swap (ZONE_MOUNT_SPAWNSET);
# EXACTLY ONE current camera and it is the FollowCamera after every mount
# (ZONE_MOUNT_CAMERA_ONE — canyon/ruins shipped Camera3D current=true, the
# W5-DA F-1 dual-current class, data-fixed to false in this same commit); the
# NODE-IDENTITY CENSUS of World/<Zone>/Environment prints BEFORE and AFTER every
# travel with counts + Name#instance-id ids (ZONE_MOUNT_CENSUS), the id sets are
# DISJOINT across the swap — nothing survives the unmount (ZONE_MOUNT_CENSUS_
# DISJOINT) — and hollow's census is DISTINCT from the other three zones by
# node count (ZONE_MOUNT_HOLLOW_DISTINCT: hollow.tscn is new in this commit and
# its Environment subtree carries its own identity, not meadow's). The stale-
# binding hazard closes RE-BIND ON MOUNT: across every mount boundary the leg
# PARKS (>= the 0.5 s decor-cull ride + the deferred navmesh bake) then pauses
# the clock and READS the mounted light + Environment BIT-EXACT against
# DayNightClock at the applied frame (ZONE_MOUNT_DRIVER_*; the S15 read idiom).
# A save-in-hollow -> travel-to-meadow -> LOAD walk must re-mount the SAVED zone
# (ZONE_MOUNT_LOAD_HOLLOW, zero save delta — ZoneId already rides v3). Finally
# the leg RENDERS the hollow.png per-zone census plate and asserts its ambient +
# fog rows pairwise-distinct before saving (ZONE_MOUNT_CAPTURE; the PNG is
# checked non-trivial BELOW, an evidence row — not a pixel gate).
run_mode ZONE_VISUAL_MOUNT pass "ZONE_MOUNT_LOAD_HOLLOW"
# MC 11243 F3 (defense in depth BESIDE the proof-side SavePng==Ok + bytes>100
# gating, not instead of it): pre-spawn rm of the capture target — a stale plate
# from a previous run can never satisfy the -s check below if THIS run's write
# is lost (the MC 11152 CWD-mismatch class, diag spawns
# .audits/202610222340-s45playfixwalls/out/diag-mount*.log, is exactly how a
# lost-but-claimed write reaches the -s gate).
rm -f "$PROJ/.tmp/s39/hollow.png"
LOGZM="$(LA_MOUNT_CAPTURE_PATH="$PROJ/.tmp/s39/hollow.png" LA_GATE_MODE=ZONE_VISUAL_MOUNT timeout 300 "$GODOT" --headless --path "$PROJ" --script "$PROOF" 2>&1)" || true
for m in ZONE_MOUNT_MOUNTED ZONE_MOUNT_SPAWNSET ZONE_MOUNT_CAMERA_ONE ZONE_MOUNT_CENSUS_DISJOINT ZONE_MOUNT_HOLLOW_DISTINCT ZONE_MOUNT_DRIVER_MEADOW ZONE_MOUNT_DRIVER_CANYON ZONE_MOUNT_DRIVER_RUINS ZONE_MOUNT_DRIVER_HOLLOW ZONE_MOUNT_PRELOAD_MEADOW ZONE_MOUNT_CAPTURE; do
  [[ "$LOGZM" == *"$m"* ]] || fail "ZONE_VISUAL_MOUNT: expected $m (S27 mount/census/camera/driver/load leg)"
done
# per-zone ENV identity census MUST print for all four zones (travel VISIBLY
# changes ENV identity — a meadow-only log means the mount never moved).
for z in meadow canyon ruins hollow; do
  [[ "$LOGZM" == *"ZONE_MOUNT_CENSUS zone=$z "* ]] \
    || fail "ZONE_VISUAL_MOUNT: no ENV census for zone=$z (mounted Environment never seen)"
done
# hollow's census must NOT be meadow's: counts differ per zone (ids differ
# per MOUNT — the DISJOINT marker above owns that half).
MCOUNT="$(sed -n 's/.*ZONE_MOUNT_CENSUS zone=meadow .*nodes=\([0-9]*\).*/\1/p' <<<"$LOGZM" | head -1)"
HCOUNT="$(sed -n 's/.*ZONE_MOUNT_CENSUS zone=hollow .*nodes=\([0-9]*\).*/\1/p' <<<"$LOGZM" | head -1)"
[[ -n "$MCOUNT" && -n "$HCOUNT" ]] || fail "ZONE_VISUAL_MOUNT: census node counts unparsable (meadow='$MCOUNT' hollow='$HCOUNT')"
[[ "$MCOUNT" != "$HCOUNT" ]] || fail "ZONE_VISUAL_MOUNT: hollow Environment census count ($HCOUNT) == meadow's ($MCOUNT) — zone ENV identity not visibly distinct"
# the plate is an evidence row: present + non-trivial on disk (no pixel gate).
[[ -s "$PROJ/.tmp/s39/hollow.png" ]] \
  || fail "ZONE_VISUAL_MOUNT: hollow.png census plate missing/empty at $PROJ/.tmp/s39/hollow.png"

# ---- GROUND_TEX_WIRING (MC 11037 / 10026.44.8) — the ground-plate wiring gate.
# Rides the Q2 mount capture ABOVE (LOGZM): zero new run_mode, zero new spawn —
# the four wired entries mount in THIS capture (the census greps above prove
# every zone mounted; an unmounted zone cannot keep this leg silent). WHY: the
# TEST wall 2026-10-20 planted-fault finding — a wrong ground_texture_path in
# canyon.tscn logged a LOUD engine ERROR (terrain_builder.gd:226 load) but the
# scene BOOTED, exit 0, and NO battery leg failed: a mis-pathed plate can ship
# WHITE GROUND through exit-code gates. THREE halves, one named verdict (the S41
# material-readback idiom + the Q2 shell-side compare idiom + the LOGP capture-
# and-grep idiom): (1) the wired path is parsed from each zone's OWN tscn (a
# tscn edit is this gate's INPUT) and must name a file present on disk; (2) the
# proof's read-back line ZONE_MOUNT_GROUNDTEX must name the SAME path and read
# painted=1 — painted=0 means the built TerrainVisual carries NO albedo plate;
# (3) ZERO engine 'Resource file not found' lines: per wired path (a plant's
# path IS the parsed path — attribution per zone) AND capture-wide.
GT_FAIL=""
for z in meadow canyon ruins hollow; do
  TP="$(sed -n 's/^[[:space:]]*ground_texture_path = "\(.*\)"$/\1/p' "$PROJ/zones/$z/$z.tscn" | head -1)"
  [[ -n "$TP" ]] || GT_FAIL="$GT_FAIL zones/$z/$z.tscn: wired ground_texture_path empty or unparsable;"
  [[ "$TP" == res://* ]] || GT_FAIL="$GT_FAIL zones/$z/$z.tscn: wired path is not a res:// path ('$TP');"
  [[ -n "$TP" && -s "$PROJ/${TP#res://}" ]] || GT_FAIL="$GT_FAIL plate file missing/empty on disk: $TP;"
  LINE="$(grep -m1 "^LA_GATE: ZONE_MOUNT_GROUNDTEX zone=$z " <<<"$LOGZM")" \
    || GT_FAIL="$GT_FAIL no ZONE_MOUNT_GROUNDTEX readback for zone=$z in the mount capture (the mount never read its plate back);"
  [[ -n "$LINE" && "$LINE" == *" tex=$TP painted="* ]] \
    || GT_FAIL="$GT_FAIL zone=$z readback names a DIFFERENT path than the tscn wires ('$TP' vs '${LINE:-<none>}');"
  [[ "$LINE" == *" painted=1"* ]] \
    || GT_FAIL="$GT_FAIL zone=$z plate NOT painted (white ground — the built TerrainVisual carries no albedo plate for '$TP');"
  GT_ERRP="$(grep -cF "Resource file not found: $TP" <<<"$LOGZM" || true)"
  [[ "${GT_ERRP:-0}" -eq 0 ]] \
    || GT_FAIL="$GT_FAIL zone=$z wired plate '$TP' failed the ENGINE load ($GT_ERRP 'Resource file not found' line(s) name it);"
done
GT_ERRS="$(grep -c '^ERROR: Resource file not found: ' <<<"$LOGZM" || true)"
[[ "${GT_ERRS:-0}" -eq 0 ]] \
  || GT_FAIL="$GT_FAIL $GT_ERRS engine 'Resource file not found' line(s) in the mount capture (exit 0 shipped them — named below);"
[[ -z "$GT_FAIL" ]] || { printf '%s\n' "GROUND_TEX_WIRING:$GT_FAIL" >&2
  grep '^ERROR: Resource file not found: ' <<<"$LOGZM" | sed 's/^/  capture: /' >&2
  fail "GROUND_TEX_WIRING: ground-plate wiring gate RED (wired tscn paths vs on-disk plates vs proof readback vs engine load errors; capture = the Q2 ZONE_VISUAL_MOUNT mount above)"; }
echo "RUNTIME_INTEGRATION_TEST: GROUND_TEX_WIRING OK — 4/4 wired ground plates on disk + read back PAINTED through the mount capture + zero engine 'Resource file not found' lines (TEST wall 2026-10-20 white-ground class closed)"

# (S) MC 10131 / 10026.x Inc-4 S8 COMMAND BARK: the bark command PURELY as
# presentation on the live scene — ONE real ActionPress("bark") per roster shape
# (the one input-map change, key G). Roster 1 (boot seed) and 3 (planted OWNED
# Followers restored through the REAL LoadGame rebuild): the walked count of
# VISIBLE "BarkBubble" nodes is EXACTLY the roster size at open and while held,
# the per-body integer counter steps EXACTLY -1 per frame (the delta-time feed
# lands RED, F4 — planted in evidence .audits/*-s8/) and the bubbles are gone on
# the EXACT end frame open+V. The EMPTY roster (EMPTY planted list — the product
# rebuild that frees bodies with the roster) presses to ZERO ghost bubbles.
# Every press: zero gameplay delta (hp/Manna/DNA/position/roster/loyalty,
# physics-frozen bit-still). Census re-grepped IN-LEG: the bus source read at
# runtime still holds EXACTLY 15 signals. S24 follow-up (MC 10238, S8 DA wall
# F1+F2): the EMPTY-roster zero-walk is floored ABOVE the settle so EVERY
# frame of window+margin is actually walked (the walk count is asserted), and
# a fresh press followed by LoadGame INSIDE the window must leave the REUSED
# bodies window-free — zero walked bubbles post-load (BARK_LOAD_CLEAR, the
# load-seam bark clear mirroring the calm clear). Stage 102, proof partial
# RuntimeIntegrationProof.Bark.cs.
run_mode BARK pass "BARK_CENSUS_15"
LOGBK="$(LA_GATE_MODE=BARK timeout 300 "$GODOT" --headless --path "$PROJ" --script "$PROOF" 2>&1)" || true
for m in BARK_ROSTER_ONE BARK_OPEN_1 BARK_STEP BARK_GONE_1 BARK_NO_GAMEPLAY_DELTA BARK_ROSTER_THREE BARK_OPEN_3 BARK_GONE_3 BARK_LOAD_CLEAR BARK_ROSTER_EMPTY BARK_EMPTY_ZERO BARK_SAVE_OWNED; do
  [[ "$LOGBK" == *"$m"* ]] || fail "BARK: expected $m (S8 command-bark leg)"
done

# (T) MC 10273 / 10026.23.6 Inc-4 S19 STORY ACT THREE (the close; RULING-8,
# board line MC 10026 append #9 row 895: "R8 zone four + act three = YES ->
# S18 + S19 proceed", owner chat "Rec on all."): the four zone-4
# QuestTable.ActThreeArc() rows played
# by a THIRD pure QuestLog on the SHIPPED S10 act mechanism, on the live
# scene. The act opens through the REAL save path (owned save: act one + act
# two diverged all-complete -> SaveGame -> LoadGame; act two ADOPTS silently,
# act three FreshOpens — ACT3_OPENED) with the table-driven open card painted
# as a guarded beat (ACT3_CARD_ON_SCREEN). Four completions each off its OWN
# distinct fact (W6 pin): three real travels onto the S18-shipped "hollow"
# complete q_h_arrival (ACT3_ARRIVAL), three real speaks complete q_h_tongue
# (ACT3_TONGUE), the ONE wage path attributes q_h_wage across THREE logs
# (ACT3_WAGE), eight extractions complete the FINAL ZONE-4 CHAIN
# (ACT3_RECKONING) — where the wiring's act-completion condition prints the
# named ACT_THREE_COMPLETE marker and the close card drains in emission order
# behind the finale reward (ACT3_CLOSED). The mid-quest save round-trip rides
# the SHIPPED v3 QuestStates wire — zero save fields, no card replay on load
# (ACT3_PERSIST). Restore decision arms: the pure ActChainSync machine
# (ActTwoSync renamed — (prevAct, thisAct)-parameterized; S19 rides it over
# (act2, act3)). Zero new bus signals (census 15), zero scene edits. Stage
# 103 (UNIQUE — the pairing gate), proof partial RuntimeIntegrationProof.Story3.cs.
run_mode ACT_THREE pass "ACT3_CLOSED"
LOGA3="$(LA_GATE_MODE=ACT_THREE timeout 300 "$GODOT" --headless --path "$PROJ" --script "$PROOF" 2>&1)" || true
[[ "$LOGA3" == *'ACT3_OPENED'* ]] || fail "ACT_THREE: ACT3_OPENED missing (restore-sync open edge broken)"
[[ "$LOGA3" == *'ACT3_CARD_ON_SCREEN'* ]] || fail "ACT_THREE: ACT3_CARD_ON_SCREEN missing (open card never painted)"
[[ "$LOGA3" == *'ACT_CARD act_three open'* ]] || fail "ACT_THREE: act_three open ACT_CARD marker missing (W5 emission leg)"
for q in q_h_arrival q_h_tongue q_h_wage q_h_reckoning; do
  [[ "$LOGA3" == *"QUEST_COMPLETED $q"* ]] || fail "ACT_THREE: QUEST_COMPLETED $q missing (W6 pin: four completions, four separate drives)"
  [[ "$LOGA3" == *"REWARD_SHOWN $q"* ]] || fail "ACT_THREE: REWARD_SHOWN $q missing (per-row reward state truth)"
done
[[ "$LOGA3" == *'WAGE_PAID for q_h_wage'* ]] || fail "ACT_THREE: WAGE_PAID q_h_wage missing (three-log attribution broken)"
[[ "$LOGA3" == *'ACT3_PERSIST'* ]] || fail "ACT_THREE: ACT3_PERSIST missing (act-three rows do not ride the shipped save wire)"
[[ "$LOGA3" == *'ACT_THREE_COMPLETE'* ]] || fail "ACT_THREE: ACT_THREE_COMPLETE missing (the named act-completion marker never fired)"
[[ "$LOGA3" == *'ACT_CARD act_three close'* ]] || fail "ACT_THREE: act_three close ACT_CARD marker missing (W5 emission leg)"
[[ "$LOGA3" == *'ACT3_CLOSED'* ]] || fail "ACT_THREE: ACT3_CLOSED missing (the close card never drained in order behind the finale)"

# (U) MC 10404 FLOOR COVERAGE (owner playtest P1: fell through the visible
# ground and died below it — no collider where the rendered floor was): the
# dense grid probe boots each zones/ scene ALONE (no main.tscn — the probe
# space must hold only the zone under test) and casts a downward ray at every
# 2 m of the visual ground's extent; every column must return solid within the
# terrain's own height range + 2 m. Zero uncovered points is the assert; the
# per-zone hit-minus-terrain-bottom stats print as diagnostics. The two decor-
# only test scenes must be NAMED as skipped (never silently absent).
# MC 10026.31(c): the leg ALSO asserts height-match — on an every-8th-point
# subgrid the ray's hit_y must sit within 0.5 m of the visual surface height
# recomputed through terrain_builder's own world<->pixel mapping (existence of
# a floor was NOT enough: the pre-fix collider floated +0.82 m over the meadow
# ground with full coverage). Marker FLOOR_HEIGHT_CLEAR is grepped below.
PROOF_FLOOR="res://ci_proofs/FloorCoverageProof.cs"
[ -f "$PROJ/ci_proofs/FloorCoverageProof.cs" ] || fail "FloorCoverageProof.cs not found"
run_mode FLOOR_COVERAGE pass "FLOOR_COVERAGE_CLEAR" "$PROOF_FLOOR"
LOGFC="$(LA_GATE_MODE=FLOOR_COVERAGE timeout 300 "$GODOT" --headless --path "$PROJ" --script "$PROOF_FLOOR" 2>&1)" || true
for zn in meadow canyon ruins; do
  [[ "$LOGFC" == *"FLOOR_COVERAGE res://zones/$zn/"* ]] \
    || fail "FLOOR_COVERAGE: probe line for zones/$zn missing (terrain ground absent from the shipped set?)"
done
for sk in bluetest/blue redtest/red; do
  [[ "$LOGFC" == *"FLOOR_COVERAGE skip res://zones/$sk.tscn"* ]] \
    || fail "FLOOR_COVERAGE: named skip line for zones/$sk.tscn missing (decor-only scenes are disclosed, not silently skipped)"
done
[[ "$LOGFC" == *'FLOOR_HEIGHT_CLEAR'* ]] \
  || fail "FLOOR_COVERAGE: FLOOR_HEIGHT_CLEAR missing (MC 10026.31(c) height-match leg — |hit_y-visual_y| < 0.5 m on the every-8th-point subgrid)"

# (V) MC 10404 FALL RECOVERY: the general guarantee — a player below the
# loaded ground's visual floor (the fall-through state, forced at y=-30 and
# +40/+40 OFF the entry column in the proof — MC 10026.31(a): the drop no
# longer lands on the player's own home X/Z, so the byte-exact recovery-to-
# entry assert is a real ~57 m traversal, not a tautology) is restored by the
# director's clamp to the zone-entry column, Life UNCHANGED, no re-fall loop
# (P1FixProof stage 50/51). The product line
# FALL_RECOVERED is grepped here in the second run.
run_mode fall_recovered pass "FALL_RECOVERED_EXACT" "$PROOF_P1"
LOGFR="$(LA_GATE_MODE=fall_recovered timeout 300 "$GODOT" --headless --path "$PROJ" --script "$PROOF_P1" 2>&1)" || true
[[ "$LOGFR" == *'FALL_RECOVERED from'* ]] \
  || fail "fall_recovered: the product FALL_RECOVERED guarantee line never printed (clamp missing or below-plane never detected)"
[[ "$LOGFR" == *'KILL_PLANE_RESCUE zone=meadow reason=below-floor-plane'* ]] \
  || fail "fall_recovered: the below-plane rescue printed WITHOUT the MC 10516/S38 named kill-plane marker (zone + reason on the guarantee event — never silent)"

# (W) MC 10420 / 10026.29 GROUND-EXTENT MOTION CLAMP: unbounded enemy MOTION is
# the x=66.69 fall class — EnemyAI chase/patrol/flee planned WITHOUT a ground
# bound and the calm_use choreography co-located the player onto such a position
# (the census pair: 2 FALL_RECOVERED behind state-only asserts). The clamp
# resolves from the terrain "ground_extent" meta (ONE channel with MC 10404's
# floor_min_y) and binds enemy motion + wild co-locations to the loaded ground.
# This leg drives a REAL spawned enemy's own flee state at the meadow edge,
# asserts it PINS there (30 frames under continued pressure — never vacuously
# short), sweeps EVERY enemy/wild/FOLLOWER position against the extent every
# frame (a beyond-extent position fails the leg NAMING its coordinates — the
# planted +100 m clamp widening walks into it, pair logs .tmp/close-10420/),
# lands a wild co-location requested 2.5 m past the edge ON the ground, and
# (CYCLE 2, DA wall F-DA1) RECRUITS that edge wild through the production wire
# and drives follow pressure to the edge: the recruited follower must PIN at
# the extent (FOLLOWER_EDGEHOVER — the DA lure class: the follow lerp was the
# one position writer NOT reading the extent, and recruited bodies live in
# _bodies/FollowerBodies, outside the two lists cycle 1 swept), while the
# PLAYER stands unclamped 0.5 m PAST the extent untouched (PLAYER_UNCLAMPED —
# rescue-not-prevention; the probe stands on the collider overshoot, never
# falls, the census stays 1/1).
run_mode GROUND_CLAMP pass "GROUND_CLAMP_CLEAR"
LOGGC="$(LA_GATE_MODE=GROUND_CLAMP timeout 240 "$GODOT" --headless --path "$PROJ" --script "$PROOF" 2>&1)" || true
[[ "$LOGGC" == *'GROUND_CLAMP_AABB'* ]] \
  || fail "GROUND_CLAMP: the meta<->visible-AABB cross-check missing (the clamp box must BE the visible ground — FloorCoverageProof's world<->extent knowledge, reused)"
[[ "$LOGGC" == *'ENEMY_GROUNDPIN'* ]] \
  || fail "GROUND_CLAMP: the driven enemy never pinned at the ground edge (clamp not biting, or the drive bypassed it)"
[[ "$LOGGC" == *'WILD_SPAWN_CLAMPED'* ]] \
  || fail "GROUND_CLAMP: the off-edge wild co-location did not land ON the ground (the calm choreography's mechanism is unclamped)"
[[ "$LOGGC" == *'FOLLOWER_EDGEHOVER'* ]] \
  || fail "GROUND_CLAMP: the RECRUIT-THEN-EDGE-HOVER case never pinned a recruited follower at the edge (F-DA1: the follow clamp is not biting — the floating-companion lure is live)"
[[ "$LOGGC" == *'PLAYER_UNCLAMPED'* ]] \
  || fail "GROUND_CLAMP: the player-unclamped probe never completed (the ground clamp must bind companions, NEVER the player — the rescue keeps its owner-fall-through purpose)"
[[ "$LOGGC" == *'GROUND_CLAMP_SWEEP'* ]] \
  || fail "GROUND_CLAMP: the every-frame beyond-extent sweep never completed"
[[ "$LOGGC" != *'FALL_RECOVERED from='* ]] \
  || fail "GROUND_CLAMP: someone went below the plane INSIDE the clamp leg — the bound leaked (the census tail would also red)"

# MC 10420 DoD 2 (reviewer R5 P2, the calm_use masking): with the clamp in, the
# calm leg can no longer place anyone off the ground — ZERO FALL_RECOVERED
# inside calm_use (baseline 575679e: 2 at from=(66.66,-2.01,-0.26)). Re-grep
# the leg-carrying log from (E-clm) above; the census tail keeps the battery-
# wide EXACT count.
[[ "$LOGCLM" != *'FALL_RECOVERED from='* ]] \
  || fail "calm_use: FALL_RECOVERED inside the calm leg — the guarantee rescued someone BELOW the plane behind this leg's state-only asserts (the R5 P2 masking; MC 10420 must keep every calm co-location ON the ground)"

# (X) MC 10516 / 10026.33 S38 FLOOR TRUST + KILL PLANE (ratified D2 §5) — the
# card's own battery leg: headless physics-query probes, battery idiom, NO GPU.
# ONE mode name on TWO proof classes (class-local modes are dispatched by their
# own entry points — mode_sets_check's doctrine): FLOOR_TRUST @ FloorCoverage-
# Proof boots each zone ALONE and (a) RECORDS the live HeightMapShape3D against
# terrain_builder's own cells formula — the 256-px canyon/ruins resample to
# 514x514 / 412x412 one-metre cells; whether physics took them verbatim is read
# back and printed (FLOOR_TRUST_PROBE256, verdict=OK|CLAMPED; a CLAMPED verdict
# fails the leg: the card's downsample/clamp would be owed there, not here),
# then (b) walks a SCRIPTED map-edge grid with the mover's own contact geometry
# (capsule-FOOTPRINT ray set — center + four offsets at the 0.4 m capsule
# radius, hits filtered to the terrain body; a sphere sweep was the first
# draft but THIS build's IntersectShape/IntersectPoint answer ZERO on
# HeightMapShape3D where rays hit — probe .tmp/s38/query-mechanism-diag.log,
# and the card wording sanctions the ray idiom): the
# border line at 1 m + a 0.75 m inset (the dead-ring class the h_scale
# footprint bug created — visible ground, no floor) + a 0.75 m outset (points
# PAST the visual ground: NOT violations — the runtime answers are the extent
# clamp BLOCKING motion and the kill plane RESCUING + LOGGING fallers; counted,
# printed, never silent). Zero visible-ground-without-collision points inside
# the span is the assert (planted-RED pair: .tmp/s38/). FLOOR_TRUST @
# RuntimeIntegrationProof drives the COMPOSED scene (quiet boot): the same
# sweep, then the player WALKS 8 border waypoints under real gravity and stays
# standing, ClampToGround past the edge BINDS (FLOOR_TRUST_BLOCKED — a silent
# no-op clamp REDs), and a drop 6 m past the east edge — where NO ground
# exists — must be RESCUED to the zone entry byte-exact in XZ, Life intact, no
# re-fall: the kill plane doing exactly what the card demands, LOGGED as
# KILL_PLANE_RESCUE zone=<zone> reason=below-floor-plane at the ONE guarantee
# site. That rescue is the battery's SECOND census hit (fall_recovered + this
# deliberate off-edge fall — the own-leg pair; every other leg keeps ZERO).
run_mode FLOOR_TRUST pass "FLOOR_TRUST_CLEAR" "$PROOF_FLOOR"
LOGFT="$(LA_GATE_MODE=FLOOR_TRUST timeout 300 "$GODOT" --headless --path "$PROJ" --script "$PROOF_FLOOR" 2>&1)" || true
for zn in meadow canyon ruins; do
  [[ "$LOGFT" == *"FLOOR_TRUST res://zones/$zn/"* ]] \
    || fail "FLOOR_TRUST: trust line for zones/$zn missing (terrain ground absent from the shipped set?)"
  [[ "$LOGFT" == *"FLOOR_TRUST_PROBE256 res://zones/$zn/"* ]] \
    || fail "FLOOR_TRUST: the 256-probe record for zones/$zn is missing — the HeightMapShape3D cell-count question stays UNPROBED (S38 records the verdict either way)"
done
for sk in bluetest/blue redtest/red; do
  [[ "$LOGFT" == *"FLOOR_TRUST skip res://zones/$sk.tscn"* ]] \
    || fail "FLOOR_TRUST: named skip line for zones/$sk.tscn missing (decor-only scenes are disclosed, not silently skipped)"
done
run_mode FLOOR_TRUST pass "FLOOR_TRUST_LIVE_CLEAR"
LOGFTL="$(LA_GATE_MODE=FLOOR_TRUST timeout 240 "$GODOT" --headless --path "$PROJ" --script "$PROOF" 2>&1)" || true
[[ "$LOGFTL" == *'FLOOR_TRUST_EDGEWALK'* ]] \
  || fail "FLOOR_TRUST live: the scripted map-edge walk never completed on the composed scene"
[[ "$LOGFTL" == *'FLOOR_TRUST_BLOCKED'* ]] \
  || fail "FLOOR_TRUST live: the past-extent BLOCKED accounting never printed (never-silent: past-edge points are named as clamp-bound/kill-planed, not dropped)"
[[ "$LOGFTL" == *'KILL_PLANE_RESCUE zone=meadow reason=below-floor-plane'* ]] \
  || fail "FLOOR_TRUST live: the OFF-EDGE fall respawned WITHOUT the named kill-plane marker (zone + reason, never silent — S38)"
[[ "$(grep -c 'FALL_RECOVERED from=' <<<"$LOGFTL")" -eq 1 ]] \
  || fail "FLOOR_TRUST live: the guarantee line printed != 1 time inside the edge-fall leg (a re-fall loop behind the settle asserts)"

# (Y) MC 11320 bus_emit RE-CERT — the S0 four-edge bus leg ENTERS the battery
# as a NAMED leg. The mode existed since MC 10098 but never rode the battery
# (dead CI code: not a counted leg, last honest green 2026-10-05; re-certified
# off the MC 11301 findings L1/L2/L3 — the leg header carries the
# dispositions). ONE run_mode leg, NO extra spawn: run_mode prints the whole
# leg transcript into the battery log, and exit 0 + LA_GATE: PASS + the named
# final marker gate it — every earlier edge is asserted IN-LEG and goes RED
# BY NAME (BUS_SHOWN_ONCE / BUS_CLOSED_ONCE / BUS_HURT_ONCE /
# BUS_NO_FALSE_FALLEN / BUS_FALLEN_ONCE / BUS_SWAP_STRIKE / BUS_FALLEN_SWAP /
# BUS_FB_QUIET_ROSTER / BUS_NO_LOAD_HURT). The leg writes the shared save
# exactly like the save legs do (MC 3910 delete-before-write, self-owning);
# it prints ZERO FALL_RECOVERED/KILL_PLANE lines, so the battery-wide census
# stays 2; the leg's own LA_DEATH lines are honest farm deaths (the seam
# revives, zero corpse agency) and the battery has no LA_DEATH census
# (grep-verified at this head).
run_mode bus_emit pass "BUS_NO_LOAD_HURT"

# (S) MC 11019 / 10026.44.7 S48 STEP-0 GL-COMPAT RENDERER CAPABILITY PROBE
# (the FX-code chain gate — ground aura rings, blood decals, torch multilight +
# flame billboards of the d2 visual direction must not be planned against an
# UNMEASURED renderer). The FIRST WINDOWED battery legs: the --headless dummy
# rasterizer answers ZERO to every capability question (it reports
# gl_compatibility but draws nothing — exactly the vacuity this card exists to
# close), while the CI-EFFECTIVE GL is only real under Xvfb+llvmpipe: the engine
# prints "switching to OpenGL 3" + "OpenGL API 4.5 ... - Compatibility -
# ... llvmpipe" there (the CI box has no Vulkan). The leg mounts the three
# mechanisms BESIDE the composed scene, reads pixels through a SubViewport
# probe FBO sharing the live world (the ROOT-viewport texture reads EMPTY at
# this renderer — measured), prints the named verdicts with every measured
# number, and pins the renderer itself (FXGL_RENDERER) — any other method exits
# 1: a verdict at the wrong renderer is vacuous by construction.
# GREEN leg (windowed, mode=FX_GLCOMPAT): QUAD_ALPHA_OK (aura_disc_a ring drawn
# AND its transparent corners respected — the ground-projected-quad mechanism
# the whole FX chain's ground FX rides); MULTILIGHT_2_OK (each shadowless
# OmniLight alone AND both together, measured on shaded 24x24-subdivided
# receivers — gl-compat lights are PER-VERTEX: the leg also MEASURES and prints
# the raw-terrain blindness as FXGL_NOTE, the renderer fact downstream cards
# must plan around); FLAME_BILLBOARD_OK (torch_flame_strip_6f frame 0);
# DECAL_SUPPORT NAMED either way — a FAIL line must carry the CUT-rule fallback
# (blood FX rides quads, no Decal node ships) AND the Forward+ upgrade RE-CHECK
# DUTY (FIX-ROUND-1); DECAL_WINDOW_ALIVE must print — the planted NON-decal
# probe quad proves the decal sample window NON-vacuous (FIX-ROUND-1).
# Captures land in $PROJ/.tmp (S27 idiom) and the disk bytes are checked.
# Zero FALL_RECOVERED in-leg.
# MC 11243 F3 (same class as the Q2 plate above): pre-spawn rm of EVERY capture
# target of this leg — stale capture bytes can never satisfy the -s gates below.
rm -rf "$PROJ/.tmp/s48step0"
FXRC=0
LOGFX="$(LA_FXCAP_PATH="$PROJ/.tmp/s48step0" LA_GATE_MODE=FX_GLCOMPAT xvfb-run -a timeout 300 "$GODOT" --path "$PROJ" --script "$PROOF" 2>&1)" || FXRC=$?
[[ $FXRC -eq 0 ]] || { printf '%s\n' "$LOGFX" | tail -30 >&2; fail "FX_GLCOMPAT: the windowed probe leg exited $FXRC (named FAIL lines above)"; }
[[ "$LOGFX" == *'FXGL_RENDERER method=gl_compatibility'* ]] \
  || fail "FX_GLCOMPAT: the renderer pin line is missing — the leg never confirmed it ran at gl_compatibility (wrong renderer = vacuous verdicts)"
[[ "$LOGFX" == *'QUAD_ALPHA_OK'* ]] \
  || fail "FX_GLCOMPAT: QUAD_ALPHA_OK missing — alpha-blended textured quads are NOT proven at the CI-effective renderer; the FX-code chain ground-FX cards stop here"
if [[ "$LOGFX" == *'DECAL_SUPPORT_FAIL'* ]]; then
  [[ "$LOGFX" == *'fallback=ground-projected QUAD'* ]] \
    || fail "FX_GLCOMPAT: DECAL_SUPPORT named FAIL WITHOUT the CUT-rule fallback line (the state must be named WITH its fallback for the downstream FX cards)"
  [[ "$LOGFX" == *'Forward+'* ]] \
    || fail "FX_GLCOMPAT: DECAL_SUPPORT FAIL without the Forward+ upgrade RE-CHECK DUTY (FX_GLCOMPAT must be re-run before un-retiring the Decal path if Forward+ ever becomes the effective renderer)"
else
  [[ "$LOGFX" == *'DECAL_SUPPORT_OK'* ]] \
    || fail "FX_GLCOMPAT: no DECAL_SUPPORT verdict printed — the decal state must be NAMED for the FX chain either way (legit result: OK, or FAIL + fallback)"
fi
[[ "$LOGFX" == *'MULTILIGHT_2_OK'* ]] \
  || fail "FX_GLCOMPAT: MULTILIGHT_2_OK missing — two shadowless OmniLights are not proven SIMULTANEOUSLY — each ALONE and ADDITIVELY together — at the CI-effective renderer (measured line above)"
[[ "$LOGFX" == *'MULTILIGHT_2 measured'* && "$LOGFX" == *'add='* ]] \
  || fail "FX_GLCOMPAT: the MULTILIGHT_2 measured line lacks the additivity add=(..) deltas (FIX-ROUND-1 gate must print its own margins)"
[[ "$LOGFX" == *'FLAME_BILLBOARD_OK'* ]] \
  || fail "FX_GLCOMPAT: FLAME_BILLBOARD_OK missing — the torch-strip billboard is not proven at the CI-effective renderer"
[[ "$LOGFX" == *'DECAL_WINDOW_ALIVE'* ]] \
  || fail "FX_GLCOMPAT: DECAL_WINDOW_ALIVE missing — the decal sample window was never proven NON-vacuous by the planted NON-decal quad (a DECAL FAIL without it is a dead-window artifact, not an engine answer)"
[[ "$LOGFX" == *'FXGL_PROBE_RESULT quad=OK'* && "$LOGFX" == *'FXGL_NOTE'* ]] \
  || fail "FX_GLCOMPAT: the FXGL_PROBE_RESULT summary or the FXGL_NOTE terrain-light fact line is missing (downstream cards consume these lines)"
[[ "$(grep -c 'FXGL_CAPTURE' <<<"$LOGFX")" -ge 8 ]] \
  || fail "FX_GLCOMPAT: fewer than 8 FXGL_CAPTURE lines — one settled capture per rig state (S27 idiom; FIX-ROUND-1 added the decwin window-probe frame)"
[[ -s "$PROJ/.tmp/s48step0/fxgl_quad.png" && -s "$PROJ/.tmp/s48step0/fxgl_l12f.png" && -s "$PROJ/.tmp/s48step0/fxgl_decwin.png" ]] \
  || fail "FX_GLCOMPAT: capture files not non-empty on disk (the printed bytes claim vs reality)"
[[ "$(grep -c 'FALL_RECOVERED from=' <<<"$LOGFX")" -eq 0 ]] \
  || fail "FX_GLCOMPAT: the probe leg produced FALL_RECOVERED lines (it must move nothing; census discipline MC 10422)"

# RED pair (planted-bad, gate-red law; FIX-ROUND-1 widened the plant to the
# WHOLE rig): the SAME rig with quad/flame/window-probe materials alpha FORCED 0
# AND the 2nd OmniLight REMOVED from the tree — QUAD_ALPHA, MULTILIGHT_2,
# FLAME_BILLBOARD and DECAL_WINDOW must EACH print their named FAIL/DEAD at the
# SAME measurement points and the run MUST exit nonzero. A plant staying green
# fails the leg as VACUOUS by name (FXGL_*_NOT_CAUGHT). The red transcript +
# red captures ride beside the green (evidence names both).
rm -rf "$PROJ/.tmp/s48step0-red"   # MC 11243 F3: pre-spawn rm (same class as above)
FXPRC=0
LOGFXP="$(LA_FXCAP_PATH="$PROJ/.tmp/s48step0-red" LA_GATE_MODE=FX_GLCOMPAT_PLANT xvfb-run -a timeout 300 "$GODOT" --path "$PROJ" --script "$PROOF" 2>&1)" || FXPRC=$?
[[ $FXPRC -ne 0 ]] \
  || { printf '%s\n' "$LOGFXP" | tail -20 >&2; fail "FX_GLCOMPAT_PLANT: the planted-bad leg EXITED GREEN — the checks are VACUOUS (alpha-0 quad/flame/window-probe and a REMOVED 2nd light cannot pass them)"; }
[[ "$LOGFXP" == *'QUAD_ALPHA_FAIL'* && "$LOGFXP" == *'FXGL_PLANT_RED'* ]] \
  || fail "FX_GLCOMPAT_PLANT: exited nonzero WITHOUT the named QUAD_ALPHA_FAIL / FXGL_PLANT_RED lines (a red leg must go red BY NAME)"
[[ "$LOGFXP" == *'MULTILIGHT_2_FAIL'* && "$LOGFXP" == *'FXGL_ML_PLANT_RED'* ]] \
  || fail "FX_GLCOMPAT_PLANT: the planted 2nd-light tree-removal did NOT print MULTILIGHT_2_FAIL / FXGL_ML_PLANT_RED at the same measurement points (the solo+additivity gate is VACUOUS)"
[[ "$LOGFXP" == *'FLAME_BILLBOARD_FAIL'* && "$LOGFXP" == *'FXGL_FLAME_PLANT_RED'* ]] \
  || fail "FX_GLCOMPAT_PLANT: the planted alpha-0 flame did NOT print FLAME_BILLBOARD_FAIL / FXGL_FLAME_PLANT_RED (the flame check is VACUOUS)"
[[ "$LOGFXP" == *'DECAL_WINDOW_DEAD'* && "$LOGFXP" == *'FXGL_DW_PLANT_RED'* ]] \
  || fail "FX_GLCOMPAT_PLANT: the planted alpha-0 window probe did NOT print DECAL_WINDOW_DEAD / FXGL_DW_PLANT_RED (the DECAL non-vacuity proof is VACUOUS)"
if grep -qE '^LA_GATE: (QUAD_ALPHA_OK|MULTILIGHT_2_OK|FLAME_BILLBOARD_OK|DECAL_WINDOW_ALIVE)' <<<"$LOGFXP"; then
  fail "FX_GLCOMPAT_PLANT: a planted-bad leg printed a verdict OK/ALIVE LINE — verdict collision (anchored to '^LA_GATE: <MARKER>' because the FXGL_PLANT armed line NAMES every verdict — a whole-log substring check false-collides with it)"
fi

# SKY_STRIP (MC 11088 / 10026.44.10 S48 BATCH-4 FIX-ROUND-1, DA F1 — the card
# DoD NAMES this leg; the ship round deferred it on a quote not on the board,
# and the deferral is CLOSED here; prose restamped by the MC 11152 MICRO-FIX,
# the MC 11113 builder's named handback): AT THIS HEAD a HARNESS-SIDE render
# PROBE — the leg mounts the shipped sky strip
# resources/generated/sky/canyon_sky_v1.png on its OWN unshaded native-4:1
# quad BESIDE the composed scene (the FxGlcompat rig idiom; SubViewport FBO
# readback — the root window reads EMPTY at llvmpipe) and MEASURES it PAINTED
# at expected screen points RUN-derived from the shipped bytes (the
# FxMaxAlphaUv law) AND MAPPED AS AUTHORED (each painted patch nearer to its
# OWN asset block color than to any other sample's — a painted-something
# generic quad cannot pass). GREEN leg (windowed, mode=SKY_STRIP) greps, all
# off one capture: renderer pin FXSKY_RENDERER method=gl_compatibility,
# verdict SKY_STRIP_RENDER_OK, the per-point/summary 'SKY_STRIP measured'
# margins, >=2 FXSKY_CAPTURE lines, both capture PNGs (skybase/skyon)
# non-empty on disk, in-leg ZERO FALL_RECOVERED. PENDING, named honestly:
# the real product mount — MC 11113/S50-SKY option-(b) ruling (canyon.tscn's
# SkyStrip backdrop on the ONE WorldMount route, procedural sky KEPT;
# docs/ARCHITECTURE.md §3 "Sky mechanism") — landed ON BRANCH la/11113 with
# this proof leg upgraded IN PLACE to measure THAT mount at the SAME battery
# greps; UNMERGED here, where the strip still has ZERO product consumers and
# the leg stays the beside-scene probe above.
rm -rf "$PROJ/.tmp/s48batch4-sky"   # MC 11243 F3: pre-spawn rm (same class as above)
SKYRC=0
LOGSKY="$(LA_SKYCAP_PATH="$PROJ/.tmp/s48batch4-sky" LA_GATE_MODE=SKY_STRIP xvfb-run -a timeout 300 "$GODOT" --path "$PROJ" --script "$PROOF" 2>&1)" || SKYRC=$?
[[ $SKYRC -eq 0 ]] || { printf '%s\n' "$LOGSKY" | tail -30 >&2; fail "SKY_STRIP: the windowed sky-strip probe leg exited $SKYRC (named FAIL lines above)"; }
[[ "$LOGSKY" == *'FXSKY_RENDERER method=gl_compatibility'* ]] \
  || fail "SKY_STRIP: the renderer pin line is missing — the leg never confirmed it ran at gl_compatibility (wrong renderer = vacuous verdicts)"
[[ "$LOGSKY" == *'SKY_STRIP_RENDER_OK'* ]] \
  || fail "SKY_STRIP: SKY_STRIP_RENDER_OK missing — the shipped canyon_sky_v1 strip is NOT proven painted-as-authored at the CI-effective renderer (the Batch-4 render-probe duty stays open)"
[[ "$LOGSKY" == *'SKY_STRIP measured pt'* && "$LOGSKY" == *'SKY_STRIP measured pts='* ]] \
  || fail "SKY_STRIP: the per-point measured lines or the summary are missing — verdicts must print their own margins (painted-delta/match/cross)"
[[ "$(grep -c 'FXSKY_CAPTURE' <<<"$LOGSKY")" -ge 2 ]] \
  || fail "SKY_STRIP: fewer than 2 FXSKY_CAPTURE lines — one settled capture per state (S27 idiom)"
[[ -s "$PROJ/.tmp/s48batch4-sky/skystrip_skybase.png" && -s "$PROJ/.tmp/s48batch4-sky/skystrip_skyon.png" ]] \
  || fail "SKY_STRIP: capture files not non-empty on disk (the printed bytes claim vs reality)"
[[ "$(grep -c 'FALL_RECOVERED from=' <<<"$LOGSKY")" -eq 0 ]] \
  || fail "SKY_STRIP: the probe leg produced FALL_RECOVERED lines (it must move nothing; census discipline MC 10422)"

# RED pair (planted-bad, gate-red law): the SAME rig with the strip material
# alpha FORCED 0 (an alpha-0 copy of the mount — the proven FX_GLCOMPAT_PLANT
# mechanism). SKY_STRIP_RENDER must print its named FAIL at the SAME points
# (marker FXSKY_PLANT_RED) and the run MUST exit nonzero; a plant staying
# green fails the leg as VACUOUS by name (FXSKY_PLANT_NOT_CAUGHT). The red
# transcript + red capture ride beside the green (evidence names both).
rm -rf "$PROJ/.tmp/s48batch4-sky-red"   # MC 11243 F3: pre-spawn rm (same class as above)
SKYPRC=0
LOGSKYP="$(LA_SKYCAP_PATH="$PROJ/.tmp/s48batch4-sky-red" LA_GATE_MODE=SKY_STRIP_PLANT xvfb-run -a timeout 300 "$GODOT" --path "$PROJ" --script "$PROOF" 2>&1)" || SKYPRC=$?
[[ $SKYPRC -ne 0 ]] \
  || { printf '%s\n' "$LOGSKYP" | tail -20 >&2; fail "SKY_STRIP_PLANT: the planted-bad leg EXITED GREEN — the check is VACUOUS (an alpha-0 strip cannot paint pixels)"; }
[[ "$LOGSKYP" == *'SKY_STRIP_RENDER_FAIL'* && "$LOGSKYP" == *'FXSKY_PLANT_RED'* ]] \
  || fail "SKY_STRIP_PLANT: exited nonzero WITHOUT the named SKY_STRIP_RENDER_FAIL / FXSKY_PLANT_RED lines (a red leg must go red BY NAME)"
if grep -qE '^LA_GATE: SKY_STRIP_RENDER_OK' <<<"$LOGSKYP"; then
  fail "SKY_STRIP_PLANT: the planted-bad leg printed the verdict OK LINE — verdict collision (anchored to '^LA_GATE: SKY_STRIP_RENDER_OK' because the FXSKY_PLANT armed line NAMES the verdict — a whole-log substring check false-collides with it)"
fi


}   # ---- end battery_body ----

# MC 10026.31(b): battery-wide FALL_RECOVERED census — the product guarantee
# line 'FALL_RECOVERED from=' must appear EXACTLY FR_CENSUS times in the WHOLE
# battery log (the tee'd stdout above), not just in its own leg. SINCE
# MC 10420 / 10026.29 the count was 1: the fall_recovered own leg ALONE.
# (History the tightening was written for: at the 10423 head the count was 3 —
# the 2 extras were section J2 (calm_use), whose choreography co-located the
# player with a wild agent past the meadow edge (x=66.69) and the guarantee
# rescued the player twice BEHIND that leg's state-only asserts (reviewer
# R5 P2). Card 10026.29 landed the ground-extent MOTION clamp (terrain
# "ground_extent" meta, one channel with floor_min_y): enemy motion pins at
# the edge and every calm co-location lands ON the ground — the calm pair
# retired, own-leg only from here. The calm_use in-leg grep above keeps the
# ZERO assertion local; this tail keeps it battery-wide.)
# MC 10516 / 10026.33 S38: the count moves 1 -> 2 as a DELIBERATE own-leg
# PAIR — fall_recovered's mid-map below-plane drop + FLOOR_TRUST live's
# DELIBERATE off-edge drop (6 m past the east border, where no ground exists:
# the card's walk-off-the-map scenario, rescued at the same single site and
# logged KILL_PLANE_RESCUE). Both legs own their fall; the in-leg grep in (X)
# keeps this leg's count at exactly 1 (a re-fall loop REDs there first); every
# OTHER leg keeps ZERO (GROUND_CLAMP + calm greps above).
# Red-capability (logs .tmp/close-10422/, pair re-logged .tmp/close-10420/):
# a planted extra 'FALL_RECOVERED from=' echo takes the census 2->3 and this
# tail goes RED, exit 1.
FR_CENSUS=2
BATT_LOG="${BATTERY_CENSUS_LOG:-$PROJ/.tmp/battery-census.$$.log}"
mkdir -p "$PROJ/.tmp"
battery_body | tee "$BATT_LOG"
BODY_CODE=${PIPESTATUS[0]}
if [ "$BODY_CODE" -ne 0 ]; then
  echo "RUNTIME_INTEGRATION_TEST: census skipped (battery body exited $BODY_CODE) — partial log $BATT_LOG" >&2
  exit "$BODY_CODE"
fi
FR_HITS=$(grep -c 'FALL_RECOVERED from=' "$BATT_LOG" || true)
[ "$FR_HITS" -eq "$FR_CENSUS" ] \
  || fail "battery-wide census: 'FALL_RECOVERED from=' printed $FR_HITS time(s), expected EXACTLY $FR_CENSUS (the MC 10516 own-leg PAIR: fall_recovered's mid-map drop + FLOOR_TRUST's deliberate off-edge drop; any extra line means motion or a co-location got off the ground again — MC 10420/10026.29 discipline holds; census log $BATT_LOG)"
echo "RUNTIME_INTEGRATION_TEST: FALL_RECOVERED_CENSUS $FR_HITS/$FR_CENSUS OK (own-leg pair: fall_recovered + S38 FLOOR_TRUST edge-fall; every other leg zero — calm drift clamped by MC 10420/10026.29)"
echo "RUNTIME_INTEGRATION_TEST: GATE PASS — authoritative runtime path verified (positive green; no_bus/no_spawn/no_controller/no_dna/save_bad_version all red with named markers; save round-trip green; zone travel + boss phase green; death recovery green; MC 1348 P1 regressions green; quest arc green, persist + evidence-rewind green, reward beats + guard green, quest_neg red; skill economy green, skill_neg red; roster follow/save-load/book arms + mean-last emit order green, pay-after-break refused green, oversized-save trim green, roster_neg cap red; dissolve suppress green; story act-two arc green; S14 character life green; S20 camera language green — DnaExtracted-consumer kill pulse with BIT-EXACT rest, READ-only boss framing with BIT-EXACT base return; S18 zone four + enemy four green — 4-zone travel cycle + wrap, wraith denizens, 9-part composed enemy four, integer gait tick, 4 EXACT Hollow anchors + fog pin; S8 command bark green — real press, EXACT walked bubble counts roster 1/3, integer -1-step window with EXACT end frame, EMPTY roster zero ghosts on every walked frame (F1), bark-press + LoadGame inside the window carries zero bubbles (F2), zero gameplay delta, census 15 re-grepped in-leg; S19 story act three green — the close: restore-sync-chain FreshOpen at the act-two finale (ACT3_OPENED + guarded open card), four zone-4 completions each off its OWN fact (arrival/tongue/wage/reckoning, W6), mid-quest save round-trip on the shipped v3 wire with no card replay (ACT3_PERSIST, zero save fields), the named ACT_THREE_COMPLETE act-completion marker and the close card drained in order (ACT3_CLOSED), census unchanged; MC 10404 floor coverage green — dense 2 m grid probe: zero uncovered walkable points on every shipped zone ground, decor-only scenes named-skipped; MC 10404 fall recovery green — below-plane fall restores to the zone entry with Life intact and the product FALL_RECOVERED line grepped; MC 10420 ground-extent motion clamp green — meta box == visible AABB, live FLEE-driven enemy MOTION pins at the loaded edge under 30-frame continued pressure with the plan re-anchored, off-edge wild co-locations land ON the ground, every enemy/wild/FOLLOWER position swept inside the extent every frame, a recruited follower edge-hovers PINNED at the edge under follow pressure while the player stands unclamped past it (MC 10420 c2, F-DA1), calm_use carries ZERO FALL_RECOVERED (the R5 P2 masking pair retired); the battery-wide FALL_RECOVERED census restamped to EXACTLY 2 (the MC 10516 own-leg pair, per the 10026.29 discipline — every other leg still zero); MC 10516 S38 floor trust + kill plane green — the 256-probe RECORDS physics' acceptance of the resampled height maps per zone (514x514 canyon / 412x412 ruins verdicts printed), the scripted map-edge walk finds ZERO visible-ground-without-collision points on every shipped zone (border + 0.75 m inset shape-sweeps, contacts filtered to the terrain body; past-extent points accounted as clamp-BLOCKED/kill-planed, never silent), the composed-scene walk stands on 8 real-gravity border waypoints, ClampToGround past the edge BINDS, and a deliberate off-edge drop is RESCUED to the zone entry byte-exact with Life intact behind the named KILL_PLANE_RESCUE zone+reason marker at the ONE guarantee site; MC 10519 S27 zone-scene mounting green — boot + travel + LOAD all mount the zone's OWN scene under World (main.tscn ships no static zone instance), the per-zone Environment node-identity census prints before/after every travel with DISJOINT ids and hollow distinct by count, the SpawnSet survives every swap, EXACTLY ONE current camera (the FollowCamera) after every mount, the day-night driver drives the MOUNTED env behind a park at every boundary (re-bind at the swap — the S15 stale-binding hazard closed), and the hollow.png census plate renders from the leg; MC 11018 S45 camera rig-mode toggle green — under the MC 11189 DEFAULT FLIP (owner ruling verbatim 'Iso,') the ONE FollowCamera boots the FIXED ISO rig when the session-only launch flag is ABSENT (CAMERA_RIG_DEFAULT_ISO: zero-lag BIT-EQUAL pivot+Offset rebase + locked orientation + pulse + clearance pinned live on the default spawn) and the FOLLOW rig when the session-only --classic-cam user flag is PRESENT (CAMERA_RIG_CLASSIC_ON: lerp lag alive after a pivot teleport — follow path code-shared with master): pivot+Offset BIT-EQUAL zero-lag rebase (CAMERA_RIG_EXACT_REBASE), bit-locked orientation across the mover (CAMERA_RIG_ROT_LOCKED), the shipped S20 pulse holding BIT-EXACT rest on the fixed base with ZERO juice constants moved (CAMERA_RIG_PULSE_ARMED/_AT_BASE), and the static camera-collision clearance clamping the sight-line under a composed occluder then releasing BIT-EXACTLY (CAMERA_RIG_OCCL_CLAMPED/_RELEASED) — rig state session-only, zero save fields (R7), both rigs stay launchable per session (the MC 10995 append 9 'Toggle' mechanism STANDS; the default was NAMED 'Iso,' by the owner's own later ruling, MC 11189) — MC 11129 FIX-ROUND-1 (DA P1-GATE, pair FLIPPED IN PLACE by MC 11189): the OWNER-FACING boot rig line is gated BOTH WAYS, the UNFLAGGED spawn must print LA_PLAYTEST_RIG=ISO and the --classic-cam spawn must print LA_PLAYTEST_RIG=CLASSIC, red BY NAME PLAYTEST_RIG_MARKER_MISSING (planted print-removal, engine rebuilt between plant and run); MC 11019 S48 STEP-0 GL-Compat capability probe green at the CI-EFFECTIVE gl_compatibility renderer — the FIRST WINDOWED legs (xvfb-run + llvmpipe, the --headless dummy would answer vacuously): alpha textured quad drawn with texture alpha RESPECTED (QUAD_ALPHA_OK — the ground-FX mechanism the FX chain rides), two shadowless OmniLights lighting SIMULTANEOUSLY with the per-vertex-terrain blindness measured and named (MULTILIGHT_2_OK + FXGL_NOTE), torch-strip billboard OK (FLAME_BILLBOARD_OK), DECAL state NAMED either way with the CUT-rule fallback on FAIL, renderer pinned in-line, the planted-bad pair caught RED by name beside the green — ALL FOUR plants named (alpha-0 QUAD + alpha-0 FLAME + alpha-0 DECAL_WINDOW probe + REMOVED 2nd OmniLight) behind the MULTILIGHT additivity gate and the DECAL_WINDOW non-vacuity proof (MC 11019 FIX-ROUND-1); MC 11088 S48 BATCH-4 FIX-ROUND-1: the SKY_STRIP render probe green at the SAME CI-effective gl_compatibility renderer — the shipped canyon_sky_v1 strip PAINTED at RUN-derived expected screen points and MAPPED AS AUTHORED (SKY_STRIP_RENDER_OK, margins measured), the planted alpha-0 strip caught RED by name (SKY_STRIP_RENDER_FAIL + FXSKY_PLANT_RED) beside the green; captures on disk; zero save fields (R7); mode vocabulary drift gate green; MC 11037 ground-plate wiring green — the four wired ground_texture_path tscn entries present on disk, read back PAINTED on their mounts (ZONE_MOUNT_GROUNDTEX material readback), and ZERO engine 'Resource file not found' lines in the mount capture — the TEST wall 2026-10-20 white-ground-through-exit-code-gates class closed; MC 11243 battery hardening green — EVERY capture target rm-ed pre-spawn (Q2 hollow plate + all four windowed FX/SKY capture dirs; defense in depth BESIDE the proof-side SavePng==Ok gating), the ZONE4 marker capture NAMES its result (ZONE4_CAPTURE_RC=<rc> + wall-clock; budget kills and the proof's own budget FAIL die LOUD as BUDGET-DIED, any other non-zero rc RED BY NAME never swallowed, marker greps assert consistency with the rc), and the three MC 10120-era mid-body cumulative echoes renamed SECTION-OK so a bare GATE PASS grep is tail-specific; MC 11297 D5+D6 production tame green — a recruit-able wild stands in the shipped meadow via production (FieldBootWild, behind the SetWildSpawnEnabled gate that EVERY other mode opts out of, byte-identical choreography), tamed through the REAL calm→offer→first-wage join with NO test seam (WILD_PRESENT_PROD + TAME_RECRUITED named-GREEN, F opens the offer + spends 12 and never joins, P is the sole join, cap 3 holds), and the short-balance Mend refusal now PRINTS the named line (MEND_REFUSAL_PRINTED + the director's "Mend REFUSED — short balance", silent before this card) while emitting no SkillUsed and spending nothing; tame_neg red — the wild gate OFF finds no castable target and leaks nothing; MC 11320 bus_emit green AS A NAMED BATTERY LEG (section Y — the mode was dead CI code before this card, never a counted leg, last honest green 2026-10-05): the S0 dialogue/hurt edges fire EXACTLY once per edge on the live bus, F-2 asserts the EXIT-TRACKED boss left the tracker (the entered zone's re-field is a NEW actor, L1) inside the no-false-emit settle window, F-A's swap-carried death emits once (BUS_FALLEN_SWAP), F-B's lower-HP save load emits NO PlayerHurt (BUS_NO_LOAD_HURT) with the roster betrayal settle silenced AT SOURCE through the shipped book/break_bond inputs before any F-B window opens (BUS_FB_QUIET_ROSTER — measured 5/7 window fails without it, L3), and the farm's honest deaths print LA_DEATH and revive ONLY through the MC 11294 keep-alive seam, zero corpse agency (L2); mode vocabulary drift gate green; non-blank render)"
exit 0
