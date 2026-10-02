# Last Animal — player guide (M14)

Last Animal is a single-player 3D ARPG about understanding creatures through a
spoken DNA language. You explore zones, fight or befriend what lives there,
raise a companion, and talk to the ecosystem in its own language.

## Controls

Verified against the `[input]` map in `project.godot`:

| Action | Keys |
|---|---|
| Move | `W`/`A`/`S`/`D` or arrow keys |
| Attack | `Space` or left mouse button |
| Interact | `E` |
| Travel to next zone | `T` |
| Pay companion wage | `P` |
| Empathy Book | `B` |
| Save game | `F5` |
| Load game | `F9` |

## The HUD

Four readouts (C13 contract, `src/ui/Hud.cs`): **Life**, **Manna**, **DNA
meter**, and **Companion hearts**. Life drops when enemies hit you. The DNA
meter tracks your language progress; companion hearts mirror your companion's
loyalty. **Manna is currently a static readout**: the gauge is drawn and
labelled, but no gameplay mechanic drives it in this build (`Hud.UpdateManna`
has no production caller), so it does not change during play.

## Core loop

1. **Extract DNA.** Defeating a creature yields its DNA signature
   (`CombatSystem.OnKill` → `DnaSignature`, C10/C2). Every creature's
   signature is a counter sequence — a word in the game's language.
2. **Speak DNA.** You play signatures back with `DnaLanguage.Speak` and the
   ecosystem answers with `DnaLanguage.Counter` — it learns from what you have
   said and strikes back with adapted counters (`EcosystemAdaptation`,
   C5). The DNA meter shows how your language stacks up.
3. **Companions.** A companion follows you (`CompanionFollowBody`), has needs
   (`CompanionNeeds`) and a loyalty score (`LoyaltyChanged` signal). Loyalty
   moves with how you treat it; neglect has consequences.
4. **Betrayal and the Empathy Book.** The `BetrayalSystem` (C7) turns a
   neglected companion against you: when loyalty reaches 0 the bond breaks,
   the betrayal damage lands and the C2 `Betrayal` signal fires. The Empathy
   Book (`src/ui/EmpathyPanel.cs`, logic in `src/empathy/EmpathyBook.cs`, C9)
   opens with `B` and lets you read a companion's hidden emotional state and
   route a resolution: **Forgive** or **Permanent break**. Press `B` again to
   close it.
5. **Wages.** The `SalarySystem` (C6) is the economy pressure behind your
   choices, and paying is **your decision**: the first wage is due after 20 s
   of companionship and a new one comes due every 30 s thereafter. While a
   wage is due, press `P` to pay it (+5 loyalty). Ignore it and every full
   30 s it stays unpaid is one skipped cycle (−3 loyalty) — withhold wages
   long enough and loyalty erodes to 0 and the companion betrays you.
6. **Zones.** The world is zoned (`zones/`: meadow, canyon, ruins; bluetest and
   redtest are engine test zones, not meant as destinations). Entering a zone
   triggers `EcosystemSpawner.OnZoneEnter` (C15): the spawn list reacts to your
   spoken-DNA history, so the ecosystem you face is the one you taught.

## Dialogue

NPC dialogue is surfaced through the on-screen dialogue box
(`DialogueSystem.Show(nodeId)`, C13). The text lives in a plain authored
data table (`src/story/DialogueTable.cs`: node id -> text), which the
composition root injects into the box — it is no longer hardcoded in the
view. A node id the table does not author (for example the `npc_<id>` ids
the interact trigger produces) still shows a short neutral line, never a
blank box.

## Quests

A quiet five-beat quest arc runs on top of the same systems
(`src/story/QuestTable.cs`, advanced by `src/story/QuestLog.cs`): **Arrival**
(enter the meadow) → **Answer in Tongue** (speak DNA to a creature with
`E`) → **Bread Before Bonds** (pay a due wage with `P`) → **Blood Teaches**
(extract DNA from four creatures) → **The Watcher Falls** (kill the boss the
zone fields once your spoken DNA crosses its threshold). Quests advance
automatically as you play; each completion starts the next, and each reward
is a short spoken beat rather than loot — the beats are authored nodes in
the dialogue table. There is no quest log screen yet: the arc happens
without menus.

## Saving and loading

There is **no autosave**. Saving is manual: press `F5` to save and `F9` to
load. Both write/read `user://savegame.json` (see [install.md](install.md)
for the on-disk location). A save persists your DNA counters, companion
identity and loyalty, the DNA meter, progression and the current zone (C14).
Loading restores those values and re-enters the saved zone with a fresh
enemy ring — note that load does **not** reposition your player character
(zone travel with `T` does); you load wherever you were standing. The saved
emotion label is written but is not restored on load.

---
Claim labels: controls, HUD, systems, save/load behaviour and save path are
VERIFIED against the source files named above. The moment-to-moment feel
(difficulty, pacing) is UNVERIFIED — no human has played the packaged Windows
build yet (wine 9.0 IS installed on the build host vm105, but the packaged
`LastAnimal.exe` crashes at startup under it — null-pointer read, no log line,
both renderers and `--headless`; likeliest the bundled .NET 8 host under Wine
9.0 — see [build-and-run.md](build-and-run.md)).
