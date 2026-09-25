using System;

// Last Animal — M05 companion-system (MC 890.12, bernie, 2026-09-06).
//
// The companion's material NEEDS: what makes a settlement due and when one
// is 'satisfied'. Pure logic (I3), no Godot types.
//
// Design note: the PHASE0 Phase-9 gate names "follow/needs" — a companion
// that currently needs something. The concrete need this module models is
// the companionship wage (the M03 force): a payment becomes due on a cadence
// and stays due until paid. This file is deliberately small — it only owns
// the *due-ness* bookkeeping, never the loyalty change (that is M03's).
//
// MC 1348 A3: the cadence is TIME-based (delta-accumulated seconds), not
// frame ticks — the director ticks it once per _Process with the frame
// delta, so the wage rhythm is identical at any frame rate. Playable
// cadence: the first wage is due after GraceSeconds (20 s) of companionship
// and recurs every PayIntervalSeconds (30 s) after each settlement; a wage
// left unpaid for one full interval is one skipped cycle (the director's
// skip arm, M03 SkipPenalty) — neglect drains loyalty to betrayal.
namespace LastAnimal.Companion;

/// <summary>
/// The companion's need ledger: when a wage payment is due and how long it
/// has gone unmet. Payments are settled via M05's Pay()/SkipPayment(), which
/// delegate the loyalty effect to M03 SalarySystem.
/// </summary>
public class CompanionNeeds
{
    /// <summary>Seconds of accompaniment before the first wage is demanded.</summary>
    public double GraceSeconds { get; set; }

    /// <summary>Seconds between demanded payments once the companion is bonded.</summary>
    public double PayIntervalSeconds { get; set; }

    /// <summary>True when a wage is currently owed and unpaid.</summary>
    public bool SalaryDue { get; private set; }

    /// <summary>Seconds the current wage has gone unpaid.</summary>
    public double DueSeconds { get; private set; }

    /// <summary>How many pay intervals were skipped while the wage went unpaid.</summary>
    public int SkippedCycles { get; private set; }

    public CompanionNeeds(double graceSeconds = 20, double payIntervalSeconds = 30)
    {
        GraceSeconds = graceSeconds;
        PayIntervalSeconds = payIntervalSeconds;
        _nextDueSeconds = graceSeconds;
    }

    /// <summary>Advance the due clock by the elapsed frame time (seconds).</summary>
    public void TickAccompaniment(double deltaSeconds)
    {
        // No loyalty math here — this only flips the "something is owed now"
        // flag once the companionship clock passes the next due point, and
        // tracks how long the current wage has sat unpaid.
        if (SalaryDue)
        {
            DueSeconds += deltaSeconds;
            return;
        }
        _sinceSettled += deltaSeconds;
        if (_sinceSettled >= _nextDueSeconds)
        {
            SalaryDue = true;
            DueSeconds = 0;
        }
    }

    /// <summary>Mark the current wage as paid; the next is due in PayIntervalSeconds.</summary>
    public void MarkPaid()
    {
        SalaryDue = false;
        DueSeconds = 0;
        SkippedCycles = 0;
        _sinceSettled = 0;   // restart the interval clock from the pay moment
        _nextDueSeconds = PayIntervalSeconds;
    }

    /// <summary>Record one skipped pay cycle (a full interval elapsed unpaid).</summary>
    public void AdvanceSkippedCycle() => SkippedCycles++;

    /// <summary>
    /// Consume one full unpaid interval: true exactly once per elapsed
    /// PayIntervalSeconds while a wage sits unpaid — the director's skip
    /// cadence (MC 1348 A3: the skip arm must actually run in play).
    /// </summary>
    public bool ConsumeUnpaidInterval()
    {
        if (!SalaryDue || DueSeconds < PayIntervalSeconds) return false;
        DueSeconds -= PayIntervalSeconds;
        return true;
    }

    /// <summary>
    /// The settlement policy hook: by default a wage is only 'settled' through
    /// an explicit Pay(); returns false so the steady state falls through to
    /// SkipPayment (the unpaid arm). Callers may override the policy.
    /// </summary>
    public virtual bool TrySettlePayment() => false;

    private double _sinceSettled;
    private double _nextDueSeconds;
}
