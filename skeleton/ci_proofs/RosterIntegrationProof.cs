// SIZE: 278 l — MC 3943 2g roster proof (DA W5 F1 + W5 c3 F-A oversized-save
// trim leg). MC 10036 split (was 600 l, AT the ceiling): the roster_follow arc
// moved VERBATIM to RosterIntegrationProof.Follow.cs (partial half); this file
// keeps the mode router, the shared helpers and the roster_neg leg.
using Godot;
using LastAnimal.Companion;
using LastAnimal.Core;
using LastAnimal.Save;
using LastAnimal.Ui;
using LastAnimal.World;
using System.Collections.Generic;

// Last Animal — MC 3943 stage 2g roster runtime proof (code, 2026-10-02).
//
// Standalone SceneTree proof in the P1FixProof/ZoneBossProof shape: the roster
// chain driven against the REAL playable scene (WorldDirector + the Roster
// partial) through REAL input actions. NOTE (recorded deviation): the card
// named this file "RuntimeIntegrationProof.Roster.cs" as a partial, but the
// main RuntimeIntegrationProof.cs is on the 2g FORBIDDEN list and its stage
// switch is the only mode router that file has — a partial cannot register a
// new mode there. The house idiom for new-mode proofs is a standalone proof
// (run_mode in ci/runtime_integration_test.sh already takes a proof path);
// Godot requires file name == class name, hence this name.
//
// Modes (env LA_GATE_MODE, default "roster_follow"):
//   roster_follow — the full roster arc must PASS:
//     ROSTER_RECRUITED      interact-offer + pay_wage recruits wild #1 (in play)
//     ROSTER_TWO_RECRUITED  wild #2 joins (cap 3 not yet hit); FOLLOWERS_FOLLOW
//                           proves both new bodies trail the player
//     WAGE_INDEPENDENT_A/B  one pay press settles ONLY the due follower; each
//                           landed settle emits its own WagePaid (ARCH W2 — a
//                           singleton wage read mutes the non-boot follower and
//                           goes RED here); unique per-follower loyalty keys
//     ROSTER_RESTORED       save -> load restores N=3 (Followers list + rebuild)
//     CYCLE_SELECTED        cycle_follower pages the Empathy Book selection
//     FORGIVE_APPLIED       pay_wage under the book = Forgive bonus, NOT a wage
//     BREAK_BOND_SELECTED   break_bond betrays ONLY the selected follower; the
//                           others keep following (still bonded, no betrayal for them)
//     PAY_AFTER_BREAK_REFUSED  (DA W5 F1) the betrayer's wage clock is driven
//                           DUE, then pay_wage is pressed: the broken bond must
//                           settle NOTHING — no WagePaid, no loyalty drift, the
//                           wage stays due (the guard the 2g move had lost)
//     HEARTS_MEAN_LAST      the emit-order contract: per-follower deltas FIRST
//                           (roster order), roster-mean LAST under the reserved
//                           "roster" key — and the Hud hearts read the mean
//     ROSTER_OVERSIZED_TRIMMED (DA W5 F4) hand-edited 5-entry save loads to N=3, no orphan body
//   roster_neg — NEG_ROSTER: at the cap of 3 the 4th recruit is REFUSED (the
//     roster and the wild flag stay unchanged); detected -> named red, exit 1.
//
// Run:  $GODOT --headless --path <proj> --script res://ci_proofs/RosterIntegrationProof.cs
public partial class RosterIntegrationProof : SceneTree
{
    private const int FrameBudget = 5000;
    private const int PressSettleFrames = 30;   // let the lerp settle after a teleport

    private EventBus? _bus;
    private WorldDirector? _director;
    private CharacterBody3D? _playerBody;
    private Hud? _hud;
    private EmpathyPanel? _empathy;
    private Node? _main;
    private string _mode = "roster_follow";
    private int _stage;
    private int _stageFrames;
    private int _physFrames;
    private bool _composed;
    private bool _failed;

    // Bus observation (ordered, so the emit-order contract is asserted on real wire order)
    private readonly List<(string key, int loyalty)> _loyaltyEmits = new();
    private int _wagePaidCount;
    private int _betrayalCount;
    private string _lastBetrayalKey = "";

    // Stage scratch
    private int _sub;
    private int _pressCount;
    private int _loyaltyBeforeForgive;
    private int _wageBeforeStage;
    private int _loyaltyBeforePayLeg;
    private int _healthBeforeBreak;
    private int _betrayalBeforeBreak;
    private int _heartsBaselineA, _heartsBaselineB;
    private int[] _savedLoyalties = System.Array.Empty<int>();
    private readonly List<int> _savedIds = new();
    private int _payToggle, _interactToggle, _bookToggle, _cycleToggle, _breakToggle;
    private const int BootEntityId = 7;   // the boot follower's authored bond id

    public override void _Initialize()
    {
        GD.Print("LA_GATE: start");
        _mode = System.Environment.GetEnvironmentVariable("LA_GATE_MODE") ?? "roster_follow";
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
        _hud = _main?.GetNodeOrNull<Hud>("UI/HudLayer/Hud");
        _empathy = _main?.GetNodeOrNull<EmpathyPanel>("UI/Empathy");
        if (_bus == null) { Fail("EventBus autoload not present"); return false; }
        if (_director == null) { Fail("main.tscn root is not WorldDirector"); return false; }
        if (_playerBody == null) { Fail("Player node not found"); return false; }
        if (_hud == null) { Fail("Hud not found at UI/HudLayer/Hud"); return false; }
        if (_empathy == null) { Fail("EmpathyPanel not found at UI/Empathy"); return false; }

        _bus.LoyaltyChanged += (key, loyalty) => _loyaltyEmits.Add((key, loyalty));
        _bus.WagePaid += _ => _wagePaidCount++;
        _bus.Betrayal += (companion, _) => { _lastBetrayalKey = companion; _betrayalCount++; };
        GD.Print($"LA_GATE: composed — roster={_director.RosterView.Count} (boot companion = roster[0])");
        return true;
    }

    public override bool _PhysicsProcess(double delta)
    {
        _physFrames++;
        return false;   // keep running (physics-tick counter, BridgeMvpProof pattern)
    }

    public override bool _Process(double delta)
    {
        if (_failed) return true;
        if (!_composed)
        {
            if (_main == null) return true;
            if (!Compose()) return _failed;
            _composed = true;
            return false;
        }
        _stageFrames++;
        if (_stageFrames > FrameBudget) { Fail($"stage {_stage} exceeded the frame budget"); return true; }

        switch (_mode)
        {
            case "roster_follow": FollowStage(); break;
            case "roster_neg": NegStage(); break;
            default: Fail($"unknown mode {_mode}"); return true;
        }
        return false;
    }

    // ---- helpers -------------------------------------------------------------

    private void Next(int stage) { _stage = stage; _stageFrames = 0; _sub = 0; }

    private static void Press(string action, ref int toggle)
    {
        toggle++;
        if (toggle % 4 == 1) Input.ActionPress(action);
        else if (toggle % 4 == 3) Input.ActionRelease(action);
    }

    /// <summary>Drive ONE recruit through the PRODUCTION path: move away, spawn
    /// a wild, speak to it (the interact scan marks the offer), pay the first
    /// wage. Returns true when roster.Count reached <paramref name="expectCount"/>.</summary>
    private bool RecruitPhase(int expectCount)
    {
        var roster = _director!.RosterView;
        switch (_sub)
        {
            case 0:   // step away so the NEXT spawn is the nearest NPC (followers trail 3.5 behind)
                _playerBody!.GlobalPosition += new Vector3(8f, 0f, 0f);
                _sub = 1;
                return false;
            case 1:
                if (_stageFrames < PressSettleFrames) return false;
                _director.SpawnWildFollower();
                _sub = 2;
                return false;
            case 2:
                {
                    var wild = _director.WildBodies[_director.WildBodies.Count - 1];
                    _playerBody!.GlobalPosition = wild.GlobalPosition;   // walk up to it
                    Press("interact", ref _interactToggle);
                    if (wild.RecruitOffered) { Input.ActionRelease("interact"); _sub = 3; }
                    return false;
                }
            case 3:
                _wageBeforeStage = _wagePaidCount;
                Input.ActionPress("pay_wage");
                _pressCount++;
                _sub = 4;
                return false;
            case 4:
                if (roster.Count > expectCount)
                {
                    Input.ActionRelease("pay_wage");
                    Fail($"recruit to {expectCount} failed (count={roster.Count})");
                    return true;
                }
                // At least TWO press edges must land: in the NEG case the count
                // already equals expect BEFORE the (refused) recruit, so a
                // single-glance return would prove nothing.
                if (_pressCount < 2 && _stageFrames % 6 == 0)
                {
                    Input.ActionRelease("pay_wage");
                    Input.ActionPress("pay_wage");
                    _pressCount++;
                }
                if (roster.Count == expectCount && _pressCount >= 2)
                {
                    Input.ActionRelease("pay_wage");
                    return true;
                }
                return false;
            default:
                return true;
        }
    }

    /// <summary>Advance one follower's OWN wage clock by <paramref name="seconds"/>
    /// (the roster tick only advances clocks by the small frame delta — the
    /// manual ticks make the due rhythm deterministic in the proof frame budget).</summary>
    private static void AdvanceClock(CompanionRoster.Follower f, double seconds)
        => f.Needs.TickAccompaniment(seconds);

    private void FailModeOr(bool ok, string why)
    {
        if (!ok) Fail(why);
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

    // ---- roster_neg: the 4th recruit is refused at the cap (D1) --------------

    private void NegStage()
    {
        var roster = _director!.RosterView;
        switch (_stage)
        {
            case 0: Next(1); break;   // arm the stage machine
            case 1: if (RecruitPhase(2)) Next(2); break;
            case 2: if (RecruitPhase(3)) Next(3); break;
            case 3:
                if (RecruitPhase(3))   // expect STAYS 3 — the 4th must be refused
                {
                    var wild = _director!.WildBodies.Count > 0 ? _director.WildBodies[_director.WildBodies.Count - 1] : null;
                    if (roster.Count == CompanionRoster.Cap && wild != null && wild.Wild)
                    {
                        GD.Print($"LA_GATE: NEG_ROSTER: 4th recruit REFUSED at cap {CompanionRoster.Cap} (owner D1) — roster={roster.Count}, wild {wild.EntityId} still wild — break detected");
                        Quit(1);
                        return;
                    }
                    Fail($"roster_neg: the cap did NOT refuse the 4th recruit (count={roster.Count}, wild={(wild?.Wild.ToString() ?? "gone")}) — the negative control is broken");
                    return;
                }
                break;
        }
    }

    public override void _Finalize()
    {
        Input.ActionRelease("pay_wage");
        Input.ActionRelease("interact");
        Input.ActionRelease("book");
        Input.ActionRelease("cycle_follower");
        Input.ActionRelease("break_bond");
    }
}
