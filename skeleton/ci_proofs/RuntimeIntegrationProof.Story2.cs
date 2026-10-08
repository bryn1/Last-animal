// SIZE: proof partial (288 l measured, restamped MC 10255 — was ~300), test-class ceiling 600 (MC 10132 S10).
using Godot;
using LastAnimal.Story;
using LastAnimal.Ui;
using LastAnimal.World;
using System.Collections.Generic;

// Last Animal — stage S10 ACT-TWO runtime proof (MC 10132, code, 2026-10-05).
//
// Partial-class half of RuntimeIntegrationProof: the act-two chain (the +4
// QuestTable.RuinsArc() rows, RULING-5) driven on the REAL playable scene —
// the same WorldDirector + InitStory + pure QuestLog machine that plays act
// one, now playing the SECOND log instance through its whole lifecycle:
// act open (act-one rows complete -> restore-driven open edge + the table-
// driven open card on the live view), the four in-act completions, the save
// round-trip of the act-two rows on the SHIPPED QuestStates wire (zero save-
// file schema delta), and the close card on the finale.
//
// Mode (LA_GATE_MODE):
//   quest_arc2 — stage 55. Phase plan:
//     0 boot: q_intro completed by the boot zone fact, q_speak Active (the
//       shipped act-one seam proven live — ACT2_BOOT).
//     1 interact near the companion completes q_speak on the real wire
//       (ACT2_SPOKEN — the act-one edge the open arm watches is real).
//     2 open edge through the REAL save path: the act-one log is diverged to
//       all-complete via the same FromSaveRows primitive quest_persist uses,
//       SaveGame() stamps that snapshot (the disk is OWNED by this write —
//       death_load idiom), LoadGame() runs the shipped restore seam, whose
//       sync finds "act one complete, act two un-started" and OPENS the act:
//       ACT2_OPENED (row 1 Active, one row on the wire) + ACT2_CARD_ON_SCREEN
//       (the open card painted as a guarded beat on the live view — the
//       restore cleared the presentation first, so what is on screen IS the
//       card; the DLQ owns its drain, untouched).
//     3 travel to ruins -> q_r_descent completes on the real zone-enter wire
//       (ACT2_DESCENT; reward beat ruins_gate on screen, reward-flagged).
//     4 two interacts -> q_r_tongue completes on the real speak wire
//       (ACT2_TONGUE; the q_speak-pinned honest end state: the interact's own
//       npc_ show owns the box, the beat rides the log's REWARD_SHOWN line).
//     5 save round-trip of ACT-TWO ROWS on the shipped v3 QuestStates wire:
//       save mid-act, diverge the live act-two log through FromSaveRows
//       (dropping q_r_tongue), LoadGame must bring it back (ACT2_PERSIST) —
//       each completion its own observation (W6): every row above completed
//       off exactly one distinct driven fact, and the persist phase never
//       replays a card (a load is not a game beat — box stays closed).
//     6 pay the due wage -> q_r_bread completes (ACT2_WAGE; the single wage
//       path now resolves attribution across both logs — QUEST_OBJECTIVE
//       q_r_bread + WAGE_PAID for q_r_bread ride the real bus).
//     7 farm six extractions -> q_r_bones completes the arc (ACT2_BONES) ->
//       the close card pair drains in emission order behind the finale reward
//       and the uniform auto-close retires the box (ACT2_CLOSED).
//   The battery leg (ci/runtime_integration_test.sh, (N)) greps ACT2_OPENED,
//   ACT2_DESCENT, ACT2_TONGUE, ACT2_WAGE, ACT2_PERSIST, ACT2_CLOSED plus the
//   per-row QUEST_COMPLETED q_r_* markers (W6 pin: four separate completions,
//   four separate drives) and the two ACT_CARD markers.
public partial class RuntimeIntegrationProof : SceneTree
{
    private const int Story2TravelBudget = 900;       // p3: meadow -> ruins
    private const int Story2SpeakBudget = 1600;       // p4: two interacts
    private const int Story2WageBudget = 3200;        // p6: wage-grace clock
    private const int Story2FarmBudget = 4800;        // p7: six extractions
    // Drain guard: the finale account is reward + close pair, PLUS a
    // straggler — if the farm outran the wage beat's dwell, the finale reward
    // PREEMPTS dark_bread (R3: head re-queues at the TAIL for a full re-read)
    // and it lands behind the close pair. Observed (2026-10-05): 4 dwells +
    // the 241-tick paint-guard each. Guards only — the assertions are the
    // seen-both + closed checks below, never the timer.
    private const int Story2DrainBudget = 7 * DialogueSystem.RewardLineFrames + 60;   // p8

    private int _s2Phase;
    private int _s2PhaseFrames;

    /// <summary>Stage-55 dispatch (called from the main file's stage switch).</summary>
    private void RunStory2Stage()
    {
        if (!QSubscribe()) return;   // the shipped quest marker taps (per-row emits)
        _s2PhaseFrames++;

        switch (_s2Phase)
        {
            case 0:   // boot: the shipped act-one seam must be live (the open arm watches it)
                if (QStatus("q_intro") == QuestStatus.Completed && QStatus("q_speak") == QuestStatus.Active)
                {
                    GD.Print("LA_GATE: ACT2_BOOT — act-one boot fact drove q_intro (open edge armed)");
                    NextS2Phase();
                }
                else if (_s2PhaseFrames > 240)
                    Fail($"quest_arc2: boot intro stalled (intro={QStatus("q_intro")}, speak={QStatus("q_speak")})");
                break;

            case 1:   // speak q_speak on the real interact wire
                if (QStatus("q_speak") == QuestStatus.Completed)
                {
                    GD.Print("LA_GATE: ACT2_SPOKEN — q_speak rode the real interact wire");
                    NextS2Phase();
                }
                else if (_s2PhaseFrames > Story2SpeakBudget)
                    Fail("quest_arc2: q_speak never completed (interact -> DnaSpoken wire broken)");
                else
                {
                    _playerBody!.GlobalPosition = _companion!.GlobalPosition;
                    QPress("interact", ref _qInteractToggle);
                }
                break;

            case 2:   // the open edge through the REAL save path (owned save)
                // Test-divergence primitive (quest_persist's own idiom): put
                // the act-one snapshot on the disk as all-complete. Only then
                // does the load exercise the shipped restore seam — the act
                // open is NOT proof-side API, it is the wiring's sync arm.
                _director!.Quests.FromSaveRows(new List<string>
                {
                    "q_intro:completed", "q_speak:completed", "q_wage:completed",
                    "q_kills:completed", "q_boss:completed",
                });
                _director.SaveGame();
                GD.Print($"LA_GATE: ACT2_SAVE_WRITTEN [{string.Join(",", _director.Quests.ToSaveRows())}]");
                _director.LoadGame();
                var act2 = _director.QuestsAct2;
                Check("act two opened by the restore sync (row 1 Active, one row on the wire)",
                      act2.Status("q_r_descent") == QuestStatus.Active &&
                      act2.Status("q_r_tongue") == QuestStatus.NotStarted &&
                      act2.ToSaveRows().Count == 1 && act2.ToSaveRows()[0] == "q_r_descent:active",
                      $"[{string.Join(",", act2.ToSaveRows())}]");
                if (_failed) return;
                // The load cleared the presentation (R9), so the ONLY thing
                // that can be on screen now is the act-open card, painted as
                // a guarded presentation beat (never DialogueShown evidence).
                Check("the table-driven open card painted on the live view, guarded",
                      _director.DialogueUi.ActiveNode == "act2_open_a" && _director.DialogueUi.ActiveNodeIsReward,
                      $"node={_director.DialogueUi.ActiveNode} reward={_director.DialogueUi.ActiveNodeIsReward}");
                if (_failed) return;
                GD.Print("LA_GATE: ACT2_OPENED — restore sync opened the act (card + feed + row 1)");
                GD.Print("LA_GATE: ACT2_CARD_ON_SCREEN — act2_open_a as a guarded presentation beat");
                NextS2Phase();
                break;

            case 3:   // reach ruins on the real zone-enter wire
                if (QStatusAct2("q_r_descent") == QuestStatus.Completed)
                {
                    Check("q_r_descent completion showed its reward beat (guarded)",
                          _director!.DialogueUi.ActiveNode == "ruins_gate" && _director.DialogueUi.ActiveNodeIsReward,
                          $"node={_director.DialogueUi.ActiveNode}");
                    if (_failed) return;
                    Check("q_r_tongue auto-started behind it (authoring order IS arc order)",
                          QStatusAct2("q_r_tongue") == QuestStatus.Active, "chained by the shipped machine");
                    if (_failed) return;
                    GD.Print("LA_GATE: ACT2_DESCENT — the ruins zone-enter fact completed q_r_descent");
                    NextS2Phase();
                }
                else if (_s2PhaseFrames > Story2TravelBudget)
                    Fail($"quest_arc2: q_r_descent never completed (zone={_director!.CurrentZone})");
                else if (_director!.CurrentZone != "ruins")
                    QPressTravel();
                break;

            case 4:   // two real speaks meet q_r_tongue
                if (QStatusAct2("q_r_tongue") == QuestStatus.Completed)
                {
                    // Honest end state, pinned like quest_arc's q_speak leg:
                    // the interact path's own npc_ show owns the box in the
                    // same call (R1/R5), so the beat itself is the log's
                    // REWARD_SHOWN q_r_tongue line — the battery greps it.
                    Check("q_r_tongue completed; the interact's own show owns the box (q_speak-pinned end state)",
                          _director!.DialogueUi.ActiveNode.StartsWith("npc_") && !_director.DialogueUi.ActiveNodeIsReward,
                          $"node={_director.DialogueUi.ActiveNode}");
                    if (_failed) return;
                    GD.Print("LA_GATE: ACT2_TONGUE — two fresh in-act speaks met q_r_tongue");
                    NextS2Phase();
                }
                else if (_s2PhaseFrames > Story2SpeakBudget)
                    Fail("quest_arc2: q_r_tongue never completed (in-act speaks -> DnaSpoken wire broken)");
                else
                {
                    _playerBody!.GlobalPosition = _companion!.GlobalPosition;
                    QPress("interact", ref _qInteractToggle);
                }
                break;

            case 5:   // ACT-TWO save round-trip on the shipped v3 QuestStates wire
                {
                    var rows = _director!.QuestsAct2.ToSaveRows();
                    Check("mid-act wire rows: descent+tongue completed, bread Active",
                          rows.Count == 3 && rows[0] == "q_r_descent:completed" &&
                          rows[1] == "q_r_tongue:completed" && rows[2] == "q_r_bread:active",
                          string.Join(",", rows));
                    if (_failed) return;
                    _director.SaveGame();
                    GD.Print($"LA_GATE: ACT2_PERSIST_SAVED [{string.Join(",", rows)}]");
                    // Diverge the LIVE act-two log through the same restore
                    // primitive (a dropped row must come back from disk, not
                    // be a no-op — the shipped quest_persist discipline).
                    _director.QuestsAct2.FromSaveRows(new List<string> { "q_r_descent:completed" });
                    Check("live act-two log diverged (q_r_tongue un-started) before the load",
                          QStatusAct2("q_r_tongue") == QuestStatus.NotStarted,
                          $"tongue={QStatusAct2("q_r_tongue")}");
                    if (_failed) return;
                    _director.LoadGame();
                    Check("ACT2_PERSIST: saved act-two rows reapplied through the real load",
                          QStatusAct2("q_r_descent") == QuestStatus.Completed &&
                          QStatusAct2("q_r_tongue") == QuestStatus.Completed &&
                          QStatusAct2("q_r_bread") == QuestStatus.Active &&
                          _director.QuestsAct2.ToSaveRows().Count == rows.Count,
                          $"[{string.Join(",", _director.QuestsAct2.ToSaveRows())}]");
                    if (_failed) return;
                    // A load is not a game beat: the restore cleared the
                    // presentation and NO card replayed.
                    Check("the load replayed no act card (a load is not a game beat)",
                          !_director.DialogueUi.IsOpen && _director.DialogueUi.ActiveNode.Length == 0,
                          $"node={_director.DialogueUi.ActiveNode}");
                    if (_failed) return;
                    GD.Print("LA_GATE: ACT2_PERSIST — act-two rows survived save -> load on the shipped wire");
                    NextS2Phase();
                    break;
                }

            case 6:   // the single wage path resolves the ACT-TWO row
                if (QStatusAct2("q_r_bread") == QuestStatus.Completed)
                {
                    Check("q_r_bread completion showed its reward beat (guarded)",
                          _director!.DialogueUi.ActiveNode == "dark_bread" && _director.DialogueUi.ActiveNodeIsReward,
                          $"node={_director.DialogueUi.ActiveNode}");
                    if (_failed) return;
                    GD.Print("LA_GATE: ACT2_WAGE — the pay settle served q_r_bread (both-logs attribution)");
                    NextS2Phase();
                }
                else if (_s2PhaseFrames > Story2WageBudget)
                    Fail("quest_arc2: q_r_bread never completed (pay/settle wire broken)");
                else if (_director!.WageDueNow)
                    QPress("pay_wage", ref _qPayToggle);
                break;

            case 7:   // six in-act extractions meet q_r_bones — the act finale
                if (QStatusAct2("q_r_bones") == QuestStatus.Completed)
                {
                    Check("the ruins-deep finale completed the ACT-TWO arc",
                          _director!.QuestsAct2.IsArcComplete, "all four RuinsArc rows Completed");
                    if (_failed) return;
                    Check("the finale reward beat holds the box in the call (the close pair appended behind it)",
                          _director.DialogueUi.ActiveNode == "bones_deeper" && _director.DialogueUi.ActiveNodeIsReward,
                          $"node={_director.DialogueUi.ActiveNode}");
                    if (_failed) return;
                    GD.Print("LA_GATE: ACT2_BONES — six fresh extractions met q_r_bones (ACT-TWO ARC COMPLETE)");
                    NextS2Phase();
                }
                else if (_s2PhaseFrames > Story2FarmBudget)
                    Fail("quest_arc2: q_r_bones never completed (kill -> DnaExtracted wire broken)");
                else if (!QKillLoop())
                    QPressTravel();
                break;

            case 8:   // the close card drains behind the finale beat IN ORDER,
                      // then the uniform auto-close retires the box (DLQ, untouched).
                {
                    var ui = _director!.DialogueUi;
                    if (_s2PhaseFrames > Story2DrainBudget)
                    { Fail($"quest_arc2: close-card drain budget exceeded (node={ui.ActiveNode}, open={ui.IsOpen})"); break; }
                    if (ui.ActiveNode == "act2_close_a" && ui.ActiveNodeIsReward)
                        _seenCloseA = true;
                    if (_seenCloseA && !_seenCloseB && ui.ActiveNode == "act2_close_b" && ui.ActiveNodeIsReward)
                        _seenCloseB = true;
                    if (_seenCloseA && _seenCloseB && !ui.IsOpen)
                    {
                        Check("both close-card nodes painted (in order) and the box auto-closed itself",
                              ui.ActiveNode.Length == 0 && !ui.Visible, "no external Close call");
                        if (_failed) return;
                        GD.Print("LA_GATE: ACT2_CLOSED — close card drained in emission order; uniform auto-close");
                        GD.Print("LA_GATE: PASS — act-two arc verified (open card -> 4 chained completions -> persist -> close card, one composition root)");
                        _asserted = true; _stage = 6; _stageFrames = 0; _holdStartPhys = _physFrames;
                    }
                    break;
                }
        }
    }

    // act-two status read (the Quests property keeps its shipped act-one
    // identity — see WorldDirector.Story.cs header).
    private QuestStatus QStatusAct2(string id) => _director!.QuestsAct2.Status(id);

    // close-card sub-latches (phase 8): the drain must SHOW both nodes.
    private bool _seenCloseA;
    private bool _seenCloseB;

    private void NextS2Phase()
    {
        _s2Phase++;
        _s2PhaseFrames = 0;
    }
}
