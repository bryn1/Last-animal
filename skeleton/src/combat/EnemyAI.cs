using System;

// Last Animal — M08 combat-3d EnemyAI (MC 890.13, artemis, 2026-09-06).
//
// C11 (PHASE0.md line 380-381): `EnemyAI.SetBehavior(defender)` state machine
// (patrol|chase|attack|flee); ports prior `EnemyAI.cs` concepts (VERIFIED
// source present at /srv/workspace/animal/src/Animal.Gameplay/EnemyAI.cs).
//
// Port notes (semantics preserved from the prior art, engine-free per I3):
//   - Enemy types Goblin/Orc/Skeleton/Demon and their HP/damage/speed/range
//     table are ported 1:1 (the prior art's Vec2 positions are now XZ-plane
//     CombatVec3; Y is unused for movement). MC 10183 R2 (owner discretion,
//     "Rec on all" 2026-10-06) re-tuned Skeleton to 65/18 — the first and only
//     deviation from the ported table, rationale at the switch arm.
//     MC 10216 / 10026.34 S18 APPENDS Type.Wraith (the zone-4 hollow denizen)
//     after Demon: new authored content, not a port deviation — the ported
//     rows above keep their 1:1 claim. Demon stays the apex BOSS type.
//   - The C11 state set is patrol|chase|attack|flee; the prior art's
//     Idle/Wander pair merges into the single "patrol" state. After an
//     attack/patrol lull the enemy returns to patrol instead of Idle.
//   - `SetBehavior` interprets C11's `defender` parameter as the target the
//     enemy engages (the player's position). It runs one AI tick for the
//     frame and returns the damage dealt to the defender this tick (0 if the
//     enemy did not land a hit). This is my ASSUMED reading of the sparse
//     C11 line (brief was a stub) — the method name and 4-state surface are
//     taken verbatim from the contract; the parameter is the defender state.
//   - Flee triggers when the enemy's health drops below a per-type threshold
//     (same as the prior art's shouldFlee).
namespace LastAnimal.Combat;

/// <summary>
/// Enemy AI state machine (C11). Ports the prior-art EnemyAI concepts:
/// a full patrol|chase|attack|flee cycle driven by the defender's position
/// and the enemy's own health.
/// </summary>
public class EnemyAI
{
    public enum Type { Goblin, Orc, Skeleton, Demon, Wraith }

    public enum State { Patrol, Chase, Attack, Flee }

    public CombatVec3 Position { get; set; }
    public Type EnemyType { get; }
    public State CurrentState { get; private set; }
    public int Health { get; private set; }
    public int MaxHealth { get; private set; }
    public int Damage { get; private set; }
    public float Speed { get; private set; }
    public float AttackRange { get; }
    public float ChaseRange { get; }
    public int EntityId { get; }

    public bool IsDead => Health <= 0;

    private readonly System.Random _rng;
    private CombatVec3 _patrolTarget;
    private float _patrolTimer;
    private float _attackCooldown;
    private readonly float _fleeThreshold;

    public EnemyAI(CombatVec3 position, int entityId, Type fixedType, int? seed = null)
    {
        Position = position;
        EntityId = entityId;
        EnemyType = fixedType;
        _rng = seed.HasValue ? new System.Random(seed.Value) : new System.Random();

        switch (EnemyType)
        {
            case Type.Goblin:
                Health = MaxHealth = 30; Damage = 5; Speed = 3.0f; AttackRange = 1.0f; ChaseRange = 5.0f; _fleeThreshold = 0.3f; break;
            case Type.Orc:
                Health = MaxHealth = 60; Damage = 15; Speed = 1.5f; AttackRange = 1.5f; ChaseRange = 4.0f; _fleeThreshold = 0.2f; break;
            case Type.Skeleton:
                // MC 10183 R2 (owner-discretion rec): 45/10 put the RUINS denizen
                // strictly below the canyon orc once the spawner's depth bonuses
                // apply (45a+25 < 60a+20 hp, 10 < 15 dmg) — the shipped depth ramp
                // inverted. 65/18 restores the monotone ladder (demon 100/25 above).
                Health = MaxHealth = 65; Damage = 18; Speed = 2.0f; AttackRange = 1.2f; ChaseRange = 4.5f; _fleeThreshold = 0.25f; break;
            case Type.Demon:
                Health = MaxHealth = 100; Damage = 25; Speed = 1.0f; AttackRange = 2.0f; ChaseRange = 6.0f; _fleeThreshold = 0.1f; break;
            case Type.Wraith:
                // MC 10216 S18: the zone-4 hollow denizen. 80/20 keeps the MC 10183 R2
                // monotone depth ladder (skeleton 65/18 < wraith 80/20 < demon 100/25);
                // 2.4 speed sits between skeleton's march and goblin's rush — a drifter.
                Health = MaxHealth = 80; Damage = 20; Speed = 2.4f; AttackRange = 1.4f; ChaseRange = 5.0f; _fleeThreshold = 0.2f; break;
            default:
                Health = MaxHealth = 100; Damage = 25; Speed = 1.0f; AttackRange = 2.0f; ChaseRange = 6.0f; _fleeThreshold = 0.1f; break;
        }

        CurrentState = State.Patrol;
        GeneratePatrolTarget();
    }

    /// <summary>
    /// Apply an ecosystem SpawnSet's scaled stats (C15 wire, MC 1344): replace
    /// the hardcoded per-type defaults with the adapted table the spawner
    /// produced, so adaptation has a live effect on the spawned enemy.
    /// </summary>
    public void ApplySpawnStats(int health, int damage, float speed)
    {
        Health = MaxHealth = health;
        Damage = damage;
        Speed = speed;
    }

    /// <summary>
    /// Run one AI tick toward the defender (player). Returns damage dealt to
    /// the defender this tick (0 unless an attack lands).
    /// </summary>
    public int SetBehavior(CombatVec3 defenderPosition, float frameTime)
    {
        if (IsDead) return 0;

        _attackCooldown = Math.Max(0, _attackCooldown - frameTime);

        float distToDefender = Position.DistanceXZ(defenderPosition);
        bool shouldFlee = Health < MaxHealth * _fleeThreshold;

        int damageDealt = 0;
        switch (CurrentState)
        {
            case State.Patrol:
                UpdatePatrol(frameTime);
                break;
            case State.Chase:
                UpdateChase(frameTime, defenderPosition);
                break;
            case State.Attack:
                damageDealt = TryAttack(frameTime);
                break;
            case State.Flee:
                UpdateFlee(frameTime, defenderPosition);
                break;
        }

        // State transitions (priority: flee > attack > chase > patrol).
        if (shouldFlee && !IsDead)
        {
            CurrentState = State.Flee;
        }
        else if (distToDefender <= AttackRange && _attackCooldown <= 0)
        {
            CurrentState = State.Attack;
        }
        else if (distToDefender <= ChaseRange)
        {
            CurrentState = State.Chase;
        }
        else if (CurrentState == State.Attack || CurrentState == State.Chase)
        {
            CurrentState = State.Patrol;
        }

        return damageDealt;
    }

    private void UpdatePatrol(float frameTime)
    {
        _patrolTimer -= frameTime;
        if (_patrolTimer <= 0)
        {
            GeneratePatrolTarget();
        }
        float dist = Position.DistanceXZ(_patrolTarget);
        if (dist > 0.1f)
        {
            Position += Position.DirectionOnXZ(_patrolTarget) * (Speed * 0.5f * frameTime);
        }
        else
        {
            GeneratePatrolTarget();
            _patrolTimer = 1.0f;
        }
    }

    private void UpdateChase(float frameTime, CombatVec3 defender)
    {
        Position += Position.DirectionOnXZ(defender) * (Speed * frameTime);
    }

    private int TryAttack(float frameTime)
    {
        if (_attackCooldown > 0) return 0;
        _attackCooldown = 1.0f;
        return Damage;
    }

    private void UpdateFlee(float frameTime, CombatVec3 defender)
    {
        var away = Position - defender;
        away.Y = 0f;
        float len = away.Length();
        if (len > 1e-6f)
            Position += away * (1f / len) * (Speed * frameTime);

        if (Health > MaxHealth * 0.5f)
        {
            CurrentState = State.Patrol;
            GeneratePatrolTarget();
        }
    }

    private void GeneratePatrolTarget()
    {
        float angle = (float)_rng.NextDouble() * MathF.PI * 2;
        float distance = 2.0f + (float)_rng.NextDouble() * 3.0f;
        _patrolTarget = new CombatVec3(
            Position.X + MathF.Cos(angle) * distance,
            Position.Y,
            Position.Z + MathF.Sin(angle) * distance
        );
        _patrolTimer = 2.0f + (float)_rng.NextDouble() * 3.0f;
    }

    public void TakeDamage(int damage)
    {
        Health = Math.Max(0, Health - damage);
    }
}
