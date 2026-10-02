using Xunit;
using LastAnimal.Story;

// Last Animal — story data-layer harness self-test (MC 3900 stage 2a, code,
// 2026-10-02). Deliberately broken: claims the default table authors a node
// it does not, so it MUST fail. The gate script (ci/story_test.sh) drops it
// via /p:IncludeHarness=false for the real green run. This is the two-sided
// calibration — the harness must be able to go red (save_test.sh pattern).
namespace LastAnimal.Tests.Story;

public class StoryHarnessSelfTest
{
    // DELIBERATELY WRONG: claims a node exists in the authored table. No such
    // node is authored — the real table answers false. Must FAIL.
    [Fact]
    public void DeliberatelyBroken_DefaultTableAuthorsPhantomNode()
    {
        Assert.True(DialogueTable.Default().Has("node_that_does_not_exist"),
                    "claimed the default table authors a phantom node (it does not)");
    }
}
