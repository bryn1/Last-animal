using Godot;
using LastAnimal.Companion;
using LastAnimal.Ui;

// Last Animal — MC 3889: composed companion visual (quadruped silhouette).
// Replaces the single gold capsule placeholder built inline in
// CompanionFollowBody._Ready. One concern: building the static visual tree.
// Keeps the gold marker colour so the rig stays readable on software GL, and
// stays ~1.1 m tall — clearly smaller than the ~1.8 m composed player.
// MC 10145 S9: per-trait ACCENT on the head — the trait (CompanionRoster.
// TraitFor, derived never persisted) picks the head material's colour from
// the EXISTING palette constants only (UiTheme tokens + the companion gold):
// zero new colours, zero new parts.
namespace LastAnimal.World;

/// <summary>Builds the companion's composed body: torso + head + four legs.</summary>
public static class CompanionVisual
{
    /// <summary>The companion gold, the colour the placeholder always carried
    /// (MC 10145: lifted to the constant it always was — same values).</summary>
    private static readonly Color Gold = new(0.95f, 0.75f, 0.15f);

    /// <summary>Shared gold material matching the old placeholder's colours.</summary>
    private static StandardMaterial3D GoldMaterial()
    {
        var mat = new StandardMaterial3D { AlbedoColor = Gold, Roughness = 0.6f };
        mat.EmissionEnabled = true;
        mat.Emission = new Color(0.85f, 0.65f, 0.1f);
        mat.EmissionEnergyMultiplier = 0.6f;
        return mat;
    }

    /// <summary>S9 per-trait ACCENT (RULING-4, presentation only): strictly
    /// the EXISTING palette constants — Steadfast the white gauge token,
    /// Forager the Empathy-book lilac, Sentinel the vignette alert red,
    /// Bonded the pack gold itself (no visible head delta: bonded to the
    /// pack IS the base visual). No new colours (plan S9 hard rule).</summary>
    private static Color AccentOf(CompanionTrait trait) => trait switch
    {
        CompanionTrait.Steadfast => UiTheme.GaugeColor,
        CompanionTrait.Forager => UiTheme.EmpathyTextColor,
        CompanionTrait.Sentinel => UiTheme.VignetteColor,
        _ => Gold,   // CompanionTrait.Bonded
    };

    private static StandardMaterial3D AccentMaterial(CompanionTrait trait)
    {
        Color c = AccentOf(trait);
        var mat = new StandardMaterial3D { AlbedoColor = c, Roughness = 0.6f };
        mat.EmissionEnabled = true;
        mat.Emission = c;
        mat.EmissionEnergyMultiplier = 0.6f;
        return mat;
    }

    private static MeshInstance3D Part(BoxMesh mesh, Material mat, Vector3 pos, string name) =>
        new() { Name = name, Mesh = mesh, MaterialOverride = mat, Position = pos };

    /// <summary>Builds the visual root (named "Visual", as the placeholder was).
    /// <paramref name="accent"/> is the follower's derived trait; the head
    /// carries its accent colour, the body stays pack gold.</summary>
    public static Node3D Build(CompanionTrait accent)
    {
        var root = new Node3D { Name = "Visual" };
        var mat = GoldMaterial();

        // Body along +X (the follow rig has no facing rotation; +X keeps the
        // silhouette axis-stable like the capsule it replaces).
        root.AddChild(Part(new BoxMesh { Size = new Vector3(0.9f, 0.4f, 0.45f) }, mat,
            new Vector3(0f, 0.12f, 0f), "Body"));
        root.AddChild(Part(new BoxMesh { Size = new Vector3(0.32f, 0.32f, 0.34f) }, AccentMaterial(accent),
            new Vector3(0.62f, 0.3f, 0f), "Head"));

        // Four legs: front/rear on X, left/right on Z.
        root.AddChild(Part(new BoxMesh { Size = new Vector3(0.11f, 0.55f, 0.11f) }, mat,
            new Vector3(0.32f, -0.38f, 0.15f), "LegFL"));
        root.AddChild(Part(new BoxMesh { Size = new Vector3(0.11f, 0.55f, 0.11f) }, mat,
            new Vector3(0.32f, -0.38f, -0.15f), "LegFR"));
        root.AddChild(Part(new BoxMesh { Size = new Vector3(0.11f, 0.55f, 0.11f) }, mat,
            new Vector3(-0.32f, -0.38f, 0.15f), "LegBL"));
        root.AddChild(Part(new BoxMesh { Size = new Vector3(0.11f, 0.55f, 0.11f) }, mat,
            new Vector3(-0.32f, -0.38f, -0.15f), "LegBR"));

        return root;
    }
}
