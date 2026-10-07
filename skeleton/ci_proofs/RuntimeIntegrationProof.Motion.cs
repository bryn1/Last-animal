using Godot;
using LastAnimal.World;
using System.Collections.Generic;

// Last Animal — MC 10198 Inc-4 S14 character-life proof (stage 98, mode CHAR_MOTION).
//
// Proves the S14 contract on the LIVE playable scene:
//   1. ENEMY WALK — a chasing goblin's composed visual bobs/leans OFF base
//      while its sim body moves, driven by the READ-ONLY SimVelocity feed the
//      motion driver reads (CHAR_ENEMY_WALK). Witnessed during the player's
//      spawn fall before anything else can stop the chase.
//   2. PLAYER WALK — the player's visual bobs/leans off base while the BODY
//      moves (real move_right wire on a probed-clear open direction), and the
//      phase driving it is an INTEGER physics-tick counter fed by velocity,
//      never delta time: the phase-vs-ticks pin lands a planted delta-time
//      phase RED (CHAR_WALK_ACTIVE).
//   3. REST — releasing the input returns the rig to its EXACT captured base
//      (walk phase 0, transforms bit-equal — no delta drift; CHAR_WALK_AT_REST).
//   4. ATTACK LEAN — one real attack press arms the lean countdown AT the ONE
//      DealDamage hunk, back EXACT base after the 6f integer decay plus the
//      breath zero-crossing (CHAR_LEAN_ACTIVE / CHAR_LEAN_AT_BASE).
//   5. PLAYER FLASH + BODY STILL — the class-swapped player VisualJuice root
//      flashes WHITE at the real player-damage site (the director's TakeDamage
//      hunk, PlayerHurt's origin), and with body physics frozen (S1 precedent)
//      the body GlobalPosition is bit-UNCHANGED across the juice window —
//      a planted motion-on-body moves it and goes RED
//      (CHAR_PLAYER_FLASH / CHAR_BODY_STILL).
//
// The leg culls the spawn set to ONE goblin through the ONE real kill path
// (CombatSystem.DealDamage + KillHide, exactly the director's TryAttack kill
// block — the DISSOLVE-leg precedent) right after the enemy-walk witness, so
// corpses never block the probe and nothing can kill the standing player.
// Markers print only after their assertion passes (harness contract).
//
// Run:  LA_GATE_MODE=CHAR_MOTION $GODOT --headless --path <proj> \
//         --script res://ci_proofs/RuntimeIntegrationProof.cs
public partial class RuntimeIntegrationProof : SceneTree
{
    private int _charPhase;
    private int _charTickMark;              // physics-tick anchor for the current phase
    private int _charPressTick;             // tick move_* was pressed
    private int _charSamplePhase, _charSampleTick;  // two-point integer-rate pin
    private Vector3 _charPlayerStart;
    private EnemyActor? _charWalker;        // nearest live enemy (walk witness, then the kept attacker)
    private Vector3 _charWalkerStart;
    private string _charWalkAction = "move_right";
    private Vector3 _charWalkDir = Vector3.Right;
    private int _charAttackToggle;
    private int _charHealthBeforeHit;
    private int _charHealthBeforeDamage;
    private Vector3 _charBodyPos;

    // Readout pacing: walk is read 6 physics ticks after the press — before any
    // per-type cycle (goblin 12, player 14) can wrap, and inside the lag band
    // of the input->velocity->phase path (a delta-time counter is >=2 behind).
    private const int CharWalkReadTicks = 6;
    private const int CharWalkLagTicks = 4;
    private const int CharSettleBudgetTicks = 240;
    private const int CharProbeBudgetTicks = 180;
    private const int CharRestWaitTicks = 140;   // breath cycle 96 + margin
    private const int CharAttackBudgetTicks = 180;
    private const int CharDamageBudgetTicks = 300;
    private const int CharReturnTicks = 8;       // 6f decay + readout lag margin

    private void MotionFail(string why) => Fail("CHAR_MOTION: " + why);

    private void RunMotionStage()
    {
        var pv = _playerBody!.GetNodeOrNull<Node3D>("Visual");
        switch (_charPhase)
        {
            case 0:   // settle on the ground + witness the goblin chase-bob
            {
                _charPlayerStart = _playerBody!.GlobalPosition;
                // Walk witness: a goblin comfortably OUTSIDE melee so it is in
                // Chase (moving) while the player settles — one already at
                // melee would be stopped (speed 0) and could not bob off base.
                if (_charWalker == null)
                {
                    EnemyActor? fallback = null;
                    foreach (var e in _director!.Enemies)
                    {
                        if (e.IsDead || !GodotObject.IsInstanceValid(e) || ReferenceEquals(e, _director!.BossActor)) continue;
                        fallback ??= e;
                        var d = e.GlobalPosition - _charPlayerStart;
                        if (new Vector2(d.X, d.Z).Length() > 1.5f) { _charWalker = e; break; }
                    }
                    _charWalker ??= fallback ?? FirstLiveEnemy();
                    if (_charWalker == null) { MotionFail("no live enemy for the walk witness"); break; }
                    _charWalkerStart = _charWalker.GlobalPosition;
                    _charTickMark = _physFrames;
                    GD.Print("LA_GATE: char motion — settling, witnessing the chase-bob");
                    break;
                }
                var w = _charWalker!;
                bool walkerMoved = GodotObject.IsInstanceValid(w) && !w.IsDead &&
                    (w.GlobalPosition - _charWalkerStart).LengthSquared() > 0.09f;
                if (walkerMoved && _director!.IsMotionVisualOffBase(w.Visual))
                {
                    GD.Print("LA_GATE: CHAR_ENEMY_WALK goblin chase-bob off base off the read-only SimVelocity feed");
                    // Cull the spawn set to the walker via the ONE real kill
                    // path — the exact kill block the director's TryAttack runs
                    // (DealDamage -> OnKill; KillHide on IsDead), so corpses
                    // leave the collision space and nothing can kill the player.
                    foreach (var e in _director!.Enemies)
                    {
                        if (e == _charWalker || e.IsDead || !GodotObject.IsInstanceValid(e)) continue;
                        _director!.Combat.DealDamage(attackerId: 0, e.Ai, 9999);
                        if (e.IsDead) e.KillHide();
                    }
                    GD.Print($"LA_GATE: char motion — spawn set culled to one goblin via the ONE DealDamage kill path (attacker hp={_charWalker!.Ai.Health})");
                    _charPhase = 1;
                    break;
                }
                if (_physFrames - _charTickMark > CharSettleBudgetTicks)
                    MotionFail("the chase-bob never appeared (walk feed not driving the rig / no chase inside budget)");
                break;
            }

            case 1:   // player settled; probe an open walk direction, press it
            {
                if (!_playerBody!.IsOnFloor())
                {
                    if (_physFrames - _charTickMark > CharSettleBudgetTicks)
                    { MotionFail("player never settled on the terrain (IsOnFloor)"); break; }
                    break;
                }
                if (!TryProbeClearDir(out _charWalkDir))
                {
                    if (_physFrames - _charTickMark > CharProbeBudgetTicks)
                    { MotionFail("no probe-clear walk direction inside budget (probe/space broken)"); break; }
                    break;   // retry next frame — bodies are moving
                }
                _charWalkAction = _charWalkDir.X > 0.5f ? "move_right"
                    : _charWalkDir.X < -0.5f ? "move_left"
                    : _charWalkDir.Z > 0.5f ? "move_down" : "move_up";
                _charPlayerStart = _playerBody!.GlobalPosition;
                _charPressTick = _physFrames;
                Input.ActionPress(_charWalkAction);
                GD.Print($"LA_GATE: char motion — {_charWalkAction} pressed (walk feed armed, dir probed clear)");
                _charPhase = 2;
                break;
            }

            case 2:   // walk-active readout at a fixed tick distance
            {
                int ticksMoving = _physFrames - _charPressTick;
                if (ticksMoving < CharWalkReadTicks) break;
                Vector3 moved = _playerBody!.GlobalPosition - _charPlayerStart;
                if (new Vector2(moved.X, moved.Z).Length() < 0.2f)
                { MotionFail($"player BODY never moved while {_charWalkAction} held (walk feed cannot be judged)"); break; }
                int phase = _director!.WalkPhaseOf(pv);
                if (phase <= 0 || phase > ticksMoving || phase < ticksMoving - CharWalkLagTicks)
                { MotionFail($"walk phase is not a velocity-fed integer tick counter (phase={phase}, ticksMoving={ticksMoving}) — the delta-time phase lands here RED"); break; }
                if (_charSamplePhase == 0)
                {
                    _charSamplePhase = phase;
                    _charSampleTick = ticksMoving;
                    break;   // second sample two ticks later: phase must ride 1/tick
                }
                // A counter advanced by anything but one-per-tick (a delta-time
                // phase is any rate != 60/s, and fast rates alias through the
                // cycle modulo) breaks this exact-delta pin even when the
                // single sample happened to land inside the lag window.
                int dPhase = phase - _charSamplePhase;
                int dTick = ticksMoving - _charSampleTick;
                if (dPhase != dTick)
                { MotionFail($"walk phase advanced {dPhase} over {dTick} physics ticks — not the integer 1-per-tick counter the F4 contract requires"); break; }
                if (!_director!.IsMotionVisualOffBase(pv))
                { MotionFail("player visual yaw/bob NOT off base while the body moves"); break; }
                GD.Print("LA_GATE: CHAR_WALK_ACTIVE player visual yaw/bob off base while the body moves (integer phase counter, velocity-fed)");
                _charPhase = 3;
                break;
            }

            case 3:   // release, then poll for the EXACT base return
            {
                Input.ActionRelease(_charWalkAction);
                _charTickMark = _physFrames;
                _charPhase = 4;
                break;
            }

            case 4:
            {
                if (_director!.IsVisualMotionAtBase(pv))
                {
                    if (_director!.WalkPhaseOf(pv) != 0)
                    { MotionFail("phase not 0 in the at-rest base state"); break; }
                    GD.Print("LA_GATE: CHAR_WALK_AT_REST EXACT base return at rest (walk phase 0, transforms bit-equal to base)");
                    _charHealthBeforeHit = _charWalker!.Ai.Health;
                    _charAttackToggle = 0;
                    _charTickMark = _physFrames;
                    _charPhase = 5;
                    break;
                }
                if (_physFrames - _charTickMark > CharRestWaitTicks)
                    MotionFail("visual never returned to the EXACT base at rest within the breath cycle");
                break;
            }

            case 5:   // into melee of the kept goblin + one attack press on the real wire
            {
                var t = _charWalker!.GlobalPosition;
                _playerBody!.GlobalPosition = new Vector3(t.X - 0.8f, t.Y + 0.5f, t.Z);
                _charPhase = 6;
                break;
            }

            case 6:   // the lean must arm AT the hit hunk
            {
                _charAttackToggle++;
                if (_charAttackToggle % 4 == 1) Input.ActionPress("attack");
                else if (_charAttackToggle % 4 == 3) Input.ActionRelease("attack");
                if (_charWalker!.Ai.Health < _charHealthBeforeHit)
                {
                    if (!_director!.IsAttackLeanActive(pv))
                    { MotionFail("attack lean NOT armed at the DealDamage hunk (hit landed, lean silent)"); break; }
                    GD.Print("LA_GATE: CHAR_LEAN_ACTIVE attack lean armed on the hit frame at the ONE DealDamage hunk");
                    _charTickMark = _physFrames;
                    _charPhase = 7;
                    break;
                }
                if (_physFrames - _charTickMark > CharAttackBudgetTicks)
                    MotionFail("attack press never landed on the kept goblin");
                break;
            }

            case 7:   // lean decays to the EXACT base (6f integer + breath zero-crossing)
            {
                if (_director!.IsVisualMotionAtBase(pv))
                {
                    GD.Print("LA_GATE: CHAR_LEAN_AT_BASE attack lean decayed to exact base (integer 6f counter, no delta)");
                    _charHealthBeforeDamage = _director!.PlayerModel.Health;
                    _charTickMark = _physFrames;
                    _charPhase = 8;
                    break;
                }
                if (_physFrames - _charTickMark > CharRestWaitTicks + CharReturnTicks)
                    MotionFail("attack lean never returned to the EXACT base");
                break;
            }

            case 8:   // the REAL damage site: goblin damage -> director TakeDamage -> player juice
            {
                // Mirror S1 JUICE_BODY_STILL: freeze the player body physics so
                // the motion driver + juice are the ONLY candidate movers —
                // gravity/MoveAndSlide floor-snap can never blur the bit-equal
                // readout, and a planted motion-on-body still moves it (RED).
                _playerBody!.SetPhysicsProcess(false);
                int hp = _director!.PlayerModel.Health;
                if (hp < _charHealthBeforeDamage)
                {
                    if (!ActorVisual.IsHitFlashActive(pv))
                    { MotionFail("player visual flash NOT active at the real player-damage site"); break; }
                    GD.Print("LA_GATE: CHAR_PLAYER_FLASH player visual flashes WHITE at the real player-damage site (the TakeDamage hunk, PlayerHurt's origin)");
                    _charBodyPos = _playerBody!.GlobalPosition;
                    _charTickMark = _physFrames;
                    _charPhase = 9;
                    break;
                }
                if (_physFrames - _charTickMark > CharDamageBudgetTicks)
                    MotionFail("no real enemy damage ever landed on the player (the damage site never executed)");
                break;
            }

            case 9:   // +8f: flash/punch at base, body bit-still
            {
                if (_physFrames - _charTickMark < CharReturnTicks) break;
                if (!ActorVisual.IsHitFlashAtBase(pv))
                { MotionFail("player flash NOT returned to base by +6f"); break; }
                if (!ActorVisual.IsVisualAtBase(pv))
                { MotionFail("player punch NOT returned to base by +6f"); break; }
                if (_playerBody!.GlobalPosition != _charBodyPos)
                { MotionFail($"player BODY GlobalPosition MOVED across the juice window ({_charBodyPos} -> {_playerBody.GlobalPosition}) — motion/juice wrote physics state"); break; }
                GD.Print($"LA_GATE: CHAR_BODY_STILL position={_charBodyPos}");
                GD.Print("LA_GATE: PASS — S14 character life verified (integer-phase walk feed, exact rest return, attack lean at the hit hunk, player flash at the damage site, body untouched)");
                _asserted = true;
                Quit(0);
                break;
            }
        }
    }

    /// <summary>Deterministic open-direction probe: a small sphere at
    /// pos + up*0.6 + dir*0.9 (the 6-tick stride envelope) against every
    /// collider except this body. Fixed candidate order — A/A deterministic.</summary>
    private bool TryProbeClearDir(out Vector3 dir)
    {
        var space = _playerBody!.GetWorld3D().DirectSpaceState;
        var query = new PhysicsShapeQueryParameters3D
        {
            Shape = new SphereShape3D { Radius = 0.45f },
            CollisionMask = 0xFFFFFFFFu,
            CollideWithAreas = false,
            Exclude = new Godot.Collections.Array<Rid> { _playerBody.GetRid() },
        };
        foreach (var d in new[] { Vector3.Right, Vector3.Forward, Vector3.Left, Vector3.Back })
        {
            query.Transform = new Transform3D(Basis.Identity,
                _playerBody!.GlobalPosition + Vector3.Up * 0.6f + d * 0.9f);
            if (space.IntersectShape(query, 1).Count == 0) { dir = d; return true; }
        }
        dir = Vector3.Right;
        return false;
    }
}
