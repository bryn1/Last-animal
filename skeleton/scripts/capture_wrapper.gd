extends Node3D
## M07 capture wrapper (bernie, 2026-09-04)
##
## Wraps a zone scene so we can snapshot it headlessly and prove the render is
## real and DISTINCT per zone. The zone scene to load is taken from the
## ZONE_SCENE env var (a res:// path). We instance it, await a few frames so
## the static terrain mesh + sky + lights draw, then write the viewport to
## OUT_PATH and quit 0.
##
## Run (Forward+ needs a GL context, so wrap in xvfb-run):
##   ZONE_SCENE=res://zones/meadow/meadow.tscn OUT_PATH=/abs/meadow.png \
##     xvfb-run -a godot --path <proj> --editor scenery ...
## Or run the scene directly:
##   godot --path <proj> res://scripts/capture_scene.tscn
##
## MC 10165 S11 perf window (additive, same mechanism — NOT a second
## capture system): with PERF_WINDOW=N the wrapper samples the engine
## monitors every frame for N frames after the warm-up and prints one
## machine-readable line per sample:
##   LA_PERF: zone=<id> i=<k> ms_wall=<frame interval> \
##            ms_proc=<Performance.TIME_PROCESS> \
##            draw_calls=<Performance.RENDER_TOTAL_DRAW_CALLS_IN_FRAME>
## OUT_PATH is then optional; with PERF_WINDOW unset the M07 path is
## byte-for-byte the old behaviour. Monitors read 0.0 headless — the
## line is only meaningful under the real Xvfb surface (smoke/helper
## path); tools/perf_probe.sh enforces that bar.
## (ms_wall rides along: on this software-GL box the process step hides
## the render cost — the wall interval and draw numbers are the signal.)
##   ZONE_SCENE=res://zones/meadow/meadow.tscn PERF_WINDOW=60 \
##     godot --path <proj> res://capture_scene.tscn

func _ready() -> void:
    var zone_path: String = OS.get_environment("ZONE_SCENE")
    var out: String = OS.get_environment("OUT_PATH")
    var window: int = int(OS.get_environment("PERF_WINDOW"))
    if zone_path.is_empty():
        push_error("[capture] ZONE_SCENE unset")
        get_tree().quit(2)
        return
    if out.is_empty() and window <= 0:
        push_error("[capture] OUT_PATH unset")
        get_tree().quit(2)
        return
    var packed: PackedScene = load(zone_path)
    if packed == null:
        push_error("[capture] cannot load zone %s" % zone_path)
        get_tree().quit(3)
        return
    var inst: Node = packed.instantiate()
    add_child(inst)
    await get_tree().process_frame
    await get_tree().process_frame
    await get_tree().process_frame
    await get_tree().process_frame
    if window > 0:
        var zone_id: String = zone_path.get_file().get_basename()
        # Godot stdout is block-buffered when redirected; a perf log file with
        # an explicit flush keeps the samples readable even if the run is
        # killed mid-window (the driver kills via timeout, see perf_probe.sh).
        var perf_log: String = OS.get_environment("PERF_LOG")
        var f: FileAccess = null
        if not perf_log.is_empty():
            var ferr := FileAccess.open(perf_log, FileAccess.WRITE)
            f = ferr
            if f == null:
                push_warning("[capture] PERF_LOG open failed (%d) — stdout only" % FileAccess.get_open_error())
        var t_prev: int = Time.get_ticks_msec()
        for i in window:
            await get_tree().process_frame
            var t_now: int = Time.get_ticks_msec()
            # ms_wall = REAL frame interval on this surface (the software-GL
            # numbers before/after a change are comparable only as pairs on
            # the same machine+harness). ms_proc/ms_draw/draw_calls come from
            # Performance.get_monitor exactly as the S11 plan names them.
            var line: String = "LA_PERF: zone=%s i=%d ms_wall=%.3f ms_proc=%.3f draw_calls=%d" % [
                zone_id, i,
                float(t_now - t_prev),
                Performance.get_monitor(Performance.TIME_PROCESS),
                Performance.get_monitor(Performance.RENDER_TOTAL_DRAW_CALLS_IN_FRAME),
            ]
            t_prev = t_now
            print(line)
            if f != null:
                f.store_line(line)
                f.flush()
        if f != null:
            f.close()
        # MIN_LIFE_MS: stay alive this long (wall) after boot so the helper's
        # capture instant lands on a live windowed frame even when the window
        # finished early (frame cost varies with shared-lane load).
        var min_life: int = int(OS.get_environment("MIN_LIFE_MS"))
        while min_life > 0 and Time.get_ticks_msec() < min_life:
            await get_tree().process_frame
        get_tree().quit(0)
        return
    var img := get_viewport().get_texture().get_image()
    var err := img.save_png(out)
    print("[capture] saved %s (%dx%d) err=%d" % [out, img.get_width(), img.get_height(), err])
    get_tree().quit(0)
