using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;

// Last Animal — bus batch contract tests (MC 3904 stage 2c, code, 2026-10-02).
//
// The plan §B bus row is explicit: EventBus.cs + FrameworkTypes.cs get THE
// ONE batched 2c edit — EXACTLY five new signals (QuestStarted,
// QuestObjective, QuestCompleted, WagePaid, SkillUsed), all string-Id per
// GD0202, plus the QuestId + SkillId carriers. 2e/2f/2g consume, never edit.
// EventBus is a Godot type and cannot compile into this pure project, so the
// leg reads the SOURCE directly: the bus must carry exactly eleven [Signal]
// delegates — six pre-2c + the five-row batch — and the new emit helpers
// must take the carriers. Plant a SIXTH signal on the bus and the count leg
// goes RED naming the signal.
namespace LastAnimal.Tests.Quest;

public class QuestBusContractTests
{
    private static readonly string[] C2Signals =
    {
        "DnaExtracted", "DnaSpoken", "LoyaltyChanged", "Betrayal",
        "EcosystemAdapted", "EmpathyBookOpened",
    };

    private static readonly string[] QuestBatchSignals =
    {
        "QuestStarted", "QuestObjective", "QuestCompleted", "WagePaid", "SkillUsed",
    };

    private static string RepoFile(string relPath)
    {
        // Locate the skeleton dir (the csproj's own dir) from the test
        // assembly's output path — walk up until the file exists. Robust for
        // any bin/Depth and any checkout location.
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, relPath);
            if (File.Exists(candidate)) return File.ReadAllText(candidate);
            dir = dir.Parent;
        }
        throw new FileNotFoundException($"bus source not found from {AppContext.BaseDirectory}: {relPath}");
    }

    private static List<string> BusSignalNames(string source)
    {
        var names = new List<string>();
        foreach (Match m in Regex.Matches(source, @"\[Signal\] public delegate void (\w+)EventHandler\("))
            names.Add(m.Groups[1].Value);
        return names;
    }

    [Fact]
    public void EventBus_CarriesExactlyFiveNewQuestSignals_NoSixth()
    {
        var names = BusSignalNames(RepoFile("autoload/EventBus.cs"));
        Assert.Equal(11, names.Count);   // 6 pre-2c + the 5-row batch. A sixth
                                         // batched signal trips exactly this.
        foreach (var s in C2Signals)
            Assert.Contains(s, names);
        foreach (var s in QuestBatchSignals)
            Assert.Contains(s, names);
    }

    [Fact]
    public void EventBus_AllQuestSignalsAreStringIdPerGd0202()
    {
        var source = RepoFile("autoload/EventBus.cs");
        foreach (var s in QuestBatchSignals)
        {
            var m = Regex.Match(source,
                @"\[Signal\] public delegate void " + s + @"EventHandler\(([^)]*)\)");
            Assert.True(m.Success, $"signal '{s}' missing its delegate declaration");
            var args = m.Groups[1].Value.Trim();
            // string-Id wire form: zero args, or ONLY string parameters.
            if (args.Length > 0)
                Assert.All(args.Split(','), a => Assert.StartsWith("string", a.Trim()));
        }
    }

    [Fact]
    public void EventBus_QuestBatchEmitHelpersTakeTheCarriers()
    {
        var source = RepoFile("autoload/EventBus.cs");
        foreach (var (emit, carrier) in new[]
        {
            ("EmitQuestStarted", "QuestId"), ("EmitQuestObjective", "QuestId"),
            ("EmitQuestCompleted", "QuestId"), ("EmitWagePaid", "QuestId"),
            ("EmitSkillUsed", "SkillId"),
        })
            Assert.Contains($"public void {emit}({carrier} ", source);
    }

    [Fact]
    public void FrameworkTypes_AuthorsTheQuestIdAndSkillIdCarriers()
    {
        var source = RepoFile("autoload/FrameworkTypes.cs");
        Assert.Contains("public partial class QuestId : Resource", source);
        Assert.Contains("public partial class SkillId : Resource", source);
    }
}
