using System;
using System.IO;

// Last Animal — repo-path test util (MC 11433, audit MC 11430 CC-13). ONE home
// for the RepoFile idiom: formerly three private copies — tests/quest/
// QuestBusContractTests.cs (the origin the two doc comments named) plus two
// VERBATIM clones in tests/skill/SkillSaveRoundtripTests.cs and tests/story/
// QuestArcCrossCheckTests.cs (body-hash collision 410d787c6204 at the audit).
// Every consuming csproj registers this file (explicit-Compile-list rule, same
// mechanism as every listed source); the engine project does NOT compile it —
// it is test-side only, so it rides the three test csprojs alone.
namespace LastAnimal.Tests;

/// <summary>Repo-path helpers for the source-pinning test suites.</summary>
internal static class TestPaths
{
    /// <summary>Locate a repo file relative to the skeleton dir by walking up
    /// from the test assembly output (QuestBusContractTests.RepoFile idiom —
    /// robust for any bin/Depth and any checkout location).</summary>
    public static string RepoFile(string relPath)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var candidate = Path.Combine(dir.FullName, relPath);
            if (File.Exists(candidate)) return File.ReadAllText(candidate);
            dir = dir.Parent;
        }
        throw new FileNotFoundException($"source not found from {AppContext.BaseDirectory}: {relPath}");
    }
}
