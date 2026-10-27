using Godot;
using LastAnimal.Dna;
using LastAnimal.Ecosystem;
using System;
using System.Text;

// Last Animal — MC 11493 TALK-TO-CREATURES (code, 2026-10-27).
//
// Owner ruling 2026-10-27 verbatim: "I want tall to creature to be implemented
// since it is built" — this RETIRES the MC 11433 CC-16 keep/retire park: the
// parked DnaLanguage message subsystem (Extract/Speak/Counter/Parse/ExtractCodon
// + LanguageSignature.Generate) gains its LIVE callers HERE, on the EXISTING
// src/dna classes — NO second communication mechanism (replace-not-add-beside).
//
// The exchange per the BEHAVIOR SPEC (tests/DnaLanguageTests.cs, the port notes
// in src/dna/*.cs): the player presses the speak key toward the nearest creature
// (WILD bodies AND follower roster bodies — the TryRosterNpc scan idiom WITHOUT
// its recruit-offer side effect: talk is not recruiting). The gate is the
// Calming-Speak-style LIVE authority (PlayerMutations.Unlocked over
// EcosystemAdaptation.ModelPlayerDna(_spokenDna), >= 3 learned positions) —
// "you can speak that creature's language" = you have learned the tongue.
// Then, exactly per the tested semantics: Generate synthesizes the player's
// learned tongue (seeded on the creature id — deterministic, no clock), Speak
// broadcasts it AT the creature, Extract reads the creature's own signature as
// its message, Counter answers with the inverted signal + Compatibility bond,
// and Parse + ExtractCodon group the reply payload into codon letters — the
// reply paints AS creature text in the dialogue band (DialogueSystem.ShowText,
// the SAME band, the SAME dwell — MC 11293 layout untouched, disjoint pins hold)
// and the spoken signal rides the EXISTING DnaSpoken bus edge (HUD meter, SFX,
// the quest Spoken observers — the same C2 semantics the interact path already
// emits; the quest-table "Speak the tongue" objectives are the tested tie-in).
//
// Named refusals (MC 11297 law — every unusable press prints WHY by name):
//   dialogue in progress  — the band holds a line (the reply would clobber it);
//   no creature in range  — TalkRange, the interact scan's own reach;
//   no tongue you know    — the live unlock gate locked (< 3 positions);
//   no signature          — the tested null-guard of Speak/Extract/Counter.
// A PLAYER corpse never reaches this arm: the speak poll sits inside the MC
// 11294 !_player.IsDead block, so a corpse returns on the death path exactly
// like Mend does (MC 11297 fix-round law: the corpse never prints REFUSED —
// zero agency, the LA_DEATH announcement is the named line there). Creature
// targets are WILD + followers only — enemy corpses are not conversable at all.
//
// R7-SAVE-SAFE: NOTHING new persists. The unlock, the tongue and the reply are
// computed live from _spokenDna (the save's existing LearnedDnaCounters round
// trip) + the entity id; the DnaSpoken emit rides the meter field that ALREADY
// persists what it counts. Zero new save fields, zero GameState delta.
//
// KEY NOTE (named deviation, MC 11493): the brief named "T"; T is the SHIPPED
// zone-travel key (project.godot travel=physical 84, docs/player-guide.md "zone
// travel with `T`", v0.5.0 GA) — rebinding a shipped control has no owner
// artifact, so speak rides a NEW unused key: V (physical 86, action "speak").
namespace LastAnimal.World;

public partial class WorldDirector
{
    /// <summary>
    /// The speak-key arm (MC 11493): polled from _Process INSIDE the MC 11294
    /// death block, beside attack/interact/travel — one agency gate, no second
    /// input mechanism (the same IsActionJustPressed idiom; the proof harnesses
    /// press it via Input.ActionPress("speak")).
    /// </summary>
    private void TrySpeak()
    {
        if (Player == null) return;

        // Refusal 1 (band discipline, MC 11293): a reply paints IN the dialogue
        // band — while it holds a line, a second line would clobber it. Named.
        if (_dialogue != null && _dialogue.IsOpen)
        {
            GD.Print($"TALK REFUSED — dialogue in progress (band holds '{_dialogue.ActiveNode}'; the reply shares the dialogue band)");
            return;
        }

        // Nearest creature: FOLLOWER bodies + WILD bodies (the TryRosterNpc
        // scan shape; the recruit-offer side effect deliberately NOT reused —
        // talking is not recruiting). TalkRange: the interact scan's own reach.
        Vector3 ppos = Player.GlobalPosition;
        Node3D? body = null;
        int npcId = 0;
        bool isWild = false;
        float best = TalkRange;
        for (int i = 0; i < _roster.Count && i < _bodies.Count; i++)
        {
            // Dead bonds are NOT talk targets: the bond flag is the shipped F1/NF7
            // authority (the pay arm excludes it the same way — Roster.cs:136), and
            // a broken companion is a husk the settle already refuses to pay. The
            // WILD body beside a dead bond still answers; that IS the scan below.
            if (!_roster[i].Component.HasCompanion) continue;
            float d = (_bodies[i].GlobalPosition - ppos).Length();
            if (d <= best) { best = d; body = _bodies[i]; isWild = false; npcId = _roster[i].Component.CompanionEntityId; }
        }
        foreach (var w in _wild)
        {
            float d = (w.GlobalPosition - ppos).Length();
            if (d <= best) { best = d; body = w; isWild = true; npcId = w.EntityId; }
        }
        if (body == null)
        {
            GD.Print($"TALK REFUSED — no creature in range (talk reach {TalkRange} m, walk closer)");
            return;
        }

        // The gate (Calming-Speak-style, plan §G D2 authority read LIVE — the
        // same consensus Unlocked/Passives read; no cache, no save field):
        // learned positions >= 3 means the player can speak the tongues.
        var profile = EcosystemAdaptation.ModelPlayerDna(_spokenDna);
        if (!PlayerMutations.Unlocked(profile).CalmingSpeak)
        {
            GD.Print($"TALK REFUSED — no tongue you know (learned DNA positions {profile.Counters.Length}/3 — extract DNA from kills first)");
            return;
        }

        // The exchange, per the tested semantics (DnaLanguageTests.cs):
        //   Generate  the player's learned tongue — length = learned positions,
        //             seeded on the creature id so every press is DETERMINISTIC
        //             (id-seeded like SignatureForEntity; no clock, no rng state).
        //   Speak     broadcasts it at the creature (null-guard: tested).
        //   Extract   the creature's own signature as its message (tested).
        //   Counter   the creature's answer: inverted signal + Compatibility
        //             bond strength (tested: payload inverted, Target = speaker).
        //   Parse + ExtractCodon group the reply into codon letters — the
        //   round trip (PayloadAsString -> Parse) is exact by the shared A/C/G/T
        //   map; codon grouping is the tested ExtractCodon contract.
        var tongue = LanguageSignature.Generate(profile.Counters.Length, new System.Random(npcId));
        var creatureSig = DnaLanguage.SignatureForEntity(npcId);   // the ONE id-seeded builder
        var spoken = _dna.Speak(tongue, targetEntityId: npcId);
        var own = _dna.Extract(creatureSig);
        var reply = spoken != null ? _dna.Counter(creatureSig, spoken) : null;

        // Refusal 4: the tested no-component guard — a null anywhere in the
        // Speak/Extract/Counter chain is a no-signature creature. Named.
        if (spoken == null || own == null || reply == null)
        {
            GD.Print($"TALK REFUSED — no signature (creature {npcId}: the Speak/Extract/Counter chain returned null — the tested no-component guard)");
            return;
        }

        var replySig = new LanguageSignature(DnaLanguage.Parse(reply.PayloadAsString()) ?? Array.Empty<int>(), npcId);
        var codons = new StringBuilder();
        for (int idx = 0; ; idx++)
        {
            int[]? codon = DnaLanguage.ExtractCodon(replySig, idx);
            if (codon == null) break;
            if (codons.Length > 0) codons.Append('-');
            foreach (int n in codon) codons.Append(LanguageSignature.ToBase(n));
        }

        string kind = isWild ? "Wild" : "Follower";
        // ORDER (measured, MC 11493 smoke): the spoken signal rides the EXISTING
        // C2 edge (meter + SFX + the quest "Speak the tongue" observers — the
        // tested Spoken tie-in, QuestTable) FIRST; the first DnaSpoken takes the
        // band with its OWN reward beat (the authored "first_speak" line — the
        // shipped presentation of the tested objective), and the reply then paints
        // OVER it through the same band, resetting the dwell (rev-4 semantics: a
        // queued beat pops at its pop point and the box drains to the auto-close
        // underneath). The player sees the authored acknowledgment, then the
        // creature's own answer.
        _bus.EmitDnaSpoken(new Core.Framework.DnaSignature(tongue.SpeciesHash, spoken.SourceEntityId.ToString()));
        // The reply renders AS creature text in the dialogue band (same rect,
        // same direct-line dwell — the MC 11293 disjoint pins are untouched).
        _dialogue?.ShowText($"talk_{npcId}", $"{kind} {npcId} answers in {own.PayloadAsString()}: {codons} (bond {reply.CounterStrength:0.00})");
        GD.Print($"TALK: speak -> DnaLanguage.Speak(tongue {tongue} at {kind} {npcId}) -> Extract {own.PayloadAsString()} -> Counter {reply.PayloadAsString()} -> DnaSpoken + DialogueSystem.ShowText talk_{npcId}");
        GD.Print($"TALK REPLIED — {kind} {npcId} answers {codons} (bond {reply.CounterStrength:0.00}) in the dialogue band [BANDSTATE node='{_dialogue?.ActiveNode}' open={_dialogue?.IsOpen}]");
    }
}
