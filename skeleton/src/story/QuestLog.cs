using System;
using System.Collections.Generic;

// Last Animal — quest core state machine (MC 3904, stage 2c, code, 2026-10-02).
//
// Engine-free (I3, same discipline as QuestTable/DialogueTable): one pure
// state machine per quest row —
//   NotStarted -> Active -> ObjectiveMet -> Completed
// and ONLY that chain. Every other transition throws InvalidOperationException
// naming the guard; tests/quest pins each guard by name (planted-bad: delete
// the Complete guard and the named test goes RED).
//
// Fact feeds (Observe*) carry ONLY already-observable facts (plan §B 2c); the
// log subscribes, it never re-derives companion/DNA/zone state. Two binding
// rules live HERE so they hold headless:
//   * F2 ruling: per-follower loyalty predicates MUST ignore the reserved
//     "roster" LoyaltyChanged key (2g emits the roster-mean last under it —
//     a quest predicate reading it would double-count the mean).
//   * the wage path is single: WagePaid rides the existing Needing->Following
//     settle and arrives keyed by the quest id it serves (no second wage path).
//
// Sequencing: the objective is auto-met-and-completed when satisfied (rewards
// are text beats — no turn-in system exists in this increment), and a
// Completed row starts the NEXT table row (authoring order IS arc order).
//
// Persistence (D2: v3 fields live): ToSaveRows/FromSaveRows speak the
// GameState.QuestStates "id:status" wire format (lower-case status tokens;
// "q_intro:active" style matches GameState.Representative). A restore is the
// FULL snapshot rewind — statuses AND the non-persisted evidence counters
// (DA P1 fix): after a load, rows re-earn progress through real
// observations. Restore sets states directly — the save is authoritative, the
// legality guards protect PLAY transitions — and emits no Changed events (a
// load is not a game beat).
namespace LastAnimal.Story;

/// <summary>The four legal quest states (the wire vocabulary is the lower-case name).</summary>
public enum QuestStatus
{
    NotStarted,
    Active,
    ObjectiveMet,
    Completed,
}

/// <summary>
/// The pure quest state machine over a QuestTable (id, status per row,
/// fact feeds, arc sequencing, save wire rows). No Godot types.
/// </summary>
public sealed class QuestLog
{
    /// <summary>F2 ruling: the reserved roster-mean LoyaltyChanged key — never a
    /// per-follower predicate input.</summary>
    public const string ReservedLoyaltyKey = "roster";

    private readonly QuestTable _table;
    private readonly Dictionary<string, QuestStatus> _status;
    private readonly Dictionary<string, int> _wagesPaidFor = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _followerLoyalty = new(StringComparer.Ordinal);
    private readonly HashSet<string> _zonesSeen = new(StringComparer.Ordinal);
    private readonly HashSet<string> _nodesSeen = new(StringComparer.Ordinal);

    private int _spoken;
    private int _kills;
    private Func<string>? _dialogueNodeProvider;
    private Func<bool>? _bossDeadProvider;
    private bool _evaluating;
    private bool _restoring;

    /// <summary>(questId, from, to) — raised on every non-restore transition.
    /// The engine-side adapter maps this to the three bus signals.</summary>
    public event Action<string, QuestStatus, QuestStatus>? Changed;

    public QuestLog(QuestTable table)
    {
        _table = table ?? throw new ArgumentNullException(nameof(table));
        _status = new Dictionary<string, QuestStatus>(StringComparer.Ordinal);
        foreach (var q in _table.Entries)
            _status[q.Id] = QuestStatus.NotStarted;
    }

    public QuestTable Table => _table;

    public QuestStatus Status(string questId) => _status[Row(questId).Id];

    /// <summary>Id of the first still-Active row (the tracker view's row), or null.</summary>
    public string? ActiveQuestId
    {
        get
        {
            foreach (var q in _table.Entries)
                if (_status[q.Id] == QuestStatus.Active) return q.Id;
            return null;
        }
    }

    /// <summary>True when every authored row is Completed (the arc's end).</summary>
    public bool IsArcComplete
    {
        get
        {
            foreach (var q in _table.Entries)
                if (_status[q.Id] != QuestStatus.Completed) return false;
            return _table.Count > 0;
        }
    }

    /// <summary>Id of the first still-Active row whose objective is of this
    /// kind, or null. A wage settle resolves attribution through THIS (not
    /// QuestTable.FindIdByObjectiveKind): a Completed row never keeps
    /// collecting settles — "the quest the settle serves" must still be
    /// serving it (DA-verdict P3, ed4a5b2 review).</summary>
    public string? FindActiveIdByObjectiveKind(QuestObjectiveKind kind)
    {
        foreach (var q in _table.Entries)
            if (_status[q.Id] == QuestStatus.Active && q.Objective.Kind == kind) return q.Id;
        return null;
    }

    // --- transitions (the legal chain; every other move throws) -----------

    public void Start(string questId)
    {
        var q = Row(questId);
        var cur = _status[q.Id];
        if (cur != QuestStatus.NotStarted)
            throw new InvalidOperationException(
                $"quest '{q.Id}': cannot start from {cur} (requires NotStarted)");
        Set(q.Id, QuestStatus.Active);
        EvaluatePass();
    }

    public void MarkObjectiveMet(string questId)
    {
        var q = Row(questId);
        var cur = _status[q.Id];
        if (cur != QuestStatus.Active)
            throw new InvalidOperationException(
                $"quest '{q.Id}': cannot mark the objective met from {cur} (requires Active)");
        Set(q.Id, QuestStatus.ObjectiveMet);
    }

    public void Complete(string questId)
    {
        var q = Row(questId);
        var cur = _status[q.Id];
        if (cur != QuestStatus.ObjectiveMet)
            throw new InvalidOperationException(
                $"quest '{q.Id}': cannot complete from {cur} (requires ObjectiveMet)");
        Set(q.Id, QuestStatus.Completed);
        var next = _table.Next(q.Id);
        if (next != null && _status[next.Id] == QuestStatus.NotStarted)
            Start(next.Id);
    }

    // --- fact feeds (existing observables only) ----------------------------

    /// <summary>The dialogue-shown seam (engine adapter feeds ActiveNode; a
    /// provider lets node-gated objectives be satisfied without a push).</summary>
    public void SetDialogueNodeProvider(Func<string>? provider) => _dialogueNodeProvider = provider;

    /// <summary>The boss-dead seam (engine adapter reads the live boss actor).</summary>
    public void SetBossDeadProvider(Func<bool>? provider) => _bossDeadProvider = provider;

    public void ObserveSpoken(int count = 1) { _spoken += count; EvaluatePass(); }

    public void ObserveKill(int count = 1) { _kills += count; EvaluatePass(); }

    public void ObserveZoneEntered(string zoneId)
    {
        if (!string.IsNullOrEmpty(zoneId)) _zonesSeen.Add(zoneId);
        EvaluatePass();
    }

    public void ObserveDialogueShown(string nodeId)
    {
        if (!string.IsNullOrEmpty(nodeId)) _nodesSeen.Add(nodeId);
        EvaluatePass();
    }

    /// <summary>One wage settle, keyed by the quest id it serves (WagePaid wire).
    /// Settles naming other/unknown ids are counted, never guessed onto a row.</summary>
    public void ObserveWagePaid(string questId)
    {
        if (string.IsNullOrEmpty(questId)) return;
        _wagesPaidFor.TryGetValue(questId, out int n);
        _wagesPaidFor[questId] = n + 1;
        EvaluatePass();
    }

    /// <summary>A LoyaltyChanged observation (companion bus key, new value).
    /// F2 ruling: the reserved "roster" key NEVER feeds a predicate.</summary>
    public void ObserveLoyalty(string companionKey, int loyalty)
    {
        if (string.IsNullOrEmpty(companionKey) || companionKey == ReservedLoyaltyKey) return;
        if (!_followerLoyalty.TryGetValue(companionKey, out int best) || loyalty > best)
            _followerLoyalty[companionKey] = loyalty;
        EvaluatePass();
    }

    // --- save wire rows (GameState.QuestStates vocabulary) -----------------

    /// <summary>Wire rows for every quest that has started, table order,
    /// "id:status" with lower-case status tokens ("q_intro:active").</summary>
    public List<string> ToSaveRows()
    {
        var rows = new List<string>();
        foreach (var q in _table.Entries)
            if (_status[q.Id] != QuestStatus.NotStarted)
                rows.Add(q.Id + ":" + _status[q.Id].ToString().ToLowerInvariant());
        return rows;
    }

    /// <summary>Apply loaded wire rows onto the log as the FULL snapshot the
    /// save is: the table resets to NotStarted first (a row absent from the
    /// save is genuinely un-started again), the in-memory evidence counters
    /// rewind with the statuses (the save persists no evidence — stale live
    /// counters must not fast-forward the arc on the next observation, see
    /// DA-verdict P1), then states are set directly — the save is authoritative
    /// (no play-transition legality) and no Changed events fire (a load is not
    /// a game beat). Unknown ids are skipped (2d swaps content rows; a stale
    /// save must not poison the current table); malformed rows throw.</summary>
    public void FromSaveRows(IEnumerable<string> rows)
    {
        if (rows == null) throw new ArgumentNullException(nameof(rows));
        _restoring = true;
        try
        {
            foreach (var q in _table.Entries)
                _status[q.Id] = QuestStatus.NotStarted;   // load REPLACES state
            // DA-verdict P1 (ed4a5b2): the FULL snapshot rewind includes the
            // non-persisted evidence — after a load the player must re-earn
            // progress with real observations, not have the loaded save's
            // unplayed rows cascade met->complete off pre-load counters.
            _wagesPaidFor.Clear();
            _followerLoyalty.Clear();
            _zonesSeen.Clear();
            _nodesSeen.Clear();
            _spoken = 0;
            _kills = 0;
            foreach (var row in rows)
            {
                if (row == null) throw new ArgumentException("null quest-state row", nameof(rows));
                int cut = row.IndexOf(':');
                if (cut <= 0 || cut == row.Length - 1 || row.IndexOf(':', cut + 1) >= 0)
                    throw new ArgumentException($"malformed quest-state row: '{row}'", nameof(rows));
                if (!TryParseStatus(row.Substring(cut + 1), out var status))
                    throw new ArgumentException($"unknown quest status in row: '{row}'", nameof(rows));
                if (_table.TryGetEntry(row.Substring(0, cut), out var q))
                    _status[q!.Id] = status;   // direct: restore is not a play transition
                // unknown id: skipped by design (see header)
            }
        }
        finally { _restoring = false; }
    }

    private static bool TryParseStatus(string token, out QuestStatus status)
    {
        status = QuestStatus.NotStarted;
        if (token == "not_started") return true;
        if (token == "active") { status = QuestStatus.Active; return true; }
        if (token == "objective_met") { status = QuestStatus.ObjectiveMet; return true; }
        if (token == "completed") { status = QuestStatus.Completed; return true; }
        return false;
    }

    // --- evaluation ---------------------------------------------------------

    private QuestDef Row(string questId)
    {
        if (!_table.TryGetEntry(questId, out var q) || q == null)
            throw new ArgumentException($"unknown quest id: '{questId}'", nameof(questId));
        return q;
    }

    private void Set(string questId, QuestStatus to)
    {
        var from = _status[questId];
        _status[questId] = to;
        if (!_restoring) Changed?.Invoke(questId, from, to);
    }

    /// <summary>One pass over the table in arc order: an Active row whose
    /// objective is satisfied is met and completed (rewards are text beats —
    /// no turn-in system), which starts the next row. The nested Start's own
    /// pass is suppressed by the guard so the outer loop keeps walking into
    /// the newly-Active row.</summary>
    private void EvaluatePass()
    {
        if (_evaluating) return;
        _evaluating = true;
        try
        {
            foreach (var q in _table.Entries)
            {
                if (_status[q.Id] != QuestStatus.Active) continue;
                if (ObjectiveSatisfied(q))
                {
                    MarkObjectiveMet(q.Id);
                    Complete(q.Id);
                }
            }
        }
        finally { _evaluating = false; }
    }

    private bool ObjectiveSatisfied(QuestDef q)
    {
        var o = q.Objective;
        switch (o.Kind)
        {
            case QuestObjectiveKind.Spoken: return _spoken >= o.Amount;
            case QuestObjectiveKind.Kills: return _kills >= o.Amount;
            case QuestObjectiveKind.ZoneReached: return _zonesSeen.Contains(o.Arg);
            case QuestObjectiveKind.WagePaid:
                return _wagesPaidFor.TryGetValue(q.Id, out int n) && n >= o.Amount;
            case QuestObjectiveKind.LoyaltyAtLeast:
                foreach (var loyalty in _followerLoyalty.Values)
                    if (loyalty >= o.Amount) return true;
                return false;
            case QuestObjectiveKind.DialogueShown:
                return _nodesSeen.Contains(o.Arg) || _dialogueNodeProvider?.Invoke() == o.Arg;
            case QuestObjectiveKind.BossDead: return _bossDeadProvider?.Invoke() == true;
            default: return false;
        }
    }
}
