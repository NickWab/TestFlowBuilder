"""Generates the Test Flow Builder logo assets (no third-party packages).

    python tools/make_icon.py

Writes into src/ATECore.TestFlowBuilder/Assets/Icons/:
  app.ico   multi-size icon (exe, taskbar, window)
  logo.png  512px mark
  logo.svg  vector source of the mark

The mark: a red tile holding three stacked cells that step down and to the right -- a test flow -- with the
last step in ink. Colours are the Modernist tokens (accent #ec3013, ink #201e1d).
"""
import os
import struct
import zlib

ACCENT = (0xEC, 0x30, 0x13)
INK = (0x20, 0x1E, 0x1D)
WHITE = (0xFF, 0xFF, 0xFF)

# Everything is drawn on a 256-unit grid: (x0, y0, x1, y1, colour). Tile first, then the three steps.
SHAPES = [
    (0, 0, 256, 256, ACCENT),
    (36, 32, 148, 80, WHITE),
    (72, 104, 184, 152, WHITE),
    (108, 176, 220, 224, INK),
]
ICO_SIZES = [16, 20, 24, 32, 40, 48, 64, 128, 256]

OUT = os.path.join(os.path.dirname(__file__), "..", "src", "ATECore.TestFlowBuilder", "Assets", "Icons")


def overlap(a0, a1, b0, b1):
    return max(0.0, min(a1, b1) - max(a0, b0))


def render(size):
    """RGBA bytes, exact analytic coverage per pixel (so edges are anti-aliased at any size)."""
    scale = 256 / size
    rows = []
    for py in range(size):
        row = bytearray()
        for px in range(size):
            x0, x1, y0, y1 = px * scale, (px + 1) * scale, py * scale, (py + 1) * scale
            r = g = b = 0.0
            a = 0.0  # premultiplied accumulation, painter's order
            for sx0, sy0, sx1, sy1, col in SHAPES:
                cov = overlap(x0, x1, sx0, sx1) * overlap(y0, y1, sy0, sy1) / (scale * scale)
                if cov <= 0:
                    continue
                r = col[0] * cov + r * (1 - cov)
                g = col[1] * cov + g * (1 - cov)
                b = col[2] * cov + b * (1 - cov)
                a = cov + a * (1 - cov)
            if a > 0:
                row += bytes((round(r / a), round(g / a), round(b / a), round(a * 255)))
            else:
                row += b"\0\0\0\0"
        rows.append(bytes(row))
    return rows


def png(size):
    rows = render(size)
    raw = b"".join(b"\0" + r for r in rows)

    def chunk(kind, data):
        body = kind + data
        return struct.pack(">I", len(data)) + body + struct.pack(">I", zlib.crc32(body) & 0xFFFFFFFF)

    return (b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", size, size, 8, 6, 0, 0, 0))
            + chunk(b"IDAT", zlib.compress(raw, 9)) + chunk(b"IEND", b""))


def ico(sizes):
    images = [(s, png(s)) for s in sizes]
    head = struct.pack("<HHH", 0, 1, len(images))
    offset = 6 + 16 * len(images)
    entries, blobs = b"", b""
    for s, data in images:
        entries += struct.pack("<BBBBHHII", s % 256, s % 256, 0, 0, 1, 32, len(data), offset + len(blobs))
        blobs += data
    return head + entries + blobs


def svg():
    hexes = lambda c: "#%02x%02x%02x" % c
    rects = "\n".join(
        f'  <rect x="{x0}" y="{y0}" width="{x1 - x0}" height="{y1 - y0}" fill="{hexes(c)}"/>'
        for x0, y0, x1, y1, c in SHAPES)
    return f'<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 256 256" width="256" height="256">\n{rects}\n</svg>\n'


if __name__ == "__main__":
    os.makedirs(OUT, exist_ok=True)
    with open(os.path.join(OUT, "app.ico"), "wb") as f:
        f.write(ico(ICO_SIZES))
    with open(os.path.join(OUT, "logo.png"), "wb") as f:
        f.write(png(512))
    with open(os.path.join(OUT, "logo.svg"), "w", encoding="utf-8") as f:
        f.write(svg())
    print("wrote", os.path.normpath(OUT))
