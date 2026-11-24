#!/usr/bin/env python3
"""Validate Claude Design exports and repack 480×270 frames for Unity Resources."""
import argparse
import hashlib
import json
import math
from pathlib import Path
from tempfile import TemporaryDirectory
from PIL import Image, ImageChops

parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('source', type=Path, help='Claude Design export root containing shot subdirectories')
parser.add_argument('timelines', type=Path, help='directory containing the Unity cinematic JSONs')
parser.add_argument('output', type=Path, help='directory to receive new -media subdirectories')
parser.add_argument('--clip', action='append', required=True, choices=(
    'r43-judges', 'r55-samuel', 'r67-kings', 'r94-nehemiah', 'r121-isaiah',
    'r151-daniel', 'r166-jonah', 'r172-habakkuk', 'r180-malachi'))
parser.add_argument('--approved', action='store_true', help='use only after independent art and Player review')
args = parser.parse_args()
source, timeline, destination = args.source.resolve(), args.timelines.resolve(), args.output.resolve()
status = "approved" if args.approved else "review"
if not __debug__:
    raise RuntimeError("Run with validation assertions enabled.")

width, height, fps, columns, rows = 480, 270, 12, 4, 7
frames_per_page = columns * rows

def sha(data):
    return hashlib.sha256(data).hexdigest()

def read(root, relative):
    path = (root / relative).resolve()
    assert path.is_relative_to(root)
    return path

def rgba(root, relative):
    return Image.open(read(root, relative)).convert("RGBA")

def check_palette(image, colors, shot):
    used = image.getcolors(len(colors) + 1)
    assert used is not None, (shot, 'too many colors')
    for _, color in used:
        assert color == (0, 0, 0, 0) or (color[3] == 255 and color[:3] in colors), (shot, color)


def prepare(clip, staging, palettes):
    tracks = json.loads((timeline / (clip + '.json')).read_text())['cuts']
    output = staging / (clip + '-media')
    output.mkdir(parents=True, exist_ok=False)
    index = {"status": status, "shots": []}
    last_end = 0
    for number, cut in enumerate(tracks, 1):
        shot = f'{clip}-c{number}'
        root = source / shot
        manifest_path = root / 'manifest.json'
        manifest = json.loads(manifest_path.read_text())
        version = manifest['design']['version']
        assert manifest['design']['shot'] == shot
        if 'designVersion' not in index:
            index['designVersion'] = version
        assert index['designVersion'] == version
        if version not in palettes:
            palette = json.loads((source / f'palette_cinematic_v{version}.json').read_text())['ramps']
            palettes[version] = {tuple(bytes.fromhex(c.removeprefix('#'))) for group in palette.values() for c in group}
        colors = palettes[version]
        time = manifest['timeline']
        count = math.ceil((cut['end'] - last_end) * fps)
        assert (time['fps'], time['frames'], time['master']['start'], time['master']['cutEnd']) == (fps, count, last_end, cut['end']), shot
        last_end = cut['end']
        for relative, digest in manifest['files'].items():
            assert sha(read(root, relative).read_bytes()) == digest, (shot, relative)
        animated = next(path for path in manifest['files'] if path.startswith('full/'))
        composite = rgba(root, animated)
        still = rgba(root, manifest['reduced_motion_still']['file'])
        assert composite.size == (count * width, height) and still.size == (width, height), shot
        assert sha(still.tobytes()) == manifest['checks']['still_sha256'], shot
        check_palette(still, colors, shot)
        layers = sorted(manifest['layers'], key=lambda layer: layer['z'])
        strips = {layer['name']: rgba(root, layer['strip']) for layer in layers}
        still_layers = {layer['name']: rgba(root, layer['still']) for layer in layers}
        subjects = manifest['checks']['key_subjects']['layers']
        measured = {name: -1 for name in subjects}
        live_terrain = any(layer['name'] == 'battle-plate' for layer in layers)
        assert not live_terrain or (clip != 'r180-malachi' and number == len(tracks)), shot
        assert len(manifest['checks']['frame_sha256']) == count, shot
        frames = []
        visible_terrain = []
        still_merge = Image.new("RGBA", (width, height))
        for layer in layers:
            xy = tuple(layer['origin_topleft_480x270'])
            layer_still = still_layers[layer['name']]
            still_merge.alpha_composite(layer_still, xy)
            if layer['name'] in measured:
                bbox = layer_still.getbbox()
                if bbox: measured[layer['name']] = xy[1] + bbox[3] - 1
        assert still_merge.tobytes() == still.tobytes(), (shot, 'still composite')
        for frame_number in range(count):
            rebuilt = Image.new("RGBA", (width, height))
            for layer in layers:
                name = layer['name']
                x, y = layer['origin_topleft_480x270']
                lw, lh = layer['cell']
                fragment = strips[name].crop((lw * frame_number, 0, lw * (frame_number + 1), lh))
                if name in measured:
                    bbox = fragment.getbbox()
                    if bbox: measured[name] = max(measured[name], y + bbox[3] - 1)
                if name == 'battle-plate':
                    assert layer == layers[-1] and (x, y, lw, lh) == (0, 0, width, height)
                    frames.append(rebuilt.copy())
                    hole = rebuilt.getchannel('A')
                    mask = fragment.getchannel('A')
                    assert ImageChops.difference(ImageChops.invert(hole), mask).getbbox() is None, (shot, frame_number)
                    visible_terrain.append(mask.histogram()[-1])
                rebuilt.alpha_composite(fragment, (x, y))
            expected = composite.crop((width * frame_number, 0, width * (frame_number + 1), height))
            check_palette(expected, colors, shot)
            assert rebuilt.tobytes() == expected.tobytes(), (shot, frame_number, 'composite')
            assert sha(expected.tobytes()) == manifest['checks']['frame_sha256'][frame_number], (shot, frame_number, 'frame hash')
            if not live_terrain: frames.append(expected)
        assert measured == subjects and max(measured.values()) <= 238, (shot, measured, subjects)
        if live_terrain:
            assert visible_terrain[0] == 0 and visible_terrain[-1] == width*height
            assert all(a <= b for a, b in zip(visible_terrain, visible_terrain[1:])), shot
        manifest_sha = sha(manifest_path.read_bytes())
        index["shots"].append({
            "frames": count,
            "liveTerrain": live_terrain,
            "manifestSha256": manifest_sha
        })
        for offset in range(0, count, frames_per_page):
            batch = frames[offset:offset + frames_per_page]
            page = Image.new("RGBA", (columns * width, math.ceil(len(batch) / columns) * height))
            for cell, frame in enumerate(batch):
                page.paste(frame, (cell % columns * width, cell // columns * height))
            page.save(output / f'cut-{number}-page-{offset // frames_per_page}.png', optimize=True)
        still.save(output / f'cut-{number}-still.png', optimize=True)
        print(shot, 'frames', count, 'subject bottom', max(measured.values()), 'manifest', manifest_sha,
              'terrain coverage' if live_terrain else '', [visible_terrain[0], visible_terrain[-1]] if live_terrain else '', flush=True)
    (output / 'index.json').write_text(json.dumps(index, indent=2) + '\n')

destination.mkdir(parents=True, exist_ok=True)
targets = [destination / (clip + '-media') for clip in args.clip]
if len(set(targets)) != len(targets) or any(path.exists() for path in targets):
    parser.error('Each clip must have a fresh -media output path.')
with TemporaryDirectory(dir=destination) as temporary:
    staging = Path(temporary)
    palettes = {}
    for clip in args.clip:
        prepare(clip, staging, palettes)
    for path in targets:
        (staging / path.name).rename(path)
