using Godot;
using LastAnimal.Combat;

// Last Animal — W3-fix composition (MC 1123.10, artemis, 2026-09-08).
// T3b ownership refactor (MC 1256.9, artemis, 2026-09-21): the AI is INJECTED.
//
// EnemyActor: the VISIBLE enemy body in the playable scene. It wraps a pure
// M08 EnemyAI (C11, engine-free, REUSED verbatim — no AI logic re-authored
// here) in a thin CharacterBody3D shell: gravity + MoveAndSlide so it rests
// on the meadow heightfield terrain exactly like the Player, a composed
// per-type silhouette (MC 3895: ActorVisual, visual-only) so it is actually
// visible AND readable by kind, and velocity toward the AI's target so it
// approaches/chases the player.
//
// Ownership (design 1256.2 §2): the WorldDirector — the ONE composition root —
// constructs the EnemyAI at spawn time and injects it via Configure(). This
// shell no longer self-constructs its AI (the old `new EnemyAI(...)` here was
// the second live AI-construction site); it only ticks and steers the injected
// instance. Movement Y is handled purely by gravity (the AI is planar).
namespace LastAnimal.World;

[GlobalClass]
public partial class EnemyActor : CharacterBody3D
{
    private const float Gravity = 9.8f;

    public EnemyAI Ai { get; private set; } = null!;

    // MC 10420 / 10026.29: the ground bounds the DIRECTOR injects at spawn
    // (world/WorldDirector.Ground.cs resolves them from the terrain meta — one
    // channel with MC 10404's floor_min_y). The pure AI plans UNBOUNDED
    // (patrol re-targets 2-5 m per cycle without limit, flee runs full speed
    // away — the x=66.69 drift class); the clamp lives in this shell, the one
    // place where the plan becomes world position, so the engine-free EnemyAI
    // stays untouched (C11 verbatim) and there is exactly ONE clamp site.
    private Vector2 _groundMin, _groundMax;
    private bool _groundBounded;
    public EnemyAI.Type Kind { get; private set; }
    public bool IsDead => Ai.IsDead;

    /// <summary>MC 3895: Node3D root of the composed ActorVisual (was the
    /// single MeshInstance3D box). LookAt in _PhysicsProcess drives facing.</summary>
    public Node3D? Visual { get; private set; }

    /// <summary>
    /// Configure the enemy: the DIRECTOR-OWNED AI (injected — this shell never
    /// constructs gameplay systems), spawn position and the composed visual
    /// for its type (MC 3895: the old flat red colour parameter is gone — the
    /// silhouette + colour are derived from the AI's EnemyType, boss flag).
    /// </summary>
    public void Configure(EnemyAI ai, float x, float z, bool isBoss = false)
    {
        Kind = ai.EnemyType;
        Ai = ai;
        Position = new Vector3(x, 4f, z);   // spawn above the terrain, gravity settles it down

        // MC 3895: composed per-type silhouette built by ActorVisual (the
        // CompanionVisual pattern, 0b5ada2), replacing the inline BoxMesh all
        // enemies shared. The node keeps the name "Visual". It rides the body
        // (this shell's own movement IS the sim follow), so there is no second
        // follow mechanism; KillHide dissolves it (20f, gone by f+21 — S3), QueueFree frees it.
        var mesh = ActorVisual.Build(Kind, isBoss);
        AddChild(mesh);
        Visual = mesh;

        var shape = new CollisionShape3D();
        shape.Shape = new BoxShape3D { Size = new Vector3(0.9f, 1.5f, 0.9f) };
        AddChild(shape);
    }

    /// <summary>MC 10420: inject the ground XZ bounds (min/max) at spawn —
    /// director-owned like the AI itself (design 1256.2 §2). Absent = the
    /// scene's ground published no extent meta (the director warned LOUD; the
    /// GROUND_CLAMP battery leg goes RED), the shell then steers unbounded as
    /// before rather than silently inventing a wall.</summary>
    public void SetGroundBounds(Vector2 min, Vector2 max)
    {
        _groundMin = min;
        _groundMax = max;
        _groundBounded = true;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (Ai == null || Ai.IsDead) return;
        float dt = (float)delta;

        // Tick the pure AI toward the player and read the resulting position.
        int dealt = Ai.SetBehavior(new CombatVec3(PlayerTargetX, 0f, PlayerTargetZ), dt);
        DamageDealt = dealt;

        // MC 10420 (10026.29): bound the PLAN before it becomes steering, so
        // the body walks TO the ground edge and stops there instead of
        // stepping onto the floorless ring; the plan and the body stay ONE
        // position (design 1256.2 §2) because the clamp below re-anchors the
        // plan to the body every tick.
        if (_groundBounded)
        {
            float px = Mathf.Clamp(Ai.Position.X, _groundMin.X, _groundMax.X);
            float pz = Mathf.Clamp(Ai.Position.Z, _groundMin.Y, _groundMax.Y);
            if (px != Ai.Position.X || pz != Ai.Position.Z)
                Ai.Position = new CombatVec3(px, Ai.Position.Y, pz);
        }

        // Steer the physics body toward the AI's planned XZ position.
        float dx = Ai.Position.X - GlobalPosition.X;
        float dz = Ai.Position.Z - GlobalPosition.Z;
        float dist = Mathf.Sqrt(dx * dx + dz * dz);

        Vector3 vel = Velocity;
        vel.Y -= Gravity * dt;
        if (dist > 0.02f)
        {
            vel.X = dx / dist * Ai.Speed * (Ai.CurrentState == EnemyAI.State.Patrol ? 0.5f : 1f);
            vel.Z = dz / dist * Ai.Speed * (Ai.CurrentState == EnemyAI.State.Patrol ? 0.5f : 1f);
            if (Visual != null)
                Visual.LookAt(new Vector3(GlobalPosition.X + dx, GlobalPosition.Y, GlobalPosition.Z + dz), Vector3.Up);
        }
        else
        {
            vel.X = 0f;
            vel.Z = 0f;
        }
        Velocity = vel;
        MoveAndSlide();

        // MC 10420 belt: the WORLD position ends at the extent even when
        // physics itself displaces the body (collision push, a spawn placed
        // past the edge). One clamp site, one channel — see SetGroundBounds.
        if (_groundBounded)
        {
            Vector3 g = GlobalPosition;
            float x = Mathf.Clamp(g.X, _groundMin.X, _groundMax.X);
            float z = Mathf.Clamp(g.Z, _groundMin.Y, _groundMax.Y);
            if (x != g.X || z != g.Z) GlobalPosition = new Vector3(x, g.Y, z);
        }
    }

    /// <summary>The player's XZ position, set by WorldDirector each frame.</summary>
    public float PlayerTargetX { get; set; }
    public float PlayerTargetZ { get; set; }

    /// <summary>MC 10198 Inc-4 S14: the READ-ONLY velocity feed the motion
    /// driver reads to advance the walk phase (world/WorldDirector.Motion.cs).
    /// Logic state flows out, never in: the driver only writes the visual rig,
    /// never this body.</summary>
    public Vector3 SimVelocity => Velocity;

    /// <summary>Damage this enemy dealt to the player on the last AI tick.</summary>
    public int DamageDealt { get; private set; }

    // MC 10183 S12 row 3 (S3 F-DA2): the dead public Damage(int) seam is GONE —
    // repo-wide grep zero callers (.cs/.gd/.tscn/Call); the ONLY damage path to
    // an enemy is the director's single CombatSystem.DealDamage call site
    // (WorldDirector TryAttack). A wrapper no caller can reach is an inert
    // second damage door; the S3 DA verdict filed it as the dead seam (P4).

    public void KillHide()
    {
        // MC 1348 A2: a dead actor deals no damage. _PhysicsProcess early-returns
        // before the per-tick assignment, so the last value would stay frozen here
        // and the director's damage sum would keep counting it every frame.
        // MC 10129 Inc-3 S3: the death tick now PINS the full suppression — the
        // plan-named collision-off line. ONE API fact pinned here: Godot 4.x
        // collision layer NUMBERS are 1-based, so the plan's literal (0,false)
        // is a SILENT NO-OP (probe: mask stays 1 — see the MC 10129 evidence
        // dir); clearing value 1 drops the default first bit and the mask hits
        // 0: a body on no layer is untouchable by anything. Targeting exclusion
        // REUSES the director's own predicate (world/WorldDirector.cs TryAttack
        // "if (e.IsDead) continue;", mirrored in the damage sum and TryInteract):
        // no second targeting list is added beside it. The old instant
        // Visible=false is replaced by the 20-frame visual dissolve — the visual
        // node frees ITSELF at f+21 (ActorVisual.PlayDeath, F4 counter), then
        // clears this body's presentation reference through OnVisualDespawned.
        SetCollisionLayerValue(1, false);
        SetPhysicsProcess(false);
        DamageDealt = 0;
        ActorVisual.PlayDeath(Visual);
    }

    /// <summary>MC 10129 S3: the visual node has dissolved and freed itself;
    /// null the presentation reference so no caller ever reads a freed wrapper.</summary>
    public void OnVisualDespawned() => Visual = null;
}
