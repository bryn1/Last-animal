// Last Animal — the ACT-CHAIN restore-sync machine (authored S10 MC 10132 fix
// cycle 2 as "ActTwoSync", F-DA2, DA-verdict 0ad8c81f; renamed MC 10273 S19 —
// the arms are (prevAct, thisAct)-parameterized, so ACT THREE rides the SAME
// machine over (act2, act3): one implementation per concern, no second sync
// machine beside this one). The arms that decide what a LOAD means for an
// act's lifecycle are quest-state logic, so they live in the pure core
// (I1, same contract as QuestLog). world/WorldDirector.Story.cs
// SyncActTwoFromRestore + Story.Act3.cs SyncActThreeFromRestore stay the
// named restore-sync seams: each runs this machine for its act and executes
// the returned action's engine-side halves (feed subscribe, cards, latches)
// and nothing else — the shipped silent-restore semantics hold (FromSaveRows
// emits no Changed; only the F-DA2 re-Start re-fires the shipped machine,
// exactly as the shipped fresh open does).
//
// F-DA2: the SilentReStart arm is the fourth state the original three-arm
// sync missed — latch open + prev act complete + ZERO this-act rows (a
// migration-shaped save: act-one-complete, act-two-empty — loaded AGAIN in
// one session) matched no arm and stranded the act open-but-unstarted for
// the session. Re-Start the arc silently: the open card already played, and
// a load is not a game beat.
namespace LastAnimal.Story;

/// <summary>What the restore-sync arms decided. The director executes the
/// engine-side follow-up for Adopt/Rollback/FreshOpen; SilentReStart and None
/// need none — Run applied every pure effect.</summary>
public enum ActChainSyncAction
{
    None,
    /// <summary>Snapshot carries this-act rows: adopt the open state silently,
    /// no card and no second feed subscription.</summary>
    Adopt,
    /// <summary>Snapshot predates the act: the restored logs are the truth,
    /// the act closes again (the caller clears its open latch).</summary>
    Rollback,
    /// <summary>Prev act complete, session not yet in this act: fresh open —
    /// the caller plays the open card and starts the arc.</summary>
    FreshOpen,
    /// <summary>F-DA2 strand: latch open + prev act complete + zero this-act
    /// rows — Run re-Started the arc's first row SILENTLY (no card: a load
    /// is not a beat, the open card already played this session).</summary>
    SilentReStart,
}

public static class ActChainSync
{
    /// <summary>Run the restore-sync arms against two pure logs (the previous
    /// act and the act being synced). <paramref name="opened"/> is the
    /// director's latch snapshot; <paramref name="closeShown"/> is owned by
    /// the arms that complete or rewind the act. Every arm that reaches Start
    /// does so only with the log all-NotStarted (ToSaveRows zero under
    /// FromSaveRows' full-snapshot rewind), so Start cannot throw — the
    /// shipped arm-3 argument, reused.</summary>
    public static ActChainSyncAction Run(QuestLog prevAct, QuestLog thisAct, bool opened, ref bool closeShown)
    {
        if (thisAct.ToSaveRows().Count > 0)
        {
            if (thisAct.IsArcComplete) closeShown = true;
            // MC 10281 (DA-verdict 22ac1aa8 P3-2): an OLDER mid-act save on
            // this arm predates the close card the session already showed —
            // the adopt REWINDS the latch too, so the close can play again.
            else closeShown = false;
            return ActChainSyncAction.Adopt;
        }
        if (opened && !prevAct.IsArcComplete)
        {
            closeShown = false;   // the act is closed again — with the rollback
            return ActChainSyncAction.Rollback;
        }
        if (!opened && prevAct.IsArcComplete)
            return ActChainSyncAction.FreshOpen;
        if (opened && prevAct.IsArcComplete)
        {
            // The act is live again from row one: a later completion may play
            // the close card, so the shown-latch clears with the rewind.
            closeShown = false;
            thisAct.Start(thisAct.Table.Entries[0].Id);
            return ActChainSyncAction.SilentReStart;
        }
        return ActChainSyncAction.None;
    }
}
