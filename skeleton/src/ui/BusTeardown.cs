using System;
using LastAnimal.Core;

// Last Animal — shared bus-teardown idiom for UI nodes (MC 11433, audit MC 11430
// CC-12). The _ExitTree disconnect block lived VERBATIM in src/ui/Hud.cs and
// src/ui/SkillsPanel.cs (the audit's body-hash scan: identical 8-line bodies);
// it is hoisted to this ONE home and both call it. Byte-equivalent behavior:
// the helper unhooks a non-null bus through the caller's own DisconnectBus and
// returns null for the caller's field reset — nothing else, no state of its own.
namespace LastAnimal.Ui;

/// <summary>
/// One home for the UI bus-unhook teardown idiom (MC 11430 audit CC-12: was a
/// verbatim 2-copy in Hud + SkillsPanel; the OTHER two _ExitTree sites in src
/// differ by design and are deliberately NOT folded in).
/// </summary>
internal static class BusTeardown
{
    /// <summary>Unhook a non-null <paramref name="bus"/> through the caller's
    /// own <paramref name="disconnect"/>; return null so the caller assigns it
    /// over its field (the reset the former inline copies did in place).</summary>
    public static EventBus? ExitAndUnhook(EventBus? bus, Action<EventBus> disconnect)
    {
        if (bus != null) disconnect(bus);
        return null;
    }
}
