using Godot;
using LastAnimal.World;

// Last Animal — T3b runtime integration proof, shared harness helpers
// (MC 1256.10; split MC 10218 W4 housekeeping).
//
// Partial-class half of RuntimeIntegrationProof carrying the helpers the
// entry dispatch and every stage partial call — ReleaseHeldRefsBeforeQuit
// (MC 10117 shutdown idiom, BridgeMvpProof pattern), FirstLiveEnemy,
// TeleportIntoRange, Check and Fail — moved VERBATIM out of the entry file,
// zero behaviour change: the entry keeps the dispatch, the KnownModes
// allow-list and the header doc mode_sets_check parses (its File-size
// paragraph names this split).
public partial class RuntimeIntegrationProof : SceneTree
{
    /// <summary>
    /// MC 10117 (10026.13.5): same latent class as MC 10112's BridgeMvpProof fix —
    /// the proof is the C# MainLoop, the LAST managed object standing at shutdown.
    /// Its live-scene fields root the scene's C# wrappers (and every Resource they
    /// hold) until after the native ObjectDB is gone, so their GC finalizers hit
    /// freed objects — "Leaked unsafe reference ... csharp_script.cpp:179", exit
    /// 134/139. Detection is probabilistic (~1/4, TEST T-1 MC 10112; at this HEAD
    /// the plant measured 0 RED / 20 — see evidence). Release the wrappers and
    /// flush finalizers BEFORE Quit, while the ObjectDB is alive. Idiom REUSED
    /// from BridgeMvpProof.ReleaseHeldRefsBeforeQuit (MC 10112).
    /// </summary>
    private void ReleaseHeldRefsBeforeQuit()
    {
        _enemies.Clear();
        _bus = null; _director = null; _main = null; _playerBody = null;
        _hud = null; _dialogue = null; _companion = null;
        System.GC.Collect();
        System.GC.WaitForPendingFinalizers();
        System.GC.Collect();
    }

    private EnemyActor? FirstLiveEnemy()
    {
        foreach (var e in _enemies)
            if (!e.IsDead) return e;
        return null;
    }

    private void TeleportIntoRange()
    {
        EnemyActor? target = FirstLiveEnemy();
        if (target == null) { Fail("no live enemy to teleport next to"); return; }
        // Put the player just inside melee range (AttackRange 1.5) of the enemy,
        // at the enemy's height so the 3D distance check is dominated by XZ.
        Vector3 e = target.GlobalPosition;
        _playerBody!.GlobalPosition = new Vector3(e.X - 0.8f, e.Y, e.Z);
        GD.Print($"LA_GATE: player teleported into attack range of {target.Name} at ({e.X:0.##},{e.Y:0.##},{e.Z:0.##})");
    }

    /// <summary>MC 11296: silent re-aim of the SAME placement TeleportIntoRange
    /// makes, for the attack loops. Why: the opening camp now fields at the
    /// 12 m D4 grace ring, so at stage-2 time the goblins are still walking/
    /// settling (they engaged the player long ago under the old 2 m ring);
    /// the once-per-stage teleport raced that settling (battery round 1: the
    /// target slid out of the 1.5 m melee sphere AFTER the teleport -> kill
    /// budget expired). Re-aiming every 30 frames is choreography only — the
    /// assert under test (attack INPUT -> director -> CombatSystem kill) is
    /// unchanged — and prints nothing (determinism streams (i)/(ii) see no
    /// new lines).</summary>
    private void RepositionIntoRange()
    {
        EnemyActor? target = FirstLiveEnemy();
        if (target == null) return;
        Vector3 e = target.GlobalPosition;
        _playerBody!.GlobalPosition = new Vector3(e.X - 0.8f, e.Y, e.Z);
    }

    private void Check(string what, bool ok, string detail)
    {
        GD.Print($"LA_GATE: check: {what}: {(ok ? "ok" : "FAIL")} {detail}");
        if (!ok) Fail(what);
    }

    private void Fail(string why)
    {
        Input.ActionRelease("move_right");
        Input.ActionRelease("attack");
        Input.ActionRelease("interact");
        _failed = true;
        GD.PrintErr($"LA_GATE: FAIL — {why}");
        Quit(1);
    }

    // MC 11019/S48 STEP-0 ceiling split (MC 10218 verbatim-move duty): the
    // entry's _Finalize override moved HERE VERBATIM — the shutdown release
    // list belongs beside ReleaseHeldRefsBeforeQuit/Fail, the harness-shutdown
    // concern; dispatch, mode-read and the fail-safe guard stayed in the entry.
    public override void _Finalize()
    {
        Input.ActionRelease("move_right");   // unconditional: never leak a pressed action
        Input.ActionRelease("attack");
        Input.ActionRelease("interact");
        Input.ActionRelease("travel");       // MC 3912 2e: skill modes travel too
        Input.ActionRelease("skill_1");
        Input.ActionRelease("skill_2");
        Input.ActionRelease("skill_3");      // MC 10031: the calming-speak arm
        Input.ActionRelease("pay_wage");     // MC 3904 2c release idiom (harmless pre-2c)
        Input.ActionRelease("bark");         // MC 10131 S8: the bark press arm
        Input.ActionRelease("speak");        // MC 11493: the speak-key arm (DNA_TALK)
        if (!_asserted && !_failed && _mode is "positive" or "save")
            GD.PrintErr("LA_GATE: FAIL — finished without asserting all stages");
    }
}
