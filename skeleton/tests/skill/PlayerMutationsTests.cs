using Xunit;
using LastAnimal.Dna;

// Last Animal — unlock-authority tests (MC 3912 stage 2e, code, 2026-10-02).
//
// Pins the plan §G D2 Option B contract on PlayerMutations.Unlocked: the
// authority is a PURE function of the per-position consensus Counters ONLY.
// The planted-bad trap (task DoD 4): reintroducing profile.ObservedCount
// (or Coverage) into Unlocked turns the ObservedCount-independence test and
// the N2 round-trip equality (SkillSaveRoundtripTests) RED by name.
namespace LastAnimal.Tests.Skill;

public class PlayerMutationsTests
{
    [Fact]
    public void Empty_or_null_profile_unlocks_nothing()
    {
        Assert.False(PlayerMutations.Unlocked(null).InvertStrike);
        Assert.False(PlayerMutations.Unlocked(null).Mend);
        var empty = EcosystemAdaptation.ModelPlayerDna(null);
        Assert.False(PlayerMutations.Unlocked(empty).InvertStrike);
        Assert.False(PlayerMutations.Unlocked(empty).Mend);
    }

    [Fact]
    public void Invert_Strike_unlocks_at_one_learned_position()
    {
        var profile = new CounterProfile(new[] { 1 }, new[] { 1 }, 1);
        var u = PlayerMutations.Unlocked(profile);
        Assert.True(u.InvertStrike);
        Assert.False(u.Mend);   // 1 position < the Mend threshold of 2
    }

    [Fact]
    public void Mend_unlocks_at_two_learned_positions()
    {
        var profile = new CounterProfile(new[] { 1, 2 }, new[] { 1, 1 }, 1);
        var u = PlayerMutations.Unlocked(profile);
        Assert.True(u.InvertStrike);
        Assert.True(u.Mend);
    }

    [Fact]
    public void Unlock_reads_Counters_only_never_ObservedCount()
    {
        // Hand-built profile WITH two learned positions but ObservedCount=0:
        // such a profile is exactly what a load produces shape-wise (the
        // synthetic signature). If the authority read ObservedCount this
        // would lock — the authority must NOT, so neither can it ever
        // re-lock after a save/load. (PLANTED-BAD TARGET 2: reintroducing
        // ObservedCount into PlayerMutations.Unlocked goes RED here.)
        var loadShape = new CounterProfile(new[] { 0, 3, 1, 2 }, new[] { 1, 1, 1, 1 }, 0);
        var u = PlayerMutations.Unlocked(loadShape);
        Assert.True(u.InvertStrike);
        Assert.True(u.Mend);
    }

    [Fact]
    public void Unlock_reads_Counters_only_never_Coverage()
    {
        // Same length of Counters, wildly different Coverage: identical
        // unlocks. Coverage is never consulted.
        var a = new CounterProfile(new[] { 2, 2 }, new[] { 50, 50 }, 50);
        var b = new CounterProfile(new[] { 0, 1 }, new[] { 0, 0 }, 1);
        Assert.Equal(PlayerMutations.Unlocked(a), PlayerMutations.Unlocked(b));
    }

    [Fact]
    public void Real_spoken_history_unlocks_both_after_two_positions_learned()
    {
        // Signatures are 6 positions deep (DnaLanguage.SignatureForEntity):
        // one extraction already learned >=2 positions -> both unlock.
        var sigs = new System.Collections.Generic.List<LanguageSignature>
        {
            DnaLanguage.SignatureForEntity(7),
        };
        var u = PlayerMutations.Unlocked(EcosystemAdaptation.ModelPlayerDna(sigs));
        Assert.True(u.InvertStrike);
        Assert.True(u.Mend);
    }

    [Fact]
    public void Unlock_ids_are_the_stable_SkillUsed_wire_ids()
    {
        var profile = new CounterProfile(new[] { 1, 1 }, new[] { 1, 1 }, 1);
        var ids = PlayerMutations.Unlocked(profile).Ids;
        Assert.Equal(new[] { "invert_strike", "mend" }, ids);
        Assert.True(PlayerMutations.IsUnlocked("invert_strike", profile));
        Assert.False(PlayerMutations.IsUnlocked("calming_speak", profile));   // 2 < CalmingSpeakPositions=3 (MC 10031 rule 3)
        Assert.False(PlayerMutations.IsUnlocked("not_a_real_skill", profile));   // genuinely unknown id → never unlocked
    }

    [Fact]
    public void F9_calm_unlock_positions()
    {
        // The third rule of the frozen authority: >= 3 learned positions
        // unlocks Calming Speak; 2 does NOT (the 2-position row above keeps
        // its outcome — DA P3-b: gained, never flipped). Counters-only: the
        // ObservedCount-independence trap above covers the read discipline.
        var two = new CounterProfile(new[] { 1, 1 }, new[] { 1, 1 }, 1);
        var three = new CounterProfile(new[] { 1, 1, 1 }, new[] { 1, 1, 1 }, 1);
        Assert.False(PlayerMutations.Unlocked(two).CalmingSpeak);
        Assert.True(PlayerMutations.Unlocked(three).CalmingSpeak);
        Assert.True(PlayerMutations.Unlocked(three).InvertStrike);
        Assert.True(PlayerMutations.Unlocked(three).Mend);
        // Authoring order rides the Ids list (the panel/HUD read idiom).
        Assert.Equal(new[] { "invert_strike", "mend", "calming_speak" },
            PlayerMutations.Unlocked(three).Ids);
    }
}
