using Godot;
using LastAnimal.Core.Framework;
using LastAnimal.Story;
using LastAnimal.Ui;
using System.Collections.Generic;

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
//
// MC 3915 reward beat: the Completed arm shows the row's Reward node through
// the EXISTING dialogue view, flagged reward-shown (DialogueSystem.Show
// fromReward) so it never feeds DialogueShown evidence — the observation
// provider rides the guarded QuestDialogueNodeNow seam. Save path stays true:
// restores emit no Changed events, so a LOAD shows no reward beat.
//
// MC 10132 Inc-3 S10 ACT TWO (RULING-5, shipped seams only): a SECOND pure
// QuestLog instance plays QuestTable.RuinsArc() (the +4 ruins-deep rows)
// after act one's arc completes. NO new mechanism: same machine, same bus
// hooks (subscribed ONCE at act open, so act-two evidence is only ever
// earned in-act — no cross-act cascade), same guarded reward-beat path, same
// QuestStates save wire. The save seam WRITES both logs' rows into the
// shipped v3 QuestStates list and the RESTORE feeds the same row list to
// both logs — each log skips the other's ids by shipped design ("a stale
// save must not poison the current table"), so this is DATA on the existing
// schema, zero save-file delta (F2). Act open/close are the table-driven
// QuestTable.ActTwo cards: ordered DialogueTable node ids queued through the
// shipped DLQ as GUARDED presentation beats (fromReward = the shipped "never
// DialogueShown evidence" guard — a card is presentation, exactly like a
// reward beat); one ACT_CARD marker per event is the REWARD_SHOWN idiom. NO
// cutscene system. _quests keeps its shipped identity (the ACT-ONE log — the
// shipped proofs read act-one ids through it after q_boss completes);
// act two reads go through QuestsAct2. S0's DialogueShown BUS emit is
// consumer-facing only (plan §S10): nothing here wires it into quest rules.
namespace LastAnimal.World;

public partial class WorldDirector
{
    private QuestLog _quests = null!;
    private QuestLog _questsAct1 = null!;
    private QuestLog _questsAct2 = null!;
    private bool _storyHooksEnabled = true;
    // Act-two lifecycle latches (S10). _act2Opened: the act-two arc is live
    // (started or restored mid-act); _act2Fed: its bus hooks subscribed ONCE
    // (re-subscribing would double-count observations); _act2CloseShown: the
    // close card played — a load is not a game beat, restore only syncs the
    // latches, it never replays cards.
    private bool _act2Opened;
    private bool _act2Fed;
    private bool _act2CloseShown;

    /// <summary>Read-only surface the runtime proofs read (proof-only, no
    /// logic). SHIPPED IDENTITY: the ACT-ONE log — quest_arc/quest_persist
    /// read act-one ids through it, including AFTER q_boss completes (an
    /// act-open reference swap would make those reads throw). Act two reads
    /// go through QuestsAct2.</summary>
    public QuestLog Quests => _quests;

    /// <summary>S10 proof-only read seam, no logic: the act-two log (the
    /// RuinsArc rows). Lives from boot; stays all-NotStarted until the act
    /// opens (act one's arc complete).</summary>
    public QuestLog QuestsAct2 => _questsAct2;

    /// <summary>Proof seam: is a wage currently due? (read-only, no logic).
    /// MC 3943 2g (ARCH W2): re-keyed from the singleton read to ANY roster
    /// follower — the roster-of-one answers exactly as before.</summary>
    public bool WageDueNow => _roster != null && _roster.AnyWageDue;

    /// <summary>Proof-only seam, no logic (MC 3915): the node the quest
    /// observer may credit as DIALOGUE OBSERVATION — the dialogue view's
    /// active node, or empty when nothing is open or the node on screen is a
    /// REWARD beat (a reward is never DialogueShown evidence). This is the
    /// seam the director itself wires as the DialogueShown provider below.</summary>
    public string QuestDialogueNodeNow =>
        _dialogue == null || _dialogue.ActiveNodeIsReward ? string.Empty : _dialogue.ActiveNode;

    /// <summary>Proof-only read seam, no logic (MC 3915): the dialogue view,
    /// so the runtime proof asserts reward beats on the REAL view state.</summary>
    public DialogueSystem DialogueUi => _dialogue;

    /// <summary>
    /// Root seam, called from _Ready right after the SaveLoadController is
    /// constructed (and before the boot EnterZone, so the meadow entry feeds
    /// the intro quest). Constructs the log, wires save seams, subscribes the
    /// hooks, and starts the arc's first row.
    /// </summary>
    private void InitStory()
    {
        _quests = _questsAct1 = new QuestLog(QuestTable.Default());
        _questsAct2 = new QuestLog(QuestTable.RuinsArc());   // S10: lives from boot, inert until opened
        _questsAct1.Changed += OnQuestChanged;
        _questsAct2.Changed += OnQuestChanged;   // same pure mapping; reward defs resolve across both tables
        _saveLoad.QuestStatesWrite = QuestSaveRows;
        _saveLoad.QuestStatesRestore = QuestRestoreRows;

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
            _quests.SetDialogueNodeProvider(() => QuestDialogueNodeNow);
        _quests.SetBossDeadProvider(() => _boss != null && _boss.IsDead);

        // Authoring order IS arc order: the table's first row starts with the
        // world; its objective (the boot zone entry) lands on the same frame.
        _quests.Start(_quests.Table.Entries[0].Id);
    }

    /// <summary>Log transition -> the batched C2 quest signals (pure mapping,
    /// no logic; restore paths emit nothing because the log stays silent).
    /// S10: the two act arms ride the SAME event (no per-log handler split) —
    /// act one's last row completing OPENS act two; act two's last row
    /// completing plays the close card. Both guards are pure state reads.</summary>
    private void OnQuestChanged(string questId, QuestStatus from, QuestStatus to)
    {
        switch (to)
        {
            case QuestStatus.Active: _bus.EmitQuestStarted(new QuestId(questId)); break;
            case QuestStatus.ObjectiveMet: _bus.EmitQuestObjective(new QuestId(questId)); break;
            case QuestStatus.Completed:
                {
                    _bus.EmitQuestCompleted(new QuestId(questId));
                    ShowRewardBeat(questId);
                    if (_questsAct1.IsArcComplete && !_act2Opened) OpenActTwo();
                    else if (_act2Opened && _questsAct2.IsArcComplete && !_act2CloseShown) ShowActTwoClose();
                    break;
                }
        }
    }

    /// <summary>MC 3915 reward beat — the row's Reward node is shown by the
    /// EXISTING dialogue system (ride, no second UI), flagged reward-shown so
    /// it never feeds DialogueShown evidence; the REWARD_SHOWN line is the
    /// gate marker (W3 GD.Print idiom, world/WorldDirector.cs).
    /// MC 10026.1 (owner ruling, DESIGN rev 4): beats QUEUE in the view in
    /// emission order and each is painted for a full dwell — "nothing gets
    /// lost". The marker stays state truth printed AT EMISSION regardless of
    /// render: a queued-but-not-yet-painted beat still prints REWARD_SHOWN.</summary>
    private void ShowRewardBeat(string questId)
    {
        if (!_storyHooksEnabled || _dialogue == null) return;   // gate-seam idiom (SetQuestHooksEnabled)
        // S10: reward defs resolve across BOTH act tables (act one first, the
        // shipped order; act two ids are unknown to the act-one table).
        if ((!_quests.Table.TryGetEntry(questId, out var def) || def == null) &&
            (!_questsAct2.Table.TryGetEntry(questId, out def) || def == null)) return;
        _dialogue.Show(def.Reward, fromReward: true);
        GD.Print($"REWARD_SHOWN {questId} -> {def.Reward}");
    }

    /// <summary>
    /// The WagePaid seam (plan §B 2c ≤3 lines there): fires the
    /// batched WagePaid signal ONLY on a landed settle (Pay() made SalaryDue
    /// fall — the machine's Needing->Following transition), keyed by the quest
    /// the wage SERVES — resolved per still-Active wage row (DA-verdict P3: a
    /// Completed row collects no settles; with no Active wage row the settle
    /// simply rides no quest signal). No settle, no signal — no second wage
    /// path.
    ///
    /// MC 3943 2g (ARCH W2, was a singleton read): the settle check moved to
    /// the CALLER — TickRoster (world/WorldDirector.Roster.cs) invokes this
    /// only for a follower whose own SalaryDue just fell, so EACH paying
    /// follower settles its own wage and emits its own WagePaid (q_wage counts
    /// events; target 1 stays fine). A stale singleton read here would mute
    /// every non-boot follower's settle.
    /// </summary>
    private void NotifyWageSettled()
    {
        if (_quests == null || !_storyHooksEnabled) return;
        // S10: the ONE wage path now resolves attribution across both act
        // logs — act one's still-Active wage row wins (shipped order), then
        // act two's. A settle with no Active wage row anywhere rides no quest
        // signal, exactly as shipped (no second wage path, ever).
        var wageQuestId = _quests.FindActiveIdByObjectiveKind(QuestObjectiveKind.WagePaid)
            ?? _questsAct2.FindActiveIdByObjectiveKind(QuestObjectiveKind.WagePaid);
        if (wageQuestId == null) return;   // no quest is being served right now
        _bus.EmitWagePaid(new QuestId(wageQuestId));
    }

    /// <summary>Gate seam: disable all quest observation (quest_neg negative
    /// control). One-line bool guard, no gameplay logic (design §4.2).
    /// S10: the act-two feeds re-check the SAME seam at delivery (DA-verdict
    /// P3 honest-no-op idiom), and with the hooks off act one never completes,
    /// so the act-open edge is unreachable — quest_neg stays the shipped red.</summary>
    public void SetQuestHooksEnabled(bool enabled) => _storyHooksEnabled = enabled;

    // ---- MC 10132 S10: ACT TWO (QuestTable.RuinsArc + ActCard, pure-log reuse) ----

    /// <summary>Save seam (v3 QuestStates, DATA not schema — F2 zero delta):
    /// act-one rows then act-two rows, each in its table's arc order. Before
    /// the act opens act two contributes NOTHING, so the shipped-era save is
    /// byte-identical (the save gate greps it).</summary>
    private List<string> QuestSaveRows()
    {
        var rows = _questsAct1.ToSaveRows();
        rows.AddRange(_questsAct2.ToSaveRows());
        return rows;
    }

    /// <summary>Restore seam: the same row list feeds BOTH logs — each skips
    /// the other's ids (shipped FromSaveRows semantics, "unknown id: skipped
    /// by design"), which is what keeps the two tables on ONE wire. The view
    /// reset is shipped (R9: queue never persisted). SyncActTwoFromRestore then
    /// decides whether this load finds the session inside act two (adopt the
    /// open state silently — a load is not a game beat, no card replays) or
    /// before it with act one already finished (open the act fresh).</summary>
    private void QuestRestoreRows(List<string> rows)
    {
        _questsAct1.FromSaveRows(rows);
        _questsAct2.FromSaveRows(rows);
        _dialogue?.ClearPresentation();
        SyncActTwoFromRestore();
    }

    private void SyncActTwoFromRestore()
    {
        if (_questsAct2.ToSaveRows().Count > 0)
        {
            // The save was taken mid-act (or past its end): adopt the open
            // state WITHOUT a card and WITHOUT a second feed subscription.
            AdoptActTwoOpen();
            if (_questsAct2.IsArcComplete) _act2CloseShown = true;
            return;
        }
        if (_act2Opened && !_questsAct1.IsArcComplete)
        {
            // Rollback: the loaded save predates the act — the restored logs
            // are the truth, act two is closed again (its evidence rewound
            // with the snapshot by FromSaveRows itself).
            _act2Opened = false;
            _act2CloseShown = false;
            return;
        }
        if (!_act2Opened && _questsAct1.IsArcComplete)
            OpenActTwo();   // fresh open: the act begins on this session's load
    }

    /// <summary>Act open (the act-one finale edge, or a restore that finds
    /// act one complete with act two unstarted): queue the table-driven open
    /// card behind whatever the finale beat is reading (the shipped DLQ drains
    /// in emission order, the uniform auto-close retires the box), subscribe
    /// the act-two feeds ONCE, and start the arc's first row — authoring order
    /// IS arc order, the shipped machine chains the rest.</summary>
    private void OpenActTwo()
    {
        if (_act2Opened) return;
        AdoptActTwoOpen();
        PlayActCard(QuestTable.ActTwo.OpenNodes, "open");
        _questsAct2.Start(_questsAct2.Table.Entries[0].Id);
    }

    /// <summary>The open-state latch + one-time feed subscription (shared by
    /// the event edge and the restore adoption — no duplicated wiring).</summary>
    private void AdoptActTwoOpen()
    {
        _act2Opened = true;
        if (_act2Fed) return;
        _act2Fed = true;
        // Act-two evidence starts at the open (a fresh log's counters —
        // act-one events never feed it, no cross-act cascade). Every lambda
        // re-checks the gate seam at DELIVERY (DA-verdict P3 idiom). The
        // BossDead/DialogueShown provider seams are deliberately NOT wired:
        // RuinsArc authors neither kind (the boss seam would ride the act-one
        // corpse; an act card is not a DialogueShown fact — QuestTable header).
        _bus.DnaSpoken += _ => { if (_storyHooksEnabled && _act2Opened) _questsAct2.ObserveSpoken(); };
        _bus.DnaExtracted += _ => { if (_storyHooksEnabled && _act2Opened) _questsAct2.ObserveKill(); };
        _bus.WagePaid += questId => { if (_storyHooksEnabled && _act2Opened) _questsAct2.ObserveWagePaid(questId); };
        _ecosystem.ZoneEntered += (zoneId, _) =>
        { if (_storyHooksEnabled && _act2Opened) _questsAct2.ObserveZoneEntered(zoneId); };
    }

    /// <summary>The act-two finale: queue the table-driven close card (the
    /// finale reward beat was queued in the same arm earlier — emission order
    /// IS the drain order, the shipped DLQ contract).</summary>
    private void ShowActTwoClose()
    {
        _act2CloseShown = true;
        PlayActCard(QuestTable.ActTwo.CloseNodes, "close");
    }

    /// <summary>Play one act card as GUARDED presentation beats through the
    /// EXISTING view + DLQ (fromReward: the shipped "never DialogueShown
    /// evidence" flag — a card is presentation, like a reward beat; a direct
    /// Show would instead STEAL the box from the mid-read finale beat). Same
    /// gate seam as the reward path. One ACT_CARD marker per event, printed at
    /// emission regardless of render (REWARD_SHOWN idiom, MC 3915).</summary>
    private void PlayActCard(IReadOnlyList<string> nodes, string edge)
    {
        if (!_storyHooksEnabled || _dialogue == null) return;
        foreach (var node in nodes)
            _dialogue.Show(node, fromReward: true);   // R2/R4: same-tick pair appends in order
        GD.Print($"ACT_CARD {QuestTable.ActTwo.Id} {edge} -> {string.Join(",", nodes)}");
    }
}
