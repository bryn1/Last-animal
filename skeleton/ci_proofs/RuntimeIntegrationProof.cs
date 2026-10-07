// SIZE: 767 l (reason): multi-mode proof harness, the entry dispatch grows one small arm
// per battery mode: 572-l gate base 5bf18b6 +24 l S15 (compose/stages moved VERBATIM to
// the 138-l DayNight partial) +17 l S16 passives at the keep-both merge +24 l S14
// CHAR_MOTION at merge b2a2b2d (the 613-l stamp at e69e1bc PREDATED that arm) +46 l
// MC 10204 dispatch guard + KnownModes allow-list + the passives mode-doc row the S16
// merge dropped + the W2 restamp = 736 l at MC 10217 (tip e20b691); +51 l MC 10217 S20
// CAMERA_KILL_PULSE+BOSS_FRAME (two mode-doc rows, KnownModes pair, quiet-boot flag,
// two compose routes, budget arm, two stage cases); +31 l MC 10216 S18 ZONE4 integrated (merge-integration delta: +2 l restored BOSS_FRAME return/close,
// at this merge (mode-doc row, KnownModes entry, stage-100 dispatch arm, compose route,
// physics-tick hook, budget arm — child measured +36 l on its own branch, 4 l absorbed
// by the conflict integration; the stage body lives in the 253-l Zone4 partial, 600-
// ceiling hygiene). MC 10098 idiom: stage bodies live in the per-concern partials, only
// the dispatch stays here; the W1 rule keeps the >600 drift closed with comment-only
// restamps — the entry crossing 600 is a KNOWN finding with split card MC 10218.
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
//   BOSS_FRAME        — MC 10217 Inc-4 S20: boss framing off the READ-only
//                     WorldDirector.HasLiveBoss — farm to BossThreshold +
//                     travel (ZoneBossProof idioms), the integer clock eases
//                     the pull-back/height preset IN (BOSS_FRAME_ENTER /
//                     _HELD), a REAL-wire boss kill eases it OUT and the
//                     camera sits BIT-EXACTLY on its event-free follow base
//                     (BOSS_FRAME_AT_BASE; stage 101,
//                     RuntimeIntegrationProof.BossFrame.cs; presentation
//                     authority: zero Bus/GameState writes).
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
//
// Run:  $GODOT --headless --path <proj> --script res://ci_proofs/RuntimeIntegrationProof.cs
//
// File size: >400 total lines — a multi-mode proof harness (9+ modes). The
// mode-specific stage bodies live in the partial-class halves
// RuntimeIntegrationProof.Save.cs (stages 20/21/22/30),
// RuntimeIntegrationProof.Interact.cs (stages 40/90),
// RuntimeIntegrationProof.Chain.cs (stages 1-4, moved VERBATIM MC 10098
// split duty) and RuntimeIntegrationProof.Bus.cs (stage 80, bus_emit);
// this file keeps the shared harness (fields, composition, dispatch,
// helpers).
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
        "ZONE4",
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
        Root.AddChild(main);

        _main = main;
    }

    private void _ComposeDeferred()
    {
        var main = _main!;
        _bus = Root.GetNodeOrNull<EventBus>("/root/EventBus");
        if (_bus == null) { Fail("EventBus autoload not present even after scene _Ready"); return; }
        _bus.DnaExtracted += _ => _dnaCount++;
        _bus.DnaSpoken += _ => _dnaSpokenCount++;

        _director = main as WorldDirector;
        if (_director == null) { Fail("main.tscn root is not WorldDirector"); return; }

        _playerBody = main.GetNodeOrNull<CharacterBody3D>("Player");
        _hud = main.GetNodeOrNull<Hud>("UI/HudLayer/Hud");
        _dialogue = main.GetNodeOrNull<DialogueSystem>("UI/Dialogue");
        _companion = main.GetNodeOrNull<CompanionEntity>("Companion/Entity");
        // MC 1348 A5: the boot Enemy{i} ring is stood down — the director's
        // live Enemies set (the zone pipeline's SpawnSet actors) is the enemy
        // population every mode drives.
        _enemies.AddRange(_director!.Enemies);

        if (_playerBody == null) { Fail("Player node not found in main.tscn"); return; }
        if (_hud == null) { Fail("Hud not found at UI/HudLayer/Hud (WorldDirector UI not built)"); return; }
        if (_companion == null) { Fail("CompanionEntity not found at Main/Companion/Entity (director must spawn the machine-wired entity)"); return; }
        if (_mode is "dna_speak" or "no_interact" && _dialogue == null)
        { Fail("DialogueSystem not found at UI/Dialogue (WorldDirector UI not built)"); return; }

        if (_mode == "no_spawn")
        {
            // The director must have spawned nothing; the chain cannot start.
            // Give the scene one frame to populate, then assert emptiness.
            _stage = 90;
            return;
        }

        // bus_emit (MC 10098 S0): routes BEFORE the live-enemy guard — this
        // mode boots with spawning off (quiet scripted legs) and re-arms it
        // in its farm phase; the stage body lives in Bus.cs.
        if (_mode == "bus_emit")
        {
            _stage = 80;
            _stageFrames = 0;
            GD.Print($"LA_GATE: composed (bus mode) — enemies={_enemies.Count} (spawning off at boot)");
            return;
        }

        // JUICE_SHAKE (MC 10121 S2): the shake stage drives its own scripted
        // edges (TakeDamage + a consumer-test BossFallen emit); routes BEFORE
        // the live-enemy guard — this mode boots with spawning off (quiet
        // legs, bus_emit idiom). Stage body lives in Shake.cs.
        if (_mode == "JUICE_SHAKE")
        {
            _stage = 96;
            _stageFrames = 0;
            GD.Print($"LA_GATE: composed (shake mode) — enemies={_enemies.Count} (spawning off at boot)");
            return;
        }

        // CAMERA_KILL_PULSE (MC 10217 S20): the pulse stage drives its own
        // scripted edge (a direct DnaExtracted emit, the consumer-test idiom);
        // routes BEFORE the live-enemy guard — quiet boot (bus_emit idiom).
        // Stage body lives in RuntimeIntegrationProof.KillPulse.cs.
        if (_mode == "CAMERA_KILL_PULSE")
        {
            _stage = 100;
            _stageFrames = 0;
            GD.Print($"LA_GATE: composed (kill-pulse mode) — enemies={_enemies.Count} (spawning off at boot)");
            return;
        }

        // DAYNIGHT_STATE (MC 10199 S15): routes BEFORE the live-enemy guard —
        // quiet boot (bus_emit idiom); compose + stage live in the partial
        // RuntimeIntegrationProof.DayNight.cs (600-ceiling hygiene: the mode
        // arms stay one-liners in the multi-mode harness).
        if (_mode == "DAYNIGHT_STATE")
        {
            DayNightCompose(main);
            return;
        }

        if (_enemies.Count == 0) { Fail("WorldDirector spawned no EnemyActor"); return; }

        _dnaBefore = _hud.DnaMeter;

        // no_bus: disconnect the HUD from the bus before the kill so the
        // DnaExtracted emit cannot move the meter.
        if (_mode == "no_bus") _hud.DisconnectBus(_bus);

        // no_controller: swap the director's authoritative PlayerController
        // binding for a decoy; the AI target must stop matching the body.
        if (_mode == "no_controller")
        {
            var bootstrap = Root.GetNodeOrNull<GameBootstrap>("/root/GameBootstrap");
            if (bootstrap == null) { Fail("GameBootstrap autoload absent"); return; }
            bootstrap.Bind(new PlayerController(new CombatVec3(999f, 0f, 999f)));
        }

        // no_dna: block the director's DnaExtracted forwarding so the kill
        // happens but the bus never hears it.
        if (_mode == "no_dna") _director.SetDnaForwarding(false);

        // no_interact: disable the director's interact/speak seam so interact
        // is pressed but neither DnaSpoken nor the dialogue may fire.
        if (_mode == "no_interact") _director.SetInteractEnabled(false);

        // quest_arc / quest_persist / quest_neg (MC 3904 2c): the quest chain
        // drives its own input (teleport + interact/pay/attack/travel); skip
        // the movement press and route to stage 50 — stage bodies live in
        // RuntimeIntegrationProof.Quests.cs.
        if (_mode is "quest_arc" or "quest_persist" or "quest_neg")
        {
            _stage = 50;
            _stageFrames = 0;
            GD.Print($"LA_GATE: composed (quest mode) — enemies={_enemies.Count} intro={_director.Quests.Status("q_intro")}");
            return;
        }

        // quest_arc2 (MC 10132 S10): the ACT-TWO chain drives its own input
        // (speak, owned save/load, travel, pay, farm); route to stage 55 —
        // NOT a 9x number: the S3 sibling takes a 9x stage on its branch,
        // the quest-family slot 55 (between 50 and 60) merges collision-free.
        if (_mode == "quest_arc2")
        {
            _stage = 55;
            _stageFrames = 0;
            GD.Print($"LA_GATE: composed (act-two mode) — enemies={_enemies.Count} intro={_director.Quests.Status("q_intro")}");
            return;
        }

        // skill_use / skill_neg (MC 3912 2e): the skill chain drives its own
        // input (farm + measured hits + skill presses); skip the movement
        // press and route to stage 60 — stage bodies live in
        // RuntimeIntegrationProof.Skills.cs.
        if (_mode is "skill_use" or "skill_neg")
        {
            _stage = 60;
            _stageFrames = 0;
            GD.Print($"LA_GATE: composed (skill mode) — enemies={_enemies.Count} manna={_director.PlayerModel.Manna}");
            return;
        }

        // passives (MC 10200 S16): the resonance-passive chain drives its own
        // input (REAL extracts + one mend press); route to stage 65 — NOT a
        // 9x number: the skill-family slot between 60 and 70 (quest_arc2's
        // 55 precedent), merging collision-free with the sibling W2 legs.
        // Stage body lives in RuntimeIntegrationProof.Passives.cs.
        if (_mode == "passives")
        {
            _stage = 65;
            _stageFrames = 0;
            GD.Print($"LA_GATE: composed (passives mode) — enemies={_enemies.Count} manna={_director.PlayerModel.Manna}");
            return;
        }

        // calm_use / calm_neg (MC 10031): the calming-speak chain drives its
        // own input (farm + calm casts + pay/interact/save/load presses);
        // skip the movement press and route to stage 70 — stage bodies live
        // in RuntimeIntegrationProof.CalmingSpeak.cs.
        if (_mode is "calm_use" or "calm_neg")
        {
            _stage = 70;
            _stageFrames = 0;
            GD.Print($"LA_GATE: composed (calm mode) — enemies={_enemies.Count} manna={_director.PlayerModel.Manna}");
            return;
        }

        // JUICE_HITFLASH (MC 10120 S1): the juice stage drives its own single
        // hit (frozen target, one attack press on the real wire); route to
        // stage 95 — stage body lives in RuntimeIntegrationProof.Juice.cs.
        if (_mode == "JUICE_HITFLASH")
        {
            _stage = 95;
            _stageFrames = 0;
            GD.Print($"LA_GATE: composed (juice mode) — enemies={_enemies.Count}");
            return;
        }

        // DISSOLVE_SUPPRESS (MC 10129 S3): the leg kills a live enemy through
        // the REAL wire and asserts the death-tick suppression + 20f dissolve.
        // It NEEDS the live spawn set (quiet-boot modes have no corpse to
        // make), so it routes after the enemy guard — stage body lives in
        // RuntimeIntegrationProof.Dissolve.cs.
        if (_mode == "DISSOLVE_SUPPRESS")
        {
            _stage = 97;
            _stageFrames = 0;
            GD.Print($"LA_GATE: composed (dissolve mode) — enemies={_enemies.Count}");
            return;
        }

        // CHAR_MOTION (MC 10198 S14): the motion leg NEEDS the live meadow
        // spawn set (chase walk witness + the real damage site) — routes after
        // the enemy guard; the leg culls it to one goblin mid-way. Stage body
        // lives in RuntimeIntegrationProof.Motion.cs.
        if (_mode == "CHAR_MOTION")
        {
            _stage = 99;
            _stageFrames = 0;
            GD.Print($"LA_GATE: composed (motion mode) — enemies={_enemies.Count}");
            return;
        }

        // BOSS_FRAME (MC 10217 S20): the boss-framing leg NEEDS the live spawn
        // set (KillLoop farm to the BossThreshold + a REAL-wire boss kill), so
        // it routes after the enemy guard like DISSOLVE/CHAR_MOTION; long leg
        // — rides the QuestFrameBudget list below. Stage body lives in
        // RuntimeIntegrationProof.BossFrame.cs.
        if (_mode == "BOSS_FRAME")
        {
            _stage = 101;
            _stageFrames = 0;
            GD.Print($"LA_GATE: composed (boss-frame mode) — enemies={_enemies.Count}");
            return;
        }

        // ZONE4 (MC 10216 S18): the zone-four leg boots the LIVE meadow set and
        // TRAVELS (the seam is the product under test — quiet-boot modes cannot
        // prove a travel cycle); routes after the enemy guard like DISSOLVE/
        // CHAR_MOTION. The zone-entry baseline rides _z4SpawnOrigin captured at
        // compose. Stage body lives in RuntimeIntegrationProof.Zone4.cs.
        if (_mode == "ZONE4")
        {
            _z4SpawnOrigin = _playerBody!.GlobalPosition;
            _stage = 100;
            _stageFrames = 0;
            GD.Print($"LA_GATE: composed (zone4 mode) — enemies={_enemies.Count} zone={_director.CurrentZone}");
            return;
        }

        // Capture the movement baseline BEFORE pressing the input (MC 1344.1):
        // stage 0 ran after the press, by which time the player had already moved.
        _playerStart = _playerBody.GlobalPosition;
        _pressPhysFrame = _physFrames;

        Input.ActionPress("move_right");
        GD.Print($"LA_GATE: composed — Player + {_enemies.Count} enemies + Hud + CompanionEntity (dnaBefore={_dnaBefore})");
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
        int budget = _mode is "quest_arc" or "quest_persist" or "quest_arc2" or "skill_use" or "skill_neg" or "calm_use" or "calm_neg" or "bus_emit" or "passives" or "BOSS_FRAME" ? QuestFrameBudget : FrameBudget;
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
            // ---- CAMERA_KILL_PULSE (MC 10217 S20): stage body lives i, "ZONE4",
        }
        return false;
    }

    /// <summary>
    /// MC 10117 (10026.13.5): same latent class as MC 10112's BridgeMvpProof fix —
    /// the proof is the C# MainLoop, the LAST managed object standing at shutdown.
    /// Its live-scene fields root the scene's C# wrappers (and every Resource they
    /// hold) until after the native ObjectDB is gone, so their GC finalizers hit
    /// freed objects — "Leaked unsafe reference ... csharp_script.cpp:179", exit
    /// 134/139. Detection is probabilistic (~1/4, TEST T-1 MC 10112; at this HEAD
    /// the plant measured 0 RED / 20 — see evidence). Release the wrappers and
    /// flush finalizers BEFORE Quit, while the ObjectDB is alive. Idiom REUSED
    /// from BridgeMvpProof.ReleaseHeldRefsBeforeQuit (MC 10112).
    /// </summary>
    private void ReleaseHeldRefsBeforeQuit()
    {
        _enemies.Clear();
        _bus = null; _director = null; _main = null; _playerBody = null;
        _hud = null; _dialogue = null; _companion = null;
        System.GC.Collect();
        System.GC.WaitForPendingFinalizers();
        System.GC.Collect();
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
        if (!_asserted && !_failed && _mode is "positive" or "save")
            GD.PrintErr("LA_GATE: FAIL — finished without asserting all stages");
    }

    private EnemyActor? FirstLiveEnemy()
    {
        foreach (var e in _enemies)
            if (!e.IsDead) return e;
        return null;
    }

    private void TeleportIntoRange()
    {
        EnemyActor? target = FirstLiveEnemy();
        if (target == null) { Fail("no live enemy to teleport next to"); return; }
        // Put the player just inside melee range (AttackRange 1.5) of the enemy,
        // at the enemy's height so the 3D distance check is dominated by XZ.
        Vector3 e = target.GlobalPosition;
        _playerBody!.GlobalPosition = new Vector3(e.X - 0.8f, e.Y, e.Z);
        GD.Print($"LA_GATE: player teleported into attack range of {target.Name} at ({e.X:0.##},{e.Y:0.##},{e.Z:0.##})");
    }

    private void Check(string what, bool ok, string detail)
    {
        GD.Print($"LA_GATE: check: {what}: {(ok ? "ok" : "FAIL")} {detail}");
        if (!ok) Fail(what);
    }

    private void Fail(string why)
    {
        Input.ActionRelease("move_right");
        Input.ActionRelease("attack");
        Input.ActionRelease("interact");
        _failed = true;
        GD.PrintErr($"LA_GATE: FAIL — {why}");
        Quit(1);
    }
}
