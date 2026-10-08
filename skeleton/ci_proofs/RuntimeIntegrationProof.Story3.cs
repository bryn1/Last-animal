// SIZE: proof partial (~270 l), test-class ceiling 600 (MC 10273 S19).
using Godot;
using LastAnimal.Story;
using LastAnimal.Ui;
using LastAnimal.World;
using System.Collections.Generic;

// Last Animal — stage S19 ACT-THREE runtime proof (MC 10273, code, 2026-10-07).
//
// Partial-class half of RuntimeIntegrationProof: the act-three chain (the +4
// QuestTable.ActThreeArc() rows, RULING-8 — board line MC 10026 append #9
// row 895: "R8 zone four + act three = YES -> S18 + S19 proceed", owner chat
// "Rec on all.") driven on the
// REAL playable scene — the same WorldDirector + InitStory + pure QuestLog
// machine, now playing the THIRD log instance through its whole lifecycle:
// act open (act-two rows complete -> restore-driven FreshOpen edge + the
// table-driven open card on the live view), the four in-act zone-4
// completions each off its OWN distinct drive (the W6 pin), the save
// round-trip of the act-three rows on the SHIPPED QuestStates wire (zero
// save-file schema delta), the named ACT_THREE_COMPLETE act-completion
// marker the wiring prints when the final zone-4 chain completes, and the
// close card on the finale. Story2.cs is the exact idiom parent — no new
// mechanism is exercised here.
//
// Mode (LA_GATE_MODE):
//   ACT_THREE — stage 103. Phase plan:
//     0 boot: q_intro completed by the boot zone fact, q_speak Active (the
//       shipped act-one seam proven live — ACT3_BOOT).
//     1 open edge through the REAL save path (owned save, Story2 phase 2
//       idiom): the act-one AND act-two logs are diverged to all-complete
//       via the same FromSaveRows primitive quest_persist uses, SaveGame()
//       stamps that snapshot, LoadGame() runs the shipped restore seam —
//       whose act-two sync ADOPTS (rows present, close card silently shown)
//       and whose act-three sync finds "act two complete, act three
//       un-started" and OPENS the act: ACT3_OPENED (row 1 Active, one row
//       on the wire) + ACT3_CARD_ON_SCREEN (the open card painted as a
//       guarded beat — the restore cleared the presentation first).
//     2 THREE travels on the real zone-enter wire reach the S18-shipped
//       zone-4 "hollow" -> q_h_arrival completes (ACT3_ARRIVAL; reward beat
//       hollow_gate on screen, reward-flagged).
//     3 three interacts -> q_h_tongue completes on the real speak wire
//       (ACT3_TONGUE; the q_speak-pinned honest end state — the interact's
//       own npc_ show owns the box, the beat rides REWARD_SHOWN).
//     4 save round-trip of ACT-THREE ROWS on the shipped v3 QuestStates
//       wire (the mid-quest restore edge): save mid-act, diverge the live
//       act-three log through FromSaveRows (dropping q_h_tongue), LoadGame
//       must bring it back (ACT3_PERSIST) — and replay NO card (a load is
//       not a game beat: the box stays closed).
//     5 pay the due wage -> q_h_wage completes (ACT3_WAGE; the single wage
//       path now resolves attribution across THREE logs — WAGE_PAID for
//       q_h_wage rides the real bus).
//     6 farm eight extractions -> q_h_reckoning completes the arc
//       (ACT3_RECKONING) -> the wiring's act-completion condition prints
//       ACT_THREE_COMPLETE and the close card pair drains in emission order
//       behind the finale reward, the uniform auto-close retiring the box
//       (ACT3_CLOSED).
//   The battery leg (ci/runtime_integration_test.sh, (T)) greps ACT3_OPENED,
//   ACT3_CARD_ON_SCREEN, ACT3_ARRIVAL, ACT3_TONGUE, ACT3_WAGE, ACT3_PERSIST,
//   ACT3_RECKONING, ACT_THREE_COMPLETE, ACT3_CLOSED, both ACT_CARD act_three
//   markers, the per-row QUEST_COMPLETED/REWARD_SHOWN q_h_* markers (W6 pin:
//   four completions, four separate drives) and WAGE_PAID for q_h_wage.
public partial class RuntimeIntegrationProof : SceneTree
{
    private const int Story3TravelBudget = 3 * 900;   // p2: meadow -> canyon -> ruins -> hollow (900/travel, Story2 pin)
    private const int Story3SpeakBudget = 2400;       // p3: three interacts
    private const int Story3WageBudget = 3200;        // p5: wage-grace clock
    private const int Story3FarmBudget = 6400;        // p6: eight extractions
    // Drain guard: the finale account is reward + close pair PLUS the
    // preempted-beat straggler (Story2's exact rationale — the farm can
    // outrun the wage beat's dwell and the re-queued head lands behind the
    // close pair). Guards only — the assertions are the seen-both + closed
    // checks below, never the timer.
    private const int Story3DrainBudget = 7 * DialogueSystem.RewardLineFrames + 60;   // p7

    private int _s3Phase;
    private int _s3PhaseFrames;
    private bool _seenCloseA3;
    private bool _seenCloseB3;

    /// <summary>Stage-103 dispatch (called from the main file's stage switch).</summary>
    private void RunActThreeStage()
    {
        if (!QSubscribe()) return;   // the shipped quest marker taps (per-row emits)
        _s3PhaseFrames++;

        switch (_s3Phase)
        {
            case 0:   // boot: the shipped act-one seam must be live (the chain arms on it)
                if (QStatus("q_intro") == QuestStatus.Completed && QStatus("q_speak") == QuestStatus.Active)
                {
                    GD.Print("LA_GATE: ACT3_BOOT — act-one boot fact drove q_intro (chain armed)");
                    NextS3Phase();
                }
                else if (_s3PhaseFrames > 240)
                    Fail($"act_three: boot intro stalled (intro={QStatus("q_intro")}, speak={QStatus("q_speak")})");
                break;

            case 1:   // the open edge through the REAL save path (owned save):
                // both PRIOR logs diverged to all-complete via the shipped
                // FromSaveRows primitive, then SaveGame -> LoadGame lets the
                // shipped restore seams decide — the act open is NOT proof-
                // side API, it is the wiring's sync chain (act2 ADOPTS its
                // restored rows, act3 FreshOpens off act two's completion).
                _director!.Quests.FromSaveRows(new List<string>
                {
                    "q_intro:completed", "q_speak:completed", "q_wage:completed",
                    "q_kills:completed", "q_boss:completed",
                });
                _director.QuestsAct2.FromSaveRows(new List<string>
                {
                    "q_r_descent:completed", "q_r_tongue:completed",
                    "q_r_bread:completed", "q_r_bones:completed",
                });
                _director.SaveGame();
                GD.Print($"LA_GATE: ACT3_SAVE_WRITTEN [{string.Join(",", _director.QuestsAct2.ToSaveRows())}]");
                _director.LoadGame();
                var act3 = _director.QuestsAct3;
                Check("act three opened by the restore sync chain (row 1 Active, one row on the wire)",
                      act3.Status("q_h_arrival") == QuestStatus.Active &&
                      act3.Status("q_h_tongue") == QuestStatus.NotStarted &&
                      act3.ToSaveRows().Count == 1 && act3.ToSaveRows()[0] == "q_h_arrival:active",
                      $"[{string.Join(",", act3.ToSaveRows())}]");
                if (_failed) return;
                // The load cleared the presentation (R9), so the ONLY thing
                // that can be on screen now is the act-open card (act two's
                // ADOPT replayed NOTHING — a load is not a game beat).
                Check("the table-driven open card painted on the live view, guarded",
                      _director.DialogueUi.ActiveNode == "act3_open_a" && _director.DialogueUi.ActiveNodeIsReward,
                      $"node={_director.DialogueUi.ActiveNode} reward={_director.DialogueUi.ActiveNodeIsReward}");
                if (_failed) return;
                GD.Print("LA_GATE: ACT3_OPENED — restore sync chain opened the act (card + feed + row 1)");
                GD.Print("LA_GATE: ACT3_CARD_ON_SCREEN — act3_open_a as a guarded presentation beat");
                NextS3Phase();
                break;

            case 2:   // three real travels reach the S18-shipped hollow on the zone-enter wire
                if (QStatusAct3("q_h_arrival") == QuestStatus.Completed)
                {
                    Check("q_h_arrival completion showed its reward beat (guarded)",
                          _director!.DialogueUi.ActiveNode == "hollow_gate" && _director.DialogueUi.ActiveNodeIsReward,
                          $"node={_director.DialogueUi.ActiveNode}");
                    if (_failed) return;
                    Check("q_h_tongue auto-started behind it (authoring order IS arc order)",
                          QStatusAct3("q_h_tongue") == QuestStatus.Active, "chained by the shipped machine");
                    if (_failed) return;
                    GD.Print("LA_GATE: ACT3_ARRIVAL — the zone-4 'hollow' entry fact completed q_h_arrival");
                    NextS3Phase();
                }
                else if (_s3PhaseFrames > Story3TravelBudget)
                    Fail($"act_three: q_h_arrival never completed (zone={_director!.CurrentZone})");
                else if (_director!.CurrentZone != "hollow")
                    QPressTravel();
                break;

            case 3:   // three real speaks meet q_h_tongue
                if (QStatusAct3("q_h_tongue") == QuestStatus.Completed)
                {
                    // Honest end state, pinned like quest_arc's q_speak leg:
                    // the interact path's own npc-node show owns the box in
                    // the same call (R1/R5), so the beat itself is the log's
                    // REWARD_SHOWN q_h_tongue line — the battery greps it.
                    Check("q_h_tongue completed; the interact's own show owns the box (q_speak-pinned end state)",
                          _director!.DialogueUi.ActiveNode.StartsWith("npc_") && !_director.DialogueUi.ActiveNodeIsReward,
                          $"node={_director.DialogueUi.ActiveNode}");
                    if (_failed) return;
                    GD.Print("LA_GATE: ACT3_TONGUE — three fresh in-act speaks met q_h_tongue");
                    NextS3Phase();
                }
                else if (_s3PhaseFrames > Story3SpeakBudget)
                    Fail("act_three: q_h_tongue never completed (in-act speaks -> DnaSpoken wire broken)");
                else
                {
                    _playerBody!.GlobalPosition = _companion!.GlobalPosition;
                    QPress("interact", ref _qInteractToggle);
                }
                break;

            case 4:   // ACT-THREE save round-trip on the shipped v3 QuestStates wire
                {
                    var rows = _director!.QuestsAct3.ToSaveRows();
                    Check("mid-act wire rows: arrival+tongue completed, wage Active",
                          rows.Count == 3 && rows[0] == "q_h_arrival:completed" &&
                          rows[1] == "q_h_tongue:completed" && rows[2] == "q_h_wage:active",
                          string.Join(",", rows));
                    if (_failed) return;
                    _director.SaveGame();
                    GD.Print($"LA_GATE: ACT3_PERSIST_SAVED [{string.Join(",", rows)}]");
                    // Diverge the LIVE act-three log through the same restore
                    // primitive (a dropped row must come back from disk, not
                    // be a no-op — the shipped quest_persist discipline).
                    _director.QuestsAct3.FromSaveRows(new List<string> { "q_h_arrival:completed" });
                    Check("live act-three log diverged (q_h_tongue un-started) before the load",
                          QStatusAct3("q_h_tongue") == QuestStatus.NotStarted,
                          $"tongue={QStatusAct3("q_h_tongue")}");
                    if (_failed) return;
                    _director.LoadGame();
                    Check("ACT3_PERSIST: saved act-three rows reapplied through the real load",
                          QStatusAct3("q_h_arrival") == QuestStatus.Completed &&
                          QStatusAct3("q_h_tongue") == QuestStatus.Completed &&
                          QStatusAct3("q_h_wage") == QuestStatus.Active &&
                          _director.QuestsAct3.ToSaveRows().Count == rows.Count,
                          $"[{string.Join(",", _director.QuestsAct3.ToSaveRows())}]");
                    if (_failed) return;
                    // A load is not a game beat: the restore cleared the
                    // presentation and NO card replayed.
                    Check("the load replayed no act card (a load is not a game beat)",
                          !_director.DialogueUi.IsOpen && _director.DialogueUi.ActiveNode.Length == 0,
                          $"node={_director.DialogueUi.ActiveNode}");
                    if (_failed) return;
                    GD.Print("LA_GATE: ACT3_PERSIST — act-three rows survived save -> load on the shipped wire");
                    NextS3Phase();
                    break;
                }

            case 5:   // the single wage path resolves the ACT-THREE row
                if (QStatusAct3("q_h_wage") == QuestStatus.Completed)
                {
                    Check("q_h_wage completion showed its reward beat (guarded)",
                          _director!.DialogueUi.ActiveNode == "hollow_wage" && _director.DialogueUi.ActiveNodeIsReward,
                          $"node={_director.DialogueUi.ActiveNode}");
                    if (_failed) return;
                    GD.Print("LA_GATE: ACT3_WAGE — the pay settle served q_h_wage (three-logs attribution)");
                    NextS3Phase();
                }
                else if (_s3PhaseFrames > Story3WageBudget)
                    Fail("act_three: q_h_wage never completed (pay/settle wire broken)");
                else if (_director!.WageDueNow)
                    QPress("pay_wage", ref _qPayToggle);
                break;

            case 6:   // eight in-act extractions meet q_h_reckoning — the story close
                if (QStatusAct3("q_h_reckoning") == QuestStatus.Completed)
                {
                    Check("the zone-4 finale completed the ACT-THREE arc (the act-completion condition)",
                          _director!.QuestsAct3.IsArcComplete, "all four ActThreeArc rows Completed");
                    if (_failed) return;
                    Check("the finale reward beat holds the box in the call (the close pair appended behind it)",
                          _director.DialogueUi.ActiveNode == "hollow_count" && _director.DialogueUi.ActiveNodeIsReward,
                          $"node={_director.DialogueUi.ActiveNode}");
                    if (_failed) return;
                    GD.Print("LA_GATE: ACT3_RECKONING — eight fresh extractions met q_h_reckoning (ACT-THREE ARC COMPLETE)");
                    NextS3Phase();
                }
                else if (_s3PhaseFrames > Story3FarmBudget)
                    Fail("act_three: q_h_reckoning never completed (kill -> DnaExtracted wire broken)");
                else if (!QKillLoop())
                    QPressTravel();
                break;

            case 7:   // the close card drains behind the finale beat IN ORDER,
                      // then the uniform auto-close retires the box (DLQ, untouched).
                {
                    var ui = _director!.DialogueUi;
                    if (_s3PhaseFrames > Story3DrainBudget)
                    { Fail($"act_three: close-card drain budget exceeded (node={ui.ActiveNode}, open={ui.IsOpen})"); break; }
                    if (ui.ActiveNode == "act3_close_a" && ui.ActiveNodeIsReward)
                        _seenCloseA3 = true;
                    if (_seenCloseA3 && !_seenCloseB3 && ui.ActiveNode == "act3_close_b" && ui.ActiveNodeIsReward)
                        _seenCloseB3 = true;
                    if (_seenCloseA3 && _seenCloseB3 && !ui.IsOpen)
                    {
                        Check("both close-card nodes painted (in order) and the box auto-closed itself",
                              ui.ActiveNode.Length == 0 && !ui.Visible, "no external Close call");
                        if (_failed) return;
                        GD.Print("LA_GATE: ACT3_CLOSED — close card drained in emission order; uniform auto-close");
                        GD.Print("LA_GATE: PASS — act-three arc verified (open card -> 4 chained zone-4 completions -> persist -> ACT_THREE_COMPLETE -> close card, one composition root)");
                        _asserted = true; _stage = 6; _stageFrames = 0; _holdStartPhys = _physFrames;
                    }
                    break;
                }
        }
    }

    // act-three status read (the Quests property keeps its shipped act-one
    // identity — see WorldDirector.Story.cs header).
    private QuestStatus QStatusAct3(string id) => _director!.QuestsAct3.Status(id);

    private void NextS3Phase()
    {
        _s3Phase++;
        _s3PhaseFrames = 0;
    }
}
