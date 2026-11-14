// The interaction prompt: an "E" key cap shown over the current target. Pivot centre.
module.exports = {
  canvas: [10, 10],
  ramps: ['steel'],
  margin: 0,
  draw(b, K) {
    const st = K.P.ramps.steel;
    K.grid(b, [
      '.KKKKKKKK.',
      'KWWWWWWWhK',
      'KWWKKKKWhK',
      'KWWKWWWWhK',
      'KWWKKKWWhK',
      'KWWKWWWWhK',
      'KWWKKKKWhK',
      'KWWWWWWWhK',
      'KhhhhhhhhK',
      '.KKKKKKKK.',
    ], { K: st[0], W: st[6], h: st[4] });
  }
};
