using Godot;
using LastAnimal.Core;
using LastAnimal.Save;
using System.Linq;

// Last Animal — T3b runtime integration proof, SAVE mode stages (MC 1256.10).
//
// Partial-class half of RuntimeIntegrationProof carrying the save/load stage
// bodies (stages 20/21/22) and the save_bad_version schema-guard stage (30).
// Extracted verbatim from RuntimeIntegrationProof.cs — no behaviour change;
// see the main file's header for the mode contract.
public partial class RuntimeIntegrationProof : SceneTree
{
    // MC 1405 N5: the player's world position at save time, plus the mutated
    // (teleported) position — the load assert is non-vacuous only if the two
    // differ. Captured in RunSaveStage/RunMutateStage, asserted in
    // RunLoadStage (LOAD_RESTORED_POSITION).
    private Godot.Vector3 _playerPosBeforeSave;
    private Godot.Vector3 _playerPosMutated;

    // MC 3901 2b (v3): the follower entity id at save time — the v3 roster
    // restores through Followers[0], so the LOAD_RESTORED_FOLLOWER leg must
    // compare against what the save actually captured (boot companion id 7,
    // WorldDirector.cs SetCompanion — non-vacuous, not the -1 sentinel).
    private int _followerIdBeforeSave;

    // ---- save mode: round-trip through the REAL scene state ----
    // MC 1344: the mode now runs the REAL kill chain first (stages 0-2),
    // so the save happens with DnaMeter > 0 and the load asserts a
    // CHANGED value round-trips. The old assert (meter unchanged while
    // LoadGame never wrote the meter) was vacuous — green by construction.
    private void RunSaveStage()
    {
        _dnaMeterBeforeSave = _hud!.DnaMeter;
        _spokenDnaBeforeSave = _director!.SpokenDna.Count;
        _loyaltyBeforeSave = _director.Companion.Companion.Loyalty;
        _followerIdBeforeSave = _director.Companion.Companion.CompanionEntityId;
        _zoneBeforeSave = _director.CurrentZone;
        _progressBeforeSave = _director.Progression;
        _playerPosBeforeSave = _playerBody!.GlobalPosition;
        Check("SAVE_DNA_NONVACUOUS: DnaMeter > 0 at save time (a zero baseline would make the round-trip vacuous)",
              _dnaMeterBeforeSave > 0, $"dna={_dnaMeterBeforeSave}");
        if (_failed) return;
        _director.SaveGame();
        var store = new GodotSaveStore();
        Check("SAVE_WRITTEN: save file exists at the globalized user:// path",
              System.IO.File.Exists(store.SavePath), $"path={store.SavePath}");
        if (_failed) return;
        GD.Print("LA_GATE: SAVE_WRITTEN");
        TeleportIntoRange();   // line up the mutation kill (stage 21)
        if (_failed) return;
        _playerPosMutated = _playerBody!.GlobalPosition;
        Check("SAVE_POSITION_MUTATION_NONVACUOUS: the player was moved away from the saved position",
              _playerPosMutated.DistanceTo(_playerPosBeforeSave) > 0.5f,
              $"saved={_playerPosBeforeSave} mutated={_playerPosMutated}");
        if (_failed) return;
        _stage = 21;
        _stageFrames = 0;
    }

    private void RunMutateStage()
    {
        // Mutate the live state AWAY from the save through the REAL
        // path: a second kill bumps DnaMeter + _spokenDna past the
        // saved values; loyalty is drained directly.
        _attackToggle++;
        if (_attackToggle % 4 == 1) Input.ActionPress("attack");
        else if (_attackToggle % 4 == 3) Input.ActionRelease("attack");
        if (_hud!.DnaMeter > _dnaMeterBeforeSave && _director!.SpokenDna.Count > _spokenDnaBeforeSave)
        {
            Input.ActionRelease("attack");
            _dnaMutated = _hud.DnaMeter;
            _director.Companion.Companion.ModifyLoyalty(-25);
            // v3 (MC 3901 2b): unbind the follower id too, so the
            // LOAD_RESTORED_FOLLOWER leg compares against a genuinely
            // broken live state (the roster restore, not a no-op).
            _director.Companion.Companion.CompanionEntityId = -1;
            Check("MUTATED_AWAY_FROM_SAVE: dna meter + spoken history moved past the saved values",
                  _dnaMutated > _dnaMeterBeforeSave, $"dna={_dnaMutated} (saved {_dnaMeterBeforeSave})");
            if (_failed) return;
            _playerPosMutated = _playerBody!.GlobalPosition;
            GD.Print("LA_GATE: MUTATED_AWAY_FROM_SAVE");
            _stage = 22;
            _stageFrames = 0;
        }
        else if (_stageFrames > AttackBudgetFrames)
        {
            Fail("save mode: mutation kill did not land within the attack budget");
        }
    }

    private void RunLoadStage()
    {
        _director!.LoadGame();
        Check("LOAD_RESTORED_DNA: DnaMeter restored to the SAVED value after being mutated",
              _hud!.DnaMeter == _dnaMeterBeforeSave,
              $"dna={_hud.DnaMeter} (saved {_dnaMeterBeforeSave}, mutated {_dnaMutated})");
        if (_failed) return;
        Check("LOAD_RESTORED_POSITION: player back at the SAVED position after being moved away",
              _playerBody!.GlobalPosition.DistanceTo(_playerPosBeforeSave) < 0.05f,
              $"now={_playerBody.GlobalPosition} (saved {_playerPosBeforeSave}, mutated {_playerPosMutated})");
        if (_failed) return;
        Check("LOAD_RESTORED_SPOKEN: spoken-DNA history restored to the saved snapshot",
              _director.SpokenDna.Count == _spokenDnaBeforeSave,
              $"spoken={_director.SpokenDna.Count} (saved {_spokenDnaBeforeSave})");
        if (_failed) return;
        Check("LOAD_RESTORED_LOYALTY: companion loyalty restored to the saved value",
              _director.Companion.Companion.Loyalty == _loyaltyBeforeSave,
              $"loyalty={_director.Companion.Companion.Loyalty} (saved {_loyaltyBeforeSave})");
        if (_failed) return;
        // v3 (MC 3901 2b): the roster-of-one restores through Followers[0] now
        // that the single-companion GameState fields are gone. The id was
        // mutated to -1 in RunMutateStage, so a load that never writes the
        // companion back from Followers[0] leaves it -1 and this goes red.
        Check("LOAD_RESTORED_FOLLOWER: roster-of-one id restored to the saved value",
              _director.Companion.Companion.CompanionEntityId == _followerIdBeforeSave,
              $"entityId={_director.Companion.Companion.CompanionEntityId} (saved {_followerIdBeforeSave})");
        if (_failed) return;
        // The v3 wire form on disk (the real user:// save) must carry the
        // roster itself, not just the live restore: a Followers-shaped save
        // is the schema v3 contract.
        var onDisk = SaveSystem.Load(new GodotSaveStore());
        Check("LOAD_RESTORED_FOLLOWER: the on-disk v3 save carries the roster-of-one",
              onDisk != null && onDisk.Followers.Count == 1
              && onDisk.Followers[0].EntityId == _followerIdBeforeSave,
              onDisk == null ? "on-disk save rejected"
                             : $"followers={onDisk.Followers.Count} id={onDisk.Followers[0].EntityId}");
        if (_failed) return;
        Check("LOAD_RESTORED_ZONE: director is back in the saved zone (re-entered, SpawnSet re-applied)",
              _director.CurrentZone == _zoneBeforeSave,
              $"zone={_director.CurrentZone} (saved {_zoneBeforeSave})");
        if (_failed) return;
        Check("LOAD_RESTORED_PROGRESSION: progression restored from the save (no hardcoded zero)",
              _director.Progression == _progressBeforeSave,
              $"prog={_director.Progression} (saved {_progressBeforeSave})");
        if (_failed) return;
        GD.Print("LA_GATE: LOAD_RESTORED_DNA + LOAD_RESTORED_LOYALTY + LOAD_RESTORED_FOLLOWER");
        // Pure round-trip anchor (regression): representative state.
        var pureStore = new TempDirSaveStore();
        var state = GameState.Representative();
        // v3 (MC 3901 2b): the representative carries non-default values for
        // ALL three new fields (Followers [7,84], QuestStates 2 entries,
        // Manna 42), so this leg asserts the v3 schema end-to-end — a field
        // dropped from GameState or its serializer goes red here.
        Check("SAVE_ROUNDTRIP_PURE: representative GameState round-trips (v3 fields incl.)",
              SaveSystem.Save(state, pureStore) && SaveSystem.Load(pureStore) != null
              && SaveSystem.Load(pureStore)!.ZoneId == state.ZoneId
              && SaveSystem.Load(pureStore)!.Manna == state.Manna
              && SaveSystem.Load(pureStore)!.QuestStates.SequenceEqual(state.QuestStates)
              && SaveSystem.Load(pureStore)!.Followers.Count == state.Followers.Count
              && SaveSystem.Load(pureStore)!.Followers[0].EntityId == state.Followers[0].EntityId
              && SaveSystem.Load(pureStore)!.Followers[0].Loyalty == state.Followers[0].Loyalty,
              $"zone={state.ZoneId} manna={state.Manna} quests={state.QuestStates.Count} followers={state.Followers.Count}");
        if (_failed) return;
        GD.Print("LA_GATE: SAVE_ROUNDTRIP_PURE");
        GD.Print("LA_GATE: PASS — save/load through the real game verified");
        _asserted = true;
        _stage = 6;
        _stageFrames = 0;
    }

    // ---- save_bad_version mode: schema guard on a real save ----
    private void RunSaveBadVersionStage()
    {
        _director!.SaveGame();
        var store = new GodotSaveStore();
        Check("save written before the version break",
              System.IO.File.Exists(store.SavePath), $"path={store.SavePath}");
        if (_failed) return;
        // Rewrite the on-disk Version header to a FUTURE version.
        string json = System.IO.File.ReadAllText(store.SavePath);
        string patched = System.Text.RegularExpressions.Regex.Replace(
            json, "\"Version\"\\s*:\\s*\\d+", $"\"Version\": {SaveSystem.CurrentVersion + 1}");
        System.IO.File.WriteAllText(store.SavePath, patched);
        GameState? loaded = SaveSystem.Load(store);
        if (loaded != null)
        {
            Fail("NEG_SAVE_VERSION: out-of-date/newer save was NOT rejected — schema guard broken");
            return;
        }
        GD.Print("LA_GATE: NEG_SAVE_VERSION: out-of-date save rejected (Load returned null)");
        Quit(1);
    }
}
