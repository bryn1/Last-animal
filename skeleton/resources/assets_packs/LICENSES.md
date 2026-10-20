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


## Fleet-generated assets — S48 Batch-3 FX/props (MC 11014 / 10026.44.2, 2026-10-20)

Fleet-generated in-house work (owner project), route B: render through BEN2 matting to a true-alpha
cutout, generator `/opt/imagegen Qwen-Image via harness imagegen MCP` (model Qwen-Image-2512,
8-step Lightning, t2i/fast — read from each sidecar). NOT third-party content; clean-room sentence
embedded in every prompt. Full verbatim prompt + seed + mode + quality + refs live in the `.json`
sidecar beside each PNG (STYLE_BIBLE §6 format). FX hue: the aura/lightning/nova assets implement
"FX-hue ruling option A (owner 2026-10-20)" — cited per row below; no FX-slot code consumes these
assets yet (engine wiring is a later code card).
POST-PROCESS (MC 11014, 2026-10-20, idiom of MC 10759): each of the 17 square (1024x1024) PNGs
additionally carries a CPU corner-haze clamp — `skeleton/ci/edge_clamp.py` zeroes the outer 8-px
border ring's alpha in place, recorded as a `postprocess` block in the same sidecar; the
`alpha_proof` block is re-measured post-clamp with the pre-clamp corner kept as
`pre_max_corner_alpha`. The four flipbook strips (non-square, `*_strip_*`) ship unclamped: their
cells carry no 4-corner semantics. In-house work either way: the clamp transforms fleet-generated
pixels and introduces no third-party content.

| Asset | License | Provenance row | Seed | Sidecar | Post-process (CPU) |
|---|---|---|---|---|---|
| `skeleton/resources/generated/fx/fire_pool_surface_a.png` | fleet-generated owner-project | fleet-generated owner-project (Qwen-Image-2512, route B, prompt+seed sidecar) — prompt head: "Game FX texture asset: a flat ground-level burning oil poo …" | 387089559 | `skeleton/resources/generated/fx/fire_pool_surface_a.json` | `edge_clamp.py` border-alpha clamp (8-px ring) |
| `skeleton/resources/generated/fx/fire_pool_surface_b.png` | fleet-generated owner-project | fleet-generated owner-project (Qwen-Image-2512, route B, prompt+seed sidecar) — prompt head: "Game FX texture asset: a smaller dying-out burning pool on …" | 1624714885 | `skeleton/resources/generated/fx/fire_pool_surface_b.json` | `edge_clamp.py` border-alpha clamp (8-px ring) |
| `skeleton/resources/generated/fx/flame_strip_8f.png` | fleet-generated owner-project | fleet-generated owner-project (Qwen-Image-2512, route B, prompt+seed sidecar) — prompt head: "Game animation flipbook sheet: ONE horizontal row of exact …" | 1654526242 | `skeleton/resources/generated/fx/flame_strip_8f.json` | — (flipbook strip, ships unclamped) |
| `skeleton/resources/generated/fx/torch_flame_strip_6f.png` | fleet-generated owner-project | fleet-generated owner-project (Qwen-Image-2512, route B, prompt+seed sidecar) — prompt head: "Game animation flipbook sheet: ONE horizontal row of exact …" | 1446772122 | `skeleton/resources/generated/fx/torch_flame_strip_6f.json` | — (flipbook strip, ships unclamped) |
| `skeleton/resources/generated/fx/lightning_strip_5f.png` | fleet-generated owner-project | fleet-generated owner-project (Qwen-Image-2512, route B, prompt+seed sidecar) — prompt head: "Game animation flipbook sheet: ONE horizontal row of exact …" — FX-hue ruling option A (owner 2026-10-20) | 2065601086 | `skeleton/resources/generated/fx/lightning_strip_5f.json` | — (flipbook strip, ships unclamped) |
| `skeleton/resources/generated/fx/nova_strip_6f.png` | fleet-generated owner-project | fleet-generated owner-project (Qwen-Image-2512, route B, prompt+seed sidecar) — prompt head: "Game animation flipbook sheet: ONE horizontal row of exact …" — FX-hue ruling option A (owner 2026-10-20) | 513874034 | `skeleton/resources/generated/fx/nova_strip_6f.json` | — (flipbook strip, ships unclamped) |
| `skeleton/resources/generated/fx/aura_disc_a.png` | fleet-generated owner-project | fleet-generated owner-project (Qwen-Image-2512, route B, prompt+seed sidecar) — prompt head: "Game FX texture asset: a glowing magic aura ring lying fla …" — FX-hue ruling option A (owner 2026-10-20) | 1318171999 | `skeleton/resources/generated/fx/aura_disc_a.json` | `edge_clamp.py` border-alpha clamp (8-px ring) |
| `skeleton/resources/generated/fx/aura_disc_b.png` | fleet-generated owner-project | fleet-generated owner-project (Qwen-Image-2512, route B, prompt+seed sidecar) — prompt head: "Game FX texture asset: a subtle dark magic aura ring lying …" — FX-hue ruling option A (owner 2026-10-20) | 1458344254 | `skeleton/resources/generated/fx/aura_disc_b.json` | `edge_clamp.py` border-alpha clamp (8-px ring) |
| `skeleton/resources/generated/fx/aura_disc_c.png` | fleet-generated owner-project | fleet-generated owner-project (Qwen-Image-2512, route B, prompt+seed sidecar) — prompt head: "Game FX texture asset: a bright blessed magic aura ring ly …" — FX-hue ruling option A (owner 2026-10-20) | 2128096377 | `skeleton/resources/generated/fx/aura_disc_c.json` | `edge_clamp.py` border-alpha clamp (8-px ring) |
| `skeleton/resources/generated/props/blood_splat_1.png` | fleet-generated owner-project | fleet-generated owner-project (Qwen-Image-2512, route B, prompt+seed sidecar) — prompt head: "Game decal texture asset: a small dried blood splat lying …" | 1954938967 | `skeleton/resources/generated/props/blood_splat_1.json` | `edge_clamp.py` border-alpha clamp (8-px ring) |
| `skeleton/resources/generated/props/blood_splat_2.png` | fleet-generated owner-project | fleet-generated owner-project (Qwen-Image-2512, route B, prompt+seed sidecar) — prompt head: "Game decal texture asset: a medium blood puddle with drip …" | 1099012379 | `skeleton/resources/generated/props/blood_splat_2.json` | `edge_clamp.py` border-alpha clamp (8-px ring) |
| `skeleton/resources/generated/props/blood_splat_3.png` | fleet-generated owner-project | fleet-generated owner-project (Qwen-Image-2512, route B, prompt+seed sidecar) — prompt head: "Game decal texture asset: a long blood drag smear lying fl …" | 145443777 | `skeleton/resources/generated/props/blood_splat_3.json` | `edge_clamp.py` border-alpha clamp (8-px ring) |
| `skeleton/resources/generated/props/blood_splat_4.png` | fleet-generated owner-project | fleet-generated owner-project (Qwen-Image-2512, route B, prompt+seed sidecar) — prompt head: "Game decal texture asset: a large heavy blood splatter lyi …" | 352406673 | `skeleton/resources/generated/props/blood_splat_4.json` | `edge_clamp.py` border-alpha clamp (8-px ring) |
| `skeleton/resources/generated/props/blood_splat_5.png` | fleet-generated owner-project | fleet-generated owner-project (Qwen-Image-2512, route B, prompt+seed sidecar) — prompt head: "Game decal texture asset: a fine arterial spray fan of blo …" | 448792342 | `skeleton/resources/generated/props/blood_splat_5.json` | `edge_clamp.py` border-alpha clamp (8-px ring) |
| `skeleton/resources/generated/props/blood_splat_6.png` | fleet-generated owner-project | fleet-generated owner-project (Qwen-Image-2512, route B, prompt+seed sidecar) — prompt head: "Game decal texture asset: an old nearly dried dark blood p …" | 73149019 | `skeleton/resources/generated/props/blood_splat_6.json` | `edge_clamp.py` border-alpha clamp (8-px ring) |
| `skeleton/resources/generated/props/prop_barrel.png` | fleet-generated owner-project | fleet-generated owner-project (Qwen-Image-2512, route B, prompt+seed sidecar) — prompt head: "Game prop texture asset: a single dark wooden medieval bar …" | 176913128 | `skeleton/resources/generated/props/prop_barrel.json` | `edge_clamp.py` border-alpha clamp (8-px ring) |
| `skeleton/resources/generated/props/prop_crate.png` | fleet-generated owner-project | fleet-generated owner-project (Qwen-Image-2512, route B, prompt+seed sidecar) — prompt head: "Game prop texture asset: a single dark wooden medieval sto …" | 687345327 | `skeleton/resources/generated/props/prop_crate.json` | `edge_clamp.py` border-alpha clamp (8-px ring) |
| `skeleton/resources/generated/props/prop_plank_pile.png` | fleet-generated owner-project | fleet-generated owner-project (Qwen-Image-2512, route B, prompt+seed sidecar) — prompt head: "Game prop texture asset: a low messy pile of broken wooden …" | 1330520519 | `skeleton/resources/generated/props/prop_plank_pile.json` | `edge_clamp.py` border-alpha clamp (8-px ring) |
| `skeleton/resources/generated/props/prop_bone_heap.png` | fleet-generated owner-project | fleet-generated owner-project (Qwen-Image-2512, route B, prompt+seed sidecar) — prompt head: "Game prop texture asset: a small heap of old bones and rib …" | 355044644 | `skeleton/resources/generated/props/prop_bone_heap.json` | `edge_clamp.py` border-alpha clamp (8-px ring) |
| `skeleton/resources/generated/props/prop_skull.png` | fleet-generated owner-project | fleet-generated owner-project (Qwen-Image-2512, route B, prompt+seed sidecar) — prompt head: "Game prop texture asset: a single old human skull resting …" | 229412729 | `skeleton/resources/generated/props/prop_skull.json` | `edge_clamp.py` border-alpha clamp (8-px ring) |
| `skeleton/resources/generated/props/prop_rubble.png` | fleet-generated owner-project | fleet-generated owner-project (Qwen-Image-2512, route B, prompt+seed sidecar) — prompt head: "Game prop texture asset: a small scatter of broken stone r …" | 10146281 | `skeleton/resources/generated/props/prop_rubble.json` | `edge_clamp.py` border-alpha clamp (8-px ring) |
