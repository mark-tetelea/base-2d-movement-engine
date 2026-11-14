// Placeholder boss: a big horned blob, twice an enemy's size (2x2 tiles), in the
// enemies' plum with fire-coloured eyes and horns so it reads as "the dangerous one".
// Pivot centre, like the enemies: its circle collider sits on the centre.
module.exports = {
  canvas: [32, 32],
  ramps: ['plum', 'fire', 'steel'],
  margin: 1,
  draw(b, K) {
    const pl = K.P.ramps.plum, fi = K.P.ramps.fire, st = K.P.ramps.steel;
    // Body: dark base, mid fill, light toward the upper left.
    K.ellipse(b, 16, 18, 12, 11, pl[0]);
    K.ellipse(b, 15, 17, 11, 9, pl[1]);
    K.ellipse(b, 12, 13, 5, 3, pl[2]);
    // Horns.
    K.poly(b, [[6, 11], [3, 2], [10, 8]], fi[1]);
    K.poly(b, [[26, 11], [29, 2], [22, 8]], fi[1]);
    b.set(4, 4, fi[3]); b.set(28, 4, fi[3]);
    // Eyes and a mouth.
    K.rect(b, 9, 16, 13, 18, st[0]);
    K.rect(b, 19, 16, 23, 18, st[0]);
    K.rect(b, 10, 16, 12, 17, fi[4]);
    K.rect(b, 20, 16, 22, 17, fi[4]);
    K.rect(b, 11, 23, 21, 24, st[0]);
    for (const x of [12, 15, 18, 21]) b.set(x, 23, st[6]);
    // Outer outline: every painted pixel next to an empty one.
    const edge = [];
    for (let y = 0; y < b.h; y++) for (let x = 0; x < b.w; x++) {
      if (b.get(x, y) === null) continue;
      if ([[1, 0], [-1, 0], [0, 1], [0, -1]].some(([dx, dy]) => b.get(x + dx, y + dy) === null)) edge.push([x, y]);
    }
    for (const [x, y] of edge) b.set(x, y, st[0]);
  }
};
