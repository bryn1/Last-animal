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
}
