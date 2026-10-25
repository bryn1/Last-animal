// SIZE: proof partial (~230 l NEW, MC 11295), test-class ceiling 600.
using Godot;
using LastAnimal.Story;
using LastAnimal.World;
using System.Collections.Generic;
using System.Text.RegularExpressions;

// Last Animal — MC 11295 quest-beat MONOTONICITY proof (playthrough D3, MC 11270
// verdict D3: after the boss-kill Act-3 beat the quest line REGRESSED — "The
// Watcher Falls" -> back to "The Way Down — Reach ruins" — while standing IN the
// ruins, and the regressed state is what the SAVE held).
//
// Partial-class half of RuntimeIntegrationProof, dispatched from stage 50's
// RunQuestStage switch (_mode) — the quest-family slot (55/103 precedent: a new
// MODE, not a new stage number; pairing stays 34==34, no Run*Stage body added).
//
// Mode (LA_GATE_MODE):
//   quest_mono — stage 50 (the quest-family slot). Phase plan (the REAL scene,
//     NO log-divergence shortcut — the whole point is the boss-death ORDERING
//     the diverge-based act proofs (quest_arc2/ACT_THREE) never drive: the act
//     opens while the player already STANDS in the act's zone):
//     0 boot: the meadow entry completed q_intro, q_speak Active (MONO_BOOT).
//     1 real interact completes q_speak (MONO_SPOKEN).
//     2 the due wage settles q_wage (MONO_WAGE).
//     3 farm four extractions -> q_kills completes, q_boss Active (MONO_KILLS).
//     4 travel to the RUINS on the real zone wire; the ruins SpawnSet fields
//       the boss at the DNA threshold (MONO_BOSS_LIVE zone=ruins); the tracker
//       prints its BEFORE line and must read "The Watcher Falls — End the
//       watcher" (MONO_TRACKER_BEFORE).
//     5 THE GATE: kill the live boss. q_boss completes -> the act-two open
//       edge fires AT THE RUINS. The beat must ADVANCE, never regress:
//       q_r_descent (ZoneReached ruins) is met AT OPEN by the standing-zone
//       observation (its fact was already true — re-earning it would need a
//       zone-exit/re-entry the player does not perform), act two's active row
//       is q_r_tongue, and LiveQuestLine() contains NEITHER "The Way Down"
//       (the D3 regression line) NOR "The Watcher Falls" (the stale act-one
//       line). RED before-fix BY NAME: on the unfixed product this check fails
//       with the stranded [q_r_descent:active] account printed (MONO_ADVANCE).
//     6 save + disk + load: the save WRITE carries the ADVANCED rows; the
//       user:// file ON DISK carries them (MONO_SAVEDROWS — the D3 evidence
//       that saved state, not HUD staleness, held the regression); LoadGame
//       round-trips the ADVANCED state (Adopt arm, silent — a load is not a
//       game beat) (MONO_PERSIST).
//     7 re-entries: FOUR travels wrap hollow -> meadow -> canyon -> RUINS —
//       the ruins re-entry is the very fact the open edge seeded; completed
//       beats must not recompute backwards (MONO_REENTER), then PASS + hold.
//   The battery leg (ci/runtime_integration_test.sh) greps MONO_BOOT,
//   MONO_SPOKEN, MONO_WAGE, MONO_KILLS, MONO_BOSS_LIVE, MONO_TRACKER_BEFORE,
//   MONO_ADVANCE, MONO_TRACKER_AFTER, MONO_SAVEDROWS, MONO_PERSIST,
//   MONO_REENTER, QUEST_COMPLETED q_r_descent (the open-edge completion rode
//   the real bus) and the ACT_CARD act_two open marker.
public partial class RuntimeIntegrationProof : SceneTree
{
    private const int MonoSpeakBudget = 1600;     // p1: interact -> q_speak
    private const int MonoWageBudget = 3200;      // p2: wage-grace clock (Story2 pin)
    private const int MonoKillBudget = 4800;      // p3: four extractions (Story2 pin)
    private const int MonoTravelBudget = 2700;    // p4: to the ruins (3 x 900, Story3 pin)
    private const int MonoBossBudget = 4800;      // p5: kill the tier-2 ruins boss
    private const int MonoReenterBudget = 3600;   // p7: four wrap travels

    private int _qmPhase;
    private int _qmPhaseFrames;
    private int _qmTravels;
    private string _qmTrackerBefore = "";

    /// <summary>Stage-50 quest_mono body (called from RunQuestStage's mode switch).</summary>
    private void QuestMonoStage()
    {
        if (!QSubscribe()) return;   // the shipped quest marker taps (per-row emits)
        _qmPhaseFrames++;

        switch (_qmPhase)
        {
            case 0:   // boot: the shipped act-one boot fact (identical first phase to the family)
                if (QStatus("q_intro") == QuestStatus.Completed && QStatus("q_speak") == QuestStatus.Active)
                {
                    GD.Print("LA_GATE: MONO_BOOT — meadow entry completed q_intro (monotonicity chain armed)");
                    NextQmPhase();
                }
                else if (_qmPhaseFrames > 240)
                    Fail($"quest_mono: boot intro stalled (intro={QStatus("q_intro")}, speak={QStatus("q_speak")})");
                break;

            case 1:   // speak q_speak on the REAL interact wire (no divergence anywhere in this mode)
                if (QStatus("q_speak") == QuestStatus.Completed)
                {
                    GD.Print("LA_GATE: MONO_SPOKEN — q_speak rode the real interact wire");
                    NextQmPhase();
                }
                else if (_qmPhaseFrames > MonoSpeakBudget)
                    Fail("quest_mono: q_speak never completed (interact -> DnaSpoken wire broken)");
                else
                {
                    _playerBody!.GlobalPosition = _companion!.GlobalPosition;
                    QPress("interact", ref _qInteractToggle);
                }
                break;

            case 2:   // the single wage path meets q_wage (the grace clock is real time)
                if (QStatus("q_wage") == QuestStatus.Completed)
                {
                    GD.Print("LA_GATE: MONO_WAGE — the pay settle met q_wage");
                    NextQmPhase();
                }
                else if (_qmPhaseFrames > MonoWageBudget)
                    Fail("quest_mono: q_wage never completed (pay/settle wire broken)");
                else if (_director!.WageDueNow)
                    QPress("pay_wage", ref _qPayToggle);
                break;

            case 3:   // farm four extractions -> q_kills meets, the shipped chain starts q_boss
                if (QStatus("q_kills") == QuestStatus.Completed)
                {
                    Check("q_boss auto-started behind the counters row (authoring order IS arc order)",
                          QStatus("q_boss") == QuestStatus.Active, "chained by the shipped machine");
                    if (_failed) return;
                    GD.Print("LA_GATE: MONO_KILLS — four fresh extractions met q_kills (q_boss now Active)");
                    NextQmPhase();
                }
                else if (_qmPhaseFrames > MonoKillBudget)
                    Fail("quest_mono: q_kills never completed (kill -> DnaExtracted wire broken)");
                else if (!QKillLoop())
                    QPressTravel();
                break;

            case 4:   // reach the RUINS on the real zone wire and wait for its boss (D3's exact ground)
                if (_director!.CurrentZone == "ruins" && _director.HasLiveBoss)
                {
                    _qmTrackerBefore = _director.LiveQuestLine();
                    Check("the pre-kill tracker paints the act-one finale line (the beat the regression rewound from)",
                          _qmTrackerBefore.Contains("The Watcher Falls") && _qmTrackerBefore.Contains("End the watcher"),
                          $"tracker=[{_qmTrackerBefore}]");
                    if (_failed) return;
                    GD.Print($"LA_GATE: MONO_BOSS_LIVE zone=ruins — the tier-2 boss fields at the DNA threshold");
                    GD.Print($"LA_GATE: MONO_TRACKER_BEFORE [{_qmTrackerBefore}]");
                    NextQmPhase();
                }
                else if (_qmPhaseFrames > MonoTravelBudget)
                    Fail($"quest_mono: no live boss in the ruins (zone={_director!.CurrentZone}) — threshold/zone wire broken");
                else if (_director!.CurrentZone != "ruins")
                    QPressTravel();
                break;

            case 5:   // THE GATE: kill the boss — the act-two open edge fires AT THE RUINS.
                if (QStatus("q_boss") == QuestStatus.Completed && _director!.Quests.IsArcComplete)
                {
                    var act2 = _director.QuestsAct2;
                    string line = _director.LiveQuestLine();
                    Check("MONO_ADVANCE: the boss-kill at the ruins ADVANCED the beat — q_r_descent met AT OPEN (the standing-zone fact), act two Active = q_r_tongue, the tracker shows neither the regressed \"The Way Down\" nor the stale act-one line",
                          act2.Status("q_r_descent") == QuestStatus.Completed &&
                          act2.ActiveQuestId == "q_r_tongue" &&
                          !line.Contains("The Way Down") && !line.Contains("The Watcher Falls"),
                          $"tracker=[{line}] act2=[{string.Join(",", act2.ToSaveRows())}] zone={_director.CurrentZone}");
                    if (_failed) return;
                    Check("the open-edge descent completion rode the real bus + the guarded reward-beat path (one completion, its own observation)",
                          _director.DialogueUi.ActiveNode == "ruins_gate" ||
                          act2.Status("q_r_tongue") == QuestStatus.Active,
                          $"node={_director.DialogueUi.ActiveNode}");
                    if (_failed) return;
                    GD.Print("LA_GATE: MONO_ADVANCE — boss-kill at the ruins opened act two WITHOUT regressing the beat (q_r_descent earned at open, MC 11295)");
                    GD.Print($"LA_GATE: MONO_TRACKER_AFTER [{line}]");
                    NextQmPhase();
                    break;
                }
                else if (_qmPhaseFrames > MonoBossBudget)
                    Fail("quest_mono: the ruins boss never died (finale wire broken)");
                else
                    QBossAttackLoop();
                break;

            case 6:   // the ADVANCED state round-trips: write -> disk -> load (D3: the save HELD the regression)
                {
                    var act2 = _director!.QuestsAct2;
                    var rows = act2.ToSaveRows();
                    Check("the save WRITE carries the advanced rows (q_r_descent completed, q_r_tongue active)",
                          rows.Contains("q_r_descent:completed") && rows.Contains("q_r_tongue:active"),
                          string.Join(",", rows));
                    if (_failed) return;
                    _director.SaveGame();
                    // Read the ACTUAL bytes on disk — the D3 evidence class is
                    // the SAVED state, so the disk file is the witness, not the
                    // in-memory row list (the playthrough save-E-hollow idiom).
                    string path = ProjectSettings.GlobalizePath("user://savegame.json");
                    string json = System.IO.File.ReadAllText(path);
                    var diskRows = new List<string>();
                    foreach (Match m in Regex.Matches(json, "\"(q_[A-Za-z0-9_]+:[a-z_]+)\""))
                        diskRows.Add(m.Groups[1].Value);
                    Check("the user:// file ON DISK carries the advanced rows (the saved state is the ADVANCED one)",
                          diskRows.Contains("q_r_descent:completed") && diskRows.Contains("q_r_tongue:active"),
                          $"[{string.Join(",", diskRows)}] at {path}");
                    if (_failed) return;
                    GD.Print($"LA_GATE: MONO_SAVEDROWS [{string.Join(",", diskRows)}]");
                    _director.LoadGame();
                    string line = _director.LiveQuestLine();
                    Check("MONO_PERSIST: the LOAD round-trips the ADVANCED beat (Adopt arm, silent — a load is not a game beat)",
                          _director.QuestsAct2.Status("q_r_descent") == QuestStatus.Completed &&
                          _director.QuestsAct2.ActiveQuestId == "q_r_tongue" &&
                          _director.Quests.IsArcComplete &&
                          !line.Contains("The Way Down"),
                          $"[{string.Join(",", _director.QuestsAct2.ToSaveRows())}] tracker=[{line}]");
                    if (_failed) return;
                    GD.Print("LA_GATE: MONO_PERSIST — save -> disk -> load round-tripped the ADVANCED quest state (MC 11295)");
                    NextQmPhase();
                    break;
                }

            case 7:   // re-entries: four wrap travels, LAST the RUINS re-entry — the seeded fact re-fires
                if (_qmTravels < 4)
                {
                    if (QPressTravel())
                    {
                        _qmTravels++;
                        GD.Print($"LA_GATE: MONO_REENTER_STEP {_qmTravels} zone={_director!.CurrentZone} act2=[{string.Join(",", _director.QuestsAct2.ToSaveRows())}]");
                    }
                    break;
                }
                {
                    var act2 = _director!.QuestsAct2;
                    string line = _director.LiveQuestLine();
                    Check("MONO_REENTER: four zone re-entries (hollow/meadow/canyon/ruins wrap — the ruins re-entry re-fires the seeded fact) regressed NOTHING: completed beats are monotonic",
                          _director.CurrentZone == "ruins" &&
                          _director.Quests.IsArcComplete &&
                          act2.Status("q_r_descent") == QuestStatus.Completed &&
                          act2.ActiveQuestId == "q_r_tongue" &&
                          !line.Contains("The Way Down") && !line.Contains("The Watcher Falls"),
                          $"zone={_director.CurrentZone} act2=[{string.Join(",", act2.ToSaveRows())}] tracker=[{line}]");
                    if (_failed) return;
                    GD.Print("LA_GATE: MONO_REENTER — four re-entries incl. the ruins re-entry: zero regress (the open-edge seed holds against every later ZoneEntered)");
                    GD.Print("LA_GATE: PASS — quest beat monotonicity verified (MC 11295): boss-kill at the ruins ADVANCES, save/load round-trips the ADVANCED state, re-entries never recompute the beat backwards");
                    _asserted = true; _stage = 6; _stageFrames = 0; _holdStartPhys = _physFrames;
                    break;
                }
        }
    }

    private void NextQmPhase()
    {
        _qmPhase++;
        _qmPhaseFrames = 0;
    }
}
