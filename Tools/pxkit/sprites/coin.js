// Coin pickup and inventory icon: a small gold disc. Pivot centre (an icon).
module.exports = {
  canvas: [8, 8],
  ramps: ['fire', 'steel'],
  margin: 0,
  draw(b, K) {
    const st = K.P.ramps.steel, fi = K.P.ramps.fire;
    K.grid(b, [
      '..KKKK..',
      '.KyYYyK.',
      'KyYWYyyK',
      'KyYYyyoK',
      'KyYyyyoK',
      'KyyyyooK',
      '.KyoooK.',
      '..KKKK..',
    ], { K: st[0], o: fi[2], y: fi[3], Y: fi[4], W: st[6] });
  }
};
