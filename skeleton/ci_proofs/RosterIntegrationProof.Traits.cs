using Godot;
using LastAnimal.Companion;
using LastAnimal.Save;
using LastAnimal.Dna;

// Last Animal — MC 10201 S17 partial-class half of RosterIntegrationProof:
// the trait_effects leg (the battery's TRAIT_EFFECTS + WAGEFREE_UPKEEP rows).
// ONE live pay press settles known traits and the observed deltas print
// AGAINST BASE — the trait tag rides the S9 print idiom ("[Bonded]",
// "[Forager]"). Plus the trait-INDEPENDENT wage-free upkeep band (the DA-F1
// settle hunk), proven as a live A/B on the shipped skip arm: the SAME
// wage-miss drive decays at width 0 (control — and lands the Steadfast -25%
// row LIVE) and is WAIVED at width 6 (the census live width).
//
// Reachability (recorded, honest): the live wild pool starts at id 21 under a
// roster cap of 3, so a fresh run can never RECRUIT a Bonded follower (24 is
// the first Bonded id). The ratified restore seam is the production path that
// lands a known id onto a slot (hand-edited saves are in-contract — NF6), so
// the leg PLANTS saves and loads them through the REAL LoadGame path:
//   save A: Followers [24 Bonded, 25 Steadfast, 26 Forager], no DNA (width 0)
//   save B: the same Followers + LearnedDnaCounters x 6 (the census width)
// One pay press then observes:
//   Bonded   landed pay   +5 loyalty AND Manna +1 vs base +0 (world rider at
//                         the shipped SkillState.GainManna add site)
//   Forager  landed pay   +3 loyalty vs base +5 (wage -2)
// and the wage-miss drive observes:
//   width 0  Steadfast   -2 (the -25% rule on the LIVE skip arm; band off)
//   width 6  Steadfast    0 (band ON: upkeep waived, no bookkeeping)
// The saves are OWNED by this mode (delete-then-write, MC 3910 idiom).
public partial class RosterIntegrationProof : SceneTree
{
    private int _mannaBeforeTraits;
    private int _steadyControlBefore;
    private int _steadySkipsAtLoad;      // band A/B baseline (needs counters
    private int _steadyUnpaidAtLoad;     // ride the reused stack across load)

    private void PlantTraitSave(bool withDna)
    {
        var store = new GodotSaveStore();
        if (System.IO.File.Exists(store.SavePath)) System.IO.File.Delete(store.SavePath);
        var st = new GameState();
        foreach (int id in new[] { 24, 25, 26 })
            st.Followers.Add(new FollowerEntry { EntityId = id, Loyalty = 50 });
        if (withDna)
            st.LearnedDnaCounters.AddRange(new[] { 1, 1, 1, 1, 1, 1 });   // the census width
        Check($"trait save planted ({(withDna ? "width 6 — band ON" : "width 0 — band OFF")}) via the existing save seam",
              SaveSystem.Save(st, store), $"path={store.SavePath}");
        _director!.LoadGame();             // the REAL load path re-derives traits + DNA (F2)
    }

    /// <summary>Drive one follower's wage unpaid past a full interval — the
    /// SAME cadence the world skip arm consumes (TickRoster runs each frame).</summary>
    private void DriveWageMiss(CompanionRoster.Follower f, int seconds)
    {
        while (!f.Needs.SalaryDue) AdvanceClock(f, 1.0);
        for (int s = 0; s < seconds; s++) AdvanceClock(f, 1.0);   // a full unpaid interval
    }

    private void TraitStage()
    {
        var roster = _director!.RosterView;
        switch (_stage)
        {
            case 0: Next(1); break;

            case 1:   // plant A (width 0), load, re-arm on the restored roster
                if (_sub == 0) { PlantTraitSave(false); _sub = 1; return; }
                if (_stageFrames < 5) return;      // let the restore settle a frame
                {
                    bool shape = roster.Count == 3
                        && roster[0].Trait == CompanionTrait.Bonded
                        && roster[1].Trait == CompanionTrait.Steadfast
                        && roster[2].Trait == CompanionTrait.Forager;
                    Check("load restored N=3 with the DERIVED traits [Bonded],[Steadfast],[Forager] (never persisted)",
                          shape,
                          $"N={roster.Count} traits=[{roster[0].Trait},{roster[1].Trait},{roster[2].Trait}]");
                    Check("shipped DNA read agrees: width 0 pre-DNA (ModelPlayerDna -> Counters.Length)",
                          EcosystemAdaptation.ModelPlayerDna(_director.SpokenDna).Counters.Length == 0,
                          $"width={EcosystemAdaptation.ModelPlayerDna(_director.SpokenDna).Counters.Length}");
                    if (_failed) return;
                    _mannaBeforeTraits = _director.PlayerModel.Manna;
                    _loyaltyEmits.Clear();
                    _wageBeforeStage = _wagePaidCount;
                    GD.Print($"LA_GATE: TRAIT_EFFECTS armed — [Bonded]24 [Steadfast]25 [Forager]26, Manna base={_mannaBeforeTraits}, DNA width 0");
                    Next(2);
                }
                break;

            case 2:   // ONE pay press settles Bonded + Forager (both driven due)
                {
                    var bonded = roster[0]; var steady = roster[1]; var forager = roster[2];
                    if (_sub == 0)
                    {
                        while (!bonded.Needs.SalaryDue) AdvanceClock(bonded, 1.0);
                        while (!forager.Needs.SalaryDue) AdvanceClock(forager, 1.0);
                        _sub = 1;
                    }
                    Press("pay_wage", ref _payToggle);
                    if (forager.Component.Loyalty == 50)
                    {
                        if (_stageFrames > 3000) Fail("trait_effects: the pay press never landed the due settles");
                        return;
                    }
                    Input.ActionRelease("pay_wage");

                    Check("[Bonded] landed pay keeps the BASE loyalty settle (+5) — its rule is Manna, not loyalty",
                          bonded.Component.Loyalty == 55, $"24 -> {bonded.Component.Loyalty}");
                    Check("[Forager] landed pay is wage -2: +3 vs base +5",
                          forager.Component.Loyalty == 53, $"26 -> {forager.Component.Loyalty}");
                    Check("[Steadfast] never due this press: untouched (its -25% rule is the wage-MISS arm)",
                          steady.Component.Loyalty == 50 && !steady.Needs.SalaryDue, $"25 -> {steady.Component.Loyalty}");
                    Check("[Bonded] settle rode the shipped Manna add site: player Manna +1 vs base +0 (Forager adds NO manna)",
                          _director.PlayerModel.Manna - _mannaBeforeTraits == CompanionRoster.BondedMannaOnPay,
                          $"Manna {_mannaBeforeTraits} -> {_director.PlayerModel.Manna}");
                    Check("settles ride NO WagePaid signal here (planted save carries no active wage row — shipped 'no settle, no signal'); the Bonded rider emits ZERO new signals (F3 census 15)",
                          _wagePaidCount == _wageBeforeStage, $"wages+{_wagePaidCount - _wageBeforeStage}");
                    if (_failed) return;

                    GD.Print($"LA_GATE: TRAIT_EFFECTS: [Bonded] settle delta Manna +1 vs base +0 ({_mannaBeforeTraits}->{_director.PlayerModel.Manna}, loyalty 50->55 base); [Forager] settle delta +3 vs base +5 (50->53); [Steadfast] idle — one live pay press, per-SLOT rules felt on the real scene");

                    // Control tick (band OFF at width 0): drive the Steadfast
                    // follower unpaid a full interval — the LIVE skip arm runs
                    // the Steadfast row (50 -> 48, the -25% vs base -3).
                    _steadyControlBefore = steady.Component.Loyalty;
                    DriveWageMiss(steady, 31);
                    Next(3);
                }
                break;

            case 3:   // control landed? then plant B (width 6) and re-drive
                {
                    var steady = roster[1];
                    if (steady.Component.Loyalty == _steadyControlBefore && steady.Needs.SkippedCycles == 0)
                    {
                        if (_stageFrames > 600)
                            Fail($"trait_effects control: the width-0 wage-miss never ran on the live skip arm (loyalty {steady.Component.Loyalty})");
                        return;
                    }
                    Check("[Steadfast] LIVE wage-miss control at width 0 decays the -25% row: -2 vs base -3",
                          steady.Component.Loyalty == _steadyControlBefore - 2 && steady.Needs.SkippedCycles >= 1,
                          $"50 -> {steady.Component.Loyalty}, SkippedCycles={steady.Needs.SkippedCycles}");
                    if (_failed) return;

                    _sub = 0;
                    Next(4);
                }
                break;

            case 4:   // plant B (width 6 — the census live width), reload
                if (_sub == 0) { PlantTraitSave(true); _sub = 1; return; }
                if (_stageFrames < 5) return;
                {
                    Check("save B restored: N=3, DNA width 6 (the census live width — Counters.Length through the shipped read)",
                          roster.Count == 3 && EcosystemAdaptation.ModelPlayerDna(_director.SpokenDna).Counters.Length == 6,
                          $"N={roster.Count} width={EcosystemAdaptation.ModelPlayerDna(_director.SpokenDna).Counters.Length}");
                    if (_failed) return;
                    _steadyControlBefore = roster[1].Component.Loyalty;   // 50 from the save
                    // Needs/Machine counters ride the REUSED stack (restore
                    // resets identity+loyalty, not the wage bookkeeping) — the
                    // waiver is proven by their DELTA staying 0, so snapshot.
                    _steadySkipsAtLoad = roster[1].Needs.SkippedCycles;
                    _steadyUnpaidAtLoad = roster[1].Machine.UnpaidCycles;
                    DriveWageMiss(roster[1], 31);                          // the SAME drive
                    Next(5);
                }
                break;

            case 5:   // band ON: the same drive waives the whole upkeep tick
                {
                    var steady = roster[1];
                    // Wait for the skip arm to CONSUME the interval (DueSeconds
                    // drops below the interval); a WAIVER never moves the
                    // counters, so consumption — not a counter bump — is the
                    // trigger. Cap at 240 frames.
                    if (steady.Needs.DueSeconds >= steady.Needs.PayIntervalSeconds && _stageFrames < 240) return;
                    // DueSeconds < interval proves the unpaid interval was
                    // CONSUMED (the tick really ran) — it was WAIVED, not lag.
                    Check("BAND row: at width 6 the wage-miss upkeep is WAIVED — loyalty 50->50 (control -2), zero cycle bookkeeping DELTA, the interval WAS consumed (DueSeconds fell back)",
                          steady.Component.Loyalty == _steadyControlBefore
                          && steady.Needs.SkippedCycles == _steadySkipsAtLoad
                          && steady.Machine.UnpaidCycles == _steadyUnpaidAtLoad
                          && steady.Needs.DueSeconds < steady.Needs.PayIntervalSeconds,
                          $"50 -> {steady.Component.Loyalty}, dSkips={steady.Needs.SkippedCycles - _steadySkipsAtLoad}, dUnpaid={steady.Machine.UnpaidCycles - _steadyUnpaidAtLoad}, Due={steady.Needs.DueSeconds:0}");
                    if (_failed) return;

                    GD.Print($"LA_GATE: WAGEFREE_UPKEEP: width 6 (census live width >= band 4) upkeep delta 0 vs control width-0 delta -2 — live skip arm waived the tick (runtime-only, never persisted)");
                    GD.Print("LA_GATE: PASS — S17 trait effects + wage-free upkeep band verified live (known-trait settles observed expected-vs-base; cap/trim/F6 legs UNCHANGED in the sibling modes)");
                    ReleaseHeldRefsBeforeQuit();    // MC 10126 rooted-fields idiom
                    Quit(0);
                }
                break;
        }
    }
}
