// SIZE: >400 (441 l) — CI proof harness, test-class ceiling 600 (MC 3910 added the death_load save-ownership leg to the 397-l file; MC 10126 added the rooted-fields release, +22 l); ONE SceneTree state machine per MC 3895 DA P2-1.
using Godot;
using LastAnimal.Combat;
using LastAnimal.Core;
using LastAnimal.Save;
using LastAnimal.World;
using System.Collections.Generic;

// Last Animal — zone-travel + boss-phase runtime proof (MC 1344 DA findings
// 3+4, C15). Companion to RuntimeIntegrationProof; owns the NEW behaviour
// only, so that file stays under its concern:
//
//   zone_travel — the travel action (input map "travel", T) must move the
//     player meadow -> canyon -> ruins: CurrentZone changes, the player body
//     is repositioned at the zone entry, the zone's SpawnSet is re-applied,
//     and the spawned enemies carry the SpawnSet's SCALED stats (not the
//     hardcoded per-type defaults). Markers: ZONE_TRAVEL_CANYON,
//     ZONE_TRAVEL_RUINS, SCALED_STATS_APPLIED.
//
//   boss_phase — kills grow the spoken-DNA history; once observed >=
//     BossThreshold(4) the next zone entry must field a live boss
//     (BOSS_REACHED; MC 1348 A5: a zone holds exactly its denizen count, so
//     the threshold can span zone entries), and further kills must step
//     BossController.Phase and fire C2 EcosystemAdapted on the REAL autoload
//     bus (BOSS_PHASE_FIRED).
//
//   death_load — MC 1348 N1: save while alive (F5 path), kill the player
//     through the model, assert the SHELL stops moving (dead = no input
//     movement), then LoadGame() (F9 path) must restore health, clear IsDead
//     and make movement work again. MC 3910 (DA W2): the phase OWNS its save
//     — the ONE shared user://savegame.json is deleted before the DEATH_SAVE
//     leg and the loaded health must equal this run's health stamp, so a
//     stale file from a sibling mode can never pass off. Markers:
//     DEATH_SAVE_OWNED, DEATH_MOVEMENT_STOPPED, DEATH_LOAD_RESURRECTED,
//     DEATH_MOVEMENT_RESTORED.
//
// Run:  $GODOT --headless --path <proj> --script res://ci_proofs/ZoneBossProof.cs
public partial class ZoneBossProof : SceneTree
{
    private const int FrameBudget = 6000;
    private const int PressFrames = 6;      // press-travel settle window
    private const int BossThreshold = 4;    // EcosystemSpawner.BossThreshold
    private const int PhaseStepTarget = 6;  // observed count that crosses a phase step
    // MC 3910: death_load stamps the saved PlayerHealth with this value (via
    // TakeDamage, the model's only damage entry) so the load below asserts
    // content written by THIS run, not a sibling mode's stale save (DA W2:
    // one shared user://savegame.json + existence-only save check = stale
    // resurrection). Ownership is provable because the file is deleted
    // immediately before this write; the stamp makes the LOAD assert it too.
    private const int DeathHealthStamp = 42;

    private EventBus? _bus;
    private WorldDirector? _director;
    private CharacterBody3D? _playerBody;
    private Node? _main;
    private int _adaptedCount;
    private int _travelToggle;
    private int _attackToggle;
    private string _thresholdZone = "";   // zone at the BossThreshold crossing
    private Vector3 _spawnOrigin;
    private string _mode = "zone_travel";
    private int _stage;
    private int _stageFrames;
    private bool _failed;
    private bool _asserted;
    private int _healthBeforeSave;
    private Vector3 _deadPos;
    private int _moveToggle;

    public override void _Initialize()
    {
        GD.Print("LA_GATE: start");
        _mode = System.Environment.GetEnvironmentVariable("LA_GATE_MODE") ?? "zone_travel";
        GD.Print($"LA_GATE: mode={_mode}");
        Engine.MaxFps = 60;

        var packed = GD.Load<PackedScene>("res://main.tscn");
        if (packed == null) { Fail("cannot load res://main.tscn"); return; }
        _main = packed.Instantiate<Node3D>();
        Root.AddChild(_main);
    }

    private bool Compose()
    {
        _bus = Root.GetNodeOrNull<EventBus>("/root/EventBus");
        _director = _main as WorldDirector;
        _playerBody = _director?.GetNodeOrNull<CharacterBody3D>("Player");
        if (_bus == null) { Fail("EventBus autoload not present"); return false; }
        if (_director == null) { Fail("main.tscn root is not WorldDirector"); return false; }
        if (_playerBody == null) { Fail("Player node not found"); return false; }
        _bus.EcosystemAdapted += _ => _adaptedCount++;
        _spawnOrigin = _playerBody.GlobalPosition;
        GD.Print($"LA_GATE: composed — zone={_director.CurrentZone} enemies={_director.Enemies.Count} origin={_spawnOrigin}");
        return true;
    }

    public override bool _Process(double delta)
    {
        if (_failed) return true;
        if (_director == null && !Compose()) return true;
        if (_director == null || _playerBody == null || _bus == null) return true;

        _stageFrames++;
        if (_stageFrames > FrameBudget) { Fail("frame budget exhausted"); return true; }

        switch (_stage)
        {
            case 0:
                _stage = _mode == "boss_phase" ? 20 : (_mode == "death_load" ? 30 : 10);
                _stageFrames = 0;
                break;

            // ---- zone_travel: first travel must land in canyon -------------
            case 10:
                if (PressTravel() && _director.CurrentZone == "canyon")
                {
                    Check("player repositioned at the zone entry on travel",
                          _playerBody.GlobalPosition.DistanceTo(_spawnOrigin) < 1.0f,
                          $"pos={_playerBody.GlobalPosition} origin={_spawnOrigin}");
                    if (_failed) return true;
                    Check("canyon SpawnSet re-applied (enemies spawned from it)",
                          _director.Enemies.Count > 0, $"enemies={_director.Enemies.Count}");
                    if (_failed) return true;
                    Check("spawned enemies carry the SpawnSet's SCALED stats (not type defaults)",
                          AnyScaledEnemy(), DescribeEnemies());
                    if (_failed) return true;
                    GD.Print("LA_GATE: SCALED_STATS_APPLIED — SpawnSet Health/Damage/Speed live on the AI");
                    GD.Print("LA_GATE: ZONE_TRAVEL_CANYON — canyon reachable in play");
                    _stage = 11;
                    _stageFrames = 0;
                    _travelToggle = 0;   // re-arm the just-pressed window
                }
                else if (_stageFrames > 120) Fail("travel did not reach canyon");
                break;

            // ---- zone_travel: second travel must land in ruins -------------
            case 11:
                if (PressTravel() && _director.CurrentZone == "ruins")
                {
                    Check("ruins SpawnSet re-applied", _director.Enemies.Count > 0,
                          $"enemies={_director.Enemies.Count}");
                    if (_failed) return true;
                    GD.Print("LA_GATE: ZONE_TRAVEL_RUINS — ruins reachable in play");
                    GD.Print("LA_GATE: PASS — zone travel verified (meadow -> canyon -> ruins)");
                    _asserted = true;
                    ReleaseHeldRefsBeforeQuit();   // MC 10126 rooted-fields idiom
                    Quit(0);
                    return true;
                }
                if (_stageFrames > 120) Fail("travel did not reach ruins");
                break;

            // ---- boss_phase: kill through the REAL path until observed >= 4.
            // MC 1348 A5: a zone fields exactly its denizen count (meadow 3 —
            // the old boot ring is stood down), so reaching BossThreshold(4)
            // spans zone entries: when the pool runs dry, travel on and keep
            // killing in the next zone's fresh set.
            case 20:
                if (_director.SpokenDna.Count >= BossThreshold)
                {
                    GD.Print($"LA_GATE: observed={_director.SpokenDna.Count} >= BossThreshold — travelling on");
                    _stage = 21;
                    _stageFrames = 0;
                    _travelToggle = 0;   // re-arm the just-pressed window
                }
                else if (!KillLoop())
                {
                    GD.Print($"LA_GATE: zone pool dry at observed={_director.SpokenDna.Count} — travelling on for a fresh set");
                    _stage = 21;
                    _stageFrames = 0;
                    _travelToggle = 0;
                }
                break;

            // ---- boss_phase: reach BossThreshold, then the NEXT zone entry
            // must field the live boss ----------------
            case 21:
                if (_director.SpokenDna.Count >= BossThreshold)
                {
                    _thresholdZone = _director.CurrentZone;
                    _stage = 23;
                    _stageFrames = 0;
                    _travelToggle = 0;
                }
                else if (!KillLoop())
                {
                    PressTravel();   // pool dry — the next entry fields a fresh set
                }
                else if (_stageFrames > 1200) Fail("kill path never reached BossThreshold");
                break;

            // ---- boss_phase: the first entry past the threshold fields the boss
            case 23:
                if (PressTravel() && _director.CurrentZone != _thresholdZone)
                {
                    Check("a zone entered past BossThreshold fields a live boss",
                          _director.HasLiveBoss, $"zone={_director.CurrentZone} bossPhase={_director.BossPhase}");
                    if (_failed) return true;
                    GD.Print("LA_GATE: BOSS_REACHED — boss enemy live in play (was unreachable before)");
                    _stage = 22;
                    _stageFrames = 0;
                }
                else if (_stageFrames > 240) Fail("travel did not leave the threshold zone (boss stage)");
                break;

            // ---- boss_phase: more kills must step the phase + fire C2 -------
            case 22:
                if (_adaptedCount > 0)
                {
                    Check("BossController.Phase transition fired C2 EcosystemAdapted",
                          _adaptedCount > 0, $"adapted={_adaptedCount} phase={_director.BossPhase}");
                    if (_failed) return true;
                    GD.Print("LA_GATE: BOSS_PHASE_FIRED — phase transition emitted EcosystemAdapted and re-fielded the SpawnSet");
                    GD.Print("LA_GATE: PASS — boss reachability + phase behaviour verified");
                    _asserted = true;
                    ReleaseHeldRefsBeforeQuit();   // MC 10126 rooted-fields idiom
                    Quit(0);
                    return true;
                }
                if (_director.SpokenDna.Count >= PhaseStepTarget && _stageFrames > 60)
                    Fail($"no EcosystemAdapted after observed={_director.SpokenDna.Count} (phase wire broken)");
                else if (!KillLoop())
                    PressTravel();   // pool dry — the next entry fields a fresh set
                break;

            // ---- death_load: own the save, save alive, kill, shell must stop,
            // load rescues. MC 3910 (DA W2): every gate mode shares the ONE
            // user://savegame.json, and the old existence-only check passed on
            // a stale file written by an earlier mode — the load then rescued
            // THAT content (order-dependent red/green). This phase now owns its
            // save: delete the path before the write, stamp health so the
            // RESURRECTED check below asserts content written by THIS run.
            case 30:
                {
                    var store = new GodotSaveStore();
                    if (System.IO.File.Exists(store.SavePath))
                        System.IO.File.Delete(store.SavePath);
                    _director!.PlayerModel.TakeDamage(_director.PlayerModel.MaxHealth - DeathHealthStamp);
                    _healthBeforeSave = _director.PlayerModel.Health;
                    _director.SaveGame();
                    Check("DEATH_SAVE_WRITTEN: save file exists at the globalized user:// path",
                          System.IO.File.Exists(store.SavePath), $"path={store.SavePath}");
                    if (_failed) return true;
                    GD.Print($"LA_GATE: DEATH_SAVE_OWNED — stale save removed, this write stamped health={_healthBeforeSave}");
                    // Kill through the model (the only damage entry point).
                    _director.PlayerModel.TakeDamage(_healthBeforeSave);
                    Check("player dead after lethal damage", _director.PlayerModel.IsDead,
                          $"health={_director.PlayerModel.Health}");
                    if (_failed) return true;
                    _deadPos = _playerBody!.GlobalPosition;
                    _stage = 31;
                    _stageFrames = 0;
                }
                break;

            case 31:
                // Hold move_right while dead: the shell must not translate.
                Input.ActionPress("move_right");
                if (_stageFrames >= 30)
                {
                    Input.ActionRelease("move_right");
                    Check("DEATH_MOVEMENT_STOPPED: dead player body does not translate under held input",
                          XzDist(_playerBody!.GlobalPosition, _deadPos) < 0.05f,
                          $"moved={XzDist(_playerBody.GlobalPosition, _deadPos):0.###}");
                    if (_failed) return true;
                    GD.Print("LA_GATE: DEATH_MOVEMENT_STOPPED");
                    _stage = 32;
                    _stageFrames = 0;
                }
                break;

            case 32:
                {
                    // F9 from the death state: health restored, IsDead cleared.
                    // Spawning is gated off first so the re-fielded enemy ring
                    // cannot interfere with the movement window below (the
                    // death assertions themselves do not involve enemies).
                    _director!.SetSpawningEnabled(false);
                    _director.LoadGame();
                    Check("DEATH_LOAD_RESURRECTED: load restores saved health and clears IsDead",
                          !_director.PlayerModel.IsDead && _director.PlayerModel.Health == _healthBeforeSave,
                          $"health={_director.PlayerModel.Health} (saved {_healthBeforeSave})");
                    if (_failed) return true;
                    GD.Print("LA_GATE: DEATH_LOAD_RESURRECTED");
                    _deadPos = _playerBody!.GlobalPosition;
                    _stage = 33;
                    _stageFrames = 0;
                }
                break;

            case 33:
                Input.ActionPress("move_right");
                if (_stageFrames >= 30)
                {
                    Input.ActionRelease("move_right");
                    float moved = XzDist(_playerBody!.GlobalPosition, _deadPos);
                    Check("DEATH_MOVEMENT_RESTORED: revived player body translates under held input",
                          moved > 0.5f && !_director!.PlayerModel.IsDead,
                          $"moved={moved:0.###} health={_director!.PlayerModel.Health}");
                    if (_failed) return true;
                    GD.Print("LA_GATE: DEATH_MOVEMENT_RESTORED");
                    GD.Print("LA_GATE: PASS — death recovery verified (dead stops, load resurrects, movement works)");
                    _asserted = true;
                    ReleaseHeldRefsBeforeQuit();   // MC 10126 rooted-fields idiom
                    Quit(0);
                    return true;
                }
                break;
        }
        return false;
    }

    /// <summary>
    /// MC 10126 (10026.13.6): same latent class as MC 10112's BridgeMvpProof fix —
    /// the proof is the C# MainLoop, the LAST managed object standing at shutdown.
    /// Its live-scene fields root the scene's C# wrappers until after the native
    /// ObjectDB is gone, so their GC finalizers hit freed objects ("Leaked unsafe
    /// reference ... csharp_script.cpp:179", exit 134/139). All three PASS Quits
    /// here (zone_travel/boss_phase/death_load) root the fields Compose() assigns.
    /// Release the wrappers and flush finalizers BEFORE Quit, while the ObjectDB is
    /// alive. Idiom REUSED from BridgeMvpProof.ReleaseHeldRefsBeforeQuit (MC 10112),
    /// as extended by MC 10117. Bypass Quit(1) Fail paths stay untouched (10117 scope).
    /// </summary>
    private void ReleaseHeldRefsBeforeQuit()
    {
        _bus = null; _director = null; _playerBody = null; _main = null;
        System.GC.Collect();
        System.GC.WaitForPendingFinalizers();
        System.GC.Collect();
    }

    /// <summary>Planar (XZ) distance — gravity jitter on Y must not count as
    /// input movement in the death-path checks.</summary>
    private static float XzDist(Vector3 a, Vector3 b)
    {
        b.Y = a.Y;
        return a.DistanceTo(b);
    }

    /// <summary>Press travel for one frame out of PressFrames (just-pressed idiom).
    /// Returns true when the press window has elapsed so the zone check is fair.</summary>
    private bool PressTravel()
    {
        _travelToggle++;
        if (_travelToggle == 1) Input.ActionPress("travel");
        if (_travelToggle >= PressFrames)
        {
            // Re-arm (MC 1348 A5 follow-up): the frame stamp that makes
            // IsActionJustPressed true is written only on the released->pressed
            // transition, so a cycle left held (KillLoop took over mid-press)
            // must be released AND the toggle restarted, or the next stage's
            // press is a silent no-op and no travel edge ever fires.
            Input.ActionRelease("travel");
            _travelToggle = 0;
            return true;
        }
        return false;
    }

    /// <summary>One kill-loop frame: keep the nearest live enemy in melee range
    /// and toggle attack, exactly like RuntimeIntegrationProof's kill stages.
    /// Returns false when no farmable enemy remains — the zone's pool is dry
    /// (MC 1348 A5: a zone fields exactly its denizen count, the boot ring is
    /// gone) — so the caller can travel on and keep killing in a fresh set.</summary>
    private bool KillLoop()
    {
        EnemyActor? target = null;
        float best = float.MaxValue;
        foreach (var e in _director!.Enemies)
        {
            if (e.IsDead || !IsInstanceValid(e)) continue;
            // Never farm the boss itself: killing it before the phase step
            // would silence the very transition this mode proves.
            if (ReferenceEquals(e, _director.BossActor)) continue;
            float d = e.GlobalPosition.DistanceTo(_playerBody!.GlobalPosition);
            if (d < best) { best = d; target = e; }
        }
        if (target == null) return false;
        if (best > _director.PlayerModel.AttackRange)
        {
            Vector3 p = target.GlobalPosition;
            _playerBody!.GlobalPosition = new Vector3(p.X - 0.8f, p.Y, p.Z);
        }
        _attackToggle++;
        if (_attackToggle % 4 == 1) Input.ActionPress("attack");
        else if (_attackToggle % 4 == 3) Input.ActionRelease("attack");
        return true;
    }

    /// <summary>True when any live spawned enemy's AI stats differ from the
    /// hardcoded per-type defaults — i.e. the SpawnSet's scaled table is live.</summary>
    private bool AnyScaledEnemy()
    {
        foreach (var e in _director!.Enemies)
        {
            if (e.IsDead) continue;
            if (e.Ai.Health != BaseHealth(e.Ai.EnemyType)) return true;
        }
        return false;
    }

    private string DescribeEnemies()
    {
        var parts = new List<string>();
        foreach (var e in _director!.Enemies)
            parts.Add($"{e.Ai.EnemyType}:hp={e.Ai.Health}");
        return string.Join(",", parts);
    }

    private static int BaseHealth(EnemyAI.Type t) => t switch
    {
        EnemyAI.Type.Goblin => 30,
        EnemyAI.Type.Orc => 60,
        EnemyAI.Type.Skeleton => 65,   // MC 10183 R2: tracks the EnemyAI base table
        EnemyAI.Type.Wraith => 80,     // MC 10216 S18: tracks the EnemyAI base table (hollow denizen)
        _ => 100, // Demon
    };

    private void Check(string what, bool ok, string detail)
    {
        GD.Print($"LA_GATE: check: {what}: {(ok ? "ok" : "FAIL")} {detail}");
        if (!ok) Fail(what);
    }

    private void Fail(string why)
    {
        Input.ActionRelease("travel");
        Input.ActionRelease("attack");
        _failed = true;
        GD.PrintErr($"LA_GATE: FAIL — {why}");
        Quit(1);
    }

    public override void _Finalize()
    {
        Input.ActionRelease("travel");
        Input.ActionRelease("attack");
        Input.ActionRelease("move_right");
        if (!_asserted && !_failed)
            GD.PrintErr("LA_GATE: FAIL — finished without asserting all stages");
    }
}
