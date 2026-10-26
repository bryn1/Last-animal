using Godot;
using LastAnimal.Ecosystem;

// Last Animal — MC 11296 opening-grace observer (playthrough D4, MC 11270).
//
// The FIX for D4 lives in the spawner's radius knob (EcosystemSpawner's
// ZoneTable.OpeningRing — the boot camp is fielded outside the goblin aggro
// ring; zero new AI, boss and the deep zones byte-identical). This partial is
// the ONE observability channel for that fix: a PRINT-ONLY sample line over
// the first GraceWindowSeconds of the opening zone, so the unattended Life
// curve the card is gated on is measured in the shipped transcript — the
// LA_GATE: print-only idiom (MC 11037 ZONE_MOUNT_GROUNDTEX precedent: the line
// prints, it asserts nothing, the gate script greps the numbers). Zero
// gameplay behavior lives here; the redaction-free numbers are the point.
//
// Window semantics: armed when the opening zone's set is FIELD-APPLIED
// (ApplySpawnSet, behind its spawn gate — a spawn-off quiet boot stays
// silent; boot included; the ring is a zone property, so is its observer)
// AND ONLY ON A PLAIN BOOT: an LA_GATE_MODE-carrying spawn is a proof-harness
// run (battery, determinism A/A) and stays silent — the byte-stable stream
// determinism_cmp keys on must not learn a new, timing-sensitive line
// (MC 11037's print-only leg made the same class of isolation a law:
// "print-only BY DESIGN"). Samples once per elapsed whole second plus a DONE
// line naming the window minimum — the floor gate greps the DONE line.
// Disarming is the window itself; no input, no UI, no save surface is touched.
namespace LastAnimal.World;

public partial class WorldDirector
{
    // --- MC 11296 opening-grace window (print-only; see file header) --------
    private float _graceT = -1f;          // < 0 = window closed
    private float _graceNextSample;       // next whole-second boundary
    private float _graceRing;             // fielded ring radius, read off the set
    private int _graceMinLife = 100;      // min Health sampled over the window
    private const float GraceWindowSeconds = 20f;   // the card's gate window

    /// <summary>Arm the observer when the opening zone's set has BEEN FIELD
    /// (called from ApplySpawnSet, behind its spawn gate — a spawn-off quiet
    /// boot stays silent; called on every meadow application, boot included;
    /// the ring is a zone property, so is its observer). Meadow only.</summary>
    private void ArmOpeningGrace(SpawnSet set)
    {
        if (set.ZoneId != "meadow") return;
        // PLAIN BOOT ONLY (header): a proof-harness spawn carries LA_GATE_MODE
        // (every battery/determinism_cmp invocation does) and stays silent —
        // determinism_cmp stream (i) is byte-stable by contract and must not
        // learn a timing-sensitive line.
        if (!string.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("LA_GATE_MODE"))) return;
        foreach (var e in set.Enemies)
        {
            if (e.IsBoss) continue;   // meadow never fields one — belt
            _graceRing = e.Position.Length();
            break;
        }
        _graceT = 0f;
        _graceNextSample = 0f;
        _graceMinLife = _player.Health;
    }

    /// <summary>Per-frame sample tick (one-line call from _Process). Tracks the
    /// window minimum every frame; prints once per whole second.</summary>
    private void TickOpeningGrace(double delta)
    {
        if (_graceT < 0f) return;
        _graceT += (float)delta;
        if (_player.Health < _graceMinLife) _graceMinLife = _player.Health;
        while (_graceT >= _graceNextSample && _graceNextSample <= GraceWindowSeconds)
        {
            GD.Print($"LA_GATE: OPENING_GRACE t={_graceNextSample:0}s life={_player.Health} " +
                     $"min={_graceMinLife} ring={_graceRing:0.#} zone={_zone}");
            _graceNextSample += 1f;
        }
        if (_graceT > GraceWindowSeconds)
        {
            GD.Print($"LA_GATE: OPENING_GRACE_DONE window={GraceWindowSeconds:0}s " +
                     $"life={_player.Health} min={_graceMinLife} ring={_graceRing:0.#} zone={_zone}");
            _graceT = -1f;
        }
    }
}
