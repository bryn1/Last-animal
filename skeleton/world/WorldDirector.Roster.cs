// SIZE: 331 l — roster-core half of the stage 2g roster wiring (MC 10080 split:
// the WILD-side members moved VERBATIM to WorldDirector.Roster.Wild.cs — pure
// move, no logic edits). What lives HERE: the roster/bodies/wild state and its
// read-only surfaces, Init/Tick, the cycle/break polls, the visible-body spawns
// and the v3 Followers save seams. (The pre-split SIZE header understated this
// file by 45 lines and was never true; that lie dies here — MC 3895 idiom.)
using Godot;
using LastAnimal.Companion;
using LastAnimal.Core.Framework;
using LastAnimal.Empathy;
using LastAnimal.Npc;
using LastAnimal.Save;
using System.Collections.Generic;

// Last Animal — MC 3943 stage 2g follower roster (code, 2026-10-02).
//
// Partial-class half of WorldDirector (one class = still THE single
// composition root, plan §B). This file OWNS the live companion loop that the
// root used to run inline for a single companion (now per FOLLOWER over the
// CompanionRoster: a roster-of-one is its minimal case, and the root gets
// SMALLER — the 400-line rule is per-file).
//
// Mechanics pinned here (plan §B 2g + §G D6, rulings cited inline):
//   * recruit = interact-with-wild + FIRST wage via the EXISTING pay_wage —
//     no new poller, no new input; cap 3 (owner D1 verbatim "All rec"), the
//     4th join REFUSED;
//   * emit-order contract (D6): unique per-follower loyalty keys
//     "<name>-<EntityId>" (DA-c2 D6) FIRST in roster order, roster-mean LAST
//     under the reserved "roster" key — the Hud last-value redraw paints the
//     mean with NO Hud edit; quest predicates ignore the reserved key (F2);
//   * I4 glue lives HERE only: book-open pay_wage = Forgive, break_bond =
//     ExecuteBetrayal/BreakCompanion on the SELECTED follower; the panel keeps
//     Open(BookEntry) and RouteResolution stays display-only;
//   * save/load: the v3 Followers list fills N and a load RESTORES N (rebuild:
//     unbonded stacks leave, stacks reuse by index, extras spawn, surplus
//     drops — the save is the full snapshot, the QuestLog.FromSaveRows idiom).
namespace LastAnimal.World;

public partial class WorldDirector
{
    private CompanionRoster _roster = null!;
    // Visible bodies, index-aligned with roster.Followers (bodies are world-side
    // presentation; the roster itself stays engine-free, I3).
    private readonly List<CompanionFollowBody> _bodies = new();
    private readonly List<CompanionFollowBody> _wild = new();
    private readonly List<CompanionRoster.LoyaltyEmit> _loyaltyEmits = new();
    // Wild bond ids are allocated deterministically per run (the boot follower
    // keeps its authored 7; 21+ is the wild pool — distinct, so bus keys never
    // collide with the boot companion's).
    private int _nextWildEntityId = 21;

    // --- read-only surfaces the roster_follow runtime proof reads (no logic) --
    public CompanionRoster RosterView => _roster;
    public IReadOnlyList<CompanionFollowBody> FollowerBodies => _bodies;
    public IReadOnlyList<CompanionFollowBody> WildBodies => _wild;
    public bool AnyWageDue => _roster != null && _roster.AnyWageDue;

    /// <summary>
    /// Root seam, called from _Ready where SpawnCompanion() used to stand: the
    /// boot companion becomes roster[0] (the SAME objects GameBootstrap already
    /// bound — roster[0] stays bootstrap-visible), its visible body is spawned,
    /// and the Followers save seams are injected into SaveLoadController.
    /// </summary>
    private void InitRoster()
    {
        _roster = new CompanionRoster(_betrayal);
        var boot = new CompanionRoster.Follower("companion", _companionCore, _needs, _companion);
        _roster.TryAdd(boot);   // boot seed: the roster is empty, cap cannot bite
        SpawnCompanion();
        _saveLoad.FollowersWrite = SnapshotFollowers;
        _saveLoad.FollowersRestore = RestoreFollowers;
    }

    /// <summary>The live roster loop (moved verbatim-per-follower from the root's
    /// _Process companion block, plan §B 2g): needs, machine, the pay/skip arms,
    /// the loyalty emits (deltas then the roster-mean LAST), the C7 betrayal
    /// transition, and the visible bodies' Advance().</summary>
    private void TickRoster(double delta)
    {
        Vector3 ppos = Player!.GlobalPosition;
        bool payPressed = Input.IsActionJustPressed("pay_wage");
        bool bookOpen = _empathy != null && _empathy.Visible;
        bool recruitAteThePress = false;

        if (payPressed && bookOpen)
        {
            // I4 glue (plan §B 2g): pay_wage while the Empathy Book is OPEN is
            // Forgive — the loyalty bonus on the SELECTED follower. The panel
            // itself never mutates; no wage settles under the book. DA W5 F1:
            // the roster REFUSES the bonus on a broken bond — no zombie affection.
            var forgiven = _roster.Selected;
            if (_roster.Forgive(forgiven))
                GD.Print($"ROSTER: Forgive -> {forgiven.BusKey} (+{CompanionRoster.ForgiveBonus} loyalty)");
            else
                GD.Print($"ROSTER: Forgive refused on {forgiven.BusKey} — bond broken (DA W5 F1)");
        }
        else if (payPressed)
        {
            // Recruit arm: the pay press pays the FIRST wage of an offered wild
            // creature if one is in reach (the existing pay_wage action — no
            // second wage path, no new poller).
            recruitAteThePress = TryRecruitOfferedWild(ppos);
        }

        for (int i = 0; i < _roster.Count; i++)
        {
            var f = _roster[i];
            f.Needs.TickAccompaniment(delta);
            var state = f.Machine.Tick();

            if (state != CompanionState.Betrayed)
            {
                // The skip arm runs exactly when THIS follower's pay arm did
                // not (MC 1348 A3 else-if semantics, per follower). The pay
                // itself lands below via the roster's own PayDueFollowers.
                bool payArm = f.Needs.SalaryDue && payPressed
                              && !recruitAteThePress && !bookOpen;
                if (!payArm && f.Needs.ConsumeUnpaidInterval())
                    f.Machine.SkipPayment();
            }

            // C7 precondition path per follower (MC 1348 A3): the machine
            // mirrored this follower's CheckBetrayal — execute ONCE on the
            // transition, under its UNIQUE bus key (key read BEFORE the break,
            // ExecuteBetrayal flips the bond id).
            if (state == CompanionState.Betrayed && f.StateLast != CompanionState.Betrayed)
            {
                string key = f.BusKey;
                BetrayalResult? result = _roster.ExecuteBetrayal(f);
                if (result != null)
                {
                    _player.TakeDamage(result.DamageDealt);
                    _hud.UpdateLife(_player.Health);
                }
                _bus.EmitBetrayal(new CompanionId(key), new TargetId("player"));
            }
            f.StateLast = state;
        }

        // Pay arm (ARCH W2, was the singleton read in the root): ONE pay press
        // settles EVERY due follower through the roster's own PayDueFollowers,
        // and EACH landed settle emits its own WagePaid.
        if (payPressed && !recruitAteThePress && !bookOpen)
            foreach (var settled in _roster.PayDueFollowers())
                NotifyWageSettled();

        // The visible bodies tick the SAME machines they are wired to (the
        // integration fix, per follower).
        for (int i = 0; i < _bodies.Count && i < _roster.Count; i++)
            _bodies[i].Entity.Advance();

        // Emit-order contract (D6): per-follower deltas in roster order, then
        // the roster-mean LAST under the reserved "roster" key.
        if (_roster.CollectLoyaltyEmits(_loyaltyEmits))
            foreach (var e in _loyaltyEmits)
                _bus.EmitLoyaltyChanged(new CompanionId(e.Key), e.Loyalty);

        // Empathy-Book paging + confirm arms (plan §B 2g new input actions).
        if (Input.IsActionJustPressed("cycle_follower"))
            CycleSelectedFollower();
        if (Input.IsActionJustPressed("break_bond") && _empathy != null && _empathy.Visible)
            BreakSelectedBond();
    }

    /// <summary>cycle_follower: page the selection; repaint the book on the
    /// newly selected entry while it is open (Open keeps its signature, I4).</summary>
    private void CycleSelectedFollower()
    {
        if (_roster.Count == 0) return;
        _roster.CycleSelected();
        var sel = _roster.Selected;
        if (_empathy != null && _empathy.Visible)
            _empathy.Open(EmpathyBook.Query(sel.Component));
        GD.Print($"ROSTER: cycle_follower -> selected {sel.BusKey}");
    }

    /// <summary>break_bond (book open): PermanentBreak CONFIRMED on the SELECTED
    /// follower only — ExecuteBetrayal (bond broken + betrayal damage + C2
    /// Betrayal under the unique key); already-broken bonds stay silent.</summary>
    private void BreakSelectedBond()
    {
        if (_roster.Count == 0) return;
        var f = _roster.Selected;
        string key = f.BusKey;
        BetrayalResult? res = _roster.ExecuteBetrayal(f);
        if (res == null)
        {
            f.Component.BreakCompanion();
            GD.Print($"ROSTER: break_bond on {key}: no live bond (already broken) — nothing to execute");
            return;
        }
        _player.TakeDamage(res.DamageDealt);
        _hud.UpdateLife(_player.Health);
        _bus.EmitBetrayal(new CompanionId(key), new TargetId("player"));
        GD.Print($"ROSTER: break_bond -> betrayal executed on {key} (damage {res.DamageDealt})");
    }

    // --- visible bodies ------------------------------------------------------

    /// <summary>Spawn the BOOT follower's visible body (moved from the root file;
    /// MC 1348 lineage). Node name stays "Companion" — the runtime proof's
    /// machine-wired path Contract Companion/Entity depends on it.</summary>
    private void SpawnCompanion()
    {
        var hook = new CompanionAnimationHook("walkBaked", "walkBaked", "walkBaked");
        var body = new CompanionFollowBody(_companion, hook)
        {
            Name = "Companion",
            Target = Player,
            Y = 0.55f,
            EntityId = _companionCore.CompanionEntityId,
            BoundFollower = _roster[0],
        };
        AddChild(body);
        _bodies.Add(body);
    }

    /// <summary>A restored follower: fresh stack (defaults + the saved bond id
    /// and loyalty) plus its visible body near the player.</summary>
    private void SpawnRestoredFollower(FollowerEntry entry, int index)
    {
        var comp = new LastAnimal.Npc.CompanionComponent { Id = entry.EntityId, Loyalty = entry.Loyalty };
        comp.CompanionEntityId = entry.EntityId;
        var needs = new CompanionNeeds();
        var machine = new CompanionStateMachine(index == 0 ? "companion" : "follower",
            comp, needs, _salary, _betrayal);
        var follower = new CompanionRoster.Follower(index == 0 ? "companion" : "follower",
            comp, needs, machine);
        if (!_roster.TryAdd(follower))
        {
            // DA W5 F4: oversized hand-edited save — the roster cap is
            // AUTHORITATIVE on load: the surplus entry drops WITH A MARKER
            // and no frozen orphan body is ever composed for it.
            GD.Print($"ROSTER: restored entry id {entry.EntityId} DROPPED — roster cap {CompanionRoster.Cap} is authoritative on load (DA W5 F4)");
            return;
        }

        var hook = new CompanionAnimationHook("walkBaked", "walkBaked", "walkBaked");
        var body = new CompanionFollowBody(machine, hook)
        {
            Name = $"Companion{entry.EntityId}",
            Target = Player,
            Y = 0.55f,
            EntityId = entry.EntityId,
            BoundFollower = follower,
        };
        AddChild(body);
        _bodies.Add(body);
    }

    private void RemoveFollowerAt(int index)
    {
        var body = _bodies[index];
        _bodies.RemoveAt(index);
        _roster.RemoveAt(index);
        body.QueueFree();
    }

    // --- save/load roster seams (v3 Followers list — 2g fills N, plan §B 2g) --

    /// <summary>Snapshot: every BONDED follower, roster order, its own bond id +
    /// loyalty (the -1 sentinel never appears — same rule as the roster-of-one).</summary>
    private List<FollowerEntry> SnapshotFollowers()
    {
        var entries = new List<FollowerEntry>();
        for (int i = 0; i < _roster.Count; i++)
        {
            var c = _roster[i].Component;
            if (!c.HasCompanion) continue;   // betrayed followers are not a phantom entry
            entries.Add(new FollowerEntry { EntityId = c.CompanionEntityId, Loyalty = c.Loyalty });
        }
        return entries;
    }

    /// <summary>Load RESTORES N (LOAD_RESTORED extended): the saved roster is
    /// the full snapshot — unbonded stacks leave, saved entries apply onto the
    /// stacks by index (stack objects, and GameBootstrap's roster[0] binding,
    /// stay identity-stable), entries beyond the live stacks spawn fresh, the
    /// surplus drops. Loyalty/state diffs reset: no phantom emits after a
    /// restore (the moved LoadGame delta-reset idiom).</summary>
    private void RestoreFollowers(List<FollowerEntry> entries)
    {
        for (int i = _roster.Count - 1; i >= 0; i--)
            if (!_roster[i].Component.HasCompanion)
                RemoveFollowerAt(i);

        for (int i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            if (i < _roster.Count)
            {
                var f = _roster[i];
                f.Component.CompanionEntityId = e.EntityId;
                f.Component.Loyalty = e.Loyalty;
                _bodies[i].EntityId = e.EntityId;
            }
            else
            {
                SpawnRestoredFollower(e, i);
            }
        }
        while (_roster.Count > entries.Count)
            RemoveFollowerAt(_roster.Count - 1);

        // DA W5 F3: seed the wild-id allocator past max(restored bond ids, 20)
        // — a fresh-run wild must never allocate an id a restored follower
        // already holds (shared BusKey + shared interact id, latent collision).
        for (int i = 0; i < _roster.Count; i++)
        {
            int bid = _roster[i].Component.CompanionEntityId;
            if (bid >= _nextWildEntityId) _nextWildEntityId = bid + 1;
        }

        ResetRosterDeltas();
        GD.Print($"ROSTER: load restored N={_roster.Count}");
        ClearCalmWindows();   // §3 load seam (ONE caller, R9 mirror): calm state is runtime-only
    }

    /// <summary>Reset every per-follower loyalty/state diff (root LoadGame seam +
    /// the load restore path): a restore is not a loyalty-delta game beat.</summary>
    private void ResetRosterDeltas()
    {
        if (_roster == null) return;
        for (int i = 0; i < _roster.Count; i++)
        {
            var f = _roster[i];
            f.LoyaltyLast = f.Component.Loyalty;
            f.StateLast = f.Machine.State;
        }
    }
}
