using Godot;
using LastAnimal.Save;
using LastAnimal.World;

// Last Animal — MC 10098 Inc-3 S0 bus-emit proof (stage 80, mode bus_emit).
//
// Proves the S0 seam: the TickUi edge-detect poll (world/WorldDirector.Ui.cs)
// carries FOUR consumer-facing emits onto the REAL autoload EventBus, each
// EXACTLY ONCE per edge, on the live playable scene:
//   DialogueShown   — ""→node: a scripted direct Show("bus_probe"); a
//                     re-poll of the unchanged value may not re-emit.
//   DialogueClosed  — node→"": the uniform 240-tick auto-close (queue/DLQ
//                     code untouched, F3 — the probe rides the public Show).
//   PlayerHurt      — PlayerController.TakeDamage DECREASE only; a restore
//                     (health UP) may not emit; a second decrease emits once.
//   BossFallen      — fields the zone boss, then (a) FALSE-EMIT GUARD (F-2,
//                     DA-c3): zone-exit with the boss ALIVE nulls _boss
//                     (ClearZoneEnemies, WorldDirector.cs:342) — a
//                     HasLiveBoss flip-detector emits here, the tracker must
//                     NOT; (b) re-field the boss, kill it through the REAL
//                     attack path, assert the emit lands once and only once.
// Markers print only after their assertion passes (harness contract).
//
// Run:  LA_GATE_MODE=bus_emit $GODOT --headless --path <proj> \
//         --script res://ci_proofs/RuntimeIntegrationProof.cs   (savegate: the
//         MC 10103 F-B leg below owns user://savegame.json before its write.)
//
// MC 10103 (card 10026.12) extends the stage machine with the two P2 legs
// from the S0-wall DA-verdict (.audits/202610041758-e91b0813), each a named
// F-act gate: phases 17-22 F-A (a same-frame kill+travel must NOT swallow
// BossFallen — the death rides the swap; markers BUS_SWAP_STRIKE /
// BUS_FALLEN_SWAP), phase 23 F-B (loading a lower-HP save lowers Health with
// NO PlayerHurt edge — restore is not damage; markers BUS_SAVE_OWNED /
// BUS_NO_LOAD_HURT).
public partial class RuntimeIntegrationProof : SceneTree
{
    private int _busPhase;
    private int _busPhaseStart;
    private int _busPhaseStamp;
    private int _busQuietStamp;
    private bool _busProbePainted;
    private bool _busSubscribed;
    private int _busShown, _busClosed, _busHurt, _busFallen;
    private string _busShownLast = "", _busClosedLast = "", _busFallenLast = "";
    private int _busHurtLast = -1;
    private int _busTravelToggle, _busAttackToggle;
    private int _busShownBase, _busClosedBase, _busHurtBase, _busFallenBase;
    private string _busZoneBeforeExit = "";

    private const int BusProbeDwellFrames = 240;      // DialogueSystem.RewardLineFrames
    private const int BusRepollFrames = 30;           // re-poll guard window
    private const int BusQuietFrames = 30;            // box-quiet margin (a late queued beat surfaces here)
    private bool _busSpawnArmed;

    // MC 10103 F-A/F-B leg state:
    private EnemyActor? _busFaBoss;
    private string _busFaId = "", _busFaZoneBefore = "";
    private int _busFallenFaBase, _busHurtFbBase, _busSavedHealth;
    private int _busFbSub;

    private bool BusSubscribe()
    {
        if (_busSubscribed) return true;
        if (_bus == null || _director == null) return false;
        _bus.DialogueShown += n => { _busShown++; _busShownLast = n; GD.Print($"LA_GATE: BUS-EVENT DialogueShown {n}"); };
        _bus.DialogueClosed += n => { _busClosed++; _busClosedLast = n; GD.Print($"LA_GATE: BUS-EVENT DialogueClosed {n}"); };
        _bus.PlayerHurt += h => { _busHurt++; _busHurtLast = h; GD.Print($"LA_GATE: BUS-EVENT PlayerHurt {h}"); };
        _bus.BossFallen += b => { _busFallen++; _busFallenLast = b; GD.Print($"LA_GATE: BUS-EVENT BossFallen {b}"); };
        _busSubscribed = true;
        return true;
    }

    // One bus-travel press per cycle with the ZoneBossProof re-arm idiom: the
    // just-pressed stamp is written only on the released->pressed edge, so a
    // cycle left held must release AND restart the toggle.
    private bool BusPressTravel()
    {
        _busTravelToggle++;
        if (_busTravelToggle == 1) Input.ActionPress("travel");
        if (_busTravelToggle >= 8)
        {
            Input.ActionRelease("travel");
            _busTravelToggle = 0;
            return true;
        }
        return false;
    }

    /// <summary>Farm one frame: keep the nearest live NON-boss enemy in melee
    //  and toggle attack (ZoneBossProof.KillLoop idiom). False = pool dry.</summary>
    private bool BusKillLoop()
    {
        EnemyActor? target = null;
        float best = float.MaxValue;
        foreach (var e in _director!.Enemies)
        {
            if (e.IsDead || !GodotObject.IsInstanceValid(e)) continue;
            if (ReferenceEquals(e, _director.BossActor)) continue;   // never farm the boss
            float d = e.GlobalPosition.DistanceTo(_playerBody!.GlobalPosition);
            if (d < best) { best = d; target = e; }
        }
        if (target == null) return false;
        if (best > _director.PlayerModel.AttackRange)
        {
            Vector3 p = target.GlobalPosition;
            _playerBody!.GlobalPosition = new Vector3(p.X - 0.8f, p.Y, p.Z);
        }
        _busAttackToggle++;
        if (_busAttackToggle % 4 == 1) Input.ActionPress("attack");
        else if (_busAttackToggle % 4 == 3) Input.ActionRelease("attack");
        return true;
    }

    private bool BusBossIsNearest(EnemyActor boss)
    {
        // TryAttack hits the NEAREST live enemy in range — the F-A strike
        // frame must be one where that nearest IS the boss.
        float bd = boss.GlobalPosition.DistanceTo(_playerBody!.GlobalPosition);
        if (bd > _director!.PlayerModel.AttackRange) return false;
        foreach (var e in _director!.Enemies)
        {
            if (e.IsDead || ReferenceEquals(e, boss) || !GodotObject.IsInstanceValid(e)) continue;
            if (e.GlobalPosition.DistanceTo(_playerBody!.GlobalPosition) < bd) return false;
        }
        return true;
    }

    private void BusNextPhase(int phase)
    {
        _busPhase = phase;
        _busPhaseStart = _frames;
        _busPhaseStamp = _frames;
    }

    private bool BusPhaseExpired(int budgetFrames) => _frames - _busPhaseStart > budgetFrames;

    private void RunBusStage()
    {
        if (!BusSubscribe()) return;
        var ui = _director!.DialogueUi;

        switch (_busPhase)
        {
            case 0:   // wait for the boot reward beat (q_intro) to settle: the
                      // box closed AND quiet for BusQuietFrames (a late
                      // draining queued beat surfaces inside the margin).
                if (ui.ActiveNode != "") _busQuietStamp = _frames;
                else if (_frames - _busQuietStamp > BusQuietFrames)
                {
                    _busShownBase = _busShown; _busClosedBase = _busClosed;
                    BusNextPhase(1);
                }
                else if (BusPhaseExpired(1200)) Fail("bus_emit: boot dialogue never settled to a quiet closed box");
                break;

            case 1:   // DIALOGUE SHOWN: one direct Show = exactly one emit, and a
                      // re-poll of the unchanged value may not re-emit.
                if (!_busProbePainted) { ui.Show("bus_probe"); _busProbePainted = true; }
                if (_busShown > _busShownBase)
                {
                    Check("DialogueShown fired for the probe node",
                          _busShownLast == "bus_probe" && _busClosed == _busClosedBase,
                          $"shown={_busShown - _busShownBase} node='{_busShownLast}'");
                    if (_failed) return;
                    BusNextPhase(2);   // re-poll guard starts here
                }
                else if (BusPhaseExpired(120)) Fail("bus_emit: Show('bus_probe') produced no DialogueShown emit");
                break;

            case 2:   // re-poll guard: the box stays open unchanged for
                      // BusRepollFrames — the emit count must NOT move again.
                if (_frames - _busPhaseStamp >= BusRepollFrames)
                {
                    Check("DialogueShown fired EXACTLY ONCE across the re-poll window",
                          _busShown - _busShownBase == 1 && ui.ActiveNode == "bus_probe",
                          $"shown={_busShown - _busShownBase} node='{ui.ActiveNode}'");
                    if (_failed) return;
                    GD.Print("LA_GATE: BUS_SHOWN_ONCE — one DialogueShown per open edge, none on re-poll");
                    BusNextPhase(3);
                }
                break;

            case 3:   // DIALOGUE CLOSED: the uniform auto-close (240-tick dwell)
                      // is the ONLY closer (F3). Event-driven — the box flips
                      // ActiveNode inside DialogueSystem._Process, BEFORE the
                      // seam's own tick (frame order: director tick -> box
                      // paint -> this proof frame), so a state read here can
                      // legitimately precede the emit by one tick. Wait for
                      // the emit, then assert the state it mirrors.
                if (_busClosed > _busClosedBase)
                {
                    Check("DialogueClosed fired once for the closing node",
                          _busClosed - _busClosedBase == 1 && _busClosedLast == "bus_probe" && ui.ActiveNode == "",
                          $"closed={_busClosed - _busClosedBase} node='{_busClosedLast}' active='{ui.ActiveNode}'");
                    if (_failed) return;
                    BusNextPhase(4);   // re-poll guard
                }
                else if (BusPhaseExpired(BusProbeDwellFrames + 300))
                    Fail(ui.ActiveNode == ""
                        ? "bus_emit: box closed but no DialogueClosed reached the bus (close edge MISSED)"
                        : "bus_emit: probe line never auto-closed (uniform auto-close broken)");
                break;

            case 4:   // re-poll guard on the closed state.
                if (_frames - _busPhaseStamp >= BusRepollFrames)
                {
                    Check("DialogueClosed fired EXACTLY ONCE across the re-poll window",
                          _busClosed - _busClosedBase == 1 && ui.ActiveNode == "",
                          $"closed={_busClosed - _busClosedBase}");
                    if (_failed) return;
                    GD.Print("LA_GATE: BUS_CLOSED_ONCE — one DialogueClosed per close edge, none missed, none on re-poll");
                    _busHurtBase = _busHurt;
                    _busHurtLast = -1;
                    _director!.PlayerModel.TakeDamage(10);   // the model's only damage entry
                    BusNextPhase(5);
                }
                break;

            case 5:   // PLAYER HURT: one TakeDamage = exactly one emit, payload =
                      // Health AFTER the decrease; re-poll may not re-emit.
                if (_busHurt > _busHurtBase)
                {
                    Check("PlayerHurt fired once with the post-decrease Health",
                          _busHurt - _busHurtBase == 1 && _busHurtLast == _director!.PlayerModel.Health,
                          $"hurt={_busHurt - _busHurtBase} payload={_busHurtLast} health={_director.PlayerModel.Health}");
                    if (_failed) return;
                    BusNextPhase(6);
                }
                else if (BusPhaseExpired(60)) Fail("bus_emit: TakeDamage(10) produced no PlayerHurt emit");
                break;

            case 6:   // restore must NOT emit; a second decrease emits ONCE more.
                if (_frames - _busPhaseStamp >= BusRepollFrames)
                {
                    Check("PlayerHurt fired EXACTLY ONCE across the re-poll window",
                          _busHurt - _busHurtBase == 1,
                          $"hurt={_busHurt - _busHurtBase}");
                    if (_failed) return;
                    _director!.PlayerModel.RestoreHealth(_director!.PlayerModel.MaxHealth);
                    BusNextPhase(7);
                }
                break;

            case 7:   // health went UP — no emit allowed; then a second decrease.
                if (_frames - _busPhaseStamp >= BusRepollFrames)
                {
                    Check("a health RESTORE emitted no PlayerHurt",
                          _busHurt - _busHurtBase == 1,
                          $"hurt={_busHurt - _busHurtBase} after restore");
                    if (_failed) return;
                    _director!.PlayerModel.TakeDamage(5);
                    BusNextPhase(8);
                }
                break;

            case 8:
                if (_busHurt - _busHurtBase == 2 && _busHurtLast == _director!.PlayerModel.Health)
                {
                    GD.Print("LA_GATE: BUS_HURT_ONCE — one PlayerHurt per health DECREASE (none on restore or re-poll)");
                    BusNextPhase(9);
                }
                else if (BusPhaseExpired(60)) Fail("bus_emit: second TakeDamage produced no second PlayerHurt");
                break;

            case 9:   // RE-ARM SPAWNING (the seam, same one the boot ran with
                      // false), field a fresh set via travel, farm to the boss
                      // threshold through the REAL kill path (ZoneBossProof idiom).
                if (!_busSpawnArmed)
                {
                    _director!.SetSpawningEnabled(true);
                    _director.PlayerModel.RestoreHealth(_director.PlayerModel.MaxHealth);
                    _busSpawnArmed = true;
                }
                if (_director!.SpokenDna.Count >= 4) { BusNextPhase(10); break; }
                if (!BusKillLoop()) BusPressTravel();
                if (BusPhaseExpired(3000)) Fail("bus_emit: kill farm never reached BossThreshold(4)");
                break;

            case 10:  // field the zone boss
                if (_director!.HasLiveBoss)
                {
                    GD.Print($"LA_GATE: BUS_BOSS_LIVE zone={_director.CurrentZone} — boss tracked by the seam");
                    _busFallenBase = _busFallen;
                    _busZoneBeforeExit = _director.CurrentZone;
                    BusNextPhase(11);
                }
                else
                {
                    BusPressTravel();
                    if (BusPhaseExpired(1200)) Fail("bus_emit: no live boss after the threshold (field wire broken)");
                }
                break;

            case 11:  // F-2 GUARD (planted-bad pair): leave the zone while the
                      // boss is ALIVE — ClearZoneEnemies nulls _boss; a flip
                      // detector would emit a FALSE BossFallen here. Must not.
                if (BusPressTravel() && _director!.CurrentZone != _busZoneBeforeExit)
                {
                    Check("zone exit left no live boss tracked",
                          !_director.HasLiveBoss, $"zone={_director.CurrentZone}");
                    if (_failed) return;
                    BusNextPhase(12);
                }
                else if (BusPhaseExpired(600)) Fail("bus_emit: travel never left the boss zone");
                break;

            case 12:  // settle window: the false-emit plant shows as fallen>base.
                if (_frames - _busPhaseStamp >= BusRepollFrames)
                {
                    Check("BossFallen did NOT fire on zone exit with the boss alive (F-2)",
                          _busFallen == _busFallenBase,
                          $"fallen={_busFallen - _busFallenBase} after zone exit");
                    if (_failed) return;
                    GD.Print("LA_GATE: BUS_NO_FALSE_FALLEN — zone exit (no boss death) emits no BossFallen");
                    BusNextPhase(13);
                }
                break;

            case 13:  // re-field the boss (threshold stays met; the pool cycles)
                if (_director!.HasLiveBoss) { BusNextPhase(14); }
                else
                {
                    BusPressTravel();
                    if (BusPhaseExpired(2400)) Fail("bus_emit: boss never re-fielded after zone exit");
                }
                break;

            case 14:  // KILL the boss through the REAL attack path (QBossAttackLoop idiom)
                {
                    var boss = _director!.BossActor;
                    if (boss == null || !GodotObject.IsInstanceValid(boss))
                    { BusNextPhase(13); break; }
                    if (boss.IsDead) { BusNextPhase(15); break; }
                    Vector3 p = boss.GlobalPosition;
                    _playerBody!.GlobalPosition = new Vector3(p.X - 0.8f, p.Y, p.Z);
                    _busAttackToggle++;
                    if (_busAttackToggle % 4 == 1) Input.ActionPress("attack");
                    else if (_busAttackToggle % 4 == 3) Input.ActionRelease("attack");
                    if (BusPhaseExpired(3000)) Fail("bus_emit: the boss never died (attack -> Damage -> IsDead wire broken)");
                }
                break;

            case 15:  // the death edge must emit EXACTLY once (settle window)
                if (_busFallen > _busFallenBase)
                {
                    Check("BossFallen payload is the dead boss's EntityId",
                          _busFallenLast == _director!.BossActor!.Ai.EntityId.ToString(),
                          $"payload='{_busFallenLast}'");
                    if (_failed) return;
                    BusNextPhase(16);
                }
                else if (BusPhaseExpired(120)) Fail("bus_emit: boss death produced no BossFallen emit");
                break;

            case 16:  // double-emit guard: the corpse stays live in _enemies for
                      // the settle window — the tracker must hold at one emit.
                if (_frames - _busPhaseStamp >= 4 * BusRepollFrames)
                {
                    Check("BossFallen fired EXACTLY ONCE per death (no re-emit on re-poll)",
                          _busFallen - _busFallenBase == 1,
                          $"fallen={_busFallen - _busFallenBase}");
                    if (_failed) return;
                    Input.ActionRelease("attack");
                    GD.Print("LA_GATE: BUS_FALLEN_ONCE — one BossFallen per tracked-actor death edge");
                    _busFaBoss = null;
                    BusNextPhase(17);   // MC 10103 F-A leg below
                }
                break;

            case 17:  // MC 10103 F-A: re-field a boss for the same-frame leg.
                if (_director!.HasLiveBoss)
                {
                    _busFaBoss = _director.BossActor;
                    _busFaId = _busFaBoss!.Ai.EntityId.ToString();
                    _busFallenFaBase = _busFallen;
                    BusNextPhase(18);
                }
                else
                {
                    BusPressTravel();
                    if (BusPhaseExpired(2400)) Fail("bus_emit F-A: boss never re-fielded for the swap leg");
                }
                break;

            case 18:  // F-A whittle: bring the boss to ONE melee swing while it
                      // stays the NEAREST live enemy (BusBossIsNearest — the
                      // strike must hit IT) and the player stays topped up.
                      // A boss-phase re-field swaps the reference: re-capture.
                {
                    var boss = _director!.BossActor;
                    if (boss == null || !GodotObject.IsInstanceValid(boss) || boss.IsDead)
                    { BusNextPhase(17); break; }
                    if (!ReferenceEquals(boss, _busFaBoss))
                    {
                        _busFaBoss = boss;
                        _busFaId = boss.Ai.EntityId.ToString();
                        _busFallenFaBase = _busFallen;
                    }
                    _director.PlayerModel.RestoreHealth(_director.PlayerModel.MaxHealth);
                    Vector3 p = boss.GlobalPosition;
                    _playerBody!.GlobalPosition = new Vector3(p.X - 0.8f, p.Y, p.Z);
                    if (boss.Ai.Health <= _director.PlayerModel.MeleeDamage)
                    { Input.ActionRelease("attack"); BusNextPhase(19); break; }
                    if (!BusBossIsNearest(boss)) break;   // someone is closer — wait
                    _busAttackToggle++;
                    if (_busAttackToggle % 4 == 1) Input.ActionPress("attack");
                    else if (_busAttackToggle % 4 == 3) Input.ActionRelease("attack");
                    if (BusPhaseExpired(3000)) Fail("bus_emit F-A: whittle never reached a one-swinger boss");
                }
                break;

            case 19:  // F-A STRIKE: ONE frame presses attack AND travel. The
                      // _Process order (attack :249 -> travel :253 -> TickUi)
                      // puts kill + BossActor swap in the SAME director tick,
                      // BEFORE the poll that would observe IsDead — exactly
                      // the window the DA-verdict named (F-A).
                {
                    var boss = _director!.BossActor;
                    if (boss == null || !GodotObject.IsInstanceValid(boss) || boss.IsDead)
                    { BusNextPhase(17); break; }
                    if (boss.Ai.Health > _director.PlayerModel.MeleeDamage || !BusBossIsNearest(boss))
                    { BusNextPhase(18); break; }
                    _busFaZoneBefore = _director.CurrentZone;
                    // Release-then-press: force FRESH JustPressed edges (the
                    // travel may still be HELD from the BusPressTravel cycles —
                    // ActionPress on a held action produces no edge; the
                    // pay_wage idiom, CalmingSpeak.cs).
                    Input.ActionRelease("attack");
                    Input.ActionRelease("travel");
                    Input.ActionPress("attack");
                    Input.ActionPress("travel");
                    GD.Print($"LA_GATE: BUS_SWAP_STRIKE entity={_busFaId} zone={_busFaZoneBefore} hp={boss.Ai.Health} — attack+travel pressed the SAME frame");
                    BusNextPhase(20);
                }
                break;

            case 20:  // F-A conditions: the strike must have killed the boss AND
                      // travelled in the same tick (a nearer enemy absorbing the
                      // hit reruns the leg — never assert on a missed window).
                if (_frames - _busPhaseStart < 3) break;   // let one director tick consume the press
                {
                    Input.ActionRelease("attack");
                    Input.ActionRelease("travel");
                    var dead = _busFaBoss!;
                    if (!dead.IsDead)
                    {
                        GD.Print("LA_GATE: BUS_SWAP_RETRY — strike frame did not kill the boss (absorbed hit), re-running the leg");
                        BusNextPhase(17);
                        break;
                    }
                    Check("F-A: same-frame strike killed the boss AND swapped BossActor before any poll could observe the death",
                          _director!.CurrentZone != _busFaZoneBefore && !ReferenceEquals(_director.BossActor, dead),
                          $"zone-before='{_busFaZoneBefore}' zone-now='{_director.CurrentZone}'");
                    if (_failed) return;
                    BusNextPhase(21);
                }
                break;

            case 21:  // F-A emit wait: the death rode the swap — BossFallen
                      // must still reach the bus. RED WITHOUT THE FIX (the
                      // swap branch re-armed without emitting).
                if (_busFallen > _busFallenFaBase)
                {
                    Check("F-A: same-frame kill+travel emits BossFallen for the swapped-out dead boss (no swallow)",
                          _busFallenLast == _busFaId, $"payload='{_busFallenLast}' want='{_busFaId}'");
                    if (_failed) return;
                    BusNextPhase(22);
                }
                else if (BusPhaseExpired(120))
                    Fail("F-A: boss died unemitted — the same-frame swap swallowed BossFallen (the audited window)");
                break;

            case 22:  // F-A settle: exactly one emit for the swap-carried death.
                if (_frames - _busPhaseStart >= 4 * BusRepollFrames)
                {
                    Check("F-A: the swap-carried death emitted EXACTLY ONCE (no re-emit on re-poll)",
                          _busFallen - _busFallenFaBase == 1,
                          $"fallen={_busFallen - _busFallenFaBase}");
                    if (_failed) return;
                    GD.Print("LA_GATE: BUS_FALLEN_SWAP — F-A closed: a death riding a swap still emits once");
                    _busFbSub = 0;
                    BusNextPhase(23);
                }
                break;

            case 23:  // MC 10103 F-B, five sub-steps. The poll samples at the
                      // FRAME BOUNDARY (F4), so the sequence must spread over
                      // ticks: 0 own the save (MC 3910 delete) + top up + step
                      // out of reach; 1 clean window -> TRUE decrease (stamps
                      // the lower HP, visible across a boundary); 2 true edge
                      // landed -> save + restore HP UP; 3 after >=2 polls saw
                      // the HIGH value, press load_game; 4 settle — the load
                      // lowers Health with NO PlayerHurt edge.
                if (_busFbSub == 0)
                {
                    _director!.SetSpawningEnabled(false);   // load re-entry fields nobody
                    _director.PlayerModel.RestoreHealth(_director.PlayerModel.MaxHealth);
                    var store = new GodotSaveStore();
                    if (System.IO.File.Exists(store.SavePath)) System.IO.File.Delete(store.SavePath);
                    var far = _playerBody!.GlobalPosition;
                    _playerBody.GlobalPosition = new Vector3(far.X + 80f, far.Y, far.Z);   // no live-enemy damage inside the windows
                    _busHurtFbBase = _busHurt;
                    GD.Print("LA_GATE: BUS_SAVE_OWNED — stale save removed before this leg's write");
                    _busPhaseStamp = _frames;
                    _busFbSub = 1;
                    break;
                }
                if (_busFbSub == 1 && _frames - _busPhaseStamp >= BusRepollFrames)
                {
                    Check("F-B harness: no stray PlayerHurt in the clean window (teleport + spawn-off held)",
                          _busHurt == _busHurtFbBase, $"hurt={_busHurt - _busHurtFbBase}");
                    if (_failed) return;
                    _director!.PlayerModel.TakeDamage(30);   // TRUE decrease, visible at the next poll
                    _busSavedHealth = _director.PlayerModel.Health;
                    _busPhaseStamp = _frames;
                    _busFbSub = 2;
                    break;
                }
                if (_busFbSub == 2 && _frames - _busPhaseStamp >= BusRepollFrames)
                {
                    Check("F-B harness: the true TakeDamage edge emitted exactly once (baseline)",
                          _busHurt - _busHurtFbBase == 1 && _busHurtLast == _busSavedHealth,
                          $"hurt={_busHurt - _busHurtFbBase} payload={_busHurtLast} saved={_busSavedHealth}");
                    if (_failed) return;
                    _director!.SaveGame();                       // carries the LOWER HP
                    GD.Print($"LA_GATE: BUS_SAVE_WRITTEN health={_busSavedHealth}");
                    _busHurtFbBase = _busHurt;                   // count only load-window edges from here
                    _director.PlayerModel.RestoreHealth(_director.PlayerModel.MaxHealth);
                    _busPhaseStamp = _frames;
                    _busFbSub = 3;
                    break;
                }
                if (_busFbSub == 3 && _frames - _busPhaseStamp >= 3)
                {
                    // >=2 polls have now observed the HIGH HP — the load's
                    // drop to the saved LOWER value crosses a frame boundary
                    // (the audited false-edge shape, F-B).
                    Input.ActionRelease("load_game");
                    Input.ActionPress("load_game");
                    _busPhaseStamp = _frames;
                    _busFbSub = 4;
                    break;
                }
                if (_busFbSub == 4 && _frames - _busPhaseStamp >= BusRepollFrames)
                {
                    Input.ActionRelease("load_game");
                    Check("F-B harness: the load really restored the stamped LOWER Health (the false-edge condition held)",
                          _director!.PlayerModel.Health == _busSavedHealth && _busSavedHealth < _director.PlayerModel.MaxHealth,
                          $"health={_director.PlayerModel.Health} saved={_busSavedHealth}");
                    Check("F-B: loading a lower-HP save emits NO PlayerHurt edge (restore is not damage)",
                          _busHurt == _busHurtFbBase,
                          $"hurt={_busHurt - _busHurtFbBase} after load of health={_busSavedHealth}");
                    if (_failed) return;
                    GD.Print("LA_GATE: BUS_NO_LOAD_HURT — F-B closed: a save-load health restore emits no false PlayerHurt");
                    GD.Print("LA_GATE: PASS — S0 bus-emit seam verified (shown/closed/hurt each once per edge; BossFallen only on the tracked boss's death, F-2; MC 10103: swap-carried death emits once (F-A), load-restore emits no hurt (F-B))");
                    _asserted = true;
                    Quit(0);
                }
                break;
        }
    }
}
