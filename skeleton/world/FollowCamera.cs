using Godot;
using LastAnimal.Core;

// SIZE reason (file hygiene law, MC 11018/S45): the S45 rig-mode switch grew
// this file 192 -> 320 l by wc AT this head. Kept whole by the card's own
// mechanism-monogamy law — the rig mode is a switch INSIDE the ONE camera;
// splitting rigs across files would author the parallel-camera-class shape
// the owner ruling (MC 10995 append 9 "Toggle") forbids. MC 11129 owner
// playtest build +11 to 331 l by wc AT this head (the ONE boot rig line — a
// print of the already-decided Rig, no new state).

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
//
// MC 11018 / 10026.44.6 S45: the SAME node also carries the fixed isometric
// rig behind a toggle — a rig-mode switch inside the existing camera, NOT a
// parallel camera class (mechanism monogamy). The mode is chosen PER BOOT
// from a session-only launch flag: `--iso-cam` as a cmdline USER arg (after
// the `--` separator). R7-safe: zero save fields, the rig is never
// persisted; a save loads in either rig. DEFAULT OFF = the follow rig below,
// code-identical when the flag is absent. The fixed rig frames
// pivot+offset (pivot = the target position, offset = the SAME exported
// Offset; no smoothing lag) with a CONSTANT orientation, and takes ONE
// clearance mechanism — camera collision against STATIC scenery (pivot
// push-in was the alternative; collision chosen: one bounded ray sweep per
// frame on the sight-line, no pivot mutation). Every juice direction
// (S2 shake / S20 pulse / S20 boss framing) derives from the LIVE base and
// the exported Offset at runtime, so the shipped windows hold under the
// fixed rig with ZERO retuned consts (consumer sweep in the S45 evidence
// names each site; battery leg CAMERA_RIG_ISO pins it).
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

/// <summary>MC 11018 S45: the two rig modes of the ONE camera (no parallel
/// camera class). Follow = the shipped smoothed behind-follow base (default,
/// byte-identical to master). FixedIso = rigid pivot+offset framing with a
/// constant orientation + static-scenery camera collision.</summary>
public enum CameraRigMode { Follow, FixedIso }

[GlobalClass]
public partial class FollowCamera : Node3D
{
    /// <summary>MC 11018 S45: session-only boot choice (R7: never persisted).
    /// The flag is a cmdline USER arg — read ONCE in _Ready, so a mid-session
    /// process could never change what a boot already decided.</summary>
    public static bool IsoCamFlagAtBoot()
    {
        foreach (var a in OS.GetCmdlineUserArgs())
            if (a == IsoCamFlagName) return true;
        return false;
    }
    private const string IsoCamFlagName = "--iso-cam";

    /// <summary>S45 READ seam for proof legs (HasLiveBoss idiom — presentation
    /// authority reads, never set by anyone but _Ready's boot read).</summary>
    public CameraRigMode Rig { get; private set; } = CameraRigMode.Follow;
    public bool RigIsFixedIso => Rig == CameraRigMode.FixedIso;

    // S45 fixed-rig clearance constants (camera collision against STATIC
    // scenery only — dynamic actors on the sight-line are stepped over;
    // consts only, no wall-clock, F4).
    private const float ClearanceMargin = 0.35f; // metres kept off a hit surface
    private const float PivotBubbleM = 1.5f;     // never clamp closer than this
    private const float RayEyeM = 0.75f;         // sight-line origin above the pivot
    private const int ClearanceRaySteps = 4;     // bounded dynamic-step-over walk

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
    // The camera prints ONLY its ONE boot rig line (MC 11129; F4-CMP).
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
        // S45: the ONE boot read of the session-only toggle (DEFAULT OFF:
        // flag absent -> Rig stays Follow, the shipped rig).
        if (IsoCamFlagAtBoot()) Rig = CameraRigMode.FixedIso;
        // MC 11129 owner playtest build: the ONE user-visible boot line naming
        // the ACTIVE rig (owner ruling "Toggle" — he plays both and names the
        // default; a raw terminal flag is not an artifact for him, so the rig
        // he is looking at must be NAMED where he can see it: stdout + the
        // engine user log). Constant text printed ONCE at boot — zero
        // per-frame output, so the F4-CMP marker streams stay clean.
        GD.Print(RigIsFixedIso
            ? "LA_PLAYTEST_RIG=ISO (fixed isometric camera)"
            : "LA_PLAYTEST_RIG=CLASSIC (follow camera)");
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

        // Rig split (S45): the fixed rig re-anchors the base on the pivot with
        // clearance; the FOLLOW path below is the shipped code, byte-unchanged.
        if (RigIsFixedIso)
            _followBase = ClearFixedBase(Target.GlobalPosition);
        else
        {
            float t = 1f - Mathf.Exp(-LerpSpeed * (float)delta);
            _followBase = _followBase.Lerp(Target.GlobalPosition + Offset, t);
        }

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
        if (RigIsFixedIso)
            GlobalRotation = _fixedRotation ??= ComputeFixedRotation();
        else
            LookAt(Target.GlobalPosition + new Vector3(0, 1f, 0), Vector3.Up);
    }

    // S45 fixed-rig state: the bit-locked constant orientation (null until
    // the first fixed frame computes it from CONSTANT data only, so the iso
    // angle never jitters by a bit while the player walks).
    private Vector3? _fixedRotation;
    private Godot.Collections.Array<Rid>? _clearExclude;

    /// <summary>Orientation of the rest pose computed from CONSTANT data only
    /// (the exported Offset + the shipped 1 m up-framing, pivot at the
    /// origin): the real geometry is translation-invariant (from=pivot+Offset,
    /// to=pivot+(0,1,0) share the pivot, which cancels), so computing once
    /// over constants is the same look the follow rig would show at rest —
    /// evaluated bit-locked (F4: no wall-clock, no drift).</summary>
    private Vector3 ComputeFixedRotation()
    {
        var prev = GlobalPosition;
        GlobalPosition = Offset;
        LookAt(new Vector3(0f, 1f, 0f), Vector3.Up);
        var rot = GlobalRotation;
        GlobalPosition = prev;
        return rot;
    }

    /// <summary>S45 occlusion clearance — the ONE mechanism: camera collision
    /// against STATIC scenery (heightfield terrain + static decor; dynamic
    /// actors standing on the sight-line are stepped over, never push the
    /// camera — the companion/followers ride behind the player by design).
    /// Bounded ray sweep from just above the pivot toward the rest pose; a
    /// static hit parks the camera ClearanceMargin off the surface, floored
    /// at the pivot bubble. Deterministic physics query (F4-safe: no
    /// wall-clock), recomputed identically every frame — a bit-still pivot
    /// yields a bit-still cleared base (the exact-rest maths hold).</summary>
    private Vector3 ClearFixedBase(Vector3 pivot)
    {
        Vector3 rest = pivot + Offset;
        Vector3 from = pivot + Vector3.Up * RayEyeM;
        Vector3 dir = rest - from;
        float len = dir.Length();
        if (len <= PivotBubbleM + ClearanceMargin) return rest;
        PhysicsDirectSpaceState3D? space = GetWorld3D()?.DirectSpaceState;
        if (space == null) return rest;

        if (_clearExclude == null || (_clearExclude.Count == 0 && Target is PhysicsBody3D))
        {
            _clearExclude = new Godot.Collections.Array<Rid>();
            if (Target is PhysicsBody3D tb) _clearExclude.Add(tb.GetRid());
        }
        Vector3 unit = dir / len;
        float travelled = 0f;
        for (int i = 0; i < ClearanceRaySteps && travelled < len - ClearanceMargin; i++)
        {
            var query = PhysicsRayQueryParameters3D.Create(from + unit * travelled, rest,
                0xFFFFFFFFu, _clearExclude);
            var hit = space.IntersectRay(query);
            if (hit.Count == 0) break;             // clear all the way to the rest pose
            float d = from.DistanceTo(hit["position"].AsVector3());
            if (hit["collider"].AsGodotObject() is not StaticBody3D)
            { travelled = d + 0.1f; continue; }    // dynamic actor: step past it, keep scanning
            return from + unit * Mathf.Clamp(d - ClearanceMargin, PivotBubbleM, len);
        }
        return rest;
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
