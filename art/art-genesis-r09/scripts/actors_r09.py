"""Original Codex gpt-6.1-sol script pixel art, R09 Genesis44.
No Claude/Design/image provider/canvas. Palette shapes rasterized at native cell size.
px03 helper lineage: unchanged R06 copy of Art03. No external raster inputs.
"""
import json,os,math
import px03 as px
ROOT=os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
EXT=json.load(open(os.path.join(ROOT,'src','palette_r09_ext.json')))
for ramp,cols in EXT['actor'].items():
    assert f'{ramp}.0' not in px.PAL
    for i,col in enumerate(cols):
        v=px.hex_rgba(col); px.PAL[f'{ramp}.{i}']=v
        px.NAME_OF.setdefault(v,f'{ramp}.{i}'); px.RAMP_OF.setdefault(v,ramp)
S=px.ramp('r09silver'); B=px.ramp('r09sack'); V=px.ramp('r09veil'); E=px.ramp('r09ember'); K=px.C('outline.0')

def poly(a,points,col):
    # Pixel-centre scan conversion, only native-grid solid palette colours.
    for y in range(max(0,int(min(p[1] for p in points))),min(a.shape[0],int(max(p[1] for p in points))+1)):
        for x in range(max(0,int(min(p[0] for p in points))),min(a.shape[1],int(max(p[0] for p in points))+1)):
            inside=False; j=len(points)-1
            for i in range(len(points)):
                xi,yi=points[i]; xj,yj=points[j]
                if (yi>y+.5)!=(yj>y+.5) and x+.5<(xj-xi)*(y+.5-yi)/(yj-yi)+xi:inside=not inside
                j=i
            if inside:px.put(a,x,y,col)

def ell(a,x,y,rx,ry,ramp):
    px.lit_ellipse(a,x,y,rx,ry,ramp,cuts=(.82,.56,.28))

def link(a,x,y,rx=2,ry=3):
    # Separate hollow links: empty centres stay transparent or show the body underneath.
    for yy in range(max(1,int(y-ry-1)),min(a.shape[0]-1,int(y+ry+2))):
        for xx in range(max(1,int(x-rx-1)),min(a.shape[1]-1,int(x+rx+2))):
            q=((xx-x)/rx)**2+((yy-y)/ry)**2
            if .30<=q<=1.15:px.put(a,xx,yy,S[0] if xx<x and yy<=y else S[2])

def finish(a,foot):return px.ground_to(px.outline(a),foot)

def i1(k):
    a=px.new(32,32); bob=[0,-1,0,1][k]; step=[-1,1,1,-1][k]
    # Low animal silhouette; one large silver coin with stamped notch.
    poly(a,[(5,19+bob),(2,14+bob),(9,17+bob),(13,22+bob)],V[2])
    for x,d in [(10,step),(20,-step)]:
        poly(a,[(x,23+bob),(x+4,23+bob),(x+4+d,28),(x+d-1,28)],V[2])
        px.line(a,x+d-1,28,x+4+d,28,S[2])
    ell(a,15,21+bob,9,5,V)
    ell(a,13,15+bob,6,6,S)
    px.line(a,12,11+bob,12,18+bob,S[2]);px.line(a,14,12+bob,14,16+bob,S[1])
    poly(a,[(21,16+bob),(22,11+bob),(25,16+bob)],V[1])
    ell(a,24,20+bob,4,4,V)
    poly(a,[(24,21+bob),(28,20+bob),(28,24+bob),(23,23+bob)],K)
    px.line(a,25,21+bob,27,21+bob,S[0]);px.put(a,27,23+bob,S[1]);px.put(a,25,18+bob,E[0])
    return finish(a,30)

def i2(k):
    a=px.new(32,32); sway=[-1,0,1,0][k]; bob=[0,-1,0,1][k]
    # Asymmetric right-facing empty mask, one trailing cloth tail.
    poly(a,[(13,8+bob),(19,8+bob),(23,17+bob),(25,27),(19,25),(16+sway,29),(13,26),(7+sway,28),(10,18+bob)],V[2])
    poly(a,[(13,11+bob),(16,13+bob),(15+sway,26),(11+sway,25)],V[1])
    poly(a,[(18,15+bob),(21,18+bob),(23,26),(18,24)],V[3])
    poly(a,[(14,8+bob),(19,6+bob),(24,9+bob),(25,15+bob),(21,20+bob),(16,17+bob)],S[1])
    poly(a,[(14,8+bob),(19,6+bob),(23,9+bob),(17,10+bob),(17,16+bob),(15,14+bob)],S[0])
    poly(a,[(20,11+bob),(24,11+bob),(23,13+bob),(20,14+bob)],K)
    px.put(a,22,12+bob,E[0]);px.line(a,20,16+bob,22,16+bob,S[3])
    # Tiny collar grain knot, no human torso or skin.
    px.put(a,16,20+bob,B[0]);px.put(a,17,21+bob,B[2])
    return finish(a,30)

def i3(k):
    a=px.new(32,32); bob=[0,-1,0,0][k]; step=[-1,0,1,0][k]
    for x,d in [(8,step),(23,-step)]:
        poly(a,[(x-1,23),(x+3,23),(x+3+d,28),(x-1+d,28)],V[2])
        px.line(a,x-1+d,28,x+3+d,28,S[2])
    ell(a,16,19+bob,11,9,B)
    poly(a,[(10,11+bob),(8,5+bob),(14,8+bob),(20,5+bob),(22,11+bob)],B[1])
    px.line(a,11,11+bob,20,11+bob,B[3]);px.line(a,15,12+bob,13,23+bob,B[2])
    px.line(a,9,20+bob,21,23+bob,B[2]);px.put(a,10,22+bob,B[0])
    # Chained bag edges but centre is visibly separated, never drawn across a passage.
    for x,y in [(6,16+bob),(7,21+bob),(25,15+bob),(24,20+bob)]:link(a,x,y,2,2.4)
    px.line(a,11,17+bob,14,18+bob,K);px.line(a,20,17+bob,23,16+bob,K)
    px.put(a,13,17+bob,E[0]);px.put(a,22,17+bob,E[0])
    return finish(a,30)

def boss(k,mode='move'):
    a=px.new(64,64)
    dead=mode=='death'; collapse=k if dead else 0
    lift=([0,-1,0,1][k] if mode=='move' else -k if mode=='windup' else [0,2][k] if mode=='attack' else k*3)
    cy=21+lift
    # Grain sack feet and lower mass anchor the 64px cup; no well cylinder.
    stride=[-1,1,1,-1][k] if mode=='move' else 0
    for x,st in [(22,stride),(43,-stride)]:
        poly(a,[(x-5,49),(x+5,49),(x+6+st,60),(x-6+st,60)],B[2])
        px.line(a,x-6+st,60,x+6+st,60,S[2])
    ell(a,33,46+min(collapse,2),17,11-min(collapse,2),B)
    px.line(a,24,40,29,52,B[2]);px.line(a,42,41,39,53,B[2])
    for x,y in [(23,44),(27,47),(39,49)]:px.put(a,x,y,B[0])
    # Broad goblet bowl tapering to a thin stem, strong silver rim at the top.
    if collapse<3:
        poly(a,[(13,cy-8),(53,cy-8),(49,cy+7),(41,cy+14),(36,cy+15),(36,40),(42,42),(23,42),(29,39),(29,cy+15),(23,cy+12),(17,cy+6)],S[2])
        poly(a,[(15,cy-7),(26,cy-7),(25,cy+9),(31,cy+14),(25,cy+11),(19,cy+4)],S[0])
        poly(a,[(28,cy-7),(42,cy-7),(40,cy+10),(35,cy+13),(29,cy+10)],S[1])
        poly(a,[(45,cy-7),(52,cy-7),(48,cy+7),(41,cy+12)],S[3])
        px.line(a,31,cy+15,31,39,S[0]);px.line(a,25,41,40,41,S[1])
        ell(a,33,cy-8,20,5,S)
        ell(a,34,cy-8,16,2.4,[K,K,K,K])
        px.line(a,18,cy-10,44,cy-10,S[0])
        # Nonhuman dark mouth in the cup, no eyes / human effigy.
        if mode in ('windup','attack'):
            px.line(a,28,cy-8,41,cy-8,E[0] if mode=='attack' else E[1])
            px.line(a,31,cy-7,38,cy-7,E[1])
    else:
        # Spilled empty bowl, visibly broken stem, finite death pose only.
        poly(a,[(12,45),(40,43),(47,50),(37,56),(18,54)],S[2])
        px.line(a,14,46,38,45,S[0]);px.line(a,18,49,35,47,S[1])
        px.line(a,40,55,48,58,S[1]);px.line(a,40,56,48,59,S[3])
    # Loose chain arms, expanding in windup and separating further on death.
    spread=k*2 if mode=='windup' else 3 if mode=='attack' else k*2 if dead else k%2
    for side in [-1,1]:
        x=33+side*(20+min(spread,4)); y=37+(k*3 if dead else -k*2 if mode=='windup' else 0)
        link(a,x,y,2.8,4)
        if not dead or k<2:link(a,x+side*2,y+8,2.3,3)
        else:link(a,x-side*2,54,2.8,2)
    # Grain tie and stitch; it is an object monster, not a bound biblical person.
    px.line(a,29,45,34,48,B[3]);px.line(a,34,45,29,48,B[3])
    return finish(a,62)

def projectile(k):
    a=px.new(16,16)
    # Longitudinal motion points right; head at x12, trailing separate open links.
    link(a,4,8,2,2);link(a,8,8,2,2.8)
    poly(a,[(10,5),(13,7),(13,9),(10,11)],S[2])
    px.line(a,10,5,12,7,S[0] if k==0 else S[1]);px.put(a,12,8,S[0]);px.put(a,10,10,S[1] if k==0 else S[0])
    return px.outline(a)

GENERATORS={
'enm_i1-silver-devourer_move_32':i1,
'enm_i2-concealing-mask_move_32':i2,
'enm_i3-brother-binder_move_32':i3,
'bos_silver-cup-accuser_move_64':lambda k:boss(k,'move'),
'bos_silver-cup-accuser_windup_64':lambda k:boss(k,'windup'),
'bos_silver-cup-accuser_attack_64':lambda k:boss(k,'attack'),
'bos_silver-cup-accuser_death_64':lambda k:boss(k,'death'),
'prj_accuser-chain_fly_16':projectile}
