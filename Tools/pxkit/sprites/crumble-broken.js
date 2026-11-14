// Crumbling ground, broken: a hole down to the void, with a sliver of slab left along the
// top edge (the far wall of the hole, lit from above) and a little rubble.
module.exports = {
  canvas: [16, 16],
  ramps: ['wood', 'steel'],
  floor: true,
  draw(b, K) {
    const [w0, w1, w2] = K.P.ramps.wood;
    const s0 = K.P.ramps.steel[0];
    K.rect(b, 0, 0, 15, 15, s0);
    K.span(b, 0, 15, 0, w2);
    K.span(b, 0, 15, 1, w1);
    K.span(b, 1, 14, 2, w0);
    for (const [x, y] of [[4, 9], [11, 12], [8, 6]]) b.set(x, y, w0);
    b.set(5, 9, w1);
  }
};
