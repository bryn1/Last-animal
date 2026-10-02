using System;
using System.Collections.Generic;
using Xunit;
using LastAnimal.Combat;
using LastAnimal.Dna;
using LastAnimal.Save;
using LastAnimal.Skills;

// Last Animal — unlock/Manna persistence tests (MC 3912 stage 2e, code,
// 2026-10-02).
//
// N2 guard (plan §G D2/D4, task DoD "unlock(from-consensus-after-roundtrip)
// == unlock(live)"): the unlock set computed from the consensus AFTER a
// save/load round-trip EQUALS the one computed from the live spoken history
// — across N>2 kills. The mirrors below reproduce SaveLoadController's
// engine-side private helpers VERBATIM (BuildLearnedCounters :212-228 and
// RestoreDna :203-208): the save path stores the per-position mode of the
// player's nucleotides; the load path re-synthesizes ONE signature from the
// counters. ObservedCount collapses to 1 there — which is exactly why the
// authority must read Counters only.
//
// PLANTED-BAD TARGET 2: reintroducing ObservedCount into
// PlayerMutations.Unlocked makes the LIVE unlock true but the AFTER-ROUNDTRIP
// unlock false (observed collapses 3 -> 1), turning the N2 equality RED —
// the gate's named trap.
//
// The Manna leg pins owner ruling D3 from the other side: a load restores
// the saved value EXACTLY (no cross-load refill through this wire).
namespace LastAnimal.Tests.Skill;

public class SkillSaveRoundtripTests
{
    /// <summary>Mirror of SaveLoadController.BuildLearnedCounters (the exact
    /// C14 per-position most-common-nucleotide snapshot).</summary>
    private static List<int> BuildLearnedCounters(List<LanguageSignature> spoken)
    {
        var counters = new List<int>();
        if (spoken.Count == 0) return counters;
        int len = 0;
        foreach (var s in spoken) len = Math.Max(len, s.Nucleotides.Length);
        for (int i = 0; i < len; i++)
        {
            var tally = new int[4];
            foreach (var s in spoken)
                if (i < s.Nucleotides.Length) tally[s.Nucleotides[i]]++;
            int best = 0;
            for (int n = 1; n < 4; n++) if (tally[n] > tally[best]) best = n;
            counters.Add(best);
        }
        return counters;
    }

    /// <summary>Mirror of SaveLoadController.RestoreDna (ONE synthetic
    /// signature whose nucleotides ARE the counters).</summary>
    private static List<LanguageSignature> RestoreDna(List<int> counters)
    {
        var rebuilt = new List<LanguageSignature>();
        if (counters.Count > 0)
            rebuilt.Add(new LanguageSignature(counters.ToArray()));
        return rebuilt;
    }

    [Fact]
    public void N2_unlock_from_consensus_after_roundtrip_equals_unlock_live()
    {
        // N > 2 kills (task DoD): three real extractions.
        var spoken = new List<LanguageSignature>
        {
            DnaLanguage.SignatureForEntity(11),
            DnaLanguage.SignatureForEntity(23),
            DnaLanguage.SignatureForEntity(37),
        };
        var live = PlayerMutations.Unlocked(EcosystemAdaptation.ModelPlayerDna(spoken));
        Assert.True(live.InvertStrike);
        Assert.True(live.Mend);

        // Save -> load through the REAL v3 schema (SaveSystem guard included).
        var store = new TempDirSaveStore();
        var state = new GameState
        {
            LearnedDnaCounters = BuildLearnedCounters(spoken),
            Manna = 27,
        };
        Assert.True(SaveSystem.Save(state, store));
        var loaded = SaveSystem.Load(store);
        Assert.NotNull(loaded);

        var afterRoundtrip = PlayerMutations.Unlocked(
            EcosystemAdaptation.ModelPlayerDna(RestoreDna(loaded!.LearnedDnaCounters)));

        // THE equality (plan §B 2e F-act 6 / §G N2): survives EXACTLY.
        Assert.Equal(live, afterRoundtrip);
        Assert.Equal(live.Ids, afterRoundtrip.Ids);
    }

    [Fact]
    public void Load_restores_saved_Manna_exactly_no_cross_load_refill()
    {
        var store = new TempDirSaveStore();
        Assert.True(SaveSystem.Save(new GameState { Manna = 73 }, store));
        var loaded = SaveSystem.Load(store);
        Assert.NotNull(loaded);
        Assert.Equal(73, loaded!.Manna);   // verbatim — the restore never refills

        // The clamp on the WRITE side (controller storage): a cap-100 value
        // round-trips at 100, a zero stays zero.
        var store2 = new TempDirSaveStore();
        Assert.True(SaveSystem.Save(new GameState { Manna = 100 }, store2));
        var loaded2 = SaveSystem.Load(store2);
        Assert.Equal(100, loaded2!.Manna);
        var player = new PlayerController(new CombatVec3(0f, 0f, 0f)) { Manna = loaded2.Manna };
        Assert.Equal(100, player.Manna);
    }

    [Fact]
    public void Skill_state_reflects_the_restored_Manna_after_a_live_load_shape()
    {
        // The engine-side seam is `_saveLoad.MannaRestore = m => _player.Manna = m`
        // (WorldDirector.Skills InitSkills). Headless reproduction: the saved
        // value lands on the controller and SkillState reads it live.
        var player = new PlayerController(new CombatVec3(0f, 0f, 0f));
        var skills = new SkillState(player);
        skills.OnKill();                      // 5 live
        skills.OnKill();                      // 10 live
        var store = new TempDirSaveStore();
        Assert.True(SaveSystem.Save(new GameState { Manna = player.Manna }, store));

        // Fresh session, fresh controller — load restores the saved value.
        var player2 = new PlayerController(new CombatVec3(0f, 0f, 0f));
        var loaded = SaveSystem.Load(store);
        Assert.NotNull(loaded);
        player2.Manna = loaded!.Manna;        // the exact MannaRestore hunk
        var skills2 = new SkillState(player2);
        Assert.Equal(10, skills2.Manna);
        Assert.True(skills2.TryInvertStrike(unlocked: true));   // spendable across the load
        Assert.Equal(0, skills2.Manna);
    }
}
