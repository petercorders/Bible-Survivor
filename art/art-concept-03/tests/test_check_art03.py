#!/usr/bin/env python3
"""Negative-input test for scripts/check_art03.py (standard library only).

Usage:  python3 -I tests/test_check_art03.py
Copies export/, src/ and third_party/ (the fonts the UI data names) into a fresh temporary folder per case, breaks ONE
thing, runs the real checker CLI as a subprocess and records its exit code. Every broken case must exit 1 and the
untouched copy must exit 0. Cases top_* and asset_layerorder_swap are QA A03-01 (iteration 01), font_* / manifest_font_*
are QA A03-02; top_ppu48, top_pivotspace_topleft, top_layerorder_reversed, top_framelayout_vertical, top_all_deleted
and font_ref_missing repeat QA's own mutations, with every data hash kept coherent.
Writes tests/results/check_negative_results.json and .txt. Exit 0 only if every expectation holds."""
import hashlib, json, os, shutil, subprocess, sys, tempfile

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
sys.path.insert(0, os.path.join(ROOT, "scripts"))
import pngio

CHECK = os.path.join(ROOT, "scripts", "check_art03.py")
OUT = os.path.join(HERE, "results")


def sha(p):
    return hashlib.sha256(open(p, "rb").read()).hexdigest()


def load_m(t):
    return json.load(open(os.path.join(t, "export", "manifest_v3.json"), encoding="utf-8"))


def save_m(t, m):
    json.dump(m, open(os.path.join(t, "export", "manifest_v3.json"), "w", encoding="utf-8"), ensure_ascii=False, indent=1)


def asset(m, aid):
    return next(a for a in m["assets"] if a["id"] == aid)


def png_edit(t, a, fn):
    p = os.path.join(t, a["file"])
    w, h, buf = pngio.read_rgba(p)
    w, h, buf = fn(w, h, bytearray(buf))
    pngio.write_rgba(p, w, h, bytes(buf))
    return p


# ---- cases: each gets a fresh copy (t) and must make the checker fail
def frames_short(t):
    """A1 cut to 1 frame; manifest made self-consistent (frames/size/rects/sha) -> only the contract can catch it"""
    m = load_m(t); a = asset(m, "enm_a1-small-snake_move_32")
    def cut(w, h, b):
        nw = 32
        nb = bytearray(nw * h * 4)
        for y in range(h):
            nb[y * nw * 4:(y + 1) * nw * 4] = b[y * w * 4:y * w * 4 + nw * 4]
        return nw, h, nb
    p = png_edit(t, a, cut)
    a.update(frames=1, size=[32, 32], frameRects=[[0, 0, 32, 32]], sha256=sha(p))
    save_m(t, m)


def layer_frames(t):
    """player beard idle reduced to 3 frames, manifest self-consistent"""
    m = load_m(t); a = asset(m, "chr_player_layer-beard_idle_32")
    def cut(w, h, b):
        nw = 96
        nb = bytearray(nw * h * 4)
        for y in range(h):
            nb[y * nw * 4:(y + 1) * nw * 4] = b[y * w * 4:y * w * 4 + nw * 4]
        return nw, h, nb
    p = png_edit(t, a, cut)
    a.update(frames=3, size=[96, 32], frameRects=[[i * 32, 0, 32, 32] for i in range(3)], sha256=sha(p))
    save_m(t, m)


def pivot_bad(t):
    m = load_m(t); a = asset(m, "enm_a2-fruit-moth_fly_32")
    a["pivot"] = {"px": [999, -3], "normalized": [999 / 32, -3 / 32]}
    save_m(t, m)


def layer_missing(t):
    m = load_m(t)
    a = asset(m, "chr_player_layer-hood_walk_32")
    os.remove(os.path.join(t, a["file"]))
    m["assets"] = [x for x in m["assets"] if x["id"] != a["id"]]
    save_m(t, m)


def alpha_bad(t):
    m = load_m(t); a = asset(m, "enm_a1-small-snake_move_32")
    def semi(w, h, b):
        for i in range(0, len(b), 4):
            if b[i + 3] == 255:
                b[i + 3] = 127
                break
        return w, h, b
    a["sha256"] = sha(png_edit(t, a, semi))
    save_m(t, m)


def ref_bad(t):
    m = load_m(t); asset(m, "enm_a1-small-snake_move_32")["ref"] = "monster:a9"
    save_m(t, m)


def file_missing(t):
    m = load_m(t); os.remove(os.path.join(t, asset(m, "wpn_sling-stone_spin_8")["file"]))


def hash_bad(t):
    m = load_m(t); a = asset(m, "wpn_bible-book_spin_16")
    def poke(w, h, b):
        for i in range(0, len(b), 4):
            if b[i + 3] == 255:
                b[i] ^= 1
                break
        return w, h, b
    png_edit(t, a, poke)          # sha256 left unchanged on purpose


def foot_bad(t):
    """A3 frame 2 moved up one row (sha updated): every-frame foot row check must catch it"""
    m = load_m(t); a = asset(m, "enm_a3-thorn-beast_walk_32")
    def up(w, h, b):
        cw = 32
        nb = bytearray(b)
        for y in range(h):
            for x in range(2 * cw, 3 * cw):
                src = ((y + 1) * w + x) * 4
                dst = (y * w + x) * 4
                nb[dst:dst + 4] = b[src:src + 4] if y + 1 < h else b"\x00\x00\x00\x00"
        return w, h, nb
    a["sha256"] = sha(png_edit(t, a, up))
    save_m(t, m)


def palette_bad(t):
    m = load_m(t); a = asset(m, "pkp_xp-gem-base_idle_8")
    def off(w, h, b):
        for i in range(0, len(b), 4):
            if b[i + 3] == 255:
                b[i:i + 4] = bytes([1, 2, 3, 255])
                break
        return w, h, b
    a["sha256"] = sha(png_edit(t, a, off))
    save_m(t, m)


def source_stale(t):
    """pixel-source grid changed (and its hash updated) -> grids no longer render to the PNG"""
    p = os.path.join(t, "export", "source", "pixel-source_v3.json")
    s = json.load(open(p, encoding="utf-8"))
    sp = next(x for x in s["sprites"] if x["name"] == "enm_a3-thorn-beast_walk_32")
    row = sp["frames"][0][20]
    i = next(k for k, ch in enumerate(row) if ch != ".")
    sp["frames"][0][20] = row[:i] + "." + row[i + 1:]
    json.dump(s, open(p, "w", encoding="utf-8"), ensure_ascii=False, separators=(",", ":"))
    m = load_m(t); m["data"]["pixelSource"]["sha256"] = sha(p); save_m(t, m)


def legacy_v2(t):
    """a legacy 02 pixel-source given as the manifest"""
    src = os.path.join(os.path.dirname(ROOT), "art-concept-02", "out", "pixel-source_art-b02.json")
    shutil.copy(src, os.path.join(t, "export", "manifest_v3.json"))


def tile_mask_bad(t):
    p = os.path.join(t, "export", "tiles", "path-16mask_v3.json")
    d = json.load(open(p, encoding="utf-8"))
    d["frames"][3]["rectTopLeft"] = [0, 0, 32, 32]
    json.dump(d, open(p, "w", encoding="utf-8"), ensure_ascii=False, indent=1)
    m = load_m(t); m["data"]["tiles"]["sha256"] = sha(p); save_m(t, m)


def nine_slice_bad(t):
    p = os.path.join(t, "export", "ui", "ui-pieces_v3.json")
    d = json.load(open(p, encoding="utf-8"))
    d["pieces"]["ui_panel_9slice_16"]["border_LBRT"] = [9, 4, 9, 4]
    json.dump(d, open(p, "w", encoding="utf-8"), ensure_ascii=False, indent=1)
    m = load_m(t); m["data"]["ui"]["sha256"] = sha(p); save_m(t, m)


def hold_frame(t):
    """player base idle frame 2 overwritten with frame 1 in the PNG and the pixel-source (all hashes updated): the 02-era
    idle where the body held still between frames -> only the frameChange rule can catch it"""
    m = load_m(t); aid = "chr_player_layer-base_idle_32"; a = asset(m, aid)
    def copy12(w, h, b):
        for y in range(h):
            r = y * w * 4
            b[r + 2 * 32 * 4:r + 3 * 32 * 4] = b[r + 1 * 32 * 4:r + 2 * 32 * 4]
        return w, h, b
    a["sha256"] = sha(png_edit(t, a, copy12))
    p = os.path.join(t, "export", "source", "pixel-source_v3.json")
    s = json.load(open(p, encoding="utf-8"))
    sp = next(x for x in s["sprites"] if x["name"] == aid)
    sp["frames"][2] = list(sp["frames"][1])
    json.dump(s, open(p, "w", encoding="utf-8"), ensure_ascii=False, separators=(",", ":"))
    m["data"]["pixelSource"]["sha256"] = sha(p)
    save_m(t, m)


# ---- QA A03-01: manifest top-level import metadata (only the manifest changes; no data hash depends on it)
def top_set(**kv):
    def fn(t):
        m = load_m(t); m.update(kv); save_m(t, m)
    fn.__doc__ = "manifest " + ", ".join(f"{k} = {v!r}" for k, v in kv.items())
    return fn


def top_del(*keys):
    def fn(t):
        m = load_m(t)
        for k in keys:
            del m[k]
        save_m(t, m)
    fn.__doc__ = "manifest top-level " + "/".join(keys) + " deleted"
    return fn


def top_layerorder_reversed(t):
    """manifest layerOrder reversed (QA A03-01 mutation)"""
    m = load_m(t); m["layerOrder"] = m["layerOrder"][::-1]; save_m(t, m)


def top_flip_left(t):
    """manifest flip says the art faces left (structure kept)"""
    m = load_m(t); m["flip"] = dict(m["flip"], artFaces="left"); save_m(t, m)


def asset_layerorder_swap(t):
    """player base idle layerOrder 1 -> 0 (same as cloak_back): per-layer order must follow contract player.layers"""
    m = load_m(t); asset(m, "chr_player_layer-base_idle_32")["layerOrder"] = 0; save_m(t, m)


# ---- QA A03-02: UI font references (UI data edits keep the manifest's ui sha256 coherent)
def ui_edit(t, fn):
    p = os.path.join(t, "export", "ui", "ui-pieces_v3.json")
    d = json.load(open(p, encoding="utf-8"))
    fn(d)
    json.dump(d, open(p, "w", encoding="utf-8"), ensure_ascii=False, indent=1)
    m = load_m(t); m["data"]["ui"]["sha256"] = sha(p); save_m(t, m)


def font_ref_missing(t):
    """fonts.title.file -> third_party/galmuri/__qa_missing__.ttf, ui hash coherent (QA A03-02 mutation)"""
    ui_edit(t, lambda d: d["fonts"]["title"].update(file="third_party/galmuri/__qa_missing__.ttf"))


def font_path_unsafe(t):
    """fonts.title.file -> third_party/galmuri/../galmuri/<real bold file> (resolves to a real file but climbs out)"""
    ui_edit(t, lambda d: d["fonts"]["title"].update(file="third_party/galmuri/../galmuri/Galmuri11Bitmap-Bold-2.40.4.ttf"))


def font_extra_unsafe(t):
    """an extra font entry (not a contract role) naming a REAL file outside the font dir via '..': only the safe-path
    rule can catch it (the file exists and extra entries have no contract hash)"""
    ui_edit(t, lambda d: d["fonts"].update(outline={"file": "third_party/galmuri/../../src/palette_v03.json", "pixelSize": 12}))


def font_role_missing(t):
    """fonts.small removed"""
    ui_edit(t, lambda d: d["fonts"].pop("small"))


def font_role_swapped(t):
    """title and label files swapped (both files exist)"""
    def fn(d):
        f = d["fonts"]
        f["title"]["file"], f["label"]["file"] = f["label"]["file"], f["title"]["file"]
    ui_edit(t, fn)


def font_license_ref_bad(t):
    """fonts.license.file -> third_party/galmuri/OFL.txt (does not exist)"""
    ui_edit(t, lambda d: d["fonts"]["license"].update(file="third_party/galmuri/OFL.txt"))


def font_source_bad(t):
    """fonts.source.version 2.40.4 -> 2.40.3"""
    ui_edit(t, lambda d: d["fonts"]["source"].update(version="2.40.3"))


def font_file_missing(t):
    """the real Galmuri9 bitmap file deleted (UI data untouched)"""
    os.remove(os.path.join(t, "third_party", "galmuri", "Galmuri9Bitmap-Regular-2.40.4.ttf"))


def font_file_changed(t):
    """Galmuri7 bitmap file overwritten with the Galmuri9 bytes (name and UI data untouched)"""
    g = os.path.join(t, "third_party", "galmuri")
    shutil.copyfile(os.path.join(g, "Galmuri9Bitmap-Regular-2.40.4.ttf"), os.path.join(g, "Galmuri7Bitmap-Regular-2.40.4.ttf"))


def font_license_missing(t):
    """third_party/galmuri/LICENSE.txt deleted"""
    os.remove(os.path.join(t, "third_party", "galmuri", "LICENSE.txt"))


def font_dir_missing(t):
    """no third_party/ folder at all (QA's copy without fonts, which used to pass)"""
    shutil.rmtree(os.path.join(t, "third_party"))


def manifest_font_notice_bad(t):
    """manifest license.fonts.notice -> third_party/galmuri/NOTICE.txt"""
    m = load_m(t); m["license"]["fonts"]["notice"] = "third_party/galmuri/NOTICE.txt"; save_m(t, m)


TOP5 = ("ppu", "pivotSpace", "frameLayout", "layerOrder", "flip")
CASES = [("frames_short", frames_short), ("layer_frames", layer_frames), ("pivot_bad", pivot_bad),
         ("layer_missing", layer_missing), ("alpha_bad", alpha_bad), ("ref_bad", ref_bad), ("file_missing", file_missing),
         ("hash_bad", hash_bad), ("foot_bad", foot_bad), ("palette_bad", palette_bad), ("source_stale", source_stale),
         ("legacy_v2", legacy_v2), ("tile_mask_bad", tile_mask_bad), ("nine_slice_bad", nine_slice_bad),
         ("hold_frame", hold_frame),
         # A03-01
         ("top_ppu48", top_set(ppu=48)), ("top_pivotspace_topleft", top_set(pivotSpace="TOP-LEFT, Y DOWN")),
         ("top_layerorder_reversed", top_layerorder_reversed), ("top_framelayout_vertical", top_set(frameLayout="vertical")),
         ("top_all_deleted", top_del(*TOP5))] + \
        [(f"top_missing_{k}", top_del(k)) for k in TOP5] + \
        [("top_ppu_string", top_set(ppu="32")),
         ("top_flip_old_string", top_set(flip="art faces right; left = flipX on the parent")),
         ("top_flip_left", top_flip_left), ("asset_layerorder_swap", asset_layerorder_swap),
         # A03-02
         ("font_ref_missing", font_ref_missing), ("font_path_unsafe", font_path_unsafe),
         ("font_extra_unsafe", font_extra_unsafe),
         ("font_role_missing", font_role_missing), ("font_role_swapped", font_role_swapped),
         ("font_license_ref_bad", font_license_ref_bad), ("font_source_bad", font_source_bad),
         ("font_file_missing", font_file_missing), ("font_file_changed", font_file_changed),
         ("font_license_missing", font_license_missing), ("font_dir_missing", font_dir_missing),
         ("manifest_font_notice_bad", manifest_font_notice_bad)]


def run_case(name, fn):
    with tempfile.TemporaryDirectory(prefix="art03-neg-") as t:
        for part in ("export", "src", "third_party"):
            shutil.copytree(os.path.join(ROOT, part), os.path.join(t, part))
        if fn:
            fn(t)
        r = subprocess.run([sys.executable, "-I", CHECK, "--root", t, "--quiet"], capture_output=True, text=True)
        lines = (r.stdout + r.stderr).replace(t, "<tmp>").strip().splitlines()
        return r.returncode, lines


def expected_tag(name):
    """the error code a broken copy must report (a crash also exits 1, so the exit code alone is not enough)"""
    if name.startswith("top_"):
        return "[TOP]"
    if name == "asset_layerorder_swap":
        return "[LAYER]"
    if name.startswith(("font_", "manifest_font_")):
        return "[FONT]"
    return None


def main():
    os.makedirs(OUT, exist_ok=True)
    results, ok = [], True
    code, lines = run_case("positive", None)
    results.append({"case": "positive (untouched copy)", "expect": 0, "exit": code, "output": lines[:4]})
    ok &= code == 0
    for name, fn in CASES:
        code, lines = run_case(name, fn)
        tag = expected_tag(name)
        rejected = bool(lines) and lines[0].startswith("FAIL:") and not any("Traceback" in x for x in lines)
        good = code == 1 and rejected and (tag is None or any(x.strip().startswith(tag) for x in lines))
        ok &= good
        results.append({"case": name, "what": (fn.__doc__ or "").strip(), "expect": 1 if tag is None else f"1 {tag}",
                        "exit": code, "pass": good, "output": lines[:4],
                        "hit": next((x.strip() for x in lines if tag and x.strip().startswith(tag)), None)})
    json.dump({"checker": "scripts/check_art03.py", "python": sys.version.split()[0], "results": results},
              open(os.path.join(OUT, "check_negative_results.json"), "w", encoding="utf-8"), ensure_ascii=False, indent=1)
    def shown(r):
        o = r["output"]
        return r.get("hit") or (o[1] if len(o) > 1 else (o[0] if o else ""))
    with open(os.path.join(OUT, "check_negative_results.txt"), "w", encoding="utf-8") as f:
        for r in results:
            f.write(f"{r['case']:28s} exit={r['exit']}  expect={r['expect']}  {shown(r)}\n")
    for r in results:
        print(f"{r['case']:28s} exit={r['exit']}  expect={r['expect']}  | {shown(r)}")
    print("NEGATIVE TEST:", "PASS" if ok else "FAIL")
    return 0 if ok else 1


if __name__ == "__main__":
    sys.exit(main())
