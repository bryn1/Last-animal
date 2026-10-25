using Godot;
using LastAnimal.Story;
using LastAnimal.World;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

// Last Animal — MC 11295 (playthrough D3): HUD quest-line MONOTONICITY capture
// script — the banked-frame vehicle for the boss-kill ordering the diverge-based
// act proofs never drive. Headful SceneTree script run by ci/ui_test.sh leg J
// (HUD_QUEST_MONO) THREE times through graphical-test-helper:
//   LA_MONO_STAGE=0 — BEFORE: the act-one log is driven (the shipped
//     FromSaveRows primitive, the leg-I idiom) to q_boss ACTIVE; the tracker
//     paints the pre-kill beat "The Watcher Falls" (the playthrough frame 22).
//   LA_MONO_STAGE=1 — AFTER the boss-kill beat, AT THE RUINS: two REAL travels
//     on the zone wire reach the ruins, then the shipped Completed arm of
//     OnQuestChanged fires through the log's own public transitions
//     (MarkObjectiveMet + Complete — the SAME arm the live boss death drives;
//     the BossDead provider is a FACT FEED, the ordering under proof is what
//     the open edge does with it). The tracker must paint the ADVANCED act-two
//     row ("The Older Tongue"): on the UNFIXED product it paints the REGRESSED
//     "The Way Down — Reach ruins" (D3 verbatim) and this run's self-check
//     goes RED — the RED frame shows the bug painted.
//   LA_MONO_STAGE=2 — the same drive, then SaveGame() + LoadGame() at the
//     ruins: the tracker must STILL paint the advanced row (the D3 finding was
//     that the SAVE held the regression — the SAVED state, not HUD staleness);
//     the disk file's q_* rows print (LA_MONO: SAVEDROWS) — the leg banks them
//     beside the frame as the "saved state shown" row.
//
// Scene law: the REAL main.tscn (travels + zone mounting + the shipped
// restore-sync must run on the playable composition — the proof-side capture
// precedent), with SetSpawningEnabled(false) BEFORE AddChild (the shipped
// quiet-boot gate seam, Initialize.cs idiom: a standing player is whittled to
// death by the spawn set in ~4 s — bus_emit precedent; no live boss can field
// either, so the log's BossDead provider stays false and the ONLY boss-kill
// edge is the driven one). The semantic pin is the in-code marker (label text
// vs the director's LiveQuestLine, HUD == director, no cache — leg I law);
// the pixel bars (quest row painted; row differs BEFORE vs AFTER) corroborate.
// Stage 2 writes user://savegame.json: gate-level SAVEGATE flock (leg I law).
public partial class HudQuestMonoCaptureTest : SceneTree
{
    private WorldDirector? _director;
    private double _t;
    private int _pressStep;
    private bool _edgeDone;
    private bool _saved;
    private bool _loaded;
    private bool _verdictPrinted;

    private static int Stage
    {
        get { int.TryParse(OS.GetEnvironment("LA_MONO_STAGE"), out int s); return s; }
    }

    public override void _Initialize()
    {
        GD.Print($"LA_MONO: start stage={Stage}");
        Engine.MaxFps = 60;

        var packed = GD.Load<PackedScene>("res://main.tscn");
        if (packed == null) { GD.PrintErr("LA_MONO: FAIL — cannot load res://main.tscn"); Quit(1); return; }
        var main = packed.Instantiate<Node3D>();
        if (main is WorldDirector d) d.SetSpawningEnabled(false);   // quiet capture, shipped seam
        Root.AddChild(main);
        _director = main as WorldDirector;
    }

    public override bool _Process(double delta)
    {
        _t += delta;

        // t>=0.4 — drive the act-one account to the finale beat (stages 0..2;
        // FromSaveRows is the shipped restore primitive: it sets statuses
        // DIRECTLY and emits no Changed, so q_boss ACTIVE here is a snapshot,
        // not an open edge).
        if (_t >= 0.4 && Stage >= 0 && _director != null && !_director.Quests.IsArcComplete
            && _director.Quests.Status("q_boss") != QuestStatus.Completed && _pressStep == 0)
        {
            _director.Quests.FromSaveRows(new List<string>
            {
                "q_intro:completed", "q_speak:completed", "q_wage:completed",
                "q_kills:completed", "q_boss:active",
            });
            _pressStep = 1;   // stage 0 parks here (the BEFORE frame)
            if (Stage == 0) _pressStep = 99;
        }

        // t 0.8..1.7 — two REAL travels meadow -> canyon -> ruins (press/
        // release pairs; the shipped travel action on the input map).
        if (Stage >= 1 && !_edgeDone)
        {
            (double, bool)[] script = { (0.8, true), (1.1, false), (1.4, true), (1.7, false) };
            while (_pressStep < script.Length && _t >= script[_pressStep].Item1)
            {
                if (script[_pressStep].Item2) Input.ActionPress("travel");
                else Input.ActionRelease("travel");
                _pressStep++;
            }
        }

        // t>=2.6 — the boss-kill beat at the ruins: the shipped Completed arm
        // of OnQuestChanged (the act-two open edge) through the log's public
        // transitions. MarkObjectiveMet + Complete = exactly what the BossDead
        // provider's satisfaction does inside EvaluatePass (QuestLog
        // EvaluatePass); the provider is a feed, the ORDERING is what's proven.
        if (Stage >= 1 && !_edgeDone && _t >= 2.6 && _director != null)
        {
            if (_director.CurrentZone != "ruins")
            {
                GD.PrintErr($"LA_MONO: FAIL — boss-kill edge needs zone=ruins, zone={_director.CurrentZone}");
                _verdictPrinted = true;   // named fail already printed
                Quit(1); return false;
            }
            var q = _director.Quests;
            q.MarkObjectiveMet("q_boss");
            q.Complete("q_boss");
            _edgeDone = true;
            GD.Print($"LA_MONO: driven — boss-kill Completed edge at zone=ruins, act2=[{string.Join(",", _director.QuestsAct2.ToSaveRows())}]");
        }

        // t>=3.4/3.7 — stage 2 only: save, then load, AT THE RUINS.
        if (Stage == 2 && _edgeDone && !_saved && _t >= 3.4 && _director != null)
        {
            _director.SaveGame();
            _saved = true;
        }
        if (Stage == 2 && _saved && !_loaded && _t >= 3.7 && _director != null)
        {
            _director.LoadGame();
            _loaded = true;
            GD.Print($"LA_MONO: loaded — act2=[{string.Join(",", _director.QuestsAct2.ToSaveRows())}] zone={_director.CurrentZone}");
        }

        // t>=4.6 — the verdict (well before the helper's --wait 6 capture).
        if (!_verdictPrinted && _t >= 4.6 && _director != null)
        {
            _verdictPrinted = true;
            Node? gauge = FindDeep(_director, "QuestGauge");
            var label = gauge as Label;
            bool ok;
            string detail;
            string line = label?.Text ?? "<no QuestGauge label>";
            string act2Row2 = _director.QuestsAct2.Table.Entries[1].Title;   // "The Older Tongue"
            if (label == null)
            {
                ok = false; detail = "no QuestGauge label";
            }
            else
            {
                bool readsLive = line == _director.LiveQuestLine();
                bool noRegress = !line.Contains("The Way Down") && !line.Contains("Reach ruins");
                if (Stage == 0)
                {
                    ok = readsLive && line.Contains("The Watcher Falls") && line.Contains("End the watcher");
                    detail = $"line=[{line}] live={readsLive}";
                }
                else if (Stage == 1)
                {
                    ok = readsLive && noRegress && line.Contains(act2Row2);
                    detail = $"line=[{line}] live={readsLive} expected=[{act2Row2} advanced]";
                }
                else
                {
                    var diskRows = DiskQuestRows();
                    GD.Print($"LA_MONO: SAVEDROWS [{string.Join(",", diskRows)}]");
                    ok = readsLive && noRegress && line.Contains(act2Row2) &&
                         diskRows.Contains("q_r_descent:completed") && diskRows.Contains("q_r_tongue:active");
                    detail = $"line=[{line}] live={readsLive} disk=[{string.Join(",", diskRows)}]";
                }
            }
            GD.Print(ok ? $"LA_MONO: PASS — {detail}" : $"LA_MONO: FAIL — {detail}");
        }

        // Stay alive past the capture; the helper kills this process (leg I law).
        if (_t >= 20.0) Quit(_verdictPrinted ? 0 : 1);
        return false;
    }

    private static Node? FindDeep(Node root, string name)
    {
        foreach (Node c in root.GetChildren())
        {
            if (c.Name == name) return c;
            if (FindDeep(c, name) is Node hit) return hit;
        }
        return null;
    }

    private static List<string> DiskQuestRows()
    {
        var rows = new List<string>();
        try
        {
            string json = System.IO.File.ReadAllText(
                ProjectSettings.GlobalizePath("user://savegame.json"));
            foreach (Match m in Regex.Matches(json, "\"(q_[A-Za-z0-9_]+:[a-z_]+)\""))
                rows.Add(m.Groups[1].Value);
        }
        catch (Exception e) { GD.PrintErr($"LA_MONO: savefile read failed: {e.Message}"); }
        return rows;
    }
}
