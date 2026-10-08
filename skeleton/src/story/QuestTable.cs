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
// ARC CONTRACT (plan §B D5, content authored by stage 2d): the five rows
// below are the 5-quest arc (owner ruling D6: ends at a zone boss) under the
// STABLE ids the arc proof (ci_proofs/RuntimeIntegrationProof.Quests.cs)
// drives — 2d swapped titles/rewards under those ids and edited no proof
// file. The per-row objective KINDS and the intro zone arg stay exactly as
// 2c authored them: they are pinned by tests/quest (the id/kind/chain contract
// tests + the DefaultArc fact-for-fact sequence) and driven fact-for-fact by
// the quest_arc runtime mode — a content swap keeps every gate green.
// Rewards are dialogue beats (no loot system): each Reward names a node id
// of DialogueTable (src/story/DialogueTable.cs). tests/story
// (QuestArcCrossCheckTests.cs) cross-checks those node references plus every
// objective arg (DialogueShown -> node table, ZoneReached -> the spawner's
// zone set), so a rename anywhere in the content chain fails loud (DA P2).
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

/// <summary>
/// One authored ACT card (MC 10132 Inc-3 S10, RULING-5): the ordered node-id
/// lists an act plays when it opens and when its last row completes. This is
/// the whole act mechanism — NO cutscene system exists and none is added:
/// every card node is an ordinary DialogueTable entry shown through the
/// existing dialogue view (the composition root queues them as guarded
/// presentation beats, the shipped DLQ drains them in emission order and the
/// uniform auto-close retires the box). Node ids are DATA here; tests/story
/// (QuestArcCrossCheckTests) cross-checks every id against
/// DialogueTable.Default(), so a rename anywhere fails the gate, not the player.
/// </summary>
public sealed class ActCard
{
    /// <summary>Stable act id (gate-marker vocabulary, e.g. "act_two").</summary>
    public string Id { get; }

    /// <summary>Ordered node ids shown when the act opens (never empty).</summary>
    public IReadOnlyList<string> OpenNodes { get; }

    /// <summary>Ordered node ids shown when the act's last row completes.</summary>
    public IReadOnlyList<string> CloseNodes { get; }

    public ActCard(string id, IReadOnlyList<string> openNodes, IReadOnlyList<string> closeNodes)
    {
        if (string.IsNullOrEmpty(id))
            throw new ArgumentException("act card id must be non-empty", nameof(id));
        if (openNodes == null || openNodes.Count == 0)
            throw new ArgumentException($"act '{id}' must author at least one open card node", nameof(openNodes));
        if (closeNodes == null || closeNodes.Count == 0)
            throw new ArgumentException($"act '{id}' must author at least one close card node", nameof(closeNodes));
        foreach (var n in openNodes)
            if (string.IsNullOrEmpty(n))
                throw new ArgumentException($"act '{id}': empty open card node id", nameof(openNodes));
        foreach (var n in closeNodes)
            if (string.IsNullOrEmpty(n))
                throw new ArgumentException($"act '{id}': empty close card node id", nameof(closeNodes));
        Id = id;
        OpenNodes = openNodes;
        CloseNodes = closeNodes;
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

    /// <summary>Reward beat — a DialogueTable node id (rewards are dialogue
    /// beats, plan 2d; no loot system). The story cross-check test validates
    /// the reference, so a renamed node fails the gate, not the player.
    /// Playback (MC 3915): the story seam (world/WorldDirector.Story.cs)
    /// plays this node on completion as a reward beat through DialogueSystem,
    /// flagged reward-shown so it never feeds DialogueShown evidence.
    /// Lifecycle (MC 10026.1, owner ruling "Reward lines queue up and the
    /// box auto-closes after reading, nothing gets lost"): beats QUEUE in
    /// emission order inside the view and each is painted for a FULL dwell
    /// (DialogueSystem.RewardLineFrames); a beat completing while a queued
    /// head is still being read takes the head, and the preempted beat
    /// re-queues at the tail for its own full dwell later. Every line —
    /// reward beat or direct dialogue — auto-closes after its dwell ("All
    /// dialogue lines"). The REWARD_SHOWN marker stays STATE TRUTH printed
    /// at emission regardless of render order.</summary>
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

    /// <summary>The first row id authored for this objective kind (a purely
    /// structural table query). NOTE (ARCH W2, reworded by 2d): the runtime
    /// wage-settle seam does NOT use this — it attributes a settle through
    /// QuestLog.FindActiveIdByObjectiveKind, because a Completed row never
    /// keeps collecting settles. This query answers "does the arc author a
    /// row of this kind, first one wins" and stays for table-API symmetry
    /// (tests/quest pins both lookups to keep the roles distinct).</summary>
    public string? FindIdByObjectiveKind(QuestObjectiveKind kind)
    {
        foreach (var e in _entries)
            if (e.Objective.Kind == kind) return e.Id;
        return null;
    }

    /// <summary>
    /// The authored 5-quest arc (ids are the 2c/2d contract, plan §B D5;
    /// content authored in stage 2d — owner ruling D6: English,
    /// diegetic-minimal, ending at the zone boss). The arc proof drives the
    /// ids and the objective facts, never the text; rewards reference the
    /// arc nodes of DialogueTable (cross-checked by tests/story).
    /// </summary>
    public static QuestTable Default() => new QuestTable(new QuestDef[]
    {
        // The boot zone entry satisfies the intro beat; the arc then runs
        // speak -> wage settle -> kills (also crossing the boss-spawn DNA
        // threshold) -> the zone-boss finale (owner ruling D6).
        new("q_intro", "Arrival",
            new QuestObjective(QuestObjectiveKind.ZoneReached, 1, "meadow"),
            "intro"),
        new("q_speak", "Answer in Tongue",
            new QuestObjective(QuestObjectiveKind.Spoken, 1),
            "first_speak"),
        new("q_wage", "Bread Before Bonds",
            new QuestObjective(QuestObjectiveKind.WagePaid, 1),
            "wage_duty"),
        new("q_kills", "Blood Teaches",
            new QuestObjective(QuestObjectiveKind.Kills, 4),
            "counters"),
        new("q_boss", "The Watcher Falls",
            new QuestObjective(QuestObjectiveKind.BossDead, 1),
            "boss_fallen"),
    });

    /// <summary>
    /// The authored ACT TWO rows (MC 10132 Inc-3 S10, RULING-5: "+4 quests
    /// ruins-deep, existing objective kinds, table-driven act cards, no
    /// cutscenes, no new zone"). Authoring order IS arc order — the SAME pure
    /// QuestLog machine plays this table (no new state, no new kind: every
    /// objective rides a shipped observable — a zone entered, a DNA speak, a
    /// wage settle, an extraction). The kinds are deliberately ordered
    /// descent -> tongue -> wage -> bones so each completion is driven by its
    /// own distinct fact, and BossDead/LoyaltyAtLeast stay UNUSED here: the
    /// act opens while the act-one boss corpse can still be the live boss, so
    /// a BossDead row would ride it — the shipped provider would read the
    /// corpse as a fresh kill (cascade). Rewards name DialogueTable nodes of
    /// the act-two set; tests/story cross-checks every reference.
    /// </summary>
    public static QuestTable RuinsArc() => new QuestTable(new QuestDef[]
    {
        new("q_r_descent", "The Way Down",
            new QuestObjective(QuestObjectiveKind.ZoneReached, 1, "ruins"),
            "ruins_gate"),
        new("q_r_tongue", "The Older Tongue",
            new QuestObjective(QuestObjectiveKind.Spoken, 2),
            "deep_tongue"),
        new("q_r_bread", "Bread in the Dark",
            new QuestObjective(QuestObjectiveKind.WagePaid, 1),
            "dark_bread"),
        new("q_r_bones", "The Deeper Count",
            new QuestObjective(QuestObjectiveKind.Kills, 6),
            "bones_deeper"),
    });

    /// <summary>
    /// The act-two open/close cards (RULING-5 "table-driven act cards"):
    /// ordered DialogueTable node ids, played as guarded presentation beats
    /// when the act opens and when <see cref="RuinsArc"/>'s last row
    /// completes. Composition-root-visible data only — the wiring shows these
    /// ids through the existing view and prints one ACT_CARD marker per event
    /// (the REWARD_SHOWN idiom); the reference leg of tests/story pins every
    /// id against the authored table.
    /// </summary>
    public static readonly ActCard ActTwo = new ActCard(
        "act_two",
        new[] { "act2_open_a", "act2_open_b" },
        new[] { "act2_close_a", "act2_close_b" });

    /// <summary>
    /// The authored ACT-THREE rows (MC 10273 Inc-4 S19, RULING-8 "R8 zone
    /// four + act three = YES -> S18 + S19 proceed" (MC 10026 append #9 row
    /// 895, owner chat "Rec on all."): the close of the story, the +4
    /// zone-4 rows on the zone four "hollow" that merged S18 shipped).
    /// Mirror of
    /// <see cref="RuinsArc"/>'s idiom exactly: the SAME pure QuestLog
    /// machine plays this table, existing objective kinds only, kinds
    /// ordered arrival -> tongue -> bread -> reckoning so each completion
    /// is driven by its own distinct fact, and BossDead/LoyaltyAtLeast/
    /// DialogueShown stay UNUSED for the same cascade reason the RuinsArc
    /// header names (the act opens on the act-two finale — a BossDead row
    /// would ride the corpse the shipped provider still reads as live).
    /// The final row is the FINAL ZONE-4 CHAIN: the act-completion condition
    /// the wiring tests is this table completing (ACT_THREE_COMPLETE).
    /// Rewards name DialogueTable nodes of the act-three set; tests/story
    /// cross-checks every reference.
    /// </summary>
    public static QuestTable ActThreeArc() => new QuestTable(new QuestDef[]
    {
        new("q_h_arrival", "The Hollow",
            new QuestObjective(QuestObjectiveKind.ZoneReached, 1, "hollow"),
            "hollow_gate"),
        new("q_h_tongue", "The Last Tongue",
            new QuestObjective(QuestObjectiveKind.Spoken, 3),
            "hollow_tongue"),
        new("q_h_wage", "Bread at the End",
            new QuestObjective(QuestObjectiveKind.WagePaid, 1),
            "hollow_wage"),
        new("q_h_reckoning", "The Hollow Count",
            new QuestObjective(QuestObjectiveKind.Kills, 8),
            "hollow_count"),
    });

    /// <summary>The act-three open/close cards — the same table-driven
    /// ActCard mechanism as <see cref="ActTwo"/> (RULING-5), opened when the
    /// act-two arc completes and closed when <see cref="ActThreeArc"/>'s
    /// last row completes (the story's close).</summary>
    public static readonly ActCard ActThree = new ActCard(
        "act_three",
        new[] { "act3_open_a", "act3_open_b" },
        new[] { "act3_close_a", "act3_close_b" });
}
