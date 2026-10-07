using Godot;
using LastAnimal.Combat;
using System.Collections.Generic;

// Last Animal — MC 10198 Inc-4 S14: character life. Procedural motion on the
// PRESENTATION roots ONLY (RULING-1 class): the physics bodies' GlobalPosition
// is never touched by motion (CHAR_BODY_STILL pins it — the planted
// motion-on-body goes RED). This partial owns the driver + the per-actor rigs;
// WorldDirector.cs grows ONLY the two one-line triggers that ride the shipped
// hunks (attack lean on the S1 hit hunk, player hurt juice on the damage site).
//
// F4: the walk phase is an INTEGER physics-tick counter gated by the body's
// planar velocity — the visual READS logic state, never writes it, and the
// counter never accumulates delta time (the planted delta-time phase goes RED
// at the CHAR-phase pin). Offsets are a pure function of the counter, so the
// at-rest return writes base + amp*Sin(0) = EXACT base (bit-equal, no drift).
//
// Single-writer per property (the reason the rig animates a dedicated "Lean"
// child and the named limb parts, never the root):
//   VisualJuice root  — Position (S1 punch) + tint (S1 flash) + Scale (S3 dissolve)
//   EnemyActor shell  — root Rotation.Y via LookAt (facing)
//   THIS driver       — "Lean".Rotation + limb/torso part Positions (and nothing else)
namespace LastAnimal.World;

/// <summary>MC 10198 S14 motion tuning — the one const block. Integers are
/// FRAMES (F4), amplitudes metres/radians; no wall-clock anywhere.</summary>
public static class MotionTuning
{
    /// <summary>Planar speed below which the walk phase sits pinned at 0 (rest).</summary>
    public const float WalkMinSpeed = 0.35f;
    /// <summary>Speed at which the velocity-derived walk lean reaches full amplitude.</summary>
    public const float WalkLeanRefSpeed = 3.0f;
    public const float WalkLeanMaxRad = 0.15f;
    /// <summary>Attack-lean decay: the same integer 6f beat as the S1 juice.</summary>
    public const int AttackLeanFrames = 6;
    public const float AttackLeanMaxRad = 0.22f;
    /// <summary>Idle breath/sway period + amplitudes (base-crossing per cycle:
    /// Sin(0) is exact, so the at-rest EXACT-base readout always lands).</summary>
    public const int BreathCycleFrames = 96;
    public const float BreathAmpM = 0.015f;
    public const float IdleSwayMaxRad = 0.03f;

    /// <summary>Per-type gait: stride cycle (integer frames) + bob amplitudes
    /// (m) + yaw sway (rad). Small/fast types bob quick and light, big/slow
    /// types roll long and heavy.</summary>
    public static (int Cycle, float Leg, float Arm, float Yaw) GaitOf(EnemyAI.Type type) => type switch
    {
        EnemyAI.Type.Goblin   => (12, 0.09f, 0.06f, 0.10f),
        EnemyAI.Type.Orc      => (20, 0.12f, 0.08f, 0.05f),
        EnemyAI.Type.Skeleton => (16, 0.10f, 0.07f, 0.08f),
        EnemyAI.Type.Wraith   => (14, 0.07f, 0.05f, 0.12f),   // MC 10216 S18 ENEMY FOUR: light drift + wide sway (leg amp rides nothing — no leg parts)
        _                     => (18, 0.11f, 0.07f, 0.07f),   // Demon
    };

    // Player rig (main.tscn composed Visual): arms-only silhouette, mid gait.
    public const int PlayerCycle = 14;
    public const float PlayerArmAmp = 0.07f;
    public const float PlayerYawAmp = 0.05f;
}

public partial class WorldDirector
{
    /// <summary>One actor's motion rig: the animated parts with their base
    /// transforms snapshotted ONCE at bind time, and the integer counters.</summary>
    private sealed class MotionRig
    {
        public Node3D Visual = null!;     // composed root (a VisualJuice)
        public Node3D Lean = null!;       // the rig's ONE rotation writer target
        public EnemyActor? Body;          // null = the player rig
        public Node3D? LegL, LegR, ArmL, ArmR, Breath;
        public Vector3 LegLBase, LegRBase, ArmLBase, ArmRBase, BreathBase;
        public int Cycle;
        public float LegAmp, ArmAmp, YawAmp;
        public int Phase;                 // walk counter, 0 = EXACT base (F4)
        public int BreathPhase;
        public int LeanFrames;            // attack-lean countdown (player rig arms it)
        public float LeanYaw;             // lean faces the struck enemy (player rig)
    }

    private readonly List<MotionRig> _motionRigs = new();
    private bool _playerRigTried;         // resolved once on the first tick

    /// <summary>Motion tick — the single WorldDirector._PhysicsProcess override
    /// lives in the DayNight partial (MC 10199) and invokes this FIRST, before
    /// its own guards, so every physics tick reaches both drivers (merge
    /// integration W2: two partials cannot both override it — CS0111).
    /// delta is READ-BUT-NEVER-USED by contract: every counter advances by one
    /// per tick, never by wall-clock (F4).</summary>
    private void MotionPhysicsTick(double delta)
    {
        if (!_playerRigTried)
        {
            _playerRigTried = true;
            TryAddMotionRig(Player);      // no rig (pre-S14 scene) = driver no-ops
        }

        for (int i = _motionRigs.Count - 1; i >= 0; i--)
        {
            var rig = _motionRigs[i];
            // Prune rigs whose actor dissolved/freed (S3 despawn, zone clear).
            if (!GodotObject.IsInstanceValid(rig.Visual) ||
                (rig.Body != null && (!GodotObject.IsInstanceValid(rig.Body) || rig.Body.Visual == null)))
            {
                _motionRigs.RemoveAt(i);
                continue;
            }
            // A corpse (KillHide'd body) feeds rest — it must never walk.
            Vector3 feed = rig.Body != null && rig.Body.IsDead ? Vector3.Zero : BodyVelocityOf(rig);
            TickMotionRig(rig, feed);
        }

        foreach (var e in _enemies)
            if (e.Visual != null && FindMotionRig(e.Visual) == null)
                TryAddMotionRig(e);
    }

    /// <summary>Read-only velocity feed: the player shell exposes nothing new —
    /// its CharacterBody3D.Velocity IS the logic state the visual reads; enemies
    /// ride their SimVelocity seam (EnemyActor.cs, MC 10198).</summary>
    private static Vector3 BodyVelocityOf(MotionRig rig)
        => rig.Body != null ? rig.Body.SimVelocity
                            : (rig.Visual.GetParent() is CharacterBody3D pb ? pb.Velocity : Vector3.Zero);

    private void TryAddMotionRig(EnemyActor e)
    {
        var rig = BuildMotionRig(e.Visual, e);
        if (rig == null) return;
        (rig.Cycle, rig.LegAmp, rig.ArmAmp, rig.YawAmp) = MotionTuning.GaitOf(e.Kind);
        _motionRigs.Add(rig);
    }

    private void TryAddMotionRig(Node3D? playerNode)
    {
        var visual = playerNode?.GetNodeOrNull<Node3D>("Visual");
        var rig = BuildMotionRig(visual, null);
        if (rig == null) return;
        rig.Cycle = MotionTuning.PlayerCycle;
        rig.LegAmp = 0f;                  // the player silhouette has no leg parts
        rig.ArmAmp = MotionTuning.PlayerArmAmp;
        rig.YawAmp = MotionTuning.PlayerYawAmp;
        _motionRigs.Add(rig);
    }

    /// <summary>Binds a rig: the parts must live under the "Lean" node that
    /// ActorVisual.Build (enemies) and main.tscn (player) compose. Base
    /// transforms are captured here, once, BEFORE the driver ever writes.</summary>
    private static MotionRig? BuildMotionRig(Node3D? visual, EnemyActor? body)
    {
        if (visual is not VisualJuice || visual.GetNodeOrNull<Node3D>("Lean") is not Node3D lean)
            return null;
        var r = new MotionRig { Visual = visual, Lean = lean, Body = body };
        r.LegL = LimbPart(lean, "LegL", out r.LegLBase);
        r.LegR = LimbPart(lean, "LegR", out r.LegRBase);
        r.ArmL = LimbPart(lean, "ArmL", out r.ArmLBase);
        r.ArmR = LimbPart(lean, "ArmR", out r.ArmRBase);
        r.Breath = LimbPart(lean, "Torso", out r.BreathBase)
                ?? LimbPart(lean, "Body", out r.BreathBase)
                ?? LimbPart(lean, "Ribcage", out r.BreathBase);
        return r;
    }

    private static Node3D? LimbPart(Node3D lean, string partName, out Vector3 basePos)
    {
        var part = lean.GetNodeOrNull<Node3D>(partName);
        basePos = part?.Position ?? Vector3.Zero;
        return part;
    }

    private MotionRig? FindMotionRig(Node3D visual)
    {
        foreach (var r in _motionRigs)
            if (ReferenceEquals(r.Visual, visual)) return r;
        return null;
    }

    /// <summary>Advance one rig. Every write is base + f(integer counter):
    /// deterministic, and EXACTLY the captured base whenever the counter is 0
    /// (Sin(0f) = 0f, base + 0f == base bit-for-bit).</summary>
    private static void TickMotionRig(MotionRig r, Vector3 bodyVelocity)
    {
        float speed = new Vector2(bodyVelocity.X, bodyVelocity.Z).Length();
        r.Phase = speed > MotionTuning.WalkMinSpeed ? (r.Phase + 1) % r.Cycle : 0;
        r.BreathPhase = (r.BreathPhase + 1) % MotionTuning.BreathCycleFrames;

        float walk = Mathf.Sin(Mathf.Tau * r.Phase / r.Cycle);       // 0f exactly at rest
        float breath = Mathf.Sin(Mathf.Tau * r.BreathPhase / MotionTuning.BreathCycleFrames);

        if (r.LegL != null) r.LegL.Position = r.LegLBase + Vector3.Up * (r.LegAmp * walk);
        if (r.LegR != null) r.LegR.Position = r.LegRBase - Vector3.Up * (r.LegAmp * walk);
        if (r.ArmL != null) r.ArmL.Position = r.ArmLBase - Vector3.Up * (r.ArmAmp * walk);
        if (r.ArmR != null) r.ArmR.Position = r.ArmRBase + Vector3.Up * (r.ArmAmp * walk);
        if (r.Breath != null) r.Breath.Position = r.BreathBase + Vector3.Up * (MotionTuning.BreathAmpM * breath);

        if (r.LeanFrames > 0)             // attack lean decaying (player rig only)
        {
            float k = r.LeanFrames / (float)MotionTuning.AttackLeanFrames;
            r.LeanFrames--;
            r.Lean.Rotation = new Vector3(MotionTuning.AttackLeanMaxRad * k, r.LeanYaw * k, 0f);
        }
        else if (r.Phase == 0)            // rest: idle sway, exact zero at breath 0
        {
            r.Lean.Rotation = new Vector3(0f, MotionTuning.IdleSwayMaxRad * breath, 0f);
        }
        else                              // walking: velocity-fed lean + yaw sway
        {
            float lean = MotionTuning.WalkLeanMaxRad * Mathf.Min(speed / MotionTuning.WalkLeanRefSpeed, 1f);
            // Enemy silhouettes face +X (pitch rides Z), the player +Z (pitch rides X).
            r.Lean.Rotation = r.Body != null
                ? new Vector3(0f, r.YawAmp * walk, -lean)
                : new Vector3(lean, r.YawAmp * walk, 0f);
        }
    }

    /// <summary>MC 10198 S14: the ONE-line lean trigger riding the shipped S1
    /// hit hunk (WorldDirector.cs TryAttack). Presentation-only: it arms the
    /// player rig's integer lean countdown; the body gets nothing.</summary>
    private void TriggerAttackLean(Vector3 hitDirWorld)
    {
        var rig = PlayerMotionRig();
        if (rig == null) return;
        var d = new Vector2(hitDirWorld.X, hitDirWorld.Z);
        rig.LeanYaw = d.LengthSquared() < 1e-6f ? 0f : Mathf.Atan2(d.X, d.Y);
        rig.LeanFrames = MotionTuning.AttackLeanFrames;
    }

    /// <summary>MC 10198 S14: the player visual adopts the S1 juice at the REAL
    /// player-damage site (the same hunk PlayerHurt's health drop originates):
    /// white flash + offset punch on the class-swapped VisualJuice root.</summary>
    private void PlayerHurtMotion()
    {
        var rig = PlayerMotionRig();
        if (rig == null) return;
        Vector3 source = Vector3.Right;   // a damage edge with no reachable dealer punches right
        float best = float.MaxValue;
        Vector3 ppos = Player?.GlobalPosition ?? Vector3.Zero;
        foreach (var e in _enemies)
        {
            if (e.IsDead || !GodotObject.IsInstanceValid(e)) continue;
            float d = (e.GlobalPosition - ppos).LengthSquared();
            if (d < best) { best = d; source = e.GlobalPosition; }
        }
        ActorVisual.PlayHitFx(rig.Visual, source - ppos);
    }

    private MotionRig? PlayerMotionRig()
    {
        foreach (var r in _motionRigs)
            if (r.Body == null) return r;
        return null;
    }

    // --- read surface the runtime proof reads (proof-only, no logic) --------

    /// <summary>The rig's integer walk phase (-1 = no rig bound). 0 means the
    /// driver is EXACTLY at base this tick.</summary>
    public int WalkPhaseOf(Node3D? visual)
        => visual != null && FindMotionRig(visual) is { } rig ? rig.Phase : -1;

    /// <summary>True while the driver is currently holding an off-base walk/lean
    /// pose (yaw or bob) on the rig's own properties.</summary>
    public bool IsMotionVisualOffBase(Node3D? visual)
    {
        if (visual == null || FindMotionRig(visual) is not { } r) return false;
        return r.Lean.Rotation != Vector3.Zero || PartOffBase(r.LegL, r.LegLBase) || PartOffBase(r.LegR, r.LegRBase)
            || PartOffBase(r.ArmL, r.ArmLBase) || PartOffBase(r.ArmR, r.ArmRBase) || PartOffBase(r.Breath, r.BreathBase);
    }

    /// <summary>True only in the EXACT-base state: every counter at 0 and every
    /// driven transform bit-equal to its captured base.</summary>
    public bool IsVisualMotionAtBase(Node3D? visual)
    {
        if (visual == null || FindMotionRig(visual) is not { } r) return false;
        return r.Phase == 0 && r.BreathPhase == 0 && r.LeanFrames == 0
            && r.Lean.Rotation == Vector3.Zero
            && !PartOffBase(r.LegL, r.LegLBase) && !PartOffBase(r.LegR, r.LegRBase)
            && !PartOffBase(r.ArmL, r.ArmLBase) && !PartOffBase(r.ArmR, r.ArmRBase)
            && !PartOffBase(r.Breath, r.BreathBase);
    }

    /// <summary>True while an attack-lean countdown is armed (the hit hunk fired).</summary>
    public bool IsAttackLeanActive(Node3D? visual)
        => visual != null && FindMotionRig(visual) is { } rig && rig.LeanFrames > 0;

    private static bool PartOffBase(Node3D? part, Vector3 basePos) => part != null && part.Position != basePos;
}
