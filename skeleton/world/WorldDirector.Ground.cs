// SIZE: 179 l measured by wc AT this head (MC 10420 c3: the c2 "177/+15" stamp was delta
// arithmetic off pre-c2 162; real +17 — the WireFollowClamp injection) — MC 10420 split of the ground
// guarantee out of WorldDirector.cs
// (floor-plane half moved VERBATIM from the composition root, the extent half
// new): one concern per file — the loaded ground's Y plane + XZ extent, resolved
// from the terrain_builder meta channel, are the composition root's smallest
// separable duty (no instance-identity binding lives here, so the SIZE reason of
// the root is not weakened by the move).
using Godot;

// Last Animal — MC 10404 fall guarantee + MC 10420 ground-extent clamp
// (world/WorldDirector partial, 2026-10-12).
//
// The terrain_builder publishes the ground's guarantees as META on the zone
// node ("floor_min_y" since MC 10404, "ground_extent" since MC 10420 — the
// SAME channel, one mechanism, never a second probe). This partial resolves
// both on every zone entry (boot + travel + load all enter through
// EnterZone), disarms LOUD when a guarantee's meta is missing (the MC 10404
// never-silent idiom), and owns the ONE clamp: ClampToGround bounds world
// MOTION and placements to the ground the level actually has.
//
// Why the clamp exists (MC 10420 / card 10026.29, reviewer R5 P2 + DA wall
// MC 10404 F3): EnemyAI chase/patrol/flee had NO ground bound — the patrol
// re-target diffuses 2-5 m per cycle without limit — so agents walked onto
// the floorless ring past the +/-48 m meadow edge (the calm_use census pair
// at x=66.69: the leg co-located the player onto such a position, the player
// fell, the guarantee rescued, and the state-not-position asserts stayed
// GREEN: the masking). The rescue is the last line, not the bound: motion
// ends at the extent now, and the rescue keeps its owner-fall-through
// purpose untouched.
namespace LastAnimal.World;

public partial class WorldDirector
{
    // --- MC 10404 fall plane (moved VERBATIM from the root) -------------------
    // The guarantee: once the player is below the LOWEST loaded ground's
    // floor_min by this margin, no surface can be under them any more —
    // return them to the zone-entry spawn, zero the fall velocity, Life
    // untouched. Rescue, never punishment.
    private float _floorMinY = float.PositiveInfinity;
    private bool _fallRecoveryArmed;
    private bool _fallWarnedMissingFloor;
    private const float FallRecoverMargin = 2.0f;

    // --- MC 10420 ground extent (XZ clamp box) -------------------------------
    private Vector2 _groundExtentMin, _groundExtentMax;   // min/max of X/Z
    private bool _groundExtentArmed;
    private bool _groundWarnedMissingExtent;

    // --- read-only surface (proofs READ; clamp authority stays here) ---------
    public bool HasGroundExtent => _groundExtentArmed;
    public Vector2 GroundExtentMin => _groundExtentMin;
    public Vector2 GroundExtentMax => _groundExtentMax;

    /// <summary>MC 10420: THE ground clamp — the ONE point world MOTION and
    /// placements bound to the loaded ground pass through (the EnemyActor
    /// motion tick, the wild-spawn co-location, and the proof legs' player
    /// placements all resolve here; XZ only, Y untouched). Disarmed scene =
    /// position returned UNCHANGED after one loud warning (a disarm that passes
    /// silently is the MC 10404 lesson; the GROUND_CLAMP battery leg goes RED
    /// on it).</summary>
    public Vector3 ClampToGround(Vector3 p)
    {
        if (!_groundExtentArmed)
        {
            if (!_groundWarnedMissingExtent)
            {
                _groundWarnedMissingExtent = true;
                GD.PushWarning("MC 10420: no ground ground_extent meta in scene — ground clamp DISARMED (the GROUND_CLAMP battery leg goes RED on this, never silently)");
            }
            return p;
        }
        p.X = Mathf.Clamp(p.X, _groundExtentMin.X, _groundExtentMax.X);
        p.Z = Mathf.Clamp(p.Z, _groundExtentMin.Y, _groundExtentMax.Y);
        return p;
    }

    /// <summary>MC 10420 cycle 2 (F-DA1): wire the follow clamp into a visible
    /// companion body — the same ownership line as the EnemyActor
    /// SetGroundBounds injection (the shell steers, the bounds say where
    /// steering ends), and literally the SAME clamp function: the body's
    /// follow target resolves through ClampToGround itself, so no second
    /// clamp arithmetic exists. Ungated on purpose (the enemy injection
    /// carries VALUES, stale across a zone travel; this carries the method —
    /// it reads the live armed state per call): a disarmed scene keeps the
    /// body unbounded as before, warned LOUD once (the MC 10404 never-silent
    /// idiom). Called at every body spawn site.</summary>
    private void WireFollowClamp(CompanionFollowBody body)
    {
        body.GroundClamp = ClampToGround;
    }

    /// <summary>Resolve the loaded ground's guarantees from the terrain meta
    /// (moved from the root's EnterZone tail; MC 10420 adds the extent to the
    /// SAME walk, and EnterZone now calls this BEFORE the zone's SpawnSet is
    /// fielded so actors are bounded FROM THEIR FIRST TICK — the bounds must
    /// exist before the actors they bound).</summary>
    private void ResolveGround()
    {
        float mn = float.PositiveInfinity;
        Vector2 lo = new(float.PositiveInfinity, float.PositiveInfinity);
        Vector2 hi = new(float.NegativeInfinity, float.NegativeInfinity);
        bool hasExtent = CollectGround(this, ref mn, ref lo, ref hi);

        if (!float.IsFinite(mn))
        {
            if (!_fallWarnedMissingFloor)
            {
                _fallWarnedMissingFloor = true;
                GD.PushWarning("MC 10404: no ground floor_min_y meta in scene — fall recovery DISARMED (the fall_recovered battery leg goes RED on this, never silently)");
            }
            _fallRecoveryArmed = false;
        }
        else
        {
            _floorMinY = mn;
            _fallRecoveryArmed = true;
            GD.Print($"W3: fall-recovery plane floorMin={mn:0.##} (triggers below y<{mn - FallRecoverMargin:0.##})");
        }

        // MC 10420: the extent arms and DISARMS on every entry exactly like the
        // plane half above — a zone WITHOUT the meta must not keep acting under
        // the PREVIOUS zone's box (travel meadow->canyon->ruins re-resolves; a
        // decor-only boot enters unarmed + loud, the MC 10404 never-silent
        // idiom). Both metas ship from the same terrain_builder _ready, so any
        // drift between the two guarantees is a bug the loud halves expose.
        if (hasExtent && float.IsFinite(lo.X))
        {
            bool first = !_groundExtentArmed;
            _groundExtentMin = lo;
            _groundExtentMax = hi;
            _groundExtentArmed = true;
            if (first)
                GD.Print($"W3: ground-extent clamp x[{lo.X:0.##}..{hi.X:0.##}] z[{lo.Y:0.##}..{hi.Y:0.##}] (enemy motion + co-locations bounded inside)");
        }
        else
        {
            _groundExtentArmed = false;
            if (!_groundWarnedMissingExtent)
            {
                _groundWarnedMissingExtent = true;
                GD.PushWarning("MC 10420: no ground ground_extent meta in scene — ground clamp DISARMED (the GROUND_CLAMP battery leg goes RED on this, never silently)");
            }
        }
    }

    // Walk the subtree collecting the terrain_builder meta channel: the lowest
    // floor_min_y (MC 10404) and the UNION of every ground_extent (MC 10420 —
    // one Ground node per playable scene today; the union keeps a future
    // multi-ground scene bounded by its whole walkable set, the interval
    // semantics the Y-min already takes). Returns whether ANY extent meta was
    // seen.
    private static bool CollectGround(Node from, ref float mn, ref Vector2 lo, ref Vector2 hi)
    {
        bool any = false;
        if (from.HasMeta("floor_min_y"))
        {
            float v = (float)from.GetMeta("floor_min_y").AsDouble();
            if (v < mn) mn = v;
        }
        if (from.HasMeta("ground_extent"))
        {
            Vector2 e = from.GetMeta("ground_extent").AsVector2();
            // The meta rides a terrain ground NODE; only a Node3D carries a
            // position to centre the box on (a non-3D carrier is a bug: the
            // extent still unions about the origin, loudly bounded by the
            // AABB cross-check the GROUND_CLAMP leg runs).
            Vector3 c = from is Node3D n3 ? n3.GlobalPosition : Vector3.Zero;
            lo.X = Mathf.Min(lo.X, c.X - e.X * 0.5f);
            hi.X = Mathf.Max(hi.X, c.X + e.X * 0.5f);
            lo.Y = Mathf.Min(lo.Y, c.Z - e.Y * 0.5f);
            hi.Y = Mathf.Max(hi.Y, c.Z + e.Y * 0.5f);
            any = true;
        }
        foreach (var child in from.GetChildren())
            any |= CollectGround(child, ref mn, ref lo, ref hi);
        return any;
    }
}
