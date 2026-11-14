// Test-arena aim marker: a small yellow diamond with a dark outline, so it reads on the
// steel floor. Pivot centre (ART.md rule 7: effects and icons pivot in the centre).
module.exports = {
  canvas: [8, 8],
  ramps: ['fire', 'steel'],
  margin: 0,
  draw(b, K) {
    const st = K.P.ramps.steel, fi = K.P.ramps.fire;
    K.grid(b, [
      '...KK...',
      '..KyyK..',
      '.KyYYyK.',
      'KyY..YyK',
      'KyY..YyK',
      '.KyYYyK.',
      '..KyyK..',
      '...KK...',
    ], { K: st[0], y: fi[3], Y: fi[4] });
  }
};
