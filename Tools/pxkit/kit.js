// Drawing primitives. Written once; a sprite file never redefines any of this.
//
// Everything here can only ever set a pixel to one literal palette colour, so
// anti-aliasing is not available to fail at (ART.md: hard pixels only).
const P = require('./palette');
const png = require('./png');

class Buf {
  constructor(w, h) {
    this.w = w; this.h = h;
    this.px = new Array(w * h).fill(null);   // null = transparent
    this.tg = new Uint8Array(w * h);         // surface tag, for per-surface lighting
  }
  set(x, y, c, t) {
    x = Math.round(x); y = Math.round(y);
    if (x < 0 || y < 0 || x >= this.w || y >= this.h) return;
    const i = y * this.w + x;
    this.px[i] = c;
    if (t !== undefined) this.tg[i] = t;
  }
  get(x, y) {
    x = Math.round(x); y = Math.round(y);
    if (x < 0 || y < 0 || x >= this.w || y >= this.h) return null;
    return this.px[y * this.w + x];
  }
  tag(x, y) {
    x = Math.round(x); y = Math.round(y);
    if (x < 0 || y < 0 || x >= this.w || y >= this.h) return 0;
    return this.tg[y * this.w + x];
  }
  toRGBA() {
    const out = Buffer.alloc(this.w * this.h * 4);
    for (let i = 0; i < this.w * this.h; i++) {
      const c = this.px[i];
      if (!c) continue;
      out[i*4]   = parseInt(c.slice(1, 3), 16);
      out[i*4+1] = parseInt(c.slice(3, 5), 16);
      out[i*4+2] = parseInt(c.slice(5, 7), 16);
      out[i*4+3] = 255;
    }
    return out;
  }
  save(file) { png.write(file, this.w, this.h, this.toRGBA()); }
  /** Bounding box of solid pixels, or null if empty. */
  bounds() {
    let x0 = this.w, y0 = this.h, x1 = -1, y1 = -1;
    for (let y = 0; y < this.h; y++) for (let x = 0; x < this.w; x++) {
      if (this.px[y * this.w + x]) {
        if (x < x0) x0 = x; if (x > x1) x1 = x;
        if (y < y0) y0 = y; if (y > y1) y1 = y;
      }
    }
    return x1 < 0 ? null : { x0, y0, x1, y1 };
  }
}

const rect = (b, x0, y0, x1, y1, c, t) => {
  for (let y = y0; y <= y1; y++) for (let x = x0; x <= x1; x++) b.set(x, y, c, t);
};
const span = (b, x0, x1, y, c, t) => rect(b, x0, y, x1, y, c, t);

/** Integer half-width of an ellipse at vertical offset dy; -1 when outside. */
function ehw(rx, ry, dy) {
  const k = 1 - (dy * dy) / (ry * ry);
  return k < 0 ? -1 : Math.round(rx * Math.sqrt(k));
}
function ellipse(b, cx, cy, rx, ry, c, t) {
  cx = Math.round(cx); cy = Math.round(cy);
  for (let dy = -Math.ceil(ry); dy <= Math.ceil(ry); dy++) {
    const w = ehw(rx, ry, dy);
    if (w >= 0) span(b, cx - w, cx + w, cy + dy, c, t);
  }
}
function roundRect(b, x0, y0, x1, y1, r, c, t) {
  for (let y = y0; y <= y1; y++) {
    const d = Math.min(y - y0, y1 - y);
    const inset = d < r ? r - Math.round(Math.sqrt(Math.max(0, r * r - (r - d) * (r - d)))) : 0;
    span(b, x0 + inset, x1 - inset, y, c, t);
  }
}

// 4x4 ordered dither. Used ONLY for light falloff - never to fake a shape.
const BAYER = [[0,8,2,10],[12,4,14,6],[3,11,1,9],[15,7,13,5]];
const bay = (x, y) => (BAYER[((y % 4) + 4) % 4][((x % 4) + 4) % 4] + 0.5) / 16 - 0.5;

/**
 * Quantise 0..1 against DESCENDING thresholds -> ramp index, or -1 below all.
 * `jitter` is the dither term; pass 0 for form, bay(x,y)*amount for falloff.
 */
function band(v, thresholds, jitter = 0) {
  for (let i = 0; i < thresholds.length; i++) {
    if (v >= thresholds[i] + jitter * thresholds[i]) return thresholds.length - 1 - i;
  }
  return -1;
}

/**
 * Rim light: repaint solid pixels whose neighbour toward the light is empty.
 * For a lit edge toward the upper-left light. `pick(x,y,left,up,colour)` returns
 * a colour or null.
 */
function rimLight(b, pick, skip) {
  const out = [];
  for (let y = 0; y < b.h; y++) for (let x = 0; x < b.w; x++) {
    const c = b.get(x, y);
    if (c === null || (skip && skip(c, x, y))) continue;
    const left = b.get(x - 1, y) === null;
    const up = b.get(x, y - 1) === null;
    if (!left && !up) continue;
    const r = pick(x, y, left, up, c);
    if (r) out.push([x, y, r]);
  }
  for (const [x, y, c] of out) b.set(x, y, c);
}

/** Trace the outermost solid pixels of a region in the outline colour (steel-0). */
function outline(b, isBody) {
  const edits = [];
  for (let y = 0; y < b.h; y++) for (let x = 0; x < b.w; x++) {
    const c = b.get(x, y);
    if (c === null || !isBody(c)) continue;
    const n = [b.get(x+1,y), b.get(x-1,y), b.get(x,y+1), b.get(x,y-1)];
    if (n.some(v => v !== null && !isBody(v))) edits.push([x, y]);
  }
  for (const [x, y] of edits) b.set(x, y, P.OUTLINE);
}

/**
 * Seeded random numbers (mulberry32), for wood grain and wear. A sprite must draw the same pixels
 * every run, so Math.random is never used; a new seed is a new drawing.
 */
function rng(seed) {
  let a = seed >>> 0;
  return () => {
    a = (a + 0x6D2B79F5) >>> 0;
    let t = a;
    t = Math.imul(t ^ (t >>> 15), t | 1);
    t ^= t + Math.imul(t ^ (t >>> 7), t | 61);
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}


/** Fill the polygon `pts` ([[x, y], ...]); `c` is a colour, or `(x, y) => colour` for a shaded fill. */
function poly(b, pts, c) {
  const ys = pts.map(p => p[1]), y0 = Math.floor(Math.min(...ys)), y1 = Math.ceil(Math.max(...ys));
  const xs = pts.map(p => p[0]), x0 = Math.floor(Math.min(...xs)), x1 = Math.ceil(Math.max(...xs));
  for (let y = y0; y <= y1; y++) for (let x = x0; x <= x1; x++) {
    let inside = false;
    for (let i = 0, j = pts.length - 1; i < pts.length; j = i++) {
      const [xi, yi] = pts[i], [xj, yj] = pts[j];
      if ((yi > y + 0.5) !== (yj > y + 0.5) && x + 0.5 < (xj - xi) * (y + 0.5 - yi) / (yj - yi) + xi) inside = !inside;
    }
    if (inside) b.set(x, y, typeof c === 'function' ? c(x, y) : c);
  }
}


/**
 * A drawing typed out pixel by pixel: `rows` are strings, each character a key into `map` (a palette colour),
 * '.' or ' ' left empty. For the smallest pictures, where a shape function has nothing left to decide.
 */
function grid(b, rows, map, x0 = 0, y0 = 0) {
  rows.forEach((row, y) => [...row].forEach((ch, x) => { if (map[ch]) b.set(x0 + x, y0 + y, map[ch]); }));
}

/** Load a sprite module by id from ./sprites. */
/** A line through these points, every pixel touching the last - one strand, never a dotted line. */
function line(b, pts, c) {
  for (let i = 0; i < pts.length - 1; i++) {
    const [ax, ay] = pts[i], [bx, by] = pts[i + 1];
    const n = Math.max(1, Math.abs(bx - ax), Math.abs(by - ay));
    for (let k = 0; k <= n; k++) b.set(ax + (bx - ax) * k / n, ay + (by - ay) * k / n, c);
  }
}

/**
 * A round-ended stroke `r` px thick (half-width) along the points `pts`, shaded across its width by the light from the
 * upper left: `pick(u, t, x, y)` gets u from -1 (the side away from the light) to 1 (the side facing it) and t from 0 at
 * the first point to 1 at the last, and returns a colour or null. What a handle, a shaft or a wire is drawn with - the
 * tools (2026-10-13) - so the shading of a long thin thing is decided once.
 */
function stroke(b, pts, r, pick) {
  const xs = pts.map(p => p[0]), ys = pts.map(p => p[1]);
  const x0 = Math.floor(Math.min(...xs) - r - 1), x1 = Math.ceil(Math.max(...xs) + r + 1);
  const y0 = Math.floor(Math.min(...ys) - r - 1), y1 = Math.ceil(Math.max(...ys) + r + 1);
  const lens = [0];
  for (let i = 1; i < pts.length; i++) lens.push(lens[i - 1] + Math.hypot(pts[i][0] - pts[i - 1][0], pts[i][1] - pts[i - 1][1]));
  const total = lens[lens.length - 1] || 1, L = Math.SQRT1_2;
  for (let y = y0; y <= y1; y++) for (let x = x0; x <= x1; x++) {
    let best = 1e9, ox = 0, oy = 0, at = 0;
    for (let i = 0; i < pts.length - 1; i++) {
      const [ax, ay] = pts[i], [bx, by] = pts[i + 1], dx = bx - ax, dy = by - ay, len2 = dx * dx + dy * dy || 1;
      const k = Math.max(0, Math.min(1, ((x - ax) * dx + (y - ay) * dy) / len2));
      const px = ax + dx * k, py = ay + dy * k, d = Math.hypot(x - px, y - py);
      if (d < best) { best = d; ox = x - px; oy = y - py; at = (lens[i] + Math.sqrt(len2) * k) / total; }
    }
    if (best > r) continue;
    const c = pick(Math.max(-1, Math.min(1, (-ox * L - oy * L) / r)), at, x, y);
    if (c) b.set(x, y, c);
  }
}

/**
 * A small lit sphere of radius `r` at (cx, cy), shaded across `ramp` (dark to light) by the key light from the upper
 * left, with the ramp's top step as the specular. For bullets, coins, knobs and handle ends - anything round a few
 * pixels across.
 */
function ball(b, cx, cy, r, ramp) {
  const L = [-0.5, -0.62, 0.6], n = Math.hypot(...L), top = ramp.length - 1;
  for (let dy = -Math.ceil(r); dy <= Math.ceil(r); dy++) for (let dx = -Math.ceil(r); dx <= Math.ceil(r); dx++) {
    const nx = dx / (r + 0.5), ny = dy / (r + 0.5), k = nx * nx + ny * ny;
    if (k > 1) continue;
    const nz = Math.sqrt(1 - k), lit = Math.max(0, (nx * L[0] + ny * L[1] + nz * L[2]) / n);
    b.set(cx + dx, cy + dy, ramp[lit > 0.93 ? top : Math.min(top - 1, Math.floor(0.15 * top + lit * (top - 0.4)))]);
  }
}

function load(id) {
  const spec = require(`./sprites/${id}.js`);
  if (!spec.id) spec.id = id;
  return spec;
}
/**
 * Draw the sprite. `spec.offset` translates the finished art inside the canvas,
 * which is how a drawing satisfies TO-DRAW's "where the art must sit" column
 * without every coordinate in the draw function being re-derived by hand.
 */
function render(spec) {
  const b = new Buf(spec.canvas[0], spec.canvas[1]);
  spec.draw(b, module.exports, P);
  const [dx, dy] = spec.offset || [0, 0];
  if (!dx && !dy) return b;
  const out = new Buf(b.w, b.h);
  for (let y = 0; y < b.h; y++) for (let x = 0; x < b.w; x++) {
    const c = b.get(x, y);
    if (c !== null) out.set(x + dx, y + dy, c, b.tag(x, y));
  }
  return out;
}

module.exports = {
  Buf, rect, span, ehw, ellipse, roundRect, bay, band, rimLight, outline, rng, poly,
  grid, line, stroke, ball,
  load, render, P, png
};
