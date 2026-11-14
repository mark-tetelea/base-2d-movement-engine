// Health pickup: a red heart. Pivot centre (an icon / floor item).
module.exports = {
  canvas: [8, 8],
  ramps: ['plum', 'steel'],
  margin: 0,
  draw(b, K) {
    const st = K.P.ramps.steel, r = K.P.ramps.plum;
    K.grid(b, [
      '.KK.KK..',
      'KRWKrrK.',
      'KRrrrrdK',
      'KrrrrrdK',
      '.KrrrdK.',
      '..KrdK..',
      '...KK...',
      '........',
    ], { K: st[0], d: r[0], r: r[1], R: r[2], W: st[6] });
  }
};
