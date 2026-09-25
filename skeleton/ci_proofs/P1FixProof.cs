using Godot;
using LastAnimal.Companion;
using LastAnimal.Core;
using LastAnimal.World;
using System.Collections.Generic;

// Last Animal — MC 1348 P1 gameplay-bug regression proofs (A2/A3/A4/A5).
//
// Companion to RuntimeIntegrationProof/ZoneBossProof; owns the four P1
// regression modes only, so those files stay under their concern. Each mode
// reproduces one audited P1 against the REAL playable scene and prints its
// marker ONLY after its assertion passes; a mode that never asserts fails via
// the frame budget. Every mode uses only surface that already existed before
// its fix, so each fix can be proven red against the pre-fix tree.
//
// Modes (env LA_GATE_MODE, default "corpse_damage"):
//   corpse_damage — A2: kill an enemy on the frame it attacks; the corpse must
//                   not keep dealing its frozen last-tick damage every frame.
//                   Markers: CORPSE_DAMAGE_STOPPED.
//   wage_betrayal — A3: the wage/betrayal pillar reachable in play — with the
//                   pay input never pressed, a due wage is skipped once per pay
//                   interval, loyalty drains to 0 and the C7 betrayal executes
//                   (bond broken, C2 Betrayal fired). Markers: BETRAYAL_FIRED.
//
// Run:  $GODOT --headless --path <proj> --script res://ci_proofs/P1FixProof.cs
public partial class P1FixProof : SceneTree
{
    private const int FrameBudget = 6000;
    private const int WatchFrames = 90;   // corpse-damage watch window (~1.5 s)
    private const int BetrayalBudget = 3600; // wage stage: grace 20 s + interval 30 s + margin

    private EventBus? _bus;
    private WorldDirector? _director;
    private CharacterBody3D? _playerBody;
    private Node? _main;
    private string _mode = "corpse_damage";
    private int _stage;
    private int _stageFrames;
    private bool _failed;
    private bool _asserted;
    private EnemyActor? _victim;
    private int _victimDamage;
    private int _healthAtKill;
    private int _betrayalCount;

    public override void _Initialize()
    {
        GD.Print("LA_GATE: start");
        _mode = System.Environment.GetEnvironmentVariable("LA_GATE_MODE") ?? "corpse_damage";
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
        _bus.Betrayal += (_, _) => _betrayalCount++;
        GD.Print($"LA_GATE: composed — zone={_director.CurrentZone} enemies={_director.Enemies.Count}");
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
                _stage = _mode == "wage_betrayal" ? 20 : 10;
                _stageFrames = 0;
                break;

            // ---- corpse_damage (A2): kill the attacker on its attack frame ----
            case 10:
                if (_victim == null)
                {
                    TeleportToNearestLiveEnemy();
                    break;
                }
                if (_victim.IsDead)
                {
                    Fail("victim died before its attack frame (engage broken)");
                    return true;
                }
                if (_victim.DamageDealt > 0)
                {
                    // The attack frame: the victim's last live physics tick dealt
                    // damage. Kill it through the ONE kill path exactly as the
                    // director's TryAttack does (DealDamage -> KillHide), so the
                    // corpse freezes a non-zero DamageDealt.
                    _victimDamage = _victim.DamageDealt;
                    _director.Combat.DealDamage(attackerId: 0, _victim.Ai, _victim.Ai.Health);
                    if (!_victim.IsDead) { Fail("victim survived a lethal DealDamage"); return true; }
                    _victim.KillHide();
                    // Leave the corpse's neighbourhood: no OTHER enemy may reach
                    // the player during the watch window, so any further damage
                    // can only come from the corpse.
                    Vector3 p = _playerBody.GlobalPosition;
                    _playerBody.GlobalPosition = new Vector3(p.X + 40f, p.Y, p.Z + 40f);
                    _healthAtKill = _director.PlayerModel.Health;
                    GD.Print($"LA_GATE: victim killed on its attack frame (frozen damage {_victimDamage}); watching {WatchFrames} frames from health {_healthAtKill}");
                    _stage = 11;
                    _stageFrames = 0;
                }
                else if (_stageFrames > 600) Fail("victim never attacked (teleport into range broken)");
                break;

            case 11:
                if (_stageFrames >= WatchFrames)
                {
                    Check("CORPSE_DAMAGE_STOPPED: the dead enemy's frozen DamageDealt deals nothing",
                          _director.PlayerModel.Health == _healthAtKill,
                          $"health={_director.PlayerModel.Health} (at kill {_healthAtKill}, frozen corpse damage {_victimDamage})");
                    if (_failed) return true;
                    GD.Print("LA_GATE: CORPSE_DAMAGE_STOPPED — corpse damage loop gone (MC 1348 A2)");
                    GD.Print("LA_GATE: PASS — corpse-damage regression verified");
                    _asserted = true;
                    Quit(0);
                    return true;
                }
                break;

            // ---- wage_betrayal (A3): withhold the wage, loyalty drains, betrayal fires ----
            case 20:
            {
                // Stand clear of every enemy so nothing but the wage cycle can
                // move loyalty, then drain it to just above one skip's penalty
                // through M03 (the same API the save/load proof uses) — the
                // production settlement policy must do the rest.
                Vector3 p = _playerBody.GlobalPosition;
                _playerBody.GlobalPosition = new Vector3(p.X + 40f, p.Y, p.Z + 40f);
                var comp = _director.Companion.Companion;
                comp.ModifyLoyalty(-(comp.Loyalty - 3));
                int loyalty = comp.Loyalty;
                Check("wage_betrayal: loyalty drained to 3 through M03", loyalty == 3, $"loyalty={loyalty}");
                if (_failed) return true;
                GD.Print("LA_GATE: wage withheld from here on — waiting for the skip arm to drain loyalty");
                _stage = 21;
                _stageFrames = 0;
                break;
            }
            case 21:
                // The player never presses pay_wage: the wage comes due after the
                // grace, each full unpaid interval is one skipped cycle, loyalty
                // hits 0 and the machine mirrors M03's betrayal. Wait for the C2
                // Betrayal signal — the definitive marker that the director
                // EXECUTED the betrayal (the visible entity ticks the same
                // machine, so the state can flip one tick before the director's
                // transition block runs).
                if (_betrayalCount > 0)
                {
                    var comp = _director.Companion.Companion;
                    Check("BETRAYAL_FIRED: loyalty drained to 0 by withheld wages", comp.Loyalty == 0, $"loyalty={comp.Loyalty}");
                    Check("BETRAYAL_FIRED: the machine mirrored the betrayal", _director.Companion.State == CompanionState.Betrayed, $"state={_director.Companion.State}");
                    Check("BETRAYAL_FIRED: ExecuteBetrayal ran — the bond is broken", !comp.HasCompanion, $"hasCompanion={comp.HasCompanion}");
                    if (_failed) return true;
                    GD.Print("LA_GATE: BETRAYAL_FIRED — wage/betrayal pillar reachable in play (MC 1348 A3)");
                    GD.Print("LA_GATE: PASS — wage/betrayal regression verified");
                    _asserted = true;
                    Quit(0);
                    return true;
                }
                if (_stageFrames > BetrayalBudget)
                    Fail($"betrayal did not fire within {BetrayalBudget} frames (loyalty={_director.Companion.Companion.Loyalty}, state={_director.Companion.State})");
                break;
        }
        return false;
    }

    private void TeleportToNearestLiveEnemy()
    {
        EnemyActor? target = null;
        float best = float.MaxValue;
        foreach (var e in _director!.Enemies)
        {
            if (e.IsDead || !IsInstanceValid(e)) continue;
            float d = e.GlobalPosition.DistanceTo(_playerBody!.GlobalPosition);
            if (d < best) { best = d; target = e; }
        }
        if (target == null) { Fail("no live enemy to engage"); return; }
        Vector3 p = target.GlobalPosition;
        _playerBody!.GlobalPosition = new Vector3(p.X - 0.8f, p.Y, p.Z);
        _victim = target;
        GD.Print($"LA_GATE: player teleported into range of {target.Name} at ({p.X:0.##},{p.Y:0.##},{p.Z:0.##})");
    }

    private void Check(string what, bool ok, string detail)
    {
        GD.Print($"LA_GATE: check: {what}: {(ok ? "ok" : "FAIL")} {detail}");
        if (!ok) Fail(what);
    }

    private void Fail(string why)
    {
        _failed = true;
        GD.PrintErr($"LA_GATE: FAIL — {why}");
        Quit(1);
    }

    public override void _Finalize()
    {
        if (!_asserted && !_failed)
            GD.PrintErr("LA_GATE: FAIL — finished without asserting all stages");
    }
}
