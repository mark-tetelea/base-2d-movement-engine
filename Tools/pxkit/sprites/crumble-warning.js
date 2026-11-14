// Crumbling ground about to break: the same slab split by dark cracks with a few
// holes through to the void - the warning between the step and the break.
module.exports = {
  canvas: [16, 16],
  ramps: ['wood', 'steel'],
  floor: true,
  draw(b, K) {
    const [w0, w1, w2, w3] = K.P.ramps.wood;
    const s0 = K.P.ramps.steel[0];
    K.rect(b, 0, 0, 15, 15, w2);
    K.span(b, 0, 15, 15, w1); K.rect(b, 15, 0, 15, 15, w1);
    K.span(b, 0, 5, 0, w3); K.rect(b, 0, 1, 0, 3, w3);
    K.line(b, [[1, 3], [5, 5], [7, 7], [7, 10], [12, 13]], w0);
    K.line(b, [[7, 7], [11, 5], [14, 2]], w0);
    K.line(b, [[5, 5], [3, 9], [1, 11]], w0);
    for (const [x, y] of [[7, 8], [6, 7], [11, 5], [3, 9]]) b.set(x, y, s0);
  }
};
