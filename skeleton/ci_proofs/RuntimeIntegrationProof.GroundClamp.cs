using Godot;
using LastAnimal.Combat;
using LastAnimal.World;
using System.Collections.Generic;
using System.Linq;

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
//   2. every live enemy/wild/FOLLOWER body position is swept against the
//      extent EVERY frame — a beyond-extent position FAILS the leg naming
//      its coordinates (the planted-bad +100 m clamp widening walks straight
//      into this); cycle 2 (DA wall F-DA1) extends the ONE sweep to
//      _bodies/FollowerBodies — recruited followers are structurally outside
//      the Enemies+WildBodies lists cycle 1 walked;
//   3. the wild-spawn co-location (the calm choreography's mechanism) must
//      land ON the ground even when requested past the edge;
//   4. RECRUIT-THEN-EDGE-HOVER (cycle 2, the F-DA1 lure): the wild of step 3
//      is recruited through the PRODUCTION wire (interact offer + pay_wage
//      first wage), the player holds 2 m inside the +X edge so the recruited
//      follower's arrival point is TARGET + FollowDistance = edge + 1.5 — it
//      must PIN at the extent (30 frames under the follow lerp's continued
//      pressure; without the injected follow clamp it settles beyond-extent
//      and the sweep names it), and the PLAYER itself must stand PAST the
//      extent untouched for 30 frames (rescue-not-prevention: the clamp binds
//      companions, never the player).
//
// Markers: GROUND_CLAMP_AABB, ENEMY_GROUNDPIN, WILD_SPAWN_CLAMPED,
// FOLLOWER_EDGEHOVER, PLAYER_UNCLAMPED, GROUND_CLAMP_SWEEP, GROUND_CLAMP_CLEAR.
// Zero FALL_RECOVERED inside this leg is asserted battery-side (the leg never
// places an actor below the plane).
public partial class RuntimeIntegrationProof : SceneTree
{
    private int _gPhase, _gFrames, _gPin, _gSwept;
    private EnemyActor? _gTarget;
    private CompanionFollowBody? _gWild;      // the phase-2 wild, RECRUITED in phase 3 (F-DA1)
    private int _gHoverSub, _gPayBefore, _gPin2, _gInteractToggle;
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
            case 3: GRecruitEdge(); break;   // MC 10420 c2 (F-DA1)
            case 4: GFinish(); break;
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
        // Co-locate the player onto it — the calm idiom verbatim — and carry the
        // wild into the RECRUIT case below: it stands PINNED at the +X edge, the
        // exact spawn placement F-DA1's lure starts from.
        _playerBody.GlobalPosition = w.GlobalPosition;
        _gWild = w;
        _gPhase = 3; _gFrames = 0; _gHoverSub = 0;
    }

    /// <summary>MC 10420 cycle 2 (DA wall F-DA1): the RECRUIT-THEN-EDGE-HOVER
    /// case — the body class the cycle-1 sweep was STRUCTURALLY blind to (a
    /// recruited follower leaves Enemies/WildBodies for _bodies/FollowerBodies
    /// while remaining the ONE position-writing motion path outside the two
    /// swept lists). Drives the PRODUCTION wire end-to-end (interact offer +
    /// pay_wage FIRST wage), then holds the player 2 m inside the +X edge: the
    /// follow lerp's arrival point is TARGET + FollowDistance along the live
    /// offset = edge + 1.5 — without the director-injected follow clamp
    /// (WireFollowClamp) the body settles past the extent on the floorless
    /// ring, floating, luring the player (the x=66.69 fall-lure class, DA's
    /// 51.5 figure) and the every-frame sweep FAILS naming the FOLLOWER
    /// position (the plant-red walks into it); with the clamp it PINS at the
    /// edge (30 frames under continued lerp pressure). Ends with the DoD-1
    /// player probe: the clamp binds companions, NEVER the player.</summary>
    private void GRecruitEdge()
    {
        SweepInside("recruit-hover");
        if (_gWild == null) { Fail("GROUND_CLAMP: phase 2 lost its wild body"); return; }
        var roster = _director!.RosterView;
        float mid = (_gMin.Y + _gMax.Y) * 0.5f;
        switch (_gHoverSub)
        {
            case 0:   // speak to the edge wild: the interact scan marks the offer
                SPress("interact", ref _gInteractToggle);
                if (_gWild.RecruitOffered)
                {
                    Input.ActionRelease("interact");
                    _gPayBefore = roster.Count;
                    Input.ActionPress("pay_wage");
                    _gHoverSub = 1; _gFrames = 0;
                }
                else if (_gFrames > 300)
                {
                    Input.ActionRelease("interact");
                    Fail("GROUND_CLAMP: the interact offer never opened on the edge wild (recruit case cannot drive)");
                }
                break;

            case 1:   // the FIRST wage: the production join moves the body to _bodies
                if (roster.Count == _gPayBefore + 1)
                {
                    Input.ActionRelease("pay_wage");
                    Check("recruited body moved into FollowerBodies (the cycle-1 sweep could never see it)",
                        _director!.FollowerBodies.Contains(_gWild) && !_director.WildBodies.Contains(_gWild),
                        $"followers={_director.FollowerBodies.Count} wilds={_director.WildBodies.Count}");
                    GD.Print($"LA_GATE: GROUND_CLAMP recruit: {_gWild.Name} RECRUITED at the edge via pay_wage (followers={roster.Count}) — follow MOTION now live");
                    // The lure geometry, seeded ON the production placement (the
                    // wild stands CLAMPED at the edge; a test placement is not
                    // motion): the player 2 m inside, follower EAST of it, so the
                    // follow arrival point wants edge + 1.5 every frame.
                    _gWild.GlobalPosition = new Vector3(_gMax.X, _gWild.GlobalPosition.Y, mid);
                    _playerBody!.GlobalPosition = new Vector3(_gMax.X - 2f, 3f, mid);
                    _gHoverSub = 2; _gFrames = 0; _gPin2 = 0;
                }
                else if (_gFrames > 600)
                {
                    Input.ActionRelease("pay_wage");
                    Fail("GROUND_CLAMP: the pay press at the edge recruited nobody (offer/wage wire broken in this choreography)");
                }
                break;

            case 2:   // edge hover: the player holds 2 m inside, the lerp pressure
                      // east stays on — the body must PIN at the extent.
                {
                    float fx = _gWild!.GlobalPosition.X;
                    if (fx >= _gMax.X - 0.3f) _gPin2++;
                    if (_gPin2 >= 30)
                    {
                        Check("recruited follower pinned AT the extent (the follow clamp BIT — arrival point edge+1.5 unclamped)",
                            fx <= _gMax.X + SweepEps,
                            $"follower x={fx:0.###} vs edge {_gMax.X:0.###}");
                        if (_failed) return;
                        GD.Print($"LA_GATE: FOLLOWER_EDGEHOVER body={_gWild!.Name} x={fx:0.###} edge={_gMax.X:0.###} player x={_playerBody!.GlobalPosition.X:0.###} (follow target wants edge+1.5 — pinned under 30 frames of lerp pressure)");
                        // DoD-1 player probe, raw write (the production write
                        // style): 0.5 m PAST the meta extent — still ON the
                        // collider overshoot (mw = ceil+2, ~1 m/side), so the
                        // probe STANDS, it never falls, the census stays 1/1.
                        _playerBody.GlobalPosition = new Vector3(_gMax.X + 0.5f, 3f, mid);
                        _gHoverSub = 3; _gFrames = 0;
                    }
                    else if (_gFrames > 900)
                    {
                        Fail($"GROUND_CLAMP: the recruited follower never pinned at the edge — reached only x={fx:0.###} of {_gMax.X:0.###} in 900 frames");
                    }
                }
                break;

            case 3:   // the player itself stays UNCLAMPED: past the extent for 30
                      // frames while the swept companions stay inside it (any
                      // accidental player clamp pulls x back — RED here).
                if (_gFrames >= 30)
                {
                    var p = _playerBody!.GlobalPosition;
                    Check("player itself stays UNCLAMPED past the extent (rescue-not-prevention)",
                        p.X >= _gMax.X + 0.4f,
                        $"player x={p.X:0.###} requested {_gMax.X + 0.5f:0.#} held past edge {_gMax.X:0.###} for {_gFrames} frames — must NOT be pulled back");
                    if (_failed) return;
                    GD.Print($"LA_GATE: PLAYER_UNCLAMPED player x={p.X:0.###} past edge {_gMax.X:0.###} for {_gFrames} frames — the follow clamp binds companions only, the player stays rescue-governed");
                    _playerBody.GlobalPosition = _director!.ClampToGround(new Vector3(_gMax.X - 3f, 3f, mid));
                    _gPhase = 4; _gFrames = 0;
                }
                break;
        }
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
        GD.Print("LA_GATE: PASS — ground-extent clamp: enemy MOTION pins at the loaded edge under live flee pressure, wild co-locations land on the ground, a recruited follower edge-hovers PINNED at the edge under follow pressure while the player stands unclamped past it, every enemy/wild/follower position stayed inside the extent");
        _asserted = true;
        ReleaseHeldRefsBeforeQuit();
        Quit(0);
    }

    /// <summary>The DoD-1 probe assertion: EVERY live enemy + wild + FOLLOWER
    /// body, EVERY physics frame of the leg, inside the ground extent — a
    /// violation names the exact beyond-extent position and exits 1 (the
    /// plant-red walks into this line the frame the widened clamp lets it past
    /// the edge). MC 10420 cycle 2 (F-DA1): the FOLLOWER list joined the sweep
    /// — the RECRUITED bodies live in _bodies/FollowerBodies, structurally
    /// OUTSIDE the two lists cycle 1 swept, which is exactly why the DA wall
    /// pinned the follow lerp as the one live unswept position writer.</summary>
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
        foreach (var f in _director.FollowerBodies)
        {
            var q = f.GlobalPosition;
            _gSwept++;
            if (q.X < _gMin.X - SweepEps || q.X > _gMax.X + SweepEps ||
                q.Z < _gMin.Y - SweepEps || q.Z > _gMax.Y + SweepEps)
            {
                Fail($"FOLLOWER_OUTSIDE_EXTENT {f.Name} at ({q.X:0.###},{q.Y:0.##},{q.Z:0.###}) beyond ground x[{_gMin.X:0.##}..{_gMax.X:0.##}] z[{_gMin.Y:0.##}..{_gMax.Y:0.##}] ({when}) — follow MOTION escaped the loaded ground (the F-DA1 lure: a floating companion past the edge)");
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
