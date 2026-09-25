using LastAnimal.Companion;
using LastAnimal.Npc;
using Xunit;

// Last Animal — MC 1348 A3 regression: the wage/betrayal pillar must be
// reachable in play. Withholding payment past the grace must drain loyalty
// (M03 SkipPenalty per skipped interval) until betrayal can fire — the C7
// precondition path (CheckBetrayal true -> ExecuteBetrayal). The production
// wiring (the director's pay/skip settlement policy) is proven in-engine by
// ci_proofs/P1FixProof.cs mode wage_betrayal; this suite pins the pure stack
// that policy drives.
namespace LastAnimal.Tests;

public class WageBetrayalPillarTests
{
    [Fact]
    public void WithheldWage_DrainsLoyalty_ToBetrayal()
    {
        var comp = new CompanionComponent { Id = 1, CompanionEntityId = 2, Loyalty = 12 };
        var needs = new CompanionNeeds(graceSeconds: 20, payIntervalSeconds: 30);
        var sm = new CompanionStateMachine("garn", comp, needs);
        var betrayal = new BetrayalSystem();

        // Grace elapses -> the first wage is due; the machine enters Needing.
        needs.TickAccompaniment(20);
        Assert.True(needs.SalaryDue);
        Assert.Equal(CompanionState.Needing, sm.Tick());

        // The player withholds: each full unpaid interval is one skipped cycle
        // (M03 SkipSalary, -3). 12 loyalty -> 4 skips -> 0.
        for (int cycle = 0; cycle < 6 && sm.State != CompanionState.Betrayed; cycle++)
        {
            needs.TickAccompaniment(30);
            if (needs.ConsumeUnpaidInterval())
                sm.SkipPayment();
            sm.Tick();
        }

        Assert.Equal(0, comp.Loyalty);
        Assert.Equal(CompanionState.Betrayed, sm.State);

        // C7 precondition path: CheckBetrayal true -> ExecuteBetrayal reachable.
        Assert.True(betrayal.CheckBetrayal(comp));
        var result = betrayal.ExecuteBetrayal(comp);
        Assert.NotNull(result);
        Assert.Equal(15, result!.DamageDealt);   // BaseBetrayalDamage 10 + proximity 5
        Assert.False(comp.HasCompanion);         // the bond is broken
    }

    [Fact]
    public void PaidWage_RestartsInterval_NoBetrayal()
    {
        // The pay decision (P): paying each due wage keeps loyalty climbing and
        // betrayal unreachable — the positive arm of the pillar.
        var comp = new CompanionComponent { Id = 1, CompanionEntityId = 2, Loyalty = 50 };
        var needs = new CompanionNeeds(graceSeconds: 20, payIntervalSeconds: 30);
        var sm = new CompanionStateMachine("garn", comp, needs);

        for (int cycle = 0; cycle < 5; cycle++)
        {
            needs.TickAccompaniment(30);
            if (needs.SalaryDue)
            {
                sm.Tick();
                sm.Pay();   // the player pays each due wage
            }
        }

        Assert.True(comp.Loyalty > 50);
        Assert.Equal(CompanionState.Following, sm.State);
        Assert.False(new BetrayalSystem().CheckBetrayal(comp));
    }
}
