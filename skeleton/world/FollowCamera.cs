using Godot;
using LastAnimal.Core;

// Last Animal — W3-fix composition (MC 1123.10, artemis, 2026-09-08).
//
// FollowCamera: the SINGLE current camera of the playable composition root.
// A small isometric third-person follow cam. It is deliberately decoupled
// from the Player physics shell (Player.cs only drives movement), so framing
// is one concern in one small node — a fixed behind-and-above offset that
// keeps terrain, player, enemies and companion in frame, with sky visible.
//
// (I4 seam: a View-layer node; it only reads the target transform and frames
// it. It owns no gameplay logic.)
namespace LastAnimal.World;

/// <summary>Inc-3 S2 camera-shake tunables (MC 10121), JuiceTuning-consistent
/// naming. The ratified JuiceTuning block lives on the S1 branch (S1's
/// ActorVisual.cs); a distinct class name keeps master compiling and the
/// orchestrator folds these in at merge. Consts only, no wall-clock (F4).</summary>
public static class JuiceShakeTuning
{
    public const int ShakeFrames = 12;       // integer-frame window, exact +12f return
    public const float ShakeMaxAmp = 0.15f;  // peak metres (ratified ceiling <= 0.15)
}

/// <summary>MC 10217 / 10026.35 Inc-4 S20 kill-pulse tunables (plan pin N-3:
/// the pulse is a DnaExtracted bus CONSUMER — no new signal, census stays 15).
/// Punch-in along the actual sight-line toward the target on the SAME additive-
/// kick-on-base pattern as the shipped S2 shake (extend the pattern, one
/// implementation per concern): INTEGER frame window, linear ramp to zero,
/// zero contribution at window close -> the base return is EXACT. Consts
/// only, no wall-clock (F4).</summary>
public static class CameraPulseTuning
{
    public const int PulseFrames = 8;        // window; exact +8f return to base
    public const float PunchMetres = 0.75f;  // peak forward displacement
}

/// <summary>MC 10217 / 10026.35 Inc-4 S20 boss-framing tunables. While a boss
/// encounter is live the camera eases to a framing PRESET (pull-back along the
/// follow-offset direction + height offset); the encounter state is READ from
/// WorldDirector.HasLiveBoss — presentation authority: the camera READS gameplay,
/// writes ONLY its own transform. INTEGER progress clock steps +1/-1 per process
/// frame toward live/quiet, so a mid-window reversal still lands on an EXACT
/// zero-contribution rest. Consts only, no wall-clock (F4).</summary>
public static class BossFramingTuning
{
    public const int EaseFrames = 18;         // full ease-in / ease-out window
    public const float PullBackMetres = 3.0f; // extra distance along the offset dir
    public const float HeightMetres = 1.5f;   // extra height above the follow base
}

[GlobalClass]
public partial class FollowCamera : Node3D
{
    /// <summary>The node to frame (the Player). Null -> stays put.</summary>
    [Export] public Node3D? Target { get; set; }

    /// <summary>World-space offset behind-and-above the target.</summary>
    [Export] public Vector3 Offset { get; set; } = new Vector3(8f, 6f, 8f);

    /// <summary>Smoothing factor (higher = snappier follow).</summary>
    [Export] public float LerpSpeed { get; set; } = 5f;

    // MC 10121 Inc-3 S2 juice shake: SUBSCRIBES S0's PlayerHurt/BossFallen
    // (SfxRouter-style consumer wiring — the EMIT is S0's Ui poll). Each edge
    // opens a ShakeFrames window decaying on an INTEGER counter (F4: no
    // delta-time/Tween/Timer). The kick is ADDED on top of the smoothed
    // follow base, never folded into it, so the +12f base return is EXACT.
    // The camera prints NOTHING (F4-CMP).
    //
    // MC 10217 Inc-4 S20 extends the SAME consumer pattern with two further
    // windows, each its own concern, all kicks summed on top of the base:
    //   PULSE  — DnaExtracted (plan pin N-3: a bus CONSUMER, no new signal)
    //            opens a CameraPulseTuning punch-in window, same integer
    //            ramp-to-zero envelope as the shake -> exact +8f base return.
    //   FRAMING — while WorldDirector.HasLiveBoss READS true the integer
    //            progress clock eases +1/frame toward EaseFrames (a pull-back
    //            + height PRESET); when it READS false the clock eases
    //            -1/frame to exactly zero, where the contribution is zero and
    //            the camera sits bit-exactly on the follow base. Presentation
    //            authority: this node only ever reads gameplay state and
    //            writes its OWN transform — zero Bus/GameState writes.
    private EventBus? _bus;
    private WorldDirector? _director;   // S20 READ-only boss-threshold view (never set by us)
    private int _shakeFramesLeft;
    private int _pulseFramesLeft;       // S20 pulse window (integer, F4)
    private int _bossFramingProgress;   // S20 framing clock, 0..EaseFrames (F4)
    private Vector3 _followBase;   // smoothed follow position WITHOUT the kick

    /// <summary>S2 proof readers (S1 ActorVisual reader idiom): window open /
    /// camera sits EXACTLY on the shake-free follow base.</summary>
    public bool IsShaking => _shakeFramesLeft > 0;
    public bool IsAtShakeBase => _shakeFramesLeft == 0 && GlobalPosition == _followBase;

    /// <summary>S20 proof readers (same idiom): pulse window open / boss
    /// framing engaged / ALL presentation windows closed and the camera sits
    /// bit-exactly on its event-free follow base (the CHAR_BODY_STILL-strength
    /// rest pin the battery legs assert).</summary>
    public bool IsPulsing => _pulseFramesLeft > 0;
    public bool IsFramingBoss => _bossFramingProgress > 0;
    public int BossFramingProgress => _bossFramingProgress;
    public bool IsAtRestBase =>
        _shakeFramesLeft == 0 && _pulseFramesLeft == 0 && _bossFramingProgress == 0
        && GlobalPosition == _followBase;

    public override void _Ready()
    {
        // Robust runtime resolution: this FollowCamera lives on the composition
        // root's Camera node, whose sibling Player is the frame target.
        Target ??= GetNodeOrNull<Node3D>("../Player");
        if (Target != null)
            GlobalPosition = Target.GlobalPosition + Offset;
        _followBase = GlobalPosition;
        // S20: the boss-threshold view lives on the composition root (this
        // node's parent). READ-only — retry-resolved in _Process like the bus.
        _director = GetNodeOrNull<WorldDirector>("..");
        TrySubscribe();
    }

    public override void _Process(double delta)
    {
        if (Target == null) return;

        // Headless --script runs load the EventBus autoload AFTER scene
        // _Ready (proof-harness precedent, MC 1344.1): retry until wired.
        if (_bus == null) TrySubscribe();
        if (_director == null) _director = GetNodeOrNull<WorldDirector>("..");

        float t = 1f - Mathf.Exp(-LerpSpeed * (float)delta);
        _followBase = _followBase.Lerp(Target.GlobalPosition + Offset, t);

        Vector3 kick = Vector3.Zero;
        if (_shakeFramesLeft > 0)
        {
            // Integer-count decay (F4); linear amp ramp to zero, deterministic
            // per-frame sign flips (no RNG — no new wall-clock sources).
            float a = JuiceShakeTuning.ShakeMaxAmp * _shakeFramesLeft / JuiceShakeTuning.ShakeFrames;
            kick = new Vector3(
                (_shakeFramesLeft & 1) == 0 ? a : -a,
                (_shakeFramesLeft & 2) == 0 ? a * 0.5f : -a * 0.5f,
                (_shakeFramesLeft & 4) == 0 ? a : -a);
            _shakeFramesLeft--;
        }
        if (_pulseFramesLeft > 0)
        {
            // S20 kill pulse: punch-in along the ACTUAL sight-line toward the
            // target (the offset direction at rest), integer ramp to zero —
            // the same exact-return envelope as the shipped shake (F4).
            Vector3 sight = _followBase - Target.GlobalPosition;
            if (sight.LengthSquared() > 0f)
            {
                float a = CameraPulseTuning.PunchMetres * _pulseFramesLeft / CameraPulseTuning.PulseFrames;
                kick -= sight.Normalized() * a;
            }
            _pulseFramesLeft--;
        }
        if (_bossFramingProgress > 0 || (_director != null && _director.HasLiveBoss))
        {
            // S20 boss framing: INTEGER progress clock toward the READ state
            // (F4); the ramped preset kick is linear in progress, so at
            // progress 0 the contribution is EXACTLY zero (bit-exact rest,
            // mid-window reversals included).
            int goal = _director != null && _director.HasLiveBoss
                ? BossFramingTuning.EaseFrames : 0;
            if (_bossFramingProgress < goal) _bossFramingProgress++;
            else if (_bossFramingProgress > goal) _bossFramingProgress--;
            if (_bossFramingProgress > 0)
            {
                float p = (float)_bossFramingProgress / BossFramingTuning.EaseFrames;
                Vector3 off = Offset;
                if (off.LengthSquared() > 0f)
                    kick += off.Normalized() * (BossFramingTuning.PullBackMetres * p);
                kick += new Vector3(0f, BossFramingTuning.HeightMetres * p, 0f);
            }
        }
        GlobalPosition = _followBase + kick;
        LookAt(Target.GlobalPosition + new Vector3(0, 1f, 0), Vector3.Up);
    }

    private void TrySubscribe()
    {
        _bus = GetNodeOrNull<EventBus>("/root/EventBus");
        if (_bus == null) return;
        _bus.PlayerHurt += _ => _shakeFramesLeft = JuiceShakeTuning.ShakeFrames;
        _bus.BossFallen += _ => _shakeFramesLeft = JuiceShakeTuning.ShakeFrames;
        // S20 (plan pin N-3): the kill pulse rides the EXISTING DnaExtracted
        // signal as a new consumer — zero new signals, zero save fields, the
        // camera never EMITS anything.
        _bus.DnaExtracted += _ => _pulseFramesLeft = CameraPulseTuning.PulseFrames;
    }
}
