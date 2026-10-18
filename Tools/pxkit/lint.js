#!/usr/bin/env node
// ART.md's drawing rules, as executable tests.
//
// WORKING-AGREEMENT.md: "Design rules get stated as a test, not as taste."
// Every check below is one sentence from ART.md or TO-DRAW.md that used to be
// something a human had to remember and an agent had to burn an image look on.
//
//   node Tools/pxkit/lint.js                 all sprites in ./sprites
//   node Tools/pxkit/lint.js coin chest      named sprites
//   node Tools/pxkit/lint.js --png path.png [--ramps wood,steel] [--anchor 15] [--margin 1]
//
// Silence is a pass. Output is deliberately short: this runs many times per
// sprite and must never cost more than a few hundred tokens.
const fs = require('fs');
const path = require('path');
const K = require('./kit');
const P = require('./palette');

const SPRITES = path.join(__dirname, 'sprites');
const GROUND = P.GROUND;
const lum = hex => P.lum(hex);
// Math.max(...a) overflows the stack on a backdrop-sized array (~170K values), so reduce instead.
const most = a => a.reduce((m, v) => v > m ? v : m, -Infinity);
const least = a => a.reduce((m, v) => v < m ? v : m, Infinity);
const GROUND_L = lum(GROUND);

/* ------------------------------------------------ pixel source abstraction */
// A lint target is just {id, w, h, at(x,y)->hex|null, spec}. That lets the same
// rules run over a pxkit buffer and over a PNG exported from Aseprite.
function fromSpec(id) {
  const spec = K.load(id);
  const b = K.render(spec);
  return { id, w: b.w, h: b.h, at: (x, y) => b.get(x, y), spec, buf: b };
}
function fromPNG(file, spec) {
  const { w, h, rgba } = K.png.read(file);
  return {
    id: path.basename(file), w, h, spec,
    at(x, y) {
      if (x < 0 || y < 0 || x >= w || y >= h) return null;
      const i = (y * w + x) * 4;
      if (rgba[i + 3] < 128) return null;
      return '#' + [rgba[i], rgba[i+1], rgba[i+2]]
        .map(v => v.toString(16).padStart(2, '0')).join('');
    }
  };
}

/* ------------------------------------------------------------------ checks */
function analyse(t) {
  const spec = t.spec || {};
  const fails = [], warns = [];
  const solid = [], colours = new Map();
  for (let y = 0; y < t.h; y++) for (let x = 0; x < t.w; x++) {
    const c = t.at(x, y);
    if (!c) continue;
    solid.push([x, y, c]);
    colours.set(c, (colours.get(c) || 0) + 1);
  }
  if (!solid.length) { fails.push(['empty', 'no solid pixels']); return { fails, warns, solid, colours }; }

  const isEdge = (x, y) =>
    !t.at(x-1, y) || !t.at(x+1, y) || !t.at(x, y-1) || !t.at(x, y+1);

  // 1. Off-palette. Trap 60: a drawing can be entirely off-palette and look fine.
  const off = [...colours.keys()].filter(c => !P.byHex.has(c));
  if (off.length) fails.push(['palette',
    `${off.length} colour(s) not in ${P.name}: ${off.slice(0, 6).join(' ')}`]);

  // 2. Slot 0 is see-through and erases. It must never be painted.
  if (colours.has(P.SEE_THROUGH)) fails.push(['slot0',
    `${colours.get(P.SEE_THROUGH)} px painted with see-through (slot 0)`]);

  // 3. One ramp per material - only the ramps the spec declares.
  if (spec.ramps) {
    const allowed = new Set(spec.ramps);
    const stray = new Map();
    for (const [c, n] of colours) {
      const r = P.rampOf.get(c);
      if (r && !allowed.has(r)) stray.set(r, (stray.get(r) || 0) + n);
    }
    if (stray.size) fails.push(['ramps',
      `undeclared ramp(s): ${[...stray].map(([r, n]) => `${r}(${n}px)`).join(' ')}`]);
  }

  // 4. (Jewelery Shop's "warm means lit" rule was here; it was that game's art
  //    direction, not a rule of this kit, and was removed 2026-10-18.)

  // 5. Anything painted in the ground colour ON THE SILHOUETTE is the floor the
  //    sprite stands on and will simply not exist on screen (trap 66: a legal
  //    palette slot that happens to be the ground).
  const vanish = solid.filter(([x, y, c]) => c === GROUND && isEdge(x, y));
  if (vanish.length > 2) fails.push(['vanish',
    `${vanish.length} silhouette px are ${GROUND} = the floor colour; ` +
    `they will not read. First at (${vanish[0][0]},${vanish[0][1]})`]);

  // 6. Where the art must sit. Trap 59: a sprite swapped in must not jump.
  const ys = solid.map(s => s[1]), xs = solid.map(s => s[0]);
  const y1 = most(ys), y0 = least(ys);
  const x1 = most(xs), x0 = least(xs);
  if (spec.anchorBottom != null && y1 !== spec.anchorBottom) fails.push(['anchor',
    `bottom of art is row ${y1}, spec says ${spec.anchorBottom}`]);

  // 7. 2 px empty margin inside item cells, so nothing clips when outlines go on.
  if (spec.margin != null) {
    const m = spec.margin;
    const tight = [];
    if (x0 < m) tight.push(`left ${x0}`);
    if (y0 < m) tight.push(`top ${y0}`);
    if (t.w - 1 - x1 < m) tight.push(`right ${t.w - 1 - x1}`);
    if (spec.anchorBottom == null && t.h - 1 - y1 < m) tight.push(`bottom ${t.h - 1 - y1}`);
    if (tight.length) fails.push(['margin', `need ${m}px, got ${tight.join(', ')}`]);
  }

  // 8. Stray pixels: solid with no solid 4-neighbour. Almost always a bug.
  const strays = solid.filter(([x, y]) =>
    !t.at(x-1, y) && !t.at(x+1, y) && !t.at(x, y-1) && !t.at(x, y+1));
  if (strays.length) warns.push(['strays',
    `${strays.length} isolated px: ${strays.slice(0, 4).map(s => `(${s[0]},${s[1]})`).join(' ')}`]);

  // 9. Ramp steps left on the table. Unused steps are contrast you paid for
  //    in the palette and did not spend.
  if (spec.ramps) {
    const unused = spec.ramps
      .map(r => {
        const miss = (P.ramps[r] || []).filter(h => !colours.has(h));
        return miss.length ? `${r} ${miss.length}/${P.ramps[r].length}` : null;
      })
      .filter(Boolean);
    if (unused.length) warns.push(['steps', `unused ramp steps: ${unused.join(', ')}`]);
  }

  // 10. Will it read at 1x against the floor? Not a pass/fail. An edge reads
  //     if it is clearly lighter OR darker than the floor - an outline in
  //     steel-0 on a steel-2 floor reads as well as a highlight does.
  const edge = solid.filter(([x, y]) => isEdge(x, y));
  const lit = edge.filter(([, , c]) => Math.abs(lum(c) - GROUND_L) > 16);
  const read = edge.length ? lit.length / edge.length : 0;
  if (read < 0.15) warns.push(['read1x',
    `only ${(read * 100).toFixed(0)}% of the silhouette stands out from the floor - check it reads at 1x`]);

  const L = solid.map(s => lum(s[2]));

  return {
    fails, warns, solid, colours,
    box: { x0, y0, x1, y1 },
    read,
    meanL: L.reduce((a, b) => a + b, 0) / L.length,
    peakL: most(L)
  };
}

/* ------------------------------------------------------------------ report */
function line(t, a) {
  const ramps = t.spec && t.spec.ramps ? t.spec.ramps.join(',') : [...new Set(
    [...a.colours.keys()].map(c => P.rampOf.get(c)).filter(Boolean))].join(',');
  return `${t.id.padEnd(11)} ${(t.w + 'x' + t.h).padEnd(8)} ${String(a.solid.length).padStart(5)}px  ` +
    `${String(a.colours.size).padStart(2)}col  ${ramps.padEnd(26)} ` +
    `L ${a.meanL.toFixed(0).padStart(3)}/${a.peakL.toFixed(0).padStart(3)}  ` +
    `edge ${(a.read * 100).toFixed(0).padStart(3)}%`;
}

function main() {
  const args = process.argv.slice(2);
  const flag = n => { const i = args.indexOf(n); return i < 0 ? null : args[i + 1]; };
  let targets = [];

  if (args.includes('--png')) {
    const spec = {};
    if (flag('--ramps')) spec.ramps = flag('--ramps').split(',');
    if (flag('--anchor')) spec.anchorBottom = +flag('--anchor');
    if (flag('--margin')) spec.margin = +flag('--margin');
    targets = [fromPNG(flag('--png'), spec)];
  } else {
    const ids = args.filter(a => !a.startsWith('-'));
    const list = ids.length ? ids
      : fs.readdirSync(SPRITES).filter(f => f.endsWith('.js')).map(f => f.slice(0, -3));
    targets = list.map(fromSpec);
  }

  // (Jewelery Shop's cross-sprite "items out-shine furniture" rule was here;
  // that game's art direction, removed 2026-10-18.)
  let bad = 0;
  for (const t of targets) {
    const a = analyse(t);
    console.log(line(t, a));
    for (const [k, m] of a.fails) { console.log(`  FAIL ${k.padEnd(8)} ${m}`); bad++; }
    for (const [k, m] of a.warns) console.log(`  WARN ${k.padEnd(8)} ${m}`);
  }
  // Trap 54: a check that ran on nothing must not say "clean".
  if (!targets.length) { console.log('nothing to lint.'); process.exit(1); }

  if (bad) { console.log(`\n${bad} failure(s).`); process.exit(1); }
  console.log('\nclean.');
}

if (require.main === module) main();
module.exports = { analyse, fromPNG, line };
