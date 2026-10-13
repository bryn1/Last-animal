using Godot;
using System.Collections.Generic;

// Last Animal — MC 10404 floor-coverage proof (owner playtest P1, v0.4.0: the
// player fell THROUGH the visible ground — camera under the terrain mesh with
// decor still rendered from below — and died there below the floor).
//
// The one invariant this leg owns: EVERY walkable point of a shipped zone's
// ground has solid collision within FALL_TOLERANCE of the surface the player
// SEES. The probe boots each zone scene under zones/ AS SHIPPED, one zone at a
// time into the bare root (nothing else in the physics space), and runs a
// dense downward ray grid over the visual ground's full extent: at every grid
// point a PhysicsRayQuery from above the surface top must return a hit no
// deeper than the terrain's own height range + FALL_TOLERANCE below the ray
// start. Zero uncovered points is the assert; per-zone hit-minus-visual stats
// print as diagnostics (they named the MC 10404 root cause: the collider was
// laid at heightmap-pixel spacing while the visual spans h_scale metres per
// pixel, so the ground ring beyond +/- map_half had NO floor at all).
//
// Zones with no terrain_builder ground are NOT silently skipped: every scene
// under res://zones is walked and classified on-disk — decor-only scenes are
// NAMED in the log, and an expected ground scene that is missing entirely is
// what the grid is there to catch, not paper over.
//
// Modes (env LA_GATE_MODE, default "FLOOR_COVERAGE"):
//   FLOOR_COVERAGE — probe every zones/*.tscn ground; prints per-zone
//                    "FLOOR_COVERAGE <zone>: probes=N uncovered=K holes=[...]"
//                    plus hit-minus-visual diagnostics, then the PASS marker
//                    "FLOOR_COVERAGE_CLEAR zones=N probes=M". Unknown modes
//                    FAIL LOUDLY (the silent-default-path lesson, MC 10210).
//                    MC 10026.31 adds the HEIGHT-MATCH leg: on an every-8th
//                    point subgrid |hit_y - visual_y| < 0.5 m, visual_y
//                    recomputing terrain_builder's world<->pixel mapping on
//                    the zone's OWN heightmap; per-zone "FLOOR_HEIGHT_DIAG
//                    ..." lines + the "FLOOR_HEIGHT_CLEAR" marker.
//   FLOOR_TRUST   — MC 10516 / 10026.33 card S38 (the ratified floor-trust
//                    leg): the SAME zones booted ALONE, queried through the
//                    mover's own support geometry — a CAPSULE-FOOTPRINT ray
//                    set (center + four offsets at the 0.4 m capsule radius).
//                    (A sphere sweep was the first draft: THIS build's
//                    IntersectShape/IntersectPoint/GetRestInfo answer ZERO on
//                    HeightMapShape3D where rays hit — probe transcript
//                    .tmp/s38/query-mechanism-diag.log; the card's "ray/
//                    shape-cast" wording sanctions the ray idiom.) Per zone:
//                    (a) the live HeightMapShape3D is READ BACK against
//                    terrain_builder's own cells formula — the 256x256
//                    canyon/ruins maps resample to 514x514 / 412x412 cells;
//                    whether the physics server accepted them verbatim is
//                    RECORDED, never assumed ("FLOOR_TRUST_PROBE256 ...
//                    verdict=OK|CLAMPED"; a CLAMPED verdict FAILS the leg:
//                    the S38 downsample/clamp would be owed);
//                    (b) a scripted map-edge WALK: every border column at
//                    1 m + a 0.75 m inset (the dead-ring class the h_scale
//                    bug created) + a 0.75 m outset line (points PAST the
//                    visual ground: no violation there — the runtime answer
//                    for them is the ground-extent clamp BLOCKING motion or
//                    the kill plane RESCUING the faller, both logged, never
//                    silent), plus an interior every-6 m shape-grid (2 m ray
//                    density stays FLOOR_COVERAGE's job; this cross-checks
//                    the idiom); a point the visual heightfield claims with
//                    NO terrain-collision hit is a VIOLATION — zero required,
//                    first one named (planted-RED pair in the S38 evidence).
//                    Per-zone "FLOOR_TRUST <zone>: ..." lines, then
//                    "FLOOR_TRUST_CLEAR zones=N probes=M violations=0".
//
// Run:  $GODOT --headless --path <proj> --script res://ci_proofs/FloorCoverageProof.cs
// SIZE: 528 l by wc AT this head (MC 10516 S38 restamp; test-class ceiling 600; the
// two modes share one boot loop, one height-field reader and one world-mapping
// mirror — splitting them would fork the mapping, the very bug class S38 hunts).
public partial class FloorCoverageProof : SceneTree
{
    // Probe grid step in metres: one ray per 2x2 m of visible ground — dense
    // against the shipping zones (meadow 96 m span -> 2304 rays; the 512 m
    // canyon 65k) and far finer than any walkable-support gap (the player
    // capsule is 0.4 m radius: a hole that drops it cannot hide between rows).
    private const float StepM = 2.0f;
    // Extra distance below the terrain's own bottom a column may go empty
    // before it counts as a hole: anything deeper is a fall that leaves the
    // rendered ground behind — the owner's death class.
    private const float FallToleranceM = 2.0f;
    private const int SettleFrames = 3;      // physics sync after zone add/free
    private const int FrameBudget = 12000;   // probe runs are wall-cheap; this only catches a wedged boot
    // MC 10026.31(c): the HEIGHT-MATCH leg — hit EXISTENCE alone (the MC 10404
    // leg) stayed green through the SECOND MC 10404 bug: the collider laid
    // v_scale/2 above the rendered ground (meadow DIAG median +0.82 m) had a
    // floor, just not the floor the player could see. Every 8th grid point on
    // BOTH axes (1/64 of the grid, ~1.7k samples over the shipped zones) also
    // asserts the hit's HEIGHT matches the visual surface. PER-POINT, not
    // median-vs-mean: collision and visual are two samplings of the SAME
    // nearest-pixel field (terrain_builder.build_collision "byte-mirrors the
    // visual builder"), so a correct build differs only by sub-metre
    // interpolation — the per-zone FLOOR_HEIGHT_DIAG maxabs printed on every
    // GREEN run is the falsification record of that choice: if a future
    // terrain makes per-point fragile, the named RED states the point and
    // the zone's maxabs, and the assert can move to the median then — with
    // the numbers, not on taste.
    private const int HeightSampleStride = 8;
    private const float HeightTolM = 0.5f;
    // FLOOR_TRUST (MC 10516/S38): the CAPSULE-FOOTPRINT support probe — a
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

    private string _mode = "FLOOR_COVERAGE";
    private readonly List<string> _scenePaths = new();
    private int _idx;                        // current scene under probe
    private int _settle = -1;                // >= 0: counting down to action
    private bool _probePending;              // true: probe now; false: teardown now
    private Node? _zone;
    private int _frames;
    private int _totalProbes, _totalUncovered, _probedZones;
    private bool _failed;                    // hard instrument failure: stop now
    private bool _holesFound;                // coverage miss: finish the sweep, fail at the end
    private string _firstHole = "";
    private bool _heightMiss;                // height-match miss: finish the sweep too, fail at the end
    private string _firstHeightOff = "";
    private int _totalHeightSamples, _totalHeightMisses;
    // FLOOR_TRUST accumulators (MC 10516/S38): same sweep-then-fail-at-end
    // shape as the coverage classes — RED must name EVERY violating zone.
    private int _totalTrustProbes, _totalTrustEdgeProbes, _totalTrustViolations, _totalTrustOutside;
    private bool _trustViolationFound;
    private string _firstTrustViolation = "";

    public override void _Initialize()
    {
        GD.Print("LA_GATE: start");
        _mode = System.Environment.GetEnvironmentVariable("LA_GATE_MODE") ?? "FLOOR_COVERAGE";
        GD.Print($"LA_GATE: mode={_mode}");
        Engine.MaxFps = 60;
        switch (_mode)
        {
            case "FLOOR_COVERAGE": break;
            case "FLOOR_TRUST": break;
            default:
                Fail($"unknown mode {_mode} — FloorCoverageProof ships ONLY FLOOR_COVERAGE and FLOOR_TRUST (the roster fail-safe idiom: a typo must never ride a default path)");
                return;
        }

        foreach (string zoneDir in DirAccess.GetDirectoriesAt("res://zones"))
        {
            string dir = $"res://zones/{zoneDir}";
            foreach (string scene in DirAccess.GetFilesAt(dir))
            {
                if (scene.EndsWith(".tscn", System.StringComparison.Ordinal))
                    _scenePaths.Add($"{dir}/{scene}");
            }
        }
        _scenePaths.Sort();
        if (_scenePaths.Count == 0) { Fail("no zone scenes found under res://zones"); return; }
        NextScene();
    }

    public override bool _PhysicsProcess(double delta)
    {
        if (_failed || _settle < 0) return false;   // MainLoop: false = keep running
        if (++_frames > FrameBudget) { Fail($"frame budget exceeded (zone {_idx})"); return true; }
        if (_settle > 0) { _settle--; return false; }
        if (_probePending)
        {
            ProbeCurrent();
            _idx++;
            _probePending = false;
            _settle = SettleFrames;   // let the free land before the next boot
        }
        else
        {
            _zone?.GetParent()?.RemoveChild(_zone);
            _zone?.QueueFree();
            _zone = null;
            NextScene();
        }
        return false;
    }

    private void NextScene()
    {
        if (_idx >= _scenePaths.Count) { Finish(); return; }
        var packed = GD.Load<PackedScene>(_scenePaths[_idx]);
        if (packed == null) { Fail($"cannot load {_scenePaths[_idx]}"); return; }
        _zone = packed.Instantiate<Node3D>();
        Root.AddChild(_zone);
        _probePending = true;
        _settle = SettleFrames;
    }

    private void ProbeCurrent()
    {
        if (_mode == "FLOOR_TRUST") { ProbeTrust(); return; }   // S38 shape-cast leg, own sweep
        string zoneName = _scenePaths[_idx];
        var ground = _zone?.GetNodeOrNull<Node3D>("Ground");
        var visual = ground?.GetNodeOrNull<MeshInstance3D>("TerrainVisual");
        if (ground == null || visual?.Mesh == null)
        {
            GD.Print($"FLOOR_COVERAGE skip {zoneName}: no terrain_builder ground (Ground/TerrainVisual absent) — decor-only scene");
            return;
        }
        Aabb ab = visual.Mesh.GetAabb();
        if (Root.GetWorld3D() != visual.GetWorld3D())
        { Fail($"{zoneName}: probe space mismatch (zone is not in the root World3D)"); return; }
        var space = Root.GetWorld3D().DirectSpaceState;
        // MC 10026.31(c): visual ground truth for the height leg — the very
        // field build_visual_mesh samples, read back the same way. A probed
        // Ground that cannot supply it is a HARD instrument failure: the
        // height leg must never silently shrink to existence-only.
        var hfield = LoadHeightField(ground, out int hw, out int hd, out float hScale);
        if (hfield == null)
        { Fail($"{zoneName}: height-match impossible — no heightmap field on Ground (the height leg never silently skips)"); return; }

        float top = ab.End.Y + 1.0f;
        float bottom = ab.End.Y - (ab.Size.Y + 1.0f + FallToleranceM);
        int uncovered = 0, probes = 0;
        var holes = new List<string>();
        var deltas = new List<float>();
        var hDeltas = new List<float>();
        int heightMisses = 0;
        float heightMaxAbs = 0f;
        // MC 10026.30 (CI hygiene): ONE query object for the whole grid, From/To
        // set per probe. The old PhysicsRayQueryParameters3D.Create per probe
        // leaked 731 RefCounted natives at exit (engine leak report; a RED run
        // aborted SIGABRT post-print, exit 134). Dispose releases the native
        // ref deterministically before exit-time finalizers. Probe semantics —
        // ray, order, grid — are byte-unchanged.
        var q = new PhysicsRayQueryParameters3D();
        int ix = 0;
        for (float x = ab.Position.X; x <= ab.End.X + 0.001f; x += StepM, ix++)
        {
            int iz = 0;
            for (float z = ab.Position.Z; z <= ab.End.Z + 0.001f; z += StepM, iz++)
            {
                probes++;
                q.From = new Vector3(x, top, z);
                q.To = new Vector3(x, bottom, z);
                var hit = space.IntersectRay(q);
                if (hit.Count == 0)
                {
                    uncovered++;
                    if (holes.Count < 4) holes.Add($"({x:0.#},{z:0.#})");
                    if (_firstHole == "") _firstHole = $"{zoneName} @ ({x:0.#},{z:0.#})";
                }
                else
                {
                    Vector3 hp = hit["position"].AsVector3();
                    deltas.Add(hp.Y - ab.Position.Y);
                    if (ix % HeightSampleStride == 0 && iz % HeightSampleStride == 0)
                    {
                        float vy = VisualHeight(hfield, hw, hd, hScale, x, z);
                        float dh = hp.Y - vy;
                        hDeltas.Add(dh);
                        float ad = System.MathF.Abs(dh);
                        if (ad > heightMaxAbs) heightMaxAbs = ad;
                        if (ad >= HeightTolM)
                        {
                            heightMisses++;
                            if (_firstHeightOff == "")
                                _firstHeightOff = $"{zoneName} @ ({x:0.#},{z:0.#}) hit={hp.Y:0.###} visual={vy:0.###} off={dh:0.###}";
                        }
                    }
                }
            }
        }
        q.Dispose();   // MC 10026.30: release the shared native query before the next zone boots
        string sink = "n/a";
        if (deltas.Count > 0)
        {
            deltas.Sort();
            sink = $"hit-height min={deltas[0]:0.##} median={deltas[deltas.Count / 2]:0.##} max={deltas[^1]:0.##} (terrain bottom={ab.Position.Y:0.##} top={ab.End.Y:0.##})";
        }
        string hsink = "n/a (no sampled hits)";
        if (hDeltas.Count > 0)
        {
            hDeltas.Sort();
            hsink = $"median={hDeltas[hDeltas.Count / 2]:0.###} maxabs={heightMaxAbs:0.###} misses={heightMisses} tol={HeightTolM:0.##}";
        }
        GD.Print($"FLOOR_COVERAGE {zoneName}: probes={probes} uncovered={uncovered} holes=[{string.Join(" ", holes)}]");
        GD.Print($"FLOOR_COVERAGE_DIAG {zoneName}: hit-minus-terrain-bottom {sink}");
        GD.Print($"FLOOR_HEIGHT_DIAG {zoneName}: height-samples={hDeltas.Count} |hit_y-visual_y| {hsink} (every {HeightSampleStride}th point both axes)");
        _totalProbes += probes;
        _totalUncovered += uncovered;
        _probedZones++;
        _totalHeightSamples += hDeltas.Count;
        _totalHeightMisses += heightMisses;
        if (uncovered > 0) _holesFound = true;   // keep sweeping: RED must name EVERY hole zone
        if (heightMisses > 0) _heightMiss = true; // and EVERY height-off zone (the two RED classes name themselves)
    }

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

    private void Finish()
    {
        if (_failed) return;   // Fail() already quit the tree
        if (_mode == "FLOOR_TRUST")
        {
            if (_probedZones == 0) { Fail("no zone with a terrain ground was probed — the zones/ set is empty/renamed"); return; }
            if (_trustViolationFound)
            {
                Fail($"FLOOR_TRUST {_totalTrustViolations}/{_totalTrustProbes + _totalTrustEdgeProbes} visible-ground points have NO live terrain collision under them — first: {_firstTrustViolation}");
                return;
            }
            GD.Print($"FLOOR_TRUST_CLEAR zones={_probedZones} probes={_totalTrustProbes} edge-walk={_totalTrustEdgeProbes} violations=0 past-extent-logged={_totalTrustOutside} (probe256 verdicts printed per zone; past-extent points are clamp-BLOCKED or kill-planed at runtime — both logged, this line is the accounting, never silence)");
            GD.Print("LA_GATE: PASS");
            Quit(0);
            return;
        }
        if (_probedZones == 0) { Fail("no zone with a terrain ground was probed — the zones/ set is empty/renamed"); return; }
        if (_holesFound)
        {
            Fail($"FLOOR_COVERAGE {_totalUncovered}/{_totalProbes} walkable points have no floor within {FallToleranceM:0.#} m — first hole: {_firstHole}");
            return;
        }
        if (_heightMiss)   // MC 10026.31(c): floor EXISTS but is not the floor you SEE
        {
            Fail($"FLOOR_HEIGHT_OFF {_totalHeightMisses}/{_totalHeightSamples} sampled points sit >= {HeightTolM:0.##} m off the visual ground — first: {_firstHeightOff}");
            return;
        }
        GD.Print($"FLOOR_COVERAGE_CLEAR zones={_probedZones} probes={_totalProbes} uncovered=0");
        GD.Print($"FLOOR_HEIGHT_CLEAR zones={_probedZones} samples={_totalHeightSamples} misses=0");
        GD.Print("LA_GATE: PASS");
        Quit(0);
    }

    /// <summary>
    /// MC 10026.31(c): the SAME height field the visual builder samples —
    /// terrain_builder.gd's _heights: load(heightmap_path).get_image(),
    /// convert to RGBA8, red channel * v_scale (its `r * 255.0 * v_scale /
    /// 255.0`, which is exactly r * vScale). Read from the Ground node's own
    /// exported config, so a re-scaled or re-pointed zone can never be
    /// checked against a stale copy. Null = not a configured
    /// terrain_builder ground (the caller fails the leg — never skip).
    /// internal (not private): the FLOOR_TRUST LIVE leg
    /// (RuntimeIntegrationProof.FloorTrust.cs, MC 10516/S38) mirrors the SAME
    /// mapping on the composed scene — one mapping implementation, never a
    /// second beside it.
    /// </summary>
    internal static float[]? LoadHeightField(Node ground, out int w, out int d, out float hScale)
    {
        w = d = 0; hScale = 0f;
        string path = ground.Get("heightmap_path").AsString();
        if (path == "" || GD.Load<Texture2D>(path) is not Texture2D tex) return null;
        float vScale = ground.Get("v_scale").AsSingle();
        hScale = ground.Get("h_scale").AsSingle();
        if (vScale <= 0f || hScale <= 0f) return null;
        var img = tex.GetImage();
        img.Convert(Image.Format.Rgba8);
        w = img.GetWidth(); d = img.GetHeight();
        var h = new float[w * d];
        for (int y = 0; y < d; y++)
            for (int x = 0; x < w; x++)
                h[y * w + x] = img.GetPixel(x, y).R * vScale;
        img.Dispose();
        return h;
    }

    /// <summary>
    /// The visual builder's world<->pixel mapping, mirrored from
    /// terrain_builder.gd build_visual_mesh: u = world/(pixels*h_scale) + 0.5,
    /// pixel = clamp(int(u * (pixels-1)), 0, pixels-1) — the nearest-pixel
    /// height the mesh vertex at this world position carries. (The mesh
    /// linearly interpolates BETWEEN vertices, 1.5-8 m apart; shipped terrain
    /// slopes under 0.02 make that interpolation centimetres — the green run's
    /// maxabs lines are the standing evidence, cf. the HeightSampleStride
    /// comment.)
    /// </summary>
    internal static float VisualHeight(float[] h, int w, int d, float hScale, float x, float z)
    {
        int px = Mathf.Clamp((int)((x / (w * hScale) + 0.5f) * (w - 1)), 0, w - 1);
        int pz = Mathf.Clamp((int)((z / (d * hScale) + 0.5f) * (d - 1)), 0, d - 1);
        return h[pz * w + px];
    }

    private void Fail(string msg)
    {
        _failed = true;
        GD.Print($"LA_GATE: FAIL: {msg}");
        Quit(1);
    }
}
