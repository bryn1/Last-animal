# D2-direction — VISUAL DIRECTION PROPOSAL v3 (MC 10405 / display 10026.28)

Status: PLAN-ONLY (2026-10-09). Cycle-3 repair of DA-c2 FIX (`DA-verdict-c2.md` §2, N-1..N-8; both
verdicts under `.audits/202610092116-60ddaeb8/out/`), listed by id in §7. v1/v2 untouched; direction,
structure and all card ids unchanged.
Owner directive (verbatim, 2026-10-09): "this is not the looks of a AAA game, i also seem to fall
through the floor.  Diablo 2 is a good start for what i'm after"
Brief: `.tmp/d2-direction-brief.md` (+ C' addendum + MC 10365 R2 guardrail addendum — binding).
Mood reference (read-only, owner-supplied): `.audits/20261009-0615-ownerplaytest/out/ref-diablo2.png`;
clean-room targets (fleet GPU, originals): `la-style-meadow-d2.png`, `la-style-ruins-d2.png` (same dir).
Compliance: §6 (R2 — genre mood only, original content, no RE).

## 0. Stage numbers and the W5 relationship (read before the cards)

- **S22–S26 SHIPPED** (Inc-4 W4 close, LEDGER:1256). **S27–S37 RESERVED (not shipped)** by
  `.tmp/w5-proposal-v0.md` REV 1 — PENDING OWNER RATIFICATION. This proposal allocates **S38+**; stage
  numbers are MODE-SCOPED (ARCHITECTURE §6), the filing orchestrator re-confirms against the tree.
- **S39↔S27 and S43↔S28 branch on the W5 PICK, not on ratification (DA P1-1; N-2 fix):** ratification
  means "the owner picks ONE wave (or none)" (w5 :261) and **S27/S28 are W5-A-only content** — a
  ratified W5-B or W5-C ships NEITHER. Therefore: owner picks **W5-A → S39 MERGES INTO S27 and S43
  INTO S28** (the d2 grade/density cards ride S27's mount; this doc's rows retire, no second
  implementation). **Any other outcome (W5-B, W5-C, or no pick) → S39 runs standalone at FULL scope**
  (hollow.tscn + hollow.png AUTHORING, DayNight stale-binding fix, WorldDirector new-partial route —
  §1/§5) and **S43 standalone carrying S28's step-0-probe idiom**. Mechanisms ship ONCE either way;
  the filing orchestrator resolves the branch from the literal W5 ruling.
- **S34 (W5 floor rider — A7/A9/save-eco registered defects) is DISJOINT from S38 (terrain):** both
  are ruling-free registered-defect work and MAY RUN CONCURRENTLY (w5 :46-48, :263-264).

## 1. Where we actually are (VERIFIED this session, tip b107cd5; re-checked against DA c1/c2 disk cites)

- **Renderer (DA P1-3, W5-DA F-7 wording adopted):** the project DECLARES Forward+
  (`project.godot:24`, since the file's introducing commit 275302b; repo root 2751839e) but the
  CI/capture runtime **EFFECTIVELY RUNS GL Compatibility** (`zones/zone.gd:13-17` VisibilityRange
  "invalid assignment" under the CI Xvfb fallback; `WorldDirector.DayNight.cs:18-21` fog washout pin,
  same runtime). The GL-Compat comments are LIVE constraint notes, NOT stale — this proposal proposes
  NO deletion of them. Consequence, with the W5-S28 precedent: **every capture-based done (S40–S43,
  S47, S48) opens with a step-0 feasibility probe at the CI-EFFECTIVE renderer + an explicit CUT rule
  if the primitive renders blank there**; a config flip would not change what CI captures.
- **Environment today (per zone .tscn):** ProceduralSkyMaterial sky, plain env fog (`fog_density 0.0055`,
  `fog_sky_affect 0.0` pinned), glow 0.4, adjustment contrast 1.08 / saturation **1.12 (pushing the
  wrong way for D2 mood)**, DirectionalLight3D 1.5 warm. Day-night writes light energy/color + ambient
  per keyframe table; **fog is NOT a keyframe channel** (`WorldDirector.DayNight.cs:83-84`).
- A10 (LEDGER:104, "zone environments never change"): `main.tscn` instantiates **only meadow.tscn**;
  `TravelToNextZone` (`WorldDirector.cs:353`) swaps spawn sets + player position but mounts no zone
  scene. canyon/ruins .tscn exist and never appear, carrying `Camera3D current=true` (canyon :106,
  ruins :107) — the dual-current bug class.
- **Floor / fall-through (DA P1-2 mechanism, N-4/N-7-corrected):** `HeightMapShape3D.map_width/
  map_depth` are set to the PNG **pixel counts** (`terrain_builder.gd:56-58`) — 1 m/cell, spanning
  `(map_width−1)` m (4.7 engine source, fetched by DA-c2) — so meadow's collision floor is **63 m**
  while the visual floor spans `_w*h_scale` = **96 m** (`:115`; `h_scale=1.5` at meadow.tscn:64;
  canyon :75 = 2.0, ruins :76 = 1.6): **the outer ring beyond ~31.5 m from centre is visible ground
  with NO collision = THE fall-through-floor cause** (owner's `owner-2` screenshot). The file's header
  claim "collision and rendering provably agree" (:6-7) is FALSE for all three zones (every shipped
  h_scale ≠ 1). Secondary: y-offset `v_scale*0.5` (**:72**). **256-vs-255:** canyon/ruins PNGs are
  256×256 (headers read); HeightMapShape3D behavior at 256 cells is an **INFERRED open question** —
  4.7 engine source shows no documented maximum and no clamp (v2's "documented 255 max" attribution
  DROPPED per N-4), but physics-server behavior at 256 is unprobed, so the S38 probe stands either
  way. Weak checks, not premises: tunneling (5 m/s ≈ 8 cm/tick, `PlayerController.cs:34`), spawn-under
  (boots y=3, `main.tscn:48`). No kill-plane/respawn exists (repo-wide grep) → falls are unrecoverable.
- Decor: hand-placed instances (6 trees in meadow) at hardcoded y −0.05..0.35, **ignoring the
  heightfield** → floating/sinking props. Ground has **no textures at all**: one flat green albedo on
  a code-built ArrayMesh (`terrain_builder.gd:154-157`). Zero `.gdshader`, zero particle nodes. CC0
  Kenney packs only (`resources/assets_packs/LICENSES.md`).
- Camera: `world/FollowCamera.cs` follow-offset (8,6,8) + lerp; boss framing / kill-pulse / shake tuned
  to that rig. **Combat is nearest-in-range radial** (`WorldDirector.cs:368-387`, no mouse aim).
  **Save carries no camera fields** (`src/save/GameState.cs`). HUD is CanvasLayer (screen-space).
  Mount hazard: the DayNight driver binds `World/Meadow/Environment/*` one-shot (:231-240) —
  mount/unmount frees them (stale-binding hazard, W5-S27, imported here). `WorldDirector.cs` is 509
  lines — new logic routes to a new partial (400-l ceiling), per W5.
- Perf floor: S21 close measured ruins 39.267 vs 32.875 ms (**+19.4% — NAMED, NOT RULED**: LEDGER:1258
  same line says it sits INSIDE S11's pooled render-identical control bound (11.317 ms, shared llvmpipe
  box) and an adjacent **paired A/A re-run is the owed instrument** before the ruins row is ruled;
  "NO material regression CLAIMED"). Canyon +6.4%. `tools/perf_probe.sh` exists (PERF leg).
- D-1 (`.tmp/10081-inc3-plan-v3.md:281`, verbatim): "D-1 REUSE-ONLY ART/AUDIO: no new assets inside
  Inc-3 … an owner-funded sound-pack card may open at Inc-3 close." Fleet-generated art has no
  precedent ruling — hence the C' ruling.

**Honest baseline:** the D2 look is baked 2D pre-rendered environments at big-studio scale (the
"300+ artists" figure is brief-borne, not a disk fact — public record points to a studio of dozens over
~3 years; INFERRED). We are realtime 3D on CC0 prims. A and C' buy GENRE MOOD (angle, palette
density, lighting, painterly surfaces) — not parity with a prerendered classic.

## 2. OPTIONS

### A — Painterly zero-new-asset pass (existing 3D, existing CC0 packs)
Levers: per-zone env re-grade (dark ambient, desaturated, `adjustment_saturation` < 1, contrast up),
warm LOCAL lights on torch/brazier props (castle kit), fog — plain env fog certain, volumetric only
if a probe proves it at the target runtime (§1 probe idiom), height-snapped prop density from existing
kits, first ground materials (`.gdshader` or texture, §1: the ground is FLAT ALBEDO today),
GPUParticles3D ambient dust/embers (probe-gated), day-night tables re-keyed gloomy, zone-scene
mounting (S39/S27 branch, §0).
Buys: biggest mood shift per hour; dark, fog-lit, cluttered, genre-readable. Does NOT buy:
painted-surface fidelity, prerendered crispness, new silhouettes.
Effort ~5–7 cards, 0 money. Risk: CI-renderer capture limits (probe idiom is the mitigation); ruins
perf row; taste gate (capture-pair evidence, owner eyes, S13 idiom).
**S38 is RULING-FREE** — registered owner-reported P0 defect work, S34 floor-rider idiom; starts
under every W5 outcome without waiting for any yes/no.
**OWNER RULING NEEDED (A):** "proceed with the zero-new-asset painterly pass S39–S44 (S40–S44 under a
W5-A pick)" — YES recommended.

### B — Fixed isometric (D2-style) camera ruling
Feasible, VERIFIED: combat nearest-in-range (nothing to aim-break), saves carry no camera state, UI screen-space. Breaks/retunes: (1) canyon/ruins `current=true` dual-camera bug (rides S39/S27 mount); (2) FollowCamera juice retune; (3) terrain occlusion — fixed rig needs clearance or camera collision; (4) readability — A's density/grades must be authored for the fixed angle (do B with/after A); (5) perf roughly neutral. Orthographic = optional flag, not rewrite. Effort: 1 card + playtest ruling. Risk: feel regression (owner-only call).
**OWNER RULING NEEDED (B):** "fixed isometric as THE camera — yes / no / try behind a toggle" —
recommendation: toggle card first (cheap, reversible), follow-cam default until he plays it.

### C — Funded external art path (prerendered / isometric rework)
A prerendered-D2 rework is a **rewrite** class: new camera+render model, per-zone baked backplate pipeline, per-direction sprite sets (the existing animation pipeline is realtime-3D only), big-studio resource asymmetry (§1 baseline). Even the humane version (commissioned stylized 3D pack replacing Kenney) is owner money under D-1. **No quote on file — C=YES opens an estimating step first** (market rows, scope sheet), it does not open a build. Buys: the only path to new silhouettes/quality ceiling. Does NOT buy: mood (A/C' buy mood now); a funded pack without A's lighting still looks like bright toys in new clothes.
**OWNER RULING NEEDED (C):** "fund an external art card: yes / no" — recommendation: NO until A+C'
land and he re-judges screenshots.

### C' — Fleet-GPU imagegen 2D layer (zero funding, Qwen-Image on the vm100 3090)
Two clean-room targets exist (bible anchors). Deliverable classes: (1) **style bible + per-zone
targets** (foundation); (2) **tileable ground/wall textures** — seam post-pass + tiling captures;
pairs A, strengthened disproportionately because the ground has NO textures today (§1); (3) **portraits
/ item icons / UI chrome** — pure 2D, additive; (4) **VFX sprites** (dust, embers, portal sheets —
feeds the probe-gated particle stage); (5) **painted backplates / skybox strips** — replaces the
ProceduralSkyMaterial horizon, cheapest C-feel in the repo; pairs A, PARTIALLY REPLACES C for framing.
Limits (binding): NO 3D geometry, NO animation from this model; consistency drift → per-zone style
refs + locked palette + seeds; GPU contends with the fleet verifier (window closes idle — batch
off-peak); provenance: full prompt+seed sidecar beside EVERY shipping asset + LICENSES.md row.
Effort 3 cards, 0 money. Risk: drift/uncanny faces; review burden; GPU scheduling.
**OWNER RULING NEEDED (C'):** "fleet-self-generated art counts as allowed in-house work under D-1
(cost 0, no funding card) — yes / no" — recommendation: YES, with the provenance rule as condition.

## 3. HYBRID RECOMMENDATION

**Wave V0 (P0, now, ruling-free):** S38 floor fix (the game must not kill the owner by falling).
**Wave V1:** S39/S27 mount, then the **S44 perf-gate extension (AFTER S39 — the mount changes what is
measured — and BEFORE the grade wave)**, then S40 grade + S41 ground materials (file races, §5).
**Wave V2:** S42 density, S43 probe-gated particles, owner screenshot review.
**Parallel from day 1 (once C' ruling lands):** S46 bible → S47/S48 (generation parallel; S47's
integration sequenced, §5). **B stays a separate ruling**, best at V1 review or via toggle S45 early.
**C stays closed** unless he opens it after V2 (then: estimating step first). Never waits on C;
C'=NO still sequences V0–V2.

## 4. OPTIONS TABLE

| Option | Visual gain (honest) | Effort | Risk | What it does NOT buy |
|---|---|---|---|---|
| A painterly pass | Large: dark fog-lit genre mood, clutter, color discipline | ~6 cards, 0 kr | CI-EFFECTIVE GL-Compat capture limits (probe idiom); ruins perf row (named-not-ruled); taste gate | painted surfaces; new shapes; prerendered crispness |
| B fixed iso cam | Medium: framing/readability of the genre | 1 card + ruling | feel regression; juice retune; zone-camera bug | any pixel of LOOK (reframes, doesn't repaint) |
| C funded art | Ceiling: new silhouettes, quality floor-lift | months + owner money (D-1); NO QUOTE ON FILE — C=YES = estimating step first | rewrite-class integration; capture-baseline churn | mood without A; speed; zero-risk |
| C' imagegen 2D | Medium-large: first-ever ground textures, icons, sky strips, VFX | 3 cards, 0 kr | consistency drift; GPU contention; review burden | 3D geometry, animation, moving characters, parity |

## 5. STAGE CARDS (ids per §0; W5-pick branch per §0 applies to S39/S43)

Common done-shape: build 0 errors, named battery leg or capture pair as evidence, LEDGER row in the
same commit, `.audits/<run>/EVIDENCE.md` ending `VERIFY_EXIT=0`. Capture dones open with the §1 step-0
renderer probe (+ CUT rule if blank at CI-effective renderer, S28 precedent).

- **S38 — Floor-trust + kill-plane (P0, RULING-FREE).** Scope: fix the **h_scale collision-footprint
  bug** (96 m visual over **63 m** collision, dead ring from ~31.5 m — re-scale `map_width/depth` by
  h_scale or scale the StaticBody, pick one), re-check the `v_scale*0.5` y-offset (`:72`), PROBE
  HeightMapShape3D at **256×256** canyon/ruins maps (INFERRED question, §1; downsample/clamp if it
  bites); tunneling/spawn-under as named checks. Add respawn-on-below-plane. Proof idiom (N-6):
  FLOOR_TRUST = **headless physics-query marker leg** (shape-cast/point-query at grid points against
  the live collision shape vs the visual heightfield — battery idiom, no GPU); overlay captures ONLY
  after a step-0 overlay probe. Artifact: commit + FLOOR_TRUST log + probe log. Done: planted-RED
  proven; scripted walk to map edge yields ZERO visible-ground-without-collision points (blocked or
  kill-planed, never silent); edge fall respawns at zone spawn; 256-probe result recorded. Dep: none.
  **FIRST CARD ON THE BOARD; ruling-free (S34 idiom).**
- **S39 — Zone-scene mounting (kills A10).** Scope: travel mounts the zone's own scene under `World`
  (route `WorldDirector.cs:351-362` + EnterZone); remove all zone-internal `current=true`; **DayNight
  rebinding on mount/unmount** (one-shot `World/Meadow/Environment/*` binding, :231-240); new logic in
  a NEW partial (WorldDirector.cs 509-l); **under the standalone branch of §0, ALSO authors**
  `zones/hollow/hollow.tscn` + `assets/terrain/hollow.png` (declared-new content; DayNight already
  keys "hollow"). Artifact: commit + per-zone capture set. Done: travel key visibly changes ENV
  identity (capture diff per zone); shipped nearest leg ZoneBossProof `zone_travel` re-greens AND new
  leg **ZONE_VISUAL_MOUNT** (W5's name) GREEN with planted-RED proof. Dep: S38; §0 branch.
- **S44 — Perf gate extension (after S39, before the grade wave; per-stage asserts).** Scope: extend
  `tools/perf_probe.sh` to per-zone per-stage asserts. Threshold: the +19.4% ruins row is **NAMED, NOT
  RULED** — inside the ~11 ms pooled control bound; **paired A/A re-run owed before any stage REDs**;
  >15% unpaired = flag for paired re-run, not auto-RED. Artifact: gate script + evidence. Done:
  planted heavy plant RED; merged tip GREEN; each subsequent card appends its paired delta row.
  Dep: S39 (mount changes what's measured); gates S40, S41, S42, S43.
- **S40 — D2 grade pass (env/light/day-night).** Scope: per-zone grade re-key (dark desaturated
  ambient, warm localized lights, saturation < 1, contrast up, gloomy day-night anchors); step-0 probe
  per §1 (env-fog certain; volumetric only if GREEN at target runtime; NO renderer-comment deletion,
  NO config flip bet). Artifact: commit + before/after captures + S44 row. Done: owner picks grade
  from 2 variants; capture pair on disk; S44 delta row. Dep: S39, S44.
- **S41 — Ground materials (FIRST ground textures ever).** Scope: ground is FLAT ALBEDO today
  (`:154-157`) — height/slope-blended dirt+moss+stone via `.gdshader` or first textures (C'-S47
  feeds); C'-2 strengthens this card, does not gate it. Artifact: material/shader site + captures +
  S44 row. Done: patch variation visible in captures at the CI-EFFECTIVE renderer (probe GREEN or
  documented compat path); S44 row. Dep: S40; **sequenced after S38** (same file `terrain_builder.gd`).
- **S42 — Prop density + height-snap.** Scope: scatter tool placing EXISTING kit props snapped to the
  heightfield, per-zone clutter rules, draw-call budget. Artifact: commit + captures + S44 row. Done:
  density counts per zone, zero floating/sinking props in captures, S44 row. Dep: S39; **sequenced
  after S40** (both edit the same zone .tscn nodes — file races below).
- **S43 — Ambient particles + hit-beat FX (probe-gated; §0 branch for S28).** Scope: GPUParticles3D
  ambient dust/embers; hit-beat FX at the ONE hit hunk (`WorldDirector.cs:387`, the
  `ActorVisual.PlayHitFx` hunk) is W5-S28's exact scope → merge into S28 **only under a W5-A pick**;
  otherwise standalone, carrying S28's step-0 probe as the card's first commit (may CUT the FX half);
  C' sprite sheets optional feed. Artifact: probe evidence + commit + captures. Done: probe GREEN at
  CI-effective renderer (else CUT logged — ambience half unaffected); particle presence in every zone
  MOUNTED by S39/S27 (count follows the mount outcome — not a fixed 3). Dep: S40; §0 branch.
- **S45 — Iso-camera rig behind a toggle (needs B ruling).** Scope: fixed isometric rig (pivot+offset,
  occlusion clearance, juice retune, toggle default=off). Artifact: commit + captures both modes.
  Done: owner plays both, names a default in writing; saves load in both (no new fields — R7 safe).
  Dep: B ruling; after S39.
- **S46 — Style bible + per-zone targets (C' foundation).** Scope: locked palette + light-mood rules
  + per-zone targets from the two pilots; provenance sidecar format. Artifact: `docs/STYLE_BIBLE.md`
  + `.audits/<run>/out/targets/`. Done: palette hexes + rules stated; 3–5 original targets, each with
  prompt+seed sidecar. Dep: C' ruling.
- **S47 — Generated textures + seam pass (C').** Scope: tileable ground/wall textures per zone into
  `skeleton/resources/generated/` + seam post-pass; **generation runs parallel to the A-wave,
  INTEGRATION (into S41's material site) is sequenced after S41** (file races below). Artifact:
  texture set + sidecars + LICENSES.md rows ("fleet-generated", prompt+seed). Done: seamless 2×2 tile
  captures; sidecar per file. Dep: S46 (generation); S41 (integration).
- **S48 — Icons/portraits/UI + sky strips (C').** Scope: item/skill icons, NPC portraits, painted
  skybox strips for zone horizons. Artifact: asset set + sidecars + captures. Done: HUD zero
  placeholders; strips render at CI-effective renderer (probe). Dep: S46.

**File races (binding sequencing, P2-2 + N-5):** `terrain_builder.gd` — S38 → S41 → S47-integration
(never parallel); zone `.tscn` Environment/Decor — S40 → S42 (never parallel; S39 mount edits precede
both); `WorldDirector*` — S38 → S39 → S43 sequential (shared entry file). Parallel: S46–S48 GENERATION
(own new dirs) ∥ A-wave after ruling; S44 after S39; S34 (if a W5 rider runs) ∥ S38 (§0). No card adds
save fields (R7; S45's toggle needs none — camera state is not persisted).

## 6. COMPLIANCE (MC 10365 R2 addendum — binding)

- Diablo-2 honored as **genre mood/look direction only**: isometric dark-fantasy ARPG, painterly feel,
  fixed ~45° framing, dark desaturated palette with LOCALIZED warm light sources, high prop clutter
  density, painted environments, small high-contrast character silhouettes — mood attributes cited from
  the owner's collage (`ref-diablo2.png`, read-only, owner-supplied), for attributes only, never
  reproduced.
- No RE/analysis of any third-party product; no scraping/copying of D2 assets, levels, monsters,
  names, UI layouts or item systems.
- All zones, creatures, systems and assets above are ORIGINAL or CC0 (Kenney, LICENSES.md rows) or
  fleet-generated with prompt+seed provenance sidecars.
- Both pilot images were generated clean-room (text prompts only, "ORIGINAL artwork, no copyrighted
  characters") — the compliance pattern for all C' output.

## 7. REPAIRS

**c1 list (`DA-verdict.md`):** P1-1..3, P2-1..3, P3-1..3, P4-1..4 — **13/13 CONFIRMED RESOLVED by
DA-c2 §1**; text carried forward except where an N-finding edits the same sentence (N-2 §0, N-4/N-7 §1+S38, N-3 §2-A).

**c2 N-REPAIRS (`DA-verdict-c2.md` §2, this cycle):**
- **N-1** §3 Wave V1 now reads S39 → S44 → grade wave (S40–S43); matches S44 card dep + File-races; no inversion. ✔
- **N-2** §0 forks on the PICK ("W5-A → merge into S27/S28; W5-B/W5-C/none → S39 standalone FULL scope, S43 standalone + probe idiom"), cites w5 :261/:263-264; S34∥S38 disjointness line added; S39/S43 cards point at the §0 branch. ✔
- **N-3** §2-A carries its own **S38 IS RULING-FREE** line (S34 floor-rider idiom); A ruling scoped S39–S44 (S40–S44 under W5-A). ✔
- **N-4** "documented 255 maximum" attribution DROPPED (§1 + S38); engine source (4.7, fetched by DA-c2) shows no note/no clamp; 256 behavior kept as INFERRED question, probe stands. ✔
- **N-5** S47: generation parallel / integration after S41; joined the File-races chain. ✔
- **N-6** S38 proof = headless physics-query marker leg (battery idiom); overlay captures only after a step-0 overlay probe. ✔
- **N-7** spans corrected: collision floor `(map_width−1)` m = **63 m** meadow, dead ring **~31.5 m**, visual 96 m (§1 + S38). ✔
- **N-8** acknowledged (process, parent-owned): c3 gate is handed this file's **sha256**, not a size claim; producer returns the hash with the pointer. ✔
