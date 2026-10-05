using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;
using LastAnimal.Story;

// Last Animal — quest/dialogue content cross-check (MC 3911 stage 2d, code,
// 2026-10-02). The DA P2 hole (MC 3904 cycle 1): the 2c/2d id contract covers
// quest ids only — a DialogueShown objective arg, a ZoneReached arg, or a
// reward-beat node reference pointing at a renamed/removed node makes that
// beat permanently unsatisfiable with every other gate green. This file is
// that missing cross-validation: static, engine-free, it joins the two
// authored tables (and the spawner's zone set) into one referential-
// integrity check over QuestTable.Default() + DialogueTable.Default().
// S10 (MC 10132) widened every leg to BOTH authored arcs (QuestTable.RuinsArc)
// and added leg 5: the act-card node references (QuestTable.ActTwo) resolve to
// authored DialogueTable nodes too — a renamed act card fails the gate too.
//
// The zone set is READ from src/ecosystem/EcosystemSpawner.cs (the source of
// truth for canonical zone ids) through the QuestBusContractTests source-read
// idiom — it is never re-invented here: rename a zone there and forget a
// QuestTable arg, and this gate goes RED naming the quest.
namespace LastAnimal.Tests.Story;

public class QuestArcCrossCheckTests
{
    private static DialogueTable Nodes() => DialogueTable.Default();

    /// <summary>BOTH authored tables (MC 10132 S10): the shipped act-one
    /// Default() arc and the act-two RuinsArc() — every reference leg runs
    /// over both, so a dangling reward/zone/card id on EITHER arc fails the
    /// story gate naming the quest (DA P2 discipline, widened with the arc).</summary>
    private static QuestTable[] Arcs() => new[] { QuestTable.Default(), QuestTable.RuinsArc() };

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

    /// <summary>Canonical zone ids, parsed off the spawner's ZoneIds line
    /// (EcosystemSpawner.cs). Fails loudly if the shape ever changes.</summary>
    private static List<string> SpawnerZoneIds()
    {
        var src = RepoFile("src/ecosystem/EcosystemSpawner.cs");
        var m = Regex.Match(src, @"ZoneIds\s*=\s*\{([^}]*)\}");
        Assert.True(m.Success,
            "could not locate the ZoneIds literal in src/ecosystem/EcosystemSpawner.cs — " +
            "update this cross-check together with the spawner (never let the zone set go dark)");
        var ids = new List<string>();
        foreach (Match q in Regex.Matches(m.Groups[1].Value, "\"([^\"]+)\""))
            ids.Add(q.Groups[1].Value);
        Assert.NotEmpty(ids);
        return ids;
    }

    // ------------------------------------------------------------------
    // Leg 1 — objective args of kind DialogueShown resolve to authored nodes
    // (the DA P2 leg: a dangling node arg = permanently unsatisfiable quest)
    // ------------------------------------------------------------------

    [Fact]
    public void DialogueShownArgs_NameAuthoredDialogueNodes()
    {
        var nodes = Nodes();
        foreach (var arc in Arcs())
        foreach (var q in arc.Entries)
        {
            if (q.Objective.Kind != QuestObjectiveKind.DialogueShown) continue;
            Assert.True(nodes.Has(q.Objective.Arg),
                $"quest '{q.Id}': DialogueShown arg '{q.Objective.Arg}' is not a node of " +
                "DialogueTable.Default() — the objective can never be satisfied");
        }
    }

    // ------------------------------------------------------------------
    // Leg 2 — objective args of kind ZoneReached name a real zone, read from
    // the spawner source (asserts non-vacuously: the authored arc must carry
    // at least one ZoneReached row for the intro beat)
    // ------------------------------------------------------------------

    [Fact]
    public void ZoneReachedArgs_NameRealZones_FromTheSpawnerSource()
    {
        var zones = SpawnerZoneIds();
        int zoneRows = 0;
        foreach (var arc in Arcs())
        foreach (var q in arc.Entries)
        {
            if (q.Objective.Kind != QuestObjectiveKind.ZoneReached) continue;
            zoneRows++;
            Assert.True(zones.Contains(q.Objective.Arg),
                $"quest '{q.Id}': ZoneReached arg '{q.Objective.Arg}' names no zone of " +
                "EcosystemSpawner.ZoneIds — the objective can never be satisfied");
        }
        Assert.True(zoneRows >= 2, "the authored arcs carry fewer than two ZoneReached rows " +
            "(act-one intro + act-two descent are both required)");
    }

    // ------------------------------------------------------------------
    // Leg 3 — reward beats are dialogue-node references (plan 2d: rewards are
    // dialogue beats, no loot system). Non-vacuous by construction: every
    // row's reward is enforced non-empty by QuestDef — this leg therefore
    // validates a reference for EVERY row, and renaming a node in
    // DialogueTable fails it naming the quest id AND the node id.
    // ------------------------------------------------------------------

    [Fact]
    public void RewardBeats_NameAuthoredDialogueNodes()
    {
        var nodes = Nodes();
        foreach (var arc in Arcs())
        foreach (var q in arc.Entries)
            Assert.True(nodes.Has(q.Reward),
                $"quest '{q.Id}': reward beat names node '{q.Reward}', which " +
                "DialogueTable.Default() does not author — the reward can never play");
    }

    // ------------------------------------------------------------------
    // Leg 4 — arg-column integrity: only the kinds QuestLog consults
    // (DialogueShown, ZoneReached) may carry an Arg; a stray arg on another
    // kind is dead, misleading data and a rename-dangling vector.
    // ------------------------------------------------------------------

    [Fact]
    public void KindsThatConsumeNoArg_KeepItEmpty()
    {
        foreach (var arc in Arcs())
        foreach (var q in arc.Entries)
        {
            var kind = q.Objective.Kind;
            if (kind == QuestObjectiveKind.DialogueShown || kind == QuestObjectiveKind.ZoneReached)
                Assert.NotEmpty(q.Objective.Arg);   // the converse: consuming kinds carry it
            else
                Assert.True(string.IsNullOrEmpty(q.Objective.Arg),
                    $"quest '{q.Id}': kind '{kind}' consumes no arg, yet carries '{q.Objective.Arg}' " +
                    "— dead data that no satisfaction path reads");
        }
    }

    // ------------------------------------------------------------------
    // Leg 5 (MC 10132 S10) — the act-two open/close cards are DIALOGUE
    // references like the rewards: every id QuestTable.ActTwo names must be
    // authored by DialogueTable.Default(), or that act beat plays the fallback
    // line and the "table-driven act card" (RULING-5) silently degrades. Same
    // rename-dangling vector the reward leg closes, now closed for the cards.
    // ------------------------------------------------------------------

    [Fact]
    public void ActCardNodes_NameAuthoredDialogueNodes()
    {
        var nodes = Nodes();
        foreach (var node in QuestTable.ActTwo.OpenNodes)
            Assert.True(nodes.Has(node),
                $"act card '{QuestTable.ActTwo.Id}' open beat names node '{node}', which " +
                "DialogueTable.Default() does not author — the open card cannot play its text");
        foreach (var node in QuestTable.ActTwo.CloseNodes)
            Assert.True(nodes.Has(node),
                $"act card '{QuestTable.ActTwo.Id}' close beat names node '{node}', which " +
                "DialogueTable.Default() does not author — the close card cannot play its text");
    }
}
