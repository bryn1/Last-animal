#!/usr/bin/env python3
# seam_check.py — S47 tileability gate (MC 10523). One concern: measure the seam.
# Metric (DoD, card S47): build a 2x2 tiled composite; the tile-boundary gradient is
# the max abs row/col pixel gradient along the two interior boundary lines of the
# composite (vertical seam between cols W-1 and W, horizontal seam between rows H-1
# and H, gradients = per-pixel max abs diff across RGB channels). The interior
# gradient pool is every other adjacent-pixel gradient inside the composite.
# PASS iff seam_max <= interior 95th percentile. Composite PNGs + one JSON row per
# file land in SEAM_OUT.
import os
import sys
import json
import numpy as np
from PIL import Image


def check(path, outdir):
    im = Image.open(path).convert("RGB")
    w, h = im.size
    tile = np.asarray(im, dtype=np.int16)
    half = np.concatenate((tile, tile), axis=1)
    comp = np.concatenate((half, half), axis=0)
    stem = os.path.splitext(os.path.basename(path))[0]
    Image.fromarray(comp.astype(np.uint8)).save(os.path.join(outdir, stem + "_2x2.png"))
    dv = np.abs(comp[:, 1:, :] - comp[:, :-1, :]).max(axis=2)  # (2H, 2W-1) col-adjacent
    dh = np.abs(comp[1:, :, :] - comp[:-1, :, :]).max(axis=2)  # (2H-1, 2W) row-adjacent
    seam = int(max(dv[:, w - 1].max(), dh[h - 1, :].max()))
    interior = np.concatenate(
        (np.delete(dv, w - 1, axis=1).ravel(), np.delete(dh, h - 1, axis=0).ravel()))
    p95 = float(np.percentile(interior, 95))
    return {
        "file": os.path.basename(path), "size": "%dx%d" % (w, h),
        "seam_max": seam, "interior_p95": round(p95, 2),
        "verdict": "PASS" if seam <= p95 else "FAIL",
    }


if __name__ == "__main__":
    outdir = os.environ.get("SEAM_OUT", ".")
    for p in sys.argv[1:]:
        print(json.dumps(check(p, outdir)))
