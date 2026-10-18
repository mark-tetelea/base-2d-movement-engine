#!/usr/bin/env node
// Reads Temp/measure-movement.csv (written by movement.cs) and prints what was measured.
//   node Tools/measure/analyse-movement.js [path.csv]
// The promise under test (CLAUDE.md, Decision 1): the same keys give the same movement at
// any frame rate. Each section prints the numbers, then OK or DIFFERENT where a check applies.
const fs = require('fs');
const path = require('path');

const file = process.argv[2] || path.join(__dirname, '..', '..', 'Temp', 'measure-movement.csv');
const rows = fs.readFileSync(file, 'utf8').trim().split('\n').slice(1).map(l => {
  const c = l.split(',');
  return { run: +c[0], kind: c[1], fps: +c[2], feel: c[3], phase: c[4], ft: +c[5], rt: +c[6], frame: +c[7],
           x: +c[8], y: +c[9], vx: +c[10], vy: +c[11] };
});
// Trap 54: a check that ran on nothing must not look clean.
if (!rows.length || rows.some(r => Number.isNaN(r.x))) { console.log(`no usable rows in ${file}`); process.exit(1); }
const runs = new Map();
for (const r of rows) { if (!runs.has(r.run)) runs.set(r.run, []); runs.get(r.run).push(r); }
const fpsOf = rs => (rs[rs.length - 1].frame - rs[0].frame) / (rs[rs.length - 1].rt - rs[0].rt);
console.log(`${rows.length} rows, ${runs.size} segments, from ${file}`);

console.log('\nLANE: hold D 2 s, release. run  fps(actual)  feel | first second | steady tiles/s (game, real clock) | moving steps | total | stop');
const seqs = {};
for (const [run, rs] of runs) {
  if (rs[0].kind !== 'lane') continue;
  const hold = rs.filter(r => r.phase.startsWith('hold'));
  const k0 = rs.findIndex(r => r.vx > 1e-6);
  const x0 = rs[k0 - 1].x;
  const a = rs[k0 + 30], b = rs[k0 + 90];
  const moving = rs.filter(r => r.vx > 1e-6).length;
  const xf = rs[rs.length - 1].x;
  let lastTop = -1; rs.forEach((r, i) => { if (r.vx > 0 && Math.abs(r.vx - a.vx) < 1e-4) lastTop = i; });
  console.log(`  ${String(run).padStart(2)}  ${rs[0].fps}(${fpsOf(hold).toFixed(1)})  ${rs[0].feel.padEnd(7)} | ${(rs[k0 + 59].x - x0).toFixed(4)} | ` +
    `${((b.x - a.x) / (b.ft - a.ft)).toFixed(4)}, ${((b.x - a.x) / (b.rt - a.rt)).toFixed(3)} | ${moving} | ${(xf - x0).toFixed(4)} | ${(xf - rs[lastTop].x).toFixed(4)}`);
  (seqs[rs[0].feel] ||= []).push({ run, fps: rs[0].fps, xs: rs.slice(k0, k0 + 120).map(r => r.x - x0) });
}
for (const [feel, list] of Object.entries(seqs)) {
  for (const s of list.slice(1)) {
    let d = 0; for (let i = 0; i < 120; i++) d = Math.max(d, Math.abs(s.xs[i] - list[0].xs[i]));
    console.log(`  ${feel}: first 120 moving steps, run ${s.run} (${s.fps} fps) vs run ${list[0].run} (${list[0].fps} fps): max difference ${d} tiles ${d === 0 ? 'OK' : 'DIFFERENT'}`);
  }
}

const names = ['W', 'W+D', 'D', 'S+D', 'S', 'S+A', 'A', 'W+A'];
for (const [, rs] of runs) {
  if (rs[0].kind !== 'dirs') continue;
  console.log(`\nDIRECTIONS (${rs[0].feel}, ${rs[0].fps} fps): velocity at the end of each 0.3 s hold`);
  for (let i = 0; i < 8; i++) {
    const seg = rs.filter(r => r.phase === 'dir' + i), r = seg[seg.length - 1];
    console.log(`  ${names[i].padEnd(4)} v=(${r.vx.toFixed(3)}, ${r.vy.toFixed(3)})  speed ${Math.hypot(r.vx, r.vy).toFixed(4)}  angle ${(Math.atan2(r.vy, r.vx) * 180 / Math.PI).toFixed(1)}`);
  }
}

for (const [, rs] of runs) {
  if (rs[0].kind !== 'taps') continue;
  const per = [];
  for (let i = 0; i < 10; i++) per.push(rs.filter(r => (r.phase === 'tap' + i || r.phase === 'tapwait' + i) && Math.abs(r.vx) > 1e-6).length);
  console.log(`\nTAPS (${rs[0].feel}, ${fpsOf(rs).toFixed(1)} fps; each tap visible to one frame): steps moved per tap ${per.join(' ')} ` +
    `${per.every(n => n >= 1) ? 'OK - none lost' : 'DIFFERENT - a tap was lost'}`);
}

console.log('\nWALLS (left wall face at x = -19; collider radius 0.375, so contact near x = -18.625)');
for (const [run, rs] of runs) {
  if (rs[0].kind !== 'wall' && rs[0].kind !== 'slide') continue;
  const hold = rs.filter(r => r.phase.startsWith('hold'));
  const late = hold[Math.floor(hold.length * 0.75)];
  console.log(`  ${String(run).padStart(2)} ${rs[0].kind.padEnd(5)} ${rs[0].feel.padEnd(7)} min x ${Math.min(...rs.map(r => r.x)).toFixed(4)}; ` +
    `late in the hold v=(${late.vx.toFixed(3)}, ${late.vy.toFixed(3)})`);
}
