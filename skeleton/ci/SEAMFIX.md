# MC 10759 — S47b CPU seam-fix pass over the five S47 plates (2026-10-16)

Branch `s47-textures` (worktree `/home/claudecode/la-work/s47`), off the S47 generation tip 4a6c381.
No push, no merge. Author of all three commits: `code (MC 10759) <code@agent-town.local>`.

- `3ece659` ci(s47): `skeleton/ci/seam_fix.py` — the wrap-fix tool (103 l, NEW).
- `dc44e2c` assets(s47): the five plates + five sidecars, post-process applied.
- (tip) docs(s47): LICENSES post-process rows + gate PASS + this card's LEDGER row.

## 1. Gate before → after (DoD 2) — `python3 skeleton/ci/seam_check.py skeleton/resources/generated/*.png`

| plate | seam_max before | interior_p95 before | seam_max AFTER | interior_p95 AFTER | margin (p95/seam) | verdict |
|---|---|---|---|---|---|---|
| ground_dirt.png  | 118 | 32.0 | **3** | 30.0 | 10.0x | **PASS** |
| ground_moss.png  | 120 | 20.0 | **3** | 19.0 | 6.3x  | **PASS** |
| ground_stone.png | 172 | 28.0 | **6** | 27.0 | 4.5x  | **PASS** |
| wall_rock.png    | 224 | 54.0 | **7** | 53.0 | 7.6x  | **PASS** |
| wall_stone.png   | 149 | 42.0 | **3** | 41.0 | 13.7x | **PASS** |

Before column = the shipped files re-measured at 4a6c381 (`../attempts.json` a1 rows; scratch copy
`/home/claudecode/la-work/s47/.tmp/seamfix/baseline_before.txt` reproduces 118/120/172/224/149 exactly). After column
was produced by the committed gate at this tip, verbatim in `red_proof/gate_proof_final.txt`, and the
gate's own 2x2 composites are in `fixed/*_2x2.png`.

**The pass is not bought by lowering the bar**: interior_p95 moved by at most 2 units on every plate
(32→30, 20→19, 28→27, 54→53, 42→41) while seam_max fell 29–50x (118/3, 120/3, 172/6, 224/7, 149/3). The blend band is ~15% of the composite's
gradient pool (1 - (1024-80)^2/1024^2 = 15.0%), so the percentile could not shift much — that is stated so nobody has to
re-derive it.

## 2. Method (DoD 1) — edge-offset blend on the plate torus

`skeleton/ci/seam_fix.py`, pure PIL + numpy (the deps `seam_check.py` already uses), no GPU, no network.

A tile is a torus, so the plate is blurred with **periodic support**: its 3x3 period tiling goes through
ONE isotropic gaussian (`ImageFilter.GaussianBlur`, radius = `SEAM_SIGMA`) and the centre copy is cropped
back — no clamp term and no mirror term at the plate's own edges. Every pixel is then pulled toward that
blur under a periodic raised-cosine weight `w` over the *periodic* distance to the wrap line: `w = 1` on
both wrap lines, `w = 0` at `SEAM_BAND` px inside, and `max(w_x, w_y)` so all four edges and the corners
are one pass. On a wrap line the crossing pixel pair therefore samples ONE smooth periodic field, so the
seam gradient collapses to that field's own local gradient instead of the generator's edge step.

Params `sigma = 10`, `band = 40` were **chosen by sweep, not taste**: `{6,8,10,12,16,20} x {24,32,40,48,64,80}`
on all five plates, plus a two-pass per-axis variant and 1–2 iterations. The single-blur union-weight form
beat the two-pass form (same or lower seam_max, one blur, less banding); `10/40` is the smallest setting
that keeps ≥ 4.5x margin on every plate (ground_stone 6 vs p95 27 is the binding plate; 6/24 drops it to
2.7x, 8/32 to 3.4x).

### What the pass costs, measured (not assumed)

| plate | max abs diff OUTSIDE the ±40 px frame | % of band pixels changed | mean 1-px gradient in the 16 px at the edge (x / y) before → after | PNG bytes before → after | metadata chunks after |
|---|---|---|---|---|---|
| ground_dirt  | **0** | 76.6% | 3.39 / 4.81 → 0.55 / 0.73 | 1684211 → 1642895 | 0 |
| ground_moss  | **0** | 77.3% | 4.27 / 4.38 → 0.66 / 0.68 | 1703938 → 1652737 | 0 |
| ground_stone | **0** | 71.0% | 3.56 / 4.40 → 0.61 / 0.79 | 1521120 → 1486190 | 0 |
| wall_rock    | **0** | 76.4% | 2.75 / 6.68 → 0.50 / 1.30 | 1187867 → 1171717 | 0 |
| wall_stone   | **0** | 72.2% | 3.18 / 3.60 → 0.58 / 0.65 | 1269195 → 1249285 | 0 |

So: detail is traded **only** inside a ±40 px frame at the four plate edges (that is the named cost of an
edge-offset blend, S47 handback), everything outside it is byte-identical to the render, and the
metadata-strip property survives (`Image.frombytes` re-encode; tEXt/iTXt/zTXt/eXIf/iCCP/gAMA/sRGB all 0).
Bytes shrank on all five. Re-applying the tool REFUSES once the sidecar records the post-process
(`SEAM_FIX_FORCE=1` is the documented override), so a double-apply cannot silently re-smooth a plate.

Provenance (DoD 4): each sidecar keeps `prompt` / `seed` / `size` / `mode` / `quality` / `refs` /
`generator` / `ts_utc` / `target_for` exactly as `asset_finalize.py` wrote them and gains ONE
`postprocess` block: method sentence, `sigma`, `band_px`, `tool`, `card`, `ts_utc`, and a note naming what
is *not* post-processed. Seeds unchanged: 4711 / 4721 / 4731 / 4741 / 4751.

## 3. Gate red-proof at this commit (DoD 6)

Transcript: `red_proof/gate_proof_final.txt` (it carries `git log -1` inside, so the sha binding lives in
the evidence, not in a filename).

- GREEN: the five shipped plates → five `"verdict": "PASS"` lines, exit 0.
- RED (planted): the FIXED `wall_rock.png` with its **left 256 columns rolled 64 rows** — a real wrap
  mismatch with nothing to do with the generator →
  `{"file": "wall_rock_planted_bad.png", "seam_max": 158, "interior_p95": 53.0, "verdict": "FAIL"}`,
  reproduced byte-identically on a second reading with a different `SEAM_OUT`.

**P3 named, NOT fixed (scope):** `seam_check.py` exits **0 whatever it prints** — its contract is the
printed verdict line, so any caller that reads only `$?` would call five FAILs green. Wiring an exit code
is a change to the gate's own card; it does not get smuggled in under a fix pass. (The gate has no
automated caller in the tree today — grep finds only LICENSES.md prose and its own file.)

## 4. Visual QC

512-px crops centred on the seam crossing of the 2x2 composite, before vs after:
`../ground_stone_seamcrop_BEFORE.png` vs `fixed/ground_stone_seamcrop.png` (plus
`fixed/{ground_dirt,wall_stone}_seamcrop.png`, `fixed/*_2x2_small.png`). The hard slab/mortar cut at the
boundary is gone — slab edges and grit now continue across it, and the ±40 px blend is not visible as a
band at normal viewing. INFERRED (human-eye level, one reader), not a pixel gate.

## 5. Lines CARRIED for S41 (DoD 7) — measured, and NOT fixed by any wrap-fix

1. **`wall_rock.png` pale sky band along the top edge** (the S47 handback flagged it INFERRED; here it is
   MEASURED). Rows **0–76** each have a majority of pixels above luma 150; rows 0–31 mean luma **202.3**
   against a plate mean of **74.2**; 63728 of the 65536 px in rows 0–63 are above 150. This pass reaches
   only **185.3** for rows 0–31 / **60052** px / 73 majority-pale rows (**-5.8%**), and the horizontal-wrap
   mean-luma step 141.9 → **124.1**. The band is 77 px tall against a 40 px seam band, so an edge-offset
   blend physically cannot remove it: it needs a crop or repaint, and cropping those 77 rows leaves 947
   rows whose restore to Po2 1024 is an **8.1% vertical stretch** — a composition call for the integration
   gate, which is why it was NOT applied unilaterally here.
2. **Generator-baked edge lighting** (a STYLE_BIBLE "no lighting gradient toward the edges" miss, seen as a
   LOW-frequency mean-luma step across the wrap, 60-px strips, before → after): ground_stone vertical
   **+34.2 → +29.9**; wall_stone vertical +22.8 → +19.7, horizontal +28.3 → +23.7; ground_dirt +10.3/+10.2 →
   +9.1/+9.9; ground_moss +4.6/+6.6 → +3.6/+5.5; wall_rock vertical +3.3 → +2.4. The hard line is gone but
   tiling still shows this lighting swing (visible in `fixed/ground_stone_seamcrop.png`: lit slabs meeting
   shadowed slabs at the boundary — present identically before the pass, `ground_stone_seamcrop_BEFORE.png`).
   A low-frequency wrap-equalization (distributed per-row / per-column cosine ramp) WOULD remove it, but it
   repaints each plate's whole luminance field, so it is named for a ruling rather than applied here.
3. Re-generation with a true-tiling pipeline stays the durable answer for both rows above; 12 draws at S47
   measured that prompt wording and seed do not move the seam metric (dirt series 118 → 197 → 234).

## 6. Reproduce

```sh
cd /home/claudecode/la-work/s47                      # branch s47-textures
python3 skeleton/ci/seam_fix.py skeleton/resources/generated/*.png   # REFUSES here: sidecars already record it
SEAM_OUT=/tmp python3 skeleton/ci/seam_check.py skeleton/resources/generated/*.png
```

Clean-checkout proof (WORKED FROM CLEAN CHECKOUT, not just this workspace): a fresh
`git clone --branch s47-textures` of the branch at this tip runs the gate VERBATIM and prints the same
five PASS lines (`seamfix_clean_clone_gate.txt`), and the same clone REFUSES a re-apply of the tool
(`seamfix_clean_clone_guard.txt`, exit 1) — so the shipped bytes, not my working tree, are what passes.

Raw measurement dump behind §1/§2: `seamfix_measures.json` (same run also wrote `seamfix_apply.txt`,
`seamfix_after_gate.txt`). Scratch that produced the sweep lives in the branch worktree's gitignored
`.tmp/seamfix/` (proto.py + the param sweep), not in the tree.
