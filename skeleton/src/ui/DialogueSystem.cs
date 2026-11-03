using Godot;
using LastAnimal.Story;
using System.Collections.Generic;

// Last Animal — M10 ui-hud (MC 890.14, dobbie, 2026-09-06).
// MC 3900 stage 2a: the text source moved from a private hardcoded switch to
// the injected engine-free DialogueTable (src/story/); the Show/Close/
// ActiveNode contract and the never-blank fallback are unchanged.
// MC 3915: Show gains the fromReward reward-beat flag (ActiveNodeIsReward);
// the one-arg Show contract is byte-identical for existing callers.
// MC 10026.1 (DESIGN rev 4): reward beats QUEUE in _pending in emission
// order (a new beat takes a mid-read head, which re-queues for a full dwell);
// EVERY line — beat or direct — auto-closes after its dwell ("All dialogue
// lines"). No wallclock; queue never persisted (R9 resets presentation on load).
// MC 11493: ShowText paints CALLER-COMPOSED text (a live DNA creature reply,
// talk-to-creatures) through the SAME band, rect and dwell — the MC 11293
// layout is untouched; the View still authors nothing, the caller supplies text.
//
// C13 (PHASE0.md line 382): `DialogueSystem.Show(nodeId)`.
// The dialogue box view: a Godot Control that, given a node id, shows that
// node's dialogue text in an on-screen box and reports which node is active.
// A View (I4 seam): it renders text the gameplay modules ask it to show; it
// owns no dialogue-authoring or branching logic.
//
// The headless test (tests/ui/UiRenderTest.cs) drives Show() and proves the
// box renders (non-blank framebuffer via graphical-test-helper) AND that the
// active node id mirrors the last Show() call (Close() resets to none).
namespace LastAnimal.Ui;

/// <summary>
/// On-screen dialogue box (C13). `Show(nodeId)` renders the dialogue for a
/// node and makes that node the active one; `Close()` dismisses it.
/// </summary>
public partial class DialogueSystem : Control
{
    /// <summary>The node id currently on screen, or empty when closed.</summary>
    public string ActiveNode { get; private set; } = string.Empty;

    /// <summary>MC 3915 guard: the node currently on screen was shown as a
    /// quest REWARD beat, not as dialogue observation — the quest observer
    /// must never treat a reward beat as a DialogueShown fact.</summary>
    public bool ActiveNodeIsReward { get; private set; }

    /// <summary>
    /// The authored dialogue table this View reads (injected by the
    /// composition root). Null falls back to DialogueTable.Default() so the
    /// box still paints authored text — the View owns no dialogue text either
    /// way; only the fallback line below is its own.
    /// </summary>
    public DialogueTable? Table { get; set; }

    private Label? _textLabel;
    private static DialogueTable? _defaultTable;

    /// <summary>MC 10026.1 TTL in painted ticks (4 s @60 fps; DESIGN §3).</summary>
    public const int RewardLineFrames = 240;

    // DESIGN §2. Invariant: while _displayedIsQueued, on-screen node == _pending[0].
    private readonly List<string> _pending = new();
    private bool _displayedIsQueued;
    private int _dwell;         // painted ticks (§2.4 frame account)
    private bool _paintedHere;  // paint-tick guard

    /// <summary>
    /// Open a dialogue node on screen: record it as active and paint its text.
    /// MC 3915: fromReward marks the show as a quest REWARD beat (the default
    /// keeps every existing one-arg caller byte-identical).
    /// MC 10026.1 (DESIGN §2.1): branch order EXACT, first match wins.
    /// </summary>
    public void Show(string nodeId, bool fromReward = false)
    {
        // R1 direct: paint as today (rev-4 F-L sets the paint flag too).
        if (!fromReward)
        {
            ActiveNode = nodeId ?? string.Empty;
            ActiveNodeIsReward = false;
            BuildIfNeeded();
            EnsureVisible();
            _textLabel!.Text = DialogueFor(ActiveNode);
            _displayedIsQueued = false;
            _dwell = 0;
            _paintedHere = true;
            return;
        }
        // R2 reward, box free (R6 folded): append + present IN THE CALL.
        if (!IsOpen || (!_displayedIsQueued && _dwell >= RewardLineFrames))
        {
            _pending.Add(nodeId);
            presentHead();
            return;
        }
        // R3 reward PREEMPTS a mid-read head: new TAKES the head, old re-queues
        // at the tail for its full dwell (MoveItem(0,end) = Add+RemoveAt).
        if (_displayedIsQueued && _dwell > 0 && _dwell < RewardLineFrames)
        {
            _pending.Add(_pending[0]);
            _pending.RemoveAt(0);
            _pending.Insert(0, nodeId);
            presentHead();
            return;
        }
        // R4 same tick as a fresh queued head: append only (strict emission order).
        if (_displayedIsQueued && _dwell == 0)
        {
            _pending.Add(nodeId);
            return;
        }
        _pending.Add(nodeId);   // R5 behind a FRESH direct line: append only (F-2 clobber guard)
    }

    /// <summary>
    /// MC 11493 (talk-to-creatures): paint CALLER-SUPPLIED text into the SAME
    /// band with the SAME direct-line semantics as R1 — the View renders text
    /// gameplay asks it to show and still owns no dialogue authoring; only here
    /// the CALLER composes the text (a live creature reply), so no node id is
    /// authored in the table. ActiveNode mirrors nodeId (the proofs' node-keyed
    /// reads keep working), the auto-close dwell applies ("All dialogue lines").
    /// </summary>
    public void ShowText(string nodeId, string text)
    {
        ActiveNode = nodeId ?? string.Empty;
        ActiveNodeIsReward = false;
        BuildIfNeeded();
        EnsureVisible();
        _textLabel!.Text = text;
        _displayedIsQueued = false;
        _dwell = 0;
        _paintedHere = true;
    }

    /// <summary>Paint _pending[0] flagged reward; reset the dwell (§2).</summary>
    private void presentHead()
    {
        ActiveNode = _pending[0];
        ActiveNodeIsReward = true;
        BuildIfNeeded();
        EnsureVisible();
        _textLabel!.Text = DialogueFor(ActiveNode);
        _displayedIsQueued = true;
        _dwell = 0;
        _paintedHere = true;
    }

    /// <summary>R9: load-time view reset (the queue is never persisted).</summary>
    public void ClearPresentation()
    {
        _pending.Clear();
        _displayedIsQueued = false;
        Close();
    }

    public override void _Process(double delta)
    {
        // Idle box: O(1). DA P3-1: keyed on IsOpen, NOT on Visible.
        if (!IsOpen && _pending.Count == 0) return;
        if (!_paintedHere) _dwell++;                    // paint-tick guard (§2.4)
        if (_pending.Count > 0)
        {
            if (_displayedIsQueued && _dwell >= RewardLineFrames)   // THE ONLY POP POINT
            {
                _pending.RemoveAt(0);
                // rev-4 F-M: NO Close here — the branch below is the ONLY one.
                if (_pending.Count == 0) { _displayedIsQueued = false; }
                else presentHead();
            }
            else if (!_displayedIsQueued && (!IsOpen || _dwell >= RewardLineFrames))
                presentHead();                          // drain into a box freed by auto-close/direct
        }
        else if (IsOpen && _dwell >= RewardLineFrames)
            Close();     // UNIFORM auto-close — "All dialogue lines"; the ONLY Close caller in _Process
        _paintedHere = false;                           // LAST statement of _Process — load-bearing
    }

    /// <summary>Dismiss the dialogue box (clears the active node).</summary>
    public void Close()
    {
        ActiveNode = string.Empty;
        ActiveNodeIsReward = false;
        Visible = false;
    }

    /// <summary>Is a dialogue node currently on screen?</summary>
    public bool IsOpen => ActiveNode.Length > 0;

    // --- the dialogue box rect (MC 11293 D1; MC 11891 live-rect reflow) ----
    // Bottom band, RIGHT of the SkillsPanel column (its x ends at
    // SkillsPanel.RegionX+RegionWidth — MC 11303 F-DA4: the coupling is that
    // arithmetic, NOT a literal 428; leg J's TAB_DIALOGUE_DISJOINT asserts the
    // two painted rects, so a Region drift goes RED, never silent): the
    // playthrough P1 (MC 11270 D1) was this box's anchors collapsing to a
    // zero-sized Control — the Label then overflowed-painted the intro/dialogue
    // line straight over the top-left HUD gauges, both layers unreadable. The
    // fix gave the box a REAL rect (so the Panel paints a designed surface).
    // MC 11891 (owner ruling B, 2026-11-03 — verbatim: "B", choosing "reflow
    // the dialogue band to fit whatever window the player has" over a minimum
    // window size): the rect is now DERIVED from the live visible viewport
    // (LayoutBand). This REPLACES — and for this band STANDS DOWN — the
    // UiTheme.PanelViewport constant-span fallback: below 1152 that fallback
    // pinned the band at x 440..1144 whatever the window really was, so the
    // window edge cut the tail of every dialogue line (MC 11303 F-P1-1). NO
    // minimum window size is introduced; the right edge can no longer exceed
    // the window. At the shipped play window (1152x648) the derivation is
    // byte-identical to the old fallback's result: x 440..1144, y 540..640,
    // disjoint from the HUD block (ends ~y 176, x ~427) AND from the TAB panel.
    public const int BoxWidth = 704;
    public const int BoxHeight = 100;
    public const int BoxEdge = 8;      // viewport margin, both axes
    public const int TextPad = 14;     // label inset inside the panel rect
    // Floor of the DERIVED band width: the smallest paintable surface (a
    // zero-sized control is the MC 11293 P1, never again). This is NOT a
    // minimum window size — the game runs at ANY width (ruling B); it is only
    // the point where a sub-112 px window stops shrinking the band instead of
    // collapsing it to nothing a player could read.
    public const int MinBandWidth = 96;

    private void BuildIfNeeded()
    {
        if (_textLabel != null) return;

        var panel = new Panel { Name = "DialoguePanel" };
        panel.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(panel);

        _textLabel = new Label { Name = "DialogueText", Position = new Vector2(TextPad, 10) };
        _textLabel.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        _textLabel.AddThemeFontSizeOverride("font_size", 22);
        _textLabel.Modulate = new Color(1.0f, 1.0f, 0.9f);
        panel.AddChild(_textLabel);

        LayoutBand();   // live-rect geometry (MC 11891) — set before first paint
        // The player may resize at any time: re-derive on every viewport
        // resize so the right edge can never exceed the current window.
        var vp = GetViewport();
        if (vp != null) vp.SizeChanged += LayoutBand;   // Action — size_changed carries no args
    }

    /// <summary>MC 11891 (owner ruling B): derive the band rect from the LIVE
    /// visible viewport so its right edge can never exceed the window. While
    /// the window can hold the FULL band right of the TAB column the band is
    /// the shipped bottom-right rect (byte-identical at the play window,
    /// 1152x648: x 440..1144, y 540..640); when it cannot, the band takes the
    /// full window width ABOVE the TAB panel (clear of the HUD block, which
    /// ends ~y 176) instead of clipping off-screen. The label wrap width is
    /// derived with the band (reflow), never left at the constant.
    /// Ceiling (named, not hidden): at widths narrow enough to wrap the text
    /// past BoxHeight the wrapped tail spills below the band surface — still
    /// inside the window; the disjointness/in-view bars (ci/ui_test.sh leg J)
    /// run at the shipped default and the planted 800 px width, where the
    /// shipped table's longest line (77 chars) never wraps past the band.</summary>
    private void LayoutBand()
    {
        var vis = GetViewport()?.GetVisibleRect().Size ?? Vector2.Zero;
        int bandW = Mathf.Min(BoxWidth, Mathf.Max(MinBandWidth, (int)vis.X - 2 * BoxEdge));
        bool rightOfTabColumn =
            (int)vis.X - BoxEdge - bandW >= SkillsPanel.RegionX + SkillsPanel.RegionWidth + BoxEdge;
        Position = rightOfTabColumn
            ? new Vector2((int)vis.X - BoxEdge - bandW,
                          Mathf.Max(BoxEdge, (int)vis.Y - BoxHeight - BoxEdge))
            : new Vector2(BoxEdge,
                          Mathf.Max(BoxEdge, Mathf.Min(SkillsPanel.RegionY - BoxEdge - BoxHeight,
                                                       (int)vis.Y - BoxHeight - BoxEdge)));
        Size = new Vector2(bandW, BoxHeight);
        CustomMinimumSize = Size;
        _textLabel!.CustomMinimumSize = new Vector2(bandW - 2 * TextPad, 0);
    }

    private void EnsureVisible() => Visible = true;

    /// <summary>
    /// Map a node id to its on-screen text via the injected data table (the
    /// script/data layer this View was always meant to read). The never-blank
    /// bar is kept: a node the table does not author (e.g. the production
    /// trigger's un-authored npc_&lt;id&gt; nodes) still renders the diegetic
    /// fallback line.
    /// </summary>
    private string DialogueFor(string nodeId) =>
        (Table ?? (_defaultTable ??= DialogueTable.Default())).FindText(nodeId)
        ?? $"Dialogue node [{nodeId}].";
}
