using Godot;
using LastAnimal.Story;

// Last Animal — stage S19 story ACT THREE wiring (MC 10273, code, 2026-10-07).
//
// Partial-class half of WorldDirector: the act-three lifecycle riding the
// SHIPPED S10 act mechanism only (RULING-8, board line MC 10026 append #9
// row 895: "R8 zone four + act three = YES -> S18 + S19 proceed", owner
// chat "Rec on all."). A third
// pure QuestLog plays QuestTable.ActThreeArc() — the final zone-4 chain on
// the "hollow" ground S18 shipped — after the ACT-TWO arc completes. Exact
// mirror of the Story.cs act-two seams: open (finale edge or restore
// FreshOpen) subscribes the feeds ONCE and starts the arc's first row; the
// restore decision arms are the SAME pure ActChainSync machine, run over
// (act2, act3) — the mid-quest restore edge exists (a save taken mid-act-
// three comes back through the Adopt arm silently, no card replay). The act-
// completion condition — ActThreeArc.IsArcComplete — prints the named
// ACT_THREE_COMPLETE marker at emission (state truth, REWARD_SHOWN idiom)
// and plays the table-driven QuestTable.ActThree close card through the same
// DLQ-guarded path. Save path: act-three rows join the shipped v3
// QuestStates list as DATA (zero save fields, R7 runtime-only stands);
// zero new bus signals (the act arms ride the shipped five, census 15);
// zero new scene or UI surface.
namespace LastAnimal.World;

public partial class WorldDirector
{
    // Act-three log + lifecycle latches — mirror of the act-two trio in
    // Story.cs (opened: arc live; fed: bus hooks subscribed ONCE;
    // closeShown: the close card played — a load is not a game beat).
    private QuestLog _questsAct3 = null!;
    private bool _act3Opened;
    private bool _act3Fed;
    private bool _act3CloseShown;

    /// <summary>S19 proof-only read seam, no logic: the act-three log (the
    /// ActThreeArc rows). Lives from boot; stays all-NotStarted until the
    /// act opens (the ACT-TWO arc complete).</summary>
    public QuestLog QuestsAct3 => _questsAct3;

    /// <summary>Act open (the act-two finale edge, or a restore that finds
    /// act two complete with act three un-started): queue the table-driven
    /// open card behind the act-two close pair (emission order IS the shipped
    /// DLQ drain order), subscribe the act-three feeds ONCE, and start the
    /// arc's first row — authoring order IS arc order, the shipped machine
    /// chains the rest. S10 OpenActTwo idiom, verbatim shape.</summary>
    private void OpenActThree()
    {
        if (_act3Opened) return;
        AdoptActThreeOpen();
        PlayActCard(QuestTable.ActThree.Id, QuestTable.ActThree.OpenNodes, "open");
        _questsAct3.Start(_questsAct3.Table.Entries[0].Id);
    }

    /// <summary>The open-state latch + one-time feed subscription (shared by
    /// the event edge and the restore adoption — no duplicated wiring; the
    /// AdoptActTwoOpen mirror). Act-three evidence starts at the open; every
    /// lambda re-checks the gate seam at DELIVERY (DA-verdict P3 idiom). The
    /// BossDead/DialogueShown provider seams are deliberately NOT wired:
    /// ActThreeArc authors neither kind (QuestTable.ActThreeArc header).</summary>
    private void AdoptActThreeOpen()
    {
        _act3Opened = true;
        if (_act3Fed) return;
        _act3Fed = true;
        _bus.DnaSpoken += _ => { if (_storyHooksEnabled && _act3Opened) _questsAct3.ObserveSpoken(); };
        _bus.DnaExtracted += _ => { if (_storyHooksEnabled && _act3Opened) _questsAct3.ObserveKill(); };
        _bus.WagePaid += questId => { if (_storyHooksEnabled && _act3Opened) _questsAct3.ObserveWagePaid(questId); };
        _ecosystem.ZoneEntered += (zoneId, _) =>
        { if (_storyHooksEnabled && _act3Opened) _questsAct3.ObserveZoneEntered(zoneId); };
    }

    /// <summary>The act-three finale — THE ACT-COMPLETION CONDITION: the
    /// final zone-4 chain (QuestTable.ActThreeArc) completed. Prints the
    /// named ACT_THREE_COMPLETE state-truth marker at emission (the
    /// REWARD_SHOWN idiom — printed regardless of render order) and queues
    /// the table-driven close card (the story's close; the finale reward
    /// beat was queued in the same arm earlier — emission order IS the drain
    /// order, the shipped DLQ contract).</summary>
    private void ShowActThreeClose()
    {
        _act3CloseShown = true;
        GD.Print("ACT_THREE_COMPLETE — the final zone-4 chain completed the act-three arc (story close)");
        PlayActCard(QuestTable.ActThree.Id, QuestTable.ActThree.CloseNodes, "close");
    }

    /// <summary>Restore sync for act three (mirror of SyncActTwoFromRestore):
    /// the arms are the pure ActChainSync machine run over (act2, act3) —
    /// this seam is its executor and owns only the engine-side halves: the
    /// one-time feed subscription, the open card, and the latches. Runs
    /// AFTER SyncActTwoFromRestore so its prevAct read is the RESTORED,
    /// sync-resolved act-two log (an act-two rewind rolls act three back
    /// through its own Rollback arm — the chain composes).</summary>
    private void SyncActThreeFromRestore()
    {
        switch (ActChainSync.Run(_questsAct2, _questsAct3, _act3Opened, ref _act3CloseShown))
        {
            case ActChainSyncAction.Adopt:
                AdoptActThreeOpen();   // adopt silently: no card, no re-subscribe
                break;
            case ActChainSyncAction.Rollback:
                _act3Opened = false;   // restored logs are the truth (closeShown
                break;                 // was cleared by the arm)
            case ActChainSyncAction.FreshOpen:
                OpenActThree();   // fresh open: the act begins on this session's load
                break;
            // SilentReStart / None: every effect is pure and Run applied it.
        }
    }
}
