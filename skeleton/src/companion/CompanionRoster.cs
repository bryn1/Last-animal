// SIZE: 399 l (target 250 exceeded, ceiling 400 held — reason): the roster
// grew with S9 (trait derivation + restore seam) and S17 (trait-rule constants,
// WageMissTick, Forager pay row; split would sever the settle from its rules).
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
// Three contracts live HERE so they hold headless:
//   * UNIQUE BUS KEYS (DA-c2 D6): every follower's LoyaltyChanged key is
//     "<name>-<EntityId>" — two followers can never cross-key one bus line.
//   * EMIT-ORDER CONTRACT (plan §G D6): TickRoster emits per-follower loyalty
//     deltas FIRST, in roster order, then the roster-mean loyalty LAST under
//     the reserved key "roster" — the existing last-value redraw (Hud.cs
//     hearts) then always paints the mean, with NO Hud edit. QuestLog
//     permanently ignores the reserved key (F2 ruling, src/story/QuestLog.cs).
//   * DERIVED IDENTITY (MC 10145 S9): the TRAIT is a pure fold of the bond
//     EntityId (TraitFor, S5 PitchFor idiom) — never persisted (F2) — and the
//     restore REUSE seam re-latches key + trait + value bounds in one place
//     (RestoreIdentity, NF5/NF6 harden).
//   * TRAIT EFFECTS (MC 10201 S17): one pure rule per trait INSIDE the
//     settle (PayDueFollowers / WageMissTick, rule table at ForagerWageGain) —
//     deterministic per-SLOT, no new state, never persisted (F2).
namespace LastAnimal.Companion;

/// <summary>
/// MC 10145 S9 (RULING-4): the follower TRAIT — DERIVED from the bond
/// EntityId, NEVER persisted (F2 wall: grep -c Trait src/save/ == 0; the
/// trait re-derives from the EntityId on every load). Presentation + print
/// only: the loyalty-delta print carries the tag, CompanionVisual takes the
/// per-trait accent. Per-SLOT deterministic — accepted by the ratified DA
/// note: ids correlate with spawn order.
/// </summary>
public enum CompanionTrait { Steadfast, Forager, Sentinel, Bonded }

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

    /// <summary>
    /// S17 TRAIT RULE TABLE (MC 10201, one pure rule per trait INSIDE the
    /// engine-free settle; no new state, never persisted — F2):
    ///   Steadfast  wage-miss tick   decay -25%   effective = max(1, floor(D*75/100))
    ///                                        over the RAW drain D (M03 SkipSalary):
    ///                                        base D=3 -> 2 (50->48); clamp edges
    ///                                        4->2, 3->1, 2->1, 1->0 (terminal)
    ///   Forager    landed pay       wage -2      +5 -> +3 (ForagerWageGain); gain
    ///                                        = min(3, what the base settle granted)
    ///   Bonded     landed pay       +1 Manna     consumed at the shipped
    ///            Manna add site via the settle RETURN (SkillState.GainManna)
    ///   Sentinel   CUT — no kill-assist seam exists (S16-census discipline:
    ///              never invent a seam; grep -ri assist == 0 hits at 0968010)
    ///   band row   SHIPPED — trait-INDEPENDENT wage-free upkeep: while the
    ///              LIVE learned-position count (the shipped Counters.Length
    ///              read at the settle glue — no PlayerMutations edit, no
    ///              S16 build dependency) reaches WageFreeUpkeepPositions,
    ///              WageMissTick waives that tick's upkeep. Threshold from
    ///              the S16/S8 census LANDED at build time
    ///              (.audits/202610070005-s16/census.md — positions 1..6
    ///              census-confirmed reachable; 4 = the S8 census number).
    /// Deterministic per-SLOT (RULING-4: the latched derived Trait), pure
    /// integer arithmetic, zero save delta. tests/roster pins this table.
    /// </summary>
    public const int ForagerWageGain = 3;

    /// <summary>S17 Steadfast: the loyalty-percentage the wage-miss decay KEEPS
    /// (the ratified -25% decay => keep 75%). effective decay = the raw M03
    /// drain D floored through this percentage, held at SteadfastDecayFloor so
    /// neglect never pins a follower above 0 (betrayal stays reachable).</summary>
    public const int SteadfastKeepPct = 75;

    /// <summary>S17 Steadfast: the minimum EFFECTIVE wage-miss decay (a tick
    /// always lands at least this; 1 -> loyalty strictly decreases, terminal 0).</summary>
    public const int SteadfastDecayFloor = 1;

    /// <summary>S17 Bonded rule size: Manna the player gains when a BONDED
    /// follower's settle LANDS (rides the settle return; the shipped
    /// SkillState.GainManna add site clamps at the cap — MC 3912).</summary>
    public const int BondedMannaOnPay = 1;

    /// <summary>S17 WAGE-FREE UPKEEP BAND (census S16/S8, threshold 4 = the
    /// S8 census number, census-confirmed reachable; the census also records
    /// THRESHOLD COLLAPSE — live content holds widths {0,6}, so the band
    /// lights at the first extraction). Runtime-only, never persisted (F2).</summary>
    public const int WageFreeUpkeepPositions = 4;

    /// <summary>The band predicate over the LIVE learned-position count
    /// (Counters.Length of the shipped profile the SkillState side consults).
    /// Pure, deterministic, census-legal only for k &lt;= 6 (cutoff row).</summary>
    public static bool WageFreeUpkeep(int learnedPositions) =>
        learnedPositions >= WageFreeUpkeepPositions;

    /// <summary>
    /// S9 TRAIT derivation (RULING-4) — pure-int fold, the S5 PitchFor idiom
    /// (autoload/SfxRouter.cs:71-76): fixed seed, unchecked, (uint) modulo.
    /// NEVER System.Random, NEVER string.GetHashCode (its seed is per-process —
    /// F4 cross-run determinism would break). Deterministic per EntityId, so
    /// the trait is per-SLOT stable across runs (DA note honored in plan v3).
    /// </summary>
    public static CompanionTrait TraitFor(int entityId)
    {
        int h = unchecked(17 * 31 + entityId);
        return (CompanionTrait)((uint)h % 4u);
    }

    /// <summary>NF6 value bound (MC 10145, DA-c2 NF6 record): the restore seam
    /// takes VALUES from a possibly hand-edited save. A saved bond id can
    /// never re-enter the -1 unbond-sentinel class from that seam (M03:
    /// HasCompanion == id >= 0 — a negative write used to unbond a restored
    /// stack, leaving a "name--1" member counted by the mean).</summary>
    public static int RestoreBondId(int entityId) => Math.Max(0, entityId);

    /// <summary>NF6: loyalty bound to the M03 clamp range [0, 100] before the
    /// direct setter write (gameplay paths go through ModifyLoyalty, which
    /// owns the same clamp).</summary>
    public static int RestoreLoyalty(int loyalty) => Math.Max(0, Math.Min(100, loyalty));

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
        /// DA-c2 D6 + DA W5 F2: the UNIQUE bus key "<name>-<EntityId>" —
        /// LATCHED at construction (identity-frozen). The bond id is set on the
        /// component on EVERY spawn path BEFORE the Follower is built (boot:
        /// SetCompanion(7) in the root; wild: SetCompanion(eid); restored:
        /// CompanionEntityId = entry.EntityId). A LIVE-computed key would drift
        /// every betrayer to "&lt;name&gt;--1" once BreakCompanion flips the bond
        /// id — two betrayed recruits would then share one LoyaltyChanged line.
        /// NF5 harden (MC 10145): the ONE path that rewrites the id on a live
        /// stack is the restore REUSE seam — it re-latches through
        /// <see cref="RestoreIdentity"/>, never a silent in-place write.
        /// </summary>
        public string BusKey { get; private set; }

        /// <summary>
        /// S9 (RULING-4): the derived TRAIT, latched with the identity exactly
        /// like BusKey (and re-latched by RestoreIdentity on the reuse path).
        /// Derived state only — never persisted (F2: re-computed every load).
        /// </summary>
        public CompanionTrait Trait { get; private set; }

        public Follower(string name, CompanionComponent component,
                        CompanionNeeds needs, CompanionStateMachine machine)
        {
            Name = name;
            Component = component;
            Needs = needs;
            Machine = machine;
            LoyaltyLast = component.Loyalty;
            StateLast = machine.State;
            BusKey = name + "-" + component.CompanionEntityId;
            Trait = CompanionRoster.TraitFor(component.CompanionEntityId);
        }

        /// <summary>
        /// NF5 harden + NF6 bound (MC 10145, DA-c2 NF5/NF6 records): the
        /// restore REUSE seam — the one path that applies a SAVED identity onto
        /// a live stack (spawn paths set the id BEFORE construction — F2). It
        /// re-latches BusKey and Trait from the saved id (key format unchanged,
        /// per the NF5 record) and bounds the saved VALUES (NF6: id out of the
        /// -1 unbond-sentinel class, loyalty into the M03 clamp range) — sizes
        /// stay the cap's job (F4 legs untouched, F6).
        /// </summary>
        public void RestoreIdentity(int entityId, int loyalty)
        {
            int id = CompanionRoster.RestoreBondId(entityId);
            Component.CompanionEntityId = id;
            Component.Loyalty = CompanionRoster.RestoreLoyalty(loyalty);
            BusKey = Name + "-" + id;
            Trait = CompanionRoster.TraitFor(id);
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
    /// DA W5 F1: a BROKEN bond is UNPAYABLE — the betrayed follower's wage
    /// clock keeps ticking (as before the move), but it never settles: no
    /// WagePaid source, no loyalty drift on a dead bond.
    /// </summary>
    public List<Follower> PayDueFollowers()
    {
        var settled = new List<Follower>();
        foreach (var f in _followers)
        {
            if (!f.Needs.SalaryDue) continue;
            // DA W5 F1 (the guard the move lost, MC 1348 A3): M03's HasCompanion
            // bond flag is the authority; the Betrayed machine state is the
            // defensive net. A betrayer takes no money on a dead bond.
            if (!f.Component.HasCompanion) continue;
            if (f.Machine.State == CompanionState.Betrayed) continue;
            int before = f.Component.Loyalty;
            f.Machine.Pay();
            if (!f.Needs.SalaryDue)
            {
                // S17 FORAGER (MC 10201 rule table): the settle lands wage -2
                // — gain min(3, what the base settle granted), so the M03 clamp
                // at the cap can never turn a pay into a Forager penalty.
                // ModifyLoyalty stays M03's clamp path (Forgive precedent).
                if (f.Trait == CompanionTrait.Forager)
                {
                    int granted = f.Component.Loyalty - before;
                    if (granted > ForagerWageGain)
                        f.Component.ModifyLoyalty(ForagerWageGain - granted);
                }
                settled.Add(f);   // landed (M03 PaySalary true)
            }
        }
        return settled;
    }

    /// <summary>
    /// The roster-owned WAGE-MISS tick (S17, MC 10201): the world skip arm
    /// calls THIS, so the rules live inside the engine-free settle.
    /// BAND (trait-INDEPENDENT): wageFreeUpkeep (the WageFreeUpkeep predicate
    /// over the LIVE Counters.Length read at the glue) WAIVES the whole upkeep
    /// this tick — no M03 decay, no cycle bookkeeping: a deeply resonant
    /// follower is simply not hungry this tick. Otherwise M03 owns the raw
    /// decay (SkipPayment -> SkipSalary -3, Phase-9 gate) and a STEADFAST
    /// follower's EFFECTIVE decay is max(SteadfastDecayFloor, floor(D *
    /// SteadfastKeepPct/100)) — the ratified -25% — refunded through M03's own
    /// ModifyLoyalty clamp path. The floor keeps every tick strictly
    /// decreasing so betrayal stays REACHABLE (table pinned in tests/roster:
    /// base 50->47 vs Steadfast 50->48, edges 4->2, 3->1, 2->1, 1->0; band-on:
    /// 50->50). Deterministic per-SLOT (the latched derived Trait).
    /// </summary>
    public void WageMissTick(Follower f, bool wageFreeUpkeep = false)
    {
        if (wageFreeUpkeep) return;                           // band: upkeep waived this tick
        int before = f.Component.Loyalty;
        f.Machine.SkipPayment();                              // M03 owns the raw decay
        if (f.Trait != CompanionTrait.Steadfast) return;
        int drained = before - f.Component.Loyalty;           // clamp-honest raw decay
        int effective = Math.Max(SteadfastDecayFloor, drained * SteadfastKeepPct / 100);
        int refund = drained - effective;                     // <= drained-1: net decay < raw
        if (refund > 0) f.Component.ModifyLoyalty(refund);    // back through M03's clamp path
    }

    /// <summary>Forgive (Empathy Book glue, applied only in the director partial):
    /// the loyalty bonus on a BONDED follower. DA W5 F1: FALSE — no bonus at all
    /// — when the bond is broken (the same guard as the pay arm; the Empathy
    /// Book never forges affection for a betrayer). M03 owns the clamp.</summary>
    public bool Forgive(Follower follower)
    {
        if (!follower.Component.HasCompanion) return false;
        if (follower.Machine.State == CompanionState.Betrayed) return false;
        follower.Component.ModifyLoyalty(ForgiveBonus);
        return true;
    }

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
