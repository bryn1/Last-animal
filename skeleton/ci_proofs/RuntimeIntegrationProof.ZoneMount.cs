using Godot;
using LastAnimal.World;
using System.Collections.Generic;

// Last Animal — MC 10519 / 10026.35 S27 zone-scene-mounting proof (stage 106,
// mode ZONE_VISUAL_MOUNT).
//
// Proves the S27 contract on the LIVE playable scene (spawning quiet at boot —
// the bus_emit idiom — re-armed by the leg before its first travel, so every
// zone it ENTERS fields its SpawnSet for real):
//   1. TRAVEL CHANGES WHAT IS MOUNTED: every EnterZone mounts the zone's own
//      scene under World and unmounts the previous one — the leg walks
//      meadow -> canyon -> ruins -> hollow (real "travel" presses, ZoneBoss-
//      Proof PressTravel idiom) and after each step asserts the MOUNTED root
//      is the entered zone's (World/<Zone>), the PREVIOUS root is GONE, and
//      the zone's SpawnSet still applied (enemies > 0 — the mount did not
//      break the spawn wire).
//   2. NODE-IDENTITY CENSUS before/after every travel: the leg PRINTS the
//      census (count + "Name#instance-id" ids) of the mounted zone's
//      World/<Zone>/Environment subtree; id sets must be disjoint across
//      mounts (the old nodes are FREED — no silent reuse), and hollow's
//      census must differ from meadow/canyon/ruins (its Environment subtree
//      ships a distinct node COUNT, so zone identity is visible in the log).
//   3. EXACTLY ONE CURRENT CAMERA after every mount (the W5-DA F-1 dual-
//      current class: canyon/ruins shipped current=true — mounting verbatim
//      would steal the viewport camera from FollowCamera; the leg counts
//      IsCurrent() cameras over the whole scene and names the winner).
//   4. DAY-NIGHT DRIVES THE MOUNTED ENV (the stale-binding close): the leg
//      PARKS across every mount boundary (>= a full 0.5 s decor-cull ride —
//      zone.gd's throttled cull + the deferred navmesh bake both land inside
//      the park) then PAUSES the driver and reads the mounted scene's light +
//      Environment against DayNightClock.Evaluate(zone, applied-frame) BIT-
//      EXACT (the S15 read idiom). A driver still bound to the freed zone
//      scene cannot write the mounted nodes — the marker cannot print.
//   5. LOAD MOUNTS THE SAVED ZONE: save in hollow, travel to meadow, load —
//      the mounted root must be World/Hollow again (ZoneId already rides the
//      v3 wire; zero save delta, F2).
//   6. hollow.png VISUAL-IDENTITY CAPTURE: the leg RENDERS a per-zone census
//      plate (noon identity colors per DayNightClock table + live fog color +
//      Environment node-count bars, one column per zone) with Image +
//      SavePng — an evidence render produced BY the leg, not an art asset.
//      Path comes from LA_MOUNT_CAPTURE_PATH (the battery passes
//      <proj>/.tmp/s39/hollow.png); the ambient and fog rows must be
//      pairwise-distinct across the four columns — asserted on the rendered
//      pixels before the save (a real instrument, not a tautology).
//
// No save content is asserted (identity only; the death_load leg owns the
// save file's CONTENT, MC 3910); zero FALL_RECOVERED: every park stands on
// the mounted zone's own ground inside the clamped extent.
//
// Run:  LA_GATE_MODE=ZONE_VISUAL_MOUNT $GODOT --headless --path <proj> \
//         --script res://ci_proofs/RuntimeIntegrationProof.cs
public partial class RuntimeIntegrationProof : SceneTree
{
    private readonly List<string> _zmPrevIds = new();
    private readonly Dictionary<string, int> _zmCounts = new();
    private readonly Dictionary<string, Color> _zmFog = new();
    private string _zmPrevRoot = "";
    private int _zmStep;                        // 0 boot .. 3 hollow, 4 save-load, 5 capture
    private bool _zmLanded;                     // current travel step already landed — now PARKING
    private int _zmPark;                        // frames parked inside the current step
    private int _zmStepFrames;                  // leg-owned watchdog (the entry's _stageFrames stays untouched)
    private int _zmTravelToggle;
    private int _zmFarmToggle;   // escort phase counter
    private bool _zmSaved;
    private bool _zmSpawnArmed;
    private const int ZmParkFrames = 40;        // > 0.5 s cull interval (30f) + deferred bake + driver rebind ticks
    private const int ZmPressFrames = 6;        // the shipped PressFrames press-settle window
    private const int ZmStepBudgetFrames = 900; // per-step watchdog (park + farm slack)
    private static readonly string[] ZmOrder = { "canyon", "ruins", "hollow" };
    private static readonly string[] ZmRootNames = { "Canyon", "Ruins", "Hollow" };

    private void ZoneMountFail(string why) => Fail("ZONE_VISUAL_MOUNT: " + why);

    // ---- census + camera instruments (whole-scene manual walks — the
    // BridgeMvpProof/Zone4 idiom: GetChildren(bool) is include_internal, NOT
    // recursion, in GodotSharp 4.7.2) ------------------------------------------
    private void ZoneMountCensus(string zoneId)
    {
        var ids = EnvIdsNow();
        if (ids == null)
        { ZoneMountFail($"no mounted zone scene at entry to {zoneId} (MountedZoneRoot null)"); return; }
        var root = _director!.MountedZoneRoot!;
        _zmCounts[zoneId] = ids.Count;
        if (root.GetNodeOrNull("Environment/WorldEnvironment") is WorldEnvironment we && we.Environment != null)
            _zmFog[zoneId] = we.Environment.FogLightColor;
        GD.Print($"LA_GATE: ZONE_MOUNT_CENSUS zone={zoneId} root=World/{root.Name} nodes={ids.Count} ids={string.Join(",", ids)}");
        // MC 11037 / 10026.44.8 ground-plate wiring READBACK (the S41 material-
        // readback idiom; the battery leg owning GROUND_TEX_WIRING owns the
        // verdict — the shell-side Q2 census-count compare is the precedent):
        // what THIS mount's terrain builder actually carries as
        // ground_texture_path, and whether the built TerrainVisual material
        // carries a NON-null albedo plate. painted=0 is the WHITE-GROUND class
        // the TEST wall 2026-10-20 named: a mis-pathed plate makes
        // terrain_builder.gd's load() log its engine resource-not-found ERROR
        // and return null — the zone still mounts and the process still exits
        // 0, so only a NAMED leg can catch it. Print-only BY DESIGN: a plant
        // must reach the battery leg's named GROUND_TEX_WIRING RED, not fail
        // the host mode mid-mount — a fail here would turn the battery RED at
        // the STALE marker greps before the leg's own assert ever runs.
        var ground = root.GetNodeOrNull("Ground");
        string gtex = ground?.Get("ground_texture_path").AsString() ?? "";
        bool painted = ground?.GetNodeOrNull("TerrainVisual") is MeshInstance3D gmv
                       && gmv.MaterialOverride is StandardMaterial3D gsm && gsm.AlbedoTexture != null;
        GD.Print($"LA_GATE: ZONE_MOUNT_GROUNDTEX zone={zoneId} tex={gtex} painted={(painted ? 1 : 0)}");
    }

    private static void CollectEnvIds(Node n, List<string> into)
    {
        into.Add($"{n.Name}#{n.GetInstanceId()}");
        foreach (var c in n.GetChildren())
            CollectEnvIds(c, into);
    }

    /// <summary>Environment-subtree ids of the CURRENT mount, or null when no
    /// zone scene / no Environment subtree exists (structural failure).
    /// Cached fields never hold the ids across mounts — resolved fresh.</summary>
    private List<string>? EnvIdsNow()
    {
        var root = _director!.MountedZoneRoot;
        if (root == null || !GodotObject.IsInstanceValid(root)) return null;
        var env = root.GetNodeOrNull("Environment");
        if (env == null) return null;
        var ids = new List<string>();
        CollectEnvIds(env, ids);
        return ids;
    }

    private void ZoneMountCameraOne(string zoneId)
    {
        int current = 0;
        string winner = "";
        CountCurrentCameras(_main!, ref current, ref winner);
        // The viewport winner must be EXACTLY one camera and it must be the
        // composition root's FollowCamera rig (path /root/Main/Camera/Camera3D
        // — GetPath keeps the /root prefix, ZoneBossProof idiom-free compare).
        if (current != 1 || winner != "root/Main/Camera/Camera3D")
        { ZoneMountFail($"after mount of {zoneId}: current-camera count={current} winner={winner} " +
                        "(EXACTLY ONE current camera expected, and it must be the FollowCamera — the S27 mount may never steal it)"); return; }
        GD.Print($"LA_GATE: ZONE_MOUNT_CAMERA_ONE zone={zoneId} current=1 winner={winner}");
    }

    private static void CountCurrentCameras(Node n, ref int count, ref string winner)
    {
        if (n is Camera3D cam && cam.IsCurrent()) { count++; winner = cam.GetPath().ToString().TrimStart('/'); }
        foreach (var c in n.GetChildren())
            CountCurrentCameras(c, ref count, ref winner);
    }

    // ---- the S15-read driver assert (mounted env, bit-exact) --------------
    private void ZoneMountDriverWrites(string zoneId)
    {
        var root = _director!.MountedZoneRoot;
        var light = root?.GetNodeOrNull<DirectionalLight3D>("Environment/DirectionalLight3D");
        var env = root?.GetNodeOrNull<WorldEnvironment>("Environment/WorldEnvironment");
        if (light == null || env?.Environment == null)
        { ZoneMountFail($"mounted {(root?.Name ?? "<none>")} ships no light/WorldEnvironment for the driver"); return; }
        _director!.SetDayNightPaused(true);   // freeze the clock: applied-frame stamp + node writes become ONE consistent read
        int applied = _director!.DayNightAppliedFrame;
        if (applied < 0)
        { _director!.SetDayNightPaused(false); ZoneMountFail("day-night driver never ticked onto the mounted env (applied-frame still -1 after the park)"); return; }
        var key = DayNightClock.Evaluate(zoneId, applied);
        float tol = DayNightTuning.RotationTolerance;
        bool rotOk = System.Math.Abs(light.Rotation.X - Mathf.DegToRad(key.SunPitchDegrees)) < tol
                  && System.Math.Abs(light.Rotation.Y - Mathf.DegToRad(key.SunYawDegrees)) < tol;
        bool ok = rotOk
            && light.LightEnergy == key.LightEnergy && light.LightColor == key.LightColor
            && env.Environment.AmbientLightColor == key.AmbientColor
            && env.Environment.AmbientLightEnergy == key.AmbientEnergy;
        if (env.Environment.Sky?.SkyMaterial is ProceduralSkyMaterial skyMat)
            ok &= skyMat.SkyTopColor == key.SkyTop && skyMat.SkyHorizonColor == key.SkyHorizon;
        _director!.SetDayNightPaused(false);
        if (!ok)
        {
            // Stale-binding RED class: a driver still bound to the FREED zone
            // scene writes THERE, so the mounted nodes never carry the table.
            ZoneMountFail($"mounted env NOT driven at frame {applied}: le={light.LightEnergy:0.###} want={key.LightEnergy:0.###} " +
                          $"amb={env.Environment.AmbientLightColor} want={key.AmbientColor} (re-bind at mount broken)");
            return;
        }
        GD.Print($"LA_GATE: ZONE_MOUNT_DRIVER_{zoneId.ToUpperInvariant()} zone={zoneId} applied={applied} " +
                 $"le={light.LightEnergy:0.###} amb={env.Environment.AmbientLightEnergy:0.###} — driver writes the MOUNTED env (S27 re-bind at swap)");
    }

    // ---- census disjointness + the hollow-must-differ assert --------------
    private void ZoneMountIdsDisjoint(List<string>? before, List<string>? after, string fromZone, string toZone)
    {
        if (before == null || after == null)
        { ZoneMountFail($"census walk failed around the {fromZone} -> {toZone} swap"); return; }
        foreach (var id in after)
            if (before.Contains(id))
            { ZoneMountFail($"env node identity {id} SURVIVED the {fromZone} -> {toZone} mount swap — stale nodes still mounted (census not disjoint)"); return; }
        GD.Print($"LA_GATE: ZONE_MOUNT_CENSUS_DISJOINT {fromZone}->{toZone} {before.Count}->{after.Count} ids disjoint (old Environment subtree FREED by the unmount)");
    }

    private void ZoneMountHollowDiffers()
    {
        int h = _zmCounts["hollow"];
        string others = "";
        foreach (var z in new[] { "meadow", "canyon", "ruins" })
        {
            if (_zmCounts[z] == h)
            { ZoneMountFail($"hollow Environment node count {h} == {z}'s — zone visual identity NOT visibly distinct (census counts must differ)"); return; }
            others += $"{z}={_zmCounts[z]} ";
        }
        GD.Print($"LA_GATE: ZONE_MOUNT_HOLLOW_DISTINCT hollow={h} vs {others}(counts + ids differ per zone)");
    }

    private bool PressZmTravel()
    {
        // ZoneBossProof / Zone4 PressTravel idiom VERBATIM in behaviour.
        _zmTravelToggle++;
        if (_zmTravelToggle == 1) Input.ActionPress("travel");
        if (_zmTravelToggle >= ZmPressFrames)
        {
            Input.ActionRelease("travel");
            _zmTravelToggle = 0;
            return true;
        }
        return false;
    }

    /// <summary>ESCORT (witness) idiom — the leg WATCHES, it never fights:
    /// while steps 0-4 ride, the player alternates two safe columns
    /// (x = ∓10 m from the zone centre, y = 3 — the S18-verified spawn height,
    /// centre heights ≤ 1.6 m per the S38 floor-trust record, both columns
    /// deep inside every zone's clamped extent) each frame, so pursuers reset
    /// before melee contact: ZERO whittle, ZERO FALL_RECOVERED, zero kills.
    /// This replaced the KillLoop farm after run 1 VERIFIED its failure
    /// mode: farmed-but-slow players reached Health 0 in the hollow park, and
    /// the step-4 save would then have carried a corpse (the shipped restore
    /// re-applies saved HP verbatim — run 3's red proves it reads back what
    /// was written). The SpawnSet assert is UNWEAKENED: it reads the live set
    /// at the arrival frame, before any escort frame.</summary>
    private void ZmEscort()
    {
        _zmFarmToggle++;
        _playerBody!.GlobalPosition = _zmFarmToggle % 2 == 0
            ? new Vector3(-10f, 3f, 0f)
            : new Vector3(10f, 3f, 0f);
    }

    // ---- the leg ------------------------------------------------------------
    private void RunZoneMountStage()
    {
        if (_failed) return;
        _zmStepFrames++;
        if (_zmStepFrames > ZmStepBudgetFrames)
        { ZoneMountFail($"step {_zmStep} budget exhausted (travel/load/park never completed)"); return; }

        if (!_zmSpawnArmed)
        {
            // Quiet boot (bus_emit idiom) — the leg arms the SAME public seam
            // before its first travel so every zone it ENTERS fields its set
            // (the boot meadow set stays empty BY DESIGN, the census/park
            // below asserts mount identity, not boot spawns).
            _director!.SetSpawningEnabled(true);
            _zmSpawnArmed = true;
        }

        // Escort the witness through every OBSERVING step (0-4): the zone's
        // SpawnSet is live from arrival, the leg neither fights nor stands —
        // see ZmEscort's record for why (run 1's farm died, run 3's red named
        // the verbatim-restore consequence).
        if (_zmStep <= 4) ZmEscort();

        switch (_zmStep)
        {
            case 0:
                ZoneMountBoot();
                break;
            case 1:
            case 2:
            case 3:
                ZoneMountTravel(ZmOrder[_zmStep - 1], ZmRootNames[_zmStep - 1]);
                break;
            case 4:
                ZoneMountSaveLoad();
                break;
            case 5:
                ZoneMountCaptureAndPass();
                break;
        }
    }

    private void ZoneMountBoot()
    {
        var root = _director!.MountedZoneRoot;
        if (_director!.CurrentZone != "meadow" || root == null || root.Name != "Meadow")
        { ZoneMountFail($"boot mount wrong: zone={_director!.CurrentZone} root={(root?.Name ?? "<none>")} (expected meadow / World/Meadow mounted by the boot EnterZone)"); return; }
        if (_zmPark == 0)
        {
            ZoneMountCensus("meadow");     // BEFORE the first travel
            if (_failed) return;
            ZoneMountCameraOne("meadow");
            if (_failed) return;
            SavePrev();
        }
        _zmPark++;   // PARK across the boot mount boundary (deferred bake + first driver ticks)
        if (_zmPark < ZmParkFrames) return;
        ZoneMountDriverWrites("meadow");
        if (_failed) return;
        _zmStep = 1; _zmStepFrames = 0; _zmPark = 0; _zmLanded = false; _zmTravelToggle = 0;
    }

    private void ZoneMountTravel(string zoneId, string rootName)
    {
        if (!_zmLanded)
        {
            if (PressZmTravel() && _director!.CurrentZone == zoneId)
            {
                var mounted = _director!.MountedZoneRoot;
                if (mounted == null || mounted.Name != rootName)
                { ZoneMountFail($"travelled to {zoneId} but mounted root is {(mounted?.Name ?? "<none>")} (expected World/{rootName})"); return; }
                if (_main!.GetNodeOrNull("World/" + _zmPrevRoot) != null)
                { ZoneMountFail($"previous root World/{_zmPrevRoot} still mounted after travelling to {zoneId}"); return; }
                GD.Print($"LA_GATE: ZONE_MOUNT_MOUNTED zone={zoneId} root=World/{rootName} prev=World/{_zmPrevRoot}");
                Check($"zone '{zoneId}' SpawnSet still applied through the mount swap",
                      _director!.Enemies.Count > 0, $"enemies={_director!.Enemies.Count}");
                if (_failed) return;
                GD.Print($"LA_GATE: ZONE_MOUNT_SPAWNSET zone={zoneId} enemies={_director!.Enemies.Count}");
                ZoneMountCameraOne(zoneId);
                if (_failed) return;
                var before = new List<string>(_zmPrevIds);
                ZoneMountCensus(zoneId);   // AFTER the travel (before-census rode the previous step)
                if (_failed) return;
                ZoneMountIdsDisjoint(before, EnvIdsNow(), _zmPrevRoot.ToLowerInvariant(), zoneId);
                if (_failed) return;
                _zmLanded = true;
                _zmPark = 0;
            }
            return;
        }

        // PARKED leg: ride the mount boundary (>= the 0.5 s cull interval);
        // the escort above keeps the witness out of melee across the window.
        _zmPark++;
        if (_zmPark < ZmParkFrames) return;
        ZoneMountDriverWrites(zoneId);
        if (_failed) return;
        if (zoneId == "hollow") ZoneMountHollowDiffers();
        if (_failed) return;
        SavePrev();
        _zmStep++; _zmStepFrames = 0; _zmPark = 0; _zmLanded = false; _zmTravelToggle = 0;
    }

    private void ZoneMountSaveLoad()
    {
        // Save IN hollow (F5 path, the death_load SaveGame() precedent), travel
        // to meadow (a mount swap away — the load must undo it), then F9: the
        // saved ZoneId re-mounts hollow. Zero save delta: ZoneId already ships.
        if (!_zmLanded)
        {
            // Save EXACTLY ONCE while standing in hollow. (Run-1 lesson: an
            // unguarded SaveGame() re-runs every frame of the 6-frame press
            // window — the travel lands mid-window and the LAST write wins
            // with zone=meadow, silently undoing the leg's own premise.)
            if (!_zmSaved)
            {
                _director!.SaveGame();
                _zmSaved = true;
                GD.Print($"LA_GATE: ZONE_MOUNT_SAVED zone={_director!.CurrentZone} health={_director!.PlayerModel.Health} — one-shot save on hollow ground (ZoneId rides the v3 wire, MC 3910 OWNED; content is the death_load leg's turf, not ours)");
            }
            if (PressZmTravel() && _director!.CurrentZone == "meadow")
            {
                var mounted = _director!.MountedZoneRoot;
                if (mounted == null || mounted.Name != "Meadow")
                { ZoneMountFail($"travel to meadow before load mounted {(mounted?.Name ?? "<none>")}"); return; }
                GD.Print("LA_GATE: ZONE_MOUNT_PRELOAD_MEADOW — save written in hollow, mounted meadow again (the load must flip it back)");
                _director!.LoadGame();
                mounted = _director!.MountedZoneRoot;
                if (_director!.CurrentZone != "hollow" || mounted == null || mounted.Name != "Hollow")
                { ZoneMountFail($"LOAD did not re-mount the saved zone: zone={_director!.CurrentZone} root={(mounted?.Name ?? "<none>")} (expected hollow / World/Hollow)"); return; }
                if (_main!.GetNodeOrNull("World/Meadow") != null)
                { ZoneMountFail("load left World/Meadow mounted beside the saved zone (mount swap not exclusive)"); return; }
                // The shipped restore re-applies saved HP VERBATIM — with the
                // escort keeping the witness alive, the load must read back
                // LIVE Life. (Run-3 history: a corpse-carrying save read back
                // 0 here and went RED by name — the read stays so the plate
                // can never hide behind a dead round-trip again.)
                Check("save carried live Life through the mount round-trip", !_director!.PlayerModel.IsDead,
                      $"health={_director!.PlayerModel.Health} after load");
                if (_failed) return;
                GD.Print("LA_GATE: ZONE_MOUNT_LOAD_HOLLOW — LOAD path mounts the SAVED zone (World/Hollow; meadow unmounted; zero save fields touched)");
                ZoneMountCameraOne("hollow");
                if (_failed) return;
                _zmLanded = true;
                _zmPark = 0;
            }
            return;
        }

        _zmPark++;   // PARK across the LOAD's mount boundary too (escorted)
        if (_zmPark < ZmParkFrames) return;
        ZoneMountDriverWrites("hollow");
        if (_failed) return;
        _zmStep = 5; _zmStepFrames = 0; _zmPark = 0; _zmLanded = false; _zmTravelToggle = 0;
    }

    private void ZoneMountCaptureAndPass()
    {
        // The census plate: one 32 px column per zone, eight 32 px bands —
        // the zone's NOON identity straight off the const DayNightClock table
        // (sky top / sky horizon / light color / ambient), the LIVE fog color
        // read off the mounted Environment during the census, then two count-
        // bar bands (Environment subtree node count, bottom-anchored grey).
        // Evidence render produced BY the leg, not an art asset.
        string[] zones = { "meadow", "canyon", "ruins", "hollow" };
        var img = Image.CreateEmpty(4 * 32, 8 * 32, false, Image.Format.Rgba8);
        for (int z = 0; z < zones.Length; z++)
        {
            var key = DayNightClock.Evaluate(zones[z], DayNightClock.NoonFrame);
            Color fog = _zmFog.TryGetValue(zones[z], out var f) ? f : new Color(0, 0, 0);
            int nodes = _zmCounts.TryGetValue(zones[z], out int c) ? c : 0;
            Color[] bands = { key.SkyTop, key.SkyHorizon, key.LightColor, key.AmbientColor, fog,
                              new Color(0f, 0f, 0f), new Color(0f, 0f, 0f), new Color(0.12f, 0.12f, 0.12f) };
            for (int b = 0; b < bands.Length; b++)
                img.FillRect(new Rect2I(z * 32, b * 32, 32, 32), bands[b]);
            int bar = Mathf.Min(nodes * 4, 32);   // count bar bottom-anchored in bands 5+6 (0-height = a bug, never silent)
            if (nodes == 0)
            { ZoneMountFail($"census render: zone {zones[z]} recorded ZERO Environment nodes"); return; }
            img.FillRect(new Rect2I(z * 32, 8 * 32 - bar, 32, bar), new Color(0.85f, 0.85f, 0.85f));
        }
        // Non-vacuous identity asserts ON THE RENDERED PIXELS: the ambient row
        // (band 3 — ZONE4_KEY4_DISTINCT's pairwise truth, re-instrumented here
        // for the plate) and the fog row (band 4 — the shipped tscn fog grades
        // are four distinct colors) must EACH separate every column pair.
        for (int a = 0; a < 3; a++)
            for (int b = a + 1; b < 4; b++)
            {
                if (img.GetPixel(a * 32 + 16, 3 * 32 + 16) == img.GetPixel(b * 32 + 16, 3 * 32 + 16))
                { ZoneMountFail($"capture columns {zones[a]}/{zones[b]} share the noon ambient pixel — plate proves no per-zone identity"); return; }
                if (img.GetPixel(a * 32 + 16, 4 * 32 + 16) == img.GetPixel(b * 32 + 16, 4 * 32 + 16))
                { ZoneMountFail($"capture columns {zones[a]}/{zones[b]} share the fog-grade pixel — plate proves no per-zone identity"); return; }
            }

        string path = System.Environment.GetEnvironmentVariable("LA_MOUNT_CAPTURE_PATH")
                      ?? "user://hollow.png";
        if (!path.StartsWith("user://") && !path.StartsWith("res://"))
        {
            string? dir = System.IO.Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) System.IO.Directory.CreateDirectory(dir);
        }
        var err = img.SavePng(path);
        if (err != Error.Ok) { ZoneMountFail($"hollow.png census render SavePng failed ({err}) at {path}"); return; }
        string full = path.StartsWith("user://") || path.StartsWith("res://")
            ? ProjectSettings.GlobalizePath(path) : path;
        long bytes = new System.IO.FileInfo(full).Length;
        if (bytes <= 100) { ZoneMountFail($"hollow.png census render empty on disk ({bytes} bytes at {full})"); return; }
        GD.Print($"LA_GATE: ZONE_MOUNT_CAPTURE path={full} bytes={bytes} — hollow.png visual-identity census plate produced BY the leg");

        GD.Print("LA_GATE: PASS — S27 zone-scene mounting verified (boot mount; travel re-mounts the ENV identity per zone with printed before/after node-identity censuses, disjoint ids + hollow distinct; SpawnSet survives every swap; EXACTLY ONE current camera after every mount; day-night driver writes the MOUNTED env behind a park at every boundary — the stale-binding close; LOAD mounts the saved zone; hollow.png census plate rendered by the leg)");
        _asserted = true;
        ReleaseHeldRefsBeforeQuit();
        Quit(0);
    }

    // ---- prev-census bookkeeping (fresh walks, never cached nodes) ---------
    private void SavePrev()
    {
        var root = _director!.MountedZoneRoot;
        if (root == null) { ZoneMountFail("SavePrev with nothing mounted"); return; }
        _zmPrevRoot = root.Name;
        _zmPrevIds.Clear();
        var ids = EnvIdsNow();
        if (ids != null) _zmPrevIds.AddRange(ids);
    }
}
