using Godot;
using LastAnimal.Story;

// Last Animal — M10 ui-hud (MC 890.14, dobbie, 2026-09-06).
// MC 3900 stage 2a: the text source moved from a private hardcoded switch to
// the injected engine-free DialogueTable (src/story/); the Show/Close/
// ActiveNode contract and the never-blank fallback are unchanged.
// MC 3915: Show gains the fromReward reward-beat flag (ActiveNodeIsReward);
// the one-arg Show contract is byte-identical for existing callers.
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

    /// <summary>
    /// Open a dialogue node on screen: record it as active and paint its text.
    /// MC 3915: fromReward marks the show as a quest REWARD beat (the default
    /// keeps every existing one-arg caller byte-identical).
    /// </summary>
    public void Show(string nodeId, bool fromReward = false)
    {
        ActiveNode = nodeId ?? string.Empty;
        ActiveNodeIsReward = fromReward;
        BuildIfNeeded();
        EnsureVisible();
        _textLabel!.Text = DialogueFor(ActiveNode);
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
