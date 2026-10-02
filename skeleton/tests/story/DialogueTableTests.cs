using System;
using Xunit;
using LastAnimal.Story;

// Last Animal — story data-layer tests (MC 3900 stage 2a, code, 2026-10-02).
//
// The C13 data-source contract, pure logic (I3, no Godot): the default table
// carries the three migrated nodes with their EXACT previous painted strings
// (migration is a move, not a reword), adds at least two new authored arc
// seeds, and the optional condition hook stays nil until the quest layer
// wires gates. Unknown ids resolve to null — the never-blank fallback is the
// VIEW's contract (pinned by tests/story/StoryTextCaptureTest.cs on the
// engine side), not the table's.
namespace LastAnimal.Tests.Story;

public class DialogueTableTests
{
    private static DialogueTable Default() => DialogueTable.Default();

    // ------------------------------------------------------------------
    // Migration: the old switch's exact strings are the painted contract
    // ------------------------------------------------------------------

    [Fact]
    public void Default_KeepsMigratedNodeTextVerbatim()
    {
        var table = Default();
        Assert.Equal("The last animal stands at the edge of the world.", table.FindText("intro"));
        Assert.Equal("A breeze moves the tall grass. Something watches.", table.FindText("meadow"));
        Assert.Equal("The companion turns. There may be no turning back.", table.FindText("betray"));
    }

    // ------------------------------------------------------------------
    // Stage 2a authored new nodes (owner ruling D6: English, diegetic-
    // minimal). Exact-text assertions: removing or rewording an authored
    // node turns this gate RED and names the node.
    // ------------------------------------------------------------------

    [Fact]
    public void Default_AuthorsNewArcSeed_first_speak()
    {
        var table = Default();
        Assert.True(table.TryGetEntry("first_speak", out var entry),
                    "authored node 'first_speak' is missing from DialogueTable.Default()");
        Assert.Equal("You answer in its own tongue. It listens, for now.", entry!.Text);
    }

    [Fact]
    public void Default_AuthorsNewArcSeed_wage_duty()
    {
        var table = Default();
        Assert.True(table.TryGetEntry("wage_duty", out var entry),
                    "authored node 'wage_duty' is missing from DialogueTable.Default()");
        Assert.Equal("The companion waits. Bread first, bonds after.", entry!.Text);
    }

    [Fact]
    public void Default_AuthorsAtLeastFiveNodes()
    {
        // 3 migrated + at least 2 new authored seeds (spec floor).
        Assert.True(Default().Count >= 5, $"default table has {Default().Count} nodes, want >= 5");
    }

    // ------------------------------------------------------------------
    // Lookup contract
    // ------------------------------------------------------------------

    [Fact]
    public void FindText_UnknownId_ReturnsNull_FallbackIsTheViews()
    {
        var table = Default();
        Assert.False(table.Has("npc_2"));
        Assert.Null(table.FindText("npc_2"));
        Assert.Null(table.FindText(""));
        Assert.Null(table.FindText(null!));
    }

    [Fact]
    public void AuthoredTexts_AreNeverEmpty()
    {
        foreach (var entry in Default().Entries)
            Assert.False(string.IsNullOrWhiteSpace(entry.Text),
                         $"authored node '{entry.Id}' has empty text");
    }

    // ------------------------------------------------------------------
    // Optional condition hook: nil for now, honored when wired
    // ------------------------------------------------------------------

    [Fact]
    public void Default_ConditionsAreNilForNow()
    {
        foreach (var entry in Default().Entries)
            Assert.Null(entry.Condition);
        // A nil condition is always available.
        Assert.True(Default().IsAvailable("intro"));
    }

    [Fact]
    public void ConditionHook_GatesAvailabilityNotText()
    {
        var table = new DialogueTable(new[]
        {
            new DialogueEntry("locked", "Hidden until the gate opens.", _ => false),
            new DialogueEntry("open",   "Always readable.",             _ => true),
        });
        Assert.False(table.IsAvailable("locked"));
        Assert.True(table.IsAvailable("open"));
        // Gating hides availability, never the authored text itself.
        Assert.Equal("Hidden until the gate opens.", table.FindText("locked"));
    }

    // ------------------------------------------------------------------
    // Table integrity: construction-time rejects (negative testing)
    // ------------------------------------------------------------------

    [Fact]
    public void DuplicateNodeId_IsRejected()
    {
        Assert.Throws<ArgumentException>(() => new DialogueTable(new[]
        {
            new DialogueEntry("dup", "One."),
            new DialogueEntry("dup", "Two."),
        }));
    }

    [Fact]
    public void EmptyIdOrEmptyText_IsRejected()
    {
        Assert.Throws<ArgumentException>(() => new DialogueEntry("", "text"));
        Assert.Throws<ArgumentException>(() => new DialogueEntry("id", ""));
        Assert.ThrowsAny<ArgumentException>(() => new DialogueTable(null!));
    }
}
