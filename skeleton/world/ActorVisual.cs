using Godot;
using LastAnimal.Combat;

// Last Animal — MC 3895: composed enemy silhouettes (one per EnemyAI.Type).
// Replaces the single red BoxMesh placeholder every enemy wore in
// EnemyActor.Configure — wolf/bear/spider notwithstanding the old brief
// wording, the live enum is Goblin/Orc/Skeleton/Demon. Mirrors the proven
// CompanionVisual pattern (0b5ada2): this class ONLY builds the static visual
// tree; the follow mechanism stays in the sim-facing body (EnemyActor moves
// its own body toward the AI, so a visual parented to the body rides it —
// ONE visual-follows-sim mechanism, no second one here). A boss is the same
// silhouette at 2.75x with a stronger emissive tint: readable at a glance.
// Visual-only: no colliders, no gameplay state. MC 10120 Inc-3 S1 adds the
// hit juice (white flash + VISUAL-NODE offset punch) on the composed root —
// presentation transform + tint ONLY, decayed on integer frame counters (F4);
// the EnemyActor body physics stay untouched (RULING-1, no physics impulse).
namespace LastAnimal.World;

/// <summary>Inc-3 S1 hit-juice tunables — RULING-1 face, ratified defaults
/// 6f flash / base return at +6f. Consts only: no wall-clock anywhere.</summary>
public static class JuiceTuning
{
    /// <summary>Frames held before the flash/punch return EXACTLY to base.</summary>
    public const int HitFlashFrames = 6;
    public const int PunchReturnFrames = 6;
    public const float PunchDistance = 0.35f;   // short, sub-body-width metres
    public const float PunchDirMinLenSq = 1e-6f;   // flatter dirs read as head-on
}

/// <summary>Builds a composed primitive silhouette for one enemy kind.</summary>
public static class ActorVisual
{
    /// <summary>Boss scale applied to the type's silhouette (2.5–3x band).</summary>
    private const float BossScale = 2.75f;

    /// <summary>MC 10120 S1: fire the hit flash + visual punch on a built visual
    /// root. Presentation-only — never touches the body; non-juice roots no-op.</summary>
    public static void PlayHitFx(Node3D? visual, Vector3 hitDirWorld)
    {
        (visual as VisualJuice)?.PlayHit(hitDirWorld);
    }

    /// <summary>True while the white flash tint is currently applied.</summary>
    public static bool IsHitFlashActive(Node3D? visual) => (visual as VisualJuice)?.IsFlashActive ?? false;

    /// <summary>True when the tint is at its built (base) colours.</summary>
    public static bool IsHitFlashAtBase(Node3D? visual) => (visual as VisualJuice)?.IsFlashAtBase ?? false;

    /// <summary>True when the visual root sits at its exact base Position.</summary>
    public static bool IsVisualAtBase(Node3D? visual) => (visual as VisualJuice)?.IsPositionAtBase ?? false;

    /// <summary>
    /// Distinct, saturated per-type colours, none matching player blue
    /// (0.2,0.6,0.9), companion gold (0.95,0.75,0.15) or the meadow greens —
    /// readable on llvmpipe software GL thanks to the emission pattern.
    /// </summary>
    private static Color AlbedoOf(EnemyAI.Type type) => type switch
    {
        EnemyAI.Type.Goblin => new Color(0.25f, 0.75f, 0.30f),    // bright green
        EnemyAI.Type.Orc => new Color(0.55f, 0.40f, 0.15f),       // muddy brown
        EnemyAI.Type.Skeleton => new Color(0.62f, 0.45f, 0.90f),  // arcane violet (sky-safe)
        _ => new Color(0.65f, 0.08f, 0.45f),                      // Demon: deep magenta
    };

    private static StandardMaterial3D MaterialFor(EnemyAI.Type type, bool isBoss)
    {
        Color c = AlbedoOf(type);
        var mat = new StandardMaterial3D { AlbedoColor = c, Roughness = 0.65f };
        mat.EmissionEnabled = true;
        mat.Emission = isBoss ? c.Lightened(0.25f) : c;
        mat.EmissionEnergyMultiplier = isBoss ? 1.4f : 0.6f;
        return mat;
    }

    private static MeshInstance3D Part(Mesh mesh, Material mat, Vector3 pos, string name) =>
        new() { Name = name, Mesh = mesh, MaterialOverride = mat, Position = pos };

    private static MeshInstance3D Part(Mesh mesh, Material mat, Vector3 pos, Vector3 rot, string name) =>
        new() { Name = name, Mesh = mesh, MaterialOverride = mat, Position = pos, Rotation = rot };

    /// <summary>
    /// Builds the visual root (named "Visual", as the placeholder was) for one
    /// enemy kind. Silhouettes face +X: EnemyActor LookAt-rotates the body and
    /// the +X axis convention is the one the companion visual already uses.
    /// A boss is the same composition scaled by <see cref="BossScale"/>.
    /// </summary>
    public static Node3D Build(EnemyAI.Type type, bool isBoss)
    {
        var mat = MaterialFor(type, isBoss);
        Node3D root = type switch
        {
            EnemyAI.Type.Goblin => BuildGoblin(mat),
            EnemyAI.Type.Orc => BuildOrc(mat),
            EnemyAI.Type.Skeleton => BuildSkeleton(mat),
            _ => BuildDemon(mat),
        };
        root.Name = "Visual";
        if (isBoss)
            root.Scale = new Vector3(BossScale, BossScale, BossScale);
        return root;
    }

    /// <summary>Small hunched humanoid: oversized head forward-low, thin
    /// dangling arms, short legs — a ~1 m croucher.</summary>
    private static Node3D BuildGoblin(Material mat)
    {
        var root = new VisualJuice(mat);
        root.AddChild(Part(new BoxMesh { Size = new Vector3(0.38f, 0.42f, 0.30f) }, mat,
            new Vector3(-0.05f, -0.45f, 0f), "Body"));
        root.AddChild(Part(new BoxMesh { Size = new Vector3(0.30f, 0.28f, 0.28f) }, mat,
            new Vector3(0.22f, -0.32f, 0f), "Head"));
        root.AddChild(Part(new BoxMesh { Size = new Vector3(0.14f, 0.10f, 0.10f) }, mat,
            new Vector3(0.40f, -0.30f, 0f), "Snout"));
        root.AddChild(Part(new BoxMesh { Size = new Vector3(0.16f, 0.09f, 0.34f) }, mat,
            new Vector3(-0.10f, -0.62f, 0f), "Hunch"));
        root.AddChild(Part(new BoxMesh { Size = new Vector3(0.08f, 0.40f, 0.08f) }, mat,
            new Vector3(0.05f, -0.55f, 0.20f), "ArmL"));
        root.AddChild(Part(new BoxMesh { Size = new Vector3(0.08f, 0.40f, 0.08f) }, mat,
            new Vector3(0.05f, -0.55f, -0.20f), "ArmR"));
        root.AddChild(Part(new BoxMesh { Size = new Vector3(0.10f, 0.45f, 0.10f) }, mat,
            new Vector3(-0.02f, -0.95f, 0.10f), "LegL"));
        root.AddChild(Part(new BoxMesh { Size = new Vector3(0.10f, 0.45f, 0.10f) }, mat,
            new Vector3(-0.02f, -0.95f, -0.10f), "LegR"));
        return root;
    }

    /// <summary>Broad brute: wide chest, big shoulders, tusked snout — a heavy
    /// ~1.7 m block with arms held out.</summary>
    private static Node3D BuildOrc(Material mat)
    {
        var root = new VisualJuice(mat);
        root.AddChild(Part(new BoxMesh { Size = new Vector3(0.42f, 0.70f, 0.62f) }, mat,
            new Vector3(0f, -0.35f, 0f), "Torso"));
        root.AddChild(Part(new BoxMesh { Size = new Vector3(0.46f, 0.26f, 0.72f) }, mat,
            new Vector3(0.05f, -0.02f, 0f), "Shoulders"));
        root.AddChild(Part(new BoxMesh { Size = new Vector3(0.30f, 0.28f, 0.30f) }, mat,
            new Vector3(0.18f, 0.20f, 0f), "Head"));
        root.AddChild(Part(new BoxMesh { Size = new Vector3(0.12f, 0.14f, 0.10f) }, mat,
            new Vector3(0.36f, 0.10f, 0.08f), "TuskL"));
        root.AddChild(Part(new BoxMesh { Size = new Vector3(0.12f, 0.14f, 0.10f) }, mat,
            new Vector3(0.36f, 0.10f, -0.08f), "TuskR"));
        root.AddChild(Part(new BoxMesh { Size = new Vector3(0.14f, 0.55f, 0.14f) }, mat,
            new Vector3(0.05f, -0.30f, 0.44f), "ArmL"));
        root.AddChild(Part(new BoxMesh { Size = new Vector3(0.14f, 0.55f, 0.14f) }, mat,
            new Vector3(0.05f, -0.30f, -0.44f), "ArmR"));
        root.AddChild(Part(new BoxMesh { Size = new Vector3(0.16f, 0.55f, 0.16f) }, mat,
            new Vector3(0f, -0.98f, 0.16f), "LegL"));
        root.AddChild(Part(new BoxMesh { Size = new Vector3(0.16f, 0.55f, 0.16f) }, mat,
            new Vector3(0f, -0.98f, -0.16f), "LegR"));
        root.AddChild(Part(new BoxMesh { Size = new Vector3(0.30f, 0.14f, 0.30f) }, mat,
            new Vector3(-0.30f, -0.55f, 0f), "BackPack"));
        return root;
    }

    /// <summary>Tall and skeletal: barrel ribcage with visible rib gaps,
    /// skull with jaw, thin limb sticks — a gaunt ~1.8 m frame.</summary>
    private static Node3D BuildSkeleton(Material mat)
    {
        var root = new VisualJuice(mat);
        root.AddChild(Part(new BoxMesh { Size = new Vector3(0.26f, 0.50f, 0.34f) }, mat,
            new Vector3(0f, -0.35f, 0f), "Ribcage"));
        root.AddChild(Part(new BoxMesh { Size = new Vector3(0.32f, 0.05f, 0.40f) }, mat,
            new Vector3(0f, -0.18f, 0f), "Rib1"));
        root.AddChild(Part(new BoxMesh { Size = new Vector3(0.30f, 0.05f, 0.36f) }, mat,
            new Vector3(0f, -0.32f, 0f), "Rib2"));
        root.AddChild(Part(new BoxMesh { Size = new Vector3(0.28f, 0.05f, 0.32f) }, mat,
            new Vector3(0f, -0.46f, 0f), "Rib3"));
        root.AddChild(Part(new BoxMesh { Size = new Vector3(0.24f, 0.26f, 0.20f) }, mat,
            new Vector3(0.04f, 0.10f, 0f), "Skull"));
        root.AddChild(Part(new BoxMesh { Size = new Vector3(0.16f, 0.07f, 0.14f) }, mat,
            new Vector3(0.20f, 0.00f, 0f), "Jaw"));
        root.AddChild(Part(new BoxMesh { Size = new Vector3(0.06f, 0.55f, 0.06f) }, mat,
            new Vector3(0.02f, -0.45f, 0.24f), "ArmL"));
        root.AddChild(Part(new BoxMesh { Size = new Vector3(0.06f, 0.55f, 0.06f) }, mat,
            new Vector3(0.02f, -0.45f, -0.24f), "ArmR"));
        root.AddChild(Part(new BoxMesh { Size = new Vector3(0.07f, 0.60f, 0.07f) }, mat,
            new Vector3(0f, -0.95f, 0.10f), "LegL"));
        root.AddChild(Part(new BoxMesh { Size = new Vector3(0.07f, 0.60f, 0.07f) }, mat,
            new Vector3(0f, -0.95f, -0.10f), "LegR"));
        return root;
    }

    /// <summary>Apex horned winged form: dark core, swept horns, spread
    /// wings — a ~1.9 m silhouette no other type shares.</summary>
    private static Node3D BuildDemon(Material mat)
    {
        var root = new VisualJuice(mat);
        root.AddChild(Part(new BoxMesh { Size = new Vector3(0.48f, 0.85f, 0.50f) }, mat,
            new Vector3(0f, -0.35f, 0f), "Torso"));
        root.AddChild(Part(new BoxMesh { Size = new Vector3(0.30f, 0.28f, 0.30f) }, mat,
            new Vector3(0.12f, 0.30f, 0f), "Head"));
        root.AddChild(Part(new BoxMesh { Size = new Vector3(0.34f, 0.09f, 0.09f) }, mat,
            new Vector3(0.02f, 0.48f, 0.14f), new Vector3(0f, 0f, 0.9f), "HornL"));
        root.AddChild(Part(new BoxMesh { Size = new Vector3(0.34f, 0.09f, 0.09f) }, mat,
            new Vector3(0.02f, 0.48f, -0.14f), new Vector3(0f, 0f, 0.9f), "HornR"));
        root.AddChild(Part(new BoxMesh { Size = new Vector3(0.55f, 0.06f, 0.70f) }, mat,
            new Vector3(-0.30f, 0.15f, 0.45f), new Vector3(0f, 0f, -0.35f), "WingL"));
        root.AddChild(Part(new BoxMesh { Size = new Vector3(0.55f, 0.06f, 0.70f) }, mat,
            new Vector3(-0.30f, 0.15f, -0.45f), new Vector3(0f, 0f, -0.35f), "WingR"));
        root.AddChild(Part(new BoxMesh { Size = new Vector3(0.12f, 0.50f, 0.12f) }, mat,
            new Vector3(0.05f, -0.45f, 0.34f), "ArmL"));
        root.AddChild(Part(new BoxMesh { Size = new Vector3(0.12f, 0.50f, 0.12f) }, mat,
            new Vector3(0.05f, -0.45f, -0.34f), "ArmR"));
        root.AddChild(Part(new BoxMesh { Size = new Vector3(0.15f, 0.55f, 0.15f) }, mat,
            new Vector3(0f, -0.98f, 0.15f), "LegL"));
        root.AddChild(Part(new BoxMesh { Size = new Vector3(0.15f, 0.55f, 0.15f) }, mat,
            new Vector3(0f, -0.98f, -0.15f), "LegR"));
        root.AddChild(Part(new BoxMesh { Size = new Vector3(0.40f, 0.10f, 0.10f) }, mat,
            new Vector3(-0.40f, -0.70f, 0f), new Vector3(0f, 0f, -0.5f), "Tail"));
        return root;
    }
}

/// <summary>
/// MC 10120 Inc-3 S1: the composed visual root + its hit-juice mechanism
/// (6-frame white flash + offset punch, <see cref="JuiceTuning"/>). The whole
/// silhouette shares the one per-enemy material instance from MaterialFor —
/// white-tinting it IS the "Modulate" flash a MeshInstance3D tree can carry
/// (Node3D has no Modulate; the shared MaterialOverride is its only analogue).
/// The punch writes ONLY this node's local Position (presentation transform;
/// EnemyActor's body physics and the boss Scale/LookAt are untouched). Decay
/// runs on INTEGER frame counters (F4, CalmedWindow idiom): no Tween, no
/// delta, no Timer — the base transform/colours return EXACTLY at +6f.
/// </summary>
[GlobalClass]
public partial class VisualJuice : Node3D
{
    private readonly StandardMaterial3D? _tint;
    private readonly Color _baseAlbedo, _baseEmission;
    private int _flashFrames, _punchFrames;
    private Vector3 _punchDir;
    private Vector3 _basePos = Vector3.Zero;   // captured on _Ready (never written by the sim)

    public VisualJuice() { }

    public VisualJuice(Material built) : this()
    {
        _tint = built as StandardMaterial3D;
        if (_tint != null)
        {
            _baseAlbedo = _tint.AlbedoColor;
            _baseEmission = _tint.Emission;
        }
    }

    public bool IsFlashActive => _flashFrames > 0;

    public bool IsFlashAtBase => _tint != null && _flashFrames == 0
        && _tint.AlbedoColor == _baseAlbedo && _tint.Emission == _baseEmission;

    public bool IsPositionAtBase => Position == _basePos;

    public override void _Ready() => _basePos = Position;

    /// <summary>Arm one hit (white tint + full offset). Presentation-only.</summary>
    public void PlayHit(Vector3 hitDirWorld)
    {
        var d = new Vector3(hitDirWorld.X, 0f, hitDirWorld.Z);
        _punchDir = d.LengthSquared() < JuiceTuning.PunchDirMinLenSq
            ? Vector3.Right
            : d.Normalized();
        _flashFrames = JuiceTuning.HitFlashFrames;
        _punchFrames = JuiceTuning.PunchReturnFrames;
        if (_tint != null)
        {
            _tint.AlbedoColor = Colors.White;
            _tint.Emission = Colors.White;
        }
        Position = _basePos + _punchDir * JuiceTuning.PunchDistance;
    }

    public override void _Process(double delta)
    {
        if (_punchFrames > 0)
        {
            _punchFrames--;
            Position = _punchFrames == 0
                ? _basePos    // exact base return at +6f (no float accumulation)
                : _basePos + _punchDir * JuiceTuning.PunchDistance
                    * _punchFrames / JuiceTuning.PunchReturnFrames;
        }
        if (_flashFrames > 0)
        {
            _flashFrames--;
            if (_flashFrames == 0 && _tint != null)
            {
                _tint.AlbedoColor = _baseAlbedo;    // exact stored colours restored at +6f
                _tint.Emission = _baseEmission;
            }
        }
    }
}
