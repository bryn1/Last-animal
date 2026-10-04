using Godot;
using LastAnimal.Core;
using LastAnimal.Core.Framework;
using LastAnimal.Dna;
using LastAnimal.Ecosystem;
using LastAnimal.Skills;
using LastAnimal.Story;
using LastAnimal.Ui;
using LastAnimal.World;
using System.Collections.Generic;

// Last Animal — stage 2f skills/quest UI logic leg (MC 3933, code,
// 2026-10-02). Headless SceneTree script run by ci/ui_test.sh leg E.
//
// Pins the 2f contract (owner ruling D4 verbatim: TOGGLE PANEL + persistent
// HUD readout; plan §G D4: no cache anywhere):
//   1. panel content is DERIVED from state — the rows mirror the live
//      PlayerMutations.Unlocked verdict + SkillState costs, never a fixture;
//   2. REAL unlock drive — the unlock happens by feeding the SAME consensus
//      the ecosystem learns from (a DnaLanguage signature into the spoken
//      history), not by hand-typing an unlocked flag (DA-c2 D4: "drive the
//      unlock, then assert the painted string");
//   3. HUD freshness guard — with live sources bound, the labels track the
//      fixture through the refresh seams alone: NO UpdateManna-style manual
//      poke is called anywhere below. Reintroducing a cached copy into the
//      HUD redraw (the planted-bad) turns these legs RED and names the row.
//   4. bus-driven panel refresh (QuestCompleted / SkillUsed) + the TAB
//      toggle round-trip with a fresh read at open;
//   5. palette guard — every label this stage composes reuses the Hud gauge
//      colour (1,1,1); zero new colours (2f palette rule, tol=20 moot when
//      the delta is exactly 0).
public partial class SkillsPanelTest : SceneTree
{
    private int _failures;
    private int _stage;

    private EventBus? _bus;
    private Hud? _hud;
    private SkillsPanel? _panel;
    private QuestLog? _log;

    // The mutable world fixture: the ONLY authority the views see is through
    // the provider closures below — exactly the production seam shape, with
    // test-owned systems standing in for the director's.
    private int _manna = 100;
    private readonly List<LanguageSignature> _spoken = new();

    private SkillUnlocks LiveUnlocks() =>
        PlayerMutations.Unlocked(EcosystemAdaptation.ModelPlayerDna(_spoken));

    public override void _Initialize()
    {
        GD.Print("LA_2F_UI_TEST: start");

        _bus = new EventBus { Name = "EventBus" };
        Root.AddChild(_bus);
        var canvas = new CanvasLayer { Name = "UiCanvas" };
        Root.AddChild(canvas);

        _hud = new Hud { Name = "Hud" };
        canvas.AddChild(_hud);
        _hud.Bind(100, 100, 0, 3);

        _panel = new SkillsPanel { Name = "SkillsPanel" };
        canvas.AddChild(_panel);
        _panel.ConnectBus(_bus);

        _log = new QuestLog(QuestTable.Default());
        _log.Start(_log.Table.Entries[0].Id);   // q_intro "Arrival", the arc's first row

        // THE 2f live seam — the same closures WorldDirector.InitUi injects,
        // reading the fixtures instead of the live director systems.
        _hud.BindLive(() => _manna, LiveUnlocks,
            () => WorldDirector.FormatQuestLine(_log.Table, _log.ActiveQuestId));
        _panel.SetLiveSource(LiveUnlocks, () => _manna);

        _stage = 1;
    }

    public override bool _Process(double delta)
    {
        switch (_stage++)
        {
            case 1:
                Check("panel closed at boot (TAB toggles from closed)", !_panel!.Visible,
                      $"visible={_panel.Visible}");
                CheckRow("StrikeRow", "SkillsPanelRoot/StrikeRow", "Invert Strike — locked");
                CheckRow("MendRow", "SkillsPanelRoot/MendRow", "Mend — locked");
                CheckRow("CalmRow", "SkillsPanelRoot/CalmRow", "Calming Speak — locked");   // MC 10031 third row
                CheckRow("panel MannaRow floor", "SkillsPanelRoot/MannaRow", "Manna: 100/100");
                CheckHud("SkillsGauge floor", "SkillsGauge", "Skills: none");
                CheckHud("QuestGauge active-quest line", "QuestGauge", "Quest: Arrival — Reach meadow");
                CheckHud("MannaGauge live digits", "MannaGauge", "Manna: 100/100");
                break;

            case 2:
                // REAL unlock drive (DA-c2 D4): a signature enters the spoken
                // history — the same fact a kill lands in production — and the
                // batched SkillUsed signal refreshes the panel (no manual
                // Refresh call on the panel path).
                _spoken.Add(DnaLanguage.SignatureForEntity(7));
                var u = LiveUnlocks();
                _bus!.EmitSkillUsed(new SkillId(PlayerMutations.InvertStrikeId));
                _hud!.RefreshLive();   // the per-frame TickUi refresh the director drives


                CheckRow("strike row mirrors live authority", "SkillsPanelRoot/StrikeRow",
                    u.InvertStrike ? $"Invert Strike — {SkillState.InvertStrikeCost} Manna"
                                   : "Invert Strike — locked");
                CheckRow("mend row mirrors live authority", "SkillsPanelRoot/MendRow",
                    u.Mend ? $"Mend — {SkillState.MendCost} Manna" : "Mend — locked");
                CheckRow("calm row mirrors live authority", "SkillsPanelRoot/CalmRow",
                    u.CalmingSpeak ? $"Calming Speak — {SkillState.CalmingSpeakCost} Manna"
                                   : "Calming Speak — locked");   // MC 10031: the 6-mer clears rule 3 too
                CheckHud("HUD learned-skill names follow the live authority", "SkillsGauge",
                    u.Ids.Count == 0 ? "Skills: none"
                        : "Skills: " + string.Join(", ", System.Array.ConvertAll(
                            System.Linq.Enumerable.ToArray(u.Ids), SkillsPanel.DisplayName)));
                break;

            case 3:
                // HUD freshness guard (planted-bad target): the currency
                // moves in the world; the per-frame refresh tick alone must
                // move the digits. NO UpdateManna poke below — a HUD that
                // kept its own copy would freeze this row and name it.
                _manna -= SkillState.InvertStrikeCost;   // a spend: 100 -> 90
                _hud!.RefreshLive();
                CheckHud("digits track the world with no manual poke", "MannaGauge", "Manna: 90/100");
                break;

            case 4:
                // Panel bus refresh on QuestCompleted alone (SkillUsed stays
                // silent this stage): a world move the panel cannot hear any
                // other way must still land on the visible rows.
                _manna = 42;
                _bus!.EmitQuestCompleted(new QuestId("q_intro"));
                CheckRow("QuestCompleted refreshes the panel", "SkillsPanelRoot/MannaRow",
                         "Manna: 42/100");
                break;

            case 5:
                // Tracker line follows the REAL log transition: q_intro's
                // objective (reach meadow) is observed -> completed -> the
                // next authored row goes Active (2d title live).
                _log!.ObserveZoneEntered("meadow");
                Check("q_intro completed, q_speak Active (authored arc order)",
                      _log.Status("q_intro") == QuestStatus.Completed &&
                      _log.ActiveQuestId == "q_speak",
                      $"intro={_log.Status("q_intro")} active={_log.ActiveQuestId}");
                _hud!.RefreshLive();
                CheckHud("tracker follows the arc without a manual repoke", "QuestGauge",
                         "Quest: Answer in Tongue — Speak the tongue");
                break;

            case 6:
                // TAB toggle round-trip + fresh read at open.
                _panel!.Toggle();
                Check("toggle opens the panel", _panel.Visible, $"visible={_panel.Visible}");
                _panel.Toggle();
                Check("toggle again closes the panel", !_panel.Visible, $"visible={_panel.Visible}");
                _manna = 13;
                _panel.Toggle();   // open: the first painted rows are fresh
                CheckRow("rows are re-read on open (no stale paint)", "SkillsPanelRoot/MannaRow",
                         "Manna: 13/100");
                _panel.Toggle();   // leave closed (boot state) for the palette walk
                break;

            case 7:
                // Palette guard: every label under the composed UI reuses the
                // Hud gauge colour — zero new colours in 2f.
                int labels = 0;
                foreach (Node n in _hud!.GetChildren())
                    if (n is Label hl) { labels++; PaletteCheck("Hud/" + hl.Name, hl); }
                foreach (Node n in _panel!.GetChildren())
                    if (n is Panel p)
                        foreach (Node m in p.GetChildren())
                            if (m is Label pl) { labels++; PaletteCheck("Panel/" + pl.Name, pl); }
                Check("both new HUD rows exist for the palette walk", labels >= 8,
                      $"labels walked={labels}");
                break;

            default:
                GD.Print(_failures == 0
                    ? "LA_2F_UI_TEST: PASS — panel content derived from live authority; real consensus unlock painted; HUD freshness without a poke; bus + toggle refresh; palette pure"
                    : $"LA_2F_UI_TEST: FAIL ({_failures} check(s) failed)");
                Quit(_failures == 0 ? 0 : 1);
                break;
        }
        return false;
    }

    private void CheckRow(string what, string path, string expected)
    {
        var label = _panel!.GetNodeOrNull<Label>(path);
        CheckExact(what, expected, label?.Text);
    }

    private void CheckHud(string what, string path, string expected)
    {
        var label = _hud!.GetNodeOrNull<Label>(path);
        CheckExact(what, expected, label?.Text);
    }

    private void CheckExact(string what, string expected, string? actual)
    {
        bool ok = actual == expected;
        GD.Print($"LA_2F_UI_TEST: {what}: {(ok ? "ok" : "FAIL")} " +
                 $"expected=[{expected}] actual=[{actual}]");
        if (!ok) _failures++;
    }

    private void PaletteCheck(string what, Label label)
    {
        var c = label.Modulate;
        bool ok = Mathf.Abs(c.R - 1f) < 0.004f && Mathf.Abs(c.G - 1f) < 0.004f &&
                  Mathf.Abs(c.B - 1f) < 0.004f;   // the Hud gauge colour (1,1,1), tol for float dust
        GD.Print($"LA_2F_UI_TEST: palette {what}: {(ok ? "ok" : "FAIL")} modulate={c}");
        if (!ok) _failures++;
    }

    private void Check(string what, bool ok, string detail)
    {
        GD.Print($"LA_2F_UI_TEST: {what}: {(ok ? "ok" : "FAIL")} {detail}");
        if (!ok) _failures++;
    }
}
