using Godot;
using LastAnimal.Core;
using LastAnimal.Empathy;

// Last Animal — M10 ui-hud (MC 890.14, dobbie, 2026-09-06).
//
// C13 (PHASE0.md line 382): `EmpathyPanel.Open(BookEntry)` — the on-screen
// surface M10 provides for the M04 Empathy-Book. It opens a real BookEntry
// read from M04 (content: EmotionalState, Summary, Hint) and renders it.
// On open it ALSO emits the C2 `EmpathyBookOpened()` signal (per the PHASE0
// C2 note: "EmpathyBookOpened() arrives with M04/M10") on the EventBus.
//
// The M04 module is pure logic (I3) and never touches the bus itself; the
// panel is the caller the C9 comment anticipated. It only *renders* what C9
// already decided — it does not re-derive the state or the route.
namespace LastAnimal.Ui;

/// <summary>
/// The Empathy-Book's on-screen panel (C13). `Open(BookEntry)` surfaces a
/// real M04 BookEntry — hidden emotional state, diegetic summary, hidden
/// hint — and emits the C2 `EmpathyBookOpened()` signal when it opens.
/// </summary>
public partial class EmpathyPanel : Control
{
    private Label? _stateLabel;
    private Label? _summaryLabel;
    private Label? _hintLabel;

    /// <summary>The entry currently on the panel, or null when closed.</summary>
    public BookEntry? Current { get; private set; }

    /// <summary>The last resolution routed from the shown entry (+0.8 empathy).</summary>
    public Resolution? LastRoute { get; private set; }

    private EventBus? _bus;

    public EmpathyPanel()
    {
        // A closed book is the panel's intrinsic default (MC 1348 A4): the
        // panel only exists on screen once Open() surfaces an entry. Without
        // this the bare Control sits visible (empty) from boot, and a toggle
        // keyed on Visible would read the boot state as "open".
        Visible = false;
    }

    /// <summary>
    /// Wire the C2 EventBus this panel emits <c>EmpathyBookOpened()</c> on.
    /// Composition sites (game scene vs headless test) choose the bus — no
    /// fragile absolute-path autoload lookup inside a View (I4 seam).
    /// </summary>
    public void ConnectBus(EventBus bus)
    {
        _bus = bus;
    }

    /// <summary>
    /// Open the panel on a real M04 BookEntry and emit the C2
    /// <c>EmpathyBookOpened()</c> signal. Surfacing the M04 content is the C13
    /// bar: hidden state, summary and hint are all painted.
    /// </summary>
    public void Open(BookEntry? entry)
    {
        BuildIfNeeded();
        Current = entry;
        Visible = entry != null;
        if (entry == null) return;

        _stateLabel!.Text = entry.EmotionalState;
        _summaryLabel!.Text = entry.Summary;
        _hintLabel!.Text = $"Hint: {entry.Hint}";

        _bus?.EmitEmpathyBookOpened();

        LastRoute = LastAnimal.Empathy.EmpathyBook.RouteResolution(entry, 0.8f);
    }

    /// <summary>Dismiss the panel and clear the surfaced entry.</summary>
    public void Close()
    {
        Current = null;
        Visible = false;
    }

    // --- the book panel rect (MC 11293 D1; SkillsPanel Region idiom) -------
    // Right band, BELOW the HUD block: the playthrough P1 (MC 11270 D1) was
    // the CenterRight anchor collapsing to a zero-sized Control — the state/
    // summary/hint Labels overflow-painted at the viewport origin, directly
    // interlaced with the Life/Manna/DNA/Hearts/Skills/Quest gauges. A REAL
    // rect anchored top-right now: at the play window (UiTheme.PanelViewport
    // fallback) exactly x 844..1144, y 200..400 — clear of the HUD block
    // (ends ~y 176, x ~427), of the TAB panel (ends x 428), and of the
    // dialogue band (starts y 540). Wrap widths grew 240 -> 280 to fill the
    // panel instead of floating in it.
    public const int PanelWidth = 300;
    public const int PanelHeight = 200;
    public const int PanelEdge = 8;      // viewport margin on the right edge
    public const int PanelTop = 200;     // clear of the six HUD gauge rows

    private void BuildIfNeeded()
    {
        if (_stateLabel != null) return;

        var vp = UiTheme.PanelViewport(GetViewport()?.GetVisibleRect().Size ?? Vector2.Zero);
        Position = new Vector2(Mathf.Max(PanelEdge, (int)vp.X - PanelWidth - PanelEdge), PanelTop);
        Size = new Vector2(PanelWidth, PanelHeight);
        CustomMinimumSize = Size;

        var panel = new Panel { Name = "EmpathyPanelRoot" };
        panel.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(panel);

        _stateLabel = new Label { Name = "EmpathyState", Position = new Vector2(10, 8) };
        _stateLabel.AddThemeFontSizeOverride("font_size", UiTheme.EmpathyFontSize);

        _summaryLabel = new Label { Name = "EmpathySummary", Position = new Vector2(10, 40) };
        _summaryLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _summaryLabel.CustomMinimumSize = new Vector2(280, 60);

        _hintLabel = new Label { Name = "EmpathyHint", Position = new Vector2(10, 110) };
        _hintLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _hintLabel.CustomMinimumSize = new Vector2(280, 60);

        foreach (var label in new[] { _stateLabel, _summaryLabel, _hintLabel })
        {
            label.Modulate = UiTheme.EmpathyTextColor;
            panel.AddChild(label);
        }
    }
}
