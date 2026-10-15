// SIZE: 104 l measured by wc AT this head — MC 10519 / 10026.35 S27: the zone-scene
// mount route is a NEW partial of the composition root (W5 law: WorldDirector.cs is
// AT its ceiling; new logic lives in a new partial, the shipped Ground/DayNight
// idiom), one concern per file: WHICH scene is mounted under World when a zone is
// entered (mount/unmount swap at the ONE EnterZone route boot + travel + load +
// boss re-field all pass through).
using Godot;

// Last Animal — MC 10519 / 10026.35 S27 zone-scene mounting on travel.
//
// One line: traveling changes what you SEE, not just what spawns. EnterZone
// mounts the entered zone's OWN scene under World (zones/<id>/<id>.tscn) and
// unmounts the previous one, so the mounted Environment (sky/fog/light grade)
// + Ground + Decor set ARE the zone's identity — the A10 registered debt
// ("zone environments never change", LEDGER:104) closes at the ONE travel
// route, no second seam:
//   * boot, TravelToNextZone, SaveLoadController's load re-entry and the boss
//     re-field all funnel through EnterZone — one call site covers every path;
//   * the swap is SYNCHRONOUS (RemoveChild frees the tree slot NOW, QueueFree
//     frees the native object at end of frame) because ResolveGround() walks
//     the subtree for the terrain meta the actors are bounded by — the
//     PREVIOUS zone's ground must be OUT of that walk before the next zone's
//     actors are fielded (the MC 10420 bounds-before-actors order holds);
//   * re-entering the SAME zone (the boss-phase EnterZone(_zone)) is an
//     idempotent no-op — a mid-fight scene swap is never triggered by a
//     SpawnSet re-field;
//   * a zone id WITHOUT a scene keeps whatever is mounted and says so LOUD
//     (the MC 10404 never-silent idiom) — mount never blanks the world
//     silently, and no zone needs a special case: the path convention
//     zones/<id>/<id>.tscn covers all four table ids including hollow.tscn.
//
// The unmount FREES the nodes the day-night driver bound (S15 bound
// World/Meadow/Environment/* one-shot) — the stale-binding hazard closes via
// the named re-resolve step InvalidateDayNightBindings() (DayNight partial):
// the cached refs drop AT the swap and the driver's null-guard re-binds the
// MOUNTED Environment on its next tick (proof: ZONE_MOUNT_DRIVER_* markers).
//
// The zone .tscn root carries a static Camera3D (meadow W3-fix, MC 1123.10):
// mounting must never steal the viewport's current camera from FollowCamera —
// canyon/ruins shipped current=true (never visible until now) and carry the
// same data-fix in this commit; the ZONE_VISUAL_MOUNT leg asserts EXACTLY ONE
// current camera after every mount.
namespace LastAnimal.World;

public partial class WorldDirector
{
    // The mounted zone scene instance (null until the boot EnterZone mounts
    // the first one) + its zone id for the idempotent same-zone no-op.
    private Node3D? _zoneMount;
    private string _mountedZoneId = "";
    private bool _mountWarnedMissingScene;

    /// <summary>Read-only surface (proofs READ; mount authority stays here,
    /// the HasGroundExtent idiom): the mounted zone scene root, or null.</summary>
    public Node3D? MountedZoneRoot => _zoneMount;

    /// <summary>Mount <paramref name="zoneId"/>'s own scene under World,
    /// unmounting the previous one. Called at the TOP of EnterZone (before
    /// ResolveGround, so the ground resolution walks ONLY the mounted zone's
    /// terrain meta). Same-zone re-entry is a no-op; a missing scene keeps the
    /// current mount and warns LOUD once.</summary>
    private void MountZoneScene(string zoneId)
    {
        if (zoneId == _mountedZoneId && _zoneMount != null && GodotObject.IsInstanceValid(_zoneMount))
            return;   // boss re-field / duplicate entry: never swap mid-fight

        var world = GetNodeOrNull<Node3D>("World");
        if (world == null)
        {
            if (!_mountWarnedMissingScene)
                GD.PushError("MC 10519: composition root has no World node — zone-scene mounting DISABLED (never silent)");
            _mountWarnedMissingScene = true;
            return;
        }

        string path = $"res://zones/{zoneId}/{zoneId}.tscn";
        var packed = GD.Load<PackedScene>(path);
        if (packed == null)
        {
            // LOUD, never silent: the zone stays unmounted (its SpawnSet still
            // applies — travel is never blanked by a missing decor scene).
            if (!_mountWarnedMissingScene)
            {
                _mountWarnedMissingScene = true;
                GD.PushError($"MC 10519: zone scene missing at {path} — keeping the mounted zone scene (the ZONE_VISUAL_MOUNT battery leg goes RED on this, never silently)");
            }
            return;
        }

        if (_zoneMount != null && GodotObject.IsInstanceValid(_zoneMount))
        {
            world.RemoveChild(_zoneMount);   // synchronous tree removal BEFORE ResolveGround walks the meta
            _zoneMount.QueueFree();          // native free at end of frame (the shipped despawn idiom)
        }
        _zoneMount = null;
        InvalidateDayNightBindings();        // stale-binding close AT the swap (DayNight partial)

        var inst = packed.Instantiate<Node3D>();
        world.AddChild(inst);                // child _ready runs here (terrain meta publishes before ResolveGround)
        _zoneMount = inst;
        _mountedZoneId = zoneId;
        GD.Print($"S27: zone scene mounted World/{inst.Name} from {path} (previous zone scene unmounted; day-night driver re-binds next tick)");
    }
}
