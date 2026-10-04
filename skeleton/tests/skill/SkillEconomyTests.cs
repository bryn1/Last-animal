using Xunit;
using LastAnimal.Combat;
using LastAnimal.Skills;

// Last Animal — skill economy F-act tests (MC 3912 stage 2e, code, 2026-10-02).
//
// The stage-2e F-act list (plan §B 2e, DA-c2 D3) pinned headless on the real
// SkillState over the real PlayerController:
//   F1 pay -> Manna drops EXACTLY once (one spend per successful use)
//   F2 kill -> +Manna (rider on the existing kill handler)
//   F3 armed hit damage == MeleeDamage x multiplier
//   F4 unarmed hit damage == base MeleeDamage
//   F5 the arm flag is consumed after ONE hit
//   F6 insufficient Manna is REJECTED (nothing spent, no arm)
// plus: locked use rejected without spending, Mend raises health, and the
// cap-100 clamp every writer rides.
//
// F3/F4/F5 read the damage VALUE the single DealDamage call site receives
// (ConsumeArmedDamage is the value the root wraps) — the runtime proof
// (skill_use mode) re-asserts the same arithmetic through the REAL
// _combat.DealDamage path on live enemies.
//
// PLANTED-BAD TARGET 1 hook: the multiplier applied at the hit site is
// exercised end-to-end by the runtime mode; a wrap that forgets to multiply
// turns ARMED_HIT_MULTIPLIED red there.
namespace LastAnimal.Tests.Skill;

public class SkillEconomyTests
{
    private static (PlayerController player, SkillState skills) Fresh(int manna = 0)
    {
        var player = new PlayerController(new CombatVec3(0f, 0f, 0f));
        player.Manna = manna;
        return (player, new SkillState(player));
    }

    [Fact]
    public void F1_pay_drops_Manna_exactly_once_per_use()
    {
        var (player, skills) = Fresh(manna: 30);
        Assert.True(skills.TryInvertStrike(unlocked: true));
        Assert.Equal(30 - SkillState.InvertStrikeCost, player.Manna);   // exactly one drop
    }

    [Fact]
    public void F2_kill_gains_Manna()
    {
        var (player, skills) = Fresh();
        skills.OnKill();
        Assert.Equal(SkillState.KillMannaGain, player.Manna);
        skills.OnKill();
        Assert.Equal(2 * SkillState.KillMannaGain, player.Manna);
    }

    [Fact]
    public void F3_armed_hit_damage_is_MeleeDamage_times_multiplier()
    {
        var (player, skills) = Fresh(manna: 30);
        Assert.True(skills.TryInvertStrike(unlocked: true));
        int armed = skills.ConsumeArmedDamage(player.MeleeDamage);
        Assert.Equal(player.MeleeDamage * SkillState.InvertStrikeMultiplier, armed);
    }

    [Fact]
    public void F4_unarmed_hit_damage_is_base_MeleeDamage()
    {
        var (player, skills) = Fresh(manna: 30);
        Assert.Equal(player.MeleeDamage, skills.ConsumeArmedDamage(player.MeleeDamage));
    }

    [Fact]
    public void F5_arm_flag_is_consumed_after_one_hit()
    {
        var (player, skills) = Fresh(manna: 30);
        Assert.True(skills.TryInvertStrike(unlocked: true));
        Assert.True(skills.IsArmed);
        int first = skills.ConsumeArmedDamage(player.MeleeDamage);       // the armed hit
        Assert.False(skills.IsArmed);
        int second = skills.ConsumeArmedDamage(player.MeleeDamage);      // next hit: base
        Assert.NotEqual(first, second);
        Assert.Equal(player.MeleeDamage, second);
    }

    [Fact]
    public void F6_insufficient_Manna_is_rejected_without_spending()
    {
        var (player, skills) = Fresh(manna: SkillState.InvertStrikeCost - 1);
        Assert.False(skills.TryInvertStrike(unlocked: true));
        Assert.Equal(SkillState.InvertStrikeCost - 1, player.Manna);     // nothing spent
        Assert.False(skills.IsArmed);                                     // no arm
        Assert.Equal(player.MeleeDamage, skills.ConsumeArmedDamage(player.MeleeDamage));
    }

    [Fact]
    public void Locked_use_is_rejected_without_spending()
    {
        // The unlock verdict is passed in live (plan §G D4); locked => no pay.
        var (player, skills) = Fresh(manna: 100);
        Assert.False(skills.TryInvertStrike(unlocked: false));
        Assert.False(skills.TryMend(unlocked: false));
        Assert.Equal(100, player.Manna);
    }

    [Fact]
    public void F_mend_raises_health_and_drops_Manna_once()
    {
        var (player, skills) = Fresh(manna: 30);
        player.TakeDamage(40);                       // 60/100
        int before = player.Health;
        Assert.True(skills.TryMend(unlocked: true));
        Assert.Equal(before + SkillState.MendHeal, player.Health);
        Assert.Equal(30 - SkillState.MendCost, player.Manna);
    }

    [Fact]
    public void Manna_gain_clamps_at_cap_100()
    {
        var (player, skills) = Fresh(manna: PlayerController.MannaCap);
        skills.OnKill();
        Assert.Equal(PlayerController.MannaCap, player.Manna);           // capped, no overflow
        Assert.Equal(100, PlayerController.MannaCap);                    // owner ruling D3's number
    }

    [Fact]
    public void Spend_beyond_balance_refuses_and_negative_cost_is_refused()
    {
        var (player, skills) = Fresh(manna: 5);
        Assert.False(skills.TrySpend(6));
        Assert.False(skills.TrySpend(-1));
        Assert.Equal(5, player.Manna);
    }

    // MC 10031 Calming Speak economy legs (design §4 unit list). The TARGET
    // SCAN lives in the director (runtime calm_use legs); here the economy
    // shape is pinned: cost 12, ONE payment per success, refuse spends zero.

    [Fact]
    public void F7_calm_cost_spend_once()
    {
        var (player, skills) = Fresh(manna: 30);
        Assert.Equal(12, SkillState.CalmingSpeakCost);   // between Q=10 and R=15
        Assert.True(skills.TryCalmingSpeak(unlocked: true));
        Assert.Equal(30 - SkillState.CalmingSpeakCost, player.Manna);   // exactly one drop
        Assert.True(skills.TryCalmingSpeak(unlocked: true));            // re-cast pays AGAIN (no cooldown, D3)
        Assert.Equal(30 - 2 * SkillState.CalmingSpeakCost, player.Manna);
    }

    [Fact]
    public void F8_calm_refuse_no_spend()
    {
        // Locked AND short-balance rejections both spend NOTHING (the design's
        // one money rule — a refusal never spends, spend-after-scan or not).
        var (player, skills) = Fresh(manna: 100);
        Assert.False(skills.TryCalmingSpeak(unlocked: false));
        Assert.Equal(100, player.Manna);
        player.Manna = SkillState.CalmingSpeakCost - 1;
        Assert.False(skills.TryCalmingSpeak(unlocked: true));
        Assert.Equal(SkillState.CalmingSpeakCost - 1, player.Manna);
    }
}
