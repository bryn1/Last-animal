using Godot;

// Last Animal — MC 11269 / 10026.44.19 S51-SKY-DIM: the canyon SkyStrip
// backdrop dimmed BY THE EXISTING day-night clock (owner taste follow-up to
// MC 11113, ruling verbatim 2026-10-25: "Do the leftovers and then do a
// playthrough" — the owner-visible frame .audits/202610240532-113s/out/
// sky-green/skystrip_skyon.png reads as a bright hard-edged rectangle over
// the dark procedural night; the strip held DAY brightness at the NIGHT
// anchor because nothing consumed the clock on it).
//
// ONE mechanism, extended, never duplicated (owner law "replace, do not add
// beside"): NO new clock, NO parallel system. The factor is the SAME pure
// DayNightClock.Evaluate row the DayNight driver already writes the sky +
// light from — normalised by that zone's NOON key (the shipped brightest
// row), so the factor is 1.0 EXACTLY at full day (noon anchor → strip
// byte-identical to the authored look) and dims continuously (the tables are
// piecewise-linear, no popping mid-transition) to canyon's night floor
// 0.35/1.40 = 0.25 of day brightness (inside the ratified 0.25-0.35 band).
// DayNight's bit-exact anchors and the asserted key tables are UNTOUCHED:
// this file READS the evaluator, it writes no clock state.
//
// PRESENTATION-ONLY, like the driver it rides: the strip's material_override
// albedo colour is written authored × factor (the authored colour is cached
// at bind so repeated writes never compound). Zones carry the material only
// through the ONE WorldMount route; the node resolve self-heals across mount
// swaps with the same IsInstanceValid-then-re-resolve idiom the DayNight
// driver uses after InvalidateDayNightBindings (MC 10519 S27), so no new
// seam into WorldDirector.Mount.cs is needed. ZERO save fields (RULING-7 —
// nothing here reaches GameState); zero bus signals (the 15-signal census
// holds); a zone without a "SkyStrip" node answers nothing (no-op — today
// only canyon.tscn ships one).
//
// Call sites (the existing mechanism, two lines in world/WorldDirector.DayNight.cs):
//   * _PhysicsProcess — after the driver's own ApplyTo write, so the strip
//     rides the very same row the sky + light were just written from;
//   * SetDayNightFrame — the capture-anchor park seam: the strip follows the
//     parked row IMMEDIATELY, the same park semantics the FXSKY_CLOCK_PARKED
//     read-back proves for the procedural sky (paused driver ⇒ frozen look,
//     also for the SKY_STRIP proof leg and its planted pair).
namespace LastAnimal.World;

public partial class WorldDirector
{
    // Self-healing resolve (DayNight re-bind idiom): dropped + re-found from
    // the CURRENT mount whenever the cached instance is gone or the mount
    // swapped. Authored albedo is cached per material instance so the per-
    // tick write is authored × factor, never a compounding multiply.
    private MeshInstance3D? _skyStripNode;
    private StandardMaterial3D? _skyStripMat;
    private Color _skyStripAuthored = Colors.White;
    private BaseMaterial3D.TransparencyEnum _skyStripAuthoredTransparency;

    /// <summary>The strip's day factor from the shipped clock: the evaluated
    /// row's LightEnergy normalised by the zone's NOON row (noon == shipped
    /// tscn grade == the brightest row in every table, MC 10199 S15 — so the
    /// ratio is 1.0 at full day and never exceeds it; the clamp is belt-and-
    /// braces for a future table retune). Pure + frame-deterministic (F4).</summary>
    private static float SkyStripDayFactor(string zoneId, int frame)
    {
        float noon = DayNightClock.Evaluate(zoneId, DayNightClock.NoonFrame).LightEnergy;
        if (noon <= 0f) return 1f;
        float now = DayNightClock.Evaluate(zoneId, DayNightClock.WrapFrame(frame)).LightEnergy;
        return Mathf.Min(1f, now / noon);
    }

    /// <summary>Write the clock factor onto the mounted strip (presentation
    /// write only). Resolves from the CURRENT zone mount each call: a cached
    /// but freed/foreign node re-resolves; no "SkyStrip" node (or no
    /// material_override) is a silent no-op — the node is the whole contract.
    /// THE WRITE IS THE ALPHA CHANNEL: (authored.RGB, authored.A × factor).
    /// WHY alpha, measured this head (.audits/202610250848-11269/out/, where
    /// the banked RED out/sky_proof_green_try2.log is the ALPHA law at the old
    /// floor): the RGB carrier dimmed the ALBEDO to the factor and the CI
    /// render's tonemap LIFTED the blacks — measured gain 0.6381 at the
    /// factor-0.25 park (FXSKY_GAIN line), the band read ~0.30 not ~0.19, and
    /// the mapping law died (additive wash, shrunk spacing: margin -0.021, leg
    /// RED by name). That RGB failing-run transcript was NOT preserved — lost
    /// to a same-name re-run overwrite, named at the DA gate F1; the decisive
    /// numbers are restated here and were independently re-measured there.
    /// The alpha carrier renders mix(BG, texel, f): block spacing scales
    /// EXACTLY with f against the measured backdrop, the wash cancels per
    /// block against the strip-hidden capture, and the shipped unshaded path
    /// proved alpha paint at this renderer before this card existed
    /// (FX_GLCOMPAT quad + the alpha-0 plant pair, MC 11019/MC 11088 — the
    /// plant's alpha half IS this channel). At noon factor == 1.0 EXACTLY the
    /// write reproduces the authored tuple byte-for-byte — full day UNCHANGED
    /// (the DAY-band row re-proves it on pixels). Zones ship strip alpha 1
    /// today (canyon.tscn StdMat_skystrip); an authored alpha < 1 is scaled
    /// too, never assumed absent.</summary>
    private void SkyStripDimApply(int frame)
    {
        if (_skyStripNode == null || !GodotObject.IsInstanceValid(_skyStripNode)
            || !IsAncestorOf(_skyStripNode))
        {
            _skyStripNode = null;
            _skyStripMat = null;
            var mount = _zoneMount;
            if (mount == null || !GodotObject.IsInstanceValid(mount)) return;
            _skyStripNode = mount.GetNodeOrNull<MeshInstance3D>("SkyStrip");
        }
        if (_skyStripNode == null) return;
        if (_skyStripNode.MaterialOverride is not StandardMaterial3D mat) return;
        if (!ReferenceEquals(mat, _skyStripMat))
        {
            _skyStripMat = mat;
            _skyStripAuthored = mat.AlbedoColor;   // cache the AUTHORED look once
            _skyStripAuthoredTransparency = mat.Transparency;
        }
        float f = SkyStripDayFactor(_zone, frame);
        Color a = _skyStripAuthored;
        // The authored tscn sets NO transparency property (the shipped enum
        // value is TRANSPARENCY_DISABLED — the banked FXSKY_DIAG2 read-back
        // prints transparency=Disabled; alpha is ignored there). The dim needs
        // the alpha BLEND, and at noon factor==1 EXACTLY the authored
        // tuple + mode come back byte-identical (full day UNCHANGED — the
        // DAY-band row re-proves it on pixels). f is continuous (piecewise
        // linear tables) and the blend at f≈1 paints the opaque frame anyway:
        // no pop mid-transition.
        mat.Transparency = f < 1f ? BaseMaterial3D.TransparencyEnum.Alpha : _skyStripAuthoredTransparency;
        mat.AlbedoColor = new Color(a.R, a.G, a.B, a.A * f);   // THE WRITE IS THE ALPHA CHANNEL (why: SkyStripDimApply doc — RGB carrier measured dead this head: tonemap lift)
    }
}
