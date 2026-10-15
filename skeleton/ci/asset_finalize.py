#!/usr/bin/env python3
# finalize.py — S47 asset finalizer (MC 10523). One concern: turn a raw design-lab
# render into a shipping asset: re-save pixels with NO generation metadata (clean
# PIL re-encode from the pixel array), verify power-of-two >= 512, and write the
# §6 STYLE_BIBLE provenance sidecar beside it. Manifest: JSON list, one entry per
# asset {name, raw, dest, seed, ts_utc, target_for, refs, prompt_file, mode,
# quality, generator, size}. Sidecar prompt is read from prompt_file verbatim.
import json
import os
import sys
from PIL import Image


def finalize(entry):
    im = Image.open(entry["raw"]).convert("RGB")
    w, h = im.size
    if w < 512 or h < 512 or (w & (w - 1)) or (h & (h - 1)):
        raise SystemExit("%s: size %dx%d is not power-of-two >= 512" % (entry["name"], w, h))
    os.makedirs(os.path.dirname(entry["dest"]), exist_ok=True)
    # frombytes() carries ONLY the pixels: no text/exif/gAMA chunks from the render.
    Image.frombytes("RGB", (w, h), im.tobytes()).save(entry["dest"])
    with open(entry["prompt_file"], encoding="utf-8") as fh:
        prompt = fh.read()
    sidecar = {
        "prompt": prompt,
        "seed": entry["seed"],
        "size": "%dx%d" % (w, h),
        "mode": entry["mode"],
        "quality": entry["quality"],
        "refs": entry["refs"],
        "generator": entry["generator"],
        "ts_utc": entry["ts_utc"],
        "target_for": entry["target_for"],
    }
    side_path = os.path.splitext(entry["dest"])[0] + ".json"
    with open(side_path, "w", encoding="utf-8") as fh:
        json.dump(sidecar, fh, ensure_ascii=False, indent=2)
        fh.write("\n")
    return {"file": entry["dest"], "bytes": os.path.getsize(entry["dest"]),
            "sidecar": side_path}


if __name__ == "__main__":
    with open(sys.argv[1], encoding="utf-8") as fh:
        manifest = json.load(fh)
    for entry in manifest:
        print(json.dumps(finalize(entry)))
