// SIZE: 557 l measured (reason; +3 l S19 fix-cycle RULING-8 attribution): the
// multi-mode harness ENTRY keeps ONLY the mode-doc
// header, KnownModes allow-list, shared fields and dispatch — >400 because the 185-l mode
// doc mode_sets_check parses (awk '/Modes \(env/ ../ Run:/' this commit; 161 at the S24
// stamp, same measure) and the 29-mode dispatch ride together here. W4 SPLIT,
// carded MC 10218 (the 814-l stamp at 6ad4007 was the MC 10212 finding) — PAID here:
// _ComposeDeferred moved VERBATIM to the 270-l RuntimeIntegrationProof.Compose.cs
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
//
// Run:  $GODOT --headless --path <proj> --script res://ci_proofs/RuntimeIntegrationProof.cs
//
// File size: >400 total lines — the mode doc + dispatch of a multi-mode proof
// harness (9+ modes). The
// mode-specific stage bodies live in the partial-class halves
// RuntimeIntegrationProof.Save.cs (stages 20/21/22/30),
// RuntimeIntegrationProof.Interact.cs (stages 40/90),
// RuntimeIntegrationProof.Chain.cs (stages 1-4, moved VERBATIM MC 10098
// split duty) and RuntimeIntegrationProof.Bus.cs (stage 80, bus_emit);
// the compose routing lives in RuntimeIntegrationProof.Compose.cs and the
// shared helpers (Fail/Check/…) in RuntimeIntegrationProof.Harness.cs (the
// MC 10218 W4 split); this file keeps the header doc, fields, KnownModes
// allow-list and dispatch.
public partial class RuntimeIntegrationProof : SceneTree
{
    private const int MoveFrames = 12;
    // MC 1344.1: movement is applied in _PhysicsProcess ticks. Headless --script
    // process frames are uncapped, so counting process frames measures wall-clock
    // frames, not engine time. Count physics ticks instead (BridgeMvpProof pattern).
    private int _physFrames;
    private int _pressPhysFrame;
    private int _followStartPhys; // physics tick when the follow baseline was captured (MC 1345)
    private int _holdStartPhys;   // physics tick when the render-bar hold began (MC 1344.1)
    private const int AttackBudgetFrames = 240;   // input-path kill budget
    private const int FollowFrames = 40;
    private const int HoldFrames = 1200;          // post-PASS hold for the render bar:
    // 1200 physics-capped frames @60fps = 20s > the helper's 15s capture window.
    // 600 (10s) raced the capture — the proof quit before the framebuffer grab
    // and the render bar read a blank screen (MC 1344.1, flaky in runtime32).
    private const int FrameBudget = 2400;

    private EventBus? _bus;
    private WorldDirector? _director;
    private CharacterBody3D? _playerBody;
    private Hud? _hud;
    private DialogueSystem? _dialogue;
    private CompanionEntity? _companion;
    private readonly List<EnemyActor> _enemies = new();

    private string _mode = "positive";
    private int _frames;
    private int _stage;
    private int _stageFrames;
    private Node3D? _main;      // composition root, resolved in _Initialize (MC 1344.1)
    private bool _composed;     // _ComposeDeferred has run (MC 1344.1)
    private bool _failed;
    private bool _asserted;
    private int _dnaCount;
    private int _dnaSpokenCount;
    private int _dnaBefore;
    private int _attackToggle;
    private int _interactToggle;
    private Vector3 _playerStart;
    private Vector3 _companionStart;
    private float _followDist0;
    private int _kills;
    private int _loyaltyBeforeSave;
    private int _dnaMeterBeforeSave;
    private int _spokenDnaBeforeSave;
    private int _progressBeforeSave;
    private int _dnaMutated;
    private string _zoneBeforeSave = "";

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
        "quest_arc", "quest_persist", "quest_neg", "quest_arc2",
        "skill_use", "skill_neg", "passives", "calm_use", "calm_neg",
        "bus_emit", "JUICE_HITFLASH", "JUICE_SHAKE", "DISSOLVE_SUPPRESS",
        "DAYNIGHT_STATE", "CHAR_MOTION", "CAMERA_KILL_PULSE", "BOSS_FRAME",
        "ZONE4", "BARK", "ACT_THREE",
    };

    private static bool IsKnownMode(string mode) => KnownModes.Contains(mode);

    public override void _Initialize()
    {
        GD.Print("LA_GATE: start");
        _mode = System.Environment.GetEnvironmentVariable("LA_GATE_MODE") ?? "positive";
        GD.Print($"LA_GATE: mode={_mode}");

        // MC 10204 hardening (S17 TEST finding F1): an UNRECOGNIZED LA_GATE_MODE
        // used to be IGNORED — every mode arm below is a positive `if`, so a
        // typo'd or stale mode fell straight through to the default positive
        // chain and could exit 0 having asserted nothing (vacuous green: a gate
        // that cannot go RED is not a gate). The dispatch's own vocabulary is
        // the allow-list (KnownModes below, this file + its partials); anything
        // else halts here, by name, exit 1. Proof-class-local modes
        // (roster_*/trait_effects, the ZoneBoss/P1 proofs) are dispatched by
        // their own entry points and are untouched by this guard.
        if (!IsKnownMode(_mode))
        {
            _failed = true;   // halts _Process before any stage runs (Fail idiom, MC 1344.1)
            GD.PrintErr($"LA_GATE: FAIL: unknown mode {_mode}");
            Quit(1);
            return;
        }

        // MC 1344.1: in --script mode the autoload EventBus loads AFTER _Initialize,
        // and the HUD is built by WorldDirector.BuildUi() in _Ready — both unavailable
        // here. Do NOT create a local bus (it collides with the autoload at
        // /root/EventBus and splits the wire); resolve bus + HUD in _ComposeDeferred
        // on the first _Process frame, after the scene _Ready has run.
        // Harness fix (MC 1344.1): cap process frames so physics ticks accumulate
        // normally (same pattern as BridgeMvpProof).
        Engine.MaxFps = 60;

        // Instance the composition root (a --script run does not load main_scene).
        var packed = GD.Load<PackedScene>("res://main.tscn");
        if (packed == null) { Fail("cannot load res://main.tscn"); return; }
        Node3D main = packed.Instantiate<Node3D>();
        // no_spawn: disable enemy spawning BEFORE the director populates — the
        // director spawns in _Ready, which runs at AddChild, so the flag must be
        // set on the INSTANTIATED (not yet added) node (MC 1344.1).
        if (_mode == "no_spawn" && main is WorldDirector d) d.SetSpawningEnabled(false);
        // bus_emit (MC 10098 S0): the scripted dialogue/hurt legs need the
        // boot zone QUIET (a standing player is whittled to death by the
        // spawn set in ~4 s — see evidence 2026-10-05); spawning is OFF at
        // boot and re-armed by the farm phase through the same public seam.
        if (_mode == "bus_emit" && main is WorldDirector db) db.SetSpawningEnabled(false);
        // quest_neg: the story/quest gate seam (MC 3904 2c) must be off BEFORE
        // the director's _Ready runs InitStory — same set-before-AddChild idiom.
        if (_mode == "quest_neg" && main is WorldDirector dneg) dneg.SetQuestHooksEnabled(false);
        // skill_neg: the skill gate seam (MC 3912 2e) must be off BEFORE
        // _Ready runs InitSkills — same set-before-AddChild idiom.
        if (_mode == "skill_neg" && main is WorldDirector dsk) dsk.SetSkillActionsEnabled(false);
        // calm_neg: the calm cast + window ride the SAME seam (MC 10031) —
        // off before _Ready; the NEG_CALM leg proves a funded press leaks zero.
        if (_mode == "calm_neg" && main is WorldDirector dcm) dcm.SetSkillActionsEnabled(false);
        // JUICE_SHAKE (MC 10121 S2): the shake legs are scripted quiet (the
        // bus_emit idiom) — a standing player would be whittled by the spawn
        // set and stray PlayerHurt edges would re-arm the window mid-assert.
        if (_mode == "JUICE_SHAKE" && main is WorldDirector dsh) dsh.SetSpawningEnabled(false);
        // CAMERA_KILL_PULSE (MC 10217 S20): the pulse leg is scripted quiet
        // (the bus_emit/JUICE_SHAKE idiom) — its wire is a DIRECT bus emit and
        // nothing else must move the follow target mid-assert.
        if (_mode == "CAMERA_KILL_PULSE" && main is WorldDirector dkp) dkp.SetSpawningEnabled(false);
        // DAYNIGHT_STATE (MC 10199 S15): the clock leg drives no combat and
        // reads no gameplay state — quiet boot (bus_emit idiom) keeps the
        // ~23 s cycle run deterministic and cheap. The driver is PAUSED at
        // construction and resumed in _ComposeDeferred: composition ticks
        // (frame 0/1 writes) never slip past the leg's first observation, so
        // the DAWN anchor window is unskippable (ordering-agnostic). No save
        // is touched.
        if (_mode == "DAYNIGHT_STATE" && main is WorldDirector ddn)
        {
            ddn.SetSpawningEnabled(false);
            ddn.SetDayNightPaused(true);
        }
        // BARK (MC 10131 S8): the bark leg is scripted quiet (bus_emit idiom) —
        // zero enemies means nothing can move the gameplay snapshot mid-assert;
        // the roster shapes are built IN the leg through the product rebuild
        // (planted saves, MC 3910 OWNED).
        if (_mode == "BARK" && main is WorldDirector dbg) dbg.SetSpawningEnabled(false);
        Root.AddChild(main);

        _main = main;
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
        int budget = _mode is "quest_arc" or "quest_persist" or "quest_arc2" or "ACT_THREE" or "skill_use" or "skill_neg" or "calm_use" or "calm_neg" or "bus_emit" or "passives" or "BOSS_FRAME" ? QuestFrameBudget : FrameBudget;
        if (_frames > budget) { Fail("frame budget exhausted before all stages"); return true; }

        switch (_stage)
        {
            case 0:
                // Baseline was captured in _ComposeDeferred BEFORE the press
                // (MC 1344.1) — go straight to the movement stage.
                _stage = 1;
                _stageFrames = 0;
                break;

            // MC 10098 (split duty, proof ceiling): stage 1-4 bodies moved
            // VERBATIM into RuntimeIntegrationProof.Chain.cs — dispatch semantics
            // unchanged (true = halt this frame).
            case 1: if (RunMoveStage())   return true; break;
            case 2: if (RunKillStage())   return true; break;
            case 3: if (RunHudStage())    return true; break;
            case 4: if (RunFollowStage()) return true; break;

            case 5:
                if (_mode == "save") { _stage = 20; _stageFrames = 0; break; }
                if (_mode == "save_bad_version") { _stage = 30; _stageFrames = 0; break; }
                if (_mode is "dna_speak" or "no_interact")
                {
                    // DNA-speak stage: get the player next to the companion (the
                    // NPC the interact path talks to), then press interact.
                    _playerBody!.GlobalPosition = _companion.GlobalPosition;
                    GD.Print($"LA_GATE: player teleported next to the companion at {_companion.GlobalPosition} — pressing interact");
                    _stage = 40;
                    _stageFrames = 0;
                    break;
                }
                GD.Print("LA_GATE: PASS — authoritative runtime path verified (player->enemy->kill->DNA->HUD through ONE composition root)");
                _asserted = true;
                _stage = 6;
                _stageFrames = 0;
                _holdStartPhys = _physFrames;
                break;

            case 6:
                // Hold the live scene so graphical-test-helper (--wait 15)
                // captures a real rendered frame, not the splash.
                if (_physFrames >= _holdStartPhys + HoldFrames) { ReleaseHeldRefsBeforeQuit(); Quit(0); return true; }
                break;

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
        }
        return false;
    }

    public override void _Finalize()
    {
        Input.ActionRelease("move_right");   // unconditional: never leak a pressed action
        Input.ActionRelease("attack");
        Input.ActionRelease("interact");
        Input.ActionRelease("travel");       // MC 3912 2e: skill modes travel too
        Input.ActionRelease("skill_1");
        Input.ActionRelease("skill_2");
        Input.ActionRelease("skill_3");      // MC 10031: the calming-speak arm
        Input.ActionRelease("pay_wage");     // MC 3904 2c release idiom (harmless pre-2c)
        Input.ActionRelease("bark");         // MC 10131 S8: the bark press arm
        if (!_asserted && !_failed && _mode is "positive" or "save")
            GD.PrintErr("LA_GATE: FAIL — finished without asserting all stages");
    }

}
