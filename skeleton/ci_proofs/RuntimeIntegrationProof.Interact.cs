using Godot;
using LastAnimal.Ui;

// Last Animal — T3b runtime integration proof, INTERACT + NO_SPAWN stages
// (MC 1256.10; dna_speak/no_interact per MC 1344 DA findings C2/C4/C13).
//
// Partial-class half of RuntimeIntegrationProof carrying the dna_speak /
// no_interact stage body (stage 40: interact -> DnaLanguage.Speak ->
// EventBus.DnaSpoken -> DialogueSystem.Show) and the no_spawn emptiness
// assert (stage 90). Extracted verbatim from RuntimeIntegrationProof.cs —
// no behaviour change; see the main file's header for the mode contract.
public partial class RuntimeIntegrationProof : SceneTree
{
    // ---- dna_speak / no_interact: the interact -> Speak -> DnaSpoken
    // -> DialogueSystem production path (MC 1344 DA findings C2/C4/C13).
    private void RunInteractStage()
    {
        if (_dnaSpokenCount > 0)
        {
            Input.ActionRelease("interact");
            if (_mode == "no_interact")
            {
                Fail("no_interact: DnaSpoken reached the bus despite the interact seam disabled — the negative control is broken");
                return;
            }
            Check("DnaSpoken fired on the REAL EventBus from the interact path",
                  _dnaSpokenCount > 0, $"spoken={_dnaSpokenCount}");
            if (_failed) return;
            GD.Print("LA_GATE: DNA_SPOKEN_EMITTED — interact -> DnaLanguage.Speak -> EventBus.DnaSpoken (real autoload bus)");
            // MC 3915 DA F2: keyed to the interact path's OWN npc_ node — the
            // boot q_intro reward beat opens the box at "intro" in every
            // hooks-on mode, so a generic non-empty node could pass on that
            // reward instead of the interact Show. WorldDirector.cs TryInteract
            // shows "npc_{id}"; the no_interact twin below keys the same way.
            Check("DialogueSystem opened the interact path's own npc_ node",
                  _dialogue!.IsOpen && _dialogue.ActiveNode.StartsWith("npc_"),
                  $"node='{_dialogue.ActiveNode}'");
            if (_failed) return;
            GD.Print("LA_GATE: DIALOGUE_SHOWN — DialogueSystem.Show rendered the spoken NPC's node");
            GD.Print("LA_GATE: PASS — DNA-speak + dialogue production path verified");
            _asserted = true;
            _stage = 6;
            _stageFrames = 0;
            _holdStartPhys = _physFrames;
            return;
        }
        if (_stageFrames > AttackBudgetFrames)
        {
            if (_mode == "no_interact")
            {
                // The seam is off: no DnaSpoken AND no INTERACT-path dialogue
                // is the DETECTED break (the control must be able to fail).
                // MC 3915: the boot q_intro REWARD beat legitimately opens the
                // box at scene start — only the interact path's own npc_ node
                // can signal a live seam (dna_speak mode proves it shows one).
                if (_dialogue!.IsOpen && _dialogue.ActiveNode.StartsWith("npc_"))
                {
                    Fail("no_interact: dialogue opened despite the interact seam disabled — the negative control is broken");
                    return;
                }
                GD.Print("LA_GATE: NEG_INTERACT: interact pressed near the NPC but no DnaSpoken fired and no interact-path dialogue opened (seam disabled) — break detected");
                Quit(1);
                return;
            }
            Fail("interact did not fire DnaSpoken within the budget (input -> director -> Speak -> bus path broken)");
            return;
        }
        // Press interact 2 frames, release 2, repeat (attack-stage idiom).
        _interactToggle++;
        if (_interactToggle % 4 == 1) Input.ActionPress("interact");
        else if (_interactToggle % 4 == 3) Input.ActionRelease("interact");
    }

    // ---- no_spawn mode: assert the scene stayed empty ----
    private void RunNoSpawnStage()
    {
        if (_stageFrames >= 10)
        {
            int live = 0;
            foreach (var e in _enemies) if (!e.IsDead) live++;
            if (live > 0)
            {
                Fail("no_spawn: enemies exist despite spawning disabled — the negative control is broken");
                return;
            }
            GD.Print("LA_GATE: NEG_SPAWN: no EnemyActor in scene; chain cannot start — break detected");
            Quit(1);
        }
    }
}
