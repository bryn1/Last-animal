using Godot;

// Last Animal — MC 10131 / 10026.x Inc-4 S8 Command Bark (code, 2026-10-07).
//
// Partial-class half of WorldDirector: the bark command, PURELY presentation.
// One input action ("bark", key G — the ONLY input-map change) makes every
// ACTIVE roster companion (the visible follower bodies, index-aligned with the
// roster) open an integer-frame BUBBLE window on its existing body — the
// shipped CalmedWindowFrames idiom (MC 10031): one int per body, swept down by
// exactly 1 per roster tick, ZERO delta timing (F4). CompanionStateMachine
// carries no attention/hold-ish state and the owner approved no gameplay
// command semantics (card 10026 append #9 scope), so this arm READS the roster
// and writes NOTHING but the bubble windows — zero gameplay writes (F4
// doctrine), zero Bus signals (the poll is one IsActionJustPressed read, the
// same kind as the shipped cycle_follower/break_bond polls; census stays 15),
// zero save fields (R7: runtime-only, like the calm window).
//
// Frame contract (TickCalmWindows §2: sweep BEFORE the press chain): a press
// landing on roster-tick frame D0 shows the bubble on D0..D0+15 and it is gone
// EXACTLY on D0+16 — the integer window in/out the S8 leg pins frame-exact
// (a delta-time feed cannot produce the exact end frame; planted RED).
namespace LastAnimal.World;

public partial class WorldDirector
{
    /// <summary>The bark bubble window in roster-tick frames (integer, F4).</summary>
    private const int BarkWindowFrames = 16;

    /// <summary>
    /// The roster tick's bark arm (called from TickRoster, one line): sweep
    /// first — every open window falls by EXACTLY 1 — then the poll: a "bark"
    /// press opens a fresh window on every visible roster body (zero bodies:
    /// the press lands and NOTHING opens; the roster is the authority on who
    /// barks). Bubble VISIBILITY itself follows the counter inside
    /// CompanionFollowBody._Process — the counter is the one truth.
    /// </summary>
    private void TickBark()
    {
        for (int i = 0; i < _bodies.Count; i++)
            if (_bodies[i].BarkWindowFrames > 0) _bodies[i].BarkWindowFrames--;

        if (!Input.IsActionJustPressed("bark")) return;
        for (int i = 0; i < _bodies.Count; i++)
            _bodies[i].BarkWindowFrames = BarkWindowFrames;
        GD.Print($"ROSTER: bark command -> {(_bodies.Count == 0 ? "no active companion" : $"{_bodies.Count} follower(s) bubble ({BarkWindowFrames}f)")}");
    }
}
