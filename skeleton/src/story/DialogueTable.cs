using System;
using System.Collections.Generic;

// Last Animal — story data layer (MC 3900, stage 2a, code, 2026-10-02).
//
// Dialogue text moves OUT of the View's private hardcoded switch (the old
// `DialogueFor` stub in src/ui/DialogueSystem.cs, per its own comment: "the
// narrative lives in the script/data layer") INTO this authored, engine-free
// table: node id -> authored text + optional condition hook (Func<string,bool>,
// nil for every node in stage 2a; the 2c quest layer gates nodes with it).
//
// Pure C# (I3): compiled by the main assembly AND by the standalone xunit
// project tests/story/LastAnimalStoryTests.csproj, so `dotnet test` runs the
// suite headless without GodotSharp. REPLACES the text switch; the
// Show/Close/ActiveNode View contract and the View's never-blank diegetic
// fallback are UNCHANGED — the composition root injects the table
// (world/WorldDirector.BuildUi), never a parallel dialogue system.
namespace LastAnimal.Story;

/// <summary>One authored dialogue node: id, diegetic text, optional gate.</summary>
public sealed class DialogueEntry
{
    /// <summary>Node id used by DialogueSystem.Show(nodeId).</summary>
    public string Id { get; }

    /// <summary>The authored on-screen text (never empty — enforced here).</summary>
    public string Text { get; }

    /// <summary>
    /// Optional availability gate over the node id. Nil for now (stage 2a
    /// authors unconditional nodes); a wired condition hides the node from
    /// IsAvailable without removing its text (the View stays logic-free).
    /// </summary>
    public Func<string, bool>? Condition { get; }

    public DialogueEntry(string id, string text, Func<string, bool>? condition = null)
    {
        if (string.IsNullOrEmpty(id))
            throw new ArgumentException("dialogue node id must be non-empty", nameof(id));
        if (string.IsNullOrEmpty(text))
            throw new ArgumentException($"authored text for node '{id}' must be non-empty", nameof(text));
        Id = id;
        Text = text;
        Condition = condition;
    }
}

/// <summary>
/// The authored dialogue nodes the dialogue View paints (C13 data source).
/// Immutable after construction; ids are ordinal-unique.
/// </summary>
public sealed class DialogueTable
{
    private readonly DialogueEntry[] _entries;
    private readonly Dictionary<string, DialogueEntry> _byId;

    public DialogueTable(IEnumerable<DialogueEntry> entries)
    {
        if (entries == null) throw new ArgumentNullException(nameof(entries));
        var list = new List<DialogueEntry>();
        _byId = new Dictionary<string, DialogueEntry>(StringComparer.Ordinal);
        foreach (var entry in entries)
        {
            if (entry == null) throw new ArgumentException("null dialogue entry", nameof(entries));
            if (!_byId.TryAdd(entry.Id, entry))
                throw new ArgumentException($"duplicate dialogue node id: '{entry.Id}'", nameof(entries));
            list.Add(entry);
        }
        _entries = list.ToArray();
    }

    /// <summary>All authored nodes, in authoring order.</summary>
    public IReadOnlyList<DialogueEntry> Entries => _entries;

    /// <summary>Number of authored nodes.</summary>
    public int Count => _entries.Length;

    /// <summary>Does the table author this node id at all (condition aside)?</summary>
    public bool Has(string nodeId) => nodeId != null && _byId.ContainsKey(nodeId);

    /// <summary>Raw entry lookup; false when the table does not author the id.</summary>
    public bool TryGetEntry(string nodeId, out DialogueEntry? entry)
    {
        entry = null;
        return nodeId != null && _byId.TryGetValue(nodeId, out entry);
    }

    /// <summary>
    /// The authored text for a node id, or null when the table has no such
    /// node. Returning null (not a fallback string) is deliberate: the
    /// never-blank diegetic fallback stays the View's honesty contract.
    /// </summary>
    public string? FindText(string nodeId) =>
        TryGetEntry(nodeId, out var entry) ? entry!.Text : null;

    /// <summary>
    /// True when the node exists and its optional condition hook passes (a nil
    /// condition is always available). The View does not consult this in 2a;
    /// the quest layer (2c) gates node availability through it.
    /// </summary>
    public bool IsAvailable(string nodeId) =>
        TryGetEntry(nodeId, out var entry) && (entry!.Condition == null || entry.Condition(nodeId));

    /// <summary>
    /// The default authored table: injected by the composition root
    /// (world/WorldDirector.BuildUi) into the dialogue View.
    /// </summary>
    public static DialogueTable Default() => new DialogueTable(new DialogueEntry[]
    {
        // Migrated verbatim from the old DialogueSystem.DialogueFor switch —
        // the exact strings are the existing painted contract.
        new("intro",  "The last animal stands at the edge of the world."),
        new("meadow", "A breeze moves the tall grass. Something watches."),
        new("betray", "The companion turns. There may be no turning back."),
        // Arc seeds authored in stage 2a, grown to the full arc by stage 2d
        // (owner ruling D6: English, diegetic-minimal, matching the migrated
        // nodes' tone). Every quest reward beat of QuestTable.Default() names
        // a node of THIS table; tests/story cross-checks the references, so
        // renaming a node here fails the story gate, not the player.
        new("first_speak", "You answer in its own tongue. It listens, for now."),
        new("wage_duty",   "The companion waits. Bread first, bonds after."),
        // 2d arc beats: the kill-quest reward and the finale reward.
        new("counters",    "They fall. Something of their tongue passes into you. The land keeps count."),
        new("boss_fallen", "The watcher falls. The grass settles. Nothing is decided."),
        // MC 10132 Inc-3 S10 act-two beats (RULING-5, owner ruling D6 tone:
        // English, diegetic-minimal). The four reward beats are the
        // QuestTable.RuinsArc() rewards; the four card nodes are the
        // QuestTable.ActTwo open/close pairs, played as guarded presentation
        // beats through the SAME view + DLQ (no cutscene system). tests/story
        // cross-checks every quest reward reference and every act-card id
        // against these entries — a rename fails the story gate, not the player.
        new("ruins_gate",   "Stone teeth open. The wind comes up colder from below."),
        new("deep_tongue",  "You speak it twice over. Something old turns its whole attention to you."),
        new("dark_bread",   "You feed yours in the dark. Bread is loyalty anywhere."),
        new("bones_deeper", "The older bones learn your tongue now. The deep ground keeps its count."),
        new("act2_open_a",  "The watcher falls behind you. The way down opens."),
        new("act2_open_b",  "Older bones line the dark. They spoke a different tongue."),
        new("act2_close_a", "The deep places keep their count now, and it includes you."),
        new("act2_close_b", "You carry two tongues and the dark's permission. Go up."),
        // MC 10273 Inc-4 S19 act-three beats (RULING-8 "yes to zone4+act3",
        // owner ruling D6 tone: English, diegetic-minimal — the close). The
        // four reward beats are the QuestTable.ActThreeArc() rewards (the
        // zone-4 "hollow" chain S18 shipped the ground for); the four card
        // nodes are the QuestTable.ActThree open/close pairs, played as
        // guarded presentation beats through the SAME view + DLQ (no
        // cutscene system, S10 mechanism). tests/story cross-checks every
        // quest reward reference and every act-card id against these entries.
        new("hollow_gate",  "Wind with no direction. The ground here listens."),
        new("hollow_tongue","You speak the old words where nothing answers. Nothing ever needed to."),
        new("hollow_wage",  "The last debt paid in the last dark. Bread does not ask where."),
        new("hollow_count", "The hollow keeps its count now. It is yours, and it is done."),
        new("act3_open_a",  "The dark's permission is spent. The hollow takes the rest."),
        new("act3_open_b",  "Nothing lines this place. That is not the same as empty."),
        new("act3_close_a", "The count is kept. The land has no more questions for you."),
        new("act3_close_b", "Last animal. The name means what it means now."),
    });
}
