using Godot;
using LastAnimal.Companion;
using LastAnimal.Dna;
using LastAnimal.Skills;
using LastAnimal.World;

// Last Animal — MC 11297 PRODUCTION tame proof (D5 + D6), stage 110 (code, 2026-10-25).
//
// The MC 11270 playthrough found the whole recruit arc UNREACHABLE in the shipped
// game: Calming Speak (F) printed "REFUSED — no castable target" every press, because
// a wild creature was ONLY ever composed by the SpawnWildFollower TEST SEAM (D5), and
// a short-balance Mend press was SILENT — no reason, nothing spent (D6). This leg is
// the PRODUCTION path the guide promises:
//
//   player-guide.md:52-55 "recruit a wild creature by interacting with it (E) and
//     paying its first wage (P) — Calming Speak (F) can open that same offer at cast
//     range, but the wage remains the only way to join."
//   player-guide.md:91-96 "Calming Speak (F, 12 Manna): the nearest castable wild
//     creature inside cast reach … is calmed into a recruit offer that stands for 10
//     seconds … A press that finds no castable wild, or cannot pay, spends NOTHING."
//
// tame_use: farm to unlock Calming Speak + earn its 12 Manna, assert the boot-zone
// wild stands in the world WITHOUT any seam (the FieldBootWild production field — the
// D5 fix), walk up, press F (the synchronous SkillUsed tap snapshots: offer OPEN +
// window 600, 12 Manna spent, roster UNCHANGED — F never joins), press P (the FIRST
// wage is the SOLE join) → the follower ENTERS the roster (TAME_RECRUITED, named-GREEN).
// Then a short-balance Mend press must emit NO SkillUsed (refusal, nothing spent) and
// PRINT the named refusal (MEND_REFUSAL_PRINTED — the D6 fix; the battery greps the
// director's "Mend REFUSED — short balance" transcript line, silence before this card).
//
// tame_neg: the wild gate OFF (InitializeBootGates) — the before-fix state. A funded,
// UNLOCKED F press finds no castable target, spends nothing, joins nothing, emits
// nothing, and the "REFUSED — no castable target" transcript line is exactly what the
// playthrough saw; the control names NEG_TAME and exits non-zero (the calm_neg/
// roster_neg idiom: the correct refusal is the leak-guard, reported RED so the leg can
// never go vacuous-green).
public partial class RuntimeIntegrationProof : SceneTree
{
    private int _tPhase, _tFrames, _tSub;
    private bool _tSubscribed;
    private int _tCalmEmits, _tMendEmits;              // per-id SkillUsed counts
    private CompanionFollowBody? _tWild;
    private bool _tSnapOffer, _tSnapWild;
    private int _tSnapWindow, _tSnapManna;
    private int _tMannaBefore, _tRosterBefore, _tMannaBeforeMend;

    private void TNext(int phase) { _tPhase = phase; _tFrames = 0; _tSub = 0; }

    /// <summary>SkillUsed fires SYNCHRONOUSLY inside the cast (§1.2 step 5), so this
    /// tap snapshots the offer/window/manna on the cast frame itself and counts the
    /// per-id emits (a REFUSED cast emits NOTHING — the non-vacuous refusal witness).
    /// The generic SSubscribe keeps the DnaExtracted kill counter (_sKills) live.</summary>
    private bool TSubscribe()
    {
        if (_tSubscribed) return SSubscribe();
        if (_bus == null || _director == null) return false;
        _bus.SkillUsed += id =>
        {
            if (id == PlayerMutations.CalmingSpeakId)
            {
                _tCalmEmits++;
                if (_tWild == null) return;
                _tSnapWindow = _tWild.CalmedWindowFrames;
                _tSnapOffer = _tWild.RecruitOffered;
                _tSnapWild = _tWild.Wild;
                _tSnapManna = _director.PlayerModel.Manna;
            }
            else if (id == PlayerMutations.MendId) _tMendEmits++;
        };
        _tSubscribed = true;
        return SSubscribe();
    }

    /// <summary>Stage-110 dispatch (called from the entry's switch (_stage)).</summary>
    private void RunTameStage()
    {
        if (!TSubscribe()) return;
        _tFrames++;
        switch (_mode)
        {
            case "tame_use": TameUseStage(); break;
            case "tame_neg": TameNegStage(); break;
        }
    }

    private void TameUseStage()
    {
        var player = _director!.PlayerModel;
        var roster = _director.RosterView;
        switch (_tPhase)
        {
            case 0:   // farm 3 extractions (unlock + fund the 12-Manna cast); assert the wild is PRODUCTION's
                if (_sKills < 3)
                {
                    if (_tFrames > 3000) Fail("tame_use: farm never produced 3 extractions");
                    if (!SFarmLoop()) SPress("travel", ref _sTravelToggle);
                    break;
                }
                Input.ActionRelease("attack");
                Input.ActionRelease("travel");
                Check("the boot-zone recruit-able wild stands in the world with NO test seam (FieldBootWild, D5 fix)",
                      _director.WildBodies.Count == 1 && _director.WildBodies[0].Wild,
                      $"wilds={_director.WildBodies.Count}");
                Check("consensus unlocks Calming Speak live (>= 3 positions)", LiveCalm().CalmingSpeak, LiveCalm().ToString());
                if (_failed) return;
                GD.Print("LA_GATE: WILD_PRESENT_PROD — the recruit-able wild is fielded by PRODUCTION, not a seam (D5)");
                TNext(1);
                break;

            case 1:   // walk up, press F: the production calm opens the offer + spends 12, NO join
                if (_tSub == 0)
                {
                    _tWild = _director.WildBodies[0];
                    _playerBody!.GlobalPosition = _tWild.GlobalPosition;   // walk-up (roster-proof idiom)
                    _tMannaBefore = player.Manna; _tRosterBefore = roster.Count; _tCalmEmits = 0;
                    Input.ActionPress("skill_3");
                    _tSub = 1; break;
                }
                if (_tFrames < 20) break;
                Input.ActionRelease("skill_3");
                if (_tFrames < 30) break;
                Check("exactly ONE SkillUsed marker id calming_speak on the PRODUCTION wild",
                      _tCalmEmits == 1, $"emits={_tCalmEmits}");
                Check("Calming Speak OPENED the production wild's offer + 600-frame window, body STILL WILD, roster UNCHANGED (F never recruits)",
                      _tSnapOffer && _tSnapWindow == 600 && _tSnapWild && roster.Count == _tRosterBefore && _tWild!.Wild,
                      $"offer={_tSnapOffer} window={_tSnapWindow} wild={_tSnapWild} roster={roster.Count}");
                Check("Manna spent EXACTLY the guide's 12 (snapshot at the cast frame, read live after)",
                      _tSnapManna == _tMannaBefore - SkillState.CalmingSpeakCost && player.Manna == _tSnapManna,
                      $"{_tMannaBefore} -> {_tSnapManna} (now {player.Manna})");
                if (_failed) return;
                TNext(2);
                break;

            case 2:   // pay the FIRST wage (P) — the SOLE join authority → follower ENTERS the roster
                if (_tSub == 0) { _tRosterBefore = roster.Count; Input.ActionPress("pay_wage"); _tSub = 1; break; }
                if (_tFrames < 20) break;
                Input.ActionRelease("pay_wage");
                if (_tFrames < 30) break;
                Check("the first wage (P) JOINED the production wild — the follower ENTERS the roster (cap 3)",
                      roster.Count == _tRosterBefore + 1 && roster.Count <= 3 && !_tWild!.Wild,
                      $"roster={roster.Count} wild={_tWild.Wild}");
                if (_failed) return;
                GD.Print("LA_GATE: TAME_RECRUITED — PRODUCTION wild tamed via Calming Speak + first wage, named-GREEN (D5)");
                TNext(3);
                break;

            case 3:   // D6: a SHORT-BALANCE Mend press emits NOTHING, spends nothing, and PRINTS the named refusal
                if (_tSub == 0)
                {
                    player.Manna = SkillState.MendCost - 1;   // unlocked but short — the refusal path
                    _tMannaBeforeMend = player.Manna; _tMendEmits = 0;
                    Input.ActionPress("skill_2");
                    _tSub = 1; break;
                }
                if (_tFrames < 20) break;
                Input.ActionRelease("skill_2");
                if (_tFrames < 30) break;
                Check("short-balance Mend emitted NO SkillUsed and SPENT nothing (the refusal path, guide:95)",
                      _tMendEmits == 0 && player.Manna == _tMannaBeforeMend,
                      $"mendEmits={_tMendEmits} manna={player.Manna}/{_tMannaBeforeMend}");
                if (_failed) return;
                // The named line itself is on stdout (the battery greps the director's
                // 'Mend REFUSED — short balance' — the transcript IS before-silent/after-named).
                GD.Print("LA_GATE: MEND_REFUSAL_PRINTED — the D6 named Mend-REFUSED line is in the transcript (was silent)");
                GD.Print("LA_GATE: PASS");
                Quit(0);
                break;
        }
    }

    private void TameNegStage()
    {
        var player = _director!.PlayerModel;
        var roster = _director.RosterView;
        switch (_tPhase)
        {
            case 0:   // farm to unlock with the WILD GATE OFF — the MC 11270 D5 before-fix state
                if (_sKills < 3)
                {
                    if (_tFrames > 3000) Fail("tame_neg: farm never produced 3 extractions");
                    if (!SFarmLoop()) SPress("travel", ref _sTravelToggle);
                    break;
                }
                Input.ActionRelease("attack");
                Input.ActionRelease("travel");
                Check("the wild gate OFF leaves NO castable wild — the production field is the only source (D5 before-fix state)",
                      _director.WildBodies.Count == 0, $"wilds={_director.WildBodies.Count}");
                Check("consensus unlocks Calming Speak live (>= 3 positions)", LiveCalm().CalmingSpeak, LiveCalm().ToString());
                if (_failed) return;
                TNext(1);
                break;

            case 1:   // press F with NO target: the refusal path spends NOTHING, joins NOTHING, emits NOTHING
                if (_tSub == 0) { _tMannaBefore = player.Manna; _tRosterBefore = roster.Count; _tCalmEmits = 0; Input.ActionPress("skill_3"); _tSub = 1; break; }
                if (_tFrames < 30) break;
                Input.ActionRelease("skill_3");
                if (player.Manna != _tMannaBefore || roster.Count != _tRosterBefore || _tCalmEmits != 0 || _director.WildBodies.Count != 0)
                {
                    Fail($"tame_neg: the calm press acted with no castable target (manna={_tMannaBefore}->{player.Manna}, " +
                         $"roster={_tRosterBefore}->{roster.Count}, emits={_tCalmEmits}, wilds={_director.WildBodies.Count}) — the gate seam leaked");
                    return;
                }
                GD.Print($"LA_GATE: NEG_TAME: wild gate OFF — a funded unlocked press found no castable target, spent " +
                         $"nothing, joined nothing, emitted nothing (the D5 before-fix REFUSED state) — break detected (manna={player.Manna}, roster={roster.Count})");
                Quit(1);
                return;
        }
    }
}
