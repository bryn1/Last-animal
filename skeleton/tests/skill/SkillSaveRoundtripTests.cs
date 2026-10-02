using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
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
// engine-side private helpers VERBATIM (BuildLearnedCounters and
// RestoreDna in world/SaveLoadController.cs — pinned by name, signature
// and body shape by the guard test at the bottom of this file, never by
// line numbers): the save path stores the per-position mode of the
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

    // ---- N2 MIRROR PIN (DA-c3 P3-2) ---------------------------------------
    // SaveLoadController is engine-side and deliberately NOT compiled into
    // this assembly (see the csproj), so type reflection cannot reach it —
    // the pin reads the production SOURCE through the RepoFile idiom the
    // 2d cross-check demonstrates (tests/story/QuestArcCrossCheckTests.cs).
    // The guard test below fails loud on any drift: a rename or signature
    // change of either private, or any change to the algorithm the mirrors
    // reproduce, turns it RED naming both files.

    /// <summary>Locate a repo file relative to the skeleton dir by walking up
    /// from the test assembly output (QuestBusContractTests.RepoFile idiom —
    /// robust for any bin/Depth and any checkout location).</summary>
    private static string RepoFile(string relPath)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, relPath);
            if (File.Exists(candidate)) return File.ReadAllText(candidate);
            dir = dir.Parent;
        }
        throw new FileNotFoundException($"source not found from {AppContext.BaseDirectory}: {relPath}");
    }

    /// <summary>The braced body of the first method matching declaration
    /// (brace-matched substring). Fails loudly if the declaration shape
    /// changed — that IS a drift event, name it, never read dark.</summary>
    private static string MethodBody(string source, string sourceName, string declaration)
    {
        var m = Regex.Match(source, declaration);
        Assert.True(m.Success,
            $"{sourceName}: no method matches '{declaration}' — the mirrored private was " +
            "renamed or reshaped; update THIS mirror and its guard together (DA-c3 P3-2)");
        int open = source.IndexOf('{', m.Index + m.Length);
        Assert.True(open >= 0, $"{sourceName}: declaration '{declaration}' has no body?");
        int depth = 0, i = open;
        for (; i < source.Length; i++)
        {
            if (source[i] == '{') depth++;
            else if (source[i] == '}') { depth--; if (depth == 0) break; }
        }
        Assert.True(depth == 0, $"{sourceName}: unbalanced braces after '{declaration}'");
        return source.Substring(open, i - open + 1);
    }

    private static string Normalize(string body) => Regex.Replace(body, @"\s+", " ").Trim();

    [Fact]
    public void N2_mirror_pinned_to_SaveLoadController_privates()
    {
        var prod = RepoFile("world/SaveLoadController.cs");
        var mirror = RepoFile("tests/skill/SkillSaveRoundtripTests.cs");

        // Leg 1 — the mirrored privates still exist with the pinned names
        // and signatures (rename / reshape = RED).
        const string ProdBuild = @"private\s+List<int>\s+BuildLearnedCounters\s*\(\s*\)";
        const string ProdRestore = @"private\s+void\s+RestoreDna\s*\(\s*List<int>\s+counters\s*\)";
        Assert.Matches(ProdBuild, prod);
        Assert.Matches(ProdRestore, prod);

        // Leg 2 — BuildLearnedCounters: the per-position most-common-
        // nucleotide algorithm is byte-equal between production and the
        // mirror, modulo the one spelling seam (the _spokenDna field vs the
        // spoken parameter). Any algorithm drift — tie-break, tally width,
        // length rule — lands here.
        var prodBuildBody = MethodBody(prod, "world/SaveLoadController.cs", ProdBuild);
        var mirrorBuildBody = MethodBody(mirror, "tests/skill/SkillSaveRoundtripTests.cs",
            @"private\s+static\s+List<int>\s+BuildLearnedCounters\s*\(");
        Assert.Equal(
            Normalize(mirrorBuildBody),
            Normalize(prodBuildBody.Replace("_spokenDna", "spoken")));

        // Leg 3 — RestoreDna: production mutates the field, the mirror
        // returns a fresh list, so compare the SYNTHESIS ESSENCE: collapse
        // the documented spelling seams (field -> rebuilt list; the field's
        // Clear and the mirror's declare/return belong to the seam, not the
        // rule) and require the remaining bodies to match — the ONE-
        // signature-of-counters re-synthesis can no longer drift silently.
        var prodRestoreBody = MethodBody(prod, "world/SaveLoadController.cs", ProdRestore);
        var mirrorRestoreBody = MethodBody(mirror, "tests/skill/SkillSaveRoundtripTests.cs",
            @"private\s+static\s+List<LanguageSignature>\s+RestoreDna\s*\(");
        var prodEssence = Normalize(prodRestoreBody
            .Replace("_spokenDna", "rebuilt")
            .Replace("rebuilt.Clear();", ""));
        var mirrorEssence = Normalize(mirrorRestoreBody
            .Replace("var rebuilt = new List<LanguageSignature>();", "")
            .Replace("return rebuilt;", ""));
        Assert.Equal(
            "if (counters.Count > 0) rebuilt.Add(new LanguageSignature(counters.ToArray()));",
            prodEssence.Replace("{", "").Replace("}", "").Trim());
        Assert.Equal(mirrorEssence.Replace("{", "").Replace("}", "").Trim(),
            prodEssence.Replace("{", "").Replace("}", "").Trim());
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
