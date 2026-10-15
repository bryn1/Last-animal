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

| Asset | License | Provenance row | Seed | Sidecar |
|---|---|---|---|---|
| `skeleton/resources/generated/ground_dirt.png` | fleet-generated | fleet-generated (Qwen-Image, prompt+seed sidecar) — prompt head: "ORIGINAL artwork … dry cracked ochre clay dirt …" | 4711 | `skeleton/resources/generated/ground_dirt.json` |
| `skeleton/resources/generated/ground_moss.png` | fleet-generated | fleet-generated (Qwen-Image, prompt+seed sidecar) — prompt head: "ORIGINAL artwork … dense dark mossy grass floor …" | 4721 | `skeleton/resources/generated/ground_moss.json` |
| `skeleton/resources/generated/ground_stone.png` | fleet-generated | fleet-generated (Qwen-Image, prompt+seed sidecar) — prompt head: "ORIGINAL artwork … cracked ancient flagstone pavement …" | 4731 | `skeleton/resources/generated/ground_stone.json` |
| `skeleton/resources/generated/wall_stone.png` | fleet-generated | fleet-generated (Qwen-Image, prompt+seed sidecar) — prompt head: "ORIGINAL artwork … vertical masonry wall face …" | 4741 | `skeleton/resources/generated/wall_stone.json` |
| `skeleton/resources/generated/wall_rock.png` | fleet-generated | fleet-generated (Qwen-Image, prompt+seed sidecar) — prompt head: "ORIGINAL artwork … canyon sandstone cliff face …" | 4751 | `skeleton/resources/generated/wall_rock.json` |

SEAM-GATE STATUS (S47 DoD, `skeleton/ci/seam_check.py`): **ALL FIVE FAIL** the boundary-vs-interior-p95
tileability metric (measured 2026-10-15 22:2xZ; per-attempt table in
`last-animal/.audits/202610151912-s47/out/attempts.json`, 2x2 composites beside it). These plates are
palette/mood candidates for S41 integration, **not shipping seamless tiles** — the integration gate must
treat them accordingly (wrap-fix, decal/triplanar use, or re-generation with a true-tiling pipeline).
