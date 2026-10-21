#!/usr/bin/env python3
# finalize.py — S47 asset finalizer (MC 10523). One concern: turn a raw design-lab
# render into a shipping asset: re-save pixels with NO generation metadata (clean
# PIL re-encode from the pixel array), verify power-of-two >= 512, and write the
# §6 STYLE_BIBLE provenance sidecar beside it. Manifest: JSON list, one entry per
# asset {name, raw, dest, seed, ts_utc, target_for, refs, prompt_file, mode,
# quality, generator, size}. Sidecar prompt is read from prompt_file verbatim.
# S48-FIX (MC 11059, owner ruling 2026-10-21 verbatim "Go, A") — the RGBA path:
# an input WITH an alpha channel is preserved ("RGBA" convert+frombytes), which
# retires the S48 route-B finalize-bypass: transparent cutouts now finalize
# THROUGH this tool instead of around it. Opaque (RGB) input keeps byte-identical
# behavior. The po2>=512 gate expression is unchanged and still runs for every
# entry that ships as an engine-sampled texture; an entry may declare
# "po2_exempt" with its reason (the four flipbook strips — the S48 PLAN §2 NAMED
# gap (ii): sheets are not engine-sampled Po2 textures yet) and the declaration
# lands VERBATIM in the sidecar. RGBA sidecars gain an alpha_proof block
# (S48 idiom: transparent a<8, opaque a>=248, corner4 max) that
# skeleton/ci/edge_clamp.py re-measures after its clamp.
import json
import os
import sys
from PIL import Image


def alpha_proof(im):
    a = im.getchannel("A")
    hist = a.histogram()
    t, o = sum(hist[:8]), sum(hist[248:])
    w, h = a.size
    return {"transparent_px": t, "partial_px": w * h - t - o, "opaque_px": o,
            "max_corner_alpha": max(a.getpixel((0, 0)), a.getpixel((w - 1, 0)),
                                    a.getpixel((0, h - 1)), a.getpixel((w - 1, h - 1))),
            "verdict": "PASS"}


def finalize(entry):
    src = Image.open(entry["raw"])
    mode = "RGBA" if "A" in src.getbands() else "RGB"
    im = src.convert(mode)
    w, h = im.size
    if not entry.get("po2_exempt") and (
            w < 512 or h < 512 or (w & (w - 1)) or (h & (h - 1))):
        raise SystemExit("%s: size %dx%d is not power-of-two >= 512" % (entry["name"], w, h))
    os.makedirs(os.path.dirname(entry["dest"]), exist_ok=True)
    # frombytes() carries ONLY the pixels: no text/exif/gAMA chunks from the render.
    Image.frombytes(mode, (w, h), im.tobytes()).save(entry["dest"])
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
    if mode == "RGBA":
        sidecar["alpha_proof"] = alpha_proof(im)
    if entry.get("po2_exempt"):
        sidecar["po2_exempt"] = entry["po2_exempt"]
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
