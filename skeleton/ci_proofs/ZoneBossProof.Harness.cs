// Last Animal — zone-travel + boss-phase proof, harness + choreography-seam half
// (MC 11432 R2 split).
//
// Partial half of ZoneBossProof: the press/farm CHOREOGRAPHY helpers
// (PressTravel, KillLoop, NearestLiveEnemy, XzDist), the scaled-stats witness
// (AnyScaledEnemy/DescribeEnemies/BaseHealth), and the verdict / fail /
// rooted-fields-release / _Finalize quit-cleanup primitives — moved VERBATIM
// out of the entry file (the MC 10218 verbatim-move idiom the entry header's
// own ceiling clause owed). ZERO behaviour change: the ONE SceneTree state
// machine (MC 3895 DA P2-1) and the mode vocabulary stay in ZoneBossProof.cs.
// Modes doc: entry header.
//
// Run:  $GODOT --headless --path <proj> --script res://ci_proofs/ZoneBossProof.cs
using Godot;
using LastAnimal.Combat;
using LastAnimal.Core;
using LastAnimal.Save;
using LastAnimal.World;
using System.Collections.Generic;

public partial class ZoneBossProof : SceneTree
{    /// <summary>
    /// MC 10126 (10026.13.6): same latent class as MC 10112's BridgeMvpProof fix —
    /// the proof is the C# MainLoop, the LAST managed object standing at shutdown.
    /// Its live-scene fields root the scene's C# wrappers until after the native
    /// ObjectDB is gone, so their GC finalizers hit freed objects ("Leaked unsafe
    /// reference ... csharp_script.cpp:179", exit 134/139). All three PASS Quits
    /// here (zone_travel/boss_phase/death_load) root the fields Compose() assigns.
    /// Release the wrappers and flush finalizers BEFORE Quit, while the ObjectDB is
    /// alive. Idiom REUSED from BridgeMvpProof.ReleaseHeldRefsBeforeQuit (MC 10112),
    /// as extended by MC 10117. Bypass Quit(1) Fail paths stay untouched (10117 scope).
    /// </summary>
    private void ReleaseHeldRefsBeforeQuit()
    {
        _bus = null; _director = null; _playerBody = null; _main = null;
        System.GC.Collect();
        System.GC.WaitForPendingFinalizers();
        System.GC.Collect();
    }

    /// <summary>Planar (XZ) distance — gravity jitter on Y must not count as
    /// input movement in the death-path checks.</summary>
    private static float XzDist(Vector3 a, Vector3 b)
    {
        b.Y = a.Y;
        return a.DistanceTo(b);
    }

    /// <summary>MC 11294: the nearest LIVE spawned enemy (KillLoop selection idiom,
    /// no boss exclusion — these legs test the attack WIRE, any live enemy is a
    /// valid witness). Null when the zone pool is dry.</summary>
    private EnemyActor? NearestLiveEnemy()
    {
        EnemyActor? best = null;
        float bestD = float.MaxValue;
        foreach (var e in _director!.Enemies)
        {
            if (e.IsDead || !IsInstanceValid(e)) continue;
            float d = e.GlobalPosition.DistanceTo(_playerBody!.GlobalPosition);
            if (d < bestD) { bestD = d; best = e; }
        }
        return best;
    }

    /// <summary>Press travel for one frame out of PressFrames (just-pressed idiom).
    /// Returns true when the press window has elapsed so the zone check is fair.</summary>
    private bool PressTravel()
    {
        _travelToggle++;
        if (_travelToggle == 1) Input.ActionPress("travel");
        if (_travelToggle >= PressFrames)
        {
            // Re-arm (MC 1348 A5 follow-up): the frame stamp that makes
            // IsActionJustPressed true is written only on the released->pressed
            // transition, so a cycle left held (KillLoop took over mid-press)
            // must be released AND the toggle restarted, or the next stage's
            // press is a silent no-op and no travel edge ever fires.
            Input.ActionRelease("travel");
            _travelToggle = 0;
            return true;
        }
        return false;
    }

    /// <summary>One kill-loop frame: keep the nearest live enemy in melee range
    /// and toggle attack, exactly like RuntimeIntegrationProof's kill stages.
    /// Returns false when no farmable enemy remains — the zone's pool is dry
    /// (MC 1348 A5: a zone fields exactly its denizen count, the boot ring is
    /// gone) — so the caller can travel on and keep killing in a fresh set.</summary>
    private bool KillLoop()
    {
        // MC 11294: farm legs are NOT death tests — the death gate refuses a dead
        // farmer its swings (pre-card this rode the D2 corpse-agency defect: the
        // battery leg was false-green ON A CORPSE). Revive ONLY when already
        // dead, through the same shipped model seam the F9 load uses.
        if (_director!.PlayerModel.IsDead) _director.PlayerModel.RestoreHealth(50);
        EnemyActor? target = null;
        float best = float.MaxValue;
        foreach (var e in _director!.Enemies)
        {
            if (e.IsDead || !IsInstanceValid(e)) continue;
            // Never farm the boss itself: killing it before the phase step
            // would silence the very transition this mode proves.
            if (ReferenceEquals(e, _director.BossActor)) continue;
            float d = e.GlobalPosition.DistanceTo(_playerBody!.GlobalPosition);
            if (d < best) { best = d; target = e; }
        }
        if (target == null) return false;
        if (best > _director.PlayerModel.AttackRange)
        {
            Vector3 p = target.GlobalPosition;
            _playerBody!.GlobalPosition = new Vector3(p.X - 0.8f, p.Y, p.Z);
        }
        _attackToggle++;
        if (_attackToggle % 4 == 1) Input.ActionPress("attack");
        else if (_attackToggle % 4 == 3) Input.ActionRelease("attack");
        return true;
    }

    /// <summary>True when any live spawned enemy's AI stats differ from the
    /// hardcoded per-type defaults — i.e. the SpawnSet's scaled table is live.</summary>
    private bool AnyScaledEnemy()
    {
        foreach (var e in _director!.Enemies)
        {
            if (e.IsDead) continue;
            if (e.Ai.Health != BaseHealth(e.Ai.EnemyType)) return true;
        }
        return false;
    }

    private string DescribeEnemies()
    {
        var parts = new List<string>();
        foreach (var e in _director!.Enemies)
            parts.Add($"{e.Ai.EnemyType}:hp={e.Ai.Health}");
        return string.Join(",", parts);
    }

    private static int BaseHealth(EnemyAI.Type t) => t switch
    {
        EnemyAI.Type.Goblin => 30,
        EnemyAI.Type.Orc => 60,
        EnemyAI.Type.Skeleton => 65,   // MC 10183 R2: tracks the EnemyAI base table
        EnemyAI.Type.Wraith => 80,     // MC 10216 S18: tracks the EnemyAI base table (hollow denizen)
        _ => 100, // Demon
    };

    private void Check(string what, bool ok, string detail)
    {
        GD.Print($"LA_GATE: check: {what}: {(ok ? "ok" : "FAIL")} {detail}");
        if (!ok) Fail(what);
    }

    private void Fail(string why)
    {
        Input.ActionRelease("travel");
        Input.ActionRelease("attack");
        _failed = true;
        GD.PrintErr($"LA_GATE: FAIL — {why}");
        Quit(1);
    }

    public override void _Finalize()
    {
        Input.ActionRelease("travel");
        Input.ActionRelease("attack");
        Input.ActionRelease("move_right");
        if (!_asserted && !_failed)
            GD.PrintErr("LA_GATE: FAIL — finished without asserting all stages");
    }
}
