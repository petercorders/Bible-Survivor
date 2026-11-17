"""Original R10 enemy pixels authored by Codex as deterministic grid shapes.
Reuses art03 px03 helper; R08 build/conversion/check structure. No image model,
Claude Design or borrowed enemy drawing. Empty constructs symbolize oppression,
not any biblical person, ethnicity or culture. Needs numpy through px03.
"""
import json, os
import px03 as px
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
EXT = json.load(open(os.path.join(ROOT, 'src', 'palette_exodus_r01_ext.json')))
for name, cols in EXT['actor'].items():
    if name+'.0' in px.PAL:
        raise ValueError('Palette ramp collision: '+name)
    for k, col in enumerate(cols):
        rgba=px.hex_rgba(col)
        px.PAL[f'{name}.{k}']=rgba
        px.NAME_OF.setdefault(rgba,f'{name}.{k}')
        px.RAMP_OF.setdefault(rgba,name)
K=px.C('outline.0')

def dot(a,x,y,c): px.put(a,round(x),round(y),c)
def rect(a,x0,y0,x1,y1,c):
    for y in range(round(y0),round(y1)+1):
        for x in range(round(x0),round(x1)+1): dot(a,x,y,c)
def tube(a,pts,r,c): px.tube(a,pts,lambda t:r,lambda *args:c)
def ell(a,x,y,rx,ry,ramp):
    px.shade_ellipse(a,x,y,rx,ry,0,lambda x,y,u,v,it:ramp[min(len(ramp)-1,px.level(it,(.84,.60,.33)))])
def finish(a): return px.ground_to(px.outline(a),30)

def brick_burden(frame=0):
    a=px.new(32,32); b=px.ramp('burden'); br=px.ramp('brick')
    f=frame%4; step=(0,1,0,-1)[f]; bob=(0,-1,-1,0)[f]
    # Four broad, blunt feet under a low nonhuman body. Alternating diagonal step.
    for x,s in ((9,-step),(20,step)):
        tube(a,[(x,21+bob),(x+s,27)],1.6,b[3])
        rect(a,x+s-1,27,x+s+2,28,b[2])
    tube(a,[(7,19+bob),(4,22+bob)],1,b[2])
    ell(a,14,20+bob,8,4.3,b)
    # Masonry block stack: horizontal mortar, staggered vertical seams.
    rect(a,7,9+bob,22,16+bob,br[2])
    for x,y,w in ((7,9,6),(15,9,7),(9,13,6),(17,13,5)):
        rect(a,x,y+bob,x+w-1,y+2+bob,br[1])
        rect(a,x,y+bob,x+w-1,y+bob,br[0])
        dot(a,x,y+2+bob,br[2])
    # Two straps hold the burden in place rather than leaving a wall behind.
    for x in (10,20):
        rect(a,x,9+bob,x,18+bob,b[3]);dot(a,x,10+bob,b[1])
    ell(a,24,20+bob,3.6,3.0,b)
    rect(a,24,20+bob,28,22+bob,b[2])
    dot(a,26,18+bob,px.C('eye.0'));dot(a,28,21+bob,K)
    for x,s in ((12,step),(23,-step)):
        tube(a,[(x,22+bob),(x+s,28)],1.7,b[1])
        rect(a,x+s-1,28,x+s+2,29,b[2]);dot(a,x+s,27,b[0])
    return finish(a)

def watching_eye(frame=0):
    a=px.new(32,32); b=px.ramp('burden'); w=px.ramp('watch'); ir=px.ramp('iris')
    f=frame%4; bob=(0,-1,-2,-1)[f]; fan=(0,-1,0,1)[f]
    # Ragged trailing membrane establishes right-facing movement, not a human face.
    for pts,r in (([(12,13+bob),(7,9+bob+fan),(4,11+bob)],1.3),
                  ([(12,17+bob),(5,17+bob+fan),(3,20+bob)],1.4),
                  ([(13,21+bob),(8,25+bob+fan),(5,25+bob)],1.2)):
        tube(a,pts,r,b[2])
    ell(a,18,17+bob,9,7,b)
    ell(a,20,17+bob,6.5,5.3,w)
    ell(a,21,17+bob,3.3,4.0,ir)
    rect(a,21,14+bob,22,20+bob,K)
    dot(a,20,14+bob,w[0]);dot(a,20,15+bob,w[0])
    # Thick upper lid, one moving rear fold; pupil stays open in all four frames.
    tube(a,[(14,13+bob),(18,11+bob),(23,12+bob),(26,14+bob)],.65,b[1])
    dot(a,10,17+bob+fan,b[0])
    return px.outline(a)

def command_chain_armor(frame=0):
    a=px.new(32,32); ar=px.ramp('command'); ch=px.ramp('chain')
    f=frame%4; step=(0,1,0,-1)[f]; bob=(0,0,-1,0)[f]
    # Wide slab feet, empty gap at waist/neck; no human wearer or royal regalia.
    for x,s,c in ((12,-step,ar[2]),(21,step,ar[1])):
        rect(a,x-2,23+bob,x+1,27,c)
        rect(a,x-3+s,28,x+2+s,29,ar[2])
        rect(a,x-2+s,28,x+1+s,28,c)
    rect(a,12,12+bob,23,22+bob,ar[2])
    rect(a,13,12+bob,21,19+bob,ar[1])
    rect(a,13,12+bob,21,13+bob,ar[0])
    rect(a,15,20+bob,23,22+bob,ar[3])
    # Large asymmetric pauldrons; the leading one projects toward the right.
    ell(a,9,14+bob,3.2,3.2,ar)
    rect(a,7,15+bob,10,22+bob,ar[2])
    ell(a,25,14+bob,3.3,3.0,ar)
    rect(a,24,16+bob,27,22+bob,ar[1]);rect(a,25,21+bob,28,23+bob,ar[2])
    # Blank helmet and right-facing slit, nothing resembling a historic crown.
    rect(a,14,5+bob,23,10+bob,ar[2])
    rect(a,15,4+bob,21,5+bob,ar[0]);rect(a,14,6+bob,16,8+bob,ar[1])
    rect(a,18,7+bob,25,8+bob,K);dot(a,23,7+bob,px.C('eye.0'))
    rect(a,16,10+bob,23,11+bob,ar[3]);dot(a,23,10+bob,ar[1])
    # Short linked decorations stay attached across chest: never swung toward player.
    swing=(0,0,1,0)[f]
    for x,y in ((11,15),(13,17),(15,18+swing),(17,19+swing),(19,18+swing),(21,17),(23,15)):
        dot(a,x,y+bob,ch[0]);dot(a,x+1,y+bob,ch[1]);dot(a,x,y+1+bob,ch[2])
    return finish(a)

GENERATORS={
    'enm_ex1-brick-burden_move_32':brick_burden,
    'enm_ex2-watching-eye_move_32':watching_eye,
    'enm_ex3-command-chain-armor_move_32':command_chain_armor,
}
