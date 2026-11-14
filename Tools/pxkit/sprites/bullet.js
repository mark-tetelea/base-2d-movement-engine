// Placeholder bullet: a small bright ball with a dark outline, so it reads on the floor.
// White core, tinted in game by the weapon's bullet colour. Pivot centre.
module.exports = {
  canvas: [6, 6],
  ramps: ['steel'],
  margin: 0,
  draw(b, K) {
    const st = K.P.ramps.steel;
    K.grid(b, [
      '.KKKK.',
      'KWWWWK',
      'KWWWwK',
      'KWWWwK',
      'KWwwwK',
      '.KKKK.',
    ], { K: st[0], W: st[4], w: st[3] });
  }
};
