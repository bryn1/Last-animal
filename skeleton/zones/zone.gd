extends Node3D
## M07 zone root script (Last Animal world-environment)
##
## Responsibilities per the M07 DoD / PHASE0.md Phase 6:
##   - exposes the M12 spawner hook key `zone_id` (C15: EcosystemSpawner.OnZoneEnter)
##     on a named `Spawners` node so later modules attach spawn logic there
##   - bakes the NavigationRegion3D navmesh at runtime from the SAME terrain mesh the
##     visual ground uses, so enemies/companions (M08/M05) can pathfind the zone.
##     Navmesh bake is fully headless (NavigationServer3D, Godot 4.7's documented path).

@export var zone_id: StringName = &"zone"

# MC 10165 S11 zone-visibility culling (Inc-3 plan action class 1): the
# engine's own VisibilityRange properties were tried FIRST and are NOT
# available on the GL Compatibility renderer (runtime "invalid assignment"
# under the CI Xvfb fallback — see .audits/*s11-perf* a1 evidence), so the
# zone root runs a throttled distance test and hides decor roots beyond
# DECOR_CULL_DISTANCE_M from the CURRENT camera — still the zone scene's
# own concern, no parallel culling system. In-game the follow camera never
# exceeds ~36 m to any decor at the spawn view (derived from the tscn
# transforms + FollowCamera offset); deep in a zone the far ring simply
# stops being drawn. The fixed perf-capture cameras sit 50-80 m from that
# ring, so the capture windows measurably shed its draw calls.
# MC 10184 S13 soft fade (owner ruling 2026-10-06, "Rec on all" accepting
# "accept 45 m cull now + soft distance-fade in S13"): one hard line at
# DECOR_CULL_DISTANCE_M meant walking players saw the far ring pop out
# whole. Decor roots now STAGGER out across the fade band — root i hides
# beyond a per-band threshold (DECOR_CULL_DISTANCE_M down to
# DECOR_FADE_INNER_M, band = i % DECOR_FADE_BANDS), and only re-shows with
# DECOR_FADE_HYSTERESIS_M of margin, so no flicker at a band edge. The
# whole band starts ABOVE the pinned <=36 m spawn-view reach, so the
# playable spawn view is unchanged BY CONSTRUCTION (nearest threshold is
# 39 m). Per-material alpha fades were rejected for this GL Compatibility
# surface: shared imported materials would cross-fade every instance and
# TRANSPARENT_alpha pulls decor into the sorted transparent pass (sort
# flicker + full-opacity shadows); the staged opaque hide is the soft
# transition the ruling names, and the S13 fog veil now carries most of
# the visual fade (a root popped at 39-45 m is already 22-42 % fog).
const DECOR_CULL_DISTANCE_M := 45.0   # outer band: fully hidden beyond
const DECOR_FADE_INNER_M := 36.0      # spawn-view reach (pinned): nothing hides nearer
const DECOR_FADE_BANDS := 3           # staggered thresholds: 45 / 42 / 39 m
const DECOR_FADE_HYSTERESIS_M := 2.0  # re-show margin per band
const DECOR_CULL_INTERVAL := 0.5   # s between checks; 10-12 roots per zone — cheap

@onready var ground: Node3D = $Ground

var _decor_roots: Array[Node3D] = []
var _cull_acc: float = DECOR_CULL_INTERVAL   # first _process culls immediately

func _ready() -> void:
    # M12 hook surface: record the zone id on the Spawners node so
    # EcosystemSpawner.OnZoneEnter(zoneId) (C15) has a stable, queryable anchor.
    spawners().set_meta("zone_id", String(zone_id))
    cull_far_decor_collect()
    print("[zone] %s ready; zone_id=%s" % [name, zone_id])
    bake_navmesh.call_deferred()

func cull_far_decor_collect() -> void:
    var decor := get_node_or_null("Decor")
    if decor == null:
        return
    for c in decor.get_children():
        if c is Node3D:
            _decor_roots.append(c)
    if _decor_roots.size() > 0:
        # S11 marker string kept verbatim (gates/captures grep it); the S13
        # fade description rides on the same line.
        print("[zone] %s: decor distance cull armed at %.0fm over %d roots; soft fade %d bands %d..%dm, hysteresis %.0fm" % [name, DECOR_CULL_DISTANCE_M, _decor_roots.size(), DECOR_FADE_BANDS, int(DECOR_FADE_INNER_M + (DECOR_CULL_DISTANCE_M - DECOR_FADE_INNER_M) / DECOR_FADE_BANDS), int(DECOR_CULL_DISTANCE_M), DECOR_FADE_HYSTERESIS_M])

func _process(delta: float) -> void:
    if _decor_roots.is_empty():
        return
    _cull_acc += delta
    if _cull_acc < DECOR_CULL_INTERVAL:
        return
    _cull_acc = 0.0
    var cam := get_viewport().get_camera_3d()
    if cam == null:
        return   # no view (headless): never hide
    var cp: Vector3 = cam.global_position
    for i in _decor_roots.size():
        var d: Node3D = _decor_roots[i]
        var hide_at: float = band_hide_at(i)
        var dist: float = cp.distance_to(d.global_position)
        if d.visible:
            if dist > hide_at:
                d.visible = false
        elif dist < hide_at - DECOR_FADE_HYSTERESIS_M:
            d.visible = true

func band_hide_at(idx: int) -> float:
    # Band 0 hides at the outer line (45 m), band 1 at 42, band 2 at 39:
    # roots were collected in scene order (spatially mixed), so each band
    # edge sheds roughly a third of the remaining ring, not all of it.
    var band: int = idx % DECOR_FADE_BANDS
    return DECOR_CULL_DISTANCE_M - band * (DECOR_CULL_DISTANCE_M - DECOR_FADE_INNER_M) / DECOR_FADE_BANDS

func spawners() -> Node3D:
    return get_node_or_null("Spawners") if has_node("Spawners") else self

func bake_navmesh() -> void:
    var nav := get_node_or_null("Navigation") as NavigationRegion3D
    if nav == null:
        push_warning("[zone] %s: no Navigation region to bake" % name)
        return
    # Source geometry for the navmesh = a CPU-side triangle mesh built from the SAME
    # height data as the ground's collision + visual mesh. HeightMapShape3D collision is
    # not directly navmesh-bakeable, so we feed this matching heightfield mesh instead —
    # walkable poly coincides with the ground the player stands on, with no GPU read-back.
    var src := NavigationMeshSourceGeometryData3D.new()
    var src_mesh: ArrayMesh = null
    if ground.has_method("build_heightfield_mesh"):
        src_mesh = ground.build_heightfield_mesh()
    if src_mesh != null:
        src.add_mesh(src_mesh, Transform3D.IDENTITY)
    NavigationServer3D.bake_from_source_geometry_data(nav.navigation_mesh, src)
    var poly: int = nav.navigation_mesh.get_polygon_count()
    var ok: bool = poly > 0
    if ok:
        print("[zone] %s: navmesh baked, polygons=%d" % [name, poly])
    else:
        push_warning("[zone] %s: navmesh bake produced 0 polygons (terrain too steep?)" % name)
