// Last Animal — MC 1348 P1 regression proofs, harness half (MC 11432 R2 split).
//
// Partial half of P1FixProof: the live-scene SEAM primitives — Compose (node
// discovery + bus hooks), the corpse-leg teleport into reach, and the verdict /
// fail / rooted-fields-release / _Finalize quit-cleanup primitives — moved
// VERBATIM out of the entry file. ZERO behaviour change: the single stage
// machine (the ONE SceneTree state machine, MC 3895 DA P2-1) and the mode
// vocabulary stay in P1FixProof.cs untouched. Same prints, same markers.
//
// Run:  $GODOT --headless --path <proj> --script res://ci_proofs/P1FixProof.cs
using Godot;
using LastAnimal.Companion;
using LastAnimal.Core;
using LastAnimal.Ui;
using LastAnimal.World;
using System.Collections.Generic;

public partial class P1FixProof : SceneTree
{    private bool Compose()
    {
        _bus = Root.GetNodeOrNull<EventBus>("/root/EventBus");
        _director = _main as WorldDirector;
        _playerBody = _director?.GetNodeOrNull<CharacterBody3D>("Player");
        _empathy = _main?.GetNodeOrNull<EmpathyPanel>("UI/Empathy");
        if (_bus == null) { Fail("EventBus autoload not present"); return false; }
        if (_director == null) { Fail("main.tscn root is not WorldDirector"); return false; }
        if (_playerBody == null) { Fail("Player node not found"); return false; }
        if (_empathy == null) { Fail("EmpathyPanel not found at UI/Empathy"); return false; }
        _bus.Betrayal += (_, _) => _betrayalCount++;
        _bus.EmpathyBookOpened += () => _bookOpenedCount++;
        GD.Print($"LA_GATE: composed — zone={_director.CurrentZone} enemies={_director.Enemies.Count}");
        return true;
    }

    private void TeleportToNearestLiveEnemy()
    {
        EnemyActor? target = null;
        float best = float.MaxValue;
        foreach (var e in _director!.Enemies)
        {
            if (e.IsDead || !IsInstanceValid(e)) continue;
            float d = e.GlobalPosition.DistanceTo(_playerBody!.GlobalPosition);
            if (d < best) { best = d; target = e; }
        }
        if (target == null) { Fail("no live enemy to engage"); return; }
        Vector3 p = target.GlobalPosition;
        _playerBody!.GlobalPosition = new Vector3(p.X - 0.8f, p.Y, p.Z);
        _victim = target;
        GD.Print($"LA_GATE: player teleported into range of {target.Name} at ({p.X:0.##},{p.Y:0.##},{p.Z:0.##})");
    }

    /// <summary>
    /// MC 10117 (10026.13.5): same latent class as MC 10112's BridgeMvpProof fix —
    /// the proof is the C# MainLoop; its live-scene fields root the scene's C#
    /// wrappers past native teardown and their finalizers hit freed ObjectDB
    /// entries ("Leaked unsafe reference", exit 134/139, detection ~1/4 — TEST
    /// T-1). Release + flush finalizers BEFORE every PASS Quit while the ObjectDB
    /// is alive. All four modes root these fields at their Quit (see evidence
    /// citations). Idiom REUSED from BridgeMvpProof.ReleaseHeldRefsBeforeQuit.
    /// </summary>
    private void ReleaseHeldRefsBeforeQuit()
    {
        _victim = null;
        _bootEnemies.Clear();
        _bus = null; _director = null; _playerBody = null;
        _empathy = null; _main = null;
        System.GC.Collect();
        System.GC.WaitForPendingFinalizers();
        System.GC.Collect();
    }

    private void Check(string what, bool ok, string detail)
    {
        GD.Print($"LA_GATE: check: {what}: {(ok ? "ok" : "FAIL")} {detail}");
        if (!ok) Fail(what);
    }

    private void Fail(string why)
    {
        Input.ActionRelease("book");
        Input.ActionRelease("travel");
        _failed = true;
        GD.PrintErr($"LA_GATE: FAIL — {why}");
        Quit(1);
    }

    public override void _Finalize()
    {
        Input.ActionRelease("book");
        Input.ActionRelease("travel");
        if (!_asserted && !_failed)
            GD.PrintErr("LA_GATE: FAIL — finished without asserting all stages");
    }
}
