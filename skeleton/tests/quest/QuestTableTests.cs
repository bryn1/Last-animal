using System;
using Xunit;
using LastAnimal.Story;

// Last Animal — quest table tests (MC 3904 stage 2c, code, 2026-10-02).
//
// The placeholder arc contract (plan §B D5): the DEFAULT table authors the
// 5-quest arc (owner ruling D6: ends at a zone boss) under the STABLE ids
// the runtime arc proof drives — q_intro, q_speak, q_wage, q_kills, q_boss.
// Stage 2d swaps content rows under the SAME ids, so the id sequence, the
// per-row objective kinds and the table integrity rules (ordinal-unique ids,
// non-empty title/reward) are the tested contract. Removing or renaming a
// row turns this gate RED and names the row.
namespace LastAnimal.Tests.Quest;

public class QuestTableTests
{
    private static QuestTable Default() => QuestTable.Default();

    [Fact]
    public void Default_AuthorsTheFiveQuestArcUnderTheContractIds()
    {
        var table = Default();
        Assert.Equal(5, table.Count);
        string[] ids = { "q_intro", "q_speak", "q_wage", "q_kills", "q_boss" };
        for (int i = 0; i < ids.Length; i++)
        {
            Assert.True(table.TryGetEntry(ids[i], out var row),
                        $"arc row '{ids[i]}' is missing from QuestTable.Default() (2c/2d id contract)");
            Assert.Equal(i, table.Entries[i].Id == ids[i] ? i : -1);
            Assert.False(string.IsNullOrEmpty(row!.Title));
            Assert.False(string.IsNullOrEmpty(row.Reward));
        }
    }

    [Fact]
    public void Default_ArcEndsAtTheZoneBossFinale()
    {
        // Owner ruling D6: the arc ends at a zone boss; the finale row's
        // objective must be the BossDead kind.
        var table = Default();
        var finale = table.Entries[table.Count - 1];
        Assert.Equal("q_boss", finale.Id);
        Assert.Equal(QuestObjectiveKind.BossDead, finale.Objective.Kind);
        Assert.Null(table.Next(finale.Id));   // finale chains into nothing
    }

    [Theory]
    [InlineData("q_intro", QuestObjectiveKind.ZoneReached)]
    [InlineData("q_speak", QuestObjectiveKind.Spoken)]
    [InlineData("q_wage", QuestObjectiveKind.WagePaid)]
    [InlineData("q_kills", QuestObjectiveKind.Kills)]
    public void Default_ArcRowObjectiveKinds(string id, QuestObjectiveKind kind)
    {
        Assert.True(Default().TryGetEntry(id, out var row), $"arc row '{id}' missing");
        Assert.Equal(kind, row!.Objective.Kind);
    }

    [Fact]
    public void Next_ChainsTheArcInAuthoringOrder()
    {
        var table = Default();
        Assert.Equal("q_speak", table.Next("q_intro")!.Id);
        Assert.Equal("q_wage", table.Next("q_speak")!.Id);
        Assert.Equal("q_kills", table.Next("q_wage")!.Id);
        Assert.Equal("q_boss", table.Next("q_kills")!.Id);
        Assert.Null(table.Next("q_boss"));
    }

    [Fact]
    public void FindIdByObjectiveKind_ResolvesTheWageQuestForTheSettleSeam()
    {
        // The WagePaid emit seam names its target through this lookup.
        Assert.Equal("q_wage", Default().FindIdByObjectiveKind(QuestObjectiveKind.WagePaid));
        Assert.Null(Default().FindIdByObjectiveKind(QuestObjectiveKind.LoyaltyAtLeast));
    }

    [Fact]
    public void Ctor_RejectsDuplicateIds()
    {
        var dup = new[]
        {
            new QuestDef("q_x", "A", new QuestObjective(QuestObjectiveKind.Spoken), "r"),
            new QuestDef("q_x", "B", new QuestObjective(QuestObjectiveKind.Spoken), "r"),
        };
        var ex = Assert.Throws<ArgumentException>(() => new QuestTable(dup));
        Assert.Contains("duplicate quest id", ex.Message);
    }

    [Theory]
    [InlineData("", "title", "reward")]          // empty id
    [InlineData("q_v", "", "reward")]            // empty title
    [InlineData("q_v", "title", "")]             // empty reward beat
    public void QuestDef_RejectsEmptyFields(string id, string title, string reward)
    {
        Assert.Throws<ArgumentException>(() =>
            new QuestDef(id, title, new QuestObjective(QuestObjectiveKind.Spoken), reward));
    }

    [Fact]
    public void QuestObjective_RejectsAmountBelowOne()
    {
        Assert.Throws<ArgumentException>(() =>
            new QuestObjective(QuestObjectiveKind.Kills, 0));
    }
}
