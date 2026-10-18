#!/usr/bin/env node
// Write finished 1x PNGs, and make colour variants mechanically by ramp swap
// (draw an enemy once in plum, get it in green and blue for free).
//
//   node Tools/pxkit/export.js                          all -> Tools/pxkit/out/
//   node Tools/pxkit/export.js coin --dir Assets/Art/Generated
//   node Tools/pxkit/export.js slime --swap plum:green,blue
const fs = require('fs');
const path = require('path');
const K = require('./kit');
const P = require('./palette');

/**
 * Swap one ramp for another by position, so ramps of different length still
 * map: fire-3 of 5 lands on blue-2 of 3. Anything outside `from` is untouched.
 */
function swapRamp(buf, from, to) {
  const a = P.ramps[from], b = P.ramps[to];
  if (!a || !b) throw new Error(`unknown ramp: ${!a ? from : to}`);
  const map = new Map(a.map((hex, i) =>
    [hex, b[a.length === 1 ? 0 : Math.round(i / (a.length - 1) * (b.length - 1))]]));
  const out = new K.Buf(buf.w, buf.h);
  out.px = buf.px.map(c => (c && map.get(c)) || c);
  return out;
}

function main() {
  const args = process.argv.slice(2);
  const flag = n => { const i = args.indexOf(n); return i < 0 ? null : args[i + 1]; };
  const dir = path.resolve(flag('--dir') || path.join(__dirname, 'out'));
  const ids = args.filter(a => !a.startsWith('-') && a !== flag('--dir') && a !== flag('--swap'));
  const list = ids.length ? ids
    : fs.readdirSync(path.join(__dirname, 'sprites')).filter(f => f.endsWith('.js')).map(f => f.slice(0, -3));

  fs.mkdirSync(dir, { recursive: true });
  for (const id of list) {
    const buf = K.render(K.load(id));
    buf.save(path.join(dir, `${id}.png`));
    console.log(`${id}.png  ${buf.w}x${buf.h}`);
    if (flag('--swap')) {
      const [from, tos] = flag('--swap').split(':');
      for (const to of tos.split(',')) {
        swapRamp(buf, from, to).save(path.join(dir, `${id}-${to}.png`));
        console.log(`${id}-${to}.png  ${from} -> ${to}`);
      }
    }
  }
  console.log(`\n-> ${dir}`);
}

if (require.main === module) main();
module.exports = { swapRamp };
