using Godot;
using LastAnimal.Dna;
using LastAnimal.Skills;
using LastAnimal.World;

// Last Animal — S16 resonance-passive runtime proof (MC 10200, code,
// 2026-10-07).
//
// Partial-class half of RuntimeIntegrationProof: proves ONE passive
// end-to-end ON THE LIVE SCENE, band reached via REAL extracts (the DoD
// wording): farm one real extraction through the ONE kill path, then read
// the OBSERVED NUMBER CHANGE at the shipped add seam — the kill credited
// KillMannaGain + the Resonant Draw bonus (not the base alone), every kill
// after credits the same band-on amount, and the second shipped seam (Mend)
// heals MendHeal + Deep Mend on a real skill_2 press. The consensus is read
// live through PlayerMutations (never a cache): width 0 -> no bands before
// the first extraction, width 6 (the census cap) after.
//
// Planted-bad targets (DoD): a SPEND-SITE mutation (the director's kill
// rider drops passives.KillMannaBonus, or the skill_2 arm drops
// passives.MendHealBonus) turns PASSIVES_KILL_BONUS / PASSIVES_MEND_BONUS
// RED here on the live scene; band-threshold drift goes RED first in
// ci/skill_test.sh's F10/F13 rows (cheaper layer catches it first).
//
// Mode (LA_GATE_MODE): passives — stage 65. Reuses the skill partial's
// SSubscribe/SFarmLoop/SPress/SNext machinery (same class, skill modes'
// proven farm loop) — no parallel farm mechanism beside it.
public partial class RuntimeIntegrationProof : SceneTree
{
    private void RunPassivesStage()
    {
        if (!SSubscribe()) return;
        _sFrames++;
        var player = _director!.PlayerModel;

        switch (_sPhase)
        {
            case 0:   // pre-band baseline: consensus width 0, no bands, no Manna
                {
                    var p0 = PlayerMutations.Passives(
                        EcosystemAdaptation.ModelPlayerDna(_director.SpokenDna));
                    Check("pre-extraction baseline: zero positions, zero bands",
                          !p0.ResonantDraw && !p0.DeepMend && player.Manna == 0,
                          $"{p0} manna={player.Manna}");
                    if (_failed) return;
                    GD.Print("LA_GATE: PASSIVES_BASELINE — 0 learned positions, no bands, Manna=0");
                    SNext(1);
                }
                break;

            case 1:   // ONE real extraction through the ONE kill path reaches the census width
                if (_sKills < 1)
                {
                    if (_sFrames > SkillFrameBudget) Fail("passives: farm never produced the first extraction");
                    if (!SFarmLoop()) SPress("travel", ref _sTravelToggle);
                    break;
                }
                Input.ActionRelease("attack");
                Input.ActionRelease("travel");
                {
                    var profile = EcosystemAdaptation.ModelPlayerDna(_director.SpokenDna);
                    var p = PlayerMutations.Passives(profile);
                    Check("the real extraction reached the census width 6 (both bands live)",
                          profile.Counters.Length == 6 && p.ResonantDraw && p.DeepMend,
                          $"width={profile.Counters.Length} {p}");
                    if (_failed) return;
                    // THE observed number change: base + bonus, not base alone.
                    Check("the kill at the shipped add seam credited base + Resonant Draw EXACTLY",
                          player.Manna == SkillState.KillMannaGain + p.KillMannaBonus
                          && p.KillMannaBonus == PlayerMutations.ResonantDrawKillManna
                          && player.Manna != SkillState.KillMannaGain,
                          $"manna={player.Manna} base={SkillState.KillMannaGain} bonus={p.KillMannaBonus}");
                    if (_failed) return;
                    GD.Print($"LA_GATE: PASSIVES_KILL_BONUS — 1 real extract -> width 6, kill credited {player.Manna} ({SkillState.KillMannaGain} base + {PlayerMutations.ResonantDrawKillManna} Resonant Draw), observed change vs the base const");
                }
                SNext(2);
                break;

            case 2:   // the band is persistent: the NEXT real kill credits the same amount
                {
                    if (_sSub == 0)
                    {
                        _mannaBeforeAction = player.Manna;   // capture ONCE (7 after kill #1)
                        _sSub = 1;
                    }
                    if (_sKills < 2)
                    {
                        if (_sFrames > SkillFrameBudget) Fail("passives: farm never produced the second extraction");
                        if (!SFarmLoop()) SPress("travel", ref _sTravelToggle);
                        break;
                    }
                    Input.ActionRelease("attack");
                    Input.ActionRelease("travel");
                    Check("second kill credited base + bonus again (band persists, one payment each)",
                          player.Manna == _mannaBeforeAction + SkillState.KillMannaGain + PlayerMutations.ResonantDrawKillManna,
                          $"manna {_mannaBeforeAction} -> {player.Manna}");
                    if (_failed) return;
                    GD.Print($"LA_GATE: PASSIVES_KILL_CREDIT — second real extract credited the same {SkillState.KillMannaGain + PlayerMutations.ResonantDrawKillManna}; the band rides every kill");
                    SNext(3);
                }
                break;

            case 3:   // the second shipped seam: Mend heals base + Deep Mend on a real skill_2 press
                {
                    if (_sSub == 0)
                    {
                        player.Manna = SkillState.MendCost;   // fund the mend (skill_use idiom)
                        // Measurable headroom WITHOUT risking a kill from the
                        // proof's own damage: drop to 20 HP, not below.
                        player.TakeDamage(System.Math.Max(0, player.Health - 20));
                        _hpBeforeHit = player.Health;
                        Input.ActionPress("skill_2");
                        _sSub = 1;
                        break;
                    }
                    if (_sSkillUsed == 0)
                    {
                        if (_sFrames > 60) Fail("passives: skill_2 never produced a SkillUsed emit (mend path broken)");
                        break;   // let the director's poll consume the press
                    }
                    Input.ActionRelease("skill_2");
                    Check("Mend healed base + Deep Mend EXACTLY through the shipped seam",
                          player.Health == System.Math.Min(player.MaxHealth,
                              _hpBeforeHit + SkillState.MendHeal + PlayerMutations.DeepMendHeal)
                          && player.Health == _hpBeforeHit + SkillState.MendHeal + PlayerMutations.DeepMendHeal,
                          $"hp={player.Health} (was {_hpBeforeHit}) heal={SkillState.MendHeal}+{PlayerMutations.DeepMendHeal}");
                    if (_failed) return;
                    GD.Print($"LA_GATE: PASSIVES_MEND_BONUS — Mend healed {SkillState.MendHeal + PlayerMutations.DeepMendHeal} (base {SkillState.MendHeal} + Deep Mend {PlayerMutations.DeepMendHeal}) on the live scene");
                    GD.Print("LA_GATE: PASS — resonance passives verified end-to-end (baseline, band reached by REAL extracts, kill-seam number change, persistence, mend-seam number change)");
                    _asserted = true;
                    _stage = 6;
                    _stageFrames = 0;
                    _holdStartPhys = _physFrames;
                }
                break;
        }
    }
}
