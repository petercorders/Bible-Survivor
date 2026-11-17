"""Original R14 native-grid pixels authored by Codex gpt-6.1-sol.
R04 minimum pipeline and Art03 helpers reused; Claude/Design/image model unused.
Fictional nonhuman ambush, discord and idolatry; no people or sacred office.
"""
import json, os
import px03 as px
ROOT=os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
EXT=json.load(open(os.path.join(ROOT,'src','palette_exodus_r05_ext.json')))
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
def tube(a,pts,r,c): px.tube(a,pts,lambda t:r,lambda *args:c)
def ell(a,x,y,rx,ry,ramp):
    px.shade_ellipse(a,x,y,rx,ry,0,lambda x,y,u,v,it:ramp[min(len(ramp)-1,px.level(it,(.84,.60,.33)))])
def finish(a): return px.ground_to(px.outline(a),30)

def ambush_shadow(frame=0):
    a=px.new(32,32);r=px.ramp('r14shade');f=frame%4
    s=(0,1,0,-1)[f];bob=(0,-1,-1,0)[f]
    # One low horizontal beast; bent claw legs, no human head/torso or costume.
    tube(a,[(12,22+bob),(7+s,19+bob),(4,16+bob)],1.5,r[2])
    ell(a,15,23+bob,7,4,r)
    ell(a,21,20+bob,5,4,r)
    tube(a,[(20,18+bob),(22,15+bob),(25,16+bob)],1,r[1])
    rect(a,24,20+bob,27,22+bob,r[2]);dot(a,25,19+bob,r[0])
    for x,dx in ((11,s),(20,-s)):
        tube(a,[(x,24+bob),(x+dx-2,27),(x+dx+1,29)],1,r[2])
    # Forward hooked talons belong to the same body, never emitted projectiles.
    tube(a,[(22,23+bob),(25+s,25),(27+s,27)],.8,r[1])
    tube(a,[(23,24+bob),(25+s,27),(27+s,28)],.6,r[0])
    return finish(a)

def discord_stone_armor(frame=0):
    a=px.new(32,32);r=px.ramp('r14stone');f=frame%4
    s=(0,1,0,-1)[f];bob=(0,-1,-1,0)[f]
    # Wide irregular carapace, three low stone treads; no paired law tablets.
    for x,dx in ((8,s),(16,-s),(24,s)):
        rect(a,x+dx-2,26,x+dx+2,29,r[3]);rect(a,x+dx-2,28,x+dx+2,29,r[2])
    ell(a,16,19+bob,12,9,r)
    ell(a,11,12+bob,6,5,[r[1],r[2],r[3]])
    ell(a,20,11+bob,5,4,[r[0],r[1],r[2]])
    rect(a,7,10+bob,10,13+bob,r[1]);rect(a,18,6+bob,21,9+bob,r[1])
    ell(a,25,19+bob,3,5,[r[1],r[2],r[3]])
    rect(a,26,18+bob,28,20+bob,K)
    # Irregular rock clefts, no letters, symbols, shield or immunity indication.
    tube(a,[(14,10+bob),(15,15+bob),(12,18+bob),(15,23+bob)],.55,r[3])
    tube(a,[(21,16+bob),(19,20+bob),(23,24+bob)],.55,r[3])
    return finish(a)

def golden_mask(frame=0):
    a=px.new(32,32);r=px.ramp('r14gold');d=px.ramp('r14shade');f=frame%4
    s=(0,1,0,-1)[f];bob=(0,-1,-1,0)[f]
    # One nonhuman low support under a broad bovine mask, not a robed worshipper.
    ell(a,15,24+bob,8,4,d)
    for x,dx in ((10,s),(22,-s)):
        tube(a,[(x,25+bob),(x+dx,28),(x+dx+2,29)],1.3,d[2])
    # Swept horns and wide ears, with solid matte ochre, no floating gold glints.
    tube(a,[(11,13+bob),(7,10+bob),(6,5+bob),(9,7+bob)],1.2,r[1])
    tube(a,[(22,12+bob),(26,9+bob),(27,4+bob),(24,6+bob)],1.2,r[0])
    ell(a,17,16+bob,8,8,r)
    tube(a,[(10,13+bob),(6,14+bob),(9,17+bob)],1,r[2])
    tube(a,[(23,13+bob),(27,13+bob),(25,16+bob)],1,r[1])
    # Off-centre broad right-facing muzzle and hollow eye distinguish a cow mask.
    ell(a,22,21+bob,6,4,[r[1],r[2],r[3]])
    rect(a,20,14+bob,23,16+bob,K);dot(a,26,20+bob,K)
    rect(a,20,24+bob,25,24+bob,r[3]);dot(a,16,11+bob,r[0])
    return finish(a)

GENERATORS={
 'enm_ex13-ambush-shadow_move_32':ambush_shadow,
 'enm_ex14-discord-stone-armor_move_32':discord_stone_armor,
 'enm_ex15-golden-mask_move_32':golden_mask,
}
