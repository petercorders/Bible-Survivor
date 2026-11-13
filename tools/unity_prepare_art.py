#!/usr/bin/env python3
"""Copy the reviewed art03 PNG bytes and emit a Unity-friendly contract; never copy bitmap fonts."""
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'art/art-concept-03'
DEST = ROOT / 'UnityGame/Assets/Adam/Resources/Art03'
PIN = '0c7f6ff0b86d83217beedf62f298294cd6c01c036ab589e2abed383c1e429751'

def sha(data):
    return hashlib.sha256(data).hexdigest()

def require(condition, message):
    if not condition:
        raise ValueError(message)

def prepare():
    raw = (SOURCE / 'export/manifest_v3.json').read_bytes()
    require(sha(raw) == PIN, 'Unreviewed art manifest')
    manifest = json.loads(raw)
    require(manifest['ppu'] == 32, 'Invalid art contract')
    require(manifest['flip']['artFaces'] == 'right' and manifest['flip']['faceLeft'] == 'flipX' and manifest['flip']['applyOn'] == 'parent', 'Invalid art contract')
    for ref in [manifest['palette'], *manifest['inputs'].values(), *manifest['data'].values()]:
        require(sha((SOURCE / ref['file']).read_bytes()) == ref['sha256'], ref['file'])
    ui = json.loads((SOURCE / manifest['data']['ui']['file']).read_bytes())['pieces']
    assets = []
    payloads = {}
    for asset in manifest['assets']:
        data = (SOURCE / asset['file']).read_bytes()
        require(sha(data) == asset['sha256'], asset['id'])
        w, h = asset['cell']
        require(asset['size'] == [w * asset['frames'], h], 'Invalid art contract')
        require(asset['frameRects'] == [[i*w, 0, w, h] for i in range(asset['frames'])], 'Invalid art contract')
        require(all(isinstance(v, int) for v in asset['pivot']['px']), 'Invalid art contract')
        require(asset['sorting'] in ['Ground', 'GroundDecal', 'Pickups', 'Actors', 'WeaponFX', 'FX', 'UI'], 'Invalid art contract')
        require(asset['quality'] in ['core', 'optional-fx', 'terrain', 'ui'], 'Invalid art contract')
        require(asset['alpha']['policy'] in ['binary', 'blend'], 'Invalid art contract')
        a = dict(id=asset['id'], source=asset['file'], sha256=asset['sha256'], width=w, height=h,
                 frames=asset['frames'], fps=asset['fps'], loop=asset['loop'], pivotX=asset['pivot']['px'][0],
                 pivotY=asset['pivot']['px'][1], sorting=asset['sorting'], quality=asset['quality'],
                 alpha=asset['alpha']['policy'], alphaValues=asset['alpha']['values'],
                 layer=asset.get('layer',''), animation=asset.get('animation',''),
                 borders=ui.get(asset['id'], {}).get('border_LBRT') or [0,0,0,0],
                 states=ui.get(asset['id'], {}).get('states') or [],
                 attachX=[v[0] for v in asset.get('attachPx',[])], attachY=[v[1] for v in asset.get('attachPx',[])])
        assets.append(a)
        payloads[asset['id'] + '.png'] = data
    require(len(assets) == 79 and len({a['id'] for a in assets}) == 79, 'Invalid art contract')
    # Validate the complete set before changing any destination bytes. Existing .meta files are retained.
    require(not DEST.exists() or {p.name for p in DEST.glob('*.png')} <= set(payloads), 'Unexpected destination PNGs')
    DEST.mkdir(parents=True, exist_ok=True)
    for name, data in payloads.items():
        path = DEST / name
        if not path.exists() or path.read_bytes() != data:
            path.write_bytes(data)
    index = dict(schema=1, manifestSha256=PIN, ppu=32, layers=manifest['layerOrder'], assets=assets)
    (DEST / 'index.json').write_text(json.dumps(index, ensure_ascii=False, indent=2) + '\n')
    require({p.name for p in DEST.glob('*.png')} == set(payloads), 'Unexpected destination PNGs')
    print(json.dumps(dict(status='PASS', assets=len(assets), manifestSha256=PIN, destination=str(DEST.relative_to(ROOT)))))

if __name__ == '__main__':
    prepare()
