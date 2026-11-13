#!/usr/bin/env python3
"""Outline-font sidecar checker (art-unity-font-01). Standard library only.

Usage:  python3 -I -B scripts/check_fonts.py [--root DIR] [--zip GALMURI_ZIP] [--quiet]
  --root  package folder holding src/, export/ and fonts/ (default: this package)
  --zip   also compare every font / licence file byte for byte with the official release zip (its sha256 must be the
          contract's source.sha256)
Exit codes: 0 = all checks passed, 1 = at least one check failed, 2 = usage error.

Judged against src/font_contract_v1.json (hand-written), never against the manifest's own claims:
  ROLE     manifest roles = contract roles; each role names the contract file
  PATH     relative, '/'-separated, under contract fontDir, no '', '.', '..', backslash, ':' or absolute path
  FILE     a regular file (not a link) exists at that path (paths are relative to --root)
  HASH     file bytes hash to the contract sha256 (manifest sha256 too)
  SFNT     the file parses as SFNT version 00010000 (TrueType outlines) with every contract table; unitsPerEm
  GLYF     the glyf table is not empty, loca ranges stay inside it, and every covered code point maps to a glyph with
           a real outline (contours != 0). A bitmap-only font (glyf 0 bytes, EBDT strikes) fails here.
  CMAP     every covered code point is mapped (Hangul syllables U+AC00-D7A3, printable ASCII, UI symbols, samples, space)
  GRID     every covered outline point and advance lies on gridUnits; gridUnits x basePx = unitsPerEm
  SIZE     basePx is a positive integer and uiScalePx[s] = basePx x s (manifest and contract)
  NAME     name table family = contract family, version starts with contract nameVersionPrefix
  LICENSE  licence notice file exists, hash, required text, no Reserved Font Name in the copyright header
  SOURCE   manifest source = contract source, zip member names; with --zip: zip sha256 and byte-identical members
  STALE    the contract changed after the manifest was built
Nothing here imports or renders the fonts in Unity (that is NOT_RUN in this package)."""
import argparse, hashlib, json, os, sys, zipfile

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import sfnt

ROLE_KEYS = ("file", "sha256", "family", "unitsPerEm", "gridUnits", "basePx", "uiScalePx")


class Report:
    def __init__(self):
        self.errors, self.fonts_ok = [], 0

    def fail(self, code, msg):
        self.errors.append(f"[{code}] {msg}")


def sha(b):
    return hashlib.sha256(b).hexdigest()


def load_json(path, R, code):
    try:
        with open(path, encoding="utf-8") as f:
            return json.load(f)
    except FileNotFoundError:
        R.fail(code, f"missing file: {os.path.basename(path)}")
    except (json.JSONDecodeError, UnicodeDecodeError) as e:
        R.fail(code, f"invalid JSON {os.path.basename(path)}: {e}")
    return None


def safe_rel(path, prefix):
    if not isinstance(path, str) or not path or path.startswith("/") or "\\" in path or ":" in path:
        return False
    if any(part in ("", ".", "..") for part in path.split("/")):
        return False
    return path.startswith(prefix)


def read_file(root, label, path, prefix, R):
    """PATH + FILE: returns the bytes or None"""
    if not safe_rel(path, prefix):
        R.fail("PATH", f"{label}: path {path!r} is not a safe relative path under {prefix}")
        return None
    p = os.path.join(root, *path.split("/"))
    if os.path.islink(p) or not os.path.isfile(p):
        R.fail("FILE", f"{label}: file missing: {path}")
        return None
    with open(p, "rb") as f:
        return f.read()


def coverage(C):
    cv = C["coverage"]
    pts = set(range(cv["hangulSyllables"][0], cv["hangulSyllables"][1] + 1))
    pts |= set(range(cv["asciiPrintable"][0], cv["asciiPrintable"][1] + 1))
    pts |= {ord(c) for c in cv["uiSymbols"]}
    pts |= {ord(c) for s in cv["samples"] for c in s if c != " "}
    return sorted(pts)


def show(cps):
    return ", ".join(f"U+{c:04X} {chr(c)!r}" for c in cps[:4]) + (" ..." if len(cps) > 4 else "")


def check_font(role, want, b, C, PTS, R):
    """SFNT, GLYF, CMAP, GRID, NAME on the bytes of one font"""
    S = C["sfnt"]
    try:
        F = sfnt.Font(b)
        if F.version.hex() != S["version"]:
            R.fail("SFNT", f"{role}: sfnt version {F.version.hex()} != {S['version']} (TrueType outlines)")
            return False
        miss = [t for t in S["requiredTables"] if t not in F.tables]
        if miss:
            R.fail("SFNT", f"{role}: tables missing {miss}")
            return False
        if F.units_per_em != want["unitsPerEm"]:
            R.fail("SFNT", f"{role}: unitsPerEm {F.units_per_em} != contract {want['unitsPerEm']}")
        nm = F.names()
        if nm.get(1) != want["family"]:
            R.fail("NAME", f"{role}: family name {nm.get(1)!r} != contract {want['family']!r}")
        if not str(nm.get(5, "")).startswith(S["nameVersionPrefix"]):
            R.fail("NAME", f"{role}: version name {nm.get(5)!r} does not start with {S['nameVersionPrefix']!r}")
        glen = F.table("glyf")[1]
        if glen == 0:
            R.fail("GLYF", f"{role}: glyf table is 0 bytes - no outlines at all (a bitmap-only font is not an outline font)")
        for gid in range(F.num_glyphs):
            F.glyph_range(gid)                                   # loca must stay inside glyf
        cm = F.cmap()
        unmapped = [c for c in PTS if c not in cm]
        if C["coverage"]["space"] not in cm:
            unmapped.insert(0, C["coverage"]["space"])
        if unmapped:
            R.fail("CMAP", f"{role}: {len(unmapped)} covered code points not mapped ({show(unmapped)})")
        empty = [c for c in PTS if c in cm and F.glyph_contours(cm[c]) == 0]
        if empty:
            R.fail("GLYF", f"{role}: {len(empty)} of {len(PTS)} covered code points map to an EMPTY glyph ({show(empty)})")
        g = want["gridUnits"]
        off = []
        for c in PTS:
            if c in cm and c not in empty:
                gid = cm[c]
                if F.advance(gid) % g or any(x % g or y % g for x, y in F.glyph_coords(gid)):
                    off.append(c)
        if off:
            R.fail("GRID", f"{role}: {len(off)} covered glyphs have points or advances off the {g}-unit grid ({show(off)})")
        return not (unmapped or empty or off or glen == 0)
    except sfnt.SfntError as e:
        R.fail("SFNT", f"{role}: not a readable TrueType font: {e}")
        return False


def check_size(role, want, r, R):
    base, g = want["basePx"], want["gridUnits"]
    if type(base) is not int or base < 1:
        R.fail("SIZE", f"{role}: contract basePx {base!r} is not a positive integer")
        return
    if g * base != want["unitsPerEm"]:
        R.fail("SIZE", f"{role}: gridUnits {g} x basePx {base} != unitsPerEm {want['unitsPerEm']}")
    expect = {s: base * int(s) for s in ("1", "2", "3")}
    if want["uiScalePx"] != expect:
        R.fail("SIZE", f"{role}: contract uiScalePx {want['uiScalePx']} != basePx x scale {expect}")
    for k in ("basePx", "gridUnits", "uiScalePx"):
        if r.get(k) != want[k]:
            R.fail("SIZE", f"{role}: manifest {k} {r.get(k)!r} != contract {want[k]!r}")


def check_license(root, C, M, R):
    L = C["license"]
    ml = M.get("license") if isinstance(M.get("license"), dict) else {}
    if ml.get("file") != L["file"] or ml.get("name") != L["name"] or ml.get("sha256") != L["sha256"]:
        R.fail("LICENSE", "manifest license file/name/sha256 differ from the contract")
    b = read_file(root, "licence", ml.get("file", L["file"]), C["fontDir"], R)
    if b is None:
        return
    if sha(b) != L["sha256"]:
        R.fail("HASH", f"licence: {ml.get('file')} bytes differ from the contract sha256")
    try:
        t = b.decode("utf-8")
    except UnicodeDecodeError:
        R.fail("LICENSE", "licence notice is not UTF-8 text")
        return
    for s in L["mustContain"]:
        if s not in t:
            R.fail("LICENSE", f"licence notice does not contain {s!r}")
    head = t.lower().split("licensed under")[0]
    if L["headerMustNotContain"].lower() in head:
        R.fail("LICENSE", f"copyright header declares a {L['headerMustNotContain']}")


def check_source(root, C, M, zip_path, R):
    if M.get("source") != C["source"]:
        R.fail("SOURCE", "manifest source differs from contract source")
    if not zip_path:
        return
    try:
        zb = open(zip_path, "rb").read()
    except OSError as e:
        R.fail("SOURCE", f"cannot read zip: {e.strerror}")
        return
    if sha(zb) != C["source"]["sha256"]:
        R.fail("SOURCE", f"zip sha256 {sha(zb)[:12]}... != contract source.sha256 {C['source']['sha256'][:12]}...")
        return
    z = zipfile.ZipFile(zip_path)
    for name, member in C["source"]["members"].items():
        try:
            data = z.read(member)
        except KeyError:
            R.fail("SOURCE", f"zip has no member {member}")
            continue
        p = os.path.join(root, *C["fontDir"].rstrip("/").split("/"), name)
        if not os.path.isfile(p) or open(p, "rb").read() != data:
            R.fail("SOURCE", f"{C['fontDir']}{name} is not byte-identical to zip member {member}")


def main(argv=None):
    ap = argparse.ArgumentParser()
    ap.add_argument("--root", default=os.path.dirname(HERE))
    ap.add_argument("--zip")
    ap.add_argument("--quiet", action="store_true")
    try:
        a = ap.parse_args(argv)
    except SystemExit as e:
        return 2 if e.code else 0
    root = os.path.abspath(a.root)
    R = Report()
    cpath = os.path.join(root, "src", "font_contract_v1.json")
    C = load_json(cpath, R, "CONTRACT")
    M = load_json(os.path.join(root, "export", "font_manifest_v1.json"), R, "MANIFEST")
    if C is None or M is None:
        return finish(R, a.quiet, 0)
    need = ("fontDir", "source", "sfnt", "roles", "coverage", "license")
    if C.get("schema") != "bible-survivors.font-contract" or C.get("schemaVersion") != 1 or any(k not in C for k in need):
        R.fail("CONTRACT", f"contract must be bible-survivors.font-contract v1 with {list(need)}")
        return finish(R, a.quiet, 0)
    if M.get("schema") != "bible-survivors.font-manifest" or M.get("schemaVersion") != 1 or not isinstance(M.get("roles"), dict):
        R.fail("MANIFEST", "manifest must be bible-survivors.font-manifest v1 with a roles object")
        return finish(R, a.quiet, 0)
    if M.get("contract", {}).get("sha256") != sha(open(cpath, "rb").read()):
        R.fail("STALE", "contract changed after the manifest was built; run scripts/build_font_package.py")
    PTS = coverage(C)
    extra = sorted(set(M["roles"]) - set(C["roles"]))
    if extra:
        R.fail("ROLE", f"manifest has roles the contract does not know: {extra}")
    for role, want in C["roles"].items():
        r = M["roles"].get(role)
        if not isinstance(r, dict):
            R.fail("ROLE", f"role '{role}' missing from the manifest")
            continue
        if r.get("file") != want["file"]:
            R.fail("ROLE", f"{role}: manifest file {r.get('file')!r} != contract {want['file']!r}")
        if r.get("sha256") != want["sha256"]:
            R.fail("HASH", f"{role}: manifest sha256 differs from the contract")
        member = C["source"]["members"].get(want["file"].rsplit("/", 1)[-1])
        if member is None or r.get("zipMember") != member:
            R.fail("SOURCE", f"{role}: zipMember {r.get('zipMember')!r} != contract member {member!r}")
        check_size(role, want, r, R)
        b = read_file(root, role, r.get("file"), C["fontDir"], R)
        if b is None:
            continue
        if sha(b) != want["sha256"]:
            R.fail("HASH", f"{role}: {r.get('file')} bytes differ from the contract sha256")
        if check_font(role, want, b, C, PTS, R):
            R.fonts_ok += 1
    check_license(root, C, M, R)
    check_source(root, C, M, a.zip, R)
    return finish(R, a.quiet, len(C["roles"]), len(PTS))


def finish(R, quiet, n, pts=0):
    if R.errors:
        print(f"FAIL: {len(R.errors)} problem(s) ({R.fonts_ok}/{n} fonts passed the outline checks)")
        for line in R.errors[: (40 if quiet else 400)]:
            print("  " + line)
        return 1
    print(f"PASS: {R.fonts_ok}/{n} roles - TrueType outlines, {pts} covered code points each with a non-empty on-grid "
          f"glyph, sizes, names, hashes, licence, source")
    return 0


if __name__ == "__main__":
    sys.exit(main())
