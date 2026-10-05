using Godot;
using LastAnimal.World;

// Last Animal — MC 10120 Inc-3 S1 juice proof (stage 95, mode JUICE_HITFLASH).
//
// Proves the S1 contract on the LIVE playable scene, at the REAL hit site:
//   1. the white flash is ACTIVE on the hit frame (material tint == white,
//      the MeshInstance3D-tree analogue of a Modulate flash — Node3D has no
//      Modulate; the per-enemy shared MaterialOverride is the one analogue);
//   2. the VISUAL-NODE offset punch is live (Visual root Position OFF base);
//   3. BOTH return EXACTLY to base at +6f (JuiceTuning ratified defaults);
//   4. the enemy BODY GlobalPosition is UNCHANGED across the whole juice
//      window — its physics process is switched OFF before the hit, so the
//      juice mechanism is the ONLY candidate mover: any body write (a planted
//      punch-on-body, the required negative control) moves it and goes RED.
//
// The hit rides the real input wire (attack action -> director TryAttack ->
// the ONE DealDamage site); nothing is invoked from the proof directly.
// Markers print only after their assertion passes (harness contract).
//
// Run:  LA_GATE_MODE=JUICE_HITFLASH $GODOT --headless --path <proj> \
//         --script res://ci_proofs/RuntimeIntegrationProof.cs
public partial class RuntimeIntegrationProof : SceneTree
{
    private int _juicePhase;
    private int _juicePhaseStart;
    private int _juiceAttackToggle;
    private int _juiceHitFrame;
    private int _juiceHealthBefore;
    private Vector3 _juiceBodyPos;
    private EnemyActor? _juiceTarget;

    private const int JuiceHitBudgetFrames = 60;   // input -> director -> hit
    private const int JuiceHoldFrames = 4;         // hit+4 readout: decay (to +6f) cannot be done yet (readout lag <= 1f)
    private const int JuiceReturnMarginFrames = 8; // +6f decay + 2f readout margin

    private void JuiceFail(string why) => Fail("JUICE_HITFLASH: " + why);

    private void RunJuiceStage()
    {
        switch (_juicePhase)
        {
            case 0:
            {
                // Pick the nearest live NON-boss enemy (BusKillLoop selection),
                // freeze its body physics, teleport the player into range and
                // press attack exactly once (the Chain.cs 2-frame press idiom).
                EnemyActor? target = null;
                foreach (var e in _director!.Enemies)
                {
                    if (e.IsDead || !GodotObject.IsInstanceValid(e)) continue;
                    if (ReferenceEquals(e, _director.BossActor)) continue;
                    target = e;
                    break;
                }
                if (target == null) { JuiceFail("no live non-boss enemy to hit"); break; }
                _juiceTarget = target;
                _juiceHealthBefore = target.Ai.Health;
                target.SetPhysicsProcess(false);   // juice is then the only mover of the body
                _juiceBodyPos = target.GlobalPosition;   // frozen baseline: any body write is the juice's
                var p = target.GlobalPosition;
                _playerBody!.GlobalPosition = new Vector3(p.X - 0.8f, p.Y, p.Z);
                GD.Print($"LA_GATE: juice target {target.Name} frozen, player moved into attack range");
                _juicePhase = 1;
                _juicePhaseStart = _frames;
                break;
            }

            case 1:   // single attack press: hold 2 frames, release, then wait
            {
                _juiceAttackToggle++;
                if (_juiceAttackToggle % 4 == 1) Input.ActionPress("attack");
                else if (_juiceAttackToggle % 4 == 3) Input.ActionRelease("attack");
                if (_juiceTarget!.Ai.Health < _juiceHealthBefore)
                {
                    // Hit frame readout: the hit rode the REAL attack wire.
                    if (!ActorVisual.IsHitFlashActive(_juiceTarget.Visual))
                    { JuiceFail("flash NOT active on the hit frame"); break; }
                    GD.Print("LA_GATE: JUICE_FLASH_ACTIVE on hit frame (white tint applied)");
                    if (ActorVisual.IsVisualAtBase(_juiceTarget.Visual))
                    { JuiceFail("punch NOT applied on the hit frame (visual still at base)"); break; }
                    GD.Print("LA_GATE: JUICE_PUNCH_ACTIVE on hit frame (visual off base)");
                    _juiceHitFrame = _frames;
                    _juicePhase = 2;
                    break;
                }
                if (_frames - _juicePhaseStart > JuiceHitBudgetFrames)
                    JuiceFail("attack press never moved enemy health (hit never landed)");
                break;
            }

            case 2:   // hold check: the decay must NOT have finished early —
                      // at +4f (readout margin) flash + punch must still be on.
            {
                if (_frames - _juiceHitFrame < JuiceHoldFrames) break;
                if (_juiceTarget!.GlobalPosition != _juiceBodyPos)
                { JuiceFail("enemy BODY MOVED during the juice window (+4f readout)"); break; }
                if (!ActorVisual.IsHitFlashActive(_juiceTarget.Visual))
                { JuiceFail("flash decayed EARLIER than the 6f window"); break; }
                if (ActorVisual.IsVisualAtBase(_juiceTarget.Visual))
                { JuiceFail("punch returned EARLIER than the 6f window"); break; }
                GD.Print("LA_GATE: JUICE_HELD at +4f (window not decayed early)");
                _juicePhase = 3;
                break;
            }

            case 3:   // +6f return window (decay ticks in VisualJuice._Process)
            {
                if (_frames - _juiceHitFrame < JuiceReturnMarginFrames) break;
                if (!ActorVisual.IsVisualAtBase(_juiceTarget!.Visual))
                { JuiceFail("visual offset NOT returned to base by +6f"); break; }
                GD.Print("LA_GATE: JUICE_PUNCH_AT_BASE (+6f exact base transform)");
                if (!ActorVisual.IsHitFlashAtBase(_juiceTarget.Visual))
                { JuiceFail("flash tint NOT returned to base by +6f"); break; }
                GD.Print("LA_GATE: JUICE_FLASH_AT_BASE (+6f exact stored colours)");
                if (_juiceTarget.GlobalPosition != _juiceBodyPos)
                { JuiceFail("enemy BODY GlobalPosition MOVED across the juice window"); break; }
                GD.Print($"LA_GATE: JUICE_BODY_STILL position={_juiceBodyPos}");
                GD.Print("LA_GATE: PASS — S1 juice verified (flash 6f, punch base return +6f, body untouched)");
                _asserted = true;
                Quit(0);
                break;
            }
        }
    }
}
