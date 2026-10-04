using Godot;
using LastAnimal.Dna;
using LastAnimal.Ecosystem;
using LastAnimal.Story;
using LastAnimal.Ui;

// Last Animal — stage 2f skills/quest UI wiring (MC 3933, code, 2026-10-02).
//
// Partial-class half of WorldDirector (one class = still THE single
// composition root, plan §B): the InitUi root seam (same idiom as
// InitStory/InitSkills) constructs the TAB-toggle SkillsPanel on the UI
// canvas, binds the live sources onto it AND onto the HUD's 2f rows, and
// owns the ui_toggle (TAB) poll plus the per-frame refresh tick.
//
// Owner ruling D4 verbatim (PLAN §D, "All rec"): the skills UI is a TOGGLE
// PANEL (TAB) + a persistent HUD readout (Manna digits + learned-skill
// names); NO tech tree; the quest HUD line is the ACTIVE quest's title +
// objective (titles authored by 2d under the ids 2c's proof pins).
//
// Freshness (plan §G D4): every readout closure below re-derives from the
// live authorities at CALL time — PlayerMutations.Unlocked over the same
// per-position consensus the ecosystem learns from (the only round-trip
// stable unlock source), PlayerController.Manna, and the QuestLog's first
// still-Active row. Nothing on this side of the seam is a copy.
//
// MC 10098 Inc-3 S0 (this file owns the bus-emit seam): TickUi additionally
// runs the per-frame EDGE-DETECT poll that mirrors dialogue/hurt/boss state
// onto the EventBus (DialogueShown/DialogueClosed/PlayerHurt/BossFallen)
// WITHOUT touching DialogueSystem/queue code (F3). The poll is the ONLY
// emitter (S2/S4/S10 subscribe); BossFallen edge-detects the tracked
// BossActor's IsDead (F-2/DA-c3: ClearZoneEnemies NULLS _boss on zone exit
// — a HasLiveBoss flip-detector would emit a FALSE BossFallen there).
namespace LastAnimal.World;

public partial class WorldDirector
{
    private SkillsPanel _skillsPanel = null!;

    // --- MC 10098 S0: bus-emit poll state (frame-boundary edge detector, F4;
    //     baselines initialise on the FIRST poll, never in InitUi — InitUi
    //     early-returns when no UI host exists, the poll must still work). ---
    private string _busNodeSeen = string.Empty;
    private int _busHealthSeen;
    private bool _busHealthBaseline;
    private EnemyActor? _busBossTracked;   // the PREVIOUS-FRAME boss reference
    private bool _busBossFallenEmitted;    // once per tracked actor's death

    /// <summary>Read-only surface the runtime proofs / capture tests read
    /// (proof-only, no logic).</summary>
    public SkillsPanel SkillsUi => _skillsPanel;

    /// <summary>
    /// Root seam, called from _Ready right after InitSkills. Constructs the
    /// panel (hidden at boot — its ctor default), wires the bus so the rows
    /// follow QuestCompleted/SkillUsed, and injects the live sources into
    /// the panel and the HUD (plan §G D4: closures over the live systems,
    /// no cached values anywhere in the View).
    /// </summary>
    private void InitUi()
    {
        if (UICanvas == null) return;   // same guard as BuildUi (no UI host)

        _skillsPanel = new SkillsPanel { Name = "SkillsPanel" };
        _skillsPanel.ConnectBus(_bus);
        UICanvas.AddChild(_skillsPanel);
        _skillsPanel.SetLiveSource(LiveUnlocks, () => _player.Manna);
        _hud?.BindLive(() => _player.Manna, LiveUnlocks, LiveQuestLine);
    }

    /// <summary>Live unlock authority (plan §G D2/D4): the SAME pure
    /// function the skill arms read — never a stored verdict.</summary>
    public SkillUnlocks LiveUnlocks() =>
        PlayerMutations.Unlocked(EcosystemAdaptation.ModelPlayerDna(_spokenDna));

    /// <summary>The quest tracker line: ACTIVE quest title + objective.</summary>
    public string LiveQuestLine() => FormatQuestLine(_quests.Table, _quests.ActiveQuestId);

    /// <summary>
    /// _Process seam (root, one line): the TAB toggle handler + the refresh
    /// tick. The HUD re-reads its live providers every frame (cheap string
    /// paints); the visible panel re-reads its own while open — bus events
    /// (QuestCompleted/SkillUsed) also refresh it, the tick additionally
    /// covers value moves that ride no bus signal (a save-load Manna
    /// restore, the kill-manna gain).
    /// </summary>
    private void TickUi()
    {
        PollBusEmits();   // MC 10098 S0: dialogue/hurt/boss edge-detect onto the bus
        if (Input.IsActionJustPressed("ui_toggle"))
            ToggleSkillsPanel();
        if (_hud != null) _hud.RefreshLive();
        if (_skillsPanel != null && _skillsPanel.Visible) _skillsPanel.Refresh();
    }

    /// <summary>
    /// MC 10098 S0: the frame-boundary edge detector that carries presentation
    /// state onto the bus (plan §S0). One emit per edge — the previous-frame
    /// baseline only updates where the value actually moved (a re-poll of an
    /// unchanged value emits nothing). F-2 (DA-c3): the boss edge is tracked
    /// by ACTOR REFERENCE — emit only when the tracked BossActor's IsDead
    /// turns true; a null swap (ClearZoneEnemies, WorldDirector.cs:342)
    /// re-arms the tracker WITHOUT emitting (zone exit is not a death).
    /// Read-only on every authority; DialogueSystem/queue code untouched.
    /// </summary>
    private void PollBusEmits()
    {
        if (_dialogue != null)
        {
            string node = _dialogue.ActiveNode;
            if (node != _busNodeSeen)
            {
                if (_busNodeSeen.Length > 0) _bus.EmitDialogueClosed(_busNodeSeen);
                if (node.Length > 0) _bus.EmitDialogueShown(node);
                _busNodeSeen = node;
            }
        }
        if (_player != null)
        {
            int health = _player.Health;
            if (_busHealthBaseline && health < _busHealthSeen) _bus.EmitPlayerHurt(health);
            _busHealthSeen = health;
            _busHealthBaseline = true;
        }
        var boss = BossActor;
        if (!ReferenceEquals(boss, _busBossTracked))
        {
            // Reference swap (spawn or the zone-exit null): re-arm, never emit.
            _busBossTracked = boss;
            _busBossFallenEmitted = false;
        }
        else if (boss != null && boss.IsDead && !_busBossFallenEmitted)
        {
            _bus.EmitBossFallen(boss.Ai.EntityId.ToString());
            _busBossFallenEmitted = true;
        }
    }

    /// <summary>TAB arm: flip the skills panel (owner ruling D5: toggle,
    /// no tree, no pause — a readout, like the empathy book).</summary>
    private void ToggleSkillsPanel()
    {
        if (_skillsPanel == null) return;
        _skillsPanel.Toggle();
        GD.Print($"W3: ui_toggle -> SkillsPanel {(_skillsPanel.Visible ? "opened" : "closed")}");
    }

    // ---- presentation formatting (pure, pinned by tests/ui) ---------------

    /// <summary>Format the HUD tracker line for the ACTIVE quest: "Quest:
    /// {Title} — {objective}". No Active row (arc not started or finished)
    /// → "Quest: none". Pure static so the 2f logic leg pins it headless.
    /// (src/story/* is 2f-forbidden, so the line composition lives here, on
    /// the UI glue side of the seam — presentation, not quest logic.)</summary>
    public static string FormatQuestLine(QuestTable table, string? activeQuestId)
    {
        if (activeQuestId == null || !table.TryGetEntry(activeQuestId, out var q) || q == null)
            return "Quest: none";
        return $"Quest: {q.Title} — {ObjectiveLine(q.Objective)}";
    }

    /// <summary>Diegetic-minimal objective line per authored kind (2f
    /// presentation vocabulary over the 2c objective kinds; English per
    /// owner ruling D6).</summary>
    public static string ObjectiveLine(QuestObjective o) => o.Kind switch
    {
        QuestObjectiveKind.DialogueShown => "Answer when it speaks",
        QuestObjectiveKind.Spoken => o.Amount == 1
            ? "Speak the tongue" : $"Speak the tongue x{o.Amount}",
        QuestObjectiveKind.WagePaid => "Pay the wage",
        QuestObjectiveKind.Kills => o.Amount == 1
            ? "Learn from one kill" : $"Learn from {o.Amount} kills",
        QuestObjectiveKind.ZoneReached => $"Reach {o.Arg}",
        QuestObjectiveKind.LoyaltyAtLeast => $"Keep a follower loyal ({o.Amount})",
        QuestObjectiveKind.BossDead => "End the watcher",
        _ => "Press on",
    };
}
