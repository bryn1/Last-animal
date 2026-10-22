# Asset pack licenses — Last Animal graphics pass

One row per downloaded pack. All packs verified CC0 (public domain) at their source page on download date.

| Pack | Source URL | License | Download date | Local path |
|---|---|---|---|---|
| Kenney Nature Kit (2.1) | https://kenney.nl/assets/nature-kit | CC0 (Creative Commons Zero) — `License.txt` in pack root | 2026-09-30 | `skeleton/resources/assets_packs/nature-kit/` |
| Kenney Castle Kit | https://kenney.nl/assets/castle-kit | CC0 (Creative Commons Zero) — `License.txt` in pack root | 2026-09-30 | `skeleton/resources/assets_packs/castle-kit/` |
| Kenney Skyboxes (1.0) | https://kenney.nl/assets/skyboxes | CC0 (Creative Commons Zero) — `License.txt` in pack root | 2026-09-30 | `skeleton/resources/assets_packs/skyboxes/` |

Notes:
- Each pack dir keeps its original `License.txt` verbatim (VERIFIED: file present in all three dirs).
- CC0 statement read from each kenney.nl asset page HTML on 2026-09-30 ("Creative Commons CC0" / "CC0 licensed").
- No attribution required by CC0; keeping this file and the pack License.txt files is good practice, not an obligation.

## Fleet-generated assets (C' ruling, STYLE_BIBLE §6 — MC 10523 / S47, 2026-10-15)

Fleet-generated in-house work (owner-ratified C', "rec for all. go" 2026-10-12); NOT third-party content.
Clean-room sentence embedded verbatim in every prompt. Full verbatim prompt + seed + mode + quality + refs live
in the `.json` sidecar beside each PNG (STYLE_BIBLE §6 format); generator = `/opt/imagegen Qwen-Image via harness
imagegen MCP` (model Qwen-Image-Edit-2511 + Lightning 8-step LoRA, read from each render's embedded workflow).
POST-PROCESS (MC 10759, 2026-10-16): each PNG additionally carries a CPU tileability pass —
`skeleton/ci/seam_fix.py` edge-offset blend, pure PIL + numpy (no GPU, no network), recorded as a `postprocess`
block in the same sidecar; the render prompt/seed/mode/quality/refs above are UNCHANGED by it, and the plate is
re-encoded from the pixel array so the 0-metadata-chunk property still holds. In-house work either way: the
post-process transforms fleet-generated pixels and introduces no third-party content.

| Asset | License | Provenance row | Seed | Sidecar | Post-process (CPU) |
|---|---|---|---|---|---|
| `skeleton/resources/generated/ground_dirt.png` | fleet-generated | fleet-generated (Qwen-Image, prompt+seed sidecar) — prompt head: "ORIGINAL artwork … dry cracked ochre clay dirt …" | 4711 | `skeleton/resources/generated/ground_dirt.json` | `seam_fix.py` edge-offset blend (sigma 10, band 40) |
| `skeleton/resources/generated/ground_moss.png` | fleet-generated | fleet-generated (Qwen-Image, prompt+seed sidecar) — prompt head: "ORIGINAL artwork … dense dark mossy grass floor …" | 4721 | `skeleton/resources/generated/ground_moss.json` | `seam_fix.py` edge-offset blend (sigma 10, band 40) |
| `skeleton/resources/generated/ground_stone.png` | fleet-generated | fleet-generated (Qwen-Image, prompt+seed sidecar) — prompt head: "ORIGINAL artwork … cracked ancient flagstone pavement …" | 4731 | `skeleton/resources/generated/ground_stone.json` | `seam_fix.py` edge-offset blend (sigma 10, band 40) |
| `skeleton/resources/generated/wall_stone.png` | fleet-generated | fleet-generated (Qwen-Image, prompt+seed sidecar) — prompt head: "ORIGINAL artwork … vertical masonry wall face …" | 4741 | `skeleton/resources/generated/wall_stone.json` | `seam_fix.py` edge-offset blend (sigma 10, band 40) |
| `skeleton/resources/generated/wall_rock.png` | fleet-generated | fleet-generated (Qwen-Image, prompt+seed sidecar) — prompt head: "ORIGINAL artwork … canyon sandstone cliff face …" | 4751 | `skeleton/resources/generated/wall_rock.json` | `seam_fix.py` edge-offset blend (sigma 10, band 40) |

SEAM-GATE STATUS (S47 DoD, `skeleton/ci/seam_check.py`): **ALL FIVE PASS** since MC 10759 (plates at
dc44e2c, gate re-run at this card's tip — transcript `out/red_proof/gate_proof_final.txt`; before → after
seam_max / interior_p95: ground_dirt 118/32 → 3/30, ground_moss 120/20 → 3/19, ground_stone 172/28 → 6/27,
wall_stone 149/42 → 3/41, wall_rock 224/54 → 7/53; margin 4.5x to 13.7x, interior_p95 moved by <=2 units).
They shipped as FAILING palette/mood candidates on 2026-10-15 (per-attempt table in
`last-animal/.audits/202610151912-s47/out/attempts.json`); the CPU wrap-fix closed the boundary step, and the
gate itself was proven RED again on a planted bad variant at this commit. Table + red-proof + 2x2 composites:
`last-animal/.audits/202610151912-s47/out/SEAMFIX.md` and `out/fixed/`. Honest framing (per the DA wall on MC 10759): this gate's certificate is the 1-px seam metric only (`seam_max` vs `interior_p95`) — the recorded low-frequency wrap lighting (+19.7..+29.9, item (2) below) is NOT certified by it and stays a named S41 ruling; gate hardening is tracked as MC 10776.

CARRIED FOR S41 (measured, NOT fixed by a wrap-fix — content, not seams): (1) `wall_rock.png` pale sky band
along the top edge: rows 0-76 each have a majority of pixels above luma 150 (77 rows), rows 0-31 mean luma 202.3
against a plate mean of 74.2, and 63728 of the 65536 px in rows 0-63 are above 150. The pass reaches only 185.3
for rows 0-31 / 60052 px / 73 majority-pale rows (-5.8%), and the horizontal-wrap luma step 141.9 → 124.1 — the
band is 77 px tall, far wider than the 40-px seam band, so it needs crop or repaint (cropping those 77 rows
leaves 947, and restoring Po2 1024 means an 8.1% vertical stretch: a composition call, not a seam fix). (2) Generator-baked edge lighting, seen as a
low-frequency mean-luma step across the wrap (60-px strips, before → after): ground_stone vertical +34.2 → +29.9,
wall_stone vertical +22.8 → +19.7 and horizontal +28.3 → +23.7, ground_dirt +10.3/+10.2 → +9.1/+9.9 — tiling now
has no hard line but still shows that lighting swing. Both measured per-plate in SEAMFIX.md. Removing that swing (a low-frequency wrap-equalization) is a **DECISION OWED** — named for an S41 ruling, never applied unilaterally: removal repaints each plate's whole luminance field, and the wrap steps it would have to remove measure +19.7..+29.9 after the seam fix (per-plate values in SEAMFIX.md) — the same standing as the crop/repaint ruling owed on (1).


## Fleet-generated assets — S48 Batch-3 FX/props (MC 11014 / 10026.44.2, 2026-10-20; RE-GENERATED + finalize-authored MC 11059 / 10026.44.9, 2026-10-21)

Fleet-generated in-house work (owner project). SUPERSEDES the 2026-10-20 Batch-3 bytes: all 21 assets
were RE-GENERATED with bible-compliant clean-room-first prompts and now ship through
`skeleton/ci/asset_finalize.py`'s new RGBA path (MC 11059) — the route-B finalize-bypass is retired by
that same change (DA wall 202610201645-s48walls P1-3 root cause: the script used to `.convert("RGB")`,
so an alpha cutout could never have been "shipped by asset_finalize.py"; the claim is now TOOL-TRUE —
each sidecar's §6 fields prompt/seed/size/mode/quality/refs/generator/ts_utc/target_for are written by
the script, `ts_utc` is the producer-recorded generation time, `seed` the seed actually passed).
Generator `generate_image Qwen-Image-2512 on vm100` (8-step Lightning, t2i/fast — per sidecar).
NOT third-party content; the clean-room sentence "ORIGINAL artwork, no copyrighted characters,
no game franchise references." starts EVERY prompt verbatim (VERIFIED this session: 21/21 shipped
sidecar prompts begin with it; the 2026-10-20 prompts failed this and are replaced, not relicensed).
Full verbatim prompt + seed + mode + quality + refs + ts_utc + target_for live in the `.json` sidecar
beside each PNG (STYLE_BIBLE §6 format). Prompt heads below are the FIRST 60 CHARACTERS OF THE ASSET
DESCRIPTION, i.e. after the clean-room sentence every prompt starts with. `blood_splat_*` now live
under `fx/` with the other FX cutouts (the 2026-10-20 ship parked them in `props/`). FX hue: the
aura/lightning/nova assets implement the option A FX-hue ruling (owner 2026-10-20) — the citation
sits on exactly those five rows below; no FX-slot code consumes these assets yet (consumer grep at
this tip is empty; engine wiring is a later code card). POST-PROCESS (MC 11014 idiom, unchanged): the 17 square
(1024x1024) PNGs additionally carry the CPU corner-haze clamp — `skeleton/ci/edge_clamp.py` zeroes the
outer 8-px border ring's alpha IN PLACE, recorded as a `postprocess` block in the same sidecar; the
`alpha_proof` block is finalize-written (pre-clamp) then re-measured post-clamp with the pre-clamp
corner kept as `pre_max_corner_alpha` (this run: pre-corners 32–50 -> 0 on all 17). The four flipbook
strips ship unclamped (their cells carry no 4-corner semantics) and are the ONLY entries carrying a
recorded `po2_exempt` declaration: 256-px sheet cells are not engine-sampled Po2 textures yet — the
S48 PLAN §2 NAMED honest gap (ii); the integration card re-imposes Po2 when it decides sampling.
In-house work either way: finalize and clamp transform fleet-generated pixels and introduce no
third-party content.

| Asset | License | Provenance row | Seed | Sidecar | Post-process (CPU) |
|---|---|---|---|---|---|
| `skeleton/resources/generated/fx/fire_pool_surface_a.png` | fleet-generated owner-project | fleet-generated owner-project (Qwen-Image-2512, mode t2i, fast, finalize-authored MC 11059, prompt+seed sidecar) — prompt head: "Game FX texture asset: a flat ground-level burning oil pool …" | 1853504421 | `skeleton/resources/generated/fx/fire_pool_surface_a.json` | `edge_clamp.py` border-alpha clamp (8-px ring) |
| `skeleton/resources/generated/fx/fire_pool_surface_b.png` | fleet-generated owner-project | fleet-generated owner-project (Qwen-Image-2512, mode t2i, fast, finalize-authored MC 11059, prompt+seed sidecar) — prompt head: "Game FX texture asset: a smaller dying-out burning pool on t …" | 11530725 | `skeleton/resources/generated/fx/fire_pool_surface_b.json` | `edge_clamp.py` border-alpha clamp (8-px ring) |
| `skeleton/resources/generated/fx/aura_disc_a.png` | fleet-generated owner-project | fleet-generated owner-project (Qwen-Image-2512, mode t2i, fast, finalize-authored MC 11059, prompt+seed sidecar) — prompt head: "Game FX texture asset: a glowing magic aura ring lying flat …" — FX-hue ruling option A (owner 2026-10-20) | 1987183777 | `skeleton/resources/generated/fx/aura_disc_a.json` | `edge_clamp.py` border-alpha clamp (8-px ring) |
| `skeleton/resources/generated/fx/aura_disc_b.png` | fleet-generated owner-project | fleet-generated owner-project (Qwen-Image-2512, mode t2i, fast, finalize-authored MC 11059, prompt+seed sidecar) — prompt head: "Game FX texture asset: a subtle dark magic aura ring lying f …" — FX-hue ruling option A (owner 2026-10-20) | 165260915 | `skeleton/resources/generated/fx/aura_disc_b.json` | `edge_clamp.py` border-alpha clamp (8-px ring) |
| `skeleton/resources/generated/fx/aura_disc_c.png` | fleet-generated owner-project | fleet-generated owner-project (Qwen-Image-2512, mode t2i, fast, finalize-authored MC 11059, prompt+seed sidecar) — prompt head: "Game FX texture asset: a bright blessed magic aura ring lyin …" — FX-hue ruling option A (owner 2026-10-20) | 948720659 | `skeleton/resources/generated/fx/aura_disc_c.json` | `edge_clamp.py` border-alpha clamp (8-px ring) |
| `skeleton/resources/generated/fx/blood_splat_1.png` | fleet-generated owner-project | fleet-generated owner-project (Qwen-Image-2512, mode t2i, fast, finalize-authored MC 11059, prompt+seed sidecar) — prompt head: "Game decal texture asset: a small dried blood splat lying fl …" | 1955396322 | `skeleton/resources/generated/fx/blood_splat_1.json` | `edge_clamp.py` border-alpha clamp (8-px ring) |
| `skeleton/resources/generated/fx/blood_splat_2.png` | fleet-generated owner-project | fleet-generated owner-project (Qwen-Image-2512, mode t2i, fast, finalize-authored MC 11059, prompt+seed sidecar) — prompt head: "Game decal texture asset: a medium blood puddle with drip ta …" | 951650284 | `skeleton/resources/generated/fx/blood_splat_2.json` | `edge_clamp.py` border-alpha clamp (8-px ring) |
| `skeleton/resources/generated/fx/blood_splat_3.png` | fleet-generated owner-project | fleet-generated owner-project (Qwen-Image-2512, mode t2i, fast, finalize-authored MC 11059, prompt+seed sidecar) — prompt head: "Game decal texture asset: a long blood drag smear lying flat …" | 1090989073 | `skeleton/resources/generated/fx/blood_splat_3.json` | `edge_clamp.py` border-alpha clamp (8-px ring) |
| `skeleton/resources/generated/fx/blood_splat_4.png` | fleet-generated owner-project | fleet-generated owner-project (Qwen-Image-2512, mode t2i, fast, finalize-authored MC 11059, prompt+seed sidecar) — prompt head: "Game decal texture asset: a large heavy blood splatter lying …" | 1484170611 | `skeleton/resources/generated/fx/blood_splat_4.json` | `edge_clamp.py` border-alpha clamp (8-px ring) |
| `skeleton/resources/generated/fx/blood_splat_5.png` | fleet-generated owner-project | fleet-generated owner-project (Qwen-Image-2512, mode t2i, fast, finalize-authored MC 11059, prompt+seed sidecar) — prompt head: "Game decal texture asset: a fine arterial spray fan of blood …" | 174264140 | `skeleton/resources/generated/fx/blood_splat_5.json` | `edge_clamp.py` border-alpha clamp (8-px ring) |
| `skeleton/resources/generated/fx/blood_splat_6.png` | fleet-generated owner-project | fleet-generated owner-project (Qwen-Image-2512, mode t2i, fast, finalize-authored MC 11059, prompt+seed sidecar) — prompt head: "Game decal texture asset: an old nearly dried dark blood poo …" | 428086296 | `skeleton/resources/generated/fx/blood_splat_6.json` | `edge_clamp.py` border-alpha clamp (8-px ring) |
| `skeleton/resources/generated/fx/flame_strip_8f.png` | fleet-generated owner-project | fleet-generated owner-project (Qwen-Image-2512, mode t2i, fast, finalize-authored MC 11059, prompt+seed sidecar) — prompt head: "Game animation flipbook sheet: ONE horizontal row of exactly …" | 1684272204 | `skeleton/resources/generated/fx/flame_strip_8f.json` | — (flipbook strip, ships unclamped; `po2_exempt` declaration recorded in sidecar) |
| `skeleton/resources/generated/fx/torch_flame_strip_6f.png` | fleet-generated owner-project | fleet-generated owner-project (Qwen-Image-2512, mode t2i, fast, finalize-authored MC 11059, prompt+seed sidecar) — prompt head: "Game animation flipbook sheet: ONE horizontal row of exactly …" | 145846646 | `skeleton/resources/generated/fx/torch_flame_strip_6f.json` | — (flipbook strip, ships unclamped; `po2_exempt` declaration recorded in sidecar) |
| `skeleton/resources/generated/fx/lightning_strip_5f.png` | fleet-generated owner-project | fleet-generated owner-project (Qwen-Image-2512, mode t2i, fast, finalize-authored MC 11059, prompt+seed sidecar) — prompt head: "Game animation flipbook sheet: ONE horizontal row of exactly …" — FX-hue ruling option A (owner 2026-10-20) | 1998792077 | `skeleton/resources/generated/fx/lightning_strip_5f.json` | — (flipbook strip, ships unclamped; `po2_exempt` declaration recorded in sidecar) |
| `skeleton/resources/generated/fx/nova_strip_6f.png` | fleet-generated owner-project | fleet-generated owner-project (Qwen-Image-2512, mode t2i, fast, finalize-authored MC 11059, prompt+seed sidecar) — prompt head: "Game animation flipbook sheet: ONE horizontal row of exactly …" — FX-hue ruling option A (owner 2026-10-20) | 1298530288 | `skeleton/resources/generated/fx/nova_strip_6f.json` | — (flipbook strip, ships unclamped; `po2_exempt` declaration recorded in sidecar) |
| `skeleton/resources/generated/props/prop_barrel.png` | fleet-generated owner-project | fleet-generated owner-project (Qwen-Image-2512, mode t2i, fast, finalize-authored MC 11059, prompt+seed sidecar) — prompt head: "Game prop texture asset: a single dark wooden medieval barre …" | 540511386 | `skeleton/resources/generated/props/prop_barrel.json` | `edge_clamp.py` border-alpha clamp (8-px ring) |
| `skeleton/resources/generated/props/prop_crate.png` | fleet-generated owner-project | fleet-generated owner-project (Qwen-Image-2512, mode t2i, fast, finalize-authored MC 11059, prompt+seed sidecar) — prompt head: "Game prop texture asset: a single dark wooden medieval stora …" | 1773510566 | `skeleton/resources/generated/props/prop_crate.json` | `edge_clamp.py` border-alpha clamp (8-px ring) |
| `skeleton/resources/generated/props/prop_plank_pile.png` | fleet-generated owner-project | fleet-generated owner-project (Qwen-Image-2512, mode t2i, fast, finalize-authored MC 11059, prompt+seed sidecar) — prompt head: "Game prop texture asset: a low messy pile of broken wooden p …" | 2136348211 | `skeleton/resources/generated/props/prop_plank_pile.json` | `edge_clamp.py` border-alpha clamp (8-px ring) |
| `skeleton/resources/generated/props/prop_bone_heap.png` | fleet-generated owner-project | fleet-generated owner-project (Qwen-Image-2512, mode t2i, fast, finalize-authored MC 11059, prompt+seed sidecar) — prompt head: "Game prop texture asset: a small heap of old bones and ribs …" | 1370995246 | `skeleton/resources/generated/props/prop_bone_heap.json` | `edge_clamp.py` border-alpha clamp (8-px ring) |
| `skeleton/resources/generated/props/prop_skull.png` | fleet-generated owner-project | fleet-generated owner-project (Qwen-Image-2512, mode t2i, fast, finalize-authored MC 11059, prompt+seed sidecar) — prompt head: "Game prop texture asset: a single old human skull resting on …" | 258095675 | `skeleton/resources/generated/props/prop_skull.json` | `edge_clamp.py` border-alpha clamp (8-px ring) |
| `skeleton/resources/generated/props/prop_rubble.png` | fleet-generated owner-project | fleet-generated owner-project (Qwen-Image-2512, mode t2i, fast, finalize-authored MC 11059, prompt+seed sidecar) — prompt head: "Game prop texture asset: a small scatter of broken stone rub …" | 1959936427 | `skeleton/resources/generated/props/prop_rubble.json` | `edge_clamp.py` border-alpha clamp (8-px ring) |

## Fleet-generated assets — S48 ground/wall plates V2 (MC 11017 / 10026.44.5, 2026-10-20; RE-GENERATED + finalize-authored MC 11059 / 10026.44.9, 2026-10-21)

Fleet-generated in-house work (owner project). REPLACES the unmerged 2026-10-20 Batch-1 candidate
(`la/s48-batch1` @ 6662aec — SUPERSEDED, never merge it: its five v2 sidecars cited a comp-anchor
sha256 that matches NO artifact on disk (DA wall 202610201645-s48walls P1-1) and its prompts lacked
the clean-room sentence (P1-2)). These five plates ship through
`skeleton/ci/asset_finalize.py` (opaque RGB path, byte-behavior unchanged — proof legs in
`.audits/202610210531-s48fix/out/`): generator `generate_image Qwen-Image-Edit-2511 on vm100`
(mode edit, fast), refs = `refs/comp-dungeon-v1.png`, the owner-ratified style anchor (MC 10995,
2026-10-20 verbatim "rec on both"), sha256
`851ba327d3fdf177f41bf75c2e62700f49c972adb8757421991c6b1d6c8bf675` — VERIFIED against every on-disk
copy of that file this session; the sidecar `refs` field carries path + this sha. The clean-room
sentence starts every prompt verbatim (VERIFIED this session: 5/5 shipped sidecar prompts).
POST-PROCESS: `skeleton/ci/seam_fix.py` edge-offset blend (sigma 10, band 40) per plate, recorded as
the sidecar `postprocess` block — the block's claim "the render as shipped by
skeleton/ci/asset_finalize.py" is now TOOL-TRUE, which it was not on the superseded branch.
WIRING: the three ground plates are LOADED by the four `ground_texture_path` lines — canyon.tscn:80
-> ground_dirt_v2, meadow.tscn:69 -> ground_moss_v2, hollow.tscn:87 and ruins.tscn:81 ->
ground_stone_v2; the two wall plates ship UNWIRED (consumer grep empty).

SEAM-GATE STATUS (`skeleton/ci/seam_check.py`, MEASURED at this tip — verdicts BY PRINT, the gate
exits 0 unconditionally, MC 10759 P3 stands): **ALL FIVE PASS** on the shipped bytes —
seam_max / interior_p95 (margin): `ground_dirt_v2` 3 / 18.0 (6.0x), `ground_moss_v2` 3 / 65.0
(21.7x), `ground_stone_v2` 1 / 36.0 (36.0x), `wall_rock_v2` 4 / 27.0 (6.8x), `wall_stone_v2`
2 / 33.0 (16.5x). Measured margin floor **6.0x**. The superseded branch's seam-margin claim
overstated this floor and is NOT repeated here — these are the gate's own printouts at this tip
(transcript + 2x2 composites: `.audits/202610210531-s48fix/out/seamcheck/`, evidence on disk,
uncommitted per .audits hygiene).

| Asset | License | Provenance row | Seed | Sidecar | Post-process (CPU) |
|---|---|---|---|---|---|
| `skeleton/resources/generated/v2/ground_dirt_v2.png` | fleet-generated owner-project | fleet-generated owner-project (Qwen-Image-Edit-2511, mode edit, fast, finalize-authored MC 11059, prompt+seed sidecar) — prompt head: "Seamlessly tileable ground texture, top-down flat view, hand …" | 120911649 | `skeleton/resources/generated/v2/ground_dirt_v2.json` | `seam_fix.py` edge-offset blend (sigma 10, band 40) |
| `skeleton/resources/generated/v2/ground_moss_v2.png` | fleet-generated owner-project | fleet-generated owner-project (Qwen-Image-Edit-2511, mode edit, fast, finalize-authored MC 11059, prompt+seed sidecar) — prompt head: "Seamlessly tileable ground texture, top-down flat view, hand …" | 565490536 | `skeleton/resources/generated/v2/ground_moss_v2.json` | `seam_fix.py` edge-offset blend (sigma 10, band 40) |
| `skeleton/resources/generated/v2/ground_stone_v2.png` | fleet-generated owner-project | fleet-generated owner-project (Qwen-Image-Edit-2511, mode edit, fast, finalize-authored MC 11059, prompt+seed sidecar) — prompt head: "Seamlessly tileable ground texture, top-down flat view, hand …" | 99898824 | `skeleton/resources/generated/v2/ground_stone_v2.json` | `seam_fix.py` edge-offset blend (sigma 10, band 40) |
| `skeleton/resources/generated/v2/wall_rock_v2.png` | fleet-generated owner-project | fleet-generated owner-project (Qwen-Image-Edit-2511, mode edit, fast, finalize-authored MC 11059, prompt+seed sidecar) — prompt head: "Seamlessly tileable WALL texture, straight-on flat elevation …" | 1931308083 | `skeleton/resources/generated/v2/wall_rock_v2.json` | `seam_fix.py` edge-offset blend (sigma 10, band 40) |
| `skeleton/resources/generated/v2/wall_stone_v2.png` | fleet-generated owner-project | fleet-generated owner-project (Qwen-Image-Edit-2511, mode edit, fast, finalize-authored MC 11059, prompt+seed sidecar) — prompt head: "Seamlessly tileable WALL texture, straight-on flat elevation …" | 449986293 | `skeleton/resources/generated/v2/wall_stone_v2.json` | `seam_fix.py` edge-offset blend (sigma 10, band 40) |

## Fleet-generated assets — S48 Batch-4 canyon sky strip + hollow icon sheet (MC 11088 / 10026.44.10, 2026-10-22)

Fleet-generated in-house work (owner project). Both assets ship through
`skeleton/ci/asset_finalize.py` (§6 sidecar beside each PNG; prompt VERBATIM from the render,
seed/mode/quality/refs/generator/ts_utc recorded by the tool from the producer manifest); the
icon sheet shipped at ship-era as `hollow_icons_v2` carrying the `skeleton/ci/edge_clamp.py`
corner-haze clamp. [RESTAMPED MC 11088 ASSET-SWAP 2026-10-22, owner ruling "Regenerate thinner":
that v2 pair is RETIRED and removed; the sheet at this head is `hollow_icons_v3_final`, regenerated
thinner (t2i, quality full) + producer crop/re-centre recompose — NO clamp on it and none needed
(native matte, `alpha_proof.max_corner_alpha` 0, no pre/post-corner split); the sidecar
`postprocess` block carries the recompose + the §5 occupancy census 20.1-35.9% max 35.9 PASS.] The
clean-room sentence "ORIGINAL artwork, no copyrighted characters, no game franchise
references." starts BOTH shipped prompts verbatim (VERIFIED this session off the finalized
sidecars). Generator per sidecar: `generate_image Qwen-Image-Edit-2511 on vm100` (sky, mode
edit, style-ref `refs/comp-dungeon-v1.png` sha256-pinned in the sidecar — the ratified S48
clean-room anchor) and `generate_image Qwen-Image-2512 on vm100` (icons, mode t2i).
`hollow_icons_v1` was REJECTED at producer review (duplicated potion cell), superseded by v2,
and NEVER entered the repo; v2 was in turn RETIRED by the ASSET-SWAP above (never a rejected
draw — an occupancy rejection by measurement + owner ruling). Consumers at this head: NEITHER asset is loaded by any
`tscn/gd/cs` — the honest zero-consumer rulings are recorded in the MC 11088 LEDGER row (zone
sky today is ProceduralSkyMaterial driven per-frame by DayNight.cs and asserted bit-exact by
the DayNight/ZoneMount legs — a painted strip cannot ride it without a new mechanism; the
hollow icon sheet per PLAN §2 Batch-4 "is additive with zero mount dependency — the honest
consumer story: it needs none today").

| path | origin | license | seed | sidecar | post-process |
|---|---|---|---|---|---|
| `skeleton/resources/generated/sky/canyon_sky_v1.png` | fleet-generated owner-project | fleet-generated owner-project (Qwen-Image-Edit-2511, mode edit, fast, finalize-authored MC 11088, prompt+seed sidecar) — prompt head: "ORIGINAL artwork … Painted panoramic SKY STRIP … WARM HAZE HORIZON …" — opaque RGB 2048x512, non-tileable strip: NO seam verdict claimed (STYLE_BIBLE §6: strips are not tiles) | 643377991 | `skeleton/resources/generated/sky/canyon_sky_v1.json` | — (opaque RGB, ships as finalized; Po2-native 2048x512, no exemption) |
| `skeleton/resources/generated/icons/hollow_icons_v3_final.png` | fleet-generated owner-project | fleet-generated owner-project (Qwen-Image-2512, mode t2i, full, finalize-authored MC 11088, prompt+seed sidecar) — REPLACES `hollow_icons_v2` RETIRED by owner ruling "Regenerate thinner" 2026-10-22 (v2 measured 74.5-75.9% cell occupancy vs STYLE_BIBLE §5 ≤40%); runner-up law: draw 1 seed 789396020 failed the same census (55.6-68.5, discarded), draw 2 seed below selected — ≤3 draws/asset honored — prompt head: "ORIGINAL artwork … Game HUD ICON SHEET … 3x3 grid of NINE game icons …" (sidecar prompt == the TRUE sent prompt VERBATIM — design-lab s48/batch4/prompts/hollow_icons_v3c.txt bytes, 1333 B studio-tail ×1 [RESTAMPED MC 11088 FIX-ROUND-2 2026-10-22: the prior “byte-equal to the seeds.jsonl prompt_sent AS SENT” was a PRODUCER MIS-RECORDING — prompt_sent carried the studio tail TWICE (1510 chars), the orchestrator double-appended it when writing the producer row; corrected sidecar `prompt_correction` block + seeds.jsonl appended correction row]; t2i — no style-ref anchor, seed+sidecar is the provenance, same structure as the retired v2 row) | 2011804690 | `skeleton/resources/generated/icons/hollow_icons_v3_final.json` | producer recompose: cell-bbox crop + re-centre in 341px cells (fit 205, PIL LANCZOS downscale); §5 occupancy census on SHIPPED bytes 20.1-35.9% max 35.9 PASS (instrument python3-PIL alpha≥8); no edge clamp (native matte, corner4 max 0) |
