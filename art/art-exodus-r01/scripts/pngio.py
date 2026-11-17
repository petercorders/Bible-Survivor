"""Minimal standard-library PNG I/O for 8-bit RGBA, non-interlaced images.
write_rgba() always uses filter 0 and zlib level 9, so the same pixels give the same bytes with the same zlib.
read_rgba() accepts any filter type (0-4) so files saved by other tools can be checked too."""
import struct, zlib

SIG = b"\x89PNG\r\n\x1a\n"


def _chunk(tag, data):
    return struct.pack(">I", len(data)) + tag + data + struct.pack(">I", zlib.crc32(tag + data) & 0xFFFFFFFF)


def write_rgba(path, width, height, rgba_bytes):
    assert len(rgba_bytes) == width * height * 4, (len(rgba_bytes), width, height)
    raw = b"".join(b"\x00" + rgba_bytes[y * width * 4:(y + 1) * width * 4] for y in range(height))
    ihdr = struct.pack(">IIBBBBB", width, height, 8, 6, 0, 0, 0)
    data = SIG + _chunk(b"IHDR", ihdr) + _chunk(b"IDAT", zlib.compress(raw, 9)) + _chunk(b"IEND", b"")
    with open(path, "wb") as f:
        f.write(data)


def read_rgba(path):
    """Return (width, height, bytes RGBA). Raises ValueError for anything but 8-bit RGBA non-interlaced."""
    with open(path, "rb") as f:
        data = f.read()
    if data[:8] != SIG:
        raise ValueError(f"{path}: not a PNG")
    pos, idat, ihdr = 8, [], None
    while pos < len(data):
        n = struct.unpack(">I", data[pos:pos + 4])[0]
        tag = data[pos + 4:pos + 8]
        body = data[pos + 8:pos + 8 + n]
        pos += 12 + n
        if tag == b"IHDR":
            ihdr = struct.unpack(">IIBBBBB", body)
        elif tag == b"IDAT":
            idat.append(body)
        elif tag == b"IEND":
            break
    if ihdr is None:
        raise ValueError(f"{path}: no IHDR")
    w, h, depth, ctype, _, _, inter = ihdr
    if depth != 8 or ctype != 6 or inter != 0:
        raise ValueError(f"{path}: need 8-bit RGBA non-interlaced, got depth={depth} colour_type={ctype} interlace={inter}")
    raw = zlib.decompress(b"".join(idat))
    stride, bpp = w * 4, 4
    out = bytearray(h * stride)
    prev = bytearray(stride)
    p = 0
    for y in range(h):
        ft = raw[p]; p += 1
        line = bytearray(raw[p:p + stride]); p += stride
        for i in range(stride):
            a = line[i - bpp] if i >= bpp else 0
            b = prev[i]
            c = prev[i - bpp] if i >= bpp else 0
            if ft == 1:
                line[i] = (line[i] + a) & 255
            elif ft == 2:
                line[i] = (line[i] + b) & 255
            elif ft == 3:
                line[i] = (line[i] + ((a + b) >> 1)) & 255
            elif ft == 4:
                pa, pb, pc = abs(b - c), abs(a - c), abs(a + b - 2 * c)
                pr = a if (pa <= pb and pa <= pc) else (b if pb <= pc else c)
                line[i] = (line[i] + pr) & 255
            elif ft != 0:
                raise ValueError(f"{path}: bad filter {ft}")
        out[y * stride:(y + 1) * stride] = line
        prev = line
    return w, h, bytes(out)
