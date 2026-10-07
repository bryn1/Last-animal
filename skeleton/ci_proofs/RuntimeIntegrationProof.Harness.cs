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
}
