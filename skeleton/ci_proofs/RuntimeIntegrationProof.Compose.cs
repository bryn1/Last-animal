using Godot;
using LastAnimal.Combat;
using LastAnimal.Companion;
using LastAnimal.Core;
using LastAnimal.Ui;
using LastAnimal.World;

// Last Animal — T3b runtime integration proof, compose routing (MC 1256.10;
// split MC 10218 W4 housekeeping).
//
// Partial-class half of RuntimeIntegrationProof carrying _ComposeDeferred —
// the per-mode compose routing (quiet-boot routes, stage-number assignment,
// the negative-control seam wiring) — moved VERBATIM out of the entry file,
// zero behaviour change: the mode contract, stage numbering, markers and
// negative controls are unchanged (the mode doc stays in the entry header,
// which mode_sets_check parses; its dispatch-arm walk covers this partial
// too). Extracted so the entry returns under the 600-line proof ceiling the
// W2/W3 mode arms grew past (814 l at 6ad4007, MC 10212 finding; split
// carded and PAID at MC 10218).
public partial class RuntimeIntegrationProof : SceneTree
{
    private void _ComposeDeferred()
    {
        var main = _main!;
        _bus = Root.GetNodeOrNull<EventBus>("/root/EventBus");
        if (_bus == null) { Fail("EventBus autoload not present even after scene _Ready"); return; }
        _bus.DnaExtracted += _ => _dnaCount++;
        _bus.DnaSpoken += _ => _dnaSpokenCount++;

        _director = main as WorldDirector;
        if (_director == null) { Fail("main.tscn root is not WorldDirector"); return; }

        _playerBody = main.GetNodeOrNull<CharacterBody3D>("Player");
        _hud = main.GetNodeOrNull<Hud>("UI/HudLayer/Hud");
        _dialogue = main.GetNodeOrNull<DialogueSystem>("UI/Dialogue");
        _companion = main.GetNodeOrNull<CompanionEntity>("Companion/Entity");
        // MC 1348 A5: the boot Enemy{i} ring is stood down — the director's
        // live Enemies set (the zone pipeline's SpawnSet actors) is the enemy
        // population every mode drives.
        _enemies.AddRange(_director!.Enemies);

        if (_playerBody == null) { Fail("Player node not found in main.tscn"); return; }
        if (_hud == null) { Fail("Hud not found at UI/HudLayer/Hud (WorldDirector UI not built)"); return; }
        if (_companion == null) { Fail("CompanionEntity not found at Main/Companion/Entity (director must spawn the machine-wired entity)"); return; }
        if (_mode is "dna_speak" or "no_interact" && _dialogue == null)
        { Fail("DialogueSystem not found at UI/Dialogue (WorldDirector UI not built)"); return; }

        if (_mode == "no_spawn")
        {
            // The director must have spawned nothing; the chain cannot start.
            // Give the scene one frame to populate, then assert emptiness.
            _stage = 90;
            return;
        }

        // bus_emit (MC 10098 S0): routes BEFORE the live-enemy guard — this
        // mode boots with spawning off (quiet scripted legs) and re-arms it
        // in its farm phase; the stage body lives in Bus.cs.
        if (_mode == "bus_emit")
        {
            _stage = 80;
            _stageFrames = 0;
            GD.Print($"LA_GATE: composed (bus mode) — enemies={_enemies.Count} (spawning off at boot)");
            return;
        }

        // JUICE_SHAKE (MC 10121 S2): the shake stage drives its own scripted
        // edges (TakeDamage + a consumer-test BossFallen emit); routes BEFORE
        // the live-enemy guard — this mode boots with spawning off (quiet
        // legs, bus_emit idiom). Stage body lives in Shake.cs.
        if (_mode == "JUICE_SHAKE")
        {
            _stage = 96;
            _stageFrames = 0;
            GD.Print($"LA_GATE: composed (shake mode) — enemies={_enemies.Count} (spawning off at boot)");
            return;
        }

        // CAMERA_KILL_PULSE (MC 10217 S20): the pulse stage drives its own
        // scripted edge (a direct DnaExtracted emit, the consumer-test idiom);
        // routes BEFORE the live-enemy guard — quiet boot (bus_emit idiom).
        // Stage body lives in RuntimeIntegrationProof.KillPulse.cs.
        if (_mode == "CAMERA_KILL_PULSE")
        {
            _stage = 100;
            _stageFrames = 0;
            GD.Print($"LA_GATE: composed (kill-pulse mode) — enemies={_enemies.Count} (spawning off at boot)");
            return;
        }

        // DAYNIGHT_STATE (MC 10199 S15): routes BEFORE the live-enemy guard —
        // quiet boot (bus_emit idiom); compose + stage live in the partial
        // RuntimeIntegrationProof.DayNight.cs (600-ceiling hygiene: the mode
        // arms stay one-liners in the multi-mode harness).
        if (_mode == "DAYNIGHT_STATE")
        {
            DayNightCompose(main);
            return;
        }

        // BARK (MC 10131 S8): routes BEFORE the live-enemy guard — quiet boot
        // (bus_emit idiom). Stage body lives in RuntimeIntegrationProof.Bark.cs
        // (600-ceiling hygiene: the mode arms stay one-liners in the multi-mode
        // harness).
        if (_mode == "BARK")
        {
            _stage = 102;
            _stageFrames = 0;
            GD.Print($"LA_GATE: composed (bark mode) — roster={_director.RosterView.Count} (spawning off at boot)");
            return;
        }

        if (_enemies.Count == 0) { Fail("WorldDirector spawned no EnemyActor"); return; }

        _dnaBefore = _hud.DnaMeter;

        // no_bus: disconnect the HUD from the bus before the kill so the
        // DnaExtracted emit cannot move the meter.
        if (_mode == "no_bus") _hud.DisconnectBus(_bus);

        // no_controller: swap the director's authoritative PlayerController
        // binding for a decoy; the AI target must stop matching the body.
        if (_mode == "no_controller")
        {
            var bootstrap = Root.GetNodeOrNull<GameBootstrap>("/root/GameBootstrap");
            if (bootstrap == null) { Fail("GameBootstrap autoload absent"); return; }
            bootstrap.Bind(new PlayerController(new CombatVec3(999f, 0f, 999f)));
        }

        // no_dna: block the director's DnaExtracted forwarding so the kill
        // happens but the bus never hears it.
        if (_mode == "no_dna") _director.SetDnaForwarding(false);

        // no_interact: disable the director's interact/speak seam so interact
        // is pressed but neither DnaSpoken nor the dialogue may fire.
        if (_mode == "no_interact") _director.SetInteractEnabled(false);

        // quest_arc / quest_persist / quest_neg (MC 3904 2c): the quest chain
        // drives its own input (teleport + interact/pay/attack/travel); skip
        // the movement press and route to stage 50 — stage bodies live in
        // RuntimeIntegrationProof.Quests.cs.
        if (_mode is "quest_arc" or "quest_persist" or "quest_neg")
        {
            _stage = 50;
            _stageFrames = 0;
            GD.Print($"LA_GATE: composed (quest mode) — enemies={_enemies.Count} intro={_director.Quests.Status("q_intro")}");
            return;
        }

        // quest_arc2 (MC 10132 S10): the ACT-TWO chain drives its own input
        // (speak, owned save/load, travel, pay, farm); route to stage 55 —
        // NOT a 9x number: the S3 sibling takes a 9x stage on its branch,
        // the quest-family slot 55 (between 50 and 60) merges collision-free.
        if (_mode == "quest_arc2")
        {
            _stage = 55;
            _stageFrames = 0;
            GD.Print($"LA_GATE: composed (act-two mode) — enemies={_enemies.Count} intro={_director.Quests.Status("q_intro")}");
            return;
        }

        // skill_use / skill_neg (MC 3912 2e): the skill chain drives its own
        // input (farm + measured hits + skill presses); skip the movement
        // press and route to stage 60 — stage bodies live in
        // RuntimeIntegrationProof.Skills.cs.
        if (_mode is "skill_use" or "skill_neg")
        {
            _stage = 60;
            _stageFrames = 0;
            GD.Print($"LA_GATE: composed (skill mode) — enemies={_enemies.Count} manna={_director.PlayerModel.Manna}");
            return;
        }

        // passives (MC 10200 S16): the resonance-passive chain drives its own
        // input (REAL extracts + one mend press); route to stage 65 — NOT a
        // 9x number: the skill-family slot between 60 and 70 (quest_arc2's
        // 55 precedent), merging collision-free with the sibling W2 legs.
        // Stage body lives in RuntimeIntegrationProof.Passives.cs.
        if (_mode == "passives")
        {
            _stage = 65;
            _stageFrames = 0;
            GD.Print($"LA_GATE: composed (passives mode) — enemies={_enemies.Count} manna={_director.PlayerModel.Manna}");
            return;
        }

        // calm_use / calm_neg (MC 10031): the calming-speak chain drives its
        // own input (farm + calm casts + pay/interact/save/load presses);
        // skip the movement press and route to stage 70 — stage bodies live
        // in RuntimeIntegrationProof.CalmingSpeak.cs.
        if (_mode is "calm_use" or "calm_neg")
        {
            _stage = 70;
            _stageFrames = 0;
            GD.Print($"LA_GATE: composed (calm mode) — enemies={_enemies.Count} manna={_director.PlayerModel.Manna}");
            return;
        }

        // JUICE_HITFLASH (MC 10120 S1): the juice stage drives its own single
        // hit (frozen target, one attack press on the real wire); route to
        // stage 95 — stage body lives in RuntimeIntegrationProof.Juice.cs.
        if (_mode == "JUICE_HITFLASH")
        {
            _stage = 95;
            _stageFrames = 0;
            GD.Print($"LA_GATE: composed (juice mode) — enemies={_enemies.Count}");
            return;
        }

        // DISSOLVE_SUPPRESS (MC 10129 S3): the leg kills a live enemy through
        // the REAL wire and asserts the death-tick suppression + 20f dissolve.
        // It NEEDS the live spawn set (quiet-boot modes have no corpse to
        // make), so it routes after the enemy guard — stage body lives in
        // RuntimeIntegrationProof.Dissolve.cs.
        if (_mode == "DISSOLVE_SUPPRESS")
        {
            _stage = 97;
            _stageFrames = 0;
            GD.Print($"LA_GATE: composed (dissolve mode) — enemies={_enemies.Count}");
            return;
        }

        // CHAR_MOTION (MC 10198 S14): the motion leg NEEDS the live meadow
        // spawn set (chase walk witness + the real damage site) — routes after
        // the enemy guard; the leg culls it to one goblin mid-way. Stage body
        // lives in RuntimeIntegrationProof.Motion.cs.
        if (_mode == "CHAR_MOTION")
        {
            _stage = 99;
            _stageFrames = 0;
            GD.Print($"LA_GATE: composed (motion mode) — enemies={_enemies.Count}");
            return;
        }

        // BOSS_FRAME (MC 10217 S20): the boss-framing leg NEEDS the live spawn
        // set (KillLoop farm to the BossThreshold + a REAL-wire boss kill), so
        // it routes after the enemy guard like DISSOLVE/CHAR_MOTION; long leg
        // — rides the QuestFrameBudget list below. Stage body lives in
        // RuntimeIntegrationProof.BossFrame.cs.
        if (_mode == "BOSS_FRAME")
        {
            _stage = 101;
            _stageFrames = 0;
            GD.Print($"LA_GATE: composed (boss-frame mode) — enemies={_enemies.Count}");
            return;
        }

        // ZONE4 (MC 10216 S18): the zone-four leg boots the LIVE meadow set and
        // TRAVELS (the seam is the product under test — quiet-boot modes cannot
        // prove a travel cycle); routes after the enemy guard like DISSOLVE/
        // CHAR_MOTION. The zone-entry baseline rides _z4SpawnOrigin captured at
        // compose. Stage body lives in RuntimeIntegrationProof.Zone4.cs.
        if (_mode == "ZONE4")
        {
            _z4SpawnOrigin = _playerBody!.GlobalPosition;
            _stage = 100;
            _stageFrames = 0;
            GD.Print($"LA_GATE: composed (zone4 mode) — enemies={_enemies.Count} zone={_director.CurrentZone}");
            return;
        }

        // Capture the movement baseline BEFORE pressing the input (MC 1344.1):
        // stage 0 ran after the press, by which time the player had already moved.
        _playerStart = _playerBody.GlobalPosition;
        _pressPhysFrame = _physFrames;

        Input.ActionPress("move_right");
        GD.Print($"LA_GATE: composed — Player + {_enemies.Count} enemies + Hud + CompanionEntity (dnaBefore={_dnaBefore})");
    }
}
