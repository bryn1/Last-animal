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
`last-animal/.audits/202610151912-s47/out/SEAMFIX.md` and `out/fixed/`.

CARRIED FOR S41 (measured, NOT fixed by a wrap-fix — content, not seams): (1) `wall_rock.png` pale sky band
along the top edge: rows 0-76 each have a majority of pixels above luma 150 (77 rows), rows 0-31 mean luma 202.3
against a plate mean of 74.2, and 63728 of the 65536 px in rows 0-63 are above 150. The pass reaches only 185.3
for rows 0-31 / 60052 px / 73 majority-pale rows (-5.8%), and the horizontal-wrap luma step 141.9 → 124.1 — the
band is 77 px tall, far wider than the 40-px seam band, so it needs crop or repaint (cropping those 77 rows
leaves 947, and restoring Po2 1024 means an 8.1% vertical stretch: a composition call, not a seam fix). (2) Generator-baked edge lighting, seen as a
low-frequency mean-luma step across the wrap (60-px strips, before → after): ground_stone vertical +34.2 → +29.9,
wall_stone vertical +22.8 → +19.7 and horizontal +28.3 → +23.7, ground_dirt +10.3/+10.2 → +9.1/+9.9 — tiling now
has no hard line but still shows that lighting swing. Both measured per-plate in SEAMFIX.md.
