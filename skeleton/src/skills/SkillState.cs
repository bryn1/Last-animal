using System;
using LastAnimal.Combat;
using LastAnimal.Dna;

// Last Animal — skill core arm/consume semantics (MC 3912 stage 2e, code,
// 2026-10-02).
//
// SkillState: the skill economy + arm/consume state machine over the ONE
// director-owned PlayerController. Engine-free (I3): the whole F-act list of
// stage 2e (pay-drops-exactly-once, armed-hit multiplier, arm consumed after
// one hit, insufficient-Manna rejection, Mend heal) is pinned headless here,
// no window, no bus.
//
// Economy (owner ruling D3 RATIFIED "All rec", plan §B 2e):
//   * spend Manna on USE (one drop per successful use, never on failure);
//   * +Manna per kill — called by the director from the EXISTING
//     OnDnaExtracted handler site (there is no second kill hook here);
//   * cap 100 — PlayerController.MannaCap clamps every write;
//   * NO cross-load regen — the value rides the v3 Manna save field
//     verbatim (the load path restores, it never refills).
//
// The UNLOCK authority is NOT here: callers pass the live
// PlayerMutations.Unlocked(...) verdict per use (plan §G D4 — single live
// source, no cached flag this class could stale out). A locked use is
// rejected WITHOUT spending. MC 10031 adds the Calming Speak cost row +
// TryCalmingSpeak (spend AFTER the caller's target scan, refuse spends zero).
//
// Arm semantics (DA-c2 D3 F-act 4): arming Invert Strike spends Manna and
// sets PlayerController.ArmMultiplier; the armed damage is read through
// ConsumeArmedDamage AT THE SINGLE melee call site (WorldDirector
// ConsumeSkillDamage wrapper) — the arm is consumed by exactly one hit, and
// every hit without an arm is base damage. The multiplier rides the damage
// VALUE, so the kill path through CombatSystem.DealDamage stays ONE.
namespace LastAnimal.Skills;

/// <summary>
/// Pure-logic skill economy + arm/consume machine over a PlayerController.
/// No Godot types, no bus (I4 — the director's Skills partial maps results
/// onto the SkillUsed signal).
/// </summary>
public sealed class SkillState
{
    // Tunables (the plan's "tunable" economy; the cap constant lives with
    // the storage that clamps it, PlayerController.MannaCap).
    /// <summary>Manna cost of arming Invert Strike.</summary>
    public const int InvertStrikeCost = 10;

    /// <summary>Manna cost of Mend.</summary>
    public const int MendCost = 15;

    /// <summary>Manna cost of Calming Speak (MC 10031: utility bridge,
    /// between Q=10 and R=15; the window it opens is the payload).</summary>
    public const int CalmingSpeakCost = 12;

    /// <summary>Damage multiplier the arm applies to the next melee hit.</summary>
    public const int InvertStrikeMultiplier = 3;

    /// <summary>Health Mend restores (before the controller's MaxHealth clamp).</summary>
    public const int MendHeal = 25;

    /// <summary>Manna gained per kill (rider on the existing DnaExtracted handler).</summary>
    public const int KillMannaGain = 5;

    private readonly PlayerController _player;

    public SkillState(PlayerController player)
    {
        _player = player ?? throw new ArgumentNullException(nameof(player));
    }

    /// <summary>The live Manna readout (storage: PlayerController).</summary>
    public int Manna => _player.Manna;

    /// <summary>Is an Invert Strike arm currently pending on a melee hit?</summary>
    public bool IsArmed => _player.ArmMultiplier > 1;

    /// <summary>
    /// Use Invert Strike: spend the cost and set the armed multiplier for
    /// the NEXT melee hit. Rejected (and NOTHING spent) when locked or when
    /// Manna is short. Re-arming while armed overwrites the pending arm —
    /// each successful use is one payment, each hit consumes at most one arm.
    /// </summary>
    public bool TryInvertStrike(bool unlocked)
    {
        if (!unlocked) return false;
        if (!TrySpend(InvertStrikeCost)) return false;
        _player.ArmMultiplier = InvertStrikeMultiplier;
        return true;
    }

    /// <summary>
    /// Use Mend: spend the cost and restore MendHeal health (clamped to
    /// MaxHealth by the controller). Rejected (and NOTHING spent) when
    /// locked or when Manna is short. A full-health Mend still fires —
    /// spending is per USE, the heal clamp belongs to health, not the economy.
    /// </summary>
    public bool TryMend(bool unlocked)
    {
        if (!unlocked) return false;
        if (!TrySpend(MendCost)) return false;
        _player.RestoreHealth(Math.Min(_player.MaxHealth, _player.Health + MendHeal));
        return true;
    }

    /// <summary>
    /// Use Calming Speak (MC 10031): spend the cost — the caller has already
    /// scanned a castable target BEFORE this runs (design §1.2: a targeted
    /// skill knows a target exists before it takes the player's Manna).
    /// Locked or short balance pays NOTHING (one payment on success, refuse
    /// spends zero) — deliberately NOT Mend's pay-when-clamped: a cast at
    /// nothing lands on nobody. No cooldown: Manna is the throttle (D3).
    /// </summary>
    public bool TryCalmingSpeak(bool unlocked) => unlocked && TrySpend(CalmingSpeakCost);

    /// <summary>
    /// Read the melee damage value FOR THE SINGLE DEALDAMAGE CALL SITE:
    /// with a pending arm, base x multiplier and the arm is consumed
    /// (multiplier back to 1); without one, base damage untouched. This is
    /// the ONLY consumer of the arm — one hit, one consumption (F-act 4).
    /// </summary>
    public int ConsumeArmedDamage(int baseDamage)
    {
        int mult = _player.ArmMultiplier;
        if (mult <= 1) return baseDamage;
        _player.ArmMultiplier = 1;
        return baseDamage * mult;
    }

    /// <summary>Per-kill Manna gain (rider — see file header).</summary>
    public void OnKill() => GainManna(KillMannaGain);

    /// <summary>Add Manna, clamped at the cap (writes route through the
    /// controller's clamping setter, so this is cap-honest even if doubled).</summary>
    public void GainManna(int amount) => _player.Manna = _player.Manna + amount;

    /// <summary>
    /// Spend Manna exactly once, or refuse and spend nothing (insufficient
    /// balance / negative cost). The single spend primitive both skills ride.
    /// </summary>
    public bool TrySpend(int cost)
    {
        if (cost < 0) return false;
        if (_player.Manna < cost) return false;
        _player.Manna = _player.Manna - cost;
        return true;
    }
}
