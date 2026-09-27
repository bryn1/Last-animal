# DA verdict — MC 1405 P3/P4 polish (N5/N6/N7, export dedup, docs) — HEAD ecd0c30

Adversarial refutation of the claims in `.audits/202609262100-n5n6n7/evidence.md`
(commits 6da97df, 9d80d20, 2b37b8b). Every refutation attempted below was tested
against the code, the spec text, or a live run on 2026-09-27.

## Refutation attempts and outcomes

1. **N5 ordering — "restore BEFORE zone re-entry matches travel semantics".**
   Attempted refutation: the zone spawn-set might field around the load-point
   position, not the restored one. REFUTED-BY-CODE: `SaveLoadController.Load()`
   calls `_restorePosition(...)` before `_enterZone(loaded.ZoneId)`
   (skeleton/world/SaveLoadController.cs, Load body), and `ApplySpawnSet` reads
   `Player?.GlobalPosition` as the ring origin (WorldDirector.cs ~line 291) —
   the same order `TravelToNextZone` uses (reposition, then EnterZone). The
   SpawnSet fields around the restored position. VERIFIED.

2. **N5 runtime coverage gap (finding, not a blocker).** The runtime proof was
   touched by 6da97df but only to declare `RuntimeIntegrationProof.Save.cs`
   fields `_playerPosBeforeSave` / `_playerPosMutated` — grep shows they are
   NEVER read or written anywhere. There is NO `LOAD_RESTORED_POSITION`
   assertion in the runtime proof; position restore is proven only by the pure
   unit round-trip (`SaveSystemTests.RoundTrip_PreservesPlayerPosition`, 16/16
   green this session) and by code reading. evidence.md's "save round-trip
   asserts position restoration" is true at unit level only. P3 hygiene/coverage
   gap (dead fields + missing runtime leg); does not break the playable-game DoD.

3. **N5 backward-compat — does HasPlayerPosition=false leave the player BROKEN?**
   No. A pre-N5 v2 save deserializes with `HasPlayerPosition=false` and the
   player stays at the live load-point position — i.e. exactly the pre-N5
   behaviour, which is where the player actually stands, not a corrupt state.
   v1 saves were ALREADY rejected by the schema guard before N5
   (`SaveSystem.CurrentVersion = 2` since MC 1344), so N5 adds no new rejection
   surface. Unit test `Load_OldSaveWithoutPosition_HasNoPositionFlag` covers it.
   VERIFIED.

4. **N5 edge (P4 observation).** `Save()` sets `HasPlayerPosition=true` with
   (0,0,0) when the Player node is absent (the WorldDirector lambda reads
   `Player?.GlobalPosition ?? Vector3.Zero`). A save from a no-player
   composition loaded into a with-player composition would teleport to origin.
   Unreachable in practice (composition is fixed per build); record, don't fix.

5. **N5 restored-position validity.** World content is static scene geometry
   plus a deterministic spawner (seed 7); a position valid at save is valid at
   load in the same binary. No teleport-into-wall runtime test exists, so this
   is INFERRED, not runtime-verified — no refutation found.

6. **N6 nullable _hud — dead tolerance, untested.** The shipped game always has
   the HUD: main.tscn carries the `UI` CanvasLayer and `BuildUi()` builds the
   Hud unconditionally when it exists; no proof or test exercises a no-UI
   composition's Save/Load. The feared "worse bug" (state restored but
   invisible) cannot occur — in a no-UI composition there is no meter to
   update. Harmless defensive tolerance, but it is UNTESTED tolerance, not a
   verified fix. Observation only.

7. **N7 — is removing EmotionState honest against the spec?** Spec text
   (PHASE0 C14, quoted in 890.15-save-progression-20260906.md): "Persists DNA
   counters (M02), emotion (M03), progression, zone", and the M11 evidence shows
   `"EmotionState": "Content"` in the saved JSON. The label field is now gone.
   However `EmotionalDepth.ReadHiddenState` is a TOTAL pure function of loyalty
   (thresholds 70/40/20 — skeleton/src/npc/EmotionalDepth.cs), EmpathyBook
   re-derives the label live, and CompanionLoyalty persists — the loaded game
   reproduces the byte-identical emotion label; no information is lost. The
   removal is honest, but it is a silent contract-text deviation: only a code
   comment records that C14's "emotion" leg is now carried derivationally; no
   doc or spec annotation states it. Minor docs gap, not a correctness break.

8. **Export dedup — byte-comparable behaviour?** `tools/package_artifact.sh` is
   a verbatim extraction of the old inline python block (compared against the
   9d80d20 diff: same zipfile write, same sorted walk, same two red-checks,
   same empty-zip guard; only the label is parameterised). Both callers pass
   the same argument shapes. VERIFIED by diff reading.

9. **Platform-scoped clean — can same-platform stale artifacts mask a broken
   re-export?** Empirically tested this session: planted stale
   `build/LastAnimal.x86_64`, `build/LastAnimal-linux-x86_64.zip` and
   `build/data_fake_linuxbsd_x86_64/`, ran `export_check.sh Linux` with a
   deliberately failing engine (`GODOT=/bin/false`). Output:
   `EXPORT_CHECK: FAIL: engine --build-solutions failed`, and `ls build/`
   afterwards showed ONLY `LastAnimal.exe` +
   `data_LastAnimalPreflight_windows_x86_64` — all three same-platform stale
   artifacts were removed BEFORE the export attempt. A broken re-export cannot
   hide behind a stale artifact of its own platform. VERIFIED (planted-bad run).

10. **Docs honesty.** The C12 paragraph is accurate: `animation_pipeline/`
    exists at the repo root as a standalone Godot mono project with
    `tools/retarget_bake.sh`, and the doc's "neither exists under those names"
    correctly refers to a ROOT-level `tools/retarget_bake.sh`. The assembly-name
    and smoke-limit paragraphs match the scripts. `grep -rn RetargetPipeline`
    over docs returns only the honest C12 paragraph — no doc claims a literal
    RetargetPipeline module. VERIFIED.

11. **Regression risk to the playable game.** Gates re-run this session on the
    combined tree: `dotnet build LastAnimalPreflight.csproj` exit 0;
    `ci/save_test.sh` GATE PASS (16/16, harness self-test went red);
    `ci/runtime_integration_test.sh` GATE PASS (positive green, all negative
    controls red with named markers). The cycle is hygiene + one real feature
    (N5); no regression found.

## Summary

Every refutation line attempted was answered by code, spec text, or an
executed check. The residue is: a dead-field/coverage gap in the runtime proof
for N5 (finding 2), an untested no-UI tolerance for N6 (finding 6), a P4
save-edge (finding 4), and an un-annotated C14 contract-text deviation for N7
(finding 7). None of these break the playable-game DoD; none is a P0/P1.

# JUDGED: 44136fa355b3678a1146ad16f7e8649e94fb4fc21fe77e8310c060f61caaff8a
# VERDICT: SHIP
