// Placeholder enemy: a round blob with two eyes, one drawing for every behaviour; the
// behaviours are told apart by colour variants (export --swap). Pivot centre: it has no
// facing and no feet, and its circle collider sits on the centre.
module.exports = {
  canvas: [16, 16],
  ramps: ['plum', 'steel'],
  margin: 1,
  draw(b, K) {
    const r = K.P.ramps.plum, st = K.P.ramps.steel;
    K.grid(b, [
      '................',
      '................',
      '.....KKKKKK.....',
      '....KrrRRrrK....',
      '...KrrRRRRrrK...',
      '..KrrRRRRRRrrK..',
      '..KrWWrRRrWWrK..',
      '..KrWKrrrrWKrK..',
      '..KrrrrrrrrrrK..',
      '..KdrrrrrrrrdK..',
      '..KddrrKKrrddK..',
      '...KddrrrrddK...',
      '....KKddddKK....',
      '......KKKK......',
      '................',
      '................',
    ], { K: st[0], d: r[0], r: r[1], R: r[2], W: st[4] });
  }
};
