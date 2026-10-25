using Godot;
using LastAnimal.Ui;
using LastAnimal.World;
using System.Collections.Generic;

// Last Animal — MC 11293 D1 proof: HUD text and the dialogue / Empathy-Book
// surfaces must never print over each other (the playthrough P1, MC 11270
// D1: B/E use painted the empathy summary + dialogue line directly onto the
// Life/Manna/DNA/Hearts/Skills/Quest block — both layers illegible).
//
// The proof boots the REAL playable scene (main.tscn, P1FixProof idiom),
// OPENS the empathy book and a dialogue line SIMULTANEOUSLY through the
// shipped inputs (book=B, interact=E), then measures the PAINTED bounding
// boxes (Control.GetGlobalRect widened to the text's combined min-size — the
// pre-fix collapse painted while the control rect was zero, so a bare rect
// compare would have gone green on the broken layout) and asserts the
// dialogue box and every Empathy-Book label are DISJOINT from every HUD
// gauge row. Overlap prints a NAMED FAIL (HUD_DIALOGUE_OVERLAP /
// HUD_EMPATHY_OVERLAP) with both rects; the gate legs grep the markers.
//
// Two-surface guard (the vacuous-green wall): the proof FAILS by name
// (OVERLAP_PROOF_NOT_ARMED) unless the dialogue AND the book are both
// genuinely open at measure time — an assert over closed panels is worthless.
//
// Runs TWO ways off one file:
//   headless (the gate's assert leg):   $GODOT --headless --path <proj>
//                                        --script res://tests/ui/HudDialogueOverlapTest.cs
//   windowed (the capture leg, under graphical-test-helper): same script,
//   holds the opened state past the helper's --wait so the fixed layout is
//   captured as a frame; exits on its own verdict at t=20 s.
//
// PLANTED-BAD (this card's red pair): revert DialogueSystem/EmpathyPanel to
// the pre-fix zero-size anchor collapse and the named HUD_*_OVERLAP FAIL
// returns — the leg cannot pass vacuously (surfaces-armed guard above).
public partial class HudDialogueOverlapTest : SceneTree
{
    private const int FrameBudget = 1500;
    private const int OpenBudget = 600;        // per-surface open window (P1FixProof BookBudget idiom)
    private const int SettleFrames = 10;       // let the layout pass run after Show/Open

    private Node? _main;
    private WorldDirector? _director;
    private Hud? _hud;
    private DialogueSystem? _dialogue;
    private EmpathyPanel? _empathy;
    private int _stage = 1;                     // start at 1: compose is polled at _Process-top (P1FixProof idiom); 2 dialogue, 3 settle, 4 verdict
    private int _stageFrames;
    private double _t;
    private bool _asserted, _failed, _verdictPrinted;

    private static bool IsHeadless => DisplayServer.GetName() == "headless";

    public override void _Initialize()
    {
        GD.Print("LA_GATE: start — hud_dialogue_overlap (MC 11293 D1)");
        Engine.MaxFps = 60;
        var packed = GD.Load<PackedScene>("res://main.tscn");
        if (packed == null) { Fail("OVERLAP_PROOF_NOT_ARMED", "cannot load res://main.tscn"); return; }
        _main = packed.Instantiate<Node3D>();
        Root.AddChild(_main);
    }

    private bool Compose()
    {
        _director = _main as WorldDirector;
        // The BuildUi tree: UI (CanvasLayer) / HudLayer / Hud; UI / Dialogue; UI / Empathy.
        _hud = _main?.GetNodeOrNull<Hud>("UI/HudLayer/Hud");
        _dialogue = _main?.GetNodeOrNull<DialogueSystem>("UI/Dialogue");
        _empathy = _main?.GetNodeOrNull<EmpathyPanel>("UI/Empathy");
        if (_director == null || _hud == null || _dialogue == null || _empathy == null) return false;
        var vp = _hud.GetViewport()?.GetVisibleRect().Size ?? Vector2.Zero;
        GD.Print($"LA_GATE: composed — zone={_director.CurrentZone} viewport={vp}");
        return true;
    }

    public override bool _Process(double delta)
    {
        _t += delta;
        if (_failed) return true;
        if (_director == null && !Compose()) return true;

        _stageFrames++;
        if (_stageFrames > FrameBudget) { Fail("OVERLAP_PROOF_NOT_ARMED", "frame budget exhausted pre-verdict"); return true; }

        switch (_stage)
        {
            case 1:
                // Book input (B): 2 frames press / 2 release until the panel opens
                // on the boot companion's live M04 entry (P1FixProof empathy arm).
                if (_stageFrames % 4 < 2) Input.ActionPress("book");
                else Input.ActionRelease("book");
                if (_empathy!.Visible) { Input.ActionRelease("book"); _stage = 2; _stageFrames = 0; }
                else if (_stageFrames > OpenBudget) Fail("OVERLAP_PROOF_NOT_ARMED", "book input never opened EmpathyPanel");
                break;

            case 2:
                // Interact (E): the shipped DNA-speak trigger — nearest NPC (the
                // follower body, or a converging enemy) -> DialogueSystem.Show.
                if (_stageFrames % 4 < 2) Input.ActionPress("interact");
                else Input.ActionRelease("interact");
                if (_dialogue!.IsOpen) { Input.ActionRelease("interact"); _stage = 3; _stageFrames = 0; }
                else if (_stageFrames > OpenBudget) Fail("OVERLAP_PROOF_NOT_ARMED", "interact never opened the dialogue box");
                break;

            case 3:
                if (_stageFrames >= SettleFrames) { MeasureAndVerdict(); _stage = 4; _stageFrames = 0; }
                break;

            case 4:
                // Headless: verdict is the deliverable, quit now. Windowed: HOLD
                // the opened state so graphical-test-helper captures the fixed
                // layout frame, then quit on the verdict (helper kills at --wait
                // otherwise; SkillsPanelCaptureTest idiom).
                if (IsHeadless || _t >= 20.0)
                {
                    ReleaseHeldRefsBeforeQuit();
                    Quit(_asserted ? 0 : 1);
                    return true;
                }
                break;
        }
        return false;
    }

    private void MeasureAndVerdict()
    {
        // Both surfaces must be OPEN at measure time or the asserts below are
        // vacuous — this is the planted-green wall.
        if (!_empathy!.Visible || !_dialogue!.IsOpen)
        {
            Fail("OVERLAP_PROOF_NOT_ARMED",
                 $"surfaces not both open at measure time (empathy={_empathy.Visible} dialogue={_dialogue.ActiveNode})");
            return;
        }

        var hudRows = PaintedLabels(_hud!);
        if (hudRows.Count == 0) { Fail("OVERLAP_PROOF_NOT_ARMED", "Hud painted no gauge labels"); return; }
        var empathyLabels = PaintedLabels(_empathy!);
        if (empathyLabels.Count == 0) { Fail("OVERLAP_PROOF_NOT_ARMED", "opened EmpathyPanel painted no labels"); return; }

        int bad = 0;
        var dlg = PaintedRect(_dialogue!.GetNode<Label>("DialoguePanel/DialogueText"));
        GD.Print($"LA_GATE: rects: dialogue={dlg} hud_rows={hudRows.Count} empathy_labels={empathyLabels.Count}");
        foreach (var (name, r) in hudRows)
            if (dlg.Intersects(r)) { bad++; GD.Print($"LA_GATE: FAIL — HUD_DIALOGUE_OVERLAP dialogue={dlg} over {name}={r}");
        }
        if (bad == 0) GD.Print($"LA_GATE: check: HUD_DIALOGUE_DISJOINT: ok dialogue={dlg} clear of all {hudRows.Count} HUD rows");

        int ebad = 0;
        foreach (var (ename, er) in empathyLabels)
            foreach (var (hname, hr) in hudRows)
                if (er.Intersects(hr))
                {
                    ebad++;
                    GD.Print($"LA_GATE: FAIL — HUD_EMPATHY_OVERLAP {ename}={er} over {hname}={hr}");
                }
        if (ebad == 0) GD.Print("LA_GATE: check: HUD_EMPATHY_DISJOINT: ok all Empathy-Book labels clear of all HUD rows");

        _verdictPrinted = true;
        if (bad + ebad == 0)
        {
            GD.Print("LA_GATE: PASS — HUD/dialogue/empathy bounding boxes DISJOINT (MC 11293 D1)");
            _asserted = true;
        }
        else
        {
            _failed = true;
            GD.PrintErr($"LA_GATE: FAIL — overlap legs red ({bad} dialogue + {ebad} empathy, above)");
            if (IsHeadless) { ReleaseHeldRefsBeforeQuit(); Quit(1); }   // the assert leg carries the exit code
        }
    }

    /// <summary>Every VISIBLE Label in <paramref name="root"/>'s SUBTREE with
    /// its painted rect (subtree, not children: the panels' labels hang under
    /// their Panel root — missing that walk once went green on an empty
    /// surface list, the vacuous-green class the guard above exists to kill).
    /// Rect = global position widened to the text's combined min size — see
    /// the file header for why GetGlobalRect alone under-measures.</summary>
    private static List<(string, Rect2)> PaintedLabels(Control root)
    {
        var list = new List<(string, Rect2)>();
        CollectLabels(root, list);
        return list;
    }

    private static void CollectLabels(Control node, List<(string, Rect2)> into)
    {
        foreach (var child in node.GetChildren())
            if (child is Label l)
            {
                if (l.Visible && l.Text.Length > 0) into.Add((l.Name, PaintedRect(l)));
            }
            else if (child is Control c)
                CollectLabels(c, into);
    }

    private static Rect2 PaintedRect(Control c)
    {
        var ms = c.GetCombinedMinimumSize();
        var r = c.GetGlobalRect();
        r.Size = new Vector2(Mathf.Max(r.Size.X, ms.X), Mathf.Max(r.Size.Y, ms.Y));
        return r;
    }

    private void Fail(string name, string why)
    {
        Input.ActionRelease("book");
        Input.ActionRelease("interact");
        _failed = true;
        GD.PrintErr($"LA_GATE: FAIL — {name}: {why}");
        if (IsHeadless) Quit(1);   // headless leg carries the exit code; windowed holds for capture then quits
    }

    private void ReleaseHeldRefsBeforeQuit()
    {
        // MC 10117 idiom (BridgeMvpProof): release live-scene refs before the
        // MainLoop quits so finalizers cannot hit a torn-down ObjectDB.
        _director = null; _hud = null; _dialogue = null; _empathy = null; _main = null;
        System.GC.Collect();
        System.GC.WaitForPendingFinalizers();
        System.GC.Collect();
    }

    public override void _Finalize()
    {
        Input.ActionRelease("book");
        Input.ActionRelease("interact");
        if (!_asserted && !_failed)
            GD.PrintErr("LA_GATE: FAIL — finished without asserting the overlap legs");
    }
}
