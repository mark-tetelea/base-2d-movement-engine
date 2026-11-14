// Key item: a gold key lying on its side. Pivot centre (an icon / floor item).
module.exports = {
  canvas: [12, 12],
  ramps: ['fire', 'steel'],
  margin: 0,
  draw(b, K) {
    const st = K.P.ramps.steel, fi = K.P.ramps.fire;
    K.grid(b, [
      '............',
      '............',
      '............',
      '.KKKK.......',
      'KyYYyK......',
      'KY..yKKKKKKK',
      'KY..yyyyyyyK',
      'KyyyoKKoKoKK',
      '.KKKK.KK.K..',
      '............',
      '............',
      '............',
    ], { K: st[0], o: fi[2], y: fi[3], Y: fi[4] });
  }
};
