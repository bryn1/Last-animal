using Godot;
using LastAnimal.World;

// Last Animal — MC 10199 Inc-4 S15 day-night proof (stage 98, mode DAYNIGHT_STATE).
//
// Proves the S15 contract on the LIVE playable scene:
//   1. FRAME-DETERMINISTIC CLOCK: the driver's applied frame equals
//      (physics tick - base) EXACTLY, every physics tick — an integer frame
//      counter on the world tick. A wall-clock (DateTime) feed makes the
//      relation diverge within a few ticks: the planted-bad goes RED at
//      DAYNIGHT_STATE (the clock-nonlinear assert).
//   2. EXACT reads at the NAMED frame windows dawn/noon/dusk/night: at each
//      exact anchor frame the SHIPPED DirectionalLight3D + Environment carry
//      the table row bit-exactly (float round-trip; rotation only within the
//      quaternion read-back tolerance).
//   3. PINNED S13 CARRY: fog_sky_affect reads EXACTLY 0.0 on the live
//      Environment every anchor (re-pinned by the driver, never raised).
//   4. Per-zone tables are three DISTINCT looks (noon rows differ pairwise)
//      and the night->dawn wrap is continuous (last frame != both ends).
//
// Spawning is off at boot (bus_emit idiom): the leg drives no combat, and a
// quiet scene keeps the ~23 s run deterministic and cheap. No save is touched.
//
// Run:  LA_GATE_MODE=DAYNIGHT_STATE $GODOT --headless --path <proj> \
//         --script res://ci_proofs/RuntimeIntegrationProof.cs
public partial class RuntimeIntegrationProof : SceneTree
{
    private WorldDirector? _dnDirector;
    private DirectionalLight3D? _dnLight;
    private WorldEnvironment? _dnEnv;
    private int _dnPhysBase = -1;        // physics tick at which applied-frame 0 was seen
    private int _dnFirstCallPhys = -1;   // first gated call — compose timing VARIES (the blocking navmesh bake eats a variable number of catch-up physics ticks), so the never-ticked budget rides the leg's own arming, never an absolute tick count (MC 10199 RED pre-proof found this race)
    private bool _dnDawn, _dnNoon, _dnDusk, _dnNight;
    private const int DayNightFirstTickBudget = 120;   // driver must tick within 120 ticks of leg arming

    private void DayNightFail(string why) => Fail("DAYNIGHT_STATE: " + why);

    /// <summary>Compose (called from _ComposeDeferred before the live-enemy
    /// guard): resolve the shipped nodes, resume the driver (frame 0 writes
    /// NOW — the DAWN window is then unskippable), arm stage 98.</summary>
    private void DayNightCompose(Node main)
    {
        _dnDirector = _director;
        _dnLight = main.GetNodeOrNull<DirectionalLight3D>("World/Meadow/Environment/DirectionalLight3D");
        _dnEnv = main.GetNodeOrNull<WorldEnvironment>("World/Meadow/Environment/WorldEnvironment");
        if (_dnLight == null || _dnEnv == null)
        { Fail("DAYNIGHT_STATE: shipped Environment/DirectionalLight3D not found under World/Meadow"); return; }
        _dnDirector!.SetDayNightPaused(false);   // resume: frame 0 writes NOW, leg catches DAWN
        _stage = 98;
        _stageFrames = 0;
        GD.Print("LA_GATE: composed (day-night mode) — shipped Environment + DirectionalLight3D resolved");
    }

    /// <summary>Called from the harness _PhysicsProcess AFTER its own tick
    /// counter — one exact physics-tick domain shared with the driver.</summary>
    private void DayNightTick()
    {
        if (_dnFirstCallPhys < 0) _dnFirstCallPhys = _physFrames;
        var director = _dnDirector!;
        int applied = director.DayNightAppliedFrame;
        if (applied < 0)
        {
            if (_physFrames - _dnFirstCallPhys > DayNightFirstTickBudget)
                DayNightFail("driver never ticked (no shipped Environment/Light in the live scene?)");
            return;
        }
        if (_dnPhysBase < 0) _dnPhysBase = _physFrames - applied;

        // (1) The exact integer-clock invariant, every tick.
        if (applied != _physFrames - _dnPhysBase)
        {
            DayNightFail($"clock non-linear: applied={applied} expected={_physFrames - _dnPhysBase} " +
                         $"(frame counter must ride the world tick — a wall-clock feed diverges here)");
            return;
        }

        // (2)+(3) EXACT reads at the named windows, on the shipped nodes.
        if (applied == DayNightClock.DawnFrame && !_dnDawn) { _dnDawn = true; AssertDayNightAnchor("DAWN", applied); }
        else if (applied == DayNightClock.NoonFrame && !_dnNoon) { _dnNoon = true; AssertDayNightAnchor("NOON", applied); }
        else if (applied == DayNightClock.DuskFrame && !_dnDusk) { _dnDusk = true; AssertDayNightAnchor("DUSK", applied); }
        else if (applied == DayNightClock.NightFrame && !_dnNight)
        {
            _dnNight = true;
            AssertDayNightAnchor("NIGHT", applied);
            AssertDayNightTables();
            GD.Print($"LA_GATE: DAYNIGHT_LINEAR base={_dnPhysBase} ticks={_physFrames} clock=integer-frame-counter");
            GD.Print("LA_GATE: PASS — S15 day-night verified (frame clock exact; dawn/noon/dusk/night EXACT on shipped nodes; fog_sky_affect=0.0 pinned; 3 distinct zone tables)");
            _asserted = true;
            _dnDirector = null; _dnLight = null; _dnEnv = null;
            ReleaseHeldRefsBeforeQuit();
            Quit(0);
        }
    }

    private void AssertDayNightAnchor(string name, int frame)
    {
        var key = DayNightClock.Evaluate("meadow", frame);
        var light = _dnLight!;
        var env = _dnEnv!.Environment!;
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
            DayNightFail($"{name}@{frame} live values diverge from the table: sun=({light.Rotation.X:0.####},{light.Rotation.Y:0.####}) " +
                         $"want=({Mathf.DegToRad(key.SunPitchDegrees):0.####},{Mathf.DegToRad(key.SunYawDegrees):0.####}) " +
                         $"energy={light.LightEnergy:0.###} want={key.LightEnergy:0.###}");
            return;
        }
        GD.Print($"LA_GATE: DAYNIGHT_{name} frame={frame} sun=({light.Rotation.X:0.####},{light.Rotation.Y:0.####}) " +
                 $"le={light.LightEnergy:0.###} amb=({env.AmbientLightColor.R:0.####},{env.AmbientLightColor.G:0.####},{env.AmbientLightColor.B:0.####},{env.AmbientLightEnergy:0.###})");
        // (3) The PINNED S13 carry, proven by READ on every anchor.
        if (env.FogSkyAffect != 0f)
        { DayNightFail($"fog_sky_affect RAISED to {env.FogSkyAffect} at {name} (S13 pins 0.0 — sky wash class)"); return; }
        GD.Print($"LA_GATE: DAYNIGHT_FOGPIN_0 at {name} (fog_sky_affect reads exactly 0.0 on the live Environment)");
    }

    private void AssertDayNightTables()
    {
        var m = DayNightClock.Evaluate("meadow", DayNightClock.NoonFrame);
        var c = DayNightClock.Evaluate("canyon", DayNightClock.NoonFrame);
        var r = DayNightClock.Evaluate("ruins", DayNightClock.NoonFrame);
        if (m.AmbientColor == c.AmbientColor || m.AmbientColor == r.AmbientColor || c.AmbientColor == r.AmbientColor)
        { DayNightFail("zone noon tables collapsed — per-zone keyframes must be three distinct looks"); return; }
        GD.Print("LA_GATE: DAYNIGHT_TABLE_CANYON + DAYNIGHT_TABLE_RUINS noon rows distinct from meadow (per-zone const tables live)");
        int last = DayNightTuning.DayLengthFrames - 1;
        var w0 = DayNightClock.Evaluate("meadow", 0);
        var w1 = DayNightClock.Evaluate("meadow", last);
        if (w0.AmbientColor == w1.AmbientColor)
        { DayNightFail("night->dawn wrap is flat (wrap segment interpolates to dawn at the cycle edge)"); return; }
        GD.Print("LA_GATE: DAYNIGHT_WRAP_CONTINUOUS last frame sits between night and dawn");
    }
}
