using Godot;
using LastAnimal.Save;

// Last Animal — S8 command-bark runtime proof (MC 10131 / 10026.x, code,
// 2026-10-07).
//
// Partial-class half of RuntimeIntegrationProof: the command bark proven
// PURELY as presentation on the live scene. ONE real Input.ActionPress("bark")
// per roster shape; the bubble window is asserted as an INTEGER frame contract:
//   - roster 1 (boot seed) and roster 3 (planted Followers [7,21,22] restored
//     through the REAL LoadGame rebuild — the trait_effects planted-save
//     precedent, save OWNED delete-then-write MC 3910): the walked count of
//     VISIBLE "BarkBubble" nodes is EXACTLY the roster size at the open frame
//     and while the window holds, the per-body counter steps EXACTLY -1 per
//     frame (the CHAR_WALK exact-delta idiom — a delta-time window lands RED
//     here), and the bubbles are gone on the EXACT end frame open+V
//     (BARK_GONE_x);
//   - roster EMPTY (planted EMPTY Followers list through the same product
//     rebuild — the one path that also FREES the bodies, DA W5 F4): zero
//     visible bubbles across the whole window + margin — the walk sees GHOSTS
//     a stale-list or un-freed-body implementation would leak (non-vacuous);
//   - every press: zero gameplay delta across the window (hp/Manna/DNA/
//     position/roster/loyalty-sum — BARK_NO_GAMEPLAY_DELTA_x, player physics
//     frozen over the window so position is bit-still, Motion/KillPulse
//     idiom);
//   - census re-grep IN-LEG: the EventBus source read at runtime still holds
//     EXACTLY 15 signals (System.IO precedent RuntimeIntegrationProof.Save.cs).
// The bubble authority is the scene-tree WALK (BridgeMvpProof manual-walk
// idiom), cross-checked against the per-body counter every frame.
//
// Quiet boot (bus_emit idiom): zero enemies — no stray damage can move the
// gameplay snapshot mid-assert. Stages of one mode: 102. Planted RED pair
// (evidence .audits/*-s8/): a delta-time window feed, and a wrong-bubble-count
// plant (open on only one body) — both land here.
public partial class RuntimeIntegrationProof : SceneTree
{
    private const int BarkSettleFrames = 30;      // let physics rest after compose/load
    private const int BarkEmptyMarginFrames = 4;  // window + margin for the zero-walk

    private int _bkPhase, _bkSub, _bkFrames;
    private int _bkOpenFrame, _bkWindowV, _bkLastVisible;
    private bool _bkPlantedSave;                  // OWNED: delete-before-plant, delete-at-end
    private Vector3 _bkPos;
    private int _bkHp, _bkManna, _bkDna, _bkSpoken, _bkRosterN, _bkLoyaltySum;

    /// <summary>Stage-102 dispatch (called from the main file's stage switch).</summary>
    private void RunBarkStage()
    {
        _bkFrames++;
        switch (_bkPhase)
        {
            case 0: BarkArm(); break;
            case 1: BarkPressTest(1, "1"); break;
            case 2: BarkPlantAndCheck(3, "BARK_ROSTER_THREE"); break;
            case 3: BarkPressTest(3, "3"); break;
            case 4: BarkPlantAndCheck(0, "BARK_ROSTER_EMPTY"); break;
            case 5: BarkEmptyTest(); break;
            case 6: BarkClose(); break;
        }
    }

    private void BarkNext(int phase) { _bkPhase = phase; _bkFrames = 0; _bkSub = 0; }

    // ---- shared machinery ----------------------------------------------------

    /// <summary>The ONE bubble authority the leg reads: a manual subtree walk
    /// (GetChildren recursion — in GodotSharp 4.7.2 GetChildren(bool) is
    /// include_internal, NOT recursion; the Zone4 proof helper note) counting
    /// VISIBLE nodes named "BarkBubble" anywhere under main.</summary>
    private int CountBubbles(Node? node)
    {
        if (node == null) return 0;
        int n = node.Name == "BarkBubble" && node is MeshInstance3D { Visible: true } ? 1 : 0;
        foreach (Node child in node.GetChildren())
            n += CountBubbles(child);
        return n;
    }

    private void SnapGameplay()
    {
        var player = _director!.PlayerModel;
        _bkHp = player.Health; _bkManna = player.Manna;
        _bkDna = _hud!.DnaMeter; _bkSpoken = _director.SpokenDna.Count;
        _bkPos = _playerBody!.GlobalPosition;
        _bkRosterN = _director.RosterView.Count;
        _bkLoyaltySum = 0;
        for (int i = 0; i < _director.RosterView.Count; i++)
            _bkLoyaltySum += _director.RosterView[i].Component.Loyalty;
    }

    private void CheckGameplayDelta(string suffix)
    {
        var player = _director!.PlayerModel;
        bool ok = player.Health == _bkHp && player.Manna == _bkManna
            && _hud!.DnaMeter == _bkDna && _director.SpokenDna.Count == _bkSpoken
            && _playerBody!.GlobalPosition == _bkPos
            && _director.RosterView.Count == _bkRosterN;
        int loyaltySum = 0;
        for (int i = 0; i < _director.RosterView.Count; i++)
            loyaltySum += _director.RosterView[i].Component.Loyalty;
        ok &= loyaltySum == _bkLoyaltySum && _director.RosterView.Count == _bkRosterN;
        Check($"BARK_NO_GAMEPLAY_DELTA_{suffix}: hp/Manna/DNA/position/roster/loyalty unchanged across the bark window",
              ok, $"hp {player.Health} vs {_bkHp}, roster {_director.RosterView.Count} vs {_bkRosterN}");
        _playerBody!.SetPhysicsProcess(true);   // release the freeze taken for the bit-still read
    }

    /// <summary>Plant an OWNED roster save (delete-then-write, MC 3910) and
    /// rebuild through the REAL load path; then wait for the settle.</summary>
    private void BarkPlantAndCheck(int n, string marker)
    {
        if (_bkSub == 0)
        {
            var store = new GodotSaveStore();
            if (System.IO.File.Exists(store.SavePath)) System.IO.File.Delete(store.SavePath);
            _bkPlantedSave = true;
            var st = new GameState();
            if (n == 3)
                foreach (int id in new[] { 7, 21, 22 })
                    st.Followers.Add(new FollowerEntry { EntityId = id, Loyalty = 50 });
            Check($"bark save OWNED+planted (Followers x{n}) via the existing save seam",
                  SaveSystem.Save(st, store), $"path={store.SavePath}");
            _director!.LoadGame();              // the product rebuild: reuse + spawn / REMOVE
            _bkSub = 1;
            return;
        }
        if (_bkFrames < BarkSettleFrames) return;
        Check(marker + $": roster={_director!.RosterView.Count} bodies={_director.FollowerBodies.Count} after the load rebuild",
              _director.RosterView.Count == n && _director.FollowerBodies.Count == n,
              $"N={_director.RosterView.Count} bodies={_director.FollowerBodies.Count}");
        if (_failed) return;
        BarkNext(n == 0 ? 5 : 3);
    }

    /// <summary>Press test for roster size N: real press, integer window in/out,
    /// walked bubble count, zero gameplay delta.</summary>
    private void BarkPressTest(int n, string suffix)
    {
        var bodies = _director!.FollowerBodies;
        switch (_bkSub)
        {
            case 0:   // shape + freeze + snapshot + THE REAL PRESS
                if (_bkFrames < BarkSettleFrames) return;
                Check($"bark shape check (roster {n})",
                      _director.RosterView.Count == n && bodies.Count == n,
                      $"N={_director.RosterView.Count} bodies={bodies.Count}");
                if (_failed) return;
                _playerBody!.SetPhysicsProcess(false);   // Motion/KillPulse bit-still idiom
                SnapGameplay();
                Input.ActionPress("bark");
                _bkSub = 1;
                break;

            case 1:   // wait for the window to OPEN (the press landed)
                {
                    int w = CountBubbles(_main);
                    if (w == 0)
                    {
                        if (_bkFrames > 120) Fail($"BARK_{suffix}: the bark press never opened a bubble window (poll dead?)");
                        return;
                    }
                    if (w != n) { Fail($"BARK_OPEN_{suffix}: walked bubble count {w} != roster size {n}"); return; }
                    _bkWindowV = bodies[0].BarkWindowFrames;
                    bool sameV = true;
                    foreach (var b in bodies) sameV &= b.BarkWindowFrames == _bkWindowV;
                    if (!sameV || _bkWindowV <= 0)
                    { Fail($"BARK_OPEN_{suffix}: window counters not one shared positive integer (V={_bkWindowV})"); return; }
                    _bkOpenFrame = _bkFrames; _bkLastVisible = _bkFrames;
                    Input.ActionRelease("bark");
                    GD.Print($"LA_GATE: BARK_OPEN_{suffix} — real bark press: walked count EXACTLY {n} == roster size at open, integer window V={_bkWindowV}f");
                    _bkSub = 2;
                }
                break;

            case 2:   // the window: counter steps EXACTLY -1, walk count holds, EXACT end frame
                {
                    int expected = _bkWindowV - (_bkFrames - _bkOpenFrame);
                    foreach (var b in bodies)
                        if (b.BarkWindowFrames != expected)
                        { Fail($"BARK_{suffix}: window step NOT exactly -1 (frame +{_bkFrames - _bkOpenFrame} reads {b.BarkWindowFrames}, expected {expected}) — a delta-time feed lands RED here"); return; }
                    int w = CountBubbles(_main);
                    int wantVisible = expected > 0 ? n : 0;
                    if (w != wantVisible)
                    { Fail($"BARK_{suffix}: walked bubble count {w} != {(expected > 0 ? "in-window " : "past-window ")}{wantVisible} at frame +{_bkFrames - _bkOpenFrame}"); return; }
                    if (expected > 0) _bkLastVisible = _bkFrames;
                    else
                    {
                        if (_bkLastVisible != _bkOpenFrame + _bkWindowV - 1)
                        { Fail($"BARK_{suffix}: bubbles persisted past the integer window (last visible +{_bkLastVisible - _bkOpenFrame} > {(_bkWindowV - 1)})"); return; }
                        GD.Print($"LA_GATE: BARK_STEP_{suffix} — per-body counter stepped EXACTLY -1 per frame from {_bkWindowV} to 0 (integer, F4)");
                        GD.Print($"LA_GATE: BARK_GONE_{suffix} — walked bubble count back to 0 on the EXACT end frame open+{_bkWindowV} (last visible open+{_bkWindowV - 1})");
                        _bkSub = 3;
                    }
                }
                break;

            case 3:   // zero gameplay delta across the window
                CheckGameplayDelta(suffix);
                if (_failed) return;
                GD.Print($"LA_GATE: BARK_NO_GAMEPLAY_DELTA_{suffix} — hp/Manna/DNA/position/roster/loyalty bit-unchanged across the bark window (presentation authority, F4)");
                BarkNext(n == 1 ? 2 : 4);   // after 1: plant the 3; after 3: plant the empty
                break;

            default:
                Fail("bark: press-test sub machine overflow");
                break;
        }
    }

    // ---- arm / empty / close -------------------------------------------------

    private void BarkArm()
    {
        if (_bkFrames < BarkSettleFrames) return;
        Check("bark arm: boot roster is the single boot companion",
              _director!.RosterView.Count == 1 && _director.FollowerBodies.Count == 1,
              $"N={_director.RosterView.Count} bodies={_director.FollowerBodies.Count}");
        if (_failed) return;
        GD.Print("LA_GATE: BARK_ROSTER_ONE — boot seed roster of 1 (InitRoster) on the live scene");
        BarkNext(1);
    }

    /// <summary>EMPTY roster: the ONLY observable truth is the WALK — zero
    /// visible bubbles across window + margin. Ghosts (a stale-list open, an
    /// un-freed body) land RED here; the press itself is proven live by the
    /// N=1/N=3 phases of the same run.</summary>
    private void BarkEmptyTest()
    {
        if (_bkSub == 0)
        {
            if (_bkFrames < BarkSettleFrames) return;
            _playerBody!.SetPhysicsProcess(false);
            SnapGameplay();
            Input.ActionPress("bark");
            _bkSub = 1;
            return;
        }
        int wait = _bkWindowV + BarkEmptyMarginFrames;
        int w = CountBubbles(_main);
        if (w != 0 || _director!.FollowerBodies.Count != 0)
        {
            Fail($"BARK_EMPTY_ZERO: ghost bubbles / bodies on the EMPTY roster (walk={w}, bodies={_director.FollowerBodies.Count}) — nothing on an empty roster may bubble");
            return;
        }
        if (_bkFrames >= wait)
        {
            Input.ActionRelease("bark");
            CheckGameplayDelta("0");
            if (_failed) return;
            GD.Print($"LA_GATE: BARK_EMPTY_ZERO — real bark press on the EMPTY roster: walked count 0 every frame across {_bkWindowV}+{BarkEmptyMarginFrames}f, no ghosts (bodies freed with the roster by the product rebuild)");
            BarkNext(6);
        }
    }

    private void BarkClose()
    {
        // Census re-grep IN-LEG: the bus source still carries EXACTLY 15 signals.
        string src = System.IO.File.ReadAllText(
            ProjectSettings.GlobalizePath("res://autoload/EventBus.cs"));
        int signals = 0;
        foreach (string line in src.Split('\n'))
            if (line.Contains("[Signal] public delegate")) signals++;
        Check("bus census re-grep IN-LEG: EXACTLY 15 signals (zero new signals shipped with bark)",
              signals == 15, $"signals={signals}");
        if (_bkPlantedSave)
        {
            var store = new GodotSaveStore();
            if (System.IO.File.Exists(store.SavePath)) System.IO.File.Delete(store.SavePath);
            _bkPlantedSave = false;
            GD.Print("LA_GATE: BARK_SAVE_OWNED — planted saves deleted after use (MC 3910 ownership idiom)");
        }
        if (_failed) return;
        GD.Print("LA_GATE: BARK_CENSUS_15 — EventBus source read at runtime holds exactly 15 signals");
        GD.Print("LA_GATE: PASS — S8 command bark verified end-to-end (pure presentation: real press, roster 1/3 EXACT walked counts, integer -1-step window with EXACT end frame, EMPTY roster zero ghosts, zero gameplay delta, census 15, zero save delta)");
        _asserted = true;
        ReleaseHeldRefsBeforeQuit();
        Quit(0);
    }
}
