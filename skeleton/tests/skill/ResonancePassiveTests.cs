using Xunit;
using LastAnimal.Combat;
using LastAnimal.Dna;
using LastAnimal.Skills;

// Last Animal — S16 Resonance passive tests (MC 10200, code, 2026-10-07).
//
// Pins the owner-ratified Inc-4 §S16 contract: the passive authority
// PlayerMutations.Passives is a PURE function of the per-position consensus
// Counters ONLY (same §G D2 Option B authority as Unlocked), its derivation
// table is pinned ROW BY ROW below, and the two effects land EXACTLY at the
// shipped write seams — the kill-manna add (SkillState.OnKill) and the Mend
// heal (SkillState.TryMend) — nowhere else.
//
// Planted-bad targets (DoD):
//   BAND-THRESHOLD-DRIFT: change a tuning const in the ONE S16 block
//     (ResonantDrawPositions 1->2 or DeepMendPositions 4->5) and
//     F10/F13/F14 go RED by name — the table is pinned to the constants'
//     literal semantics, so drift cannot silently move a band.
//   SPEND-SITE-MUTATION: drop the bonus at a seam (OnKill ignoring
//     bonusManna, or TryMend adding only the base) and F11/F12 go RED —
//     the seam arithmetic is asserted exactly, cap-honest both ways.
//
// The census (S16 STEP 0, .audits/*-s16/census.md) confirmed both thresholds
// reachable in live content (every real extraction is 6 wide: the ONE live
// signature builder allocates 6 nucleotides), and a ≥7 band CUT; F14 re-derives
// the live-extraction width through the REAL builder + modeler so an
// unreachable-band re-introduction has a named RED waiting.
namespace LastAnimal.Tests.Skill;

public class ResonancePassiveTests
{
    private static CounterProfile Width(int width) =>
        new CounterProfile(new int[width], new int[width], width > 0 ? 1 : 0);

    [Fact]
    public void F10_passive_derivation_table_pinned()
    {
        // The FULL derivation table, row by row (width -> bands, bonuses).
        // Widths 1..5 are hand-buildable states (a load/legacy consensus can
        // carry them; the live step is 0 -> 6) — the FUNCTION is still
        // pinned at every width, that is what "derivation table" means.
        AssertPass(0, draw: false, mend: false, kill: 0, heal: 0);
        AssertPass(1, draw: true, mend: false, kill: PlayerMutations.ResonantDrawKillManna, heal: 0);
        AssertPass(2, draw: true, mend: false, kill: PlayerMutations.ResonantDrawKillManna, heal: 0);
        AssertPass(3, draw: true, mend: false, kill: PlayerMutations.ResonantDrawKillManna, heal: 0);
        AssertPass(4, draw: true, mend: true, kill: PlayerMutations.ResonantDrawKillManna, heal: PlayerMutations.DeepMendHeal);
        AssertPass(5, draw: true, mend: true, kill: PlayerMutations.ResonantDrawKillManna, heal: PlayerMutations.DeepMendHeal);
        AssertPass(6, draw: true, mend: true, kill: PlayerMutations.ResonantDrawKillManna, heal: PlayerMutations.DeepMendHeal);
        // Widths above the live cap keep both bands (monotone authority; the
        // census CUT says no LIVE content ever reaches them, not that the
        // pure fn would misbehave if one were hand-built).
        AssertPass(7, draw: true, mend: true, kill: PlayerMutations.ResonantDrawKillManna, heal: PlayerMutations.DeepMendHeal);

        // Null/empty authority: nothing unlocked (same shape as Unlocked).
        var none = PlayerMutations.Passives(null);
        Assert.Equal(new SkillPassives(false, false), none);
        Assert.Equal(0, none.KillMannaBonus);
        Assert.Equal(0, none.MendHealBonus);
    }

    private static void AssertPass(int width, bool draw, bool mend, int kill, int heal)
    {
        var p = PlayerMutations.Passives(Width(width));
        Assert.Equal(draw, p.ResonantDraw);
        Assert.Equal(mend, p.DeepMend);
        Assert.Equal(kill, p.KillMannaBonus);
        Assert.Equal(heal, p.MendHealBonus);
    }

    [Fact]
    public void F13_band_threshold_boundaries_exact()
    {
        // Band-threshold-drift trap: each band flips AT its const and NOT
        // one position later/earlier.
        Assert.False(PlayerMutations.Passives(Width(PlayerMutations.ResonantDrawPositions - 1)).ResonantDraw);
        Assert.True(PlayerMutations.Passives(Width(PlayerMutations.ResonantDrawPositions)).ResonantDraw);
        Assert.False(PlayerMutations.Passives(Width(PlayerMutations.DeepMendPositions - 1)).DeepMend);
        Assert.True(PlayerMutations.Passives(Width(PlayerMutations.DeepMendPositions)).DeepMend);
        // The literal anchors, separately: drift of either const goes RED
        // even if both assertions above are kept in sync by the drifter.
        Assert.Equal(1, PlayerMutations.ResonantDrawPositions);
        Assert.Equal(4, PlayerMutations.DeepMendPositions);
    }

    [Fact]
    public void F11_resonant_draw_adds_bonus_at_kill_seam()
    {
        // The shipped add site, end-to-end engine-free: OnKill credits the
        // BASE plus the LIVE band bonus, exactly once, cap-honest.
        var player = new PlayerController(new CombatVec3(0, 0, 0));
        var skills = new SkillState(player);

        skills.OnKill(0);   // band locked: base only
        Assert.Equal(SkillState.KillMannaGain, player.Manna);

        var band = PlayerMutations.Passives(Width(PlayerMutations.ResonantDrawPositions));
        skills.OnKill(band.KillMannaBonus);   // band reached: base + bonus
        Assert.Equal(2 * SkillState.KillMannaGain + PlayerMutations.ResonantDrawKillManna, player.Manna);

        // Spend-site mutation trap: the cap still clamps the BONUSED gain.
        player.Manna = PlayerController.MannaCap - 1;   // one below cap
        skills.OnKill(band.KillMannaBonus);             // +7 wants 6 over
        Assert.Equal(PlayerController.MannaCap, player.Manna);

        // A negative "bonus" is a caller bug: the whole gain is refused
        // (refuse-spend-zero discipline at the ADD site).
        player.Manna = 50;
        skills.OnKill(-1);
        Assert.Equal(50, player.Manna);
    }

    [Fact]
    public void F12_deep_mend_adds_bonus_at_heal_seam()
    {
        var player = new PlayerController(new CombatVec3(0, 0, 0));
        var skills = new SkillState(player);
        player.Manna = 3 * SkillState.MendCost;
        var band = PlayerMutations.Passives(Width(PlayerMutations.DeepMendPositions));

        // The MaxHealth clamp owns the ceiling, bonus included (90 + 25 + 10
        // wants to overflow; the controller's setter decides, not the seam).
        player.TakeDamage(10);   // HP 90
        Assert.True(skills.TryMend(unlocked: true, band.MendHealBonus));
        Assert.Equal(player.MaxHealth, player.Health);
        Assert.Equal(2 * SkillState.MendCost, player.Manna);   // paid exactly once

        // The SEAM distinguishes the bonus where it is observable: from
        // HP 20, a locked-band read heals 45 (base only), a Deep Mend read
        // heals 55 (base + bonus) — different finals, same cost, same seam.
        player.TakeDamage(80);   // HP 90 -> clamped at max, so: 100-80 -> 20
        Assert.Equal(20, player.Health);
        Assert.True(skills.TryMend(unlocked: true, PlayerMutations.Passives(Width(0)).MendHealBonus));
        Assert.Equal(20 + SkillState.MendHeal, player.Health);   // locked read: base only
        Assert.Equal(SkillState.MendCost, player.Manna);   // second payment

        player.TakeDamage(player.Health - 20);   // back to exactly 20
        Assert.Equal(20, player.Health);
        Assert.True(skills.TryMend(unlocked: true, band.MendHealBonus));
        Assert.Equal(20 + SkillState.MendHeal + PlayerMutations.DeepMendHeal, player.Health);
        Assert.Equal(0, player.Manna);   // three payments, no more

        // Locked SKILL rejects without spending; a negative bonus refuses
        // and spends zero (caller-bug discipline).
        Assert.False(skills.TryMend(unlocked: false, band.MendHealBonus));
        Assert.False(skills.TryMend(unlocked: true, -1));
        Assert.Equal(0, player.Manna);
    }

    [Fact]
    public void F14_live_extraction_reaches_both_bands_census_confirmed()
    {
        // Census re-derivation THROUGH THE REAL PATHS (engine-free): a real
        // kill extraction is built by DnaLanguage.SignatureForEntity and
        // modelled by EcosystemAdaptation.ModelPlayerDna. ONE such
        // extraction must reach BOTH census-confirmed band thresholds —
        // an invented-unreachability regression has a named RED here.
        var extraction = DnaLanguage.SignatureForEntity(entityId: 7);
        var profile = EcosystemAdaptation.ModelPlayerDna(new[] { extraction });
        Assert.Equal(6, profile.Counters.Length);   // the live cap, census row 2
        var p = PlayerMutations.Passives(profile);
        Assert.True(p.ResonantDraw);
        Assert.True(p.DeepMend);
    }

    [Fact]
    public void N3_passives_load_shape_equals_live_at_same_width()
    {
        // Round-trip stability rides the SAME structural argument as the
        // N2 unlock guard: a load re-synthesizes one signature reproducing
        // the persisted consensus (SaveLoadController.RestoreDna), so the
        // load SHAPE at width w and the live profile at width w derive the
        // SAME passives. Value equality asserts directly.
        var loadShape = new CounterProfile(new[] { 0, 3, 1, 2, 0, 1 }, new[] { 1, 1, 1, 1, 1, 1 }, 1);
        var live = new CounterProfile(new[] { 1, 1, 2, 3, 0, 2 }, new[] { 4, 4, 3, 2, 1, 1 }, 4);
        Assert.Equal(PlayerMutations.Passives(loadShape), PlayerMutations.Passives(live));
    }
}
