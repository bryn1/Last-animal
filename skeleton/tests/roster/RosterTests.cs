using LastAnimal.Companion;
using LastAnimal.Npc;
using LastAnimal.Story;
using System;
using System.Collections.Generic;
using Xunit;

// Last Animal — MC 3943 stage 2g roster pure-logic suite (code, 2026-10-02).
//
// Headless (I3) pins of the roster contracts the runtime proof (mode
// roster_follow) re-asserts end-to-end on the live scene:
//   * cap 3 (owner ruling D1 verbatim "All rec") — the 4th join is REFUSED;
//   * INDEPENDENT per-follower wages + the ARCH W2 settle contract (the
//     settled list is what TickRoster maps onto per-follower WagePaid emits);
//   * per-follower betrayal: one turns, the others keep following;
//   * the betrayal burst AT THE CAP is deterministic (DA-c2 D9-6): roster
//     order, exact damage, exact identity;
//   * UNIQUE bus keys "<name>-<EntityId>" (DA-c2 D6) — no cross-keying;
//   * the EMIT-ORDER contract (plan §G D6): deltas first in roster order,
//     roster-mean LAST under the reserved key;
//   * F2 REGRESSION: the REAL QuestLog predicate permanently ignores the
//     reserved "roster" key (the mean must never feed a quest predicate).
// The visible bodies + Godot glue stay engine-side (world/WorldDirector.Roster.cs).
namespace LastAnimal.Tests.Roster;

public class CompanionRosterTests
{
    // --- builders ------------------------------------------------------------

    private static (CompanionRoster roster, CompanionComponent comp, CompanionNeeds needs,
                    CompanionStateMachine machine) NewRosterOfOne(string name = "companion", int entityId = 7)
    {
        var roster = new CompanionRoster();
        var stack = NewFollower(name, entityId);
        Assert.True(roster.TryAdd(stack));
        return (roster, stack.Component, stack.Needs, stack.Machine);
    }

    private static CompanionRoster.Follower NewFollower(string name, int entityId, int loyalty = 50)
    {
        var comp = new CompanionComponent { Id = entityId, CompanionEntityId = entityId, Loyalty = loyalty };
        var needs = new CompanionNeeds(graceSeconds: 20, payIntervalSeconds: 30);
        var machine = new CompanionStateMachine(name, comp, needs);
        return new CompanionRoster.Follower(name, comp, needs, machine);
    }

    /// <summary>Starve one follower: grace -> due, then one skip per unpaid
    /// interval (the same cadence TickRoster's skip arm runs).</summary>
    private static void DrainToZero(CompanionRoster.Follower f)
    {
        for (int guard = 0; guard < 100 && f.Component.Loyalty > 0; guard++)
        {
            while (!f.Needs.SalaryDue) f.Needs.TickAccompaniment(1);
            f.Needs.TickAccompaniment(30);           // a full unpaid interval
            if (f.Needs.ConsumeUnpaidInterval()) f.Machine.SkipPayment();
            f.Machine.Tick();
        }
    }

    // --- cap (owner ruling D1) ------------------------------------------------

    [Fact]
    public void Cap3_FourthJoinIsRefused_RosterUnchanged()
    {
        var roster = new CompanionRoster();
        Assert.True(roster.TryAdd(NewFollower("companion", 7)));
        Assert.True(roster.TryAdd(NewFollower("follower", 21)));
        Assert.True(roster.TryAdd(NewFollower("follower", 22)));
        // The 4th: REFUSED, roster stays at the cap, the refused entry is not added.
        var fourth = NewFollower("follower", 23);
        Assert.False(roster.TryAdd(fourth));
        Assert.Equal(3, roster.Count);
        Assert.DoesNotContain(fourth, roster.Followers);
        Assert.Equal(3, CompanionRoster.Cap);   // the ratified number itself
    }

    // --- independent wages (ARCH W2 settle contract) ---------------------------

    [Fact]
    public void Wages_AreIndependentPerFollower_SettledListNamesOnlyTheDueOnes()
    {
        var (roster, _, bootNeeds, _) = NewRosterOfOne();
        var recruit = NewFollower("follower", 21);
        roster.TryAdd(recruit);

        // Boot clock to due; the recruit's clock STAYS untouched (recruited fresh).
        while (!bootNeeds.SalaryDue) bootNeeds.TickAccompaniment(1);

        var settled = roster.PayDueFollowers();
        Assert.Single(settled);
        Assert.Same(roster[0], settled[0]);                       // only the DUE follower settled
        Assert.Equal(55, roster[0].Component.Loyalty);            // its OWN wage landed (+5)
        Assert.Equal(50, recruit.Component.Loyalty);              // the recruit is untouched

        // The recruit's wage comes due on ITS OWN clock later; the boot's does not.
        while (!recruit.Needs.SalaryDue) recruit.Needs.TickAccompaniment(1);
        settled = roster.PayDueFollowers();
        Assert.Single(settled);
        Assert.Same(recruit, settled[0]);                         // W2: THIS settle belongs to the recruit
        Assert.Equal(55, recruit.Component.Loyalty);
        Assert.Equal(55, roster[0].Component.Loyalty);            // boot unchanged by the recruit's settle
    }

    [Fact]
    public void OnePayPress_SettlesEveryDueFollower_Separately()
    {
        var roster = new CompanionRoster();
        var a = NewFollower("follower", 21);
        var b = NewFollower("follower", 22);
        roster.TryAdd(a);
        roster.TryAdd(b);
        while (!a.Needs.SalaryDue) a.Needs.TickAccompaniment(1);
        while (!b.Needs.SalaryDue) b.Needs.TickAccompaniment(1);

        var settled = roster.PayDueFollowers();                   // one press, both due
        Assert.Equal(2, settled.Count);                           // EACH settles its own wage
        Assert.Equal(55, a.Component.Loyalty);
        Assert.Equal(55, b.Component.Loyalty);
    }

    // --- per-follower betrayal ---------------------------------------------------

    [Fact]
    public void OneFollowerBetrays_TheOthersKeepFollowing()
    {
        var roster = new CompanionRoster();
        var a = NewFollower("companion", 7);
        var b = NewFollower("follower", 21);
        var c = NewFollower("follower", 22);
        roster.TryAdd(a); roster.TryAdd(b); roster.TryAdd(c);

        DrainToZero(b);
        Assert.Equal(CompanionState.Betrayed, b.Machine.Tick());
        var result = roster.ExecuteBetrayal(b);
        Assert.NotNull(result);
        Assert.False(b.Component.HasCompanion);                   // b's bond broke

        // a + c: still bonded, their machines keep following (own clocks idle).
        Assert.True(a.Component.HasCompanion);
        Assert.True(c.Component.HasCompanion);
        Assert.NotEqual(CompanionState.Betrayed, a.Machine.Tick());
        Assert.NotEqual(CompanionState.Betrayed, c.Machine.Tick());
        Assert.Equal(50, a.Component.Loyalty);
        Assert.Equal(50, c.Component.Loyalty);
    }

    [Fact]
    public void BetrayalBurst_AtCap_IsDeterministic()
    {
        // DA-c2 D9-6: three followers at the cap, ALL drained — executing in
        // roster order must produce the EXACT sequence (identity, damage,
        // loyalty), no matter which pay/betray path ran first in play.
        var roster = new CompanionRoster();
        var a = NewFollower("companion", 7, loyalty: 3);
        var b = NewFollower("follower", 21, loyalty: 3);
        var c = NewFollower("follower", 22, loyalty: 3);
        roster.TryAdd(a); roster.TryAdd(b); roster.TryAdd(c);

        DrainToZero(a); DrainToZero(b); DrainToZero(c);
        Assert.Equal(0, a.Component.Loyalty);
        Assert.Equal(CompanionState.Betrayed, a.Machine.Tick());

        var results = new List<int>();
        for (int i = 0; i < roster.Count; i++)
        {
            var r = roster.ExecuteBetrayal(roster[i]);
            Assert.NotNull(r);
            results.Add(r!.BetrayerEntityId * 1000 + r.DamageDealt);   // identity + damage pinned
        }
        Assert.Equal(new[] { 7 * 1000 + 15, 21 * 1000 + 15, 22 * 1000 + 15 }, results.ToArray());
        foreach (var f in roster.Followers)
        {
            Assert.False(f.Component.HasCompanion);
            Assert.Equal(0, f.Component.Loyalty);
        }
        // The mean now floors over three broken bonds (still roster members).
        Assert.Equal(0, roster.MeanLoyalty());
    }

    // --- unique bus keys + the emit-order contract (D6) -------------------------

    [Fact]
    public void BusKeys_AreUnique_PerFollowerDeltasNeverCrossKeys()
    {
        var (roster, bootComp, _, _) = NewRosterOfOne();
        var recruit = NewFollower("follower", 21);
        roster.TryAdd(recruit);
        Assert.Equal("companion-7", roster[0].BusKey);
        Assert.Equal("follower-21", roster[1].BusKey);

        bootComp.ModifyLoyalty(5);
        var emits = new List<CompanionRoster.LoyaltyEmit>();
        Assert.True(roster.CollectLoyaltyEmits(emits));
        Assert.Equal("companion-7", emits[0].Key);                // only the boot's key moved
        Assert.Equal(2, emits.Count);                             // + the reserved mean LAST
        Assert.Equal(CompanionRoster.ReservedMeanKey, emits[1].Key);
    }

    [Fact]
    public void EmitOrder_PerFollowerDeltasFirst_RosterMeanLAST()
    {
        var roster = new CompanionRoster();
        var a = NewFollower("companion", 7, loyalty: 60);
        var b = NewFollower("follower", 21, loyalty: 55);
        var idle = NewFollower("follower", 22, loyalty: 55);
        roster.TryAdd(a); roster.TryAdd(b); roster.TryAdd(idle);

        a.Component.ModifyLoyalty(-10);       // 50
        b.Component.ModifyLoyalty(+5);        // 60

        var emits = new List<CompanionRoster.LoyaltyEmit>();
        Assert.True(roster.CollectLoyaltyEmits(emits));
        // Deltas in ROSTER ORDER, then the mean EXACTLY LAST (the Hud last-value
        // redraw must always land on the mean — plan §G D6).
        Assert.Equal(3, emits.Count);
        Assert.Equal("companion-7", emits[0].Key); Assert.Equal(50, emits[0].Loyalty);
        Assert.Equal("follower-21", emits[1].Key); Assert.Equal(60, emits[1].Loyalty);
        Assert.Equal(CompanionRoster.ReservedMeanKey, emits[2].Key);
        Assert.Equal((50 + 60 + 55) / 3, emits[2].Loyalty);       // integer floor mean = 55
    }

    [Fact]
    public void EmitOrder_NoChange_NoEmits()
    {
        var (roster, _, _, _) = NewRosterOfOne();
        var emits = new List<CompanionRoster.LoyaltyEmit>();
        Assert.False(roster.CollectLoyaltyEmits(emits));          // nothing moved: silent
        Assert.Empty(emits);
        roster[0].Component.ModifyLoyalty(1);
        Assert.True(roster.CollectLoyaltyEmits(emits));           // then the diff fires
        Assert.False(roster.CollectLoyaltyEmits(emits));          // diffs updated: silent again
    }

    [Fact]
    public void Mean_EmptyRoster_IsZero_NotEmitted()
    {
        var roster = new CompanionRoster();
        Assert.Equal(0, roster.MeanLoyalty());
        var emits = new List<CompanionRoster.LoyaltyEmit>();
        Assert.False(roster.CollectLoyaltyEmits(emits));          // no followers: no mean ride
    }

    // --- F2 REGRESSION: quest loyalty predicates ignore the reserved key --------

    [Fact]
    public void QuestLoyaltyPredicates_PermanentlyIgnoreTheRosterKey()
    {
        // The compile-time pin: both sides MUST keep naming the same reserved key.
        Assert.Equal(QuestLog.ReservedLoyaltyKey, CompanionRoster.ReservedMeanKey);

        // The behavioral regression on the REAL QuestLog: a LoyaltyAtLeast row
        // must NOT be fed by the mean emit under "roster" (double count), and
        // MUST be fed by a real follower key.
        var table = new QuestTable(new[]
        {
            new QuestDef("q_bond", "Devotion",
                new QuestObjective(QuestObjectiveKind.LoyaltyAtLeast, 60), "reward"),
        });
        var log = new QuestLog(table);
        log.Start("q_bond");

        log.ObserveLoyalty(CompanionRoster.ReservedMeanKey, 99);   // the mean must be IGNORED
        Assert.Equal(QuestStatus.Active, log.Status("q_bond"));

        log.ObserveLoyalty("companion-7", 61);                     // a real follower key feeds it
        // The arc auto-met-and-completes on satisfaction (single-row table: the
        // row completes — the point is the predicate RAN for a real key only).
        Assert.Equal(QuestStatus.Completed, log.Status("q_bond"));
    }

    // --- Forgive / selection -----------------------------------------------------

    [Fact]
    public void Forgive_AppliesBonusToThatFollower_ClampedByM03()
    {
        var roster = new CompanionRoster();
        var a = NewFollower("companion", 7, loyalty: 50);
        var b = NewFollower("follower", 21, loyalty: 98);
        roster.TryAdd(a); roster.TryAdd(b);

        roster.Forgive(b);
        Assert.Equal(100, b.Component.Loyalty);                    // M03 clamp owns the ceiling
        Assert.Equal(50, a.Component.Loyalty);                     // the other follower is untouched
        Assert.Equal(5, CompanionRoster.ForgiveBonus);
    }

    [Fact]
    public void CycleSelected_Wraps_EmptySafe()
    {
        var roster = new CompanionRoster();
        roster.CycleSelected();                                    // empty: no throw
        Assert.Equal(0, roster.SelectedIndex);
        roster.TryAdd(NewFollower("companion", 7));
        roster.TryAdd(NewFollower("follower", 21));
        roster.CycleSelected(); Assert.Equal(1, roster.SelectedIndex);
        roster.CycleSelected(); Assert.Equal(0, roster.SelectedIndex);
    }

    [Fact]
    public void RemoveAt_ClampsSelection_RosterOrderPreserved()
    {
        var roster = new CompanionRoster();
        roster.TryAdd(NewFollower("companion", 7));
        roster.TryAdd(NewFollower("follower", 21));
        roster.TryAdd(NewFollower("follower", 22));
        roster.CycleSelected();                                    // selected = 1
        roster.RemoveAt(1);                                        // load-rebuild shape op
        Assert.Equal(2, roster.Count);
        Assert.Equal(22, roster[1].Component.CompanionEntityId);   // order intact
        Assert.True(roster.SelectedIndex < roster.Count);          // selection clamped
    }

    [Fact]
    public void AnyWageDue_TracksThePerFollowerClocks()
    {
        var (roster, _, bootNeeds, _) = NewRosterOfOne();
        var recruit = NewFollower("follower", 21);
        roster.TryAdd(recruit);
        Assert.False(roster.AnyWageDue);
        while (!recruit.Needs.SalaryDue) recruit.Needs.TickAccompaniment(1);
        Assert.True(roster.AnyWageDue);                            // ANY follower's due counts
        roster.PayDueFollowers();
        Assert.False(roster.AnyWageDue);
        while (!bootNeeds.SalaryDue) bootNeeds.TickAccompaniment(1);
        Assert.True(roster.AnyWageDue);
    }
}
