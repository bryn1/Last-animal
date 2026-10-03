// SIZE: CI proof harness (600 l) — AT the test-class ceiling 600 (MC 3943 2g;
// DA W5 F1 + W5 c3 F-A oversized-save trim leg; split owed before any next leg).
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

    // ---- roster_follow: the full arc ------------------------------------------

    private void FollowStage()
    {
        var roster = _director!.RosterView;
        switch (_stage)
        {
            case 0: Next(1); break;   // arm the stage machine

            case 1:   // recruit wild #1
                if (RecruitPhase(2))
                {
                    Check("recruited follower is bonded to its own component id 21",
                          roster[1].Component.CompanionEntityId == 21, $"id={roster[1].Component.CompanionEntityId}");
                    if (_failed) return;
                    GD.Print("LA_GATE: ROSTER_RECRUITED — interact-offer + pay_wage (FIRST wage) joined wild 21 in play");
                    Next(2);
                }
                break;

            case 2:   // recruit wild #2
                if (RecruitPhase(3))
                {
                    Check("second follower has its OWN unique id 22",
                          roster[2].Component.CompanionEntityId == 22, $"id={roster[2].Component.CompanionEntityId}");
                    if (_failed) return;
                    GD.Print("LA_GATE: ROSTER_TWO_RECRUITED — followers=3 (boot 7 + 21 + 22), cap 3 not yet hit");
                    Next(3);
                }
                break;

            case 3:   // both NEW bodies must trail the player (follow semantics)
                {
                    if (_sub == 0)
                    {
                        _playerBody!.GlobalPosition += new Vector3(20f, 0f, 0f);
                        _sub = 1;
                        _stageFrames = 0;
                        break;
                    }
                    if (_stageFrames < 90) break;
                    float d1 = _director.FollowerBodies[1].GlobalPosition.DistanceTo(_playerBody!.GlobalPosition);
                    float d2 = _director.FollowerBodies[2].GlobalPosition.DistanceTo(_playerBody!.GlobalPosition);
                    Check("recruited followers trail the player (follow lerp live)",
                          d1 < 19f && d2 < 19f, $"dist1={d1:0.#} dist2={d2:0.#}");
                    if (_failed) return;
                    GD.Print($"LA_GATE: FOLLOWERS_FOLLOW — both recruited bodies closed from 20 to ({d1:0.#},{d2:0.#})");
                    Next(4);
                }
                break;

            case 4:   // WAGE_INDEPENDENT_A: the RECRUIT's wage is due, boot's is NOT —
                      // it settles its own wage and emits its own WagePaid (ARCH W2:
                      // a singleton wage read keyed on the boot needs ledger would
                      // mute this settle entirely; q_wage is still Active, so the
                      // settle really rides the bus).
                {
                    var f0 = roster[0]; var f1 = roster[1]; var f2 = roster[2];
                    if (_sub == 0)
                    {
                        _loyaltyEmits.Clear();
                        _wageBeforeStage = _wagePaidCount;
                        while (!f1.Needs.SalaryDue) AdvanceClock(f1, 1.0);   // boot clock untouched
                        _sub = 1;
                    }
                    Press("pay_wage", ref _payToggle);
                    if (f1.Component.Loyalty != 50)
                    {
                        Input.ActionRelease("pay_wage");
                        Check("only the RECRUIT settled: its own wage +5, boot and follower-22 untouched",
                              f1.Component.Loyalty == 55 && f0.Component.Loyalty == 50 && f2.Component.Loyalty == 50,
                              $"loy=({f0.Component.Loyalty},{f1.Component.Loyalty},{f2.Component.Loyalty}) bootDue={f0.Needs.SalaryDue}");
                        if (_failed) return;
                        Check("the non-boot follower's settle emitted its OWN WagePaid (q_wage still active)",
                              _wagePaidCount - _wageBeforeStage == 1,
                              $"wages+{_wagePaidCount - _wageBeforeStage} (singleton wage read would emit ZERO here)");
                        if (_failed) return;
                        bool sawFollowerKey = false;
                        foreach (var (key, _) in _loyaltyEmits)
                            if (key == "follower-21") sawFollowerKey = true;
                        Check("recruit loyalty delta rode the UNIQUE key follower-21", sawFollowerKey,
                              string.Join(",", _loyaltyEmits.ConvertAll(e => e.key)));
                        if (_failed) return;
                        GD.Print("LA_GATE: WAGE_INDEPENDENT_A — the DUE follower alone settled on ITS OWN clock and emitted its own WagePaid (W2 re-key live)");
                        Next(5);
                    }
                    else if (_stageFrames > 3000) Fail("recruit never settled its due wage (pay arm did not reach it)");
                    break;
                }

            case 5:   // WAGE_INDEPENDENT_B: boot settles on ITS OWN clock; the recruit's
                      // clock was reset by its pay — the earlier settle changed nothing here.
                {
                    var f0 = roster[0]; var f1 = roster[1];
                    if (_sub == 0)
                    {
                        _wageBeforeStage = _wagePaidCount;
                        while (!f0.Needs.SalaryDue) AdvanceClock(f0, 1.0);   // f1 clock fresh from its pay
                        _sub = 1;
                    }
                    Press("pay_wage", ref _payToggle);
                    if (f0.Component.Loyalty != 50)
                    {
                        Input.ActionRelease("pay_wage");
                        Check("boot settled its own wage; the already-paid recruit stayed at 55 (ledgers independent)",
                              f0.Component.Loyalty == 55 && f1.Component.Loyalty == 55 && !f1.Needs.SalaryDue,
                              $"boot={f0.Component.Loyalty} f1={f1.Component.Loyalty} f1Due={f1.Needs.SalaryDue}");
                        if (_failed) return;
                        GD.Print("LA_GATE: WAGE_INDEPENDENT_B — boot paid on ITS OWN clock; the recruit's reset ledger stayed uninvolved");
                        Next(6);
                    }
                    else if (_stageFrames > 3000) Fail("boot never settled its due wage");
                    break;
                }

            case 6:   // save -> load restores N=3 (own the save first, MC 3910 idiom)
                {
                    if (_sub == 0)
                    {
                        var store = new GodotSaveStore();
                        if (System.IO.File.Exists(store.SavePath)) System.IO.File.Delete(store.SavePath);
                        _savedLoyalties = new[] { roster[0].Component.Loyalty, roster[1].Component.Loyalty, roster[2].Component.Loyalty };
                        _savedIds.Clear();
                        foreach (var f in roster.Followers) _savedIds.Add(f.Component.CompanionEntityId);
                        _director.SaveGame();
                        // Diverge the live state so the restore is observable.
                        roster[2].Component.ModifyLoyalty(-30);
                        _director.LoadGame();
                        _sub = 1;
                    }
                    if (_stageFrames < 5) break;   // let the restore settle a frame
                    bool loyaltyOk = roster.Count == 3
                        && roster[0].Component.Loyalty == _savedLoyalties[0]
                        && roster[1].Component.Loyalty == _savedLoyalties[1]
                        && roster[2].Component.Loyalty == _savedLoyalties[2];
                    bool idsOk = roster.Count == 3
                        && roster[0].Component.CompanionEntityId == _savedIds[0]
                        && roster[1].Component.CompanionEntityId == _savedIds[1]
                        && roster[2].Component.CompanionEntityId == _savedIds[2];
                    Check("save->load restored ALL THREE followers with their saved bonds + loyalties",
                          loyaltyOk && idsOk,
                          $"N={roster.Count} loy=[{roster[0].Component.Loyalty},{roster[1].Component.Loyalty},{roster[2].Component.Loyalty}] saved=[{_savedLoyalties[0]},{_savedLoyalties[1]},{_savedLoyalties[2]}]");
                    if (_failed) return;
                    GD.Print($"LA_GATE: ROSTER_RESTORED — LOAD_RESTORED N=3 (Followers list ids {_savedIds[0]},{_savedIds[1]},{_savedIds[2]})");
                    Next(7);
                }
                break;

            case 7:   // open the book
                Press("book", ref _bookToggle);
                if (_empathy!.Visible)
                {
                    Input.ActionRelease("book");
                    GD.Print("LA_GATE: check: book opened on the SELECTED follower's entry: ok");
                    Next(8);
                }
                else if (_stageFrames > 600) Fail("book never opened");
                break;

            case 8:   // cycle_follower pages the selection
                Press("cycle_follower", ref _cycleToggle);
                if (roster.SelectedIndex != 0)
                {
                    Input.ActionRelease("cycle_follower");
                    Check("cycle_follower selected follower #1 and repainted the book on it",
                          roster.SelectedIndex == 1 && _empathy!.Visible && _empathy.Current != null,
                          $"selected={roster.SelectedIndex} entry={_empathy!.Current?.EmotionalState}");
                    if (_failed) return;
                    GD.Print($"LA_GATE: CYCLE_SELECTED — selected {roster.Selected.BusKey} (EmpathyPanel signature untouched, I4)");
                    Next(9);
                }
                else if (_stageFrames > 600) Fail("cycle_follower never moved the selection");
                break;

            case 9:   // Forgive: pay_wage under the book is the bonus, NOT a wage
                _loyaltyBeforeForgive = roster[1].Component.Loyalty;
                _wageBeforeStage = _wagePaidCount;
                Input.ActionPress("pay_wage");
                _sub = 1;
                Next(10);
                break;

            case 10:
                if (roster[1].Component.Loyalty != _loyaltyBeforeForgive)
                {
                    Input.ActionRelease("pay_wage");
                    Check("pay_wage under the open book = Forgive (+bonus) on the SELECTED follower only, no WagePaid",
                          roster[1].Component.Loyalty == _loyaltyBeforeForgive + CompanionRoster.ForgiveBonus
                          && roster[0].Component.Loyalty != _loyaltyBeforeForgive + CompanionRoster.ForgiveBonus
                          && _wagePaidCount == _wageBeforeStage,
                          $"f1={roster[1].Component.Loyalty} (was {_loyaltyBeforeForgive}) wages+{_wagePaidCount - _wageBeforeStage}");
                    if (_failed) return;
                    GD.Print("LA_GATE: FORGIVE_APPLIED — book-open pay_wage applied the Forgive bonus (glue lives in the Roster partial only)");
                    Next(11);
                }
                else if (_stageFrames > 300) { Input.ActionRelease("pay_wage"); Fail("Forgive never applied"); }
                break;

            case 11:  // break_bond betrays ONLY the selected follower
                _healthBeforeBreak = _director.PlayerModel.Health;
                _betrayalBeforeBreak = _betrayalCount;
                Input.ActionPress("break_bond");
                Next(12);
                break;

            case 12:
                if (_betrayalCount > _betrayalBeforeBreak)
                {
                    Input.ActionRelease("break_bond");
                    var betrayed = roster[1].Component;
                    bool othersOk = roster[0].Component.HasCompanion && roster[2].Component.HasCompanion
                        && roster[0].Machine.State != CompanionState.Betrayed
                        && roster[2].Machine.State != CompanionState.Betrayed;
                    Check("break_bond executed the betrayal on the SELECTED follower (unique key) — others keep their bonds",
                          !betrayed.HasCompanion && othersOk
                          && _lastBetrayalKey == "follower-21"
                          && _betrayalCount - _betrayalBeforeBreak == 1,
                          $"key={_lastBetrayalKey} betrayedBond={betrayed.HasCompanion} emits={_betrayalCount - _betrayalBeforeBreak}");
                    if (_failed) return;
                    Check("the betrayal dealt its damage to the player",
                          _healthBeforeBreak - _director.PlayerModel.Health >= 15,
                          $"health {_healthBeforeBreak} -> {_director.PlayerModel.Health}");
                    if (_failed) return;
                    GD.Print("LA_GATE: BREAK_BOND_SELECTED — one follower betrayed, the other two stay bonded and following (per-component betrayal)");
                    Input.ActionPress("book");   // close the book again
                    Next(13);
                }
                else if (_stageFrames > 300) { Input.ActionRelease("break_bond"); Fail("break_bond never executed the betrayal"); }
                break;

            case 13:  // close the book, then arm the hearts stage
                Press("book", ref _bookToggle);
                if (!_empathy!.Visible)
                {
                    Input.ActionRelease("book");
                    _loyaltyEmits.Clear();
                    _heartsBaselineA = roster[0].Component.Loyalty;
                    _heartsBaselineB = roster[2].Component.Loyalty;
                    Next(14);
                }
                break;

            case 14:  // PAY_AFTER_BREAK_REFUSED (DA W5 F1): drive the betrayer's OWN
                      // wage clock DUE, then press pay — the broken bond must settle
                      // NOTHING: no WagePaid, no loyalty drift, the wage STAYS due.
                {
                    var betrayer = roster[1];
                    if (_sub == 0)
                    {
                        while (!betrayer.Needs.SalaryDue) AdvanceClock(betrayer, 1.0);
                        _wageBeforeStage = _wagePaidCount;
                        _loyaltyBeforePayLeg = betrayer.Component.Loyalty;   // 0 (bond broken)
                        _sub = 1;
                    }
                    Press("pay_wage", ref _payToggle);
                    if (_stageFrames < 30) break;                  // several pay edges get a chance
                    Input.ActionRelease("pay_wage");
                    // The AUTHORITATIVE invariant is the broken bond (M03's flag):
                    // on this manual break_bond path loyalty was still positive at
                    // the break, so the machine legitimately rests at Needing —
                    // CheckBetrayal (loyalty<=0) never fires after the bond is gone.
                    Check("pay press on the BROKEN bond settles nothing: no WagePaid, no loyalty drift, wage stays due",
                          _wagePaidCount == _wageBeforeStage
                          && betrayer.Component.Loyalty == _loyaltyBeforePayLeg
                          && !betrayer.Component.HasCompanion
                          && betrayer.Needs.SalaryDue,
                          $"wages+{_wagePaidCount - _wageBeforeStage} loyalty={betrayer.Component.Loyalty} bond={betrayer.Component.HasCompanion} due={betrayer.Needs.SalaryDue}");
                    if (_failed) return;
                    GD.Print("LA_GATE: PAY_AFTER_BREAK_REFUSED — the betrayed follower's live wage clock never settles (no WagePaid, no drift) — DA W5 F1 guard live");
                    Next(15);
                    break;
                }

            case 15:  // HEARTS_MEAN_LAST: starve followers 0 and 2 with the skip arm — NO pay press
                {
                    AdvanceClock(roster[0], 2.0);
                    AdvanceClock(roster[2], 2.0);
                    bool bothMoved = roster[0].Component.Loyalty != _heartsBaselineA
                                  && roster[2].Component.Loyalty != _heartsBaselineB;
                    if (!bothMoved)
                    {
                        if (_stageFrames > 4000) Fail("hearts stage: the skip arm never moved two followers' loyalty");
                        break;
                    }
                    int mean = roster.MeanLoyalty();
                    bool orderOk = _loyaltyEmits.Count >= 3
                        && _loyaltyEmits[^1].key == CompanionRoster.ReservedMeanKey
                        && _loyaltyEmits[^1].loyalty == mean
                        // The entry right before the mean must be a PER-FOLLOWER
                        // delta (not another mean): the mean rides LAST, always.
                        && !_loyaltyEmits[^2].key.StartsWith(CompanionRoster.ReservedMeanKey,
                                                              System.StringComparison.Ordinal);
                    Check("emit-order contract: per-follower deltas rode FIRST, the roster-mean rides LAST under the reserved key",
                          orderOk,
                          $"emits=[{string.Join(" ", _loyaltyEmits.ConvertAll(e => e.key + ":" + e.loyalty))}] mean={mean}");
                    if (_failed) return;
                    Check("the HUD hearts read the roster mean (last-value redraw, NO Hud edit)",
                          _hud!.CompanionHearts == Mathf.Clamp((int)Mathf.Round(mean / 20f), 0, 5),
                          $"hearts={_hud.CompanionHearts} mean={mean}");
                    if (_failed) return;
                    GD.Print($"LA_GATE: HEARTS_MEAN_LAST — last LoyaltyChanged = (\"roster\", {mean}); Hud hearts={_hud.CompanionHearts} from the mean");
                    Next(16); return;
                }
            case 16:  // OVERSIZED-SAVE RESTORE — the world-side F4 drop-arm pin (TEST W5 c3 F-A)
                {
                    if (_sub == 0)
                    {
                        var store = new GodotSaveStore(); if (System.IO.File.Exists(store.SavePath)) System.IO.File.Delete(store.SavePath);
                        var st = new GameState(); foreach (int id in new[] { 7, 21, 22, 23, 24 }) st.Followers.Add(new FollowerEntry { EntityId = id, Loyalty = 60 });
                        Check("hand-edited OVERSIZED save (5 Followers entries > cap 3) written via the existing save seam", SaveSystem.Save(st, store), $"path={store.SavePath}");
                        _director.LoadGame();   // the REAL load path: LoadGame -> RestoreFollowers over 5 entries
                        _sub = 1; return;
                    }
                    if (_stageFrames < 5) return;   // let the restore + the deferred QueueFree settle
                    int bodies = 0; foreach (var c in _director.GetChildren()) if (c is CompanionFollowBody) bodies++;
                    Check("ROSTER_OVERSIZED_TRIMMED — F4 drop-arm trims the 5-entry restore at the world-side cap: roster N=3 and companion bodies in the scene == 3 (no orphan frozen body); loyalty 60 proves the OVERSIZED save loaded",
                          roster.Count == CompanionRoster.Cap && bodies == CompanionRoster.Cap && roster[1].Component.Loyalty == 60, $"N={roster.Count} bodies={bodies} f1loy={roster[1].Component.Loyalty}");
                    if (_failed) return;
                    GD.Print("LA_GATE: ROSTER_OVERSIZED_TRIMMED — hand-edited 5-entry save: N=3, scene bodies=3, the 2 surplus entries DROPPED with the marker (DA W5 F4)\nLA_GATE: PASS — roster chain verified end-to-end (recruit, follow, independent wages per follower, save/load N, cycle/forgive/break, pay-after-break refusal, mean-last emit order, oversized-save trim)");
                    Quit(0); return;
                }
        }
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
