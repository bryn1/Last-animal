using Godot;

// SIZE reason (file hygiene law): 382 l by wc AT this head — ONE stage body:
// the capture machine, the rig, the RUN-derived point selector and the verdicts
// share one clock, one probe and one point list; splitting a stage body mid-leg
// has no pairing-gate precedent (body == arm, MC 10218).
//
// Last Animal — MC 11088 / 10026.44.10 S48 BATCH-4 FIX-ROUND-1 (DA F1): the
// sky-strip render probe the Batch-4 card DoD NAMES (stage 109, modes
// SKY_STRIP / SKY_STRIP_PLANT). The ship card deferred this leg on an author-
// ity quote not on the board (DA-verdict F1: "No artifact = no decision") —
// the deferral is CLOSED here: the CI-probe mount rides the FxGlcompat idiom
// (a rig added BESIDE the live World3D — the player, camera and every shipped
// system untouched), so NO second sky mechanism enters the GAME: the zone sky
// law (ProceduralSkyMaterial + DayNight, ZoneMount.cs:152 bit-exact legs) is
// untouched by this stage, exactly the reading the ship card itself stated —
// the probe answers "does the SHIPPED painted strip ride the CI-effective GL-
// Compatibility renderer at the expected screen points", not "does the game
// use it" (that mount stays the integration card's ruling — the LEDGER row).
//
// Mount: an unshaded textured quad 8 m x 2 m — the strip's NATIVE 4:1 aspect,
// zero stretch in the mount — east of the player's screen band (the FX rig's
// off-band law) and ~1.2 m clear of ground clutter. READBACK: a SubViewport
// probe FBO (640x360, UPDATE-ALWAYS) sharing the live World3D — at this
// renderer the ROOT-window texture reads EMPTY (measured at step-0), the FBO
// renders through the SAME RenderingServer and reads back real pixels; the
// main window shrinks to 160x120 so llvmpipe paints the probe, not a view
// nobody reads. The renderer is PINNED (FXSKY_RENDERER): any other method
// exits 1 — a verdict at the wrong renderer is vacuous by construction.
//
// Sample points are RUN-DERIVED from the shipped PNG (the FxMaxAlphaUv law:
// the points are derived from the bytes, never hard-pasted): a candidate grid
// over the central band, block-mean colors, the triple with the WIDEST mutual
// color spread kept; a strip too flat to prove its own mapping fails the leg
// LOUD at the rig ("not honest sample points"). Verdicts (battery greps the
// names, every measured margin prints beside its line):
//   SKY_STRIP_RENDER_OK|FAIL — at EVERY expected screen point the strip MUST
//     (a) PAINT: |dRGB| against the same-camera/point patch with the quad
//         REMOVED exceeds SkyInThr (the S48 step-2 gate, quoted by card name);
//     (b) MAP AS AUTHORED: the painted patch color is NEARER to its own asset
//         block color than to ANY other sample's (a painted-something generic
//         quad cannot pass — only the strip's own left/right color spread
//         routes each point to its own asset color);
//     (c) stay within SkyMatchThr of its own asset color (documented from the
//         measured green transcript at this head — an unshaded texture at
//         llvmpipe lands near its bytes; the margin is printed either way).
// Non-vacuous (gate-red law): SKY_STRIP_PLANT forces the mounted strip's
// material alpha 0 (an alpha-0 copy of the mount — the FX_GLCOMPAT_PLANT
// mechanism proven red-catchable at this renderer); the verdict must print
// SKY_STRIP_RENDER_FAIL at the SAME points (marker FXSKY_PLANT_RED) or the
// leg fails VACUOUS by name (FXSKY_PLANT_NOT_CAUGHT). Red rides beside green.
// Captures by the S27 idiom (env LA_SKYCAP_PATH, default user://skystrip),
// byte-counted on disk. Zero save fields (R7); zero player motion; quiet
// boot + PAUSED clock (the FX gate seam — a frozen sun is what makes a pixel
// deliverable here); FALL_RECOVERED census contribution: zero.
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
    private const int SkyDeadlineMs = 120_000;      // leg wall watchdog (< battery timeout)
    private const float SkyInThr = 0.035f;          // same paint threshold as the proven FX rig (FxInThr)
    private const float SkyEpsStable = 0.002f;      // settle tolerance over the verdict regions
    private const int SkyPatchR = 3;
    private const float SkyMatchThr = 0.25f;        // painted-vs-asset max |dRGB|; DOCUMENTED from the
                                                    // measured green transcript at this head: matches
                                                    // 0.083/0.109/0.134 (plant: 0.159-0.525 FAILs anyway
                                                    // on delta 0.0000) — worst green margin 1.9x
    private const float SkyMinPairwise = 0.15f;     // the RUN-derived triple's MINIMUM mutual asset
                                                    // distance, else the mapping check proves nothing
    private const int SkyAssetR = 32;               // asset-space footprint of the painted 7x7 screen patch
    private const float SkyFlatMax = 0.06f;         // HONEST-POINT law: keep only locally-flat asset blocks
    private const float SkyQuadW = 8f, SkyQuadH = 2f;   // native 2048x512 aspect — zero stretch

    // ---- stage machine ----
    private int _skyPhase;                          // 0 boot-park, 1 capture machine, 2 verdicts
    private int _skyCapIdx, _skyCapStage, _skyCapTry, _skyMarkMs;
    private Image? _skyCapA;
    private readonly System.Collections.Generic.Dictionary<string, Image> _skyCaps = new();
    private readonly System.Diagnostics.Stopwatch _skyClock = new();

    // ---- rig ----
    private SubViewport? _skyProbe;
    private Camera3D? _skyProbeCam;
    private MeshInstance3D? _skyStripQuad;
    private Vector3 _skyCenter;
    private bool _skyPlant;
    private readonly System.Collections.Generic.List<(float U, float V, Color Asset)> _skyPts = new();

    private static readonly string[] SkyStates = { "skybase", "skyon" };

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
            if (_skyPlant) GD.Print("LA_GATE: FXSKY_PLANT armed — the mounted canyon_sky_v1 strip material alpha FORCED 0 (an alpha-0 copy of the mount, the proven FX_GLCOMPAT_PLANT mechanism); SKY_STRIP_RENDER must print its named FAIL at the SAME RUN-derived points (S28 gate-red law)");
        }
        if (_skyClock.ElapsedMilliseconds > SkyDeadlineMs)
        { Fail($"SKY_STRIP: leg wall deadline {SkyDeadlineMs} ms exceeded in phase {_skyPhase} (llvmpipe too slow, or a state never settled)"); return; }

        switch (_skyPhase)
        {
            case 0:
                if (_stageFrames < SkyBootParkFrames || _skyClock.ElapsedMilliseconds < SkyBootParkMs) return;
                BuildSkyRig();
                if (_failed) return;
                _skyPhase = 1; _skyCapIdx = 0; SkyStartCapture();
                break;
            case 1:
                SkyCaptureMachine();
                break;
            case 2:
                SkyVerdicts();
                break;
        }
    }

    // ---- rig build — a mount that cannot be PLACED (or points that cannot
    // prove the asset) answers nothing: loud fail ----
    private void BuildSkyRig()
    {
        var tex = GD.Load<Texture2D>("res://resources/generated/sky/canyon_sky_v1.png");
        if (tex == null) { Fail("SKY_STRIP: the shipped strip resources/generated/sky/canyon_sky_v1.png is missing — nothing to mount"); return; }
        var img = tex.GetImage();
        if (img.GetWidth() != 2048 || img.GetHeight() != 512)
        { Fail($"SKY_STRIP: the shipped strip decoded {img.GetWidth()}x{img.GetHeight()}, not the shipped 2048x512 — the mount aspect (and the DoD's shipped-asset claim) is broken"); return; }
        if (!SkySelectPoints(img)) return;   // sets _skyPts or Fails LOUD (the strip cannot prove its own mapping)

        Vector3 p0 = _playerBody!.GlobalPosition;
        _skyCenter = p0 + new Vector3(3.0f, 2.2f, -6.5f);   // east of the player's screen band (the FX rig's off-band law); bottom edge ~1.2 m up, clear of ground clutter
        _skyStripQuad = new MeshInstance3D
        {
            Mesh = new QuadMesh { Size = new Vector2(SkyQuadW, SkyQuadH) },   // the strip's NATIVE 4:1 aspect — zero stretch in the mount
            Position = _skyCenter,
            Visible = false,
        };
        var mat = new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            AlbedoTexture = tex,
        };
        if (_skyPlant) mat.AlbedoColor = new Color(1f, 1f, 1f, 0f);   // THE PLANT: alpha FORCED 0 (mirror of the FX quad plant)
        _skyStripQuad.MaterialOverride = mat;

        // Probe FBO sharing the LIVE world; camera square to the strip.
        _skyProbe = new SubViewport { Size = new Vector2I(640, 360), RenderTargetUpdateMode = SubViewport.UpdateMode.Always };
        Root.AddChild(_skyProbe);
        _skyProbe.World3D = Root.GetWorld3D();
        _skyProbeCam = new Camera3D { Fov = 70f };
        _skyProbe.AddChild(_skyProbeCam);
        _skyProbeCam.LookAtFromPosition(_skyCenter + new Vector3(0f, 1.2f, 9.5f), _skyCenter, Vector3.Up);

        Root.AddChild(_skyStripQuad);   // BESIDE the live scene — the composed world runs untouched underneath
        Root.Size = new Vector2I(160, 120);
        GD.Print($"LA_GATE: FXSKY_RIG strip@{_skyCenter} size={SkyQuadW}x{SkyQuadH}m points={_skyPts.Count} RUN-derived (pairwise asset dists {SkyPairwiseSummary()}) probe=640x360 window=160x120");
    }

    /// <summary>RUN-derived sample points (the HONEST-POINT law, measured at
    /// this head): a candidate grid over the strip; each candidate's block mean
    /// is taken at SkyAssetR — the asset-space footprint of the painted 7x7
    /// screen patch (the quad renders ~212 px wide: 1 screen px ~ 9.7 asset px)
    /// — and the candidate is kept ONLY if its block is LOCALLY FLAT (every
    /// half-scale sub-quadrant mean within SkyFlatMax of the block mean): on a
    /// high-contrast band the GPU's filter, not the mapping, would decide the
    /// color compare and the check would lie (the round's first probe measured
    /// exactly that: a dark-horizon candidate matched 0.2018 to its own asset
    /// color vs 0.1473 to a neighbour's — a FILTER artifact, not a mapping
    /// failure). Kept triple = the widest mutual spread (greedy: widest pair,
    /// then the point maximizing the MINIMUM mutual distance). A triple below
    /// SkyMinPairwise FAILS the leg — a strip that cannot offer three smooth,
    /// distinct colors cannot prove a mapping, and the lie would be the
    /// probe's, not the asset's.</summary>
    private bool SkySelectPoints(Image img)
    {
        int W = img.GetWidth(), H = img.GetHeight();
        var cand = new System.Collections.Generic.List<(float U, float V, Color C)>();
        for (int iy = 0; iy <= 17; iy++)
            for (int ix = 0; ix <= 42; ix++)
            {
                float u = 0.08f + 0.84f * ix / 42f, v = 0.08f + 0.84f * iy / 17f;
                int cx = (int)(u * W), cy = (int)(v * H);
                var c = SkyAssetMean(img, cx, cy, SkyAssetR);
                float flatness = 0f;
                int q = SkyAssetR / 2;
                foreach (var (dy, dx) in new[] { (-q, -q), (-q, q), (q, -q), (q, q) })
                    flatness = Mathf.Max(flatness, SkyColorDist(c, SkyAssetMean(img, cx + dx, cy + dy, q)));
                if (flatness < SkyFlatMax) cand.Add((u, v, c));
            }
        float best = -1f; int bi = 0, bj = 0;
        for (int i = 0; i < cand.Count; i++)
            for (int j = i + 1; j < cand.Count; j++)
            {
                float d = SkyColorDist(cand[i].C, cand[j].C);
                if (d > best) { best = d; bi = i; bj = j; }
            }
        int bk = -1; float bestMin = -1f;
        for (int k = 0; k < cand.Count; k++)
        {
            if (k == bi || k == bj) continue;
            float m = Mathf.Min(SkyColorDist(cand[k].C, cand[bi].C), SkyColorDist(cand[k].C, cand[bj].C));
            if (m > bestMin) { bestMin = m; bk = k; }
        }
        float minPair = bk < 0 ? -1f : Mathf.Min(best, bestMin);
        if (bk < 0 || minPair < SkyMinPairwise)
        { Fail($"SKY_STRIP: RUN-derived sample points are NOT honest — {cand.Count} flat candidates, the triple's minimum mutual asset distance is {minPair:0.000} < {SkyMinPairwise}: this strip cannot prove its own mapping, so a render verdict here would be vacuous (named, not silently passed)"); return false; }
        foreach (int i in new[] { bi, bj, bk }) _skyPts.Add((cand[i].U, cand[i].V, cand[i].C));
        return true;
    }

    private string SkyPairwiseSummary()
    {
        float mn = float.MaxValue;
        for (int i = 0; i < _skyPts.Count; i++)
            for (int j = i + 1; j < _skyPts.Count; j++)
                mn = Mathf.Min(mn, SkyColorDist(_skyPts[i].Asset, _skyPts[j].Asset));
        return $"min={mn:0.000}>={SkyMinPairwise}";
    }

    // ---- capture machine (the FxGlcompat park/grab-A/grab-B idiom, own clock) ----
    private void SkyStartCapture()
    {
        _skyStripQuad!.Visible = SkyStates[_skyCapIdx] == "skyon";
        _skyCapStage = 0; _skyCapTry = 0; _skyCapA = null;
        _stageFrames = 0; _skyMarkMs = (int)_skyClock.ElapsedMilliseconds;
    }

    private bool SkyParked(int needFrames, int needMs)
        => _stageFrames >= needFrames && _skyClock.ElapsedMilliseconds - _skyMarkMs >= needMs;

    private Godot.Collections.Array<Rect2I> SkyStableRegions()
    {
        var arr = new Godot.Collections.Array<Rect2I>();
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
            _skyCapA = _skyProbe!.GetTexture().GetImage();
            _skyCapStage = 1; _stageFrames = 0; _skyMarkMs = (int)_skyClock.ElapsedMilliseconds;
            return;
        }
        if (_skyCapStage != 1) return;
        if (!SkyParked(SkyStableFrames, SkyStableMs)) return;
        var b = _skyProbe!.GetTexture().GetImage();
        float eps = FxRegionsDelta(_skyCapA!, b, SkyStableRegions());
        if (eps < SkyEpsStable)
        {
            _skyCaps[SkyStates[_skyCapIdx]] = _skyCapA;
            _skyCapIdx++;
            if (_skyCapIdx >= SkyStates.Length) { _skyPhase = 2; return; }
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
    private Vector3 SkyWorld(float u, float v) => new(
        _skyCenter.X + (u - 0.5f) * SkyQuadW,
        _skyCenter.Y + (0.5f - v) * SkyQuadH,   // texture v=0 is the asset's TOP row — the upright-quad mapping
        _skyCenter.Z);

    private Vector2 SkyScreen(Vector3 w) => _skyProbeCam!.UnprojectPosition(w);

    private void SkyVerdicts()
    {
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
        GD.Print($"LA_GATE: SKY_STRIP measured pts={_skyPts.Count} painted={(allPainted ? "ALL" : "SOME-FAIL")} mapped-as-authored={(allNearer ? "ALL" : "SOME-FAIL")} within-asset={(allWithin ? "ALL" : "SOME-FAIL")} (paint-vs-quad-removed; nearer-to-own-block-than-any-other; an alpha-0 or swapped or missing strip CANNOT pass)");
        if (ok) GD.Print("LA_GATE: SKY_STRIP_RENDER_OK — the shipped canyon_sky_v1.png rides the CI-effective gl_compatibility renderer: painted at EVERY RUN-derived expected screen point AND each painted patch is nearer to its OWN asset block color than to any other sample's — the strip renders AS AUTHORED (mapping measured), not just 'a quad drew something'");
        else GD.Print("LA_GATE: SKY_STRIP_RENDER_FAIL — the shipped strip is NOT proven painted-as-authored at the expected points (see the measured lines above)");

        SaveSkyCaptures();
        if (_failed) return;

        if (_skyPlant)
        {
            ReleaseHeldRefsBeforeQuit();
            if (!ok) GD.Print("LA_GATE: FXSKY_PLANT_RED — planted alpha-0 strip CAUGHT: SKY_STRIP_RENDER printed its named FAIL at the SAME measurement points (the check can go RED — red transcript + capture ride beside the green pair)");
            else Fail("FXSKY_PLANT_NOT_CAUGHT — the alpha-0 strip stayed GREEN past the SKY_STRIP check: the probe would be VACUOUS");
            Fail("FXSKY_PLANT — the planted alpha-0 canyon_sky_v1 mount printed SKY_STRIP_RENDER_FAIL by name at the RUN-derived expected points (the red leg ends RED by design; the green pair + captures ride beside it)");
            return;
        }
        if (!ok)
        { Fail("SKY_STRIP: the shipped canyon_sky_v1 strip is NOT proven painted-as-authored at the CI-effective renderer (the measured lines above name which gate) — the Batch-4 render-probe duty stays open"); return; }
        GD.Print("LA_GATE: PASS — S48 BATCH-4 FIX-ROUND-1 sky-strip render probe verified at gl_compatibility (MC 11088 DA F1): the shipped strip is PAINTED at every RUN-derived expected screen point and its mapping is measured — CI-probe mount only, the in-game sky path (ProceduralSkyMaterial/DayNight) is untouched; zero save fields (R7), zero player motion, spawning off at boot");
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
