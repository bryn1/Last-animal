// SIZE: 267 l by wc AT this head (created MC 10516; +13 the DA-wall F1+F2 fix-forward (F1
// settle-comment, F2 gate-comment) on top of the merge-head 254. CI proof partial, ONE concern:
// scripted map-edge walk, the clamp-boundary demo and the edge-fall kill-plane
// rescue, all DRIVEN by the one composed scene (a split would orphan a check
// from the scene that drives it — GROUND_CLAMP precedent).
using Godot;
using System.Collections.Generic;

// Last Animal — MC 10516 / 10026.33 card S38 floor-trust LIVE leg (code, 2026-10-12).
//
// Partial half of RuntimeIntegrationProof: mode FLOOR_TRUST (stage 105) proves
// on the COMPOSED scene what the ratified D2 §5 card demands the game itself
// guarantees at its edges — quiet boot (bus_emit idiom: no enemy can interfere
// with a leg that parks the player on bare map borders):
//
//   0. ARMED tripwires (never-silent idiom): the ground-extent clamp and the
//      kill plane both resolve from the one terrain meta channel
//      (WorldDirector.Ground.cs) — an unarmed guarantee fails HERE, named;
//   1. a scripted map-edge WALK with the mover's own support geometry (the
//      CAPSULE-FOOTPRINT ray set — center + four offsets at the 0.4 m capsule
//      radius; the sphere sweep FloorCoverageProof drafted first answers ZERO
//      on HeightMapShape3D in this build, probe .tmp/s38/query-mechanism-
//      diag.log; the card's "ray/shape-cast" wording sanctions the ray idiom),
//      the SAME visual<->pixel mapping REUSED from FloorCoverageProof, never a
//      second one): every border column at 1 m, a 0.75 m inset line (the old
//      dead-ring class the h_scale footprint bug created) and a 0.75 m outset
//      line; inside the visual span every point MUST have terrain collision
//      within fall tolerance — ZERO visible-ground-without-collision points
//      required (planted-RED pair: one query point displaced under the map,
//      the leg REDs naming it, transcripts in .tmp/s38); past the span the
//      game's two answers are accounted, never silent: the extent clamp
//      BLOCKS motion (phase 2) and the kill plane RESCUES + LOGS fallers
//      (phase 3, KILL_PLANE_RESCUE with zone + reason, WorldDirector._Process);
//      then the PLAYER literally walks 8 border waypoints (corners + edge
//      mids) under gravity and must stay standing at every one;
//   2. ClampToGround PAST the +X edge must land ON the extent — the bound
//      bites (FLOOR_TRUST_BLOCKED printed; a silent no-op clamp is RED);
//   3. the edge fall: the player is placed 6 m past the east edge (no ground
//      exists there) and must be RESCUED back to the zone entry byte-exact in
//      XZ with Life intact and no re-fall over 60 settle frames — the kill
//      plane's guarantee, driven by FALLING OFF THE MAP EDGE itself (fall_
//      recovered drops below-plane mid-map; this is the owner's walk-off
//      scenario verbatim).
//
// Markers: FLOOR_TRUST_EDGEWALK, FLOOR_TRUST_BLOCKED, FLOOR_TRUST_LIVE_CLEAR;
// the product line KILL_PLANE_RESCUE zone=<zone> reason=below-floor-plane +
// the census-owned FALL_RECOVERED line print from the product (grepped
// battery-side; this leg's fall is the SECOND and LAST census hit, MC 10516).
public partial class RuntimeIntegrationProof : SceneTree
{
    private int _ftPhase, _ftFrames, _ftWalkIdx, _ftSub;
    private int _ftProbes, _ftEdgeProbes, _ftViolations, _ftPastExtent;
    private float _ftMaxOff;
    private Vector3 _ftHome;
    private int _ftHealth0;
    private readonly List<Vector3> _ftWalk = new();
    private Aabb _ftAb;
    private Vector2 _ftMin, _ftMax;
    private float[]? _ftField; private int _ftW, _ftD; private float _ftHSc;

    private const float TrustEdgeStepM = 1.0f;     // border + 0.75 m lines at 1 m
    private const float TrustEdgeOffsetM = 0.75f;
    private const float TrustCastRadiusM = 0.4f;   // player capsule radius
    private const float TrustInteriorStepM = 4.0f; // composed-scene interior cross-check
    private const float TrustHeightTolM = 0.5f;    // == FloorCoverageProof's height tol
    // WalkSettleF — the DA wall F1 (P2): at gravity 9.8 (Player.cs:34) a body
    // placed at surface+0.35 with zero velocity and NO support needs t:
    // 4.9t^2 > 1.15 m (placement height + the 0.8 band) => t > 0.485 s => 30
    // frames MINIMUM before the y > surface-0.8 assert can fire; the shipped
    // 10(+1) let it drop ~0.17 m — the assert could NEVER fail (vacuous by
    // arithmetic; the next waypoint's teleport reset the fall). 40 frames
    // (t=0.667 s, drop 2.18 m) keeps ~1 m margin; a SUPPORTED body is long
    // settled by frame 40 (measured standing at ~+0.17 over surface at 11).
    private const float WalkSettleF = 40f;         // frames to settle a walk waypoint (F1 fix-forward)
    private const float DropPastEdgeM = 6.0f;      // past collider overshoot (0.5) -> free fall

    private void RunFloorTrustStage()
    {
        if (_director == null || _playerBody == null || _failed) return;
        _ftFrames++;
        switch (_ftPhase)
        {
            case 0: FtWalk(); break;
            case 1: FtPlayerWalk(); break;
            case 2: FtClampBlocked(); break;
            case 3: FtEdgeFall(); break;
            case 4: FtSettle(); break;
            case 5: FtFinish(); break;
        }
    }

    private void FtWalk()
    {
        Check("ground extent ARMED (S38 tripwire)", _director!.HasGroundExtent,
            "ground_extent meta unresolved — the clamp AND the kill plane are DISARMED (never silent)");
        _ftMin = _director.GroundExtentMin; _ftMax = _director.GroundExtentMax;
        var metaNode = FindGroundMetaNode(_main!);
        var visual = metaNode?.GetNodeOrNull<MeshInstance3D>("TerrainVisual");
        var terrainBody = metaNode?.GetNodeOrNull<StaticBody3D>("TerrainCollision");
        Check("visible ground + terrain body found under the meta node",
            visual?.Mesh != null && terrainBody != null,
            $"metaNode={metaNode?.Name ?? "none"} (Ground/TerrainVisual + Ground/TerrainCollision required)");
        if (_failed) return;
        _ftAb = visual!.Mesh!.GetAabb();
        _ftField = FloorCoverageProof.LoadHeightField(metaNode!, out _ftW, out _ftD, out _ftHSc);
        Check("visual height field readable (FloorCoverageProof's mapping REUSED)", _ftField != null, "no heightmap on the ground node");
        if (_failed) return;

        _ftHome = _playerBody!.GlobalPosition;
        _ftHealth0 = _director.PlayerModel.Health;
        var space = Root.GetWorld3D().DirectSpaceState;
        // CAPSULE-FOOTPRINT RAY SET (center + 4 offsets at the capsule radius):
        // this build's IntersectShape/IntersectPoint/GetRestInfo answer ZERO on
        // HeightMapShape3D where rays hit (probe .tmp/s38/query-mechanism-diag.log,
        // same finding FloorCoverageProof's FLOOR_TRUST doc carries) — the card's
        // "ray/shape-cast" wording sanctions the ray idiom. Hits are FILTERED to
        // the terrain body: a decorative prop over a floorless column is the
        // false trust this leg must not count.
        var q = new PhysicsRayQueryParameters3D { CollideWithBodies = true, CollisionMask = uint.MaxValue };
        float top = _ftAb.End.Y + 1.0f, bottom = _ftAb.End.Y - (_ftAb.Size.Y + 1.0f + 2.0f);
        float R = TrustCastRadiusM;

        void Trust(float x, float z, bool edgePoint)
        {
            if (edgePoint) _ftEdgeProbes++; else _ftProbes++;
            bool onTerrain = false; float hitY = float.MinValue;
            for (int k = 0; k < 5; k++)
            {
                float ox = k == 1 ? R : k == 2 ? -R : 0f;
                float oz = k == 3 ? R : k == 4 ? -R : 0f;
                q.From = new Vector3(x + ox, top, z + oz);
                q.To = new Vector3(x + ox, bottom, z + oz);
                var hit = space.IntersectRay(q);
                if (hit.Count == 0) continue;
                if (hit["collider"].AsGodotObject() as CollisionObject3D != terrainBody) continue;
                onTerrain = true;
                float hy = hit["position"].AsVector3().Y;
                if (hy > hitY) hitY = hy;
            }
            bool inside = x >= _ftAb.Position.X - 0.001f && x <= _ftAb.End.X + 0.001f &&
                          z >= _ftAb.Position.Z - 0.001f && z <= _ftAb.End.Z + 0.001f;
            if (!onTerrain)
            {
                if (inside) FtViolate($"({x:0.##},{z:0.##}) visual heightfield says ground (y={FloorCoverageProof.VisualHeight(_ftField!, _ftW, _ftD, _ftHSc, x, z):0.###}) but the live collision query finds NOTHING");
                else _ftPastExtent++;
                return;
            }
            // F2 fix-forward: SAME gate as FloorCoverageProof's height assert
            // ("!edgePoint || !insideSpan") — OUTSET points sit past the visual
            // border, where the mapping CLAMPS to the border pixel; asserting a
            // clamped value is the same check run two ways (false-RED
            // fragility: a wall will trip it).
            if (!edgePoint || !inside) return;   // height mirror asserted on INSIDE-edge points only (S38 item-2 class on the LIVE scene)
            float off = hitY - FloorCoverageProof.VisualHeight(_ftField!, _ftW, _ftD, _ftHSc, x, z);
            if (System.MathF.Abs(off) > _ftMaxOff) _ftMaxOff = System.MathF.Abs(off);
            if (System.MathF.Abs(off) >= TrustHeightTolM) FtViolate($"({x:0.##},{z:0.##}) floor {off:0.###} m OFF the visible surface (tol {TrustHeightTolM:0.##}) — the y-offset class");
        }
        void FtViolate(string why)
        {
            _ftViolations++;
            GD.Print($"LA_GATE: check: FLOOR_TRUST visible-ground-without-collision: FAIL {why}");
        }

        float x0 = _ftAb.Position.X, x1 = _ftAb.End.X, z0 = _ftAb.Position.Z, z1 = _ftAb.End.Z;
        for (float x = x0; x <= x1 + 0.001f; x += TrustInteriorStepM)
            for (float z = z0; z <= z1 + 0.001f; z += TrustInteriorStepM)
                Trust(x, z, false);
        for (float z = z0; z <= z1 + 0.001f; z += TrustEdgeStepM)
        {
            Trust(x0 - TrustEdgeOffsetM, z, true); Trust(x0, z, true); Trust(x0 + TrustEdgeOffsetM, z, true);
            Trust(x1 - TrustEdgeOffsetM, z, true); Trust(x1, z, true); Trust(x1 + TrustEdgeOffsetM, z, true);
        }
        for (float x = x0; x <= x1 + 0.001f; x += TrustEdgeStepM)
        {
            Trust(x, z0 - TrustEdgeOffsetM, true); Trust(x, z0, true); Trust(x, z0 + TrustEdgeOffsetM, true);
            Trust(x, z1 - TrustEdgeOffsetM, true); Trust(x, z1, true); Trust(x, z1 + TrustEdgeOffsetM, true);
        }
        q.Dispose();   // MC 10026.30 leak law: release before any Fail path below

        GD.Print($"FLOOR_TRUST_EDGEWALK meadow: interior={_ftProbes} edge={_ftEdgeProbes} violations={_ftViolations} max-edge-off={_ftMaxOff:0.###} past-extent={_ftPastExtent}");
        if (_ftViolations > 0)
            Fail($"FLOOR_TRUST {_ftViolations} visible-ground-without-collision points on the composed scene (first named above) — zero required; past-extent points are clamp-BLOCKED or kill-planed, never silent");
        if (_failed) return;

        _ftWalk.Add(new Vector3(x0, 0, z0)); _ftWalk.Add(new Vector3((x0 + x1) * 0.5f, 0, z0)); _ftWalk.Add(new Vector3(x1, 0, z0));
        _ftWalk.Add(new Vector3(x1, 0, (z0 + z1) * 0.5f)); _ftWalk.Add(new Vector3(x1, 0, z1));
        _ftWalk.Add(new Vector3((x0 + x1) * 0.5f, 0, z1)); _ftWalk.Add(new Vector3(x0, 0, z1));
        _ftWalk.Add(new Vector3(x0, 0, (z0 + z1) * 0.5f));
        _ftPhase = 1; _ftSub = -1;
    }

    private void FtPlayerWalk()
    {
        var wp = _ftWalk[_ftWalkIdx];
        float surface = FloorCoverageProof.VisualHeight(_ftField!, _ftW, _ftD, _ftHSc, wp.X, wp.Z);
        if (_ftSub < 0)   // place, then settle under REAL gravity
        {
            _playerBody!.GlobalPosition = new Vector3(wp.X, surface + 0.35f, wp.Z);
            if (_playerBody is CharacterBody3D cb) cb.Velocity = Vector3.Zero;
            _ftSub = 0;
            return;
        }
        if (++_ftSub <= WalkSettleF) return;
        float y = _playerBody!.GlobalPosition.Y;
        Check($"edge waypoint {_ftWalkIdx} holds the player (walked to ({wp.X:0.#},{wp.Z:0.#}))",
            y > surface - 0.8f, $"y={y:0.###} surface={surface:0.###} — the point looked trusted but the body fell through");
        if (_failed) return;
        _ftWalkIdx++; _ftSub = -1;
        if (_ftWalkIdx >= _ftWalk.Count) _ftPhase = 2;
    }

    private void FtClampBlocked()
    {
        var req = new Vector3(_ftMax.X + 10f, _ftHome.Y, _ftHome.Z);
        var cl = _director!.ClampToGround(req);
        GD.Print($"FLOOR_TRUST_BLOCKED class: requested x={req.X:0.#} landed x={cl.X:0.##} at extent max x={_ftMax.X:0.##} — past-extent motion is CLAMP-BOUND (runtime answer #1); a faller past the edge is kill-planed + LOGGED (answer #2)");
        Check("ClampToGround binds past-extent motion (never a silent no-op)", cl.X == _ftMax.X, $"requested {req.X:0.#} landed {cl.X:0.##}");
        _ftPhase = 3; _ftFrames = 0;
    }

    private void FtEdgeFall()
    {
        if (_ftFrames == 1)
        {
            _playerBody!.GlobalPosition = new Vector3(_ftMax.X + DropPastEdgeM, _ftHome.Y + 1f, _ftHome.Z);
            if (_playerBody is CharacterBody3D cb) cb.Velocity = Vector3.Zero;
            GD.Print($"LA_GATE: floor_trust — player placed OFF THE MAP EDGE at x={_ftMax.X + DropPastEdgeM:0.#} (no ground exists there); the kill plane must rescue to the zone entry");
            return;
        }
        var p = _playerBody!.GlobalPosition;
        if (p.X == _ftHome.X && p.Z == _ftHome.Z)   // XZ byte-exact at the spawn column: the rescue fired (the fall keeps x pinned past the edge — no input is pressed in this mode; a Y-window check would false-fire in the drop frames, the P1Fix lesson below-plane idiom)
        {
            Check("KILL_PLANE_RESCUE XZ: the edge fall returned the player to the zone-entry column",
                p.Y > _ftHome.Y - 0.5f, $"pos={p} home={_ftHome}");   // XZ byte-exact (the P1Fix FALL_RECOVERED_XZ precedent does NOT byte-assert Y — snap noise; standing-height band instead)
            Check("KILL_PLANE_RESCUE HEALTH: the edge fall costs no Life",
                _director!.PlayerModel.Health == _ftHealth0, $"health={_director.PlayerModel.Health} atDrop={_ftHealth0}");
            if (_failed) return;
            _ftPhase = 4; _ftFrames = 0;
        }
        else if (_ftFrames > 180)
            Fail("no kill-plane rescue 180 frames after the OFF-EDGE drop (player still below the world — the guarantee missed the map-edge class)");
    }

    private void FtSettle()
    {
        var p = _playerBody!.GlobalPosition;
        // THE FLOOR, NOT THE BOOT HOME: respawn drops at the zone entry settle to
        // floor+capsule on the spawn column's own surface (iteration 1 measured
        // settle y=1.562 vs the boot home 2.630 — the boot frame caught the body
        // mid-settling above a prop that nothing respawns). Assert standing on
        // the SURFACE (waypoint-class tolerance, Y never byte-asserted — P1Fix
        // precedent); XZ keeps the tight drift band (SweepEps precedent — the
        // byte-exact claim lives at the recovery moment, this is the aftermath).
        float surfaceHome = FloorCoverageProof.VisualHeight(_ftField!, _ftW, _ftD, _ftHSc, p.X, p.Z);
        if (p.Y < surfaceHome - 0.8f || System.MathF.Abs(p.X - _ftHome.X) > 0.05f || System.MathF.Abs(p.Z - _ftHome.Z) > 0.05f)
            Fail($"re-fell or drifted off the zone entry after the kill-plane rescue (pos={p} surface={surfaceHome:0.###} home={_ftHome})");
        if (_ftFrames >= 60) _ftPhase = 5;
    }

    private void FtFinish()
    {
        GD.Print($"FLOOR_TRUST_LIVE meadow: edge-walk waypoints={_ftWalk.Count} probes={_ftProbes}+{_ftEdgeProbes} violations=0 past-extent={_ftPastExtent} (clamp-bound + kill-planed, accounted above); edge fall RESCUED byte-exact to the zone entry, Life intact — the KILL_PLANE_RESCUE line above names zone + reason");
        GD.Print($"FLOOR_TRUST_LIVE_CLEAR edges={_ftWalk.Count} probes={_ftProbes + _ftEdgeProbes} violations=0 edge-fall=logged-rescued");
        GD.Print("LA_GATE: PASS — floor-trust live leg: map-edge walk found zero visible-ground-without-collision points, past-extent motion is clamp-bound, an off-edge fall is kill-planed and LOGGED (zone + reason, never silent)");
        Quit(0);
    }
}
