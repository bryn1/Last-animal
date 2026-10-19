#!/usr/bin/env python3
# seam_fix.py — S47 CPU wrap-fix (MC 10759). One concern: make a generated plate tile.
# WHY (measured, not assumed): the five S47 plates FAIL skeleton/ci/seam_check.py — the wrap
# boundary (tile col W-1 read next to col 0, row H-1 next to row 0) jumps 118-224 per pixel
# while the plate's OWN interior 95th-percentile gradient is 20-54. Qwen-Image lights the
# edges of its own frame; no prompt wording or seed moved the metric (12 draws,
# .audits/202610151912-s47/out/attempts.json). So the seam is fixed as a CPU post-process.
# METHOD (edge-offset blend). A tile is a torus, so the plate is blurred with PERIODIC
# support: its 3x3 period tiling goes through one isotropic gaussian and the centre copy is
# cropped back — no clamp, no mirror term at the plate's own edges. Each pixel is then pulled
# toward that blurred value under a periodic raised-cosine weight w: w = 1 on the two wrap
# lines, w = 0 at `band` px inside. On a wrap line the crossing pixel pair samples ONE smooth
# periodic field, so the seam gradient collapses to that field's own local gradient (measured
# 3-7 per pixel on these plates, 4.5-13x UNDER the gate bar) instead of the generator's edge
# step. Outside the band w == 0, so those pixels stay byte-identical to the render: detail is
# traded only inside a +/-`band` frame at the four plate edges, and that is the accepted cost
# of an edge-offset blend (HANDBACK-DELIVERED.md, S47). Pure PIL + numpy, no GPU, no network.
# PROVENANCE: the .json sidecar beside each PNG keeps prompt/seed/mode/quality/refs exactly as
# skeleton/ci/asset_finalize.py wrote them and gains ONE "postprocess" block (method, params,
# ts, tool). PNGs are re-encoded from the pixel array (Image.frombytes), so the S47
# metadata-strip property (0 tEXt/iTXt/zTXt/eXIf chunks) survives the pass.
# Env knobs (same idiom as seam_check.py's SEAM_OUT): SEAM_SIGMA (default 10), SEAM_BAND
# (default 40 px), SEAM_FIX_FORCE=1 to re-apply to an already-fixed plate.
import json
import os
import sys
import time

try:
    import numpy  # noqa: F401
except ImportError:  # HOME-redirected runners (CI sets HOME=.knut) lose ~/.local
    for _c in ("/home/claudecode/.local/lib/python3.10/site-packages",):
        if os.path.isdir(_c) and _c not in sys.path:
            sys.path.append(_c)
import numpy as np
from PIL import Image, ImageFilter

SIGMA = int(os.environ.get("SEAM_SIGMA", "10"))
BAND = int(os.environ.get("SEAM_BAND", "40"))
FORCE = os.environ.get("SEAM_FIX_FORCE") == "1"
METHOD = "edge-offset blend: wrap-aware isotropic gaussian (3x3 period tiling) under a " \
         "periodic raised-cosine weight, 1 on the wrap lines, 0 at `band` px inside"


def wrap_blur(tile, sigma):
    """Gaussian of `tile` (H,W,3 uint8) over periodic support; centre crop back to size."""
    tiled = np.tile(tile, (3, 3, 1))
    blurred = Image.fromarray(tiled).filter(ImageFilter.GaussianBlur(radius=sigma))
    b = np.asarray(blurred, dtype=np.float32)
    h, w = tile.shape[:2]
    return b[h:2 * h, w:2 * w, :]


def periodic_weight(n, band):
    """Raised cosine over the PERIODIC distance to index 0: 1 at the wrap line, 0 at `band`."""
    d = np.minimum(np.arange(n), n - np.arange(n)).astype(np.float32)
    w = np.zeros(n, dtype=np.float32)
    inside = d <= band
    w[inside] = 0.5 * (1.0 + np.cos(np.pi * d[inside] / band))
    return w


def fix_tile(tile, sigma=SIGMA, band=BAND):
    """Return the wrap-fixed plate (uint8, same shape) as an edge-offset blend."""
    f = tile.astype(np.float32)
    weight = np.maximum(periodic_weight(tile.shape[1], band)[None, :, None],
                        periodic_weight(tile.shape[0], band)[:, None, None])
    out = f + (wrap_blur(tile, sigma) - f) * weight
    return np.uint8(np.clip(np.rint(out), 0, 255))


def _interior_p95(tile):
    """Interior 95th-percentile RGB gradient (max abs across channels), same
    instrument as seam_check.py's pool: row/col adjacent diffs of the single tile,
    seam lines excluded. Returned rounded to 2 decimals for the sidecar."""
    f = tile.astype(np.int16)
    dv = np.abs(f[:, 1:, :] - f[:, :-1, :]).max(axis=2)
    dh = np.abs(f[1:, :, :] - f[:-1, :, :]).max(axis=2)
    h, w = f.shape[:2]
    pool = np.concatenate((np.delete(dv, min(w, dv.shape[1]) - 1, axis=1).ravel(),
                           np.delete(dh, min(h, dh.shape[0]) - 1, axis=0).ravel()))
    return round(float(np.percentile(pool, 95)), 2)


def fix_file(path):
    """Fix one plate in place and record the post-process in its sidecar."""
    side_path = os.path.splitext(path)[0] + ".json"
    with open(side_path, encoding="utf-8") as fh:
        sidecar = json.load(fh)
    if "postprocess" in sidecar and not FORCE:
        raise SystemExit("%s: sidecar already records a post-process (SEAM_FIX_FORCE=1 to re-apply)"
                         % side_path)
    im = Image.open(path).convert("RGB")
    w, h = im.size
    # Po2 >= 512 rule OWNER: skeleton/ci/asset_finalize.py:17-18; accepted copy
    # self-guarding in-place work — mirror if that bar moves.
    if w < 512 or h < 512 or (w & (w - 1)) or (h & (h - 1)):
        raise SystemExit("%s: size %dx%d is not power-of-two >= 512" % (path, w, h))
    # Knob validation (DA-10759 §1 R-B): reject degenerate values instead of
    # silently shipping a whole-plate blur (band >= n/2 makes w == 1 everywhere)
    # or a no-op (band <= 0 changes zero pixels yet used to write the block).
    if SIGMA <= 0:
        raise SystemExit("%s: SEAM_SIGMA=%d invalid (need 0 < sigma; sigma >= n is a "
                         "full-plate blur)" % (path, SIGMA))
    n_min = min(w, h)
    if BAND <= 0 or BAND >= n_min // 2:
        raise SystemExit("%s: SEAM_BAND=%d invalid (need 0 < band < %d = n/2)"
                         % (path, BAND, n_min))
    src = np.asarray(im, dtype=np.uint8)
    p95_before = _interior_p95(src)
    fixed = fix_tile(src)
    changed = int(np.count_nonzero(np.any(src != fixed, axis=2)))
    if changed == 0:
        raise SystemExit("%s: zero pixels changed (sigma=%d band=%d) — refusing to "
                         "write a postprocess block for a no-op" % (path, SIGMA, BAND))
    # frombytes() carries ONLY the pixels: the S47 metadata-strip property survives.
    Image.frombytes("RGB", (w, h), fixed.tobytes()).save(path)
    p95_after = _interior_p95(fixed)
    sidecar["postprocess"] = {
        "method": METHOD,
        "sigma": SIGMA,
        "band_px": BAND,
        "pixels_changed": changed,
        "interior_p95_before": p95_before,
        "interior_p95_after": p95_after,
        "delta_p95": round(p95_after - p95_before, 2),
        "tool": "skeleton/ci/seam_fix.py",
        "card": "MC 10759",
        "ts_utc": time.strftime("%Y-%m-%dT%H:%M:%SZ", time.gmtime()),
        "note": "generation prompt/seed/mode/quality/refs above are the render as shipped by "
                "skeleton/ci/asset_finalize.py; this block is the CPU post-process only",
    }
    with open(side_path, "w", encoding="utf-8") as fh:
        json.dump(sidecar, fh, ensure_ascii=False, indent=2)
        fh.write("\n")
    return {"file": path, "bytes": os.path.getsize(path), "sidecar": side_path,
            "postprocess": "seam_fix sigma=%d band_px=%d delta_p95=%+.2f pixels=%d"
                           % (SIGMA, BAND, p95_after - p95_before, changed)}


if __name__ == "__main__":
    if len(sys.argv) < 2:
        raise SystemExit("usage: seam_fix.py <plate.png> [...]  (in place + sidecar provenance)")
    for p in sys.argv[1:]:
        print(json.dumps(fix_file(p)))
