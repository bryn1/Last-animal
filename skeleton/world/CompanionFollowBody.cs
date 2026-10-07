using Godot;
using LastAnimal.Companion;
using LastAnimal.Ui;

// Last Animal — T3b ownership refactor (MC 1256.9, artemis, 2026-09-21).
//
// CompanionFollowBody: the VISIBLE companion body in the playable scene,
// replacing world/CompanionActor.cs (deleted per design 1256.2 §3). The old
// CompanionActor was a bare lerp-follower with NO machine reference — the
// machine and the visible body were two different companions.
//
// This node composes the REAL CompanionEntity (src/companion/CompanionEntity.cs,
// the M05 rig + AnimationPlayer driven by the director-owned machine) as a
// child, and adds the follow-to-player movement the rig entity lacks. The
// director constructs it with the SAME CompanionStateMachine it owns and ticks
// the entity's Advance() in its own _Process — so the machine and the visible
// body are one companion (the integration fix the design names).
//
// It owns NO gameplay logic: movement is presentation, loyalty is M03's,
// behavior is the machine's (I1).
namespace LastAnimal.World;

[GlobalClass]
public partial class CompanionFollowBody : Node3D
{
    /// <summary>The node to follow (the Player).</summary>
    [Export] public Node3D? Target { get; set; }

    /// <summary>Follow distance behind the target.</summary>
    [Export] public float FollowDistance { get; set; } = 3.5f;

    /// <summary>How quickly the companion catches up.</summary>
    [Export] public float LerpSpeed { get; set; } = 3f;

    /// <summary>Height the companion sits above the terrain baseline.</summary>
    [Export] public float Y { get; set; } = 0.55f;

    /// <summary>The machine-wired entity (rig + animation), ticked by the director.</summary>
    public CompanionEntity Entity { get; }

    // --- MC 3943 stage 2g: roster fields on the body (flags + ids only, no
    // gameplay logic — the roster semantics live in WorldDirector.Roster.cs).

    /// <summary>MC 3943 2g: TRUE while this is a WILD creature — not yet in the
    /// CompanionRoster. A wild body stands its ground (Target is null) until
    /// recruited (interact offers, pay_wage pays the FIRST wage; owner D1 cap).</summary>
    public bool Wild { get; set; }

    /// <summary>MC 3943 2g: the creature's world entity id (interact/speak +
    /// dialogue target; becomes the follower's bond id on recruit).</summary>
    public int EntityId { get; set; } = -1;

    /// <summary>MC 3943 2g: set by the interact scan when the player spoke to
    /// this WILD creature — the standing recruit offer the next pay_wage consumes.</summary>
    public bool RecruitOffered { get; set; }

    /// <summary>MC 10031: frames left on the calm window a calming_speak cast
    /// opened (decayed by the Skills partial's sweep; the E interact retires
    /// it — the standing offer is time-unbounded). Runtime-only: the load
    /// seam clears every window with its body (CALM_LOAD_CLEARED).</summary>
    public int CalmedWindowFrames { get; set; }

    /// <summary>MC 3943 2g: the roster stack built for this body (moved in on
    /// recruit; null only for bodies composed outside the roster path).</summary>
    public LastAnimal.Companion.CompanionRoster.Follower? BoundFollower { get; set; }

    /// <summary>MC 10131 S8 Command Bark: frames left on this follower's bark
    /// BUBBLE (the CalmedWindowFrames idiom, MC 10031): an INTEGER window set
    /// by the roster tick's bark poll and swept down by 1 per frame — zero
    /// delta timing (F4), runtime-only (R7: no save field). Presentation
    /// authority: this field writes NOTHING but the bubble's visibility; the
    /// bark READS the roster and never touches gameplay state.</summary>
    public int BarkWindowFrames { get; set; }

    private MeshInstance3D? _barkBubble;

    public CompanionFollowBody(CompanionStateMachine machine, CompanionAnimationHook hook)
    {
        Entity = new CompanionEntity(machine, hook) { Name = "Entity" };
    }

    public override void _Ready()
    {
        AddChild(Entity);

        // A clearly visible companion marker (gold), distinct from player/enemies,
        // so the rig's bones have a readable silhouette on llvmpipe software GL.
        // MC 3889: composed quadruped visual (body + head + 4 legs), built by
        // CompanionVisual — the node is still named "Visual" as before.
        // MC 10145 S9: the head accent follows the bound follower's DERIVED
        // trait; a body composed outside the roster path keeps the base look
        // (Bonded = the pack gold itself).
        AddChild(CompanionVisual.Build(
            BoundFollower?.Trait ?? LastAnimal.Companion.CompanionTrait.Bonded));

        // MC 10131 S8 Command Bark: ONE small primitive above the head, a child
        // of the body like the composed visual (the one visual-follows-sim
        // mechanism — no new widget system). Hidden until the integer bark
        // window opens; the UiTheme white token is an EXISTING palette constant
        // (the S9 no-new-colours rule).
        _barkBubble = new MeshInstance3D
        {
            Name = "BarkBubble",
            Mesh = new BoxMesh { Size = new Vector3(0.16f, 0.16f, 0.16f) },
            MaterialOverride = BarkBubbleMaterial(),
            Position = new Vector3(0.62f, 0.72f, 0f),   // above the Head part
            Visible = false,
        };
        AddChild(_barkBubble);
    }

    /// <summary>Bubble material on the shipped emission idiom (software-GL
    /// readable), carrying ONLY the existing UiTheme token colour.</summary>
    private static StandardMaterial3D BarkBubbleMaterial()
    {
        var mat = new StandardMaterial3D { AlbedoColor = UiTheme.GaugeColor, Roughness = 0.6f };
        mat.EmissionEnabled = true;
        mat.Emission = UiTheme.GaugeColor;
        mat.EmissionEnergyMultiplier = 0.6f;
        return mat;
    }

    public override void _Process(double delta)
    {
        // MC 10131: presentation follows the integer window counter — this line
        // writes NOTHING but the bubble's own visibility (F4 doctrine).
        if (_barkBubble != null) _barkBubble.Visible = BarkWindowFrames > 0;

        if (Target == null) return;

        Vector3 want = Target.GlobalPosition;
        // Keep a set distance behind the target on the XZ plane.
        Vector3 p = GlobalPosition;
        Vector3 d = p - want;
        d.Y = 0f;
        float len = d.Length();
        if (len < 1e-4f)
        {
            want += new Vector3(-FollowDistance, 0f, 0f);
        }
        else
        {
            want += d / len * FollowDistance;
        }
        want.Y = Y;

        float t = 1f - Mathf.Exp(-LerpSpeed * (float)delta);
        GlobalPosition = GlobalPosition.Lerp(want, t);
    }
}
