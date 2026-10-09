extends Node3D
## M07 terrain builder (Last Animal world-environment)
##
## Turns a heightmap PNG under res://assets/terrain/<zone>.png into BOTH:
##   - a HeightMapShape3D on a StaticBody3D collision shape (walkable ground, default
##     physics), resampled to one sample per metre over the h_scale-extended footprint so
##     the collider covers EXACTLY the visible ground (MC 10404: laid at pixel counts it
##     fell short of the visual span and the player fell through the ring), and
##   - a matching ArrayMesh visual surface whose vertices come from the SAME pixel data
##     at the same world scale, so collision and rendering provably agree.
##
## The PNG is the single source of truth: brightness = elevation. Bright pixel = high ground.
## Everything is built in _ready() so a plain `godot --path <proj> zones/<zone>/<zone>.tscn`
## headless run materialises both from the imported PNG asset with zero editor steps.
##
## Per zone config (exported): zone_id (the M12 spawner hook key), heightmap_path,
## horizontal scale (world units per pixel), height scale (world units per 255),
## and render_resolution (grid density of the visual mesh; shader-free, built directly).

const HD_RENDER: int = 64  # visual mesh grid resolution (keep lightweight)

@export var zone_id: StringName = &"zone"
@export_file("*.png") var heightmap_path: String = ""
@export var h_scale: float = 1.0   # world units per heightmap pixel
@export var v_scale: float = 1.0   # world units per 255 grey levels

var _heights: PackedFloat32Array
var _w: int = 0
var _d: int = 0

func _ready() -> void:
    if heightmap_path.is_empty():
        push_error("[terrain] %s: heightmap_path is empty" % name)
        return
    var img: Image = load(heightmap_path).get_image()
    if img.is_empty():
        push_error("[terrain] %s: could not load heightmap %s" % [name, heightmap_path])
        return
    img.convert(Image.FORMAT_RGBA8)
    _w = img.get_width()
    _d = img.get_height()
    _heights.resize(_w * _d)
    for y in _d:
        for x in _w:
            # brightness (0..1) -> elevation in world units (R==G==B for our grayscale source)
            _heights[y * _w + x] = img.get_pixel(x, y).r * 255.0 * v_scale / 255.0
    build_collision()
    build_visual_mesh()
    var mn: float = _heights[0]
    var mx: float = _heights[0]
    for h in _heights:
        mn = min(mn, h)
        mx = max(mx, h)
    print("[terrain] %s: %dx%d map, height range %.2f..%.2f" % [name, _w, _d, mn, mx])
    # MC 10404: the lowest point of THIS ground surface, published for the
    # director's below-plane fall-recovery guarantee (WorldDirector._Process;
    # the FLOOR_RECOVERED battery leg). Meta, not a new signal: read lazily.
    set_meta("floor_min_y", mn)

func build_collision() -> void:
    # MC 10404 (owner playtest P1 — fell THROUGH the visible ground and died):
    # HeightMapShape3D spans map_width x map_depth METRES (one sample per
    # metre), so laying it at the PNG pixel counts gave only _w x _d metres
    # of floor under a _w*h_scale x _d*h_scale visual ground — every shipped
    # zone (h_scale 1.5/1.6/2.0, all > 1) had a floorless ring past
    # +/- (map-half) that the player could walk onto (FLOOR_COVERAGE RED
    # pair: 60-76 % of the visible ground uncovered, uncovered == 1 -
    # 1/h_scale^2). Godot 4 physics IGNORES CollisionShape3D node scaling,
    # so the closure is geometry: resample to ONE SAMPLE PER METRE over the
    # scaled extent, sampling the SAME nearest-pixel height field as
    # build_visual_mesh at the SAME world<->pixel mapping (int(u *
    # (pixels-1)) — byte-mirrors the visual builder). Godot 4.7 uses map_data as RAW local-Y heights, so the old
    # body.position y-shift (a "visual mesh pivoted at top" compensation)
    # only floated the collider v_scale/2 ABOVE the ground (FLOOR_COVERAGE
    # DIAG: meadow hit-heights rode a median +0.82 m over a surface whose
    # bottom is 0.0) — it is gone. ceil(): a rounding step must never
    # re-leave an edge gap. Row/column pixel indices are precomputed so the
    # 256k-sample canyon grid is a flat lookup.
    # SEMANTICS MEASURED (FLOOR_COVERAGE edge pair): samples sit at 1 m
    # spacing and the mw-sample grid must OVERSHOOT the visual's +/- half-size
    # by a full sample each side (mw = ceil(size) + 2): with the grid ending
    # exactly at the border (mw = ceil(size), and even mw = ceil(size)+1
    # vertex-on-edge) rays landing ON the border vertex resolved
    # inconsistently — the canyon +/-256 ring missed at mw=512, meadow's
    # x=-48 row missed at mw=97 while +48 hit. One metre of overshoot makes
    # every visible border column strictly INTERIOR: deterministic hits.
    # Sample c therefore sits at x = c - (mw-1)/2; its height is the SAME
    # nearest pixel the visual vertex at that world x gets. The padded ring
    # clamps to the border pixel — a <=1.5 m skirt of edge height beyond the
    # visible ground, strictly better than a floorless border.
    var mw: int = int(ceil(_w * h_scale)) + 2
    var md: int = int(ceil(_d * h_scale)) + 2
    var size_x: float = _w * h_scale
    var size_z: float = _d * h_scale
    var cols := PackedInt32Array()
    for c in mw:
        var x: float = float(c) - float(mw - 1) * 0.5
        var u: float = x / size_x + 0.5
        cols.append(clampi(int(u * float(_w - 1)), 0, _w - 1))
    var data := PackedFloat32Array()
    data.resize(mw * md)
    for r in md:
        var z: float = float(r) - float(md - 1) * 0.5
        var v: float = z / size_z + 0.5
        var pz: int = clampi(int(v * float(_d - 1)), 0, _d - 1)
        var row: int = pz * _w
        for c in mw:
            data[r * mw + c] = _heights[row + cols[c]]
    var shape := HeightMapShape3D.new()
    shape.map_width = mw
    shape.map_depth = md
    # In Godot 4.7 the elevation array property is map_data (PackedFloat32Array);
    # map_width/map_depth set the local x/z size; map_data carries the heights.
    shape.map_data = data  # raw world-unit heights, same field as the visual
    var body := StaticBody3D.new()
    body.name = "TerrainCollision"
    var col := CollisionShape3D.new()
    col.name = "Shape"
    col.shape = shape
    body.add_child(col)
    # runtime-built bodies: owner is unnecessary (not saved to a packed scene),
    # and setting it on the parent while the child shape has none triggers a
    # "owner must be an ancestor" error — leave both owner-less.
    add_child(body)

func build_heightfield_mesh() -> ArrayMesh:
    # Walkable source for navmesh baking: a triangle mesh whose vertices sample the SAME
    # height data as the collision + visual mesh, at the same world scale (h_scale, v_scale).
    # This is a plain CPU mesh, so NavigationServer3D does NOT need to read back GPU mesh
    # data when baking (the engine's recommended path) — and it provably matches the ground.
    var n := HD_RENDER
    var tri := PackedVector3Array()
    var size_x := float(_w) * h_scale
    var size_z := float(_d) * h_scale
    for r in n:
        for c in n:
            var u0 := float(c) / float(n)
            var u1 := float(c + 1) / float(n)
            var v0 := float(r) / float(n)
            var v1 := float(r + 1) / float(n)
            var h00 := sample_height(int(u0 * (_w - 1)), int(v0 * (_d - 1)))
            var h10 := sample_height(int(u1 * (_w - 1)), int(v0 * (_d - 1)))
            var h01 := sample_height(int(u0 * (_w - 1)), int(v1 * (_d - 1)))
            var h11 := sample_height(int(u1 * (_w - 1)), int(v1 * (_d - 1)))
            var x0 := u0 * size_x - size_x / 2.0
            var x1 := u1 * size_x - size_x / 2.0
            var z0 := v0 * size_z - size_z / 2.0
            var z1 := v1 * size_z - size_z / 2.0
            tri.push_back(Vector3(x0, h00, z0)); tri.push_back(Vector3(x1, h10, z0)); tri.push_back(Vector3(x0, h01, z1))
            tri.push_back(Vector3(x1, h10, z0)); tri.push_back(Vector3(x1, h11, z1)); tri.push_back(Vector3(x0, h01, z1))
    var mesh := ArrayMesh.new()
    var a := []
    a.resize(Mesh.ARRAY_MAX)
    a[Mesh.ARRAY_VERTEX] = tri
    mesh.add_surface_from_arrays(Mesh.PRIMITIVE_TRIANGLES, a)
    return mesh

func sample_height(px: int, pz: int) -> float:
    return _heights[pz * _w + px]

func build_visual_mesh() -> void:
    # Build an ArrayMesh whose vertex grid samples the SAME _heights data at the SAME
    # world scale (h_scale per pixel, v_scale per grey), so the rendered surface sits
    # exactly on the collision heightmap. x spans width, z spans depth.
    var plat = PlaneMesh.new()
    plat.size = Vector2(_w * h_scale, _d * h_scale)
    plat.subdivide_width = HD_RENDER
    plat.subdivide_depth = HD_RENDER
    var aabb := plat.get_aabb()
    var arrays := []
    arrays.resize(Mesh.ARRAY_MAX)
    var verts := PackedVector3Array()
    var normals := PackedVector3Array()
    var idx := PackedInt32Array()
    var step := Vector2(_w - 1, _d - 1)  # map grid step over the plane's -0.5..0.5 uv

    for r in HD_RENDER + 1:
        for c in HD_RENDER + 1:
            var u: float = float(c) / float(HD_RENDER)
            var v: float = float(r) / float(HD_RENDER)
            # sample the heightmap pixel nearest this grid point
            var px := clampi(int(u * (_w - 1)), 0, _w - 1)
            var pz := clampi(int(v * (_d - 1)), 0, _d - 1)
            var h: float = _heights[pz * _w + px]
            var wx := -aabb.size.x * 0.5 + u * aabb.size.x
            var wz := -aabb.size.z * 0.5 + v * aabb.size.z
            verts.push_back(Vector3(wx, h, wz))
    # index buffer (two triangles per grid cell)
    for r in HD_RENDER:
        for c in HD_RENDER:
            var i0: int = r * (HD_RENDER + 1) + c
            var i1: int = i0 + 1
            var i2: int = i0 + (HD_RENDER + 1)
            var i3: int = i2 + 1
            idx.push_back(i0); idx.push_back(i2); idx.push_back(i1)
            idx.push_back(i1); idx.push_back(i2); idx.push_back(i3)
    arrays[Mesh.ARRAY_VERTEX] = verts
    arrays[Mesh.ARRAY_INDEX] = idx
    var mesh := ArrayMesh.new()
    mesh.add_surface_from_arrays(Mesh.PRIMITIVE_TRIANGLES, arrays)
    var mi := MeshInstance3D.new()
    mi.name = "TerrainVisual"
    mi.mesh = mesh
    # grass-coloured surface so the visual reads as ground (not wireframe black)
    var mat := StandardMaterial3D.new()
    mat.albedo_color = Color(0.35, 0.5, 0.28, 1.0)
    mat.roughness = 1.0
    mi.material_override = mat
    _recalc_normals(mesh)
    add_child(mi)

func _recalc_normals(mesh: ArrayMesh) -> void:
    # Rebuild smooth normals for the height field so lighting reads correctly.
    var surface := mesh.surface_get_arrays(0)
    var v: PackedVector3Array = surface[Mesh.ARRAY_VERTEX]
    var n := PackedVector3Array()
    n.resize(v.size())
    var idx: PackedInt32Array = surface[Mesh.ARRAY_INDEX]
    for i in range(0, idx.size(), 3):
        var a := v[idx[i]]
        var b := v[idx[i + 1]]
        var c := v[idx[i + 2]]
        var norm := (b - a).cross(c - a).normalized()
        n[idx[i]] += norm
        n[idx[i + 1]] += norm
        n[idx[i + 2]] += norm
    for i in n.size():
        n[i] = n[i].normalized()
    var sz := Mesh.ARRAY_MAX
    var out := []
    out.resize(sz)
    for i in sz:
        out[i] = surface[i]
    out[Mesh.ARRAY_NORMAL] = n
    mesh.surface_remove(0)
    mesh.add_surface_from_arrays(Mesh.PRIMITIVE_TRIANGLES, out)
