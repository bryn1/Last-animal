using System;
using System.Collections.Generic;
using Xunit;
using LastAnimal.Save;

// Last Animal — M11 save-progression tests (MC 890.15, gunilla, 2026-09-06).
//
// C14 contract tests + the Phase-12 DoD round-trip. All tests run headless:
// SaveSystem.Load/Save are pure logic (I3); the store is the engine-free
// TempDirSaveStore (System.IO under the OS temp dir), so nothing touches the
// repo / group-readable working tree. Each test gets a fresh temp store in a
// brand-new directory, so the suite is order-independent and repeatable.
namespace LastAnimal.Tests.Save;

public class SaveSystemTests
{
    private TempDirSaveStore NewStore() => new TempDirSaveStore();

    // ------------------------------------------------------------------
    // C14: Save -> Load round-trip preserves a representative GameState
    // ------------------------------------------------------------------

    [Fact]
    public void RoundTrip_PreservesRepresentativeGameState()
    {
        var store = NewStore();
        var original = GameState.Representative();

        bool saved = SaveSystem.Save(original, store);
        Assert.True(saved, "Save should succeed to a temp store");

        var loaded = SaveSystem.Load(store);
        Assert.NotNull(loaded);
        Assert.Equal(original.ZoneId, loaded.ZoneId);
        Assert.Equal(original.Progression, loaded.Progression);
        // v3 (MC 3901 2b): the roster-of-one replaces the v2 companion pair.
        Assert.Equal(original.Followers.Count, loaded.Followers.Count);
        Assert.Equal(original.Followers[0].EntityId, loaded.Followers[0].EntityId);
        Assert.Equal(original.Followers[0].Loyalty, loaded.Followers[0].Loyalty);
        Assert.Equal(original.QuestStates, loaded.QuestStates);
        Assert.Equal(original.Manna, loaded.Manna);
        Assert.Equal(original.Version, loaded.Version);
        Assert.Equal(original.LearnedDnaCounters, loaded.LearnedDnaCounters);
        Assert.Equal(original.DnaEventCount, loaded.DnaEventCount);
        Assert.Equal(original.PlayerHealth, loaded.PlayerHealth);
        // MC 1405 N5: the player position rides the snapshot.
        Assert.True(loaded.HasPlayerPosition);
        Assert.Equal(original.PlayerX, loaded.PlayerX);
        Assert.Equal(original.PlayerY, loaded.PlayerY);
        Assert.Equal(original.PlayerZ, loaded.PlayerZ);
    }

    [Fact]
    public void RoundTrip_WritesOutsideRepoTree()
    {
        // The save file must live in the OS temp dir (or user:// in the engine),
        // NOT the shared /srv/workspace tree. Prove the store path is under the
        // temp dir and the file exists there.
        var store = NewStore();
        Assert.True(SaveSystem.Save(GameState.Representative(), store));
        Assert.True(System.IO.File.Exists(store.SavePath));
        Assert.StartsWith(System.IO.Path.GetTempPath(), store.SavePath);
    }

    [Fact]
    public void Save_StampsCurrentVersion()
    {
        var store = NewStore();
        var state = GameState.Representative();
        state.Version = 999; // deliberately wrong
        SaveSystem.Save(state, store);
        Assert.Equal(SaveSystem.CurrentVersion, state.Version);
    }

    [Fact]
    public void PersistsDnaCounters_LoyaltyEmotion_Progression_Zone()
    {
        // C14: persists DNA counters (M02), emotion (M03 — carried by
        // follower loyalty; the derivable EmotionState label was removed,
        // MC 1405 N7), progression, zone.
        var store = NewStore();
        var state = new GameState
        {
            LearnedDnaCounters = new List<int> { 3, 0, 1, 3, 2 },
            Followers = new List<FollowerEntry> { new FollowerEntry { EntityId = 7, Loyalty = 12 } },
            Progression = 5,
            ZoneId = "ruins"
        };
        SaveSystem.Save(state, store);
        var loaded = SaveSystem.Load(store);
        Assert.NotNull(loaded);
        Assert.Equal(new List<int> { 3, 0, 1, 3, 2 }, loaded.LearnedDnaCounters);
        Assert.Single(loaded.Followers);
        Assert.Equal(12, loaded.Followers[0].Loyalty);
        Assert.Equal(5, loaded.Progression);
        Assert.Equal("ruins", loaded.ZoneId);
    }

    [Fact]
    public void V3Persists_QuestStates_Manna_FollowersList()
    {
        // MC 3901 2b: the ONE ratified v2->v3 break adds exactly three fields.
        // QuestStates (List<string> "id:status"), Manna (int), Followers
        // (List of {EntityId, Loyalty}) must each survive a save/load
        // round-trip. Asserts the EXACT values, so a dropped field or wrong
        // element goes red.
        var store = NewStore();
        var state = new GameState
        {
            QuestStates = new List<string> { "intro:active", "wage:completed", "finale:locked" },
            Manna = 37,
            Followers = new List<FollowerEntry>
            {
                new FollowerEntry { EntityId = 11, Loyalty = 90 },
                new FollowerEntry { EntityId = 12, Loyalty = 4 },
            }
        };
        Assert.True(SaveSystem.Save(state, store));
        var loaded = SaveSystem.Load(store);
        Assert.NotNull(loaded);
        Assert.Equal(new List<string> { "intro:active", "wage:completed", "finale:locked" }, loaded.QuestStates);
        Assert.Equal(37, loaded.Manna);
        Assert.Equal(2, loaded.Followers.Count);
        Assert.Equal(11, loaded.Followers[0].EntityId);
        Assert.Equal(90, loaded.Followers[0].Loyalty);
        Assert.Equal(12, loaded.Followers[1].EntityId);
        Assert.Equal(4, loaded.Followers[1].Loyalty);
    }

    [Fact]
    public void V3HasNoLearnedMutationsField()
    {
        // MC 3901 2b (plan §G D2/D4): the v3 schema deliberately carries NO
        // LearnedMutations field — the unlock authority recomputes from the
        // round-trip-stable LearnedDnaCounters consensus. Proof-of-absence:
        // serialize the representative state and assert the field name is
        // absent from the wire form, and no v3 field is named "LearnedMutations".
        var json = System.Text.Json.JsonSerializer.Serialize(GameState.Representative());
        Assert.DoesNotContain("LearnedMutations", json);
        // And the v2 single-companion fields are gone from the wire form too:
        Assert.DoesNotContain("CompanionEntityId", json);
        Assert.DoesNotContain("CompanionLoyalty", json);
    }

    [Fact]
    public void RoundTrip_PreservesPlayerHealth()
    {
        // MC 1348 N1: the save must capture player health so a load rescues a
        // dead player (F9 from the death state restores a live HP value).
        var store = NewStore();
        var original = GameState.Representative();
        original.PlayerHealth = 42;

        Assert.True(SaveSystem.Save(original, store));
        var loaded = SaveSystem.Load(store);
        Assert.NotNull(loaded);
        Assert.Equal(42, loaded.PlayerHealth);
    }

    [Fact]
    public void RoundTrip_PreservesPlayerPosition()
    {
        // MC 1405 N5: the save must capture the player's world position so a
        // load puts the player back where they stood.
        var store = NewStore();
        var original = GameState.Representative();
        original.PlayerX = 12.5f;
        original.PlayerY = 0.25f;
        original.PlayerZ = -7.75f;

        Assert.True(SaveSystem.Save(original, store));
        var loaded = SaveSystem.Load(store);
        Assert.NotNull(loaded);
        Assert.True(loaded!.HasPlayerPosition);
        Assert.Equal(12.5f, loaded.PlayerX);
        Assert.Equal(0.25f, loaded.PlayerY);
        Assert.Equal(-7.75f, loaded.PlayerZ);
    }

    [Fact]
    public void Load_OldSaveWithoutPosition_HasNoPositionFlag()
    {
        // MC 1405 N5 backward compatibility: a pre-N5 save carries no position
        // fields. It must deserialize with HasPlayerPosition == false so the
        // load path keeps the player where they are — never a teleport to
        // (0,0,0) from the float defaults.
        var store = NewStore();
        store.WriteAllText(store.SavePath,
            "{\"Version\": " + SaveSystem.CurrentVersion + ", \"ZoneId\": \"canyon\"}");
        var loaded = SaveSystem.Load(store);
        Assert.NotNull(loaded);
        Assert.False(loaded!.HasPlayerPosition);
        Assert.Equal("canyon", loaded.ZoneId);
    }

    // ------------------------------------------------------------------
    // C14: versioned schema guard
    // ------------------------------------------------------------------

    [Fact]
    public void Load_RejectsOutOfDateSave_AndLogsUpgradePath()
    {
        var store = NewStore();
        SaveSystem.Save(GameState.Representative(), store);
        // Rewrite the on-disk Version header to an older schema.
        Assert.True(SaveSystem.RewriteSavedVersionForTest(store, SaveSystem.CurrentVersion - 1));

        SaveSystem.Log.Clear();
        var loaded = SaveSystem.Load(store);
        Assert.Null(loaded); // outdated save rejected

        var logged = string.Join(" | ", SaveSystem.Log);
        Assert.Contains("REJECTED", logged);
        Assert.Contains("Upgrade path", logged);
        Assert.Contains("m11_upgrade", logged);
    }

    [Fact]
    public void Load_RejectsNewerThanCurrent_AndLogsUpgradePath()
    {
        var store = NewStore();
        SaveSystem.Save(GameState.Representative(), store);
        SaveSystem.RewriteSavedVersionForTest(store, SaveSystem.CurrentVersion + 1);

        SaveSystem.Log.Clear();
        var loaded = SaveSystem.Load(store);
        Assert.Null(loaded);
        Assert.Contains("newer build", string.Join(" | ", SaveSystem.Log));
    }

    [Fact]
    public void Load_RejectsV2ShapedSave_AndLogsUpgradePath()
    {
        // MC 3901 2b (owner ruling D2 RATIFIED): a real v2-shaped save —
        // literal Version 2 with the removed single-companion fields — is
        // REJECTED, never silently migrated. Hardcoding 2 is correct here
        // (it names the OLD schema under test); only the NEWER-than guard
        // must stay CurrentVersion-relative (§G D9).
        Assert.Equal(3, SaveSystem.CurrentVersion); // this test speaks to the v3 schema
        var store = NewStore();
        store.WriteAllText(store.SavePath,
            "{\"Version\": 2, \"ZoneId\": \"canyon\", \"CompanionEntityId\": 7, \"CompanionLoyalty\": 84}");

        SaveSystem.Log.Clear();
        var loaded = SaveSystem.Load(store);
        Assert.Null(loaded);

        var logged = string.Join(" | ", SaveSystem.Log);
        Assert.Contains("REJECTED", logged);
        Assert.Contains("found v2", logged);
        Assert.Contains("Upgrade path", logged);
    }

    [Fact]
    public void Load_ReturnsNullAndWelcomesFirstRun_WhenNoSaveExists()
    {
        var store = NewStore(); // nothing saved yet
        SaveSystem.Log.Clear();
        var loaded = SaveSystem.Load(store);
        Assert.Null(loaded);
        Assert.Contains("first run", string.Join(" | ", SaveSystem.Log));
    }

    [Fact]
    public void Save_RejectsNullState()
    {
        var store = NewStore();
        Assert.False(SaveSystem.Save(null!, store));
    }
}
