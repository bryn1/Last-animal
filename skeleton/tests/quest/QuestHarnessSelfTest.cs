using Xunit;
using LastAnimal.Story;

// Last Animal — quest-core harness self-test (MC 3904 stage 2c, code,
// 2026-10-02). Deliberately broken: it claims the COMPLETE-WITHOUT-MET
// shortcut exists (Complete straight from Active), which the state machine
// forbids — so it MUST fail. The gate script (ci/quest_test.sh) drops it via
// /p:IncludeHarness=false for the real green run. Two-sided calibration,
// the ci/save_test.sh pattern: a harness that cannot go red proves nothing.
namespace LastAnimal.Tests.Quest;

public class QuestHarnessSelfTest
{
    // DELIBERATELY WRONG: claims a legal chain shortcut the guard denies.
    // The real log throws; this expectation therefore FAILS as required.
    [Fact]
    public void DeliberatelyBroken_CompleteAcceptsAnActiveQuestWithoutMet()
    {
        var log = new QuestLog(QuestTable.Default());
        log.Start("q_intro");
        // The state machine REQUIRES ObjectiveMet first — claiming otherwise
        // is the planted lie this self-test exists to expose.
        log.Complete("q_intro");
        Assert.True(false, "unreachable: the Complete guard must throw first");
    }
}
