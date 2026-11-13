#!/usr/bin/env python3
"""Build the outline-font sidecar (art-unity-font-01). Standard library only.

Usage:  python3 -I -B scripts/build_font_package.py [--zip GALMURI_ZIP] [--out DIR]
  --zip  the official release zip named in src/font_contract_v1.json (source.url). Its sha256 must equal source.sha256.
         Each contract member is copied to fonts/galmuri-outline/ byte for byte. A file that is already there must be
         identical (exit 1 otherwise): this script never overwrites a font or licence file.
  --out  write the manifest under DIR/export/ instead of this folder (used by the regeneration check).
Writes  export/font_manifest_v1.json: per role the file, sha256, size, names and what was MEASURED in the font itself
        (pixel grid = gcd of every covered point and advance -> basePx = unitsPerEm / grid, line metrics in px,
        coverage counts, how many glyphs have outline data). Same input -> same bytes (no dates, no machine paths).
Exit    0 ok, 1 source/zip/existing-file problem, 2 usage error.
Nothing here imports, renders or tests the fonts in Unity."""
import argparse, hashlib, json, math, os, sys, zipfile

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
sys.path.insert(0, HERE)
import sfnt

MANIFEST = os.path.join("export", "font_manifest_v1.json")


def sha(b):
    return hashlib.sha256(b).hexdigest()


def coverage_points(C):
    cv = C["coverage"]
    pts = set(range(cv["hangulSyllables"][0], cv["hangulSyllables"][1] + 1))
    pts |= set(range(cv["asciiPrintable"][0], cv["asciiPrintable"][1] + 1))
    pts |= {ord(c) for c in cv["uiSymbols"]}
    pts |= {ord(c) for s in cv["samples"] for c in s if c != " "}
    return sorted(pts)


def measure(F, C):
    """values measured in the font file itself"""
    cm = F.cmap()
    pts = coverage_points(C)
    gids = sorted({cm[c] for c in pts if c in cm})
    g = F.units_per_em
    for gid in gids:
        for x, y in F.glyph_coords(gid):
            g = math.gcd(g, math.gcd(x, y))
        g = math.gcd(g, F.advance(gid))
    lo = F.loca()
    outline = sum(1 for i in range(F.num_glyphs) if lo[i + 1] > lo[i])
    hh = F.table("hhea")[0]
    asc, desc, gap = F._i16(hh + 4), F._i16(hh + 6), F._i16(hh + 8)
    nm = F.names()
    return {"sfntVersion": F.version.hex(), "tables": sorted(F.tables), "family": nm.get(1), "version": nm.get(5),
            "unitsPerEm": F.units_per_em, "glyphs": F.num_glyphs, "glyfBytes": F.table("glyf")[1],
            "glyphsWithOutlineData": outline, "measuredGridUnits": g, "measuredBasePx": F.units_per_em / g,
            "lineMetricsAtBasePx": {"ascender": asc / g, "descender": desc / g, "lineGap": gap / g,
                                    "lineHeight": (asc - desc + gap) / g},
            "coverage": {"codePointsChecked": len(pts), "mapped": sum(1 for c in pts if c in cm),
                         "withOutline": sum(1 for c in pts if c in cm and F.glyph_contours(cm[c]) != 0)}}


def main(argv=None):
    ap = argparse.ArgumentParser()
    ap.add_argument("--zip")
    ap.add_argument("--out")
    try:
        a = ap.parse_args(argv)
    except SystemExit as e:
        return 2 if e.code else 0
    C = json.load(open(os.path.join(ROOT, "src", "font_contract_v1.json"), encoding="utf-8"))
    fdir = os.path.join(ROOT, *C["fontDir"].rstrip("/").split("/"))
    if a.zip:
        zb = open(a.zip, "rb").read()
        if sha(zb) != C["source"]["sha256"]:
            print(f"SOURCE: zip sha256 {sha(zb)} != contract source.sha256 {C['source']['sha256']}")
            return 1
        z = zipfile.ZipFile(a.zip)
        os.makedirs(fdir, exist_ok=True)
        for name, member in C["source"]["members"].items():
            data = z.read(member)
            dst = os.path.join(fdir, name)
            if os.path.exists(dst):
                if open(dst, "rb").read() != data:
                    print(f"EXISTS: {C['fontDir']}{name} differs from zip member {member}; not overwritten")
                    return 1
                print(f"same     {C['fontDir']}{name}")
            else:
                with open(dst, "xb") as f:
                    f.write(data)
                print(f"copied   {C['fontDir']}{name} <- zip:{member}")
    roles = {}
    for role, want in C["roles"].items():
        p = os.path.join(ROOT, *want["file"].split("/"))
        b = open(p, "rb").read()
        m = measure(sfnt.Font(b), C)
        base = m["measuredBasePx"]
        roles[role] = {"file": want["file"], "sha256": sha(b), "bytes": len(b),
                       "zipMember": C["source"]["members"][os.path.basename(want["file"])],
                       "basePx": int(base) if base == int(base) else base, "gridUnits": m["measuredGridUnits"],
                       "uiScalePx": {s: int(base * int(s)) for s in ("1", "2", "3")} if base == int(base) else None,
                       "use": want["use"], "measured": m}
    lp = os.path.join(ROOT, *C["license"]["file"].split("/"))
    manifest = {"schema": "bible-survivors.font-manifest", "schemaVersion": 1, "status": "handoff-draft",
                "statusNote": "outline UI fonts beside art 03 (sidecar). Unity import, TextMeshPro/dynamic font rendering "
                              "and devices: NOT_RUN. art 03 Galmuri*Bitmap*.ttf are bitmap-only (glyf 0 bytes) and are "
                              "neither replaced nor used here.",
                "contract": {"file": "src/font_contract_v1.json",
                             "sha256": sha(open(os.path.join(ROOT, "src", "font_contract_v1.json"), "rb").read())},
                "source": C["source"],
                "license": {"file": C["license"]["file"], "sha256": sha(open(lp, "rb").read()), "name": C["license"]["name"]},
                "sizeRule": C["sizeRule"], "roles": roles,
                "build": {"command": "python3 -I -B scripts/build_font_package.py [--zip <Galmuri-v2.40.4.zip>]",
                          "python": sys.version.split()[0]}}
    out = os.path.join(a.out or ROOT, MANIFEST)
    os.makedirs(os.path.dirname(out), exist_ok=True)
    with open(out, "w", encoding="utf-8") as f:
        json.dump(manifest, f, ensure_ascii=False, indent=1)
        f.write("\n")
    for role, r in roles.items():
        m = r["measured"]
        print(f"{role:6s} {os.path.basename(r['file']):20s} basePx {r['basePx']} (grid {r['gridUnits']}/{m['unitsPerEm']}) "
              f"glyf {m['glyfBytes']} B, outline glyphs {m['glyphsWithOutlineData']}/{m['glyphs']}, "
              f"coverage {m['coverage']['withOutline']}/{m['coverage']['codePointsChecked']}")
    print("manifest ->", MANIFEST if not a.out else out)
    return 0


if __name__ == "__main__":
    sys.exit(main())
