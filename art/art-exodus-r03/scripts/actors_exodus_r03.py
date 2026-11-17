"""Original Codex native-grid R12 candidate pixels; no image model/Claude/Design.
Empty nonhuman oppression/pursuit constructs, never people or divine plagues.
"""
import json, os, math
import px03 as px
ROOT=os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
EXT=json.load(open(os.path.join(ROOT,'src/palette_exodus_r03_ext.json')))
for name,cols in EXT['actor'].items():
    if name+'.0' in px.PAL: raise ValueError('Palette collision: '+name)
    for k,col in enumerate(cols):
        rgba=px.hex_rgba(col);px.PAL[f'{name}.{k}']=rgba
        px.NAME_OF.setdefault(rgba,f'{name}.{k}');px.RAMP_OF.setdefault(rgba,name)
K=px.C('outline.0')
def dot(a,x,y,c): px.put(a,round(x),round(y),c)
def rect(a,x0,y0,x1,y1,c):
    for y in range(round(y0),round(y1)+1):
        for x in range(round(x0),round(x1)+1):dot(a,x,y,c)
def line(a,x0,y0,x1,y1,c):px.line(a,x0,y0,x1,y1,c)
def ell(a,x,y,rx,ry,r):
    px.shade_ellipse(a,x,y,rx,ry,0,lambda x,y,u,v,it:r[min(len(r)-1,px.level(it,(.84,.60,.33)))])
def finish(a):return px.ground_to(px.outline(a),a.shape[0]-2)
def link(a,x,y,vertical,r):
    rx,ry=(2,4) if vertical else (4,2)
    ell(a,x,y,rx,ry,r)
    rect(a,x-rx+1,y-ry+1,x+rx-1,y+ry-1,K)
    dot(a,x-rx,y-1,r[0])

def labor_chain(f):
    a=px.new(32,32);s=px.ramp('chainsteel');p=px.ramp('pursuit');b=(0,-1,0,1)[f];sway=(-1,0,1,0)[f]
    # Linked loops form an open body, with loose chain ends instead of limbs/victims.
    for x,y,v in [(11+sway,9+b,True),(15,15+b,False),(20,17+b,True),(12,22,True),(19+sway,26,False)]:link(a,x,y,v,s)
    link(a,7+sway,19+b,False,s);link(a,6,25,True,s)
    ell(a,22,10+b,4,4,p);rect(a,21,9+b,25,10+b,K);dot(a,25,9+b,s[0])
    rect(a,21,13+b,25,14+b,s[2]);dot(a,23,13+b,s[0])
    line(a,11,6+b,15+sway,4+b,s[1]);dot(a,16+sway,4+b,s[0])
    return finish(a)

def crown_mask(f):
    a=px.new(32,32);r=px.ramp('crownbronze');s=px.ramp('chainsteel');b=(0,-1,-1,0)[f];lean=(-1,0,1,0)[f]
    # Hollow industrial mask with three broken teeth, no historic royal headdress.
    rect(a,10,11+b,23,22+b,r[2]);rect(a,8,12+b,10,19+b,r[1]);rect(a,22,13+b,25,20+b,r[3])
    rect(a,11,13+b,21,20+b,K);rect(a,10,10+b,23,12+b,r[1])
    for x,top in [(10,6),(16,4),(22,7)]:
        rect(a,x-1,top+b,x+1,10+b,r[1]);dot(a,x-1,top+b,r[0])
    rect(a,19,15+b,24,16+b,r[0]);rect(a,21,15+b,23,15+b,K)
    line(a,11,21+b,14+lean,25,r[2]);line(a,22,22+b,20-lean,27,r[1])
    link(a,12+lean,27,False,s);dot(a,19-lean,28,s[0])
    for x,y in [(10,11),(16,11),(22,11)]:dot(a,x,y+b,r[0])
    return finish(a)

def wheel(a,x,y,rad,f,r):
    ell(a,x,y,rad,rad,r);ell(a,x,y,rad-2,rad-2,[r[3]]*4)
    ang=f*math.pi/2+.25
    for sign in (-1,1):line(a,x,y,x+sign*(rad-1)*math.cos(ang),y+sign*(rad-1)*math.sin(ang),r[1])
    dot(a,x+(rad-1)*math.cos(ang),y+(rad-1)*math.sin(ang),r[0])
    dot(a,x,y,r[0])

def chariot(f,clip='move',boss=False):
    n=64 if boss else 32;a=px.new(n,n);r=px.ramp('pursuit');s=px.ramp('chainsteel');b=px.ramp('crownbronze')
    if boss:
        # Empty armored carriage: large wheel pair, cage and forward ram. No driver.
        bob=(0,-1,0,1)[f%4] if clip=='move' else 0
        crouch=f if clip=='windup' else 0
        y=27+bob+crouch
        rect(a,13,y+6,49,y+22,r[2]);rect(a,11,y+9,16,y+19,r[1])
        rect(a,18,y-9,43,y+5,s[2]);rect(a,21,y-6,40,y+2,K)
        for x in (20,28,36,42):rect(a,x,y-8,x+1,y+4,s[1]);dot(a,x,y-8,s[0])
        rect(a,16,y-11,45,y-9,s[1]);rect(a,18,y-12,42,y-12,s[0])
        # Jagged pressure plates rather than a crown or helmet.
        for x,dy in ((18,-15),(29,-17),(41,-14)):
            rect(a,x,y+dy,x+3,y-11,r[1]);dot(a,x,y+dy,r[0])
        rect(a,43,y+3,51,y+15,b[2]);rect(a,47,y+5,54,y+12,b[1])
        line(a,52,y+7,58,y+9,b[0]);line(a,58,y+9,52,y+12,b[2])
        for x in (17,26,35,44):dot(a,x,y+9,s[0]);line(a,x,y+12,x-2,y+17,s[2])
        rect(a,15,y+20,49,y+23,s[2]);rect(a,17,y+20,48,y+20,s[0])
        wheel(a,20,54,7,f,s);wheel(a,45,54,7,f+1,s)
        # Windup visibly compresses cage; ratchet lamps charge within the body only.
        if clip=='windup':
            for i in range(f+1):rect(a,44,32-i*3,47,33-i*3,b[0])
        if clip=='dash':
            rect(a,44,30,50,32,b[0]);line(a,8,37+f,14,39+f,r[1]);line(a,6,45-f,12,45-f,s[2])
        if clip=='death':
            # Collapse into inert components; binary alpha, no shockwave/impact packet.
            shift=(0,4,8,12)[f]
            if shift:
                cut=42-f*4
                upper=a[:cut].copy();a[:cut]=0
                a[shift:cut+shift]=upper
                rect(a,29-f,18+shift,31+f,47,K)
                line(a,13,22+shift,25,31+shift,K)
            if f>=2:
                rect(a,18,15,46,28, (0,0,0,0));line(a,6,58,12,57,s[1]);line(a,51,58,57,59,b[2])
    else:
        bob=(0,-1,0,1)[f%4] if clip=='move' else 0;y=12+bob+(f if clip=='windup' else 0)
        rect(a,8,y+1,22,y+11,r[2]);rect(a,10,y-5,20,y,r[1]);rect(a,12,y-3,18,y-1,K)
        rect(a,9,y-6,21,y-5,s[1]);dot(a,10,y-6,s[0])
        rect(a,7,y+9,24,y+11,s[2]);rect(a,8,y+9,23,y+9,s[0])
        rect(a,22,y+2,26,y+8,b[1]);line(a,25,y+3,28,y+5,b[0]);line(a,28,y+5,25,y+7,b[2])
        wheel(a,10,26,3,f,s);wheel(a,22,26,3,f+1,s)
        if clip=='windup':rect(a,21,13-f,24,14-f,b[0]);dot(a,7-f,21,s[0])
        if clip=='dash':line(a,3,17+f,7,18+f,r[1]);line(a,3,22-f,6,22-f,s[2]);rect(a,23,14,25,15,b[0])
    return finish(a)
GENERATORS={
 'enm_ex7-labor-chain_move_32':labor_chain,
 'enm_ex8-crown-mask_move_32':crown_mask,
}
for clip in ('move','windup','dash'):
    GENERATORS[f'enm_ex9-chariot-afterimage_{clip}_32']=lambda f,c=clip:chariot(f,c)
for clip in ('move','windup','dash','death'):
    GENERATORS[f'bos_oppression-chariot_{clip}_64']=lambda f,c=clip:chariot(f,c,True)
