using Godot;
using LastAnimal.Core;
using LastAnimal.World;

// Last Animal — T3b runtime integration proof, boot-time mode setup
// (MC 11018 S45 SPLIT, MC 10218 verbatim-move precedent).
//
// _Initialize's BODY (frame cap + composition-root instantiation + every
// mode's set-before-AddChild spawn/hook gates + the S45 LA_RIG_CAPTURE
// read) moved VERBATIM out of the 637-l entry, which crossed the 600 house
// ceiling at S27; §6's law: "ANY further entry growth MUST be paid by an
// MC 10218-precedent verbatim-move split BEFORE merging." The mode READ and
// the unknown-mode fail-safe guard STAY in the entry's own _Initialize (the
// mode_sets_check fail-safe grep is entry-scoped — a moved-away guard reads
// RED by design); the entry calls this right after the guard passes.
// Zero behaviour change: the full battery re-run at this head is the proof;
// mode_sets_check's 3-way vocabulary gate is untouched by the move.
public partial class RuntimeIntegrationProof : SceneTree
{
    /// <summary>The boot gates half of _Initialize (the entry keeps the mode
    /// read + unknown-mode guard; see RuntimeIntegrationProof.cs).</summary>
    private void InitializeBootGates()
    {
        // MC 1344.1: in --script mode the autoload EventBus loads AFTER _Initialize,
        // and the HUD is built by WorldDirector.BuildUi() in _Ready — both unavailable
        // here. Do NOT create a local bus (it collides with the autoload at
        // /root/EventBus and splits the wire); resolve bus + HUD in _ComposeDeferred
        // on the first _Process frame, after the scene _Ready has run.
        // Harness fix (MC 1344.1): cap process frames so physics ticks accumulate
        // normally (same pattern as BridgeMvpProof).
        Engine.MaxFps = 60;

        // MC 11099 S49-COMP: session-only low-framerate probe (LA_MAX_FPS, the
        // LA_RIG_CAPTURE env idiom, same boot gate). The S45 cold-repro (MC
        // 11018) died the (E) follow check under a first-boot llvmpipe frame
        // rate; this hook reproduces that condition IN-PROCESS and
        // deterministically — no cold HOME, no raster choice — so the battery
        // leg below forces the class instead of waiting for the environment to
        // roll it. Unset (every shipped leg) keeps the 60 pin above untouched.
        var fpsCap = System.Environment.GetEnvironmentVariable("LA_MAX_FPS");
        if (int.TryParse(fpsCap, out var fps) && fps > 0) Engine.MaxFps = fps;

        // Instance the composition root (a --script run does not load main_scene).
        var packed = GD.Load<PackedScene>("res://main.tscn");
        if (packed == null) { Fail("cannot load res://main.tscn"); return; }
        Node3D main = packed.Instantiate<Node3D>();
        // no_spawn: disable enemy spawning BEFORE the director populates — the
        // director spawns in _Ready, which runs at AddChild, so the flag must be
        // set on the INSTANTIATED (not yet added) node (MC 1344.1).
        if (_mode == "no_spawn" && main is WorldDirector d) d.SetSpawningEnabled(false);
        // bus_emit (MC 10098 S0): the scripted dialogue/hurt legs need the
        // boot zone QUIET (a standing player is whittled to death by the
        // spawn set in ~4 s — see evidence 2026-10-05); spawning is OFF at
        // boot and re-armed by the farm phase through the same public seam.
        if (_mode == "bus_emit" && main is WorldDirector db) db.SetSpawningEnabled(false);
        // quest_neg: the story/quest gate seam (MC 3904 2c) must be off BEFORE
        // the director's _Ready runs InitStory — same set-before-AddChild idiom.
        if (_mode == "quest_neg" && main is WorldDirector dneg) dneg.SetQuestHooksEnabled(false);
        // skill_neg: the skill gate seam (MC 3912 2e) must be off BEFORE
        // _Ready runs InitSkills — same set-before-AddChild idiom.
        if (_mode == "skill_neg" && main is WorldDirector dsk) dsk.SetSkillActionsEnabled(false);
        // calm_neg: the calm cast + window ride the SAME seam (MC 10031) —
        // off before _Ready; the NEG_CALM leg proves a funded press leaks zero.
        if (_mode == "calm_neg" && main is WorldDirector dcm) dcm.SetSkillActionsEnabled(false);
        // JUICE_SHAKE (MC 10121 S2): the shake legs are scripted quiet (the
        // bus_emit idiom) — a standing player would be whittled by the spawn
        // set and stray PlayerHurt edges would re-arm the window mid-assert.
        if (_mode == "JUICE_SHAKE" && main is WorldDirector dsh) dsh.SetSpawningEnabled(false);
        // CAMERA_KILL_PULSE (MC 10217 S20): the pulse leg is scripted quiet
        // (the bus_emit/JUICE_SHAKE idiom) — its wire is a DIRECT bus emit and
        // nothing else must move the follow target mid-assert.
        if (_mode == "CAMERA_KILL_PULSE" && main is WorldDirector dkp) dkp.SetSpawningEnabled(false);
        // DAYNIGHT_STATE (MC 10199 S15): the clock leg drives no combat and
        // reads no gameplay state — quiet boot (bus_emit idiom) keeps the
        // ~23 s cycle run deterministic and cheap. The driver is PAUSED at
        // construction and resumed in _ComposeDeferred: composition ticks
        // (frame 0/1 writes) never slip past the leg's first observation, so
        // the DAWN anchor window is unskippable (ordering-agnostic). No save
        // is touched.
        if (_mode == "DAYNIGHT_STATE" && main is WorldDirector ddn)
        {
            ddn.SetSpawningEnabled(false);
            ddn.SetDayNightPaused(true);
        }
        // BARK (MC 10131 S8): the bark leg is scripted quiet (bus_emit idiom) —
        // zero enemies means nothing can move the gameplay snapshot mid-assert;
        // the roster shapes are built IN the leg through the product rebuild
        // (planted saves, MC 3910 OWNED).
        if (_mode == "BARK" && main is WorldDirector dbg) dbg.SetSpawningEnabled(false);
        // FLOOR_TRUST (MC 10516 S38): the edge-walk/clamp/kill-plane leg parks
        // the player on bare borders and drops them off the map — quiet boot
        // (bus_emit idiom), no whittling, no interference with the fall.
        if (_mode == "FLOOR_TRUST" && main is WorldDirector dft) dft.SetSpawningEnabled(false);
        // ZONE_VISUAL_MOUNT (MC 10519 S27): quiet boot so the boot census +
        // first park stand in a calm scene; the leg RE-ARMS the same public
        // seam before its first travel press — every zone it ENTERS then
        // fields its SpawnSet for real (the mount-vs-spawn assert needs it).
        if (_mode == "ZONE_VISUAL_MOUNT" && main is WorldDirector dzm) dzm.SetSpawningEnabled(false);
        // CAMERA_RIG_ISO (MC 11018 S45): the rig pins are scripted quiet
        // (bus_emit idiom) — nothing may move the follow target mid-assert.
        // LA_RIG_CAPTURE (capture runs only) parks the live scene per zone.
        if (_mode == "CAMERA_RIG_ISO")
        {
            if (main is WorldDirector dcri) dcri.SetSpawningEnabled(false);
            var cap = System.Environment.GetEnvironmentVariable("LA_RIG_CAPTURE");
            _crCaptureZone = string.IsNullOrEmpty(cap) ? null : cap;
        }
        // FX_GLCOMPAT / _PLANT (MC 11019 / 10026.44.7 S48 STEP-0): the GL-Compat
        // capability probe is scripted quiet (bus_emit idiom) — a live spawn set
        // would move agents across the probe's sample patches and the FBO would
        // never settle; the leg touches no gameplay state at all. The clock is
        // PAUSED too (DAYNIGHT_STATE seam): the ~23 s sun cycle re-lights the
        // ground patches between the two settle grabs — a frozen clock is what
        // makes a pixel DELIVERABLE here, and the probe measures RENDERER
        // capability, never the clock.
        if (_mode is "FX_GLCOMPAT" or "FX_GLCOMPAT_PLANT" && main is WorldDirector dfx)
        {
            dfx.SetSpawningEnabled(false);
            dfx.SetDayNightPaused(true);
        }
        Root.AddChild(main);

        _main = main;
    }
}
