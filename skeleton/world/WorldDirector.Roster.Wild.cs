// SIZE: 158 l — WILD-side half of the stage 2g roster wiring (MC 10080 split of
// WorldDirector.Roster.cs: every member below moved VERBATIM, pure move).
using Godot;
using LastAnimal.Companion;

// Last Animal — MC 10080 wild/recruit half of the MC 3943 stage 2g roster
// wiring (code, 2026-10-04).
//
// Partial-class half of WorldDirector (one class = still THE single
// composition root, plan §B). This file OWNS the wild-side concerns: the
// standing recruit offer's ONE open/close pair (MC 10031 §3), the recruit join
// (interact + FIRST wage, cap 3 — owner D1), the TEST-SEAM wild spawn, the
// interact NPC scan over the follower AND wild bodies, and the calm-window load
// seam (R9 mirror). The roster-core — state lists + read-only surfaces,
// Init/Tick, the cycle/break polls, the visible-body spawns and the Followers
// save seams — stays in WorldDirector.Roster.cs.
namespace LastAnimal.World;

public partial class WorldDirector
{
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
        AddChild(body);                              // enters the tree FIRST (Godot 4.7: GlobalPosition requires it)
        body.GlobalPosition = p + new Vector3(2.5f, 0f, 0f);
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
}
