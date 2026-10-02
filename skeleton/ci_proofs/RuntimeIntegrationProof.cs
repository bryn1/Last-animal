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
//
// Run:  $GODOT --headless --path <proj> --script res://ci_proofs/RuntimeIntegrationProof.cs
//
// File size: >400 total lines — a multi-mode proof harness (8+ modes). The
// mode-specific stage bodies live in the partial-class halves
// RuntimeIntegrationProof.Save.cs (stages 20/21/22/30) and
// RuntimeIntegrationProof.Interact.cs (stages 40/90); this file keeps the
// shared harness (fields, composition, dispatch, main chain, helpers).
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

    public override void _Initialize()
    {
        GD.Print("LA_GATE: start");
        _mode = System.Environment.GetEnvironmentVariable("LA_GATE_MODE") ?? "positive";
        GD.Print($"LA_GATE: mode={_mode}");

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
        // quest_neg: the story/quest gate seam (MC 3904 2c) must be off BEFORE
        // the director's _Ready runs InitStory — same set-before-AddChild idiom.
        if (_mode == "quest_neg" && main is WorldDirector dneg) dneg.SetQuestHooksEnabled(false);
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
        int budget = _mode == "quest_arc" ? QuestFrameBudget : FrameBudget;
        if (_frames > budget) { Fail("frame budget exhausted before all stages"); return true; }

        switch (_stage)
        {
            case 0:
                // Baseline was captured in _ComposeDeferred BEFORE the press
                // (MC 1344.1) — go straight to the movement stage.
                _stage = 1;
                _stageFrames = 0;
                break;

            case 1:
                // Count physics ticks SINCE THE PRESS (MC 1344.1): process frames
                // are uncapped headless, physics ticks are the real engine time.
                if (_physFrames >= _pressPhysFrame + MoveFrames)
                {
                    Vector3 now = _playerBody.GlobalPosition;
                    float dx = now.X - _playerStart.X;
                    float dz = now.Z - _playerStart.Z;
                    if (!(dx > 0.05f || dz > 0.05f))
                    {
                        Fail($"player did not move on simulated WASD (dx={dx:0.###}, dz={dz:0.###})");
                        return true;
                    }
                    // The body's movement must reach the DIRECTOR-OWNED
                    // PlayerController (single source of truth), resolved
                    // through GameBootstrap — instance identity, not class.
                    var resolved = Root.GetNodeOrNull<GameBootstrap>("/root/GameBootstrap")?.Resolve<PlayerController>();
                    if (resolved == null) { Fail("GameBootstrap.Resolve<PlayerController>() returned null — director did not bind its controller"); return true; }
                    // no_controller mode INJECTS the decoy binding — the identity
                    // mismatch is the defect under test, detected in stage 2 via
                    // the AI-target mismatch (NEG_CONTROLLER). Skip here (MC 1344.1).
                    if (_mode != "no_controller" && !ReferenceEquals(resolved, _director.PlayerModel))
                    {
                        Fail("resolved PlayerController is NOT the director-owned instance (two live controllers)");
                        return true;
                    }
                    float modelDx = resolved.Position.X - _director.PlayerModel.Position.X;
                    // no_controller mode: the decoy position diverges by design —
                    // that divergence is the defect under test (NEG_CONTROLLER in
                    // stage 2), not a failure here (MC 1344.1).
                    if (_mode != "no_controller" && System.Math.Abs(modelDx) > 0.0001f)
                    {
                        Fail("resolved controller position diverged from the director's model");
                        return true;
                    }
                    Input.ActionRelease("move_right");
                    GD.Print("LA_GATE: PLAYER_EXISTS_MOVED — WASD -> body -> director-owned PlayerController (instance identity via GameBootstrap)");
                    TeleportIntoRange();
                    // MC 1345 (option A): capture the follow baseline HERE, right
                    // after the teleport — the companion is still at the player's
                    // pre-teleport position, so dist0 honestly reads "started away"
                    // and the close-in assert in stage 4 is exercised as written.
                    // Capturing at stage 4 raced the companion's convergence during
                    // the attack wait, and the check could fire on the capture frame
                    // itself (_followStartPhys defaults 0), reading field defaults
                    // (dist0=0) instead of a measurement.
                    _companionStart = _companion.GlobalPosition;
                    _followDist0 = _companion.GlobalPosition.DistanceTo(_playerBody.GlobalPosition);
                    _followStartPhys = _physFrames;
                    GD.Print($"LA_GATE: follow baseline captured dist0={_followDist0:0.###}");
                    _stage = 2;
                    _stageFrames = 0;
                }
                break;

            case 2:
                if (_mode == "no_controller")
                {
                    // The decoy binding must be detectable: the AI's target is
                    // fed from the director's REAL controller, so with the
                    // binding swapped the proof asserts the mismatch and the
                    // gate expects the NEG marker + non-zero exit.
                    var decoy = Root.GetNodeOrNull<GameBootstrap>("/root/GameBootstrap")!.Resolve<PlayerController>();
                    if (decoy != null && !ReferenceEquals(decoy, _director.PlayerModel)
                        && (System.Math.Abs(decoy.Position.X - _director.PlayerModel.Position.X) > 1f))
                    {
                        GD.Print("LA_GATE: NEG_CONTROLLER: EnemyAI target != authoritative PlayerController.Position (decoy binding detected)");
                        Quit(1);
                        return true;
                    }
                    if (_stageFrames > 60) { Fail("no_controller: decoy binding was not detectable"); return true; }
                    break;
                }

                if (_dnaCount > 0)
                {
                    Input.ActionRelease("attack");
                    if (_mode == "no_dna")
                    {
                        // The kill happened but the director's forwarding was
                        // blocked — the bus must NOT have heard it. If it did,
                        // the break failed (gate broken).
                        Fail("no_dna: DnaExtracted reached the bus despite forwarding disabled — the negative control is broken");
                        return true;
                    }
                    if (_mode == "no_bus")
                    {
                        // The bus fired but the HUD was disconnected: the meter
                        // must NOT have moved. That is the DETECTED break.
                        if (_hud.DnaMeter > _dnaBefore)
                        {
                            Fail("no_bus: Hud.DnaMeter moved despite the HUD being disconnected — the negative control is broken");
                            return true;
                        }
                        GD.Print("LA_GATE: NEG_BUS: DnaExtracted fired but Hud.DnaMeter did not move (HUD disconnected) — break detected");
                        Quit(1);
                        return true;
                    }
                    Check("Hud.DnaMeter incremented by the kill's DnaExtracted",
                          _hud.DnaMeter > _dnaBefore, $"dna={_hud.DnaMeter} (before {_dnaBefore})");
                    if (_failed) return true;
                    GD.Print("LA_GATE: DNA_EXTRACTED_EMITTED — kill via the REAL CombatSystem path -> EventBus.DnaExtracted -> Hud.DnaMeter");
                    if (_mode == "save")
                    {
                        // Save mode skips the HUD/companion stages: the kill
                        // chain already produced the DNA>0 baseline the save
                        // round-trip needs (MC 1344 non-vacuous gate).
                        _stage = 20;
                    }
                    else
                    {
                        _stage = 3;
                    }
                    _stageFrames = 0;
                }
                else if (_stageFrames > AttackBudgetFrames)
                {
                    // no_dna mode: the kill DOES land (forwarding is blocked, so the
                    // bus counter never moves — that is the point). Detect the kill
                    // via the enemy's death and assert the bus stayed silent.
                    if (_mode == "no_dna")
                    {
                        bool anyDead = _enemies.Any(e => e.IsDead);
                        if (anyDead && _dnaCount == 0)
                        {
                            Input.ActionRelease("attack");
                            GD.Print("LA_GATE: NEG_DNA: kill landed (enemy dead) but DnaExtracted never reached the bus (forwarding blocked) — break detected");
                            Quit(1);
                            return true;
                        }
                    }
                    Fail("kill did not land within the attack budget (input -> director -> CombatSystem path broken)");
                }
                else
                {
                    // Press attack 2 frames, release 2, repeat: the director's
                    // attack path fires on IsActionJustPressed with the player
                    // in range.
                    _attackToggle++;
                    if (_attackToggle % 4 == 1) Input.ActionPress("attack");
                    else if (_attackToggle % 4 == 3) Input.ActionRelease("attack");
                }
                break;

            case 3:
                {
                    // HUD_REFLECTS_STATE: the HUD mirrors the director-owned
                    // model (Life) and the bus (DnaMeter).
                    Check("Hud.Life matches the director-owned PlayerController.Health",
                          _hud.Life == _director.PlayerModel.Health,
                          $"hud={_hud.Life} model={_director.PlayerModel.Health}");
                    if (_failed) return true;
                    GD.Print("LA_GATE: HUD_REFLECTS_STATE — Hud.Life == PlayerController.Health (single health tracker)");
                    _stage = 4;
                    _stageFrames = 0;
                }
                break;

            case 4:
                // Gate on PHYSICS ticks (MC 1344.1): the companion's lerp is
                // delta-based, so its progress tracks ENGINE TIME. Process frames
                // and physics ticks diverge under Xvfb (a process frame can span
                // many ticks), so a window counted in process frames measures the
                // wrong thing in both directions. 40 physics ticks = 0.67s of
                // engine time regardless of frame rate. The baseline was captured
                // at the teleport (stage 1, MC 1345) — see _followStartPhys.
                if (_physFrames >= _followStartPhys + FollowFrames)
                {
                    // COMPANION_FOLLOWS: the machine-wired CompanionEntity
                    // trails the player.
                    float d1 = _companion.GlobalPosition.DistanceTo(_playerBody.GlobalPosition);
                    float moved = _companion.GlobalPosition.DistanceTo(_companionStart);
                    if (moved < 0.2f || d1 > _followDist0 - 0.25f)
                    {
                        Fail($"companion did not follow (moved={moved:0.###}, dist {_followDist0:0.###} -> {d1:0.###}) playerNow={_playerBody.GlobalPosition} companionNow={_companion.GlobalPosition}");
                        return true;
                    }
                    GD.Print($"LA_GATE: COMPANION_FOLLOWS — CompanionEntity (machine-wired) closed on the player (dist {_followDist0:0.###} -> {d1:0.###})");
                    _stage = 5;
                    _stageFrames = 0;
                }
                break;

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
                if (_physFrames >= _holdStartPhys + HoldFrames) { Quit(0); return true; }
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
        }
        return false;
    }

    public override void _Finalize()
    {
        Input.ActionRelease("move_right");   // unconditional: never leak a pressed action
        Input.ActionRelease("attack");
        Input.ActionRelease("interact");
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
