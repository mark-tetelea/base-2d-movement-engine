// Lever, off (handle to the left). The "on" look is the same drawing flipped in Unity.
// Stands on the floor: pivot bottom-centre.
module.exports = {
  canvas: [16, 16],
  ramps: ['steel', 'plum'],
  margin: 0,
  draw(b, K) {
    const st = K.P.ramps.steel, r = K.P.ramps.plum;
    K.grid(b, [
      '................',
      '................',
      '................',
      '...KKK..........',
      '..KRrrK.........',
      '..KrrdK.........',
      '...KKKK.........',
      '.....KlK........',
      '......KlK.......',
      '.......KlK......',
      '....KKKKKKKK....',
      '...KhhhhhhhhK...',
      '...KhHHHHHHmK...',
      '...KmmmmmmmmK...',
      '....KKKKKKKK....',
      '................',
    ], { K: st[0], m: st[3], h: st[4], H: st[5], l: st[5], d: r[0], r: r[1], R: r[2] });
  }
};
