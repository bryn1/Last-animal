using System;
using Xunit;
using LastAnimal.Story;

// Last Animal — quest state-machine tests (MC 3904 stage 2c, code, 2026-10-02).
//
// The pure QuestLog contract, headless (I3):
//   * the legal chain NotStarted -> Active -> ObjectiveMet -> Completed and
//     NO other transition — every illegal move throws InvalidOperationException
//     naming the guard (a planted-bad removal of any guard turns a named test
//     RED);
//   * fact feeds over existing observables only;
//   * the wage path is single: WagePaid arrives keyed by the quest id it
//     serves (riding the engine's Needing->Following settle — the pure half
//     tested here, the ride tested in the runtime proof);
//   * F2 ruling: the reserved "roster" LoyaltyChanged key NEVER feeds a
//     per-follower predicate;
//   * arc sequencing: satisfied -> met -> completed -> the NEXT row starts.
namespace LastAnimal.Tests.Quest;

public class QuestLogTests
{
    private static QuestLog DefaultLog() => new QuestLog(QuestTable.Default());

    private static QuestLog SingleLog(QuestObjectiveKind kind, int amount = 1, string arg = "") =>
        new QuestLog(new QuestTable(new[]
        {
            new QuestDef("q_a", "A", new QuestObjective(kind, amount, arg), "reward beat"),
        }));

    // ------------------------------------------------------------------
    // Legal chain
    // ------------------------------------------------------------------

    [Fact]
    public void LegalChain_WalksAllFourStatesAndRaisesChanged()
    {
        var log = SingleLog(QuestObjectiveKind.Spoken);
        var beats = new System.Collections.Generic.List<string>();
        log.Changed += (id, from, to) => beats.Add($"{id}:{from}->{to}");

        Assert.Equal(QuestStatus.NotStarted, log.Status("q_a"));
        log.Start("q_a");
        Assert.Equal(QuestStatus.Active, log.Status("q_a"));
        log.MarkObjectiveMet("q_a");
        Assert.Equal(QuestStatus.ObjectiveMet, log.Status("q_a"));
        log.Complete("q_a");
        Assert.Equal(QuestStatus.Completed, log.Status("q_a"));
        Assert.Equal(new[]
        {
            "q_a:NotStarted->Active", "q_a:Active->ObjectiveMet", "q_a:ObjectiveMet->Completed",
        }, beats);
    }

    // ------------------------------------------------------------------
    // Illegal transitions — one named test per guard (planted-bad targets)
    // ------------------------------------------------------------------

    [Fact]
    public void IllegalTransition_CompleteFromActive_ThrowsNamingTheGuard()
    {
        // THE planted-bad guard (task DoD 4): delete the ObjectiveMet check in
        // QuestLog.Complete and this test goes RED naming the guard.
        var log = SingleLog(QuestObjectiveKind.Spoken);
        log.Start("q_a");
        var ex = Assert.Throws<InvalidOperationException>(() => log.Complete("q_a"));
        Assert.Contains("requires ObjectiveMet", ex.Message);
        Assert.Equal(QuestStatus.Active, log.Status("q_a"));   // state untouched
    }

    [Fact]
    public void IllegalTransition_StartFromActive_Throws()
    {
        var log = SingleLog(QuestObjectiveKind.Spoken);
        log.Start("q_a");
        Assert.Throws<InvalidOperationException>(() => log.Start("q_a"));
    }

    [Fact]
    public void IllegalTransition_StartFromCompleted_Throws()
    {
        var log = SingleLog(QuestObjectiveKind.Spoken);
        log.Start("q_a"); log.MarkObjectiveMet("q_a"); log.Complete("q_a");
        var ex = Assert.Throws<InvalidOperationException>(() => log.Start("q_a"));
        Assert.Contains("requires NotStarted", ex.Message);
    }

    [Fact]
    public void IllegalTransition_MetFromNotStarted_Throws()
    {
        var log = SingleLog(QuestObjectiveKind.Spoken);
        var ex = Assert.Throws<InvalidOperationException>(() => log.MarkObjectiveMet("q_a"));
        Assert.Contains("requires Active", ex.Message);
    }

    [Fact]
    public void IllegalTransition_MetFromCompleted_Throws()
    {
        var log = SingleLog(QuestObjectiveKind.Spoken);
        log.Start("q_a"); log.MarkObjectiveMet("q_a"); log.Complete("q_a");
        Assert.Throws<InvalidOperationException>(() => log.MarkObjectiveMet("q_a"));
    }

    [Fact]
    public void IllegalTransition_CompleteFromNotStarted_Throws()
    {
        var log = SingleLog(QuestObjectiveKind.Spoken);
        Assert.Throws<InvalidOperationException>(() => log.Complete("q_a"));
    }

    [Fact]
    public void UnknownQuestId_ThrowsArgumentException()
    {
        var log = DefaultLog();
        Assert.Throws<ArgumentException>(() => log.Start("q_not_authored"));
        Assert.Throws<ArgumentException>(() => log.Status("q_not_authored"));
    }

    // ------------------------------------------------------------------
    // Fact feeds (existing observables only)
    // ------------------------------------------------------------------

    [Fact]
    public void SpokenFeed_CountsToTheAmount()
    {
        var log = SingleLog(QuestObjectiveKind.Spoken, 2);
        log.Start("q_a");
        log.ObserveSpoken();
        Assert.Equal(QuestStatus.Active, log.Status("q_a"));   // one of two
        log.ObserveSpoken();
        Assert.Equal(QuestStatus.Completed, log.Status("q_a"));
    }

    [Fact]
    public void ZoneFeed_OnlyTheNamedZoneSatisfies()
    {
        var log = SingleLog(QuestObjectiveKind.ZoneReached, 1, "meadow");
        log.Start("q_a");
        log.ObserveZoneEntered("canyon");
        Assert.Equal(QuestStatus.Active, log.Status("q_a"));
        log.ObserveZoneEntered("meadow");
        Assert.Equal(QuestStatus.Completed, log.Status("q_a"));
    }

    [Fact]
    public void DialogueFeed_ProviderSeamSatisfiesWithoutPush()
    {
        var log = SingleLog(QuestObjectiveKind.DialogueShown, 1, "intro");
        log.Start("q_a");
        Assert.Equal(QuestStatus.Active, log.Status("q_a"));
        log.SetDialogueNodeProvider(() => "intro");
        log.ObserveSpoken();      // any later evaluation pass sees the provider's node
        Assert.Equal(QuestStatus.Completed, log.Status("q_a"));
    }

    [Fact]
    public void BossDeadFeed_ProviderSeamSatisfiesTheFinale()
    {
        var log = SingleLog(QuestObjectiveKind.BossDead);
        log.SetBossDeadProvider(() => false);
        log.Start("q_a");
        Assert.Equal(QuestStatus.Active, log.Status("q_a"));
        log.SetBossDeadProvider(() => true);
        log.ObserveKill();        // evaluation rides any feed (a kill re-checks)
        Assert.Equal(QuestStatus.Completed, log.Status("q_a"));
    }

    // ------------------------------------------------------------------
    // Wage path — single, quest-id keyed (the ride is proven at runtime)
    // ------------------------------------------------------------------

    [Fact]
    public void WageFeed_CountsUnderTheServedQuestIdOnly()
    {
        var log = SingleLog(QuestObjectiveKind.WagePaid);
        log.Start("q_a");
        log.ObserveWagePaid("q_other");          // a settle serving another id
        Assert.Equal(QuestStatus.Active, log.Status("q_a"));
        log.ObserveWagePaid("");                 // no id: never counted
        Assert.Equal(QuestStatus.Active, log.Status("q_a"));
        log.ObserveWagePaid("q_a");              // THE settle for this quest
        Assert.Equal(QuestStatus.Completed, log.Status("q_a"));
    }

    [Fact]
    public void WageAttribution_ResolvesTheActiveWageRowOnly()
    {
        // DA-verdict P3 (ed4a5b2): the settle seam resolves "the quest the
        // wage serves" via the ACTIVE row — a Completed row collects nothing.
        var log = DefaultLog();
        log.SetBossDeadProvider(() => false);
        Assert.Null(log.FindActiveIdByObjectiveKind(QuestObjectiveKind.WagePaid));
        log.Start("q_intro");
        log.ObserveZoneEntered("meadow");        // -> q_speak
        log.ObserveSpoken();                     // -> q_wage ACTIVE
        Assert.Equal("q_wage", log.FindActiveIdByObjectiveKind(QuestObjectiveKind.WagePaid));
        log.ObserveWagePaid("q_wage");           // completes it
        Assert.Equal(QuestStatus.Completed, log.Status("q_wage"));
        Assert.Null(log.FindActiveIdByObjectiveKind(QuestObjectiveKind.WagePaid));
        // Table-wide lookup still names the (now completed) row — proving the
        // attribution fix moved to the ACTIVE-row API, not the table query.
        Assert.Equal("q_wage", log.Table.FindIdByObjectiveKind(QuestObjectiveKind.WagePaid));
    }

    // ------------------------------------------------------------------
    // F2 ruling — the reserved "roster" LoyaltyChanged key is never a
    // per-follower predicate input
    // ------------------------------------------------------------------

    [Fact]
    public void LoyaltyFeed_IgnoresTheReservedRosterKey()
    {
        var log = SingleLog(QuestObjectiveKind.LoyaltyAtLeast, 60);
        log.Start("q_a");
        log.ObserveLoyalty(QuestLog.ReservedLoyaltyKey, 100);   // the roster mean
        Assert.Equal(QuestStatus.Active, log.Status("q_a"));
        Assert.Equal("roster", QuestLog.ReservedLoyaltyKey);
        log.ObserveLoyalty("companion", 59);
        Assert.Equal(QuestStatus.Active, log.Status("q_a"));    // below threshold
        log.ObserveLoyalty("companion", 60);
        Assert.Equal(QuestStatus.Completed, log.Status("q_a")); // real follower
    }

    // ------------------------------------------------------------------
    // Arc sequencing over the default placeholder table
    // ------------------------------------------------------------------

    [Fact]
    public void DefaultArc_ChainsFactForFactToTheBossFinale()
    {
        var log = DefaultLog();
        log.SetBossDeadProvider(() => false);
        log.Start("q_intro");

        log.ObserveZoneEntered("meadow");       // q_intro met->completed->q_speak
        Assert.Equal(QuestStatus.Completed, log.Status("q_intro"));
        Assert.Equal(QuestStatus.Active, log.Status("q_speak"));
        Assert.Equal("q_speak", log.ActiveQuestId);

        log.ObserveSpoken();                    // -> q_wage
        Assert.Equal(QuestStatus.Completed, log.Status("q_speak"));
        Assert.Equal(QuestStatus.Active, log.Status("q_wage"));

        log.ObserveWagePaid("q_wage");          // -> q_kills
        Assert.Equal(QuestStatus.Completed, log.Status("q_wage"));
        Assert.Equal(QuestStatus.Active, log.Status("q_kills"));

        log.ObserveKill(4);                     // -> q_boss (Amount 4)
        Assert.Equal(QuestStatus.Completed, log.Status("q_kills"));
        Assert.Equal(QuestStatus.Active, log.Status("q_boss"));

        log.SetBossDeadProvider(() => true);    // finale: the kill push re-checks
        log.ObserveKill();
        Assert.Equal(QuestStatus.Completed, log.Status("q_boss"));
        Assert.True(log.IsArcComplete);
        Assert.Null(log.ActiveQuestId);
    }

    [Fact]
    public void LateObjective_StartTimeReEvaluationCatchesUp()
    {
        // The boss is already dead when the finale row starts (facts can
        // outrun the arc): Start re-evaluates and the row catches up.
        var log = DefaultLog();
        log.SetBossDeadProvider(() => true);
        log.Start("q_intro");
        log.ObserveZoneEntered("meadow"); log.ObserveSpoken();
        log.ObserveWagePaid("q_wage"); log.ObserveKill(4);
        Assert.Equal(QuestStatus.Completed, log.Status("q_boss"));
        Assert.True(log.IsArcComplete);
    }
}
