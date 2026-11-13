#!/usr/bin/env python3
"""Negative-input CLI test for scripts/check_fonts.py (standard library only).

Usage:  python3 -I -B tests/test_check_fonts.py [--zip GALMURI_ZIP] [--bitmap-dir DIR]
  --zip         the official Galmuri 2.40.4 zip; enables the cases that need it (otherwise they are SKIPPED, not passed)
  --bitmap-dir  folder with the art 03 Galmuri*Bitmap*.ttf files (default ../art-concept-03/third_party/galmuri)
Copies src/, export/ and fonts/ into a fresh temporary folder per case, breaks ONE thing, runs the real checker CLI
(and for build_refuses_overwrite the real build CLI) as a subprocess. When a case changes font or licence bytes it also
writes the new sha256 into the contract and the manifest (and the contract hash into the manifest), so only the
targeted check can catch it. A broken copy counts as rejected only with exit 1, a first line 'FAIL:', no Python
traceback and a line starting with the expected [CODE]. The untouched copy must exit 0.
Writes  tests/results/check_font_negative_results.json and .txt. Exit 0 only if every expectation holds."""
import argparse, hashlib, json, os, shutil, struct, subprocess, sys, tempfile, zipfile

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
sys.path.insert(0, os.path.join(ROOT, "scripts"))
import sfnt

CHECK = os.path.join(ROOT, "scripts", "check_fonts.py")
BUILD = os.path.join(ROOT, "scripts", "build_font_package.py")
OUT = os.path.join(HERE, "results")
FD = "fonts/galmuri-outline/"


def sha(b):
    return hashlib.sha256(b).hexdigest()


def jload(t, rel):
    with open(os.path.join(t, rel), encoding="utf-8") as f:
        return json.load(f)


def jsave(t, rel, d):
    with open(os.path.join(t, rel), "w", encoding="utf-8") as f:
        json.dump(d, f, ensure_ascii=False, indent=1)


CON, MAN = "src/font_contract_v1.json", "export/font_manifest_v1.json"


def contract_edit(t, fn):
    """edit the contract and keep the manifest's record of the contract hash coherent (so STALE does not fire)"""
    c = jload(t, CON); fn(c); jsave(t, CON, c)
    m = jload(t, MAN)
    with open(os.path.join(t, CON), "rb") as f:
        m["contract"]["sha256"] = sha(f.read())
    jsave(t, MAN, m)


def manifest_edit(t, fn):
    m = jload(t, MAN); fn(m); jsave(t, MAN, m)


def font_path(t, role):
    return os.path.join(t, *jload(t, CON)["roles"][role]["file"].split("/"))


def set_font(t, role, data, contract_fn=None):
    """replace a role's font bytes and make every recorded hash agree with them"""
    with open(font_path(t, role), "wb") as f:
        f.write(data)
    def fn(c):
        c["roles"][role]["sha256"] = sha(data)
        if contract_fn:
            contract_fn(c)
    contract_edit(t, fn)
    manifest_edit(t, lambda m: m["roles"][role].update(sha256=sha(data)))


def set_license(t, text):
    data = text.encode("utf-8")
    with open(os.path.join(t, *(FD + "LICENSE.txt").split("/")), "wb") as f:
        f.write(data)
    contract_edit(t, lambda c: c["license"].update(sha256=sha(data)))
    manifest_edit(t, lambda m: m["license"].update(sha256=sha(data)))


def font_bytes(t, role):
    with open(font_path(t, role), "rb") as f:
        return bytearray(f.read())


def table_entry(b, tag):
    n = struct.unpack_from(">H", b, 4)[0]
    for i in range(n):
        o = 12 + 16 * i
        if b[o:o + 4] == tag.encode("latin-1"):
            return o
    raise KeyError(tag)


# ---- cases (each gets a fresh copy t)
def file_missing(t, ctx):
    """body font file deleted"""
    os.remove(font_path(t, "body"))


def bitmap_as_outline(t, ctx):
    """art 03 Galmuri11Bitmap-Regular put in the label slot, and the contract + manifest rewritten to that file's own
    hash, family, version prefix, unitsPerEm 1000, basePx 10: everything agrees except that it has no outlines"""
    p = os.path.join(ctx["bitmap_dir"], "Galmuri11Bitmap-Regular-2.40.4.ttf")
    data = open(p, "rb").read()
    def fn(c):
        c["roles"]["label"].update(family="Galmuri11Bitmap", unitsPerEm=1000, gridUnits=100, basePx=10,
                                   uiScalePx={"1": 10, "2": 20, "3": 30})
        c["sfnt"]["nameVersionPrefix"] = "Version 2.40"
    set_font(t, "label", data, fn)
    manifest_edit(t, lambda m: m["roles"]["label"].update(basePx=10, gridUnits=100, uiScalePx={"1": 10, "2": 20, "3": 30}))


def empty_glyf_synthetic(t, ctx):
    """Galmuri7 rewritten with a 0-byte glyf table and an all-zero loca (hashes coherent)"""
    b = font_bytes(t, "small")
    F = sfnt.Font(bytes(b))
    struct.pack_into(">I", b, table_entry(b, "glyf") + 12, 0)
    lo_off, lo_len = F.table("loca")
    b[lo_off:lo_off + lo_len] = bytes(lo_len)
    set_font(t, "small", bytes(b))


def sample_glyph_empty(t, ctx):
    """only the glyph of '가' in Galmuri9 made empty (its loca start moved to its end; hashes coherent)"""
    b = font_bytes(t, "body")
    F = sfnt.Font(bytes(b))
    gid = F.cmap()[ord("가")]
    lo_off = F.table("loca")[0]
    if F.index_to_loc_format == 0:
        struct.pack_into(">H", b, lo_off + 2 * gid, struct.unpack_from(">H", b, lo_off + 2 * (gid + 1))[0])
    else:
        struct.pack_into(">I", b, lo_off + 4 * gid, struct.unpack_from(">I", b, lo_off + 4 * (gid + 1))[0])
    set_font(t, "body", bytes(b))


def not_truetype(t, ctx):
    """the licence text saved as the body font (hashes coherent)"""
    set_font(t, "body", open(os.path.join(t, *(FD + "LICENSE.txt").split("/")), "rb").read())


def cff_version(t, ctx):
    """title font's SFNT version changed to 'OTTO' (CFF outlines, no glyf expected; hashes coherent)"""
    b = font_bytes(t, "title"); b[0:4] = b"OTTO"
    set_font(t, "title", bytes(b))


def truncated(t, ctx):
    """label font cut to half its size (hashes coherent)"""
    b = font_bytes(t, "label")
    set_font(t, "label", bytes(b[:len(b) // 2]))


def loca_out_of_range(t, ctx):
    """small font's last loca entry pointed far past the glyf table (hashes coherent)"""
    b = font_bytes(t, "small")
    F = sfnt.Font(bytes(b))
    lo_off = F.table("loca")[0]
    n = F.num_glyphs
    if F.index_to_loc_format == 0:
        struct.pack_into(">H", b, lo_off + 2 * n, 0xFFFF)
    else:
        struct.pack_into(">I", b, lo_off + 4 * n, 0x7FFFFFFF)
    set_font(t, "small", bytes(b))


def hash_bad(t, ctx):
    """one byte of the body font changed, hashes left alone"""
    b = font_bytes(t, "body"); b[-1] ^= 1
    with open(font_path(t, "body"), "wb") as f:
        f.write(bytes(b))


def role_missing(t, ctx):
    """manifest role 'small' removed"""
    manifest_edit(t, lambda m: m["roles"].pop("small"))


def role_swapped(t, ctx):
    """manifest title and label entries swapped (each still names a real file with its true hash)"""
    def fn(m):
        m["roles"]["title"], m["roles"]["label"] = m["roles"]["label"], m["roles"]["title"]
    manifest_edit(t, fn)


def role_extra(t, ctx):
    """manifest gains a role 'huge' the contract does not define"""
    manifest_edit(t, lambda m: m["roles"].update(huge=dict(m["roles"]["title"])))


def path_unsafe(t, ctx):
    """manifest body file -> fonts/galmuri-outline/../galmuri-outline/Galmuri9.ttf (resolves to the real file)"""
    manifest_edit(t, lambda m: m["roles"]["body"].update(file=FD + "../galmuri-outline/Galmuri9.ttf"))


def size_not_integer(t, ctx):
    """body basePx 10.5 in contract and manifest"""
    contract_edit(t, lambda c: c["roles"]["body"].update(basePx=10.5))
    manifest_edit(t, lambda m: m["roles"]["body"].update(basePx=10.5))


def scale_px_wrong(t, ctx):
    """manifest title uiScalePx['2'] = 23"""
    manifest_edit(t, lambda m: m["roles"]["title"]["uiScalePx"].update({"2": 23}))


def grid_claim_6px(t, ctx):
    """label claimed at basePx 6 / gridUnits 200 (6 x 200 = 1200, coherent in contract and manifest): the font's points
    sit on 100-unit steps, so at 6 px they fall between pixels"""
    v = dict(basePx=6, gridUnits=200, uiScalePx={"1": 6, "2": 12, "3": 18})
    contract_edit(t, lambda c: c["roles"]["label"].update(v))
    manifest_edit(t, lambda m: m["roles"]["label"].update(v))


def cmap_symbol_missing(t, ctx):
    """contract requires '►' (Galmuri7 has no glyph for it)"""
    contract_edit(t, lambda c: c["coverage"]["uiSymbols"].append("►"))


def name_wrong(t, ctx):
    """contract title family 'Galmuri11 Regular' (the bold file's name table says 'Galmuri11 Bold')"""
    contract_edit(t, lambda c: c["roles"]["title"].update(family="Galmuri11 Regular"))


def license_missing(t, ctx):
    """LICENSE.txt deleted"""
    os.remove(os.path.join(t, *(FD + "LICENSE.txt").split("/")))


def license_text(t, ctx):
    """licence replaced by 'All rights reserved.' (hashes coherent)"""
    set_license(t, "Copyright (c) 2019-2025 Somebody. All rights reserved.\n")


def license_rfn(t, ctx):
    """'with Reserved Font Name \"Galmuri\"' added to the copyright header (hashes coherent)"""
    text = open(os.path.join(t, *(FD + "LICENSE.txt").split("/")), encoding="utf-8").read()
    first, rest = text.split("\n", 1)
    set_license(t, first + ', with Reserved Font Name "Galmuri".\n' + rest)


def source_mismatch(t, ctx):
    """manifest source.version 2.40.3"""
    manifest_edit(t, lambda m: m["source"].update(version="2.40.3"))


def stale_contract(t, ctx):
    """contract edited after the build (manifest still records the old contract hash)"""
    c = jload(t, CON); c["note"] += " edited"; jsave(t, CON, c)


def zip_wrong(t, ctx):
    """--zip given a different zip (a small zip made here)"""
    z = os.path.join(t, "other.zip")
    with zipfile.ZipFile(z, "w") as f:
        f.writestr("Galmuri11.ttf", b"not the release")
    ctx["args"] = ["--zip", z]


def zip_member_differs(t, ctx):
    """real release zip, but the packaged licence was changed (hashes coherent): only the byte compare can catch it"""
    text = open(os.path.join(t, *(FD + "LICENSE.txt").split("/")), encoding="utf-8").read()
    set_license(t, text.replace("https://openfontlicense.org", "https://openfontlicense.org/"))
    ctx["args"] = ["--zip", ctx["zip"]]


def build_refuses_overwrite(t, ctx):
    """build --zip run on a copy whose Galmuri9.ttf differs from the zip member: must exit 1 and leave the file alone"""
    p = os.path.join(t, *(FD + "Galmuri9.ttf").split("/"))
    b = bytearray(open(p, "rb").read()); b[-1] ^= 1
    open(p, "wb").write(bytes(b))
    ctx["before"] = sha(bytes(b))
    ctx["cmd"] = "build"
    ctx["args"] = ["--zip", ctx["zip"]]
    ctx["after_check"] = lambda: sha(open(p, "rb").read()) == ctx["before"]


CASES = [  # name, function, expected code, needs
    ("file_missing", file_missing, "[FILE]", None),
    ("bitmap_as_outline", bitmap_as_outline, "[GLYF]", "bitmap"),
    ("empty_glyf_synthetic", empty_glyf_synthetic, "[GLYF]", None),
    ("sample_glyph_empty", sample_glyph_empty, "[GLYF]", None),
    ("not_truetype", not_truetype, "[SFNT]", None),
    ("cff_version", cff_version, "[SFNT]", None),
    ("truncated", truncated, "[SFNT]", None),
    ("loca_out_of_range", loca_out_of_range, "[SFNT]", None),
    ("hash_bad", hash_bad, "[HASH]", None),
    ("role_missing", role_missing, "[ROLE]", None),
    ("role_swapped", role_swapped, "[ROLE]", None),
    ("role_extra", role_extra, "[ROLE]", None),
    ("path_unsafe", path_unsafe, "[PATH]", None),
    ("size_not_integer", size_not_integer, "[SIZE]", None),
    ("scale_px_wrong", scale_px_wrong, "[SIZE]", None),
    ("grid_claim_6px", grid_claim_6px, "[GRID]", None),
    ("cmap_symbol_missing", cmap_symbol_missing, "[CMAP]", None),
    ("name_wrong", name_wrong, "[NAME]", None),
    ("license_missing", license_missing, "[FILE]", None),
    ("license_text", license_text, "[LICENSE]", None),
    ("license_rfn", license_rfn, "[LICENSE]", None),
    ("source_mismatch", source_mismatch, "[SOURCE]", None),
    ("stale_contract", stale_contract, "[STALE]", None),
    ("zip_wrong", zip_wrong, "[SOURCE]", None),
    ("zip_member_differs", zip_member_differs, "[SOURCE]", "zip"),
    ("build_refuses_overwrite", build_refuses_overwrite, "EXISTS:", "zip"),
]


def run_case(fn, ctx):
    with tempfile.TemporaryDirectory(prefix="font01-neg-") as t:
        for part in ("src", "export", "fonts"):
            shutil.copytree(os.path.join(ROOT, part), os.path.join(t, part))
        if fn:
            fn(t, ctx)
        if ctx.get("cmd") == "build":
            # the build script works on its own package folder, so run a copy of it inside t
            shutil.copytree(os.path.join(ROOT, "scripts"), os.path.join(t, "scripts"))
            cmd = [sys.executable, "-I", "-B", os.path.join(t, "scripts", "build_font_package.py")] + ctx.get("args", [])
        else:
            cmd = [sys.executable, "-I", "-B", CHECK, "--root", t, "--quiet"] + ctx.get("args", [])
        r = subprocess.run(cmd, capture_output=True, text=True)
        out = (r.stdout + r.stderr).replace(t, "<tmp>")
        if ctx.get("zip"):
            out = out.replace(ctx["zip"], "<zip>")
        extra_ok = ctx["after_check"]() if "after_check" in ctx else True
        return r.returncode, out.strip().splitlines(), extra_ok


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--zip")
    ap.add_argument("--bitmap-dir", default=os.path.join(os.path.dirname(ROOT), "art-concept-03", "third_party", "galmuri"))
    a = ap.parse_args()
    have = {"zip": bool(a.zip and os.path.isfile(a.zip)),
            "bitmap": os.path.isfile(os.path.join(a.bitmap_dir, "Galmuri11Bitmap-Regular-2.40.4.ttf"))}
    os.makedirs(OUT, exist_ok=True)
    results, ok = [], True
    code, lines, _ = run_case(None, {})
    results.append({"case": "positive (untouched copy)", "expect": "0", "exit": code, "pass": code == 0, "output": lines[:2]})
    ok &= code == 0
    if have["zip"]:
        code, lines, _ = run_case(None, {"args": ["--zip", a.zip], "zip": a.zip})
        results.append({"case": "positive + --zip release", "expect": "0", "exit": code, "pass": code == 0, "output": lines[:2]})
        ok &= code == 0
    for name, fn, tag, need in CASES:
        if need and not have[need]:
            results.append({"case": name, "what": fn.__doc__.strip(), "expect": f"1 {tag}", "exit": None, "pass": None,
                            "output": [f"SKIPPED: needs --{'zip' if need == 'zip' else 'bitmap-dir'}"]})
            continue
        ctx = {"zip": a.zip, "bitmap_dir": a.bitmap_dir}
        code, lines, extra_ok = run_case(fn, ctx)
        if ctx.get("cmd") == "build":
            good = code == 1 and any(x.startswith(tag) for x in lines) and extra_ok and not any("Traceback" in x for x in lines)
        else:
            good = (code == 1 and bool(lines) and lines[0].startswith("FAIL:") and not any("Traceback" in x for x in lines)
                    and any(x.strip().startswith(tag) for x in lines))
        ok &= good
        hit = next((x.strip() for x in lines if x.strip().startswith(tag)), lines[1].strip() if len(lines) > 1 else "")
        codes = sorted({x.strip().split("]")[0] + "]" for x in lines if x.strip().startswith("[")})
        results.append({"case": name, "what": fn.__doc__.strip(), "expect": f"1 {tag}", "exit": code, "pass": good,
                        "codes": codes, "hit": hit, "output": lines[:6]})
    skipped = [r["case"] for r in results if r["pass"] is None]
    with open(os.path.join(OUT, "check_font_negative_results.json"), "w", encoding="utf-8") as f:
        json.dump({"checker": "scripts/check_fonts.py", "python": sys.version.split()[0], "zipCasesRun": have["zip"],
                   "bitmapCaseRun": have["bitmap"], "skipped": skipped, "results": results}, f, ensure_ascii=False, indent=1)
    with open(os.path.join(OUT, "check_font_negative_results.txt"), "w", encoding="utf-8") as f:
        for r in results:
            state = "SKIP" if r["pass"] is None else ("ok  " if r["pass"] else "BAD ")
            f.write(f"{state} {r['case']:26s} exit={r['exit']}  expect={r['expect']:10s} codes={','.join(r.get('codes', []))}  "
                    f"{r.get('hit') or (r['output'][0] if r['output'] else '')}\n")
        f.write(f"\nNEGATIVE TEST: {'PASS' if ok else 'FAIL'}" + (f" (skipped: {', '.join(skipped)})" if skipped else "") + "\n")
    print(open(os.path.join(OUT, "check_font_negative_results.txt"), encoding="utf-8").read(), end="")
    return 0 if ok else 1


if __name__ == "__main__":
    sys.exit(main())
