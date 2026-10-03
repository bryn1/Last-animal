using Godot;
using LastAnimal.World;

// Last Animal — stage 2f skills-panel framebuffer capture script (MC 3933,
// code, 2026-10-02). Headful SceneTree script run TWICE by ci/ui_test.sh
// leg F under graphical-test-helper: LA_2F_TOGGLED=0 (panel closed) and
// LA_2F_TOGGLED=1 (TAB pressed -> panel open). The gate diffs the two
// captures inside the panel's fixed rect — the before/after proof that the
// real ui_toggle handler paints. Delete the TAB handler from TickUi and the
// two frames become identical and the leg goes RED (planted-bad #2).
//
// Determinism: NO Camera3D exists in this composition, so no 3D pixel can
// move between runs; every painted pixel is 2D UI over one uniform backdrop
// colour (the UiRenderTest backdrop idiom). The director boots for real
// (hand-attached Player + UI host, autoloads arrive per the MC 1344.1
// --script timing), so the toggle rides the REAL _Process poll — the same
// Input.ActionPress arm the runtime proofs use (BridgeMvpProof idiom).
public partial class SkillsPanelCaptureTest : SceneTree
{
    private WorldDirector? _director;
    private double _t;
    private bool _pressed;
    private bool _released;
    private bool _verdictPrinted;

    private static bool ToggleRequested =>
        OS.GetEnvironment("LA_2F_TOGGLED") == "1";

    public override void _Initialize()
    {
        GD.Print($"LA_2F_CAPTURE: start toggled={(ToggleRequested ? 1 : 0)}");

        // Frame pacing pinned like the proofs (physics-independent timing).
        Engine.MaxFps = 60;

        // One uniform backdrop so every pixel outside the UI is identical
        // across the two runs (palette: the UiRenderTest backdrop colour).
        // Layer 0 deliberately: on this engine a root-level layer-1 canvas
        // sits BESIDE the director's default-layer UI canvas instead of
        // under it (the HUD idiom escapes the same collision with Layer 2 —
        // see WorldDirector.BuildUi), so a sibling layer-1 backdrop would
        // bury the panels. At layer 0 the backdrop can only ever be floor.
        var backdropLayer = new CanvasLayer { Name = "Backdrop", Layer = 0 };
        var backdrop = new ColorRect { Color = new Color(0.10f, 0.12f, 0.14f) };
        backdrop.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        backdropLayer.AddChild(backdrop);
        Root.AddChild(backdropLayer);

        // The composition root boots for real — but hand-composed: a plain
        // Node3D player (no Player.cs, no CharacterBody3D, no camera) keeps
        // every 3D pixel out of the capture. WorldDirector._Ready resolves
        // Player/UICanvas by child name (its own fallback lookup) after the
        // autoload bus + bootstrap have arrived (MC 1344.1 timing).
        _director = new WorldDirector { Name = "Director" };
        _director.AddChild(new Node3D { Name = "Player" });
        _director.AddChild(new CanvasLayer { Name = "UI" });
        Root.AddChild(_director);
    }

    public override bool _Process(double delta)
    {
        _t += delta;

        // The TAB arm: a real press edge into the real ui_toggle poll.
        // Times are SHORT on purpose: the helper's --wait budget starts when
        // the process launches, so the usable window is (wait - boot). Boot
        // (C# init + autoloads) costs seconds on a cold shader cache — press
        // at 0.4s, verdict by 0.8s of live frames, captured whenever the
        // helper shoots after that (the opened panel is static).
        if (!_pressed && ToggleRequested && _t >= 0.4)
        {
            Input.ActionPress("ui_toggle");
            _pressed = true;
        }
        else if (_pressed && !_released && _t >= 0.5)
        {
            Input.ActionRelease("ui_toggle");
            _released = true;
        }

        // Verdict well before the helper's --wait 6 capture, so the marker
        // is in the gate log beside the PNG the pixels are measured from.
        if (!_verdictPrinted && _t >= 0.8)
        {
            _verdictPrinted = true;
            bool visible = _director != null && _director.SkillsUi != null &&
                           _director.SkillsUi.Visible;
            bool ok = visible == ToggleRequested;
            string want = ToggleRequested ? "1" : "0";
            GD.Print(ok
                ? $"LA_2F_CAPTURE: PASS — panel visible={visible} matches toggled={want}"
                : $"LA_2F_CAPTURE: FAIL — panel visible={visible} expected={want}");
        }

        // Stay alive past the capture; the helper kills this process.
        if (_t >= 20.0) Quit(_verdictPrinted ? 0 : 1);
        return false;
    }
}
