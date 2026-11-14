// Crown: the Little RPG room's treasure. Pivot centre (an icon / floor item).
module.exports = {
  canvas: [12, 12],
  ramps: ['fire', 'steel', 'blue'],
  margin: 0,
  draw(b, K) {
    const st = K.P.ramps.steel, fi = K.P.ramps.fire, bl = K.P.ramps.blue;
    K.grid(b, [
      '............',
      '............',
      '.K...KK...K.',
      'KYK.KYYK.KYK',
      'KyYKyYYyKYyK',
      'KyyYyyyyYyyK',
      'KyyyyBByyyyK',
      'KyyyyBByyyoK',
      'KoyyyyyyyooK',
      'KKKKKKKKKKKK',
      '............',
      '............',
    ], { K: st[0], o: fi[2], y: fi[3], Y: fi[4], B: bl[2] });
  }
};
