using Godot;
using LastAnimal.World;

// SIZE reason (file hygiene law): 581 l by wc AT this head (the MC 11113
// REAL-MOUNT rewrite grew the 11088 leg's 382 — travel choreography, the
// two-pass wash-measured point selector and the plant-after-selection machine
// are all ONE stage body): splitting a stage body mid-leg has no pairing-gate
// precedent (body == arm, MC 10218).
//
// Last Animal — MC 11113 / 10026.44.12 S50-SKY: the SKY_STRIP leg UPGRADED IN
// PLACE from the 11088 CI-side proxy to a REAL-MOUNT assertion. Option (b)
// (ruling in docs/ARCHITECTURE.md §3 + the LEDGER row) wires the shipped
// canyon_sky_v1 strip into the PRODUCT as a horizon backdrop node authored
// into zones/canyon/canyon.tscn — mounted by the ONE WorldMount route
// (WorldDirector.Mount.cs, MC 10519/S27) when EnterZone mounts the canyon
// scene. The ProceduralSkyMaterial + DayNight per-frame sky law is KEPT
// (option (a) rejected: the DayNight/ZoneMount bit-exact pins stay green,
// zero retired pins). The old leg mounted a harness-authored quad BESIDE the
// composed scene — that proxy mount is REPLACED here: this leg now TRAVELS
// (one shipped "travel" press, ZoneMount idiom), asserts the REAL backdrop
// node exists in the MOUNTED Canyon scene, and pixel-measures THAT node.
// Zero-consumer honesty: the strip's NAMED zero-consumer ruling (11088) is
// retired by this card for canyon_sky_v1; hollow_icons stays zero-consumer
// (occupancy/HUD wiring ruling still open — untouched).
//
// REAL-MOUNT asserts (loud, named — a mount that cannot be FOUND answers
// nothing): the mounted Canyon root carries MeshInstance3D "SkyStrip"; its
// material_override is a StandardMaterial3D whose albedo_texture is the
// SHIPPED res://resources/generated/sky/canyon_sky_v1.png; its mesh is a
// QuadMesh at the strip's NATIVE 4:1 aspect (zero stretch — the law moved
// from the CI rig into the product mount). Any miss = named FAIL, not a
// silently-skipped leg.
//
// Day-night: the leg drives the shipped presentation seam — un-pause so the
// driver re-binds the MOUNTED env (S27 re-bind), then SetDayNightFrame
// (DayNightClock.NightFrame) — the capture-anchor park (S15 seam, capture
// scripts park the clock; exact anchor row writes BIT-EXACTLY) — then pause.
// The NIGHT anchor is the honest worst case for the PAINT delta: canyon's
// night procedural sky is the shipped darkest band, so a warm painted strip
// that cannot paint over it cannot paint at all. The park is READ BACK
// against DayNightClock.Evaluate (marker FXSKY_CLOCK_PARKED) — the same
// bit-exact read family the DAYNIGHT/ZoneMount legs own, printed here as the
// proof that the procedural sky is ALIVE underneath the strip (option (b)
// keeps it; the strip is a consumer of art, not a second sky).
//
// READBACK: a SubViewport probe FBO (640x360, UPDATE-ALWAYS) sharing the live
// World3D, its camera mirroring the SHIPPED FollowCamera rig (live Camera3D
// global position/rotation/FOV — the captured frame is the player's view,
// captured from inside the running game; at this renderer the ROOT-window
// texture reads EMPTY — measured MC 11019 step-0 — so the FBO renders through
// the SAME RenderingServer and is the honest frame-capture idiom here; the
// main window shrinks to 160x120 so llvmpipe paints the probe). The renderer
// is PINNED (FXSKY_RENDERER): any other method exits 1 — a verdict at the
// wrong renderer is vacuous by construction.
//
// Sample points are RUN-DERIVED from the shipped PNG (the FxMaxAlphaUv law:
// the points are derived from the bytes, never hard-pasted): a candidate grid
// over the central band, block-mean colors, the triple with the WIDEST mutual
// color spread kept; a strip too flat to prove its own mapping fails the leg
// LOUD at the rig ("not honest sample points"). Verdicts (battery greps the
// names, every measured margin prints beside its line):
//   SKY_STRIP_RENDER_OK|FAIL — at EVERY expected screen point the mounted
//     strip MUST (a) PAINT: |dRGB| against the SAME view with the backdrop
//         HIDDEN exceeds SkyInThr;
//     (b) MAP AS AUTHORED: the painted patch color is NEARER to its own asset
//         block than to ANY other sample's (projected through the REAL node's
//         GlobalTransform — a swapped/generic quad cannot pass);
//     (c) stay within SkyMatchThr of its own asset color — DOCUMENTED from
//         this head's measured green transcript (an unshaded texture behind
//         canyon's fog + tonemap/adjust grade lands near its bytes; the wash
//         margin is printed either way).
// Non-vacuous (gate-red law): SKY_STRIP_PLANT replaces the REAL backdrop's
// material_override with an alpha-0 copy (the FX_GLCOMPAT_PLANT mechanism
// applied to the PRODUCT mount — a TRANSPARENT shipped strip), the verdict
// must print SKY_STRIP_RENDER_FAIL at the SAME points (marker FXSKY_PLANT_RED)
// or the leg fails VACUOUS by name (FXSKY_PLANT_NOT_CAUGHT). Red rides beside
// green. Captures by the S27 idiom (env LA_SKYCAP_PATH, default user://skystrip),
// byte-counted on disk. Zero save fields (R7); one shipped travel press, zero
// physics-driven player motion; quiet boot + NIGHT-parked clock (the FX gate
// seam); FALL_RECOVERED census contribution: zero.
//
// Run: xvfb-run -a $GODOT --path <proj> --script res://ci_proofs/
//        RuntimeIntegrationProof.cs    LA_GATE_MODE=SKY_STRIP[_PLANT]
public partial class RuntimeIntegrationProof : SceneTree
{
    // ---- machine constants (same family as the FxGlcompat rig's) ----
    private const int SkyBootParkFrames = 40;
    private const int SkyBootParkMs = 4000;
    private const int SkyStateFrames = 10;          // park per state before grabbing
    private const int SkyStateMs = 1000;
    private const int SkyStableFrames = 5;          // gap between the two settle grabs
    private const int SkyStableMs = 500;
    private const int SkyStableMaxTries = 6;
    private const int SkyDeadlineMs = 180_000;      // leg wall watchdog (< battery timeout); +travel+parks ride it too
    private const float SkyInThr = 0.035f;          // same paint threshold as the proven FX rig (FxInThr)
    private const float SkyEpsStable = 0.002f;      // settle tolerance over the verdict regions
    private const int SkyPatchR = 3;
    private const float SkyMatchThr = 0.25f;        // painted-vs-asset max |dRGB|; DOCUMENTED from the
                                                    // measured green transcript AT this head (the
                                                    // REAL-mount run: fog wash at ~48 m is <=0.10 of
                                                    // the fog color + the shipped tonemap/adjust grade)
    private const float SkyMinPairwise = 0.15f;     // floor: three smooth DISTINCT asset colors, else
                                                    // the mapping check proves nothing (a strip cannot
                                                    // lie about its own palette)
    private const float SkyMinMargin = 0.02f;       // TWO-PASS law (this head): for every ORDERED pair
                                                    // of the selected triple, |rigPatch_i - asset_j| -
                                                    // |rigPatch_i - asset_i| >= margin — the DECISION
                                                    // ITSELF predicted from the deterministic skyrig
                                                    // capture (same state the skyon verdict re-measures;
                                                    // settle tolerance 0.002). A conservative triangle
                                                    // bound (pair - w_i - w_j) failed honest here: the
                                                    // wash vectors are not adversarial, real separation
                                                    // beats the bound — so the margin is MEASURED FROM
                                                    // THE RENDER, never pre-guessed. FLOOR 0.02 DOCUMENTED
                                                    // from this head's green transcript (best triple's
                                                    // render margin 0.035 under the NIGHT wash; restamp
                                                    // from the measured margins, the SkyMatchThr law).
    private const int SkyAssetR = 32;               // asset-space footprint of the painted 7x7 screen patch
    private const float SkyFlatMax = 0.04f;         // HONEST-POINT law: keep only locally-flat asset blocks
    private const int SkyPressFrames = 6;           // the shipped travel press-settle window (ZmPressFrames)
    private const int SkyMountParkFrames = 60;      // > 0.5 s decor-cull + deferred navmesh bake + driver re-bind ticks
    private const int SkyMountParkMs = 3500;

    // ---- stage machine ----
    private int _skyPhase;                          // 0 boot-park, 1 travel, 2 rig build, 3 capture machine, 4 verdicts
    private int _skyCapIdx, _skyCapStage, _skyCapTry, _skyMarkMs;
    private int _skyPressToggle, _skyBindTicks;
    private Image? _skyCapA;
    private readonly System.Collections.Generic.Dictionary<string, Image> _skyCaps = new();
    private readonly System.Diagnostics.Stopwatch _skyClock = new();

    // ---- rig (all fields resolve from the PRODUCT mount — nothing is authored here) ----
    private SubViewport? _skyProbe;
    private Camera3D? _skyProbeCam;
    private MeshInstance3D? _skyBackdrop;           // the mounted Canyon scene's REAL backdrop node
    private Image? _skyAssetImg;                    // shipped strip bytes (selection + verdicts)
    private Camera3D? _skyLiveCam;                  // the shipped rig the probe mirrors
    private float _skyQuadW, _skyQuadH;             // read from the mounted QuadMesh
    private bool _skyPlant;
    private readonly System.Collections.Generic.List<(float U, float V, Color Asset)> _skyPts = new();

    // skyrig FIRST: the product as authored (strip VISIBLE) — the wash MEASUREMENT
    // capture that makes point selection honest; then the two verdict states.
    private static readonly string[] SkyStates = { "skyrig", "skybase", "skyon" };

    private void RunSkyStripStage()
    {
        if (!_skyClock.IsRunning)
        {
            _skyClock.Start();
            var rm = RenderingServer.GetCurrentRenderingMethod();
            GD.Print($"LA_GATE: FXSKY_RENDERER method={rm}");
            if (rm != "gl_compatibility")
            { Fail($"SKY_STRIP: CI-effective renderer is {rm}, not gl_compatibility — a verdict measured at the wrong renderer is vacuous"); return; }
            _skyPlant = _mode == "SKY_STRIP_PLANT";
            if (_skyPlant) GD.Print("LA_GATE: FXSKY_PLANT armed — the REAL mounted canyon backdrop's material_override REPLACED with an alpha-0 copy of the shipped material (a TRANSPARENT strip in the MOUNTED product scene — the FX_GLCOMPAT_PLANT mechanism on the product mount); SKY_STRIP_RENDER must print its named FAIL at the SAME RUN-derived points (S28 gate-red law)");
        }
        if (_skyClock.ElapsedMilliseconds > SkyDeadlineMs)
        { Fail($"SKY_STRIP: leg wall deadline {SkyDeadlineMs} ms exceeded in phase {_skyPhase} (llvmpipe too slow, or the travel/mount never landed, or a state never settled)"); return; }

        switch (_skyPhase)
        {
            case 0:
                if (_stageFrames < SkyBootParkFrames || _skyClock.ElapsedMilliseconds < SkyBootParkMs) return;
                SkyPressTravel();                  // one shipped travel press: meadow -> canyon
                _skyPhase = 1; _skyMarkMs = (int)_skyClock.ElapsedMilliseconds; _stageFrames = 0;
                break;
            case 1:
                if (_skyPressToggle < SkyPressFrames) SkyPressTravel();
                if (_director!.CurrentZone != "canyon"
                    || _director.MountedZoneRoot?.Name != "Canyon")
                {
                    if (_skyClock.ElapsedMilliseconds - _skyMarkMs > 30_000)
                        Fail($"SKY_STRIP: the shipped travel press never landed canyon (zone={_director.CurrentZone}, mounted={_director.MountedZoneRoot?.Name ?? "<none>"}) — no mount, no real-mount probe");
                    return;
                }
                if (!SkyParked(SkyMountParkFrames, SkyMountParkMs)) return;
                _skyPhase = 2;
                break;
            case 2:
                SkyBindClockAndBuildRig();         // day-night driver re-bind -> NIGHT park -> read-back -> rig
                break;
            case 3:
                SkyCaptureMachine();
                break;
            case 4:
                SkyVerdicts();
                break;
        }
    }

    private void SkyPressTravel()
    {
        _skyPressToggle++;
        if (_skyPressToggle == 1) Input.ActionPress("travel");
        if (_skyPressToggle >= SkyPressFrames) Input.ActionRelease("travel");
    }

    private bool SkyParked(int needFrames, int needMs)
        => _stageFrames >= needFrames && _skyClock.ElapsedMilliseconds - _skyMarkMs >= needMs;

    // ---- REAL-MOUNT resolve + clock park + rig build — every miss answers LOUD ----
    private void SkyBindClockAndBuildRig()
    {
        if (_phase2Step == 0)
        {
            // The boot gate PAUSED the driver (S48 idiom) before it ever ticked —
            // un-pause so the null-guard RE-BINDS the MOUNTED Canyon env (S27),
            // a few physics ticks for the bind, then the capture-anchor park.
            _director!.SetDayNightPaused(false);
            _skyBindTicks = 0;
            _phase2Step = 1;
            return;
        }
        if (_phase2Step == 1)
        {
            if (++_skyBindTicks < 8) return;
            _director!.SetDayNightFrame(DayNightClock.NightFrame);
            _director.SetDayNightPaused(true);
            // Fresh settle clock AFTER the parked write — the park below must
            // measure the PARKED look, not wall-time spent finding the mount.
            _stageFrames = 0; _skyMarkMs = (int)_skyClock.ElapsedMilliseconds;
            _phase2Step = 2;
            return;
        }
        if (_phase2Step == 2)
        {
            if (!SkyParked(SkyStateFrames, SkyStateMs)) return;   // one park so the parked write lands in the render target
            BuildSkyRig();
            if (_failed) return;
            _skyPhase = 3; _skyCapIdx = 0; SkyStartCapture();
        }
    }
    private int _phase2Step;

    private void BuildSkyRig()
    {
        // 1. The REAL consumer must exist in the MOUNTED scene (retired-claim
        // canary: if the product mount ever regresses to zero-consumer, THIS
        // line goes RED by name — the leg cannot silently fall back to a proxy).
        var mount = _director!.MountedZoneRoot!;
        _skyBackdrop = mount.GetNodeOrNull<MeshInstance3D>("SkyStrip");
        if (_skyBackdrop == null)
        { Fail($"SKY_STRIP: REAL MOUNT MISSING — the mounted Canyon scene carries no MeshInstance3D \"SkyStrip\" (zones/canyon/canyon.tscn, MC 11113 option (b)): the shipped strip's product consumer is GONE and the 11088 zero-consumer state would be back — go RED by name, never probe a proxy instead"); return; }

        // 2. The consumer must carry the SHIPPED bytes.
        if (_skyBackdrop.MaterialOverride is not StandardMaterial3D mat || mat.AlbedoTexture == null)
        { Fail("SKY_STRIP: the REAL mount carries no material_override with an albedo texture — nothing shipped is being consumed"); return; }
        const string assetPath = "res://resources/generated/sky/canyon_sky_v1.png";
        GD.Print($"LA_GATE: FXSKY_DIAG2 parsed material: transparency={mat.Transparency} shading={mat.ShadingMode} cull={mat.CullMode} tint={mat.AlbedoColor} tex={mat.AlbedoTexture?.GetWidth()}x{mat.AlbedoTexture?.GetHeight()}");
        if (mat.AlbedoTexture.ResourcePath != assetPath)
        { Fail($"SKY_STRIP: the REAL mount's albedo_texture is {mat.AlbedoTexture.ResourcePath}, not the shipped {assetPath} — the probe would measure some OTHER art"); return; }
        var tex = GD.Load<Texture2D>(assetPath);
        if (tex == null) { Fail($"SKY_STRIP: the shipped strip {assetPath} is missing — nothing to mount"); return; }
        var img = tex.GetImage();
        if (img.GetWidth() != 2048 || img.GetHeight() != 512)
        { Fail($"SKY_STRIP: the shipped strip decoded {img.GetWidth()}x{img.GetHeight()}, not the shipped 2048x512 — the mount aspect (and the DoD's shipped-asset claim) is broken"); return; }

        // 3. Native 4:1 aspect at the mount (the zero-stretch law lives in the product now).
        if (_skyBackdrop.Mesh is not QuadMesh quad)
        { Fail($"SKY_STRIP: the REAL mount mesh is {_skyBackdrop.Mesh?.GetType().Name ?? "<null>"}, not a QuadMesh — the uv mapping the leg measures has no defined shape"); return; }
        _skyQuadW = quad.Size.X; _skyQuadH = quad.Size.Y;
        if (Mathf.Abs(_skyQuadW / _skyQuadH - 4f) > 0.01f)
        { Fail($"SKY_STRIP: the REAL mount quad is {_skyQuadW}x{_skyQuadH} m — not the strip's NATIVE 4:1 aspect (zero-stretch law): the product is STRETCHING the shipped art"); return; }

        // 4. Probe FBO mirroring the SHIPPED rig (the player's own view, captured
        // from inside the running game — the FBO idiom MC 11019 proved at llvmpipe).
        // Built BEFORE point selection: the honest points must be validated ON THE
        // REAL PROJECTION (a strip out of the shipped view is named, not probed).
        _skyLiveCam = _main!.GetNodeOrNull<Camera3D>("Camera/Camera3D");
        if (_skyLiveCam == null) { Fail("SKY_STRIP: the composition root ships no Camera/Camera3D — nothing to mirror, the probe view would be invented"); return; }
        _skyProbe = new SubViewport { Size = new Vector2I(640, 360), RenderTargetUpdateMode = SubViewport.UpdateMode.Always };
        Root.AddChild(_skyProbe);
        _skyProbe.World3D = Root.GetWorld3D();
        _skyProbeCam = new Camera3D { Fov = _skyLiveCam.Fov, Near = _skyLiveCam.Near, Far = _skyLiveCam.Far };
        _skyProbe.AddChild(_skyProbeCam);
        SkySyncProbeCam();
        Root.Size = new Vector2I(160, 120);

        // 5. The NIGHT park is READ BACK bit-exact (the F4 exact-anchor law, S15 read
        // idiom): the procedural sky underneath the strip is the shipped driver's
        // NIGHT row — option (b) keeps DayNight authoritative, and this line PROVES
        // it is alive in the captured frame rather than asserting it in a comment.
        var env = mount.GetNodeOrNull<WorldEnvironment>("Environment/WorldEnvironment");
        if (env?.Environment?.Sky?.SkyMaterial is not ProceduralSkyMaterial skyMat)
        { Fail("SKY_STRIP: the mounted Canyon env ships no ProceduralSkyMaterial — the shipped dynamic-sky law (option (b) KEEPS it) is broken at the source"); return; }
        var key = DayNightClock.Evaluate("canyon", DayNightClock.NightFrame);
        if (skyMat.SkyTopColor != key.SkyTop || skyMat.SkyHorizonColor != key.SkyHorizon)
        { Fail($"SKY_STRIP: the NIGHT park did not land on the exact anchor row (top={skyMat.SkyTopColor} want={key.SkyTop}, horizon={skyMat.SkyHorizonColor} want={key.SkyHorizon}) — the captured clock is not the asserted one"); return; }
        GD.Print($"LA_GATE: FXSKY_CLOCK_PARKED frame={DayNightClock.NightFrame} top={key.SkyTop} horizon={key.SkyHorizon} — procedural sky ALIVE under the strip (DayNight kept; option (b) ruling)");

        // 6. Points are selected AFTER the skyrig capture (two-pass, wash-aware —
        // the render itself scores the candidates); the asset bytes are kept.
        _skyAssetImg = img;

        var gp = _skyBackdrop.GlobalPosition;
        GD.Print($"LA_GATE: FXSKY_REALMOUNT node={_skyBackdrop.GetPath()} pos=({gp.X:0.#},{gp.Y:0.#},{gp.Z:0.#}) quad={_skyQuadW}x{_skyQuadH}m aspect=native4:1 tex={assetPath} — the SHIPPED strip's PRODUCT consumer, mounted by the ONE WorldMount route");
        // DIAG (kept: a failing leg must say WHERE it is): the strip's four
        // projected corners under the mirrored cam — a zero-paint verdict is
        // only decidable against WHERE the mount claims to be on screen.
        var c0 = SkyScreen(SkyWorld(0f, 0f)); var c1 = SkyScreen(SkyWorld(1f, 0f));
        var c2 = SkyScreen(SkyWorld(1f, 1f)); var c3 = SkyScreen(SkyWorld(0f, 1f));
        var b = _skyBackdrop.GlobalTransform.Basis;
        GD.Print($"LA_GATE: FXSKY_DIAG corners uv00=({c0.X:0},{c0.Y:0}) uv10=({c1.X:0},{c1.Y:0}) uv11=({c2.X:0},{c2.Y:0}) uv01=({c3.X:0},{c3.Y:0}) basis=({b.X.X:0.##},{b.X.Y:0.##},{b.X.Z:0.##}|{b.Z.X:0.##},{b.Z.Y:0.##},{b.Z.Z:0.##}) visible={_skyBackdrop.IsVisibleInTree()} quadMeshVisible={_skyBackdrop.Visible} camCurrent={_skyProbeCam.IsCurrent()} liveDist={_skyLiveCam.GlobalPosition.DistanceTo(gp):0.#}");
        GD.Print($"LA_GATE: FXSKY_RIG probe=640x360 window=160x120 cam-mirrored fov={_skyProbeCam.Fov} — points selected two-pass after the skyrig wash capture");
    }

    private void SkySyncProbeCam()
    {
        // Mirror the SHIPPED rig every capture step: the frame the leg measures IS
        // the game view (the FixedIso base is static while the leg runs — the
        // sync is belt-and-braces, never a re-aim).
        _skyProbeCam!.GlobalPosition = _skyLiveCam!.GlobalPosition;
        _skyProbeCam.GlobalRotation = _skyLiveCam.GlobalRotation;
    }

    /// <summary>RUN-derived sample points, TWO-PASS (the HONEST-POINT law,
    /// re-measured AT this head): a candidate grid over the strip's sky-facing
    /// top half (v <= 0.55: the bottom rows land ON the distant terrain haze —
    /// measured: the v=0.70 candidate came back match 0.240 / cross 0.123, a
    /// haze/occlusion mix, not the art; the strip reads as distant haze BEHIND
    /// the hills anyway). Each candidate's block mean is taken at SkyAssetR —
    /// the asset-space footprint of the painted 7x7 screen patch — and the
    /// candidate is kept ONLY if its block is LOCALLY FLAT (half-scale
    /// sub-quadrants within SkyFlatMax): on a high-contrast band the GPU's
    /// filter, not the mapping, would decide the color compare (the 11088
    /// round measured exactly that artifact). PASS TWO is the wash law: the
    /// skyrig capture (product as authored, clock parked on NIGHT) measures
    /// each candidate's OWN RENDERED wash w = |patch - asset| — fog + tonemap
    /// pull at distance — and the kept triple maximizes the MINIMUM margin
    /// pair_ij - w_i - w_j over its three pairs: a nearest-own-block verdict is
    /// only decidable where the blocks sit farther apart than the render pulls
    /// them together (a guessed fixed floor failed at this head: widest spread
    /// 0.248 with a 0.186 wash lost its cross check to a wash-twin block — the
    /// MARGIN IS COMPUTED FROM THE RENDER, never pre-guessed). Below SkyMinPairwise
    /// or SkyMinMargin the leg FAILS LOUD: a strip that cannot show three
    /// smooth, render-distinct colors cannot prove a mapping, and the lie would
    /// be the probe's, not the asset's.</summary>
    private bool SkySelectPoints(Image img, Image rig)
    {
        int W = img.GetWidth(), H = img.GetHeight();
        var cand = new System.Collections.Generic.List<(float U, float V, Color C, Color Patch, float Wash)>();
        for (int iy = 0; iy <= 17; iy++)
            for (int ix = 0; ix <= 42; ix++)
            {
                float u = 0.08f + 0.84f * ix / 42f, v = 0.08f + 0.47f * iy / 17f;
                int cx = (int)(u * W), cy = (int)(v * H);
                var c = SkyAssetMean(img, cx, cy, SkyAssetR);
                float flatness = 0f;
                int q = SkyAssetR / 2;
                foreach (var (dy, dx) in new[] { (-q, -q), (-q, q), (q, -q), (q, q) })
                    flatness = Mathf.Max(flatness, SkyColorDist(c, SkyAssetMean(img, cx + dx, cy + dy, q)));
                if (flatness >= SkyFlatMax) continue;
                var sp = SkyScreen(SkyWorld(u, v));
                if (sp.X < SkyPatchR + 2 || sp.Y < SkyPatchR + 2 || sp.X > 640 - SkyPatchR - 2 || sp.Y > 360 - SkyPatchR - 2) continue;   // out of the shipped view — cannot be pixel-checked
                var pc = SkyPatchMean(rig, sp, SkyPatchR);
                cand.Add((u, v, c, pc, SkyColorDist(pc, c)));
            }
        if (cand.Count < 3)
        { Fail($"SKY_STRIP: RUN-derived sample points are NOT honest — only {cand.Count} flat, in-view candidates: this strip cannot offer three smooth sampleable colors, so a render verdict here would be vacuous (named, not silently passed)"); return false; }
        // Margin-maximizing triple over ALL flat in-view candidates (brute force,
        // one-shot ~8M ops — and NEVER pre-filter by wash: sorting ascending by
        // |patch-asset| pre-selects the DARK MUTED blocks whose pairwise spread
        // collapses (measured at this head: top-60-low-wash pool pairMin 0.078,
        // the honest pool needs its high-contrast blocks in the scoring).
        int k = cand.Count;
        float bestScore = float.MinValue; int bi = 0, bj = 0, bk = 1;
        static float ordMargin((float U, float V, Color C, Color Patch, float Wash) i,
                               (float U, float V, Color C, Color Patch, float Wash) j)
            => SkyColorDist(i.Patch, j.C) - i.Wash;   // >0: painted i is nearer its own block than j's
        for (int i = 0; i < k; i++)
            for (int j = i + 1; j < k; j++)
            {
                float m1 = Mathf.Min(ordMargin(cand[i], cand[j]), ordMargin(cand[j], cand[i]));
                if (m1 <= bestScore && bestScore > 0f) continue;
                for (int m = j + 1; m < k; m++)
                {
                    float m2 = Mathf.Min(m1, Mathf.Min(ordMargin(cand[i], cand[m]), ordMargin(cand[m], cand[i])));
                    float m3 = Mathf.Min(m2, Mathf.Min(ordMargin(cand[j], cand[m]), ordMargin(cand[m], cand[j])));
                    if (m3 > bestScore && Mathf.Min(
                        Mathf.Min(SkyColorDist(cand[i].C, cand[j].C), SkyColorDist(cand[i].C, cand[m].C)),
                        SkyColorDist(cand[j].C, cand[m].C)) >= SkyMinPairwise)
                    { bestScore = m3; bi = i; bj = j; bk = m; }   // margin-max ONLY among pairwise-qualified triples
                }
            }
        float pairMin = Mathf.Min(
            Mathf.Min(SkyColorDist(cand[bi].C, cand[bj].C), SkyColorDist(cand[bi].C, cand[bk].C)),
            SkyColorDist(cand[bj].C, cand[bk].C));
        if (pairMin < SkyMinPairwise || bestScore < SkyMinMargin)
        { Fail($"SKY_STRIP: RUN-derived sample points are NOT honest — {cand.Count} flat in-view candidates, the best triple's min pairwise asset distance is {pairMin:0.000} (floor {SkyMinPairwise}) with a render-verified nearest-own margin of {bestScore:0.000} (floor {SkyMinMargin}): the NIGHT render does not keep any three smooth blocks of this strip mutually distinct, so a mapping verdict here would be vacuous (named, not silently passed)"); return false; }
        foreach (int i in new[] { bi, bj, bk }) _skyPts.Add((cand[i].U, cand[i].V, cand[i].C));
        GD.Print($"LA_GATE: FXSKY_POINTS triple uv=({cand[bi].U:0.00},{cand[bi].V:0.00})/({cand[bj].U:0.00},{cand[bj].V:0.00})/({cand[bk].U:0.00},{cand[bk].V:0.00}) pairMin={pairMin:0.000}>={SkyMinPairwise} margin={bestScore:0.000}>={SkyMinMargin} washes={cand[bi].Wash:0.000}/{cand[bj].Wash:0.000}/{cand[bk].Wash:0.000} from {cand.Count} flat in-view candidates (skyrig-measured)");
        return true;
    }



    // ---- capture machine (the FxGlcompat park/grab-A/grab-B idiom, own clock) ----
    private void SkyStartCapture()
    {
        // The states are two VIEWS OF THE REAL MOUNT: hidden (the shipped view
        // without the strip — procedural sky + terrain + fog underneath) and
        // shown (the product as authored). Hiding the backdrop is a probe-side
        // presentation toggle — the scene file is never written.
        _skyBackdrop!.Visible = SkyStates[_skyCapIdx] != "skybase";
        SkySyncProbeCam();
        _skyCapStage = 0; _skyCapTry = 0; _skyCapA = null;
        _stageFrames = 0; _skyMarkMs = (int)_skyClock.ElapsedMilliseconds;
    }

    private Godot.Collections.Array<Rect2I> SkyStableRegions()
    {
        var arr = new Godot.Collections.Array<Rect2I>();
        if (_skyPts.Count == 0)   // skyrig settle: quad-anchored regions (the points do
        {                         // not exist until that capture completes — the strip's
            foreach (var (u, v) in new[] { (0.2f, 0.2f), (0.5f, 0.3f), (0.8f, 0.5f) })   // own face settles it
            {
                var p = SkyScreen(SkyWorld(u, v));
                arr.Add(new Rect2I(Mathf.Clamp((int)p.X - 12, 0, 640 - 24), Mathf.Clamp((int)p.Y - 12, 0, 360 - 24), 24, 24));
            }
            return arr;
        }
        foreach (var (u, v, _) in _skyPts)
        {
            var p = SkyScreen(SkyWorld(u, v));
            int x = Mathf.Clamp((int)p.X - 12, 0, 640 - 24), y = Mathf.Clamp((int)p.Y - 12, 0, 360 - 24);
            arr.Add(new Rect2I(x, y, 24, 24));
        }
        return arr;
    }

    private void SkyCaptureMachine()
    {
        if (_skyCapStage == 0)
        {
            if (!SkyParked(SkyStateFrames, SkyStateMs)) return;
            SkySyncProbeCam();
            _skyCapA = _skyProbe!.GetTexture().GetImage();
            _skyCapStage = 1; _stageFrames = 0; _skyMarkMs = (int)_skyClock.ElapsedMilliseconds;
            return;
        }
        if (_skyCapStage != 1) return;
        if (!SkyParked(SkyStableFrames, SkyStableMs)) return;
        SkySyncProbeCam();
        var b = _skyProbe!.GetTexture().GetImage();
        float eps = FxRegionsDelta(_skyCapA!, b, SkyStableRegions());
        if (eps < SkyEpsStable)
        {
            var done = SkyStates[_skyCapIdx];
            _skyCaps[done] = _skyCapA;
            if (done == "skyrig")
            {
                if (!SkySelectPoints(_skyAssetImg!, _skyCapA)) return;
                // THE PLANT lands only AFTER the honest wash capture + selection —
                // points are chosen against the product AS AUTHORED, never against
                // the planted state (that would rig the red pair's own gate).
                if (_skyPlant)
                {
                    var plant = new StandardMaterial3D
                    {
                        ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
                        CullMode = BaseMaterial3D.CullModeEnum.Disabled,
                        Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                        AlbedoTexture = GD.Load<Texture2D>("res://resources/generated/sky/canyon_sky_v1.png"),
                        AlbedoColor = new Color(1f, 1f, 1f, 0f),   // alpha FORCED 0
                    };
                    _skyBackdrop!.MaterialOverride = plant;
                    GD.Print("LA_GATE: FXSKY_PLANT APPLIED — the REAL mount's material_override is now an alpha-0 copy (a TRANSPARENT shipped strip in the mounted product scene; the scene file is never written)");
                }
            }
            _skyCapIdx++;
            if (_skyCapIdx >= SkyStates.Length) { _skyPhase = 4; return; }
            SkyStartCapture();
            return;
        }
        _skyCapTry++;
        GD.Print($"LA_GATE: FXSKY_UNSETTLED {SkyStates[_skyCapIdx]} try={_skyCapTry} regions-delta={eps:0.0000} (eps<{SkyEpsStable})");
        if (_skyCapTry >= SkyStableMaxTries)
        { Fail($"SKY_STRIP: capture {SkyStates[_skyCapIdx]} never settled in {SkyStableMaxTries} tries — the FBO is not deterministic at this renderer"); return; }
        SkyStartCapture();
    }

    // ---- verdicts ----
    private Vector3 SkyWorld(float u, float v) => _skyBackdrop!.ToGlobal(new Vector3(
        (u - 0.5f) * _skyQuadW,
        (0.5f - v) * _skyQuadH,   // texture v=0 is the asset's TOP row — the upright-quad mapping
        0f));

    private Vector2 SkyScreen(Vector3 w) => _skyProbeCam!.UnprojectPosition(w);

    private void SkyVerdicts()
    {
        _skyBackdrop!.Visible = true;   // the product as authored before the verdict captures
        var base0 = _skyCaps["skybase"];
        var on = _skyCaps["skyon"];
        bool allPainted = true, allNearer = true, allWithin = true;
        for (int i = 0; i < _skyPts.Count; i++)
        {
            var (u, v, asset) = _skyPts[i];
            var sp = SkyScreen(SkyWorld(u, v));
            float delta = FxPatchDelta(on, base0, sp, SkyPatchR);
            var paint = SkyPatchMean(on, sp, SkyPatchR);
            float match = SkyColorDist(paint, asset);
            float cross = float.MaxValue;
            for (int j = 0; j < _skyPts.Count; j++)
                if (j != i) cross = Mathf.Min(cross, SkyColorDist(paint, _skyPts[j].Asset));
            bool painted = delta > SkyInThr, nearer = match < cross, within = match < SkyMatchThr;
            allPainted &= painted; allNearer &= nearer; allWithin &= within;
            GD.Print($"LA_GATE: SKY_STRIP measured pt{i} uv=({u:0.00},{v:0.00}) painted-delta={delta:0.0000} (thr>{SkyInThr}) match={match:0.0000} (thr<{SkyMatchThr}) cross={cross:0.0000} -> {(painted && nearer && within ? "OK" : "FAIL")}");
        }
        bool ok = allPainted && allNearer && allWithin;
        GD.Print($"LA_GATE: SKY_STRIP measured pts={_skyPts.Count} painted={(allPainted ? "ALL" : "SOME-FAIL")} mapped-as-authored={(allNearer ? "ALL" : "SOME-FAIL")} within-asset={(allWithin ? "ALL" : "SOME-FAIL")} (paint-vs-backdrop-hidden real view; nearer-to-own-block-than-any-other; a transparent, swapped, stretched or MISSING product strip CANNOT pass)");
        if (ok) GD.Print("LA_GATE: SKY_STRIP_RENDER_OK — the shipped canyon_sky_v1.png rides the CI-effective gl_compatibility renderer THROUGH THE PRODUCT MOUNT: painted at EVERY RUN-derived expected screen point of the MIRRORED SHIPPED VIEW and each painted patch is nearer to its OWN asset block color than to any other sample's — the REAL mounted strip (zones/canyon/canyon.tscn SkyStrip, WorldMount idiom) renders AS AUTHORED (mapping measured), not just 'a quad drew something'");
        else GD.Print("LA_GATE: SKY_STRIP_RENDER_FAIL — the shipped strip is NOT proven painted-as-authored at the expected points (see the measured lines above)");

        SaveSkyCaptures();
        if (_failed) return;

        if (_skyPlant)
        {
            ReleaseHeldRefsBeforeQuit();
            if (!ok) GD.Print("LA_GATE: FXSKY_PLANT_RED — planted alpha-0 REAL mount CAUGHT: SKY_STRIP_RENDER printed its named FAIL at the SAME measurement points (the check can go RED — red transcript + capture ride beside the green pair)");
            else Fail("FXSKY_PLANT_NOT_CAUGHT — the alpha-0 REAL mount stayed GREEN past the SKY_STRIP check: the probe would be VACUOUS");
            Fail("FXSKY_PLANT — the planted alpha-0 canyon_sky_v1 product mount printed SKY_STRIP_RENDER_FAIL by name at the RUN-derived expected points (the red leg ends RED by design; the green pair + captures ride beside it)");
            return;
        }
        if (!ok)
        { Fail("SKY_STRIP: the shipped canyon_sky_v1 strip is NOT proven painted-as-authored THROUGH THE REAL MOUNT at the CI-effective renderer (the measured lines above name which gate) — the S50 real-mount duty stays open"); return; }
        GD.Print("LA_GATE: PASS — S50-SKY (MC 11113) REAL-MOUNT render probe verified at gl_compatibility: the canyon backdrop node mounted by the ONE WorldMount route carries the shipped strip and it is PAINTED at every RUN-derived expected screen point of the mirrored shipped view, mapping measured — DayNight/ProceduralSky KEPT (option (b)); the clock parked on the NIGHT anchor and read back bit-exact; one shipped travel press, zero physics-driven player motion, zero save fields (R7), spawning off at boot");
        _asserted = true;
        ReleaseHeldRefsBeforeQuit();
        Quit(0);
    }

    // ---- color helpers (the patch law mirrors FxPatchDelta: mean |dRGB| over
    // a (2r+1)^2 box; asset colors are block means of the shipped bytes) ----
    private static float SkyColorDist(Color a, Color b) =>
        (Mathf.Abs(a.R - b.R) + Mathf.Abs(a.G - b.G) + Mathf.Abs(a.B - b.B)) / 3f;

    private static Color SkyAssetMean(Image img, int cx, int cy, int r)
    {
        float s = 0f, g = 0f, bb = 0f; int n = 0;
        for (int y = cy - r; y <= cy + r; y++)
            for (int x = cx - r; x <= cx + r; x++)
            {
                if (x < 0 || y < 0 || x >= img.GetWidth() || y >= img.GetHeight()) continue;
                var c = img.GetPixel(x, y);
                s += c.R; g += c.G; bb += c.B; n++;
            }
        return n == 0 ? new Color(0, 0, 0) : new Color(s / n, g / n, bb / n);
    }

    private Color SkyPatchMean(Image img, Vector2 p, int r)
    {
        float s = 0f, g = 0f, bb = 0f; int n = 0;
        for (int y = (int)p.Y - r; y <= (int)p.Y + r; y++)
            for (int x = (int)p.X - r; x <= (int)p.X + r; x++)
            {
                if (x < 0 || y < 0 || x >= img.GetWidth() || y >= img.GetHeight()) continue;
                var c = img.GetPixel(x, y);
                s += c.R; g += c.G; bb += c.B; n++;
            }
        return n == 0 ? new Color(0, 0, 0) : new Color(s / n, g / n, bb / n);
    }

    private void SaveSkyCaptures()
    {
        string env = System.Environment.GetEnvironmentVariable("LA_SKYCAP_PATH") ?? "";
        string dir = string.IsNullOrEmpty(env) ? ProjectSettings.GlobalizePath("user://skystrip") : env;
        System.IO.Directory.CreateDirectory(dir);
        foreach (var kv in _skyCaps)
        {
            string path = System.IO.Path.Combine(dir, $"skystrip_{kv.Key}.png");
            var err = kv.Value.SavePng(path);
            if (err != Error.Ok) { Fail($"SKY_STRIP: capture SavePng failed ({err}) at {path}"); return; }
            long bytes = new System.IO.FileInfo(path).Length;
            if (bytes <= 100) { Fail($"SKY_STRIP: capture {kv.Key} empty on disk ({bytes} bytes at {path})"); return; }
            GD.Print($"LA_GATE: FXSKY_CAPTURE {kv.Key} path={path} bytes={bytes}");
        }
    }
}
