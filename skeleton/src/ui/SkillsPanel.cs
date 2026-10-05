using Godot;
using LastAnimal.Core;
using LastAnimal.Dna;
using LastAnimal.Skills;
using System;

// Last Animal — stage 2f skills panel (MC 3933, code, 2026-10-02).
//
// SkillsPanel: the TAB-toggle readout for the player's DNA-mutation skills
// (owner ruling D5: toggle panel, NO tech tree). A View beside EmpathyPanel
// in the per-concern panel idiom: it renders the LIVE unlock authority and
// the current Manna and owns no gameplay logic (I4).
//
// Freshness (plan §G D4, RATIFIED "All rec"): the row text is re-derived on
// every Refresh from the provider functions injected by the composition root.
// The panel stores providers, never values — there is no cache here to stale
// out. It re-reads on the batched 2c bus signals it can hear
// (QuestCompleted, SkillUsed), on Toggle (a fresh read at open), and via the
// director's per-frame TickUi while visible.
//
// Costs are read from the SkillState economy constants (single source), ids
// from PlayerMutations (the unlock authority's wire ids). Palette: the labels
// reuse the Hud gauge colour UiTheme.GaugeColor exactly — zero new colours
// (2f palette rule; S6 pulled the literal into the UiTheme token home).
namespace LastAnimal.Ui;

/// <summary>
/// Toggle panel over the skill set: one row per skill (unlocked → name +
/// Manna cost, locked → name + "locked"), plus the current Manna readout.
/// View-only; content is derived from the live sources on every Refresh.
/// </summary>
public partial class SkillsPanel : Control
{
    /// <summary>Panel rect on the 1152x648 play screen (fixed, deterministic:
    /// the ui_test.sh framebuffer leg crops exactly this region).</summary>
    public const int RegionX = 8;
    public const int RegionY = 440;
    public const int RegionWidth = 420;
    public const int RegionHeight = 260;

    private Label? _headerLabel;
    private Label? _strikeLabel;
    private Label? _mendLabel;
    private Label? _calmLabel;
    private Label? _mannaLabel;

    // 2f live sources (plan §G D4): invoked at every Refresh, never copied.
    private Func<SkillUnlocks>? _liveUnlocks;
    private Func<int>? _liveManna;

    private EventBus? _bus;

    public SkillsPanel()
    {
        // Closed at boot is the panel's intrinsic default (EmpathyPanel
        // idiom, MC 1348 A4): the TAB toggle keys on Visible, so the bare
        // Control must never sit visible from boot.
        Visible = false;
    }

    /// <summary>
    /// Subscribe the batched 2c signals this panel may listen to
    /// (QuestCompleted, SkillUsed) so its rows follow the game from the bus.
    /// I4 seam: composition sites hand the View the bus — no autoload lookup.
    /// </summary>
    public void ConnectBus(EventBus bus)
    {
        if (_bus == bus) return;
        if (_bus != null) DisconnectBus(_bus);
        _bus = bus;
        _bus.QuestCompleted += OnBusRefresh;
        _bus.SkillUsed += OnBusRefresh;
    }

    /// <summary>Unsubscribe from the bus (Hud idiom; teardown-safe).</summary>
    public void DisconnectBus(EventBus bus)
    {
        bus.QuestCompleted -= OnBusRefresh;
        bus.SkillUsed -= OnBusRefresh;
    }

    private void OnBusRefresh(string _) => Refresh();

    public override void _ExitTree()
    {
        if (_bus != null)
        {
            DisconnectBus(_bus);
            _bus = null;
        }
        base._ExitTree();
    }

    /// <summary>
    /// Bind the live sources: the unlock verdict and the current Manna. They
    /// are re-invoked at every Refresh — the panel never stores their values
    /// (plan §G D4).
    /// </summary>
    public void SetLiveSource(Func<SkillUnlocks> liveUnlocks, Func<int> liveManna)
    {
        _liveUnlocks = liveUnlocks ?? throw new ArgumentNullException(nameof(liveUnlocks));
        _liveManna = liveManna ?? throw new ArgumentNullException(nameof(liveManna));
        Refresh();
    }

    /// <summary>TAB arm (director Ui partial): flip open/closed; opening
    /// re-reads the live sources so the first painted frame is fresh.</summary>
    public void Toggle()
    {
        Visible = !Visible;
        Refresh();
    }

    /// <summary>Re-derive every row from the live sources (no-op safe before
    /// Bind; empty sources paint the locked/zero floor, never a stale row).</summary>
    public void Refresh()
    {
        BuildIfNeeded();
        var unlocks = _liveUnlocks?.Invoke() ?? default;
        int manna = _liveManna?.Invoke() ?? 0;
        _strikeLabel!.Text = RowFor(PlayerMutations.InvertStrikeId, unlocks.InvertStrike,
                                    SkillState.InvertStrikeCost);
        _mendLabel!.Text = RowFor(PlayerMutations.MendId, unlocks.Mend,
                                  SkillState.MendCost);
        _calmLabel!.Text = RowFor(PlayerMutations.CalmingSpeakId, unlocks.CalmingSpeak,
                                  SkillState.CalmingSpeakCost);
        _mannaLabel!.Text = $"Manna: {manna}/100";
    }

    /// <summary>One authored row: unlocked → name + cost, locked → name +
    /// "locked". Content is derived from the state, never a fixture.</summary>
    private static string RowFor(string skillId, bool unlocked, int cost) =>
        unlocked ? $"{DisplayName(skillId)} — {cost} Manna" : $"{DisplayName(skillId)} — locked";

    /// <summary>Stable wire id → player-facing name (presentation mapping,
    /// shared with the HUD Skills row — one implementation).</summary>
    public static string DisplayName(string skillId) => skillId switch
    {
        PlayerMutations.InvertStrikeId => "Invert Strike",
        PlayerMutations.MendId => "Mend",
        PlayerMutations.CalmingSpeakId => "Calming Speak",
        _ => skillId,
    };

    private void BuildIfNeeded()
    {
        if (_headerLabel != null) return;

        // Fixed rect (see RegionX/Y constants): the framebuffer leg of
        // ci/ui_test.sh crops exactly this box for the TAB pixel diff.
        Position = new Vector2(RegionX, RegionY);
        Size = new Vector2(RegionWidth, RegionHeight);
        CustomMinimumSize = Size;

        var panel = new Panel { Name = "SkillsPanelRoot" };
        panel.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(panel);

        _headerLabel = MakeRow("SkillsHeader", "Skills", 8);
        _strikeLabel = MakeRow("StrikeRow", string.Empty, 42);
        _mendLabel = MakeRow("MendRow", string.Empty, 76);
        _calmLabel = MakeRow("CalmRow", string.Empty, 110);
        _mannaLabel = MakeRow("MannaRow", string.Empty, 154);
        foreach (var label in new[] { _headerLabel, _strikeLabel, _mendLabel, _calmLabel, _mannaLabel })
            panel.AddChild(label);
    }

    private static Label MakeRow(string name, string text, float y)
    {
        var label = new Label { Name = name, Text = text, Position = new Vector2(10, y) };
        label.AddThemeFontSizeOverride("font_size", UiTheme.GaugeFontSize);
        label.Modulate = UiTheme.GaugeColor;   // the Hud gauge colour — zero new colours
        return label;
    }
}
