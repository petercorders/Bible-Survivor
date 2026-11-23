"""Check registered Unity strips against the independently reviewed source."""
import hashlib
import json
from pathlib import Path
import struct
import zlib


ART = Path(__file__).resolve().parent
MASTER = ART.parent / "art-concept-03/src/palette_v03.json"
UNITY = ART.parent.parent / "UnityGame/Assets/Adam/Resources/Issue47Weapons"


def read_json(path):
    return json.loads(path.read_bytes())


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def require(condition, message):
    if not condition:
        raise ValueError(message)


def read_raw_png(path, width, height):
    data = path.read_bytes()
    require(data.startswith(b"\x89PNG\r\n\x1a\n"), f"{path.name}: invalid PNG signature")
    parts = []
    offset = 8
    ended = False
    while offset < len(data):
        size, kind = struct.unpack_from(">I4s", data, offset)
        end = offset + 8 + size
        payload = data[offset + 8:end]
        require(struct.unpack_from(">I", data, end)[0] == zlib.crc32(kind + payload),
                f"{path.name}: invalid chunk CRC")
        if kind == b"IHDR":
            require(struct.unpack(">IIBBBBB", payload) == (width, height, 8, 6, 0, 0, 0),
                    f"{path.name}: expected RGBA8 strip")
        elif kind == b"IDAT":
            parts.append(payload)
        elif kind == b"IEND":
            ended = True
        offset = end + 4
    require(ended, f"{path.name}: missing IEND")
    return zlib.decompress(b"".join(parts))


def main():
    source_path = ART / "source/issue47-production_v1.json"
    contract_path = ART / "contract_v1.json"
    source = read_json(source_path)
    export = read_json(ART / "manifest_v1.json")
    contract = read_json(contract_path)["sprites"]
    manifest = read_json(UNITY / "manifest.json")
    require(export["sourceSha256"] == sha(source_path) and export["contractSha256"] == sha(contract_path),
            "Source or contract differs from the reviewed export")
    master = {f"{ramp}.{i}": value for ramp, entries in read_json(MASTER)["actor"].items()
              for i, value in enumerate(entries)}
    ui_path = ART / "palette_issue47_v1.json"
    ui = {key: entry["hex"] for key, entry in read_json(ui_path)["keys"].items()}
    palette_files = {"master-v0.3": MASTER, "issue47-ink-bronze-v1": ui_path}
    for name, file in palette_files.items():
        require(source["palettes"][name]["sha256"] == sha(file), f"{name}: palette hash differs")
    palettes = {"master-v0.3": master, "issue47-ink-bronze-v1": ui}
    registered = {entry["id"]: entry for entry in manifest["assets"]}
    exports = {entry["name"]: entry for entry in export["sprites"]}
    declared = [entry["name"] for entry in source["sprites"]]
    require(len(declared) == len(export["sprites"]) == len(manifest["assets"]) == len(contract) == 63 and
            set(declared) == contract.keys() == registered.keys() == exports.keys(),
            "Expected one registration per approved stable ID")
    require(manifest["ppu"] == 32 and manifest["pivotSpace"] == "bottom-left-pixel-edge",
            "Unexpected Unity coordinate contract")
    require({file.name for file in UNITY.glob("*.png")} == {name + ".png" for name in declared},
            "Unity strip files differ from reviewed IDs")
    for sprite in source["sprites"]:
        name = sprite["name"]
        asset, original, spec = registered[name], exports[name], contract[name]
        frames = sprite["frames"]
        w, h = sprite["cell"]
        require(asset["file"] == name + ".png" and asset["cell"] == spec["cell"] == [w, h] and
                asset["pivot"] == spec["pivot_px"] == sprite["pivot_px"] and
                asset["frames"] == spec["frames"] == len(frames) and
                asset["fps"] == (sprite["fps"] or 0) and asset["loop"] == sprite["loop"] and
                asset["borders"] == spec.get("border", [0] * 4) and asset["alpha"] == "binary",
                f"{name}: Unity registration differs from the source")
        path = UNITY / asset["file"]
        require(asset["sha256"] == original["sha256"] == sha(path), f"{name}: changed export")
        palette = palettes[sprite["palette"]]
        rgba = {key: bytes.fromhex(palette[token][1:]) + b"\xff" for key, token in sprite["keys"].items()}
        require(len(set(rgba.values())) <= 16 and all(len(frame) == h and all(len(row) == w for row in frame)
                                                       for frame in frames), f"{name}: invalid cell data")
        rgba["."] = bytes(4)
        expected = b"".join(b"\x00" + b"".join(rgba[pixel] for frame in frames for pixel in frame[y])
                            for y in range(h))
        require(read_raw_png(path, w * len(frames), h) == expected, f"{name}: source pixels differ")
    print("Issue #47 art source, Unity registration and rendered strips match (63/63)")


if __name__ == "__main__":
    main()
