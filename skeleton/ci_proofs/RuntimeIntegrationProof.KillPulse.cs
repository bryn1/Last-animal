using Godot;
using LastAnimal.Core.Framework;
using LastAnimal.World;

// Last Animal — MC 10217 / 10026.35 Inc-4 S20 kill-pulse proof
// (stage 100, mode CAMERA_KILL_PULSE).
//
// Proves the S20 kill-pulse contract on the LIVE playable scene:
//   1. FollowCamera SUBSCRIBES the EXISTING DnaExtracted bus signal (plan pin
//      N-3: a CONSUMER — zero new signals, census stays 15; the emit authority
//      is the director's kill forwarding, already proven by the positive/chain
//      legs — so this leg uses the SHIPPED consumer-test idiom: a direct
//      bus emit, the JUICE_SHAKE BossFallen / AudioTest precedent);
//   2. the punch window opens on the emit, HOLDS at +4f (camera strictly
//      CLOSER to the player than its rest distance — the punch-in actually
//      moves the camera, not just the flag), and is an INTEGER counter (F4:
//      the planted delta-time decay goes RED);
//   3. EXACT REST, CHAR_BODY_STILL-strength: the player's physics is FROZEN
//      (Motion.cs idiom) so the follow base is bit-still, and after the window
//      the camera transform is BIT-EQUAL to its pre-trigger position — not a
//      tolerance, a bit comparison (the pulse never writes _followBase; the
//      kick is additive and exactly zero at window close).
// Spawning is off at boot (bus_emit/JUICE_SHAKE quiet-leg idiom) and the
// camera prints NOTHING (F4-CMP) — every marker here is proof output.
//
// Run:  LA_GATE_MODE=CAMERA_KILL_PULSE $GODOT --headless --path <proj> \
//         --script res://ci_proofs/RuntimeIntegrationProof.cs
public partial class RuntimeIntegrationProof : SceneTree
{
    private int _pulsePhase;
    private int _pulseStillRun;
    private int _pulseHitFrame;
    private int _pulseSettleStart;
    private Vector3 _pulsePrevPos;
    private Vector3 _pulseRestPos;
    private float _pulseRestDist;
    private FollowCamera? _pulseCam;

    private const int PulseSettleBudgetFrames = 180; // bit-still base witness
    private const int PulseStillWitnessFrames = 2;   // consecutive bit-stable
    private const int PulseHoldFrames = 4;           // +4f: window still open
    private const int PulseReturnMarginFrames = 12;  // 8f decay + 4f readout margin

    private void PulseFail(string why) => Fail("CAMERA_KILL_PULSE: " + why);

    private void RunPulseStage()
    {
        switch (_pulsePhase)
        {
            case 0:
            {
                _pulseCam = _main!.GetNodeOrNull<FollowCamera>("Camera");
                if (_pulseCam == null) { PulseFail("FollowCamera absent at Main/Camera"); break; }
                if (_pulseCam.IsShaking || _pulseCam.IsPulsing || _pulseCam.IsFramingBoss)
                { PulseFail("camera boots with a presentation window open"); break; }
                // The bit-exact rest pin needs a bit-still base: freeze the
                // player's physics (Motion.cs:250 idiom). In this quiet boot
                // nothing else moves the follow target.
                _playerBody!.SetPhysicsProcess(false);
                _pulsePrevPos = _pulseCam.GlobalPosition;
                _pulseSettleStart = _frames;
                GD.Print("LA_GATE: pulse leg — physics frozen, witnessing a bit-still follow base");
                _pulsePhase = 1;
                break;
            }

            case 1:   // witness: the camera transform stops moving (bit-stable
            {          // across 2 consecutive frames => permanently still here)
                var pos = _pulseCam!.GlobalPosition;
                _pulseStillRun = pos == _pulsePrevPos ? _pulseStillRun + 1 : 0;
                _pulsePrevPos = pos;
                if (_pulseStillRun < PulseStillWitnessFrames)
                {
                    if (_frames - _pulseSettleStart > PulseSettleBudgetFrames)
                        PulseFail("follow base never went bit-still after the freeze (base mover?)");
                    break;
                }
                _pulseRestPos = pos;
                _pulseRestDist = pos.DistanceTo(_playerBody!.GlobalPosition);
                // The consumer-test idiom: emit the EXISTING signal directly
                // (its kill-wire authority is proven by the positive chain).
                _bus!.EmitDnaExtracted(new DnaSignature("s20-pulse", "proof"));
                if (!_pulseCam.IsPulsing) { PulseFail("DnaExtracted emit did NOT arm the camera"); break; }
                GD.Print("LA_GATE: CAMERA_PULSE_ACTIVE — DnaExtracted consumer opened the 8f punch window");
                _pulseHitFrame = _frames;
                _pulsePhase = 2;
                break;
            }

            case 2:   // hold: window must NOT finish early at +4f, and the
                      // camera must actually sit INSIDE its rest distance.
            {
                if (_frames - _pulseHitFrame < PulseHoldFrames) break;
                if (!_pulseCam!.IsPulsing) { PulseFail("window closed EARLIER than the 8f contract"); break; }
                if (_pulseCam.IsAtRestBase) { PulseFail("camera at rest base INSIDE the window (punch never applied)"); break; }
                float d = _pulseCam.GlobalPosition.DistanceTo(_playerBody!.GlobalPosition);
                if (!(d < _pulseRestDist))
                { PulseFail($"punch-in moved the camera AWAY (dist={d:0.####} vs rest={_pulseRestDist:0.####})"); break; }
                GD.Print("LA_GATE: CAMERA_PULSE_HELD at +4f (punched in, window open)");
                _pulsePhase = 3;
                break;
            }

            case 3:   // exact return: bit-equal to the PRE-TRIGGER transform
            {
                if (_frames - _pulseHitFrame < PulseReturnMarginFrames) break;
                if (_pulseCam!.IsPulsing) { PulseFail("window still open past +8f (decay MISSED)"); break; }
                if (_pulseCam.IsPulsing || _pulseCam.IsShaking || _pulseCam.IsFramingBoss)
                { PulseFail("a presentation window is still open past the return margin"); break; }
                if (!_pulseCam.IsAtRestBase) { PulseFail("camera NOT on its exact follow base past +8f"); break; }
                if (_pulseCam.GlobalPosition != _pulseRestPos)   // BIT comparison, no tolerance
                { PulseFail($"rest NOT bit-equal to pre-trigger pos={_pulseCam.GlobalPosition} rest={_pulseRestPos}"); break; }
                GD.Print($"LA_GATE: CAMERA_PULSE_AT_BASE — exact return pos=({_pulseCam.GlobalPosition.X:0.####},{_pulseCam.GlobalPosition.Y:0.####},{_pulseCam.GlobalPosition.Z:0.####}) bit-equal pre-trigger");
                GD.Print("LA_GATE: PASS — S20 kill pulse verified (DnaExtracted consumer, 8f integer punch window, held +4f, BIT-EXACT rest return)");
                _asserted = true;
                _pulseCam = null;                     // MC 10117 rooted-fields idiom
                ReleaseHeldRefsBeforeQuit();
                Quit(0);
                break;
            }
        }
    }
}
