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
// Visual-only: no colliders, no gameplay state, no per-frame work.
namespace LastAnimal.World;

/// <summary>Builds a composed primitive silhouette for one enemy kind.</summary>
public static class ActorVisual
{
    /// <summary>Boss scale applied to the type's silhouette (2.5–3x band).</summary>
    private const float BossScale = 2.75f;

    /// <summary>
    /// Distinct, saturated per-type colours, none matching player blue
    /// (0.2,0.6,0.9), companion gold (0.95,0.75,0.15) or the meadow greens —
    /// readable on llvmpipe software GL thanks to the emission pattern.
    /// </summary>
    private static Color AlbedoOf(EnemyAI.Type type) => type switch
    {
        EnemyAI.Type.Goblin => new Color(0.25f, 0.75f, 0.30f),    // bright green
        EnemyAI.Type.Orc => new Color(0.55f, 0.40f, 0.15f),       // muddy brown
        EnemyAI.Type.Skeleton => new Color(0.72f, 0.92f, 0.98f),  // ice cyan (sky-safe)
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
        var root = new Node3D();
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
        var root = new Node3D();
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
        var root = new Node3D();
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
        var root = new Node3D();
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
