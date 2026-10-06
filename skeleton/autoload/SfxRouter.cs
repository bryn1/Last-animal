using System.Collections.Generic;
using Godot;

// Last Animal — M06 audio (MC 890.7, artemis, 2026-09-03).
//
// SfxRouter: the thin wiring that "routes SFX off EventBus signals" (PHASE0.md
// Phase 7 gate). It subscribes to the five C2 game signals on EventBus and, for
// each, spawns a positional AudioStreamPlayer3D on the Sfx bus whose stream comes
// from MusicManager (so a single sub plays through the shared audio autoload).
//
// Deliberately NOT an autoload itself and no gameplay logic: a scene/root just adds
// it next to EventBus and calls Subscribe(). The headless test does exactly that.
namespace LastAnimal.Core.Audio;

public partial class SfxRouter : Node
{
    private MusicManager? _music;

    // MC 10165 S11 pool reuse (Inc-3 plan action class 2): Fire() recycles a
    // finished player instead of node-spawn-per-signal (the old path created an
    // AudioStreamPlayer3D and QueueFree'd it on every signal — node churn was
    // the cost, not the audio). Idle-first scan keeps routing deterministic:
    // the same signal sequence always re-arms the same player sequence (F4).
    // When the whole pool is busy at the cap, re-arming round-robin is bounded
    // behaviour the old unbounded churn never had.
    private readonly List<AudioStreamPlayer3D> _pool = new();
    private const int PoolCap = 12;
    private int _steal;

    // Test seam (MC 10165): under pooling the router's child COUNT no longer
    // measures routing (a finished player re-arms without a new child), so the
    // AudioTest MAP legs assert on this count + the last routed player.
    // A planted double-route increments twice in one emit and trips the leg —
    // strictly stronger than the old spawn-count proxy, which was blind to a
    // double route that re-armed an idle player.
    public int RoutedCount { get; private set; }
    public AudioStreamPlayer3D? LastRouted { get; private set; }

    private AudioStreamPlayer3D AcquirePooled()
    {
        foreach (var p in _pool)
            if (!p.Playing) return p;                 // stream finished: re-arm in place
        if (_pool.Count < PoolCap)
        {
            var fresh = _music!.SpawnSfxPlayer();     // positional, on the Sfx bus
            _pool.Add(fresh);
            AddChild(fresh);                          // tree residency once, at birth
            return fresh;
        }
        var next = _pool[_steal++ % PoolCap];         // all busy: bounded re-arm
        next.Stop();
        return next;
    }

    /// <summary>Wire SFX to the given EventBus via the shared MusicManager autoload.</summary>
    public void Subscribe(EventBus bus, MusicManager music)
    {
        _music = music;
        bus.DnaExtracted += _ => Fire("dna_extract");
        bus.DnaSpoken += _ => Fire("dna_spoken");
        bus.LoyaltyChanged += (_, _) => Fire("loyalty");
        bus.Betrayal += (_, _) => Fire("betrayal");
        bus.EcosystemAdapted += _ => Fire("ecosystem");
        // MC 3912 stage 2e (owner ruling D7 RATIFIED): route the new SkillUsed
        // signal onto an EXISTING stream — a skill is the player speaking its
        // own mutated DNA, so it rides "dna_spoken". Zero new assets (plan §D
        // 7: no new audio this increment; Fire already skips a missing stream).
        bus.SkillUsed += _ => Fire("dna_spoken");
        // MC 10130 S5 (Inc-3 W2): route ONLY the currently-unmapped signals, each
        // exactly once, onto the five EXISTING streams (D-1: zero new assets).
        // DialogueShown/DialogueClosed are deliberately NOT routed — MusicManager.
        // SubscribeMix (S4) owns their presentation-adjacent audio (Music-bus duck);
        // an SFX route here would double-route those signals. EntityId-carrying
        // signals fire at a deterministic per-Id pitch (PitchFor).
        bus.QuestStarted += q => Fire("loyalty", q);        // companion-bond beat
        bus.QuestObjective += q => Fire("loyalty", q);      //   (quests recruit and
        bus.WagePaid += q => Fire("loyalty", q);            //    hold the roster)
        bus.QuestCompleted += q => Fire("dna_extract", q);  // reward beat = a pickup
        bus.EmpathyBookOpened += () => Fire("dna_spoken");  // spoken idiom (as 2e)
        bus.PlayerHurt += _ => Fire("betrayal");            // harm beat: harsh stream
        bus.BossFallen += b => Fire("ecosystem", b);        // zone reshapes (as C5)
    }

    private void Fire(string sfx, string? entityId = null)
    {
        var rand = _music?.GetSfx(sfx);
        if (rand is null)
        {
            GD.Print($"SFX_ROUTER: fire \"{sfx}\" SKIPPED (stream not loaded)");
            return;
        }
        var player = AcquirePooled();              // recycled positional player (S11)
        player.Stream = rand;                      // randomizer -> variation on repeat
        // Explicit pitch: a recycled player carries its previous fire's pitch,
        // so the no-entity case must restate 1.0 rather than rely on the default.
        player.PitchScale = entityId is not null ? PitchFor(entityId) : 1.0f;
        player.Play();                             // node is already tree-resident (pool)
        RoutedCount++;
        LastRouted = player;
        GD.Print($"SFX_ROUTER: fired \"{sfx}\"  bus={player.Bus}  playing={player.Playing}" +
                 $"  streamLen={player.Stream?.GetLength():0.000}s");
    }

    /// <summary>Deterministic per-EntityId pitch (S5): integer-keyed fold of the Id
    /// over five fixed steps above unity — same Id, same pitch, every run. NOT
    /// System.Random, and NOT string.GetHashCode (its seed is per-process, which
    /// would break cross-run determinism, F4).</summary>
    public static float PitchFor(string entityId)
    {
        int h = 17;
        foreach (var c in entityId) h = unchecked(h * 31 + c);
        return 1.0f + (int)((uint)h % 5u) * 0.05f;
    }
}
