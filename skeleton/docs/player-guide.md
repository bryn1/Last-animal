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
| Invert Strike (skill) | `Q` |
| Mend (skill) | `R` |
| Skills panel (toggle) | `TAB` |
| Cycle selected follower | `C` |
| Break bond (selected follower) | `J` (with the Empathy Book open) |
| Calming Speak (skill) | `F` |
| Save game | `F5` |
| Load game | `F9` |

## The HUD

Six readouts (C13 contract extended by MC 3933, `src/ui/Hud.cs`): **Life**,
**Manna**, **DNA meter**, **Companion hearts**, **Skills** (what you have
learned) and the **ACTIVE quest** line. Life drops when enemies hit you. The DNA
meter tracks your language progress; the hearts show the MEAN loyalty across
your whole follower roster (a follower whose bond you broke can no longer be
paid or forgiven; it stays in the roster — counted in the mean at zero — until
you save and load, which drops it). **Manna is skill fuel**: every DNA
extraction (kill) grants +5 and
casting a skill spends it (`SkillState`, `src/skills/`). The gauge moves
during play, caps at 100, and a save restores the exact saved value on
load — nothing refills it for free. The skills and their keys are under
**Skills** below.

## Core loop

1. **Extract DNA.** Defeating a creature yields its DNA signature
   (`CombatSystem.OnKill` → `DnaSignature`, C10/C2). Every creature's
   signature is a counter sequence — a word in the game's language.
2. **Speak DNA.** You play signatures back with `DnaLanguage.Speak` and the
   ecosystem answers with `DnaLanguage.Counter` — it learns from what you have
   said and strikes back with adapted counters (`EcosystemAdaptation`,
   C5). The DNA meter shows how your language stacks up.
3. **Companions.** You can lead a roster of up to **three** followers
   (`CompanionRoster`, MC 3943): recruit a wild creature by interacting with it
   (`E`) and paying its first wage (`P`) — Calming Speak (`F`) can open that
   same offer at cast range, but the wage remains the only way to join. Each
   follower follows you (`CompanionFollowBody`), has its
   own needs (`CompanionNeeds`) and its own loyalty (`LoyaltyChanged` per
   follower; the HUD shows the mean). Loyalty moves with how you treat each
   one; neglect has consequences — and one follower's betrayal does not make
   the others leave. `C` cycles which follower is selected, `J` breaks the
   bond with the selected one.
4. **Betrayal and the Empathy Book.** The `BetrayalSystem` (C7) turns a
   neglected follower against you: when its loyalty reaches 0 the bond breaks,
   the betrayal damage lands and the C2 `Betrayal` signal fires. The Empathy
   Book (`src/ui/EmpathyPanel.cs`, logic in `src/empathy/EmpathyBook.cs`, C9)
   opens with `B` and lets you read the selected follower's hidden emotional
   state and route a resolution: **Forgive** or **Permanent break** (applies to
   the `C`-selected follower; a broken bond can never be paid or forgiven).
   Press `B` again to close it.
5. **Wages.** The `SalarySystem` (C6) is the economy pressure behind your
   choices, and paying is **your decision**: each follower's first wage is due
   after 20 s of following and a new one comes due every 30 s thereafter. While
   any follower's wage is due, press `P` to pay ALL due wages (+5 loyalty to
   each one paid). Ignore one and every full
   30 s it stays unpaid is one skipped cycle (−3 loyalty) — withhold wages
   long enough and loyalty erodes to 0 and the companion betrays you.
6. **Zones.** The world is zoned (`zones/`: meadow, canyon, ruins; bluetest and
   redtest are engine test zones, not meant as destinations). Entering a zone
   triggers `EcosystemSpawner.OnZoneEnter` (C15): the spawn list reacts to your
   spoken-DNA history, so the ecosystem you face is the one you taught.

## Skills

Three launch skills ride the Manna economy (the tunables — costs, heal,
per-kill gain — live in `src/skills/SkillState.cs`, the cap in
`src/combat/PlayerController.cs`):

- **Invert Strike** (`Q`, 10 Manna): your next melee hit deals triple
  damage; the arm is spent by that one hit.
- **Mend** (`R`, 15 Manna): restore 25 life, never past your maximum.
- **Calming Speak** (`F`, 12 Manna): the nearest castable wild creature inside
  cast reach (a good deal further than talk range) is calmed into a recruit
  offer that stands for 10 seconds (600 frames, counted down every frame — a
  save/load clears it, and an `E` offer outlives the window). A press that
  finds no castable wild, or cannot pay, spends NOTHING. `F` never recruits:
  the first wage (`P`) still does the joining.

You unlock skills by learning DNA: each extraction teaches the ecosystem's
language a counter, and the learned positions open the skills
(`src/dna/PlayerMutations.cs`) — the first extractions are enough. A press
while locked or short of Manna spends nothing.

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
automatically as you play; each completion starts the next. A quest's
reward is a short authored spoken beat rather than loot: when you
complete a quest, its beat is spoken in-world in the dialogue box (see
Dialogue above). Quest beats queue up and are read in order — a beat that
arrives while another is still on screen waits its turn (and if it lands
while a queued beat is mid-read, that beat simply re-queues for a full read
afterwards; nothing gets skipped). Every line — a quest beat or a spoken
NPC reply — is read for a few seconds, then the box closes on its own; the
next line opens it again. There is
no quest log screen yet: the arc happens without menus.

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
