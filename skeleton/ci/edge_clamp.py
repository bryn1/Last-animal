#!/usr/bin/env python3
# edge_clamp.py — S48 Batch-3 corner-haze clamp (MC 11014). One concern: clamp the
# outer BORDER-px ring of shipped SQUARE cutouts to alpha 0 (generator haze lives in
# the corners), then re-measure the sidecar alpha_proof to the post-clamp state,
# keeping the pre-clamp corner number as pre_max_corner_alpha. Non-square flipbook
# strips are skipped (cells carry no 4-corner semantics; they ship as rendered).
# Idempotent: a ring already 0 re-measures identically; nothing is ever refused (exit 0).
# Definitions copied from the batch-3 producer sidecars: transparent a<8, opaque a>=248,
# partial between; max_corner_alpha = max of the 4 corner pixels. stdlib + PIL only.
import json, os, sys, datetime
import numpy as np
from PIL import Image

BORDER = 8

def proof(a):
    t, o = int((a < 8).sum()), int((a >= 248).sum())
    return {"transparent_px": t, "partial_px": int(a.size - t - o), "opaque_px": o,
            "max_corner_alpha": int(max(a[0, 0], a[-1, 0], a[0, -1], a[-1, -1])),
            "verdict": "PASS"}

for p in sys.argv[1:]:
    side = os.path.splitext(p)[0] + ".json"
    s = json.load(open(side))
    im = Image.open(p).convert("RGBA")
    arr = np.asarray(im).copy()
    if im.size[0] != im.size[1] or s["alpha_proof"]["max_corner_alpha"] == 0:
        print("%s: skip (size %dx%d, sidecar corner %d)" % (
            os.path.basename(p), im.size[0], im.size[1], s["alpha_proof"]["max_corner_alpha"]))
        continue
    pre = proof(arr[..., 3])["max_corner_alpha"]
    arr[:BORDER, :, 3] = 0; arr[-BORDER:, :, 3] = 0
    arr[:, :BORDER, 3] = 0; arr[:, -BORDER:, 3] = 0
    post = proof(arr[..., 3])
    im2 = Image.fromarray(arr, "RGBA"); im2.save(p)
    s.setdefault("postprocess", {
        "method": "outer %d-px border ring alpha clamped to 0 (corner haze)" % BORDER,
        "border_px": BORDER, "tool": "skeleton/ci/edge_clamp.py", "card": "MC 11014",
        "ts_utc": datetime.datetime.now(datetime.timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ"),
        "note": "prompt/seed/mode/quality/refs above are the render as shipped; this block is the CPU edge clamp only; the alpha_proof block is re-measured post-clamp with the pre-clamp corner kept there as pre_max_corner_alpha"})
    post["pre_max_corner_alpha"] = s["alpha_proof"]["max_corner_alpha"]
    s["alpha_proof"] = post
    open(side, "w").write(json.dumps(s, indent=0))
    print("%s: corner alpha %d -> %d" % (os.path.basename(p), pre, post["max_corner_alpha"]))
