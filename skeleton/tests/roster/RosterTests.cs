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
//   * S9 (MC 10145): the TRAIT derivation table pinned as DATA (planted-bad:
//     hash seed change -> red), the frozen/re-latched derived identity, and
//     the NF5/NF6 restore-identity seam (values bounded, sizes not).
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
        Assert.Equal(55, a.Component.Loyalty);                    // 21 Steadfast: base +5
        // S17 (MC 10201) restamp: id 22 is the FORAGER — its landed pay is
        // wage -2 (+3). The row's INDEPENDENCE claim is untouched; the
        // trait-effect table lives in TraitEffectTests.cs.
        Assert.Equal(53, b.Component.Loyalty);
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

    // --- DA W5 F1/F2/F4 regression pins (fix round 2026-10-03) -----------------

    [Fact]
    public void BetrayedFollower_IsNeverSettled_NoWagePaid_NoLoyaltyDrift()
    {
        // F1: the betrayer's wage clock stays live (SalaryDue TRUE — the very
        // state betrayal arrives in), yet EVERY pay press must settle nothing:
        // the settled list is the WagePaid source (TickRoster maps it 1:1), so
        // exclusion = no WagePaid; loyalty never moves = no drift, no quest
        // credit, no rising hearts on a hostile follower (DA W5 F1 chain 1-4).
        var roster = new CompanionRoster();
        var a = NewFollower("companion", 7);
        var b = NewFollower("follower", 21);
        roster.TryAdd(a); roster.TryAdd(b);

        DrainToZero(b);
        Assert.Equal(CompanionState.Betrayed, b.Machine.Tick());
        Assert.NotNull(roster.ExecuteBetrayal(b));
        Assert.True(b.Needs.SalaryDue);                            // clock still live

        // Drain the BETRAYAL's own delta the way TickRoster does every frame
        // (50 -> 0 is a legit betrayal emit); the pins below are about what the
        // pay presses add on top — which must be NOTHING.
        var emits = new List<CompanionRoster.LoyaltyEmit>();
        Assert.True(roster.CollectLoyaltyEmits(emits));

        for (int press = 0; press < 3; press++)                   // repeated pay presses
        {
            var settled = roster.PayDueFollowers();
            Assert.DoesNotContain(b, settled);                    // never a settle -> no WagePaid
            Assert.Equal(0, b.Component.Loyalty);                 // no loyalty drift
            Assert.True(b.Needs.SalaryDue);                       // never consumed
            Assert.Equal(CompanionState.Betrayed, b.Machine.State);
        }
        Assert.False(roster.CollectLoyaltyEmits(emits));          // pay presses moved NOTHING

        // Per-follower guard only: the BONDED follower still settles normally.
        while (!a.Needs.SalaryDue) a.Needs.TickAccompaniment(1);
        var s2 = roster.PayDueFollowers();
        Assert.Single(s2);
        Assert.Same(a, s2[0]);
        Assert.Equal(55, a.Component.Loyalty);
        Assert.Equal(0, b.Component.Loyalty);                     // betrayer untouched
    }

    [Fact]
    public void Forgive_OnBetrayer_IsRefused_NoBonusNoDrift()
    {
        // F1 (Forgive arm): the Empathy Book must not forge +5 affection on a
        // permanently-broken bond; bonded followers keep the bonus.
        var roster = new CompanionRoster();
        var a = NewFollower("companion", 7);
        var b = NewFollower("follower", 21);
        roster.TryAdd(a); roster.TryAdd(b);

        DrainToZero(b);
        Assert.Equal(CompanionState.Betrayed, b.Machine.Tick());
        Assert.NotNull(roster.ExecuteBetrayal(b));

        Assert.False(roster.Forgive(b));                          // refused: no bonus
        Assert.Equal(0, b.Component.Loyalty);                     // no drift on a dead bond
        Assert.True(roster.Forgive(a));                           // bonded path intact
        Assert.Equal(55, a.Component.Loyalty);
    }

    [Fact]
    public void TwoBetrayedRecruits_DistinctBusKeys()
    {
        // F2: BreakCompanion flips BOTH bond ids to -1; a LIVE-computed key
        // would drift both betrayers to "follower--1" (the D6 no-cross-key
        // contract violated in a reachable state). The key is identity-frozen
        // at construction, so post-break lines stay distinct.
        var roster = new CompanionRoster();
        var a = NewFollower("follower", 21);
        var b = NewFollower("follower", 22);
        roster.TryAdd(a); roster.TryAdd(b);

        DrainToZero(a); Assert.Equal(CompanionState.Betrayed, a.Machine.Tick());
        Assert.NotNull(roster.ExecuteBetrayal(a));
        DrainToZero(b); Assert.Equal(CompanionState.Betrayed, b.Machine.Tick());
        Assert.NotNull(roster.ExecuteBetrayal(b));

        Assert.False(a.Component.HasCompanion);                   // both bond ids are -1 NOW
        Assert.False(b.Component.HasCompanion);
        Assert.Equal("follower-21", a.BusKey);                    // frozen — no "--1" drift
        Assert.Equal("follower-22", b.BusKey);
        Assert.NotEqual(a.BusKey, b.BusKey);
    }

    [Fact]
    public void OversizedFollowersList_TruncatedToCap()
    {
        // F4: a hand-edited oversized save feeds MORE entries than the cap on
        // load. The world-side restore drops every REFUSED entry with a marker
        // and composes no body for it — this pins the contract that guard
        // stands on: TryAdd returns FALSE for every add past the cap, the
        // roster keeps exactly the first Cap entries, in order.
        var roster = new CompanionRoster();
        var accepted = new List<CompanionRoster.Follower>();
        for (int i = 0; i < 5; i++)                               // 5 entries, cap 3
        {
            var f = NewFollower(i == 0 ? "companion" : "follower", i == 0 ? 7 : 20 + i);
            bool ok = roster.TryAdd(f);
            // NF9 repair (was `if (!ok) continue; Assert.True(ok);` — the True was
            // unreachable, so the guard could return anything past the cap without
            // going red). The per-iteration contract pinned instead: the first Cap
            // adds return TRUE, EVERY add past the cap returns FALSE.
            Assert.Equal(i < CompanionRoster.Cap, ok);
            if (ok) accepted.Add(f);
        }
        Assert.Equal(CompanionRoster.Cap, roster.Count);
        Assert.Equal(3, accepted.Count);
        for (int i = 0; i < accepted.Count; i++)
            Assert.Same(accepted[i], roster[i]);                  // order intact, nothing swapped in
    }

    // --- S9 traits + NF5/NF6 restore-identity hardening (MC 10145) -----------

    [Fact]
    public void TraitFor_DerivationTable_IsPinnedAsData()
    {
        // RULING-4: TRAIT = EntityId hash % 4 over {Steadfast, Forager,
        // Sentinel, Bonded}, the S5 PitchFor pure-int fold (fixed seed 17 —
        // no System.Random, no string.GetHashCode). PINNED AS DATA: the
        // planted-bad for this card changes the hash seed in
        // CompanionRoster.TraitFor and THIS row must go RED. The repeat call
        // per id is the determinism pin (same input, same answer, pure).
        int[] ids = { 0, 7, 21, 22, 23, 24, 25, 100 };
        CompanionTrait[] expected =
        {
            CompanionTrait.Bonded,     CompanionTrait.Sentinel,
            CompanionTrait.Steadfast,  CompanionTrait.Forager,
            CompanionTrait.Sentinel,   CompanionTrait.Bonded,
            CompanionTrait.Steadfast,  CompanionTrait.Bonded,
        };
        for (int i = 0; i < ids.Length; i++)
        {
            Assert.Equal(expected[i], CompanionRoster.TraitFor(ids[i]));
            Assert.Equal(expected[i], CompanionRoster.TraitFor(ids[i]));   // deterministic re-call
        }
        // All four names are reachable over the id domain (the % 4 fold is
        // total and hits every arm; ids 0..3 are one full period for a
        // stride-1 fold).
        var reached = new HashSet<CompanionTrait>();
        for (int id = 0; id < 4; id++) reached.Add(CompanionRoster.TraitFor(id));
        Assert.Equal(4, reached.Count);
    }

    [Fact]
    public void FollowerTrait_IsFrozenAtConstruction_LikeTheBusKey()
    {
        // Trait latches with the identity at construction (spawn paths set
        // the bond id BEFORE the Follower is built — F2) and does NOT re-roll
        // when BreakCompanion flips the id to -1 (the same no-drift contract
        // TwoBetrayedRecruits_DistinctBusKeys pins for the key).
        var roster = new CompanionRoster();
        var f = NewFollower("follower", 21);                      // 21 -> Steadfast (table above)
        Assert.Equal(CompanionTrait.Steadfast, f.Trait);
        roster.TryAdd(f);

        DrainToZero(f);
        Assert.Equal(CompanionState.Betrayed, f.Machine.Tick());
        Assert.NotNull(roster.ExecuteBetrayal(f));
        Assert.Equal(-1, f.Component.CompanionEntityId);          // id flipped ...
        Assert.Equal(CompanionTrait.Steadfast, f.Trait);          // ... the trait stayed
    }

    [Fact]
    public void RestoreIdentity_ReLatchesBusKeyAndTrait_NF5()
    {
        // NF5 harden: the restore REUSE path rewrote the bond id in place and
        // left the LATCHED key stale ("<name>-<construction-time id>"). The
        // seam now re-latches key AND derived trait from the saved id (key
        // format unchanged, per the DA-c2 NF5 record).
        var f = NewFollower("follower", 21);
        Assert.Equal("follower-21", f.BusKey);
        f.RestoreIdentity(30, 60);
        Assert.Equal(30, f.Component.CompanionEntityId);
        Assert.Equal(60, f.Component.Loyalty);
        Assert.Equal("follower-30", f.BusKey);                    // was: stale "follower-21"
        Assert.Equal(CompanionRoster.TraitFor(30), f.Trait);      // 30 -> Forager (557 % 4)
        Assert.Equal(CompanionTrait.Forager, f.Trait);            // pinned, not self-referential
    }

    [Fact]
    public void RestoreIdentity_BoundsValuesNotSize_NF6()
    {
        // NF6 harden: a hand-edited save can carry ANY values; the restore
        // seam bounds them — bond id out of the -1 unbond-sentinel class (a
        // negative write used to leave a "name--1" member counted by the
        // mean), loyalty into the M03 clamp range [0,100]. SIZES stay the
        // cap's job (the F4 cap/trim legs above are UNCHANGED — F6).
        var f = NewFollower("follower", 21);

        f.RestoreIdentity(-5, 9999);
        Assert.Equal(0, f.Component.CompanionEntityId);           // never the sentinel class
        Assert.True(f.Component.HasCompanion);                    // hostile id cannot unbond a restored stack
        Assert.Equal(100, f.Component.Loyalty);                   // clamped into M03's range
        Assert.Equal("follower-0", f.BusKey);
        Assert.Equal(CompanionTrait.Bonded, f.Trait);             // 0 -> Bonded (527 % 4)

        f.RestoreIdentity(22, -7);
        Assert.Equal(0, f.Component.Loyalty);
        Assert.Equal(22, f.Component.CompanionEntityId);
    }

    [Fact]
    public void RestoreBounds_Helpers_PinTheTable_NF6()
    {
        // The pure bounds the restore seam writes with, pinned as data.
        Assert.Equal(0, CompanionRoster.RestoreBondId(-1));
        Assert.Equal(0, CompanionRoster.RestoreBondId(0));
        Assert.Equal(21, CompanionRoster.RestoreBondId(21));
        Assert.Equal(0, CompanionRoster.RestoreLoyalty(-7));
        Assert.Equal(0, CompanionRoster.RestoreLoyalty(0));
        Assert.Equal(84, CompanionRoster.RestoreLoyalty(84));
        Assert.Equal(100, CompanionRoster.RestoreLoyalty(100));
        Assert.Equal(100, CompanionRoster.RestoreLoyalty(9999));
    }

    // --- MC 10183 Inc-4 S12 row 6 (S9 carries c2 + c4) ----------------------

    [Fact]
    public void RebondedDeadBond_ClockBounded_PaysAtMostOneDeferredSkip()
    {
        // Save-surgery check at the UNIT-gate level (S9 c2, deferred P3 —
        // the run_mode leg stays DEFERRED per the DA-c2 rationale, reproduced
        // in the card evidence, declared not silent). Reachability note: in
        // production the surgically re-bonded dead stack cannot form (the
        // snapshot writes bonded-only; the restore DROPS broken stacks
        // before the reuse seam) — RestoreIdentity is nonetheless the ONE
        // live-stack mutation site such an edited save lands on, so its
        // carried wage clock is pinned HERE.
        // c4 (DueSeconds bound): the wage clock keeps ticking on a dead bond
        // (DA W5 F1 keeps the clock live) — it must stay BOUNDED at
        // PayIntervalSeconds, never accumulate without end.
        var f = NewFollower("follower", 21, loyalty: 50);
        f.Component.BreakCompanion();                 // manual break: machine stays Needing (NF7 shape)
        for (int s = 0; s < 600; s++) f.Needs.TickAccompaniment(1);
        Assert.True(f.Needs.SalaryDue);
        Assert.Equal(30.0, f.Needs.DueSeconds);       // pinned at the interval, never past it (pre-bound: 580)

        // The reuse seam re-bonds (surgery analogue: id and loyalty from the
        // edited entry, NF6 bounds as always).
        f.RestoreIdentity(9, 20);

        // TickRoster-mirror skip arm (bond flag true post-rebond): across
        // TWO consume opportunities the carried buffer drains EXACTLY ONE
        // deferred SkipPayment; the next one needs a full fresh interval.
        int skips = 0;
        for (int s = 0; s < 2; s++)
            if (f.Needs.ConsumeUnpaidInterval()) { f.Machine.SkipPayment(); skips++; }
        Assert.Equal(1, skips);                       // pre-bound: the buffer burst-skipped (up to 10)
        Assert.Equal(17, f.Component.Loyalty);        // exactly ONE SkipPenalty (3), clamp uninvolved
        Assert.False(f.Needs.ConsumeUnpaidInterval());
    }
}
