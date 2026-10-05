using Godot;
using LastAnimal.Ui;
using LastAnimal.World;
using System;
using System.Collections.Generic;

// Last Animal — MC 10138 / card 10026.15.9: HUD quest-line ACT-TWO capture
// script. Headful SceneTree script run TWICE by ci/ui_test.sh leg I
// (HUD_QUESTLINE_ACT2) under graphical-test-helper:
//   LA_QUESTLINE_ACT2=0 — the baseline: the tracker paints the ACT-ONE
//     active row (the boot fact drives q_intro complete -> q_speak Active).
//   LA_QUESTLINE_ACT2=1 — the pin: the act opens through the REAL save path
//     (the quest_arc2 idiom: diverge act one to all-complete with the shipped
//     FromSaveRows primitive, SaveGame/LoadGame — the act opens via the
//     shipped restore sync, NOT a proof-side API), and mid-arc the tracker
//     must paint the ACT-TWO row's title ("The Way Down").
// The in-code verdict is the semantic pin: with LiveQuestLine's union
// reverted the second run reads "Quest: none" (act one's arc is complete)
// and the marker goes RED (the gate's planted-bad). The gate's pixel leg
// proves the row is painted and that its pixels actually changed between
// the runs — same shape as the S6 HUD_VIGNETTE leg.
//
// Boot idiom is SkillsPanelCaptureTest (hand-composed Player + UI host,
// autoloads per the MC 1344.1 --script timing; no Camera3D so every pixel
// outside the 2D UI is one uniform backdrop colour). Save/load is safe here:
// the director's PlayerController is its OWN construct (WorldDirector.cs
// _Ready), independent of the plain Node3D "Player" node, and the restored
// zone is the boot meadow (no ruins cascade — q_r_descent's objective stays
// unmet). The save is user://savegame.json: the gate runs under SAVEGATE.
public partial class HudQuestAct2CaptureTest : SceneTree
{
    private WorldDirector? _director;
    private double _t;
    private bool _driven;
    private bool _verdictPrinted;

    private static bool ActTwoRequested =>
        OS.GetEnvironment("LA_QUESTLINE_ACT2") == "1";

    public override void _Initialize()
    {
        GD.Print($"LA_QUESTLINE: start act2={(ActTwoRequested ? 1 : 0)}");
        Engine.MaxFps = 60;   // frame pacing pinned like the proofs

        // One uniform backdrop (layer 0, the leg-F idiom) so the gate's
        // quest-row crop is pure backdrop wherever no glyph paints.
        var backdropLayer = new CanvasLayer { Name = "Backdrop", Layer = 0 };
        var backdrop = new ColorRect { Color = new Color(0.10f, 0.12f, 0.14f) };
        backdrop.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        backdropLayer.AddChild(backdrop);
        Root.AddChild(backdropLayer);

        _director = new WorldDirector { Name = "Director" };
        _director.AddChild(new Node3D { Name = "Player" });
        _director.AddChild(new CanvasLayer { Name = "UI" });
        Root.AddChild(_director);
    }

    public override bool _Process(double delta)
    {
        _t += delta;

        // The act-open drive (press timing per leg F: boot+C# init costs
        // seconds, so drive early and verdict well before the --wait 6
        // capture). Rows are act one's five shipped ids, all-complete — the
        // load's restore sync then finds act one finished with act two
        // unstarted and opens the act fresh (the shipped FreshOpen arm).
        if (!_driven && ActTwoRequested && _t >= 0.4 && _director != null)
        {
            _driven = true;
            _director.Quests.FromSaveRows(new List<string>
            {
                "q_intro:completed", "q_speak:completed", "q_wage:completed",
                "q_kills:completed", "q_boss:completed",
            });
            _director.SaveGame();
            _director.LoadGame();
            GD.Print($"LA_QUESTLINE: driven — act2 rows [{string.Join(",", _director.QuestsAct2.ToSaveRows())}]");
        }

        if (!_verdictPrinted && _t >= 0.8)
        {
            _verdictPrinted = true;
            var hud = _director?.GetNodeOrNull<Hud>("UI/HudLayer/Hud");
            var label = hud?.GetNodeOrNull<Label>("QuestGauge");
            bool ok = false;
            string detail = label == null ? "no QuestGauge label" : "no director";
            if (_director != null && label != null)
            {
                string line = label.Text;
                string act2Title = _director.QuestsAct2.Table.Entries[0].Title;
                string? act2Active = _director.QuestsAct2.ActiveQuestId;
                string? act1Active = _director.Quests.ActiveQuestId;
                string act1Title = act1Active != null
                    ? _director.Quests.Table.FindTitle(act1Active) ?? string.Empty
                    : string.Empty;
                bool readsLive = line == _director.LiveQuestLine();   // HUD == director, no cache
                if (ActTwoRequested)
                {
                    ok = act2Active != null && readsLive && line != "Quest: none" &&
                         line.Contains(act2Title);
                    detail = $"line=[{line}] act2Active={act2Active ?? "-"} live={readsLive}";
                }
                else
                {
                    ok = act2Active == null && readsLive && act1Title.Length > 0 &&
                         line.Contains(act1Title) && !line.Contains(act2Title);
                    detail = $"line=[{line}] act1Active={act1Active ?? "-"} live={readsLive}";
                }
            }
            GD.Print(ok
                ? $"LA_QUESTLINE: PASS — {detail}"
                : $"LA_QUESTLINE: FAIL — {detail}");
        }

        // Stay alive past the capture; the helper kills this process.
        if (_t >= 20.0) Quit(_verdictPrinted ? 0 : 1);
        return false;
    }
}
