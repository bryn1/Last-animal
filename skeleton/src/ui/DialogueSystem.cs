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

    private void BuildIfNeeded()
    {
        if (_textLabel != null) return;
        SetAnchorsPreset(LayoutPreset.CenterBottom);
        var panel = new Panel { Name = "DialoguePanel" };
        panel.SetAnchorsPreset(LayoutPreset.FullRect);
        AddChild(panel);

        _textLabel = new Label { Name = "DialogueText" };
        _textLabel.SetAnchorsPreset(LayoutPreset.FullRect);
        _textLabel.AddThemeFontSizeOverride("font_size", 22);
        _textLabel.Modulate = new Color(1.0f, 1.0f, 0.9f);
        panel.AddChild(_textLabel);
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
