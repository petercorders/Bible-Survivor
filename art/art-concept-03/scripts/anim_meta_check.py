#!/usr/bin/env python3
"""Check the review animations (review/anim/*.gif and *_apng.png) against the hand-written contract (standard library only).

Usage:  python3 -I scripts/anim_meta_check.py
For every review animation the frame count, per-frame delay and loop flag are read from the file bytes and compared with
src/contract_v3.json (the player timeline, or the asset's frames / fps / loop):
  GIF   delays are stored in 1/100 s, so the expected delay is round(100 / fps) centiseconds (8 fps -> 12 cs = 120 ms);
        looping = a NETSCAPE2.0 block with count 0, once-only = no NETSCAPE2.0 block.
  APNG  acTL num_frames and num_plays (0 = loop, 1 = once); every fcTL delay within 1 ms of 1000 / fps.
This reads metadata only. It does not show that the motion looks right: watch review/index.html or the files for that.
Writes tests/results/anim_metadata.txt. Exit 0 = every file matches, 1 = a mismatch or a missing partner file."""
import json, os, struct, sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
ANIM = os.path.join(ROOT, "review", "anim")


def gif_meta(path):
    d = open(path, "rb").read()
    if d[:6] not in (b"GIF87a", b"GIF89a"):
        raise ValueError("not a GIF")
    pos = 13
    if d[10] & 0x80:
        pos += 3 * (2 << (d[10] & 7))
    frames, delays, loop = 0, [], None

    def skip_sub(p):
        while d[p]:
            p += d[p] + 1
        return p + 1
    while pos < len(d):
        b = d[pos]
        if b == 0x3B:
            break
        if b == 0x21:
            label = d[pos + 1]
            if label == 0xF9:
                delays.append(struct.unpack("<H", d[pos + 4:pos + 6])[0])
            elif label == 0xFF and d[pos + 3:pos + 14] == b"NETSCAPE2.0":
                loop = struct.unpack("<H", d[pos + 16:pos + 18])[0]
            pos = skip_sub(pos + 2)
        elif b == 0x2C:
            frames += 1
            flags = d[pos + 9]
            pos += 10
            if flags & 0x80:
                pos += 3 * (2 << (flags & 7))
            pos = skip_sub(pos + 1)
        else:
            raise ValueError(f"unexpected byte {b:#x} at {pos}")
    return frames, delays, loop


def apng_meta(path):
    d = open(path, "rb").read()
    pos, actl, delays = 8, None, []
    while pos < len(d):
        n = struct.unpack(">I", d[pos:pos + 4])[0]
        t, body = d[pos + 4:pos + 8], d[pos + 8:pos + 8 + n]
        if t == b"acTL":
            actl = struct.unpack(">II", body)
        elif t == b"fcTL":
            num, den = struct.unpack(">HH", body[20:24])
            delays.append(1000.0 * num / (den or 100))
        pos += 12 + n
    if actl is None:
        raise ValueError("no acTL chunk (not an APNG)")
    return actl[0], delays, actl[1]


def expected(name, C):
    if name.startswith("player_"):
        anim = name.split("_")[2]
        return C["player"]["animations"][anim], f"contract player.animations.{anim}"
    assets = {a["id"]: a for a in C["assets"]}
    if name.startswith("fx_jawbone-swing"):
        return assets["fx_jawbone-swing-edge_hit_64"], "contract fx_jawbone-swing-edge_hit_64"
    for aid, a in assets.items():
        if aid.rsplit("_", 1)[0] == name:
            return a, f"contract {aid}"
    raise KeyError(name)


def main():
    C = json.load(open(os.path.join(ROOT, "src", "contract_v3.json"), encoding="utf-8"))
    names = sorted(f[:-4] for f in os.listdir(ANIM) if f.endswith(".gif"))
    lines, ok = [], True
    for nm in names:
        try:
            spec, src = expected(nm, C)
        except KeyError:
            lines.append(f"FAIL  {nm}: no contract entry to compare with"); ok = False
            continue
        n, fps, loop = spec["frames"], spec["fps"], spec["loop"]
        want_cs = round(100 / fps)
        gf, gd, gl = gif_meta(os.path.join(ANIM, nm + ".gif"))
        probs = []
        if gf != n:
            probs.append(f"gif frames {gf} != {n}")
        if any(x != want_cs for x in gd) or len(gd) != gf:
            probs.append(f"gif delays {sorted(set(gd))} cs != {want_cs}")
        if (gl == 0) != loop or (not loop and gl is not None):
            probs.append(f"gif loop block {gl} for loop={loop}")
        ap = os.path.join(ANIM, nm + "_apng.png")
        if not os.path.exists(ap):
            probs.append("apng missing")
        else:
            af, ad, plays = apng_meta(ap)
            if af != n or len(ad) != n:
                probs.append(f"apng frames {af}/{len(ad)} != {n}")
            if any(abs(x - 1000 / fps) > 1 for x in ad):
                probs.append(f"apng delays {sorted(set(round(x, 2) for x in ad))} ms != {1000 / fps:.2f}")
            if plays != (0 if loop else 1):
                probs.append(f"apng num_plays {plays} for loop={loop}")
        ok &= not probs
        lines.append(f"{'OK  ' if not probs else 'FAIL'}  {nm:36s} frames {n} · {fps} fps · {'loop' if loop else 'once'} "
                     f"| gif {want_cs * 10} ms ({100 / want_cs:.2f} fps) · apng {1000 / fps:.2f} ms  [{src}]"
                     + ("  " + "; ".join(probs) if probs else ""))
    head = ["review animation metadata vs src/contract_v3.json (metadata only: the motion itself is not judged here)",
            f"files: {len(names)} GIF + {sum(os.path.exists(os.path.join(ANIM, x + '_apng.png')) for x in names)} APNG", ""]
    res = "ANIM_META: " + ("PASS" if ok else "FAIL")
    os.makedirs(os.path.join(ROOT, "tests", "results"), exist_ok=True)
    with open(os.path.join(ROOT, "tests", "results", "anim_metadata.txt"), "w", encoding="utf-8") as f:
        f.write("\n".join(head + lines + ["", res]) + "\n")
    print("\n".join(lines[-3:]))
    print(res)
    return 0 if ok else 1


if __name__ == "__main__":
    sys.exit(main())
