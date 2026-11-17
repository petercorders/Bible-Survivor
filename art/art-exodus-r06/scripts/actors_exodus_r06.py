"""Original Codex R15 native-grid candidates; no image/Claude/Design provider.
Fictional thread/metal constructs, never sacred objects, people or divine presence.
"""
import json
import math
import os
import px03 as px

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
EXT = json.load(open(os.path.join(ROOT, 'src/palette_exodus_r06_ext.json')))
for name, cols in EXT['actor'].items():
    if name + '.0' in px.PAL:
        raise ValueError('Palette collision: ' + name)
    for i, col in enumerate(cols):
        rgba = px.hex_rgba(col)
        px.PAL[f'{name}.{i}'] = rgba
        px.NAME_OF.setdefault(rgba, f'{name}.{i}')
        px.RAMP_OF.setdefault(rgba, name)
K = px.C('outline.0')
M, T, R = (px.ramp(n) for n in ('r15metal', 'r15thread', 'r15rust'))


def line(a, x, y, xx, yy, col):
    px.line(a, x, y, xx, yy, col)


def dot(a, x, y, col):
    px.put(a, round(x), round(y), col)


def poly(a, points, col):
    # Integer lattice fill: no antialiasing, interpolation or external image input.
    for y in range(max(0, min(p[1] for p in points)), min(a.shape[0], max(p[1] for p in points) + 1)):
        for x in range(max(0, min(p[0] for p in points)), min(a.shape[1], max(p[0] for p in points) + 1)):
            inside = False
            j = len(points) - 1
            for i, (xi, yi) in enumerate(points):
                xj, yj = points[j]
                if (yi > y) != (yj > y) and x < (xj - xi) * (y - yi) / (yj - yi) + xi:
                    inside = not inside
                j = i
            if inside:
                dot(a, x, y, col)
    for p, q in zip(points, points[1:] + points[:1]):
        line(a, *p, *q, col)


def ell(a, x, y, rx, ry, ramp):
    px.lit_ellipse(a, x, y, rx, ry, ramp)


def strand(a, points, col):
    for p, q in zip(points, points[1:]):
        line(a, *p, *q, col)


def coil(a, x, y, rx, ry, f, ramp=T):
    points = []
    for i in range(40):
        angle = i * math.pi / 12 + f * .2
        scale = 1 - i / 54
        points.append((round(x + math.cos(angle) * rx * scale), round(y + math.sin(angle) * ry * scale)))
    strand(a, points, ramp[2])
    strand(a, [(x - rx, y - 1), (x - rx + 2, y - ry + 2), (x, y - ry)], ramp[1])


def finish(a):
    return px.ground_to(px.outline(a), a.shape[0] - 2)


def greedy_hook(f):
    a = px.new(32, 32)
    b = (0, -1, 0, 1)[f]
    sway = (-1, 0, 1, 0)[f]
    # A compact open metal claw, no hands, purse, coin or victim.
    ell(a, 14, 20 + b, 6, 7, R)
    poly(a, [(12, 14+b), (18, 11+b), (20, 18+b), (16, 24+b), (10, 22+b)], M[2])
    line(a, 12, 15+b, 17, 13+b, M[0])
    strand(a, [(17, 20+b), (23, 18+b), (26, 12+b), (25, 7+b), (22, 6+b), (20, 9+b)], M[1])
    strand(a, [(18, 21+b), (24, 19+b), (28, 12+b), (27, 7+b), (24, 4+b), (20, 5+b)], M[2])
    line(a, 21, 5+b, 24, 5+b, M[0])
    dot(a, 20, 9+b, M[0])
    # Short nonhuman metal supports keep the ground anchor fixed.
    strand(a, [(11, 23+b), (7+sway, 27), (10+sway, 29)], R[1])
    strand(a, [(17, 24+b), (20-sway, 28), (23-sway, 29)], M[1])
    line(a, 7, 17+b, 5+sway, 21+b, T[1])
    dot(a, 14+f%2, 19+b, M[0])
    return finish(a)


def twisted_weave(f):
    a = px.new(32, 32)
    b = (0, -1, 0, 1)[f]
    # Broad hollow tangle; the ends belong to this body, never to a ground web.
    coil(a, 15, 17+b, 10, 7, f)
    coil(a, 20, 20+b, 6, 7, f+2)
    coil(a, 10, 20+b, 5, 5, f+1)
    for x, dx in [(7, -3), (13, -2), (21, 2), (25, 3)]:
        strand(a, [(x, 22+b), (x+dx, 26+(f+x)%2), (x+dx+1, 29)], T[1])
    strand(a, [(7, 14+b), (5, 9+b), (8+f%2, 7+b), (11, 10+b)], T[1])
    strand(a, [(21, 13+b), (24, 9+b), (27, 11+b)], T[2])
    poly(a, [(22, 18+b), (27, 16+b), (26, 20+b), (23, 22+b)], M[2])
    line(a, 23, 18+b, 26, 17+b, M[0])
    dot(a, 12+f, 17+b, T[0])
    return finish(a)


def ruin_shard(f):
    a = px.new(32, 32)
    b = (0, 0, -1, 0)[f]
    # Low broad weight-bearing broken plates, no vessel, altar or human armour.
    poly(a, [(5, 20+b), (9, 10+b), (15, 13+b), (19, 7+b), (24, 14+b), (27, 23+b), (22, 27), (9, 27)], M[3])
    poly(a, [(6, 19+b), (9, 11+b), (13, 14+b), (11, 23+b)], M[1])
    poly(a, [(13, 15+b), (19, 8+b), (22, 16+b), (18, 24+b)], M[2])
    poly(a, [(20, 19+b), (26, 16+b), (28, 23+b), (23, 26)], R[1])
    line(a, 9, 12+b, 11, 14+b, M[0])
    line(a, 15, 13+b, 19, 9+b, M[0])
    strand(a, [(8, 24), (7+(f%2), 28), (11, 29)], M[2])
    strand(a, [(21, 25), (24-(f%2), 28), (27, 29)], M[1])
    strand(a, [(13, 19+b), (17, 21+b), (15, 26)], T[1])
    line(a, 21, 22+b, 24, 21+b, K)
    dot(a, 14+f, 22+b, R[0])
    return finish(a)


def weaver(f, clip):
    a = px.new(64, 64)
    if clip == 'death':
        # Dissociate into inert scrap within this short clip; no permanent field.
        for i, (x, y, w) in enumerate([(15, 36, 8), (32, 30, 10), (47, 40, 8), (25, 48, 7)]):
            yy = min(58, y + f * (4 + i))
            xx = x + (f if i % 2 else -f)
            poly(a, [(xx-w, yy+3), (xx-2, yy-5+f), (xx+w, yy+1), (xx+4, yy+6), (xx-w+1, yy+6)], M[2 if i%2 else 1])
            line(a, xx-w+1, yy+2, xx-2, yy-4+f, M[0])
            if f < 3:
                strand(a, [(xx-4, yy+1), (xx, yy-2), (xx+4-f, yy+3)], T[1])
        return finish(a)
    bob = (0, -1, 0, 1)[f] if clip == 'move' else 0
    tight = f if clip == 'windup' else 0
    y = 30 + bob + tight
    # Open loops of black thread are the body, with separate jagged metal plates.
    coil(a, 30, y+5, 17-tight*2, 18-tight, f)
    coil(a, 34, y+6, 12-tight, 13-tight, f+3)
    coil(a, 24, y+7, 9, 10, f+1)
    poly(a, [(15, y-7), (22, y-15), (27, y-6), (22, y+1), (14, y+2)], M[2])
    line(a, 16, y-7, 22, y-13, M[0])
    poly(a, [(30, y-14), (37, y-20), (41, y-10), (36, y-3)], M[1])
    line(a, 32, y-14, 37, y-18, M[0])
    poly(a, [(39, y-1), (48, y-6), (51, y+2), (45, y+11), (39, y+9)], R[2])
    line(a, 41, y-1, 47, y-4, R[0])
    poly(a, [(19, y+14), (26, y+10), (29, y+23), (20, y+24), (15, y+20)], M[2])
    poly(a, [(34, y+17), (42, y+12), (46, y+22), (37, y+25)], M[1])
    line(a, 35, y+18, 41, y+14, M[0])
    # Asymmetric forward loose-metal pincer; windup folds it, attack opens it.
    reach = (0, 2)[f] if clip == 'attack' else -tight
    strand(a, [(43, y+5), (50+reach, y+8), (55+reach, y+3), (53+reach, y-1)], M[1])
    strand(a, [(44, y+9), (52+reach, y+13), (57+reach, y+8)], M[2])
    line(a, 53+reach, y+4, 55+reach, y+3, M[0])
    # Fixed ground supports; no humanoid feet, robe, face or loom structure.
    sway = (-1, 0, 1, 0)[f%4]
    for x, dx in [(17,-5),(27,-2),(38,2),(46,5)]:
        strand(a, [(x, y+17), (x+dx+sway, 57), (x+dx+2, 61)], T[1])
        line(a, x+dx, 60, x+dx+3, 61, M[2])
    if clip == 'windup':
        for i in range(f+1):
            line(a, 31+i*3, y-1, 33+i*3, y-4, M[0])
    if clip == 'attack':
        line(a, 39, y+3+f, 45, y+4+f, M[0])
    return finish(a)


def projectile(f):
    a = px.new(16, 16)
    points = ([(3,7),(7,4),(12,6),(10,9),(5,11)], [(5,4),(10,5),(12,9),(8,12),(5,9)])[f]
    poly(a, points, M[2])
    strand(a, ([(4,7),(7,5),(11,6)], [(6,5),(9,6),(11,9)])[f], M[0])
    line(a, 7, 8, 9, 9-f, R[1])
    return px.outline(a)


GENERATORS = {
    'enm_ex16-greedy-hook_move_32': greedy_hook,
    'enm_ex17-twisted-weave_move_32': twisted_weave,
    'enm_ex18-ruin-shard_move_32': ruin_shard,
    'prj_defilement-shard_spin_16': projectile,
}
for clip in ('move', 'windup', 'attack', 'death'):
    GENERATORS[f'bos_defilement-weaver_{clip}_64'] = lambda f, c=clip: weaver(f, c)
