using LastAnimal.Companion;
using LastAnimal.Npc;
using System;
using System.Collections.Generic;
using Xunit;

// Last Animal — MC 10201 S17 trait-effect suite (code, 2026-10-06).
//
// The ratified rule table (plan .tmp/10147-inc4-plan-v1.md §S17, lines
// 219-241), pinned AS DATA — one pure rule per trait inside the engine-free
// CompanionRoster settle. Hash-seed drift REDS every row: each row drives a
// follower whose TRAIT is pinned from the S9 derivation table first (ids:
// 7 -> Sentinel, 21 -> Steadfast, 22 -> Forager, 24 -> Bonded), so a seed
// change re-labels the actor and the pinned delta no longer lands. Effect-
// not-applied REDS every row too: remove the rule in CompanionRoster and the
// pinned number breaks (planted-bad runs in the card evidence).
//
//   trait       trigger        base      rule           shipped row
//   Steadfast   wage-miss tick  -3       decay -25%     effective max(1,
//                                           floor(D*75/100)): 50->48, 4->2,
//                                           3->1, 2->1, 1->0 (terminal, all
//                                           strictly decreasing)
//   Forager     landed pay      +5       wage -2        net +3 (50->53);
//                                           gain min(3, granted): 97->100,
//                                           98->100, 100->100 (clamp-honest)
//   Bonded      landed pay      +0 Manna +1 Manna       the settle RETURN names
//                                           the follower (the world pay arm
//                                           consumes it at the shipped
//                                           SkillState.GainManna add site —
//                                           the live add is the TRAIT_EFFECTS
//                                           battery leg, not headless xunit)
//   Sentinel    —               —        CUT: no kill-assist seam exists at
//                                           0968010 (grep -ri assist == 0);
//                                           S16-census discipline: no seam
//                                           invented
//   band row    —               —        CUT: the S16/S8 counter census was
//                                           not filed at build time
namespace LastAnimal.Tests.Roster;

public class TraitEffectTests
{
    // --- builders (the RosterTests idiom) -------------------------------------

    private static CompanionRoster.Follower NewFollower(string name, int entityId, int loyalty = 50)
    {
        var comp = new CompanionComponent { Id = entityId, CompanionEntityId = entityId, Loyalty = loyalty };
        var needs = new CompanionNeeds(graceSeconds: 20, payIntervalSeconds: 30);
        var machine = new CompanionStateMachine(name, comp, needs);
        return new CompanionRoster.Follower(name, comp, needs, machine);
    }

    private static void MakeDue(CompanionRoster.Follower f)
    {
        while (!f.Needs.SalaryDue) f.Needs.TickAccompaniment(1);
    }

    // --- S9 derivation pins the rule rows ride (seed drift breaks them) ------

    [Fact]
    public void RuleTableActors_TraitsArePinned()
    {
        // The actors of every rule row below, traits pinned from the S9 table
        // FIRST: TraitFor seed drift re-labels these ids and REDS the rows.
        Assert.Equal(CompanionTrait.Steadfast, CompanionRoster.TraitFor(21));
        Assert.Equal(CompanionTrait.Forager, CompanionRoster.TraitFor(22));
        Assert.Equal(CompanionTrait.Sentinel, CompanionRoster.TraitFor(7));
        Assert.Equal(CompanionTrait.Bonded, CompanionRoster.TraitFor(24));
    }

    // --- STEADFAST: loyalty decay -25% on wage-miss ticks ---------------------

    [Fact]
    public void Steadfast_WageMissTick_TablePinned()
    {
        // Rule: effective decay = max(1, floor(D * 75/100)) over the RAW drain
        // D — the ratified -25%, floored, floored AGAIN at 1 so every tick
        // strictly decreases (no starvation immunity, betrayal reachable).
        var roster = new CompanionRoster();
        var steady = NewFollower("follower", 21);          // Steadfast (table above)
        var baseLine = NewFollower("follower", 7);         // Sentinel: no wage-miss rule
        Assert.Equal(CompanionTrait.Steadfast, steady.Trait);

        roster.TryAdd(steady);
        roster.WageMissTick(steady);
        Assert.Equal(48, steady.Component.Loyalty);        // D=3 -> 2 (base: -3)

        roster.TryAdd(baseLine);
        roster.WageMissTick(baseLine);
        Assert.Equal(47, baseLine.Component.Loyalty);      // the shipped M03 decay

        // Determinism (RULING-4): a fresh same-id slot replays the same answer.
        var again = NewFollower("follower", 21);
        roster.WageMissTick(again);
        Assert.Equal(48, again.Component.Loyalty);

        // Clamp edges pinned as data: the -25% rides the CLAMP-HONEST raw
        // drain, and the floor(1) keeps the sequence strictly decreasing to 0.
        var four = NewFollower("follower", 21, loyalty: 4);
        var three = NewFollower("follower", 21, loyalty: 3);
        var two = NewFollower("follower", 21, loyalty: 2);
        var one = NewFollower("follower", 21, loyalty: 1);
        roster.WageMissTick(four);                         // raw 3 -> eff 2
        roster.WageMissTick(three);                        // raw 3 -> eff 2
        roster.WageMissTick(two);                          // raw 2 -> eff 1
        roster.WageMissTick(one);                          // raw 1 -> eff 1 (terminal)
        Assert.Equal(2, four.Component.Loyalty);
        Assert.Equal(1, three.Component.Loyalty);
        Assert.Equal(1, two.Component.Loyalty);
        Assert.Equal(0, one.Component.Loyalty);
    }

    [Fact]
    public void Steadfast_WageMiss_BookkeepingIntact_BetrayalReachable()
    {
        // The rule discounts ONLY the loyalty decay: the M03 clock bookkeeping
        // (SkippedCycles/UnpaidCycles — the NF7 counters) keeps advancing, and
        // repeated ticks still drive a Steadfast follower to betrayal.
        var roster = new CompanionRoster();
        var f = NewFollower("follower", 21);               // Steadfast
        roster.TryAdd(f);
        MakeDue(f);
        roster.WageMissTick(f);
        Assert.Equal(48, f.Component.Loyalty);
        Assert.Equal(1, f.Needs.SkippedCycles);
        Assert.Equal(1, f.Machine.UnpaidCycles);

        int guard = 0;
        while (f.Component.Loyalty > 0 && guard++ < 100)
            roster.WageMissTick(f);
        Assert.Equal(0, f.Component.Loyalty);              // reachable, never pinned
        Assert.True(guard < 100);
        Assert.True(f.Needs.SkippedCycles > 1);            // counters kept advancing
        Assert.Equal(CompanionState.Betrayed, f.Machine.Tick());
    }

    // --- FORAGER: wage -2 on pay ----------------------------------------------

    [Fact]
    public void Forager_PayWageMinus2_TablePinned()
    {
        // "wage -2 on pay" in the ONLY wage the settle holds: the landed pay's
        // gain is 3 where the base settle grants 5 (PayBonus). Gain is
        // min(3, what the base granted) — at the cap edge the M03 clamp owns
        // the ceiling and a pay NEVER becomes a Forager penalty.
        var roster = new CompanionRoster();
        var forager = NewFollower("follower", 22);         // Forager (table above)
        var baseLine = NewFollower("follower", 21);        // Steadfast: no pay rule
        Assert.Equal(CompanionTrait.Forager, forager.Trait);
        roster.TryAdd(forager);
        MakeDue(forager);

        var settled = roster.PayDueFollowers();
        Assert.Single(settled);                            // settle RETURN intact
        Assert.Same(forager, settled[0]);
        Assert.Equal(53, forager.Component.Loyalty);       // 50 + 3 (base would: 55)
        Assert.False(forager.Needs.SalaryDue);             // the wage SETTLED

        roster.TryAdd(baseLine);
        MakeDue(baseLine);
        roster.PayDueFollowers();
        Assert.Equal(55, baseLine.Component.Loyalty);      // base +5 untouched

        // Cap edges: gain min(3, granted) — never below the base clamp result.
        var near = NewFollower("follower", 22, loyalty: 97);
        var atEdge = NewFollower("follower", 22, loyalty: 98);
        var capped = NewFollower("follower", 22, loyalty: 100);
        var r2 = new CompanionRoster();
        r2.TryAdd(near); r2.TryAdd(atEdge); r2.TryAdd(capped);
        MakeDue(near); MakeDue(atEdge);
        r2.PayDueFollowers();                              // capped never DUE
        Assert.Equal(100, near.Component.Loyalty);         // 97+3 = 100 exactly
        Assert.Equal(100, atEdge.Component.Loyalty);       // base granted 2 -> kept
        Assert.Equal(100, capped.Component.Loyalty);       // never discounted down
    }

    [Fact]
    public void Forager_WageMissStaysBase_SteadfastPayStaysBase()
    {
        // Cross-purity: each trait carries EXACTLY one rule. The Forager's
        // wage-miss tick decays the base -3; the Steadfast's pay grants +5.
        var roster = new CompanionRoster();
        var forager = NewFollower("follower", 22);
        var steady = NewFollower("follower", 21);
        roster.TryAdd(forager); roster.TryAdd(steady);

        roster.WageMissTick(forager);
        Assert.Equal(47, forager.Component.Loyalty);       // base decay for Forager

        MakeDue(steady);
        roster.PayDueFollowers();
        Assert.Equal(55, steady.Component.Loyalty);        // base pay for Steadfast
    }

    // --- BONDED: +1 Manna on its successful pay (settle RETURN rides it) ------

    [Fact]
    public void Bonded_SettleReturnNamesIt_RuleConstantPinned()
    {
        // Headless half of the Bonded rule: the EXISTING settle return names
        // the landed Bonded follower (its trait is the glue's dispatch key)
        // and the rule size is pinned at 1. The Manna ADD itself lives at the
        // shipped world-side add site (SkillState.GainManna in the pay arm) —
        // observed LIVE by the TRAIT_EFFECTS battery leg, never headless.
        Assert.Equal(1, CompanionRoster.BondedMannaOnPay);

        var roster = new CompanionRoster();
        var bonded = NewFollower("companion", 24);         // Bonded (table above)
        var other = NewFollower("follower", 22);           // Forager
        Assert.Equal(CompanionTrait.Bonded, bonded.Trait);
        roster.TryAdd(bonded); roster.TryAdd(other);
        MakeDue(bonded); MakeDue(other);

        var settled = roster.PayDueFollowers();
        Assert.Equal(2, settled.Count);                    // BOTH landed, roster order
        Assert.Same(bonded, settled[0]);
        Assert.Equal(CompanionTrait.Bonded, settled[0].Trait);   // the glue's key
        Assert.Equal(55, bonded.Component.Loyalty);        // NO loyalty rule for Bonded
    }

    [Fact]
    public void Bonded_BetrayerNeverSettled_RiderCannotFireOnDeadBond()
    {
        // DA W5 F1 stays the authority: a betrayed Bonded never enters the
        // settle return, so the world rider can never pay Manna on a dead bond.
        var roster = new CompanionRoster();
        var bonded = NewFollower("companion", 24);
        roster.TryAdd(bonded);

        MakeDue(bonded);
        int guard = 0;
        while (bonded.Component.Loyalty > 0 && guard++ < 100)
        {
            roster.WageMissTick(bonded);
            if (!bonded.Needs.SalaryDue) MakeDue(bonded);
        }
        Assert.Equal(CompanionState.Betrayed, bonded.Machine.Tick());
        Assert.NotNull(roster.ExecuteBetrayal(bonded));
        MakeDue(bonded);                                   // dead bond clock ticks

        var settled = roster.PayDueFollowers();
        Assert.Empty(settled);                             // no settle -> no rider
    }

    // --- WAGE-FREE UPKEEP BAND (trait-INDEPENDENT settle row, DA-F1) ----------

    [Fact]
    public void WageFreeUpkeep_BandTablePinned()
    {
        // Threshold pinned from the S16/S8 census (positions 1..6 confirmed
        // reachable; 4 = the S8 census number; the census's own CUT row makes
        // any k >= 7 illegal — WageFreeUpkeep stays a pure predicate so the
        // table below is the whole authority). Threshold drift REDS here.
        Assert.Equal(4, CompanionRoster.WageFreeUpkeepPositions);
        Assert.False(CompanionRoster.WageFreeUpkeep(0));
        Assert.False(CompanionRoster.WageFreeUpkeep(3));
        Assert.True(CompanionRoster.WageFreeUpkeep(4));     // the band lights
        Assert.True(CompanionRoster.WageFreeUpkeep(6));     // the live width (census {0,6})
    }

    [Fact]
    public void WageFreeUpkeep_WaivesTheWholeTick_TraitIndependent()
    {
        // Band ON waives the ENTIRE upkeep tick — no M03 decay, no cycle
        // bookkeeping — for EVERY trait (Steadfast/Forager/Sentinel/Bonded
        // alike); band OFF keeps each trait's own row EXACTLY (cross-check
        // against the shipped -3 and the Steadfast -2).
        var roster = new CompanionRoster();
        foreach (int id in new[] { 21, 22, 23, 24 })       // all four traits, cap-permitting order
        {
            var f = NewFollower("follower", id);
            roster.WageMissTick(f, wageFreeUpkeep: true);
            Assert.Equal(50, f.Component.Loyalty);          // 50->50: no decay, no…
            Assert.Equal(0, f.Needs.SkippedCycles);         // …no bookkeeping
            Assert.Equal(0, f.Machine.UnpaidCycles);
        }
        var steady = NewFollower("follower", 21);
        var forager = NewFollower("follower", 22);
        var sentinel = NewFollower("follower", 23);
        roster.WageMissTick(steady, wageFreeUpkeep: false);
        roster.WageMissTick(forager, wageFreeUpkeep: false);
        roster.WageMissTick(sentinel, wageFreeUpkeep: false);
        Assert.Equal(48, steady.Component.Loyalty);         // OFF = rows unchanged
        Assert.Equal(47, forager.Component.Loyalty);
        Assert.Equal(47, sentinel.Component.Loyalty);
    }

    [Fact]
    public void WageFreeUpkeep_PredicateIsPure_Deterministic()
    {
        // RULING-4: the predicate is a pure function of the position count —
        // no hidden state, replays identically (the width itself rides the
        // shipped consensus read at the glue; runtime-only, never persisted).
        for (int i = 0; i < 3; i++)
        {
            Assert.True(CompanionRoster.WageFreeUpkeep(4));
            Assert.False(CompanionRoster.WageFreeUpkeep(0));
        }
    }

    // --- no-new-state / determinism ---------------------------------------------

    [Fact]
    public void Rules_ArePureFunctions_NoNewStatePerSlot()
    {
        // RULING-4 per-SLOT determinism, one integrated trace: two fresh
        // rosters fed the SAME id sequence + the SAME rule calls replay
        // identical loyalties — the rules are pure functions of
        // (latched trait, M03 state), nothing else.
        static int[] RunRoster()
        {
            var roster = new CompanionRoster();
            var ids = new[] { 21, 22, 24 };
            foreach (int id in ids) roster.TryAdd(NewFollower("follower", id));
            for (int tick = 0; tick < 3; tick++)
            {
                roster.WageMissTick(roster[0]);            // Steadfast
                roster.WageMissTick(roster[1]);            // Forager (base decay)
                MakeDue(roster[2]);                        // Bonded
                roster.PayDueFollowers();                  // +3? no: Bonded +5, Manna world-side
            }
            var outp = new int[roster.Count];
            for (int i = 0; i < roster.Count; i++) outp[i] = roster[i].Component.Loyalty;
            return outp;
        }
        Assert.Equal(RunRoster(), RunRoster());
        int[] trace = RunRoster();
        Assert.Equal(44, trace[0]);                        // 50 - 2*3
        Assert.Equal(41, trace[1]);                        // 50 - 3*3
        Assert.Equal(65, trace[2]);                        // 50 + 5*3
    }
}
