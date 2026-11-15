#!/usr/bin/env python3
"""Strict R02 sidecar gate and deterministic Unity index; does not sync Core or edit Art03/fonts."""
from pathlib import Path
import argparse, hashlib, json, subprocess, sys
ROOT=Path(__file__).resolve().parents[1]
SNAPSHOT_SHA='a6c7c7df440afb621d44243b6084b462078de88bf73d5a375e19a76b162a060c'
MANIFEST_SHA='e3a69af3c0734e5d37848b5780d020d09bbb054538e4173bf8aacafc0db1be89'
IDS=['enm_b1-envy-eye_move_32','enm_b2-rage-hound_run_32','enm_b3-cracked-mask_move_32']
HASHES=['060c78299d9501e44b01130dd079d6c767381c730b283b767f003f3d49bbfd6b','c2fb37d65ead991ab6e0b5adbe139fd3e9556069096c44804c834b2c5eaa5fbd','32a6294b9c4e1c85718eaf60ad2dc28f95719f661b0e82413d560c5c5de359a7']
def sha(data):return hashlib.sha256(data).hexdigest()
def index_bytes(package):
    # get_rgba reads top-down; Unity GetPixels32 has bottom-left row first.
    sys.dont_write_bytecode=True
    sys.path.insert(0,str(package/'scripts'));import pngio
    assets=[]
    for i,name in enumerate(IDS):
        raw=(package/'export/png'/(name+'.png')).read_bytes()
        if sha(raw)!=HASHES[i]:raise ValueError('Unreviewed PNG '+name)
        w,h,pixels=pngio.read_rgba(str(package/'export/png'/(name+'.png')))
        if (w,h)!=(128,32):raise ValueError('Invalid sheet size')
        bottom_up=b''.join(pixels[y*w*4:(y+1)*w*4] for y in reversed(range(h)))
        assets.append({'monster':'b'+str(i+1),'ground':i!=0,'shadow':'fx_shadow-small_static_12x4' if i==0 else 'fx_shadow-wide_static_24x6','pixelSha256':sha(bottom_up),'clip':{'id':name,'source':'art/art-genesis-r02/export/png/'+name+'.png','sha256':HASHES[i],'sorting':'Actors','quality':'core','alpha':'binary','width':32,'height':32,'frames':4,'pivotX':16,'pivotY':1,'fps':8 if i<2 else 6,'loop':True,'borders':[0,0,0,0]}})
    return (json.dumps({'schema':1,'ppu':32,'manifestSha256':MANIFEST_SHA,'assets':assets},ensure_ascii=False,indent=2)+'\n').encode()
def main():
    ap=argparse.ArgumentParser(description=__doc__);ap.add_argument('--prepare',action='store_true');ap.add_argument('--root',type=Path,default=ROOT);a=ap.parse_args();root=a.root.resolve();pkg=root/'art/art-genesis-r02';target=root/'UnityGame/Assets/Adam/Resources/ArtR02'
    snapshot=(pkg/'approved-inputs.json').read_bytes()
    if sha(snapshot)!=SNAPSHOT_SHA:raise ValueError('Unreviewed public input inventory')
    inventory=json.loads(snapshot)
    if any(not (pkg/name).is_file() or sha((pkg/name).read_bytes())!=digest for name,digest in inventory.items()):raise ValueError('Approved source/checker inputs differ')
    actual={str(p.relative_to(pkg)) for p in pkg.rglob('*') if p.is_file() and '__pycache__' not in p.parts}
    if actual!=set(inventory)|{'README.md','approved-inputs.json'}:raise ValueError('Unexpected/missing public sidecar files')
    if sha((pkg/'export/manifest_r02.json').read_bytes())!=MANIFEST_SHA:raise ValueError('Unreviewed R02 manifest')
    cmd=[sys.executable,'-I','-S','-B',str(pkg/'scripts/check_r02.py'),'--art03',str(root/'art/art-concept-03'),'--font-root',str(root/'art/art-unity-font-01'),'--catalog',str(root/'content/genesis/catalog.json')]
    result=subprocess.run(cmd)
    if result.returncode:return result.returncode
    desired={'index.json':index_bytes(pkg),'manifest.json':(pkg/'export/manifest_r02.json').read_bytes()}
    desired.update({name+'.png':(pkg/'export/png'/(name+'.png')).read_bytes() for name in IDS})
    if a.prepare:target.mkdir(parents=True,exist_ok=True)
    for name,data in desired.items():
        p=target/name
        if p.exists() and p.read_bytes()!=data:raise ValueError('Existing Unity input differs; refusing overwrite: '+name)
        if a.prepare and not p.exists():p.write_bytes(data)
        if not p.is_file() or p.read_bytes()!=data:raise ValueError('Missing/mismatched Unity R02 input: '+name)
    if set(p.name for p in target.iterdir() if p.is_file() and p.suffix.lower()=='.png')!=set(name+'.png' for name in IDS):raise ValueError('Unexpected R02 PNG')
    print('R02 PREPARE/CHECK PASS indexSha256='+sha(desired['index.json'])+'; Unity import/render NOT_RUN')
    return 0
if __name__=='__main__':
    try:sys.exit(main())
    except (OSError,ValueError,KeyError,TypeError) as error:print('R02 FAIL: '+str(error),file=sys.stderr);sys.exit(1)
