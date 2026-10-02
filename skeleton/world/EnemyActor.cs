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
        // follow mechanism; KillHide hides it, QueueFree frees it.
        var mesh = ActorVisual.Build(Kind, isBoss);
        AddChild(mesh);
        Visual = mesh;

        var shape = new CollisionShape3D();
        shape.Shape = new BoxShape3D { Size = new Vector3(0.9f, 1.5f, 0.9f) };
        AddChild(shape);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (Ai == null || Ai.IsDead) return;
        float dt = (float)delta;

        // Tick the pure AI toward the player and read the resulting position.
        int dealt = Ai.SetBehavior(new CombatVec3(PlayerTargetX, 0f, PlayerTargetZ), dt);
        DamageDealt = dealt;

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
    }

    /// <summary>The player's XZ position, set by WorldDirector each frame.</summary>
    public float PlayerTargetX { get; set; }
    public float PlayerTargetZ { get; set; }

    /// <summary>Damage this enemy dealt to the player on the last AI tick.</summary>
    public int DamageDealt { get; private set; }

    public void Damage(int amount)
    {
        if (Ai != null && !Ai.IsDead)
            Ai.TakeDamage(amount);
    }

    public void KillHide()
    {
        Visible = false;
        SetPhysicsProcess(false);
        // MC 1348 A2: a dead actor deals no damage. _PhysicsProcess early-returns
        // before the per-tick assignment, so the last value would stay frozen here
        // and the director's damage sum would keep counting it every frame.
        DamageDealt = 0;
    }
}
