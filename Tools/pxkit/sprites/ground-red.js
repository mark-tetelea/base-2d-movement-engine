// Red ground: kills whoever stands on it. Lava-like - a dark red pool with brighter
// bubbles and a hot glint, so it reads as danger against the steel floor at a glance.
module.exports = {
  canvas: [16, 16],
  ramps: ['fire'],
  floor: true,
  draw(b, K) {
    const [f0, f1, f2, f3] = K.P.ramps.fire;
    K.rect(b, 0, 0, 15, 15, f0);
    for (const [x, y, r] of [[4, 4, 2], [11, 9, 2], [3, 12, 1], [12, 2, 1]]) K.ellipse(b, x, y, r, r, f1);
    for (const [x, y] of [[3, 3], [10, 8], [12, 1]]) b.set(x, y, f2);
    b.set(4, 3, f3); b.set(10, 9, f2); b.set(7, 13, f1); b.set(8, 13, f1);
  }
};
