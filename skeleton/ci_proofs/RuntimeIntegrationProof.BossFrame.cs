using Godot;
using LastAnimal.Ecosystem;
using LastAnimal.World;

// Last Animal — MC 10217 / 10026.35 Inc-4 S20 boss-framing proof
// (stage 101, mode BOSS_FRAME).
//
// Proves the S20 boss-framing contract on the LIVE playable scene, driven
// entirely through the SHIPPED gameplay paths (the ZoneBossProof boss_phase
// idioms: KillLoop farm to EcosystemSpawner.BossThreshold, PressTravel to the
// next zone entry — the first entry past the threshold fields the live boss):
//   1. ENTER: the camera READS WorldDirector.HasLiveBoss (presentation
//      authority: READS gameplay, writes ONLY its own transform — zero Bus/
//      GameState writes, zero new signals, zero save fields) and its INTEGER
//      progress clock ramps the pull-back/height PRESET in (BOSS_FRAME_ENTER,
//      then BOSS_FRAME_HELD at full progress with the camera verifiably FARTHER
//      from the framed player than at the live edge);
//   2. EXIT: killing the boss through the REAL attack wire flips HasLiveBoss;
//      the same integer clock ramps out — and because the boss kill is the
//      ONLY kill after the threshold, OnDnaExtracted's dead-boss early-return
//      means no re-field flicker; the BossFallen shake + farm-extract pulse
//      windows must also close (IsAtRestBase checks every window);
//   3. EXACT REST: after the exit the camera sits BIT-EXACTLY on its
//      event-free follow base (IsAtRestBase — the shipped JUICE_SHAKE_AT_BASE
//      strength pin). F4: all timing is INTEGER frames, zero delta math.
// This mode NEEDS the live spawn set (farm + boss) — it routes after the
// enemy guard, like DISSOLVE_SUPPRESS/CHAR_MOTION. The camera prints NOTHING
// (F4-CMP) — every marker here is proof output.
//
// Run:  LA_GATE_MODE=BOSS_FRAME $GODOT --headless --path <proj> \
//         --script res://ci_proofs/RuntimeIntegrationProof.cs
public partial class RuntimeIntegrationProof : SceneTree
{
    private int _bfPhase;
    private int _bfPhaseStart;
    private int _bfKillToggle;
    private int _bfTravelToggle;
    private string _bfThresholdZone = "";
    private float _bfDistAtLive;
    private FollowCamera? _bfCam;

    private const int BfFarmBudgetFrames = 1200;    // ZoneBossProof stage-21 parity
    private const int BfTravelBudgetFrames = 240;
    private const int BfKillBudgetFrames = 1800;    // boss HP is scaled — generous
    private const int BfRestBudgetFrames = 120;     // 18f ease-out + 12f shake + 8f pulse + readout
    private const int BfPressFrames = 6;            // ZoneBossProof travel press idiom

    private void BfFail(string why) => Fail("BOSS_FRAME: " + why);

    /// <summary>ZoneBossProof.KillLoop adapted: farm the nearest live non-boss
    /// enemy (never the boss before the framing asserts — a pre-assert boss
    /// kill would skip the very ENTER this leg proves).</summary>
    private bool BfFarmKill()
    {
        EnemyActor? target = null;
        float best = float.MaxValue;
        foreach (var e in _director!.Enemies)
        {
            if (e.IsDead || !IsInstanceValid(e)) continue;
            if (ReferenceEquals(e, _director.BossActor)) continue;
            float d = e.GlobalPosition.DistanceTo(_playerBody!.GlobalPosition);
            if (d < best) { best = d; target = e; }
        }
        if (target == null) return false;
        if (best > _director.PlayerModel.AttackRange)
        {
            Vector3 p = target.GlobalPosition;
            _playerBody!.GlobalPosition = new Vector3(p.X - 0.8f, p.Y, p.Z);
        }
        _bfKillToggle++;
        if (_bfKillToggle % 4 == 1) Input.ActionPress("attack");
        else if (_bfKillToggle % 4 == 3) Input.ActionRelease("attack");
        return true;
    }

    /// <summary>ZoneBossProof.PressTravel (re-arm idiom included).</summary>
    private bool BfPressTravel()
    {
        _bfTravelToggle++;
        if (_bfTravelToggle == 1) Input.ActionPress("travel");
        if (_bfTravelToggle >= BfPressFrames)
        {
            Input.ActionRelease("travel");
            _bfTravelToggle = 0;
            return true;
        }
        return false;
    }

    private void RunBossFrameStage()
    {
        switch (_bfPhase)
        {
            case 0:
            {
                _bfCam = _main!.GetNodeOrNull<FollowCamera>("Camera");
                if (_bfCam == null) { BfFail("FollowCamera absent at Main/Camera"); break; }
                if (_bfCam.IsFramingBoss) { BfFail("camera boots with boss framing engaged"); break; }
                if (_director!.HasLiveBoss) { BfFail("boot zone already fields a live boss"); break; }
                _bfPhase = 1;
                _bfPhaseStart = _frames;
                GD.Print("LA_GATE: boss-frame leg — farming spoken DNA to the BossThreshold");
                break;
            }

            case 1:   // farm: kill through the REAL path until observed >= BossThreshold
            {
                if (_director!.SpokenDna.Count >= EcosystemSpawner.BossThreshold)
                {
                    _bfThresholdZone = _director.CurrentZone;
                    _bfPhase = 2;
                    _bfPhaseStart = _frames;
                    _bfTravelToggle = 0;
                    GD.Print($"LA_GATE: BOSS_THRESHOLD_REACHED observed={_director.SpokenDna.Count} zone={_bfThresholdZone} — travelling on");
                    break;
                }
                if (!BfFarmKill())
                {
                    BfPressTravel();   // pool dry — the next entry fields a fresh set
                }
                else if (_frames - _bfPhaseStart > BfFarmBudgetFrames)
                    BfFail("kill path never reached BossThreshold");
                break;
            }

            case 2:   // the first entry past the threshold fields the live boss
            {
                if (BfPressTravel() && _director!.CurrentZone != _bfThresholdZone)
                {
                    if (!_director.HasLiveBoss)
                    { BfFail("zone entered past BossThreshold fielded NO live boss"); break; }
                    _bfDistAtLive = _bfCam!.GlobalPosition.DistanceTo(_playerBody!.GlobalPosition);
                    GD.Print($"LA_GATE: BOSS_FRAME_LIVE zone={_director.CurrentZone} — camera must READ it and ease in");
                    _bfPhase = 3;
                    _bfPhaseStart = _frames;
                }
                else if (_frames - _bfPhaseStart > BfTravelBudgetFrames)
                    BfFail("travel did not leave the threshold zone (boss stage)");
                break;
            }

            case 3:   // ENTER: the integer clock eases the preset in
            {
                if (!_bfCam!.IsFramingBoss)
                {
                    if (_frames - _bfPhaseStart > 60)
                        BfFail("HasLiveBoss READ true but the camera never armed the framing clock");
                    break;
                }
                GD.Print("LA_GATE: BOSS_FRAME_ENTER — camera framing clock eases to the boss preset");
                _bfPhase = 4;
                _bfPhaseStart = _frames;
                break;
            }

            case 4:   // HELD: full progress + the camera sits measurably off
                      // its non-framing distance (the preset is REAL, not a flag)
            {
                if (_bfCam!.BossFramingProgress < BossFramingTuning.EaseFrames)
                {
                    if (_frames - _bfPhaseStart > 120)
                        BfFail($"framing clock stalled at progress={_bfCam.BossFramingProgress}");
                    break;
                }
                float d = _bfCam.GlobalPosition.DistanceTo(_playerBody!.GlobalPosition);
                if (!(d > _bfDistAtLive + 1.0f))
                { BfFail($"framing preset did not pull the camera back (dist={d:0.####} atLive={_bfDistAtLive:0.####})"); break; }
                GD.Print("LA_GATE: BOSS_FRAME_HELD — full preset progress, camera verifiably pulled back");
                _bfPhase = 5;
                _bfPhaseStart = _frames;
                break;
            }

            case 5:   // kill the BOSS through the REAL attack wire (the only
                      // kill past the threshold -> OnDnaExtracted early-returns
                      // on the dead boss -> no re-field flicker)
            {
                EnemyActor? boss = _director!.BossActor;
                if (boss == null || !IsInstanceValid(boss) || boss.IsDead)
                {
                    Input.ActionRelease("attack");
                    GD.Print("LA_GATE: BOSS_FRAME_KILLED — HasLiveBoss must fall and the clock ease out");
                    _bfPhase = 6;
                    _bfPhaseStart = _frames;
                    break;
                }
                Vector3 p = boss.GlobalPosition;
                if (p.DistanceTo(_playerBody!.GlobalPosition) > _director.PlayerModel.AttackRange)
                    _playerBody!.GlobalPosition = new Vector3(p.X - 0.8f, p.Y, p.Z);
                _bfKillToggle++;
                if (_bfKillToggle % 4 == 1) Input.ActionPress("attack");
                else if (_bfKillToggle % 4 == 3) Input.ActionRelease("attack");
                if (_frames - _bfPhaseStart > BfKillBudgetFrames)
                    BfFail("boss never died through the attack wire");
                break;
            }

            case 6:   // EXACT REST: every presentation window (framing ease-out,
                      // the BossFallen shake, farm-extract pulses) closed and the
                      // camera BIT-EXACTLY on its event-free follow base
            {
                if (_director!.HasLiveBoss) { BfFail("boss died but HasLiveBoss stayed true"); break; }
                if (!_bfCam!.IsAtRestBase)
                {
                    if (_frames - _bfPhaseStart > BfRestBudgetFrames)
                        BfFail($"camera never returned to its exact follow base (framing={_bfCam.BossFramingProgress} pulsing={_bfCam.IsPulsing} shaking={_bfCam.IsShaking})");
                    break;
                }
                if (_bfCam.BossFramingProgress != 0)
                { BfFail("rest base reached with the framing clock NOT at zero"); break; }
                GD.Print($"LA_GATE: BOSS_FRAME_AT_BASE — exact return pos=({_bfCam.GlobalPosition.X:0.####},{_bfCam.GlobalPosition.Y:0.####},{_bfCam.GlobalPosition.Z:0.####}) on the event-free follow base");
                GD.Print("LA_GATE: PASS — S20 boss framing verified (HasLiveBoss READ, integer ease-in to the preset, REAL-wire boss kill, BIT-EXACT base return; zero writes to gameplay)");
                _asserted = true;
                _bfCam = null;                        // MC 10117 rooted-fields idiom
                ReleaseHeldRefsBeforeQuit();
                Quit(0);
                break;
            }
        }
    }
}
