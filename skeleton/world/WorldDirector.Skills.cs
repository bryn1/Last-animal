using Godot;
using LastAnimal.Companion;
using LastAnimal.Core.Framework;
using LastAnimal.Dna;
using LastAnimal.Skills;

// Last Animal — stage 2e skill wiring (MC 3912, code, 2026-10-02).
//
// Partial-class half of WorldDirector (one class = still THE single
// composition root, plan §B): constructs the engine-free SkillState over the
// ONE director-owned PlayerController, injects the v3 Manna save seams into
// SaveLoadController, rides the kill-manna gain on the EXISTING
// OnDnaExtracted handler site (via the bus forward that handler emits — NO
// second kill hook exists here), polls the skill input arms (MC 10031 adds
// the third: Calming Speak + its frame-bounded calm-window sweep), and wraps
// the melee damage VALUE at the single _combat.DealDamage call site (root
// ConsumeSkillDamage hunk) so an armed Invert Strike multiplies damage
// THROUGH the one kill path (plan §B 2e grep leg stays exactly 1).
//
// Unlock reads are LIVE per use: PlayerMutations.Unlocked(ModelPlayerDna
// (_spokenDna)) — the same consensus the ecosystem learns from and the only
// DNA part the save round-trips (plan §G D2/D4: no cache, no save field).
// Successful uses map onto the batched SkillUsed bus signal (2c landed the
// batch; 2e only CONSUMES it — EventBus/FrameworkTypes untouched).
//
// Gate seam (design §4.2 idiom, same honest no-op as SetQuestHooksEnabled):
// SetSkillActionsEnabled — one bool; every hook re-checks it at DELIVERY,
// so flipping it after _Ready stalls the whole 2e economy (the skill_neg
// runtime mode proves it).
namespace LastAnimal.World;

public partial class WorldDirector
{
    private SkillState _skills = null!;
    private bool _skillHooksEnabled = true;

    /// <summary>Read-only surface the runtime proofs read (proof-only, no logic).</summary>
    public SkillState Skills => _skills;

    /// <summary>
    /// Root seam, called from _Ready right after InitStory (the SaveLoad-
    /// Controller already exists at that point). Constructs the skill state,
    /// wires the Manna save seams, subscribes the kill-manna rider to the
    /// existing DnaExtracted bus forward and binds the HUD's dead Manna seam
    /// (UpdateManna was never called in production before 2e — this replaces
    /// it, plan §B 2e).
    /// </summary>
    private void InitSkills()
    {
        _skills = new SkillState(_player);
        _saveLoad.MannaWrite = () => _player.Manna;
        // Load restores the saved value EXACTLY (owner ruling D3: no
        // cross-load refill); the controller's setter clamps at the cap.
        _saveLoad.MannaRestore = m =>
        {
            _player.Manna = m;
            if (_hud != null) _hud.UpdateManna(_player.Manna);
        };
        if (_skillHooksEnabled)
            _bus.DnaExtracted += _ => OnSkillKill();
        if (_hud != null) _hud.UpdateManna(_player.Manna);
    }

    /// <summary>Kill-manna rider: attached to the EXISTING OnDnaExtracted
    /// handler site's bus forward — one payment per extraction, capped.</summary>
    private void OnSkillKill()
    {
        if (!_skillHooksEnabled) return;   // honest no-op even after a late flip
        _skills.OnKill();
        if (_hud != null) _hud.UpdateManna(_player.Manna);
    }

    /// <summary>
    /// Input arms (root _Process seam, one line): skill_1 = Invert Strike
    /// (arm), skill_2 = Mend (heal), skill_3 = Calming Speak (MC 10031 — the
    /// third press arm; skill_3's input-map binding was reserved at 2e and is
    /// now live, map ZERO-diff). The unlock verdict is read LIVE at press
    /// time; a locked or shortfunded press spends NOTHING.
    /// </summary>
    private void PollSkillActions()
    {
        if (!_skillHooksEnabled || _skills == null) return;
        TickCalmWindows();   // MC 10031: the sweep runs BEFORE the press chain (§2 frame contract)
        if (Input.IsActionJustPressed("skill_1"))
        {
            var unlocked = PlayerMutations.Unlocked(
                EcosystemAdaptation.ModelPlayerDna(_spokenDna));
            if (_skills.TryInvertStrike(unlocked.InvertStrike))
            {
                _bus.EmitSkillUsed(new SkillId(PlayerMutations.InvertStrikeId));
                GD.Print($"W3: skill_1 -> Invert Strike ARMED (Manna={_player.Manna})");
            }
        }
        else if (Input.IsActionJustPressed("skill_2"))
        {
            var unlocked = PlayerMutations.Unlocked(
                EcosystemAdaptation.ModelPlayerDna(_spokenDna));
            if (_skills.TryMend(unlocked.Mend))
            {
                _bus.EmitSkillUsed(new SkillId(PlayerMutations.MendId));
                if (_hud != null) _hud.UpdateLife(_player.Health);
                GD.Print($"W3: skill_2 -> Mend (HP={_player.Health}, Manna={_player.Manna})");
            }
        }
        else if (Input.IsActionJustPressed("skill_3"))
        {
            TryCastCalmingSpeak();   // MC 10031: the third launch skill (Q wins over F on one frame — chain order)
        }
    }

    // Calming Speak tunables (MC 10031 design §2): the WINDOW is frame
    // arithmetic on one int (zero wallclock, zero Timer); the range mirrors
    // the reach of the offer mechanic it bridges (talk range 4.5, a cast
    // reaches a bit further — the skill is the reach option, never the join).
    private const int CalmingSpeakWindowFrames = 600;   // 600 frames @ 60 fps = 10 s
    private const float CalmingSpeakRange = 9.0f;

    /// <summary>
    /// skill_3 = Calming Speak (MC 10031). The rule machine, EXACT order
    /// (design §1.2): gate seam (PollSkillActions guard above) → unlock read
    /// LIVE → TARGET SCAN BEFORE ANY SPEND → the single TrySpend path → on
    /// success, in this order: open offer → set window AFTER the open (the
    /// open retires a prior window) → SkillUsed marker → print. Join NEVER
    /// happens here: pay_wage → TryRecruitOfferedWild stays the SOLE join
    /// authority (PLAN:118 pay-first, RATIFIED). A refusal NEVER spends.
    /// </summary>
    private void TryCastCalmingSpeak()
    {
        var unlocked = PlayerMutations.Unlocked(
            EcosystemAdaptation.ModelPlayerDna(_spokenDna));
        if (!unlocked.CalmingSpeak) return;   // step 2: locked spends nothing (2e idiom)

        // Step 3: the scan runs BEFORE any spend (DA-c1 P1-1). A fresh wild
        // or one already inside a window is castable; an E-STANDING offer
        // (offered, window 0) is NOT — F must never downgrade E's standing.
        CompanionFollowBody? target = null;
        float best = CalmingSpeakRange;
        Vector3 ppos = Player!.GlobalPosition;
        foreach (var w in _wild)
        {
            if (w.RecruitOffered && w.CalmedWindowFrames <= 0) continue;
            float d = (w.GlobalPosition - ppos).Length();
            if (d <= best) { best = d; target = w; }
        }
        if (target == null)
        {
            GD.Print("W3: skill_3 -> Calming Speak REFUSED — no castable target (nothing spent)");
            return;
        }

        // Step 4: the ONE spend path; a short balance refuses and spends zero.
        if (!_skills.TryCalmingSpeak(unlocked.CalmingSpeak))
        {
            GD.Print($"W3: skill_3 -> Calming Speak REFUSED — short balance (Manna={_player.Manna}, nothing spent)");
            return;
        }

        // Step 5: open → window AFTER the open → marker → print. The offer
        // rides the SAME RecruitOffered flag E opens; the chime + panel ride
        // the existing SkillUsed subscription (zero new assets/signals).
        OpenRecruitOffer(target);
        target.CalmedWindowFrames = CalmingSpeakWindowFrames;
        _bus.EmitSkillUsed(new SkillId(PlayerMutations.CalmingSpeakId));
        GD.Print($"W3: skill_3 -> Calming Speak on wild id {target.EntityId} (Manna={_player.Manna}, window {CalmingSpeakWindowFrames}f)");
    }

    /// <summary>Calm-window decay sweep (design §1.1): every _Process frame,
    /// BEFORE the press chain, each open window decays by exactly 1; at the
    /// frame it reaches 0 the offer is withdrawn through the ONE closer.
    /// Gate seam: the sweep sits behind the _skillHooksEnabled guard — a
    /// mid-window flip FREEZES windows exactly like it stalls the 2e economy
    /// (skill_neg semantics, §2). E-standing offers (window 0) are untouched
    /// by construction.</summary>
    private void TickCalmWindows()
    {
        foreach (var w in _wild)
        {
            if (w.CalmedWindowFrames <= 0) continue;
            w.CalmedWindowFrames--;
            if (w.CalmedWindowFrames == 0)
            {
                CloseRecruitOffer(w);
                GD.Print($"ROSTER: calm window EXPIRED on wild id {w.EntityId} — offer withdrawn");
            }
        }
    }

    /// <summary>
    /// The armed-damage wrap (root TryAttack hunk at the ONE DealDamage
    /// call site): rides the multiplier through the damage VALUE, so the
    /// kill path stays ONE. Without an arm the base value passes through
    /// untouched; the gate seam off means base, always.
    /// </summary>
    private int ConsumeSkillDamage(int baseDamage)
    {
        if (!_skillHooksEnabled || _skills == null) return baseDamage;
        return _skills.ConsumeArmedDamage(baseDamage);
    }

    /// <summary>Gate seam: disable the whole 2e skill economy (skill_neg
    /// negative control). One-line bool guard, no gameplay logic (design
    /// §4.2); every hook re-checks it at delivery.</summary>
    public void SetSkillActionsEnabled(bool enabled) => _skillHooksEnabled = enabled;
}
