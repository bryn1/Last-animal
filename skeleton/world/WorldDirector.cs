// SIZE: inherited >400 (506 l at the MC 10167 W4 restamp; 501 after MC 3943 2g,
// 539 pre-2g — live companion loop left for Roster.cs; since 2g: +4 l S1 hunk
// a6704d0, +1 l W1 note 87ba446) — reasons per MC 3895 DA P2-1; block below.
using Godot;
using LastAnimal.Combat;
using LastAnimal.Companion;
using LastAnimal.Core;
using LastAnimal.Core.Framework;
using LastAnimal.Dna;
using LastAnimal.Ecosystem;
using LastAnimal.Empathy;
using LastAnimal.Npc;
using LastAnimal.Ui;
using System;
using System.Collections.Generic;

// Last Animal — W3-fix composition (MC 1123.10, artemis, 2026-09-08).
// T3b ownership refactor (MC 1256.9, artemis, 2026-09-21): THE composition root.
//
// WorldDirector: the scene's composition root + live-loop owner on main.tscn.
// Per the approved design (1256.2 §1.1/§2) this is the ONLY production site
// that constructs gameplay systems, and it binds every one of them into
// GameBootstrap so tests/proofs resolve the SAME instances (instance identity,
// not class identity). Single concern: own the authoritative runtime path —
//   - constructs ONE instance of each pure system (PlayerController,
//     CombatSystem, EcosystemSpawner, DnaLanguage, the companion stack),
//   - binds them into GameBootstrap (Bind<T>),
//   - spawns EnemyActor shells, each wrapping an EnemyAI the DIRECTOR
//     constructs and injects (EnemyActor no longer self-constructs),
//   - spawns CompanionFollowBody wrapping the machine-wired CompanionEntity,
//   - routes attacks through CombatSystem.DealDamage -> OnKill (the ONLY kill
//     path; OnKill's extraction feeds _spokenDna and the bus — no hardcoded
//     species string),
//   - applies the zone's SpawnSet on zone entry (spawns from it, not discards),
//   - owns the save/load actions via SaveLoadController (SaveSystem +
//     GodotSaveStore; MC 1344),
//   - builds + wires the HUD / DialogueSystem / EmpathyPanel on the UI
//     CanvasLayer (Bind + ConnectBus) so the C2 bus drives the readouts.
//
// It owns NO gameplay rules of its own — it wires the existing pure modules
// into the scene (I1). The two Set*Enabled hooks are one-line gate seams named
// in the design (§4.2); they carry no gameplay behavior.
//
// SIZE REASON (hygiene rule, 400 ceiling): design 1256.2 names this class THE
// single composition root — the ONLY production site that may construct and
// bind the gameplay systems. Splitting construction/binding across files would
// create a second composition site and break the instance-identity invariant
// the design rests on, so the file deliberately exceeds the ceiling as one unit.
// (The A14 MC 1348 PlayerActions/UI-build split remains the scheduled follow-up
// if the file keeps growing.)
namespace LastAnimal.World;

[GlobalClass]
public partial class WorldDirector : Node3D
{
    /// <summary>The Player shell (movement handled by Player.cs).</summary>
    [Export] public Node3D? Player { get; set; }

    /// <summary>The CanvasLayer into which the HUD/panels are placed.</summary>
    [Export] public CanvasLayer? UICanvas { get; set; }

    // --- director-owned systems (the ONE instance of each per running game) ---
    private PlayerController _player = null!;
    private CombatSystem _combat = null!;
    private EcosystemSpawner _ecosystem = null!;
    private DnaLanguage _dna = null!;
    private CompanionComponent _companionCore = null!;
    private CompanionNeeds _needs = null!;
    private CompanionStateMachine _companion = null!;
    private SalarySystem _salary = null!;
    private BetrayalSystem _betrayal = null!;

    // MC 3943 stage 2g: the live companion loop + its per-follower diffs moved
    // to world/WorldDirector.Roster.cs (TickRoster); the singleton body/loyalty/
    // state diff fields died with it (they are per-follower now).

    private readonly List<LanguageSignature> _spokenDna = new();
    private readonly List<EnemyActor> _enemies = new();
    private readonly List<EnemyActor> _zoneEnemies = new();
    // MC 3895: the zone-spawned enemy bodies (and their composed ActorVisual
    // children) live under one "Visuals" container per playable scene, so the
    // spawn-visual bridge has a named, enumerable home. Bodies, not detached
    // meshes: ONE visual-follows-sim mechanism (the body follows the AI), the
    // container is organization only — transforms identity, no gameplay move.
    private Node3D _visuals = null!;
    private string _zone = EcosystemSpawner.DefaultZone;
    private Vector3 _spawnOrigin;
    private EnemyActor? _boss;
    private BossPhaseState _bossPhase;

    // Gate seams (design §4.2): one-line bool guards, default true, no gameplay behavior.
    private bool _spawningEnabled = true;
    private bool _dnaForwarding = true;
    private bool _interactEnabled = true;

    private EventBus _bus = null!;
    private GameBootstrap _bootstrap = null!;
    private SaveLoadController _saveLoad = null!;

    private Hud _hud = null!;
    private DialogueSystem _dialogue = null!;
    private EmpathyPanel _empathy = null!;

    // Interact reach: must exceed the companion's FollowDistance (3.5) so the
    // trailing companion is talkable when the player stops.
    private const float TalkRange = 4.5f;

    // --- read-only surface the runtime proof reads (proof-only, no logic) ----
    public PlayerController PlayerModel => _player;
    public CombatSystem Combat => _combat;
    public IReadOnlyList<LanguageSignature> SpokenDna => _spokenDna;
    /// <summary>The bootstrap-visible machine: roster[0]'s while the roster has
    /// a follower (at boot this IS the bound instance — identity preserved);
    /// the boot machine otherwise (MC 3943 2g read-surface honesty).</summary>
    public CompanionStateMachine Companion =>
        _roster != null && _roster.Count > 0 ? _roster[0].Machine : _companion;
    public string CurrentZone => _zone;
    public int Progression => _saveLoad.Progression;
    public bool HasLiveBoss => _boss != null && !_boss.IsDead;
    public EnemyActor? BossActor => _boss;
    public int BossPhase => _bossPhase.Phase;
    public IReadOnlyList<EnemyActor> Enemies => _enemies;

    public override void _Ready()
    {
        // Resolve the composition references at runtime (robust on any load
        // path) rather than relying on .tscn exported NodePath binding.
        Player ??= GetNodeOrNull<Node3D>("Player");
        UICanvas ??= GetNodeOrNull<CanvasLayer>("UI");

        _bus = GetNode<EventBus>("/root/EventBus");
        _bootstrap = GetNode<GameBootstrap>("/root/GameBootstrap");

        // G-OWNERSHIP (design §4.5): a second composition root trying to
        // construct is a regression — fail loudly, never silently duplicate.
        if (_bootstrap.Has<PlayerController>())
        {
            GD.PushError("LA_GATE: DUPLICATE ROOT — PlayerController already bound; a second composition root is constructing systems");
        }

        // --- construct the ONE instance of each gameplay system --------------
        Vector3 origin = Player?.GlobalPosition ?? Vector3.Zero;
        _spawnOrigin = origin;
        _player = new PlayerController(new CombatVec3(origin.X, 0f, origin.Z));
        _combat = new CombatSystem();
        _ecosystem = new EcosystemSpawner(seed: 7);
        _dna = new DnaLanguage();
        _salary = new SalarySystem();
        _betrayal = new BetrayalSystem();
        _companionCore = new CompanionComponent { Id = 1 };
        _needs = new CompanionNeeds();
        _companionCore.SetCompanion(7);
        _companion = new CompanionStateMachine("companion", _companionCore, _needs, _salary, _betrayal);

        // --- bind into GameBootstrap: instance identity for tests/proofs -----
        _bootstrap.Bind(_player);
        _bootstrap.Bind(_combat);
        _bootstrap.Bind(_ecosystem);
        _bootstrap.Bind(_dna);
        _bootstrap.Bind(_companion);

        // --- the ONLY kill path: DealDamage -> OnKill -> DnaExtracted --------
        // The handler appends to _spokenDna (the ecosystem's input) and
        // forwards the REAL extraction to the bus (C10 -> M02 -> C2).
        _combat.DnaExtracted += OnDnaExtracted;
        _ecosystem.ZoneEntered += OnZoneEntered;

        BuildUi();
        _saveLoad = new SaveLoadController(
            _spokenDna, _companionCore, _hud,
            currentZone: () => _zone,
            enterZone: EnterZone,
            // MC 1348 N1: health rides the snapshot so F9 rescues a dead player.
            playerHealth: () => _player.Health,
            restoreHealth: h => _player.RestoreHealth(h),
            // MC 1405 N5: the player's world position rides the snapshot so F9
            // puts the player back where they stood (travel semantics).
            playerPosition: () =>
            {
                var p = Player?.GlobalPosition ?? Vector3.Zero;
                return new CombatVec3(p.X, p.Y, p.Z);
            },
            restorePosition: p =>
            {
                if (Player != null)
                    Player.GlobalPosition = new Vector3(p.X, p.Y, p.Z);
            });
        // MC 3904 stage 2c: story/quest root seam (world/WorldDirector.Story.cs
        // partial) — constructs the quest core, wires the bus hooks and the
        // QuestStates save seams. Runs before the boot EnterZone below so the
        // meadow entry feeds the intro quest.
        InitStory();
        // MC 3912 stage 2e: skill root seam (world/WorldDirector.Skills.cs
        // partial) — constructs the skill core, wires the v3 Manna save seams
        // and the kill-manna rider on the DnaExtracted forward below.
        InitSkills();
        // MC 3933 stage 2f: UI root seam (world/WorldDirector.Ui.cs partial)
        // — the TAB-toggle skills panel + live HUD readouts (Manna digits,
        // learned-skill names, ACTIVE quest line; plan §G D4, owner D4/D5).
        InitUi();
        // MC 1348 A5: the boot enemy set is the zone pipeline's job — the
        // EnterZone below fires OnZoneEntered -> ApplySpawnSet, which fields
        // the meadow SpawnSet into _zoneEnemies so zone travel can despawn it.
        // The old hard-coded SpawnEnemies() ring spawned BESIDE that set and
        // registered only in _enemies, so travel never despawned it and the
        // stale boot goblins patrolled the player's re-entry point forever.
        // MC 3895: the zone-visuals container must exist BEFORE the first
        // EnterZone below (it drives ApplySpawnSet).
        _visuals = new Node3D { Name = "Visuals" };
        AddChild(_visuals);

        InitRoster();   // MC 3943 2g: boot companion = roster[0] (Roster partial)
        EnterZone(_zone);
        GD.Print($"W3DBG: director ready (composition root) player={(Player != null)} uicanvas={(UICanvas != null)} enemies={_enemies.Count} origin={origin}");
    }

    public override void _Process(double delta)
    {
        if (Player == null) return;
        Vector3 ppos = Player.GlobalPosition;

        // Point each enemy's AI at the live player; collect incoming damage.
        int incoming = 0;
        foreach (var e in _enemies)
        {
            // MC 1348 A2: a dead actor's DamageDealt is frozen at its last value
            // (the death path stops the per-tick assignment) — never sum a corpse.
            if (e.IsDead) continue;
            e.PlayerTargetX = ppos.X;
            e.PlayerTargetZ = ppos.Z;
            incoming += e.DamageDealt;
        }

        if (incoming > 0)
        {
            _player.TakeDamage(incoming);
            _hud.UpdateLife(_player.Health);
        }

        // MC 3943 stage 2g: the companion loop (M03 -> M05) MOVED to
        // world/WorldDirector.Roster.cs as TickRoster — per follower over the
        // CompanionRoster (cap 3, owner D1): needs, machine, the pay/skip arms,
        // unique-key loyalty deltas THEN the roster-mean LAST under the
        // reserved "roster" key (D6), the C7 betrayal transition and the
        // bodies' Advance(). The roster-of-one is its minimal case — the MC
        // 1348 A3 settlement policy is unchanged.
        TickRoster(delta);

        if (Input.IsActionJustPressed("attack"))
            TryAttack();
        if (Input.IsActionJustPressed("interact"))
            TryInteract();
        if (Input.IsActionJustPressed("travel"))
            TravelToNextZone();
        if (Input.IsActionJustPressed("book"))
            ToggleEmpathyBook();
        if (Input.IsActionJustPressed("save_game"))
            _saveLoad.Save();
        // MC 10103 DA F-DA1 (P4, ratified): a same-frame load+damage suppresses that frame's PlayerHurt edge — the payload is unresolvable at frame granularity; the TickUi rebaseline owns it. Comment-only, zero behaviour.
        if (Input.IsActionJustPressed("load_game"))
            _saveLoad.Load();
        PollSkillActions();   // MC 3912 2e: skill_1/skill_2 arms (Skills partial)
        TickUi();             // MC 3933 2f: ui_toggle (TAB) handler + live HUD refresh (Ui partial)
    }

    private void BuildUi()
    {
        if (UICanvas == null) return;

        // Hud rides its own higher CanvasLayer layer (2) so the four gauge
        // readouts always draw clean on top, never overlapped by the dialogue
        // or empathy panels (which live on the base UI layer).
        var hudLayer = new CanvasLayer { Name = "HudLayer", Layer = 2 };
        _hud = new Hud { Name = "Hud" };
        _hud.Bind(100, 100, 0, 3);
        _hud.ConnectBus(_bus);
        hudLayer.AddChild(_hud);
        UICanvas.AddChild(hudLayer);

        // Dialogue + empathy panels: instanced (per the composition DoD) but
        // not force-opened on boot, so the full-width intro line does not
        // collide with the top-left HUD. They remain available to show later.
        // MC 3900 stage 2a: inject the authored story data table into the view.
        _dialogue = new DialogueSystem { Name = "Dialogue", Table = LastAnimal.Story.DialogueTable.Default() };
        UICanvas.AddChild(_dialogue);

        _empathy = new EmpathyPanel { Name = "Empathy" };
        _empathy.ConnectBus(_bus);
        UICanvas.AddChild(_empathy);
    }

    /// <summary>Apply a SpawnSet: spawn an EnemyActor per entry (the design's
    /// "SpawnSet APPLIED — enemies spawned from it, not discarded"). The set's
    /// scaled Health/Damage/Speed and IsBoss flag are passed INTO the spawned
    /// enemy (MC 1344 DA finding 4: they were discarded before, making the
    /// adaptation cosmetic).</summary>
    private void ApplySpawnSet(SpawnSet set)
    {
        if (!_spawningEnabled) return;
        Vector3 origin = Player?.GlobalPosition ?? Vector3.Zero;
        foreach (var spawned in set.Enemies)
        {
            var ai = new EnemyAI(
                new CombatVec3(origin.X + spawned.Position.X, 0f, origin.Z + spawned.Position.Z),
                spawned.EntityId, spawned.Type, seed: spawned.EntityId);
            // C15 live adaptation: the SpawnSet's scaled stats override the
            // per-type defaults; the boss's phase adds its hard-counter tier.
            int health = spawned.Health;
            int damage = spawned.Damage;
            if (spawned.IsBoss)
            {
                _bossPhase = BossController.Phase(BossController.Initial,
                    EcosystemAdaptation.ModelPlayerDna(_spokenDna));
                float mult = BossController.CounterMultiplier(_bossPhase.Phase);
                health = (int)Math.Round(health * mult);
                damage = (int)Math.Round(damage * mult);
            }
            ai.ApplySpawnStats(health, damage, spawned.Speed);
            var actor = new EnemyActor { Name = $"Spawned{spawned.EntityId}" };
            // MC 3895: the composed silhouette is derived from the spawned
            // type + IsBoss flag inside EnemyActor (ActorVisual.Build) — the
            // old flat per-spawn colour is gone; the body (with its "Visual"
            // child) hangs under the scene's Visuals container.
            actor.Configure(ai, origin.X + spawned.Position.X, origin.Z + spawned.Position.Z,
                spawned.IsBoss);
            _visuals.AddChild(actor);
            _enemies.Add(actor);
            _zoneEnemies.Add(actor);
            if (spawned.IsBoss) _boss = actor;
        }
    }

    /// <summary>Despawn the previous zone's SpawnSet enemies before re-fielding
    /// (zone travel and save-load re-entry must not stack duplicate sets).</summary>
    private void ClearZoneEnemies()
    {
        foreach (var e in _zoneEnemies)
        {
            _enemies.Remove(e);
            e.QueueFree();
        }
        _zoneEnemies.Clear();
        _boss = null;
    }

    // Zone travel (MC 1344 DA finding 3): canyon/ruins were unreachable —
    // EnterZone fired only once at boot. The travel action (input map, same
    // mechanism as attack/interact) cycles the ordered zone list, despawns the
    // previous zone's set, repositions the player at the zone entry and
    // re-enters (EnterZone -> OnZoneEntered -> ApplySpawnSet).
    private void TravelToNextZone()
    {
        if (Player == null) return;
        var zones = EcosystemSpawner.ZoneIds;
        int idx = System.Array.IndexOf(zones, _zone);
        string next = zones[(idx + 1) % zones.Length];
        ClearZoneEnemies();
        Player.GlobalPosition = _spawnOrigin;
        EnterZone(next);
        GD.Print($"W3: travel -> zone '{next}' (player repositioned, SpawnSet re-applied)");
    }

    // MC 3943 stage 2g: SpawnCompanion moved to world/WorldDirector.Roster.cs
    // (InitRoster spawns the boot body; recruited/restored followers add theirs).

    private void TryAttack()
    {
        if (Player == null) return;
        Vector3 ppos = Player.GlobalPosition;

        EnemyActor? best = null;
        float bestDist = _player.AttackRange;
        foreach (var e in _enemies)
        {
            if (e.IsDead) continue;
            float d = (e.GlobalPosition - ppos).Length();
            if (d <= bestDist) { best = e; bestDist = d; }
        }
        if (best == null) return;

        // The ONLY kill path (design §1.1): through the director-owned
        // CombatSystem. OnKill extracts the entity-id-seeded signature.
        // MC 3912 2e: the damage VALUE rides the armed-skill multiplier at
        // this single call site (kill-path grep leg stays exactly 1).
        _combat.DealDamage(attackerId: 0, best.Ai, ConsumeSkillDamage(_player.MeleeDamage));
        // MC 10120 Inc-3 S1: the ONLY hit site drives the presentation juice —
        // flash + punch ride the VISUAL node (transform + tint) only; the body
        // gets no impulse (RULING-1; F4: decay is integer frames in VisualJuice).
        ActorVisual.PlayHitFx(best.Visual, best.GlobalPosition - ppos);
        if (best.IsDead)
        {
            best.KillHide();
            GD.Print("W3: enemy killed via CombatSystem.DealDamage -> OnKill (HUD DNA meter +1)");
        }
    }

    // C4 speak half + C13 (MC 1344 DA findings): the production trigger for
    // DNA-speak and dialogue. Interact near a living NPC (the companion or an
    // enemy) -> DnaLanguage.Speak -> EventBus.DnaSpoken (C2) and the dialogue
    // box opens on that NPC's node. Same idiom as TryAttack: nearest target
    // within range, no second input mechanism.
    private void TryInteract()
    {
        if (Player == null || !_interactEnabled) return;
        Vector3 ppos = Player.GlobalPosition;

        Node3D? npc = null;
        int npcId = 0;
        float best = TalkRange;
        // MC 3943 2g: the nearest-FOLLOWER scan moved to the Roster partial —
        // it covers every follower body AND the wild creatures (interacting
        // with a wild one marks the standing recruit offer, plan §B 2g).
        TryRosterNpc(ppos, ref best, ref npc, ref npcId);
        foreach (var e in _enemies)
        {
            if (e.IsDead) continue;
            float d = (e.GlobalPosition - ppos).Length();
            if (d <= best) { best = d; npc = e; npcId = e.Ai.EntityId; }
        }
        if (npc == null) return;

        var sig = DnaLanguage.SignatureForEntity(npcId);
        var msg = _dna.Speak(sig, targetEntityId: 0);
        if (msg == null) return;
        _bus.EmitDnaSpoken(new DnaSignature(sig.SpeciesHash, msg.SourceEntityId.ToString()));
        _dialogue.Show($"npc_{npcId}");
        GD.Print($"W3: interact -> DnaLanguage.Speak (npc={npcId}) -> DnaSpoken + DialogueSystem.Show");
    }

    /// <summary>
    /// Open/close the Empathy Book (MC 1348 A4): the C13 surface becomes
    /// reachable in play. The book action opens the panel on the SELECTED
    /// follower's LIVE M04 entry (C9 Query — hidden state, summary, hint);
    /// pressing it again dismisses the panel. MC 3943 2g: selection pages
    /// with the cycle_follower action (Roster partial).
    /// </summary>
    private void ToggleEmpathyBook()
    {
        if (_empathy.Visible)
            _empathy.Close();
        else if (_roster != null && _roster.Count > 0)
            _empathy.Open(EmpathyBook.Query(_roster.Selected.Component));
        GD.Print($"W3: book action -> EmpathyPanel {( _empathy.Visible ? "opened (C9 Query)" : "closed")}");
    }

    // C10 -> M02 -> C2: an OnKill extraction appends to the spoken history and
    // forwards to the EventBus. This is the ONLY writer of _spokenDna. When a
    // live boss observes enough new history, BossController.Phase steps up and
    // the transition fires C2 EcosystemAdapted + re-fields a meaner SpawnSet.
    private void OnDnaExtracted(LanguageSignature signature)
    {
        _spokenDna.Add(signature);
        if (!_dnaForwarding) return;   // gate seam (design §4.2)
        _bus.EmitDnaExtracted(new DnaSignature(signature.SpeciesHash, signature.Id.ToString()));

        if (_boss == null || _boss.IsDead) return;
        var next = BossController.Phase(_bossPhase, EcosystemAdaptation.ModelPlayerDna(_spokenDna));
        if (!next.Changed) return;
        _bossPhase = next;
        _bus.EmitEcosystemAdapted(new MutationId($"boss_phase_{next.Phase}"));
        EnterZone(_zone);
        GD.Print($"W3: boss phase -> {next.Phase} (EcosystemAdapted emitted, SpawnSet re-applied)");
    }

    // C15: (re)populate the current zone's SpawnSet when the player enters it.
    // The previous zone's set is despawned first so re-entry never stacks.
    private void OnZoneEntered(string zoneId, SpawnSet set)
    {
        _zone = zoneId;
        _saveLoad.OnZoneEntered(zoneId);
        ClearZoneEnemies();
        ApplySpawnSet(set);
        GD.Print($"W3: zone '{zoneId}' SpawnSet applied (enemies={set.Count}, adaptation={set.AdaptationLevel:0.##})");
    }

    private void EnterZone(string zoneId)
    {
        var profile = EcosystemAdaptation.ModelPlayerDna(_spokenDna);
        _ecosystem.OnZoneEnter(zoneId, profile);
    }

    // ---- save/load (design §4.4): delegated to SaveLoadController (MC 1344) -

    /// <summary>Snapshot the live game to user://savegame.json (F5).</summary>
    public void SaveGame() => _saveLoad.Save();

    /// <summary>Restore the saved state and re-enter the saved zone (F9).</summary>
    public void LoadGame()
    {
        // MC 3943 2g: the delta reset moved to the Roster partial (per follower)
        // — a restore emits no phantom loyalty deltas.
        if (_saveLoad.Load())
            ResetRosterDeltas();
    }

    // ---- gate seams (design §4.2): one-line bool guards, no gameplay logic --

    /// <summary>Gate seam: disable enemy spawning (no_spawn negative control).</summary>
    public void SetSpawningEnabled(bool enabled) => _spawningEnabled = enabled;

    /// <summary>Gate seam: block the DnaExtracted bus forward (no_dna negative control).</summary>
    public void SetDnaForwarding(bool enabled) => _dnaForwarding = enabled;

    /// <summary>Gate seam: disable the interact/speak path (no_interact negative control).</summary>
    public void SetInteractEnabled(bool enabled) => _interactEnabled = enabled;
}
