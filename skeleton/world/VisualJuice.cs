using Godot;

// Last Animal — MC 10183 Inc-4 S12 row 2: the S1/S3 juice mechanism split out
// of world/ActorVisual.cs (at-cap hygiene: ActorVisual was 357 l and S14's
// motion pass grows the silhouette builders further — the juice node and the
// static-visual builder are TWO concerns, now one file each). NO behaviour
// change in the move: JuiceTuning + VisualJuice below are the MC 10120 S1 +
// MC 10129 S3 code as ratified (RULING-1 face: presentation transform + tint
// ONLY, decay on INTEGER frame counters, F4).
namespace LastAnimal.World;

/// <summary>Inc-3 juice/dissolve tunables — RULING-1 face, ratified defaults
/// 6f flash / base return at +6f. Consts only: no wall-clock anywhere.</summary>
public static class JuiceTuning
{
    /// <summary>Frames held before the flash/punch return EXACTLY to base.</summary>
    public const int HitFlashFrames = 6;
    public const int PunchReturnFrames = 6;
    /// <summary>MC 10183 R1 (owner-discretion rec, "Rec on all" 2026-10-06):
    /// 0.35 m = 39% of the 0.9 m collision box read as teleport-off-footprint
    /// at the 6f return; 0.30 keeps the beat strictly under a third of it.</summary>
    public const float PunchDistance = 0.30f;
    public const float PunchDirMinLenSq = 1e-6f;   // flatter dirs read as head-on
    /// <summary>MC 10129 S3: death-dissolve length; the visual node despawns
    /// (frees itself) on frame f+21 when the counter drains at f+20.</summary>
    public const int DissolveFrames = 20;
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
    private int _dissolveFrames;               // MC 10129 S3: death-dissolve counter
    private Vector3 _punchDir;
    private Vector3 _basePos = Vector3.Zero;   // captured on _Ready (never written by the sim)
    private Vector3 _baseScale = Vector3.One;  // MC 10129 S3: the BUILT scale (boss 2.75x rides it)

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

    /// <summary>MC 10129 S3: True while the death dissolve is draining.</summary>
    public bool IsDissolving => _dissolveFrames > 0;

    public override void _Ready()
    {
        _basePos = Position;
        _baseScale = Scale;   // MC 10129 S3: Build() already applied the boss scale
    }

    /// <summary>Arm one hit (white tint + full offset). Presentation-only.</summary>
    public void PlayHit(Vector3 hitDirWorld)
    {
        if (_dissolveFrames > 0) return;   // S3: a dying visual takes no new juice
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

    /// <summary>MC 10129 S3: arm one death dissolve (20 frames alpha + scale
    /// down to nothing; the node frees itself at f+21). An in-flight hit juice
    /// is retired to EXACT base first (the killing hit flashed this same frame),
    /// so the dissolve always runs from the ratified base state. Alpha needs
    /// the shared material switched to its alpha-blend transparency once.</summary>
    public void PlayDeath()
    {
        if (_dissolveFrames > 0) return;
        _flashFrames = 0;
        _punchFrames = 0;
        Position = _basePos;
        if (_tint != null)
        {
            _tint.AlbedoColor = _baseAlbedo;
            _tint.Emission = _baseEmission;
            _tint.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;   // alpha fade needs the blend mode; once, never undone
        }
        _dissolveFrames = JuiceTuning.DissolveFrames;
    }

    public override void _Process(double delta)
    {
        if (_dissolveFrames > 0)
        {
            // S3: integer-frame dissolve (F4) — alpha and scale both read the
            // EXACT counter fraction, never a delta accumulation. At the zero
            // tick the node despawns itself: visual gone by f+21.
            _dissolveFrames--;
            float k = (float)_dissolveFrames / JuiceTuning.DissolveFrames;
            if (_tint != null)
                _tint.AlbedoColor = new Color(_baseAlbedo.R, _baseAlbedo.G, _baseAlbedo.B, k);
            Scale = _baseScale * k;
            if (_dissolveFrames == 0)
            {
                if (GetParent() is EnemyActor body) body.OnVisualDespawned();
                QueueFree();
            }
            return;
        }
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
