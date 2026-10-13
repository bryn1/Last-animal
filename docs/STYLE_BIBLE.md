# STYLE_BIBLE — Last Animal visual style bible (MC 10517 / display 10026.34, stage S46)

Status: S46 C' foundation, 2026-10-12; cycle-2 DA-fix pass same day (verdict FIX, findings F1–F7
addressed — provenance/method statements corrected, locked hexes unchanged except none). Basis:
`docs/d2-visual-direction-proposal.md` v3 (§1, §2 C',
§5 S46–S48, §6 COMPLIANCE — all binding). Owner ratified C' (fleet-generated art = allowed in-house
work under D-1), verbatim "rec for all. go" 2026-10-12. This bible is the generation contract for
S47 (textures) and S48 (icons/sky strips): every generated asset uses the locked palettes, the
light-mood rules and the provenance format below.

Compliance (binding, proposal §6): Diablo 2 is honored as **genre mood direction only** — dark
desaturated palette, localized warm light sources, painterly dark-fantasy surfaces, high prop
clutter, small high-contrast silhouettes. These are mood attributes; we NEVER reproduce D2 assets,
characters, names, UI layouts or item systems, and no RE/analysis of any third-party product.
**Every generation prompt MUST contain verbatim:**
`ORIGINAL artwork, no copyrighted characters, no game franchise references`

## 1. Zones (truth from code, VERIFIED this session)

`EcosystemSpawner.cs:125` — `ZoneIds = { "meadow", "canyon", "ruins", "hollow" }`. Four zones, no hub.
- meadow / canyon / ruins: shipped `.tscn` + env grade (`zones/<z>/<z>.tscn`, read this session).
- hollow: code-registered identity (`WorldDirector.DayNight.cs:121-132`, MC 10216 S18 zone four) —
  table-only, NO `.tscn` on disk yet (authored later under the S39/S27 branch, proposal §0).
- `zones/bluetest`, `zones/redtest` are preflight test scenes, NOT game zones — excluded from the bible.

## 2. Locked palettes (HEX) — generation contract

Provenance tags: **MEASURED** = EXACT pixel values present in the ratified pilot images
(`.audits/20261009-0615-ownerplaytest/out/` pilots — the bible anchors named in proposal §2 C'),
extracted from pilot pixels by method **LOST**: the originally stated k-means(8, seed 7) recipe
reproduces 0/8 centroids and no measurement script survives (DA c1 F5). The hexes are kept —
verified real pilot pixels (13/15 exact, the other 2 within L1≤5) — but they are NOT re-derivable.
**DERIVED** = live code value shifted by the documented method below (the C' mood target);
**RAW** = live code value adopted UNshifted (source column cited in the row). DERIVED canyon/hollow
slots stay UNVERIFIED as a render until the first S47/S48 capture pair.

Derived-space method (mechanical, reproducible): take the zone's live DayNight/`tscn` color
(`WorldDirector.DayNight.cs` keyframe rows — every anchor below names its exact column, and every
shift re-verifies within ±1 8-bit step; `zones/<z>/<z>.tscn` env block) and apply HSV
S×0.70, V×0.85 (the desaturate-toward-pilot-family rule; pilot sat MAJORITY 0.09–0.39, §3.1).

Pilot mass rule (stated per F3 — applies to every mass % in §2/§3): every full-res pilot pixel
assigned to its nearest locked slot color (RGB L1, NO tolerance band), % of image, re-measured
this session. The predecessor's figures (meadow `#0c100c` 21.7%, ruins `#14150f` 12.1%) came from
the LOST recipe above and match no rule testable today — closest family: 256 px thumbnail
nearest-slot (21.3–21.8% / 12.2–13.0%); DA c1's restatements (34.9% / 0.0%) reproduced under NO
tested rule (cycle-2 commit body). Figures below are restated full-res values.

### 2.1 Meadow — dark moss + ochre, cold light, one warm break (pilot `la-style-meadow-d2.png`)

| slot | hex | role | prov |
|---|---|---|---|
| shadow | `#0c100c` | unlit ground, 22.3% pilot mass (rule §2 preamble) | MEASURED |
| dark-moss | `#1d2117` | grass body shadow side | MEASURED |
| olive-mid | `#343624` | grass mid tone, main field color | MEASURED |
| slate-sage | `#3f4c49` / `#636e61` | stone + wet-lichen mids | MEASURED |
| ochre | `#62582d` | dry dirt patches (sat peak 0.54) | MEASURED |
| sage-hi | `#869081` | lit tuft tops | MEASURED |
| pale-top | `#c0c4b1` | raking-light highlights only | MEASURED |
| warm-break | `#ff9e59` | the ONE localized warm light — RAW code value, see note below | RAW |

`#ff9e59` relabeled DERIVED→RAW (F2): it is live code `Color(1.00f, 0.62f, 0.35f)` verbatim at
`WorldDirector.DayNight.cs:98` (meadow dusk **LightColor**) and `:106` (canyon noon **SkyHorizon**).
The S×0.70 V×0.85 method ON it yields `#d99f76` (measured), so it cannot have come from the method.
Kept as hex — it is the live noon-horizon identity also cited by §4 and §7; re-deriving to `#d99f76`
would shift the shared warm family and §4 (new decision, not a doc fix).

### 2.2 Ruins — warm umber stone, near-black hollows, rose accent (pilot `la-style-ruins-d2.png`)

| slot | hex | role | prov |
|---|---|---|---|
| void | `#14150f` | deep hollows, 14.5% pilot mass (rule §2 preamble) | MEASURED |
| stone-dark | `#24231b` / `#352f24` | shaded masonry | MEASURED |
| stone-mid | `#493f2e` / `#5e4f39` | lit masonry, mortar | MEASURED |
| stone-warm | `#766349` / `#997e5f` | grazing-light faces | MEASURED |
| rose-accent | `#c38289` | the pilot's single accent (<=3% of any asset) | MEASURED |

### 2.3 Canyon — baked warm haze, desaturated toward the pilots (no pilot yet; from code identity)

| slot | hex | raw code anchor → derived | prov |
|---|---|---|---|
| deep-shadow | `#0f0f1a` | night sky-top `#0d0d1f` | DERIVED |
| night-cold | `#9699c3` | night **LightColor** `#999ee6` (night ambient is `#59527a`, NOT the anchor) | DERIVED |
| rock-dusk | `#ce7a5f` | dusk sky-horizon `#f26638` | DERIVED |
| warm-mid | `#d9946f` | dusk light-color `#ff8c4c` | DERIVED |
| haze | `#d9ba9c` | noon fog light `#ffcc99` (canyon.tscn) | DERIVED |
| pale-top | `#d9caae` | noon **LightColor** `#ffe6b8` (noon ambient is `#ffb880`, NOT the anchor) | DERIVED |

### 2.4 Hollow — cold spectral teal-silver (identity from `DayNight.cs` Hollow rows; no pilot yet)

| slot | hex | raw code anchor → derived | prov |
|---|---|---|---|
| deep-shadow | `#0a1016` | night sky-top `#050f1a` | DERIVED |
| teal-mid | `#3b565f` | noon **SkyTop** `#336170` (noon ambient is `#b2d9cc`, NOT the anchor) | DERIVED |
| spectral | `#92adaa` | noon sky-horizon `#9eccc7` | DERIVED |
| dawn-horizon | `#86a9a0` | dawn sky-horizon `#8cc7b8` | DERIVED |
| night-ambient | `#7f9da9` | night **LightColor** `#80b2c7` (night ambient is `#3d616b`, NOT the anchor) | DERIVED |
| pale-top | `#cbd4d0` | noon **LightColor** `#ebfaf2` (noon ambient is `#b2d9cc`, NOT the anchor) | DERIVED |
| ember-accent | `#d9946f` | shared warm rule (§3), sparing — spectral zones still get ONE warm light | DERIVED |

**Shared warm family** (localized lights, all zones): `#ff9e59` core (RAW, §2.1 note), `#d9946f`
ember falloff. Nothing else in a frame may be warm and saturated.
**Column-label fix (F4):** five §2.3/§2.4 anchors above cited `ambient` but are `LightColor`/
`SkyTop` values — corrected in place; all 12 DERIVED shifts re-verified this session against the
NOW-NAMED column, each landing on the locked hex within ±1 8-bit step (rounding convention), so
every hex stays as locked and every anchor is re-derivable from code.

## 3. Light-mood rules (binding for every capture, prompt and future env re-grade — proposal §1/§2-A)

1. **Ambient desaturation < 1.** `adjustment_saturation` must come DOWN below 1.0. Current shipped
   values push the WRONG way (meadow 1.12, canyon 1.18 — read from .tscn this session; S40 re-keys).
   Generated 2D assets stay in the pilot family: sat MAJORITY 0.09–0.39 (measured full-res,
   S=(max−min)/max: 56.8% meadow / 57.9% ruins of pixels land there). The band is a majority
   statement, NOT a ceiling — the ratified pilots themselves sit at 39.0% / 37.0% of pixels above
   0.39 and 12.2% / 7.8% above 0.55 (measured this session, F2) — so the enforceable rule is the
   §2 SLOT list, not a numeric cap: exceeding sat 0.39 is only legal on a §2-locked slot, whose
   peaks the bible itself carries: `#62582d` 0.54 (meadow ground ochre — a ground color, not an
   accent), `#ce7a5f` 0.54, `#d9946f` 0.49, `#ff9e59` 0.65. Any OTHER hue above 0.39 is a defect,
   above 0.55 a shipping defect. The warm pair `#ff9e59`/`#d9946f` stays localized (rule 2, <=3%
   frame area) EXCEPT the canyon baked warm-horizon target (§7) — that is baked sky, not a light.
2. **Localized warm lights only.** Warm, saturated light exists ONLY as point sources (torch,
   brazier, ember, sun-break) inside the shared warm family (§2 end). Ambient/sky stay cold or
   neutral. A frame with two unrelated warm hues is out of style.
3. **Darkness is graded, not blacked.** Val floor near-black olive (`#0c100c`-family) with readable
   shape — measured near-black mass (full-res V<0.13): meadow **31.6%**, ruins **19.5%**; the prior
   "pilots prove mass 12–22%" claim matched no tested rule (F3, thumbnail variant 30.9%/18.0%).
   The rule is readability INSIDE the darkness, not a mass cap.
4. **Contrast up, energy down.** Keep `adjustment_contrast >= 1.08`; ambient energy stays in the
   shipped 0.20–0.55 band (§4); glow 0.25–0.45 (canyon/ruins/meadow tscn precedent).
5. **Fog pin holds.** `fog_sky_affect = 0.0` is a GL-Compat washout pin, live constraint, re-written
   every frame by the driver — no asset or prompt fights it (`DayNight.cs:18-21` comment).
6. **Painterly, not photoreal.** Visible brush character, no photo textures, no modern-render crisp
   specular — matches both pilots and proposal §2-A.

## 4. Day-night anchors (from live `DayNight.cs` tables — the ONE source of truth stays the code;
this table fixes what generated horizons/sky strips must match at the two extreme anchors)

| zone | anchor | sun | light | sky-top | horizon | ambient @energy |
|---|---|---|---|---|---|---|
| meadow | noon | 1.50 | `#fff5e0` | `#4c85e6` | `#c7dbf2` | `#fff5e0` @0.55 |
| meadow | night | 0.35 | `#8ca6f2` | `#080d1f` | `#1a1f40` | `#4c6199` @0.22 |
| canyon | noon | 1.60 | `#ffe6b8` | `#59669e` | `#ff9e59` | `#ffb880` @0.45 |
| canyon | night | 0.35 | `#999ee6` | `#0d0d1f` | `#2e1f33` | `#59527a` @0.20 |
| ruins | noon | 1.10 | `#f2f2ff` | `#141c38` | `#738099` | `#99add9` @0.50 |
| ruins | night | 0.28 | `#738cd9` | `#050814` | `#0f1429` | `#384773` @0.25 |
| hollow | noon | 1.25 | `#ebfaf2` | `#336170` | `#9eccc7` | `#b2d9cc` @0.48 |
| hollow | night | 0.30 | `#80b2c7` | `#050f1a` | `#142933` | `#3d616b` @0.24 |

Rules carried from the tables (VERIFIED reads): ruins **darkness is identity — the cycle never
brightens it**; hollow has no tscn grade so its **noon row IS its base look**; fog is NOT a keyframe
channel (S13 fog grade survives the cycle); anchors interleave piecewise-linear, cycle = 1800 ticks
@60 Hz. Generated sky strips / horizon art must carry the zone's horizon hex at day and stay inside
§2 palettes at night.

## 5. Silhouette + clutter rules

- **Small, high-contrast characters**: silhouettes read against the LOCAL background value (pilot
  mids `#343624`–`#636e61` meadow / `#352f24`–`#766349` ruins): character edges always >= 2 palette
  steps from the value behind them; no character color may sit inside the ground-mid slot.
- **High prop clutter, low prop saturation** (genre-readable density, per proposal §6 mood list):
  props enter at mid/low slots, warm accents stay with the lights (§3.2). Density follows S42's
  per-zone clutter rules; until S42 ships, generated clutter sheets (icons/VFX) stay under ~40%
  occupied pixels so in-engine scatter can thin them.
- **Everything height-snapped**: no floating/sinking props in any capture (S42 done-shape; today's
  hand-placed decor ignores the heightfield — proposal §1).
- Icons/portraits: single silhouette per cell, dark backdrop, one rim-light in the zone's warm or
  spectral accent; consistent frame treatment per set (S48 done-shape: zero HUD placeholders).

## 6. Provenance sidecar format (binding for EVERY fleet-generated asset, C' ruling condition)

Every generated PNG ships with a `.json` sidecar **beside it, same basename**:

```json
{
  "prompt": "<EXACT full prompt text sent to the generator>",
  "seed": 1234,
  "size": "1536x1024",
  "mode": "t2i | edit",
  "quality": "fast | full",
  "refs": ["<path of EACH reference image passed>", "..."],
  "generator": "<exact tool/model id, e.g. generate_image Qwen-Image-Edit-2511 on vm100>",
  "ts_utc": "2026-10-12T20:12:55Z",
  "target_for": "<zone>/<asset-class>: <card id>"
}
```

- `prompt` verbatim (not summarized); it MUST include the clean-room sentence (§ top).
- `seed`: the seed actually passed (or `null` + note if the model sampled unseeded).
- `mode` / `quality`: the values actually passed — output differs materially between `t2i` and
  `edit` and between `fast` (8-step Lightning) and `full` (40-step), so both are required for a
  re-run to be honest (F1). `mode=edit` is what an in-context style ref forces; pair it with `refs`.
- `refs`: path list of EVERY reference image passed, `[]` if none. This is the field §7's
  style-ref mandate records into — the §7 ref image path goes here, no new decision needed (F1).
- `generator`: tool + model id (the model behind `generate_image` has changed before; provenance
  must say which one produced the asset).
- `ts_utc`: generation call timestamp, ISO-8601 Z.
- `target_for`: style-bible target id (e.g. `meadow/ground-texture: S47`) or shipping asset path.
- Shipping assets (S47/S48 into `skeleton/resources/generated/`) additionally carry a
  `resources/assets_packs/LICENSES.md` row: `fleet-generated (Qwen-Image, prompt+seed sidecar)`.
- Non-reproducible prompt drift is a defect: sidecar prompt not byte-equal to what was sent = asset
  does not ship.

## 7. Per-zone C' target assignments (which generated asset class leads each zone)

| zone | primary C' target | card | why this class first |
|---|---|---|---|
| meadow | ground textures (dirt+moss+stone blend, tileable) | S47→S41-integration | ground has ZERO textures today (flat albedo `mat.albedo_color`, `skeleton/world/terrain_builder.gd:214-215`); meadow is the first zone the owner plays |
| canyon | painted sky strip / horizon | S48 | canyon's identity IS its warm haze horizon (`#ff9e59` noon fog); "cheapest C-feel in the repo" per proposal §2-C' |
| ruins | ground + wall textures (cracked flagstone, moss) | S47 | ruins has the strongest stone-against-warm-light mood (measured pilot family); feeds S41 material site |
| hollow | icon sheet / portraits (teal-spectral chrome) | S48 | hollow has NO .tscn yet (S39/S27 pending) — ground/sky generation would precede the zone's own scene; icons are additive with zero mount dependency |

Secondary (queued after primaries): canyon ground, meadow sky strip, ruins icons (rose-accent set),
hollow sky strip + ground after its mount ships. Style-ref discipline (drift control, proposal §2
C' limits): every future generation for a zone cites the zone's §2 palette hexes IN the prompt plus
one in-context style ref from that zone's pilot/target set.

## 8. Targets — honest gap (DO NOT paper over)

The four per-zone target images (1536×1024, clean-room prompts drafted and held at
`.audits/202610121830-s46/`) were **NOT generated: 0 of 4.** Root cause, VERIFIED this session:
`mcp__imagegen__generate_image` failed twice (20:12:56Z and 20:17:34Z) — `imagegen.service` on the
GPU host vm100 (192.168.5.230) dies at `Failed at step CHDIR`: its `WorkingDirectory=
/opt/imagegen/ComfyUI` **no longer exists** (`ls: cannot access '/opt/imagegen': No such file or
directory`; unit last healthy 2026-10-09 20:23 UTC, when the two pilot images were rendered). The
window rollback restored llama-swap both times; GPU verifier unaffected. This is an infra repair
outside this leaf's scope (reinstall ComfyUI+venv+models on vm100), not a doc fix.
Consequences: no target PNGs / no sidecars yet; §2.3/§2.4 canyon+hollow palettes are DERIVED,
UNVERIFIED until first capture or a rendered target post-repair; the ratified pilots
(`la-style-meadow-d2.png`, `la-style-ruins-d2.png`) remain the ONLY image anchors. On repair,
re-run S46 target generation with the §6 sidecar format and restamp §2.3/§2.4 as MEASURED.

SIDECARS: 0 OK
