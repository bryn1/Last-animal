using Godot;
using LastAnimal.Companion;
using LastAnimal.Core;
using LastAnimal.Ui;
using LastAnimal.World;
using System.Collections.Generic;

// Last Animal — T3b runtime integration proof, shared stage-state fields
// (MC 11088 S48 FIX-ROUND-1 SPLIT, MC 10218 verbatim-move precedent).
//
// The entry's shared machine fields + movement/budget consts (MoveFrames ..
// _zoneBeforeSave) moved HERE VERBATIM — the entry crossed the 600 house
// ceiling at 620 with MC 11088's SKY_STRIP mode-doc rows + stage-109 arm, and
// §6's law pays every entry growth with a verbatim-move split IN-CARD (the
// MC 11019/S48 STEP-0 double split is the precedent). Zero behaviour change:
// a partial class holds every field wherever the declaration file sits; the
// entry keeps what mode_sets_check parses ENTRY-SCOPED by design (mode doc,
// KnownModes, the mode read + fail-safe guard, the stage dispatch). Every
// field is read by the entry's dispatch or by a stage partial — dead_code.py
// runs clean on this file. The full battery re-run at this head is the proof.
public partial class RuntimeIntegrationProof : SceneTree
{
    private const int MoveFrames = 12;
    // MC 1344.1: movement is applied in _PhysicsProcess ticks. Headless --script
    // process frames are uncapped, so counting process frames measures wall-clock
    // frames, not engine time. Count physics ticks instead (BridgeMvpProof pattern).
    private int _physFrames;
    private int _pressPhysFrame;
    private int _followStartPhys; // physics tick when the follow baseline was captured (MC 1345)
    private int _holdStartPhys;   // physics tick when the render-bar hold began (MC 1344.1)
    private const int AttackBudgetFrames = 240;   // input-path kill budget
    private const int FollowFrames = 40;
    private const int HoldFrames = 1200;          // post-PASS hold for the render bar:
    // 1200 physics-capped frames @60fps = 20s > the helper's 15s capture window.
    // 600 (10s) raced the capture — the proof quit before the framebuffer grab
    // and the render bar read a blank screen (MC 1344.1, flaky in runtime32).
    private const int FrameBudget = 2400;

    private EventBus? _bus;
    private WorldDirector? _director;
    private CharacterBody3D? _playerBody;
    private Hud? _hud;
    private DialogueSystem? _dialogue;
    private CompanionEntity? _companion;
    private readonly List<EnemyActor> _enemies = new();

    private string _mode = "positive";
    private int _frames;
    private int _stage;
    private int _stageFrames;
    private Node3D? _main;      // composition root, resolved in _Initialize (MC 1344.1)
    private bool _composed;     // _ComposeDeferred has run (MC 1344.1)
    private bool _failed;
    private bool _asserted;
    private int _dnaCount;
    private int _dnaSpokenCount;
    private int _dnaBefore;
    private int _attackToggle;
    private int _interactToggle;
    private Vector3 _playerStart;
    private Vector3 _companionStart;
    private float _followDist0;
    private int _kills;
    private int _loyaltyBeforeSave;
    private int _dnaMeterBeforeSave;
    private int _spokenDnaBeforeSave;
    private int _progressBeforeSave;
    private int _dnaMutated;
    private string _zoneBeforeSave = "";
}
