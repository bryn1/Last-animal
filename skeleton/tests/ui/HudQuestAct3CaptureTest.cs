using Godot;
using LastAnimal.Ui;
using LastAnimal.World;
using System;
using System.Collections.Generic;

// Last Animal — MC 11311: HUD quest-line ACT-THREE capture script. Headful
// SceneTree script run TWICE by ci/ui_test.sh leg L (HUD_QUESTLINE_ACT3)
// under graphical-test-helper:
//   LA_QUESTLINE_ACT3=0 — the baseline: the tracker paints the ACT-ONE
//     active row (the boot fact drives q_intro complete -> q_speak Active —
//     the same control run I asserts, so the boot face stays pinned).
//   LA_QUESTLINE_ACT3=1 — the pin: the act opens through the REAL save path
//     (the Story3 phase-1 idiom: diverge act one AND act two to all-complete
//     with the shipped FromSaveRows primitive, SaveGame/LoadGame — the act
//     opens via the shipped restore sync's FreshOpen arm, NOT a proof-side
//     API), and mid-arc the tracker must paint the ACT-THREE row's title
//     ("The Hollow").
// The in-code verdict is the semantic pin: on the act-three-open state the
// UNFIXED LiveQuestLine unions only act one + act two (the MC 11295 F1 gap)
// — both logs are arc-complete there, so the tracker falls through to
// "Quest: none" and the marker goes RED (the leg's RED-before-fix is the
// product's own before-face; the banked frame SHOWS the gap painted). The
// gate's pixel leg proves the row is painted and its pixels actually changed
// between the runs — same shape as leg I (HudQuestAct2CaptureTest is the
// exact idiom parent; no new mechanism).
//
// Boot idiom is HudQuestAct2CaptureTest (hand-composed Player + UI host,
// autoloads per the MC 1344.1 --script timing; no Camera3D so every pixel
// outside the 2D UI is one uniform backdrop colour). The restored zone is
// the boot meadow, so q_h_arrival (ZoneReached hollow) stays Active — the
// act-three-open state is exactly what a player carries until they reach
// the hollow. The save is user://savegame.json: the gate runs under SAVEGATE.
public partial class HudQuestAct3CaptureTest : SceneTree
{
    private WorldDirector? _director;
    private double _t;
    private bool _driven;
    private bool _verdictPrinted;

    private static bool ActThreeRequested =>
        OS.GetEnvironment("LA_QUESTLINE_ACT3") == "1";

    public override void _Initialize()
    {
        GD.Print($"LA_QUESTLINE_ACT3: start act3={(ActThreeRequested ? 1 : 0)}");
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

        // The act-open drive (press timing per leg I: boot+C# init costs
        // seconds, so drive early and verdict well before the --wait 6
        // capture). Act one's five rows + every act-two row are complete —
        // the load's act-two sync ADOPTS the complete log and the act-three
        // sync finds "act two complete, act three un-started" and OPENS the
        // act fresh (the shipped FreshOpen arm, Story3 phase 1 verbatim).
        if (!_driven && ActThreeRequested && _t >= 0.4 && _director != null)
        {
            _driven = true;
            _director.Quests.FromSaveRows(new List<string>
            {
                "q_intro:completed", "q_speak:completed", "q_wage:completed",
                "q_kills:completed", "q_boss:completed",
            });
            var act2Rows = new List<string>();
            foreach (var q in _director.QuestsAct2.Table.Entries)
                act2Rows.Add(q.Id + ":completed");
            _director.QuestsAct2.FromSaveRows(act2Rows);
            _director.SaveGame();
            _director.LoadGame();
            GD.Print($"LA_QUESTLINE_ACT3: driven — act3 rows [{string.Join(",", _director.QuestsAct3.ToSaveRows())}]");
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
                string act3Title = _director.QuestsAct3.Table.Entries[0].Title;
                string? act3Active = _director.QuestsAct3.ActiveQuestId;
                string? act2Active = _director.QuestsAct2.ActiveQuestId;
                string? act1Active = _director.Quests.ActiveQuestId;
                string act1Title = act1Active != null
                    ? _director.Quests.Table.FindTitle(act1Active) ?? string.Empty
                    : string.Empty;
                bool readsLive = line == _director.LiveQuestLine();   // HUD == director, no cache
                if (ActThreeRequested)
                {
                    // The act-three-open state: act three's row paints and no
                    // exhausted earlier row or fallthrough does (the F1 class:
                    // act3 rows NEVER paint — the tracker reads "Quest: none").
                    ok = act3Active != null && act2Active == null && readsLive &&
                         line != "Quest: none" && line.Contains(act3Title);
                    detail = $"line=[{line}] act3Active={act3Active ?? "-"} live={readsLive}";
                }
                else
                {
                    ok = act3Active == null && readsLive && act1Title.Length > 0 &&
                         line.Contains(act1Title) && !line.Contains(act3Title);
                    detail = $"line=[{line}] act1Active={act1Active ?? "-"} live={readsLive}";
                }
            }
            GD.Print(ok
                ? $"LA_QUESTLINE_ACT3: PASS — {detail}"
                : $"LA_QUESTLINE_ACT3: FAIL — {detail}");
        }

        // Stay alive past the capture; the helper kills this process.
        if (_t >= 20.0) Quit(_verdictPrinted ? 0 : 1);
        return false;
    }
}
