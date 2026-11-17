"""Original R13 native-grid pixels authored by Codex gpt-6.1-sol.
R10 pipeline and Art03 helpers reused; no Claude Design/image model/canvas.
Fictional banner, empty jar and sack constructs; never people, souls or miracles.
"""
import json, os
import px03 as px
ROOT=os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
EXT=json.load(open(os.path.join(ROOT,'src','palette_exodus_r04_ext.json')))
for name,cols in EXT['actor'].items():
    if name+'.0' in px.PAL: raise ValueError('Palette collision: '+name)
    for k,col in enumerate(cols):
        rgba=px.hex_rgba(col);px.PAL[f'{name}.{k}']=rgba
        px.NAME_OF.setdefault(rgba,f'{name}.{k}');px.RAMP_OF.setdefault(rgba,name)
K=px.C('outline.0')
def dot(a,x,y,c): px.put(a,round(x),round(y),c)
def rect(a,x0,y0,x1,y1,c):
    for y in range(round(y0),round(y1)+1):
        for x in range(round(x0),round(x1)+1): dot(a,x,y,c)
def tube(a,pts,r,c): px.tube(a,pts,lambda t:r,lambda *args:c)
def ell(a,x,y,rx,ry,ramp):
    px.shade_ellipse(a,x,y,rx,ry,0,lambda x,y,u,v,it:ramp[min(len(ramp)-1,px.level(it,(.84,.60,.33)))])
def finish(a): return px.ground_to(px.outline(a),30)

def retreat_echo(frame=0):
    a=px.new(32,32);r=px.ramp('r13cloth');c=px.ramp('r13clay')
    f=frame%4;s=(0,1,0,-1)[f];w=(-1,0,1,0)[f]
    # Narrow banner, ragged trailing edge and no head, helmet, uniform or human limbs.
    tube(a,[(16,28),(19,6)],.7,c[2])
    for y in range(7,22):
        left=8+w+(y//3)%2
        right=18-(y//5)%2
        rect(a,left,y,right,y,r[1] if y<15 else r[2])
    for x,y in ((8,9),(8,15),(10,20)):
        rect(a,x+w,y,x+2+w,y+1,r[0])
    # Cut three unmistakable tears into the trailing edge.
    for x,y in ((8+w,12),(9+w,17),(10+w,21)):
        rect(a,x,y,x+3,y+1,(0,0,0,0))
    tube(a,[(17,24),(13+s,28),(10+s,29)],1,r[2])
    tube(a,[(17,25),(21-s,28),(23-s,29)],1,r[1])
    rect(a,19,7,21,8,c[1]);dot(a,20,9,c[0])
    return finish(a)

def wilderness_grumble(frame=0):
    a=px.new(32,32);r=px.ramp('r13clay');sack=px.ramp('r13sack')
    f=frame%4;s=(0,1,0,-1)[f];bob=(0,-1,-1,0)[f]
    # Low sand skirt supports a wide open, empty broken jar, not a person.
    for x,k in ((6,-s),(14,s),(23,-s)):
        tube(a,[(15,24+bob),(x+k,28),(x+k+2,29)],1.3,sack[2])
    ell(a,16,22+bob,10,4,sack)
    ell(a,17,17+bob,8,8,r)
    rect(a,11,9+bob,23,11+bob,r[1])
    ell(a,18,11+bob,6,2,[r[3]])
    rect(a,11,8+bob,14,9+bob,r[0]);rect(a,20,8+bob,23,9+bob,r[0])
    # Large right-facing gaping mouth cut into the clay body; no eyes or emitted aura.
    ell(a,22,18+bob,4,4,[K]);rect(a,23,16+bob,26,20+bob,K)
    dot(a,23,15+bob,r[0]);dot(a,23,22+bob,r[0])
    tube(a,[(12,13+bob),(14,16+bob),(12,19+bob)],.55,r[3])
    dot(a,9+s,25+bob,sack[0]);dot(a,22-s,26+bob,sack[1])
    return finish(a)

def greedy_sack(frame=0):
    a=px.new(32,32);r=px.ramp('r13sack');cloth=px.ramp('r13cloth')
    f=frame%4;s=(0,1,0,-1)[f];bob=(0,-1,-1,0)[f];tie=(-1,0,1,0)[f]
    # Tall heavy rounded sack, tied neck and large black side mouth.
    for x,k in ((10,s),(23,-s)):
        tube(a,[(x,24+bob),(x+k,28)],1.7,r[3]);rect(a,x+k-2,28,x+k+1,29,r[2])
    ell(a,16,19+bob,11,10,r)
    rect(a,13,6+bob,20,10+bob,r[1]);rect(a,12,5+bob,20,6+bob,cloth[2])
    tube(a,[(16,5+bob),(12+tie,3+bob)],.65,r[1])
    tube(a,[(18,5+bob),(21+tie,3+bob)],.65,r[2])
    ell(a,22,20+bob,5,6,[K]);rect(a,24,17+bob,27,23+bob,K)
    rect(a,21,14+bob,24,15+bob,r[0]);rect(a,21,25+bob,24,26+bob,r[1])
    # Cloth seam and block stitches, distinct from a glow, gem or reward bag icon.
    tube(a,[(10,12+bob),(9,18+bob),(11,25+bob)],.55,cloth[2])
    for y in (14,18,22):rect(a,8,y+bob,11,y+bob,cloth[1])
    return finish(a)

GENERATORS={
 'enm_ex10-retreat-echo_move_32':retreat_echo,
 'enm_ex11-wilderness-grumble_move_32':wilderness_grumble,
 'enm_ex12-greedy-sack_move_32':greedy_sack,
}
