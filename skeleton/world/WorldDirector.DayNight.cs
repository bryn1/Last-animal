using Godot;

// Last Animal — MC 10199 / 10026.26 Inc-4 S15: the day-night driver
// (world-tick integer frame clock + per-zone const keyframe tables).
//
// Contract (plan 10147-inc4 §S15, owner-ratified):
//   RUNTIME-ONLY. The clock is an INTEGER FRAME COUNTER on the world tick
//   (the deterministic 60 Hz physics tick — the same F4 idiom as the juice
//   decay): never wall-clock. A DateTime/Time.GetTicksMsec feed is the
//   planted-bad the DAYNIGHT_STATE leg proves RED.
//   PRESENTATION-ONLY. The driver WRITES the shipped zone Environment +
//   DirectionalLight3D nodes (modulating S13's graded values — no .tscn was
//   edited, nothing new exists in the scenes); gameplay reads NOTHING from
//   it and NO bus signal is added (the 15-signal census holds).
//   ZERO SAVE DELTA (RULING-7, owner "Rec on all": persistence NO). No field
//   of this file rides GameState — the frame counter resets at every boot.
//
// PINNED S13 CARRY (binding): env fog WITH a sky background WASHES THE SKY on
// GL Compatibility (fog_sky_affect defaults 1.0). S13 shipped 0.0 in all three
// zone tscns; the driver never raises it — it re-pins 0.0 on every applied
// frame (cheap hard guarantee; the DAYNIGHT leg proves the pin by READ).
namespace LastAnimal.World;

/// <summary>MC 10199 S15: the ONE tuning block for the day-night cycle
/// (plan §S15 "day length const in one tuning block"). Consts only — no
/// wall-clock anywhere. A full cycle at the fixed 60 Hz world tick:</summary>
public static class DayNightTuning
{
    /// <summary>Frames per full day (1800 physics ticks @60 Hz = 30 s).
    /// The anchors below ride this single const — retune here, nowhere else.</summary>
    public const int DayLengthFrames = 1800;

    /// <summary>Rotation read-back tolerance: Node3D stores rotation as a
    /// quaternion and extracts Euler on read (MC 10199) — last-ulp drift, not
    /// clock drift. Everything else is bit-exact float round-trip.</summary>
    public const float RotationTolerance = 1e-4f;
}

/// <summary>One keyframe row: everything the driver writes at a cycle anchor
/// (immutable const data; MC 10199).</summary>
public readonly struct DayNightKey
{
    public readonly float SunPitchDegrees;   // DirectionalLight3D rotation.X
    public readonly float SunYawDegrees;     //                    rotation.Y
    public readonly float LightEnergy;
    public readonly Color LightColor;
    public readonly Color SkyTop;
    public readonly Color SkyHorizon;
    public readonly Color AmbientColor;
    public readonly float AmbientEnergy;

    public DayNightKey(float sunPitchDegrees, float sunYawDegrees,
        float lightEnergy, Color lightColor, Color skyTop, Color skyHorizon,
        Color ambientColor, float ambientEnergy)
    {
        SunPitchDegrees = sunPitchDegrees;
        SunYawDegrees = sunYawDegrees;
        LightEnergy = lightEnergy;
        LightColor = lightColor;
        SkyTop = skyTop;
        SkyHorizon = skyHorizon;
        AmbientColor = ambientColor;
        AmbientEnergy = ambientEnergy;
    }

    /// <summary>The blended row at an arbitrary frame (const interp — MC 10199).
    /// At t == 0 this returns THIS row bit-exactly: anchors read EXACT.</summary>
    public DayNightKey Lerp(DayNightKey other, float t) => new(
        SunPitchDegrees + (other.SunPitchDegrees - SunPitchDegrees) * t,
        SunYawDegrees + (other.SunYawDegrees - SunYawDegrees) * t,
        LightEnergy + (other.LightEnergy - LightEnergy) * t,
        LightColor.Lerp(other.LightColor, t),
        SkyTop.Lerp(other.SkyTop, t),
        SkyHorizon.Lerp(other.SkyHorizon, t),
        AmbientColor.Lerp(other.AmbientColor, t),
        AmbientEnergy + (other.AmbientEnergy - AmbientEnergy) * t);
}

/// <summary>MC 10199 S15: the pure clock — per-zone const keyframe tables
/// (dawn/noon/dusk/night, in cycle order) + the deterministic evaluator the
/// driver writes from and the DAYNIGHT_STATE leg asserts against. Noon keys
/// sit ON the shipped S13 looks (zone tscn values) so the graded identity is
/// modulated, never replaced. Fog is NOT a keyframe channel at all — the S13
/// fog grade survives untouched; only the fog_sky_affect=0.0 pin is re-written.</summary>
public static class DayNightClock
{
    // Named frame windows (plan §S15): exact anchors of the cycle.
    public const int DawnFrame = 0;
    public const int NoonFrame = DayNightTuning.DayLengthFrames / 4;        // 450
    public const int DuskFrame = DayNightTuning.DayLengthFrames / 2;        // 900
    public const int NightFrame = 3 * DayNightTuning.DayLengthFrames / 4;   // 1350

    // Meadow — warm identity (noon == shipped meadow.tscn values).
    private static readonly DayNightKey[] Meadow =
    {
        new(-12f,  25f, 1.00f, new Color(1.00f, 0.78f, 0.60f), new Color(0.25f, 0.35f, 0.60f), new Color(0.95f, 0.65f, 0.45f), new Color(0.90f, 0.70f, 0.60f), 0.45f),
        new(-65f,   0f, 1.50f, new Color(1.00f, 0.96f, 0.88f), new Color(0.30f, 0.52f, 0.90f), new Color(0.78f, 0.86f, 0.95f), new Color(1.00f, 0.96f, 0.88f), 0.55f),
        new(-10f, -35f, 1.10f, new Color(1.00f, 0.62f, 0.35f), new Color(0.20f, 0.28f, 0.55f), new Color(0.98f, 0.50f, 0.30f), new Color(0.85f, 0.60f, 0.50f), 0.40f),
        new(-50f, 165f, 0.35f, new Color(0.55f, 0.65f, 0.95f), new Color(0.03f, 0.05f, 0.12f), new Color(0.10f, 0.12f, 0.25f), new Color(0.30f, 0.38f, 0.60f), 0.22f),
    };

    // Canyon — warm haze identity (noon == shipped canyon.tscn values).
    private static readonly DayNightKey[] Canyon =
    {
        new(-14f,  20f, 1.10f, new Color(1.00f, 0.80f, 0.62f), new Color(0.30f, 0.34f, 0.55f), new Color(0.98f, 0.60f, 0.38f), new Color(0.95f, 0.70f, 0.52f), 0.40f),
        new(-68f,   0f, 1.60f, new Color(1.00f, 0.90f, 0.72f), new Color(0.35f, 0.40f, 0.62f), new Color(1.00f, 0.62f, 0.35f), new Color(1.00f, 0.72f, 0.50f), 0.45f),
        new(-12f, -30f, 1.20f, new Color(1.00f, 0.55f, 0.30f), new Color(0.22f, 0.22f, 0.45f), new Color(0.95f, 0.40f, 0.22f), new Color(0.85f, 0.55f, 0.40f), 0.38f),
        new(-48f, 170f, 0.35f, new Color(0.60f, 0.62f, 0.90f), new Color(0.05f, 0.05f, 0.12f), new Color(0.18f, 0.12f, 0.20f), new Color(0.35f, 0.32f, 0.48f), 0.20f),
    };

    // Ruins — dark-BUT-GRADED identity (noon == shipped ruins.tscn values;
    // S13: darkness is identity — the cycle never brightens it).
    private static readonly DayNightKey[] Ruins =
    {
        new(-16f,  15f, 0.80f, new Color(0.80f, 0.82f, 0.95f), new Color(0.10f, 0.13f, 0.28f), new Color(0.40f, 0.46f, 0.58f), new Color(0.55f, 0.62f, 0.78f), 0.42f),
        new(-60f,   0f, 1.10f, new Color(0.95f, 0.95f, 1.00f), new Color(0.08f, 0.11f, 0.22f), new Color(0.45f, 0.50f, 0.60f), new Color(0.60f, 0.68f, 0.85f), 0.50f),
        new(-14f, -25f, 0.80f, new Color(0.90f, 0.70f, 0.55f), new Color(0.08f, 0.09f, 0.20f), new Color(0.50f, 0.38f, 0.32f), new Color(0.52f, 0.50f, 0.62f), 0.40f),
        new(-45f, 160f, 0.28f, new Color(0.45f, 0.55f, 0.85f), new Color(0.02f, 0.03f, 0.08f), new Color(0.06f, 0.08f, 0.16f), new Color(0.22f, 0.28f, 0.45f), 0.25f),
    };

    // Hollow — cold spectral teal-silver identity (MC 10216 S18 zone four).
    // No tscn grade exists to anchor its noon to (the zone is table-only, the
    // S15 values-only precedent), so the NOON row IS hollow's base look; the
    // cycle modulates that, never the other zones' looks. Ambient is pairwise
    // distinct from meadow/canyon/ruins at noon (the ZONE4 leg proves it).
    private static readonly DayNightKey[] Hollow =
    {
        new(-15f,  18f, 0.90f, new Color(0.75f, 0.90f, 0.85f), new Color(0.12f, 0.22f, 0.28f), new Color(0.55f, 0.78f, 0.72f), new Color(0.55f, 0.72f, 0.68f), 0.40f),
        new(-62f,   0f, 1.25f, new Color(0.92f, 0.98f, 0.95f), new Color(0.20f, 0.38f, 0.44f), new Color(0.62f, 0.80f, 0.78f), new Color(0.70f, 0.85f, 0.80f), 0.48f),
        new(-12f, -28f, 0.85f, new Color(0.62f, 0.78f, 0.72f), new Color(0.10f, 0.16f, 0.24f), new Color(0.42f, 0.60f, 0.55f), new Color(0.48f, 0.62f, 0.58f), 0.36f),
        new(-46f, 168f, 0.30f, new Color(0.50f, 0.70f, 0.78f), new Color(0.02f, 0.06f, 0.10f), new Color(0.08f, 0.16f, 0.20f), new Color(0.24f, 0.38f, 0.42f), 0.24f),
    };

    /// <summary>Zone ids are EcosystemSpawner.ZoneIds; an unknown zone falls
    /// back to meadow exactly like the spawner's table lookup does.</summary>
    private static DayNightKey[] TableFor(string zoneId) => zoneId switch
    {
        "canyon" => Canyon,
        "ruins" => Ruins,
        "hollow" => Hollow,   // MC 10216 S18 (zone four; fog pin carries via ApplyTo — one writer)
        _ => Meadow,
    };

    /// <summary>The pure, frame-deterministic evaluator (F4): piecewise-linear
    /// between anchors, wrapping. At an exact anchor frame t==0 → the key row
    /// BIT-EXACTLY (the DAYNIGHT leg's EXACT reads rest on this).</summary>
    public static DayNightKey Evaluate(string zoneId, int frame)
    {
        int len = DayNightTuning.DayLengthFrames;
        int f = ((frame % len) + len) % len;
        DayNightKey[] table = TableFor(zoneId);
        int[] anchors = { DawnFrame, NoonFrame, DuskFrame, NightFrame };
        int seg = f < NoonFrame ? 0 : f < DuskFrame ? 1 : f < NightFrame ? 2 : 3;
        int a = anchors[seg];
        int b = seg == 3 ? len + DawnFrame : anchors[seg + 1];
        DayNightKey from = table[seg];
        DayNightKey to = table[(seg + 1) % 4];
        return from.Lerp(to, (float)(f - a) / (b - a));
    }

    /// <summary>Presentation seam (capture scripts park the clock at a named
    /// anchor): wraps any int into the cycle. Gameplay never calls this.</summary>
    public static int WrapFrame(int frame)
    {
        int len = DayNightTuning.DayLengthFrames;
        return ((frame % len) + len) % len;
    }
}

// --- the driver half of the composition root (MC 10199 S15) -----------------
public partial class WorldDirector
{
    // INTEGER FRAME CLOCK on the world tick — the ONLY state (runtime-only,
    // RULING-7: nothing of this reaches a save).
    private int _dayNightNextFrame;
    private int _dayNightAppliedFrame = -1;   // -1 until the first write
    private bool _dayNightPaused;
    private bool _dayNightNodesMissing;
    private DirectionalLight3D? _dayNightLight;
    private WorldEnvironment? _dayNightWorldEnv;

    /// <summary>Last frame the driver WROTE (-1 = never). The DAYNIGHT_STATE
    /// leg's exact linear-clock assertion reads this (proof read-surface,
    /// same idiom as BossPhase/Enemies above).</summary>
    public int DayNightAppliedFrame => _dayNightAppliedFrame;

    /// <summary>Gate seam (Juice/Calm idiom): pause the cycle. Presentation
    /// seam for the capture scripts; carries no gameplay behaviour.</summary>
    public void SetDayNightPaused(bool paused) => _dayNightPaused = paused;

    /// <summary>Gate seam: park the clock NOW, writing the anchor row onto the
    /// shipped nodes IMMEDIATELY (then pausing freezes that look; unpaused the
    /// next tick continues from it). Capture-seam semantics matter: headful
    /// idle callbacks sample slower than the 60 Hz physics ticks, so a value
    /// applied only on the next tick can land BETWEEN two idle samples and
    /// never be observed (measured MC 10199). Carries no gameplay behaviour.</summary>
    public void SetDayNightFrame(int frame)
    {
        _dayNightNextFrame = DayNightClock.WrapFrame(frame);
        if (_dayNightLight != null && _dayNightWorldEnv?.Environment != null)
        {
            DayNightClock.Evaluate(_zone, _dayNightNextFrame).ApplyTo(
                _dayNightLight, _dayNightWorldEnv);
            _dayNightAppliedFrame = _dayNightNextFrame;
        }
    }

    // The world tick: fixed 60 Hz physics (F4 — integer ticks, never delta
    // time, never wall-clock). Runs alongside the _Process live loop; the
    // driver READS no gameplay state and nothing gameplay-side reads it.
    // This is the ONE WorldDirector._PhysicsProcess override in the partial
    // family (W2 merge integration: MC 10198 S14's motion driver was folded
    // in as MotionPhysicsTick, invoked FIRST and unconditionally so both
    // drivers keep their per-tick contract regardless of these guards).
    public override void _PhysicsProcess(double delta)
    {
        MotionPhysicsTick(delta);         // S14: presentation motion, every tick
        if (_dayNightPaused || _dayNightNodesMissing) return;
        if (_dayNightLight == null || _dayNightWorldEnv == null)
            ResolveDayNightNodes();
        if (_dayNightLight == null || _dayNightWorldEnv == null) return;

        DayNightClock.Evaluate(_zone, _dayNightNextFrame).ApplyTo(
            _dayNightLight, _dayNightWorldEnv);
        _dayNightAppliedFrame = _dayNightNextFrame;
        _dayNightNextFrame = (_dayNightNextFrame + 1) % DayNightTuning.DayLengthFrames;
    }

    // Resolve the SHIPPED zone nodes (S15: nothing new in scenes — the tscn
    // path first, a name search fallback so any zone-installed layout works).
    private void ResolveDayNightNodes()
    {
        _dayNightLight = GetNodeOrNull<DirectionalLight3D>(
            "World/Meadow/Environment/DirectionalLight3D")
            ?? FindChildOfType<DirectionalLight3D>();
        _dayNightWorldEnv = GetNodeOrNull<WorldEnvironment>(
            "World/Meadow/Environment/WorldEnvironment")
            ?? FindChildOfType<WorldEnvironment>();
        if (_dayNightLight == null || _dayNightWorldEnv?.Environment == null)
        {
            // A playable scene must ship its shipped Environment + light;
            // fail loudly (never silently: S15 without nodes would freeze the
            // old look and pass every other leg) then stand the driver down.
            GD.PushError("LA_GATE: S15 day-night driver found no shipped Environment/DirectionalLight3D in the scene");
            _dayNightNodesMissing = true;
        }
    }

    private T? FindChildOfType<T>() where T : Node =>
        FindChild(typeof(T).Name, recursive: true, owned: false) as T;
}

// ApplyTo lives as an extension so the pure table half stays Godot-node-free
// apart from Color (same discipline as the src/ pure layers).
internal static class DayNightKeyApply
{
    /// <summary>Write the row onto the shipped nodes. Presentation writes
    /// ONLY: sun angle + energy + colour on the DirectionalLight3D, ambient
    /// colour/energy + sky top/horizon on the Environment — fog channels are
    /// NOT touched (S13 grade survives); the PINNED fog_sky_affect=0.0 carry
    /// is re-pinned on every frame (GL Compatibility sky-wash guard, never
    /// raised).</summary>
    public static void ApplyTo(this in DayNightKey key,
        DirectionalLight3D light, WorldEnvironment worldEnv)
    {
        light.RotationDegrees = new Vector3(key.SunPitchDegrees, key.SunYawDegrees, 0f);
        light.LightEnergy = key.LightEnergy;
        light.LightColor = key.LightColor;

        Environment env = worldEnv.Environment!;
        env.AmbientLightColor = key.AmbientColor;
        env.AmbientLightEnergy = key.AmbientEnergy;
        if (env.Sky?.SkyMaterial is ProceduralSkyMaterial skyMat)
        {
            skyMat.SkyTopColor = key.SkyTop;
            skyMat.SkyHorizonColor = key.SkyHorizon;
        }
        env.FogSkyAffect = 0f;   // PINNED S13 carry — re-pinned, never raised
    }
}
