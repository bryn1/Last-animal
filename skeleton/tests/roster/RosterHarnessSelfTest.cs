using LastAnimal.Companion;
using Xunit;

// Last Animal — MC 3943 stage 2g roster harness self-test (code, 2026-10-02).
// ONE deliberately-broken case (the ci/companion_test.sh two-sided calibration
// idiom): it claims the roster-mean loyalty must EXCEED every follower's own
// loyalty, which the mean's definition forbids — so it MUST fail. ci/roster_test.sh
// drops it for the real green pass. A harness that cannot go red proves nothing.
namespace LastAnimal.Tests.Roster;

public class RosterHarnessSelfTest
{
    [Fact]
    public void DeliberatelyBroken_RosterMeanExceedsEveryFollower()
    {
        var roster = new CompanionRoster();
        var comp = new LastAnimal.Npc.CompanionComponent { Id = 1, CompanionEntityId = 7, Loyalty = 40 };
        var needs = new CompanionNeeds();
        var machine = new CompanionStateMachine("companion", comp, needs);
        roster.TryAdd(new CompanionRoster.Follower("companion", comp, needs, machine));

        // WRONG on purpose: a mean of loyalties in [0,100] can never exceed
        // its own only member — the mean rides the mean, not above it.
        Assert.True(roster.MeanLoyalty() > 40);
    }
}
