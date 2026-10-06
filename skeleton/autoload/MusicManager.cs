using Godot;
using System;
using System.Collections.Generic;

// Last Animal — M06 audio (MC 890.7, artemis, 2026-09-03).
//
// MusicManager: the audio autoload (PHASE0.md Phase 7 / line 300). It owns the
// two audio buses M06 introduces — "Music" (music playback) and "Sfx" (positional
// 3D sound effects) — and hands out positional AudioStreamPlayer3D nodes on the
// Sfx bus, so SFX can be routed from gameplay code (SfxRouter, a separate file)
// without coupling to the audio internals (I4: modules never reference each other
// directly; they go through this autoload or the EventBus).
//
// Deliberately thin (C3 orchestration discipline): it does NOT decide WHAT plays —
// SfxRouter maps EventBus signals to streams. MusicManager only provides the
// bus + player infrastructure and the music entry point. Together they satisfy the
// Phase-7 gate: WAV SFX + Ogg music imported, positional AudioStreamPlayer3D on the
// Sfx bus, AudioStreamRandomizer variation, and a Music bus for cross-scene music.
//
// Headless note (test-friendly by construction): buses are ensured at runtime
// (AudioServer.AddBus) so a headless --script run with no default_bus_layout.tres
// still gets a named "Sfx"/"Music" bus to assert on — this is what lets
// tests/AudioTest.cs check "non-silent SFX on the correct bus" without a device.
namespace LastAnimal.Core.Audio;

public partial class MusicManager : Node
{
    public const string SfxBus = "Sfx";
    public const string MusicBus = "Music";

    // Every SFX-backed EventBus signal we route. The Id key matches the SfxRouter
    // routing table; one randomizer per signal gives variation (AudioStreamRandomizer).
    private readonly Dictionary<string, AudioStreamRandomizer> _sfx = new();

    // --- S4 mix duck (MC 10122 / 10026.15.3, Inc-3 stage S4) -------------------
    // The Music bus ducks while dialogue is active (the S0 DialogueShown/
    // DialogueClosed bracket, subscribed SfxRouter-style) and harder under a
    // boss-fight stance. Pure volume automation over the ONE per-bus table
    // below; runtime-only (F2: zero save delta), prints nothing — so it stays
    // OUTSIDE the ci marker streams (F4-CMP): assertions read bus STATE back.
    public const float DialogueDuckDb = -8f;    // dialogue box on screen (S0)
    public const float BossDuckDb = -12f;       // boss-fight stance held
    private const int DuckReleaseFrames = 30;   // decay in integer frames (F4)

    // Per-bus const mix levels (dB): every bus volume derives from this table;
    // the duck below only ever offsets the Music bus from its table base.
    private static readonly Dictionary<string, float> BusBaseDb = new() { [SfxBus] = 0f, [MusicBus] = 0f };

    private bool _mixSubscribed, _dialogueActive, _bossStance;
    private float _duckDb;              // duck offset currently applied
    private float _releaseFromDb;       // duck captured when the stance let go
    private int _releaseFramesLeft;     // release counter, no wall-clock (F4)

    // Music bus + cross-scene music entry point (pure infrastructure, no logic).
    public void Boot()
    {
        EnsureBuses();
        ApplyBusLevels();
        SubscribeMix(GetNodeOrNull<EventBus>("/root/EventBus"));  // autoload path; headless tests wire their own bus
        LoadSfx();
        GD.Print("MusicManager: booted (buses: ", SfxBus, "/", MusicBus, ")");
    }

    /// <summary>Create the "Music" and "Sfx" buses if they do not already exist.</summary>
    public void EnsureBuses()
    {
        AddBusIfMissing(SfxBus);
        AddBusIfMissing(MusicBus);
    }

    private static void AddBusIfMissing(string name)
    {
        for (int i = 0; i < AudioServer.BusCount; i++)
            if (AudioServer.GetBusName(i) == name)
                return;
        AudioServer.AddBus();
        AudioServer.SetBusName(AudioServer.BusCount - 1, name);
        GD.Print("MusicManager: ensured bus \"", name, "\"");
    }

    /// <summary>Load the five CC0 SFX streams into per-signal AudioStreamRandomizers.</summary>
    private void LoadSfx()
    {
        RegisterSfx("dna_extract", "res://assets/audio/dna_extract.wav");
        RegisterSfx("dna_spoken", "res://assets/audio/dna_spoken.wav");
        RegisterSfx("loyalty", "res://assets/audio/loyalty.wav");
        RegisterSfx("betrayal", "res://assets/audio/betrayal.wav");
        RegisterSfx("ecosystem", "res://assets/audio/ecosystem.wav");
    }

    private void RegisterSfx(string id, string path)
    {
        // AudioStreamWav: PCM WAV -> stream with a real, >0 sample length (non-silent).
        // The randomizer wraps ALL the variants for one signal so repeated firings
        // vary (AudioStreamRandomizer -> picks randomly), which is the Phase-7 gate.
        var wav = GD.Load<AudioStreamWav>(path);
        if (wav is null)
        {
            GD.PushWarning($"MusicManager: failed to load SFX {id} at {path}");
            return;
        }
        var rand = new AudioStreamRandomizer();
        rand.AddStream(0, wav, 1.0f);
        _sfx[id] = rand;
        GD.Print($"MusicManager: loaded SFX \"{id}\" ({path}, length={wav.GetLength():0.000}s)");
    }

    /// <summary>Return the variation randomizer for an SFX id (null if unknown).</summary>
    public AudioStreamRandomizer? GetSfx(string id)
        => _sfx.TryGetValue(id, out var s) ? s : null;

    /// <summary>Create a positional AudioStreamPlayer3D on the Sfx bus.
    /// S11 (MC 10165): no more self-QueueFree-on-Finished — the SfxRouter
    /// pool (its only caller) creates once and recycles finished players, so
    /// node churn replaced the free-on-finish buildup it used to paper over;
    /// the pool caps at its own limit and lives under the router.</summary>
    public AudioStreamPlayer3D SpawnSfxPlayer()
    {
        var p = new AudioStreamPlayer3D { Bus = SfxBus };
        return p;
    }

    /// <summary>Play cross-scene music on the Music bus (single looping player).</summary>
    public void PlayMusic(AudioStream stream)
    {
        var player = GetNodeOrNull<AudioStreamPlayer>("%MusicPlayer");
        if (player is null)
        {
            player = new AudioStreamPlayer { Name = "MusicPlayer", Bus = MusicBus };
            AddChild(player);
            player.SetMeta("MusicPlayer", true);
        }
        player.Stream = stream;
        player.Autoplay = true;
        player.Finished += () => player.Play(); // seamless loop (OggVorbis loops via its stream too)
        GD.Print($"MusicManager: playing music on bus \"{MusicBus}\"");
    }

    /// <summary>SfxRouter-style wiring: hold the dialogue duck over the S0 bracket.</summary>
    public void SubscribeMix(EventBus? bus)
    {
        if (bus is null || _mixSubscribed) return;
        _mixSubscribed = true;
        bus.DialogueShown += _ => _dialogueActive = true;
        bus.DialogueClosed += _ => _dialogueActive = false;
    }

    /// <summary>Boss-fight stance, held by whoever runs the fight; strongest duck wins.</summary>
    public void SetBossStance(bool active) => _bossStance = active;
    public float MusicDuckDb => _duckDb;   // currently applied duck offset (test seam)

    private void ApplyBusLevels()          // seat every named bus at its table base
    {
        foreach (var (bus, db) in BusBaseDb)
        {
            int i = AudioServer.GetBusIndex(bus);
            if (i >= 0) AudioServer.SetBusVolumeDb(i, db);
        }
    }

    // F4: integer frame counter only — duck holds at full depth while a stance is
    // active (boss wins over dialogue) and decays to the table base over
    // DuckReleaseFrames process frames once released. No Tween/Timer/delta,
    // and nothing is printed from this path (marker streams stay untouched).
    public override void _Process(double delta)
    {
        float target = _bossStance ? BossDuckDb : _dialogueActive ? DialogueDuckDb : 0f;
        if (target != 0f)
        {
            _duckDb = target;                 // duck engages on the spot
            _releaseFromDb = target;
            _releaseFramesLeft = DuckReleaseFrames;
        }
        else if (_duckDb != 0f)
        {
            _releaseFramesLeft--;             // linear counter decay to zero
            _duckDb = _releaseFramesLeft > 0
                ? _releaseFromDb * _releaseFramesLeft / DuckReleaseFrames
                : 0f;
        }
        int music = AudioServer.GetBusIndex(MusicBus);
        if (music < 0) return;
        float want = BusBaseDb[MusicBus] + _duckDb;
        if (AudioServer.GetBusVolumeDb(music) != want)
            AudioServer.SetBusVolumeDb(music, want);
    }
}
