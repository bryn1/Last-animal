// SIZE: 171 l measured by wc AT this head (MC 10420 c3 F-E: the c2 "169/+5" stamp was
// delta-summed off pre-c2 164; real growth was +7 — WireFollowClamp call + note; delta
// arithmetic dies here, MC 3895 idiom) — WILD-side half of the stage 2g roster
// wiring (MC 10080 split of WorldDirector.Roster.cs: every member below moved VERBATIM, pure move).
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

    // MC 11297 PRODUCTION gate: the shipped game field ONE recruit-able wild
    // creature in the boot zone so the player-guide.md promise (lines 52-55:
    // "recruit a wild creature by interacting with it (E) and paying its first
    // wage (P)") and the Calming Speak skill (line 91: "the nearest castable
    // wild creature … is calmed into a recruit offer") are REACHABLE from
    // shipped input. Before this card SpawnWildNear stood behind the test seam
    // ONLY, so F always printed REFUSED in production (MC 11270 D5). The gate
    // mirrors SetSpawningEnabled: default ON (the product promise), one bool,
    // every caller re-checks at delivery; proofs whose wild choreography
    // predates this content flip it off before AddChild (the no_spawn idiom)
    // so their hand-driven wild stays the sole target, and the tame proof
    // leaves it ON — that IS the production path.
    private bool _wildSpawnEnabled = true;
    // One wild per run: fielded at the boot zone only, never re-fielded on
    // travel/boss-re-entry (no duplicate, no zone-clear interaction with the
    // enemy set). The recruit join already removes the body from _wild.
    private bool _wildFielded;

    /// <summary>Gate seam (design §4.2, SetSpawningEnabled idiom): disable the
    /// production wildlife fielding so a proof's own SpawnWildFollower seam is
    /// the sole wild. Off means a wild exists ONLY where a proof puts one.</summary>
    public void SetWildSpawnEnabled(bool enabled) => _wildSpawnEnabled = enabled;

    /// <summary>PRODUCTION field (MC 11297, the D5 fix): called from _Ready AFTER
    /// EnterZone (so the zone is mounted + its ground resolved and the wild's
    /// co-location clamps onto loaded ground), fields the zone's single recruit-able wild through the
    /// SHARED SpawnWildNear path — the seam and production are the only two
    /// wild composers and both build through it (replace-not-add). Idempotent:
    /// the flag makes boot the sole field point.</summary>
    private void FieldBootWild()
    {
        if (!_wildSpawnEnabled || _wildFielded) return;
        _wildFielded = true;
        SpawnWildNear(_spawnOrigin);
    }

    /// <summary>TEST SEAM (design §4.2 idiom): spawn a WILD creature near the
    /// player — interact offers, pay_wage recruits. The roster mechanic is
    /// proven through this seam exactly as a player drives it (in-play input);
    /// MC 11297 wired the PRODUCTION path (FieldBootWild) through the SAME
    /// SpawnWildNear, so the seam is the shared code path, not a parallel one.</summary>
    public void SpawnWildFollower()
    {
        Vector3 p = Player?.GlobalPosition ?? Vector3.Zero;
        SpawnWildNear(p + new Vector3(2.5f, 0f, 0f));
    }

    /// <summary>The ONE wild-body composer (the shared code path): builds a
    /// WILD CompanionFollowBody at <paramref name="anchor"/>, ground-clamped
    /// (Target null → stands its ground). Both the test seam (SpawnWildFollower)
    /// and production (FieldBootWild) enter here — one implementation.</summary>
    private void SpawnWildNear(Vector3 anchor)
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
        AddChild(body);                              // enters the tree FIRST (Godot 4.7: GlobalPosition requires it)
        // MC 10420 (10026.29): the co-location lands ON the ground — this site
        // placed wild id 24 18 m past the meadow edge during calm_use's off-
        // edge choreography, and every later co-location onto that body dragged
        // the player below the plane (the 2 census FALL_RECOVERED the MC 10423
        // reason line names). The SAME ClampToGround bound that owns enemy
        // MOTION bounds this placement — one mechanism.
        body.GlobalPosition = ClampToGround(anchor);
        // MC 10420 c2 F-DA1: WireFollowClamp is UNGATED at every body spawn
        // site (boot, restored, this wild one), so the GroundClamp delegate is
        // LIVE here from the spawn — a wild body merely lacks a Target, and
        // _Process returns before it is read (pay_wage sets Target + _bodies).
        WireFollowClamp(body);
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
