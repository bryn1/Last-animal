using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using LastAnimal.Story;

// Last Animal — ACT-THREE state-machine tests (MC 10273 Inc-4 S19, code,
// 2026-10-07). RULING-8 ("yes to zone4+act3") mirror of ActTwoArcTests for
// QuestTable.ActThreeArc(): the four zone-4 ("hollow", S18-shipped ground)
// rows on the SAME pure QuestLog machine — shipped objective kinds only, no
// new state, no new kind:
//   * the four rows chain fact-for-fact (arrival -> tongue -> wage ->
//     reckoning), every completion driven by its OWN observation (the W6
//     pin), and the Changed beats name the exact met/complete sequence;
//   * the kind set repeats the RuinsArc no-cascade ruling (no BossDead /
//     LoyaltyAtLeast / DialogueShown — act three opens on the act-two
//     finale, a BossDead row would ride the still-read corpse);
//   * the THREE-LOG cross-table restore (act1 + act2 + act3 rows on the ONE
//     shipped v3 "id:status" list, each log skipping the foreign ids) — the
//     persistence contract the extended QuestSaveRows/QuestRestoreRows seam
//     depends on;
//   * act three rides the ActChainSync machine over (act2, act3): the
//     FreshOpen edge (act two complete, act three un-started) and the
//     F-DA2 double-load leg re-run at the act-three seam — the act-completion
//     condition (IsArcComplete of ActThreeArc) must be reachable after the
//     double load, not stranded.
namespace LastAnimal.Tests.Quest;

public class ActThreeArcTests
{
    private static QuestLog ActThreeLog() => new QuestLog(QuestTable.ActThreeArc());

    /// <summary>Drive the arc to (and into) the wage row: arrival + tongue complete.</summary>
    private static void DriveToWageRow(QuestLog log)
    {
        log.Start("q_h_arrival");
        log.ObserveZoneEntered("hollow");   // q_h_arrival: met -> completed -> tongue Active
        log.ObserveSpoken(3);               // q_h_tongue: met -> completed -> wage Active
    }

    // ------------------------------------------------------------------
    // Full legal walk — each completion rides its own observation
    // ------------------------------------------------------------------

    [Fact]
    public void ActThreeArc_WalksFactForFact_ChangedBeatsNameEveryStep()
    {
        var log = ActThreeLog();
        var beats = new List<string>();
        log.Changed += (id, from, to) => beats.Add($"{id}:{from}->{to}");

        log.Start("q_h_arrival");
        Assert.Equal(QuestStatus.Active, log.Status("q_h_arrival"));

        log.ObserveZoneEntered("hollow");
        Assert.Equal(QuestStatus.Completed, log.Status("q_h_arrival"));
        Assert.Equal(QuestStatus.Active, log.Status("q_h_tongue"));
        Assert.Equal("q_h_tongue", log.ActiveQuestId);

        log.ObserveSpoken(2);               // one short of Amount 3 — not yet met
        Assert.Equal(QuestStatus.Active, log.Status("q_h_tongue"));
        log.ObserveSpoken();                // its OWN observation completes it
        Assert.Equal(QuestStatus.Completed, log.Status("q_h_tongue"));
        Assert.Equal(QuestStatus.Active, log.Status("q_h_wage"));

        log.ObserveWagePaid("q_h_wage");    // the wage rides the id it serves
        Assert.Equal(QuestStatus.Completed, log.Status("q_h_wage"));
        Assert.Equal(QuestStatus.Active, log.Status("q_h_reckoning"));

        log.ObserveKill(7);                 // one short of Amount 8
        Assert.Equal(QuestStatus.Active, log.Status("q_h_reckoning"));
        log.ObserveKill();
        Assert.Equal(QuestStatus.Completed, log.Status("q_h_reckoning"));
        Assert.True(log.IsArcComplete);     // the ACT-COMPLETION condition
        Assert.Null(log.ActiveQuestId);

        Assert.Equal(new[]
        {
            "q_h_arrival:NotStarted->Active",
            "q_h_arrival:Active->ObjectiveMet", "q_h_arrival:ObjectiveMet->Completed",
            "q_h_tongue:NotStarted->Active",
            "q_h_tongue:Active->ObjectiveMet", "q_h_tongue:ObjectiveMet->Completed",
            "q_h_wage:NotStarted->Active",
            "q_h_wage:Active->ObjectiveMet", "q_h_wage:ObjectiveMet->Completed",
            "q_h_reckoning:NotStarted->Active",
            "q_h_reckoning:Active->ObjectiveMet", "q_h_reckoning:ObjectiveMet->Completed",
        }, beats);
    }

    [Fact]
    public void ActThreeArc_AuthorsOnlyShippedNonCascadeKinds()
    {
        // RULING-8 face on data: act three authors ZoneReached/Spoken/WagePaid/
        // Kills only — and NOT BossDead/LoyaltyAtLeast/DialogueShown, the same
        // no-cascade ruling RuinsArc carries: the act opens on the act-two
        // finale, so a BossDead row would ride a corpse the shipped provider
        // can still read as a fresh kill.
        var kinds = QuestTable.ActThreeArc().Entries
            .Select(q => q.Objective.Kind).Distinct().OrderBy(k => k).ToArray();
        Assert.Equal(4, kinds.Length);
        Assert.DoesNotContain(QuestObjectiveKind.BossDead, kinds);
        Assert.DoesNotContain(QuestObjectiveKind.LoyaltyAtLeast, kinds);
        Assert.DoesNotContain(QuestObjectiveKind.DialogueShown, kinds);
    }

    [Fact]
    public void ActThree_FinalChainNamesTheS18ShippedZone()
    {
        // The zone-4 hook of the brief: the act-three chain OPENS on the
        // zone-4 arrival — the ZoneReached arg names the S18-shipped "hollow"
        // (tests/story cross-checks it against the spawner's ZoneIds).
        var arrival = QuestTable.ActThreeArc().Entries[0];
        Assert.Equal(QuestObjectiveKind.ZoneReached, arrival.Objective.Kind);
        Assert.Equal("hollow", arrival.Objective.Arg);
    }

    // ------------------------------------------------------------------
    // Illegal transitions on act-three rows — named guards (planted-bad
    // targets, same discipline as the act-one/act-two guard tests)
    // ------------------------------------------------------------------

    [Fact]
    public void Illegal_CompleteFromActive_AndStartTwice_Throw()
    {
        var log = ActThreeLog();
        log.Start("q_h_arrival");
        var ex = Assert.Throws<InvalidOperationException>(() => log.Complete("q_h_arrival"));
        Assert.Contains("requires ObjectiveMet", ex.Message);
        Assert.Throws<InvalidOperationException>(() => log.Start("q_h_arrival"));  // already Active
        var ex2 = Assert.Throws<InvalidOperationException>(() => log.MarkObjectiveMet("q_h_reckoning"));
        Assert.Contains("requires Active", ex2.Message);
    }

    // ------------------------------------------------------------------
    // Wage single-path + attribution (shipped semantics, act-three rows)
    // ------------------------------------------------------------------

    [Fact]
    public void WageSettle_ForeignId_NeverGuessesOntoTheActiveWageRow()
    {
        var log = ActThreeLog();
        DriveToWageRow(log);
        Assert.Equal(QuestStatus.Active, log.Status("q_h_wage"));
        log.ObserveWagePaid("q_r_bread");   // a settle serving the OTHER act's row
        Assert.Equal(QuestStatus.Active, log.Status("q_h_wage"));   // counted, never guessed
        log.ObserveWagePaid("q_h_wage");    // the settle that SERVES this row
        Assert.Equal(QuestStatus.Completed, log.Status("q_h_wage"));
    }

    [Fact]
    public void FindActiveIdByKind_OnlyAnswersWhileTheRowIsActive()
    {
        var table = QuestTable.ActThreeArc();
        Assert.Equal("q_h_wage", table.FindIdByObjectiveKind(QuestObjectiveKind.WagePaid));

        var log = ActThreeLog();
        Assert.Null(log.FindActiveIdByObjectiveKind(QuestObjectiveKind.WagePaid));  // unstarted
        DriveToWageRow(log);
        Assert.Equal("q_h_wage", log.FindActiveIdByObjectiveKind(QuestObjectiveKind.WagePaid));
        log.ObserveWagePaid("q_h_wage");
        // A Completed row collects no settles (DA-verdict P3 on the shipped machine):
        Assert.Null(log.FindActiveIdByObjectiveKind(QuestObjectiveKind.WagePaid));
    }

    // ------------------------------------------------------------------
    // Save wire — THREE logs on the ONE shipped v3 list (the extended
    // QuestSaveRows/QuestRestoreRows composition contract)
    // ------------------------------------------------------------------

    [Fact]
    public void CrossTableRestore_ThreeLogs_OneRowList_EachSkipsTheForeignIds()
    {
        // The composition seam feeds the SAME list to all THREE logs; the
        // shipped "unknown id: skipped by design" is what keeps three tables
        // on one wire — zero save-file schema delta (F2, ActTwo precedent).
        var shared = new List<string>
        {
            "q_intro:completed", "q_speak:completed", "q_wage:completed",
            "q_kills:completed", "q_boss:completed",
            "q_r_descent:completed", "q_r_tongue:completed", "q_r_bread:completed", "q_r_bones:completed",
            "q_h_arrival:completed", "q_h_tongue:completed", "q_h_wage:active",
        };

        var act3 = ActThreeLog();
        act3.FromSaveRows(shared);
        Assert.Equal(QuestStatus.Completed, act3.Status("q_h_arrival"));
        Assert.Equal(QuestStatus.Completed, act3.Status("q_h_tongue"));
        Assert.Equal(QuestStatus.Active, act3.Status("q_h_wage"));
        Assert.Equal(QuestStatus.NotStarted, act3.Status("q_h_reckoning"));   // un-started rows stay
        Assert.Equal(new[] { "q_h_arrival:completed", "q_h_tongue:completed", "q_h_wage:active" },
                     act3.ToSaveRows());

        var act1 = new QuestLog(QuestTable.Default());
        act1.FromSaveRows(shared);
        Assert.True(act1.IsArcComplete);
        Assert.Equal(5, act1.ToSaveRows().Count);   // act-two + act-three ids skipped, not poisoned

        var act2 = new QuestLog(QuestTable.RuinsArc());
        act2.FromSaveRows(shared);
        Assert.True(act2.IsArcComplete);
        Assert.Equal(4, act2.ToSaveRows().Count);   // act-one + act-three ids skipped
    }

    // ------------------------------------------------------------------
    // Act three rides the ActChainSync machine over (act2, act3) — the
    // brief's "ActTwoSync pure restore-arm pattern, act-3 equivalent": the
    // mid-act restore edge EXISTS (player saves mid-act-three), so the same
    // four arms decide what a LOAD means; this leg pins FreshOpen + Adopt +
    // the F-DA2 double-load shape at the act-three seam.
    // ------------------------------------------------------------------

    [Fact]
    public void Sync_OpenEdgeAndMidActRestore_AndDoubleLoad_ActThreeChainStaysAlive()
    {
        var act1 = new QuestLog(QuestTable.Default());
        var act2 = new QuestLog(QuestTable.RuinsArc());
        var act3 = ActThreeLog();
        bool opened2 = false, opened3 = false, closeShown3 = false;
        var openCardsPlayed3 = 0;

        // The shipped act-one + act-two rows (both arcs complete on disk).
        var throughActTwo = new List<string>
        {
            "q_intro:completed", "q_speak:completed", "q_wage:completed",
            "q_kills:completed", "q_boss:completed",
            "q_r_descent:completed", "q_r_tongue:completed", "q_r_bread:completed", "q_r_bones:completed",
        };

        void LoadThree()   // SyncActThreeFromRestore's pure half (Story.Act3.cs)
        {
            act3.FromSaveRows(throughActTwo);
            switch (ActChainSync.Run(act2, act3, opened3, ref closeShown3))
            {
                case ActChainSyncAction.Adopt: opened3 = true; break;
                case ActChainSyncAction.Rollback: opened3 = false; break;
                case ActChainSyncAction.FreshOpen:
                    opened3 = true; openCardsPlayed3++;        // OpenActThree: card + Start
                    act3.Start(act3.Table.Entries[0].Id);
                    break;
            }
        }

        act1.FromSaveRows(throughActTwo);
        act2.FromSaveRows(throughActTwo);
        Assert.True(act2.IsArcComplete);        // the act-three open edge is armed

        LoadThree();                                                  // load 1
        Assert.Equal(1, openCardsPlayed3);                            // FreshOpen
        Assert.Equal("q_h_arrival", act3.ActiveQuestId);

        // Mid-act restore: the disk now carries act-three rows -> Adopt arm,
        // silent (no card, no strand) — the mid-quest restore edge.
        throughActTwo.AddRange(new[] { "q_h_arrival:completed", "q_h_tongue:active" });
        LoadThree();
        Assert.Equal(1, openCardsPlayed3);                            // a load is not a beat
        Assert.Equal(QuestStatus.Completed, act3.Status("q_h_arrival"));
        Assert.Equal(QuestStatus.Active, act3.Status("q_h_tongue"));

        // F-DA2 shape at the act-three seam: prev act complete + latch open +
        // ZERO act-three rows (an older save loaded AGAIN in one session) —
        // SilentReStart, chain ALIVE, no second card.
        throughActTwo.RemoveRange(9, 2);
        LoadThree();
        Assert.Equal(1, openCardsPlayed3);
        Assert.False(closeShown3);
        Assert.Equal("q_h_arrival", act3.ActiveQuestId);

        // …and COMPLETABLE from the re-Started state — the act-completion
        // condition (the ACT_THREE_COMPLETE edge) is reachable.
        act3.ObserveZoneEntered("hollow");
        act3.ObserveSpoken(3);
        act3.ObserveWagePaid("q_h_wage");
        act3.ObserveKill(8);
        Assert.True(act3.IsArcComplete);
    }

    // ------------------------------------------------------------------
    // Act card data — the table shape (node REFERENCE integrity is pinned
    // against DialogueTable by tests/story/QuestArcCrossCheckTests)
    // ------------------------------------------------------------------

    [Fact]
    public void ActThree_CardAuthorsOrderedNonEmptyNodeLists()
    {
        var card = QuestTable.ActThree;
        Assert.Equal("act_three", card.Id);
        Assert.Equal(new[] { "act3_open_a", "act3_open_b" }, card.OpenNodes);
        Assert.Equal(new[] { "act3_close_a", "act3_close_b" }, card.CloseNodes);
    }
}
