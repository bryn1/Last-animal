using Godot;
using LastAnimal.Core;
using LastAnimal.Save;

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
    // differ.
    private Godot.Vector3 _playerPosBeforeSave;
    private Godot.Vector3 _playerPosMutated;

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
        _zoneBeforeSave = _director.CurrentZone;
        _progressBeforeSave = _director.Progression;
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
            Check("MUTATED_AWAY_FROM_SAVE: dna meter + spoken history moved past the saved values",
                  _dnaMutated > _dnaMeterBeforeSave, $"dna={_dnaMutated} (saved {_dnaMeterBeforeSave})");
            if (_failed) return;
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
        Check("LOAD_RESTORED_SPOKEN: spoken-DNA history restored to the saved snapshot",
              _director.SpokenDna.Count == _spokenDnaBeforeSave,
              $"spoken={_director.SpokenDna.Count} (saved {_spokenDnaBeforeSave})");
        if (_failed) return;
        Check("LOAD_RESTORED_LOYALTY: companion loyalty restored to the saved value",
              _director.Companion.Companion.Loyalty == _loyaltyBeforeSave,
              $"loyalty={_director.Companion.Companion.Loyalty} (saved {_loyaltyBeforeSave})");
        if (_failed) return;
        Check("LOAD_RESTORED_ZONE: director is back in the saved zone (re-entered, SpawnSet re-applied)",
              _director.CurrentZone == _zoneBeforeSave,
              $"zone={_director.CurrentZone} (saved {_zoneBeforeSave})");
        if (_failed) return;
        Check("LOAD_RESTORED_PROGRESSION: progression restored from the save (no hardcoded zero)",
              _director.Progression == _progressBeforeSave,
              $"prog={_director.Progression} (saved {_progressBeforeSave})");
        if (_failed) return;
        GD.Print("LA_GATE: LOAD_RESTORED_DNA + LOAD_RESTORED_LOYALTY");
        // Pure round-trip anchor (regression): representative state.
        var pureStore = new TempDirSaveStore();
        var state = GameState.Representative();
        Check("SAVE_ROUNDTRIP_PURE: representative GameState round-trips",
              SaveSystem.Save(state, pureStore) && SaveSystem.Load(pureStore) != null
              && SaveSystem.Load(pureStore)!.ZoneId == state.ZoneId,
              $"zone={state.ZoneId}");
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
