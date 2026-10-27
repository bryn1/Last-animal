// Last Animal — MC 10404 floor-coverage proof, FLOOR_TRUST leg (MC 10516 / 10026.33 S38).
//
// Partial half of FloorCoverageProof: the capsule-footprint trust sweep moved
// VERBATIM out of the entry file by the MC 11432 clean-code R2 split — ZERO
// behaviour change; mode vocabulary, dispatch, prints and markers are the
// entry's. The shared boot loop and the ONE height-field reader + world<->pixel
// mapping mirror (LoadHeightField/VisualHeight) live in FloorCoverageProof.cs:
// this file is the leg that CONSUMES them — the split moves the METHOD, it
// does not fork the mapping (the S38 doc's stated hazard). Modes doc: entry header.
//
// Run:  LA_GATE_MODE=FLOOR_TRUST $GODOT --headless --path <proj> --script res://ci_proofs/FloorCoverageProof.cs
using Godot;

public partial class FloorCoverageProof : SceneTree
{    // FLOOR_TRUST (MC 10516/S38): the CAPSULE-FOOTPRINT support probe — a
    // center ray plus four offsets at the player capsule radius (this build's
    // shape/point queries answer zero on HeightMapShape3D — mechanism probe
    // recorded, see the modes doc; the mover still contacts the ground, so the
    // footprint set mirrors what the mover can stand on). Interior step 6 m:
    // the 2 m RAY grid above is the shipped interior density (green, battery);
    // this leg's density lives on the EDGES (the dead-ring class), the interior
    // step only cross-checks the idiom. Edge lines at 1 m: 0.75 m INSIDE the
    // visual border (inside-span: trusted floor required) and 0.75 m OUTSIDE
    // (no floor claim — BLOCKED/kill-planed class, logged, not violations).
    private const float TrustStepM = 6.0f;
    private const float TrustEdgeStepM = 1.0f;
    private const float TrustEdgeOffsetM = 0.75f;
    private const float TrustCastRadiusM = 0.4f;   // == the player capsule radius
    // FLOOR_TRUST accumulators (MC 10516/S38): same sweep-then-fail-at-end
    // shape as the coverage classes — RED must name EVERY violating zone.
    private int _totalTrustProbes, _totalTrustEdgeProbes, _totalTrustViolations, _totalTrustOutside;
    private bool _trustViolationFound;
    private string _firstTrustViolation = "";
    // FLOOR_TRUST (MC 10516 / 10026.33 S38): the shape-cast trust sweep for
    // the zone booted under _idx. One query object per zone (MC 10026.30 leak
    // law): a capsule-radius sphere swept top->bottom, hits FILTERED to the
    // terrain StaticBody itself — a decorative prop's collider over a floorless
    // column is exactly the false trust this leg must not count.
    private void ProbeTrust()
    {
        string zoneName = _scenePaths[_idx];
        var ground = _zone?.GetNodeOrNull<Node3D>("Ground");
        var visual = ground?.GetNodeOrNull<MeshInstance3D>("TerrainVisual");
        if (ground == null || visual?.Mesh == null)
        {
            GD.Print($"FLOOR_TRUST skip {zoneName}: no terrain_builder ground (Ground/TerrainVisual absent) — decor-only scene");
            return;
        }
        var terrainBody = ground.GetNodeOrNull<StaticBody3D>("TerrainCollision");
        var hms = ground.GetNodeOrNull<CollisionShape3D>("TerrainCollision/Shape")?.Shape as HeightMapShape3D;
        if (terrainBody == null || hms == null)
        { Fail($"{zoneName}: FLOOR_TRUST needs the live HeightMapShape3D at Ground/TerrainCollision/Shape — a trust grid without it is vacuous (never silent)"); return; }
        // QUERY-MECHANISM PROBE (MC 10516, recorded .tmp/s38/query-mechanism-diag.log):
        // this engine build answers RAYS against a HeightMapShape3D but Intersect-
        // Shape / IntersectPoint / GetRestInfo return ZERO hits over ground rays
        // hit (the mover itself still collides — MoveAndSlide contacts, proven by
        // this leg's edge-walk phase + GROUND_CLAMP's PLAYER_EDGEHOVER). The card
        // text sanctions "ray/shape-cast": trust density here is the CAPSULE-
        // FOOTPRINT ray set (center + 4 offsets at the player capsule radius —
        // the mover's own support geometry), not a sphere sweep the server
        // cannot answer.
        var hfield = LoadHeightField(ground, out int w, out int d, out float hSc);
        if (hfield == null)
        { Fail($"{zoneName}: FLOOR_TRUST cannot mirror the visual field — no heightmap on Ground (never silent)"); return; }
        Aabb ab = visual.Mesh.GetAabb();
        if (Root.GetWorld3D() != visual.GetWorld3D())
        { Fail($"{zoneName}: probe space mismatch (zone is not in the root World3D)"); return; }
        var space = Root.GetWorld3D().DirectSpaceState;

        // (a) THE 256 PROBE (S38 item 3): canyon/ruins are 256x256-pixel maps;
        // build_collision resamples them to ceil(size)+2 cells PER METRE
        // (514x514 / 412x412). Whether the physics server takes that verbatim
        // is an engine question — read it back against terrain_builder's OWN
        // formula and RECORD it either way. A CLAMPED verdict fails: the card's
        // downsample/clamp would then be owed in build_collision, not here.
        int expW = (int)System.Math.Ceiling(w * hSc) + 2;
        int expD = (int)System.Math.Ceiling(d * hSc) + 2;
        bool accepted = hms.MapWidth == expW && hms.MapDepth == expD && hms.MapData.Length == expW * expD;
        GD.Print($"FLOOR_TRUST_PROBE256 {zoneName}: src={w}x{d}px h_scale={hSc:0.##} cells-set={expW}x{expD} readback={hms.MapWidth}x{hms.MapDepth} data={hms.MapData.Length} verdict={(accepted ? "OK" : "CLAMPED")}");
        if (!accepted)
        { Fail($"FLOOR_TRUST_PROBE256 {zoneName}: physics did NOT take the {expW}x{expD}-cell height map verbatim (readback {hms.MapWidth}x{hms.MapDepth}, data={hms.MapData.Length}) — S38's downsample/clamp is owed in terrain_builder.build_collision"); return; }

        float top = ab.End.Y + 1.0f;
        float bottom = ab.End.Y - (ab.Size.Y + 1.0f + FallToleranceM);
        int probes = 0, edge = 0, viol = 0, outside = 0;
        float maxOff = 0f;

        var q = new PhysicsRayQueryParameters3D { CollideWithBodies = true, CollisionMask = uint.MaxValue };
        float R = TrustCastRadiusM;
        void Trust(float x, float z, bool edgePoint)
        {
            if (edgePoint) edge++; else probes++;
            // Capsule-footprint support: CENTER + four offsets at the capsule
            // radius. onTerrain = the mover's own footprint (any ray of the set
            // finding terrain); hitY = highest terrain contact; a column whose
            // SET finds nothing supports nothing — the violation class.
            bool onTerrain = false;
            float hitY = float.MinValue;
            for (int k = 0; k < 5; k++)
            {
                float ox = k == 1 ? R : k == 2 ? -R : 0f;
                float oz = k == 3 ? R : k == 4 ? -R : 0f;
                q.From = new Vector3(x + ox, top, z + oz);
                q.To = new Vector3(x + ox, bottom, z + oz);
                var hit = space.IntersectRay(q);
                if (hit.Count == 0) continue;
                if (hit["collider"].AsGodotObject() as CollisionObject3D != terrainBody) continue;   // decor masking a floorless column is NOT trust
                onTerrain = true;
                float y = hit["position"].AsVector3().Y;
                if (y > hitY) hitY = y;
            }
            bool insideSpan = x >= ab.Position.X - 0.001f && x <= ab.End.X + 0.001f &&
                              z >= ab.Position.Z - 0.001f && z <= ab.End.Z + 0.001f;
            if (!onTerrain)
            {
                // The heightfield claims ground here (the visual mesh spans
                // it). No terrain collision under it is the S38 violation —
                // UNLESS the point is OUTSIDE the visual span: there the game
                // answer is the ground-extent clamp BLOCKING motion, or a
                // faller the kill plane RESCUES and LOGS (KILL_PLANE_RESCUE,
                // WorldDirector._Process; fall_recovered + FLOOR_TRUST live
                // legs) — accounted below, never silent, never a violation.
                if (insideSpan)
                {
                    viol++;
                    if (_firstTrustViolation == "")
                        _firstTrustViolation = $"{zoneName} @ ({x:0.##},{z:0.##}): visual heightfield says ground (visual y={VisualHeight(hfield, w, d, hSc, x, z):0.###}) but the live collision query finds NOTHING";
                }
                else outside++;
                return;
            }
            if (!edgePoint || !insideSpan) return;   // height mirror asserted on inside-edge points (the v_scale*0.5 class, S38 item 2 recheck)
            float off = hitY - VisualHeight(hfield, w, d, hSc, x, z);
            float ad = System.MathF.Abs(off);
            if (ad > maxOff) maxOff = ad;
            if (ad >= HeightTolM)
            {
                viol++;
                if (_firstTrustViolation == "")
                    _firstTrustViolation = $"{zoneName} @ ({x:0.##},{z:0.##}): floor found {off:0.###} m off the VISIBLE surface (tol {HeightTolM:0.##}) — the y-offset class";
            }
        }

        // (b1) interior shape-grid cross-check (2 m RAY density is FLOOR_COVERAGE's
        // shipped interior; this proves the same answer with the mover's own
        // contact geometry).
        for (float x = ab.Position.X; x <= ab.End.X + 0.001f; x += TrustStepM)
            for (float z = ab.Position.Z; z <= ab.End.Z + 0.001f; z += TrustStepM)
                Trust(x, z, false);
        // (b2) the scripted map-edge WALK: the border line itself (collider
        // overshoot >= a full sample makes it strictly interior by design —
        // MC 10404), a 0.75 m INSET (the dead-ring class the h_scale bug
        // created: visible ground, no floor) and a 0.75 m OUTSET (past-edge:
        // blocked/kill-planed class, counted, logged).
        float x0 = ab.Position.X, x1 = ab.End.X, z0 = ab.Position.Z, z1 = ab.End.Z;
        float offM = TrustEdgeOffsetM;
        for (float z = z0; z <= z1 + 0.001f; z += TrustEdgeStepM)
        {
            Trust(x0 - offM, z, true); Trust(x0, z, true); Trust(x0 + offM, z, true);
            Trust(x1 - offM, z, true); Trust(x1, z, true); Trust(x1 + offM, z, true);
        }
        for (float x = x0; x <= x1 + 0.001f; x += TrustEdgeStepM)
        {
            Trust(x, z0 - offM, true); Trust(x, z0, true); Trust(x, z0 + offM, true);
            Trust(x, z1 - offM, true); Trust(x, z1, true); Trust(x, z1 + offM, true);
        }
        q.Dispose();   // MC 10026.30 leak law: release the shared native query before the next zone boots

        GD.Print($"FLOOR_TRUST {zoneName}: probes={probes} edge-walk={edge} violations={viol} max-edge-off={maxOff:0.###} past-extent={outside}");
        _totalTrustProbes += probes;
        _totalTrustEdgeProbes += edge;
        _totalTrustViolations += viol;
        _totalTrustOutside += outside;
        _probedZones++;
        if (viol > 0) _trustViolationFound = true;   // sweep on: RED names EVERY violating zone
    }

}
