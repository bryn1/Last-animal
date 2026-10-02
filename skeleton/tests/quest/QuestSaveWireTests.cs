using System;
using System.Collections.Generic;
using System.IO;
using Xunit;
using LastAnimal.Save;
using LastAnimal.Story;

// Last Animal — QuestStates save-wire tests (MC 3904 stage 2c, code, 2026-10-02).
//
// The persistence half of the quest core: GameState.QuestStates carries one
// "id:status" row per started quest (the v3 field authored by 2b; the map
// amendment 2026-10-02 wires it through the live save path). These tests run
// the REAL SaveSystem schema guard over a temp-dir store (same engine-free
// pattern as tests/save) and pin:
//   * round-trip: driven log -> rows -> save -> load -> fresh log = same
//     statuses (including a resting objective_met row);
//   * load REPLACES: a row missing from the save is NotStarted again;
//   * restore REWINDS THE EVIDENCE (DA P1 fix): post-load observations
//     re-earn progress; stale live counters cascade nothing;
//   * malformed rows throw (loud, never half-loaded);
//   * unknown ids are skipped (2d content swaps must not poison a load).
namespace LastAnimal.Tests.Quest;

public class QuestSaveWireTests
{
    private sealed class TempStore : ISaveStore
    {
        private readonly string _dir = Path.Combine(Path.GetTempPath(),
            "lastanimal-quest-tests-" + Guid.NewGuid().ToString("N"));
        public string SavePath => Path.Combine(_dir, SaveSystem.SaveFileName);
        public void WriteAllText(string path, string contents)
        {
            Directory.CreateDirectory(_dir);
            File.WriteAllText(path, contents);
        }
        public string ReadAllText(string path) => File.ReadAllText(path);
        public bool Exists(string path) => File.Exists(path);
    }

    [Fact]
    public void QuestStates_RoundTripThroughTheRealSaveSystem()
    {
        var log = new QuestLog(QuestTable.Default());
        log.SetBossDeadProvider(() => false);
        log.Start("q_intro");
        log.ObserveZoneEntered("meadow");   // q_intro completed, q_speak active
        log.ObserveSpoken();                // q_speak completed, q_wage active

        var state = new GameState { QuestStates = log.ToSaveRows() };
        // Representative-style vocabulary check on the wire itself.
        Assert.Contains("q_intro:completed", state.QuestStates);
        Assert.Contains("q_speak:completed", state.QuestStates);
        Assert.Contains("q_wage:active", state.QuestStates);
        Assert.DoesNotContain("q_kills:not_started", state.QuestStates);

        var store = new TempStore();
        Assert.True(SaveSystem.Save(state, store));
        var loaded = SaveSystem.Load(store);
        Assert.NotNull(loaded);

        var revived = new QuestLog(QuestTable.Default());
        revived.FromSaveRows(loaded!.QuestStates);
        Assert.Equal(QuestStatus.Completed, revived.Status("q_intro"));
        Assert.Equal(QuestStatus.Completed, revived.Status("q_speak"));
        Assert.Equal(QuestStatus.Active, revived.Status("q_wage"));
        Assert.Equal(QuestStatus.NotStarted, revived.Status("q_kills"));
        Assert.Equal(QuestStatus.NotStarted, revived.Status("q_boss"));
        // The restored log keeps playing: the arc continues from where it was.
        revived.ObserveWagePaid("q_wage");
        Assert.Equal(QuestStatus.Active, revived.Status("q_kills"));
    }

    [Fact]
    public void QuestStates_RestoreCarriesTheObjectiveMetVocabulary()
    {
        var revived = new QuestLog(QuestTable.Default());
        revived.FromSaveRows(new[] { "q_intro:completed", "q_speak:objective_met" });
        Assert.Equal(QuestStatus.ObjectiveMet, revived.Status("q_speak"));
        Assert.Equal(QuestStatus.Completed, revived.Status("q_intro"));
    }

    [Fact]
    public void Load_ReplacesState_RowAbsentFromSaveIsNotStartedAgain()
    {
        var log = new QuestLog(QuestTable.Default());
        log.SetBossDeadProvider(() => false);
        log.Start("q_intro");
        log.ObserveZoneEntered("meadow");       // q_intro completed, q_speak active
        log.FromSaveRows(new List<string> { "q_intro:completed" });
        Assert.Equal(QuestStatus.Completed, log.Status("q_intro"));
        Assert.Equal(QuestStatus.NotStarted, log.Status("q_speak"));  // replaced away
        Assert.Equal(QuestStatus.NotStarted, log.Status("q_wage"));
    }

    [Fact]
    public void Restore_EmitsNoChangedEvents_ARestoreIsNotAGameBeat()
    {
        var log = new QuestLog(QuestTable.Default());
        int beats = 0;
        log.Changed += (_, _, _) => beats++;
        log.FromSaveRows(new[] { "q_intro:completed", "q_speak:objective_met" });
        Assert.Equal(0, beats);
    }

    [Theory]
    [InlineData("garbage")]                // no separator
    [InlineData(":active")]                // empty id
    [InlineData("q_intro:")]               // empty status
    [InlineData("q_intro:completed:extra")] // double separator
    [InlineData("q_intro:winning")]         // unknown status token
    public void MalformedWireRows_Throw(string row)
    {
        var log = new QuestLog(QuestTable.Default());
        var ex = Assert.Throws<ArgumentException>(() => log.FromSaveRows(new[] { row }));
        Assert.Contains(row, ex.Message);   // the bad row is named in the error
    }

    [Fact]
    public void UnknownQuestIdInWireRows_IsSkippedByDesign()
    {
        // 2d may swap content; a stale save naming a retired id must not
        // poison the current table (nor resurrect a phantom row).
        var log = new QuestLog(QuestTable.Default());
        log.FromSaveRows(new[] { "q_old_drafted_arc:completed", "q_intro:active" });
        Assert.Equal(QuestStatus.Active, log.Status("q_intro"));
        Assert.Equal(QuestStatus.NotStarted, log.Status("q_speak"));
    }

    // --- evidence rewind on restore (DA-verdict P1, ed4a5b2 review) ----------
    // The save persists statuses ONLY; FromSaveRows must therefore also clear
    // the in-memory evidence counters. Without it, the live session's stale
    // counters fast-forward the arc: one post-load observation runs ONE
    // EvaluatePass that chains met->complete through the table, silently
    // re-skipping gameplay the loaded save says was never played. These tests
    // pin the rewind through the two counter shapes (kill counter + spoken
    // counter); the deliberate in-session catch-up (QuestLogTests
    // LateObjective_StartTimeReEvaluationCatchesUp) stays green — that catch-
    // up uses facts observed AFTER Start, never a restore.

    [Fact]
    public void FromSaveRows_RewindsEvidence_OnePostLoadKillDoesNotCascadeTheKillsRow()
    {
        // DA P1 reproduction: evidence accrues live, the save leaves the kills
        // row Active at 0/4, the player loads and observes ONE kill. Stale
        // counters (pre-fix) would read 4/4 and cascade q_kills met->complete
        // and q_boss NotStarted->Active in the same pass.
        var log = new QuestLog(QuestTable.Default());
        log.SetBossDeadProvider(() => false);
        log.Start("q_intro");
        log.ObserveKill(); log.ObserveKill(); log.ObserveKill();   // live 3/4
        int beatsAfterLoad = 0;
        log.Changed += (_, _, _) => beatsAfterLoad++;
        log.FromSaveRows(new[]
        {
            "q_intro:completed", "q_speak:completed", "q_wage:completed", "q_kills:active",
        });
        log.ObserveKill();   // the ONE post-load observation
        Assert.Equal(QuestStatus.Active, log.Status("q_kills"));      // 1/4, re-earned
        Assert.Equal(QuestStatus.NotStarted, log.Status("q_boss"));   // no chain
        Assert.Equal(0, beatsAfterLoad);                              // no cascade beats
    }

    [Fact]
    public void FromSaveRows_RewindsEvidence_PostLoadFactDoesNotResatisfyARestoredSpokenRow()
    {
        // The spoken-counter shape: one live speak, a save that leaves q_speak
        // ACTIVE; after the load an UNRELATED fact (a zone entry) must not
        // re-meet q_speak off the stale counter — but a REAL later speak still
        // legitimately meets it (catch-up through genuine observation holds).
        var log = new QuestLog(QuestTable.Default());
        log.Start("q_intro");
        log.ObserveSpoken();                     // live evidence, row not yet started
        log.FromSaveRows(new[] { "q_intro:completed", "q_speak:active" });
        log.ObserveZoneEntered("nowhere-quest"); // neutral trigger for one pass
        Assert.Equal(QuestStatus.Active, log.Status("q_speak"));      // stale spoken gone
        log.ObserveSpoken();                     // the real post-load fact
        Assert.Equal(QuestStatus.Completed, log.Status("q_speak"));   // legitimately earned
        Assert.Equal(QuestStatus.Active, log.Status("q_wage"));
    }
}
