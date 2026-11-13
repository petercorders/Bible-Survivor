"""Minimal TrueType (SFNT) reader, standard library only (art-unity-font-01).

Reads exactly what the font checker needs and nothing more: the table directory, head (unitsPerEm, indexToLocFormat),
maxp (numGlyphs), hhea/hmtx (advance widths), cmap (formats 4 and 12, Unicode subtables), loca/glyf (where each glyph's
outline data starts and ends, contour count, simple-glyph point coordinates, composite component offsets) and name.
Every read is bounds-checked; a malformed file raises SfntError (never IndexError / struct.error).
"""
import struct


class SfntError(ValueError):
    pass


class Font:
    def __init__(self, data):
        if len(data) < 12:
            raise SfntError("file shorter than an SFNT header")
        self.data = data
        self.version = data[:4]
        n = self._u16(4)
        self.tables = {}
        for i in range(n):
            o = 12 + 16 * i
            if o + 16 > len(data):
                raise SfntError("table directory runs past the end of the file")
            tag = data[o:o + 4].decode("latin-1")
            off, length = self._u32(o + 8), self._u32(o + 12)
            if off + length > len(data):
                raise SfntError(f"table {tag} runs past the end of the file")
            self.tables[tag] = (off, length)
        self._loca = None
        self._cmap = None

    # ---- raw reads
    def _chk(self, o, n):
        if o < 0 or o + n > len(self.data):
            raise SfntError(f"read of {n} bytes at {o} is outside the file")

    def _u16(self, o):
        self._chk(o, 2)
        return struct.unpack_from(">H", self.data, o)[0]

    def _i16(self, o):
        self._chk(o, 2)
        return struct.unpack_from(">h", self.data, o)[0]

    def _u32(self, o):
        self._chk(o, 4)
        return struct.unpack_from(">I", self.data, o)[0]

    def table(self, tag):
        if tag not in self.tables:
            raise SfntError(f"table {tag} missing")
        return self.tables[tag]

    # ---- head / maxp / hhea
    @property
    def units_per_em(self):
        return self._u16(self.table("head")[0] + 18)

    @property
    def index_to_loc_format(self):
        return self._i16(self.table("head")[0] + 50)

    @property
    def num_glyphs(self):
        return self._u16(self.table("maxp")[0] + 4)

    def advance(self, gid):
        n_hm = self._u16(self.table("hhea")[0] + 34)
        off = self.table("hmtx")[0]
        return self._u16(off + 4 * min(gid, n_hm - 1))

    # ---- loca / glyf
    def loca(self):
        if self._loca is None:
            off, length = self.table("loca")
            n = self.num_glyphs + 1
            if self.index_to_loc_format == 0:
                if length < 2 * n:
                    raise SfntError("loca shorter than numGlyphs + 1 entries")
                self._loca = [2 * self._u16(off + 2 * i) for i in range(n)]
            elif self.index_to_loc_format == 1:
                if length < 4 * n:
                    raise SfntError("loca shorter than numGlyphs + 1 entries")
                self._loca = [self._u32(off + 4 * i) for i in range(n)]
            else:
                raise SfntError(f"unknown indexToLocFormat {self.index_to_loc_format}")
        return self._loca

    def glyph_range(self, gid):
        lo = self.loca()
        if not 0 <= gid < self.num_glyphs:
            raise SfntError(f"glyph id {gid} outside 0..{self.num_glyphs - 1}")
        a, b = lo[gid], lo[gid + 1]
        glen = self.table("glyf")[1]
        if a > b or b > glen:
            raise SfntError(f"glyph {gid}: loca range {a}..{b} is not inside glyf ({glen} bytes)")
        return a, b

    def glyph_contours(self, gid):
        """numberOfContours (>0 simple, <0 composite); 0 when the glyph has no outline data at all"""
        a, b = self.glyph_range(gid)
        if b - a == 0:
            return 0
        if b - a < 10:
            raise SfntError(f"glyph {gid}: {b - a} bytes is shorter than a glyph header")
        return self._i16(self.table("glyf")[0] + a)

    def glyph_coords(self, gid):
        """simple glyph: list of (x, y) point coordinates in font units; composite: list of component (dx, dy)
        offsets that are x/y values. Empty list for an empty glyph."""
        a, b = self.glyph_range(gid)
        if b == a:
            return []
        g = self.table("glyf")[0] + a
        nc = self._i16(g)
        if nc >= 0:
            ends = [self._u16(g + 10 + 2 * i) for i in range(nc)]
            npts = ends[-1] + 1 if ends else 0
            p = g + 10 + 2 * nc
            p += 2 + self._u16(p)                                   # skip instructions
            flags = []
            while len(flags) < npts:
                self._chk(p, 1)
                f = self.data[p]; p += 1
                flags.append(f)
                if f & 8:
                    self._chk(p, 1)
                    flags.extend([f] * self.data[p]); p += 1
            flags = flags[:npts]
            xs, x = [], 0
            for f in flags:
                if f & 2:
                    self._chk(p, 1); d = self.data[p]; p += 1
                    x += d if f & 16 else -d
                elif not f & 16:
                    x += self._i16(p); p += 2
                xs.append(x)
            ys, y = [], 0
            for f in flags:
                if f & 4:
                    self._chk(p, 1); d = self.data[p]; p += 1
                    y += d if f & 32 else -d
                elif not f & 32:
                    y += self._i16(p); p += 2
                ys.append(y)
            if p > self.table("glyf")[0] + b:
                raise SfntError(f"glyph {gid}: point data runs past its loca range")
            return list(zip(xs, ys))
        out, p = [], g + 10
        while True:
            flags, _ = self._u16(p), self._u16(p + 2); p += 4
            if flags & 1:
                dx, dy = self._i16(p), self._i16(p + 2); p += 4
            else:
                self._chk(p, 2)
                dx, dy = struct.unpack_from(">bb", self.data, p); p += 2
            if flags & 2:                                           # ARGS_ARE_XY_VALUES
                out.append((dx, dy))
            if flags & 8:
                p += 2
            elif flags & 0x40:
                p += 4
            elif flags & 0x80:
                p += 8
            if not flags & 0x20:                                    # MORE_COMPONENTS
                return out

    # ---- cmap
    def cmap(self):
        """Unicode code point -> glyph id, from the best Unicode subtable (format 12 preferred, else format 4)"""
        if self._cmap is not None:
            return self._cmap
        base = self.table("cmap")[0]
        n = self._u16(base + 2)
        subs = []
        for i in range(n):
            pid, eid, off = self._u16(base + 4 + 8 * i), self._u16(base + 6 + 8 * i), self._u32(base + 8 + 8 * i)
            if (pid == 3 and eid in (1, 10)) or pid == 0:
                subs.append((self._u16(base + off), base + off))
        subs.sort(key=lambda s: {12: 0, 4: 1}.get(s[0], 9))
        if not subs or subs[0][0] not in (4, 12):
            raise SfntError("no Unicode cmap subtable of format 4 or 12")
        fmt, o = subs[0]
        m = {}
        if fmt == 12:
            for i in range(self._u32(o + 12)):
                s, e, g = self._u32(o + 16 + 12 * i), self._u32(o + 20 + 12 * i), self._u32(o + 24 + 12 * i)
                if e - s > 0x10FFFF:
                    raise SfntError("cmap format 12 group too large")
                for c in range(s, e + 1):
                    m[c] = g + c - s
        else:
            seg = self._u16(o + 6) // 2
            ends, starts = o + 14, o + 16 + 2 * seg
            deltas, ranges = starts + 2 * seg, starts + 4 * seg
            for i in range(seg):
                e, s = self._u16(ends + 2 * i), self._u16(starts + 2 * i)
                d, r = self._i16(deltas + 2 * i), self._u16(ranges + 2 * i)
                if s > e:
                    raise SfntError("cmap format 4 segment start > end")
                for c in range(s, e + 1):
                    if c == 0xFFFF:
                        continue
                    if r == 0:
                        g = (c + d) & 0xFFFF
                    else:
                        g = self._u16(ranges + 2 * i + r + 2 * (c - s))
                        g = (g + d) & 0xFFFF if g else 0
                    if g:
                        m[c] = g
        self._cmap = m
        return m

    # ---- GPOS 'kern' pair adjustment (lookup type 2, formats 1 and 2; type 9 extension unwrapped)
    def _coverage(self, o):
        fmt = self._u16(o)
        if fmt == 1:
            return {self._u16(o + 4 + 2 * i): i for i in range(self._u16(o + 2))}
        if fmt == 2:
            m = {}
            for i in range(self._u16(o + 2)):
                s, e, k = self._u16(o + 4 + 6 * i), self._u16(o + 6 + 6 * i), self._u16(o + 8 + 6 * i)
                for g in range(s, e + 1):
                    m[g] = k + g - s
            return m
        raise SfntError(f"coverage format {fmt}")

    def _classdef(self, o):
        fmt = self._u16(o)
        if fmt == 1:
            s = self._u16(o + 2)
            return {s + i: self._u16(o + 6 + 2 * i) for i in range(self._u16(o + 4))}
        if fmt == 2:
            m = {}
            for i in range(self._u16(o + 2)):
                s, e, c = self._u16(o + 4 + 6 * i), self._u16(o + 6 + 6 * i), self._u16(o + 8 + 6 * i)
                for g in range(s, e + 1):
                    m[g] = c
            return m
        raise SfntError(f"class definition format {fmt}")

    @staticmethod
    def _xadv(vf, data_off, rd):
        """x advance inside a ValueRecord at data_off with ValueFormat vf (0 when absent)"""
        if not vf & 4:
            return 0
        return rd(data_off + 2 * bin(vf & 3).count("1"))

    def kern_subtables(self):
        """list of parsed PairPos subtables reachable from the GPOS 'kern' feature, in lookup order"""
        if getattr(self, "_kern", None) is not None:
            return self._kern
        self._kern = []
        if "GPOS" not in self.tables:
            return self._kern
        g = self.table("GPOS")[0]
        fl, ll = g + self._u16(g + 6), g + self._u16(g + 8)
        idx = []
        for i in range(self._u16(fl)):
            r = fl + 2 + 6 * i
            if self.data[r:r + 4] == b"kern":
                ft = fl + self._u16(r + 4)
                idx += [self._u16(ft + 4 + 2 * k) for k in range(self._u16(ft + 2))]
        for li in sorted(set(idx)):
            lk = ll + self._u16(ll + 2 + 2 * li)
            typ, subs = self._u16(lk), []
            for k in range(self._u16(lk + 4)):
                st = lk + self._u16(lk + 6 + 2 * k)
                t = typ
                if t == 9:
                    t, st = self._u16(st + 2), st + self._u32(st + 4)
                if t != 2:
                    continue
                fmt, cov = self._u16(st), self._coverage(st + self._u16(st + 2))
                vf1, vf2 = self._u16(st + 4), self._u16(st + 6)
                sz1, sz2 = 2 * bin(vf1).count("1"), 2 * bin(vf2).count("1")
                if fmt == 1:
                    subs.append(("p", st, cov, vf1, vf2, sz1, sz2))
                elif fmt == 2:
                    c1, c2 = self._classdef(st + self._u16(st + 8)), self._classdef(st + self._u16(st + 10))
                    subs.append(("c", st, cov, vf1, vf2, sz1, sz2, c1, c2, self._u16(st + 12), self._u16(st + 14)))
            self._kern.append(subs)
        return self._kern

    def kern(self, left, right):
        """total x-advance change (font units) the 'kern' feature applies to the pair (left, right) glyph ids"""
        total = 0
        for subs in self.kern_subtables():
            for s in subs:
                if left not in s[2]:
                    continue
                st, vf1, vf2, sz1, sz2 = s[1], s[3], s[4], s[5], s[6]
                if s[0] == "p":
                    ps = st + self._u16(st + 10 + 2 * s[2][left])
                    rec = 2 + sz1 + sz2
                    hit = None
                    for i in range(self._u16(ps)):
                        if self._u16(ps + 2 + rec * i) == right:
                            hit = ps + 2 + rec * i + 2
                            break
                    if hit is None:
                        continue
                    total += self._xadv(vf1, hit, self._i16) + self._xadv(vf2, hit + sz1, self._i16)
                else:
                    c1, c2, n1, n2 = s[7].get(left, 0), s[8].get(right, 0), s[9], s[10]
                    if c1 >= n1 or c2 >= n2:
                        continue
                    r = st + 16 + (c1 * n2 + c2) * (sz1 + sz2)
                    total += self._xadv(vf1, r, self._i16) + self._xadv(vf2, r + sz1, self._i16)
                break                                               # first applicable subtable of a lookup wins
        return total

    # ---- name
    def names(self):
        """nameID -> first Windows-Unicode (3,1) or Mac-Roman (1,0) string"""
        base = self.table("name")[0]
        n, sto = self._u16(base + 2), base + self._u16(base + 4)
        out = {}
        for i in range(n):
            r = base + 6 + 12 * i
            pid, eid, nid, ln, of = (self._u16(r), self._u16(r + 2), self._u16(r + 6), self._u16(r + 8), self._u16(r + 10))
            self._chk(sto + of, ln)
            raw = self.data[sto + of:sto + of + ln]
            if pid == 3 and eid == 1:
                out.setdefault(nid, raw.decode("utf-16-be", "replace"))
            elif pid == 1 and eid == 0:
                out.setdefault(nid, raw.decode("latin-1"))
        return out
