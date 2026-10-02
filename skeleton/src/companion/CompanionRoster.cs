using System;
using System.Collections.Generic;
using LastAnimal.Npc;

// Last Animal — MC 3943 stage 2g follower roster (code, 2026-10-02).
//
// CompanionRoster: the ROSTER of the EXISTING companion stack — one entry per
// follower, each holding its OWN CompanionComponent (M03) + CompanionNeeds
// (M05) + CompanionStateMachine (M05). This is NOT a new system: it replaces
// the director's singleton _companionCore/_needs/_companion fields with a
// list of those same objects (plan §B 2g — "up to 3 followers as a ROSTER of
// the existing stack", owner ruling D1 verbatim "All rec" -> cap 3). The
// per-follower wage, betrayal and loyalty semantics stay exactly where they
// were (M03 owns loyalty math, the machine owns behavior — Phase-9 gate).
//
// Pure logic (I3): no Godot types — the same discipline as CompanionStateMachine,
// headless-testable under `dotnet test` (tests/roster/). The visible bodies
// (CompanionFollowBody/CompanionVisual) stay world-side in
// world/WorldDirector.Roster.cs, index-aligned with Followers.
//
// Two contracts live HERE so they hold headless:
//   * UNIQUE BUS KEYS (DA-c2 D6): every follower's LoyaltyChanged key is
//     "<name>-<EntityId>" — two followers can never cross-key one bus line.
//   * EMIT-ORDER CONTRACT (plan §G D6): TickRoster emits per-follower loyalty
//     deltas FIRST, in roster order, then the roster-mean loyalty LAST under
//     the reserved key "roster" — the existing last-value redraw (Hud.cs
//     hearts) then always paints the mean, with NO Hud edit. QuestLog
//     permanently ignores the reserved key (F2 ruling, src/story/QuestLog.cs).
namespace LastAnimal.Companion;

/// <summary>
/// The follower roster: N x (CompanionComponent + CompanionNeeds +
/// CompanionStateMachine), cap 3 (owner D1), unique per-follower bus keys,
/// and the roster-mean-last emit-order contract (D6).
/// </summary>
public sealed class CompanionRoster
{
    /// <summary>Max simultaneous followers (owner ruling D1 verbatim "All rec" -> cap 3).</summary>
    public const int Cap = 3;

    /// <summary>
    /// F2 ruling (plan §G D6): the reserved LoyaltyChanged key the mean rides.
    /// Same reserved string as QuestLog.ReservedLoyaltyKey — quest predicates
    /// permanently ignore it; the HUD last-value redraw shows it.
    /// </summary>
    public const string ReservedMeanKey = "roster";

    /// <summary>Loyalty bonus applied by Forgive (same size as M03 PayBonus).</summary>
    public const int ForgiveBonus = 5;

    /// <summary>One follower: the existing stack objects, no new mechanism.</summary>
    public sealed class Follower
    {
        /// <summary>Diag name (also the machine's name); part of the bus key.</summary>
        public string Name { get; }

        /// <summary>This follower's OWN M03 component (loyalty authority).</summary>
        public CompanionComponent Component { get; }

        /// <summary>This follower's OWN M05 needs ledger (its wage clock).</summary>
        public CompanionNeeds Needs { get; }

        /// <summary>This follower's OWN M05 machine.</summary>
        public CompanionStateMachine Machine { get; }

        /// <summary>Loyalty last emitted for this follower (per-follower delta diff).</summary>
        public int LoyaltyLast { get; set; }

        /// <summary>Machine state last seen (per-follower betrayal-transition edge).</summary>
        public CompanionState StateLast { get; set; }

        /// <summary>
        /// DA-c2 D6: the UNIQUE bus key "<name>-<EntityId>" — the live bond id,
        /// so no two followers (wild or boot) ever share a LoyaltyChanged line.
        /// </summary>
        public string BusKey => Name + "-" + Component.CompanionEntityId;

        public Follower(string name, CompanionComponent component,
                        CompanionNeeds needs, CompanionStateMachine machine)
        {
            Name = name;
            Component = component;
            Needs = needs;
            Machine = machine;
            LoyaltyLast = component.Loyalty;
            StateLast = machine.State;
        }
    }

    /// <summary>One LoyaltyChanged emit: bus key + value (the ordered contract unit).</summary>
    public readonly struct LoyaltyEmit
    {
        public string Key { get; }
        public int Loyalty { get; }
        public LoyaltyEmit(string key, int loyalty) { Key = key; Loyalty = loyalty; }
    }

    private readonly List<Follower> _followers = new();
    private readonly BetrayalSystem _betrayal;

    public CompanionRoster(BetrayalSystem? betrayal = null)
        => _betrayal = betrayal ?? new BetrayalSystem();

    public IReadOnlyList<Follower> Followers => _followers;
    public int Count => _followers.Count;
    public Follower this[int index] => _followers[index];

    /// <summary>Selected follower index (cycle_follower pages it; the Empathy Book
    /// and the Forgive/break_bond glue act on THIS follower only).</summary>
    public int SelectedIndex { get; private set; }

    public Follower Selected => _followers[SelectedIndex];

    /// <summary>Add a follower. FALSE when the roster is at the cap (D1: the
    /// 4th join is refused, the roster unchanged).</summary>
    public bool TryAdd(Follower follower)
    {
        if (_followers.Count >= Cap) return false;
        _followers.Add(follower);
        return true;
    }

    /// <summary>Remove one follower (load rebuild / despawn); clamps the selection.</summary>
    public void RemoveAt(int index)
    {
        _followers.RemoveAt(index);
        if (SelectedIndex >= _followers.Count)
            SelectedIndex = Math.Max(0, _followers.Count - 1);
    }

    /// <summary>cycle_follower: advance the selection, wrapping (empty-safe).</summary>
    public void CycleSelected()
    {
        if (_followers.Count == 0) { SelectedIndex = 0; return; }
        SelectedIndex = (SelectedIndex + 1) % _followers.Count;
    }

    /// <summary>True while ANY follower's wage is owed (the re-keyed WageDueNow read).</summary>
    public bool AnyWageDue
    {
        get
        {
            foreach (var f in _followers)
                if (f.Needs.SalaryDue) return true;
            return false;
        }
    }

    /// <summary>
    /// ARCH W2 re-key (was a singleton read): pay EVERY follower whose wage is
    /// due and return the followers whose settle actually LANDED (their
    /// SalaryDue fell). The caller emits WagePaid ONCE per settled follower —
    /// each paying follower settles its own wage and owns its WagePaid emit.
    /// </summary>
    public List<Follower> PayDueFollowers()
    {
        var settled = new List<Follower>();
        foreach (var f in _followers)
        {
            if (!f.Needs.SalaryDue) continue;
            f.Machine.Pay();
            if (!f.Needs.SalaryDue) settled.Add(f);   // landed (M03 PaySalary true)
        }
        return settled;
    }

    /// <summary>Forgive (Empathy Book glue, applied only in the director partial):
    /// the loyalty bonus on the SELECTED follower. M03 owns the clamp.</summary>
    public void Forgive(Follower follower) => follower.Component.ModifyLoyalty(ForgiveBonus);

    /// <summary>The follower's own betrayal execution (M03 C7). Returns null when
    /// the bond is already broken — break_bond is per-selected-follower only.</summary>
    public BetrayalResult? ExecuteBetrayal(Follower follower) => _betrayal.ExecuteBetrayal(follower.Component);

    /// <summary>Roster-mean loyalty over ALL current followers, integer floor
    /// (deterministic; the reserved-key mean rides this exact value).</summary>
    public int MeanLoyalty()
    {
        if (_followers.Count == 0) return 0;
        int sum = 0;
        foreach (var f in _followers) sum += f.Component.Loyalty;
        return sum / _followers.Count;
    }

    /// <summary>
    /// The emit-order contract (plan §G D6), as pure data so tests/roster can
    /// pin it: append every follower whose loyalty moved since the last call —
    /// in roster order, each under its UNIQUE BusKey — then, if ANY moved,
    /// append the roster-mean LAST under ReservedMeanKey and update the
    /// per-follower diffs. True when at least one emit was collected.
    /// </summary>
    public bool CollectLoyaltyEmits(List<LoyaltyEmit> emits)
    {
        emits.Clear();
        bool any = false;
        foreach (var f in _followers)
        {
            if (f.Component.Loyalty == f.LoyaltyLast) continue;
            emits.Add(new LoyaltyEmit(f.BusKey, f.Component.Loyalty));
            f.LoyaltyLast = f.Component.Loyalty;
            any = true;
        }
        if (!any) return false;
        // D6: the mean MUST ride last — the Hud last-value redraw shows the
        // LAST LoyaltyChanged of the frame. Never move this line above the loop.
        emits.Add(new LoyaltyEmit(ReservedMeanKey, MeanLoyalty()));
        return true;
    }
}
