// SIZE reason: 599 l by wc AT this fourth-merge head (keyword repair MC 11431 R1: opener word only, stamp text kept; the MC 11297 merge left the entry AT the ceiling, 600; this card's quest_mono arms landed 602 and were PAID IN-CARD AGAIN the MC 11019 FIX-ROUND-1 re-word precedent — mode-doc row to the tame-mode 1-l idiom + the card's own re-measure clauses folded, all re-word cuts inside this card's own text; the earlier MC 11294-base crossing at 604 paid the same way; UNDER the ceiling, NO split owed; the clause stays live for future growth; MC 11088 S48 BATCH-4 FIX-ROUND-1: the card's +22 (SKY_STRIP mode-doc rows + the stage-109 arm) crossed the ceiling at 620 and was PAID IN-CARD by the MC 10218 verbatim-move split — the shared machine fields + consts moved to the 70-l RuntimeIntegrationProof.State.cs, zero behaviour change; prior MC 11019 S48 FIX-ROUND-1 −1: the two FXGL mode-doc rows reworded at net −1 to name the widened plant + gates IN-CARD — ceiling still honored; prior MC 11019 S48 STEP-0 NET −10 over the 609 at S45: the card's +32 paid
// IN-CARD by a double verbatim-move split — chain case 0/5/6 bodies -> Chain.cs (MC 10098 precedent) +
// _Finalize -> Harness.cs (MC 10218 precedent) — plus the S45 owed 9-l remainder: 600 ceiling RESTORED; prior S45 SPLIT + restamp: NET −28
// over the 637 re-measured at the MC 10731 head — the +40 the CAMERA_RIG_ISO card
// owed (mode-doc row, KnownModes entry, capture-arm gate + budget lines, case-107
// arm) was PAID with an MC 10218-precedent split in the same card: the _Initialize
// BODY (frame cap + composition-root instantiation + every per-mode set-before-
// AddChild gate) moved VERBATIM to the 102-l RuntimeIntegrationProof.Initialize.cs,
// the mode read + unknown-mode fail-safe guard STAYED in the entry (the mode_sets
// fail-safe grep is entry-scoped by design); the entry is now SMALLER than at S27;
// prior 637 read: the MC 10519 S27 +38 over the 599 at MC 10516 — the
// ZONE_VISUAL_MOUNT mode-doc row, KnownModes entry, case-106 arm, quiet-boot spawn-off guard + its
// header lines; the 599: the S38 +16 over the 583 at MC 10420 c2; that 583: the 557 stamp lagged
// cycle 1's own +17; numbers below are re-measured, never delta-summed): the
// multi-mode harness ENTRY keeps ONLY the mode-doc
// header, KnownModes allow-list and dispatch (the shared fields moved VERBATIM
// to State.cs at MC 11088 — see that header) — >400 because the 316-l mode
// doc mode_sets_check parses (awk from '// Modes \(env' through '// Run:' this
// commit, BOTH ends inclusive; 316 at this head (MC 11295 re-measure at the fourth-merge head; prior 315 at the d2e8913 head, 289 at MC 11019 S48, 162 at the S24 head) and the 40-mode
// dispatch (MC 11295 re-measure at the fourth-merge head = 40; MC 11297 added tame_use+tame_neg; 37 at MC 11088 S48 BATCH-4; 35 at the MC 11019 S48 head; 32-mode at the MC 10731
// head) ride together here. W4 SPLIT,
// carded MC 10218 (the 814-l stamp at 6ad4007 was the MC 10212 finding) — PAID here:
// _ComposeDeferred moved VERBATIM to the 334-l RuntimeIntegrationProof.Compose.cs (270-l at the a3a3c2f split; re-measured by wc at this head, MC 11018)
// (per-mode compose routing) and Fail/Check/FirstLiveEnemy/TeleportIntoRange/
// ReleaseHeldRefsBeforeQuit to the 70-l RuntimeIntegrationProof.Harness.cs — zero
// behaviour change, marker-log-diff across 8 modes in evidence .audits/*-proofsplit.
// MC 10098 idiom kept: stage bodies live in the per-concern partials, only the dispatch
// stays here. Merge 781ea66 lesson kept: dispatch arms can be silently fused away — the
// mode-gated camera+zone4 stage arms (restored at 4677b7c) are now PINNED by the
// mode_sets_check stage-pairing leg shipped with this same card (MC 10218).
using Godot;
using LastAnimal.Combat;
using LastAnimal.Companion;
using LastAnimal.Core;
using LastAnimal.Core.Framework;
using LastAnimal.Save;
using LastAnimal.Ui;
using LastAnimal.World;
using System.Collections.Generic;
using System.Linq;

// Last Animal — T3b runtime integration proof (MC 1256.10, bernie, 2026-09-20).
//
// Proves the ONE authoritative runtime path (design 1256.2 §1.1/§4.1): the
// playable main.tscn scene (WorldDirector composition root) drives the REAL
// pure-logic systems — the same instances tests exercise — with no sidecar
// state. Stages print a marker ONLY after their assertion passes; a stage that
// never asserts fails via the frame budget (no passing on unconditional output).
//
// Modes (env LA_GATE_MODE, default "positive"):
//   positive       — the full chain must PASS.
//   no_bus         — Hud is disconnected from the bus before the kill; the
//                    chain must detect DnaExtracted fired but the meter frozen
//                    (NEG_BUS) and exit non-zero.
//   no_spawn       — director spawning disabled; no EnemyActor exists, the
//                    chain cannot start (NEG_SPAWN), exit non-zero.
//   no_controller  — the director's PlayerController binding is swapped for a
//                    decoy; the AI target must no longer match the
//                    authoritative controller (NEG_CONTROLLER), exit non-zero.
//   no_dna         — the director's DnaExtracted forwarding is disabled; the
//                    kill happens but the bus never hears it (NEG_DNA),
//                    exit non-zero.
//   save           — after the kill chain: SaveGame() -> LoadGame() round-trip
//                    through the REAL scene state (SAVE_WRITTEN,
//                    LOAD_RESTORED_DNA, LOAD_RESTORED_LOYALTY,
//                    SAVE_ROUNDTRIP_PURE).
//   save_bad_version — writes a save with Version = CurrentVersion + 1 and
//                     asserts Load rejects it (NEG_SAVE_VERSION), exit non-zero.
//   dna_speak       — after the follow stage the player is teleported next to
//                     the companion and interact is pressed; the production
//                     path interact -> DnaLanguage.Speak -> EventBus.DnaSpoken
//                     -> DialogueSystem.Show must fire (DNA_SPOKEN_EMITTED,
//                     DIALOGUE_SHOWN).
//   no_interact     — the director's interact seam is disabled; interact is
//                     pressed but no DnaSpoken fires and no dialogue opens
//                     (NEG_INTERACT), exit non-zero.
//   quest_arc       — MC 3904 stage 2c: the FULL 5-quest placeholder arc
//                     driven to the zone-boss finale (QUEST_COMPLETED;
//                     RuntimeIntegrationProof.Quests.cs).
//   quest_persist   — live-scene quest progress survives save -> load
//                     (QUEST_PERSIST; the v3 QuestStates wire).
//   quest_neg       — the SetQuestHooksEnabled gate seam is off; the arc must
//                     stall (NEG_QUEST), exit non-zero.
//   quest_arc2      — MC 10132 S10: ACT TWO (QuestTable.RuinsArc) on the live
//                     scene — act-open card, the four chained ruins-deep
//                     completions, the act-two save round-trip on the shipped
//                     QuestStates wire, close card (ACT2_* + ACT_CARD markers;
//                     RuntimeIntegrationProof.Story2.cs).
//   quest_mono      — MC 11295 (playthrough D3): REAL boss-kill AT the ruins opens act two at the standing zone; the beat ADVANCES, save->disk->load keeps it, re-entries regress nothing (MONO_*). QuestMono.cs, stage-50 slot.
//   skill_use       — MC 3912 stage 2e: the skill economy end-to-end on the
//                     live scene — kill gain, live unlock, armed multiplier
//                     AT the single DealDamage site, arm consumed after one
//                     hit, rejection, Mend (RuntimeIntegrationProof.Skills.cs).
//   skill_neg       — the SetSkillActionsEnabled gate seam is off; kills and
//                     presses must move nothing (NEG_SKILL), exit non-zero.
//   passives        — MC 10200 S16: the Resonant Draw / Deep Mend bands on the
//                     live scene (REAL extracts + one mend press, stage 65;
//                     RuntimeIntegrationProof.Passives.cs). Undocumented at the
//                     S16 keep-both merge; row added MC 10204 so the header's
//                     mode list matches the dispatch + KnownModes exactly.
//   calm_use        — MC 10031 Calming Speak end-to-end: cast spend-after-scan,
//                     pay-in-window join, refusals, expiry, E-stands,
//                     load-cleared (RuntimeIntegrationProof.CalmingSpeak.cs).
//   calm_neg        — the same seam off: a funded unlocked calm press leaks
//                     nothing (NEG_CALM), exit non-zero.
//   tame_use        — MC 11297 D5+D6: boot wild tamed via the calm→offer→first-wage join, NO seam (TAME_RECRUITED); D6 Mend short-balance PRINTS (MEND_REFUSAL_PRINTED). Tame.cs stage 110.
//   tame_neg        — MC 11297: wild gate OFF (before-fix) — the press finds no castable target, leaks nothing (NEG_TAME), exit non-zero.
//   bus_emit        — MC 10098 Inc-3 S0: the four CONSUMER-FACING presentation
//                     emits (DialogueShown/DialogueClosed/PlayerHurt/
//                     BossFallen) fire EXACTLY ONCE per edge on the live
//                     scene, incl. the F-2 guard: a zone exit with the boss
//                     alive (no death) must NOT emit BossFallen
//                     (RuntimeIntegrationProof.Bus.cs).
//   JUICE_HITFLASH  — MC 10120 Inc-3 S1: the hit-flash + VISUAL-NODE punch
//                     leg on the live scene — flash/punch active on the hit
//                     frame, held at +4f, EXACT base return by +6f, enemy
//                     BODY GlobalPosition UNCHANGED (stage 95,
//                     RuntimeIntegrationProof.Juice.cs).
//   JUICE_SHAKE     — MC 10121 Inc-3 S2: FollowCamera subscribes S0's
//                     PlayerHurt/BossFallen; the shake window opens on the
//                     emit, holds at +4f, and the camera returns to its
//                     EXACT shake-free follow base by +12f (integer-frame
//                     decay, F4; stage 96, RuntimeIntegrationProof.Shake.cs).
//   DISSOLVE_SUPPRESS — MC 10129 Inc-3 S3: kill a live enemy on the real
//                     wire; from the death tick the body collision is OFF and
//                     the corpse is outside every director targeting scan
//                     (existing IsDead predicate reused, WorldDirector.cs:375),
//                     a FORCED direct DealDamage still deals 0, the visual
//                     dissolves 20f (integer counter, F4) and frees itself at
//                     f+21; enemy-count/existence prints show the corpse
//                     absent (stage 97, RuntimeIntegrationProof.Dissolve.cs).
//   DAYNIGHT_STATE    — MC 10199 Inc-4 S15: the day-night clock is an EXACT
//                     integer frame counter on the world physics tick; the
//                     shipped Environment + DirectionalLight3D carry the
//                     per-zone table rows EXACTLY at the dawn/noon/dusk/night
//                     frame windows, fog_sky_affect reads 0.0, and a wall-clock
//                     feed goes RED at the clock-nonlinear assert (stage 98,
//                     RuntimeIntegrationProof.DayNight.cs).
//   CHAR_MOTION       — MC 10198 Inc-4 S14: procedural character life on the
//                     live scene — integer-phase walk feed off the read-only
//                     velocity (CHAR_WALK_ACTIVE / CHAR_ENEMY_WALK), EXACT base
//                     return at rest (CHAR_WALK_AT_REST), attack lean at the
//                     ONE DealDamage hunk (CHAR_LEAN_ACTIVE / CHAR_LEAN_AT_BASE),
//                     player VisualJuice flash at the real damage site
//                     (CHAR_PLAYER_FLASH), body GlobalPosition bit-untouched
//                     across the juice window (CHAR_BODY_STILL; stage 99,
//                     RuntimeIntegrationProof.Motion.cs).
//   CAMERA_KILL_PULSE — MC 10217 Inc-4 S20: the FollowCamera's DnaExtracted
//                     CONSUMER (plan pin N-3: no new signal, census stays 15) —
//                     the 8f integer punch-in window opens on the emit, HOLDS
//                     +4f (camera strictly closer to the player), and with the
//                     player's physics FROZEN the camera returns BIT-EQUAL to
//                     its pre-trigger transform (CAMERA_PULSE_ACTIVE / _HELD /
//                     _AT_BASE; stage 100, RuntimeIntegrationProof.KillPulse.cs).
//                     S23 STRENGTH (MC 10229): the +4f kick vector is pinned
//                     COLLINEAR with and OPPOSING the player->camera sight-line
//                     (CAMERA_PULSE_DIR), the measured kick envelope is pinned
//                     to PulseFrames=8 (CAMERA_PULSE_WINDOW_8) and the measured
//                     peak displacement to PunchMetres=0.75
//                     (CAMERA_PULSE_PUNCH_075) — against the literals, not the
//                     tunable class.
//   BOSS_FRAME        — MC 10217 Inc-4 S20: boss framing off the READ-only
//                     WorldDirector.HasLiveBoss — farm to BossThreshold +
//                     travel (ZoneBossProof idioms), the integer clock eases
//                     the pull-back/height preset IN (BOSS_FRAME_ENTER /
//                     _HELD), a REAL-wire boss kill eases it OUT and the
//                     camera sits BIT-EXACTLY on its event-free follow base
//                     (BOSS_FRAME_AT_BASE; stage 101,
//                     RuntimeIntegrationProof.BossFrame.cs; presentation
//                     authority: zero Bus/GameState writes). S23 STRENGTH
//                     (MC 10229): at progress==18 with the player lifted
//                     clear + physics FROZEN the follow base sits on its
//                     exact lerp fixed point, and the MEASURED preset delta
//                     is pinned to PullBack=3.0 / Height=1.5 within float
//                     tolerance (BOSS_FRAME_CONSTANTS — a gutted preset
//                     stays GREEN under the old dist>+1.0 check alone).
//   ZONE4             — MC 10216 Inc-4 S18: zone four + enemy four on the live
//                     scene — the travel seam reaches the 4th zone "hollow" and
//                     WRAPS to meadow on the shipped modulo (ZONE4_TRAVEL /
//                     ZONE4_WRAP, real input, ZoneBossProof press idiom); the
//                     4-zone denizen table fields 6 enemy-four Wraiths
//                     (ZONE4_TABLE); the 9-part spectre silhouette counts through
//                     the Lean subtree (ZONE4_ENEMY4); the wraith's rig phase
//                     advances EXACTLY +1 mod Cycle per physics tick incl. one
//                     wrap (ZONE4_GAIT_INT — the delta-time feed lands RED, F4);
//                     and the 4th day-night keyframe table reads EXACT at all
//                     four anchors with fog_sky_affect=0.0 (ZONE4_ANCHOR_* /
//                     ZONE4_FOGPIN_0 / ZONE4_KEY4_DISTINCT). Runtime-only, zero
//                     save delta, zero new signal (stage 100,
//                     RuntimeIntegrationProof.Zone4.cs).
//   BARK            — MC 10131 Inc-4 S8: the command bark, PURELY presentation
//                     on the live scene — ONE real ActionPress("bark") (the one
//                     input-map change, key G) opens a 16f INTEGER bubble window
//                     on every ACTIVE roster body (counter steps EXACTLY -1 per
//                     frame — the delta-time feed lands RED, F4 — gone on the
//                     EXACT end frame open+V); the walked count of VISIBLE
//                     "BarkBubble" nodes is EXACTLY the roster size at open and
//                     while held (1 = boot seed, 3 = planted Followers restored
//                     through the REAL LoadGame rebuild, save OWNED per MC 3910)
//                     and back to 0 at the end; the EMPTY roster (an EMPTY
//                     planted Followers list — the product rebuild that FREES
//                     the bodies with the roster, DA W5 F4) presses to ZERO
//                     ghost bubbles + exit 0; zero gameplay delta across every
//                     press (BARK_NO_GAMEPLAY_DELTA, physics-frozen bit-still,
//                     Motion/KillPulse idiom); census-15 re-grep IN-LEG; S24
//                     follow-up (MC 10238): press + LoadGame INSIDE the window
//                     leaves the REUSED bodies window-free — zero walked
//                     bubbles post-load (BARK_LOAD_CLEAR, load-seam bark
//                     clear, mirror of the calm clear) — and the EMPTY-roster
//                     zero-walk is floored above the settle so every frame of
//                     window + margin is walked (F1; stage
//                     102, RuntimeIntegrationProof.Bark.cs).
//   ACT_THREE       — MC 10273 Inc-4 S19: story ACT THREE, the close
//                     (RULING-8, board line MC 10026 append #9 row 895:
//                     "R8 zone four + act three = YES -> S18 + S19 proceed",
//                     owner chat "Rec on all.") on the live scene — the
//                     four zone-4 QuestTable.ActThreeArc() rows played by a
//                     THIRD pure QuestLog on the shipped S10 act mechanism.
//                     The act opens through the REAL save path (owned save:
//                     act one + act two diverged all-complete -> SaveGame ->
//                     LoadGame: act two ADOPTS, act three FreshOpens —
//                     ACT3_OPENED + the table-driven open card on the live
//                     view ACT3_CARD_ON_SCREEN); then four completions each
//                     off its OWN distinct fact (W6): three real travels
//                     onto the S18-shipped "hollow" (ACT3_ARRIVAL), three
//                     speaks (ACT3_TONGUE), the single wage settle (ACT3_WAGE,
//                     three-logs attribution), eight extractions
//                     (ACT3_RECKONING); the mid-quest save round-trip of the
//                     act-three rows on the SHIPPED v3 QuestStates wire
//                     (ACT3_PERSIST, zero save fields, no card replay); the
//                     wiring's act-completion condition prints the named
//                     ACT_THREE_COMPLETE marker and the close card drains in
//                     order behind the finale reward (ACT3_CLOSED).
//                     Runtime-only, zero save delta, zero new signal (stage
//                     103, RuntimeIntegrationProof.Story3.cs).
//   GROUND_CLAMP    — MC 10420 / 10026.29: the ground-extent clamp on the LIVE
//                    scene. The calm_use census pair (2 FALL_RECOVERED at
//                    x=66.69 behind state-only asserts) named the class:
//                    unbounded enemy MOTION + off-edge co-locations. Proves the
//                    extent meta == the visible ground's AABB (FloorCoverage-
//                    Proof's world<->extent knowledge, reused not re-authored),
//                    drives a REAL spawned enemy's own flee state at full speed
//                    into the +X bound and asserts it PINS at the extent (never
//                    beyond, never vacuously short of it, plan re-anchored with
//                    the body), sweeps EVERY enemy/wild/FOLLOWER position
//                    against the extent every frame (a beyond-extent position
//                    fails the leg naming its coordinates), lands a wild
//                    co-location requested 2.5 m past the edge ON the ground,
//                    and (cycle 2, DA wall F-DA1) RECRUITS that edge wild on
//                    the production wire (interact offer + pay_wage first
//                    wage), holds the player 2 m inside the edge so the follow
//                    lerp's arrival point wants edge + 1.5 — the companion
//                    PINS at the extent — while the PLAYER stands unclamped
//                    PAST it (rescue-not-prevention) (stage 104,
//                    RuntimeIntegrationProof.GroundClamp.cs). Markers
//                    GROUND_CLAMP_AABB / ENEMY_GROUNDPIN / WILD_SPAWN_CLAMPED /
//                    FOLLOWER_EDGEHOVER / PLAYER_UNCLAMPED /
//                    GROUND_CLAMP_SWEEP / GROUND_CLAMP_CLEAR.
//   FLOOR_TRUST     — MC 10516 / 10026.33 card S38: floor trust + kill plane
//                    on the LIVE scene, quiet boot. Scripted map-edge WALK (
//                    capsule-footprint ray set; FloorCoverageProof's mapping
//                    REUSED) requires ZERO visible-ground-without-collision
//                    points; 8 real-gravity border waypoints stand; past-edge
//                    motion BINDS at the extent (FLOOR_TRUST_BLOCKED); an off-
//                    edge drop is RESCUED byte-exact to the zone entry, Life
//                    intact, behind the product KILL_PLANE_RESCUE zone+reason
//                    marker (never silent). Stage 105, .FloorTrust.cs.
//   ZONE_VISUAL_MOUNT — MC 10519 / 10026.35 S27: zone-SCENE mounting on travel,
//                    quiet boot (bus_emit idiom; the leg re-arms spawning
//                    before its first travel). Boot ENTERZONE mounts the zone's
//                    own scene (main.tscn ships no static zone instance any
//                    more); every real travel press re-mounts — the leg asserts
//                    the mounted root IS World/<Zone>, the previous root is
//                    GONE, the SpawnSet still applied (ZONE_MOUNT_MOUNTED /
//                    _SPAWNSET), EXACTLY ONE current camera after every mount
//                    and it is the FollowCamera (ZONE_MOUNT_CAMERA_ONE — the
//                    W5-DA F-1 dual-current class), and prints the node-
//                    identity CENSUS (count + Name#instance-id) of World/<
//                    Zone>/Environment before/after every travel — id sets
//                    disjoint across swaps (ZONE_MOUNT_CENSUS / _DISJOINT) with
//                    hollow's census DISTINCT from the other three (
//                    ZONE_MOUNT_HOLLOW_DISTINCT). The day-night driver drives
//                    the MOUNTED env: across every mount boundary the leg PARKS
//                    (>= the 0.5 s decor-cull ride) then READS the mounted
//                    light/Environment bit-exact against DayNightClock at the
//                    applied frame (ZONE_MOUNT_DRIVER_* — the stale-binding
//                    close, S15 read idiom). LOAD mounts the SAVED zone behind
//                    a save-in-hollow -> travel -> load walk (ZONE_MOUNT_LOAD_
//                    HOLLOW; zero save delta — ZoneId already rides v3). The
//                    leg RENDERS the hollow.png per-zone census plate and
//                    asserts its identity rows pairwise-distinct before
//                    saving (ZONE_MOUNT_CAPTURE; path env LA_MOUNT_CAPTURE_
//                    PATH). Stage 106, RuntimeIntegrationProof.ZoneMount.cs.
//   CAMERA_RIG_ISO — MC 11018 / 10026.44.6 S45 rig-mode switch, MC 11189
//                    DEFAULT FLIP (owner ruling verbatim "Iso,"): the FIXED
//                    ISO rig is the shipped default; session-only opt-out
//                    flag `-- --classic-cam`, quiet boot (bus_emit idiom).
//                    The flag and the leg's expectation ride SEPARATE
//                    channels (the camera reads the cmdline USER arg; the
//                    leg reads LA_RIG_EXPECT) so the rig pin is non-
//                    circular. No flag (expect the default): the rig must be
//                    FIXED ISO — pin CAMERA_RIG_DEFAULT_ISO — with pivot+
//                    Offset BIT-equal zero-lag rebase (CAMERA_RIG_EXACT_
//                    REBASE), orientation bit-locked across the mover
//                    (CAMERA_RIG_ROT_LOCKED), the shipped S20 pulse holding
//                    BIT-EXACT rest on the fixed base (CAMERA_RIG_PULSE_
//                    ARMED / _AT_BASE), and the static camera-collision
//                    clearance clamping the sight under a composed occluder
//                    then releasing BIT-EXACTLY (CAMERA_RIG_OCCL_CLAMPED /
//                    _RELEASED). Flag + LA_RIG_EXPECT=follow: the FOLLOW rig,
//                    lerp lag alive (CAMERA_RIG_CLASSIC_ON). Optional
//                    LA_RIG_CAPTURE=<zone> parks the composed scene per zone
//                    for a graphical-test-helper capture (not a battery leg).
//                    Stage 107, RuntimeIntegrationProof.CameraRig.cs.
//   FX_GLCOMPAT — MC 11019 / 10026.44.7 S48 STEP-0: the GL-COMPAT renderer
//                    capability probe for the FX-code chain (S28 step-0
//                    idiom), quiet boot (bus_emit idiom). Mounts the three
//                    mechanisms BESIDE the composed scene — a ground-projected
//                    aura_disc_a alpha QUAD, a Decal node over terrain
//                    (blood_splat_1 into texture_albedo), and TWO shadowless
//                    OmniLight3D + a torch flame billboard (frame 0 of
//                    torch_flame_strip_6f) — and MEASURES pixels from a
//                    SubViewport probe FBO sharing the live world (the root-
//                    viewport texture reads EMPTY at this renderer). Prints
//                    named verdicts QUAD_ALPHA_OK|FAIL, DECAL_SUPPORT_OK|FAIL
//                    (on FAIL its fallback AND Forward+ re-check duty are named —
//                    S28 CUT rule), DECAL_WINDOW_ALIVE|DEAD (the decal-window
//                    non-vacuity proof, FIX-ROUND-1), ADDITIVE MULTILIGHT_2 +
//                    FLAME_BILLBOARD, then the FXGL_PROBE_RESULT summary the
//                    downstream FX cards consume. HALTS on any renderer != gl_compatibility (the
//                    CI-effective one; --headless dummy answers ZERO to every
//                    question). Windowed spawn (xvfb-run) — the battery's
//                    first. Captures by the S27 idiom (env LA_FXCAP_PATH).
//                    Stage 108, RuntimeIntegrationProof.FxGlcompat.cs.
//   FX_GLCOMPAT_PLANT — the S48 gate-red pair (FIX-ROUND-1: the plant covers
//                    the WHOLE rig): quad/flame/window-probe materials alpha
//                    FORCED 0 + the 2nd OmniLight REMOVED from the tree — all
//                    four named checks must print their FAIL, or the leg fails
//                    as VACUOUS by name. Red rides beside green. Stage 108.
//   SKY_STRIP — MC 11113 / 10026.44.12 S50-SKY: the REAL-MOUNT upgrade of
//                    the 11088 probe (option (b) wiring: the shipped strip
//                    rides zones/canyon/canyon.tscn as the SkyStrip horizon
//                    backdrop, mounted by the ONE WorldMount route): the leg
//                    presses the shipped travel ONCE, asserts the REAL back-
//                    drop node in the MOUNTED Canyon scene carries the shipped
//                    PNG on a native-4:1 quad, parks the clock on the NIGHT
//                    anchor and READS IT BACK bit-exact (the kept Procedural-
//                    Sky/DayNight law proven ALIVE, not asserted in a comment),
//                    mirrors the shipped camera into a SubViewport probe FBO
//                    at gl_compatibility and measures the strip PAINTED at
//                    points RUN-DERIVED from the asset bytes (the FxMaxAlphaUv
//                    law) and MAPPED AS AUTHORED through the node's own
//                    GlobalTransform. Prints SKY_STRIP_RENDER_OK|FAIL with
//                    every margin beside it; the renderer is pinned (FXSKY_
//                    RENDERER). Stage 109, RuntimeIntegrationProof.SkyStrip.cs.
//   SKY_STRIP_PLANT — the gate-red pair: the REAL mount's material_override
//                    REPLACED with an alpha-0 copy (a TRANSPARENT shipped
//                    strip inside the mounted product scene — the FXGL_-
//                    PLANT mechanism applied to the product mount).
//                    SKY_STRIP_RENDER must print its named FAIL at the SAME
//                    points (marker FXSKY_PLANT_RED), else the leg fails as
//                    VACUOUS by name (FXSKY_PLANT_NOT_CAUGHT). Red rides
//                    beside green. Stage 109.
//
// Run:  $GODOT --headless --path <proj> --script res://ci_proofs/RuntimeIntegrationProof.cs
//
// File size: >400 total lines — the mode doc + dispatch of a multi-mode proof
// harness (9+ modes). 597 l by wc AT this head (MC 11113 S50-SKY +8: the
// SKY_STRIP/_PLANT mode-doc rows REWRITTEN for the REAL-MOUNT upgrade (+6) and
// this stamp (+2) — no mode added, none retired, vocabulary 37 UNCHANGED; the
// prior stamp said 589 at the MC 11088 FIX-ROUND-1 head below (MC 11088 S48 BATCH-4 FIX-
// ROUND-1: the card's +22 crossed the ceiling at 620 and was PAID IN-CARD by
// the MC 10218 verbatim-move split — the shared machine fields + consts to the
// State.cs partial). Doc, allow-list, the mode read + fail-safe guard and the
// stage arms are ENTRY-SCOPED BY DESIGN (mode_sets_check.py parses the ENTRY's
// header doc, its KnownModes and its switch (_stage) arms); every stage BODY
// lives in a per-concern partial — Save.cs (20/21/22/30), Interact.cs (40/90),
// Chain.cs (1-4, moved VERBATIM MC 10098 split duty), Bus.cs (stage 80,
// bus_emit) — the roster continuing one-file-per-stage through SkyStrip.cs
// (stage 109, MC 11088); compose routing in Compose.cs, shared helpers in
// Harness.cs and the shutdown release list in Harness.cs (the MC 10218 W4 split).
public partial class RuntimeIntegrationProof : SceneTree
{
    // MC 11088 S48 FIX-ROUND-1 SPLIT (MC 10218 verbatim-move precedent): the
    // shared machine fields + movement/budget consts moved VERBATIM to
    // RuntimeIntegrationProof.State.cs — the card's +22 (two mode-doc rows +
    // the stage-109 arm) crossed the 600 house ceiling at 620 and §6's law
    // ("ANY further entry growth MUST be paid by an MC 10218-precedent
    // verbatim-move split BEFORE merging") is paid IN-CARD here. Zero behaviour
    // change: a partial class holds every field wherever the declaration sits;
    // the entry keeps the mode doc, the KnownModes allow-list, the mode read +
    // unknown-mode fail-safe guard and the stage dispatch (every one of them
    // mode_sets_check-entry-scoped BY DESIGN). The full battery re-run at this
    // head is the proof.
    /// <summary>The mode vocabulary of THIS dispatch (this file + its partials)
    /// — the unknown-mode guard's allow-list (MC 10204, S17 TEST F1): a new mode
    /// arm must register HERE too, or the harness refuses to run it. Kept beside
    /// the mode doc in the header, which lists the same set. Red-capability of
    /// the guard itself is proven by LA_GATE_MODE=bogus_mode -> exit 1
    /// (evidence .audits/*-s16fix). Class-local modes of the sibling proofs
    /// (RosterIntegrationProof, ZoneBossProof, P1FixProof) are NOT listed: they
    /// are dispatched by their own entry points.</summary>
    private static readonly HashSet<string> KnownModes = new(System.StringComparer.Ordinal)
    {
        "positive", "no_bus", "no_spawn", "no_controller", "no_dna",
        "save", "save_bad_version", "dna_speak", "no_interact",
        "quest_arc", "quest_persist", "quest_neg", "quest_arc2", "quest_mono",
        "skill_use", "skill_neg", "passives", "calm_use", "calm_neg", "tame_use", "tame_neg",
        "bus_emit", "JUICE_HITFLASH", "JUICE_SHAKE", "DISSOLVE_SUPPRESS",
        "DAYNIGHT_STATE", "CHAR_MOTION", "CAMERA_KILL_PULSE", "BOSS_FRAME",
        "ZONE4", "BARK", "ACT_THREE", "GROUND_CLAMP", "FLOOR_TRUST", "ZONE_VISUAL_MOUNT",
        "CAMERA_RIG_ISO",
        "FX_GLCOMPAT", "FX_GLCOMPAT_PLANT", "SKY_STRIP", "SKY_STRIP_PLANT",
    };

    private static bool IsKnownMode(string mode) => KnownModes.Contains(mode);

    // MC 11018 S45 SPLIT (MC 10218 verbatim-move precedent): the boot gates
    // moved VERBATIM to RuntimeIntegrationProof.Initialize.cs; the mode read +
    // unknown-mode guard STAY here (the mode_sets_check fail-safe grep is
    // entry-scoped by design), so this stays the full boot ENTRY POINT.
    public override void _Initialize()
    {
        GD.Print("LA_GATE: start");
        _mode = System.Environment.GetEnvironmentVariable("LA_GATE_MODE") ?? "positive";
        GD.Print($"LA_GATE: mode={_mode}");

        // MC 10204 hardening (S17 TEST finding F1): an UNRECOGNIZED LA_GATE_MODE
        // used to be IGNORED — every mode arm is a positive `if`, so a typo'd or
        // stale mode fell through to the default positive chain and could exit 0
        // having asserted nothing (vacuous green: a gate that cannot go RED is
        // not a gate). The dispatch's own vocabulary is the allow-list (Known-
        // Modes above); anything else halts here, by name, exit 1. Proof-class-
        // local modes (roster_*/trait_effects, the ZoneBoss/P1 proofs) dispatch
        // by their own entry points and are untouched by this guard.
        if (!IsKnownMode(_mode))
        {
            _failed = true;   // halts _Process before any stage runs (Fail idiom, MC 1344.1)
            GD.PrintErr($"LA_GATE: FAIL: unknown mode {_mode}");
            Quit(1);
            return;
        }

        InitializeBootGates();   // MC 11018 S45 SPLIT — verbatim body, see Initialize.cs
    }

    public override bool _PhysicsProcess(double delta)
    {
        _physFrames++;   // MC 1344.1: physics-tick counter (return false = keep running)
        // MC 10199 S15: the day-night clock leg runs in THIS exact tick domain
        // (shared with the driver's own _PhysicsProcess), after the counter.
        if (_mode == "DAYNIGHT_STATE" && _composed && _stage == 98 && !_failed)
            DayNightTick();
        // MC 10216 S18: the ZONE4 gait window rides THIS exact tick domain too
        // (one rig advance per physics tick — the +1 mod cycle pair check).
        if (_mode == "ZONE4" && _composed && _stage == 100 && !_failed)
            Zone4Tick();
        return false;
    }

    public override bool _Process(double delta)
    {
        if (_failed) return true;
        if (!_composed)
        {
            // First frame: the scene is in the tree and _Ready() has run —
            // resolve the bus/HUD/enemy references now (MC 1344.1).
            if (_main == null) return true;
            _ComposeDeferred();
            _composed = true;
            return false;
        }
        if (_director == null || _playerBody == null || _hud == null || _companion == null || _bus == null)
            return true;

        _frames++;
        _stageFrames++;
        // quest_arc runs the full 5-quest arc (wage grace clock + kill farm +
        // boss): it needs the larger budget defined in the Quests partial.
        int budget = _mode is "quest_arc" or "quest_persist" or "quest_arc2" or "quest_mono" or "ACT_THREE" or "skill_use" or "skill_neg" or "calm_use" or "calm_neg" or "tame_use" or "tame_neg" or "bus_emit" or "passives" or "BOSS_FRAME" ? QuestFrameBudget : FrameBudget;
        // MC 11018 S45: the CAMERA_RIG_ISO CAPTURE arm parks the live scene
        // for the graphical-test-helper framebuffer grab — the run's external
        // timeout is its watchdog; the in-leg budget would quit before the
        // capture lands (MC 1344.1 quit-race class). Battery legs keep the
        // shipped FrameBudget untouched.
        if (_mode == "CAMERA_RIG_ISO" && _crCaptureZone != null) budget = int.MaxValue;
        if (_frames > budget) { Fail("frame budget exhausted before all stages"); return true; }

        switch (_stage)
        {
            // MC 11019/S48 STEP-0 ceiling split (MC 10098 duty): case 0/5/6 bodies VERBATIM in Chain.cs.
            case 0: RunBootstrapStage(); break;

            // MC 10098 (split duty, proof ceiling): stage 1-4 bodies moved
            // VERBATIM into RuntimeIntegrationProof.Chain.cs — dispatch semantics
            // unchanged (true = halt this frame).
            case 1: if (RunMoveStage())   return true; break;
            case 2: if (RunKillStage())   return true; break;
            case 3: if (RunHudStage())    return true; break;
            case 4: if (RunFollowStage()) return true; break;

            case 5: RunChainRouterStage(); break;
            case 6: if (RunHoldStage()) return true; break;

            // ---- save mode: round-trip through the REAL scene state ----
            // MC 1344: the mode now runs the REAL kill chain first (stages 0-2),
            // so the save happens with DnaMeter > 0 and the load asserts a
            // CHANGED value round-trips. The old assert (meter unchanged while
            // LoadGame never wrote the meter) was vacuous — green by construction.
            // Stage bodies live in RuntimeIntegrationProof.Save.cs (partial).
            case 20: RunSaveStage(); break;
            case 21: RunMutateStage(); break;
            case 22: RunLoadStage(); break;

            // ---- save_bad_version mode: schema guard on a real save ----
            case 30: RunSaveBadVersionStage(); break;

            // ---- dna_speak / no_interact + no_spawn: stage bodies live in
            // RuntimeIntegrationProof.Interact.cs (partial).
            case 40: RunInteractStage(); break;
            case 90: RunNoSpawnStage(); break;

            // ---- quest_arc / quest_persist / quest_neg (MC 3904 2c): stage
            // bodies live in RuntimeIntegrationProof.Quests.cs (partial).
            case 50: RunQuestStage(); break;

            // ---- quest_arc2 (MC 10132 S10 ACT TWO): stage body lives in
            // RuntimeIntegrationProof.Story2.cs (partial).
            case 55: RunStory2Stage(); break;

            // ---- skill_use / skill_neg (MC 3912 2e): stage bodies live in
            // RuntimeIntegrationProof.Skills.cs (partial).
            case 60: RunSkillStage(); break;

            // ---- passives (MC 10200 S16): stage body lives in
            // RuntimeIntegrationProof.Passives.cs (partial).
            case 65: RunPassivesStage(); break;

            // ---- calm_use / calm_neg (MC 10031): stage bodies live in
            // RuntimeIntegrationProof.CalmingSpeak.cs (partial).
            case 70: RunCalmStage(); break;

            // ---- bus_emit (MC 10098 S0): stage body lives in
            // RuntimeIntegrationProof.Bus.cs (partial).
            case 80: RunBusStage(); break;

            // ---- JUICE_HITFLASH (MC 10120 S1): stage body lives in
            // RuntimeIntegrationProof.Juice.cs (partial).
            case 95: RunJuiceStage(); break;
            // ---- JUICE_SHAKE (MC 10121 S2): stage body lives in
            // RuntimeIntegrationProof.Shake.cs (partial).
            case 96: RunShakeStage(); break;
            // ---- DISSOLVE_SUPPRESS (MC 10129 S3): stage body lives in
            // RuntimeIntegrationProof.Dissolve.cs (partial).
            case 97: RunDissolveStage(); break;
            // ---- DAYNIGHT_STATE (MC 10199 S15): the leg works entirely in
            // _PhysicsProcess (DayNightTick, RuntimeIntegrationProof.DayNight.cs);
            // the frame-budget guard above is its watchdog.
            case 98: break;
            // ---- CHAR_MOTION (MC 10198 S14): stage body lives in
            // RuntimeIntegrationProof.Motion.cs (partial).
            case 99: RunMotionStage(); break;
            // ---- S20 camera + S18 zone-four stages (merged W3). Stage numbers are
            // MODE-SCOPED: CAMERA_KILL_PULSE and ZONE4 both open stage 100, so the
            // dispatch is mode-gated — the shared numbers cannot collide (MC 10217
            // 59ab466 stage 100/101; MC 10216 2566df3 stage 100). Bodies live in
            // RuntimeIntegrationProof.KillPulse.cs / .BossFrame.cs / .Zone4.cs.
            case 100:
                if (_mode == "ZONE4") RunZone4Stage(); else RunPulseStage();
                break;
            case 101: RunBossFrameStage(); break;
            // ---- BARK (MC 10131 S8): stage body lives in
            // RuntimeIntegrationProof.Bark.cs (partial).
            case 102: RunBarkStage(); break;
            // ---- ACT_THREE (MC 10273 S19): stage body lives in
            // RuntimeIntegrationProof.Story3.cs (partial).
            case 103: RunActThreeStage(); break;
            case 104: RunGroundClampStage(); break;
            // ---- FLOOR_TRUST (MC 10516 S38): stage body lives in
            // RuntimeIntegrationProof.FloorTrust.cs (partial).
            case 105: RunFloorTrustStage(); break;
            // MC 10519 S27: zone-scene mounting (stage 106 — UNIQUE number;
            // stage body lives in RuntimeIntegrationProof.ZoneMount.cs).
            case 106: RunZoneMountStage(); break;
            // MC 11018 S45: camera rig-mode switch (stage 107 — UNIQUE number;
            // stage body lives in RuntimeIntegrationProof.CameraRig.cs).
            case 107: RunCameraRigStage(); break;
            // MC 11019 S48 STEP-0: GL-Compat capability probe (stage 108 —
            // UNIQUE number; body lives in RuntimeIntegrationProof.FxGlcompat.cs).
            case 108: RunFxGlcompatStage(); break;
            // MC 11088 S48 FIX-ROUND-1 (DA F1): sky-strip render probe (stage 109 —
            // UNIQUE number; body lives in RuntimeIntegrationProof.SkyStrip.cs).
            case 109: RunSkyStripStage(); break;
            case 110: RunTameStage(); break;
        }
        return false;
    }

}
