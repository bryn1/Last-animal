// SIZE: 422 l — crossed the 400 ceiling ONLY through the DA W5 F1/F3/F4 fix
// guards (forgive-refusal caller arm, restore-cap drop marker, allocator
// seed); the root WorldDirector.cs must NOT grow, so the guards live HERE.
// Reason-per-MC 3895 idiom; split is owed before any further growth here.
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

    // --- offer helpers (MC 10031, design §3): the ONE pair of mutation points
    // for RecruitOffered. The flag keeps exactly two writers through these two
    // helpers (interact-E + the calm cast open; expiry, the recruit join and
    // the load seam close — the load seam clears THROUGH CloseRecruitOffer).

    /// <summary>Open (or refresh) the standing recruit offer: prints the
    /// OFFERED line ONLY on the false→true flip and RETIRES any calm window —
    /// the standing offer is time-unbounded (E's semantics, unchanged; the
    /// skill's window can never downgrade it). Replaces the inline offer
    /// block the interact scan used to carry (§3).</summary>
    private void OpenRecruitOffer(CompanionFollowBody w)
    {
        if (!w.RecruitOffered)
            GD.Print($"ROSTER: recruit OFFERED to wild id {w.EntityId} — pay the first wage (pay_wage)");
        w.RecruitOffered = true;
        if (w.CalmedWindowFrames > 0)
        {
            w.CalmedWindowFrames = 0;
            GD.Print($"ROSTER: calm window RETIRED on wild id {w.EntityId} — the standing offer outlives it");
        }
    }

    /// <summary>Close the offer AND zero the window (belt, DA-c1 P4-2: a
    /// recruited follower must never carry a stale window). Used by the calm
    /// expiry path, the recruit join (replaces the bare flag clear) and the
    /// load seam's ClearCalmWindows (§3).</summary>
    private void CloseRecruitOffer(CompanionFollowBody w)
    {
        if (!w.RecruitOffered && w.CalmedWindowFrames == 0) return;
        w.RecruitOffered = false;
        w.CalmedWindowFrames = 0;
        GD.Print($"ROSTER: recruit offer CLEARED on wild id {w.EntityId}");
    }

    /// <summary>Recruit arm (pay_wage): the nearest OFFERED wild creature inside
    /// talk range joins with its FIRST wage paid by this press. Cap 3 (D1): the
    /// 4th join is REFUSED, the roster unchanged, the offer stands. Returns true
    /// only when the press was consumed by a real recruit.</summary>
    private bool TryRecruitOfferedWild(Vector3 ppos)
    {
        CompanionFollowBody? wild = null;
        float best = TalkRange;
        foreach (var w in _wild)
        {
            if (!w.RecruitOffered || w.BoundFollower == null) continue;
            float d = (w.GlobalPosition - ppos).Length();
            if (d <= best) { best = d; wild = w; }
        }
        if (wild == null) return false;

        if (_roster.Count >= CompanionRoster.Cap)
        {
            GD.Print($"ROSTER: recruit of wild id {wild.EntityId} REFUSED — cap {CompanionRoster.Cap} reached (owner ruling D1)");
            return false;
        }
        if (wild.BoundFollower == null || !_roster.TryAdd(wild.BoundFollower))
            return false;   // belt: TryAdd owns the cap

        _wild.Remove(wild);
        wild.Wild = false;
        CloseRecruitOffer(wild);   // §3(c): flag + window belt (the join may not carry a stale window)
        wild.Target = Player;
        wild.Name = $"Companion{wild.EntityId}";
        _bodies.Add(wild);
        GD.Print($"ROSTER: wild id {wild.EntityId} RECRUITED (first wage paid via pay_wage) — followers={_roster.Count}/{CompanionRoster.Cap}");
        return true;
    }

    /// <summary>TEST SEAM (design §4.2 idiom): spawn a WILD creature near the
    /// player — interact offers, pay_wage recruits. Content wiring (wild spawn
    /// from the ecosystem table) is a later card; the roster mechanic is proven
    /// through this seam exactly as a player drives it (in-play input).</summary>
    public void SpawnWildFollower()
    {
        int eid = _nextWildEntityId++;
        var comp = new LastAnimal.Npc.CompanionComponent { Id = eid };
        comp.SetCompanion(eid);
        var needs = new CompanionNeeds();
        var machine = new CompanionStateMachine("follower", comp, needs, _salary, _betrayal);
        var follower = new CompanionRoster.Follower("follower", comp, needs, machine);

        var hook = new CompanionAnimationHook("walkBaked", "walkBaked", "walkBaked");
        var body = new CompanionFollowBody(machine, hook)
        {
            Name = $"Wild{eid}",
            Y = 0.55f,                       // Target stays null: a wild body stands its ground
            Wild = true,
            EntityId = eid,
            BoundFollower = follower,
        };
        Vector3 p = Player?.GlobalPosition ?? Vector3.Zero;
        body.GlobalPosition = p + new Vector3(2.5f, 0f, 0f);
        AddChild(body);
        _wild.Add(body);
        GD.Print($"ROSTER: wild creature spawned id={eid} (interact to offer, pay_wage to recruit)");
    }

    /// <summary>Root seam (TryInteract): the nearest NPC among the FOLLOWER
    /// bodies and the WILD bodies (replaces the old single-companion scan).
    /// Following an interact, the WILD creature selected becomes the standing
    /// recruit offer (plan §B 2g: recruitment = interact + first wage).</summary>
    private void TryRosterNpc(Vector3 ppos, ref float best, ref Node3D? npc, ref int npcId)
    {
        for (int i = 0; i < _roster.Count && i < _bodies.Count; i++)
        {
            var body = _bodies[i];
            float d = (body.GlobalPosition - ppos).Length();
            if (d <= best)
            {
                best = d; npc = body;
                npcId = _roster[i].Component.CompanionEntityId;   // the live bond id speaks
            }
        }
        foreach (var w in _wild)
        {
            float d = (w.GlobalPosition - ppos).Length();
            if (d <= best)
            {
                best = d; npc = w; npcId = w.EntityId;
                OpenRecruitOffer(w);   // §3: replaces the inline offer block (helper = the ONE opener)
            }
        }
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

    /// <summary>MC 10031 R9 mirror (design §1.1): a save/load must not roll the
    /// Manna economy back under a live calm window (save-scum repeatably-FREE
    /// calming). Every _wild body with an open window is closed THROUGH
    /// CloseRecruitOffer; E-standing offers (window 0) are untouched — by this
    /// seam and by the decay sweep alike. Called ONLY at the end of
    /// RestoreFollowers, so BOTH load entries (the player load_game key AND
    /// proof-only LoadGame) clear through it (DA-c2 F-1).</summary>
    private void ClearCalmWindows()
    {
        foreach (var w in _wild)
            if (w.CalmedWindowFrames > 0)
                CloseRecruitOffer(w);
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
