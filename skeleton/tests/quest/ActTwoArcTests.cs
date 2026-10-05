using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using LastAnimal.Story;

// Last Animal — ACT TWO state-machine tests (MC 10132 Inc-3 S10, code, 2026-10-05).
//
// The FULL walk of QuestTable.RuinsArc() on the SAME pure QuestLog machine
// (RULING-5: shipped objective kinds only, no new state, no new kind):
//   * the four rows chain fact-for-fact (descent -> tongue -> bread -> bones),
//     every completion driven by its OWN observation (the W6 pin), and the
//     Changed beats name the exact met/complete sequence per row;
//   * every illegal transition on an act-two row throws InvalidOperationException
//     naming the shipped guard (planted-bad targets, same discipline as the
//     act-one guard tests — delete a guard and a named test goes RED);
//   * the wage settle is quest-id keyed and single: a settle naming a foreign
//     id is counted, never guessed onto a row, and FindActiveIdByObjectiveKind
//     only answers while the row is Active;
//   * save wire: RuinsArc rides the shipped "id:status" v3 vocabulary and the
//     cross-table restore (one row list for BOTH logs, each log skipping the
//     other's ids) — the persistence contract the WorldDirector.Story.cs save
//     seam depends on.
namespace LastAnimal.Tests.Quest;

public class ActTwoArcTests
{
    private static QuestLog ActTwoLog() => new QuestLog(QuestTable.RuinsArc());

    /// <summary>Drive the arc to (and into) the bread row: descent + tongue complete.</summary>
    private static void DriveToBreadRow(QuestLog log)
    {
        log.Start("q_r_descent");
        log.ObserveZoneEntered("ruins");    // q_r_descent: met -> completed -> tongue Active
        log.ObserveSpoken(2);               // q_r_tongue: met -> completed -> bread Active
    }

    // ------------------------------------------------------------------
    // Full legal walk — each completion rides its own observation
    // ------------------------------------------------------------------

    [Fact]
    public void RuinsArc_WalksFactForFact_ChangedBeatsNameEveryStep()
    {
        var log = ActTwoLog();
        var beats = new List<string>();
        log.Changed += (id, from, to) => beats.Add($"{id}:{from}->{to}");

        log.Start("q_r_descent");
        Assert.Equal(QuestStatus.Active, log.Status("q_r_descent"));

        log.ObserveZoneEntered("ruins");
        Assert.Equal(QuestStatus.Completed, log.Status("q_r_descent"));
        Assert.Equal(QuestStatus.Active, log.Status("q_r_tongue"));
        Assert.Equal("q_r_tongue", log.ActiveQuestId);

        log.ObserveSpoken();                // one of two — not yet met
        Assert.Equal(QuestStatus.Active, log.Status("q_r_tongue"));
        log.ObserveSpoken();                // its OWN observation completes it
        Assert.Equal(QuestStatus.Completed, log.Status("q_r_tongue"));
        Assert.Equal(QuestStatus.Active, log.Status("q_r_bread"));

        log.ObserveWagePaid("q_r_bread");   // the wage rides the id it serves
        Assert.Equal(QuestStatus.Completed, log.Status("q_r_bread"));
        Assert.Equal(QuestStatus.Active, log.Status("q_r_bones"));

        log.ObserveKill(5);                 // one short of Amount 6
        Assert.Equal(QuestStatus.Active, log.Status("q_r_bones"));
        log.ObserveKill();
        Assert.Equal(QuestStatus.Completed, log.Status("q_r_bones"));
        Assert.True(log.IsArcComplete);
        Assert.Null(log.ActiveQuestId);

        Assert.Equal(new[]
        {
            "q_r_descent:NotStarted->Active",
            "q_r_descent:Active->ObjectiveMet", "q_r_descent:ObjectiveMet->Completed",
            "q_r_tongue:NotStarted->Active",
            "q_r_tongue:Active->ObjectiveMet", "q_r_tongue:ObjectiveMet->Completed",
            "q_r_bread:NotStarted->Active",
            "q_r_bread:Active->ObjectiveMet", "q_r_bread:ObjectiveMet->Completed",
            "q_r_bones:NotStarted->Active",
            "q_r_bones:Active->ObjectiveMet", "q_r_bones:ObjectiveMet->Completed",
        }, beats);
    }

    [Fact]
    public void RuinsArc_AuthorsOnlyShippedNonCascadeKinds()
    {
        // RULING-5 face on data: act two authors ZoneReached/Spoken/WagePaid/
        // Kills only — and NOT BossDead: the act opens while the act-one boss
        // corpse can still be the live boss, so a BossDead row would cascade
        // off the shipped provider reading that corpse as a fresh kill.
        var kinds = QuestTable.RuinsArc().Entries
            .Select(q => q.Objective.Kind).Distinct().OrderBy(k => k).ToArray();
        Assert.Equal(4, kinds.Length);
        Assert.DoesNotContain(QuestObjectiveKind.BossDead, kinds);
        Assert.DoesNotContain(QuestObjectiveKind.LoyaltyAtLeast, kinds);
        Assert.DoesNotContain(QuestObjectiveKind.DialogueShown, kinds);
    }

    // ------------------------------------------------------------------
    // Illegal transitions on act-two rows — named guards (planted-bad targets)
    // ------------------------------------------------------------------

    [Fact]
    public void Illegal_CompleteFromActive_ThrowsNamingTheGuard()
    {
        // "Skip a completion gate": Active straight to Completed is illegal —
        // the objective-met step cannot be bypassed.
        var log = ActTwoLog();
        log.Start("q_r_descent");
        var ex = Assert.Throws<InvalidOperationException>(() => log.Complete("q_r_descent"));
        Assert.Contains("requires ObjectiveMet", ex.Message);
    }

    [Fact]
    public void Illegal_StartTwice_AndMetFromNotStarted_Throw()
    {
        var log = ActTwoLog();
        log.Start("q_r_descent");
        Assert.Throws<InvalidOperationException>(() => log.Start("q_r_descent"));  // already Active
        var ex = Assert.Throws<InvalidOperationException>(() => log.MarkObjectiveMet("q_r_bones"));
        Assert.Contains("requires Active", ex.Message);
    }

    [Fact]
    public void Illegal_CompleteAfterCompleted_Throws()
    {
        var log = ActTwoLog();
        log.Start("q_r_descent");
        log.ObserveZoneEntered("ruins");
        var ex = Assert.Throws<InvalidOperationException>(() => log.Complete("q_r_descent"));
        Assert.Contains("requires ObjectiveMet", ex.Message);   // Completed is not ObjectiveMet
    }

    // ------------------------------------------------------------------
    // Wage single-path + attribution (shipped semantics, act-two rows)
    // ------------------------------------------------------------------

    [Fact]
    public void WageSettle_ForeignId_NeverGuessesOntoTheActiveBreadRow()
    {
        var log = ActTwoLog();
        DriveToBreadRow(log);
        Assert.Equal(QuestStatus.Active, log.Status("q_r_bread"));
        log.ObserveWagePaid("q_intro");     // a settle serving the other table's row
        Assert.Equal(QuestStatus.Active, log.Status("q_r_bread"));   // counted, never guessed
        log.ObserveWagePaid("q_r_bread");   // the settle that SERVES this row
        Assert.Equal(QuestStatus.Completed, log.Status("q_r_bread"));
    }

    [Fact]
    public void FindActiveIdByKind_OnlyAnswersWhileTheRowIsActive()
    {
        var table = QuestTable.RuinsArc();
        Assert.Equal("q_r_bread", table.FindIdByObjectiveKind(QuestObjectiveKind.WagePaid));

        var log = ActTwoLog();
        Assert.Null(log.FindActiveIdByObjectiveKind(QuestObjectiveKind.WagePaid));  // unstarted
        DriveToBreadRow(log);
        Assert.Equal("q_r_bread", log.FindActiveIdByObjectiveKind(QuestObjectiveKind.WagePaid));
        log.ObserveWagePaid("q_r_bread");
        // A Completed row collects no settles (DA-verdict P3 on the shipped machine):
        Assert.Null(log.FindActiveIdByObjectiveKind(QuestObjectiveKind.WagePaid));
    }

    // ------------------------------------------------------------------
    // Save wire — RuinsArc rides the shipped v3 "id:status" vocabulary and
    // the cross-table restore the double-log composition seam depends on
    // ------------------------------------------------------------------

    [Fact]
    public void SaveRows_RideShippedWireVocabulary_InArcOrder()
    {
        var log = ActTwoLog();
        Assert.Empty(log.ToSaveRows());                       // nothing started: nothing written
        log.Start("q_r_descent");
        log.ObserveZoneEntered("ruins");
        log.ObserveSpoken();
        var rows = log.ToSaveRows();
        Assert.Equal(new[] { "q_r_descent:completed", "q_r_tongue:active" }, rows);
    }

    [Fact]
    public void CrossTableRestore_OneRowListBothLogs_EachSkipsTheForeignIds()
    {
        // The composition seam feeds the SAME list to both logs; the shipped
        // "unknown id: skipped by design" is what keeps two tables on one
        // wire. Act two restores its rows; act one's ids pass through it
        // untouched — and the act-two ids are inert against the act-one log.
        var shared = new List<string>
        {
            "q_intro:completed", "q_speak:completed", "q_wage:completed",
            "q_kills:completed", "q_boss:completed",
            "q_r_descent:completed", "q_r_tongue:completed", "q_r_bread:active",
        };

        var act2 = ActTwoLog();
        act2.FromSaveRows(shared);
        Assert.Equal(QuestStatus.Completed, act2.Status("q_r_descent"));
        Assert.Equal(QuestStatus.Completed, act2.Status("q_r_tongue"));
        Assert.Equal(QuestStatus.Active, act2.Status("q_r_bread"));
        Assert.Equal(QuestStatus.NotStarted, act2.Status("q_r_bones"));   // un-started rows stay
        Assert.Equal(new[] { "q_r_descent:completed", "q_r_tongue:completed", "q_r_bread:active" },
                     act2.ToSaveRows());

        var act1 = new QuestLog(QuestTable.Default());
        act1.FromSaveRows(shared);
        Assert.True(act1.IsArcComplete);
        Assert.Equal(5, act1.ToSaveRows().Count);   // act-two ids skipped, not poisoned
    }

    [Fact]
    public void Restore_DroppedRow_GoesBackToNotStarted_AndRewindsEvidence()
    {
        // The restore is the FULL snapshot (DA P1 idiom, act-two face): a row
        // missing from the list is genuinely un-started again and its evidence
        // rewinds — a later observation cannot fast-forward the arc.
        var log = ActTwoLog();
        DriveToBreadRow(log);
        log.FromSaveRows(new List<string> { "q_r_descent:completed" });
        Assert.Equal(QuestStatus.Completed, log.Status("q_r_descent"));
        Assert.Equal(QuestStatus.NotStarted, log.Status("q_r_tongue"));
        log.ObserveSpoken();                     // ONE post-restore speech: the pre-restore
        Assert.Equal(QuestStatus.NotStarted, log.Status("q_r_tongue"));  // 2x must not ride along
        log.Start("q_r_tongue");                 // Start re-evaluates: 1 of 2 is NOT met
        Assert.Equal(QuestStatus.Active, log.Status("q_r_tongue"));
        log.ObserveSpoken();                     // the second is genuinely re-earned
        Assert.Equal(QuestStatus.Completed, log.Status("q_r_tongue"));
    }

    // ------------------------------------------------------------------
    // Act card data — the table shape (node REFERENCE integrity is pinned
    // against DialogueTable by tests/story/QuestArcCrossCheckTests)
    // ------------------------------------------------------------------

    [Fact]
    public void ActTwo_CardAuthorsOrderedNonEmptyNodeLists()
    {
        var card = QuestTable.ActTwo;
        Assert.Equal("act_two", card.Id);
        Assert.Equal(new[] { "act2_open_a", "act2_open_b" }, card.OpenNodes);
        Assert.Equal(new[] { "act2_close_a", "act2_close_b" }, card.CloseNodes);
        Assert.Throws<ArgumentException>(() => new ActCard("x", Array.Empty<string>(), new[] { "a" }));
        Assert.Throws<ArgumentException>(() => new ActCard("x", new[] { "a" }, new[] { "" }));
        Assert.Throws<ArgumentException>(() => new ActCard("", new[] { "a" }, new[] { "b" }));
    }

    // ------------------------------------------------------------------
    // F-DA2 (DA-verdict 0ad8c81f, fix cycle 2): the DOUBLE-LOAD of a
    // migration-shaped save — the stranding state the shipped three-arm
    // restore-sync missed. The leg drives the REAL ActTwoSync machine with
    // the director's engine-side follow-ups: load, load again, NO save
    // between — the chain must come out ALIVE.
    // ------------------------------------------------------------------

    [Fact]
    public void Restore_MigrationShapedSave_DoubleLoad_ChainStaysAlive()
    {
        // Migration shape: a pre-S10 save — act-one ids complete, ZERO
        // act-two rows on the shipped v3 wire vocabulary (same list shape the
        // CrossTableRestore leg feeds).
        var migration = new List<string>
        {
            "q_intro:completed", "q_speak:completed", "q_wage:completed",
            "q_kills:completed", "q_boss:completed",
        };

        var act1 = new QuestLog(QuestTable.Default());
        var act2 = ActTwoLog();
        bool opened = false, closeShown = false;
        var openCardsPlayed = 0;

        void Load()   // QuestRestoreRows + the director's follow-ups (Story.cs partial)
        {
            act1.FromSaveRows(migration);
            act2.FromSaveRows(migration);
            switch (ActTwoSync.Run(act1, act2, opened, ref closeShown))
            {
                case ActTwoSyncAction.Adopt: opened = true; break;   // AdoptActTwoOpen, silent
                case ActTwoSyncAction.Rollback: opened = false; break;
                case ActTwoSyncAction.FreshOpen:                     // OpenActTwo:
                    opened = true; openCardsPlayed++;                //   card + Start
                    act2.Start(act2.Table.Entries[0].Id);
                    break;
                // SilentReStart / None: Run applied every pure effect.
            }
        }

        Load();                                                       // load 1
        Assert.Equal(1, openCardsPlayed);
        Assert.Equal("q_r_descent", act2.ActiveQuestId);

        Load();                                                       // load 2, NO save between
        Assert.Equal(1, openCardsPlayed);    // a load is not a beat: NO second open card
        Assert.False(closeShown);            // the act is live again from row one
        Assert.Equal("q_r_descent", act2.ActiveQuestId);              // chain ALIVE, not stranded

        // …and COMPLETABLE: the same driven walk as the fact-for-fact leg
        // completes every row from the re-Started state.
        act2.ObserveZoneEntered("ruins");
        act2.ObserveSpoken(2);
        act2.ObserveWagePaid("q_r_bread");
        act2.ObserveKill(6);
        Assert.True(act2.IsArcComplete);
    }
}
