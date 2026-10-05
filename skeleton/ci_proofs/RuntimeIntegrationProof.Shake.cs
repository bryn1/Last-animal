using Godot;
using LastAnimal.World;

// Last Animal — MC 10121 Inc-3 S2 juice-shake proof (stage 96, mode JUICE_SHAKE).
//
// Proves the S2 contract on the LIVE playable scene:
//   1. FollowCamera SUBSCRIBES S0's PlayerHurt (the hurt arm driven through
//      the real wire: PlayerModel.TakeDamage -> the Ui-poll emit) — the
//      window is OPEN on the emit readout and the camera actually MOVES
//      off its follow base inside the window;
//   2. the window holds at +4f (no early decay) and the camera is back on
//      its EXACT shake-free follow base by +12f (+2f readout margin) —
//      the decay is an INTEGER frame counter (F4); the planted-bad
//      negative control swaps it for a delta-time decay and goes RED;
//   3. the BossFallen arm reacts too (direct bus emit = the consumer-test
//      idiom, AudioTest precedent — BossFallen's EMIT authority is S0's
//      and is proven by the bus_emit leg).
// Spawning is off at boot (bus_emit quiet-leg idiom): a standing player is
// otherwise whittled by the spawn set and stray PlayerHurt edges would
// re-open the window mid-assertion. The camera itself prints NOTHING
// (F4-CMP) — every marker here is proof output.
//
// Run:  LA_GATE_MODE=JUICE_SHAKE $GODOT --headless --path <proj> \
//         --script res://ci_proofs/RuntimeIntegrationProof.cs
public partial class RuntimeIntegrationProof : SceneTree
{
    private int _shakePhase;
    private int _shakePhaseStart;
    private int _shakeHurt;
    private int _shakeHurtBase;
    private int _shakeHitFrame;
    private FollowCamera? _shakeCam;

    private const int ShakeHurtBudgetFrames = 60;   // TakeDamage -> Ui-poll emit
    private const int ShakeHoldFrames = 4;          // +4f: window must still be open
    private const int ShakeReturnMarginFrames = 14; // 12f decay + 2f readout margin

    private void ShakeFail(string why) => Fail("JUICE_SHAKE: " + why);

    private void RunShakeStage()
    {
        switch (_shakePhase)
        {
            case 0:
            {
                _shakeCam = _main!.GetNodeOrNull<FollowCamera>("Camera");
                if (_shakeCam == null) { ShakeFail("FollowCamera absent at Main/Camera"); break; }
                if (_shakeCam.IsShaking) { ShakeFail("camera boots already shaking"); break; }
                _bus!.PlayerHurt += _ => _shakeHurt++;
                _shakeHurtBase = _shakeHurt;
                _director!.PlayerModel.TakeDamage(10);   // the model's only damage entry
                GD.Print("LA_GATE: shake trigger — TakeDamage(10) on the live model");
                _shakePhase = 1;
                _shakePhaseStart = _frames;
                break;
            }

            case 1:   // hurt arm: the emit reached the camera (S0's poll, real wire)
            {
                if (_shakeHurt > _shakeHurtBase)
                {
                    if (!_shakeCam!.IsShaking) { ShakeFail("PlayerHurt emit did NOT arm the camera"); break; }
                    GD.Print("LA_GATE: JUICE_SHAKE_ACTIVE — PlayerHurt opened the 12f window");
                    _shakeHitFrame = _frames;
                    _shakePhase = 2;
                    break;
                }
                if (_frames - _shakePhaseStart > ShakeHurtBudgetFrames)
                    ShakeFail("TakeDamage(10) produced no PlayerHurt emit (S0 poll broken)");
                break;
            }

            case 2:   // hold: decay must NOT finish early at +4f, and the
                      // camera must actually sit OFF its follow base.
            {
                if (_frames - _shakeHitFrame < ShakeHoldFrames) break;
                if (!_shakeCam!.IsShaking) { ShakeFail("window closed EARLIER than the 12f contract"); break; }
                if (_shakeCam.IsAtShakeBase) { ShakeFail("camera at base INSIDE the window (kick never applied)"); break; }
                GD.Print("LA_GATE: JUICE_SHAKE_HELD at +4f (off base, window open)");
                _shakePhase = 3;
                break;
            }

            case 3:   // exact base return by +12f (integer-decay contract, F4)
            {
                if (_frames - _shakeHitFrame < ShakeReturnMarginFrames) break;
                if (_shakeCam!.IsShaking) { ShakeFail("window still open past +12f (decay MISSED)"); break; }
                if (!_shakeCam.IsAtShakeBase) { ShakeFail("camera NOT at exact follow base by +12f"); break; }
                GD.Print($"LA_GATE: JUICE_SHAKE_AT_BASE — exact return pos={_shakeCam.GlobalPosition}");
                GD.Print("LA_GATE: JUICE_SHAKE — hurt arm verified (active, held +4f, exact base return +12f)");
                _bus!.EmitBossFallen("s2-proof");   // consumer-test idiom (AudioTest)
                _shakePhase = 4;
                _shakePhaseStart = _frames;
                break;
            }

            case 4:   // boss arm: the same Subscribe() must react
            {
                if (!_shakeCam!.IsShaking) { ShakeFail("BossFallen emit did NOT arm the camera"); break; }
                GD.Print("LA_GATE: JUICE_SHAKE_BOSS_ACTIVE — BossFallen opened the window too");
                _shakePhase = 5;
                _shakePhaseStart = _frames;
                break;
            }

            case 5:   // boss window also returns EXACTLY by +12f
            {
                if (_frames - _shakePhaseStart < ShakeReturnMarginFrames) break;
                if (_shakeCam!.IsShaking || !_shakeCam.IsAtShakeBase)
                { ShakeFail("boss window did not return to exact base by +12f"); break; }
                GD.Print("LA_GATE: JUICE_SHAKE_BOSS_AT_BASE — boss arm exact return");
                GD.Print("LA_GATE: PASS — S2 juice shake verified (PlayerHurt+BossFallen subscribe; 12f integer window; exact base return)");
                _asserted = true;
                Quit(0);
                break;
            }
        }
    }
}
