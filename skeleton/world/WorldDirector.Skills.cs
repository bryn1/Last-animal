using Godot;
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
// second kill hook exists here), polls the two skill input arms, and wraps
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
    /// (arm), skill_2 = Mend (heal). The unlock verdict is read LIVE at
    /// press time; a locked or shortfunded press spends NOTHING. skill_3 is
    /// reserved in the input map (third launch skill = Calming Speak, a
    /// follow-up card after 2g per owner ruling D4) — no handler here by
    /// design.
    /// </summary>
    private void PollSkillActions()
    {
        if (!_skillHooksEnabled || _skills == null) return;
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
