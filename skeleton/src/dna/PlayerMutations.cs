using System;
using System.Collections.Generic;

// Last Animal — player-side mutation unlocks (MC 3912 stage 2e, code, 2026-10-02).
//
// PlayerMutations: the unlock AUTHORITY for the player's DNA-mutation skills.
// Pure logic (I3, same discipline as EcosystemAdaptation it reads): no Godot
// types, no state, no bus — the player-side mirror of the ecosystem's C5
// learner, reading the SAME consensus.
//
// Authority rule (plan §G D2, remedy Option B, owner ruling D3/D4 RATIFIED
// "All rec"): Unlocked(profile) is a PURE FUNCTION OF THE PER-POSITION
// CONSENSUS `Counters` ONLY — never ObservedCount, never Coverage. This is
// deliberate and load-bearing: the save path persists exactly that consensus
// (GameState.LearnedDnaCounters via SaveLoadController.BuildLearnedCounters)
// and RestoreDna re-synthesizes ONE signature reproducing it, so Counters
// (its LENGTH = how many positions the player's spoken DNA reached) is the
// only part of the profile that round-trips; ObservedCount collapses to 1 on
// every load. Round-trip stability of the unlocks is therefore STRUCTURAL,
// not asserted hopefully: same consensus in -> same unlocks out.
//
// Thresholds (tunable inside this pure fn per the plan):
//   Invert Strike  — >= 1 position carries a learned counter.
//   Mend           — >= 2 positions.
//   Calming Speak  — >= 3 positions (MC 10031; same Counters-only authority).
//
// Deliberately NOT here (plan §G D4): NO LearnedMutations save field, NO HUD
// cache. The panel/HUD read Unlocked(...) LIVE; there is no cache to go
// stale. The id strings below are the stable SkillUsed wire ids (2c batch,
// GD0202 string-Id carriers).
namespace LastAnimal.Dna;

/// <summary>
/// The set of unlocked player mutations at one instant (value-equality so
/// save/load round-trip equality is assertable directly).
/// </summary>
public readonly struct SkillUnlocks : IEquatable<SkillUnlocks>
{
    /// <summary>Invert Strike unlocked (>= 1 learned position).</summary>
    public bool InvertStrike { get; }

    /// <summary>Mend unlocked (>= 2 learned positions).</summary>
    public bool Mend { get; }

    /// <summary>Calming Speak unlocked (>= 3 learned positions, MC 10031).</summary>
    public bool CalmingSpeak { get; }

    public SkillUnlocks(bool invertStrike, bool mend, bool calmingSpeak)
    {
        InvertStrike = invertStrike;
        Mend = mend;
        CalmingSpeak = calmingSpeak;
    }

    /// <summary>Unlocked skill ids in authoring order (the panel read idiom).</summary>
    public IReadOnlyList<string> Ids
    {
        get
        {
            var ids = new List<string>(3);
            if (InvertStrike) ids.Add(PlayerMutations.InvertStrikeId);
            if (Mend) ids.Add(PlayerMutations.MendId);
            if (CalmingSpeak) ids.Add(PlayerMutations.CalmingSpeakId);
            return ids;
        }
    }

    public bool Equals(SkillUnlocks other) =>
        InvertStrike == other.InvertStrike && Mend == other.Mend
        && CalmingSpeak == other.CalmingSpeak;

    public override bool Equals(object? obj) => obj is SkillUnlocks u && Equals(u);

    public override int GetHashCode() =>
        (InvertStrike, Mend, CalmingSpeak).GetHashCode();

    public static bool operator ==(SkillUnlocks a, SkillUnlocks b) => a.Equals(b);
    public static bool operator !=(SkillUnlocks a, SkillUnlocks b) => !a.Equals(b);

    public override string ToString() =>
        $"SkillUnlocks[invert_strike:{InvertStrike}, mend:{Mend}, calming_speak:{CalmingSpeak}]";
}

/// <summary>
/// The unlock authority: a pure function from the ecosystem's CounterProfile
/// consensus to the player's unlocked mutations (see file header).
/// </summary>
public static class PlayerMutations
{
    /// <summary>Stable SkillUsed wire id — Invert Strike (owner ruling D4).</summary>
    public const string InvertStrikeId = "invert_strike";

    /// <summary>Stable SkillUsed wire id — Mend (owner ruling D4).</summary>
    public const string MendId = "mend";

    /// <summary>Stable SkillUsed wire id — Calming Speak (MC 10031, rides the
    /// same string-Id carrier as the 2c batch; no bus edit).</summary>
    public const string CalmingSpeakId = "calming_speak";

    /// <summary>Learned positions required for Invert Strike.</summary>
    public const int InvertStrikePositions = 1;

    /// <summary>Learned positions required for Mend.</summary>
    public const int MendPositions = 2;

    /// <summary>Learned positions required for Calming Speak (MC 10031).</summary>
    public const int CalmingSpeakPositions = 3;

    /// <summary>
    /// The unlock authority. Reads profile.Counters.Length (how many
    /// positions carry a learned counter) and NOTHING ELSE — a null/empty
    /// profile unlocks nothing. See the file header for why this is the
    /// only round-trip-stable authority (plan §G D2 Option B).
    /// </summary>
    public static SkillUnlocks Unlocked(CounterProfile? profile)
    {
        int positions = profile?.Counters.Length ?? 0;
        return new SkillUnlocks(
            positions >= InvertStrikePositions,
            positions >= MendPositions,
            positions >= CalmingSpeakPositions);
    }

    /// <summary>Single-skill convenience (same authority, live read).</summary>
    public static bool IsUnlocked(string skillId, CounterProfile? profile) =>
        skillId switch
        {
            InvertStrikeId => Unlocked(profile).InvertStrike,
            MendId => Unlocked(profile).Mend,
            CalmingSpeakId => Unlocked(profile).CalmingSpeak,
            _ => false,   // unknown id is never unlocked
        };
}
