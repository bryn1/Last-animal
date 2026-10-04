using Godot;
using LastAnimal.Combat;
using LastAnimal.Core;
using LastAnimal.Core.Framework;
using System.Linq;

// Last Animal — T3b runtime integration proof, MOVE/KILL/HUD/FOLLOW stages
// (MC 1256.10; MC 10098 split duty).
//
// Partial-class half of RuntimeIntegrationProof carrying the POSITIVE-CHAIN
// stage bodies (stages 1/2/3/4) moved VERBATIM out of the main dispatch —
// no behaviour change; the mode contract, stage numbering, markers and
// negative controls are unchanged (see RuntimeIntegrationProof.cs header).
// Extracted so the 600-line main harness gains the S0 bus-emit dispatch
// without breaching the proof ceiling (MC 10098 S0).
public partial class RuntimeIntegrationProof : SceneTree
{
    // ---- stage 1 (moved verbatim out of the main dispatch, MC 10098 split): WASD press -> body -> director-owned PlayerController.
    private bool RunMoveStage()
    {
                // Count physics ticks SINCE THE PRESS (MC 1344.1): process frames
                // are uncapped headless, physics ticks are the real engine time.
                if (_physFrames >= _pressPhysFrame + MoveFrames)
                {
                    Vector3 now = _playerBody.GlobalPosition;
                    float dx = now.X - _playerStart.X;
                    float dz = now.Z - _playerStart.Z;
                    if (!(dx > 0.05f || dz > 0.05f))
                    {
                        Fail($"player did not move on simulated WASD (dx={dx:0.###}, dz={dz:0.###})");
                        return true;
                    }
                    // The body's movement must reach the DIRECTOR-OWNED
                    // PlayerController (single source of truth), resolved
                    // through GameBootstrap — instance identity, not class.
                    var resolved = Root.GetNodeOrNull<GameBootstrap>("/root/GameBootstrap")?.Resolve<PlayerController>();
                    if (resolved == null) { Fail("GameBootstrap.Resolve<PlayerController>() returned null — director did not bind its controller"); return true; }
                    // no_controller mode INJECTS the decoy binding — the identity
                    // mismatch is the defect under test, detected in stage 2 via
                    // the AI-target mismatch (NEG_CONTROLLER). Skip here (MC 1344.1).
                    if (_mode != "no_controller" && !ReferenceEquals(resolved, _director.PlayerModel))
                    {
                        Fail("resolved PlayerController is NOT the director-owned instance (two live controllers)");
                        return true;
                    }
                    float modelDx = resolved.Position.X - _director.PlayerModel.Position.X;
                    // no_controller mode: the decoy position diverges by design —
                    // that divergence is the defect under test (NEG_CONTROLLER in
                    // stage 2), not a failure here (MC 1344.1).
                    if (_mode != "no_controller" && System.Math.Abs(modelDx) > 0.0001f)
                    {
                        Fail("resolved controller position diverged from the director's model");
                        return true;
                    }
                    Input.ActionRelease("move_right");
                    GD.Print("LA_GATE: PLAYER_EXISTS_MOVED — WASD -> body -> director-owned PlayerController (instance identity via GameBootstrap)");
                    TeleportIntoRange();
                    // MC 1345 (option A): capture the follow baseline HERE, right
                    // after the teleport — the companion is still at the player's
                    // pre-teleport position, so dist0 honestly reads "started away"
                    // and the close-in assert in stage 4 is exercised as written.
                    // Capturing at stage 4 raced the companion's convergence during
                    // the attack wait, and the check could fire on the capture frame
                    // itself (_followStartPhys defaults 0), reading field defaults
                    // (dist0=0) instead of a measurement.
                    _companionStart = _companion.GlobalPosition;
                    _followDist0 = _companion.GlobalPosition.DistanceTo(_playerBody.GlobalPosition);
                    _followStartPhys = _physFrames;
                    GD.Print($"LA_GATE: follow baseline captured dist0={_followDist0:0.###}");
                    _stage = 2;
                    _stageFrames = 0;
                }
        return false;
    }

    // ---- stage 2 (moved verbatim): attack loop -> kill -> DnaExtracted; no_bus/no_dna negative controls.
    private bool RunKillStage()
    {
                if (_mode == "no_controller")
                {
                    // The decoy binding must be detectable: the AI's target is
                    // fed from the director's REAL controller, so with the
                    // binding swapped the proof asserts the mismatch and the
                    // gate expects the NEG marker + non-zero exit.
                    var decoy = Root.GetNodeOrNull<GameBootstrap>("/root/GameBootstrap")!.Resolve<PlayerController>();
                    if (decoy != null && !ReferenceEquals(decoy, _director.PlayerModel)
                        && (System.Math.Abs(decoy.Position.X - _director.PlayerModel.Position.X) > 1f))
                    {
                        GD.Print("LA_GATE: NEG_CONTROLLER: EnemyAI target != authoritative PlayerController.Position (decoy binding detected)");
                        Quit(1);
                        return true;
                    }
                    if (_stageFrames > 60) { Fail("no_controller: decoy binding was not detectable"); return true; }
                    return false;
                }

                if (_dnaCount > 0)
                {
                    Input.ActionRelease("attack");
                    if (_mode == "no_dna")
                    {
                        // The kill happened but the director's forwarding was
                        // blocked — the bus must NOT have heard it. If it did,
                        // the break failed (gate broken).
                        Fail("no_dna: DnaExtracted reached the bus despite forwarding disabled — the negative control is broken");
                        return true;
                    }
                    if (_mode == "no_bus")
                    {
                        // The bus fired but the HUD was disconnected: the meter
                        // must NOT have moved. That is the DETECTED break.
                        if (_hud.DnaMeter > _dnaBefore)
                        {
                            Fail("no_bus: Hud.DnaMeter moved despite the HUD being disconnected — the negative control is broken");
                            return true;
                        }
                        GD.Print("LA_GATE: NEG_BUS: DnaExtracted fired but Hud.DnaMeter did not move (HUD disconnected) — break detected");
                        Quit(1);
                        return true;
                    }
                    Check("Hud.DnaMeter incremented by the kill's DnaExtracted",
                          _hud.DnaMeter > _dnaBefore, $"dna={_hud.DnaMeter} (before {_dnaBefore})");
                    if (_failed) return true;
                    GD.Print("LA_GATE: DNA_EXTRACTED_EMITTED — kill via the REAL CombatSystem path -> EventBus.DnaExtracted -> Hud.DnaMeter");
                    // ENEMIES_EXIST_TARGETED (MC 10058): the gate leg grepping this marker was a vacuous || true; the assert lives here now.
                    var aimed = FirstLiveEnemy();
                    if (aimed == null) { Fail("ENEMIES_EXIST_TARGETED: no live enemy left in the director set"); return true; }
                    Check("live enemy AI target tracks the live player position",
                          System.Math.Abs(aimed.PlayerTargetX - _playerBody.GlobalPosition.X) < 0.05f
                          && System.Math.Abs(aimed.PlayerTargetZ - _playerBody.GlobalPosition.Z) < 0.05f,
                          $"aim=({aimed.PlayerTargetX:0.###},{aimed.PlayerTargetZ:0.###}) player=({_playerBody.GlobalPosition.X:0.###},{_playerBody.GlobalPosition.Z:0.###})");
                    if (_failed) return true;
                    GD.Print("LA_GATE: ENEMIES_EXIST_TARGETED — live director-set enemy AIMED at the live player position");
                    if (_mode == "save")
                    {
                        // Save mode skips the HUD/companion stages: the kill
                        // chain already produced the DNA>0 baseline the save
                        // round-trip needs (MC 1344 non-vacuous gate).
                        _stage = 20;
                    }
                    else
                    {
                        _stage = 3;
                    }
                    _stageFrames = 0;
                }
                else if (_stageFrames > AttackBudgetFrames)
                {
                    // no_dna mode: the kill DOES land (forwarding is blocked, so the
                    // bus counter never moves — that is the point). Detect the kill
                    // via the enemy's death and assert the bus stayed silent.
                    if (_mode == "no_dna")
                    {
                        bool anyDead = _enemies.Any(e => e.IsDead);
                        if (anyDead && _dnaCount == 0)
                        {
                            Input.ActionRelease("attack");
                            GD.Print("LA_GATE: NEG_DNA: kill landed (enemy dead) but DnaExtracted never reached the bus (forwarding blocked) — break detected");
                            Quit(1);
                            return true;
                        }
                    }
                    Fail("kill did not land within the attack budget (input -> director -> CombatSystem path broken)");
                }
                else
                {
                    // Press attack 2 frames, release 2, repeat: the director's
                    // attack path fires on IsActionJustPressed with the player
                    // in range.
                    _attackToggle++;
                    if (_attackToggle % 4 == 1) Input.ActionPress("attack");
                    else if (_attackToggle % 4 == 3) Input.ActionRelease("attack");
                }
        return false;
    }

    // ---- stage 3 (moved verbatim): HUD mirrors the director model + bus meter.
    private bool RunHudStage()
    {
                {
                    // HUD_REFLECTS_STATE: the HUD mirrors the director-owned
                    // model (Life) and the bus (DnaMeter).
                    Check("Hud.Life matches the director-owned PlayerController.Health",
                          _hud.Life == _director.PlayerModel.Health,
                          $"hud={_hud.Life} model={_director.PlayerModel.Health}");
                    if (_failed) return true;
                    GD.Print("LA_GATE: HUD_REFLECTS_STATE — Hud.Life == PlayerController.Health (single health tracker)");
                    _stage = 4;
                    _stageFrames = 0;
                }
        return false;
    }

    // ---- stage 4 (moved verbatim): CompanionEntity closes on the player (physics-tick window).
    private bool RunFollowStage()
    {
                // Gate on PHYSICS ticks (MC 1344.1): the companion's lerp is
                // delta-based, so its progress tracks ENGINE TIME. Process frames
                // and physics ticks diverge under Xvfb (a process frame can span
                // many ticks), so a window counted in process frames measures the
                // wrong thing in both directions. 40 physics ticks = 0.67s of
                // engine time regardless of frame rate. The baseline was captured
                // at the teleport (stage 1, MC 1345) — see _followStartPhys.
                if (_physFrames >= _followStartPhys + FollowFrames)
                {
                    // COMPANION_FOLLOWS: the machine-wired CompanionEntity
                    // trails the player.
                    float d1 = _companion.GlobalPosition.DistanceTo(_playerBody.GlobalPosition);
                    float moved = _companion.GlobalPosition.DistanceTo(_companionStart);
                    if (moved < 0.2f || d1 > _followDist0 - 0.25f)
                    {
                        Fail($"companion did not follow (moved={moved:0.###}, dist {_followDist0:0.###} -> {d1:0.###}) playerNow={_playerBody.GlobalPosition} companionNow={_companion.GlobalPosition}");
                        return true;
                    }
                    GD.Print($"LA_GATE: COMPANION_FOLLOWS — CompanionEntity (machine-wired) closed on the player (dist {_followDist0:0.###} -> {d1:0.###})");
                    _stage = 5;
                    _stageFrames = 0;
                }
        return false;
    }
}
