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
    private int _pressStep;          // travel script step (0..4), 99 = stage-0 parked
    private bool _rowsDone;
    private double _rowsT;           // in-process time the rows landed
    private double _lastPressT;      // last travel press/release (bake anchor)
    private bool _edgeDone;
    private double _edgeT;
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

        // Zone-GATED phases (a zone MOUNT bakes synchronously and blocks the
        // process loop — the clock here is in-process time, so each wait is
        // counted AFTER the bake that blocks it; a wall-clock schedule would
        // starve before the edge, the MC 11295 first-draft finding).
        // P0 — boot mounted (first zone up) + 0.3 s: drive the act-one account
        // to the finale beat (FromSaveRows is the shipped restore primitive: it
        // sets statuses DIRECTLY and emits no Changed, so q_boss ACTIVE here is
        // a snapshot, not an open edge).
        if (!_rowsDone && _director != null && _director.CurrentZone != "" && _t >= 0.3)
        {
            _director.Quests.FromSaveRows(new List<string>
            {
                "q_intro:completed", "q_speak:completed", "q_wage:completed",
                "q_kills:completed", "q_boss:active",
            });
            _rowsDone = true;
            _rowsT = _t;
            _pressStep = (Stage == 0) ? 99 : 1;   // stage 0 parks here (BEFORE frame)
            GD.Print($"LA_MONO: rows — act-one account parked at q_boss ACTIVE (zone={_director.CurrentZone})");
        }

        // P1 (stage >= 1) — two REAL travels meadow -> canyon -> ruins (press/
        // release pairs on the shipped travel action; the mounts serialize the
        // presses behind their bakes by themselves).
        if (Stage >= 1 && _rowsDone && _pressStep >= 1 && _pressStep <= 4)
        {
            (double, bool)[] script = { (0.4, true), (0.7, false), (1.2, true), (1.5, false) };
            if (_t >= _rowsT + script[_pressStep - 1].Item1)
            {
                // MC 11294 keep-alive (the Quests.cs QBossAttackLoop seam, quoted):
                // the honest-death gate refuses TRAVEL of a corpse, and the long
                // synchronous zone bakes leave the standing capture player
                // exposed to the scene SpawnSet — revive ONLY when already dead;
                // a live farmer is never touched (agency law intact).
                if (_director!.PlayerModel.IsDead) _director.PlayerModel.RestoreHealth(50);
                if (script[_pressStep - 1].Item2) Input.ActionPress("travel");
                else Input.ActionRelease("travel");
                _lastPressT = _t;
                _pressStep++;
            }
        }

        // P2 (stage >= 1) — the boss-kill beat AT THE RUINS: zone-gated (waits
        // the ruins mount out). MarkObjectiveMet + Complete fires the shipped
        // Completed arm of OnQuestChanged — the SAME arm the live BossDead
        // provider's satisfaction drives inside EvaluatePass; the provider is a
        // FACT FEED, the ordering under proof is what the open edge does with it.
        if (Stage >= 1 && _pressStep > 4 && !_edgeDone && _director != null
            && _director.CurrentZone == "ruins" && _t >= _lastPressT + 0.8)
        {
            var q = _director.Quests;
            q.MarkObjectiveMet("q_boss");
            q.Complete("q_boss");
            _edgeDone = true;
            _edgeT = _t;
            GD.Print($"LA_MONO: driven — boss-kill Completed edge at zone=ruins, act2=[{string.Join(",", _director.QuestsAct2.ToSaveRows())}]");
        }
        if (Stage >= 1 && _pressStep > 4 && !_edgeDone && _director != null
            && _director.CurrentZone != "ruins" && _t >= _lastPressT + 15.0)
        {
            GD.PrintErr($"LA_MONO: FAIL — travel never reached the ruins (zone={_director.CurrentZone})");
            _verdictPrinted = true;
            Quit(1); return false;
        }

        // P3 — stage 2 only: save, then load, AT THE RUINS (after the edge).
        if (Stage == 2 && _edgeDone && !_saved && _t >= _edgeT + 0.5 && _director != null)
        {
            _director.SaveGame();
            _saved = true;
        }
        if (Stage == 2 && _saved && !_loaded && _t >= _edgeT + 0.9 && _director != null)
        {
            _director.LoadGame();
            _loaded = true;
            GD.Print($"LA_MONO: loaded — act2=[{string.Join(",", _director.QuestsAct2.ToSaveRows())}] zone={_director.CurrentZone}");
        }

        // P4 — the verdict: stage 0 reads the rows+1.5 s; stage 1 reads
        // edge+1.6 s; stage 2 reads loaded+1.2 s (well before --wait 12).
        double verdictAt = Stage == 0 ? _rowsT + 1.5 : (Stage == 1 ? _edgeT + 1.6 : _edgeT + 2.1);
        bool ready = Stage == 0 ? _rowsDone : (Stage == 1 ? _edgeDone : _loaded);
        if (!_verdictPrinted && ready && _t >= verdictAt && _director != null)
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

        // Stay alive past the capture; the helper kills this process (leg I
        // law). Zone bakes (heightfield + navmesh, ~5 s each, synchronous in
        // the director's own travel handler) run the wall clock past _t: the
        // full two-travel settle lands ~35 s, the verdict ~37 s, so the leg
        // banks --wait 45 and this self-quit stays past it.
        if (_t >= 55.0) Quit(_verdictPrinted ? 0 : 1);
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
