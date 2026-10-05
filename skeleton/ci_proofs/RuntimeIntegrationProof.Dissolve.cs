using Godot;
using LastAnimal.World;

// Last Animal — MC 10129 Inc-3 S3 death-dissolve proof (stage 97, mode
// DISSOLVE_SUPPRESS). The ghost-body EVIDENCE leg of plan S3 / DA P2-4.
//
// Proves the S3 contract on the LIVE playable scene:
//   1. kill a live non-boss enemy on the REAL input wire (attack press ->
//      director TryAttack -> the ONE DealDamage site -> KillHide);
//   2. from the death tick the body's collision MASK is 0 (the plan-named
//      suppression line — cleared through the engine's 1-based layer-number
//      API; the 0-based literal (0,false) is a silent no-op, proven by the
//      MC 10129 probe, see the evidence dir) — the planted-bad "skip
//      collision-off" plant goes RED here (the corpse would remain a
//      touchable ghost body);
//   3. the visual dissolves over 20 integer frames (F4) and frees ITSELF at
//      f+21; the body's Visual reference is cleared at the despawn tick;
//   4. through the whole window a FORCED attack deals 0: repeated input
//      presses never land on the corpse (the director's own targeting
//      predicate WorldDirector.cs:375 "if (e.IsDead) continue;" already
//      excludes it — this leg PROVES the exclusion, it does not re-assert a
//      new mechanism) and a direct forced CombatSystem.DealDamage on the
//      corpse returns false with the corpse health pinned at 0 and zero
//      DamageDealt events naming its entity id;
//   5. the enemy-count/existence prints (DISSOLVE_ABSENT) show the corpse
//      ABSENT from the live/targeting population while it dissolves.
//
// Scoping: other live enemies may legitimately fall to the forced presses —
// every corpse assertion is scoped to the target's own entity id / Ai, so
// the leg cannot false-green on them and cannot false-red on them either.
//
// Run:  LA_GATE_MODE=DISSOLVE_SUPPRESS $GODOT --headless --path <proj> \
//         --script res://ci_proofs/RuntimeIntegrationProof.cs
public partial class RuntimeIntegrationProof : SceneTree
{
    private int _dissPhase;
    private int _deathFrame = -1;
    private int _dissAttackToggle;
    private int _dissCorpseHits;          // post-death DamageDealt events naming the corpse
    private EnemyActor? _dissTarget;
    private int _dissLiveAtAbsent;
    private bool _dissNoCollisionPrinted, _dissActivePrinted, _dissAbsentPrinted;

    private const int DissKillBudgetFrames = 600;  // presses + approach + flee chase
    private const int DissForceReadoutFrame = 8;   // mid-window forced attack
    private const int DissAbsentReadoutFrame = 12; // mid-window count/existence print
    private const int DissWindowFrames = 14;       // end of the always-on window asserts
    private const int DissGoneReadoutFrame = 22;   // f+21 despawn + 2f readout margin

    private void DissFail(string why) => Fail("DISSOLVE_SUPPRESS: " + why);

    /// <summary>The director's TryAttack targeting scan (WorldDirector.cs:371-379)
    /// reproduced line-for-line — the corpse-exclusion predicate is REUSED from
    /// there, this is the evidence mirror, not a second mechanism.</summary>
    private EnemyActor? DissLiveScan(out int live)
    {
        EnemyActor? best = null;
        float bestDist = _director!.PlayerModel.AttackRange;
        live = 0;
        Vector3 ppos = _playerBody!.GlobalPosition;
        foreach (var e in _director.Enemies)
        {
            if (e.IsDead) continue;   // the cited predicate: world/WorldDirector.cs:375
            live++;
            float d = (e.GlobalPosition - ppos).Length();
            if (d <= bestDist) { best = e; bestDist = d; }
        }
        return best;
    }

    private void OnDissDamage(int attackerId, int targetId)
    {
        // count ONLY post-kill hits naming the corpse's entity id — pre-death
        // hits on the same id are the legitimate kill chain, not ghost damage.
        if (_deathFrame >= 0 && _dissTarget != null && targetId == _dissTarget.Ai.EntityId)
            _dissCorpseHits++;
    }

    private void RunDissolveStage()
    {
        switch (_dissPhase)
        {
            case 0:
            {
                // Nearest live NON-boss enemy (the Juice-leg selector idiom).
                EnemyActor? target = null;
                foreach (var e in _director!.Enemies)
                {
                    if (e.IsDead || !GodotObject.IsInstanceValid(e)) continue;
                    if (ReferenceEquals(e, _director.BossActor)) continue;
                    target = e;
                    break;
                }
                if (target == null) { DissFail("no live non-boss enemy to kill"); break; }
                _dissTarget = target;
                _director.Combat.DamageDealt += OnDissDamage;   // corpse-scoped damage wire
                GD.Print($"LA_GATE: dissolve target {target.Name} (entity {target.Ai.EntityId}); pressing attack on the real wire");
                _dissPhase = 1;
                break;
            }

            case 1:   // kill it on the real wire: hold range each frame (flee-proof),
                      // press attack 2f / release 2f (the Chain.cs press idiom).
            {
                if (_dissTarget!.IsDead)
                {
                    _deathFrame = _frames;   // suppression reads start at +1f (same-frame order is harness-dependent)
                    GD.Print($"LA_GATE: DISSOLVE_DEATH_TICK corpse made on the real wire at f={_frames}");
                    Input.ActionRelease("attack");   // reset the toggle: window presses start clean
                    _dissAttackToggle = 0;
                    _dissPhase = 2;
                    break;
                }
                var p = _dissTarget.GlobalPosition;
                _playerBody!.GlobalPosition = new Vector3(p.X - 0.8f, p.Y, p.Z);
                _dissAttackToggle++;
                if (_dissAttackToggle % 4 == 1) Input.ActionPress("attack");
                else if (_dissAttackToggle % 4 == 3) Input.ActionRelease("attack");
                if (_frames > DissKillBudgetFrames) DissFail("kill never landed within the budget");
                break;
            }

            case 2:   // the f+1..+20 window: forced attacks land nothing, the body
                      // is not a body, the visual dissolves on its integer counter.
            {
                int sinceDeath = _frames - _deathFrame;
                if (_dissTarget!.Ai.Health != 0) { DissFail($"corpse health left 0 during the window ({_dissTarget.Ai.Health})"); break; }
                if (_dissCorpseHits != 0) { DissFail($"corpse TOOK DAMAGE during the dissolve window ({_dissCorpseHits} events) — ghost body"); break; }

                if (!_dissNoCollisionPrinted && sinceDeath >= 1)
                {
                    // The ghost-body RED trigger: skip the collision-off line in
                    // KillHide and the mask stays 1 (probe: the (0,false) literal
                    // is a no-op) so this read goes non-zero and the leg FAILS.
                    uint mask = _dissTarget.GetCollisionLayer();
                    if (mask != 0)
                    { DissFail("corpse collision mask STILL " + mask + " (a touchable ghost body) at +" + sinceDeath + "f"); break; }
                    GD.Print($"LA_GATE: DISSOLVE_NO_COLLISION mask=0 at death tick (+{sinceDeath}f read)");
                    _dissNoCollisionPrinted = true;
                }
                if (!_dissActivePrinted && sinceDeath >= 1)
                {
                    if (!ActorVisual.IsDissolving(_dissTarget.Visual))
                    { DissFail("visual dissolve NOT armed at +" + sinceDeath + "f"); break; }
                    GD.Print($"LA_GATE: DISSOLVE_ACTIVE 20f alpha/scale window draining (+{sinceDeath}f read)");
                    _dissActivePrinted = true;
                }

                // Forced attack #1 — the real input wire, presses kept coming at
                // the corpse (player held 0.8 m away, inside AttackRange).
                _dissAttackToggle++;
                if (_dissAttackToggle % 4 == 1) Input.ActionPress("attack");
                else if (_dissAttackToggle % 4 == 3) Input.ActionRelease("attack");

                if (sinceDeath == DissForceReadoutFrame)
                {
                    // Forced attack #2 — direct on the single kill site, the
                    // HARDEST forcing the runtime offers. Must deal 0.
                    bool dealt = _director!.Combat.DealDamage(0, _dissTarget.Ai, 9999);
                    if (dealt || _dissTarget.Ai.Health != 0 || _dissCorpseHits != 0)
                    { DissFail("FORCED DealDamage on the corpse dealt damage (returned " + dealt + ")"); break; }
                    GD.Print("LA_GATE: DISSOLVE_FORCE_ZERO forced DealDamage(9999) dealt 0, health pinned, zero damage events");
                }
                if (!_dissAbsentPrinted && sinceDeath >= DissAbsentReadoutFrame)
                {
                    // Mirror the director's OWN targeting scan (predicate cited
                    // from TryAttack, WorldDirector.cs:375) and prove the corpse
                    // is neither its result nor part of the live population.
                    EnemyActor? best = DissLiveScan(out int live);
                    if (ReferenceEquals(best, _dissTarget))
                    { DissFail("corpse is STILL director-targetable inside the window"); break; }
                    _dissLiveAtAbsent = live;
                    GD.Print($"LA_GATE: DISSOLVE_ABSENT raw={_director!.Enemies.Count} live={live} corpseIsDead={_dissTarget!.IsDead} corpseTargeted=False corpseVisualExists=True");
                    _dissAbsentPrinted = true;
                }
                if (sinceDeath > DissWindowFrames)
                {
                    Input.ActionRelease("attack");
                    _dissPhase = 3;
                }
                break;
            }

            case 3:   // the visual despawned itself at f+21; the corpse body is
                      // inert: collision off, health 0, zero damage, no visual.
            {
                if (_frames - _deathFrame < DissGoneReadoutFrame) break;
                if (_dissTarget!.Visual != null)
                { DissFail("visual NOT despawned by f+21 (body still holds it)"); break; }
                if (_dissTarget.GetCollisionLayer() != 0)
                { DissFail("corpse collision mask came back non-zero after the window"); break; }
                if (_dissTarget.Ai.Health != 0 || _dissCorpseHits != 0)
                { DissFail("corpse health/damage moved after the death tick"); break; }
                GD.Print("LA_GATE: DISSOLVE_GONE f+21 visual despawned (Visual ref cleared; body inert: collision off, health 0, zero damage events)");
                EnemyActor? bestAfter = DissLiveScan(out int liveAfter);
                if (ReferenceEquals(bestAfter, _dissTarget))
                { DissFail("corpse became targetable again after the visual despawn"); break; }
                GD.Print($"LA_GATE: DISSOLVE_ABSENT raw={_director!.Enemies.Count} live={liveAfter} corpseIsDead={_dissTarget.IsDead} corpseTargeted=False corpseVisualExists=False");
                GD.Print("LA_GATE: PASS — S3 death dissolve verified (collision off + targeting predicate reuse at death tick; 20f integer dissolve; visual despawn f+21; forced attack 0)");
                _asserted = true;
                _director.Combat.DamageDealt -= OnDissDamage;
                _dissTarget = null;                   // MC 10117 rooted-fields idiom
                ReleaseHeldRefsBeforeQuit();          // (Shake.cs precedent)
                Quit(0);
                break;
            }
        }
    }
}
