using Godot;
using LastAnimal.World;
using System;

// Last Animal — MC 10199 / 10026.26 S15: day-night CAPTURE script. Headful
// SceneTree script run TWICE by the S15 evidence pass under
// graphical-test-helper: LA_DAYNIGHT_ANCHOR=0 (dawn) and =1350 (night).
//
// Same capture idiom as HudVignetteCaptureTest (MC 10123): the real playable
// scene is instantiated and driven ONLY through the driver's presentation
// seams (SetDayNightFrame parks + writes the anchor row immediately, then
// SetDayNightPaused freezes that look — no game state is written); the
// verdict prints well before the helper's capture and the process stays
// alive until the helper kills it. The two captures being non-blank AND
// non-equal is EVIDENCE rows in the card's EVIDENCE.md (helper RESULT lines +
// an ImageMagick AE/RMSE pair), explicitly NOT a pixel gate (plan §S15).
public partial class DayNightCaptureTest : SceneTree
{
    private WorldDirector? _director;
    private double _t;
    private bool _parked;
    private bool _verdictPrinted;

    private static int RequestedAnchor =>
        int.TryParse(OS.GetEnvironment("LA_DAYNIGHT_ANCHOR"), out var v) ? v : 0;

    public override void _Initialize()
    {
        GD.Print($"LA_DAYNIGHT_CAPTURE: start anchor={RequestedAnchor}");
        Engine.MaxFps = 60;
        var packed = GD.Load<PackedScene>("res://main.tscn");
        if (packed == null) { GD.PrintErr("LA_DAYNIGHT_CAPTURE: FAIL — cannot load main.tscn"); Quit(1); return; }
        Root.AddChild(packed.Instantiate<Node3D>());
    }

    public override bool _Process(double delta)
    {
        _t += delta;
        _director ??= Root.GetNodeOrNull<WorldDirector>("Main");
        if (_director == null) return _t > 10;

        if (!_parked && _t > 0.1)
        {
            // Park = the seam WRITES the anchor row now (immediate apply),
            // then the pause freezes the look — physics-vs-idle pacing can
            // skip a next-tick-only value entirely (measured, MC 10199).
            _director.SetDayNightFrame(RequestedAnchor);
            _director.SetDayNightPaused(true);
            _parked = true;
        }

        if (!_verdictPrinted && _t >= 1.2)
        {
            _verdictPrinted = true;
            int applied = _director.DayNightAppliedFrame;
            bool ok = applied == DayNightClock.WrapFrame(RequestedAnchor);
            GD.Print(ok
                ? $"LA_DAYNIGHT_CAPTURE: PASS anchor={RequestedAnchor} applied={applied} paused=true"
                : $"LA_DAYNIGHT_CAPTURE: FAIL anchor={RequestedAnchor} applied={applied}");
        }
        // Stay alive past the capture; the helper kills this process.
        if (_t >= 25.0) Quit(_verdictPrinted ? 0 : 1);
        return false;
    }
}
