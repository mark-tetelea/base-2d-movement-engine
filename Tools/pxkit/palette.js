// The palette, read from Assets/Art/Palette/endesga-32.gpl at load. There is no second copy.
//
// Why: a drawing can be entirely off-palette and look completely fine (trap 60). A hardcoded
// copy of the palette in here would drift from the .gpl the moment a slot moves, and then the
// lint would be checking against a lie - which is worse than not checking at all.
// This file reads the .gpl and never writes it (trap 70).
const fs = require('fs');
const path = require('path');

const GPL = path.join(__dirname, '..', '..', 'Assets', 'Art', 'Palette', 'endesga-32.gpl');

function parse(file) {
  const lines = fs.readFileSync(file, 'utf8').split(/\r?\n/);
  const slots = [];
  for (const line of lines) {
    if (!line.trim() || line.startsWith('#') || /^(GIMP Palette|Name:|Columns:)/.test(line)) continue;
    const m = line.match(/^\s*(\d+)\s+(\d+)\s+(\d+)\s+(.+?)\s*$/);
    if (!m) continue;
    const [, r, g, b, name] = m;
    slots.push({
      index: slots.length,
      name: name.trim(),
      r: +r, g: +g, b: +b,
      hex: '#' + [+r, +g, +b].map(v => v.toString(16).padStart(2, '0')).join('')
    });
  }
  return slots;
}

const slots = parse(GPL);

// Ramps are recovered from the slot names (`steel-0` .. `steel-6`), so adding
// a ramp to the .gpl is enough - nothing here needs editing.
const ramps = {};
for (const s of slots) {
  const m = s.name.match(/^([a-z]+)-\d+$/);
  if (!m) continue;
  (ramps[m[1]] ||= []).push(s.hex);
}

const byHex = new Map(slots.map(s => [s.hex, s]));
const rampOf = new Map();
for (const [name, list] of Object.entries(ramps)) list.forEach(h => rampOf.set(h, name));

const lum = hex => {
  const s = byHex.get(hex);
  if (!s) return 0;
  return 0.2126 * s.r + 0.7152 * s.g + 0.0722 * s.b;
};

module.exports = {
  slots, ramps, byHex, rampOf, lum,
  SEE_THROUGH: slots[0].hex,      // never paint with it; Aseprite index 0 erases
  GROUND: ramps.steel[2],         // the test arena's floor: what sprites are seen against,
                                  // so a silhouette in it does not read (trap 66)
  OUTLINE: ramps.steel[0],        // the darkest slot; also the void outside the arena
  TICK: ramps.steel[4],           // review.js's anchor tick
  name: path.basename(GPL),
  source: GPL
};
