using Godot;
using LastAnimal.Combat;
using LastAnimal.World;

// Last Animal — MC 10216 / 10026.34 S18 zone-four + enemy-four proof (stage 100, mode ZONE4).
//
// Proves the S18 contract on the LIVE playable scene:
//   1. TRAVEL CYCLE IS 4 ZONES ON THE EXISTING SEAM: four real "travel" input
//      presses walk meadow -> canyon -> ruins -> hollow -> meadow (ZoneBossProof
//      PressTravel idiom); at hollow the SpawnSet is re-applied (6 denizens) and
//      the player sits at the zone entry. Zero new seam, zero new signal, zero
//      save touch (the travel hunk is untouched — the 4th zone is a table row).
//   2. ENEMY FOUR: every hollow denizen is Kind==Wraith and its composed visual
//      subtree counts EXACTLY 9 MeshInstance3D (the unique Wraith part count —
//      manual recursion, the BridgeMvpProof idiom, never GetChildren(bool) which
//      is include_internal in GodotSharp 4.7.2).
//   3. GAIT ROW LIVE + INTEGER (F4): while a witness wraith moves, its rig phase
//      advances EXACTLY +1 mod 14 per physics tick and wraps once — a delta-time
//      phase cannot hold an exact +1/tick relation across consecutive ticks (the
//      planted-bad class from S14/DAYNIGHT). Rest ticks resync, never false-RED.
//   4. KEYFRAME ANCHORS: the Hollow table reads BIT-EXACT at the four named
//      frame windows on the SHIPPED Environment + DirectionalLight3D (the S15
//      capture-seam: pause + park), the fog_sky_affect=0.0 pin READS 0.0 at every
//      anchor (S13 carry), and the four zones' noon rows are pairwise distinct.
//
// Boots the LIVE meadow set (DISSOLVE/CHAR_MOTION precedent — the leg travels
// there); no save is touched.
//
// Run:  LA_GATE_MODE=ZONE4 $GODOT --headless --path <proj> \
//         --script res://ci_proofs/RuntimeIntegrationProof.cs
public partial class RuntimeIntegrationProof : SceneTree
{
    private DirectionalLight3D? _z4Light;
    private WorldEnvironment? _z4Env;
    private EnemyActor? _z4Witness;
    private Vector3 _z4SpawnOrigin;       // zone-entry baseline, captured at compose (the director repositions the player HERE on every travel)
    private float _z4DisplacedBy;         // S27: metres walked OFF the entry column before the final (hollow) travel — the column-return assert's non-vacuity precondition
    private int _z4MoveFrames;            // off-column displacement sub-phase counter
    private int _z4TravelToggle;          // PressTravel-style press cycle (ZoneBossProof idiom)
    private int _z4Leg;                   // 0..2 travel legs, 3 = gait window, 4 = wrap leg
    private int _z4GaitCycle;             // the Wraith gait cycle the proof reads FROM the table
    private int _z4GaitPrev = -1;         // -1 = resynced (rest or rig-not-ready)
    private int _z4GaitSamples;
    private int _z4GaitWraps;
    private int _z4LegFrames;
    private bool _z4GaitProven;
    private const int Z4PressFrames = 6;  // press settle window — the shipped PressFrames value
    private const int Z4GaitMinSamples = 20;   // >= cycle 14 + slack: the wrap is inside the window
    private const int Z4LegBudgetFrames = 400; // per sub-leg watchdog (travel 120-class, gait needs the chase window)
    private static readonly string[] Z4Order = { "canyon", "ruins", "hollow" };

    private void Zone4Fail(string why) => Fail("ZONE4: " + why);

    private void RunZone4Stage()
    {
        _z4LegFrames++;
        if (_z4LegFrames > Z4LegBudgetFrames)
        {
            Zone4Fail($"sub-leg {_z4Leg} budget exhausted (gait window never proved / travel never landed)");
            return;
        }

        if (_z4Leg <= 2)
        {
            // ---- travel: meadow -> canyon -> ruins -> hollow on the real seam ----
            // S27 displacement sub-phase (the FINAL leg only, before its press):
            // the witness walks ~1.6 m off the entry column (12 m/s·ticks of the
            // shipped 5 m/s direct feed, PlayerController.cs:34/89 — no inertia,
            // release stops dead), so the hollow-arrival column-return assert
            // below is a REAL test of the reposition instead of a witness that
            // never left the column. An unhooked reposition lands the arrival
            // error at the displacement; a hooked one zeroes it.
            if (_z4Leg == 2 && _z4DisplacedBy == 0f)
            {
                if (_z4MoveFrames == 0) Input.ActionPress("move_right");
                _z4MoveFrames++;
                if (_z4MoveFrames >= 20)
                {
                    Input.ActionRelease("move_right");
                    _z4DisplacedBy = System.Math.Abs(_playerBody!.GlobalPosition.X - _z4SpawnOrigin.X);
                    GD.Print($"LA_GATE: ZONE4_OFFCOLUMN displaced-by={_z4DisplacedBy:0.###} m — the column-return assert below tests a return, not a stand");
                }
                return;
            }
            if (PressZ4Travel() && _director!.CurrentZone == Z4Order[_z4Leg])
            {
                if (_z4Leg < 2)
                {
                    GD.Print($"LA_GATE: ZONE4_TRANSIT_{Z4Order[_z4Leg].ToUpperInvariant()} — travel leg {_z4Leg + 1} landed {_director.CurrentZone}");
                    _z4Leg++; _z4LegFrames = 0; _z4TravelToggle = 0;
                    return;
                }
                // hollow arrived: the 4-zone cycle + the table + the enemy four.
                // S27 EXPECTATION SWEEP, second site (the first: the anchor read
                // in AssertZone4Anchors): "player repositioned at the zone entry"
                // was ONE 3-D DistanceTo < 1.0 m budget read at the arrival frame.
                // The zone mount now runs its terrain build + navmesh bake ON the
                // travel frame; physics catch-up advances tens of ticks before the
                // proof's first READ, and free-fall ALONE ate the vertical half of
                // that budget: VERIFIED over 8 solo runs — d=0.893/0.915 GREEN,
                // d=1.054/1.029 RED, X/Z EXACT (0,0) in ALL eight. The budget sat
                // on a timing artifact (an earlier peak-Y sampler attempt: the
                // write moment is UNOBSERVABLE behind the bake — maxY read 2.4-2.5,
                // never the written 3.0, .tmp/s39/zone4-s*.log). Re-instrumented
                // to what the guarantee actually is, made NON-VACUOUS by the
                // OFFCOLUMN sub-phase above: the witness displaced ~1.6 m, and
                // travel must bring it back to the entry COLUMN — horizontal error
                // < 0.1 m, X/Z-exact class. The old budget could not fail for a
                // witness that never left the column; the new one fails LOUDLY if
                // the reposition is unhooked (error == the displacement).
                var p4 = _playerBody!.GlobalPosition;
                float dXZ4 = new Vector2(p4.X - _z4SpawnOrigin.X, p4.Z - _z4SpawnOrigin.Z).Length();
                Check("travel returned the displaced player to the zone entry COLUMN (off-column by >=0.5 m BEFORE, arrival error <0.1 m AFTER)",
                      _z4DisplacedBy >= 0.5f && dXZ4 < 0.1f,
                      $"offColumnBy={_z4DisplacedBy:0.###} m arrival-err={dXZ4:0.####} m pos=({p4.X:0.###},{p4.Y:0.###},{p4.Z:0.###}) origin=({_z4SpawnOrigin.X:0.###},{_z4SpawnOrigin.Y:0.###},{_z4SpawnOrigin.Z:0.###})");
                if (_failed) return;
                Check("hollow SpawnSet re-applied: 6 denizens through the existing wire",
                      _director.Enemies.Count == 6, $"enemies={_director.Enemies.Count}");
                if (_failed) return;
                GD.Print("LA_GATE: ZONE4_TRAVEL — the 4-zone travel cycle reaches hollow (loop wrap leg follows)");
                bool allWraith = true;
                foreach (var e in _director.Enemies)
                    if (e.Kind != EnemyAI.Type.Wraith) allWraith = false;
                Check("every hollow denizen is enemy four (Kind==Wraith through the SpawnSet)",
                      allWraith, "a spawned Kind is not Wraith");
                if (_failed) return;
                GD.Print("LA_GATE: ZONE4_TABLE — hollow fields 6 Wraiths (4-zone tables live)");

                // ENEMY FOUR composed visual: 9 mesh parts under the Lean node.
                _z4Witness = null;
                foreach (var e in _director.Enemies)
                {
                    if (_z4Witness == null ||
                        e.GlobalPosition.DistanceTo(_playerBody!.GlobalPosition) <
                        _z4Witness.GlobalPosition.DistanceTo(_playerBody!.GlobalPosition))
                        _z4Witness = e;
                }
                int parts = CountMeshesInSubtreeZ4(_z4Witness!.Visual);
                Check("wraith visual subtree is the 9-part spectre (composed, under Lean)",
                      parts == 9, $"parts={parts}");
                if (_failed) return;
                GD.Print("LA_GATE: ZONE4_ENEMY4 — the composed wraith silhouette counts EXACTLY 9 MeshInstance3D (Lean subtree walk)");

                // Open the gait window: the witness rig must be bound and moving;
                // Zone4Tick (physics-tick hook) proves +1 mod cycle per tick.
                _z4GaitCycle = MotionTuning.GaitOf(EnemyAI.Type.Wraith).Cycle;
                _z4Leg = 3; _z4LegFrames = 0;
            }
            return;
        }

        if (_z4Leg == 3)
        {
            // The gait assertion itself runs on the PHYSICS tick (Zone4Tick —
            // the same exact tick domain the driver advances in); this frame
            // only shepherds the watchdog and advances on proof.
            if (_z4GaitProven)
            {
                AssertZone4Anchors();
                if (_failed) return;
                _z4Leg = 4; _z4LegFrames = 0; _z4TravelToggle = 0;
            }
            return;
        }

        // ---- wrap leg: the NEXT entry past hollow returns to meadow (the loop
        // pattern is the shipped modulo wrap — this is its 4-zone case) ----
        if (PressZ4Travel() && _director!.CurrentZone == "meadow")
        {
            GD.Print("LA_GATE: ZONE4_WRAP — hollow -> meadow on the shipped modulo wrap (cycle is 4 zones)");
            GD.Print("LA_GATE: PASS — S18 zone four verified (4-zone travel cycle + wrap on the existing seam; hollow fields 6 wraiths; 9-part composed enemy four; gait +1 mod 14 integer tick; 4 EXACT Hollow anchors + fog_sky_affect=0.0 read at each; noon rows pairwise distinct)");
            _asserted = true;
            _z4Witness = null; _z4Light = null; _z4Env = null;
            ReleaseHeldRefsBeforeQuit();
            Quit(0);
        }
    }

    /// <summary>Physics-tick half of the gait window: the F4 pin. One rig
    /// advance per physics tick — every consecutive sample pair while the
    /// witness MOVES must be exactly +1 mod cycle (a delta-time phase lands RED
    /// here, the S14/DAYNIGHT planted-bad class). Rest or rig-not-ready ticks
    /// resync the chain — they cannot false-RED the exact pair.</summary>
    private void Zone4Tick()
    {
        if (_z4Leg != 3 || _z4GaitProven) return;
        if (_z4Witness == null || !GodotObject.IsInstanceValid(_z4Witness)) { Zone4Fail("witness freed inside the gait window"); return; }
        int phase = _director!.WalkPhaseOf(_z4Witness.Visual);
        if (phase < 0) { _z4GaitPrev = -1; return; }          // rig not bound yet
        var v = _z4Witness.SimVelocity;                       // the SAME read-only feed the rig reads
        if (new Vector2(v.X, v.Z).Length() <= MotionTuning.WalkMinSpeed) { _z4GaitPrev = -1; return; }  // rest pin is the driver's own (S14)
        if (_z4GaitPrev >= 0 && phase != (_z4GaitPrev + 1) % _z4GaitCycle)
        {
            Zone4Fail($"gait phase not integer +1/tick: prev={_z4GaitPrev} now={phase} cycle={_z4GaitCycle} " +
                      "(phase must be an INTEGER counter advanced one per physics tick — a delta-time feed diverges here)");
            return;
        }
        if (_z4GaitPrev == _z4GaitCycle - 1 && phase == 0) _z4GaitWraps++;
        _z4GaitPrev = phase;
        _z4GaitSamples++;
        if (_z4GaitSamples >= Z4GaitMinSamples && _z4GaitWraps >= 1)
        {
            GD.Print($"LA_GATE: ZONE4_GAIT_INT — wraith phase advanced EXACTLY +1 mod {_z4GaitCycle} per physics tick over {_z4GaitSamples} moving samples incl. one cycle wrap (per-type gait row live; integer counter, F4)");
            _z4GaitProven = true;
        }
    }

    /// <summary>The four EXACT anchor reads (capture-seam: pause + park writes
    /// NOW, MC 10199) + the fog pin read at every anchor + the pairwise-distinct
    /// noon rows across all FOUR zone tables.</summary>
    private void AssertZone4Anchors()
    {
        // S27 expectation sweep (MC 10519, the S14 same-commit precedent): with
        // zone-SCENE mounting, the driven Environment + light live UNDER THE
        // MOUNTED ZONE ROOT — this leg stands in hollow at this point, and
        // "World/Meadow" is exactly the stale-binding path this card closes
        // (reading it here would read FREED nodes). The assert itself is
        // UNCHANGED: the Hollow row must read bit-exact on the live driven
        // nodes — those nodes are now the mounted scene's own (the driver
        // re-binds at the swap, WorldDirector.DayNight.cs).
        var mounted = _director!.MountedZoneRoot;
        _z4Light = mounted?.GetNodeOrNull<DirectionalLight3D>("Environment/DirectionalLight3D");
        _z4Env = mounted?.GetNodeOrNull<WorldEnvironment>("Environment/WorldEnvironment");
        if (_z4Light == null || _z4Env == null)
        { Zone4Fail($"no Environment/DirectionalLight3D under the MOUNTED zone root ({mounted?.Name ?? "<none>"}) — the day-night driver has nothing to drive"); return; }
        _director!.SetDayNightPaused(true);
        AssertZone4Anchor("DAWN", DayNightClock.DawnFrame);
        if (_failed) return;
        AssertZone4Anchor("NOON", DayNightClock.NoonFrame);
        if (_failed) return;
        AssertZone4Anchor("DUSK", DayNightClock.DuskFrame);
        if (_failed) return;
        AssertZone4Anchor("NIGHT", DayNightClock.NightFrame);
        if (_failed) return;

        var m = DayNightClock.Evaluate("meadow", DayNightClock.NoonFrame);
        var c = DayNightClock.Evaluate("canyon", DayNightClock.NoonFrame);
        var r = DayNightClock.Evaluate("ruins", DayNightClock.NoonFrame);
        var h = DayNightClock.Evaluate("hollow", DayNightClock.NoonFrame);
        if (h.AmbientColor == m.AmbientColor || h.AmbientColor == c.AmbientColor || h.AmbientColor == r.AmbientColor
            || m.AmbientColor == c.AmbientColor || m.AmbientColor == r.AmbientColor || c.AmbientColor == r.AmbientColor)
        { Zone4Fail("zone noon tables collapsed — the fourth keyframe table must be a distinct look"); return; }
        GD.Print("LA_GATE: ZONE4_KEY4_DISTINCT — meadow/canyon/ruins/hollow noon rows pairwise distinct (4 per-zone const tables live)");
    }

    private void AssertZone4Anchor(string name, int frame)
    {
        _director!.SetDayNightFrame(frame);   // parks + WRITES NOW (capture-seam semantics)
        var key = DayNightClock.Evaluate("hollow", frame);
        var light = _z4Light!;
        var env = _z4Env!.Environment!;
        float tol = DayNightTuning.RotationTolerance;
        bool rotOk = System.Math.Abs(light.Rotation.X - Mathf.DegToRad(key.SunPitchDegrees)) < tol
                  && System.Math.Abs(light.Rotation.Y - Mathf.DegToRad(key.SunYawDegrees)) < tol;
        bool ok = rotOk
            && light.LightEnergy == key.LightEnergy && light.LightColor == key.LightColor
            && env.AmbientLightColor == key.AmbientColor && env.AmbientLightEnergy == key.AmbientEnergy;
        if (env.Sky?.SkyMaterial is ProceduralSkyMaterial skyMat)
            ok &= skyMat.SkyTopColor == key.SkyTop && skyMat.SkyHorizonColor == key.SkyHorizon;
        if (!ok)
        {
            Zone4Fail($"{name}@{frame} live values diverge from the Hollow table: " +
                      $"energy={light.LightEnergy:0.###} want={key.LightEnergy:0.###} " +
                      $"amb={env.AmbientLightColor} want={key.AmbientColor}");
            return;
        }
        GD.Print($"LA_GATE: ZONE4_ANCHOR_{name} frame={frame} hollow row EXACT on the shipped nodes (le={light.LightEnergy:0.###} amb={env.AmbientLightEnergy:0.###})");
        if (env.FogSkyAffect != 0f)
        { Zone4Fail($"fog_sky_affect RAISED to {env.FogSkyAffect} at {name} (S13 pins 0.0 — sky wash class)"); return; }
        GD.Print($"LA_GATE: ZONE4_FOGPIN_0 at {name} (fog_sky_affect reads exactly 0.0 on the live Environment)");
    }

    private bool PressZ4Travel()
    {
        // ZoneBossProof PressTravel idiom VERBATIM in behaviour: press once, the
        // edge fires TravelToNextZone within a frame, re-arm on release so the
        // next leg's press is a real released->pressed transition (a held cycle
        // is a silent no-op — MC 1348 A5 follow-up).
        _z4TravelToggle++;
        if (_z4TravelToggle == 1) Input.ActionPress("travel");
        if (_z4TravelToggle >= Z4PressFrames)
        {
            Input.ActionRelease("travel");
            _z4TravelToggle = 0;
            return true;
        }
        return false;
    }

    /// <summary>Manual subtree walk (BridgeMvpProof / VisualJuice.FirstComposedTint
    /// idiom): in GodotSharp 4.7.2 GetChildren(bool) is include_internal, NOT
    /// recursion — the Lean wrapper sits between the root and the parts.</summary>
    private static int CountMeshesInSubtreeZ4(Node? node)
    {
        if (node == null) return 0;
        int n = node is MeshInstance3D ? 1 : 0;
        foreach (var child in node.GetChildren())
            n += CountMeshesInSubtreeZ4(child);
        return n;
    }
}
