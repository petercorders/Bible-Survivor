#!/usr/bin/env python3
"""Prepare only the independently reviewed font sidecar; the original bitmap fonts remain untouched."""
import hashlib
import json
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT/'art/art-unity-font-01'
DEST = ROOT/'UnityGame/Assets/Adam/Resources/ArtFonts'
PIN = '16308da615ad12445794a89ac8dfd612b7a4060223e4e657b18eef79c37579bd'

def checked(path, expected):
    data=path.read_bytes()
    if hashlib.sha256(data).hexdigest()!=expected:raise ValueError('Font SHA mismatch: '+str(path.relative_to(ROOT)))
    return data

def prepare():
    raw=checked(SOURCE/'export/font_manifest_v1.json',PIN);manifest=json.loads(raw)
    checked(SOURCE/manifest['contract']['file'],manifest['contract']['sha256'])
    files={'font-manifest.json':raw,'LICENSE.txt':checked(SOURCE/manifest['license']['file'],manifest['license']['sha256'])}
    if set(manifest['roles'])!={'title','label','body','small'}:raise ValueError('Unexpected font roles')
    for role,value in manifest['roles'].items():
        if value['basePx']!={'title':12,'label':12,'body':10,'small':8}[role]:raise ValueError('Font base size mismatch')
        files[Path(value['file']).name]=checked(SOURCE/value['file'],value['sha256'])
    if DEST.exists() and not {p.name for p in DEST.glob('*.ttf')} <= set(files):raise ValueError('Unexpected destination fonts')
    DEST.mkdir(parents=True,exist_ok=True)
    for name,data in files.items():
        p=DEST/name
        if not p.exists() or p.read_bytes()!=data:p.write_bytes(data)
    print(json.dumps(dict(status='PASS',roles=4,manifestSha256=PIN,destination=str(DEST.relative_to(ROOT)))))

if __name__=='__main__':prepare()
