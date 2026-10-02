using Godot;
using LastAnimal.Story;
using LastAnimal.Ui;

// Last Animal — story exact-text capture proof (MC 3900 stage 2a, code,
// 2026-10-02). Headless SceneTree script compiled by the ENGINE project
// (it drives the real DialogueSystem view), run by ci/ui_test.sh leg D.
//
// Standing ruling (PLAN 2a / DA-c1 P3-12): a non-blank bar is NOT enough —
// this proof asserts EXACT painted-text equality: the dialogue Label's Text
// equals the authored DialogueTable string for a NEW authored node, read back
// from the live node tree (DialoguePanel/DialogueText) after real process
// frames. It also pins the two behaviours the spec freezes: Close() clears
// the active node and hides the box, and an un-authored id (the production
// trigger shows npc_<id>) still renders the diegetic fallback — never blank.
//
// Run: godot --headless --path <proj> --script res://tests/story/StoryTextCaptureTest.cs
public partial class StoryTextCaptureTest : SceneTree
{
    // The node under test and its authored string, kept as a fixture here so
    // removing/rewording the node in the table turns THIS leg red and names
    // the node (planted-bad capability of the ui gate).
    private const string Node = "first_speak";
    private const string Authored = "You answer in its own tongue. It listens, for now.";

    private int _failures;
    private int _frame;
    private DialogueSystem? _dialogue;

    public override void _Initialize()
    {
        GD.Print("LA_STORY_CAPTURE: start");
        var canvas = new CanvasLayer { Name = "UiCanvas" };
        Root.AddChild(canvas);
        _dialogue = new DialogueSystem { Name = "Dialogue", Table = DialogueTable.Default() };
        canvas.AddChild(_dialogue);
    }

    public override bool _Process(double delta)
    {
        // Show on frame 1, assert on frame 2: the label goes through real
        // process frames before the text is read back (UiRenderTest idiom).
        if (_frame == 1) _dialogue!.Show(Node);
        if (_frame == 2)
        {
            var label = _dialogue!.GetNodeOrNull<Label>("DialoguePanel/DialogueText");
            CheckExact("exact painted text", Node, Authored, label?.Text);
            Check("ActiveNode mirrors Show", _dialogue.ActiveNode == Node,
                  $"active={_dialogue.ActiveNode}");

            // View contract frozen by the spec: Close clears + hides.
            _dialogue.Close();
            Check("Close clears ActiveNode and hides the box",
                  _dialogue.ActiveNode == string.Empty && !_dialogue.Visible,
                  $"active='{_dialogue.ActiveNode}' visible={_dialogue.Visible}");

            // Fallback-never-blank, UNCHANGED: un-authored production node.
            _dialogue.Show("npc_99");
            var fb = _dialogue.GetNodeOrNull<Label>("DialoguePanel/DialogueText");
            CheckExact("fallback never blank", "npc_99", "Dialogue node [npc_99].", fb?.Text);

            GD.Print(_failures == 0
                ? $"LA_STORY_CAPTURE: PASS — authored node '{Node}' painted its exact string; Close/fallback contract holds"
                : $"LA_STORY_CAPTURE: FAIL ({_failures} check(s) failed)");
            Quit(_failures == 0 ? 0 : 1);
        }
        _frame++;
        return false;
    }

    private void CheckExact(string what, string node, string expected, string? actual)
    {
        bool ok = actual == expected;
        GD.Print($"LA_STORY_CAPTURE: {what}: node={node} {(ok ? "ok" : "FAIL")} " +
                 $"expected=[{expected}] actual=[{actual}]");
        if (!ok) _failures++;
    }

    private void Check(string what, bool ok, string detail)
    {
        GD.Print($"LA_STORY_CAPTURE: {what}: {(ok ? "ok" : "FAIL")} {detail}");
        if (!ok) _failures++;
    }
}
