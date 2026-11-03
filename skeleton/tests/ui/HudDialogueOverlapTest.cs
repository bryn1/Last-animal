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
// Three-surface guard (the vacuous-green wall): the proof FAILS by name
// (OVERLAP_PROOF_NOT_ARMED) unless the dialogue, the book AND the TAB panel
// are all genuinely open at measure time — an assert over closed panels is
// worthless.
//
// MC 11303 F-DA1 extends the SAME leg with a TAB-OPEN stage (no parallel
// proof): the band's left edge is viewport-dependent (vp.X-712) while the TAB
// panel's right edge is fixed at SkillsPanel.RegionX+RegionWidth (428), and
// leg J never pressed TAB — the overlap class had no guard. The assert names
// TAB_DIALOGUE_OVERLAP / TAB_DIALOGUE_DISJOINT. LA_OVERLAP_FORCE_W (the
// card's planted narrow window) resizes the root viewport BEFORE the scene
// composes, so the band builds at a content width the shipped default never
// enters. MC 11891 (owner ruling B) adds DIALOGUE_IN_VIEW — band + text
// fully inside the live visible rect — and stands the constant-span
// PanelViewport fallback down FOR THE BAND, which is what made width matter
// at all: at the reviewer's captured 800 px window (the F-P1-1 clip, banked
// .audits/20261101-1215-taboverlap/) the OLD geometry reads RED on this
// assert by name, and the reflowed band at the same width reads GREEN
// (red/green pair banked .audits/20261103-0700-bandreflow/out/).
//
// Runs TWO ways off one file:
//   headless (retired as a gate leg by MC 11891: a --script run's visible
//   rect sits at the nominal 64x64 whatever the planted width — composed
//   viewport=(64, 64), MC 11303 F-P2-1 — so its geometry asserts a layout
//   that never paints):   $GODOT --headless --path <proj>
//                         --script res://tests/ui/HudDialogueOverlapTest.cs
//   windowed (THE gate leg, under graphical-test-helper): same script, holds
//   the opened state past the helper's --wait so the fixed layout is
//   captured as a frame — RED holds too (MC 11891: returning true on a
//   failed verdict quits via MainLoop semantics; the failing state must
//   survive to be captured); exits on its own verdict at t=20 s and the leg
//   captures the process's own exit code into the app log (GAME_RC=) — rc
//   from the process, never from a pipe tail.
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
    private SkillsPanel? _skills;
    private int _stage = 1;                     // start at 1: compose is polled at _Process-top (P1FixProof idiom); 2 dialogue, 3 TAB, 4 settle, 5 verdict
    private int _stageFrames;
    private double _t;
    private bool _asserted, _failed, _verdictPrinted;

    private static bool IsHeadless => DisplayServer.GetName() == "headless";

    public override void _Initialize()
    {
        GD.Print("LA_GATE: start — hud_dialogue_overlap (MC 11293 D1)");
        // MC 11303: the card's planted narrow window — resize the root BEFORE
        // composing so DialogueSystem builds its rect off the planted viewport
        // (unset = shipped default, gate legs never set it).
        if (int.TryParse(OS.GetEnvironment("LA_OVERLAP_FORCE_W"), out int fw) && fw > 0)
        {
            var planted = new Vector2I(fw, UiTheme.PlayVpHeight);
            // WindowSetSize FIRST, then Root.Size: a lone Root.Size is reverted
            // by the first DisplayServer sync (resize-probe measured, this card) —
            // with both, the visible rect stays at the planted width in
            // headless AND windowed, and the band builds off it.
            DisplayServer.WindowSetSize(planted);
            Root.Size = planted;
            GD.Print($"LA_GATE: planted-width — LA_OVERLAP_FORCE_W={fw} root={Root.Size} vis={Root.GetVisibleRect()}");
        }
        Engine.MaxFps = 60;
        var packed = GD.Load<PackedScene>("res://main.tscn");
        if (packed == null) { Fail("OVERLAP_PROOF_NOT_ARMED", "cannot load res://main.tscn"); return; }
        _main = packed.Instantiate<Node3D>();
        Root.AddChild(_main);
    }

    private bool Compose()
    {
        _director = _main as WorldDirector;
        // The BuildUi tree: UI (CanvasLayer) / HudLayer / Hud; UI / Dialogue; UI / Empathy;
        // UI / SkillsPanel (InitUi, the TAB panel — MC 11303).
        _hud = _main?.GetNodeOrNull<Hud>("UI/HudLayer/Hud");
        _dialogue = _main?.GetNodeOrNull<DialogueSystem>("UI/Dialogue");
        _empathy = _main?.GetNodeOrNull<EmpathyPanel>("UI/Empathy");
        _skills = _main?.GetNodeOrNull<SkillsPanel>("UI/SkillsPanel");
        if (_director == null || _hud == null || _dialogue == null || _empathy == null || _skills == null) return false;
        var vp = _hud.GetViewport()?.GetVisibleRect().Size ?? Vector2.Zero;
        GD.Print($"LA_GATE: composed — zone={_director.CurrentZone} viewport={vp}");
        return true;
    }

    public override bool _Process(double delta)
    {
        _t += delta;
        // (MainLoop contract: returning true QUITS the app — false continues.)
        if (_failed)
        {
            // MC 11891: a FAIL used to return true here — under MainLoop
            // semantics that QUIT the windowed run at the verdict, one tick
            // before any capture could see the failure (the MC 11303 red PNG
            // survived only because its helper captured before the verdict).
            // FAIL now HOLDS the opened state exactly like green, so the
            // capture leg gets the failing frame deterministically, and
            // quits with the verdict rc at t=20 for the rc run.
            if (IsHeadless || _t >= 20.0) { ReleaseHeldRefsBeforeQuit(); Quit(1); return true; }
            return false;
        }
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
                // TAB input (MC 11303 F-DA1): open the SkillsPanel so the dialogue
                // band and the TAB panel are BOTH open at measure time — the
                // overlap class leg J never guarded.
                if (_stageFrames % 4 < 2) Input.ActionPress("ui_toggle");
                else Input.ActionRelease("ui_toggle");
                if (_skills!.Visible) { Input.ActionRelease("ui_toggle"); _stage = 4; _stageFrames = 0; }
                else if (_stageFrames > OpenBudget) Fail("OVERLAP_PROOF_NOT_ARMED", "ui_toggle never opened the SkillsPanel");
                break;

            case 4:
                if (_stageFrames >= SettleFrames) { MeasureAndVerdict(); _stage = 5; _stageFrames = 0; }
                break;

            case 5:
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
        // All three surfaces must be OPEN at measure time or the asserts below
        // are vacuous — this is the planted-green wall.
        if (!_empathy!.Visible || !_dialogue!.IsOpen || !_skills!.Visible)
        {
            Fail("OVERLAP_PROOF_NOT_ARMED",
                 $"surfaces not all open at measure time (empathy={_empathy.Visible} dialogue={_dialogue.ActiveNode} tab={_skills.Visible})");
            return;
        }

        var hudRows = PaintedLabels(_hud!);
        if (hudRows.Count == 0) { Fail("OVERLAP_PROOF_NOT_ARMED", "Hud painted no gauge labels"); return; }
        var empathyLabels = PaintedLabels(_empathy!);
        if (empathyLabels.Count == 0) { Fail("OVERLAP_PROOF_NOT_ARMED", "opened EmpathyPanel painted no labels"); return; }

        int bad = 0;
        var dlg = PaintedRect(_dialogue!.GetNode<Label>("DialoguePanel/DialogueText"));
        var tabPanel = PaintedRect(_skills!);
        GD.Print($"LA_GATE: rects: dialogue={dlg} tab_panel={tabPanel} hud_rows={hudRows.Count} empathy_labels={empathyLabels.Count}");
        foreach (var (name, r) in hudRows)
            if (dlg.Intersects(r)) { bad++; GD.Print($"LA_GATE: FAIL — HUD_DIALOGUE_OVERLAP dialogue={dlg} over {name}={r}");
        }
        if (bad == 0) GD.Print($"LA_GATE: check: HUD_DIALOGUE_DISJOINT: ok dialogue={dlg} clear of all {hudRows.Count} HUD rows");

        // MC 11303 F-DA1: the dialogue band vs the TAB panel region — text must
        // never paint underneath the panel (the W-712 / 428 coupling).
        int tbad = 0;
        if (dlg.Intersects(tabPanel))
        {
            tbad++;
            GD.Print($"LA_GATE: FAIL — TAB_DIALOGUE_OVERLAP dialogue={dlg} under TAB panel={tabPanel}");
        }
        if (tbad == 0) GD.Print($"LA_GATE: check: TAB_DIALOGUE_DISJOINT: ok dialogue={dlg} clear of the TAB panel {tabPanel}");

        // MC 11891 (owner ruling B) — DIALOGUE_IN_VIEW: the band and its text
        // Label must lie ENTIRELY inside the live visible rect. This is the
        // assertion the old gate lacked: below 1152 the constant-span fallback
        // painted the band flush against a window edge that cut the tail off
        // every line, and leg J certified THAT screen PASS (MC 11303 F-P1-1 —
        // the control log + frame are banked there). A rect past any window
        // edge names DIALOGUE_IN_VIEW and turns the leg red at ANY width.
        var vis = Root.GetVisibleRect();
        var band = PaintedRect(_dialogue!);
        int vbad = 0;
        foreach (var (who, r) in new[] { ("band", band), ("label", dlg) })
            if (r.Position.X < vis.Position.X || r.End.X > vis.End.X
                || r.Position.Y < vis.Position.Y || r.End.Y > vis.End.Y)
            {
                vbad++;
                GD.PrintErr($"LA_GATE: FAIL — DIALOGUE_IN_VIEW {who}={r} outside window vis={vis} (window edge cuts the band)");
            }
        if (vbad == 0) GD.Print($"LA_GATE: check: DIALOGUE_IN_VIEW: ok band={band} label={dlg} inside window vis={vis}");

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
        if (bad + ebad + tbad + vbad == 0)
        {
            GD.Print("LA_GATE: PASS — HUD/dialogue/empathy/TAB bounding boxes DISJOINT and the dialogue band is DIALOGUE_IN_VIEW (MC 11293 D1 + MC 11303 F-DA1 + MC 11891 B)");
            _asserted = true;
        }
        else
        {
            _failed = true;
            GD.PrintErr($"LA_GATE: FAIL — overlap/in-view legs red ({bad} dialogue + {ebad} empathy + {tbad} TAB + {vbad} in-view, above)");
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
        Input.ActionRelease("ui_toggle");
        _failed = true;
        GD.PrintErr($"LA_GATE: FAIL — {name}: {why}");
        if (IsHeadless) Quit(1);   // headless leg carries the exit code; windowed holds for capture then quits
    }

    private void ReleaseHeldRefsBeforeQuit()
    {
        // MC 10117 idiom (BridgeMvpProof): release live-scene refs before the
        // MainLoop quits so finalizers cannot hit a torn-down ObjectDB.
        _director = null; _hud = null; _dialogue = null; _empathy = null; _skills = null; _main = null;
        System.GC.Collect();
        System.GC.WaitForPendingFinalizers();
        System.GC.Collect();
    }

    public override void _Finalize()
    {
        Input.ActionRelease("book");
        Input.ActionRelease("interact");
        Input.ActionRelease("ui_toggle");
        if (!_asserted && !_failed)
            GD.PrintErr("LA_GATE: FAIL — finished without asserting the overlap legs");
    }
}
