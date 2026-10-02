using LastAnimal.Core.Framework;
using LastAnimal.Story;

// Last Animal — stage 2c story/quest wiring (MC 3904, code, 2026-10-02).
//
// Partial-class half of WorldDirector (one class = still THE single
// composition root, plan §B): constructs the pure quest core
// (src/story/QuestLog + QuestTable), subscribes it to the EXISTING
// observables (DNA speak/extract on the bus, loyalty deltas on the bus,
// zone entries on the ecosystem event, the live boss, the dialogue view's
// active node), maps the log's transitions onto the five batched bus
// signals, and injects the QuestStates save seams into SaveLoadController.
//
// It owns NO gameplay rules (I1 — same contract as the root file): the state
// machine, the wage-riding semantics and the F2 roster-key exclusion all live
// in the pure QuestLog. The WagePaid emit seam (NotifyWageSettled) is called
// from the root's pay arm and fires ONLY when the settle actually landed
// (SalaryDue fell) — the companion state machine stays event-free (Phase-9
// contract) and no second wage path exists.
//
// Gate seam (design §4.2 idiom): SetQuestHooksEnabled — one bool, no gameplay
// logic; the quest_neg runtime mode proves the arc cannot advance without it.
namespace LastAnimal.World;

public partial class WorldDirector
{
    private QuestLog _quests = null!;
    private bool _storyHooksEnabled = true;

    /// <summary>Read-only surface the runtime proofs read (proof-only, no logic).</summary>
    public QuestLog Quests => _quests;

    /// <summary>Proof seam: is a wage currently due? (read-only, no logic).</summary>
    public bool WageDueNow => _needs.SalaryDue;

    /// <summary>
    /// Root seam, called from _Ready right after the SaveLoadController is
    /// constructed (and before the boot EnterZone, so the meadow entry feeds
    /// the intro quest). Constructs the log, wires save seams, subscribes the
    /// hooks, and starts the arc's first row.
    /// </summary>
    private void InitStory()
    {
        _quests = new QuestLog(QuestTable.Default());
        _quests.Changed += OnQuestChanged;
        _saveLoad.QuestStatesWrite = () => _quests.ToSaveRows();
        _saveLoad.QuestStatesRestore = rows => _quests.FromSaveRows(rows);

        if (!_storyHooksEnabled) return;   // gate seam (design §4.2)

        // DA-verdict P3 (ed4a5b2): the seam is an HONEST no-op even when
        // flipped after _Ready — every hook re-checks _storyHooksEnabled at
        // delivery, not just at subscribe time.
        _bus.DnaSpoken += _ => { if (_storyHooksEnabled) _quests.ObserveSpoken(); };
        _bus.DnaExtracted += _ => { if (_storyHooksEnabled) _quests.ObserveKill(); };
        _bus.WagePaid += questId => { if (_storyHooksEnabled) _quests.ObserveWagePaid(questId); };
        _bus.LoyaltyChanged += (companion, loyalty) =>
        { if (_storyHooksEnabled) _quests.ObserveLoyalty(companion, loyalty); };
        _ecosystem.ZoneEntered += (zoneId, _) =>
        { if (_storyHooksEnabled) _quests.ObserveZoneEntered(zoneId); };
        if (_dialogue != null)
            _quests.SetDialogueNodeProvider(() => _dialogue.ActiveNode);
        _quests.SetBossDeadProvider(() => _boss != null && _boss.IsDead);

        // Authoring order IS arc order: the table's first row starts with the
        // world; its objective (the boot zone entry) lands on the same frame.
        _quests.Start(_quests.Table.Entries[0].Id);
    }

    /// <summary>Log transition -> the batched C2 quest signals (pure mapping,
    /// no logic; restore paths emit nothing because the log stays silent).</summary>
    private void OnQuestChanged(string questId, QuestStatus from, QuestStatus to)
    {
        switch (to)
        {
            case QuestStatus.Active: _bus.EmitQuestStarted(new QuestId(questId)); break;
            case QuestStatus.ObjectiveMet: _bus.EmitQuestObjective(new QuestId(questId)); break;
            case QuestStatus.Completed: _bus.EmitQuestCompleted(new QuestId(questId)); break;
        }
    }

    /// <summary>
    /// The WagePaid seam (root pay arm, plan §B 2c ≤3 lines there): fires the
    /// batched WagePaid signal ONLY on a landed settle (Pay() made SalaryDue
    /// fall — the machine's Needing->Following transition), keyed by the quest
    /// the wage SERVES — resolved per still-Active wage row (DA-verdict P3: a
    /// Completed row collects no settles; with no Active wage row the settle
    /// simply rides no quest signal). No settle, no signal — no second wage
    /// path.
    /// </summary>
    private void NotifyWageSettled()
    {
        if (_quests == null || !_storyHooksEnabled) return;
        if (_needs.SalaryDue) return;   // Pay() did not settle this tick
        var wageQuestId = _quests.FindActiveIdByObjectiveKind(QuestObjectiveKind.WagePaid);
        if (wageQuestId == null) return;   // no quest is being served right now
        _bus.EmitWagePaid(new QuestId(wageQuestId));
    }

    /// <summary>Gate seam: disable all quest observation (quest_neg negative
    /// control). One-line bool guard, no gameplay logic (design §4.2).</summary>
    public void SetQuestHooksEnabled(bool enabled) => _storyHooksEnabled = enabled;
}
