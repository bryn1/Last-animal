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
    private EventBus? _bus;
    private int _shakeFramesLeft;
    private Vector3 _followBase;   // smoothed follow position WITHOUT the kick

    /// <summary>S2 proof readers (S1 ActorVisual reader idiom): window open /
    /// camera sits EXACTLY on the shake-free follow base.</summary>
    public bool IsShaking => _shakeFramesLeft > 0;
    public bool IsAtShakeBase => _shakeFramesLeft == 0 && GlobalPosition == _followBase;

    public override void _Ready()
    {
        // Robust runtime resolution: this FollowCamera lives on the composition
        // root's Camera node, whose sibling Player is the frame target.
        Target ??= GetNodeOrNull<Node3D>("../Player");
        if (Target != null)
            GlobalPosition = Target.GlobalPosition + Offset;
        _followBase = GlobalPosition;
        TrySubscribe();
    }

    public override void _Process(double delta)
    {
        if (Target == null) return;

        // Headless --script runs load the EventBus autoload AFTER scene
        // _Ready (proof-harness precedent, MC 1344.1): retry until wired.
        if (_bus == null) TrySubscribe();

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
        GlobalPosition = _followBase + kick;
        LookAt(Target.GlobalPosition + new Vector3(0, 1f, 0), Vector3.Up);
    }

    private void TrySubscribe()
    {
        _bus = GetNodeOrNull<EventBus>("/root/EventBus");
        if (_bus == null) return;
        _bus.PlayerHurt += _ => _shakeFramesLeft = JuiceShakeTuning.ShakeFrames;
        _bus.BossFallen += _ => _shakeFramesLeft = JuiceShakeTuning.ShakeFrames;
    }
}
