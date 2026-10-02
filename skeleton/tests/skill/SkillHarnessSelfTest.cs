using Xunit;
using LastAnimal.Combat;
using LastAnimal.Skills;

// Last Animal — skill-core harness self-test (MC 3912 stage 2e, code,
// 2026-10-02). Deliberately broken: it claims the Manna payment is
// OPTIONAL (arm lands even when the balance is short), which the economy
// forbids — so it MUST fail. The gate script (ci/skill_test.sh) drops it
// via /p:IncludeHarness=false for the real green run. Two-sided
// calibration, the ci/quest_test.sh pattern: a harness that cannot go red
// proves nothing.
namespace LastAnimal.Tests.Skill;

public class SkillHarnessSelfTest
{
    // DELIBERATELY WRONG: claims arming works on an insufficient balance.
    // The real TryInvertStrike rejects and spends nothing — this expectation
    // therefore FAILS as required.
    [Fact]
    public void DeliberatelyBroken_ArmLandsEvenOnInsufficientManna()
    {
        var player = new PlayerController(new CombatVec3(0f, 0f, 0f)) { Manna = 1 };
        var skills = new SkillState(player);
        // The economy REQUIRES the full cost up front — claiming otherwise
        // is the planted lie this self-test exists to expose.
        Assert.True(skills.TryInvertStrike(unlocked: true),
            "unreachable: the Manna cost guard must refuse the arm first");
    }
}
