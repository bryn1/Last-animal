using Godot;
using LastAnimal.Dna;
using LastAnimal.Skills;
using LastAnimal.World;
using System.Collections.Generic;

// Last Animal — stage 2e skill runtime proof (MC 3912, code, 2026-10-02).
//
// Partial-class half of RuntimeIntegrationProof: the skill chain driven
// against the REAL playable scene (WorldDirector InitSkills + SkillState +
// the ONE _combat.DealDamage call site). Every arithmetic F-act of the pure
// gate is re-asserted END-TO-END here on live enemies through the real kill
// path: the armed hit must multiply damage THROUGH the single DealDamage
// site (planted-bad target 1: a wrap that forgets the multiplier turns
// ARMED_HIT_MULTIPLIED red; the kill-path grep leg staying at 1 is gated
// by ci/skill_test.sh's sibling runtime leg in runtime_integration_test.sh).
//
// Modes (LA_GATE_MODE):
//   skill_use — farm three extractions (+Manna rides the existing
//     OnDnaExtracted handler site, exact amount per kill: KILL_MANNA_GAIN);
//     the live consensus unlocks both skills (UNLOCK_*); a base hit deals
//     exactly MeleeDamage (UNARMED_HIT_BASE); skill_1 pays exactly once and
//     arms (MANNA_SPEND_ONCE + SKILL_USED invert_strike); the next hit deals
//     MeleeDamage x multiplier (ARMED_HIT_MULTIPLIED) and the arm is gone —
//     the hit after is base again (ARM_CONSUMED); a short balance refuses
//     without spending (MANNA_REJECTED); skill_2 pays once and raises health
//     (MEND_HEALTH_UP + SKILL_USED mend). PASS.
//   skill_neg — the SetSkillActionsEnabled(false) gate seam is set on the
//     INSTANTIATED root BEFORE AddChild (no_spawn idiom): kills happen, the
//     skill economy must not — zero Manna, no arm, no SkillUsed emit;
//     detection prints NEG_SKILL and exits non-zero.
public partial class RuntimeIntegrationProof : SceneTree
{
    private const int SkillFrameBudget = 6000;   // skill_use (farm + measured hits)

    private int _sPhase;
    private int _sFrames;
    private int _sSub;              // sub-step inside a phase
    private int _sSkillUsed;
    private int _sKills;
    private bool _sSubscribed;
    private int _mannaBeforeAction;
    private int _hpBeforeHit;
    private EnemyActor? _hitTarget;
    private int _skillUsedBefore;

    /// <summary>Stage-60 dispatch (called from the main file's stage switch).</summary>
    private void RunSkillStage()
    {
        switch (_mode)
        {
            case "skill_use": SkillUseStage(); break;
            case "skill_neg": SkillNegStage(); break;
        }
    }

    /// <summary>Marker taps on the REAL bus (subscribe once, first skill
    /// frame): every SkillUsed emit prints its marker line; the counter
    /// proves "exactly once" per action.</summary>
    private bool SSubscribe()
    {
        if (_sSubscribed) return true;
        if (_bus == null || _director == null) return false;
        _bus.SkillUsed += id =>
        {
            GD.Print($"LA_GATE: SKILL_USED {id}");
            _sSkillUsed++;
        };
        _bus.DnaExtracted += _ => _sKills++;
        _sSubscribed = true;
        return true;
    }

    /// <summary>Farm the nearest live non-boss enemy (QKillLoop idiom);
    /// false = the zone pool is dry (travel on).</summary>
    private bool SFarmLoop()
    {
        EnemyActor? target = null;
        float best = float.MaxValue;
        foreach (var e in _director!.Enemies)
        {
            if (e.IsDead || !GodotObject.IsInstanceValid(e)) continue;
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
        SPress("attack", ref _sAttackToggle);
        return true;
    }

    private int _sAttackToggle;
    private int _sSkillToggle;

    private static void SPress(string action, ref int toggle)
    {
        toggle++;
        if (toggle % 4 == 1) Input.ActionPress(action);
        else if (toggle % 4 == 3) Input.ActionRelease(action);
    }

    /// <summary>ONE deterministic hit: press once (IsActionJustPressed
    /// fires exactly once), return true when the target's health dropped.</summary>
    private bool SHitOnce()
    {
        var t = _hitTarget;
        if (t == null || !GodotObject.IsInstanceValid(t)) return false;
        Vector3 p = t.GlobalPosition;
        _playerBody!.GlobalPosition = new Vector3(p.X - 0.8f, p.Y, p.Z);
        if (_sSub == 0)
        {
            // Pin the target's health so the delta reads EXACTLY (a base
            // hit must not kill a 200-HP target; an armed hit must not
            // overkill-clamp the delta below base x multiplier either).
            t.Ai.ApplySpawnStats(200, t.Ai.Damage, t.Ai.Speed);
            _hpBeforeHit = t.Ai.Health;
            Input.ActionPress("attack");
            _sSub = 1;
            return false;
        }
        bool landed = t.Ai.Health < _hpBeforeHit || t.IsDead;
        if (landed) Input.ActionRelease("attack");
        return landed;
    }

    private int HitDelta() => _hpBeforeHit - (_hitTarget?.Ai.Health ?? _hpBeforeHit);

    private void SNext(int phase)
    {
        _sPhase = phase;
        _sFrames = 0;
        _sSub = 0;
    }

    private void SkillUseStage()
    {
        if (!SSubscribe()) return;
        _sFrames++;
        var player = _director!.PlayerModel;

        switch (_sPhase)
        {
            case 0:   // farm exactly 3 extractions; +Manna per kill is EXACT
                if (_sKills < 3)
                {
                    if (_sFrames > SkillFrameBudget) Fail("skill_use: farm never produced 3 extractions");
                    if (!SFarmLoop()) SPress("travel", ref _sTravelToggle);
                    break;
                }
                Input.ActionRelease("attack");
                Input.ActionRelease("travel");
                Check($"kill->+Manna: 3 kills credited exactly 3 x {SkillState.KillMannaGain}",
                      player.Manna == 3 * SkillState.KillMannaGain, $"manna={player.Manna}");
                if (_failed) return;
                GD.Print($"LA_GATE: KILL_MANNA_GAIN — {_sKills} kills on the existing DnaExtracted handler credited {_sKills * SkillState.KillMannaGain} Manna");
                SNext(1);
                break;

            case 1:   // live consensus unlocks BOTH launch skills (owner D4)
                {
                    var u = PlayerMutations.Unlocked(
                        EcosystemAdaptation.ModelPlayerDna(_director.SpokenDna));
                    Check("consensus unlocks Invert Strike + Mend live", u.InvertStrike && u.Mend,
                          u.ToString());
                    if (_failed) return;
                    GD.Print("LA_GATE: UNLOCK_INVERT_STRIKE + UNLOCK_MEND — PlayerMutations.Unlocked read live from the consensus");
                    SNext(2);
                }
                break;

            case 2:   // F4 at the REAL hit site: unarmed hit == base MeleeDamage
                if (!SPickTarget())
                {   // pool dry (the farm emptied it): cycle zones (QPressTravel idiom)
                    SPress("travel", ref _sTravelToggle);
                    if (_sFrames > SkillFrameBudget) Fail("skill_use: no target for the unarmed hit");
                    break;
                }
                Input.ActionRelease("travel");
                if (!SHitOnce())
                {
                    if (_sFrames > 240) Fail("skill_use: unarmed hit never landed");
                    break;
                }
                Check("unarmed hit damage == base MeleeDamage at the DealDamage site",
                      HitDelta() == player.MeleeDamage, $"delta={HitDelta()} base={player.MeleeDamage}");
                if (_failed) return;
                GD.Print($"LA_GATE: UNARMED_HIT_BASE — one hit dealt exactly MeleeDamage={player.MeleeDamage}");
                SNext(3);
                break;

            case 3:   // F1: arm Invert Strike — pays EXACTLY once, sets the arm
                {
                    if (_sSub == 0)
                    {
                        _mannaBeforeAction = player.Manna;
                        _skillUsedBefore = _sSkillUsed;
                        Input.ActionPress("skill_1");
                        _sSub = 1;
                        break;
                    }
                    if (_sSkillUsed - _skillUsedBefore == 0)
                    {
                        if (_sFrames > 60) Fail("skill_use: skill_1 never produced a SkillUsed emit (arm path broken)");
                        break;   // let the director's poll consume the press
                    }
                    Input.ActionRelease("skill_1");
                    Check("pay -> Manna dropped EXACTLY once (Invert Strike cost)",
                          player.Manna == _mannaBeforeAction - SkillState.InvertStrikeCost,
                          $"manna { _mannaBeforeAction} -> {player.Manna}");
                    if (_failed) return;
                    Check("the use armed the multiplier", _director.Skills.IsArmed, "armed=true");
                    if (_failed) return;
                    GD.Print($"LA_GATE: MANNA_SPEND_ONCE — {SkillState.InvertStrikeCost} Manna spent exactly once, Invert Strike ARMED");
                    SNext(4);
                }
                break;

            case 4:   // F3 at the REAL hit site: armed hit == MeleeDamage x mult
                if (!SPickTarget())
                {   // pool dry (the farm emptied it): cycle zones (QPressTravel idiom)
                    SPress("travel", ref _sTravelToggle);
                    if (_sFrames > SkillFrameBudget) Fail("skill_use: no target for the armed hit");
                    break;
                }
                Input.ActionRelease("travel");
                if (!SHitOnce())
                {
                    if (_sFrames > 240) Fail("skill_use: armed hit never landed");
                    break;
                }
                Check("armed hit damage == MeleeDamage x multiplier THROUGH the single DealDamage site",
                      HitDelta() == player.MeleeDamage * SkillState.InvertStrikeMultiplier
                      && SkillState.InvertStrikeMultiplier == 3,   // S7 F-4 (TEST-verdict-10030 F-1): literal-3 balance pin — drift 3→4 goes RED here, not silently
                      $"delta={HitDelta()} expected={player.MeleeDamage * SkillState.InvertStrikeMultiplier}");
                if (_failed) return;
                GD.Print($"LA_GATE: ARMED_HIT_MULTIPLIED — one hit dealt {player.MeleeDamage * SkillState.InvertStrikeMultiplier} (x{SkillState.InvertStrikeMultiplier})");
                SNext(5);
                break;

            case 5:   // F5: the arm rode exactly ONE hit — the next hit is base
                Check("the arm was consumed by exactly one hit", !_director.Skills.IsArmed, "armed=false");
                if (_failed) return;
                if (!SPickTarget())
                {   // pool dry (the farm emptied it): cycle zones (QPressTravel idiom)
                    SPress("travel", ref _sTravelToggle);
                    if (_sFrames > SkillFrameBudget) Fail("skill_use: no target for the follow-up hit");
                    break;
                }
                Input.ActionRelease("travel");
                if (!SHitOnce())
                {
                    if (_sFrames > 240) Fail("skill_use: follow-up hit never landed");
                    break;
                }
                Check("post-arm hit back to base MeleeDamage",
                      HitDelta() == player.MeleeDamage, $"delta={HitDelta()}");
                if (_failed) return;
                GD.Print("LA_GATE: ARM_CONSUMED — second hit dealt base damage; the arm rode exactly one hit");
                SNext(6);
                break;

            case 6:   // F6 at the REAL arm path: short balance refuses, spends nothing
                {
                    if (_sSub == 0)
                    {
                        player.Manna = SkillState.InvertStrikeCost - 1;   // 9 < 10
                        _mannaBeforeAction = player.Manna;
                        _skillUsedBefore = _sSkillUsed;
                        Input.ActionPress("skill_1");
                        _sSub = 1;
                        break;
                    }
                    if (_sFrames < 12) break;   // give the poll its frames
                    Input.ActionRelease("skill_1");
                    Check("insufficient-Manna use REJECTED: nothing spent, nothing armed, no emit",
                          player.Manna == _mannaBeforeAction && !_director.Skills.IsArmed
                          && _sSkillUsed == _skillUsedBefore,
                          $"manna={player.Manna} armed={_director.Skills.IsArmed} emits={_sSkillUsed - _skillUsedBefore}");
                    if (_failed) return;
                    GD.Print("LA_GATE: MANNA_REJECTED — short balance refused without spending");
                    SNext(7);
                }
                break;

            case 7:   // Mend: pays once, health rises, SKILL_USED mend rides
                {
                    if (_sSub == 0)
                    {
                        player.Manna = SkillState.MendCost;   // fund the mend
                        // Measurable headroom WITHOUT risking a kill from
                        // the proof's own damage: drop to 20 HP, not below.
                        player.TakeDamage(System.Math.Max(0, player.Health - 20));
                        _mannaBeforeAction = player.Manna;
                        _skillUsedBefore = _sSkillUsed;
                        _hpBeforeHit = player.Health;
                        Input.ActionPress("skill_2");
                        _sSub = 1;
                        break;
                    }
                    if (_sSkillUsed - _skillUsedBefore == 0)
                    {
                        if (_sFrames > 60) Fail("skill_use: skill_2 never produced a SkillUsed emit (mend path broken)");
                        break;
                    }
                    Input.ActionRelease("skill_2");
                    Check("Mend raised health by the mend amount and paid EXACTLY once",
                          player.Health == System.Math.Min(player.MaxHealth, _hpBeforeHit + SkillState.MendHeal)
                          && player.Health > _hpBeforeHit
                          && player.Manna == _mannaBeforeAction - SkillState.MendCost,
                          $"hp={player.Health} (was {_hpBeforeHit}) manna={player.Manna}");
                    if (_failed) return;
                    GD.Print("LA_GATE: MEND_HEALTH_UP — Mend healed and paid exactly once");
                    GD.Print("LA_GATE: PASS — skill economy verified end-to-end (kill gain, live unlock, armed multiplier at the ONE hit site, arm consumed, rejection, mend)");
                    _asserted = true;
                    _stage = 6;
                    _stageFrames = 0;
                    _holdStartPhys = _physFrames;
                }
                break;
        }
    }

    /// <summary>Pick a live non-boss enemy for a measured hit (boosted to
    /// 200 HP inside SHitOnce so deltas read exactly).</summary>
    private bool SPickTarget()
    {
        if (_hitTarget != null && GodotObject.IsInstanceValid(_hitTarget) && !_hitTarget.IsDead)
            return true;
        EnemyActor? best = null;
        float bestD = float.MaxValue;
        foreach (var e in _director!.Enemies)
        {
            if (e.IsDead || !GodotObject.IsInstanceValid(e)) continue;
            if (ReferenceEquals(e, _director.BossActor)) continue;
            float d = e.GlobalPosition.DistanceTo(_playerBody!.GlobalPosition);
            if (d < bestD) { bestD = d; best = e; }
        }
        _hitTarget = best;
        return best != null;
    }

    private int _sTravelToggle;

    // ---- skill_neg: the SetSkillActionsEnabled(false) seam must stall the economy
    private void SkillNegStage()
    {
        if (!SSubscribe()) return;
        _sFrames++;
        // Keep playing: farm kills and press the skill arm like a player would.
        if (!SFarmLoop()) SPress("travel", ref _sTravelToggle);
        SPress("skill_1", ref _sSkillToggle);
        if (_sFrames < 300) return;   // frames the economy WOULD have needed

        Input.ActionRelease("attack");
        Input.ActionRelease("travel");
        Input.ActionRelease("skill_1");
        var player = _director!.PlayerModel;
        if (player.Manna != 0 || _director.Skills.IsArmed || _sSkillUsed != 0)
        {
            Fail($"skill_neg: the skill economy ran despite the seam off (manna={player.Manna}, armed={_director.Skills.IsArmed}, emits={_sSkillUsed}) — the negative control is broken");
            return;
        }
        GD.Print($"LA_GATE: NEG_SKILL: skill seam off — {_sKills} kills credited no Manna, the arm stayed inert — break detected");
        Quit(1);
    }
}
