using Godot;
using LastAnimal.Combat;
using LastAnimal.World;

// Last Animal — MC 10420 / 10026.29 ground-extent clamp proof (code, 2026-10-12).
//
// Partial half of RuntimeIntegrationProof: mode GROUND_CLAMP (stage 104) proves
// the MOTION bound on the live composed scene, the class the calm_use census
// pair exposed (reviewer R5 P2): EnemyAI chase/patrol/flee plan UNBOUNDED and
// the visible body followed them onto the floorless ring past the +/-48 m
// meadow edge (x=66.69) — the leg stayed GREEN while the MC 10404 guarantee
// rescued the player twice BEHIND its state-only asserts. The fix clamps world
// motion + co-locations at the director's ground extent (one mechanism: the
// terrain_builder "ground_extent" meta, same channel as floor_min_y); this leg
// is the gate that cannot pass vacuously:
//
//   0. the extent must be ARMED (a silent disarm is RED here, never silent —
//      the MC 10404 idiom) and cross-checked against the VISIBLE ground: the
//      meta box vs the Ground/TerrainVisual AABB — the exact world<->extent
//      knowledge FloorCoverageProof already owns (its grid spans that AABB; the
//      clamp box must be that ground, not a number beside it);
//   1. a live spawned enemy is driven at the +X edge WITH REAL MOTION: the
//      proof drops it below the flee threshold and the AI itself runs away
//      from the (far -X) player at full speed, every tick, until world motion
//      hits the bound — the body must PIN at the extent (never beyond, and it
//      must actually REACH it: no vacuous green);
//   2. every enemy/wild body position is swept against the extent EVERY frame
//      — a beyond-extent position FAILS the leg naming its coordinates
//      (the planted-bad +100 m clamp widening walks straight into this);
//   3. the wild-spawn co-location (the calm choreography's mechanism) must
//      land ON the ground even when requested past the edge.
//
// Markers: GROUND_CLAMP_AABB, ENEMY_GROUNDPIN, WILD_SPAWN_CLAMPED,
// GROUND_CLAMP_SWEEP, GROUND_CLAMP_CLEAR. Zero FALL_RECOVERED inside this leg
// is asserted battery-side (the leg never places an actor below the plane).
public partial class RuntimeIntegrationProof : SceneTree
{
    private int _gPhase, _gFrames, _gPin, _gSwept;
    private EnemyActor? _gTarget;
    private Vector2 _gMin, _gMax;          // ground extent XZ (meta-resolved)
    private const float SweepEps = 0.05f;  // float-noise tolerance on the bound

    /// <summary>Stage-104 dispatch (called from the main file's stage switch).</summary>
    private void RunGroundClampStage()
    {
        if (_director == null || _playerBody == null || _failed) return;
        _gFrames++;
        switch (_gPhase)
        {
            case 0: GSetup(); break;
            case 1: GPin(); break;
            case 2: GWildCoLocate(); break;
            case 3: GFinish(); break;
        }
    }

    private void GSetup()
    {
        // The leg's own disarm tripwire first: no extent meta = no clamp = RED.
        Check("ground extent ARMED", _director!.HasGroundExtent,
            "ground_extent meta unresolved — the MC 10420 clamp is DISARMED (never silent)");
        _gMin = _director.GroundExtentMin;
        _gMax = _director.GroundExtentMax;

        // World<->extent cross-check REUSED from FloorCoverageProof: the meta
        // box must be the visible ground's AABB (Ground/TerrainVisual), within
        // half a metre of the mesh it draws.
        var metaNode = FindGroundMetaNode(_main!);
        var visual = metaNode?.GetNodeOrNull<MeshInstance3D>("TerrainVisual");
        Check("visible ground AABB found for the meta node", visual != null,
            $"no Ground/TerrainVisual under the ground_extent meta node ({metaNode?.Name ?? "none"})");
        Aabb ab = visual!.Mesh.GetAabb();
        float metaX = _gMax.X - _gMin.X, metaZ = _gMax.Y - _gMin.Y;
        Check("meta box == visual AABB (FloorCoverageProof's world<->extent)",
            Mathf.Abs(ab.Size.X - metaX) < 0.5f && Mathf.Abs(ab.Size.Z - metaZ) < 0.5f,
            $"meta {metaX:0.#}x{metaZ:0.#} vs visual {ab.Size.X:0.#}x{ab.Size.Z:0.#}");
        GD.Print($"LA_GATE: GROUND_CLAMP_AABB meta=[{_gMin.X:0.#}..{_gMax.X:0.#}]x[{_gMin.Y:0.#}..{_gMax.Y:0.#}] visual=OK");

        // The FLEE rig: the player stands deep -X (the clamp keeps it ON the
        // ground; the far side gives the flee a straight +X run at the edge),
        // the target enemy is PLACED (a test placement is not motion) mid-run
        // at x = edge-23, then wounded below its flee threshold so the AI's OWN
        // UpdateFlee drives every tick at full Speed toward +X.
        _playerBody!.GlobalPosition = _director.ClampToGround(
            new Vector3(_gMin.X + 15f, 3f, (_gMin.Y + _gMax.Y) * 0.5f));
        _gTarget = FirstLiveEnemy();
        if (_gTarget == null) { Fail("GROUND_CLAMP: no live spawned enemy to drive"); return; }
        _gTarget.GlobalPosition = new Vector3(_gMax.X - 23f, 2f, (_gMin.Y + _gMax.Y) * 0.5f);
        _gTarget.Ai.Position = new CombatVec3(_gMax.X - 23f, 0f, (_gMin.Y + _gMax.Y) * 0.5f);
        int dmg = _gTarget.Ai.Health - _gTarget.Ai.MaxHealth / 4;   // Health -> Max/4 < flee threshold
        _gTarget.Ai.TakeDamage(dmg < 1 ? 1 : dmg);
        GD.Print($"LA_GATE: GROUND_CLAMP setup: enemy={_gTarget.Name} hp={_gTarget.Ai.Health}/{_gTarget.Ai.MaxHealth} state={_gTarget.Ai.CurrentState} -> fleeing +X to edge {_gMax.X:0.#}");
        _gPhase = 1; _gFrames = 0;
    }

    private void GPin()
    {
        SweepInside("pin-watch");
        float x = _gTarget!.GlobalPosition.X;
        if (x >= _gMax.X - 0.3f) _gPin++;
        if (_gPin >= 30)
        {
            // The clamp BIT: the body reached the bound and held it under the
            // AI's continued flee pressure; the PLAN is bounded too (the shell
            // re-anchors it every tick — body and plan one position).
            Check("plan clamped with the body",
                _gTarget.Ai.Position.X <= _gMax.X + SweepEps && _gTarget.Ai.Position.X >= _gMax.X - 0.3f,
                $"plan X={_gTarget.Ai.Position.X:0.###} vs edge {_gMax.X:0.###}");
            GD.Print($"LA_GATE: ENEMY_GROUNDPIN body x={x:0.###} plan x={_gTarget.Ai.Position.X:0.###} edge={_gMax.X:0.###} (flee pressure still on — held 30 frames)");
            _gPhase = 2; _gFrames = 0;
            return;
        }
        if (_gFrames > 1500)
        {
            Fail($"GROUND_CLAMP: clamp never bit — enemy reached only x={x:0.###} of edge {_gMax.X:0.###} in 1500 frames (a motion path bypasses the clamp)");
            return;
        }
    }

    private void GWildCoLocate()
    {
        // The calm choreography's mechanism, aimed deliberately PAST the edge:
        // the player is clamped ONTO the edge, and the wild-spawn seam must
        // land ON the ground even though it REQUESTS 2.5 m beyond it.
        _playerBody!.GlobalPosition = _director!.ClampToGround(
            new Vector3(_gMax.X + 30f, 3f, (_gMin.Y + _gMax.Y) * 0.5f));
        Check("player clamp lands ON the edge",
            Mathf.Abs(_playerBody.GlobalPosition.X - _gMax.X) < 0.05f,
            $"player x={_playerBody.GlobalPosition.X:0.###} requested {_gMax.X + 30f:0.#} clamped to {_gMax.X:0.#}");
        _director.SpawnWildFollower();
        var w = _director.WildBodies[_director.WildBodies.Count - 1];
        float zx = (_gMin.Y + _gMax.Y) * 0.5f;
        Check("wild co-location lands INSIDE the extent",
            w.GlobalPosition.X <= _gMax.X + SweepEps,
            $"wild x={w.GlobalPosition.X:0.###} requested {_gMax.X + 2.5f:0.#}");
        Check("wild co-location clamp BIT (pinned at the edge, not merely inside)",
            w.GlobalPosition.X >= _gMax.X - 0.05f && Mathf.Abs(w.GlobalPosition.Z - zx) < 0.05f,
            $"wild landed ({w.GlobalPosition.X:0.###},{w.GlobalPosition.Z:0.###}) vs edge ({_gMax.X:0.#},{zx:0.#})");
        GD.Print($"LA_GATE: WILD_SPAWN_CLAMPED x={w.GlobalPosition.X:0.###} (requested {_gMax.X + 2.5f:0.#}) y={w.GlobalPosition.Y:0.##}");
        // Co-locate the player onto it — the calm idiom verbatim — and finish
        // sweeping: nothing on this scene may be off the ground, least of all
        // the player whose falls the census counts.
        _playerBody.GlobalPosition = w.GlobalPosition;
        _gPhase = 3; _gFrames = 0;
    }

    private void GFinish()
    {
        SweepInside("finish");
        var p = _playerBody!.GlobalPosition;
        Check("player co-located ON the ground", p.X <= _gMax.X + SweepEps && p.X >= _gMin.X - SweepEps,
            $"player x={p.X:0.###} outside [{_gMin.X:0.#}..{_gMax.X:0.#}]");
        if (_gFrames < 60) return;
        GD.Print($"LA_GATE: GROUND_CLAMP_SWEEP frames={_gFrames} position-checks={_gSwept} zero beyond-extent");
        GD.Print("GROUND_CLAMP_CLEAR");
        GD.Print("LA_GATE: PASS — ground-extent clamp: enemy MOTION pins at the loaded edge under live flee pressure, wild co-locations land on the ground, every enemy/wild position stayed inside the extent");
        _asserted = true;
        ReleaseHeldRefsBeforeQuit();
        Quit(0);
    }

    /// <summary>The DoD-1 probe assertion: EVERY live enemy + wild body, EVERY
    /// physics frame of the leg, inside the ground extent — a violation names
    /// the exact beyond-extent position and exits 1 (the plant-red walks into
    /// this line the frame the widened clamp lets it past the edge).</summary>
    private void SweepInside(string when)
    {
        foreach (var e in _director!.Enemies)
        {
            var q = e.GlobalPosition;
            _gSwept++;
            if (q.X < _gMin.X - SweepEps || q.X > _gMax.X + SweepEps ||
                q.Z < _gMin.Y - SweepEps || q.Z > _gMax.Y + SweepEps)
            {
                Fail($"ENEMY_OUTSIDE_EXTENT {e.Name} at ({q.X:0.###},{q.Y:0.##},{q.Z:0.###}) beyond ground x[{_gMin.X:0.##}..{_gMax.X:0.##}] z[{_gMin.Y:0.##}..{_gMax.Y:0.##}] ({when}) — unbounded motion past the loaded ground");
                return;
            }
        }
        foreach (var w in _director.WildBodies)
        {
            var q = w.GlobalPosition;
            _gSwept++;
            if (q.X < _gMin.X - SweepEps || q.X > _gMax.X + SweepEps ||
                q.Z < _gMin.Y - SweepEps || q.Z > _gMax.Y + SweepEps)
            {
                Fail($"WILD_OUTSIDE_EXTENT {w.Name} at ({q.X:0.###},{q.Y:0.##},{q.Z:0.###}) beyond ground x[{_gMin.X:0.##}..{_gMax.X:0.##}] z[{_gMin.Y:0.##}..{_gMax.Y:0.##}] ({when}) — a co-location landed off the ground");
                return;
            }
        }
    }

    /// <summary>The scene node that carries the terrain meta (the same channel
    /// the director collects — depth-first, first hit).</summary>
    private static Node3D? FindGroundMetaNode(Node from)
    {
        if (from is Node3D n3 && from.HasMeta("ground_extent")) return n3;
        foreach (var c in from.GetChildren())
            if (FindGroundMetaNode(c) is Node3D hit) return hit;
        return null;
    }
}
