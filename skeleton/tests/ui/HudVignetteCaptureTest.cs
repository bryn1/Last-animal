using Godot;
using LastAnimal.Ui;
using System;

// Last Animal — MC 10123 / card 10026.15.4 S6: HUD low-health vignette
// capture script. Headful SceneTree script run TWICE by ci/ui_test.sh leg H
// (HUD_VIGNETTE) under graphical-test-helper: LA_VIGNETTE_HEALTH=29 (BELOW
// the UiTheme threshold -> tint ON, alpha > 0 every frame) and =30 (AT the
// threshold -> OFF, alpha EXACTLY 0 — the strict `<` boundary).
//
// Same capture idiom as SkillsPanelCaptureTest: one uniform layer-0 backdrop
// so the gate's corner crop is pure backdrop in the OFF run and visibly
// RED-tinted in the ON run (that pixel delta IS the "tint pixel non-blank"
// bar); the verdict prints well before the helper's --wait capture, and the
// process stays alive until the helper kills it.
//
// The Hud is the real production class driven by its real seams: health
// rides Bind (the combat-seam mirror the View reads — the script writes no
// game state, and the verdict re-checks the bound value survived every
// redraw), and RefreshLive is ticked per frame exactly as the director's
// TickUi drives it in play.
public partial class HudVignetteCaptureTest : SceneTree
{
    private Hud? _hud;
    private double _t;
    private bool _verdictPrinted;

    private static int RequestedHealth =>
        int.TryParse(OS.GetEnvironment("LA_VIGNETTE_HEALTH"), out var v) ? v : 55;

    public override void _Initialize()
    {
        GD.Print($"LA_VIGNETTE: start health={RequestedHealth}");
        Engine.MaxFps = 60;   // frame pacing pinned like the proofs

        // Uniform backdrop, layer 0 (SkillsPanelCaptureTest idiom): every
        // non-UI pixel is identical between the two runs, so the only thing
        // that can move the gate's corner crop is the vignette itself.
        var backdropLayer = new CanvasLayer { Name = "Backdrop", Layer = 0 };
        var backdrop = new ColorRect { Color = new Color(0.10f, 0.12f, 0.14f) };
        backdrop.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        backdropLayer.AddChild(backdrop);
        Root.AddChild(backdropLayer);

        // A CanvasLayer above the backdrop hosts the HUD (WorldDirector.BuildUi
        // idiom — the HUD rides a UI canvas layer, never the window root).
        var uiLayer = new CanvasLayer { Name = "UI", Layer = 1 };
        _hud = new Hud { Name = "Hud" };
        uiLayer.AddChild(_hud);
        Root.AddChild(uiLayer);
        _hud.Bind(RequestedHealth, 100, 0, 0);
    }

    public override bool _Process(double delta)
    {
        _t += delta;
        _hud?.RefreshLive();   // drives the vignette counter as TickUi does

        if (!_verdictPrinted && _t >= 0.8)
        {
            _verdictPrinted = true;
            int health = RequestedHealth;
            var vig = _hud?.GetNodeOrNull<ColorRect>("Vignette");
            float a = vig?.Color.A ?? -1f;
            bool wantOn = health < UiTheme.LowHealthThreshold;
            // ON: alpha strictly above zero in EVERY frame (pulse has a floor).
            // OFF: alpha exactly zero. Plus the read-not-write proof: the
            // health the View was handed is unchanged after every repaint.
            bool ok = vig != null && _hud != null &&
                      (wantOn ? a > 0f : a == 0f) && _hud.Life == health;
            GD.Print(ok
                ? $"LA_VIGNETTE: PASS — health={health} alpha={a:0.000} matches on={wantOn} life-mirror={_hud!.Life}"
                : $"LA_VIGNETTE: FAIL — health={health} alpha={a:0.000} expected on={wantOn}");
        }

        // Stay alive past the capture; the helper kills this process.
        if (_t >= 20.0) Quit(_verdictPrinted ? 0 : 1);
        return false;
    }
}
