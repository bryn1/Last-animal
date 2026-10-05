// Last Animal — S10 ACT-TWO restore-sync machine (MC 10132 fix cycle 2,
// F-DA2, DA-verdict 0ad8c81f). The arms that decide what a LOAD means for the
// act-two lifecycle are quest-state logic, so they live in the pure core
// (I1, same contract as QuestLog). world/WorldDirector.Story.cs
// SyncActTwoFromRestore stays the named restore-sync seam: it runs this
// machine and executes the returned action's engine-side halves (feed
// subscribe, cards, latches) and nothing else — the shipped silent-restore
// semantics hold (FromSaveRows emits no Changed; only the F-DA2 re-Start
// re-fires the shipped machine, exactly as the shipped fresh open does).
//
// F-DA2: the SilentReStart arm is the fourth state the original three-arm
// sync missed — latch open + act one complete + ZERO act-two rows (a
// migration-shaped save: act-one-complete, act-two-empty — loaded AGAIN in
// one session) matched no arm and stranded the act open-but-unstarted for
// the session. Re-Start the arc silently: the open card already played, and
// a load is not a game beat.
namespace LastAnimal.Story;

/// <summary>What the restore-sync arms decided. The director executes the
/// engine-side follow-up for Adopt/Rollback/FreshOpen; SilentReStart and None
/// need none — Run applied every pure effect.</summary>
public enum ActTwoSyncAction
{
    None,
    /// <summary>Snapshot carries act-two rows: adopt the open state silently,
    /// no card and no second feed subscription.</summary>
    Adopt,
    /// <summary>Snapshot predates the act: the restored logs are the truth,
    /// the act closes again (the caller clears its open latch).</summary>
    Rollback,
    /// <summary>Act one complete, session not yet in act two: fresh open —
    /// the caller plays the open card and starts the arc.</summary>
    FreshOpen,
    /// <summary>F-DA2 strand: latch open + act one complete + zero act-two
    /// rows — Run re-Started the arc's first row SILENTLY (no card: a load
    /// is not a beat, the open card already played this session).</summary>
    SilentReStart,
}

public static class ActTwoSync
{
    /// <summary>Run the restore-sync arms against the two pure logs.
    /// <paramref name="opened"/> is the director's latch snapshot;
    /// <paramref name="closeShown"/> is owned by the arms that complete or
    /// rewind the act. Every arm that reaches Start does so only with the
    /// log all-NotStarted (ToSaveRows zero under FromSaveRows' full-snapshot
    /// rewind), so Start cannot throw — the shipped arm-3 argument, reused.</summary>
    public static ActTwoSyncAction Run(QuestLog act1, QuestLog act2, bool opened, ref bool closeShown)
    {
        if (act2.ToSaveRows().Count > 0)
        {
            if (act2.IsArcComplete) closeShown = true;
            return ActTwoSyncAction.Adopt;
        }
        if (opened && !act1.IsArcComplete)
        {
            closeShown = false;   // act two is closed again — with the rollback
            return ActTwoSyncAction.Rollback;
        }
        if (!opened && act1.IsArcComplete)
            return ActTwoSyncAction.FreshOpen;
        if (opened && act1.IsArcComplete)
        {
            // The act is live again from row one: a later completion may play
            // the close card, so the shown-latch clears with the rewind.
            closeShown = false;
            act2.Start(act2.Table.Entries[0].Id);
            return ActTwoSyncAction.SilentReStart;
        }
        return ActTwoSyncAction.None;
    }
}
