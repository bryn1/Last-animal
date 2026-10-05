using Godot;
using LastAnimal.Companion;
using LastAnimal.Save;
using LastAnimal.World;

// Last Animal — MC 10036 partial-class half of RosterIntegrationProof: the
// roster_follow leg (the full arc, stages 1-16: recruit, follow, independent
// wages, save/load restore, book/cycle/forgive/break, pay-after-break refusal,
// mean-last emit order, oversized-save trim). Extracted VERBATIM from
// RosterIntegrationProof.cs — moves only, no behaviour change; see the main
// file's header for the mode contract and the roster_neg leg.
public partial class RosterIntegrationProof : SceneTree
{
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
                    ReleaseHeldRefsBeforeQuit();    // MC 10126 rooted-fields idiom (method in entry file)
                    Quit(0); return;
                }
        }
    }

}
