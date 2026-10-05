using Godot;

// Last Animal — MC 10123 / card 10026.15.4 (Inc-3 S6): UI theme tokens.
//
// UiTheme: the single source for the presentation literals the UI panels
// paint with. Honest scale (plan S6): the tokens ARE the literals the three
// panel files already carried — Hud, SkillsPanel and EmpathyPanel — pulled
// into one class. It is a single-source tidy, not an "everywhere" restyle:
// no token here changes a pixel; every value is byte-equal to the literal it
// replaces. ci/ui_test.sh leg UI_TOKENS_SINGLE_SOURCE greps the three panel
// files for a reappearing raw literal and goes RED on one.
//
// The S6 additions (vignette + gauge smoothing constants) are NEW theme
// values for the S6 visual layer — they live here from birth, so the panel
// files never carry a raw literal for them either.
namespace LastAnimal.Ui;

/// <summary>
/// Theme constants for the UI panels (S6). Colours, font sizes, and the
/// frame-counter pacing values for the visual-only gauge smoothing and the
/// low-health vignette. Presentation only — no gameplay value lives here.
/// </summary>
public static class UiTheme
{
    // --- gauge readouts (Hud + SkillsPanel rows) --------------------------
    // The 2f palette rule: the panel rows reuse the Hud gauge colour —
    // zero new colours (SkillsPanelTest palette guard pins it).
    public static Color GaugeColor => new(1.0f, 1.0f, 1.0f);
    public const int GaugeFontSize = 20;

    // --- EmpathyPanel text -------------------------------------------------
    public static Color EmpathyTextColor => new(0.95f, 0.9f, 1.0f);
    public const int EmpathyFontSize = 22;

    // --- low-health edge vignette (S6, R6 ratified) ------------------------
    // STRICT less-than: health < 30 pulses, health >= 30 is exactly off.
    // The Hud reads its Life readout (the combat-seam mirror of the
    // player's health) — the View never writes gameplay state (I4).
    public const int LowHealthThreshold = 30;
    public static Color VignetteColor => new(0.85f, 0.08f, 0.08f);
    // Pulse floor/ceiling of the alpha, in integer frames per cycle:
    // a triangle wave on the Hud's redraw counter (F4 — no Tween/Timer,
    // no delta). The floor keeps the tint measurable in every captured
    // frame while low-health holds; "off" is alpha EXACTLY 0.
    public const float VignetteMinAlpha = 0.10f;
    public const float VignetteMaxAlpha = 0.25f;
    public const int VignettePulseFrames = 48;

    // --- gauge VISUAL smoothing (S6) ----------------------------------------
    // The Life digits ease to the exact value within this many redraws on an
    // integer counter (CalmedWindow idiom). Visual only: the Hud's Life
    // property — the logic value every consumer reads — is never touched.
    public const int SmoothFrames = 10;
}
