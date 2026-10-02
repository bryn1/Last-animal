using System;
using System.Collections.Generic;

// Last Animal — quest data layer (MC 3904, stage 2c, code, 2026-10-02).
//
// Engine-free quest definitions (I3): one row per quest — id, title, objective
// spec, reward text beat. Same shape discipline as src/story/DialogueTable.cs:
// pure C#, compiled by the main assembly AND by tests/quest (dotnet test,
// no GodotSharp). Objective specs name EXISTING observable facts only (plan
// §B 2c: zone entered, DNA extracted, wage settled, dialogue node shown,
// per-follower loyalty, boss dead) — the quest core subscribes, it never
// re-derives companion/DNA/zone state and it never duplicates ZoneProgression.
//
// PLACEHOLDER CONTRACT (plan §B D5): the five rows below are the 5-quest arc
// (owner ruling D6: ends at a zone boss) under STABLE ids. Stage 2d swaps the
// content rows (titles, rewards, objective args) under the SAME ids — the
// arc proof (ci_proofs/RuntimeIntegrationProof.Quests.cs) drives the ids and
// is never edited by 2d.
namespace LastAnimal.Story;

/// <summary>The objective fact a quest row waits on (all bus/scene observables).</summary>
public enum QuestObjectiveKind
{
    /// <summary>A dialogue node id was shown (the shown-node seam, provider-fed).</summary>
    DialogueShown,
    /// <summary>The player spoke DNA this many times (EventBus.DnaSpoken).</summary>
    Spoken,
    /// <summary>A wage settle named this quest (EventBus.WagePaid, quest-id keyed).</summary>
    WagePaid,
    /// <summary>DNA extractions (kills) observed this many times (EventBus.DnaExtracted).</summary>
    Kills,
    /// <summary>The named zone was entered (EcosystemSpawner.ZoneEntered).</summary>
    ZoneReached,
    /// <summary>Any NON-reserved follower reached this loyalty (EventBus.LoyaltyChanged).</summary>
    LoyaltyAtLeast,
    /// <summary>The live zone boss is dead (scene seam, provider-fed).</summary>
    BossDead,
}

/// <summary>One objective spec: which fact, how many, with which argument.</summary>
public sealed class QuestObjective
{
    public QuestObjectiveKind Kind { get; }

    /// <summary>How many observations satisfy it (>= 1; 1 for boolean kinds).</summary>
    public int Amount { get; }

    /// <summary>Kind argument: node id (DialogueShown) or zone id (ZoneReached); else empty.</summary>
    public string Arg { get; }

    public QuestObjective(QuestObjectiveKind kind, int amount = 1, string arg = "")
    {
        if (amount < 1)
            throw new ArgumentException($"objective amount for kind '{kind}' must be >= 1", nameof(amount));
        Kind = kind;
        Amount = amount;
        Arg = arg ?? "";
    }
}

/// <summary>One authored quest row: id, title, objective, reward text beat.</summary>
public sealed class QuestDef
{
    /// <summary>Stable quest id — the arc-proof contract; 2d never renames it.</summary>
    public string Id { get; }

    /// <summary>Player-facing title (never empty — enforced here).</summary>
    public string Title { get; }

    public QuestObjective Objective { get; }

    /// <summary>Reward text beat (no loot system: rewards are dialogue beats, plan 2d).</summary>
    public string Reward { get; }

    public QuestDef(string id, string title, QuestObjective objective, string reward)
    {
        if (string.IsNullOrEmpty(id))
            throw new ArgumentException("quest id must be non-empty", nameof(id));
        if (string.IsNullOrEmpty(title))
            throw new ArgumentException($"quest '{id}' title must be non-empty", nameof(title));
        if (string.IsNullOrEmpty(reward))
            throw new ArgumentException($"quest '{id}' reward beat must be non-empty", nameof(reward));
        Id = id;
        Title = title;
        Objective = objective ?? throw new ArgumentNullException(nameof(objective));
        Reward = reward;
    }
}

/// <summary>
/// The authored quest rows (engine-free table, immutable after construction,
/// ids ordinal-unique). Authoring order IS the arc order: the log chains a
/// completed quest into the next row.
/// </summary>
public sealed class QuestTable
{
    private readonly QuestDef[] _entries;
    private readonly Dictionary<string, QuestDef> _byId;

    public QuestTable(IEnumerable<QuestDef> entries)
    {
        if (entries == null) throw new ArgumentNullException(nameof(entries));
        var list = new List<QuestDef>();
        _byId = new Dictionary<string, QuestDef>(StringComparer.Ordinal);
        foreach (var entry in entries)
        {
            if (entry == null) throw new ArgumentException("null quest row", nameof(entries));
            if (!_byId.TryAdd(entry.Id, entry))
                throw new ArgumentException($"duplicate quest id: '{entry.Id}'", nameof(entries));
            list.Add(entry);
        }
        _entries = list.ToArray();
    }

    /// <summary>All authored rows in arc order.</summary>
    public IReadOnlyList<QuestDef> Entries => _entries;

    public int Count => _entries.Length;

    public bool Has(string questId) => questId != null && _byId.ContainsKey(questId);

    public bool TryGetEntry(string questId, out QuestDef? entry)
    {
        entry = null;
        return questId != null && _byId.TryGetValue(questId, out entry);
    }

    public string? FindTitle(string questId) =>
        TryGetEntry(questId, out var entry) ? entry!.Title : null;

    /// <summary>The next arc row after the given id, or null at the finale.</summary>
    public QuestDef? Next(string questId)
    {
        for (int i = 0; i < _entries.Length - 1; i++)
            if (_entries[i].Id == questId) return _entries[i + 1];
        return null;
    }

    /// <summary>The first row id authored for an objective kind (the wage-settle
    /// seam names its target with this); null when no row carries the kind.</summary>
    public string? FindIdByObjectiveKind(QuestObjectiveKind kind)
    {
        foreach (var e in _entries)
            if (e.Objective.Kind == kind) return e.Id;
        return null;
    }

    /// <summary>
    /// The placeholder 5-quest arc (ids are the 2c/2d contract, plan §B D5).
    /// Content rows are swapped by stage 2d under these same ids; the arc
    /// proof drives the ids and never the text.
    /// </summary>
    public static QuestTable Default() => new QuestTable(new QuestDef[]
    {
        // The boot zone entry satisfies the intro beat; the proof then drives
        // speak -> wage settle -> kills (also crossing the boss-spawn DNA
        // threshold) -> the zone-boss finale (owner ruling D6).
        new("q_intro", "Placeholder: First Steps",
            new QuestObjective(QuestObjectiveKind.ZoneReached, 1, "meadow"),
            "Placeholder reward (2d authors)."),
        new("q_speak", "Placeholder: Answer in Tongue",
            new QuestObjective(QuestObjectiveKind.Spoken, 1),
            "Placeholder reward (2d authors)."),
        new("q_wage", "Placeholder: Bread Before Bonds",
            new QuestObjective(QuestObjectiveKind.WagePaid, 1),
            "Placeholder reward (2d authors)."),
        new("q_kills", "Placeholder: Learn Their Counters",
            new QuestObjective(QuestObjectiveKind.Kills, 4),
            "Placeholder reward (2d authors)."),
        new("q_boss", "Placeholder: Fall of the Zone Boss",
            new QuestObjective(QuestObjectiveKind.BossDead, 1),
            "Placeholder reward (2d authors)."),
    });
}
