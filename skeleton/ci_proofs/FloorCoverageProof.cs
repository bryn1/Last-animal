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
//
// Run:  $GODOT --headless --path <proj> --script res://ci_proofs/FloorCoverageProof.cs
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

    public override void _Initialize()
    {
        GD.Print("LA_GATE: start");
        _mode = System.Environment.GetEnvironmentVariable("LA_GATE_MODE") ?? "FLOOR_COVERAGE";
        GD.Print($"LA_GATE: mode={_mode}");
        Engine.MaxFps = 60;
        switch (_mode)
        {
            case "FLOOR_COVERAGE": break;
            default:
                Fail($"unknown mode {_mode} — FloorCoverageProof ships ONLY FLOOR_COVERAGE (the roster fail-safe idiom: a typo must never ride a default path)");
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

        float top = ab.End.Y + 1.0f;
        float bottom = ab.End.Y - (ab.Size.Y + 1.0f + FallToleranceM);
        int uncovered = 0, probes = 0;
        var holes = new List<string>();
        var deltas = new List<float>();
        for (float x = ab.Position.X; x <= ab.End.X + 0.001f; x += StepM)
        {
            for (float z = ab.Position.Z; z <= ab.End.Z + 0.001f; z += StepM)
            {
                probes++;
                var q = PhysicsRayQueryParameters3D.Create(new Vector3(x, top, z), new Vector3(x, bottom, z));
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
                }
            }
        }
        string sink = "n/a";
        if (deltas.Count > 0)
        {
            deltas.Sort();
            sink = $"hit-height min={deltas[0]:0.##} median={deltas[deltas.Count / 2]:0.##} max={deltas[^1]:0.##} (terrain bottom={ab.Position.Y:0.##} top={ab.End.Y:0.##})";
        }
        GD.Print($"FLOOR_COVERAGE {zoneName}: probes={probes} uncovered={uncovered} holes=[{string.Join(" ", holes)}]");
        GD.Print($"FLOOR_COVERAGE_DIAG {zoneName}: hit-minus-terrain-bottom {sink}");
        _totalProbes += probes;
        _totalUncovered += uncovered;
        _probedZones++;
        if (uncovered > 0) _holesFound = true;   // keep sweeping: RED must name EVERY hole zone
    }

    private void Finish()
    {
        if (_failed) return;   // Fail() already quit the tree
        if (_probedZones == 0) { Fail("no zone with a terrain ground was probed — the zones/ set is empty/renamed"); return; }
        if (_holesFound)
        {
            Fail($"FLOOR_COVERAGE {_totalUncovered}/{_totalProbes} walkable points have no floor within {FallToleranceM:0.#} m — first hole: {_firstHole}");
            return;
        }
        GD.Print($"FLOOR_COVERAGE_CLEAR zones={_probedZones} probes={_totalProbes} uncovered=0");
        GD.Print("LA_GATE: PASS");
        Quit(0);
    }

    private void Fail(string msg)
    {
        _failed = true;
        GD.Print($"LA_GATE: FAIL: {msg}");
        Quit(1);
    }
}
