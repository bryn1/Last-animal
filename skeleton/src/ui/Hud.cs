using Godot;
using LastAnimal.Core;
using LastAnimal.Core.Framework;
using LastAnimal.Dna;
using System;

// Last Animal — M10 ui-hud (MC 890.14, dobbie, 2026-09-06).
// MC 3933 stage 2f: two extra readout rows — learned-skill names + the ACTIVE
// quest line — fed through BindLive, and the Manna digits move to the LIVE
// source when one is bound. Plan §G D4 (RATIFIED): the View re-invokes the
// live provider functions on EVERY redraw; it keeps no state copy of its own,
// so there is nothing here that can go stale (tests/ui SkillsPanelTest
// freshness leg plants exactly that defect and goes RED on it).
//
// C13 (PHASE0.md line 382): `Hud.Bind(Life, Manna, DnaMeter, CompanionHearts)`.
// The head-up display: the player's four readouts, rendered as a Godot Control
// tree of Labels. It is a View (I4 seam): it binds the C2 EventBus so its
// gauge values move when the game speaks, and it owns NO gameplay logic.
//
//   Life / Manna          — driven by combat (M08). There is no C2 signal in
//                           the contract for these, so the HUD exposes
//                           UpdateLife/UpdateManna for the combat seam to call.
//   DnaMeter              — updated FROM the bus: every DnaExtracted /
//                           DnaSpoken fires and bumps the meter the player
//                           reads before speaking.
//   CompanionHearts       — updated FROM the bus: every LoyaltyChanged fires
//                           and re-draws the hearts for that companion.
//
// The Headless test (tests/ui/UiRenderTest.cs) drives every one of these and
// asserts the rendered labels reflect the bus-updated values.
namespace LastAnimal.Ui;

/// <summary>
/// The heads-up display (C13). Renders Life / Manna / DnaMeter /
/// CompanionHearts as child Labels, plus the 2f rows: learned-skill names
/// and the ACTIVE quest line. Two gauges move on the C2 EventBus
/// (DnaMeter, CompanionHearts); Life/Manna move through the combat seam.
/// Once <see cref="BindLive"/> is wired the Manna digits and the 2f rows are
/// re-read from the live providers on every redraw (plan §G D4 — no cache).
/// </summary>
public partial class Hud : Control
{
    // --- the four bound readouts -----------------------------------------
    public int Life { get; private set; }
    public int Manna { get; private set; }
    public int DnaMeter { get; private set; }
    public int CompanionHearts { get; private set; }

    private Label? _lifeLabel;
    private Label? _mannaLabel;
    private Label? _dnaLabel;
    private Label? _heartsLabel;
    private Label? _skillsLabel;
    private Label? _questLabel;

    // 2f live sources (plan §G D4): re-invoked at every redraw, never copied
    // into a field — the View holds providers, not values.
    private Func<int>? _liveManna;
    private Func<SkillUnlocks>? _liveUnlocks;
    private Func<string>? _liveQuest;

    private EventBus? _bus;

    /// <summary>
    /// Bind the four values that seed the gauges. The bus is wired separately
    /// via <see cref="ConnectBus"/> so composition sites (game scene vs headless
    /// test) choose how the bus reaches the HUD — no fragile absolute-path
    /// autoload lookup inside a View (I4 seam).
    /// </summary>
    public void Bind(int life, int manna, int dnaMeter, int companionHearts)
    {
        Life = life;
        Manna = manna;
        DnaMeter = dnaMeter;
        CompanionHearts = companionHearts;

        BuildControls();
        Redraw();
    }

    /// <summary>
    /// 2f live seam (plan §G D4): bind the sources the readouts are re-drawn
    /// FROM. The three provider functions are invoked fresh at EVERY redraw —
    /// the HUD never stores their values, so there is no copy here to stale
    /// out. Composition sites inject closures over the live systems (the
    /// WorldDirector Ui partial does this in production; tests/ui injects
    /// mutable fixtures and proves the labels track the fixture).
    /// </summary>
    public void BindLive(Func<int> liveManna, Func<SkillUnlocks> liveUnlocks, Func<string> liveQuest)
    {
        _liveManna = liveManna ?? throw new ArgumentNullException(nameof(liveManna));
        _liveUnlocks = liveUnlocks ?? throw new ArgumentNullException(nameof(liveUnlocks));
        _liveQuest = liveQuest ?? throw new ArgumentNullException(nameof(liveQuest));
        BuildControls();
        Redraw();
    }

    /// <summary>
    /// 2f refresh seam: pull the live providers once more and repaint. The
    /// production driver is the director's per-frame TickUi; bus events also
    /// redraw through the existing handlers. No manual poke needed anywhere
    /// else — that is the freshness contract the tests/ui leg pins.
    /// </summary>
    public void RefreshLive()
    {
        BuildControls();
        Redraw();
    }

    /// <summary>
    /// Subscribe to the C2 EventBus so DnaMeter (DnaExtracted/DnaSpoken) and
    /// CompanionHearts (LoyaltyChanged) follow the game from the bus.
    /// </summary>
    public void ConnectBus(EventBus bus)
    {
        if (_bus == bus) return;
        if (_bus != null) DisconnectBus(_bus);
        _bus = bus;
        _bus.DnaExtracted += OnDnaEvent;
        _bus.DnaSpoken += OnDnaEvent;
        _bus.LoyaltyChanged += OnLoyaltyChanged;
        Redraw();
    }

    /// <summary>Unsubscribe from the bus on teardown.</summary>
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
    /// Unsubscribe from the bus. Public since the T3b ownership refactor
    /// (MC 1256.9): the runtime gate's no_bus negative control disconnects the
    /// HUD to prove the meter freezes when the bus link is broken (design
    /// 1256.2 §4.2). No gameplay behavior — a pure unsubscribe.
    /// </summary>
    public void DisconnectBus(EventBus bus)
    {
        bus.DnaExtracted -= OnDnaEvent;
        bus.DnaSpoken -= OnDnaEvent;
        bus.LoyaltyChanged -= OnLoyaltyChanged;
    }

    /// <summary>Combat seam: set the Life readout and redraw. Clamped to [0,100].</summary>
    public void UpdateLife(int life)
    {
        Life = Clamp(life);
        Redraw();
    }

    /// <summary>Combat seam: set the Manna readout and redraw. Clamped to [0,100].</summary>
    public void UpdateManna(int manna)
    {
        Manna = Clamp(manna);
        Redraw();
    }

    /// <summary>
    /// Save/load seam (MC 1344): set the DnaMeter readout directly and redraw,
    /// mirroring UpdateLife/UpdateManna. LoadGame uses it to restore the meter
    /// to the saved event count — the bus only ever increments, so a restore
    /// cannot ride a DnaExtracted/DnaSpoken event. Clamped to [0,100].
    /// </summary>
    public void UpdateDnaMeter(int dnaMeter)
    {
        DnaMeter = Clamp(dnaMeter);
        Redraw();
    }

    private void OnDnaEvent(string signature)
    {
        DnaMeter = Mathf.Min(DnaMeter + 1, 100);
        Redraw();
    }

    private void OnLoyaltyChanged(string companion, int loyalty)
    {
        // Hearts = loyalty percent translated to 1-5 hearts, bus-driven.
        CompanionHearts = Mathf.Clamp((int)Mathf.Round(loyalty / 20f), 0, 5);
        Redraw();
    }

    private static int Clamp(int v) => Mathf.Clamp(v, 0, 100);

    private void BuildControls()
    {
        if (_lifeLabel != null) return;          // already built

        SetAnchorsPreset(LayoutPreset.FullRect);

        _lifeLabel = MakeLabel("Life");
        _mannaLabel = MakeLabel("Manna");
        _dnaLabel = MakeLabel("DNA");
        _heartsLabel = MakeLabel("Hearts");
        _skillsLabel = MakeLabel("Skills");
        _questLabel = MakeLabel("Quest");

        // Stack the readouts down the top-left corner so all are readable.
        for (int i = 0; i < 6; i++)
        {
            Label l = i switch
            {
                0 => _lifeLabel,
                1 => _mannaLabel,
                2 => _dnaLabel,
                3 => _heartsLabel,
                4 => _skillsLabel,
                _ => _questLabel
            };
            l.Position = new Vector2(8, 8 + i * 28);
            AddChild(l);
        }
    }

    private static Label MakeLabel(string title)
    {
        var label = new Label();
        label.Name = $"{title}Gauge";
        label.AddThemeFontSizeOverride("font_size", 20);
        label.Modulate = new Color(1.0f, 1.0f, 1.0f);
        return label;
    }

    private void Redraw()
    {
        if (_lifeLabel == null) return;
        _lifeLabel!.Text = $"Life: {Life}/100";
        // 2f: with a live source bound, the digits are pulled from it right
        // now; the UpdateManna push seam (2e) stays the bus-less path and
        // stays consistent because both read the same PlayerController.
        int manna = _liveManna != null ? Clamp(_liveManna()) : Manna;
        _mannaLabel!.Text = $"Manna: {manna}/100";
        _dnaLabel!.Text = $"DNA meter: {DnaMeter}";
        _heartsLabel!.Text = $"Hearts: {CompanionHearts}";
        if (_skillsLabel != null)
            _skillsLabel.Text = _liveUnlocks != null ? SkillNames(_liveUnlocks()) : string.Empty;
        if (_questLabel != null)
            _questLabel!.Text = _liveQuest != null ? _liveQuest() : string.Empty;
    }

    /// <summary>The learned-skill row, derived from the LIVE unlock verdict
    /// (plan §G D4: single source — display names are presentation only).</summary>
    private static string SkillNames(SkillUnlocks unlocks)
    {
        var ids = unlocks.Ids;
        if (ids.Count == 0) return "Skills: none";
        var names = new string[ids.Count];
        for (int i = 0; i < ids.Count; i++) names[i] = SkillsPanel.DisplayName(ids[i]);
        return "Skills: " + string.Join(", ", names);
    }
}
