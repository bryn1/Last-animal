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
            if (PressZ4Travel() && _director!.CurrentZone == Z4Order[_z4Leg])
            {
                if (_z4Leg < 2)
                {
                    GD.Print($"LA_GATE: ZONE4_TRANSIT_{Z4Order[_z4Leg].ToUpperInvariant()} — travel leg {_z4Leg + 1} landed {_director.CurrentZone}");
                    _z4Leg++; _z4LegFrames = 0; _z4TravelToggle = 0;
                    return;
                }
                // hollow arrived: the 4-zone cycle + the table + the enemy four
                Check("player repositioned at the zone entry on travel",
                      _playerBody!.GlobalPosition.DistanceTo(_z4SpawnOrigin) < 1.0f,
                      "pos rides the (ii) numeric stream");
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
        var main = _main!;
        _z4Light = main.GetNodeOrNull<DirectionalLight3D>("World/Meadow/Environment/DirectionalLight3D");
        _z4Env = main.GetNodeOrNull<WorldEnvironment>("World/Meadow/Environment/WorldEnvironment");
        if (_z4Light == null || _z4Env == null)
        { Zone4Fail("shipped Environment/DirectionalLight3D not found under World/Meadow"); return; }
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
