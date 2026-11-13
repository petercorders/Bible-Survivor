"""Measurements used as review evidence (not a quality score).
hair_skin_boundary(): CIE76 colour difference across every 4-neighbour boundary between a visible hair pixel
and a visible skin pixel, for one skin x hair combination and one composited frame."""
import numpy as np


def _lab(rgb):
    c = np.asarray(rgb, np.float64) / 255.0
    c = np.where(c > 0.04045, ((c + 0.055) / 1.055) ** 2.4, c / 12.92)
    M = np.array([[0.4124, 0.3576, 0.1805], [0.2126, 0.7152, 0.0722], [0.0193, 0.1192, 0.9505]])
    xyz = c @ M.T / np.array([0.95047, 1.0, 1.08883])
    f = np.where(xyz > 0.008856, np.cbrt(xyz), 7.787 * xyz + 16 / 116)
    return np.stack([116 * f[..., 1] - 16, 500 * (f[..., 0] - f[..., 1]), 200 * (f[..., 1] - f[..., 2])], -1)


def hair_skin_boundary(img, hair_mask, skin_mask, weak=12.0):
    """img: composited RGBA; hair_mask/skin_mask: visible pixels of each class. Returns dict."""
    lab = _lab(img[..., :3])
    d = []
    h, w = hair_mask.shape
    for y in range(h):
        for x in range(w):
            if not hair_mask[y, x]:
                continue
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                xx, yy = x + dx, y + dy
                if 0 <= xx < w and 0 <= yy < h and skin_mask[yy, xx]:
                    d.append(float(np.linalg.norm(lab[y, x] - lab[yy, xx])))
    d = np.array(d) if d else np.array([np.nan])
    return {"pairs": int(np.isfinite(d).sum()), "min_dE": round(float(np.nanmin(d)), 1),
            "median_dE": round(float(np.nanmedian(d)), 1), "weak_share": round(float(np.mean(d < weak)), 3)}
