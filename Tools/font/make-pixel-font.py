# Draws the kit's pixel font in code and writes it as a TrueType file.
#     python Tools/font/make-pixel-font.py
# Writes Assets/UI/Fonts/KitPixel.ttf. 5x7 capitals, lowercase with ascenders and
# 2-pixel descenders, digits and ASCII signs; proportional (each glyph its own width + 1).
# One font pixel = 128 units, 1024 units per em: at font size 8 one font pixel is one
# UI pixel (UITheme.pixelFontSize = 8). No libraries: the TrueType tables are packed here.
# Drawn by Claude for the kit (CLAUDE.md: kit art is drawn in code); free to use with the kit.
import os, struct, time

PX = 128          # units per font pixel
EM = 1024         # units per em (8 pixels)
ADV = 6 * PX      # advance: 5 pixels + 1 spacing
ASC, DESC = 8 * PX, 2 * PX

# Rows top to bottom: 7 rows from the cap line to the baseline, then up to 2 below.
G = {
' ': [],
'!': ['..#..', '..#..', '..#..', '..#..', '..#..', '.....', '..#..'],
'"': ['.#.#.', '.#.#.', '.....', '.....', '.....', '.....', '.....'],
'#': ['.#.#.', '.#.#.', '#####', '.#.#.', '#####', '.#.#.', '.#.#.'],
'$': ['..#..', '.####', '#.#..', '.###.', '..#.#', '####.', '..#..'],
'%': ['##...', '##..#', '...#.', '..#..', '.#...', '#..##', '...##'],
'&': ['.##..', '#..#.', '#.#..', '.#...', '#.#.#', '#..#.', '.##.#'],
"'": ['..#..', '..#..', '.....', '.....', '.....', '.....', '.....'],
'(': ['...#.', '..#..', '.#...', '.#...', '.#...', '..#..', '...#.'],
')': ['.#...', '..#..', '...#.', '...#.', '...#.', '..#..', '.#...'],
'*': ['.....', '..#..', '#.#.#', '.###.', '#.#.#', '..#..', '.....'],
'+': ['.....', '..#..', '..#..', '#####', '..#..', '..#..', '.....'],
',': ['.....', '.....', '.....', '.....', '.....', '..#..', '..#..', '.#...'],
'-': ['.....', '.....', '.....', '#####', '.....', '.....', '.....'],
'.': ['.....', '.....', '.....', '.....', '.....', '.....', '..#..'],
'/': ['.....', '....#', '...#.', '..#..', '.#...', '#....', '.....'],
'0': ['.###.', '#...#', '#..##', '#.#.#', '##..#', '#...#', '.###.'],
'1': ['..#..', '.##..', '..#..', '..#..', '..#..', '..#..', '.###.'],
'2': ['.###.', '#...#', '....#', '...#.', '..#..', '.#...', '#####'],
'3': ['#####', '...#.', '..#..', '...#.', '....#', '#...#', '.###.'],
'4': ['...#.', '..##.', '.#.#.', '#..#.', '#####', '...#.', '...#.'],
'5': ['#####', '#....', '####.', '....#', '....#', '#...#', '.###.'],
'6': ['..##.', '.#...', '#....', '####.', '#...#', '#...#', '.###.'],
'7': ['#####', '....#', '...#.', '..#..', '.#...', '.#...', '.#...'],
'8': ['.###.', '#...#', '#...#', '.###.', '#...#', '#...#', '.###.'],
'9': ['.###.', '#...#', '#...#', '.####', '....#', '...#.', '.##..'],
':': ['.....', '.....', '..#..', '.....', '.....', '..#..', '.....'],
';': ['.....', '.....', '..#..', '.....', '.....', '..#..', '..#..', '.#...'],
'<': ['...#.', '..#..', '.#...', '#....', '.#...', '..#..', '...#.'],
'=': ['.....', '.....', '#####', '.....', '#####', '.....', '.....'],
'>': ['.#...', '..#..', '...#.', '....#', '...#.', '..#..', '.#...'],
'?': ['.###.', '#...#', '....#', '...#.', '..#..', '.....', '..#..'],
'@': ['.###.', '#...#', '#.###', '#.#.#', '#.###', '#....', '.###.'],
'A': ['.###.', '#...#', '#...#', '#####', '#...#', '#...#', '#...#'],
'B': ['####.', '#...#', '#...#', '####.', '#...#', '#...#', '####.'],
'C': ['.###.', '#...#', '#....', '#....', '#....', '#...#', '.###.'],
'D': ['###..', '#..#.', '#...#', '#...#', '#...#', '#..#.', '###..'],
'E': ['#####', '#....', '#....', '####.', '#....', '#....', '#####'],
'F': ['#####', '#....', '#....', '####.', '#....', '#....', '#....'],
'G': ['.###.', '#...#', '#....', '#.###', '#...#', '#...#', '.####'],
'H': ['#...#', '#...#', '#...#', '#####', '#...#', '#...#', '#...#'],
'I': ['.###.', '..#..', '..#..', '..#..', '..#..', '..#..', '.###.'],
'J': ['..###', '...#.', '...#.', '...#.', '...#.', '#..#.', '.##..'],
'K': ['#...#', '#..#.', '#.#..', '##...', '#.#..', '#..#.', '#...#'],
'L': ['#....', '#....', '#....', '#....', '#....', '#....', '#####'],
'M': ['#...#', '##.##', '#.#.#', '#.#.#', '#...#', '#...#', '#...#'],
'N': ['#...#', '#...#', '##..#', '#.#.#', '#..##', '#...#', '#...#'],
'O': ['.###.', '#...#', '#...#', '#...#', '#...#', '#...#', '.###.'],
'P': ['####.', '#...#', '#...#', '####.', '#....', '#....', '#....'],
'Q': ['.###.', '#...#', '#...#', '#...#', '#.#.#', '#..#.', '.##.#'],
'R': ['####.', '#...#', '#...#', '####.', '#.#..', '#..#.', '#...#'],
'S': ['.####', '#....', '#....', '.###.', '....#', '....#', '####.'],
'T': ['#####', '..#..', '..#..', '..#..', '..#..', '..#..', '..#..'],
'U': ['#...#', '#...#', '#...#', '#...#', '#...#', '#...#', '.###.'],
'V': ['#...#', '#...#', '#...#', '#...#', '#...#', '.#.#.', '..#..'],
'W': ['#...#', '#...#', '#...#', '#.#.#', '#.#.#', '#.#.#', '.#.#.'],
'X': ['#...#', '#...#', '.#.#.', '..#..', '.#.#.', '#...#', '#...#'],
'Y': ['#...#', '#...#', '.#.#.', '..#..', '..#..', '..#..', '..#..'],
'Z': ['#####', '....#', '...#.', '..#..', '.#...', '#....', '#####'],
'[': ['.###.', '.#...', '.#...', '.#...', '.#...', '.#...', '.###.'],
'\\': ['.....', '#....', '.#...', '..#..', '...#.', '....#', '.....'],
']': ['.###.', '...#.', '...#.', '...#.', '...#.', '...#.', '.###.'],
'^': ['..#..', '.#.#.', '#...#', '.....', '.....', '.....', '.....'],
'_': ['.....', '.....', '.....', '.....', '.....', '.....', '#####'],
'`': ['.#...', '..#..', '.....', '.....', '.....', '.....', '.....'],
'a': ['.....', '.....', '.###.', '....#', '.####', '#...#', '.####'],
'b': ['#....', '#....', '####.', '#...#', '#...#', '#...#', '####.'],
'c': ['.....', '.....', '.###.', '#....', '#....', '#...#', '.###.'],
'd': ['....#', '....#', '.####', '#...#', '#...#', '#...#', '.####'],
'e': ['.....', '.....', '.###.', '#...#', '#####', '#....', '.###.'],
'f': ['..##.', '.#..#', '.#...', '###..', '.#...', '.#...', '.#...'],
'g': ['.....', '.....', '.####', '#...#', '#...#', '.####', '....#', '.###.'],
'h': ['#....', '#....', '#.##.', '##..#', '#...#', '#...#', '#...#'],
'i': ['..#..', '.....', '.##..', '..#..', '..#..', '..#..', '.###.'],
'j': ['...#.', '.....', '..##.', '...#.', '...#.', '...#.', '#..#.', '.##..'],
'k': ['#....', '#....', '#..#.', '#.#..', '##...', '#.#..', '#..#.'],
'l': ['.##..', '..#..', '..#..', '..#..', '..#..', '..#..', '.###.'],
'm': ['.....', '.....', '##.#.', '#.#.#', '#.#.#', '#...#', '#...#'],
'n': ['.....', '.....', '#.##.', '##..#', '#...#', '#...#', '#...#'],
'o': ['.....', '.....', '.###.', '#...#', '#...#', '#...#', '.###.'],
'p': ['.....', '.....', '####.', '#...#', '#...#', '####.', '#....', '#....'],
'q': ['.....', '.....', '.####', '#...#', '#...#', '.####', '....#', '....#'],
'r': ['.....', '.....', '#.##.', '##..#', '#....', '#....', '#....'],
's': ['.....', '.....', '.###.', '#....', '.###.', '....#', '####.'],
't': ['.#...', '.#...', '###..', '.#...', '.#...', '.#..#', '..##.'],
'u': ['.....', '.....', '#...#', '#...#', '#...#', '#..##', '.##.#'],
'v': ['.....', '.....', '#...#', '#...#', '#...#', '.#.#.', '..#..'],
'w': ['.....', '.....', '#...#', '#...#', '#.#.#', '#.#.#', '.#.#.'],
'x': ['.....', '.....', '#...#', '.#.#.', '..#..', '.#.#.', '#...#'],
'y': ['.....', '.....', '#...#', '#...#', '#...#', '.####', '....#', '.###.'],
'z': ['.....', '.....', '#####', '...#.', '..#..', '.#...', '#####'],
'{': ['...#.', '..#..', '..#..', '.#...', '..#..', '..#..', '...#.'],
'|': ['..#..', '..#..', '..#..', '..#..', '..#..', '..#..', '..#..'],
'}': ['.#...', '..#..', '..#..', '...#.', '..#..', '..#..', '.#...'],
'~': ['.....', '.....', '.#...', '#.#.#', '...#.', '.....', '.....'],
}

def cols(rows):
    # First and last column with ink (None for an empty glyph).
    used = [x for line in rows for x, ch in enumerate(line) if ch == '#']
    return (min(used), max(used)) if used else None

def advance(rows):
    # Proportional: the glyph's own width + 1 pixel of spacing; a space is 3 + 1.
    c = cols(rows)
    return ((c[1] - c[0] + 1) if c else 3) * PX + PX

def rects(rows):
    # Horizontal runs of each row as rectangles (x0, y0, x1, y1) in font units,
    # shifted so the first inked column sits at x = 0.
    out = []
    c = cols(rows)
    shift = c[0] if c else 0
    for r, line in enumerate(rows):
        top = (7 - r) * PX
        x = 0
        while x < len(line):
            if line[x] == '#':
                s = x
                while x < len(line) and line[x] == '#': x += 1
                out.append(((s - shift) * PX, top - PX, (x - shift) * PX, top))
            else:
                x += 1
    return out

def glyf_data(rs):
    if not rs:
        return b''
    ends, xs, ys = [], [], []
    for (x0, y0, x1, y1) in rs:
        # Clockwise: TrueType outer contours.
        pts = [(x0, y0), (x0, y1), (x1, y1), (x1, y0)]
        for p in pts:
            xs.append(p[0]); ys.append(p[1])
        ends.append(len(xs) - 1)
    xmin, ymin, xmax, ymax = min(xs), min(ys), max(xs), max(ys)
    d = struct.pack('>hhhhh', len(ends), xmin, ymin, xmax, ymax)
    d += b''.join(struct.pack('>H', e) for e in ends)
    d += struct.pack('>H', 0)                      # no instructions
    d += bytes([0x01] * len(xs))                   # on-curve, both deltas as shorts
    px = py = 0
    for x in xs: d += struct.pack('>h', x - px); px = x
    for y in ys: d += struct.pack('>h', y - py); py = y
    if len(d) % 2: d += b'\0'
    return d

chars = sorted(G.keys(), key=ord)
glyphs = [b''] + [glyf_data(rects(G[c])) for c in chars]   # glyph 0 = .notdef (empty)
names = ['.notdef'] + chars

glyf = b''; loca = []
for g in glyphs:
    loca.append(len(glyf)); glyf += g
loca.append(len(glyf))
loca_t = b''.join(struct.pack('>I', o) for o in loca)

n = len(glyphs)
advs = [ADV] + [advance(G[c]) for c in chars]
hmtx = b''.join(struct.pack('>Hh', a, 0) for a in advs)

# cmap format 4: one segment per character, plus the end segment.
segs = [(ord(c), ord(c), i + 1) for i, c in enumerate(chars)]
segs.append((0xFFFF, 0xFFFF, 0))
segX2 = len(segs) * 2
sr = 2 ** (len(segs).bit_length() - 1) * 2
ends = b''.join(struct.pack('>H', e) for s, e, g in segs)
starts = b''.join(struct.pack('>H', s) for s, e, g in segs)
deltas = b''.join(struct.pack('>h', ((g - s) + 0x10000) % 0x10000 - (0x10000 if ((g - s) % 0x10000) >= 0x8000 else 0)) for s, e, g in segs)
offs = b''.join(struct.pack('>H', 0) for _ in segs)
sub = struct.pack('>HHHHHH', 4, 0, 0, segX2, sr, (sr // 2).bit_length() - 1) + struct.pack('>H', segX2 - sr)
sub += ends + b'\0\0' + starts + deltas + offs
sub = sub[:2] + struct.pack('>H', len(sub)) + sub[4:]
cmap = struct.pack('>HH', 0, 2) + struct.pack('>HHI', 0, 3, 20) + struct.pack('>HHI', 3, 1, 20) + sub

assert struct.calcsize('>IhhhHhhhhhhhhhhhH') == 36
head = struct.pack('>IIIIHHqqhhhhHHhhh', 0x00010000, 0x00010000, 0, 0x5F0F3CF5, 0x000B, EM,
                   0, 0, 0, -DESC, 5 * PX, 7 * PX, 0, 8, 2, 1, 0)
hhea = struct.pack('>IhhhHhhhhhhhhhhhH', 0x00010000, ASC, -DESC, 0, ADV, 0, 0, 5 * PX, 1, 0, 0, 0, 0, 0, 0, 0, n)
maxPts = max([len(rects(G[c])) * 4 for c in chars] + [0])
maxCont = max([len(rects(G[c])) for c in chars] + [0])
maxp = struct.pack('>IHHHHHHHHHHHHHH', 0x00010000, n, maxPts, maxCont, 0, 0, 2, 0, 0, 0, 0, 0, 0, 0, 0)
os2 = struct.pack('>HhHHHhhhhhhhhhhh', 3, ADV, 400, 5, 0, 5 * PX, 7 * PX, 0, 0, 5 * PX, 7 * PX, 0, 3 * PX, 64, 3 * PX, 0)
os2 += bytes(10)                                     # panose
os2 += struct.pack('>IIII', 1, 0, 0, 0)              # unicode ranges: basic latin
os2 += b'KITP'
os2 += struct.pack('>HHH', 0x0040, 32, 126)          # regular, first / last char
os2 += struct.pack('>hhhHH', ASC, -DESC, 0, ASC, DESC)
os2 += struct.pack('>II', 1, 0)                      # code pages: latin 1
os2 += struct.pack('>hhHHH', 5 * PX, 7 * PX, 0, 32, 1)

def name_table():
    recs = [(1, 'Kit Pixel'), (2, 'Regular'), (3, 'Kit Pixel Regular'), (4, 'Kit Pixel Regular'), (5, 'Version 1.0'), (6, 'KitPixel-Regular')]
    data = b''; rows = b''
    for nid, text in recs:
        b = text.encode('utf-16-be')
        rows += struct.pack('>HHHHHH', 3, 1, 0x409, nid, len(b), len(data))
        data += b
    return struct.pack('>HHH', 0, len(recs), 6 + 12 * len(recs)) + rows + data

post = struct.pack('>IIhhIIIII', 0x00030000, 0, -PX, PX // 2, 1, 0, 0, 0, 0)

tables = {'OS/2': os2, 'cmap': cmap, 'glyf': glyf, 'head': head, 'hhea': hhea, 'hmtx': hmtx,
          'loca': loca_t, 'maxp': maxp, 'name': name_table(), 'post': post}

def checksum(b):
    b = b + b'\0' * ((4 - len(b) % 4) % 4)
    return sum(struct.unpack('>%dI' % (len(b) // 4), b)) & 0xFFFFFFFF

tags = sorted(tables)
nt = len(tags)
es = nt.bit_length() - 1
font = struct.pack('>IHHHH', 0x00010000, nt, (2 ** es) * 16, es, nt * 16 - (2 ** es) * 16)
offset = 12 + 16 * nt
dir_ = b''; body = b''
for t in tags:
    d = tables[t]
    dir_ += struct.pack('>4sIII', t.encode('ascii'), checksum(d), offset + len(body), len(d))
    body += d + b'\0' * ((4 - len(d) % 4) % 4)
font += dir_ + body
# head.checkSumAdjustment
adj = (0xB1B0AFBA - checksum(font)) & 0xFFFFFFFF
hpos = font.index(b'head')
hoff = struct.unpack('>I', font[hpos + 8:hpos + 12])[0]
font = font[:hoff + 8] + struct.pack('>I', adj) + font[hoff + 12:]

here = os.path.dirname(os.path.abspath(__file__))
out = os.path.normpath(os.path.join(here, '..', '..', 'Assets', 'UI', 'Fonts', 'KitPixel.ttf'))
os.makedirs(os.path.dirname(out), exist_ok=True)
tmp = out + '.tmp'
with open(tmp, 'wb') as f: f.write(font)
os.replace(tmp, out)
print('wrote', out, len(font), 'bytes,', n, 'glyphs')
