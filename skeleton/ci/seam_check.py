#!/usr/bin/env python3
# seam_check.py — S47 tileability gate (MC 10523; hardened MC 10776 per DA-10759).
# One concern: measure the seam, like-for-like, with an absolute floor.
# Metric: build a 2x2 tiled composite; the seam is the two interior wrap lines
# (col W-1|W, row H-1|H); per-pixel gradient = max abs diff across RGB of one
# adjacent pair. The like-for-like bar (DA §3a): seam_p95 vs an equal-N interior
# p95 — the interior pool is TRIMMED to the same sample count N as the seam, so
# both sides are the same statistic of the same N (seam_max vs a p95 over 8.4M
# made a truly seamless plate FAIL — DA row: iid-noise plate seam 88 vs p95 50).
# ABSOLUTE FLOOR: the interior must carry texture — interior_p95 >= FLOOR_INT (3)
# — else a solid plate cannot PASS (DA row: solid gray 0/0 PASSed).
# REPORT-ONLY (not gated yet, DA §3b): low-freq term = the DC luminance step at
# the wrap, mean(luma strip of 40 px inside each wrap line) across the line,
# max abs over the four strips — one number that flags baked edge lighting.
# Exit codes: 0 all PASS, 1 any FAIL, 2 usage/plumbing (SEAM_OUT is created).
import os
import sys
import json
try:
    import numpy  # noqa: F401
except ImportError:  # HOME-redirected runners (CI sets HOME=.knut) lose ~/.local
    for _c in ("/home/claudecode/.local/lib/python3.10/site-packages",):
        if os.path.isdir(_c) and _c not in sys.path:
            sys.path.append(_c)
import numpy as np
from PIL import Image

FLOOR_INT = 3          # interior p95 must exceed this for a PASS (texture exists)
STRIP = 40             # low-freq report term: 40-px luma strip at the wrap line


def check(path, outdir):
    im = Image.open(path).convert("RGB")
    w, h = im.size
    tile = np.asarray(im, dtype=np.int16)
    half = np.concatenate((tile, tile), axis=1)
    comp = np.concatenate((half, half), axis=0)
    stem = os.path.splitext(os.path.basename(path))[0]
    Image.fromarray(comp.astype(np.uint8)).save(os.path.join(outdir, stem + "_2x2.png"))
    dv = np.abs(comp[:, 1:, :] - comp[:, :-1, :]).max(axis=2)  # col-adjacent
    dh = np.abs(comp[1:, :, :] - comp[:-1, :, :]).max(axis=2)  # row-adjacent
    seam_max = int(max(dv[:, w - 1].max(), dh[h - 1, :].max()))
    seam_pool = np.concatenate((dv[:, w - 1], dh[h - 1, :]))
    interior = np.concatenate(
        (np.delete(dv, w - 1, axis=1).ravel(), np.delete(dh, h - 1, axis=0).ravel()))
    n = int(seam_pool.size)                       # like-for-like: ONE N for both sides
    # Equal-N quantile curve (DA §3a): the interior is reduced to the SAME number of
    # points as the seam — n evenly spaced ranks of the sorted full pool — so BOTH
    # sides answer one statistic (p95) on n samples. The old skew (max vs p95) made a
    # truly seamless iid plate FAIL (DA row: seam 88 vs 50 — same distribution, two
    # different statistics). TOL=1 is the standard-error of a p95 on n samples.
    rank = np.round(np.linspace(0, interior.size - 1, n)).astype(int)
    interior_curve = np.sort(interior)[rank]
    seam_p95 = float(np.percentile(seam_pool, 95))
    interior_p95 = float(np.percentile(interior_curve, 95))
    luma = comp[:, :, :3] @ np.array([0.2126, 0.7152, 0.0722], dtype=np.float64)
    s_top = luma[:STRIP, :].mean(axis=0)          # strip inside the horizontal wrap (row 0 side)
    s_bot = luma[-STRIP:, :].mean(axis=0)         # ... and the other side (row 2H-1)
    s_left = luma[:, :STRIP].mean(axis=1)
    s_right = luma[:, -STRIP:].mean(axis=1)
    lowfreq = max(abs(s_top[:w] - s_bot[:w]).max(), abs(s_left[:h] - s_right[:h]).max())
    # TOL: p95 of integer gradients on n=4096 samples — the two equal-N arms differ
    # by at most 1 unit for a truly seamless (same-distribution) plate.
    verdict = "PASS" if (seam_p95 <= interior_p95 + 1.0
                         and interior_p95 >= FLOOR_INT) else "FAIL"
    return {
        "file": os.path.basename(path), "size": "%dx%d" % (w, h),
        "seam_max": seam_max, "seam_p95": round(seam_p95, 2),
        "interior_p95": round(interior_p95, 2), "n": n,
        "floor_int": FLOOR_INT,
        "lowfreq_step": round(float(lowfreq), 2),
        "verdict": verdict,
    }


if __name__ == "__main__":
    outdir = os.environ.get("SEAM_OUT", ".")
    os.makedirs(outdir, exist_ok=True)             # no traceback on a missing dir (DA P3)
    if len(sys.argv) < 2:
        print("usage: seam_check.py <plate.png> [...]", file=sys.stderr)
        raise SystemExit(2)
    rows = [check(p, outdir) for p in sys.argv[1:]]
    for r in rows:
        print(json.dumps(r))
    raise SystemExit(0 if all(r["verdict"] == "PASS" for r in rows) else 1)
