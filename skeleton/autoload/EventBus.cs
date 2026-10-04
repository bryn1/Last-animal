using Godot;
using LastAnimal.Core.Framework;

// Last Animal — M01 core-framework (MC 839.3, teddy, 2026-09-01).
//
// EventBus: the single global signal bus (C2). Autoload (I1: orchestration only).
// Modules NEVER reference each other directly (I4); they publish/subscribe here.
// One signal per contract line — the framework carries the carriers, the
// gameplay modules interpret them.
//
// M09 repair (MC 839.4, henrik, 2026-09-01) — C# signal idiom:
// Godot 4.7.2's C# code generator rejects [Signal] parameters that are user
// types (GD0202: "parameter ... is not supported" — only built-in value types,
// strings, Node references, Variant and Array<T> are allowed). It ALSO
// generates a nested static class `SignalName` on any node that declares
// [Signal]s, which collided with the hand-written `public static class
// SignalName` below (CS0102 + CS0713).
//
// Repair, keeping the C2 contract surface (the same five events, same
// argument order, same stable string names — C2 is unchanged; this is a C#
// idiom fix, not a contract change):
//   1. Each record carrier is passed as a `string` Id in the C# signal
//      signature. The record stays the canonical carrier in C# code: the
//      emit helpers below keep taking the record and extract its Id for the
//      signal, and subscribers can wrap the Id back into a record (Empty
//      factory included) — the event's payload is the same data, only its
//      wire form on the C# signal is the identifier string Godot requires.
//   2. The hand-written `SignalName` nested class was REMOVED: the source
//      generator now provides it. All usages were updated to the generated
//      members. (Verified by a compile of the whole project after the change.)
namespace LastAnimal.Core;

public partial class EventBus : Node
{
    // --- C2: global signals (single pub/sub) --------------------------------
    // [signal] DnaExtracted(String)          — a DNA signature was extracted (M02).
    // [signal] DnaSpoken(String)             — a learned signature was spoken (M02).
    // [signal] LoyaltyChanged(String, int)   — companion loyalty moved (M03).
    // [signal] Betrayal(String, String)      — a companion turned (M03).
    // [signal] EcosystemAdapted(String)      — ecosystem adapted to the player (C5).
    // EmpathyBookOpened() arrives with M04/M10 (not in the M01 C2 set).
    // (String = the carrier record's Id, see file header.)

    [Signal] public delegate void DnaExtractedEventHandler(string signature);
    [Signal] public delegate void DnaSpokenEventHandler(string signature);
    [Signal] public delegate void LoyaltyChangedEventHandler(string companion, int loyalty);
    [Signal] public delegate void BetrayalEventHandler(string companion, string target);
    [Signal] public delegate void EcosystemAdaptedEventHandler(string mutation);

    // --- M04 (C2): EmpathyBookOpened() — added with M04/M10 (per PHASE0 C2 note;
    //     "EmpathyBookOpened() arrives with M04/M10 (not in the M01 C2 set)"). ----
    // The book opening is a no-payload signal; the entries it will show come from
    // the C9 pure-logic side (EmpathyBook.Query) fed by the M10 EmpathyPanel.
    [Signal] public delegate void EmpathyBookOpenedEventHandler();

    // --- MC 3904 stage 2c (plan §B "2c only"): THE ONE batched bus edit.
    //     Exactly five quest-core signals, all string-Id per GD0202 (the same
    //     wire idiom as above: carriers QuestId / SkillId in FrameworkTypes).
    //     2e/2f/2g CONSUME these and never edit this file. SkillUsed is
    //     emitted/consumed by 2e (skill core) — 2c only lands the batch. ------
    // [signal] QuestStarted(String)   — a quest row went Active.
    // [signal] QuestObjective(String) — an active row's objective was met.
    // [signal] QuestCompleted(String) — a met row was completed (reward beat).
    // [signal] WagePaid(String)       — a wage settle landed (rides the existing
    //                                   Needing->Following settle); Id = the
    //                                   quest the settle serves.
    // [signal] SkillUsed(String)      — a player skill fired (2e).
    [Signal] public delegate void QuestStartedEventHandler(string quest);
    [Signal] public delegate void QuestObjectiveEventHandler(string quest);
    [Signal] public delegate void QuestCompletedEventHandler(string quest);
    [Signal] public delegate void WagePaidEventHandler(string quest);
    [Signal] public delegate void SkillUsedEventHandler(string skill);

    // --- MC 10098 Inc-3 S0: consumer-facing presentation emits (additive). ---
    // Emitted by the per-frame EDGE-DETECT poll in the TickUi seam
    // (world/WorldDirector.Ui.cs) — DialogueSystem/queue code stays untouched
    // (F3). One emit per edge: DialogueShown ""→node, DialogueClosed node→""
    // (payload = the node leaving screen, so a direct node→node swap reads as
    // Closed(old)+Shown(new)); PlayerHurt carries the health AFTER the
    // decrease; BossFallen carries the dead boss's EntityId (string wire
    // idiom, GD0202) and fires on the tracked BossActor's IsDead edge ONLY
    // (F-2: a nulled _boss on zone exit is NOT a death — DA-c3).
    // [signal] DialogueShown(String)   — the dialogue box opened a node.
    // [signal] DialogueClosed(String)  — the shown node left the screen.
    // [signal] PlayerHurt(Int)         — player health decreased (new Health).
    // [signal] BossFallen(String)      — the zone boss's IsDead turned true.
    [Signal] public delegate void DialogueShownEventHandler(string node);
    [Signal] public delegate void DialogueClosedEventHandler(string node);
    [Signal] public delegate void PlayerHurtEventHandler(int health);
    [Signal] public delegate void BossFallenEventHandler(string boss);

    // --- C2: publish surface (thin, no logic) --------------------------------

    public void EmitDnaExtracted(DnaSignature signature)
        => EmitSignal(SignalName.DnaExtracted, signature.Id);

    public void EmitDnaSpoken(DnaSignature signature)
        => EmitSignal(SignalName.DnaSpoken, signature.Id);

    public void EmitLoyaltyChanged(CompanionId companion, int loyalty)
        => EmitSignal(SignalName.LoyaltyChanged, companion.Id, loyalty);

    public void EmitBetrayal(CompanionId companion, TargetId target)
        => EmitSignal(SignalName.Betrayal, companion.Id, target.Id);

    public void EmitEcosystemAdapted(MutationId mutation)
        => EmitSignal(SignalName.EcosystemAdapted, mutation.Id);

    // --- M04: EmpathyBookOpened() emit (C2) --------------------------------
    public void EmitEmpathyBookOpened()
        => EmitSignal(SignalName.EmpathyBookOpened);

    // --- MC 3904 stage 2c: quest-batch emit surface (see signals above) ----
    public void EmitQuestStarted(QuestId quest)
        => EmitSignal(SignalName.QuestStarted, quest.Id);

    public void EmitQuestObjective(QuestId quest)
        => EmitSignal(SignalName.QuestObjective, quest.Id);

    public void EmitQuestCompleted(QuestId quest)
        => EmitSignal(SignalName.QuestCompleted, quest.Id);

    public void EmitWagePaid(QuestId quest)
        => EmitSignal(SignalName.WagePaid, quest.Id);

    public void EmitSkillUsed(SkillId skill)
        => EmitSignal(SignalName.SkillUsed, skill.Id);

    // --- MC 10098 Inc-3 S0: presentation emit surface (see signals above) ----
    public void EmitDialogueShown(string node)
        => EmitSignal(SignalName.DialogueShown, node);

    public void EmitDialogueClosed(string node)
        => EmitSignal(SignalName.DialogueClosed, node);

    public void EmitPlayerHurt(int health)
        => EmitSignal(SignalName.PlayerHurt, health);

    public void EmitBossFallen(string boss)
        => EmitSignal(SignalName.BossFallen, boss);
}
