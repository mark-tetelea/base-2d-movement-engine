// Trail ground: a splash of hot red goo left behind a trail enemy, drawn over the floor
// (the gaps show the floor through). It hurts, so it shares red ground's fire ramp, but
// patchy and darker so it reads as "a mess on the floor", not a pool. Tile pivot.
module.exports = {
  canvas: [16, 16],
  ramps: ['fire'],
  margin: 0,
  draw(b, K) {
    const [f0, f1, f2] = K.P.ramps.fire;
    K.ellipse(b, 8, 8, 6, 5, f0);
    K.ellipse(b, 3, 12, 2, 2, f0);
    K.ellipse(b, 13, 3, 2, 1, f0);
    K.ellipse(b, 7, 7, 4, 3, f1);
    b.set(5, 6, f2); b.set(6, 5, f2); b.set(10, 9, f1); b.set(3, 11, f1);
  }
};
