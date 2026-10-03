using Godot;
using LastAnimal.Story;
using LastAnimal.World;
using System.Collections.Generic;

// Last Animal — stage 2c quest runtime proof (MC 3904, code, 2026-10-02).
//
// Partial-class half of RuntimeIntegrationProof: the quest-arc stages driven
// against the REAL playable scene (WorldDirector + InitStory + the pure
// QuestLog). Arc-proof contract (plan §B D5): the FULL 5-quest arc runs
// against the PLACEHOLDER QuestTable ids (q_intro..q_boss) — stage 2d swaps
// content rows under the same ids and edits NO proof file.
//
// Modes (LA_GATE_MODE):
//   quest_arc — the full arc headlessly: boot zone entry completes q_intro
//     and starts q_speak; interact near the companion speaks (q_speak); the
//     pay_wage arm settles a wage — WagePaid rides the Needing->Following
//     settle and meets q_wage's objective (QUEST_OBJECTIVE marker leg);
//     kills (also crossing the boss-spawn DNA threshold) meet q_kills; the
//     finale kills the live zone boss -> QUEST_COMPLETED (owner ruling D6).
//     MC 3915 reward-beat legs: each completion branch asserts the REAL
//     dialogue-view state — the row's Reward node on screen flagged
//     reward-shown (q_speak's leg pins the honest end state: the interact
//     path's own npc-node show replaces the first_speak beat in the SAME
//     call, clearing the flag — REWARD_SHOWN q_speak on the log is that
//     beat's marker). Case 6 is the named REWARD_GUARD leg: while the
//     boss_fallen reward is on screen, a fresh DialogueShown probe on the
//     director's OWN guarded seam (QuestDialogueNodeNow) must stay Active;
//     the same node shown as a NORMAL dialogue satisfies it (non-vacuous).
//   quest_persist — after two completed quests: SaveGame(), diverge the LIVE
//     log (FromSaveRows corruption seam), LoadGame() must restore the saved
//     rows onto the live scene -> QUEST_PERSIST. THEN the rewind leg (DA P1):
//     keep playing past the loaded point (settle a wage, save with q_kills
//     ACTIVE at 0/4, farm four live kills), LOAD the rewind save, and drive
//     exactly ONE post-load extraction — zero QUEST_COMPLETED emits and
//     q_kills still Active -> QUEST_REWIND (evidence rewinds with the save;
//     stale counters must never cascade the arc).
//   quest_neg — the SetQuestHooksEnabled(false) gate seam is set on the
//     instantiated root BEFORE AddChild (no_spawn idiom): the arc must not
//     advance; detection prints NEG_QUEST and exits non-zero.
public partial class RuntimeIntegrationProof : SceneTree
{
    private const int QuestFrameBudget = 12000;   // quest_arc/quest_persist (wage wait + farm + boss)

    private int _qPhase;
    private int _qFrames;
    private int _qInteractToggle;
    private int _qPayToggle;
    private int _qAttackToggle;
    private int _qTravelToggle;
    private bool _qSubscribed;
    private List<string> _qSavedRows = new();
    // Post-load cascade counters (the DA-P1 gate hole: the old persist leg
    // checked statuses and stopped — NO observation after LoadGame).
    private bool _qCounting;
    private int _qCompletedAfterLoad;
    private int _qExtractsAfterLoad;

    /// <summary>Stage-50 dispatch (called from the main file's stage switch).</summary>
    private void RunQuestStage()
    {
        switch (_mode)
        {
            case "quest_arc": QuestArcStage(); break;
            case "quest_persist": QuestPersistStage(); break;
            case "quest_neg": QuestNegStage(); break;
        }
    }

    /// <summary>Marker taps on the REAL bus (subscribe once, first quest frame):
    /// every signal the arc emits mid-drive prints its marker line.</summary>
    private bool QSubscribe()
    {
        if (_qSubscribed) return true;
        if (_bus == null || _director == null) return false;
        _bus.QuestStarted += id => GD.Print($"LA_GATE: QUEST_STARTED {id}");
        _bus.QuestObjective += id => GD.Print($"LA_GATE: QUEST_OBJECTIVE {id}");
        _bus.QuestCompleted += id =>
        {
            GD.Print($"LA_GATE: QUEST_COMPLETED {id}");
            if (_qCounting) _qCompletedAfterLoad++;
        };
        _bus.WagePaid += id => GD.Print($"LA_GATE: WAGE_PAID for {id}");
        _bus.DnaExtracted += _ => { if (_qCounting) _qExtractsAfterLoad++; };
        _qSubscribed = true;
        return true;
    }

    private QuestStatus QStatus(string id) => _director!.Quests.Status(id);

    /// <summary>Travel press/release cycle (ZoneBossProof re-arm idiom).</summary>
    private bool QPressTravel()
    {
        _qTravelToggle++;
        if (_qTravelToggle == 1) Input.ActionPress("travel");
        if (_qTravelToggle >= 6)
        {
            Input.ActionRelease("travel");
            _qTravelToggle = 0;
            return true;
        }
        return false;
    }

    /// <summary>Farm the nearest live enemy; the boss is excluded until the
    /// finale (killing it early would consume the quest this mode proves).
    /// False = the zone pool is dry (travel on, ZoneBossProof idiom).</summary>
    private bool QKillLoop()
    {
        EnemyActor? target = null;
        float best = float.MaxValue;
        foreach (var e in _director!.Enemies)
        {
            if (e.IsDead || !GodotObject.IsInstanceValid(e)) continue;
            if (ReferenceEquals(e, _director.BossActor)) continue;
            float d = e.GlobalPosition.DistanceTo(_playerBody!.GlobalPosition);
            if (d < best) { best = d; target = e; }
        }
        if (target == null) return false;
        if (best > _director.PlayerModel.AttackRange)
        {
            Vector3 p = target.GlobalPosition;
            _playerBody!.GlobalPosition = new Vector3(p.X - 0.8f, p.Y, p.Z);
        }
        _qAttackToggle++;
        if (_qAttackToggle % 4 == 1) Input.ActionPress("attack");
        else if (_qAttackToggle % 4 == 3) Input.ActionRelease("attack");
        return true;
    }

    /// <summary>Teleport next to the live boss and attack it (finale only).</summary>
    private void QBossAttackLoop()
    {
        var boss = _director!.BossActor;
        if (boss == null || !GodotObject.IsInstanceValid(boss)) return;
        Vector3 p = boss.GlobalPosition;
        _playerBody!.GlobalPosition = new Vector3(p.X - 0.8f, p.Y, p.Z);
        _qAttackToggle++;
        if (_qAttackToggle % 4 == 1) Input.ActionPress("attack");
        else if (_qAttackToggle % 4 == 3) Input.ActionRelease("attack");
    }

    private void QPress(string action, ref int toggle)
    {
        toggle++;
        if (toggle % 4 == 1) Input.ActionPress(action);
        else if (toggle % 4 == 3) Input.ActionRelease(action);
    }

    // ---- quest_arc: the full 5-quest placeholder arc to the zone-boss finale
    private void QuestArcStage()
    {
        if (!QSubscribe()) return;
        _qFrames++;

        switch (_qPhase)
        {
            case 0:   // boot: the meadow zone entry completed q_intro, auto-started q_speak
                if (QStatus("q_intro") == QuestStatus.Completed && QStatus("q_speak") == QuestStatus.Active)
                {
                    Check("boot zone entry completed q_intro and auto-started q_speak",
                          true, "intro=completed speak=active");
                    Check("MC 3915: q_intro reward beat on screen",
                          _director!.DialogueUi.ActiveNode == "intro" && _director!.DialogueUi.ActiveNodeIsReward,
                          $"node={_director.DialogueUi.ActiveNode}");
                    if (_failed) return;
                    GD.Print("LA_GATE: QUEST_ACTIVE q_speak — boot zone fact drove the intro quest");
                    NextPhase();
                }
                else if (_qFrames > 240)
                    Fail($"quest_arc: boot intro stalled (intro={QStatus("q_intro")}, speak={QStatus("q_speak")})");
                break;

            case 1:   // speak: teleport to the companion, press interact
                if (QStatus("q_speak") == QuestStatus.Completed)
                {
                    // MC 3915 view leg, honest reading: the first_speak reward
                    // beat rides the Completed transition INSIDE EmitDnaSpoken,
                    // and the interact path's own dialogue show (WorldDirector
                    // .cs TryInteract, untouched by MC 3915) replaces it in the
                    // SAME call — the real end-of-call view state is the npc
                    // node with the reward flag cleared (a one-arg Show resets
                    // it). REWARD_SHOWN q_speak on the log is the beat itself.
                    Check("MC 3915: q_speak completion rode the reward beat; the interact's own show owns the view now (npc node, reward flag cleared)",
                          _director!.DialogueUi.ActiveNode.StartsWith("npc_") && !_director!.DialogueUi.ActiveNodeIsReward,
                          $"node={_director.DialogueUi.ActiveNode}");
                    if (_failed) return;
                    GD.Print("LA_GATE: QUEST_SPOKE — interact drove q_speak through the bus");
                    NextPhase();
                    break;
                }
                if (_qFrames > 1200) Fail("quest_arc: q_speak never completed (interact -> DnaSpoken wire broken)");
                _playerBody!.GlobalPosition = _companion!.GlobalPosition;
                QPress("interact", ref _qInteractToggle);
                break;

            case 2:   // wage: wait for the due clock (grace 20s), then press pay_wage
                if (QStatus("q_wage") == QuestStatus.Completed)
                {
                    Check("MC 3915: q_wage reward beat on screen",
                          _director!.DialogueUi.ActiveNode == "wage_duty" && _director!.DialogueUi.ActiveNodeIsReward,
                          $"node={_director.DialogueUi.ActiveNode}");
                    if (_failed) return;
                    GD.Print("LA_GATE: QUEST_WAGE — WagePaid rode the pay settle and met q_wage");
                    NextPhase();
                    break;
                }
                if (_qFrames > QuestFrameBudget) Fail("quest_arc: q_wage never completed (pay/settle wire broken)");
                if (_director!.WageDueNow) QPress("pay_wage", ref _qPayToggle);
                break;

            case 3:   // kills: farm non-boss enemies (travelling when dry) to 4 extractions
                if (QStatus("q_kills") == QuestStatus.Completed)
                {
                    Check("MC 3915: q_kills reward beat on screen",
                          _director!.DialogueUi.ActiveNode == "counters" && _director!.DialogueUi.ActiveNodeIsReward,
                          $"node={_director.DialogueUi.ActiveNode}");
                    if (_failed) return;
                    GD.Print("LA_GATE: QUEST_KILLS — four extractions met the counters quest");
                    NextPhase();
                    break;
                }
                if (_qFrames > QuestFrameBudget) Fail("quest_arc: q_kills never completed (kill -> DnaExtracted wire broken)");
                if (!QKillLoop()) QPressTravel();
                break;

            case 4:   // finale: travel until a zone fields the boss (DNA threshold met)
                if (_director!.HasLiveBoss)
                {
                    GD.Print($"LA_GATE: BOSS_LIVE zone={_director.CurrentZone} — q_boss finale engaged");
                    NextPhase();
                }
                else if (_qFrames > QuestFrameBudget) Fail("quest_arc: no live boss after the kill quest (threshold/zone wire broken)");
                else QPressTravel();
                break;

            case 5:   // kill the boss: the arc ends at QUEST_COMPLETED (owner ruling D6),
                      // then the MC 3915 REWARD_GUARD leg (case 6) runs on the live view.
                if (QStatus("q_boss") == QuestStatus.Completed && _director!.Quests.IsArcComplete)
                {
                    Check("the zone-boss finale completed the arc", _director.Quests.IsArcComplete,
                          "all five rows Completed");
                    if (_failed) return;
                    Check("MC 3915: q_boss reward beat on screen",
                          _director!.DialogueUi.ActiveNode == "boss_fallen" && _director!.DialogueUi.ActiveNodeIsReward,
                          $"node={_director.DialogueUi.ActiveNode}");
                    if (_failed) return;
                    GD.Print("LA_GATE: QUEST_COMPLETED q_boss — FULL 5-QUEST ARC COMPLETE at the zone boss");
                    GD.Print("LA_GATE: PASS — quest arc verified (intro->speak->wage->kills->boss through ONE composition root)");
                    NextPhase();
                    break;
                }
                if (_qFrames > QuestFrameBudget) Fail("quest_arc: the boss never died (finale wire broken)");
                QBossAttackLoop();
                break;

            case 6:   // MC 3915 REWARD_GUARD: the boss_fallen reward is STILL on
                      // screen. A fresh DialogueShown probe wired through the
                      // director's OWN guarded seam must not credit the reward;
                      // the same node shown as a normal dialogue must (non-vacuous).
                var probe = new QuestLog(new QuestTable(new[] { new QuestDef("g_shown", "Reward-Guard Probe",
                    new QuestObjective(QuestObjectiveKind.DialogueShown, 1, "boss_fallen"), "boss_fallen") }));
                probe.SetDialogueNodeProvider(() => _director!.QuestDialogueNodeNow);  // the seam the director itself wires
                probe.Start("g_shown");   // EvaluatePass polls the provider WHILE the boss_fallen reward is on screen
                bool rewardGuarded = probe.Status("g_shown") == QuestStatus.Active;      // reward fed nothing (guard)
                _director!.DialogueUi.Show("boss_fallen");                               // a NORMAL dialogue, same node
                probe.ObserveSpoken(0);                                                  // re-evaluation nudge, same seam
                bool shownSatisfies = probe.Status("g_shown") == QuestStatus.Completed;  // channel is live (non-vacuous)
                Check("REWARD_GUARD: reward-shown node never satisfies DialogueShown; a normal show does",
                      rewardGuarded && shownSatisfies,
                      $"rewardOnScreenGuarded={rewardGuarded} normalShowCompletes={shownSatisfies}");
                if (_failed) return;
                GD.Print("LA_GATE: REWARD_GUARD — boss_fallen as reward satisfied nothing; as dialogue it satisfies");
                GD.Print("LA_GATE: PASS — quest arc verified (full arc + reward beats + DialogueShown guard, MC 3915)");
                _asserted = true; _stage = 6; _stageFrames = 0; _holdStartPhys = _physFrames;
                break;
        }
    }

    // ---- quest_persist: live-scene quest progress survives save -> load ----
    private void QuestPersistStage()
    {
        if (!QSubscribe()) return;
        _qFrames++;

        switch (_qPhase)
        {
            case 0:   // same boot fact as quest_arc (drives the persisted leg)
                if (QStatus("q_intro") == QuestStatus.Completed && QStatus("q_speak") == QuestStatus.Active)
                {
                    GD.Print("LA_GATE: QUEST_ACTIVE q_speak — persist leg: boot intro observed");
                    NextPhase();
                }
                else if (_qFrames > 240) Fail("quest_persist: boot intro stalled");
                break;

            case 1:   // complete q_speak so the snapshot carries two completed rows + one active
                if (QStatus("q_speak") == QuestStatus.Completed)
                {
                    _qSavedRows = _director!.Quests.ToSaveRows();
                    Check("live log rows carry intro+speak completed, wage active",
                          _qSavedRows.Contains("q_intro:completed") &&
                          _qSavedRows.Contains("q_speak:completed") &&
                          _qSavedRows.Contains("q_wage:active"),
                          string.Join(",", _qSavedRows));
                    if (_failed) return;
                    _director.SaveGame();
                    GD.Print($"LA_GATE: QUEST_SAVE_WRITTEN [{string.Join(",", _qSavedRows)}]");
                    // Diverge the LIVE log through the same restore primitive
                    // (a dropped row must come back from disk, not be a no-op).
                    _director.Quests.FromSaveRows(new List<string> { "q_intro:completed" });
                    Check("live log diverged from the save before Load",
                          QStatus("q_speak") == QuestStatus.NotStarted,
                          $"q_speak={QStatus("q_speak")}");
                    if (_failed) return;
                    NextPhase();
                }
                else if (_qFrames > 1200) Fail("quest_persist: q_speak never completed");
                else
                {
                    _playerBody!.GlobalPosition = _companion!.GlobalPosition;
                    QPress("interact", ref _qInteractToggle);
                }
                break;

            case 2:   // F9: the live scene's quest states come back from disk
                _director!.LoadGame();
                Check("LOAD_RESTORED_QUESTS: saved rows reapplied to the live log",
                      QStatus("q_intro") == QuestStatus.Completed &&
                      QStatus("q_speak") == QuestStatus.Completed &&
                      QStatus("q_wage") == QuestStatus.Active &&
                      _director.Quests.ToSaveRows().Count == _qSavedRows.Count,
                      $"[{string.Join(",", _director.Quests.ToSaveRows())}]");
                if (_failed) return;
                GD.Print("LA_GATE: QUEST_PERSIST — live-scene quest progress survived save -> load (v3 QuestStates)");
                NextPhase();
                break;

            case 3:   // rewind leg (DA P1): continue LIVE past the loaded point —
                      // settle a wage, so q_kills becomes the save's Active row
                if (QStatus("q_wage") == QuestStatus.Completed)
                {
                    GD.Print("LA_GATE: QUEST_REWIND_LIVE — wage settled live; q_kills now Active pre-save");
                    NextPhase();
                    break;
                }
                if (_qFrames > QuestFrameBudget) Fail("quest_persist: rewind leg never settled a wage");
                if (_director!.WageDueNow) QPress("pay_wage", ref _qPayToggle);
                break;

            case 4:   // the rewind save: rows now carry q_kills ACTIVE at 0/4
                _qSavedRows = _director!.Quests.ToSaveRows();
                Check("rewind save taken with q_kills Active (0/4)",
                      _qSavedRows.Contains("q_wage:completed") &&
                      _qSavedRows.Contains("q_kills:active"),
                      string.Join(",", _qSavedRows));
                if (_failed) return;
                _director.SaveGame();
                GD.Print($"LA_GATE: QUEST_REWIND_SAVED [{string.Join(",", _qSavedRows)}]");
                NextPhase();
                break;

            case 5:   // live drift: farm 4 extractions — q_kills completes live
                if (QStatus("q_kills") == QuestStatus.Completed)
                {
                    GD.Print("LA_GATE: QUEST_REWIND_DRIFT — four live extractions completed q_kills");
                    NextPhase();
                    break;
                }
                if (_qFrames > QuestFrameBudget) Fail("quest_persist: rewind leg never farmed the kills");
                if (!QKillLoop()) QPressTravel();
                break;

            case 6:   // LOAD the rewind save: q_kills goes back to Active 0/4.
                      // THE gate hole: arm the marker counters and keep playing.
                _director!.LoadGame();
                Check("rewind save reapplied: q_kills Active again, q_boss NotStarted",
                      QStatus("q_kills") == QuestStatus.Active &&
                      QStatus("q_boss") == QuestStatus.NotStarted &&
                      _director.Quests.ToSaveRows().Count == _qSavedRows.Count,
                      $"[{string.Join(",", _director.Quests.ToSaveRows())}]");
                if (_failed) return;
                _qCounting = true;
                GD.Print("LA_GATE: QUEST_REWIND_ARMED — ONE post-load observation follows; zero completes allowed");
                NextPhase();
                break;

            case 7:   // ONE observation after the load: a single extraction.
                      // Stale evidence (DA P1 pre-fix) would read 4/4 here and
                      // cascade QUEST_COMPLETED q_kills + start q_boss in one
                      // pass — the counters below catch it.
                if (_qExtractsAfterLoad < 1)
                {
                    if (_qFrames > 2400) Fail("quest_persist: rewind leg observed no kill after the load (inconclusive)");
                    if (!QKillLoop()) QPressTravel();
                    break;
                }
                Check("post-load evidence rewound: ONE extraction met nothing, cascaded nothing",
                      _qCompletedAfterLoad == 0 &&
                      QStatus("q_kills") == QuestStatus.Active &&
                      QStatus("q_boss") == QuestStatus.NotStarted,
                      $"completesAfterLoad={_qCompletedAfterLoad} kills={QStatus("q_kills")} boss={QStatus("q_boss")}");
                if (_failed) return;
                GD.Print("LA_GATE: QUEST_REWIND — post-load observation re-earned 1/4; zero cascading completes (DA P1 closed)");
                GD.Print("LA_GATE: PASS — quest persistence + evidence rewind verified");
                _asserted = true;
                _stage = 6;
                _stageFrames = 0;
                _holdStartPhys = _physFrames;
                break;
        }
    }

    // ---- quest_neg: the SetQuestHooksEnabled(false) gate seam must stall the arc
    private void QuestNegStage()
    {
        if (!QSubscribe()) return;
        _qFrames++;
        if (_qFrames < 180) return;   // frames the arc WOULD need at boot

        var intro = QStatus("q_intro");
        if (intro == QuestStatus.Completed)
        {
            Fail("quest_neg: the arc advanced despite the story hooks disabled — the negative control is broken");
            return;
        }
        GD.Print($"LA_GATE: NEG_QUEST: story hooks disabled — arc stalled at q_intro={intro} — break detected");
        Quit(1);
    }

    private void NextPhase()
    {
        _qPhase++;
        _qFrames = 0;
    }
}
