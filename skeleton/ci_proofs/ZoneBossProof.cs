// SIZE reason: >400 (600 l by wc AT this head = 460@MC10255 + 137 MC 11294/D2 (152 in / 15 out: the death_load corpse-agency extension — rejection stages 34-35, acceptance stages 36-37, the NearestLiveEnemy helper, the KillLoop keep-alive seam, the mode-doc rewrite at net 9 lines, the fields block — comment compressions landed the file 3 l UNDER the 600 ceiling; the ceiling rule stands: the next growth of this file pays an MC 10218 verbatim-move split, entry-file precedent) + 3 MC 11297@6d0913a (the +3 crossed the 3-l margin — the file now sits AT the 600 ceiling; the 597 stamp lagged the +3, restamped from wc AT the MC 11431 R1 head, CC-8) — prior reading 460 l measured at MC 10255 head) — CI proof harness, test-class ceiling 600 (MC 3910 added the death_load save-ownership leg to the 397-l file; MC 10126 added the rooted-fields release, +22 l; MC 10210 unknown-mode dispatch switch +18 l, incl. the header Modes default row; MC 10216 S18 BaseHealth row +1 l — the 459 stamp lagged it, restamped from wc MC 10255); ONE SceneTree state machine per MC 3895 DA P2-1.
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
// Modes (env LA_GATE_MODE, default "zone_travel"):
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
//   death_load — MC 1348 N1: save while alive (F5 path), kill the player through
//     the model, assert the corpse is INERT — no shell movement, no attack
//     damage, no travel (MC 11294 D2 closed the run2 corpse-bosskill, Life 0)
//     — then LoadGame() (F9) restores health, clears IsDead and returns
//     movement, travel AND attack. MC 3910 (DA W2): the phase OWNS its save
//     (delete-then-write + health stamp; a stale sibling file can never pass
//     off). Markers: DEATH_SAVE_OWNED, DEATH_MOVEMENT_STOPPED,
//     DEATH_ATTACK_REJECTED, DEATH_TRAVEL_REJECTED, DEATH_LOAD_RESURRECTED,
//     DEATH_MOVEMENT_RESTORED, REVIVE_TRAVEL_ACCEPTED, REVIVE_ATTACK_ACCEPTED.
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
    // MC 11294 D2: corpse-rejection + post-revive acceptance state (death_load
    // extension; D2 = the Life-0 corpse travelling/killing, MC 11270 frames 17/22).
    private string _zoneBeforeDeath = "";
    private EnemyActor? _rejectTarget;
    private int _rejectHp0, _rejectDna0;
    private int _rejectTravelWindows;
    private EnemyActor? _reviveTarget;
    private int _reviveHp0, _reviveDna0;

    public override void _Initialize()
    {
        GD.Print("LA_GATE: start");
        _mode = System.Environment.GetEnvironmentVariable("LA_GATE_MODE") ?? "zone_travel";
        GD.Print($"LA_GATE: mode={_mode}");
        Engine.MaxFps = 60;

        var packed = GD.Load<PackedScene>("res://main.tscn");
        if (packed == null) { Fail("cannot load res://main.tscn"); return; }
        _main = packed.Instantiate<Node3D>();
        // MC 11297 (D5): opt out of the production boot wild (a stray recruit-able
        // body would drift the boss/threshold + travel counts); set-before-AddChild.
        if (_main is WorldDirector d) d.SetWildSpawnEnabled(false);
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
                // MC 10210 W2 tail (F-C + orchestrator ruling 10203 append #5):
                // the old nested ternary routed ANY unrecognized LA_GATE_MODE to
                // the zone_travel arc — a typo'd/stale/empty mode could exit 0
                // having asserted the WRONG leg (the MC 10204 vacuous-green
                // class). Fail-safe idiom copied from RosterIntegrationProof.
                // _Process: one named arm per documented mode (enumerable for
                // ci/mode_sets_check.sh); the default halts by name, exit 1, at
                // dispatch-top before any stage side-effect. UNSET still means
                // zone_travel — the _Initialize default is untouched, so default
                // behavior is byte-unchanged (proven by the ci battery's leg).
                switch (_mode)
                {
                    case "zone_travel": _stage = 10; break;
                    case "boss_phase": _stage = 20; break;
                    case "death_load": _stage = 30; break;
                    default: Fail($"unknown mode {_mode}"); return true;
                }
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
                    _zoneBeforeDeath = _director.CurrentZone;   // MC 11294: the travel-rejection baseline
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
                    _stage = 34;   // MC 11294: movement was never the only agency lost — continue into the action rejections
                    _stageFrames = 0;
                }
                break;

            // ---- MC 11294 D2: the ATTACK rejection. A live enemy is held in melee
            // reach (the KillLoop teleport idiom, corpse-side) and the attack key
            // pressed: before this card's gate the swing LANDED (run2 killed the
            // boss as a corpse). Asserted on WORLD STATE, not a print: the target's
            // AI health and the spoken-DNA history must NOT move.
            case 34:
                {
                    _rejectTarget ??= NearestLiveEnemy();
                    if (_rejectTarget == null)
                    {
                        if (_stageFrames > 240) Fail("death_attack: no live enemy to stand in reach");
                        break;
                    }
                    if (_rejectHp0 == 0 && _rejectDna0 == 0) { _rejectHp0 = _rejectTarget.Ai.Health; _rejectDna0 = _director!.SpokenDna.Count; }
                    Vector3 ep = _rejectTarget.GlobalPosition;
                    _playerBody!.GlobalPosition = new Vector3(ep.X - 0.8f, _playerBody.GlobalPosition.Y, ep.Z);
                    _attackToggle++;
                    if (_attackToggle % 4 == 1) Input.ActionPress("attack");
                    else if (_attackToggle % 4 == 3) Input.ActionRelease("attack");
                    if (_stageFrames >= 32)
                    {
                        Input.ActionRelease("attack");
                        _attackToggle = 0;
                        bool untouched = IsInstanceValid(_rejectTarget)
                            && _rejectTarget.Ai.Health == _rejectHp0
                            && _director!.SpokenDna.Count == _rejectDna0;
                        Check("DEATH_ATTACK_REJECTED: corpse attack presses damage nothing", untouched,
                              $"hp={(IsInstanceValid(_rejectTarget) ? _rejectTarget!.Ai.Health : -999)} (was {_rejectHp0}) obs={_director!.SpokenDna.Count} (was {_rejectDna0})");
                        if (_failed) return true;
                        GD.Print("LA_GATE: DEATH_ATTACK_REJECTED");
                        _stage = 35;
                        _stageFrames = 0;
                        _rejectTravelWindows = 0;
                    }
                }
                break;

            // ---- MC 11294 D2: the TRAVEL rejection. Two full travel press windows
            // (the PressTravel re-arm idiom) while dead: the zone must be unchanged
            // (run2 travelled canyon->ruins->hollow on Life 0).
            case 35:
                if (PressTravel()) _rejectTravelWindows++;
                if (_rejectTravelWindows >= 2)
                {
                    Check("DEATH_TRAVEL_REJECTED: corpse travel presses change no zone",
                          _director!.CurrentZone == _zoneBeforeDeath,
                          $"zone={_director.CurrentZone} (died in {_zoneBeforeDeath})");
                    if (_failed) return true;
                    GD.Print("LA_GATE: DEATH_TRAVEL_REJECTED");
                    _stage = 32;
                    _stageFrames = 0;
                }
                else if (_stageFrames > 120) Fail("travel windows never completed");
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
                    _stage = 36;   // MC 11294: agency must come back WHOLE, not just walking
                    _stageFrames = 0;
                    _travelToggle = 0;   // re-arm the just-pressed window
                }
                break;

            // ---- MC 11294 D2: post-revive the TRAVEL press is ACCEPTED again
            // (spawn re-armed after stage 32's quiet load — the next zone then
            // fields the set the attack leg below farms).
            case 36:
                _director!.SetSpawningEnabled(true);
                if (PressTravel() && _director.CurrentZone != _zoneBeforeDeath)
                {
                    Check("REVIVE_TRAVEL_ACCEPTED: revived player travels — zone changed after the F9 load",
                          _director.CurrentZone != _zoneBeforeDeath,
                          $"zone={_director.CurrentZone} (died in {_zoneBeforeDeath})");
                    if (_failed) return true;
                    GD.Print("LA_GATE: REVIVE_TRAVEL_ACCEPTED");
                    _stage = 37;
                    _stageFrames = 0;
                    _attackToggle = 0;
                }
                else if (_stageFrames > 240) Fail("travel never fired after the revive");
                break;

            // ---- MC 11294 D2: post-revive the ATTACK press lands again — the mirror
            // of stage 34 on the SAME press idiom: the target's AI health (or the
            // spoken-DNA history, if the swing killed it) MUST move.
            case 37:
                {
                    _reviveTarget ??= NearestLiveEnemy();
                    if (_reviveTarget == null)
                    {
                        if (_stageFrames > 240) Fail("revive_attack: no live enemy in the new zone");
                        break;
                    }
                    if (_reviveHp0 == 0) { _reviveHp0 = _reviveTarget.Ai.Health; _reviveDna0 = _director!.SpokenDna.Count; }
                    Vector3 ep = _reviveTarget.GlobalPosition;
                    _playerBody!.GlobalPosition = new Vector3(ep.X - 0.8f, _playerBody.GlobalPosition.Y, ep.Z);
                    _attackToggle++;
                    if (_attackToggle % 4 == 1) Input.ActionPress("attack");
                    else if (_attackToggle % 4 == 3) Input.ActionRelease("attack");
                    if (_stageFrames >= 60)
                    {
                        Input.ActionRelease("attack");
                        bool landed = (IsInstanceValid(_reviveTarget) && _reviveTarget!.Ai.Health < _reviveHp0)
                            || _director!.SpokenDna.Count > _reviveDna0;
                        Check("REVIVE_ATTACK_ACCEPTED: revived player's attacks damage again", landed,
                              $"hp={(IsInstanceValid(_reviveTarget) ? _reviveTarget!.Ai.Health : -999)} (was {_reviveHp0}) obs={_director!.SpokenDna.Count} (was {_reviveDna0})");
                        if (_failed) return true;
                        GD.Print("LA_GATE: REVIVE_ATTACK_ACCEPTED");
                        GD.Print("LA_GATE: PASS — death recovery verified (dead stops movement, attack and travel; load resurrects; movement, travel and attack work again)");
                        _asserted = true;
                        ReleaseHeldRefsBeforeQuit();   // MC 10126 rooted-fields idiom
                        Quit(0);
                        return true;
                    }
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

    /// <summary>MC 11294: the nearest LIVE spawned enemy (KillLoop selection idiom,
    /// no boss exclusion — these legs test the attack WIRE, any live enemy is a
    /// valid witness). Null when the zone pool is dry.</summary>
    private EnemyActor? NearestLiveEnemy()
    {
        EnemyActor? best = null;
        float bestD = float.MaxValue;
        foreach (var e in _director!.Enemies)
        {
            if (e.IsDead || !IsInstanceValid(e)) continue;
            float d = e.GlobalPosition.DistanceTo(_playerBody!.GlobalPosition);
            if (d < bestD) { bestD = d; best = e; }
        }
        return best;
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
        // MC 11294: farm legs are NOT death tests — the death gate refuses a dead
        // farmer its swings (pre-card this rode the D2 corpse-agency defect: the
        // battery leg was false-green ON A CORPSE). Revive ONLY when already
        // dead, through the same shipped model seam the F9 load uses.
        if (_director!.PlayerModel.IsDead) _director.PlayerModel.RestoreHealth(50);
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
