#!/usr/bin/env node
// Build the ONE image an agent is allowed to look at per revision.
//
// Why this exists: a loose contact sheet of the whole set at 6x was 2536x1744.
// Claude downscales that to 1568px on the long edge and charges ~2250 tokens
// for it, and roughly 60% of those pixels were empty background. Cropping to
// the art, dropping to 4x and reviewing one sprite at a time costs 20-800
// tokens for the same judgement. This tool does the cropping so the saving is
// automatic rather than something to remember.
//
//   node Tools/pxkit/review.js ring              one sprite, cropped, 4x + 1x
//   node Tools/pxkit/review.js ring pearl ingot  packed in a row
//   node Tools/pxkit/review.js customer -s 3     scale down for a tall sprite
//   node Tools/pxkit/review.js ring --full       whole canvas, with anchor tick
//   node Tools/pxkit/review.js --png a.png       an Aseprite export
const fs = require('fs');
const path = require('path');
const K = require('./kit');
const P = require('./palette');

const OUT = path.join(__dirname, 'out');
const hx = c => [parseInt(c.slice(1,3),16), parseInt(c.slice(3,5),16), parseInt(c.slice(5,7),16)];

function main() {
  const args = process.argv.slice(2);
  const num = (...names) => {
    for (const n of names) { const i = args.indexOf(n); if (i >= 0) return +args[i + 1]; }
    return null;
  };
  const asked = num('-s', '--scale');           // explicit scale wins over the budget
  const budget = num('-b', '--budget') ?? 400;  // tokens you are willing to spend on one look
  const full = args.includes('--full');
  const pad = 2;
  let scale = asked;

  let panels = [];
  if (args.includes('--png')) {
    const file = args[args.indexOf('--png') + 1];
    const { w, h, rgba } = K.png.read(file);
    const at = (x, y) => {
      const i = (y * w + x) * 4;
      return rgba[i + 3] < 128 ? null
        : '#' + [rgba[i], rgba[i+1], rgba[i+2]].map(v => v.toString(16).padStart(2, '0')).join('');
    };
    panels = [{ id: path.basename(file), w, h, at, spec: {} }];
  } else {
    const ids = args.filter(a => !a.startsWith('-') && !/^\d+$/.test(a));
    const list = ids.length ? ids
      : fs.readdirSync(path.join(__dirname, 'sprites')).filter(f => f.endsWith('.js')).map(f => f.slice(0, -3));
    panels = list.map(id => {
      const spec = K.load(id);
      const b = K.render(spec);
      return { id, w: b.w, h: b.h, at: (x, y) => b.get(x, y), spec };
    });
  }

  // Crop each panel to its art unless --full. This is where the saving is.
  for (const p of panels) {
    let box = { x0: 0, y0: 0, x1: p.w - 1, y1: p.h - 1 };
    if (!full) {
      let x0 = p.w, y0 = p.h, x1 = -1, y1 = -1;
      for (let y = 0; y < p.h; y++) for (let x = 0; x < p.w; x++) {
        if (p.at(x, y)) {
          if (x < x0) x0 = x; if (x > x1) x1 = x;
          if (y < y0) y0 = y; if (y > y1) y1 = y;
        }
      }
      if (x1 >= 0) box = {
        x0: Math.max(0, x0 - pad), y0: Math.max(0, y0 - pad),
        x1: Math.min(p.w - 1, x1 + pad), y1: Math.min(p.h - 1, y1 + pad)
      };
    }
    p.box = box;
    p.cw = box.x1 - box.x0 + 1;
    p.ch = box.y1 - box.y0 + 1;
  }

  // What a look costs.
  //
  //   W = gut + SUM( cw_i * s_i + gut )
  //   H = MAX( ch_i * (s_i + 1) + 6 ) + 2*gut         (the 1x sample sits under the zoom)
  //   tokens = W * H / 750
  //
  // 1568 IS A HARD CEILING, NOT A DISCOUNT. Above it the image is resampled down before
  // anyone sees it, and a fractional resample destroys the pixel grid - the one thing this
  // kit exists to protect. The old formula billed against the capped size, which made the
  // cost PLATEAU past the ceiling instead of climbing: one 64x64 panel bills ~3,140 tokens
  // at 24x and ~3,240 at 100x, asymptotic to 1568*1568/750. So past the ceiling you pay the
  // same and get a blurrier picture, every time. Refuse to cross it and f is always 1, which
  // is why it has gone from the formula.
  //
  // EACH PANEL GETS ITS OWN SCALE. One shared scale is set by the tallest sprite, so putting
  // the 288px customer beside the 32px ingot dragged every panel to 1x and the small ones
  // became unreadable. What matters is not the scale but the RENDERED size, and a 32px sprite
  // at 8x and a 288px sprite at 1x are about equally readable. So aim every panel at one
  // rendered height T and solve for the largest T that fits both the ceiling and the budget.
  const gut = 10;
  const CAP = 1568;
  const scalesFor = T => panels.map(p => Math.max(1, Math.min(8, Math.round(T / p.ch))));
  const measure = ss => {
    const W = panels.reduce((a, p, i) => a + p.cw * ss[i] + gut, gut);
    const H = Math.max(...panels.map((p, i) => p.ch * (ss[i] + 1) + 6)) + gut * 2;
    return { ss, W, H, over: Math.max(W, H) > CAP, tok: Math.round(W * H / 750) };
  };

  let plan, clamped = false;
  if (asked) {
    plan = measure(panels.map(() => asked));
    // A hand-picked scale still may not cross the ceiling - the picture would come back
    // resampled and the judgement made on it would be worthless.
    for (let s = asked; s >= 1 && plan.over; s--) {
      plan = measure(panels.map(() => s));
      clamped = s < asked;
    }
  } else {
    for (let T = 1400; T >= 8; T--) {
      const c = measure(scalesFor(T));
      if (!c.over && c.tok <= budget) { plan = c; break; }
    }
    if (!plan) plan = measure(panels.map(() => 1));   // nothing fits; ship 1x and say so below
  }
  const { W, H, tok, ss } = plan;
  scale = Math.max(...ss);
  const img = Buffer.alloc(W * H * 4);
  const [gr, gg, gb] = hx(P.GROUND);
  for (let i = 0; i < W * H; i++) { img[i*4] = gr; img[i*4+1] = gg; img[i*4+2] = gb; img[i*4+3] = 255; }
  const put = (x, y, c) => {
    if (x < 0 || y < 0 || x >= W || y >= H) return;
    const [r, g, b] = hx(c), i = (y * W + x) * 4;
    img[i] = r; img[i+1] = g; img[i+2] = b;
  };

  let ox = gut;
  panels.forEach((p, i) => {
    const s = ss[i];                                // this panel's own scale
    p.colW = p.cw * s;                              // big view, 1x sample sits under it
    // the zoomed view
    for (let y = 0; y < p.ch; y++) for (let x = 0; x < p.cw; x++) {
      const c = p.at(p.box.x0 + x, p.box.y0 + y);
      if (!c) continue;
      for (let sy = 0; sy < s; sy++) for (let sx = 0; sx < s; sx++)
        put(ox + x * s + sx, gut + y * s + sy, c);
    }
    // where the art must sit, drawn so it can be checked by eye
    if (full && p.spec && p.spec.anchorBottom != null) {
      const ay = gut + p.spec.anchorBottom * s + s - 1;
      for (let x = 0; x < p.cw * s; x += 2) put(ox + x, ay, P.TICK);
    }
    // the 1x sample directly beneath, same ground
    const by = gut + p.ch * s + 6;
    for (let y = 0; y < p.ch; y++) for (let x = 0; x < p.cw; x++) {
      const c = p.at(p.box.x0 + x, p.box.y0 + y);
      if (c) put(ox + x, by + y, c);
    }
    ox += p.colW + gut;
  });

  fs.mkdirSync(OUT, { recursive: true });
  const file = path.join(OUT, '_review.png');
  K.png.write(file, W, H, img);

  // Keep the cost visible. An agent that cannot see the price will not manage it.
  console.log(`${file}  ${W}x${H}  ${panels.map((p, i) => `${p.id} ${ss[i]}x`).join('  ')}`);
  console.log(`~${tok} image tokens to look at this.` +
    (asked ? `  (scale fixed by hand${clamped ? `, clamped to ${scale}x by the ${CAP}px ceiling` : ''})`
           : `  (largest rendered size inside a ${budget}-token budget)`) +
    (tok > budget ? `  <- over budget even at ${scale}x: review fewer sprites at once.` : ''));
  // A panel the budget pushed below 3x is not a review, it is a thumbnail. Say WHICH ones,
  // because with per-panel scales the small sprites are usually fine and one tall one is not.
  const thumbs = panels.filter((p, i) => ss[i] < 3).map((p, i2, a) => p.id);
  if (!asked && thumbs.length) {
    console.log(`too small to judge at this size: ${thumbs.join(', ')}. ` +
      (panels.length > 1
        ? 'Review those on their own, or raise --budget.'
        : 'Raise --budget, or this sprite is too big to review whole.'));
  }
}

main();
