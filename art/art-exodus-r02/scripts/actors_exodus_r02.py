"""Original R11 native-grid pixels authored by Codex gpt-6.1-sol.
R10 pipeline and Art03 helpers reused; no Claude Design/image model/canvas.
Fictional reed, lantern and mud constructs, never people or ethnic symbols.
"""
import json, os
import px03 as px
ROOT=os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
EXT=json.load(open(os.path.join(ROOT,'src','palette_exodus_r02_ext.json')))
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

def reed_stalker(frame=0):
    a=px.new(32,32);r=px.ramp('reed');m=px.ramp('mud')
    f=frame%4;step=(0,1,0,-1)[f];bob=(0,-1,-1,0)[f];sway=(-1,0,1,0)[f]
    # Low bundled body with four short root feet. Reed tips remain visibly attached.
    for x,s in ((9,-step),(21,step)):
        tube(a,[(x,22+bob),(x+s,28)],1.3,m[2]);rect(a,x+s-1,28,x+s+1,29,m[1])
    ell(a,15,20+bob,9,5,r)
    for x,top in ((7,10),(11,6),(15,9),(19,5),(22,10)):
        tube(a,[(x,19+bob),(x+sway,top+bob)],.65,r[1]);
        tube(a,[(x+sway,top+2+bob),(x+sway-2,top+bob)],.65,r[0])
        dot(a,x+sway,top-1+bob,m[1])
    tube(a,[(7,18+bob),(13,20+bob),(22,18+bob)],.8,m[2])
    ell(a,25,20+bob,3.3,3,r);rect(a,25,21+bob,28,22+bob,r[2])
    dot(a,26,18+bob,px.C('eye.0'))
    for x,s in ((12,step),(23,-step)):
        tube(a,[(x,23+bob),(x+s,28)],1.3,r[2]);rect(a,x+s-1,28,x+s+1,29,r[1])
    return finish(a)

def search_lantern(frame=0):
    a=px.new(32,32);m=px.ramp('lanternmetal');l=px.ramp('lantern')
    f=frame%4;bob=(0,-1,-2,-1)[f];sway=(-1,0,1,1)[f]
    # Empty mechanical eye lantern. All pale light stays inside the metal cage.
    tube(a,[(12,9+bob),(12+sway,6+bob),(19+sway,6+bob),(21,10+bob)],.8,m[1])
    rect(a,10,11+bob,23,23+bob,m[2]);rect(a,12,12+bob,21,21+bob,l[1])
    ell(a,18,17+bob,4.5,4,l);rect(a,19,15+bob,20,19+bob,K);dot(a,18,15+bob,l[0])
    rect(a,8,10+bob,24,11+bob,m[1]);rect(a,9,23+bob,24,25+bob,m[2])
    rect(a,11,12+bob,12,22+bob,m[1]);rect(a,22,12+bob,23,22+bob,m[1])
    rect(a,9,24+bob,23,24+bob,m[0]);rect(a,11,9+bob,22,9+bob,m[0])
    tube(a,[(15,26+bob),(14+sway,28+bob)],.65,m[1]);dot(a,14+sway,29+bob,m[0])
    return px.outline(a)

def nile_hunter(frame=0):
    a=px.new(32,32);h=px.ramp('hunter');m=px.ramp('mud');r=px.ramp('reed')
    f=frame%4;step=(0,2,0,-2)[f];bob=(0,-1,-1,0)[f];tail=(-1,0,1,0)[f]
    # Angular fast quadruped, nonhuman and without historical or royal motifs.
    tube(a,[(10,20+bob),(6,18+bob),(3,14+bob+tail)],1,h[2])
    for x,s in ((12,-step),(22,step)):
        tube(a,[(x,22+bob),(x+s-1,27),(x+s+1,28)],1,h[3]);rect(a,x+s,28,x+s+2,29,m[2])
    ell(a,17,20+bob,8,4,h);ell(a,24,17+bob,4,3.5,h)
    rect(a,24,18+bob,28,20+bob,h[1]);dot(a,26,15+bob,px.C('eye.0'));dot(a,28,19+bob,K)
    for x,y in ((11,18),(16,17),(19,20)):
        rect(a,x,y+bob,x+2,y+1+bob,m[1]);dot(a,x,y+bob,m[0])
    for x,top in ((12,11),(16,9),(20,11)):
        tube(a,[(x,17+bob),(x-2+tail,top+bob)],.65,r[1]);dot(a,x-2+tail,top+bob,r[0])
    for x,s in ((14,step),(24,-step)):
        tube(a,[(x,22+bob),(x+s,27)],1.2,h[1]);rect(a,x+s-1,28,x+s+2,29,m[1])
    return finish(a)

GENERATORS={
    'enm_ex4-reed-stalker_move_32':reed_stalker,
    'enm_ex5-search-lantern_move_32':search_lantern,
    'enm_ex6-nile-hunter_move_32':nile_hunter,
}
