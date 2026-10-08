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
// S23 STRENGTHENING (MC 10229 / DA S20 residual P2): closer/farther alone
// keeps a wrong-direction punch or a gutted preset GREEN — the punch
// displacement is dwarfed by the 12.8m rest distance, so a punch along ANY
// axis with a toward-player component measures "closer", and half the
// PunchMetres still measures closer. So the leg now additionally asserts:
//   4. CAMERA_PULSE_DIR — the punch KICK VECTOR (observed pos minus the
//      bit-still rest base) lies ON the player->camera sight-line
//      (collinear, off-axis residual < 1e-2) and AGAINST it (dot < 0:
//      punch-IN); a wrong-direction or off-axis punch lands RED even while
//      strictly closer;
//   5. CAMERA_PULSE_WINDOW_8 — the kick envelope, sampled EVERY frame from
//      the emit (frames the camera is observed off its bit-still base), is
//      EXACTLY PulseFrames=8, pinned against the literal contract, not the
//      tuning class (measuring window length against the class gutted by
//      the very plant would be vacuous);
//   6. CAMERA_PULSE_PUNCH_075 — the peak displacement inside the window is
//      PunchMetres=0.75 within float tolerance (1e-3 — the base is
//      bit-still, so the residual is float accumulation only), again
//      against the literal, not the class.
// The envelope measure is order-insensitive to whether the proof samples
// before or after the camera's _Process in a frame: both orderings observe
// the SAME 8 kick frames (the first and last are simply witnessed one proof
// frame apart) and the SAME peak (the full-amplitude first kick frame).
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
    private bool _pulseDirDone;          // S23: the +4f DIR+HOLD frame ran once
    private int _pulseMotionStart;       // S23: first frame observed off the rest base (0 = none)
    private float _pulseMaxDisp;         // S23: peak |pos - rest base| inside the window

    private const int PulseSettleBudgetFrames = 180; // bit-still base witness
    private const int PulseStillWitnessFrames = 2;   // consecutive bit-stable
    private const int PulseHoldFrames = 4;           // +4f: window still open
    private const int PulseReturnMarginFrames = 12;  // 8f decay + 4f readout margin
    private const int PulseWalkBudgetFrames = 24;    // S23: envelope must close well inside
    // S23 contract LITERALS (MC 10229): the pin measures the RUNTIME against
    // the shipped values themselves — comparing against CameraPulseTuning
    // would let a gutted class redefine its own contract (vacuous green).
    private const int PulseFramesContract = 8;
    private const float PunchMetresContract = 0.75f;
    private const float PunchPinTol = 1e-3f;         // bit-still base: accumulation only

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

            case 2:   // S23 EVERY-FRAME WALK from the emit to the envelope
                      // close: samples the kick envelope (window length) and
                      // its peak displacement, fires the shipped +4f HOLD
                      // asserts once, and adds the sight-line DIR pin.
            {
                var pos = _pulseCam!.GlobalPosition;
                if (pos != _pulseRestPos)
                {
                    if (_pulseMotionStart == 0) _pulseMotionStart = _frames;
                    float disp = pos.DistanceTo(_pulseRestPos);
                    if (disp > _pulseMaxDisp) _pulseMaxDisp = disp;
                    // MC 10280 (DA S23 P2-1): the +4f DIR sample alone let an
                    // alternating outward punch ship GREEN — pin the kick SIGN
                    // on EVERY off-base frame (DIR's convention: player->camera
                    // sight, punch-IN means the dot is negative).
                    if ((pos - _pulseRestPos).Dot(_pulseRestPos - _playerBody!.GlobalPosition) >= 0f)
                    { PulseFail($"CAMERA_PULSE_SIGN: off-base kick {pos - _pulseRestPos} NOT toward the player at +{_frames - _pulseHitFrame}f"); break; }
                }

                if (_frames - _pulseHitFrame >= PulseHoldFrames && !_pulseDirDone)
                {
                    _pulseDirDone = true;
                    if (!_pulseCam.IsPulsing) { PulseFail("window closed EARLIER than the 8f contract"); break; }
                    if (_pulseCam.IsAtRestBase) { PulseFail("camera at rest base INSIDE the window (punch never applied)"); break; }
                    // CAMERA_PULSE_DIR (S23): kick = observed pos minus the
                    // bit-still rest base. It must be COLLINEAR with the
                    // player->camera sight-line and oppose it (punch-IN).
                    // The base is bit-still, so the kick is exact — the
                    // tolerance is float accumulation, never slack. Asserted
                    // BEFORE the legacy closer check: a wrong-direction or
                    // off-axis punch must name the NEW marker, not the old
                    // weaker one (MC 10229 DA residual).
                    Vector3 kick = pos - _pulseRestPos;
                    Vector3 sight = _pulseRestPos - _playerBody!.GlobalPosition; // player -> camera
                    if (kick.LengthSquared() < 1e-4f)
                    { PulseFail("CAMERA_PULSE_DIR: zero kick vector at +4f (punch never displaced the camera)"); break; }
                    if (sight.LengthSquared() < 1e-4f)
                    { PulseFail("CAMERA_PULSE_DIR: degenerate sight-line (camera rests on the player)"); break; }
                    sight = sight.Normalized();
                    float along = kick.Dot(sight);
                    Vector3 perp = kick - sight * along;
                    if (along >= 0f)
                    { PulseFail($"CAMERA_PULSE_DIR: punch kick NOT toward the player (dist={along:0.####} on the player->camera sight, kick={kick})"); break; }
                    if (perp.LengthSquared() >= 1e-4f)
                    { PulseFail($"CAMERA_PULSE_DIR: punch kick OFF the sight-line (perp dist={perp.Length():0.####} kick={kick})"); break; }
                    float d = pos.DistanceTo(_playerBody!.GlobalPosition);
                    if (!(d < _pulseRestDist))
                    { PulseFail($"punch-in moved the camera AWAY (dist={d:0.####} vs rest={_pulseRestDist:0.####})"); break; }
                    GD.Print($"LA_GATE: CAMERA_PULSE_DIR — kick=({kick.X:0.####},{kick.Y:0.####},{kick.Z:0.####}) dist={along:0.####} perp dist={perp.Length():0.####} vs sight=({sight.X:0.####},{sight.Y:0.####},{sight.Z:0.####})");
                    GD.Print("LA_GATE: CAMERA_PULSE_HELD at +4f (punched in, window open)");
                }

                if (_pulseMotionStart != 0 && pos == _pulseRestPos && !_pulseCam.IsPulsing)
                {
                    int window = _frames - _pulseMotionStart; // off-base frames: start..close-1
                    if (window != PulseFramesContract)
                    { PulseFail($"CAMERA_PULSE_WINDOW_8: kick envelope measured {window}f off-base (contract PulseFrames={PulseFramesContract})"); break; }
                    if (!(Mathf.Abs(_pulseMaxDisp - PunchMetresContract) <= PunchPinTol))
                    { PulseFail($"CAMERA_PULSE_PUNCH_075: peak displacement dist={_pulseMaxDisp:0.######} outside {PunchMetresContract}±{PunchPinTol} (contract PunchMetres)"); break; }
                    GD.Print($"LA_GATE: CAMERA_PULSE_WINDOW_8 — kick envelope measured {window}f == contract PulseFrames={PulseFramesContract}");
                    GD.Print($"LA_GATE: CAMERA_PULSE_PUNCH_075 — peak kick dist={_pulseMaxDisp:0.######} within {PunchMetresContract}±{PunchPinTol} (contract PunchMetres)");
                    _pulsePhase = 3;
                    break;
                }
                if (_frames - _pulseHitFrame > PulseWalkBudgetFrames)
                    PulseFail($"envelope never closed inside the {PulseWalkBudgetFrames}f walk budget (start={_pulseMotionStart})");
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
