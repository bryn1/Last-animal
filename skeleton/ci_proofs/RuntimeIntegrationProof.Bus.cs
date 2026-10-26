// SIZE reason (over the 600-l proof ceiling at 685 l by wc AT the MC 11320 head —
// 676 pre-rebase; +5 as the MC 11301 alive-revive normalization rebased under this
// card at 4a36611, its case-9 removal + case-18 revive-if-dead guard riding the same
// file with DISJOINT hunks, KEEP-BOTH; +4 this header's own restamp lines — the
// arith named per the never-delta-sum discipline, all measured by wc AT this head):
// the stage machine is ONE leg matrix — S0 four-edge proofs + F-A + F-B share
// one phase switch, one bus-subscription set and one settle-cadence (the
// battery-matrix class; prior reading 574 l by wc AT the MC 11294 head, +102 AT
// this head = L1 exit-tracker assert, L2 keep-alive seam, L3 roster-silence
// step + their attribution comments). 11301's "split-owed if it grows further"
// clause FIRES: the F-A/F-B bodies are the named split carry into
// RuntimeIntegrationProof.Bus.{FA,FB}.cs partials — a mechanism move OUT of
// this re-cert card's scope, no behavior rides it, file stays whole here.
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
//                     DA-c3; restamped honest by MC 11320 L1): zone-exit with
//                     the boss ALIVE drops that boss from the tracker
//                     (ClearZoneEnemies) while the ENTERED zone may re-field
//                     its OWN boss in the SAME travel tick (ApplySpawnSet's
//                     `if (spawned.IsBoss) _boss = actor`) — so HasLiveBoss
//                     can legitimately be TRUE right after the exit (ruins →
//                     hollow re-fields; the old `!HasLiveBoss` proxy rotted
//                     then and lied RED deterministically). The assert is the
//                     exit-tracked actor left the tracker AND nothing emitted:
//                     a flip-detector would emit here, the tracker must NOT.
//                     (b) re-field the boss, kill it through the REAL
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
//
// MC 11320 RE-CERT (this leg was DEAD CI CODE at head — never a battery leg,
// last honest green 2026-10-05; findings L1/L2/L3 named by MC 11301, evidence
// .audits/202610251700-d31): L1 — the F-2 exit proxy `!HasLiveBoss` rotted
// when the ENTERED zone began re-fielding its own boss on the SAME travel
// tick (ruins -> hollow); the assert is now the EXIT-TRACKED actor leaving
// the tracker (phase 11), the no-emit half stays the phase-12 window. L2 —
// the travel waits run inside the live spawn set, so BusPressTravel carries
// the MC 11294 farm keep-alive (revive-if-dead ONLY, the quoted seam);
// without it the 11294 death gate starves the leg at the F-A re-field
// (measured at this head: named LA_DEATH then "boss never re-fielded").
// L3 — phase 23 sub -1 silences the roster betrayal settle, the damage
// source the clean window missed (window FAIL 5/7 without it,
// out/TIP-measure-run*.log), through the SHIPPED book/break_bond inputs
// before ANY F-B window opens; marker BUS_FB_QUIET_ROSTER. The leg rides the
// battery as a NAMED leg from this card (count restamp in the battery).
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
    private EnemyActor? _busBossBeforeExit;   // MC 11320 L1: the actor the tracker held when the F-2 exit began

    private const int BusProbeDwellFrames = 240;      // DialogueSystem.RewardLineFrames
    private const int BusRepollFrames = 30;           // re-poll guard window
    private const int BusQuietFrames = 30;            // box-quiet margin (a late queued beat surfaces here)
    private bool _busSpawnArmed;

    // MC 10103 F-A/F-B leg state:
    private EnemyActor? _busFaBoss;
    private string _busFaId = "", _busFaZoneBefore = "";
    private int _busFallenFaBase, _busHurtFbBase, _busSavedHealth;
    private int _busFbSub;
    private int _busSilenceStep;   // MC 11320 L3: roster-silence press cadence (F-B pre-window)

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

    // Fresh release-then-press tap (the pay_wage idiom: ActionPress on a HELD
    // action produces no edge) — used by the phase-23 roster-silence presses
    // (book / break_bond / cycle_follower), each polled on the next tick.
    private static void BusTap(string action)
    {
        Input.ActionRelease(action);
        Input.ActionPress(action);
    }

    // One bus-travel press per cycle with the ZoneBossProof re-arm idiom: the
    // just-pressed stamp is written only on the released->pressed edge, so a
    // cycle left held must release AND restart the toggle.
    private bool BusPressTravel()
    {
        // MC 11320 L2: EVERY travel wait in this mode runs inside the live
        // spawn set — waiting for the boss pool to cycle IS farm time, not a
        // death test (MC 11294's law, quoted from BusKillLoop below). Under
        // the 11294 gate a DEAD farmer's travel press is REFUSED outright, so
        // a seam-free travel wait starves the leg — measured at this head:
        // "LA_GATE: FAIL — bus_emit F-A: boss never re-fielded for the swap
        // leg" after a named LA_DEATH. Revive ONLY when already dead, through
        // the same shipped model seam the F9 load uses.
        if (_director!.PlayerModel.IsDead) _director.PlayerModel.RestoreHealth(50);
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
        // MC 11294: farm legs are NOT death tests — the death gate refuses a dead
        // farmer its swings (pre-card this rode the D2 corpse-agency defect: the
        // battery leg was false-green ON A CORPSE). Revive ONLY when already
        // dead, through the same shipped model seam the F9 load uses.
        if (_director!.PlayerModel.IsDead) _director.PlayerModel.RestoreHealth(50);
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
                    // MC 11301 (DA F3): ungarded entry top-up REMOVED — a live farmer's
                    // health may never rise free; the farm keep-alive is BusKillLoop's
                    // MC 11294 revive-if-dead seam, not a cheat at phase entry.
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
                    _busBossBeforeExit = _director.BossActor;   // MC 11320 L1: the F-2 exit must drop THIS actor
                    BusNextPhase(11);
                }
                else
                {
                    BusPressTravel();
                    if (BusPhaseExpired(1200)) Fail("bus_emit: no live boss after the threshold (field wire broken)");
                }
                break;

            case 11:  // F-2 GUARD (planted-bad pair): leave the zone while the
                      // boss is ALIVE — ClearZoneEnemies drops it from the
                      // tracker; a flip detector would emit a FALSE BossFallen
                      // here (asserted in the phase-12 settle window). Must
                      // not. MC 11320 L1 re-cert: the ENTERED zone re-fields
                      // its own boss in the SAME travel tick when the DNA
                      // threshold is met, so the old `!HasLiveBoss` proxy is
                      // no longer the contract — the tracker must have DROPPED
                      // the exit-tracked actor (an empty tracker OR a
                      // re-fielded NEW actor both satisfy it; the planted
                      // still-tracked boss goes RED by name).
                if (BusPressTravel() && _director!.CurrentZone != _busZoneBeforeExit)
                {
                    Check("zone exit dropped the exit-tracked boss (a re-field in the entered zone is a NEW actor)",
                          !ReferenceEquals(_director.BossActor, _busBossBeforeExit),
                          $"zone={_director.CurrentZone} reFielded={_director.HasLiveBoss}");
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
                    // MC 11294 keep-alive (see BusKillLoop): a dead farmer cannot
                    // swing the boss down either — revive ONLY when already dead.
                    if (_director!.PlayerModel.IsDead) _director.PlayerModel.RestoreHealth(50);
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
                      // strike must hit IT); the farmer revives only if dead.
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
                    // MC 11301 (DA F3): was an ungarded per-frame full top-up — a LIVE farmer's
                    // health rose free every frame (alive-revive cheat class). Now the MC 11294
                    // farm idiom: revive ONLY when already dead.
                    if (_director.PlayerModel.IsDead) _director.PlayerModel.RestoreHealth(50);
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
                    _busFbSub = -1;   // MC 11320 L3: silence the roster settle FIRST (sub -1), then the five F-B steps
                    BusNextPhase(23);
                }
                break;

            case 23:  // MC 10103 F-B, sub -1 (MC 11320 L3, below) + five sub-steps.
                      // The poll samples at the FRAME BOUNDARY (F4), so the
                      // sequence must spread over ticks: -1 silence the roster
                      // settle (L3 — the clean window's "teleport + spawn-off"
                      // missed the betrayal-settle caller); 0 own the save
                      // (MC 3910 delete) + top up + step out of reach; 1 clean
                      // window -> TRUE decrease (stamps the lower HP, visible
                      // across a boundary); 2 true edge landed -> save +
                      // restore HP UP; 3 after >=2 polls saw the HIGH value,
                      // press load_game; 4 settle — the load lowers Health
                      // with NO PlayerHurt edge.
                if (_busFbSub == -1)
                {
                    // MC 11320 L3 (re-cert): the F-B clean window can be entered
                    // while a roster betrayal bond is still LIVE — the betrayal
                    // settle (TickRoster's C7 ExecuteBetrayal, PlayerController.
                    // TakeDamage's SECOND caller) ignores the teleport AND the
                    // spawn-off (it is wage mechanics, not a melee chase).
                    // Measured on this head WITHOUT the silence: the window
                    // check went FAIL in 5 of 7 runs (out/TIP-measure-run*.log;
                    // each stray PlayerHurt is named in-transcript behind its
                    // 'fired "betrayal"' SFX line). Silenced with the SHIPPED
                    // inputs — book open, break_bond per follower (cycle pages
                    // the selection), book close — a broken bond makes
                    // CheckBetrayal permanently false (HasCompanion), so no
                    // settle re-arms inside ANY F-B window. The break damage
                    // lands BEFORE the top-up and the baseline stamp below —
                    // the window contract itself is UNCHANGED, and this stays
                    // honest because the settle would ALSO corrupt the load
                    // window (sub 4): silence-at-source is the only complete
                    // fix, not a re-baseline.
                    int n = _director!.RosterView.Count;
                    if (_frames - _busPhaseStamp < 2) return;   // one press per 2 frames — the director polls the NEXT tick
                    _busPhaseStamp = _frames;
                    int step = _busSilenceStep++;
                    if (step == 0)
                        GD.Print($"LA_GATE: BUS_FB_QUIET_ROSTER n={n} — breaking live bonds before the clean window (L3 settle class)");
                    if (n == 0) { _busSilenceStep = 0; _busFbSub = 0; return; }   // empty roster: nothing to silence
                    if (step == 0) BusTap("book");                               // open the Empathy Book (poll is outside the death gate)
                    else if (step <= 2 * n - 1)
                    {
                        if (step % 2 == 1) BusTap("break_bond");                 // ExecuteBetrayal on the SELECTED follower
                        else BusTap("cycle_follower");                           // page to the next one
                    }
                    else if (step == 2 * n) BusTap("book");                      // close it again
                    if (step >= 2 * n + 1) { _busSilenceStep = 0; _busFbSub = 0; }
                    return;
                }
                if (_busFbSub == 0 && _frames - _busPhaseStamp >= 2)
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
                    // MC 10117: this PASS Quit bypasses the stage-6 hold, so it must
                    // carry the same release (fields are declared in the entry file;
                    // idiom reused from BridgeMvpProof/MC 10112).
                    ReleaseHeldRefsBeforeQuit();
                    Quit(0);
                }
                break;
        }
    }
}
