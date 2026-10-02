using LastAnimal.Combat;
using LastAnimal.Dna;
using LastAnimal.Ecosystem;
using LastAnimal.Npc;
using LastAnimal.Save;
using LastAnimal.Ui;
using System;
using System.Collections.Generic;

// Last Animal — save/load production wiring (MC 1344, DA findings 1+2).
//
// SaveLoadController: the save/load concern extracted from WorldDirector so
// the composition root stays under the file-size ceiling and this file owns
// ONE concern: snapshot the live game state -> GameState, and apply a loaded
// GameState back onto the live game.
//
// What load actually restores (the DA findings this fixes):
//   - the spoken-DNA history (_spokenDna), rebuilt from the saved
//     LearnedDnaCounters as ONE signature whose per-position most-common
//     nucleotides ARE the counters — so a save->load->save round-trip is
//     stable and EcosystemAdaptation.ModelPlayerDna sees the same profile;
//   - the HUD DNA meter (DnaEventCount) via Hud.UpdateDnaMeter — the bus only
//     increments, so a restore cannot ride a bus event;
//   - Progression (no hardcoded zeros — the counter lives here and ticks on
//     first entry into each new zone);
//   - the follower roster (v3: the companion is Followers[0]);
//   - the saved zone: the director's enter-zone callback is invoked so the
//     zone's SpawnSet is re-applied (re-entry, not just a field write).
//
// Engine-free apart from the GodotSaveStore seam (same as WorldDirector's
// previous inline implementation): pure logic + SaveSystem.
namespace LastAnimal.World;

/// <summary>
/// Owns SaveGame/LoadGame for the playable path: builds the GameState
/// snapshot from live director state and applies a loaded snapshot back.
/// </summary>
public sealed class SaveLoadController
{
    private readonly List<LanguageSignature> _spokenDna;
    private readonly CompanionComponent _companion;
    // MC 1405 N6: nullable, not `null!` — a composition without a UI canvas
    // (BuildUi early-returns) leaves the HUD absent, and Save/Load must not
    // NRE on it. Same tolerance WorldDirector applies to its optional nodes.
    private readonly Hud? _hud;
    private readonly Func<string> _currentZone;
    private readonly Action<string> _enterZone;

    // Progression: chapters cleared = first entries into DISTINCT zones
    // (MC 1348 N2 — the counting rule lives in the pure ZoneProgression so it
    // is headless-testable; this controller only delegates). Seeded with the
    // default zone so the boot-time meadow entry is not counted as progress.
    private readonly ZoneProgression _progression = new(EcosystemSpawner.DefaultZone);

    // Player health seam (MC 1348 N1): read at save, restored on load, so the
    // F9 load path rescues a dead player.
    private readonly Func<int> _playerHealth;
    private readonly Action<int> _restoreHealth;

    // Player position seam (MC 1405 N5): read at save, restored on load BEFORE
    // the zone re-entry, so the SpawnSet fields around where the player stood
    // when they saved — matching travel semantics. Nullable for the same
    // reason as _hud: a composition without a player node skips the restore.
    private readonly Func<CombatVec3>? _playerPosition;
    private readonly Action<CombatVec3>? _restorePosition;

    // QuestStates seams (MC 3904 2c): the v3 field exists since 2b but nothing
    // persisted it during play (the W1 F-1 map amendment). WorldDirector.Story
    // injects these after construction; the vocabulary ("id:status") is owned
    // by the pure QuestLog. Unset (a composition without the story seam)
    // leaves the v3 field empty at save and unread at load — the field's
    // presence is still the schema's, never this controller's invention.
    public Func<List<string>>? QuestStatesWrite { get; set; }
    public Action<List<string>>? QuestStatesRestore { get; set; }

    public SaveLoadController(
        List<LanguageSignature> spokenDna,
        CompanionComponent companion,
        Hud? hud,
        Func<string> currentZone,
        Action<string> enterZone,
        Func<int> playerHealth,
        Action<int> restoreHealth,
        Func<CombatVec3>? playerPosition = null,
        Action<CombatVec3>? restorePosition = null)
    {
        _spokenDna = spokenDna;
        _companion = companion;
        _hud = hud;
        _currentZone = currentZone;
        _enterZone = enterZone;
        _playerHealth = playerHealth;
        _restoreHealth = restoreHealth;
        _playerPosition = playerPosition;
        _restorePosition = restorePosition;
    }

    /// <summary>Progression counter (chapters cleared / distinct zones entered).</summary>
    public int Progression => _progression.Chapters;

    /// <summary>
    /// Progression tick from the director's zone-entered handler: the FIRST
    /// entry into each distinct zone clears a chapter; re-entering a zone
    /// already visited (including a load's re-entry) does not.
    /// </summary>
    public void OnZoneEntered(string zoneId) => _progression.OnZoneEntered(zoneId);

    /// <summary>Snapshot the live game into user://savegame.json. False on I/O error.</summary>
    public bool Save()
    {
        var state = new GameState
        {
            ZoneId = _currentZone(),
            Progression = _progression.Chapters,
            // The per-position most-common nucleotide of the spoken history
            // (the C14 "persists DNA counters" snapshot).
            LearnedDnaCounters = BuildLearnedCounters(),
            // MC 1405 N6: the HUD is optional (no-UI composition) — read the
            // meter only when it exists, else the meter count is simply 0.
            DnaEventCount = _hud?.DnaMeter ?? 0,
            // v3 (MC 3901 2b): the roster-of-one — the companion, when it
            // exists, is Followers[0]; an empty roster means no follower.
            Followers = BuildFollowers(),
            // MC 1348 N1: health is part of the snapshot so a load can rescue
            // a dead player.
            PlayerHealth = _playerHealth(),
        };
        // MC 3904 2c: quest progress rides the snapshot (v3 QuestStates,
        // "id:status" rows from the live QuestLog; unset seam saves an empty
        // field, the schema default).
        if (QuestStatesWrite != null)
            state.QuestStates = QuestStatesWrite();
        // MC 1405 N5: the player's world position rides the snapshot too.
        if (_playerPosition != null)
        {
            var p = _playerPosition();
            state.HasPlayerPosition = true;
            state.PlayerX = p.X;
            state.PlayerY = p.Y;
            state.PlayerZ = p.Z;
        }
        return SaveSystem.Save(state, new GodotSaveStore());
    }

    /// <summary>
    /// Apply the saved state back onto the live game and re-enter the saved
    /// zone. False when there is no loadable save (schema guard / no file).
    /// </summary>
    public bool Load()
    {
        var loaded = SaveSystem.Load(new GodotSaveStore());
        if (loaded == null) return false;

        RestoreDna(loaded.LearnedDnaCounters);
        // MC 3904 2c: the live QuestLog re-applies the saved rows (direct
        // restore, no replayed transitions — see QuestLog.FromSaveRows).
        QuestStatesRestore?.Invoke(loaded.QuestStates);
        // v3 (MC 3901 2b): restore the roster-of-one from Followers[0].
        // An empty roster restores the no-companion defaults (the same
        // -1/50 the v2 defaults carried). Stage 2g grows this to N; the
        // single live companion object stays authoritative until then.
        var follower = loaded.Followers.Count > 0 ? loaded.Followers[0] : new FollowerEntry();
        _companion.CompanionEntityId = follower.EntityId;
        _companion.Loyalty = follower.Loyalty;
        // MC 1405 N6: HUD restore is guarded — a no-UI composition has no
        // meter or life gauge to update.
        _hud?.UpdateDnaMeter(loaded.DnaEventCount);
        // MC 1348 N1: restore health (reviving a dead player) and mirror it
        // on the HUD life gauge, which the bus does not drive.
        _restoreHealth(loaded.PlayerHealth);
        _hud?.UpdateLife(loaded.PlayerHealth);
        // MC 1405 N5: put the player back where they stood BEFORE the zone
        // re-entry, so the re-applied SpawnSet spawns around the restored
        // position (travel semantics), not around the load-point position.
        // Old saves without the position fields keep the player where they
        // are (HasPlayerPosition == false) — never a teleport to origin.
        if (loaded.HasPlayerPosition && _restorePosition != null)
            _restorePosition(new CombatVec3(loaded.PlayerX, loaded.PlayerY, loaded.PlayerZ));
        _progression.SeedForLoad(loaded.ZoneId, loaded.Progression);
        // Re-enter: the director's handler re-applies the zone's SpawnSet.
        _enterZone(loaded.ZoneId);
        return true;
    }

    /// <summary>v3 roster snapshot (MC 3901 2b): the companion when bonded,
    /// as Followers[0]; an empty list when there is no follower (the -1
    /// sentinel is not persisted as a phantom roster entry).</summary>
    private List<FollowerEntry> BuildFollowers()
    {
        var roster = new List<FollowerEntry>();
        if (_companion.HasCompanion)
            roster.Add(new FollowerEntry
            {
                EntityId = _companion.CompanionEntityId,
                Loyalty = _companion.Loyalty
            });
        return roster;
    }

    /// <summary>Rebuild the spoken history from the saved counters: ONE
    /// signature whose nucleotides are the counters, so a subsequent save
    /// reproduces the same LearnedDnaCounters (stable round-trip).</summary>
    private void RestoreDna(List<int> counters)
    {
        _spokenDna.Clear();
        if (counters.Count > 0)
            _spokenDna.Add(new LanguageSignature(counters.ToArray()));
    }

    /// <summary>Per-position most-common nucleotide across the spoken history
    /// (the C14 LearnedDnaCounters snapshot; empty when nothing was spoken).</summary>
    private List<int> BuildLearnedCounters()
    {
        var counters = new List<int>();
        if (_spokenDna.Count == 0) return counters;
        int len = 0;
        foreach (var s in _spokenDna) len = Math.Max(len, s.Nucleotides.Length);
        for (int i = 0; i < len; i++)
        {
            var tally = new int[4];
            foreach (var s in _spokenDna)
                if (i < s.Nucleotides.Length) tally[s.Nucleotides[i]]++;
            int best = 0;
            for (int n = 1; n < 4; n++) if (tally[n] > tally[best]) best = n;
            counters.Add(best);
        }
        return counters;
    }
}
