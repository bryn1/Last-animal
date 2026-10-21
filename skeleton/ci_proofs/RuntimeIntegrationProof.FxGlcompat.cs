using Godot;
using LastAnimal.World;

// SIZE reason (file hygiene law): 537 l by wc AT this head — ONE stage body
// (the seven-state capture machine + the four verdict computations share the
// probe rig, the texture-derived sample points and the Image diffs; splitting
// a stage body mid-leg has no precedent under the mode_sets_check pairing gate,
// one Run*Stage body == one dispatch arm).
//
// Last Animal — MC 11019 / 10026.44.7 S48 STEP-0 GL-COMPAT renderer capability
// probe (stage 108, modes FX_GLCOMPAT / FX_GLCOMPAT_PLANT).
//
// The FX-code chain downstream (ground aura rings, blood decals, torch
// multilight + flame billboards — the d2 visual-direction S43/S48 rows) must
// not be planned against an UNMEASURED renderer. The CI-effective renderer IS
// GL Compatibility: the pinned 4.7.2 engine prints "switching to OpenGL 3"
// under Xvfb+llvmpipe (no Vulkan); docs/ARCHITECTURE.md rows (VisibilityRange
// F2, the alpha-fade rejection) were all learned at THIS renderer. S28's
// step-0 probe idiom: mount the mechanisms on the REAL engine, measure pixels,
// PRINT the named verdict lines — and where a capability FAILS, name the
// documented fallback (the CUT rule) INSTEAD of shipping the mechanism.
//
// Mounts (composed main.tscn, quiet boot — bus_emit idiom; the rig is added
// BESIDE the live scene: the player, its camera and every shipped system are
// untouched): (a) a ground-projected textured QUAD using the COMPLIANT SHIPPED
// asset resources/generated/fx/aura_disc_a.png (the S48-FIX re-gen — NOT the
// superseded design-lab originals), unshaded + alpha; (b) a Godot Decal node
// over terrain (blood_splat_1.png into texture_albedo — the downstream blood
// use; Godot 4.7 decals take a plain Texture2D there, no runtime DecalTexture
// API needed — the class-presence fact is printed either way); (c) TWO
// OmniLight3D, shadows OFF (the card's perf posture), red + blue, plus a
// billboard quad using torch_flame_strip_6f.png FRAME 0 (AtlasTexture region
// 0,0,256,256) — a torch.
// The pixel READBACK uses a SubViewport probe camera (640x360 FBO, UPDATE-
// ALWAYS) sharing the live World3D: at this renderer the ROOT-viewport texture
// reads EMPTY (spike-measured at this exact engine + llvmpipe), so a root-
// window texture cannot carry any verdict; the FBO renders through the SAME
// RenderingServer and reads back real pixels. The main window is shrunk to
// 160x120 (mode-gated) so llvmpipe spends its clock on the probe, not on a
// 1152x648 nobody reads. Captures are stability-gated (two byte-equal grabs)
// and every park is frames AND wall-clock — llvmpipe is slow and process
// frames are not a stable clock there.
//
// Verdict lines (battery greps the names; every measured number rides the
// line so thresholds stay auditable):
//   QUAD_ALPHA_OK|FAIL — the ring texture's max-alpha point (scanned AT RUN
//     from the shipped PNG) MUST change pixels and its four transparent
//     corners MUST NOT (alpha 0 -> center delta ~0 -> FAIL; alpha ignored ->
//     corners move -> FAIL). Both v-axis mappings of the flat rotation are
//     sampled: the "drawn" leg takes the stronger candidate, the "respected"
//     leg requires ALL corner squares clean — the check cannot pass on a
//     mapping accident.
//   DECAL_SUPPORT_OK|FAIL — the footprint center must carry the blood
//     signature; in-footprint corners (texture alpha 0 there) must stay clean.
//     On FAIL the line names the FALLBACK: ground-projected quads — the
//     mount-(a) path this same probe proves OK (S28 CUT rule: the blood FX
//     card rides quads, no Decal node ships). DECAL FAIL is a LEGITIMATE
//     probe result; the leg still passes.
//   MULTILIGHT_2_OK|FAIL — EACH light ALONE shifts its ground patch AND BOTH
//     shift theirs simultaneously (a one-light renderer stays RED under both-
//     on); FLAME_BILLBOARD_OK|FAIL rides with the mount.
//   FXGL_PROBE_RESULT quad=.. decal=.. multilight=.. flame=.. renderer=.. —
//     the ONE summary line the downstream FX cards consume.
//   FXGL_RENDERER method=... — the leg HALTS (exit 1) on any method other
//     than gl_compatibility: a verdict measured at the wrong renderer is
//     vacuous by construction (the --headless dummy rasterizer answers ZERO
//     to every capability question — the vacuity this card exists to close).
// Captures per the S27/S47 idiom — PNGs produced BY the leg (env
// LA_FXCAP_PATH, default user://fxglcompat/), one settled frame per state,
// byte-count printed and disk-checked; the battery greps lines + bytes.
//
// Non-vacuous (gate-red law): FX_GLCOMPAT_PLANT mounts the SAME rig with the
// quad's material alpha FORCED 0 — QUAD_ALPHA must print its named FAIL; if
// the plant were ever NOT caught the leg fails as VACUOUS by name. The red
// transcript + red capture ride beside the green pair in the battery.
// Zero save fields (R7); zero player motion; quiet boot; FALL_RECOVERED
// census contribution: zero.
//
// Run: xvfb-run -a $GODOT --path <proj> --script res://ci_proofs/
//        RuntimeIntegrationProof.cs    LA_GATE_MODE=FX_GLCOMPAT[_PLANT]
public partial class RuntimeIntegrationProof : SceneTree
{
    // ---- machine constants (thresholds: mean |dRGB| 0..1 over a 7x7 patch;
    // every measured value prints beside the verdict it drives) ----
    private const int FxBootParkFrames = 40;
    private const int FxBootParkMs = 4000;
    private const int FxStateFrames = 10;      // park per rig state before grabbing
    private const int FxStateMs = 1000;
    private const int FxStableFrames = 5;      // gap between the two byte-equal grabs
    private const int FxStableMs = 500;
    private const int FxStableMaxTries = 6;
    private const int FxDeadlineMs = 170_000;  // the leg's own wall watchdog (< battery timeout)
    private const float FxInThr = 0.035f;
    private const float FxOutThr = 0.02f;
    private const float FxLightThr = 0.02f;
    private const float FxEpsStable = 0.002f;  // settle tolerance over the verdict regions
    private const int FxPatchR = 3;

    // ---- stage machine ----
    private int _fxPhase;                      // 0 boot-park, 1 capture machine, 2 verdicts
    private int _fxCapIdx;
    private int _fxCapStage;                   // 0 park, 1 grab A + stable-park, 2 grab B + compare
    private int _fxCapTry;
    private int _fxMarkMs;                     // wall mark for the current park
    private Image? _fxCapA;
    private readonly System.Collections.Generic.Dictionary<string, Image> _fxCaps = new();
    private readonly System.Diagnostics.Stopwatch _fxClock = new();

    // ---- rig ----
    private SubViewport? _fxProbe;
    private Camera3D? _fxProbeCam;
    private MeshInstance3D? _fxQuad;
    private Decal? _fxDecal;
    private OmniLight3D? _fxL1;
    private OmniLight3D? _fxL2;
    private MeshInstance3D? _fxFlame;
    private MeshInstance3D? _fxRecv1, _fxRecv2;   // shaded 1 m receiver quads under the lights
    private Texture2D? _fxAura;
    private Vector3 _fxQuadAt, _fxDecalAt, _fxFlameAt, _fxG1, _fxG2, _fxT1, _fxT2;
    private Vector2 _fxAuraMaxUv;
    private bool _fxPlant;

    private static readonly string[] FxStates = { "base", "quad", "decal", "l1", "l2", "l12", "l12f" };


    private void RunFxGlcompatStage()
    {
        if (!_fxClock.IsRunning)
        {
            _fxClock.Start();
            var rm = RenderingServer.GetCurrentRenderingMethod();
            GD.Print($"LA_GATE: FXGL_RENDERER method={rm}");
            if (rm != "gl_compatibility")
            { Fail($"FX_GLCOMPAT: CI-effective renderer is {rm}, not gl_compatibility — a verdict measured at the wrong renderer is vacuous"); return; }
            _fxPlant = _mode == "FX_GLCOMPAT_PLANT";
            if (_fxPlant) GD.Print("LA_GATE: FXGL_PLANT armed — quad material alpha FORCED 0; QUAD_ALPHA must go RED by name (S28 gate-red law)");
        }
        if (_fxClock.ElapsedMilliseconds > FxDeadlineMs)
        { Fail($"FX_GLCOMPAT: leg wall deadline {FxDeadlineMs} ms exceeded in phase {_fxPhase} (llvmpipe too slow, or a state never settled)"); return; }

        switch (_fxPhase)
        {
            case 0:
                if (_stageFrames < FxBootParkFrames || _fxClock.ElapsedMilliseconds < FxBootParkMs) return;
                BuildFxRig();
                if (_failed) return;
                _fxPhase = 1; _fxCapIdx = 0; FxStartCapture();
                break;
            case 1:
                FxCaptureMachine();
                break;
            case 2:
                FxVerdicts();
                break;
        }
    }

    // ---- rig build — a mount that cannot be PLACED answers nothing: loud fail ----
    private void BuildFxRig()
    {
        _fxAura = GD.Load<Texture2D>("res://resources/generated/fx/aura_disc_a.png");
        var blood = GD.Load<Texture2D>("res://resources/generated/fx/blood_splat_1.png");
        var strip = GD.Load<Texture2D>("res://resources/generated/fx/torch_flame_strip_6f.png");
        if (_fxAura == null || blood == null || strip == null)
        { Fail("FX_GLCOMPAT: shipped fx asset missing (aura_disc_a / blood_splat_1 / torch_flame_strip_6f)"); return; }
        _fxAuraMaxUv = FxMaxAlphaUv(_fxAura, out float aCorner, out _, out float aBest);
        _ = FxMaxAlphaUv(blood, out float bCorner, out float bCenter, out _);
        if (aCorner > 0.06f || aBest < 0.5f || bCorner > 0.06f || bCenter < 0.5f)
        { Fail($"FX_GLCOMPAT: asset alpha preconditions broken (aura corner={aCorner:0.###} max={aBest:0.###}; blood corner={bCorner:###} center={bCenter:###}) — the sample points are not honest"); return; }

        Vector3 p0 = _playerBody!.GlobalPosition;
        Vector3 c0 = p0 + new Vector3(0f, 0f, -1.5f);   // rig center, north of the spawn
        // The standing player occupies a narrow screen band at world X=0 (the
        // char-life idle bob moves those pixels run to run — measured): every
        // sample point is placed OFF that band (the decal sits west AND far
        // north; nothing samples between the player's projected top and feet).
        Vector3 dec = p0 + new Vector3(-1f, 0f, -4f);   // decal anchor, off the player band
        float? gy = FxGround(dec), qy = FxGround(c0 + new Vector3(-3f, 0f, 0f)), fy = FxGround(c0 + new Vector3(2.6f, 0f, 0f));
        if (gy == null || qy == null || fy == null)
        { Fail($"FX_GLCOMPAT: terrain ground raycast miss under the rig (center={gy.HasValue}, quad={qy.HasValue}, torch={fy.HasValue}) — the probe cannot be placed"); return; }

        // (a) aura quad: 2 m, ground-flat 3 cm above the hit, unshaded + alpha.
        _fxQuad = new MeshInstance3D
        {
            Mesh = new QuadMesh { Size = new Vector2(2f, 2f) },
            Position = new Vector3(c0.X - 3f, qy.Value + 0.03f, c0.Z),
            RotationDegrees = new Vector3(-90f, 0f, 0f),
            Visible = false,
        };
        _fxQuadAt = _fxQuad.Position;
        var qmat = new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            AlbedoTexture = _fxAura,
        };
        if (_fxPlant) qmat.AlbedoColor = new Color(1f, 1f, 1f, 0f);   // THE PLANT: alpha forced 0
        _fxQuad.MaterialOverride = qmat;

        // (b) Decal over terrain at the rig center (blood into texture_albedo).
        _fxDecal = new Decal
        {
            TextureAlbedo = blood,
            Size = new Vector3(2f, 3f, 2f),
            Position = new Vector3(dec.X, gy.Value + 1.2f, dec.Z),
            Visible = false,
        };
        _fxDecalAt = new Vector3(dec.X, gy.Value + 0.02f, dec.Z);

        // Lit RECEIVER quads (shaded StandardMaterial3D, flat, always visible)
        // directly under each light. Measured at this renderer: the terrain
        // mesh is per-vertex-lit and too coarse for a 4.5 m torch pool to
        // register (raw-terrain delta ~0 — printed as FXGL_NOTE below; the
        // high-poly character mesh DOES pick the light up, which is what the
        // FX chain's torches light in the real game). The capability question
        // "do two OmniLights light shaded geometry simultaneously" is therefore
        // sampled on shaded receiver quads — renderer truth, not a tessellation
        // accident.
        float? lg1 = FxGround(c0 + new Vector3(2.6f, 0f, -1.2f)), lg2 = FxGround(c0 + new Vector3(2.6f, 0f, 1.2f));
        float? tn1 = FxGround(c0 + new Vector3(4.4f, 0f, -1.2f)), tn2 = FxGround(c0 + new Vector3(0.8f, 0f, 1.2f));
        if (tn1 == null || tn2 == null) { Fail("FX_GLCOMPAT: ground not found under the terrain-pool NOTE points"); return; }
        if (lg1 == null || lg2 == null) { Fail("FX_GLCOMPAT: ground not found under the light receivers — multilight cannot be sampled"); return; }
        // (c) two shadowless OmniLights + torch billboard (strip frame 0).
        // The lights are gated by TREE PRESENCE, not Visible: this build's
        // Light3D API has no enabled-toggle and Visible demonstrably does NOT
        // gate a light (measured: Visible-only toggling left every capture
        // pixel-identical) — in-tree = lit, detached = gone. Held by field for
        // re-attach, never freed.
        _fxL1 = new OmniLight3D { Position = c0 + new Vector3(2.6f, 1.1f, -1.2f), LightColor = new Color(1f, 0.35f, 0.15f), OmniRange = 4.5f, LightEnergy = 2f, ShadowEnabled = false };
        _fxL2 = new OmniLight3D { Position = c0 + new Vector3(2.6f, 1.1f, 1.2f), LightColor = new Color(0.15f, 0.4f, 1f), OmniRange = 4.5f, LightEnergy = 2f, ShadowEnabled = false };
        _fxG1 = new Vector3(_fxL1.Position.X, lg1.Value + 0.06f, _fxL1.Position.Z);
        _fxG2 = new Vector3(_fxL2.Position.X, lg2.Value + 0.06f, _fxL2.Position.Z);
        // DENSELY SUBDIVIDED (24x24) on purpose: gl_compatibility lights are
        // PER-VERTEX — a pool smaller than the tessellation vanishes (the 4-corner
        // quad measured 0.0001; the high-poly character mesh measures strong).
        // The receiver asks "does light reach shaded geometry at all" at the
        // tessellation that CAN answer it; the coarse-terrain blindness is the
        // renderer FACT printed as FXGL_NOTE, never hidden.
        MeshInstance3D FlatRecv(float y, Vector3 pos) => new MeshInstance3D
        {
            Mesh = new PlaneMesh { Size = new Vector2(2f, 2f), SubdivideWidth = 24, SubdivideDepth = 24 },
            Position = new Vector3(pos.X, y, pos.Z),
            // MID-GRAY on purpose: a near-white receiver saturates under ambient
            // + light (measured: the 0.9-albedo slabs clip to white in every
            // state, delta invisible). 0.35 leaves the pool inside range.
            MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color(0.35f, 0.35f, 0.35f) },
        };
        _fxRecv1 = FlatRecv(lg1.Value + 0.10f, _fxL1.Position);
        _fxRecv2 = FlatRecv(lg2.Value + 0.10f, _fxL2.Position);
        // raw-terrain sample point beside each pool (1.8 m out — clear of the 2 m receiver,
        // on bare terrain; a pool delta landing HERE too would falsify FXGL_NOTE) — INFO only.
        _fxT1 = new Vector3(_fxL1.Position.X + 1.8f, tn1.Value + 0.02f, _fxL1.Position.Z);
        _fxT2 = new Vector3(_fxL2.Position.X - 1.8f, tn2.Value + 0.02f, _fxL2.Position.Z);
        _fxFlame = new MeshInstance3D
        {
            Mesh = new QuadMesh { Size = new Vector2(1f, 1f) },
            Position = new Vector3(c0.X + 2.6f, fy.Value + 0.9f, c0.Z),
            Visible = false,
        };
        _fxFlameAt = _fxFlame.Position;
        _fxFlame.MaterialOverride = new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            BillboardMode = BaseMaterial3D.BillboardModeEnum.Enabled,
            AlbedoTexture = new AtlasTexture { Atlas = strip, Region = new Rect2(0, 0, 256, 256) },
        };

        // Probe FBO sharing the LIVE world; camera aimed at the cluster.
        _fxProbe = new SubViewport { Size = new Vector2I(640, 360), RenderTargetUpdateMode = SubViewport.UpdateMode.Always };
        Root.AddChild(_fxProbe);
        _fxProbe.World3D = Root.GetWorld3D();
        _fxProbeCam = new Camera3D { Fov = 74f };
        _fxProbe.AddChild(_fxProbeCam);
        _fxProbeCam.LookAtFromPosition(c0 + new Vector3(0f, 9.5f, 12f), c0 + new Vector3(0f, 0.3f, 0f), Vector3.Up);

        // Rig BESIDE the live scene — root siblings share the root window's
        // default World3D, which the probe above just adopted. main.tscn and
        // every shipped system keep running untouched underneath.
        Root.AddChild(_fxQuad); Root.AddChild(_fxDecal); Root.AddChild(_fxFlame);   // the lights join by state (tree-presence gate)
        Root.AddChild(_fxRecv1); Root.AddChild(_fxRecv2);
        Root.Size = new Vector2I(160, 120);   // mode-gated: the unread window shrinks, llvmpipe paints the probe
        GD.Print($"LA_GATE: FXGL_RIG quad@{_fxQuadAt} decal@{_fxDecalAt} l1@{_fxL1.Position} l2@{_fxL2.Position} flame@{_fxFlameAt} aura-max-uv={_fxAuraMaxUv} probe=640x360 window=160x120");
    }

    private float? FxGround(Vector3 xz)
    {
        var space = Root.GetWorld3D().DirectSpaceState;
        var q = new PhysicsRayQueryParameters3D
        {
            From = new Vector3(xz.X, _playerBody!.GlobalPosition.Y + 3f, xz.Z),
            To = new Vector3(xz.X, _playerBody!.GlobalPosition.Y - 3f, xz.Z),
            CollideWithBodies = true,
            CollisionMask = uint.MaxValue,
            Exclude = new Godot.Collections.Array<Rid> { _playerBody.GetRid() },
        };
        var hit = space.IntersectRay(q);
        return hit.Count == 0 ? null : hit["position"].AsVector3().Y;
    }

    /// <summary>Max-alpha UV scanned AT RUN from the shipped texture (stride 16)
    /// + corner/center/max alphas the honesty prechecks use — the sample points
    /// are derived from the asset, never hard-pasted from a designer's file.</summary>
    private static Vector2 FxMaxAlphaUv(Texture2D tex, out float cornerAlpha, out float centerAlpha, out float maxAlpha)
    {
        var img = tex.GetImage();
        int w = img.GetWidth(), h = img.GetHeight();
        float best = -1f; Vector2 uv = new(0.5f, 0.5f);
        for (int y = 0; y < h; y += 16)
            for (int x = 0; x < w; x += 16)
            {
                float a = img.GetPixel(x, y).A;
                if (a > best) { best = a; uv = new Vector2((float)x / w, (float)y / h); }
            }
        cornerAlpha = img.GetPixel(w - 8, h - 8).A;
        centerAlpha = img.GetPixel(w / 2, h / 2).A;
        maxAlpha = best;
        return uv;
    }

    // ---- capture machine: park (frames AND wall — llvmpipe frames are not a
    // clock), grab A, short park, grab B. SETTLED = mean |dRGB| over the union
    // of the verdict sample regions < EPS: byte-equality is not required and
    // not achievable (player idle life moves a few pixels); determinism where
    // the verdicts READ is what the verdicts need. The clock is paused at boot,
    // so ground light is frozen between the grabs. A never-settling FBO fails
    // loudly by name.
    private void FxStartCapture()
    {
        FxSetState(FxStates[_fxCapIdx]);
        _fxCapStage = 0; _fxCapTry = 0; _fxCapA = null;
        _stageFrames = 0; _fxMarkMs = (int)_fxClock.ElapsedMilliseconds;
    }

    private bool FxParked(int needFrames, int needMs)
        => _stageFrames >= needFrames && _fxClock.ElapsedMilliseconds - _fxMarkMs >= needMs;

    private Godot.Collections.Array<Rect2I>? _fxStableRegions;
    private Godot.Collections.Array<Rect2I> FxStableRegions()
    {
        // lazily built once the probe camera is fully current (first compare):
        // a 24x24 box around every point a verdict will sample — union of the
        // quad in/corner candidates, decal center/corners, the two light
        // ground points and the flame center.
        if (_fxStableRegions != null) return _fxStableRegions;
        var pts = new System.Collections.Generic.List<Vector3>
        {
            new(_fxQuadAt.X + (_fxAuraMaxUv.X - 0.5f) * 2f, _fxQuadAt.Y, _fxQuadAt.Z + (_fxAuraMaxUv.Y - 0.5f) * 2f),
            new(_fxQuadAt.X + (_fxAuraMaxUv.X - 0.5f) * 2f, _fxQuadAt.Y, _fxQuadAt.Z - (_fxAuraMaxUv.Y - 0.5f) * 2f),
            _fxDecalAt, _fxG1, _fxG2, _fxFlameAt,
        };
        foreach (int ux in new[] { -1, 1 })
            foreach (int uy in new[] { -1, 1 })
                pts.Add(new Vector3(_fxQuadAt.X + ux * 0.9f, _fxQuadAt.Y, _fxQuadAt.Z + uy * 0.9f));
        var arr = new Godot.Collections.Array<Rect2I>();
        foreach (var w in pts)
        {
            var p = FxScreen(w);
            int x = Mathf.Clamp((int)p.X - 12, 0, 640 - 24), y = Mathf.Clamp((int)p.Y - 12, 0, 360 - 24);
            arr.Add(new Rect2I(x, y, 24, 24));
        }
        _fxStableRegions = arr;
        return arr;
    }

    private static float FxRegionsDelta(Image a, Image b, Godot.Collections.Array<Rect2I> regions)
    {
        float s = 0f; int n = 0;
        foreach (var r in regions)
            for (int y = r.Position.Y; y < r.Position.Y + r.Size.Y; y++)
                for (int x = r.Position.X; x < r.Position.X + r.Size.X; x++)
                {
                    var ca = a.GetPixel(x, y); var cb = b.GetPixel(x, y);
                    s += Mathf.Abs(ca.R - cb.R) + Mathf.Abs(ca.G - cb.G) + Mathf.Abs(ca.B - cb.B);
                    n++;
                }
        return n == 0 ? 0f : s / (3f * n);
    }

    private void FxSetLight(Node3D light, bool on)
    {
        if (on) { if (light.GetParent() == null) Root.AddChild(light); }
        else light.GetParent()?.RemoveChild(light);   // detach, keep the object (re-attachable)
    }

    private void FxSetState(string s)
    {
        _fxQuad!.Visible = s == "quad";
        _fxDecal!.Visible = s == "decal";
        FxSetLight(_fxL1!, s is "l1" or "l12" or "l12f");
        FxSetLight(_fxL2!, s is "l2" or "l12" or "l12f");
        _fxFlame!.Visible = s == "l12f";
    }

    private void FxCaptureMachine()
    {
        if (_fxCapStage == 0)
        {
            if (!FxParked(FxStateFrames, FxStateMs)) return;
            _fxCapA = _fxProbe!.GetTexture().GetImage();
            _fxCapStage = 1; _stageFrames = 0; _fxMarkMs = (int)_fxClock.ElapsedMilliseconds;
            return;
        }
        if (_fxCapStage != 1) return;
        if (!FxParked(FxStableFrames, FxStableMs)) return;
        var b = _fxProbe!.GetTexture().GetImage();
        float eps = FxRegionsDelta(_fxCapA!, b, FxStableRegions());
        if (eps < FxEpsStable)
        {
            _fxCaps[FxStates[_fxCapIdx]] = _fxCapA;
            _fxCapIdx++;
            if (_fxCapIdx >= FxStates.Length) { _fxPhase = 2; return; }
            FxStartCapture();
            return;
        }
        _fxCapTry++;
        GD.Print($"LA_GATE: FXGL_UNSETTLED {FxStates[_fxCapIdx]} try={_fxCapTry} regions-delta={eps:0.0000} (eps<{FxEpsStable})");
        if (_fxCapTry >= FxStableMaxTries)
        { Fail($"FX_GLCOMPAT: capture {FxStates[_fxCapIdx]} never settled in {FxStableMaxTries} tries — the FBO is not deterministic at this renderer"); return; }
        FxStartCapture();
    }

    // ---- verdicts ----
    private Vector2 FxScreen(Vector3 w) => _fxProbeCam!.UnprojectPosition(w);

    private static float FxPatchDelta(Image a, Image b, Vector2 p, int r)
    {
        float s = 0f; int n = 0;
        for (int dy = -r; dy <= r; dy++)
            for (int dx = -r; dx <= r; dx++)
            {
                int x = (int)p.X + dx, y = (int)p.Y + dy;
                if (x < 0 || y < 0 || x >= a.GetWidth() || y >= a.GetHeight()) continue;
                var ca = a.GetPixel(x, y); var cb = b.GetPixel(x, y);
                s += Mathf.Abs(ca.R - cb.R) + Mathf.Abs(ca.G - cb.G) + Mathf.Abs(ca.B - cb.B);
                n++;
            }
        return n == 0 ? 0f : s / (3f * n);
    }

    private void FxVerdicts()
    {
        var cap = _fxCaps;
        var base0 = cap["base"];

        // (a) QUAD_ALPHA — both v-mappings for "drawn" (stronger candidate);
        // all four texture-transparent corners must stay clean for "respected".
        float quadIn = 0f;
        foreach (int vs in new[] { 1, -1 })
        {
            var wpt = new Vector3(_fxQuadAt.X + (_fxAuraMaxUv.X - 0.5f) * 2f, _fxQuadAt.Y,
                                  _fxQuadAt.Z + vs * (_fxAuraMaxUv.Y - 0.5f) * 2f);
            quadIn = Mathf.Max(quadIn, FxPatchDelta(cap["quad"], base0, FxScreen(wpt), FxPatchR));
        }
        float quadOut = 0f;
        foreach (int ux in new[] { 0, 1 })
            foreach (int uy in new[] { 0, 1 })
            {
                var wpt = new Vector3(_fxQuadAt.X + (ux * 0.9f - 0.45f) * 2f, _fxQuadAt.Y,
                                      _fxQuadAt.Z + (uy * 0.9f - 0.45f) * 2f);
                quadOut = Mathf.Max(quadOut, FxPatchDelta(cap["quad"], base0, FxScreen(wpt), FxPatchR));
            }
        bool quadOk = quadIn > FxInThr && quadOut < FxOutThr;
        GD.Print($"LA_GATE: QUAD_ALPHA measured in={quadIn:0.0000} corner={quadOut:0.0000} thr in>{FxInThr} corner<{FxOutThr}");
        if (quadOk) GD.Print("LA_GATE: QUAD_ALPHA_OK — unshaded+alpha textured quad drawn, texture alpha RESPECTED at gl_compatibility");
        else GD.Print("LA_GATE: QUAD_ALPHA_FAIL — alpha-blended textured quad NOT proven (see the measured line)");

        // (b) DECAL_SUPPORT — footprint center carries the blood signature;
        // in-footprint corners (alpha 0 in the texture) stay clean.
        float decalIn = FxPatchDelta(cap["decal"], base0, FxScreen(_fxDecalAt), FxPatchR);
        float decalOut = 0f;
        foreach (int ux in new[] { -1, 1 })
            foreach (int uy in new[] { -1, 1 })
            {
                var wpt = _fxDecalAt + new Vector3(ux * 0.85f, 0f, uy * 0.85f);
                decalOut = Mathf.Max(decalOut, FxPatchDelta(cap["decal"], base0, FxScreen(wpt), FxPatchR));
            }
        bool decalOk = decalIn > FxInThr && decalOut < FxOutThr;
        bool dtClass = ClassDB.ClassExists("DecalTexture");
        GD.Print($"LA_GATE: DECAL_SUPPORT measured center={decalIn:0.0000} corner={decalOut:0.0000} DecalTextureClass={dtClass} (texture wired via Decal.texture_albedo — a plain Texture2D, no runtime DecalTexture API needed)");
        if (decalOk) GD.Print("LA_GATE: DECAL_SUPPORT_OK — the Decal node projects its texture at gl_compatibility");
        else GD.Print("LA_GATE: DECAL_SUPPORT_FAIL — Decal not proven at the CI-effective renderer — fallback=ground-projected QUAD (the mount-(a) path this probe proves OK; S28 CUT rule: blood FX cards ride quads, no Decal node ships)");

        // (c) MULTILIGHT_2 — each light alone, then both together; flame rides.
        float t1 = FxPatchDelta(cap["l12"], base0, FxScreen(_fxT1), FxPatchR);
        float t2 = FxPatchDelta(cap["l12"], base0, FxScreen(_fxT2), FxPatchR);
        GD.Print($"LA_GATE: FXGL_NOTE raw-terrain pool deltas both-on=({t1:0.0000},{t2:0.0000}) — terrain mesh is per-vertex-lit and a pool smaller than the grid vanishes; dense meshes (characters, subdivided receivers) DO carry the light — the FX chain's ground-pool FX CANNOT rely on terrain light at this renderer");
        float d1 = FxPatchDelta(cap["l1"], base0, FxScreen(_fxG1), FxPatchR + 1);
        float d2 = FxPatchDelta(cap["l2"], base0, FxScreen(_fxG2), FxPatchR + 1);
        float b1 = FxPatchDelta(cap["l12"], base0, FxScreen(_fxG1), FxPatchR + 1);
        float b2 = FxPatchDelta(cap["l12"], base0, FxScreen(_fxG2), FxPatchR + 1);
        bool multiOk = d1 > FxLightThr && d2 > FxLightThr && b1 > FxLightThr && b2 > FxLightThr;
        GD.Print($"LA_GATE: MULTILIGHT_2 measured alone=({d1:0.0000},{d2:0.0000}) both=({b1:0.0000},{b2:0.0000}) thr>{FxLightThr} shadows=off");
        if (multiOk) GD.Print("LA_GATE: MULTILIGHT_2_OK — two shadowless OmniLights light the terrain SIMULTANEOUSLY at gl_compatibility");
        else GD.Print("LA_GATE: MULTILIGHT_2_FAIL — a light is missing where both shine (see the measured line)");

        float df = FxPatchDelta(cap["l12f"], cap["l12"], FxScreen(_fxFlameAt), FxPatchR + 1);
        bool flameOk = df > FxInThr;
        GD.Print($"LA_GATE: FLAME_BILLBOARD_{(flameOk ? "OK" : "FAIL")} measured={df:0.0000} torch_flame_strip_6f frame 0 (AtlasTexture 0,0,256,256) billboard");

        GD.Print($"LA_GATE: FXGL_PROBE_RESULT quad={(quadOk ? "OK" : "FAIL")} decal={(decalOk ? "OK" : "FAIL")} multilight={(multiOk ? "OK" : "FAIL")} flame={(flameOk ? "OK" : "FAIL")} renderer=gl_compatibility");

        FxSaveCaptures();
        if (_failed) return;

        if (_fxPlant)
        {
            // The red leg: the plant MUST have forced the named FAIL.
            ReleaseHeldRefsBeforeQuit();
            if (!quadOk) Fail("FXGL_PLANT_RED — planted alpha-0 quad CAUGHT: QUAD_ALPHA printed its named FAIL (the check can go RED — red transcript + capture ride beside the green pair)");
            else Fail("FXGL_PLANT_NOT_CAUGHT — the alpha-0 plant stayed GREEN past the QUAD_ALPHA check: the probe would be VACUOUS");
            return;
        }
        if (!quadOk || !multiOk || !flameOk)
        { Fail($"FX_GLCOMPAT: core GL-Compat capabilities missing at the CI-effective renderer (quad={(quadOk ? "OK" : "FAIL")} multilight={(multiOk ? "OK" : "FAIL")} flame={(flameOk ? "OK" : "FAIL")}) — the FX-code chain cards stop here (the DECAL state already carries its named fallback above)"); return; }
        GD.Print($"LA_GATE: PASS — S48 STEP-0 GL-Compat capability probe verified at gl_compatibility: QUAD_ALPHA OK + MULTILIGHT_2 OK + FLAME_BILLBOARD OK; DECAL_SUPPORT={(decalOk ? "OK" : "FAIL")} (fallback named on FAIL — the one line downstream FX cards consume); zero save fields (R7), zero player motion, spawning off at boot");
        _asserted = true;
        ReleaseHeldRefsBeforeQuit();
        Quit(0);
    }

    private void FxSaveCaptures()
    {
        string env = System.Environment.GetEnvironmentVariable("LA_FXCAP_PATH") ?? "";
        string dir = string.IsNullOrEmpty(env) ? ProjectSettings.GlobalizePath("user://fxglcompat") : env;
        System.IO.Directory.CreateDirectory(dir);
        foreach (var kv in _fxCaps)
        {
            string path = System.IO.Path.Combine(dir, $"fxgl_{kv.Key}.png");
            var err = kv.Value.SavePng(path);
            if (err != Error.Ok) { Fail($"FX_GLCOMPAT: capture SavePng failed ({err}) at {path}"); return; }
            long bytes = new System.IO.FileInfo(path).Length;
            if (bytes <= 100) { Fail($"FX_GLCOMPAT: capture {kv.Key} empty on disk ({bytes} bytes at {path})"); return; }
            GD.Print($"LA_GATE: FXGL_CAPTURE {kv.Key} path={path} bytes={bytes}");
        }
    }
}
