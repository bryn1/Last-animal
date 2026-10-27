// Last Animal — MC 10519 / 10026.35 S27 zone-visual-mount proof, CHECK half
// (MC 11432 R2 split).
//
// Partial half of the ZONE_VISUAL_MOUNT leg: the census + camera + driver-
// binding + node-identity CHECK instruments (mounted-Environment census,
// exactly-one-current-camera walk, S15-read driver assert, census disjointness,
// hollow-must-differ) moved VERBATIM out of RuntimeIntegrationProof.ZoneMount.cs.
// ZERO behaviour change: the leg's travel/park/save/capture state machine stays
// in ZoneMount.cs, the stage arm + mode vocabulary in the entry RIP.cs.
// Same prints, same markers. Size census: entry header + MC 11037 battery section.
using Godot;
using LastAnimal.World;
using System.Collections.Generic;

public partial class RuntimeIntegrationProof : SceneTree
{    private void ZoneMountFail(string why) => Fail("ZONE_VISUAL_MOUNT: " + why);

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

}
