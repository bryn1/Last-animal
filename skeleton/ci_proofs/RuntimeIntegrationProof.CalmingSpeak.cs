using Godot;
using LastAnimal.Companion;
using LastAnimal.Dna;
using LastAnimal.Save;
using LastAnimal.Skills;
using LastAnimal.World;
using System.Linq;

// Last Animal — MC 10031 Calming Speak runtime proof (code, 2026-10-04).
//
// Partial half of RuntimeIntegrationProof: the calm→recruit bridge on the
// REAL scene through REAL input (skill_use idiom; roster-proof's
// SpawnWildFollower seam). Pay-first stays the law — the cast only OPENS the
// offer + a 600-frame window; the ONLY join is the unchanged pay_wage path.
// calm_use legs (markers print ONLY after their asserts pass): CALM_CAST (one
// calming_speak marker, Manna −12 EXACTLY, window == 600 snapshotted INSIDE
// the synchronous emit = the cast frame T, body still WILD — PB-SPEND-first
// cannot hide), CALM_PAY_IN_WINDOW (wage joins, window belt-cleared),
// CALM_REFUSE_SHORT (PB-SPEND-FIRST reds it), CALM_REFUSE_STANDING (funded
// press spends ZERO — PB-PRESS-ORDER / PB-DOWNGRADE reds it, DA-c2 F-2),
// CALM_E_STANDS (PB-EXPIRY-CLEARS-E), CALM_WINDOW_EXPIRES (PB-NEVER-DECAYS),
// CALM_LOAD_CLEARED (REAL load_game press; PB-NO-LOAD-CLEAR — the R9 class).
// calm_neg: seam off BEFORE AddChild; a FUNDED UNLOCKED press leaks nothing
// → NEG_CALM, exit non-zero (PB-GATE-BYPASS makes the marker appear → red).
public partial class RuntimeIntegrationProof : SceneTree
{
    private int _cPhase, _cFrames, _cSub;
    private bool _cSubscribed;
    private int _calmEmits;                       // calming_speak-id SkillUsed count
    private CompanionFollowBody? _calmTarget, _standW, _eWild, _expWild;
    private int _snapWindow, _snapManna;          // state AT the synchronous emit (frame T)
    private bool _snapOffer, _snapWild;
    private int _mannaBefore, _rosterBefore, _waitUntil, _cPressCount;
    private int _cInteractToggle;

    private SkillUnlocks LiveCalm() =>
        PlayerMutations.Unlocked(EcosystemAdaptation.ModelPlayerDna(_director!.SpokenDna));

    /// <summary>Stage-70 dispatch (called from the main file's stage switch).</summary>
    private void RunCalmStage()
    {
        if (!CSubscribe()) return;
        _cFrames++;
        switch (_mode)
        {
            case "calm_use": CalmUseStage(); break;
            case "calm_neg": CalmNegStage(); break;
        }
    }

    /// <summary>The SkillUsed emit is SYNCHRONOUS inside the cast (§1.2 step
    /// 5), so this tap reads the offer flags on the cast frame itself. The
    /// generic SSubscribe (Skills partial) keeps printing + counting kills.</summary>
    private bool CSubscribe()
    {
        if (_cSubscribed) return SSubscribe();
        if (_bus == null || _director == null) return false;
        _bus.SkillUsed += id =>
        {
            if (id != PlayerMutations.CalmingSpeakId) return;
            _calmEmits++;
            if (_calmTarget == null) return;
            _snapWindow = _calmTarget.CalmedWindowFrames;
            _snapOffer = _calmTarget.RecruitOffered;
            _snapWild = _calmTarget.Wild;
            _snapManna = _director.PlayerModel.Manna;
        };
        _cSubscribed = true;
        return SSubscribe();
    }

    private void CNext(int phase) { _cPhase = phase; _cFrames = 0; _cSub = 0; }

    private CompanionFollowBody SpawnCalmWild()
    {
        _director!.SpawnWildFollower();
        var w = _director.WildBodies[_director.WildBodies.Count - 1];
        _playerBody!.GlobalPosition = w.GlobalPosition;   // walk up to it (roster-proof idiom)
        return w;
    }

    private void CalmPress() { _mannaBefore = _director!.PlayerModel.Manna; _calmEmits = 0; Input.ActionPress("skill_3"); }

    private bool CalmMarkerLanded(string why)
    {
        if (_calmEmits > 0) { Input.ActionRelease("skill_3"); return true; }
        if (_cFrames > 90) Fail($"calm_use: {why} — the cast never produced a SkillUsed marker");
        return false;
    }

    private void CalmUseStage()
    {
        var player = _director!.PlayerModel;
        var roster = _director.RosterView;
        switch (_cPhase)
        {
            case 0:   // farm exactly 3 extractions: 15 Manna, the third rule live
                if (_sKills < 3)
                {
                    if (_cFrames > 3000) Fail("calm_use: farm never produced 3 extractions");
                    if (!SFarmLoop()) SPress("travel", ref _sTravelToggle);
                    break;
                }
                Input.ActionRelease("attack");
                Input.ActionRelease("travel");
                Check("3 kills credited 3 x KillMannaGain", player.Manna == 3 * SkillState.KillMannaGain, $"manna={player.Manna}");
                Check("consensus unlocks Calming Speak live (>= 3 positions)", LiveCalm().CalmingSpeak, LiveCalm().ToString());
                if (_failed) return;
                GD.Print("LA_GATE: UNLOCK_CALMING_SPEAK — third rule of the frozen authority, read live");
                CNext(1);
                break;

            case 1:   // CALM_CAST
                if (_cSub == 0) { _calmTarget = SpawnCalmWild(); _rosterBefore = roster.Count; CalmPress(); _cSub = 1; break; }
                if (!CalmMarkerLanded("CALM_CAST")) break;
                Check("exactly ONE SkillUsed marker id calming_speak", _calmEmits == 1, $"emits={_calmEmits}");
                Check("Manna dropped EXACTLY 12 at the cast",
                      _snapManna == _mannaBefore - SkillState.CalmingSpeakCost && player.Manna == _snapManna,
                      $"{_mannaBefore} -> {_snapManna} (now {player.Manna})");
                Check("offer OPEN + window == 600 ON THE CAST FRAME, body STILL WILD, roster untouched (NO join)",
                      _snapOffer && _snapWindow == 600 && _snapWild && _rosterBefore == roster.Count && _calmTarget!.Wild,
                      $"offer={_snapOffer} window={_snapWindow} wild={_snapWild} roster={roster.Count}");
                if (_failed) return;
                GD.Print("LA_GATE: CALM_CAST — spend-after-scan: 12 Manna, offer open, window 600, pay-first intact (join only via wage)");
                CNext(2);
                break;

            case 2:   // CALM_PAY_IN_WINDOW — the unchanged wage path joins
                if (_cSub == 0) { _rosterBefore = roster.Count; Input.ActionPress("pay_wage"); _cSub = 1; break; }
                if (_cFrames > 90) Fail("calm_use: the in-window pay press recruited nobody (offer/wage wire broken)");
                if (roster.Count != _rosterBefore + 1) break;
                Input.ActionRelease("pay_wage");
                Check("the calmed wild JOINED via pay_wage (roster +1, left _wild, window belt-cleared)",
                      !_calmTarget!.Wild && _calmTarget.CalmedWindowFrames == 0 && !_director.WildBodies.Contains(_calmTarget),
                      $"roster={roster.Count} wild={_calmTarget.Wild} window={_calmTarget.CalmedWindowFrames}");
                if (_failed) return;
                GD.Print("LA_GATE: CALM_PAY_IN_WINDOW — T+2 pay press joined through the UNCHANGED first-wage path");
                CNext(3);
                break;

            case 3:   // CALM_REFUSE_SHORT — Manna < 12 spends NOTHING
                if (_cSub == 0)
                {
                    player.Manna = SkillState.CalmingSpeakCost - 1;   // 11 < 12
                    _calmTarget = SpawnCalmWild();
                    CalmPress();
                    _cSub = 1;
                    break;
                }
                if (_cFrames < 12) break;   // give the poll its frames
                Input.ActionRelease("skill_3");
                Check("short balance: zero Manna delta, no offer, no marker",
                      player.Manna == _mannaBefore && !_calmTarget!.RecruitOffered
                      && _calmTarget.CalmedWindowFrames == 0 && _calmEmits == 0,
                      $"manna={player.Manna} offer={_calmTarget.RecruitOffered} emits={_calmEmits}");
                if (_failed) return;
                GD.Print("LA_GATE: CALM_REFUSE_SHORT — refused, spent NOTHING");
                CNext(4);
                break;

            case 4:   // CALM_REFUSE_STANDING — E's standing offer is NOT castable
                if (_cSub == 0)
                {
                    player.Manna = 20;   // FUNDED: a spend-before-scan mutation must bite on the cast frame (DA-c2 F-2)
                    SPress("interact", ref _cInteractToggle);
                    if (_calmTarget!.RecruitOffered) { Input.ActionRelease("interact"); _standW = _calmTarget; _cSub = 1; }
                    if (_cFrames > 120) Fail("calm_use: interact never opened the standing offer");
                    break;
                }
                if (_cSub == 1) { CalmPress(); _cSub = 2; break; }
                if (_cFrames < 12) break;
                Input.ActionRelease("skill_3");
                Check("standing offer: refused, spent ZERO, window stays 0 (E never downgraded)",
                      player.Manna == _mannaBefore && _standW!.CalmedWindowFrames == 0 && _calmEmits == 0,
                      $"manna {_mannaBefore} -> {player.Manna} window={_standW.CalmedWindowFrames} emits={_calmEmits}");
                if (_failed) return;
                GD.Print("LA_GATE: CALM_REFUSE_STANDING — F refused on E's standing offer, nothing spent");
                CNext(5);
                break;

            case 5:   // CALM_E_STANDS — cast, E retires the window, the flag outlives 700 frames
                if (_cSub == 0) { _calmTarget = SpawnCalmWild(); player.Manna = 20; CalmPress(); _cSub = 1; break; }
                if (_cSub == 1) { if (!CalmMarkerLanded("E_STANDS cast")) break; _cSub = 2; break; }
                if (_cSub == 2)
                {
                    SPress("interact", ref _cInteractToggle);
                    if (_calmTarget!.RecruitOffered && _calmTarget.CalmedWindowFrames == 0)
                    {
                        Input.ActionRelease("interact");
                        _eWild = _calmTarget;          // standing witness for phase 7 too
                        _waitUntil = _cFrames + 700;
                        _cSub = 3;
                    }
                    if (_cFrames > 120) Fail("calm_use: E never upgraded the calmed wild to a standing offer");
                    break;
                }
                if (_cFrames < _waitUntil) break;
                Check("standing offer STILL true at T+700 (the retired window never touched it again)",
                      _eWild!.RecruitOffered && _eWild.CalmedWindowFrames == 0,
                      $"offer={_eWild.RecruitOffered} window={_eWild.CalmedWindowFrames}");
                if (_failed) return;
                GD.Print("LA_GATE: CALM_E_STANDS — the skill's window retired under E; the standing offer outlives it");
                CNext(6);
                break;

            case 6:   // CALM_WINDOW_EXPIRES — fresh cast, 602 frames, pay joins NOBODY
                if (_cSub == 0) { _playerBody!.GlobalPosition += new Vector3(60f, 0f, 0f); _cSub = 1; break; }   // far from every standing wild
                if (_cSub == 1)
                {
                    if (_cFrames < 30) break;   // let the followers settle
                    _calmTarget = _expWild = SpawnCalmWild();
                    player.Manna = 20;
                    CalmPress();
                    _cSub = 2;
                    break;
                }
                if (_cSub == 2)
                {
                    if (!CalmMarkerLanded("EXPIRES cast")) break;
                    _waitUntil = _cFrames + 602 + 12;   // T+600 close + settle margin (§2 frame table)
                    _rosterBefore = roster.Count;
                    _cSub = 3;
                    break;
                }
                if (_cFrames < _waitUntil) break;
                if (_cSub == 3)
                {
                    _playerBody!.GlobalPosition = _expWild!.GlobalPosition;   // co-locate: the pay press must test the offer IN range (player drifts during the long wait)
                    Input.ActionPress("pay_wage"); _cPressCount = 1; _cSub = 4; break;
                }
                if (_cFrames % 6 == 0 && _cPressCount < 3)   // ≥2 press edges (RecruitPhase idiom)
                { _playerBody!.GlobalPosition = _expWild!.GlobalPosition; Input.ActionRelease("pay_wage"); Input.ActionPress("pay_wage"); _cPressCount++; }
                if (roster.Count != _rosterBefore) Fail("calm_use: an EXPIRED window still recruited — decay broken");
                if (_cPressCount < 3) break;
                Input.ActionRelease("pay_wage");
                Check("expired: flag false, window 0, still wild, the pay press joined nobody",
                      !_expWild!.RecruitOffered && _expWild.CalmedWindowFrames == 0 && _expWild.Wild,
                      $"offer={_expWild.RecruitOffered} window={_expWild.CalmedWindowFrames} roster={roster.Count}");
                if (_failed) return;
                GD.Print("LA_GATE: CALM_WINDOW_EXPIRES — offer EXPIRED at T+600; pay_wage found no offer, skip-arm semantics unchanged");
                CNext(7);
                break;

            case 7:   // CALM_LOAD_CLEARED — REAL save_game / load_game presses
                if (_cSub == 0)
                {
                    var store = new GodotSaveStore();
                    if (System.IO.File.Exists(store.SavePath)) System.IO.File.Delete(store.SavePath);   // own the save (MC 3910)
                    player.Manna = 20;
                    _mannaBefore = 20;   // the value the save must carry back
                    Input.ActionPress("save_game");
                    _cSub = 1;
                    break;
                }
                if (_cSub == 1)
                {
                    if (_cFrames < 6) break;
                    Input.ActionRelease("save_game");
                    _calmTarget = SpawnCalmWild();   // the calmed witness A
                    CalmPress();
                    _cSub = 2;
                    break;
                }
                if (_cSub == 2)
                {
                    if (!CalmMarkerLanded("LOAD cast")) break;
                    Check("cast landed before the load (window fresh on A, Manna 20-12=8)",
                          _snapOffer && _snapWindow == 600 && _snapManna == 8,
                          $"offer={_snapOffer} window={_snapWindow} manna={_snapManna}");
                    if (_failed) return;
                    Input.ActionPress("load_game");
                    _waitUntil = _cFrames + 8;   // the restore lands a frame after the press
                    _cSub = 3;
                    break;
                }
                if (_cFrames < _waitUntil) break;
                Input.ActionRelease("load_game");
                Check("REAL load_game: Manna reads the SAVED value (the rollback is why the window must die)",
                      player.Manna == _mannaBefore, $"manna={player.Manna} saved={_mannaBefore}");
                Check("A (calmed): window 0 AND no offer — cleared by the load seam (R9 mirror)",
                      !_calmTarget!.RecruitOffered && _calmTarget.CalmedWindowFrames == 0,
                      $"offer={_calmTarget.RecruitOffered} window={_calmTarget.CalmedWindowFrames}");
                Check("B (standing): offer KEPT (true, window 0) — the seam touches no standing offer",
                      _eWild!.RecruitOffered && _eWild.CalmedWindowFrames == 0,
                      $"offer={_eWild.RecruitOffered} window={_eWild.CalmedWindowFrames}");
                if (_failed) return;
                GD.Print("LA_GATE: CALM_LOAD_CLEARED — calm state died WITH its body at the load-restore seam; the standing offer untouched");
                GD.Print("LA_GATE: PASS — calming speak verified end-to-end (cast+scan, pay-in-window join, refusals, expiry, E stands, load-cleared)");
                _asserted = true;
                _stage = 6;
                _stageFrames = 0;
                _holdStartPhys = _physFrames;
                break;
        }
    }

    // ---- calm_neg: the SAME seam off must leak NOTHING from the calm arm ----
    private void CalmNegStage()
    {
        var player = _director!.PlayerModel;
        if (_cSub == 0)
        {
            if (_sKills < 1)   // ONE extraction: the consensus unlocks (so a BYPASS mutation could leak)
            {
                if (_cFrames > 3000) Fail("calm_neg: farm never produced an extraction");
                if (!SFarmLoop()) SPress("travel", ref _sTravelToggle);
                return;
            }
            Input.ActionRelease("attack");
            Input.ActionRelease("travel");
            player.Manna = 30;               // FUNDED: the SEAM, not the economy, must be what stalls the cast
            _calmTarget = SpawnCalmWild();
            _cSub = 1;
            return;
        }
        SPress("skill_3", ref _sSkillToggle);
        if (_cFrames < 300) return;          // frames the cast WOULD have needed
        Input.ActionRelease("skill_3");
        if (player.Manna != 30 || _calmEmits != 0 || !_calmTarget!.Wild
            || _calmTarget.RecruitOffered || _calmTarget.CalmedWindowFrames != 0)
        {
            Fail($"calm_neg: the calm arm ran despite the seam off (manna={player.Manna}, emits={_calmEmits}, offer={_calmTarget.RecruitOffered}, window={_calmTarget.CalmedWindowFrames}) — the negative control is broken");
            return;
        }
        GD.Print($"LA_GATE: NEG_CALM: skill seam off — a funded unlocked press opened nothing, spent nothing, emitted nothing — break detected (kills={_sKills}, unlocked={LiveCalm().CalmingSpeak})");
        Quit(1);
    }
}
